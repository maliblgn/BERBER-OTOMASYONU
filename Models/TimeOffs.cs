namespace SoftetroBarber.Models;

public class TimeOffs
{
    public Guid Id { get; set; }
    public Guid BarberId { get; set; }
    public DateTime StartDateTime { get; set; }
    public DateTime EndDateTime { get; set; }
    public string? Reason { get; set; }

    // Navigation Properties
    public Barber Barber { get; set; } = null!;
}
