using System;
using System.ComponentModel.DataAnnotations;

namespace SoftetroBarber.Models;

public abstract class BaseEntity
{
    // Not: Projenizdeki tüm ID alanları mevcut durumda Guid olduğu için arayüz (UI) ve Repository katmanlarının bozulmaması adına Guid olarak ayarlanmıştır.
    public Guid Id { get; set; }
    public bool IsDeleted { get; set; } = false;

    [Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}
