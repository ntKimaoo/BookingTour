using System;
using System.Collections.Generic;

namespace BookingTour.Models;

public partial class TourOption
{
    public int OptionId { get; set; }

    public string OptionName { get; set; } = null!;

    public string? Description { get; set; }

    public string Category { get; set; } = null!;

    public decimal Price { get; set; }

    public string? PriceType { get; set; }

    public string? Status { get; set; }

    public DateTime? CreatedDate { get; set; }

    public virtual ICollection<BookingOption> BookingOptions { get; set; } = new List<BookingOption>();

    public virtual ICollection<TourOptionAvailable> TourOptionAvailables { get; set; } = new List<TourOptionAvailable>();
}
