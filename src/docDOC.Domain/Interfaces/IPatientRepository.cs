using docDOC.Domain.Entities;

namespace docDOC.Domain.Interfaces;

public interface IPatientRepository : IRepository<Patient>
{
    Task<Patient?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
}
