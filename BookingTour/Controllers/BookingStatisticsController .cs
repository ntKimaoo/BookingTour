using BookingTour.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookingTour.Controllers
{
    public class BookingStatisticsController : Controller
    {
        private readonly TourBookingSystemContext _context;

        public BookingStatisticsController(TourBookingSystemContext context)
        {
            _context = context;
        }
        /// <summary>
        /// Lấy tổng doanh thu trong 1 tháng
        /// Tính tổng TotalAmount của tất cả booking có CreatedDate trong tháng chỉ định
        /// </summary>
        /// <param name="year">Năm (VD: 2024)</param>
        /// <param name="month">Tháng (1-12)</param>
        /// <returns>Tổng doanh thu trong tháng</returns>
        [HttpGet("monthly-revenue")]
        public async Task<ActionResult<decimal>> GetMonthlyRevenue(int year, int month)
        {
            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            var totalRevenue = await _context.Bookings
                .Where(b => b.CreatedDate.HasValue &&
                           b.CreatedDate.Value.Date >= startDate &&
                           b.CreatedDate.Value.Date <= endDate &&
                           b.Status == "Confirmed")
                .SumAsync(b => b.TotalAmount);

            return Ok(totalRevenue);
        }

        /// <summary>
        /// Lấy tổng số đơn đặt tour trong 1 tháng
        /// Đếm số lượng booking được tạo trong tháng chỉ định
        /// </summary>
        /// <param name="year">Năm</param>
        /// <param name="month">Tháng</param>
        /// <returns>Số lượng đơn đặt tour</returns>
        [HttpGet("monthly-bookings-count")]
        public async Task<ActionResult<int>> GetMonthlyBookingsCount(int year, int month)
        {
            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            var bookingsCount = await _context.Bookings
                .Where(b => b.CreatedDate.HasValue &&
                           b.CreatedDate.Value.Date >= startDate &&
                           b.CreatedDate.Value.Date <= endDate)
                .CountAsync();

            return Ok(bookingsCount);
        }

        /// <summary>
        /// Lấy số khách hàng tham gia trong 1 tháng (cả user lẫn non-user)
        /// Tính tổng NumberOfPeople của tất cả booking trong tháng
        /// </summary>
        /// <param name="year">Năm</param>
        /// <param name="month">Tháng</param>
        /// <returns>Tổng số khách hàng tham gia</returns>
        [HttpGet("monthly-participants")]
        public async Task<ActionResult<int>> GetMonthlyParticipants(int year, int month)
        {
            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            var totalParticipants = await _context.Bookings
                .Where(b => b.CreatedDate.HasValue &&
                           b.CreatedDate.Value.Date >= startDate &&
                           b.CreatedDate.Value.Date <= endDate &&
                           b.Status == "Confirmed")
                .SumAsync(b => b.NumberOfPeople);

            return Ok(totalParticipants);
        }

        /// <summary>
        /// Lấy tổng số tour đang hoạt động (active)
        /// Giả sử tour có trạng thái IsActive hoặc Status = "Active"
        /// </summary>
        /// <returns>Số lượng tour đang hoạt động</returns>
        [HttpGet("active-tours-count")]
        public async Task<ActionResult<int>> GetActiveToursCount()
        {
            // Giả sử Tour model có thuộc tính IsActive hoặc Status
            // Thay đổi điều kiện tùy theo cấu trúc Tour model của bạn
            var activeToursCount = await _context.Tours
                .Where(t => t.IsActive == true) // Hoặc t.Status == "Active"
                .CountAsync();

            return Ok(activeToursCount);
        }

        /// <summary>
        /// Lấy Top 5 tour có doanh thu cao nhất
        /// Tính tổng doanh thu theo TourId và sắp xếp giảm dần
        /// </summary>
        /// <returns>Top 5 tour có doanh thu cao nhất</returns>
        [HttpGet("top-revenue-tours")]
        public async Task<ActionResult<IEnumerable<object>>> GetTopRevenueTours()
        {
            var topTours = await _context.Bookings
                .Where(b => b.Status == "Confirmed")
                .GroupBy(b => b.TourId)
                .Select(g => new
                {
                    TourId = g.Key,
                    TourName = g.First().Tour.TourName, // Giả sử Tour có thuộc tính Name
                    TotalRevenue = g.Sum(b => b.TotalAmount),
                    BookingsCount = g.Count()
                })
                .OrderByDescending(x => x.TotalRevenue)
                .Take(5)
                .ToListAsync();

            return Ok(topTours);
        }

        /// <summary>
        /// Lấy tổng doanh thu mỗi tour
        /// Nhóm theo TourId và tính tổng doanh thu cho từng tour
        /// </summary>
        /// <returns>Danh sách doanh thu từng tour</returns>
        [HttpGet("tours-revenue")]
        public async Task<ActionResult<IEnumerable<object>>> GetToursRevenue()
        {
            var toursRevenue = await _context.Bookings
                .Where(b => b.Status == "Confirmed")
                .GroupBy(b => b.TourId)
                .Select(g => new
                {
                    TourId = g.Key,
                    TourName = g.First().Tour.TourName,
                    TotalRevenue = g.Sum(b => b.TotalAmount),
                    BookingsCount = g.Count(),
                    TotalParticipants = g.Sum(b => b.NumberOfPeople)
                })
                .OrderByDescending(x => x.TotalRevenue)
                .ToListAsync();

            return Ok(toursRevenue);
        }

        /// <summary>
        /// Lấy tổng số lượng đơn đặt tour đối với mỗi tour
        /// Đếm số booking cho từng tour
        /// </summary>
        /// <returns>Số lượng booking của từng tour</returns>
        [HttpGet("tours-bookings-count")]
        public async Task<ActionResult<IEnumerable<object>>> GetToursBookingsCount()
        {
            var toursBookingsCount = await _context.Bookings
                .GroupBy(b => b.TourId)
                .Select(g => new
                {
                    TourId = g.Key,
                    TourName = g.First().Tour.TourName, // Giả sử Tour có thuộc tính Name
                    BookingsCount = g.Count(),
                    ConfirmedBookings = g.Count(b => b.Status == "Confirmed"),
                    PendingBookings = g.Count(b => b.Status == "Pending"),
                    CancelledBookings = g.Count(b => b.Status == "Cancelled")
                })
                .OrderByDescending(x => x.BookingsCount)
                .ToListAsync();

            return Ok(toursBookingsCount);
        }

        /// <summary>
        /// Lấy 5 đơn đặt tour gần nhất
        /// Sắp xếp theo CreatedDate giảm dần và lấy 5 đơn đầu
        /// </summary>
        /// <returns>5 booking mới nhất</returns>
        [HttpGet("recent-bookings/5")]
        public async Task<ActionResult<IEnumerable<Booking>>> GetRecentBookings5()
        {
            var recentBookings = await _context.Bookings
                .Include(b => b.Tour)
                .Include(b => b.User)
                .OrderByDescending(b => b.CreatedDate)
                .Take(5)
                .ToListAsync();

            return Ok(recentBookings);
        }

        /// <summary>
        /// Lấy 20 đơn đặt tour gần nhất
        /// Sắp xếp theo CreatedDate giảm dần và lấy 20 đơn đầu
        /// </summary>
        /// <returns>20 booking mới nhất</returns>
        [HttpGet("recent-bookings/20")]
        public async Task<ActionResult<IEnumerable<Booking>>> GetRecentBookings20()
        {
            var recentBookings = await _context.Bookings
                .Include(b => b.Tour)
                .Include(b => b.User)
                .OrderByDescending(b => b.CreatedDate)
                .Take(20)
                .ToListAsync();

            return Ok(recentBookings);
        }

        /// <summary>
        /// API tổng hợp: Lấy thống kê tổng quan hệ thống
        /// Bao gồm tổng doanh thu, tổng booking, tổng khách hàng, tour active
        /// </summary>
        /// <returns>Thống kê tổng quan</returns>
        [HttpGet("overview-statistics")]
        public async Task<ActionResult<object>> GetOverviewStatistics()
        {
            var currentMonth = DateTime.Now.Month;
            var currentYear = DateTime.Now.Year;
            var startDate = new DateTime(currentYear, currentMonth, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            var statistics = new
            {
                MonthlyRevenue = await _context.Bookings
                    .Where(b => b.CreatedDate.HasValue &&
                               b.CreatedDate.Value.Date >= startDate &&
                               b.CreatedDate.Value.Date <= endDate &&
                               b.Status == "Confirmed")
                    .SumAsync(b => b.TotalAmount),

                MonthlyBookingsCount = await _context.Bookings
                    .Where(b => b.CreatedDate.HasValue &&
                               b.CreatedDate.Value.Date >= startDate &&
                               b.CreatedDate.Value.Date <= endDate)
                    .CountAsync(),

                MonthlyParticipants = await _context.Bookings
                    .Where(b => b.CreatedDate.HasValue &&
                               b.CreatedDate.Value.Date >= startDate &&
                               b.CreatedDate.Value.Date <= endDate &&
                               b.Status == "Confirmed")
                    .SumAsync(b => b.NumberOfPeople),

                ActiveToursCount = await _context.Tours
                    .Where(t => t.IsActive == true)
                    .CountAsync(),

                TotalRevenue = await _context.Bookings
                    .Where(b => b.Status == "Confirmed")
                    .SumAsync(b => b.TotalAmount),

                TotalBookings = await _context.Bookings.CountAsync(),

                Month = currentMonth,
                Year = currentYear
            };

            return Ok(statistics);
        }
        // GET: api/booking/statistics
        /// <summary>
        /// Lấy tổng số booking theo trạng thái
        /// </summary>
        [HttpGet("statistics")]
        public async Task<ActionResult<BookingStatisticsDto>> GetBookingStatistics()
        {
            var totalBookings = await _context.Bookings.CountAsync();
            var pendingBookings = await _context.Bookings.CountAsync(b => b.Status == "Pending");
            var confirmedBookings = await _context.Bookings.CountAsync(b => b.Status == "Confirmed");
            var completedBookings = await _context.Bookings.CountAsync(b => b.Status == "Completed");
            var cancelledBookings = await _context.Bookings.CountAsync(b => b.Status == "Cancelled");
            var totalRevenue = await _context.Bookings.SumAsync(b => b.TotalAmount);

            return Ok(new BookingStatisticsDto
            {
                TotalBookings = totalBookings,
                PendingBookings = pendingBookings,
                ConfirmedBookings = confirmedBookings,
                CompletedBookings = completedBookings,
                CancelledBookings = cancelledBookings,
                TotalRevenue = totalRevenue
            });
        }
    }
    public class BookingStatisticsDto
    {
        public int TotalBookings { get; set; }
        public int PendingBookings { get; set; }
        public int ConfirmedBookings { get; set; }
        public int CompletedBookings { get; set; }
        public int CancelledBookings { get; set; }
        public decimal TotalRevenue { get; set; }
    }
}
