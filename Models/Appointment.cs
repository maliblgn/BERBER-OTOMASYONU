using SoftetroBarber.Enums;

namespace SoftetroBarber.Models;

public class Appointment : BaseEntity
{
    public Guid CustomerId { get; set; }
    public Guid BarberId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public decimal TotalPrice { get; set; }
    public AppointmentStatus Status { get; set; }
    public bool IsReminderSent { get; set; } = false;

    // Navigation Properties
    public Customer Customer { get; set; } = null!;
    public Barber Barber { get; set; } = null!;
    public ICollection<AppointmentService> AppointmentServices { get; set; } = new List<AppointmentService>();
}
