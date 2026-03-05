using Microsoft.EntityFrameworkCore;
using SoftetroBarber.Models;

namespace SoftetroBarber.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Customer> Customers { get; set; }
    public DbSet<Barber> Barbers { get; set; }
    public DbSet<Service> Services { get; set; }
    public DbSet<WorkingHours> WorkingHours { get; set; }
    public DbSet<TimeOffs> TimeOffs { get; set; }
    public DbSet<Appointment> Appointments { get; set; }
    public DbSet<AppointmentService> AppointmentServices { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 1. AppointmentService için Composite Primary Key (Çoka çok ilişki için tablo anahtarı)
        modelBuilder.Entity<AppointmentService>()
            .HasKey(x => new { x.AppointmentId, x.ServiceId });

        // Hassasiyet Ayarları (Decimal alanlar için)
        modelBuilder.Entity<Appointment>().Property(a => a.TotalPrice).HasPrecision(18, 2);
        modelBuilder.Entity<Service>().Property(s => s.Price).HasPrecision(18, 2);

        // 2. SQL Server Multiple-Cascade-Path Çakışmalarını Önlemek İçin Kısıtlamalar (Restrict)
        
        // Bir müşteri silinmek istenirse ve randevuları varsa, silmeyi engelle.
        modelBuilder.Entity<Appointment>()
            .HasOne(a => a.Customer)
            .WithMany(c => c.Appointments)
            .HasForeignKey(a => a.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        // Bir berber silinmek istenirse, randevuları varsa silmeyi engelle.
        modelBuilder.Entity<Appointment>()
            .HasOne(a => a.Barber)
            .WithMany(b => b.Appointments)
            .HasForeignKey(a => a.BarberId)
            .OnDelete(DeleteBehavior.Restrict);

        // Hizmetler içinden bir servis silindiğinde direkt olarak o servisin bağlı olduğu tüm randevu geçmişini silmeyi engelle.
        modelBuilder.Entity<AppointmentService>()
            .HasOne(asvc => asvc.Service)
            .WithMany(s => s.AppointmentServices)
            .HasForeignKey(asvc => asvc.ServiceId)
            .OnDelete(DeleteBehavior.Restrict);
            
        // Not: WorkingHours, TimeOffs gibi tamamen barbere bağımlı olan alt verilerde default özellik Action/Cascade kalabilir.
        // Berberin molası veya çalışma saati, berberle beraber silinmesinde bir problem yoktur.
    }
}
