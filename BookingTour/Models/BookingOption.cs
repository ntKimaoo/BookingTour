using System;
using System.Collections.Generic;

namespace BookingTour.Models;

public partial class BookingOption
{
    public int BookingOptionId { get; set; }

    public int BookingId { get; set; }

    public int OptionId { get; set; }

    public int? Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal TotalPrice { get; set; }

    public virtual Booking Booking { get; set; } = null!;

    public virtual TourOption Option { get; set; } = null!;
}
