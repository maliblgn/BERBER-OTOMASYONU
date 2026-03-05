using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SoftetroBarber.Data;
using SoftetroBarber.Enums;
using SoftetroBarber.Extensions;
using SoftetroBarber.Models;
using SoftetroBarber.Services;
using SoftetroBarber.ViewModels;

namespace SoftetroBarber.Pages.Booking;

public class Step6_VerifyModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly IWhatsAppService _whatsAppService;

    public Step6_VerifyModel(ApplicationDbContext context, IWhatsAppService whatsAppService)
    {
        _context = context;
        _whatsAppService = whatsAppService;
    }

    [BindProperty]
    public string OtpCode { get; set; } = string.Empty;

    public string PhoneNumberMasked { get; set; } = string.Empty;

    public IActionResult OnGet()
    {
        var sessionModel = HttpContext.Session.Get<BookingSessionModel>("BookingSession");
        if (sessionModel == null || string.IsNullOrWhiteSpace(sessionModel.CustomerPhone))
        {
            return RedirectToPage("Step1_Services");
        }

        var phone = sessionModel.CustomerPhone;
        // Basic masking: 555***4567
        PhoneNumberMasked = phone.Length >= 10 ? phone.Substring(0, 3) + "***" + phone.Substring(phone.Length - 4) : phone;

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var sessionModel = HttpContext.Session.Get<BookingSessionModel>("BookingSession");
        if (sessionModel == null) return RedirectToPage("Step1_Services");

        var isValid = _whatsAppService.VerifyOtp(sessionModel.CustomerPhone, OtpCode);

        if (!isValid)
        {
            ModelState.AddModelError("", "Hatalı veya süresi geçmiş kod girdiniz.");
            
            var phone = sessionModel.CustomerPhone;
            PhoneNumberMasked = phone.Length >= 10 ? phone.Substring(0, 3) + "***" + phone.Substring(phone.Length - 4) : phone;
            return Page();
        }

        // --- OTP Valid. Book the appointment ---
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // 1. Check or Create Customer
            var cleanPhone = new string(sessionModel.CustomerPhone.Where(char.IsDigit).ToArray());
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.PhoneNumber == cleanPhone);

            if (customer == null)
            {
                customer = new Customer
                {
                    Id = Guid.NewGuid(),
                    FullName = sessionModel.CustomerName,
                    PhoneNumber = cleanPhone
                };
                await _context.Customers.AddAsync(customer);
            }
            else
            {
                customer.FullName = sessionModel.CustomerName; // Update name
            }

            // 2. Fetch Services and calculate price/duration
            var services = await _context.Services
                .Where(s => sessionModel.SelectedServiceIds.Contains(s.Id))
                .ToListAsync();

            decimal totalPrice = services.Sum(s => s.Price);
            int totalDuration = services.Sum(s => s.DurationInMinutes);
            
            var startTime = sessionModel.SelectedDate.Date + sessionModel.SelectedTime;
            var endTime = startTime.AddMinutes(totalDuration);

            // 3. Create Appointment
            var appointment = new Appointment
            {
                Id = Guid.NewGuid(),
                CustomerId = customer.Id,
                BarberId = sessionModel.BarberId,
                StartTime = startTime,
                EndTime = endTime,
                TotalPrice = totalPrice,
                Status = AppointmentStatus.Confirmed
            };
            await _context.Appointments.AddAsync(appointment);

            // 4. Create Appointment Services relationships
            foreach (var service in services)
            {
                await _context.AppointmentServices.AddAsync(new AppointmentService
                {
                    AppointmentId = appointment.Id,
                    ServiceId = service.Id
                });
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            // Clear session to prevent double booking
            HttpContext.Session.Remove("BookingSession");

            return RedirectToPage("Step7_Success");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            ModelState.AddModelError("", "Randevu oluşturulurken sistemsel bir hata oluştu: " + ex.Message);
            
            var phone = sessionModel.CustomerPhone;
            PhoneNumberMasked = phone.Length >= 10 ? phone.Substring(0, 3) + "***" + phone.Substring(phone.Length - 4) : phone;
            return Page();
        }
    }
}
