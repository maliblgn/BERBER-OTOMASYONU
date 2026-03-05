namespace SoftetroBarber.ViewModels;

public class BookingSessionModel
{
    public List<Guid> SelectedServiceIds { get; set; } = new List<Guid>();
    public Guid BarberId { get; set; }
    public DateTime SelectedDate { get; set; }
    public TimeSpan SelectedTime { get; set; }
    
    // Additional UI helper properties
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
}
