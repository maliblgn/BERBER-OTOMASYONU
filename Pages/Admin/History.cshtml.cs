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

        // ARŞİV MANTIĞI: Durumu ne olursa olsun, sadece ZAMANI GEÇMİŞ randevuları getir
        PastAppointments = await _context.Appointments
            .Include(a => a.Customer)
            .Include(a => a.Barber)
            .Where(a => a.StartTime < now) // KRİTİK DEĞİŞİKLİK: Sadece geçmiş tarihler
            .OrderByDescending(a => a.StartTime)
            .ToListAsync();
    }
    // History.cshtml.cs içine eklenecek metod:
    public async Task<IActionResult> OnPostUpdateStatusAsync(Guid appointmentId, SoftetroBarber.Enums.AppointmentStatus newStatus)
    {
        var appointment = await _context.Appointments.FindAsync(appointmentId);

        if (appointment != null)
        {
            appointment.Status = newStatus;
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Randevu durumu başarıyla güncellendi.";
        }

        return RedirectToPage();
    }
    public async Task<IActionResult> OnPostDeleteOldAppointmentsAsync()
    {
        // Şu andan 7 gün (1 hafta) öncesini hesapla
        var thresholdDate = DateTime.Now.AddDays(-7);

        // 7 günden eski olan randevuları getir
        var oldAppointments = _context.Appointments
            .Where(a => a.StartTime < thresholdDate);

        int deletedCount = await oldAppointments.CountAsync();

        if (deletedCount > 0)
        {
            _context.Appointments.RemoveRange(oldAppointments);
            await _context.SaveChangesAsync();

            // Başarı mesajını da 7 gün olarak güncelledik
            TempData["SuccessMessage"] = $"7 günden eski olan {deletedCount} adet randevu başarıyla arşivden temizlendi.";
        }
        else
        {
            TempData["InfoMessage"] = "Son 7 günden daha eski bir randevu kaydı bulunamadı.";
        }

        return RedirectToPage();
    }
}