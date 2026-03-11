using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SoftetroBarber.Data;
using SoftetroBarber.Enums;
using SoftetroBarber.Models;
using SoftetroBarber.Services;

namespace SoftetroBarber.Pages.Admin;

[Authorize(Roles = "Admin")]
public class CreateAppointmentModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly IBookingService _bookingService;

    public CreateAppointmentModel(ApplicationDbContext context, IBookingService bookingService)
    {
        _context = context;
        _bookingService = bookingService;
    }

    // Reference Data
    public List<Barber> ActiveBarbers { get; set; } = new();
    public List<Service> AvailableServices { get; set; } = new();
    public List<TimeSlotDto> TimeSlots { get; set; } = new();

    // Bound Form Data
    [BindProperty] public string CustomerName { get; set; } = string.Empty;
    [BindProperty] public string CustomerPhone { get; set; } = string.Empty;
    [BindProperty] public List<Guid> SelectedServiceIds { get; set; } = new();
    [BindProperty] public DateTime AppointmentDate { get; set; } = DateTime.Today;
    [BindProperty] public TimeSpan AppointmentTime { get; set; }
    [BindProperty] public Guid SelectedBarberId { get; set; }

    public async Task OnGetAsync(DateTime? date, int duration = 30)
    {
        await LoadReferenceDataAsync();

        // Eğer belli bir tarih seçilip post-back olmuşsa saatleri ona göre yükle
        var dateToLoad = date ?? DateTime.Today;
        AppointmentDate = dateToLoad;
        TimeSlots = await _bookingService.GetTimeSlotsForDateAsync(dateToLoad, duration);
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await LoadReferenceDataAsync(); // Model hatalıysa geri dönerken listeler boş olmasın diye

        if (!SelectedServiceIds.Any())
        {
            ModelState.AddModelError("", "Lütfen en az bir hizmet seçin.");
            return Page();
        }

        if (string.IsNullOrWhiteSpace(CustomerName) || string.IsNullOrWhiteSpace(CustomerPhone))
        {
            ModelState.AddModelError("", "Müşteri adı ve telefonu zorunludur.");
            return Page();
        }

        // Seçili hizmetleri ve süreleri topla
        var services = await _context.Services.Where(s => SelectedServiceIds.Contains(s.Id)).ToListAsync();
        int totalDuration = services.Sum(s => s.DurationInMinutes);
        decimal totalPrice = services.Sum(s => s.Price);

        // Çakışma (Conflict) Kontrolü
        var availableBarbersForSlot = await _bookingService.GetAvailableBarbersForSlotAsync(AppointmentDate, AppointmentTime, totalDuration);

        if (!availableBarbersForSlot.Any(b => b.Id == SelectedBarberId))
        {
            ModelState.AddModelError("", "Seçilen berber bu saat diliminde uygun değildir (Mesai saati dışı, izinli veya başka randevusu var). Lütfen başka bir saat veya berber seçiniz.");
            TimeSlots = await _bookingService.GetTimeSlotsForDateAsync(AppointmentDate, totalDuration);
            return Page();
        }

        // Transaction Başlat
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Müşteriyi bul veya oluştur
            var cleanPhone = new string(CustomerPhone.Where(char.IsDigit).ToArray());
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.PhoneNumber == cleanPhone);

            if (customer == null)
            {
                customer = new Customer { Id = Guid.NewGuid(), FullName = CustomerName, PhoneNumber = cleanPhone };
                await _context.Customers.AddAsync(customer);
            }
            else
            {
                customer.FullName = CustomerName; // İsim güncel değilse ez
            }

            var startTime = AppointmentDate.Date + AppointmentTime;
            
            // Randevu Oluştur
            var appointment = new Appointment
            {
                Id = Guid.NewGuid(),
                CustomerId = customer.Id,
                BarberId = SelectedBarberId,
                StartTime = startTime,
                EndTime = startTime.AddMinutes(totalDuration),
                TotalPrice = totalPrice,
                Status = AppointmentStatus.Confirmed
            };
            await _context.Appointments.AddAsync(appointment);

            // İlişkili Servisleri Ekle
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

            TempData["SuccessMessage"] = "Randevu başarıyla manuel olarak oluşturuldu.";
            return RedirectToPage("/Admin/Appointments");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            ModelState.AddModelError("", "Kayıt işlemi sırasında sistemsel bir hata oluştu: " + ex.Message);
            return Page();
        }
    }

    private async Task LoadReferenceDataAsync()
    {
        ActiveBarbers = await _context.Barbers.Where(b => b.IsActive).ToListAsync();
        AvailableServices = await _context.Services.ToListAsync();
    }
}
