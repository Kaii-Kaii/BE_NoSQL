namespace API_NoSQL.Settings;

public class SendGridSettings
{
    public string ApiKey { get; set; } = default!;
    public string FromEmail { get; set; } = default!;
    public string FromName { get; set; } = default!;
    public string TemplateId { get; set; } = default!;
}