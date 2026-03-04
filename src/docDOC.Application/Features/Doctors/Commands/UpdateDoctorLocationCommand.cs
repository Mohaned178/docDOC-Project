using docDOC.Application.Interfaces;
using docDOC.Domain.Exceptions;
using docDOC.Domain.Interfaces;
using MediatR;
using NetTopologySuite.Geometries;
using System.Threading;
using System.Threading.Tasks;

namespace docDOC.Application.Features.Doctors.Commands;

public sealed record UpdateDoctorLocationCommand(double Longitude, double Latitude, bool IsOnline) : IRequest<UpdateDoctorLocationResponse>;

public sealed record UpdateDoctorLocationResponse(string Message);

public sealed class UpdateDoctorLocationCommandHandler : IRequestHandler<UpdateDoctorLocationCommand, UpdateDoctorLocationResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRedisService _redisService;
    private readonly ICurrentUserService _currentUserService;

    public UpdateDoctorLocationCommandHandler(
        IUnitOfWork unitOfWork,
        IRedisService redisService,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _redisService = redisService;
        _currentUserService = currentUserService;
    }

    public async Task<UpdateDoctorLocationResponse> Handle(UpdateDoctorLocationCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        var doctor = await _unitOfWork.Doctors.GetByIdAsync(userId, cancellationToken);
        if (doctor == null)
            throw new NotFoundException("Doctor not found.");

        doctor.Location = new Point(request.Longitude, request.Latitude) { SRID = 4326 };
        doctor.IsOnline = request.IsOnline;

        _unitOfWork.Doctors.Update(doctor);

        if (request.IsOnline)
        {
            await _redisService.GeoAddAsync("doctors:geo", request.Longitude, request.Latitude, doctor.Id.ToString());
        }
        else
        {
            await _redisService.GeoRemoveAsync("doctors:geo", doctor.Id.ToString());
        }

        return new UpdateDoctorLocationResponse("Location updated successfully.");
    }
}
