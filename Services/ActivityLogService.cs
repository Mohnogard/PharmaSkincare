using PharmaSkincare.Data;
using PharmaSkincare.Models;

namespace PharmaSkincare.Services
{
    public class ActivityLogService : IActivityLogService
    {
        private readonly ApplicationDbContext _context;

        public ActivityLogService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task LogAsync(string userId, string action, string? entityType = null, int? entityId = null, string? details = null, string? ipAddress = null)
        {
            var log = new ActivityLog
            {
                UserId = userId,
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                Details = details,
                IpAddress = ipAddress,
                Timestamp = DateTime.UtcNow
            };

            _context.ActivityLogs.Add(log);
            await _context.SaveChangesAsync();
        }
    }
}
