using BookingTour.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

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
        /// <summary>
        /// Lấy tất cả tour
        /// </summary>
        [HttpGet("GetAllTours")]
        public IActionResult GetAllTours()
        {
            var tours = _context.Tours.ToList();
            return Ok(tours);
        }

        // GET: api/tour/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<Tour>> GetTour(int id)
        {
            try
            {
                var tour = await _context.Tours
                    .Include(t => t.TourImages)
                    .Include(t => t.TourConditions)
                    .Include(t => t.Bookings)
                    .FirstOrDefaultAsync(t => t.TourId == id);

                if (tour == null)
                {
                    return NotFound(new { Message = "Không tìm thấy tour" });
                }

                return Ok(tour);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Đã xảy ra lỗi khi lấy thông tin tour", Error = ex.Message });
            }
        }

        // POST: api/tour
        [HttpPost("Create")]
        public async Task<ActionResult<Tour>> CreateTour([FromBody] CreateTourRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var tour = new Tour
                {
                    TourName = request.TourName,
                    Destination = request.Destination,
                    Description = request.Description,
                    Duration = request.Duration,
                    Price = request.Price,
                    MaxParticipants = request.MaxParticipants,
                    StartDate = request.StartDate,
                    EndDate = request.EndDate,
                    Status = request.Status ?? "Active",
                    CreatedDate = DateTime.Now,
                    Transport = request.Transport,
                    Thumbnail = request.Thumbnail
                };

                _context.Tours.Add(tour);
                await _context.SaveChangesAsync();

                // Add tour conditions if provided
                if (request.TourConditions != null && request.TourConditions.Any())
                {
                    foreach (var condition in request.TourConditions)
                    {
                        _context.TourConditions.Add(new TourCondition
                        {
                            TourId = tour.TourId,
                            Title = condition.Title,
                            Content = condition.Content,
                            CreatedDate = DateTime.Now
                        });
                    }
                }

                // Add tour images if provided
                if (request.TourImages != null && request.TourImages.Any())
                {
                    foreach (var image in request.TourImages)
                    {
                        _context.TourImages.Add(new TourImage
                        {
                            TourId = tour.TourId,
                            ImageUrl = image.ImageUrl,
                            Caption = image.Caption,
                            CreatedDate = DateTime.Now
                        });
                    }
                }

                await _context.SaveChangesAsync();

                // Return created tour with related data
                var createdTour = await _context.Tours
                    .Include(t => t.TourImages)
                    .Include(t => t.TourConditions)
                    .FirstOrDefaultAsync(t => t.TourId == tour.TourId);

                return CreatedAtAction(nameof(GetTour), new { id = tour.TourId }, createdTour);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Đã xảy ra lỗi khi tạo tour", Error = ex.Message });
            }
        }

        // PUT: api/tour/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTour(int id, [FromBody] UpdateTourRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var tour = await _context.Tours
                    .Include(t => t.TourConditions)
                    .Include(t => t.TourImages)
                    .FirstOrDefaultAsync(t => t.TourId == id);

                if (tour == null)
                {
                    return NotFound(new { Message = "Không tìm thấy tour" });
                }

                // Update tour properties
                tour.TourName = request.TourName ?? tour.TourName;
                tour.Destination = request.Destination ?? tour.Destination;
                tour.Description = request.Description ?? tour.Description;
                tour.Duration = request.Duration ?? tour.Duration;
                tour.Price = request.Price ?? tour.Price;
                tour.MaxParticipants = request.MaxParticipants ?? tour.MaxParticipants;
                tour.StartDate = request.StartDate ?? tour.StartDate;
                tour.EndDate = request.EndDate ?? tour.EndDate;
                tour.Status = request.Status ?? tour.Status;
                tour.Transport = request.Transport ?? tour.Transport;
                tour.Thumbnail = request.Thumbnail ?? tour.Thumbnail;

                // Update tour conditions if provided
                if (request.TourConditions != null)
                {
                    _context.TourConditions.RemoveRange(tour.TourConditions);
                    foreach (var condition in request.TourConditions)
                    {
                        _context.TourConditions.Add(new TourCondition
                        {
                            TourId = tour.TourId,
                            Title = condition.Title,
                            Content = condition.Content,
                            CreatedDate = DateTime.Now
                        });
                    }
                }

                // Update tour images if provided
                if (request.TourImages != null)
                {
                    _context.TourImages.RemoveRange(tour.TourImages);
                    foreach (var image in request.TourImages)
                    {
                        _context.TourImages.Add(new TourImage
                        {
                            TourId = tour.TourId,
                            ImageUrl = image.ImageUrl,
                            Caption = image.Caption,
                            CreatedDate = DateTime.Now
                        });
                    }
                }

                await _context.SaveChangesAsync();

                return Ok(new { Message = "Cập nhật tour thành công" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Đã xảy ra lỗi khi cập nhật tour", Error = ex.Message });
            }
        }

        // DELETE: api/tour/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTour(int id)
        {
            try
            {
                var tour = await _context.Tours
                    .Include(t => t.Bookings)
                    .FirstOrDefaultAsync(t => t.TourId == id);

                if (tour == null)
                {
                    return NotFound(new { Message = "Không tìm thấy tour" });
                }

                // Check if tour has bookings
                if (tour.Bookings.Any())
                {
                    return BadRequest(new { Message = "Không thể xóa tour đã có booking" });
                }

                _context.Tours.Remove(tour);
                await _context.SaveChangesAsync();

                return Ok(new { Message = "Xóa tour thành công" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Đã xảy ra lỗi khi xóa tour", Error = ex.Message });
            }
        }

        // GET: api/tour/search
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<Tour>>> SearchTours([FromQuery] string keyword)
        {
            try
            {
                if (string.IsNullOrEmpty(keyword))
                {
                    return BadRequest(new { Message = "Từ khóa tìm kiếm không được để trống" });
                }

                var tours = await _context.Tours
                    .Include(t => t.TourImages)
                    .Include(t => t.TourConditions)
                    .Where(t => t.TourName.Contains(keyword) ||
                               t.Destination.Contains(keyword) ||
                               (t.Description != null && t.Description.Contains(keyword)))
                    .OrderByDescending(t => t.CreatedDate)
                    .ToListAsync();

                return Ok(tours);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Đã xảy ra lỗi khi tìm kiếm tour", Error = ex.Message });
            }
        }

        // GET: api/tour/available
        [HttpGet("available")]
        public async Task<ActionResult<IEnumerable<Tour>>> GetAvailableTours([FromQuery] DateTime? date = null)
        {
            try
            {
                var searchDate = date ?? DateTime.Now;

                var tours = await _context.Tours
                    .Include(t => t.TourImages)
                    .Include(t => t.TourConditions)
                    .Where(t => t.Status == "Active" &&
                               t.StartDate >= searchDate)
                    .OrderBy(t => t.StartDate)
                    .ToListAsync();

                return Ok(tours);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Đã xảy ra lỗi khi lấy danh sách tour khả dụng", Error = ex.Message });
            }
        }
    }

    // Request DTOs
    public class CreateTourRequest
    {
        [Required(ErrorMessage = "Tên tour là bắt buộc")]
        public string TourName { get; set; } = null!;

        [Required(ErrorMessage = "Điểm đến là bắt buộc")]
        public string Destination { get; set; } = null!;

        public string? Description { get; set; }

        [Required(ErrorMessage = "Thời gian tour là bắt buộc")]
        [Range(1, int.MaxValue, ErrorMessage = "Thời gian tour phải lớn hơn 0")]
        public int Duration { get; set; }

        [Required(ErrorMessage = "Giá tour là bắt buộc")]
        [Range(0, double.MaxValue, ErrorMessage = "Giá tour phải lớn hơn hoặc bằng 0")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Số lượng tham gia tối đa là bắt buộc")]
        [Range(1, int.MaxValue, ErrorMessage = "Số lượng tham gia tối đa phải lớn hơn 0")]
        public int MaxParticipants { get; set; }

        [Required(ErrorMessage = "Ngày bắt đầu là bắt buộc")]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "Ngày kết thúc là bắt buộc")]
        public DateTime EndDate { get; set; }

        public string? Status { get; set; }
        public string? Transport { get; set; }
        public string? Thumbnail { get; set; }

        public List<CreateTourConditionRequest>? TourConditions { get; set; }
        public List<CreateTourImageRequest>? TourImages { get; set; }
    }

    public class UpdateTourRequest
    {
        public string? TourName { get; set; }
        public string? Destination { get; set; }
        public string? Description { get; set; }
        public int? Duration { get; set; }
        public decimal? Price { get; set; }
        public int? MaxParticipants { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Status { get; set; }
        public string? Transport { get; set; }
        public string? Thumbnail { get; set; }

        public List<CreateTourConditionRequest>? TourConditions { get; set; }
        public List<CreateTourImageRequest>? TourImages { get; set; }
    }

    public class CreateTourConditionRequest
    {
        [Required(ErrorMessage = "Tiêu đề điều kiện là bắt buộc")]
        public string Title { get; set; } = null!;

        [Required(ErrorMessage = "Nội dung điều kiện là bắt buộc")]
        public string Content { get; set; } = null!;
    }

    public class CreateTourImageRequest
    {
        [Required(ErrorMessage = "URL hình ảnh là bắt buộc")]
        public string ImageUrl { get; set; } = null!;

        public string? Caption { get; set; }
    }
}
