using docDOC.Domain.Entities;
using docDOC.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace docDOC.Infrastructure.Persistence.Repositories;

public class SpecialityRepository : BaseRepository<Speciality>, ISpecialityRepository
{
    public SpecialityRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Speciality>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.ToListAsync(cancellationToken);
    }
}
