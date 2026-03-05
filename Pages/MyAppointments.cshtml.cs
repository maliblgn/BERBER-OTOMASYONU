using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SoftetroBarber.Data;
using SoftetroBarber.Enums;
using SoftetroBarber.Models;

namespace SoftetroBarber.Pages;

public class MyAppointmentsModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public MyAppointmentsModel(ApplicationDbContext context)
    {
        _context = context;
    }

    [BindProperty(SupportsGet = true)]
    public string? PhoneNumber { get; set; }

    public List<Appointment> Appointments { get; set; } = new();
    public bool HasSearched { get; set; } = false;

    public async Task<IActionResult> OnGetAsync()
    {
        if (!string.IsNullOrWhiteSpace(PhoneNumber))
        {
            HasSearched = true;
            var cleanPhone = new string(PhoneNumber.Where(char.IsDigit).ToArray());

            Appointments = await _context.Appointments
                .Include(a => a.Customer)
                .Include(a => a.Barber)
                .Include(a => a.AppointmentServices)
                    .ThenInclude(aps => aps.Service)
                .Where(a => a.Customer.PhoneNumber == cleanPhone)
                .OrderByDescending(a => a.StartTime)
                .ToListAsync();
        }

        return Page();
    }

    public async Task<IActionResult> OnPostCancelAsync(Guid appointmentId, string phoneNumber)
    {
        var appt = await _context.Appointments
            .Include(a => a.Barber)
            .Include(a => a.Customer)
            .FirstOrDefaultAsync(a => a.Id == appointmentId);
            
        if (appt != null && appt.Status == AppointmentStatus.Confirmed)
        {
            if ((appt.StartTime - DateTime.Now).TotalHours >= 1)
            {
                appt.Status = AppointmentStatus.Cancelled;
                await _context.SaveChangesAsync();
                
                // Barber Notification (Log)
                Console.WriteLine($"[TEST-WhatsApp-Barber] Berber {appt.Barber.Name}'e Bildirim: {appt.Customer.FullName} isimli müşterinizin {appt.StartTime:dd MMMM yyyy HH:mm} tarihli randevusu İPTAL EDİLMİŞTİR.");
                
                TempData["SuccessMessage"] = "Randevunuz başarıyla iptal edildi.";
            }
            else
            {
                TempData["ErrorMessage"] = "Randevunuza 1 saatten az kaldığı için web üzerinden iptal işlemi gerçekleştirilemez. Lütfen salonu arayınız.";
            }
        }
        
        return RedirectToPage(new { PhoneNumber = phoneNumber });
    }
}
