using docDOC.Application.Interfaces;
using docDOC.Domain.Entities;
using docDOC.Domain.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace docDOC.Application.Features.Doctors.Queries;

public sealed record GetNearbyDoctorsQuery(double Latitude, double Longitude, double RadiusKm, int? SpecialityId) : IRequest<GetNearbyDoctorsResponse>;

public sealed record DoctorDto(
    int Id,
    string FirstName,
    string LastName,
    int SpecialityId,
    decimal AverageRating,
    int TotalReviews,
    bool IsOnline,
    double? Longitude,
    double? Latitude);

public sealed record GetNearbyDoctorsResponse(IEnumerable<DoctorDto> Doctors);

public sealed class GetNearbyDoctorsQueryHandler : IRequestHandler<GetNearbyDoctorsQuery, GetNearbyDoctorsResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRedisService _redisService;

    public GetNearbyDoctorsQueryHandler(IUnitOfWork unitOfWork, IRedisService redisService)
    {
        _unitOfWork = unitOfWork;
        _redisService = redisService;
    }

    public async Task<GetNearbyDoctorsResponse> Handle(GetNearbyDoctorsQuery request, CancellationToken cancellationToken)
    {
   
        var geoResults = await _redisService.GeoSearchAsync("doctors:geo", request.Longitude, request.Latitude, request.RadiusKm, 20);

        if (geoResults.Length > 0)
        {
            var doctors = new List<DoctorDto>();

            foreach (var docIdStr in geoResults)
            {
                if (int.TryParse(docIdStr, out var doctorId))
                {
                    var docDto = await GetDoctorDetailsCachedAsync(doctorId, cancellationToken);
                    if (docDto != null && (!request.SpecialityId.HasValue || docDto.SpecialityId == request.SpecialityId.Value))
                    {
                        doctors.Add(docDto);
                    }
                }
            }

            if (doctors.Any())
            {
                return new GetNearbyDoctorsResponse(doctors);
            }
        }

var cacheKey = $"nearby:cache:{request.Latitude}:{request.Longitude}:{request.RadiusKm}";
        var cachedNearby = await _redisService.GetAsync(cacheKey);

        List<DoctorDto> allNearbyDtos;

        if (!string.IsNullOrEmpty(cachedNearby))
        {
            allNearbyDtos = JsonSerializer.Deserialize<List<DoctorDto>>(cachedNearby) ?? new List<DoctorDto>();
        }
        else
        {
            var dbDoctors = await _unitOfWork.Doctors.GetNearbyAsync(request.Latitude, request.Longitude, request.RadiusKm, null, cancellationToken);
            
            allNearbyDtos = dbDoctors.Select(d => new DoctorDto(
                d.Id,
                d.FirstName,
                d.LastName,
                d.SpecialityId,
                d.AverageRating,
                d.TotalReviews,
                d.IsOnline,
                d.Location?.X,
                d.Location?.Y
            )).ToList();

            await _redisService.SetAsync(cacheKey, JsonSerializer.Serialize(allNearbyDtos), TimeSpan.FromMinutes(2));
        }

        var resultDto = allNearbyDtos
            .Where(d => !request.SpecialityId.HasValue || d.SpecialityId == request.SpecialityId.Value)
            .ToList();

        return new GetNearbyDoctorsResponse(resultDto);
    }

    private async Task<DoctorDto?> GetDoctorDetailsCachedAsync(int doctorId, CancellationToken cancellationToken)
    {
        var cacheKey = $"doctor:cache:{doctorId}";
        var cachedDoc = await _redisService.GetAsync(cacheKey);

        if (!string.IsNullOrEmpty(cachedDoc))
        {
            return JsonSerializer.Deserialize<DoctorDto>(cachedDoc);
        }

        var doctor = await _unitOfWork.Doctors.GetByIdAsync(doctorId, cancellationToken);
        if (doctor == null) return null;

        var dto = new DoctorDto(
            doctor.Id,
            doctor.FirstName,
            doctor.LastName,
            doctor.SpecialityId,
            doctor.AverageRating,
            doctor.TotalReviews,
            doctor.IsOnline,
            doctor.Location?.X,
            doctor.Location?.Y
        );

        await _redisService.SetAsync(cacheKey, JsonSerializer.Serialize(dto), TimeSpan.FromMinutes(10));
        return dto;
    }
}
