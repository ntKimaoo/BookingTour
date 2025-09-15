using System;
using System.Collections.Generic;

namespace BookingTour.Models;

public partial class Payment
{
    public int PaymentId { get; set; }

    public int BookingId { get; set; }

    public decimal PaymentAmount { get; set; }

    public DateTime? PaymentDate { get; set; }

    public string? PaymentMethod { get; set; }

    public string? PaymentStatus { get; set; }

    public string? TransactionId { get; set; }

    public virtual Booking Booking { get; set; } = null!;
}
