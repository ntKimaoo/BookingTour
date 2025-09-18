using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BookingTour.Models;

namespace BookingTour.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BookingController : ControllerBase
    {
        private readonly TourBookingSystemContext _context;

        public BookingController(TourBookingSystemContext context)
        {
            _context = context;
        }

        // GET: api/booking
            [HttpGet("GetPaginationBooking")]
        public async Task<ActionResult<IEnumerable<BookingDto>>> GetBookings(
            [FromQuery] int? userId = null,
            [FromQuery] string? status = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var query = _context.Bookings
                .Include(b => b.Tour)
                .Include(b => b.User)
                .Include(b => b.Voucher)
                .Include(b => b.BookingOptions)
                    .ThenInclude(bo => bo.Option)
                .Include(b => b.Payments)
                .AsQueryable();

            if (userId.HasValue)
            {
                query = query.Where(b => b.UserId == userId.Value);
            }

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(b => b.Status == status);
            }

            var totalCount = await query.CountAsync();
            var bookings = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(b => MapToDto(b))
                .ToListAsync();

            Response.Headers.Add("X-Total-Count", totalCount.ToString());
            Response.Headers.Add("X-Page", page.ToString());
            Response.Headers.Add("X-Page-Size", pageSize.ToString());

            return Ok(bookings);
        }

        // GET: api/booking/{id}
        [HttpGet("Get/{id}")]
        public async Task<ActionResult<BookingDto>> GetBooking(int id)
        {
            var booking = await _context.Bookings
                .Include(b => b.Tour)
                .Include(b => b.User)
                .Include(b => b.Voucher)
                .Include(b => b.BookingOptions)
                    .ThenInclude(bo => bo.Option)
                .Include(b => b.Payments)
                .FirstOrDefaultAsync(b => b.BookingId == id);

            if (booking == null)
            {
                return NotFound($"Booking with ID {id} not found.");
            }

            return Ok(MapToDto(booking));
        }

        // POST: api/booking
        [HttpPost("CreateBooking")]
        public async Task<ActionResult<BookingDto>> CreateBooking(CreateBookingDto createBookingDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Validate tour exists
            var tour = await _context.Tours.FindAsync(createBookingDto.TourId);
            if (tour == null)
            {
                return BadRequest("Invalid Tour ID.");
            }

            // Validate user exists
            var user = await _context.Users.FindAsync(createBookingDto.UserId);
            if (user == null)
            {
                return BadRequest("Invalid User ID.");
            }

            // Validate voucher if provided
            if (createBookingDto.VoucherId.HasValue)
            {
                var voucher = await _context.Vouchers.FindAsync(createBookingDto.VoucherId.Value);
                if (voucher == null)
                {
                    return BadRequest("Invalid Voucher ID.");
                }
            }

            var booking = new Booking
            {
                UserId = createBookingDto.UserId,
                TourId = createBookingDto.TourId,
                BookingDate = DateTime.UtcNow,
                NumberOfPeople = createBookingDto.NumberOfPeople,
                TotalAmount = createBookingDto.TotalAmount,
                Status = createBookingDto.Status ?? "Pending",
                PaymentStatus = "Pending",
                Notes = createBookingDto.Notes,
                VoucherId = createBookingDto.VoucherId,
                DiscountAmount = createBookingDto.DiscountAmount
            };

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            // Add booking options if provided
            if (createBookingDto.BookingOptions != null && createBookingDto.BookingOptions.Any())
            {
                foreach (var optionDto in createBookingDto.BookingOptions)
                {
                    var bookingOption = new BookingOption
                    {
                        BookingId = booking.BookingId,
                        OptionId = optionDto.OptionId,
                        Quantity = optionDto.Quantity,
                        UnitPrice = optionDto.UnitPrice,
                        TotalPrice = optionDto.TotalPrice
                    };
                    _context.BookingOptions.Add(bookingOption);
                }
                await _context.SaveChangesAsync();
            }

            // Reload with includes to return complete data
            var createdBooking = await _context.Bookings
                .Include(b => b.Tour)
                .Include(b => b.User)
                .Include(b => b.Voucher)
                .Include(b => b.BookingOptions)
                    .ThenInclude(bo => bo.Option)
                .Include(b => b.Payments)
                .FirstAsync(b => b.BookingId == booking.BookingId);

            return CreatedAtAction(nameof(GetBooking),
                new { id = booking.BookingId },
                MapToDto(createdBooking));
        }

        // PUT: api/booking/{id}
        [HttpPut("Edit/{id}")]
        public async Task<IActionResult> UpdateBooking(int id, UpdateBookingDto updateBookingDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null)
            {
                return NotFound($"Booking with ID {id} not found.");
            }

            // Update properties
            booking.NumberOfPeople = updateBookingDto.NumberOfPeople ?? booking.NumberOfPeople;
            booking.TotalAmount = updateBookingDto.TotalAmount ?? booking.TotalAmount;
            booking.Status = updateBookingDto.Status ?? booking.Status;
            booking.PaymentStatus = updateBookingDto.PaymentStatus ?? booking.PaymentStatus;
            booking.Notes = updateBookingDto.Notes ?? booking.Notes;
            booking.VoucherId = updateBookingDto.VoucherId ?? booking.VoucherId;
            booking.DiscountAmount = updateBookingDto.DiscountAmount ?? booking.DiscountAmount;

            try
            {
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!BookingExists(id))
                {
                    return NotFound();
                }
                throw;
            }
        }

        // DELETE: api/booking/{id}
        [HttpDelete("Delete/{id}")]
        public async Task<IActionResult> DeleteBooking(int id)
        {
            var booking = await _context.Bookings
                .Include(b => b.BookingOptions)
                .Include(b => b.Payments)
                .FirstOrDefaultAsync(b => b.BookingId == id);

            if (booking == null)
            {
                return NotFound($"Booking with ID {id} not found.");
            }

            // Check if booking can be deleted (business logic)
            if (booking.Status == "Completed" || booking.PaymentStatus == "Paid")
            {
                return BadRequest("Cannot delete completed or paid bookings.");
            }

            _context.Bookings.Remove(booking);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // PUT: api/booking/{id}/status
        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateBookingStatus(int id, [FromBody] UpdateStatusDto statusDto)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null)
            {
                return NotFound($"Booking with ID {id} not found.");
            }

            booking.Status = statusDto.Status;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // PUT: api/booking/{id}/payment-status
        [HttpPut("{id}/payment-status")]
        public async Task<IActionResult> UpdatePaymentStatus(int id, [FromBody] UpdatePaymentStatusDto paymentStatusDto)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null)
            {
                return NotFound($"Booking with ID {id} not found.");
            }

            booking.PaymentStatus = paymentStatusDto.PaymentStatus;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // GET: api/booking/{id}/options
        [HttpGet("{id}/options")]
        public async Task<ActionResult<IEnumerable<BookingOptionDto>>> GetBookingOptions(int id)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null)
            {
                return NotFound($"Booking with ID {id} not found.");
            }

            var options = await _context.BookingOptions
                .Include(bo => bo.Option)
                .Where(bo => bo.BookingId == id)
                .Select(bo => new BookingOptionDto
                {
                    BookingOptionId = bo.BookingOptionId,
                    BookingId = bo.BookingId,
                    OptionId = bo.OptionId,
                    OptionName = bo.Option.OptionName,
                    Quantity = bo.Quantity,
                    UnitPrice = bo.UnitPrice,
                    TotalPrice = bo.TotalPrice
                })
                .ToListAsync();

            return Ok(options);
        }

        // GET: api/booking/{id}/payments
        [HttpGet("{id}/payments")]
        public async Task<ActionResult<IEnumerable<PaymentDto>>> GetBookingPayments(int id)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null)
            {
                return NotFound($"Booking with ID {id} not found.");
            }

            var payments = await _context.Payments
                .Where(p => p.BookingId == id)
                .Select(p => new PaymentDto
                {
                    PaymentId = p.PaymentId,
                    BookingId = p.BookingId,
                    PaymentAmount = p.PaymentAmount,
                    PaymentDate = p.PaymentDate,
                    PaymentMethod = p.PaymentMethod,
                    PaymentStatus = p.PaymentStatus,
                    TransactionId = p.TransactionId
                })
                .ToListAsync();

            return Ok(payments);
        }

        // GET: api/booking/statistics
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

        private bool BookingExists(int id)
        {
            return _context.Bookings.Any(e => e.BookingId == id);
        }

        private static BookingDto MapToDto(Booking booking)
        {
            return new BookingDto
            {
                BookingId = booking.BookingId,
                UserId = booking.UserId,
                UserName = booking.User?.FullName,
                TourId = booking.TourId,
                TourName = booking.Tour?.TourName,
                BookingDate = booking.BookingDate,
                NumberOfPeople = booking.NumberOfPeople,
                TotalAmount = booking.TotalAmount,
                Status = booking.Status,
                PaymentStatus = booking.PaymentStatus,
                Notes = booking.Notes,
                VoucherId = booking.VoucherId,
                VoucherCode = booking.Voucher?.VoucherCode,
                DiscountAmount = booking.DiscountAmount,
                BookingOptions = booking.BookingOptions?.Select(bo => new BookingOptionDto
                {
                    BookingOptionId = bo.BookingOptionId,
                    BookingId = bo.BookingId,
                    OptionId = bo.OptionId,
                    OptionName = bo.Option?.OptionName,
                    Quantity = bo.Quantity,
                    UnitPrice = bo.UnitPrice,
                    TotalPrice = bo.TotalPrice
                }).ToList(),
                Payments = booking.Payments?.Select(p => new PaymentDto
                {
                    PaymentId = p.PaymentId,
                    BookingId = p.BookingId,
                    PaymentAmount = p.PaymentAmount,
                    PaymentDate = p.PaymentDate,
                    PaymentMethod = p.PaymentMethod,
                    PaymentStatus = p.PaymentStatus,
                    TransactionId = p.TransactionId
                }).ToList()
            };
        }
    }

    // DTOs
    public class BookingDto
    {
        public int BookingId { get; set; }
        public int UserId { get; set; }
        public string? UserName { get; set; }
        public int TourId { get; set; }
        public string? TourName { get; set; }
        public DateTime? BookingDate { get; set; }
        public int NumberOfPeople { get; set; }
        public decimal TotalAmount { get; set; }
        public string? Status { get; set; }
        public string? PaymentStatus { get; set; }
        public string? Notes { get; set; }
        public int? VoucherId { get; set; }
        public string? VoucherCode { get; set; }
        public decimal? DiscountAmount { get; set; }
        public List<BookingOptionDto>? BookingOptions { get; set; }
        public List<PaymentDto>? Payments { get; set; }
    }

    public class CreateBookingDto
    {
        public int UserId { get; set; }
        public int TourId { get; set; }
        public int NumberOfPeople { get; set; }
        public decimal TotalAmount { get; set; }
        public string? Status { get; set; }
        public string? Notes { get; set; }
        public int? VoucherId { get; set; }
        public decimal? DiscountAmount { get; set; }
        public List<CreateBookingOptionDto>? BookingOptions { get; set; }
    }

    public class UpdateBookingDto
    {
        public int? NumberOfPeople { get; set; }
        public decimal? TotalAmount { get; set; }
        public string? Status { get; set; }
        public string? PaymentStatus { get; set; }
        public string? Notes { get; set; }
        public int? VoucherId { get; set; }
        public decimal? DiscountAmount { get; set; }
    }

    public class BookingOptionDto
    {
        public int BookingOptionId { get; set; }
        public int BookingId { get; set; }
        public int OptionId { get; set; }
        public string? OptionName { get; set; }
        public int? Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
    }

    public class CreateBookingOptionDto
    {
        public int OptionId { get; set; }
        public int? Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
    }

    public class PaymentDto
    {
        public int PaymentId { get; set; }
        public int BookingId { get; set; }
        public decimal PaymentAmount { get; set; }
        public DateTime? PaymentDate { get; set; }
        public string? PaymentMethod { get; set; }
        public string? PaymentStatus { get; set; }
        public string? TransactionId { get; set; }
    }

    public class UpdateStatusDto
    {
        public string Status { get; set; } = null!;
    }

    public class UpdatePaymentStatusDto
    {
        public string PaymentStatus { get; set; } = null!;
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