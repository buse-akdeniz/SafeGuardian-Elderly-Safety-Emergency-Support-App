using System.Net.Http.Headers;

namespace ilk_projem.Services;

public sealed class EmergencySmsService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public EmergencySmsService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_configuration["Sms:Twilio:AccountSid"])
        && !string.IsNullOrWhiteSpace(_configuration["Sms:Twilio:AuthToken"])
        && !string.IsNullOrWhiteSpace(_configuration["Sms:FromNumber"]);

    public async Task<bool> SendAsync(
        string recipient,
        string message,
        CancellationToken cancellationToken)
    {
        var sid = _configuration["Sms:Twilio:AccountSid"]!;
        var authToken = _configuration["Sms:Twilio:AuthToken"]!;
        var from = _configuration["Sms:FromNumber"]!;
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Basic",
            Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{sid}:{authToken}")));
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["To"] = recipient,
            ["From"] = from,
            ["Body"] = message
        });
        using var response = await client.PostAsync(
            $"https://api.twilio.com/2010-04-01/Accounts/{Uri.EscapeDataString(sid)}/Messages.json",
            content,
            cancellationToken);
        return response.IsSuccessStatusCode;
    }
}
