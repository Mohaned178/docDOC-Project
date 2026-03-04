using docDOC.Domain.Exceptions;
using docDOC.Domain.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace docDOC.Application.Features.Doctors.Queries;

public sealed record GetDoctorAvailabilityQuery(int DoctorId, DateOnly Date) : IRequest<GetDoctorAvailabilityResponse>;

public sealed record GetDoctorAvailabilityResponse(int DoctorId, DateOnly Date, IEnumerable<string> AvailableSlots);

public sealed class GetDoctorAvailabilityQueryHandler : IRequestHandler<GetDoctorAvailabilityQuery, GetDoctorAvailabilityResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetDoctorAvailabilityQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<GetDoctorAvailabilityResponse> Handle(GetDoctorAvailabilityQuery request, CancellationToken cancellationToken)
    {
        if (request.Date.DayOfWeek == DayOfWeek.Sunday)
            throw new DomainException("Booking is not allowed on Sundays.");

        var availableSlots = new List<string>();
        var startTime = new TimeOnly(8, 0);
        var endTime = new TimeOnly(16, 30);

        var currentTime = startTime;
        while (currentTime <= endTime)
        {
            var isTaken = await _unitOfWork.Appointments.IsSlotTakenAsync(request.DoctorId, request.Date, currentTime, cancellationToken);
            if (!isTaken)
            {
                availableSlots.Add(currentTime.ToString("HH:mm"));
            }

            currentTime = currentTime.AddMinutes(30);
        }

        return new GetDoctorAvailabilityResponse(request.DoctorId, request.Date, availableSlots);
    }
}
