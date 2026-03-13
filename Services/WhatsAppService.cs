using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;

namespace SoftetroBarber.Services;

public class WhatsAppService : IWhatsAppService
{
    private readonly IMemoryCache _cache;
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IHostEnvironment _env;

    public WhatsAppService(
        IMemoryCache cache, 
        IConfiguration configuration, 
        IHttpClientFactory httpClientFactory,
        IHostEnvironment env)
    {
        _cache = cache;
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
        _env = env;
    }

    public async Task<bool> GenerateAndSendOtpAsync(string phoneNumber)
    {
        int limit = _configuration.GetValue<int>("WhatsAppSettings:HourlyRateLimit", 3);
        int otpValidity = _configuration.GetValue<int>("WhatsAppSettings:OtpValidityMinutes", 3);

        string limitKey = $"OTP_Limit_{phoneNumber}";   
        if (_cache.TryGetValue(limitKey, out int requestCount))
        {
            if (requestCount >= limit)
            {
                throw new InvalidOperationException($"Saatlik OTP SMS limitinizi ({limit} kez) doldurdunuz. Lütfen daha sonra tekrar deneyiniz.");
            }
            _cache.Set(limitKey, requestCount + 1, TimeSpan.FromHours(1));
        }
        else
        {
            _cache.Set(limitKey, 1, TimeSpan.FromHours(1));
        }

        // 1. Generate a 6-digit random OTP
        var random = new Random();
        var otp = random.Next(100000, 999999).ToString();

        // 2. Set MemoryCache parameters
        var cacheEntryOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromMinutes(otpValidity));

        // 3. Save OTP to Cache
        _cache.Set(phoneNumber, otp, cacheEntryOptions);

        // 4. Send OTP via SMS
        await SendSmsAsync(phoneNumber, $"[SoftetroBarber] Giriş kodunuz: {otp}. Bu kod {otpValidity} dakika geçerlidir.", otp);
        
        return true;
    }

    public bool VerifyOtp(string phoneNumber, string otp)
    {
        if (_cache.TryGetValue(phoneNumber, out string? cachedOtp))
        {
            if (cachedOtp == otp)
            {
                // Remove OTP from cache to avoid reuse
                _cache.Remove(phoneNumber);
                return true;
            }
        }
        
        return false;
    }

    private async Task SendSmsAsync(string phoneNumber, string message, string otp)
    {
        string apiUrl = _configuration.GetValue<string>("WhatsAppSettings:ApiUrl", "") ?? "";
        string apiKey = _configuration.GetValue<string>("WhatsAppSettings:ApiKey", "") ?? "";
        string sender = _configuration.GetValue<string>("WhatsAppSettings:Sender", "SOFTETRO") ?? "";

        // Geliştirici Modu (Kritik): ApiKey boşsa veya IsDevelopment modundaysak sadece logla
        if (string.IsNullOrWhiteSpace(apiKey) || _env.IsDevelopment())
        {
            string logMessage = $"[TEST MODU] OTP KODU: {otp} - Lütfen bu kodu sisteme girin.";
            Debug.WriteLine(logMessage);
            Console.WriteLine(logMessage);
            return;
        }

        // Gerçek API Gönderimi
        var client = _httpClientFactory.CreateClient();
        
        var payload = new
        {
            to = phoneNumber,
            message = message,
            sender = sender,
            api_key = apiKey
        };

        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        try
        {
            var response = await client.PostAsync(apiUrl, content);
            response.EnsureSuccessStatusCode();
            // İsteğe bağlı olarak dönen response loglanabilir.
        }
        catch (Exception ex)
        {
            // SMS gönderimi başarısız olduysa hata fırlatabiliriz veya loglayabiliriz
            Console.WriteLine($"SMS gönderim hatası: {ex.Message}");
            throw new InvalidOperationException("SMS gönderilemedi, lütfen tekrar deneyiniz.", ex);
        }
    }
}
