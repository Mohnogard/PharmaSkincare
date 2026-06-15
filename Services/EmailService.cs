using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using PharmaSkincare.Models;

namespace PharmaSkincare.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration config, ILogger<EmailService> logger)
        {
            _config = config;
            _logger = logger;
        }

        private bool IsConfigured() =>
            !string.IsNullOrWhiteSpace(_config["Email:Username"]) &&
            !string.IsNullOrWhiteSpace(_config["Email:Password"]);

        private async Task SendAsync(string toEmail, string toName, string subject, string htmlBody)
        {
            if (!IsConfigured())
            {
                _logger.LogWarning("Email not configured — skipping send to {Email}", toEmail);
                return;
            }

            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(
                    _config["Email:FromName"] ?? "PharmaSkincare",
                    _config["Email:FromAddress"] ?? "noreply@pharmaskincare.com"));
                message.To.Add(new MailboxAddress(toName, toEmail));
                message.Subject = subject;
                message.Body = new TextPart("html") { Text = htmlBody };

                using var client = new SmtpClient();
                await client.ConnectAsync(
                    _config["Email:Host"] ?? "smtp.gmail.com",
                    int.Parse(_config["Email:Port"] ?? "587"),
                    SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(_config["Email:Username"], _config["Email:Password"]);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                _logger.LogInformation("Email sent to {Email}: {Subject}", toEmail, subject);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Email}", toEmail);
            }
        }

        public async Task SendOrderConfirmationAsync(string toEmail, string customerName, Order order)
        {
            var itemsHtml = string.Join("", order.OrderItems.Select(i =>
                $"<tr><td style='padding:8px 12px;border-bottom:1px solid #f0f0f0;'>{i.Product?.DisplayName ?? "Product"}</td>" +
                $"<td style='padding:8px 12px;border-bottom:1px solid #f0f0f0;text-align:center;'>{i.Quantity}</td>" +
                $"<td style='padding:8px 12px;border-bottom:1px solid #f0f0f0;text-align:right;'>JD {(i.UnitPrice * i.Quantity):N2}</td></tr>"));

            var html = $@"
<!DOCTYPE html>
<html>
<body style='margin:0;padding:0;background:#f5f5f5;font-family:Arial,sans-serif;'>
  <table width='100%' cellpadding='0' cellspacing='0'>
    <tr><td align='center' style='padding:32px 16px;'>
      <table width='600' cellpadding='0' cellspacing='0' style='background:#ffffff;border-radius:8px;overflow:hidden;box-shadow:0 2px 8px rgba(0,0,0,0.08);'>

        <tr><td style='background:#1a7a5e;padding:28px 32px;'>
          <h1 style='margin:0;color:#ffffff;font-size:22px;letter-spacing:1px;'>PharmaSkincare</h1>
          <p style='margin:6px 0 0;color:rgba(255,255,255,0.85);font-size:13px;'>Pharmaceutical-Grade Topical Minerals</p>
        </td></tr>

        <tr><td style='padding:32px;'>
          <h2 style='margin:0 0 8px;color:#1a7a5e;font-size:20px;'>Order Confirmed!</h2>
          <p style='margin:0 0 24px;color:#555;font-size:15px;'>Hi {customerName}, thank you for your order. We'll get it ready for you as soon as possible.</p>

          <table width='100%' cellpadding='0' cellspacing='0' style='background:#f9f9f9;border-radius:6px;margin-bottom:24px;'>
            <tr>
              <td style='padding:12px 16px;'><strong style='color:#333;'>Order Number</strong></td>
              <td style='padding:12px 16px;text-align:right;color:#1a7a5e;font-weight:bold;'>#{order.OrderId}</td>
            </tr>
            <tr style='background:#f0f0f0;'>
              <td style='padding:12px 16px;'><strong style='color:#333;'>Date</strong></td>
              <td style='padding:12px 16px;text-align:right;color:#555;'>{order.OrderDate:dd MMM yyyy}</td>
            </tr>
            <tr>
              <td style='padding:12px 16px;'><strong style='color:#333;'>Status</strong></td>
              <td style='padding:12px 16px;text-align:right;'><span style='background:#fff3e0;color:#f39c12;padding:3px 10px;border-radius:12px;font-size:13px;font-weight:bold;'>Pending</span></td>
            </tr>
          </table>

          <h3 style='margin:0 0 12px;color:#333;font-size:15px;'>Order Summary</h3>
          <table width='100%' cellpadding='0' cellspacing='0' style='border:1px solid #f0f0f0;border-radius:6px;overflow:hidden;margin-bottom:20px;'>
            <tr style='background:#f5f5f5;'>
              <th style='padding:10px 12px;text-align:left;font-size:13px;color:#666;font-weight:600;'>Product</th>
              <th style='padding:10px 12px;text-align:center;font-size:13px;color:#666;font-weight:600;'>Qty</th>
              <th style='padding:10px 12px;text-align:right;font-size:13px;color:#666;font-weight:600;'>Price</th>
            </tr>
            {itemsHtml}
          </table>

          <table width='100%' cellpadding='0' cellspacing='0' style='margin-bottom:28px;'>
            <tr>
              <td style='padding:6px 0;color:#666;font-size:14px;'>Subtotal</td>
              <td style='padding:6px 0;text-align:right;color:#333;font-size:14px;'>JD {order.TotalAmount:N2}</td>
            </tr>
            <tr>
              <td style='padding:6px 0;color:#666;font-size:14px;'>Shipping</td>
              <td style='padding:6px 0;text-align:right;color:#333;font-size:14px;'>JD {order.ShippingCost:N2}</td>
            </tr>
            {(order.DiscountAmount > 0 ? $"<tr><td style='padding:6px 0;color:#27ae60;font-size:14px;'>Discount</td><td style='padding:6px 0;text-align:right;color:#27ae60;font-size:14px;'>-JD {order.DiscountAmount:N2}</td></tr>" : "")}
            <tr style='border-top:2px solid #1a7a5e;'>
              <td style='padding:10px 0 0;color:#1a7a5e;font-size:16px;font-weight:bold;'>Total</td>
              <td style='padding:10px 0 0;text-align:right;color:#1a7a5e;font-size:16px;font-weight:bold;'>JD {order.GrandTotal:N2}</td>
            </tr>
          </table>

          <p style='margin:0;color:#888;font-size:13px;'>You'll receive another email when your order status changes. If you have any questions, contact us at <a href='mailto:support@pharmaskincare.com' style='color:#1a7a5e;'>support@pharmaskincare.com</a>.</p>
        </td></tr>

        <tr><td style='background:#f5f5f5;padding:20px 32px;text-align:center;border-top:1px solid #e8e8e8;'>
          <p style='margin:0;color:#aaa;font-size:12px;'>© {DateTime.UtcNow.Year} PharmaSkincare Ltd. All rights reserved.</p>
        </td></tr>

      </table>
    </td></tr>
  </table>
</body>
</html>";

            await SendAsync(toEmail, customerName, $"Order Confirmed – #{order.OrderId} | PharmaSkincare", html);
        }

        public async Task SendOrderStatusUpdateAsync(string toEmail, string customerName, int orderId, string newStatus)
        {
            var (badgeColor, badgeBg, statusMessage) = newStatus switch
            {
                "Processing" => ("#1976d2", "#e3f2fd", "Great news! We're preparing your order right now."),
                "Packed"     => ("#9b59b6", "#f3e5f5", "Your order is packed and ready to be dispatched."),
                "Shipped"    => ("#1a7a5e", "#e8f5f0", "Your order is on its way! Expect delivery soon."),
                "Delivered"  => ("#27ae60", "#e8f5e9", "Your order has been delivered. We hope you love it!"),
                "Cancelled"  => ("#e74c3c", "#fdecea", "Your order has been cancelled. Contact us if you have questions."),
                "Returned"   => ("#95a5a6", "#f5f5f5", "Your return has been processed successfully."),
                _            => ("#f39c12", "#fff3e0", "Your order status has been updated.")
            };

            var html = $@"
<!DOCTYPE html>
<html>
<body style='margin:0;padding:0;background:#f5f5f5;font-family:Arial,sans-serif;'>
  <table width='100%' cellpadding='0' cellspacing='0'>
    <tr><td align='center' style='padding:32px 16px;'>
      <table width='600' cellpadding='0' cellspacing='0' style='background:#ffffff;border-radius:8px;overflow:hidden;box-shadow:0 2px 8px rgba(0,0,0,0.08);'>

        <tr><td style='background:#1a7a5e;padding:28px 32px;'>
          <h1 style='margin:0;color:#ffffff;font-size:22px;letter-spacing:1px;'>PharmaSkincare</h1>
          <p style='margin:6px 0 0;color:rgba(255,255,255,0.85);font-size:13px;'>Pharmaceutical-Grade Topical Minerals</p>
        </td></tr>

        <tr><td style='padding:32px;text-align:center;'>
          <h2 style='margin:0 0 8px;color:#333;font-size:20px;'>Order Status Update</h2>
          <p style='margin:0 0 28px;color:#666;font-size:15px;'>Hi {customerName}, your order has been updated.</p>

          <table width='100%' cellpadding='0' cellspacing='0' style='background:#f9f9f9;border-radius:6px;margin-bottom:24px;'>
            <tr>
              <td style='padding:16px;text-align:center;'>
                <p style='margin:0 0 8px;color:#888;font-size:13px;'>Order Number</p>
                <p style='margin:0 0 16px;color:#1a7a5e;font-size:22px;font-weight:bold;'>#{orderId}</p>
                <p style='margin:0 0 8px;color:#888;font-size:13px;'>New Status</p>
                <span style='background:{badgeBg};color:{badgeColor};padding:6px 20px;border-radius:20px;font-size:15px;font-weight:bold;display:inline-block;'>{newStatus}</span>
              </td>
            </tr>
          </table>

          <p style='margin:0 0 28px;color:#555;font-size:15px;'>{statusMessage}</p>

          <p style='margin:0;color:#888;font-size:13px;'>Questions? Email us at <a href='mailto:support@pharmaskincare.com' style='color:#1a7a5e;'>support@pharmaskincare.com</a>.</p>
        </td></tr>

        <tr><td style='background:#f5f5f5;padding:20px 32px;text-align:center;border-top:1px solid #e8e8e8;'>
          <p style='margin:0;color:#aaa;font-size:12px;'>© {DateTime.UtcNow.Year} PharmaSkincare Ltd. All rights reserved.</p>
        </td></tr>

      </table>
    </td></tr>
  </table>
</body>
</html>";

            await SendAsync(toEmail, customerName, $"Your Order #{orderId} is now {newStatus} | PharmaSkincare", html);
        }

        public async Task SendPasswordResetEmailAsync(string toEmail, string customerName, string resetLink)
        {
            var html = $@"
<!DOCTYPE html>
<html>
<body style='margin:0;padding:0;background:#f5f5f5;font-family:Arial,sans-serif;'>
  <table width='100%' cellpadding='0' cellspacing='0'>
    <tr><td align='center' style='padding:32px 16px;'>
      <table width='600' cellpadding='0' cellspacing='0' style='background:#ffffff;border-radius:8px;overflow:hidden;box-shadow:0 2px 8px rgba(0,0,0,0.08);'>
        <tr><td style='background:#1a7a5e;padding:28px 32px;'>
          <h1 style='margin:0;color:#ffffff;font-size:22px;letter-spacing:1px;'>PharmaSkincare</h1>
          <p style='margin:6px 0 0;color:rgba(255,255,255,0.85);font-size:13px;'>Pharmaceutical-Grade Topical Minerals</p>
        </td></tr>
        <tr><td style='padding:32px;'>
          <h2 style='margin:0 0 8px;color:#1a7a5e;font-size:20px;'>Reset Your Password</h2>
          <p style='margin:0 0 24px;color:#555;font-size:15px;'>Hi {customerName}, we received a request to reset the password for your account. Click the button below to set a new password.</p>
          <div style='text-align:center;margin:28px 0;'>
            <a href='{resetLink}' style='background:#1a7a5e;color:#fff;padding:14px 32px;border-radius:8px;font-size:15px;font-weight:bold;text-decoration:none;display:inline-block;'>Reset My Password</a>
          </div>
          <p style='margin:0 0 12px;color:#888;font-size:13px;'>Or copy this link into your browser:</p>
          <p style='margin:0 0 24px;color:#555;font-size:12px;word-break:break-all;'>{resetLink}</p>
          <p style='margin:0;color:#888;font-size:13px;'>This link expires in 24 hours. If you did not request a password reset, please ignore this email — your account is safe.</p>
        </td></tr>
        <tr><td style='background:#f5f5f5;padding:20px 32px;text-align:center;border-top:1px solid #e8e8e8;'>
          <p style='margin:0;color:#aaa;font-size:12px;'>© {DateTime.UtcNow.Year} PharmaSkincare Ltd. All rights reserved.</p>
        </td></tr>
      </table>
    </td></tr>
  </table>
</body>
</html>";

            await SendAsync(toEmail, customerName, "Reset Your Password | PharmaSkincare", html);
        }
    }
}
