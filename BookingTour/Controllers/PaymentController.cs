using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BookingTour.Models;

namespace BookingTour.Controllers
{
    /// <summary>
    /// API quản lý thanh toán cho hệ thống booking tour
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentController : ControllerBase
    {
        private readonly TourBookingSystemContext _context;

        public PaymentController(TourBookingSystemContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Lấy danh sách tất cả các payment
        /// </summary>
        /// <returns>Danh sách payment kèm thông tin booking</returns>
        /// <response code="200">Trả về danh sách payment thành công</response>
        // GET: api/Payment
        [HttpGet]
        [ProducesResponseType(200)]
        public async Task<ActionResult<IEnumerable<Payment>>> GetPayments()
        {
            return await _context.Payments
                .Include(p => p.Booking)
                .ToListAsync();
        }

        /// <summary>
        /// Lấy thông tin payment theo ID
        /// </summary>
        /// <param name="id">ID của payment cần lấy</param>
        /// <returns>Thông tin payment</returns>
        /// <response code="200">Tìm thấy payment</response>
        /// <response code="404">Không tìm thấy payment</response>
        // GET: api/Payment/5
        [HttpGet("{id}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<Payment>> GetPayment(int id)
        {
            var payment = await _context.Payments
                .Include(p => p.Booking)
                .FirstOrDefaultAsync(p => p.PaymentId == id);

            if (payment == null)
            {
                return NotFound();
            }

            return payment;
        }

        /// <summary>
        /// Lấy danh sách payment theo booking ID
        /// </summary>
        /// <param name="bookingId">ID của booking</param>
        /// <returns>Danh sách payment của booking đó</returns>
        /// <remarks>
        /// Dùng để xem lịch sử thanh toán của 1 booking cụ thể
        /// </remarks>
        /// <response code="200">Danh sách payment (có thể rỗng)</response>
        // GET: api/Payment/booking/5
        [HttpGet("booking/{bookingId}")]
        [ProducesResponseType(200)]
        public async Task<ActionResult<IEnumerable<Payment>>> GetPaymentsByBooking(int bookingId)
        {
            var payments = await _context.Payments
                .Include(p => p.Booking)
                .Where(p => p.BookingId == bookingId)
                .ToListAsync();

            return payments;
        }

        /// <summary>
        /// Tạo payment mới
        /// </summary>
        /// <param name="payment">Thông tin payment cần tạo</param>
        /// <returns>Payment vừa tạo</returns>
        /// <remarks>
        /// Sample request:
        /// 
        ///     POST /api/payment
        ///     {
        ///         "bookingId": 10,
        ///         "paymentAmount": 500000.00,
        ///         "paymentMethod": "Credit Card",
        ///         "transactionId": "TXN_123456789"
        ///     }
        ///     
        /// **Lưu ý:**
        /// - paymentDate sẽ tự động set = DateTime.Now nếu không truyền
        /// - paymentStatus sẽ tự động set = "Pending" nếu không truyền  
        /// - bookingId và paymentAmount là bắt buộc
        /// - paymentAmount phải > 0
        /// </remarks>
        /// <response code="201">Payment được tạo thành công</response>
        /// <response code="400">Dữ liệu đầu vào không hợp lệ</response>
        // POST: api/Payment
        [HttpPost]
        [ProducesResponseType(201)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<Payment>> CreatePayment(Payment payment)
        {
            // Validate required fields
            if (payment.BookingId <= 0)
            {
                return BadRequest("BookingId is required");
            }

            if (payment.PaymentAmount <= 0)
            {
                return BadRequest("PaymentAmount must be greater than 0");
            }

            // Check if booking exists
            var bookingExists = await _context.Bookings
                .AnyAsync(b => b.BookingId == payment.BookingId);

            if (!bookingExists)
            {
                return BadRequest("Booking not found");
            }

            // Set default values if not provided
            payment.PaymentDate ??= DateTime.Now;
            payment.PaymentStatus ??= "Pending";

            _context.Payments.Add(payment);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return Conflict("Error creating payment");
            }

            return CreatedAtAction(
                nameof(GetPayment),
                new { id = payment.PaymentId },
                payment);
        }

        /// <summary>
        /// Cập nhật thông tin payment
        /// </summary>
        /// <param name="id">ID của payment cần cập nhật</param>
        /// <param name="payment">Thông tin payment mới (phải bao gồm paymentId)</param>
        /// <returns>Không trả về nội dung</returns>
        /// <response code="204">Cập nhật thành công</response>
        /// <response code="400">Dữ liệu đầu vào không hợp lệ hoặc ID không khớp</response>
        /// <response code="404">Không tìm thấy payment</response>
        // PUT: api/Payment/5
        [HttpPut("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> UpdatePayment(int id, Payment payment)
        {
            if (id != payment.PaymentId)
            {
                return BadRequest("Payment ID mismatch");
            }

            // Validate required fields
            if (payment.BookingId <= 0)
            {
                return BadRequest("BookingId is required");
            }

            if (payment.PaymentAmount <= 0)
            {
                return BadRequest("PaymentAmount must be greater than 0");
            }

            // Check if booking exists
            var bookingExists = await _context.Bookings
                .AnyAsync(b => b.BookingId == payment.BookingId);

            if (!bookingExists)
            {
                return BadRequest("Booking not found");
            }

            _context.Entry(payment).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PaymentExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        /// <summary>
        /// Cập nhật trạng thái payment
        /// </summary>
        /// <param name="id">ID của payment</param>
        /// <param name="status">Trạng thái mới</param>
        /// <returns>Không trả về nội dung</returns>
        /// <remarks>
        /// **Các trạng thái hợp lệ:**
        /// - Pending: Đang chờ xử lý
        /// - Completed: Đã hoàn thành  
        /// - Failed: Thất bại
        /// - Cancelled: Đã hủy
        /// - Refunded: Đã hoàn tiền
        /// 
        /// Sample request:
        /// 
        ///     PATCH /api/payment/1/status
        ///     "Completed"
        ///     
        /// </remarks>
        /// <response code="204">Cập nhật trạng thái thành công</response>
        /// <response code="400">Trạng thái không hợp lệ</response>
        /// <response code="404">Không tìm thấy payment</response>
        // PATCH: api/Payment/5/status
        [HttpPatch("{id}/status")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> UpdatePaymentStatus(int id, [FromBody] string status)
        {
            var payment = await _context.Payments.FindAsync(id);

            if (payment == null)
            {
                return NotFound();
            }

            // Validate status values
            var validStatuses = new[] { "Pending", "Completed", "Failed", "Cancelled", "Refunded" };
            if (!validStatuses.Contains(status))
            {
                return BadRequest("Invalid payment status");
            }

            payment.PaymentStatus = status;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return Conflict("Error updating payment status");
            }

            return NoContent();
        }

        /// <summary>
        /// Xóa payment
        /// </summary>
        /// <param name="id">ID của payment cần xóa</param>
        /// <returns>Không trả về nội dung</returns>
        /// <response code="204">Xóa thành công</response>
        /// <response code="404">Không tìm thấy payment</response>
        // DELETE: api/Payment/5
        [HttpDelete("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeletePayment(int id)
        {
            var payment = await _context.Payments.FindAsync(id);

            if (payment == null)
            {
                return NotFound();
            }

            _context.Payments.Remove(payment);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        /// <summary>
        /// Lấy thống kê tổng quan về payment
        /// </summary>
        /// <returns>Thống kê tổng số payment, tổng tiền và phân bổ theo trạng thái</returns>
        /// <remarks>
        /// **Response example:**
        /// 
        ///     {
        ///         "totalPayments": 150,
        ///         "totalAmount": 50000000.00,
        ///         "statusBreakdown": [
        ///             {
        ///                 "status": "Completed",
        ///                 "count": 120,
        ///                 "totalAmount": 45000000.00
        ///             }
        ///         ]
        ///     }
        ///     
        /// **Dùng cho:** Dashboard, báo cáo tổng quan
        /// </remarks>
        /// <response code="200">Thống kê payment</response>
        // GET: api/Payment/statistics
        [HttpGet("statistics")]
        [ProducesResponseType(200)]
        public async Task<ActionResult<object>> GetPaymentStatistics()
        {
            var stats = await _context.Payments
                .GroupBy(p => p.PaymentStatus)
                .Select(g => new
                {
                    Status = g.Key,
                    Count = g.Count(),
                    TotalAmount = g.Sum(p => p.PaymentAmount)
                })
                .ToListAsync();

            var totalPayments = await _context.Payments.CountAsync();
            var totalAmount = await _context.Payments.SumAsync(p => p.PaymentAmount);

            return new
            {
                TotalPayments = totalPayments,
                TotalAmount = totalAmount,
                StatusBreakdown = stats
            };
        }

        /// <summary>
        /// Lấy thống kê payment theo phương thức thanh toán
        /// </summary>
        /// <returns>Thống kê số lượng và tổng tiền theo từng phương thức thanh toán</returns>
        /// <remarks>
        /// **Dùng cho:** Biểu đồ phân tích phương thức thanh toán phổ biến
        /// 
        /// **Response example:**
        /// 
        ///     [
        ///         {
        ///             "paymentMethod": "Credit Card",
        ///             "count": 80,
        ///             "totalAmount": 30000000.00
        ///         },
        ///         {
        ///             "paymentMethod": "Bank Transfer", 
        ///             "count": 40,
        ///             "totalAmount": 15000000.00
        ///         }
        ///     ]
        ///     
        /// </remarks>
        /// <response code="200">Thống kê theo phương thức thanh toán</response>
        // GET: api/Payment/by-method
        [HttpGet("by-method")]
        [ProducesResponseType(200)]
        public async Task<ActionResult<IEnumerable<object>>> GetPaymentsByMethod()
        {
            var result = await _context.Payments
                .GroupBy(p => p.PaymentMethod)
                .Select(g => new
                {
                    PaymentMethod = g.Key,
                    Count = g.Count(),
                    TotalAmount = g.Sum(p => p.PaymentAmount)
                })
                .ToListAsync();

            return Ok(result);
        }

        /// <summary>
        /// Lấy danh sách payment gần đây
        /// </summary>
        /// <param name="count">Số lượng payment muốn lấy (mặc định = 10)</param>
        /// <returns>Danh sách payment được sắp xếp theo thời gian mới nhất</returns>
        /// <remarks>
        /// **Dùng cho:** Dashboard hiển thị activity gần đây, notifications
        /// 
        /// **Example:** `/api/payment/recent?count=5` để lấy 5 payment gần nhất
        /// </remarks>
        /// <response code="200">Danh sách payment gần đây</response>
        // GET: api/Payment/recent
        [HttpGet("recent")]
        [ProducesResponseType(200)]
        public async Task<ActionResult<IEnumerable<Payment>>> GetRecentPayments(int count = 10)
        {
            var recentPayments = await _context.Payments
                .Include(p => p.Booking)
                .OrderByDescending(p => p.PaymentDate)
                .Take(count)
                .ToListAsync();

            return recentPayments;
        }

        private bool PaymentExists(int id)
        {
            return _context.Payments.Any(e => e.PaymentId == id);
        }
    }
}