using System;
using System.Collections.Generic;

namespace BookingTour.Models;

public partial class TourOptionAvailable
{
    public int TourId { get; set; }

    public int OptionId { get; set; }

    public bool? IsDefault { get; set; }

    public virtual TourOption Option { get; set; } = null!;

    public virtual Tour Tour { get; set; } = null!;
}
