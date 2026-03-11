using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SoftetroBarber.Data;
using SoftetroBarber.Enums;
using SoftetroBarber.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SoftetroBarber.Pages.Admin
{
    public class LocalTimeSlot
    {
        public TimeSpan Time { get; set; }
        public bool IsBooked { get; set; }
        public bool IsPassed { get; set; } // Geçmiş saatler için yeni özellik
    }

    [Authorize(Roles = "Admin")]
    public class CreateAppointmentModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        public CreateAppointmentModel(ApplicationDbContext context) => _context = context;

        public List<Barber> ActiveBarbers { get; set; } = new();
        public List<LocalTimeSlot> TimeSlots { get; set; } = new();

        [BindProperty] public string? CustomerName { get; set; }
        [BindProperty] public string CustomerPhone { get; set; } = string.Empty;
        [BindProperty] public decimal? TotalPrice { get; set; }
        [BindProperty] public DateTime AppointmentDate { get; set; } = DateTime.Today;
        [BindProperty] public TimeSpan AppointmentTime { get; set; }
        [BindProperty] public Guid SelectedBarberId { get; set; }
        [BindProperty] public int Duration { get; set; } = 30;

        public async Task OnGetAsync(DateTime? date, Guid? barberId, int duration = 30)
        {
            ActiveBarbers = await _context.Barbers.Where(b => b.IsActive).ToListAsync();
            AppointmentDate = date ?? DateTime.Today;
            Duration = duration;
            SelectedBarberId = barberId ?? (ActiveBarbers.FirstOrDefault()?.Id ?? Guid.Empty);

            if (SelectedBarberId != Guid.Empty) await GenerateInternalTimeSlots();
        }

        private async Task GenerateInternalTimeSlots()
        {
            TimeSlots = new List<LocalTimeSlot>();
            var start = new TimeSpan(10, 0, 0);
            var end = new TimeSpan(21, 0, 0);
            var now = DateTime.Now; // Şu anki zaman

            var existingApts = await _context.Appointments
                .Where(a => a.BarberId == SelectedBarberId && a.StartTime.Date == AppointmentDate.Date)
                .Select(a => new { a.StartTime, a.EndTime })
                .ToListAsync();

            while (start.Add(TimeSpan.FromMinutes(Duration)) <= end)
            {
                var slotStart = AppointmentDate.Date.Add(start);
                var slotEnd = slotStart.AddMinutes(Duration);

                // 1. Durum: Randevu zaten dolu mu?
                bool isBooked = existingApts.Any(a =>
                    (slotStart >= a.StartTime && slotStart < a.EndTime) ||
                    (slotEnd > a.StartTime && slotEnd <= a.EndTime) ||
                    (a.StartTime >= slotStart && a.StartTime < slotEnd));

                // 2. Durum: Bu saat bugün geçti mi?
                bool isPassed = slotStart < now;

                TimeSlots.Add(new LocalTimeSlot
                {
                    Time = start,
                    IsBooked = isBooked || isPassed, // İkisinden biri doğruysa kapat
                    IsPassed = isPassed
                });
                start = start.Add(TimeSpan.FromMinutes(15));
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrEmpty(CustomerPhone)) return Page();
            var startTime = AppointmentDate.Date + AppointmentTime;

            // BACKEND KORUMASI: Form bir şekilde geçilse bile geçmişe randevu kaydetme
            if (startTime < DateTime.Now)
            {
                ModelState.AddModelError("", "Geçmiş bir tarihe/saate randevu alamazsınız.");
                await OnGetAsync(AppointmentDate, SelectedBarberId, Duration);
                return Page();
            }

            var cleanPhone = new string(CustomerPhone.Where(char.IsDigit).ToArray());
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.PhoneNumber == cleanPhone)
                           ?? new Customer { Id = Guid.NewGuid(), FullName = CustomerName ?? "Müşteri", PhoneNumber = cleanPhone };

            if (customer.Id != Guid.Empty && string.IsNullOrEmpty(customer.FullName) == false) _context.Customers.Update(customer);
            else await _context.Customers.AddAsync(customer);

            await _context.Appointments.AddAsync(new Appointment
            {
                Id = Guid.NewGuid(),
                CustomerId = customer.Id,
                BarberId = SelectedBarberId,
                StartTime = startTime,
                EndTime = startTime.AddMinutes(Duration),
                Status = AppointmentStatus.Confirmed,
                TotalPrice = TotalPrice ?? 0
            });

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Randevu başarıyla eklendi.";
            return RedirectToPage("/Admin/Appointments");
        }
    }
}