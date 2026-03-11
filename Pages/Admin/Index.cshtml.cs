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

    public int TodayCompletedCount { get; set; }
    public int TotalBarbersCount { get; set; }
    public int TotalCustomersCount { get; set; }
    public decimal TodayRevenue { get; set; }
    public decimal TodayNetProfit { get; set; }

    public List<Models.Appointment> UpcomingAppointments { get; set; } = new();

    // Yeni Analitik Değişkenler
    public Dictionary<string, decimal> Last7DaysRevenue { get; set; } = new();
    public Dictionary<string, decimal> Last7DaysExpense { get; set; } = new();
    public Dictionary<string, int> TopServicesUsage { get; set; } = new();
    public List<TopBarberDto> TopBarbers { get; set; } = new();

    public class TopBarberDto
    {
        public string Name { get; set; } = string.Empty;
        public int CompletedAppointments { get; set; }
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var now = DateTime.Now; // Şu anki tarih ve saat: 09.03.2026 16:31
        var today = DateTime.Today;

        // Ciro kartında tamamlanan işlemlerin sayısını ("miktarını") da göstermek için
        TodayCompletedCount = await _context.Appointments
            .Where(a => a.StartTime.Date == today && a.Status == AppointmentStatus.Completed)
            .CountAsync();

        TotalBarbersCount = await _context.Barbers.CountAsync(b => b.IsActive);
        TotalCustomersCount = await _context.Customers.CountAsync();

        TodayRevenue = await _context.Appointments
            .Where(a => a.StartTime.Date == today && a.Status == AppointmentStatus.Completed)
            .SumAsync(a => a.TotalPrice);

        var todayExpense = await _context.Expenses
            .Where(e => e.Date.Date == today)
            .SumAsync(e => e.Amount);

        TodayNetProfit = TodayRevenue - todayExpense;

        // Sadece bugünün KALAN randevuları
        UpcomingAppointments = await _context.Appointments
            .Include(a => a.Customer)
            .Include(a => a.Barber)
            .Where(a => a.StartTime >= now && a.StartTime.Date == today && a.Status == AppointmentStatus.Confirmed)
            .OrderBy(a => a.StartTime)
            .ToListAsync();

        // 1. Son 7 Günün Geliri ve Gideri
        var sevenDaysAgo = today.AddDays(-6);
        var weeklyRevenueData = await _context.Appointments
            .Where(a => a.Status == AppointmentStatus.Completed && a.StartTime.Date >= sevenDaysAgo && a.StartTime.Date <= today)
            .GroupBy(a => a.StartTime.Date)
            .Select(g => new { Date = g.Key, Revenue = g.Sum(a => a.TotalPrice) })
            .ToListAsync();

        var weeklyExpenseData = await _context.Expenses
            .Where(e => e.Date.Date >= sevenDaysAgo && e.Date.Date <= today)
            .GroupBy(e => e.Date.Date)
            .Select(g => new { Date = g.Key, Expense = g.Sum(e => e.Amount) })
            .ToListAsync();

        for (int i = 0; i < 7; i++)
        {
            var targetDate = sevenDaysAgo.AddDays(i);
            
            var revForDay = weeklyRevenueData.FirstOrDefault(x => x.Date == targetDate)?.Revenue ?? 0;
            Last7DaysRevenue.Add(targetDate.ToString("dd MMM"), revForDay);

            var expForDay = weeklyExpenseData.FirstOrDefault(x => x.Date == targetDate)?.Expense ?? 0;
            Last7DaysExpense.Add(targetDate.ToString("dd MMM"), expForDay);
        }

        // 2. Hizmet Dağılımı (Top Services)
        var servicesUsage = await _context.AppointmentServices
            .Include(a => a.Service)
            .Include(a => a.Appointment)
            .Where(a => a.Appointment.Status == AppointmentStatus.Completed)
            .GroupBy(a => a.Service.Name)
            .Select(g => new { ServiceName = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(5)
            .ToListAsync();

        foreach (var item in servicesUsage)
            TopServicesUsage.Add(item.ServiceName, item.Count);

        // 3. Popüler Berberler (En çok randevu tamamlayan ilk 3)
        TopBarbers = await _context.Appointments
            .Include(a => a.Barber)
            .Where(a => a.Status == AppointmentStatus.Completed)
            .GroupBy(a => a.Barber.Name)
            .Select(g => new TopBarberDto { Name = g.Key, CompletedAppointments = g.Count() })
            .OrderByDescending(b => b.CompletedAppointments)
            .Take(3)
            .ToListAsync();

        return Page();
    }
}
