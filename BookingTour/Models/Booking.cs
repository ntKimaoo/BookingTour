using System;
using System.Collections.Generic;

namespace BookingTour.Models;

public partial class Booking
{
    public int BookingId { get; set; }

    public int UserId { get; set; }

    public int TourId { get; set; }

    public DateTime? BookingDate { get; set; }

    public int NumberOfPeople { get; set; }

    public decimal TotalAmount { get; set; }

    public string? Status { get; set; }

    public string? PaymentStatus { get; set; }

    public string? Notes { get; set; }

    public int? VoucherId { get; set; }

    public decimal? DiscountAmount { get; set; }
    
    public DateTime? CreatedDate { get; set; }
    public virtual ICollection<BookingOption> BookingOptions { get; set; } = new List<BookingOption>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual Tour Tour { get; set; } = null!;

    public virtual User User { get; set; } = null!;

    public virtual Voucher? Voucher { get; set; }
}
