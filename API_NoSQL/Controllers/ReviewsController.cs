using API_NoSQL.Dtos;
using API_NoSQL.Services;
using Microsoft.AspNetCore.Mvc;

namespace API_NoSQL.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReviewsController : ControllerBase
    {
        private readonly ReviewService _reviews;

        public ReviewsController(ReviewService reviews)
        {
            _reviews = reviews;
        }

        // POST /api/Reviews
        /// <summary>
        /// Tạo đánh giá mới (yêu cầu X-Customer-Code header)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateReview([FromBody] CreateReviewDto dto)
        {
            var customerCode = Request.Headers["X-Customer-Code"].ToString();
            if (string.IsNullOrEmpty(customerCode))
                return BadRequest(new { error = "X-Customer-Code header is required" });

            var (ok, error, review) = await _reviews.CreateReviewAsync(customerCode, dto);

            if (!ok)
                return BadRequest(new { error });

            return Created($"/api/Reviews/{dto.BookCode}/{review!.ReviewId}", review);
        }

        // PUT /api/Reviews/{bookCode}/{reviewId}
        /// <summary>
        /// Cập nhật đánh giá (chỉ người tạo)
        /// </summary>
        [HttpPut("{bookCode}/{reviewId}")]
        public async Task<IActionResult> UpdateReview(
            string bookCode, 
            string reviewId, 
            [FromBody] UpdateReviewDto dto)
        {
            var customerCode = Request.Headers["X-Customer-Code"].ToString();
            if (string.IsNullOrEmpty(customerCode))
                return BadRequest(new { error = "X-Customer-Code header is required" });

            var (ok, error) = await _reviews.UpdateReviewAsync(customerCode, bookCode, reviewId, dto);

            if (!ok)
                return BadRequest(new { error });

            return NoContent();
        }

        // DELETE /api/Reviews/{bookCode}/{reviewId}
        /// <summary>
        /// Xóa đánh giá (chỉ người tạo)
        /// </summary>
        [HttpDelete("{bookCode}/{reviewId}")]
        public async Task<IActionResult> DeleteReview(string bookCode, string reviewId)
        {
            var customerCode = Request.Headers["X-Customer-Code"].ToString();
            if (string.IsNullOrEmpty(customerCode))
                return BadRequest(new { error = "X-Customer-Code header is required" });

            var (ok, error) = await _reviews.DeleteReviewAsync(customerCode, bookCode, reviewId);

            if (!ok)
                return BadRequest(new { error });

            return NoContent();
        }

        // GET /api/Reviews/{bookCode}
        /// <summary>
        /// Lấy tất cả đánh giá của 1 sách (public, ai cũng xem được)
        /// </summary>
        [HttpGet("{bookCode}")]
        public async Task<IActionResult> GetReviews(string bookCode)
        {
            // Lấy customerCode nếu có (để check canEdit)
            var customerCode = Request.Headers["X-Customer-Code"].ToString();

            var reviews = await _reviews.GetReviewsByBookAsync(bookCode, customerCode);
            return Ok(reviews);
        }

        // GET /api/Reviews/{bookCode}/can-review
        /// <summary>
        /// Kiểm tra customer có thể đánh giá sách này không
        /// </summary>
        [HttpGet("{bookCode}/can-review")]
        public async Task<IActionResult> CanReview(string bookCode)
        {
            var customerCode = Request.Headers["X-Customer-Code"].ToString();
            if (string.IsNullOrEmpty(customerCode))
                return Ok(new { canReview = false });

            var canReview = await _reviews.CanReviewBookAsync(customerCode, bookCode);
            return Ok(new { canReview });
        }
    }
}