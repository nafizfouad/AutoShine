using AutoShine.Common;
using AutoShine.Service.DTOs.Reviews;
using AutoShine.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoShine.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReviewsController : ControllerBase
{
    private readonly IReviewService _reviewService;

    public ReviewsController(IReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    private int GetCurrentUserId() => User.GetUserId();

    /// <summary>Get all reviews for an employee.</summary>
    [HttpGet("employee/{employeeId:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<IEnumerable<ReviewDto>>>> GetEmployeeReviews(int employeeId)
    {
        var reviews = await _reviewService.GetReviewsByEmployeeAsync(employeeId);
        return Ok(ApiResponse<IEnumerable<ReviewDto>>.Ok(reviews));
    }

    /// <summary>Get the review for a specific booking.</summary>
    [HttpGet("booking/{bookingId:int}")]
    public async Task<ActionResult<ApiResponse<ReviewDto>>> GetBookingReview(int bookingId)
    {
        var review = await _reviewService.GetReviewByBookingAsync(bookingId);
        if (review == null) return NotFound(ApiResponse<ReviewDto>.Fail("No review found for this booking."));

        var userId = GetCurrentUserId();
        if (!User.IsInRole("Admin") && review.CustomerId != userId && review.EmployeeId != userId)
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<ReviewDto>.Fail("You do not have access to this review."));

        return Ok(ApiResponse<ReviewDto>.Ok(review));
    }

    /// <summary>Submit a review for a completed booking (Customer only).</summary>
    [HttpPost]
    [Authorize(Roles = "Customer")]
    public async Task<ActionResult<ApiResponse<ReviewDto>>> CreateReview([FromBody] CreateReviewDto dto)
    {
        var customerId = GetCurrentUserId();
        var review = await _reviewService.CreateReviewAsync(customerId, dto);
        return Ok(ApiResponse<ReviewDto>.Ok(review, "Review submitted."));
    }

    /// <summary>Delete a review (Admin only).</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteReview(int id)
    {
        var success = await _reviewService.DeleteReviewAsync(id);
        if (!success) return NotFound(ApiResponse<bool>.Fail("Review not found."));
        return Ok(ApiResponse<bool>.Ok(true, "Review deleted."));
    }
}
