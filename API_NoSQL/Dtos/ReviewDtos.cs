namespace API_NoSQL.Dtos
{
    // DTO tạo đánh giá mới
    public class CreateReviewDto
    {
        public string BookCode { get; set; } = default!;
        public string OrderCode { get; set; } = default!;
        public int Rating { get; set; }
        public string Content { get; set; } = default!;
    }

    // DTO cập nhật đánh giá
    public class UpdateReviewDto
    {
        public int? Rating { get; set; }
        public string? Content { get; set; }
    }

    // DTO response đánh giá (cho frontend)
    public class ReviewResponseDto
    {
        public string ReviewId { get; set; } = default!;
        public string CustomerCode { get; set; } = default!;
        public string ReviewerName { get; set; } = default!;
        public string? AvatarUrl { get; set; }  // ✅ THÊM AVATAR
        public string OrderCode { get; set; } = default!;
        public int Rating { get; set; }
        public string Content { get; set; } = default!;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool CanEdit { get; set; }
    }
}