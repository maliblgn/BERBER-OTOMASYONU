using Microsoft.Extensions.Caching.Memory;

namespace SoftetroBarber.Services;

public class WhatsAppService : IWhatsAppService
{
    private readonly IMemoryCache _cache;

    public WhatsAppService(IMemoryCache cache)
    {
        _cache = cache;
    }

    public async Task<bool> GenerateAndSendOtpAsync(string phoneNumber)
    {
        string limitKey = $"OTP_Limit_{phoneNumber}";
        if (_cache.TryGetValue(limitKey, out int requestCount))
        {
            if (requestCount >= 3)
            {
                throw new InvalidOperationException("Saatlik OTP SMS limitinizi (3 kez) doldurdunuz. Lütfen 1 saat sonra tekrar deneyiniz.");
            }
            _cache.Set(limitKey, requestCount + 1, TimeSpan.FromHours(1));
        }
        else
        {
            _cache.Set(limitKey, 1, TimeSpan.FromHours(1));
        }

        // 1. Generate a 6-digit random OTP (Şimdilik test için sabit "123456" yapıyoruz)
        // var random = new Random();
        // var otp = random.Next(100000, 999999).ToString();
        var otp = "123456";

        // Console'a bilgi verelim (terminalden görmek için)
        Console.WriteLine($"[TEST-WhatsApp] {phoneNumber} adresine OTP gönderildi: {otp}");

        // 2. Set MemoryCache parameters (3 minutes absolute expiration)
        var cacheEntryOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromMinutes(3));

        // 3. Save OTP to Cache using phoneNumber as Key
        _cache.Set(phoneNumber, otp, cacheEntryOptions);

        // 4. Send OTP via WhatsApp API
        // TODO: Meta Cloud API Entegrasyonu
        // Example: await _httpClient.PostAsync("meta_api_url", requestBody);
        
        // Simulating the delay for the API request for now.
        await Task.Delay(500);
        
        // Assuming success for demo purposes
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
}
