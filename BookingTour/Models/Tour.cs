using System;
using System.Collections.Generic;

namespace BookingTour.Models;

public partial class Tour
{
    public int TourId { get; set; }

    public string TourName { get; set; } = null!;

    public string Destination { get; set; } = null!;

    public string? Description { get; set; }

    public int Duration { get; set; }

    public decimal Price { get; set; }

    public int MaxParticipants { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public string? Status { get; set; }

    public DateTime? CreatedDate { get; set; }

    public string? Transport { get; set; }

    public string? Thumbnail { get; set; }
    public bool IsActive { get; set; }
    public bool IsDelete { get; set; }
    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    public virtual ICollection<TourCondition> TourConditions { get; set; } = new List<TourCondition>();

    public virtual ICollection<TourImage> TourImages { get; set; } = new List<TourImage>();

    public virtual ICollection<TourOptionAvailable> TourOptionAvailables { get; set; } = new List<TourOptionAvailable>();
}
