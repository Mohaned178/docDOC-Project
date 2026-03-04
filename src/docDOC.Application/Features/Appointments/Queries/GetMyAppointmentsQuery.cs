using docDOC.Application.Interfaces;
using docDOC.Domain.Enums;
using docDOC.Domain.Exceptions;
using docDOC.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace docDOC.Application.Features.Appointments.Queries;

public sealed record GetMyAppointmentsQuery(
    AppointmentStatus? Status = null,
    int Page = 1,
    int PageSize = 20) : IRequest<PaginatedAppointmentsResponse>;

public sealed class GetMyAppointmentsQueryHandler : IRequestHandler<GetMyAppointmentsQuery, PaginatedAppointmentsResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<GetMyAppointmentsQueryHandler> _logger;

    public GetMyAppointmentsQueryHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        ILogger<GetMyAppointmentsQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<PaginatedAppointmentsResponse> Handle(GetMyAppointmentsQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId == 0) throw new ForbiddenException("Not authenticated");
        var userType = _currentUserService.UserType;

var pageSize = Math.Min(request.PageSize, 50);
        var page = Math.Max(request.Page, 1);

        _logger.LogInformation("GetMyAppointmentsQuery: user {UserId} ({UserType}), status={Status}, page={Page}, pageSize={PageSize}",
            userId, userType, request.Status, page, pageSize);

        var (appointments, totalCount) = await _unitOfWork.Appointments.GetPagedForUserAsync(
            userId, userType, request.Status, page, pageSize, cancellationToken);

        var items = appointments.Select(a => new AppointmentDto(
            a.Id, a.PatientId, a.DoctorId, a.Date, a.Time, a.Type, a.Status));

        return new PaginatedAppointmentsResponse(items, totalCount, page, pageSize);
    }
}

public sealed record AppointmentDto(
    int Id, int PatientId, int DoctorId,
    DateOnly Date, TimeOnly Time,
    AppointmentType Type, AppointmentStatus Status);

public sealed record PaginatedAppointmentsResponse(
    IEnumerable<AppointmentDto> Items,
    int TotalCount,
    int Page,
    int PageSize);
