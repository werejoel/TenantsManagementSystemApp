using Microsoft.EntityFrameworkCore;
using TenantsManagementApp.Data;
using TenantsManagementApp.DTOS;
using TenantsManagementApp.Models;

namespace TenantsManagementApp.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly ApplicationDbContext _context;

        public PaymentService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Payment>> GetPaymentsByTenantAsync(int tenantId)
        {
            return await _context.Payments
                .Include(p => p.House)
                .Include(p => p.PaymentCharges)
                .ThenInclude(pc => pc.Charge)
                .Where(p => p.TenantId == tenantId)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();
        }

        public async Task<Payment> RecordPaymentAsync(Payment payment, List<int>? chargeIds = null)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _context.Payments.Add(payment);
                await _context.SaveChangesAsync();

                // If specific charges are provided, allocate payment to them
                if (chargeIds?.Any() == true)
                {
                    var charges = await _context.Charges
                        .Where(c => chargeIds.Contains(c.Id) && c.TenantId == payment.TenantId)
                        .OrderBy(c => c.DueDate)
                        .ToListAsync();

                    var remainingAmount = payment.AmountPaid;

                    foreach (var charge in charges)
                    {
                        if (remainingAmount <= 0) break;

                        var outstandingAmount = charge.OutstandingAmount;
                        var allocationAmount = Math.Min(remainingAmount, outstandingAmount);

                        if (allocationAmount > 0)
                        {
                            var paymentCharge = new PaymentCharge
                            {
                                PaymentId = payment.Id,
                                ChargeId = charge.Id,
                                AmountPaid = allocationAmount
                            };

                            _context.PaymentCharges.Add(paymentCharge);
                            remainingAmount -= allocationAmount;

                            // Update charge status
                            if (charge.OutstandingAmount - allocationAmount <= 0)
                            {
                                charge.Status = "Paid";
                            }
                            else if (charge.AmountPaid + allocationAmount > 0)
                            {
                                charge.Status = "Partial";
                            }
                        }
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return payment;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<PaymentSummaryDto> GetPaymentSummaryAsync(DateTime fromDate, DateTime toDate)
        {
            return await _context.GetPaymentSummaryAsync(fromDate, toDate);
        }

        public async Task<List<Payment>> GetAllPaymentsAsync(int page = 1, int pageSize = 50)
        {
            return await _context.Payments
                .Include(p => p.Tenant)
                .ThenInclude(t => t.User)
                .Include(p => p.House)
                .OrderByDescending(p => p.PaymentDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }
    }
}
