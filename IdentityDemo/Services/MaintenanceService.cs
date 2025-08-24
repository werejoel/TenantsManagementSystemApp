using Microsoft.EntityFrameworkCore;
using TenantsManagementApp.Data;
using TenantsManagementApp.Models;

namespace TenantsManagementApp.Services
{
    public class MaintenanceService : IMaintenanceService
    {
        private readonly ApplicationDbContext _context;

        public MaintenanceService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<MaintenanceRequest>> GetMaintenanceRequestsAsync()
        {
            return await _context.MaintenanceRequests
                .Include(mr => mr.Tenant)
                .ThenInclude(t => t.User)
                .Include(mr => mr.House)
                .OrderByDescending(mr => mr.RequestedAt)
                .ToListAsync();
        }

        public async Task<MaintenanceRequest> CreateMaintenanceRequestAsync(MaintenanceRequest request)
        {
            _context.MaintenanceRequests.Add(request);
            await _context.SaveChangesAsync();
            return request;
        }

        public async Task<MaintenanceRequest?> UpdateMaintenanceRequestAsync(MaintenanceRequest request)
        {
            var existingRequest = await _context.MaintenanceRequests.FindAsync(request.Id);
            if (existingRequest == null) return null;

            existingRequest.Status = request.Status;
            existingRequest.ManagerNotes = request.ManagerNotes;
            existingRequest.Priority = request.Priority;

            if (request.Status == "Completed")
            {
                existingRequest.CompletedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return existingRequest;
        }

        public async Task<List<MaintenanceRequest>> GetRequestsByTenantAsync(int tenantId)
        {
            return await _context.MaintenanceRequests
                .Include(mr => mr.House)
                .Where(mr => mr.TenantId == tenantId)
                .OrderByDescending(mr => mr.RequestedAt)
                .ToListAsync();
        }

        public async Task<List<MaintenanceRequest>> GetPendingRequestsAsync()
        {
            return await _context.MaintenanceRequests
                .Include(mr => mr.Tenant)
                .ThenInclude(t => t.User)
                .Include(mr => mr.House)
                .Where(mr => mr.Status == "Pending" || mr.Status == "In Progress")
                .OrderByDescending(mr => mr.RequestedAt)
                .ToListAsync();
        }
    }

    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _context;

        public NotificationService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Notification>> GetUserNotificationsAsync(Guid userId)
        {
            return await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(50)
                .ToListAsync();
        }

        public async Task<Notification> CreateNotificationAsync(Notification notification)
        {
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
            return notification;
        }

        public async Task<bool> MarkAsReadAsync(int notificationId)
        {
            var notification = await _context.Notifications.FindAsync(notificationId);
            if (notification == null) return false;

            notification.IsRead = true;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task SendPaymentRemindersAsync()
        {
            // Get overdue charges directly from the context
            var overdueCharges = await _context.Charges
                .Include(c => c.Tenant)
                .ThenInclude(t => t.User)
                .Include(c => c.House)
                .Where(c => c.DueDate < DateTime.Now && c.Status != "Paid")
                .ToListAsync();

            foreach (var charge in overdueCharges)
            {
                if (charge.Tenant?.User != null)
                {
                    var notification = new Notification
                    {
                        UserId = charge.Tenant.UserId,
                        Type = "Payment Reminder",
                        Title = "Overdue Payment",
                        Message = $"Your {charge.ChargeType} payment of UGX {charge.Amount:N0} for {charge.House?.Name} was due on {charge.DueDate:dd/MM/yyyy}. Please make payment as soon as possible.",
                        Channel = "Email",
                        ScheduledAt = DateTime.UtcNow
                    };

                    await CreateNotificationAsync(notification);
                }
            }
        }

        public async Task SendLeaseExpiryRemindersAsync()
        {
            // Get tenants with expiring leases (within next 30 days)
            var thirtyDaysFromNow = DateTime.Now.AddDays(30);
            var expiringLeases = await _context.Tenants
                .Include(t => t.User)
                .Include(t => t.House)
                .Where(t => t.LeaseEndDate.HasValue &&
                           t.LeaseEndDate <= thirtyDaysFromNow &&
                           t.LeaseEndDate > DateTime.Now &&
                           t.IsActive)
                .ToListAsync();

            foreach (var tenant in expiringLeases)
            {
                if (tenant.User != null)
                {
                    var notification = new Notification
                    {
                        UserId = tenant.UserId,
                        Type = "Lease Expiry",
                        Title = "Lease Expiring Soon",
                        Message = $"Your lease for {tenant.House?.Name} expires on {tenant.LeaseEndDate:dd/MM/yyyy}. Please contact management to discuss renewal.",
                        Channel = "Email",
                        ScheduledAt = DateTime.UtcNow
                    };

                    await CreateNotificationAsync(notification);
                }
            }
        }
    }
}