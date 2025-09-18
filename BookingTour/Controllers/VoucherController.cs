using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BookingTour.Models;
using System.ComponentModel.DataAnnotations;

namespace BookingTour.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VoucherController : ControllerBase
    {
        private readonly TourBookingSystemContext _context; 

        public VoucherController(TourBookingSystemContext context)
        {
            _context = context;
        }

        // GET: api/Voucher
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Voucher>>> GetVouchers()
        {
            try
            {
                var vouchers = await _context.Vouchers
                    .Where(v => v.Status != "Deleted")
                    .OrderByDescending(v => v.CreatedDate)
                    .ToListAsync();

                return Ok(vouchers);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi server: " + ex.Message });
            }
        }

        // GET: api/Voucher/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Voucher>> GetVoucher(int id)
        {
            try
            {
                var voucher = await _context.Vouchers
                    .Include(v => v.Bookings)
                    .FirstOrDefaultAsync(v => v.VoucherId == id && v.Status != "Deleted");

                if (voucher == null)
                {
                    return NotFound(new { message = "Không tìm thấy voucher" });
                }

                return Ok(voucher);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi server: " + ex.Message });
            }
        }

        // GET: api/Voucher/code/{code}
        [HttpGet("code/{code}")]
        public async Task<ActionResult<Voucher>> GetVoucherByCode(string code)
        {
            try
            {
                var voucher = await _context.Vouchers
                    .FirstOrDefaultAsync(v => v.VoucherCode == code && v.Status == "Active");

                if (voucher == null)
                {
                    return NotFound(new { message = "Không tìm thấy mã voucher hoặc voucher không còn hiệu lực" });
                }

                // Kiểm tra thời hạn voucher
                if (DateTime.Now < voucher.ValidFrom || DateTime.Now > voucher.ValidTo)
                {
                    return BadRequest(new { message = "Voucher đã hết hạn hoặc chưa có hiệu lực" });
                }

                // Kiểm tra giới hạn sử dụng
                if (voucher.UsageLimit.HasValue && voucher.UsedCount >= voucher.UsageLimit)
                {
                    return BadRequest(new { message = "Voucher đã hết lượt sử dụng" });
                }

                return Ok(voucher);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi server: " + ex.Message });
            }
        }

        // GET: api/Voucher/active
        [HttpGet("active")]
        public async Task<ActionResult<IEnumerable<Voucher>>> GetActiveVouchers()
        {
            try
            {
                var currentDate = DateTime.Now;
                var activeVouchers = await _context.Vouchers
                    .Where(v => v.Status == "Active"
                           && v.ValidFrom <= currentDate
                           && v.ValidTo >= currentDate
                           && (!v.UsageLimit.HasValue || v.UsedCount < v.UsageLimit))
                    .OrderBy(v => v.ValidTo)
                    .ToListAsync();

                return Ok(activeVouchers);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi server: " + ex.Message });
            }
        }

        // POST: api/Voucher
        [HttpPost]
        public async Task<ActionResult<Voucher>> CreateVoucher(Voucher voucher)
        {
            try
            {
                // Validate dữ liệu
                var validationResult = ValidateVoucher(voucher);
                if (validationResult != null)
                {
                    return BadRequest(validationResult);
                }

                // Kiểm tra trùng mã voucher
                var existingVoucher = await _context.Vouchers
                    .FirstOrDefaultAsync(v => v.VoucherCode == voucher.VoucherCode);

                if (existingVoucher != null)
                {
                    return BadRequest(new { message = "Mã voucher đã tồn tại" });
                }

                // Set default values
                voucher.CreatedDate = DateTime.Now;
                voucher.UsedCount = voucher.UsedCount ?? 0;
                voucher.Status = voucher.Status ?? "Active";

                _context.Vouchers.Add(voucher);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetVoucher), new { id = voucher.VoucherId }, voucher);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi server: " + ex.Message });
            }
        }

        // PUT: api/Voucher/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateVoucher(int id, Voucher voucher)
        {
            try
            {
                if (id != voucher.VoucherId)
                {
                    return BadRequest(new { message = "ID không khớp" });
                }

                var existingVoucher = await _context.Vouchers.FindAsync(id);
                if (existingVoucher == null)
                {
                    return NotFound(new { message = "Không tìm thấy voucher" });
                }

                // Validate dữ liệu
                var validationResult = ValidateVoucher(voucher);
                if (validationResult != null)
                {
                    return BadRequest(validationResult);
                }

                // Kiểm tra trùng mã voucher (trừ chính nó)
                var duplicateVoucher = await _context.Vouchers
                    .FirstOrDefaultAsync(v => v.VoucherCode == voucher.VoucherCode && v.VoucherId != id);

                if (duplicateVoucher != null)
                {
                    return BadRequest(new { message = "Mã voucher đã tồn tại" });
                }

                // Cập nhật các thuộc tính
                existingVoucher.VoucherCode = voucher.VoucherCode;
                existingVoucher.VoucherName = voucher.VoucherName;
                existingVoucher.Description = voucher.Description;
                existingVoucher.DiscountType = voucher.DiscountType;
                existingVoucher.DiscountValue = voucher.DiscountValue;
                existingVoucher.MinOrderAmount = voucher.MinOrderAmount;
                existingVoucher.MaxDiscountAmount = voucher.MaxDiscountAmount;
                existingVoucher.UsageLimit = voucher.UsageLimit;
                existingVoucher.UsedCount = voucher.UsedCount;
                existingVoucher.ValidFrom = voucher.ValidFrom;
                existingVoucher.ValidTo = voucher.ValidTo;
                existingVoucher.Status = voucher.Status;

                await _context.SaveChangesAsync();

                return Ok(new { message = "Cập nhật voucher thành công" });
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!VoucherExists(id))
                {
                    return NotFound(new { message = "Không tìm thấy voucher" });
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi server: " + ex.Message });
            }
        }

        // DELETE: api/Voucher/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteVoucher(int id)
        {
            try
            {
                var voucher = await _context.Vouchers.FindAsync(id);
                if (voucher == null)
                {
                    return NotFound(new { message = "Không tìm thấy voucher" });
                }

                // Soft delete
                voucher.Status = "Deleted";
                await _context.SaveChangesAsync();

                return Ok(new { message = "Xóa voucher thành công" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi server: " + ex.Message });
            }
        }

        // POST: api/Voucher/apply
        [HttpPost("apply")]
        public async Task<ActionResult> ApplyVoucher([FromBody] ApplyVoucherRequest request)
        {
            try
            {
                var voucher = await _context.Vouchers
                    .FirstOrDefaultAsync(v => v.VoucherCode == request.VoucherCode && v.Status == "Active");

                if (voucher == null)
                {
                    return NotFound(new { message = "Mã voucher không tồn tại hoặc không còn hiệu lực" });
                }

                // Kiểm tra thời hạn
                var currentDate = DateTime.Now;
                if (currentDate < voucher.ValidFrom || currentDate > voucher.ValidTo)
                {
                    return BadRequest(new { message = "Voucher đã hết hạn hoặc chưa có hiệu lực" });
                }

                // Kiểm tra giới hạn sử dụng
                if (voucher.UsageLimit.HasValue && voucher.UsedCount >= voucher.UsageLimit)
                {
                    return BadRequest(new { message = "Voucher đã hết lượt sử dụng" });
                }

                // Kiểm tra số tiền tối thiểu
                if (voucher.MinOrderAmount.HasValue && request.OrderAmount < voucher.MinOrderAmount)
                {
                    return BadRequest(new
                    {
                        message = $"Đơn hàng phải có giá trị tối thiểu {voucher.MinOrderAmount:N0} VND"
                    });
                }

                // Tính toán giảm giá
                decimal discountAmount = 0;
                if (voucher.DiscountType.ToLower() == "percentage")
                {
                    discountAmount = request.OrderAmount * (voucher.DiscountValue / 100);

                    // Áp dụng giới hạn giảm giá tối đa
                    if (voucher.MaxDiscountAmount.HasValue && discountAmount > voucher.MaxDiscountAmount)
                    {
                        discountAmount = voucher.MaxDiscountAmount.Value;
                    }
                }
                else if (voucher.DiscountType.ToLower() == "fixed")
                {
                    discountAmount = voucher.DiscountValue;

                    // Không thể giảm quá số tiền đơn hàng
                    if (discountAmount > request.OrderAmount)
                    {
                        discountAmount = request.OrderAmount;
                    }
                }

                var finalAmount = request.OrderAmount - discountAmount;

                return Ok(new
                {
                    voucherId = voucher.VoucherId,
                    voucherCode = voucher.VoucherCode,
                    voucherName = voucher.VoucherName,
                    originalAmount = request.OrderAmount,
                    discountAmount = discountAmount,
                    finalAmount = finalAmount,
                    message = "Áp dụng voucher thành công"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi server: " + ex.Message });
            }
        }

        // POST: api/Voucher/{id}/use
        [HttpPost("{id}/use")]
        public async Task<IActionResult> UseVoucher(int id)
        {
            try
            {
                var voucher = await _context.Vouchers.FindAsync(id);
                if (voucher == null)
                {
                    return NotFound(new { message = "Không tìm thấy voucher" });
                }

                voucher.UsedCount = (voucher.UsedCount ?? 0) + 1;
                await _context.SaveChangesAsync();

                return Ok(new { message = "Đã cập nhật lượt sử dụng voucher" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi server: " + ex.Message });
            }
        }

        private bool VoucherExists(int id)
        {
            return _context.Vouchers.Any(e => e.VoucherId == id);
        }

        private object? ValidateVoucher(Voucher voucher)
        {
            if (string.IsNullOrWhiteSpace(voucher.VoucherCode))
                return new { message = "Mã voucher không được để trống" };

            if (string.IsNullOrWhiteSpace(voucher.VoucherName))
                return new { message = "Tên voucher không được để trống" };

            if (string.IsNullOrWhiteSpace(voucher.DiscountType))
                return new { message = "Loại giảm giá không được để trống" };

            if (voucher.DiscountType.ToLower() != "percentage" && voucher.DiscountType.ToLower() != "fixed")
                return new { message = "Loại giảm giá chỉ có thể là 'percentage' hoặc 'fixed'" };

            if (voucher.DiscountValue <= 0)
                return new { message = "Giá trị giảm giá phải lớn hơn 0" };

            if (voucher.DiscountType.ToLower() == "percentage" && voucher.DiscountValue > 100)
                return new { message = "Phần trăm giảm giá không thể lớn hơn 100" };

            if (voucher.ValidFrom >= voucher.ValidTo)
                return new { message = "Ngày bắt đầu phải nhỏ hơn ngày kết thúc" };

            if (voucher.UsageLimit.HasValue && voucher.UsageLimit <= 0)
                return new { message = "Giới hạn sử dụng phải lớn hơn 0" };

            return null;
        }
    }

    public class ApplyVoucherRequest
    {
        [Required]
        public string VoucherCode { get; set; } = null!;

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Số tiền đơn hàng phải lớn hơn 0")]
        public decimal OrderAmount { get; set; }
    }
}