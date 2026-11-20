package com.example.fixnear.ui.home

import android.Manifest
import android.content.pm.PackageManager
import android.location.Location
import android.location.LocationListener
import android.location.LocationManager
import android.os.Bundle
import android.os.Handler
import android.os.Looper
import android.view.View
import androidx.activity.result.contract.ActivityResultContracts
import androidx.appcompat.app.AppCompatActivity
import androidx.core.content.ContextCompat
import androidx.lifecycle.lifecycleScope
import androidx.recyclerview.widget.LinearLayoutManager
import com.example.fixnear.data.remote.RetrofitClient
import com.example.fixnear.databinding.ActivityNearbyShopsBinding
import kotlinx.coroutines.launch
import com.example.fixnear.data.model.customer.NearbyShopItem
import com.google.gson.Gson
import com.google.gson.JsonElement
import com.google.gson.JsonObject
import android.util.Log

class NearbyShopsActivity : AppCompatActivity() {

    private lateinit var binding: ActivityNearbyShopsBinding
    private lateinit var adapter: NearbyShopAdapter
    private lateinit var locationManager: LocationManager

    private val handler = Handler(Looper.getMainLooper())

    private var serviceId = -1
    private var locationListener: LocationListener? = null
    private var scanTimeout: Runnable? = null
    private var scanCompleted = false

    private val permissionLauncher =
        registerForActivityResult(
            ActivityResultContracts.RequestMultiplePermissions()
        ) { permissions ->
            val granted =
                permissions[Manifest.permission.ACCESS_FINE_LOCATION] == true ||
                        permissions[Manifest.permission.ACCESS_COARSE_LOCATION] == true

            if (granted) {
                startLocationScan()
            } else {
                showState(
                    "Bạn cần cấp quyền vị trí để tìm tiệm gần bạn.",
                    false
                )
            }
        }

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)

        binding = ActivityNearbyShopsBinding.inflate(layoutInflater)
        setContentView(binding.root)

        serviceId = intent.getIntExtra("SERVICE_ID", -1)

        val serviceName =
            intent.getStringExtra("SERVICE_NAME")
                .orEmpty()

        binding.tvSelectedService.text =
            "Dịch vụ: $serviceName • Trong bán kính 10 km"

        locationManager =
            getSystemService(LOCATION_SERVICE) as LocationManager

        adapter = NearbyShopAdapter()

        binding.rvNearbyShops.apply {
            layoutManager = LinearLayoutManager(
                this@NearbyShopsActivity
            )
            adapter = this@NearbyShopsActivity.adapter
        }

        binding.btnBack.setOnClickListener {
            finish()
        }

        binding.btnScanAgain.setOnClickListener {
            checkPermissionAndScan()
        }

        checkPermissionAndScan()
    }

    private fun checkPermissionAndScan() {
        if (serviceId <= 0) {
            showState("Dịch vụ không hợp lệ.", false)
            return
        }

        val fine = ContextCompat.checkSelfPermission(
            this,
            Manifest.permission.ACCESS_FINE_LOCATION
        ) == PackageManager.PERMISSION_GRANTED

        val coarse = ContextCompat.checkSelfPermission(
            this,
            Manifest.permission.ACCESS_COARSE_LOCATION
        ) == PackageManager.PERMISSION_GRANTED

        if (fine || coarse) {
            startLocationScan()
        } else {
            permissionLauncher.launch(
                arrayOf(
                    Manifest.permission.ACCESS_FINE_LOCATION,
                    Manifest.permission.ACCESS_COARSE_LOCATION
                )
            )
        }
    }

    @Suppress("MissingPermission")
    private fun startLocationScan() {

        stopLocationScan()
        scanCompleted = false

        adapter.submitItems(emptyList())
        showState("Đang quét vị trí của bạn...", true)

        val fineGranted = ContextCompat.checkSelfPermission(
            this,
            Manifest.permission.ACCESS_FINE_LOCATION
        ) == PackageManager.PERMISSION_GRANTED

        val provider = when {
            fineGranted &&
                    locationManager.isProviderEnabled(
                        LocationManager.GPS_PROVIDER
                    ) -> LocationManager.GPS_PROVIDER

            locationManager.isProviderEnabled(
                LocationManager.NETWORK_PROVIDER
            ) -> LocationManager.NETWORK_PROVIDER

            else -> null
        }

        if (provider == null) {
            showState(
                "Vui lòng bật Vị trí trên điện thoại rồi quét lại.",
                false
            )
            return
        }

        try {
            val listener = object : LocationListener {

                override fun onLocationChanged(location: Location) {
                    if (scanCompleted) return

                    scanCompleted = true
                    stopLocationScan()

                    loadNearbyShops(
                        location.latitude,
                        location.longitude
                    )
                }

                override fun onProviderDisabled(provider: String) {
                    if (!scanCompleted) {
                        scanCompleted = true
                        stopLocationScan()
                        showState(
                            "Dịch vụ vị trí đã bị tắt. Hãy bật lại và quét.",
                            false
                        )
                    }
                }
            }

            locationListener = listener

            locationManager.requestSingleUpdate(
                provider,
                listener,
                Looper.getMainLooper()
            )

            val timeout = Runnable {
                if (!scanCompleted) {
                    scanCompleted = true
                    stopLocationScan()
                    showState(
                        "Chưa lấy được vị trí. Kiểm tra GPS và thử quét lại.",
                        false
                    )
                }
            }

            scanTimeout = timeout
            handler.postDelayed(timeout, 15000L)

        } catch (e: SecurityException) {
            showState("Ứng dụng chưa được cấp quyền vị trí.", false)
        } catch (e: Exception) {
            showState(
                "Không thể quét vị trí: ${e.localizedMessage}",
                false
            )
        }
    }

    private fun stopLocationScan() {
        locationListener?.let { listener ->
            try {
                locationManager.removeUpdates(listener)
            } catch (_: Exception) {
                // Không còn đăng ký vị trí.
            }
        }

        locationListener = null

        scanTimeout?.let { handler.removeCallbacks(it) }
        scanTimeout = null
    }

    private fun loadNearbyShops(
        latitude: Double,
        longitude: Double
    ) {
        Log.d(
            "FIXNEAR_GPS",
            "serviceId=$serviceId, " +
                    "latitude=$latitude, " +
                    "longitude=$longitude, radiusKm=10"
        )
        showState("Đang tìm tiệm sửa chữa gần bạn...", true)

        lifecycleScope.launch {
            try {
                val response =
                    RetrofitClient.customerDiscoveryApi.getNearbyShops(
                        serviceId = serviceId,
                        latitude = latitude,
                        longitude = longitude,
                        radiusKm = 10.0
                    )

                if (!response.isSuccessful) {
                    showState(
                        "Không tải được tiệm: HTTP ${response.code()}",
                        false
                    )
                    return@launch
                }

                val shops = parseNearbyShops(response.body())
                adapter.submitItems(shops)

                if (shops.isEmpty()) {
                    showState(
                        "Không tìm thấy cửa hàng phù hợp trong bán kính 10 km. " +
                                "Cửa hàng cũng cần có tọa độ và trạng thái hoạt động.",
                        false
                    )
                } else {
                    showState(
                        "Tìm thấy ${shops.size} cửa hàng gần bạn",
                        false
                    )
                }

            } catch (e: Exception) {
                showState(
                    "Lỗi kết nối: ${e.localizedMessage}",
                    false
                )
            }
        }
    }
    private fun parseNearbyShops(body: JsonElement?): List<NearbyShopItem> {
        if (body == null || body.isJsonNull) return emptyList()

        val data = when {
            body.isJsonArray -> body

            body.isJsonObject -> {
                val root = body.asJsonObject

                when {
                    root.has("data") -> root.get("data")
                    root.has("items") -> root.get("items")
                    root.has("shops") -> root.get("shops")
                    else -> null
                }
            }

            else -> null
        } ?: return emptyList()

        if (!data.isJsonArray) return emptyList()

        val gson = Gson()

        return data.asJsonArray.mapNotNull { element ->
            try {
                gson.fromJson(
                    element,
                    NearbyShopItem::class.java
                )
            } catch (_: Exception) {
                null
            }
        }
    }

    private fun showState(
        message: String,
        isLoading: Boolean
    ) {
        binding.tvScanState.text = message

        binding.progressScan.visibility =
            if (isLoading) View.VISIBLE else View.GONE

        binding.btnScanAgain.isEnabled = !isLoading
    }

    override fun onDestroy() {
        stopLocationScan()
        super.onDestroy()
    }
}
