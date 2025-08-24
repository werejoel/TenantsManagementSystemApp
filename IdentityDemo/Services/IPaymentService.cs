using TenantsManagementApp.DTOS;
using TenantsManagementApp.Models;

namespace TenantsManagementApp.Services
{
    public interface IPaymentService
    {
        Task<List<Payment>> GetPaymentsByTenantAsync(int tenantId);
        Task<Payment> RecordPaymentAsync(Payment payment, List<int>? chargeIds = null);
        Task<PaymentSummaryDto> GetPaymentSummaryAsync(DateTime fromDate, DateTime toDate);
        Task<List<Payment>> GetAllPaymentsAsync(int page = 1, int pageSize = 50);
    }
}
