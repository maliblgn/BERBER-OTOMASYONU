using Microsoft.AspNetCore.Authorization; // Authorize hatası için
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages; // PageModel hatası için
using Microsoft.EntityFrameworkCore; // ToListAsync ve sorgular için
using SoftetroBarber.Data; // ApplicationDbContext hatası için
using SoftetroBarber.Models; // Appointment hatası için

namespace SoftetroBarber.Pages.Admin;

[Authorize(Roles = "Admin")]
public class HistoryModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public HistoryModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public List<Appointment> PastAppointments { get; set; } = new();
    // History.cshtml.cs - Güncellenmiş ve Daha Garanti Sorgu
    // History.cshtml.cs - Güncellenmiş ve Daha Garanti Sorgu
    public async Task OnGetAsync()
    {
        var now = DateTime.Now;

        // Arşiv sorgusu: Zamanı geçenler VEYA durumu netleşenler
        PastAppointments = await _context.Appointments
            .Include(a => a.Customer)
            .Include(a => a.Barber)
            .Where(a => a.EndTime < now ||
                        a.Status == SoftetroBarber.Enums.AppointmentStatus.Completed ||
                        a.Status == SoftetroBarber.Enums.AppointmentStatus.Cancelled ||
                        a.Status == SoftetroBarber.Enums.AppointmentStatus.NoShow)
            .OrderByDescending(a => a.StartTime)
            .ToListAsync();
    }
    public async Task<IActionResult> OnPostDeleteOldAppointmentsAsync()
    {
        // Şu andan 3 gün öncesini hesapla
        var thresholdDate = DateTime.Now.AddDays(-3);

        // 3 günden eski olan randevuları getir
        var oldAppointments = _context.Appointments
            .Where(a => a.StartTime < thresholdDate);

        int deletedCount = await oldAppointments.CountAsync();

        if (deletedCount > 0)
        {
            _context.Appointments.RemoveRange(oldAppointments);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"3 günden eski olan {deletedCount} adet randevu başarıyla silindi.";
        }
        else
        {
            TempData["InfoMessage"] = "Silinecek eski randevu bulunamadı.";
        }

        return RedirectToPage();
    }
}