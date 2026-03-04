using docDOC.Application.Interfaces;
using docDOC.Domain.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace docDOC.Application.Features.Specialities.Queries;

public sealed record GetSpecialitiesQuery() : IRequest<GetSpecialitiesResponse>;

public sealed record SpecialityDto(int Id, string Name, string? IconCode);

public sealed record GetSpecialitiesResponse(IEnumerable<SpecialityDto> Specialities);

public sealed class GetSpecialitiesQueryHandler : IRequestHandler<GetSpecialitiesQuery, GetSpecialitiesResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRedisService _redisService;

    public GetSpecialitiesQueryHandler(IUnitOfWork unitOfWork, IRedisService redisService)
    {
        _unitOfWork = unitOfWork;
        _redisService = redisService;
    }

    public async Task<GetSpecialitiesResponse> Handle(GetSpecialitiesQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = "specialities:cache";
        var cached = await _redisService.GetAsync(cacheKey);

        if (!string.IsNullOrEmpty(cached))
        {
            var dtos = JsonSerializer.Deserialize<List<SpecialityDto>>(cached) ?? new List<SpecialityDto>();
            return new GetSpecialitiesResponse(dtos);
        }

        var specialities = await _unitOfWork.Specialities.GetAllAsync(cancellationToken);
        
        var dtosList = specialities.Select(s => new SpecialityDto(s.Id, s.Name, s.IconCode)).ToList();

        await _redisService.SetAsync(cacheKey, JsonSerializer.Serialize(dtosList), TimeSpan.FromHours(1));

        return new GetSpecialitiesResponse(dtosList);
    }
}
