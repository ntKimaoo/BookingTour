using System;
using System.Collections.Generic;

namespace BookingTour.Models;

public partial class TourCondition
{
    public int ConditionId { get; set; }

    public int TourId { get; set; }

    public string Title { get; set; } = null!;

    public string Content { get; set; } = null!;

    public DateTime? CreatedDate { get; set; }

    public virtual Tour Tour { get; set; } = null!;
}
