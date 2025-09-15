using System;
using System.Collections.Generic;

namespace BookingTour.Models;

public partial class TourImage
{
    public int ImageId { get; set; }

    public int TourId { get; set; }

    public string ImageUrl { get; set; } = null!;

    public string? Caption { get; set; }

    public DateTime? CreatedDate { get; set; }

    public virtual Tour Tour { get; set; } = null!;
}
