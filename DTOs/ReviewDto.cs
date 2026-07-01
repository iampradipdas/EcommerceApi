using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EcommerceApi.DTOs.Reviews
{
    public class CreateReviewDto
    {
        [Required]
        public int ProductId { get; set; }

        [Required]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5.")]
        public int Rating { get; set; }

        [Required]
        [MinLength(5, ErrorMessage = "Comment must be at least 5 characters long.")]
        public string Comment { get; set; } = null!;
    }

    public class ReviewResponseDto
    {
        public int ReviewId { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = null!;
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class ReviewSummaryDto
    {
        public decimal AverageRating { get; set; }
        public int TotalReviews { get; set; }
        public List<ReviewBreakdownDto> Breakdown { get; set; } = new();
    }

    public class ReviewBreakdownDto
    {
        public int Stars { get; set; }
        public int Count { get; set; }
        public double Percent { get; set; }
    }
}
