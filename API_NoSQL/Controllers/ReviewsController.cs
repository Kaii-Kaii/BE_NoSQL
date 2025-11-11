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

        // GET /api/Reviews/{bookCode}>
        [HttpGet("{bookCode}")]
        public async Task<IActionResult> GetReviews(string bookCode)
        {
            // Lấy customerCode nếu có (để check canEdit)
            var customerCode = Request.Headers["X-Customer-Code"].ToString();

            var reviews = await _reviews.GetReviewsByBookAsync(bookCode, customerCode);
            return Ok(reviews);
        }

        // GET /api/Reviews/{bookCode}/can-review
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