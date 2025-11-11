namespace API_NoSQL.Dtos
{
    public class CreateReviewDto
    {
        public string BookCode { get; set; } = default!;
        public string OrderCode { get; set; } = default!;
        public int Rating { get; set; }
        public string Content { get; set; } = default!;
    }

    public class UpdateReviewDto
    {
        public int? Rating { get; set; }
        public string? Content { get; set; }
    }

    public class ReviewResponseDto
    {
        public string ReviewId { get; set; } = default!;
        public string CustomerCode { get; set; } = default!;
        public string ReviewerName { get; set; } = default!;
        public string? AvatarUrl { get; set; }
        public string OrderCode { get; set; } = default!;
        public int Rating { get; set; }
        public string Content { get; set; } = default!;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool CanEdit { get; set; }
    }
}