using docDOC.Domain.Entities;
using docDOC.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace docDOC.Infrastructure.Persistence.Repositories;

public class PatientRepository : BaseRepository<Patient>, IPatientRepository
{
    public PatientRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Patient?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(p => p.Email == email, cancellationToken);
    }
}
