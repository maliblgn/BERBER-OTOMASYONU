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
    public DbSet<Expense> Expenses { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 1. AppointmentService için Composite Primary Key
        modelBuilder.Entity<AppointmentService>()
            .HasKey(x => new { x.AppointmentId, x.ServiceId });

        // Hassasiyet Ayarları
        modelBuilder.Entity<Appointment>().Property(a => a.TotalPrice).HasPrecision(18, 2);
        modelBuilder.Entity<Service>().Property(s => s.Price).HasPrecision(18, 2);
        modelBuilder.Entity<Expense>().Property(e => e.Amount).HasPrecision(18, 2);

        // 2. Cascade Kısıtlamaları (Restrict)
        modelBuilder.Entity<Appointment>()
            .HasOne(a => a.Customer)
            .WithMany(c => c.Appointments)
            .HasForeignKey(a => a.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Appointment>()
            .HasOne(a => a.Barber)
            .WithMany(b => b.Appointments)
            .HasForeignKey(a => a.BarberId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<AppointmentService>()
            .HasOne(asvc => asvc.Service)
            .WithMany(s => s.AppointmentServices)
            .HasForeignKey(asvc => asvc.ServiceId)
            .OnDelete(DeleteBehavior.Restrict);
            
        // 3. Global Query Filter (Soft Delete)
        modelBuilder.Entity<Barber>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Service>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Appointment>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Expense>().HasQueryFilter(e => !e.IsDeleted);

        // 4. Database Indexing
        modelBuilder.Entity<Appointment>().HasIndex(a => a.StartTime);
        modelBuilder.Entity<Appointment>().HasIndex(a => a.BarberId);
        modelBuilder.Entity<Appointment>().HasIndex(a => a.CustomerId);
    }

    public override int SaveChanges()
    {
        HandleSoftDelete();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        HandleSoftDelete();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void HandleSoftDelete()
    {
        var entries = ChangeTracker.Entries<BaseEntity>();
        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Deleted)
            {
                entry.State = EntityState.Modified;
                entry.Entity.IsDeleted = true;
            }
        }
    }
}
