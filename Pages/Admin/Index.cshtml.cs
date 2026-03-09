using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SoftetroBarber.Data;
using SoftetroBarber.Enums;

namespace SoftetroBarber.Pages.Admin;

[Authorize(Roles = "Admin")]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public IndexModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public int TodayAppointmentsCount { get; set; }
    public int TotalBarbersCount { get; set; }
    public int TotalCustomersCount { get; set; }
    public decimal TodayRevenue { get; set; }

    public List<Models.Appointment> UpcomingAppointments { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        var now = DateTime.Now; // Şu anki tarih ve saat: 09.03.2026 16:31
        var today = DateTime.Today;

        // İstatistikler aynı kalıyor
        TodayAppointmentsCount = await _context.Appointments
            .Where(a => a.StartTime.Date == today && a.Status != AppointmentStatus.Cancelled)
            .CountAsync();

        TotalBarbersCount = await _context.Barbers.CountAsync(b => b.IsActive);
        TotalCustomersCount = await _context.Customers.CountAsync();

        TodayRevenue = await _context.Appointments
            .Where(a => a.StartTime.Date == today && a.Status == AppointmentStatus.Completed)
            .SumAsync(a => a.TotalPrice);

        // Sadece bugünün KALAN randevuları
        UpcomingAppointments = await _context.Appointments
            .Include(a => a.Customer)
            .Include(a => a.Barber)
            .Where(a => a.StartTime >= now && a.StartTime.Date == today && a.Status == AppointmentStatus.Confirmed)
            .OrderBy(a => a.StartTime)
            .ToListAsync();

        return Page();
    }
}
