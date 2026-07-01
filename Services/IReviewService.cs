using EcommerceApi.Dal;
using EcommerceApi.Dal.Entities;
using EcommerceApi.DTOs.Reviews;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EcommerceApi.Services
{
    public interface IReviewService
    {
        Task<IEnumerable<ReviewResponseDto>> GetByProductIdAsync(int productId);
        Task<ReviewSummaryDto> GetSummaryByProductIdAsync(int productId);
        Task<ReviewResponseDto> CreateAsync(int userId, CreateReviewDto dto);
    }

    public class ReviewService : IReviewService
    {
        private readonly EcomDbContext _db;

        public ReviewService(EcomDbContext db)
        {
            _db = db;
        }

        public async Task<IEnumerable<ReviewResponseDto>> GetByProductIdAsync(int productId)
        {
            var reviews = await _db.Reviews
                .Include(r => r.User)
                .Where(r => r.ProductId == productId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return reviews.Select(r => new ReviewResponseDto
            {
                ReviewId = r.ReviewId,
                UserId = r.UserId,
                UserName = $"{r.User.FirstName} {r.User.LastName}",
                Rating = r.Rating,
                Comment = r.Comment,
                CreatedAt = r.CreatedAt
            });
        }

        public async Task<ReviewSummaryDto> GetSummaryByProductIdAsync(int productId)
        {
            var reviews = await _db.Reviews
                .Where(r => r.ProductId == productId)
                .ToListAsync();

            var totalReviews = reviews.Count;
            var averageRating = totalReviews > 0 ? Math.Round((decimal)reviews.Average(r => r.Rating), 1) : 0;

            var breakdown = new List<ReviewBreakdownDto>();
            for (int stars = 5; stars >= 1; stars--)
            {
                var count = reviews.Count(r => r.Rating == stars);
                var percent = totalReviews > 0 ? Math.Round((double)count / totalReviews * 100, 1) : 0;
                breakdown.Add(new ReviewBreakdownDto
                {
                    Stars = stars,
                    Count = count,
                    Percent = percent
                });
            }

            return new ReviewSummaryDto
            {
                AverageRating = averageRating,
                TotalReviews = totalReviews,
                Breakdown = breakdown
            };
        }

        public async Task<ReviewResponseDto> CreateAsync(int userId, CreateReviewDto dto)
        {
            // Verify product exists
            var productExists = await _db.Products.AnyAsync(p => p.ProductId == dto.ProductId);
            if (!productExists)
                throw new InvalidOperationException("Product not found.");

            // Check if user already reviewed
            var alreadyReviewed = await _db.Reviews.AnyAsync(r => r.UserId == userId && r.ProductId == dto.ProductId);
            if (alreadyReviewed)
                throw new InvalidOperationException("You have already reviewed this product.");

            var user = await _db.Users.FindAsync(userId);
            if (user == null)
                throw new InvalidOperationException("User not found.");

            var review = new Review
            {
                UserId = userId,
                ProductId = dto.ProductId,
                Rating = dto.Rating,
                Comment = dto.Comment,
                CreatedAt = DateTime.Now
            };

            _db.Reviews.Add(review);
            await _db.SaveChangesAsync();

            return new ReviewResponseDto
            {
                ReviewId = review.ReviewId,
                UserId = review.UserId,
                UserName = $"{user.FirstName} {user.LastName}",
                Rating = review.Rating,
                Comment = review.Comment,
                CreatedAt = review.CreatedAt
            };
        }
    }
}
