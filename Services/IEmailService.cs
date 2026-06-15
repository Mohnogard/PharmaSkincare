using PharmaSkincare.Models;

namespace PharmaSkincare.Services
{
    public interface IEmailService
    {
        Task SendOrderConfirmationAsync(string toEmail, string customerName, Order order);
        Task SendOrderStatusUpdateAsync(string toEmail, string customerName, int orderId, string newStatus);
        Task SendPasswordResetEmailAsync(string toEmail, string customerName, string resetLink);
    }
}
