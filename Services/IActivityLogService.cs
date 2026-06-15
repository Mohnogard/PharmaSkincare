namespace PharmaSkincare.Services
{
    public interface IActivityLogService
    {
        Task LogAsync(string userId, string action, string? entityType = null, int? entityId = null, string? details = null, string? ipAddress = null);
    }
}
