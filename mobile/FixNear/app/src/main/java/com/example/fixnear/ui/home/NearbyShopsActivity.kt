
package com.example.fixnear.ui.home

import android.Manifest
import android.content.Intent
import android.content.pm.PackageManager
import android.location.Location
import android.location.LocationListener
import android.location.LocationManager
import android.os.Bundle
import android.os.Handler
import android.os.Looper
import android.util.Log
import android.view.View
import androidx.activity.result.contract.ActivityResultContracts
import androidx.appcompat.app.AppCompatActivity
import androidx.core.content.ContextCompat
import androidx.lifecycle.lifecycleScope
import androidx.recyclerview.widget.LinearLayoutManager
import com.example.fixnear.data.local.SessionManager
import com.example.fixnear.data.model.customer.NearbyShopItem
import com.example.fixnear.data.remote.RetrofitClient
import com.example.fixnear.databinding.ActivityNearbyShopsBinding
import com.google.gson.Gson
import kotlinx.coroutines.launch

class NearbyShopsActivity : AppCompatActivity() {

    private lateinit var binding: ActivityNearbyShopsBinding
    private lateinit var adapter: NearbyShopAdapter
    private lateinit var locationManager: LocationManager

    private val handler = Handler(Looper.getMainLooper())

    private var serviceId = -1
    private var serviceName = ""

    private var page = 0
    private var totalPages = 1
    private var loading = false
    private var generation = 0

    private var latitude: Double? = null
    private var longitude: Double? = null
    private var locationSource = ""

    private var listener: LocationListener? = null
    private var timeout: Runnable? = null
    private var scanFinished = false

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
                useDefaultAddress("Bạn đã từ chối quyền GPS")
            }
        }

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)

        binding = ActivityNearbyShopsBinding.inflate(layoutInflater)
        setContentView(binding.root)

        serviceId = intent.getIntExtra("SERVICE_ID", -1)
        serviceName =
            intent.getStringExtra("SERVICE_NAME").orEmpty()

        binding.tvSelectedService.text =
            "Dịch vụ: $serviceName • Bán kính tối đa 10 km"

        locationManager =
            getSystemService(LOCATION_SERVICE) as LocationManager

        adapter = NearbyShopAdapter { shop ->
            startActivity(
                Intent(
                    this,
                    ShopDetailActivity::class.java
                ).apply {
                    putExtra("SHOP_ID", shop.shopId)
                    putExtra("SHOP_NAME", shop.shopName)
                    putExtra("SERVICE_ID", serviceId)
                    putExtra("SERVICE_NAME", serviceName)
                }
            )
        }

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
            generation++
            loading = false
            checkPermissionAndScan()
        }

        binding.btnLoadMore.setOnClickListener {
            loadPage(page + 1)
        }

        checkPermissionAndScan()
    }

    // ============================================
    // KIỂM TRA QUYỀN VỊ TRÍ
    // ============================================

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

    // ============================================
    // QUÉT GPS
    // ============================================

    @Suppress("MissingPermission")
    private fun startLocationScan() {

        stopScan()

        scanFinished = false

        binding.btnLoadMore.visibility = View.GONE
        adapter.submitItems(emptyList())

        showState("Đang lấy vị trí GPS...", true)

        val fine = ContextCompat.checkSelfPermission(
            this,
            Manifest.permission.ACCESS_FINE_LOCATION
        ) == PackageManager.PERMISSION_GRANTED

        val coarse = ContextCompat.checkSelfPermission(
            this,
            Manifest.permission.ACCESS_COARSE_LOCATION
        ) == PackageManager.PERMISSION_GRANTED

        val provider = when {

            fine &&
                    locationManager.isProviderEnabled(
                        LocationManager.GPS_PROVIDER
                    ) -> {
                LocationManager.GPS_PROVIDER
            }

            (fine || coarse) &&
                    locationManager.isProviderEnabled(
                        LocationManager.NETWORK_PROVIDER
                    ) -> {
                LocationManager.NETWORK_PROVIDER
            }

            else -> null
        }

        if (provider == null) {
            useDefaultAddress("GPS đang tắt")
            return
        }

        val requestedGeneration = generation

        try {

            val current = object : LocationListener {

                override fun onLocationChanged(location: Location) {

                    if (
                        scanFinished ||
                        requestedGeneration != generation
                    ) {
                        return
                    }

                    scanFinished = true
                    stopScan()

                    useCoordinates(
                        location.latitude,
                        location.longitude,
                        "GPS hiện tại"
                    )
                }

                override fun onProviderDisabled(provider: String) {

                    if (
                        !scanFinished &&
                        requestedGeneration == generation
                    ) {
                        scanFinished = true
                        stopScan()

                        useDefaultAddress("GPS bị tắt")
                    }
                }
            }

            listener = current

            locationManager.requestSingleUpdate(
                provider,
                current,
                Looper.getMainLooper()
            )

            val onTimeout = Runnable {

                if (
                    !scanFinished &&
                    requestedGeneration == generation
                ) {
                    scanFinished = true
                    stopScan()

                    useDefaultAddress("GPS không phản hồi")
                }
            }

            timeout = onTimeout
            handler.postDelayed(onTimeout, 12000L)

        } catch (e: Exception) {

            stopScan()

            Log.e(
                "FIXNEAR_GPS",
                "Không thể lấy GPS",
                e
            )

            useDefaultAddress("Không lấy được GPS")
        }
    }

    // ============================================
    // GPS KHÔNG DÙNG ĐƯỢC:
    // LẤY ĐỊA CHỈ MẶC ĐỊNH CỦA CUSTOMER
    // ============================================

    private fun useDefaultAddress(reason: String) {

        stopScan()

        val rawToken = SessionManager(this)
            .getToken()
            ?.trim()
            .orEmpty()

        if (rawToken.isBlank()) {

            showState(
                "$reason. Hãy đăng nhập và thêm địa chỉ mặc định.",
                false
            )

            return
        }

        val token = if (
            rawToken.startsWith("Bearer ", ignoreCase = true)
        ) {
            rawToken
        } else {
            "Bearer $rawToken"
        }

        showState(
            "$reason. Đang lấy địa chỉ mặc định...",
            true
        )

        val requestedGeneration = generation

        lifecycleScope.launch {

            try {

                val response =
                    RetrofitClient.customerAddressApi
                        .getAddresses(token)

                if (requestedGeneration != generation) {
                    return@launch
                }

                if (!response.isSuccessful) {

                    showState(
                        "Không tải được địa chỉ mặc định " +
                                "(HTTP ${response.code()}).",
                        false
                    )

                    return@launch
                }

                val selected = response.body()
                    .orEmpty()
                    .firstOrNull { it.isDefault }

                val lat = selected?.latitude
                val lon = selected?.longitude

                if (
                    lat != null &&
                    lon != null &&
                    lat in -90.0..90.0 &&
                    lon in -180.0..180.0
                ) {

                    useCoordinates(
                        lat,
                        lon,
                        "địa chỉ mặc định"
                    )

                } else {

                    showState(
                        "$reason. Chưa có địa chỉ mặc định " +
                                "có tọa độ. Vào Tài khoản → Địa chỉ " +
                                "để cập nhật.",
                        false
                    )
                }

            } catch (e: Exception) {

                if (requestedGeneration == generation) {

                    showState(
                        "Lỗi tải địa chỉ: ${e.localizedMessage}",
                        false
                    )
                }
            }
        }
    }

    // ============================================
    // CẬP NHẬT VỊ TRÍ ĐỂ TÌM SHOP
    // ============================================

    private fun useCoordinates(
        lat: Double,
        lon: Double,
        source: String
    ) {

        if (
            lat !in -90.0..90.0 ||
            lon !in -180.0..180.0
        ) {

            showState(
                "Tọa độ không hợp lệ.",
                false
            )

            return
        }

        Log.d(
            "FIXNEAR_GPS",
            "serviceId=$serviceId, " +
                    "latitude=$lat, " +
                    "longitude=$lon, " +
                    "radiusKm=10, source=$source"
        )

        latitude = lat
        longitude = lon
        locationSource = source

        page = 0
        totalPages = 1
        loading = false

        adapter.submitItems(emptyList())

        loadPage(1)
    }

    // ============================================
    // GET DANH SÁCH SHOP GẦN NHẤT
    // ============================================

    private fun loadPage(nextPage: Int) {

        val lat = latitude ?: return
        val lon = longitude ?: return

        if (
            loading ||
            nextPage > totalPages ||
            nextPage < 1
        ) {
            return
        }

        loading = true

        binding.btnLoadMore.visibility = View.GONE

        showState(
            "Đang tìm cửa hàng gần bạn ($locationSource)...",
            true
        )

        val requestedGeneration = generation

        lifecycleScope.launch {

            try {

                val response =
                    RetrofitClient.customerDiscoveryApi
                        .getNearbyShops(
                            serviceId = serviceId,
                            latitude = lat,
                            longitude = lon,
                            radiusKm = 10.0,
                            page = nextPage,
                            pageSize = 10
                        )

                if (requestedGeneration != generation) {
                    return@launch
                }

                if (!response.isSuccessful) {

                    showState(
                        "Lỗi tìm cửa hàng: HTTP ${response.code()}",
                        false
                    )

                    return@launch
                }

                val root = response.body()
                    ?.takeIf { it.isJsonObject }
                    ?.asJsonObject

                val shops = root
                    ?.get("data")
                    ?.takeIf { it.isJsonArray }
                    ?.asJsonArray
                    ?.mapNotNull { item ->

                        try {
                            Gson().fromJson(
                                item,
                                NearbyShopItem::class.java
                            )
                        } catch (_: Exception) {
                            null
                        }
                    }
                    .orEmpty()

                if (nextPage == 1) {
                    adapter.submitItems(shops)
                } else {
                    adapter.appendItems(shops)
                }

                page = root?.get("page")
                    ?.takeIf { !it.isJsonNull }
                    ?.asInt
                    ?: nextPage

                totalPages = root?.get("totalPages")
                    ?.takeIf { !it.isJsonNull }
                    ?.asInt
                    ?: nextPage

                val total = root?.get("total")
                    ?.takeIf { !it.isJsonNull }
                    ?.asInt
                    ?: shops.size

                showState(
                    if (total == 0) {
                        "Không có cửa hàng phù hợp trong 10 km."
                    } else {
                        "Tìm thấy $total cửa hàng • " +
                                "Nguồn vị trí: $locationSource • " +
                                "Trang $page/$totalPages"
                    },
                    false
                )

            } catch (e: Exception) {

                if (requestedGeneration == generation) {

                    showState(
                        "Lỗi kết nối: ${e.localizedMessage}",
                        false
                    )
                }

            } finally {

                if (requestedGeneration == generation) {

                    loading = false

                    binding.btnLoadMore.visibility =
                        if (page < totalPages) {
                            View.VISIBLE
                        } else {
                            View.GONE
                        }
                }
            }
        }
    }

    // ============================================
    // GIAO DIỆN
    // ============================================

    private fun showState(
        text: String,
        progress: Boolean
    ) {

        binding.tvScanState.text = text

        binding.progressScan.visibility =
            if (progress) View.VISIBLE else View.GONE

        binding.btnScanAgain.isEnabled = !progress
    }

    private fun stopScan() {

        listener?.let {

            try {
                locationManager.removeUpdates(it)
            } catch (_: Exception) {
            }
        }

        listener = null

        timeout?.let {
            handler.removeCallbacks(it)
        }

        timeout = null
    }

    override fun onDestroy() {

        generation++
        stopScan()

        super.onDestroy()
    }
}
