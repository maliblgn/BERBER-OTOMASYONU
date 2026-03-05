namespace SoftetroBarber.Models;

public class AppointmentService
{
    public Guid AppointmentId { get; set; }
    public Guid ServiceId { get; set; }

    // Navigation Properties
    public Appointment Appointment { get; set; } = null!;
    public Service Service { get; set; } = null!;
}
