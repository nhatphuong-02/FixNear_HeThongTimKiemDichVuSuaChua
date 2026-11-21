package com.example.fixnear.data.model.technician

data class PagedResponse<T>(
    val page: Int,
    val pageSize: Int,
    val total: Int,
    val totalPages: Int,
    val data: List<T>
)

data class ActionResponse(
    val message: String?,
    val repairRequestId: Int?,
    val status: Int?,
    val repairReportId: Int?,
    val repairPartUsageId: Int?,
    val quoteId: Int?,
    val totalAmount: Double?
)

data class AssignmentService(
    val serviceId: Int,
    val serviceName: String
)

data class TechnicianAssignment(
    val repairAssignmentId: Int,
    val repairRequestId: Int,
    val shopId: Int,
    val shopName: String,
    val customerId: Int,
    val customerName: String,
    val description: String?,
    val repairMethod: Int,
    val preferredDate: String?,
    val preferredTimeFrom: String?,
    val preferredTimeTo: String?,
    val repairStatus: Int,
    val assignmentStatus: Int,
    val assignedAt: String,
    val respondedAt: String?,
    val note: String?,
    val rejectionReason: String?,
    val services: List<AssignmentService>
)

data class RejectRequest(
    val reason: String
)

data class SurveyRequest(
    val diagnosis: String,
    val recommendation: String? = null
)

data class PartUsageRequest(
    val repairRequestId: Int,
    val repairPartId: Int,
    val quantity: Double,
    val note: String? = null
)

data class CompleteRequest(
    val result: String,
    val workDescription: String? = null,
    val recommendation: String? = null
)

data class ShopRepairItem(
    val shopRepairItemId: Int,
    val shopId: Int,
    val repairItemId: Int,
    val itemCode: String,
    val itemName: String,
    val description: String?,
    val unit: String?,
    val price: Double,
    val isActive: Boolean
)

data class ShopRepairPart(
    val shopRepairPartId: Int,
    val shopId: Int,
    val repairPartId: Int,
    val partCode: String,
    val partName: String,
    val description: String?,
    val unit: String?,
    val price: Double,
    val isActive: Boolean
)

data class CurrentPrice(
    val price: Double?,
    val minPrice: Double?,
    val maxPrice: Double?,
    val priceUnit: String?
)

data class ShopServiceItem(
    val shopServiceId: Int,
    val serviceId: Int,
    val serviceName: String,
    val categoryId: Int,
    val categoryName: String?,
    val serviceDescription: String?,
    val shopDescription: String?,
    val currentPrice: CurrentPrice?
)

data class QuoteDetailRequest(
    val itemType: Int,
    val serviceRefId: Int? = null,
    val repairItemRefId: Int? = null,
    val repairPartRefId: Int? = null,
    val description: String? = null,
    val quantity: Double
)


data class QuoteDetailItem(
    val quoteDetailId: Int,
    val itemType: Int,
    val serviceRefId: Int?,
    val repairItemRefId: Int?,
    val repairPartRefId: Int?,
    val description: String?,
    val quantity: Double,
    val unitPrice: Double,
    val amount: Double
)

data class QuoteHistoryItem(
    val quoteHistoryId: Int,
    val changedByUserId: Int?,
    val oldStatus: Int?,
    val newStatus: Int?,
    val changeNote: String?,
    val snapshotTotalAmount: Double?,
    val createdAt: String
)

data class QuoteResponse(
    val quoteId: Int,
    val repairRequestId: Int,
    val technicianId: Int,
    val quoteNumber: String,
    val totalAmount: Double,
    val status: Int,
    val isSupplemental: Boolean,
    val customerResponse: Int?,
    val customerResponseAt: String?,
    val validUntil: String?,
    val createdAt: String,
    val details: List<QuoteDetailItem>,
    val history: List<QuoteHistoryItem>
)

data class TechnicianSchedule(
    val repairScheduleId: Int,
    val repairRequestId: Int,
    val shopId: Int,
    val shopName: String,
    val description: String?,
    val startTime: String,
    val endTime: String,
    val status: Int,
    val isCurrent: Boolean
)

object RepairStatus {
    const val ASSIGNED = 4
    const val INSPECTING = 5
    const val QUOTED = 6
    const val QUOTE_APPROVED = 7
    const val REPAIRING = 8
    const val COMPLETED = 9
}

object AssignStatus {
    const val OFFERED = 1
    const val ACCEPTED = 2
    const val REJECTED = 3
    const val CANCELLED = 4
}