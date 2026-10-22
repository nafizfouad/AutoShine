namespace AutoShine.Service.DTOs.Reviews;

public record ReviewDto(
    int Id,
    int BookingId,
    int CustomerId,
    string CustomerName,
    int EmployeeId,
    string EmployeeName,
    int Rating,
    string? Comment,
    DateTime CreatedAt
);

public record CreateReviewDto(
    int BookingId,
    int Rating,
    string? Comment
);
