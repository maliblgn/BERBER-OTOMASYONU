using Microsoft.EntityFrameworkCore;
using SoftetroBarber.Data;
using SoftetroBarber.Enums;

namespace SoftetroBarber.Services;

public class AppointmentStatusWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AppointmentStatusWorker> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(30);

    public AppointmentStatusWorker(IServiceProvider serviceProvider, ILogger<AppointmentStatusWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Appointment Status Worker Service is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessAppointmentsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while executing Appointment Status Worker Service.");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("Appointment Status Worker Service is stopping.");
    }

    private async Task ProcessAppointmentsAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var currentTime = DateTime.Now;
        var thresholdTime = currentTime.AddHours(-1);

        // Find appointments that are Confirmed and their EndTime is older than 1 hour from now
        var appointmentsToComplete = await dbContext.Appointments
            .Where(a => a.Status == AppointmentStatus.Confirmed && a.EndTime <= thresholdTime)
            .ToListAsync(stoppingToken);

        if (appointmentsToComplete.Any())
        {
            foreach (var appointment in appointmentsToComplete)
            {
                appointment.Status = AppointmentStatus.Completed;
            }

            await dbContext.SaveChangesAsync(stoppingToken);
            _logger.LogInformation($"Appointment Status Worker: Successfully updated {appointmentsToComplete.Count} appointments to Completed.");
        }
        else
        {
            _logger.LogInformation("Appointment Status Worker: No appointments needed status update.");
        }
    }
}
