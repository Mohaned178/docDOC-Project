using docDOC.Domain.Entities;

namespace docDOC.Domain.Interfaces;

public interface ISpecialityRepository : IRepository<Speciality>
{
    Task<IEnumerable<Speciality>> GetAllAsync(CancellationToken cancellationToken = default);
}
