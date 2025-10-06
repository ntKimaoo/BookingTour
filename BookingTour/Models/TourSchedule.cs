using System.ComponentModel.DataAnnotations;

namespace BookingTour.Models
{
    public class TourSchedule
    {
        [Key]
        public int ScheduleId { get; set; }

        public int TourId { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public int AvailableSlots { get; set; }  // số chỗ còn trống (tuỳ chọn)

        public string? Note { get; set; }

        // Navigation property
        public virtual Tour Tour { get; set; } = null!;
    }
}
