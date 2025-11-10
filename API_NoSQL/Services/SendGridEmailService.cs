using API_NoSQL.Models.Emails;
using API_NoSQL.Settings;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace API_NoSQL.Services;

public interface ISendGridEmailService
{
    Task SendOrderConfirmationAsync(string toEmail, string toName, OrderEmailDto orderData, CancellationToken cancellationToken = default);
}

public class SendGridEmailService : ISendGridEmailService
{
    private readonly SendGridSettings _settings;
    private readonly ILogger<SendGridEmailService> _logger;

    public SendGridEmailService(SendGridSettings settings, ILogger<SendGridEmailService> logger)
    {
        _settings = settings;
        _logger = logger;
    }

    public async Task SendOrderConfirmationAsync(string toEmail, string toName, OrderEmailDto orderData, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_settings.ApiKey))
            {
                _logger.LogError("SendGrid API Key chưa được cấu hình");
                throw new InvalidOperationException("SendGrid API Key chưa được cấu hình");
            }
            if (string.IsNullOrWhiteSpace(_settings.TemplateId))
            {
                _logger.LogError("SendGrid Template ID chưa được cấu hình");
                throw new InvalidOperationException("SendGrid Template ID chưa được cấu hình");
            }
            if (string.IsNullOrWhiteSpace(_settings.FromEmail))
            {
                _logger.LogError("SendGrid From Email chưa được cấu hình");
                throw new InvalidOperationException("SendGrid From Email chưa được cấu hình");
            }

            _logger.LogInformation($"Bắt đầu gửi email tới {toEmail} với Template ID: {_settings.TemplateId}");

            var client = new SendGridClient(_settings.ApiKey);
            var from = new EmailAddress(_settings.FromEmail, _settings.FromName ?? "KaiiKaii Shop");
            var to = new EmailAddress(toEmail, toName);

            var msg = new SendGridMessage
            {
                From = from,
                Subject = $"Xác nhận đơn hàng #{orderData.OrderId}",
            };

            msg.AddTo(to);
            msg.SetTemplateId(_settings.TemplateId);

            var templateData = new
            {
                customer_name = orderData.CustomerName,
                customer_email = orderData.CustomerEmail,
                phone_number = orderData.PhoneNumber,
                shipping_address = orderData.ShippingAddress,
                order_id = orderData.OrderId,
                order_date = orderData.OrderDate,
                order_status = orderData.Status,
                items = orderData.Items.Select(i => new
                {
                    product_name = i.ProductName,
                    quantity = i.Quantity,
                    price = i.Price.ToString("N0"),
                    subtotal = i.Subtotal.ToString("N0")
                }).ToList(),
                subtotal = orderData.Subtotal.ToString("N0"),
                shipping_fee = orderData.ShippingFee.ToString("N0"),
                tax = orderData.Tax.ToString("N0"),
                total = orderData.Total.ToString("N0")
            };

            msg.SetTemplateData(templateData);

            _logger.LogInformation($"Gửi email qua SendGrid API...");
            var response = await client.SendEmailAsync(msg, cancellationToken);

            if ((int)response.StatusCode >= 400)
            {
                var errorBody = await response.Body.ReadAsStringAsync(cancellationToken);
                _logger.LogError($"SendGrid API Error {(int)response.StatusCode}: {errorBody}");
                throw new InvalidOperationException($"SendGrid gửi email thất bại: HTTP {(int)response.StatusCode} - {errorBody}");
            }

            _logger.LogInformation($"✅ Email xác nhận đơn hàng gửi thành công tới {toEmail}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"❌ Lỗi khi gửi email: {ex.Message}");
            throw;
        }
    }
}