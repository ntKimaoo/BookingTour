using BookingTour.Models;
using Microsoft.AspNetCore.Mvc;

namespace BookingTour.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TourController : Controller
    {
        private readonly TourBookingSystemContext _context; // Replace with your actual DbContext name

        public TourController(TourBookingSystemContext context)
        {
            _context = context;
        }
        [HttpGet("GetAllTours")]
        public IActionResult GetAllTours()
        {
            var tours = _context.Tours.ToList();
            return Ok(tours);
        }
    }
}
