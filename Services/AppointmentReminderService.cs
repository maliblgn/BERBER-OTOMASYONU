using Microsoft.EntityFrameworkCore;
using SoftetroBarber.Data;
using SoftetroBarber.Enums;

namespace SoftetroBarber.Services;

public class AppointmentReminderService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AppointmentReminderService> _logger;

    public AppointmentReminderService(IServiceProvider serviceProvider, ILogger<AppointmentReminderService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Appointment Reminder Background Service is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessRemindersAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred executing Appointment Reminder Background Service.");
            }

            // Bekleme süresi: 5 dakika
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }

        _logger.LogInformation("Appointment Reminder Background Service is stopping.");
    }

    private async Task ProcessRemindersAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var whatsappService = scope.ServiceProvider.GetRequiredService<IWhatsAppService>();

        var currentTime = DateTime.Now;

        // Bulunduğumuz andan 60 ile 65 dakika arası ilerideki randevuları bul
        var thresholdStart = currentTime.AddMinutes(60);
        var thresholdEnd = currentTime.AddMinutes(65);

        var upcomingAppointments = await dbContext.Appointments
            .Include(a => a.Customer)
            .Include(a => a.Barber)
            .Where(a => a.Status == AppointmentStatus.Confirmed
                        && !a.IsReminderSent
                        && a.StartTime >= thresholdStart
                        && a.StartTime <= thresholdEnd)
            .ToListAsync();

        if (upcomingAppointments.Any())
        {
            _logger.LogInformation($"Found {upcomingAppointments.Count} appointments to remind.");

            foreach (var appointment in upcomingAppointments)
            {
                // Send WhatsApp Reminder (Simulated for now)
                string message = $"Sayın {appointment.Customer.FullName}, {appointment.Barber.Name} ile olan randevunuza 1 saat kalmıştır. Bizi tercih ettiğiniz için teşekkür ederiz. Konum: https://maps.google.com/...";
                
                // Müşteri numarasına Mesaj Atılıyor.
                _logger.LogInformation($"[WHATSAPP MESSAGE SENT to {appointment.Customer.PhoneNumber}]: {message}");

                // Update IsReminderSent flag
                appointment.IsReminderSent = true;
                dbContext.Appointments.Update(appointment);
            }

            await dbContext.SaveChangesAsync();
        }
    }
}
