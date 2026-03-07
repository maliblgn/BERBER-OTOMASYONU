using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SoftetroBarber.Data;
using SoftetroBarber.Enums;
using SoftetroBarber.Models;

namespace SoftetroBarber.Pages.Admin;

[Authorize(Roles = "Admin")]
public class AppointmentsModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public AppointmentsModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public List<Appointment> Appointments { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        Appointments = await _context.Appointments
            .Include(a => a.Customer)
            .Include(a => a.Barber)
            .OrderByDescending(a => a.StartTime)
            .ToListAsync();
            
        return Page();
    }

    public async Task<IActionResult> OnPostUpdateStatusAsync(Guid appointmentId, AppointmentStatus newStatus)
    {
        var appointment = await _context.Appointments
            .Include(a => a.Customer)
            .FirstOrDefaultAsync(a => a.Id == appointmentId);
            
        if (appointment != null)
        {
            appointment.Status = newStatus;
            await _context.SaveChangesAsync();
            
            if (newStatus == AppointmentStatus.NoShow)
            {
                var noShowCount = await _context.Appointments
                    .CountAsync(a => a.CustomerId == appointment.CustomerId && a.Status == AppointmentStatus.NoShow);
                    
                if (noShowCount >= 2 && !appointment.Customer.IsBlacklisted)
                {
                    TempData["WarningMessage"] = $"DİKKAT: {appointment.Customer.FullName} isimli müşteri 2. kez randevusuna gelmedi! Lütfen Müşteriler sayfasından kendisini Kara Listeye almayı değerlendirin.";
                }
                else
                {
                    TempData["SuccessMessage"] = "Randevu durumu 'Gelmedi' olarak işaretlendi.";
                }
            }
            else
            {
                TempData["SuccessMessage"] = "Randevu durumu başarıyla güncellendi.";
            }
        }
        
        return RedirectToPage();
    }
}
