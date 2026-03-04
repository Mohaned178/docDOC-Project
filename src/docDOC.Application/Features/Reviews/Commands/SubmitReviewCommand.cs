using docDOC.Domain.Entities;
using docDOC.Domain.Enums;
using docDOC.Domain.Exceptions;
using docDOC.Domain.Interfaces;
using docDOC.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace docDOC.Application.Features.Reviews.Commands;

public sealed record SubmitReviewCommand(
    int AppointmentId,
    int Rating,
    string? Comment) : IRequest<SubmitReviewResponse>;

public sealed class SubmitReviewCommandHandler : IRequestHandler<SubmitReviewCommand, SubmitReviewResponse>
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUserService;
    private readonly IRedisService _redisService;
    private readonly ILogger<SubmitReviewCommandHandler> _logger;

    public SubmitReviewCommandHandler(
        IUnitOfWork uow,
        ICurrentUserService currentUserService,
        IRedisService redisService,
        ILogger<SubmitReviewCommandHandler> logger)
    {
        _uow = uow;
        _currentUserService = currentUserService;
        _redisService = redisService;
        _logger = logger;
    }

    public async Task<SubmitReviewResponse> Handle(SubmitReviewCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Submitting review for appointment {AppointmentId}", request.AppointmentId);

        var appointment = await _uow.Appointments.GetByIdAsync(request.AppointmentId, cancellationToken);
        if (appointment == null || appointment.Status != AppointmentStatus.Completed)
        {
            _logger.LogWarning("Review attempt for non-completed or non-existent appointment {AppointmentId}", request.AppointmentId);
            throw new DomainException("Review can only be submitted for completed appointments.");
        }

        var currentUserId = _currentUserService.UserId;
        if (appointment.PatientId != currentUserId)
        {
            _logger.LogWarning("User {UserId} attempted to review appointment {AppointmentId} belonging to patient {PatientId}", 
                currentUserId, request.AppointmentId, appointment.PatientId);
            throw new ForbiddenException("You can only review your own appointments.");
        }

        if (await _uow.Reviews.ExistsForAppointmentAsync(request.AppointmentId, cancellationToken))
        {
            _logger.LogWarning("Duplicate review attempt for appointment {AppointmentId}", request.AppointmentId);
            throw new ConflictException("A review already exists for this appointment.");
        }

        var doctor = await _uow.Doctors.GetByIdAsync(appointment.DoctorId, cancellationToken);
        if (doctor == null)
        {
            throw new NotFoundException($"Doctor with id {appointment.DoctorId} not found");
        }

        var review = new Review
        {
            AppointmentId = request.AppointmentId,
            Rating = request.Rating,
            Comment = request.Comment,
            PatientId = appointment.PatientId,
            DoctorId = appointment.DoctorId
        };

decimal currentAvg = doctor.AverageRating;
        int currentCount = doctor.TotalReviews;
        
        doctor.AverageRating = ((currentAvg * (decimal)currentCount) + (decimal)request.Rating) / (decimal)(currentCount + 1);
        doctor.TotalReviews++;

        await _uow.Reviews.AddAsync(review, cancellationToken);
        _uow.Doctors.Update(doctor);

await _redisService.RemoveAsync($"doctor:cache:{doctor.Id}");

        return new SubmitReviewResponse(
            review.Id, review.AppointmentId, review.DoctorId,
            review.Rating, review.Comment, review.CreatedAt);
    }
}

public sealed record SubmitReviewResponse(
    int Id, int AppointmentId, int DoctorId,
    int Rating, string? Comment, DateTimeOffset CreatedAt);
