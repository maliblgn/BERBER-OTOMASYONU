namespace SoftetroBarber.Services;

public interface IWhatsAppService
{
    Task<bool> GenerateAndSendOtpAsync(string phoneNumber);
    bool VerifyOtp(string phoneNumber, string otp);
}
