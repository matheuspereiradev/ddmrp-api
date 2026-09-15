using Microsoft.EntityFrameworkCore;
using Service.Domain.Account;
using Service.Domain.Entities;
using Service.Domain.Interfaces;
using Service.Infra.Data.Context;

namespace Service.Infra.Data.Repositories
{
    public class WorkspaceRepository : BaseRepository<Workspace>, IWorkspaceRepository
    {
        public WorkspaceRepository(ApplicationDbContext context, ICurrentUserService currentUser) : base(context, currentUser)
        {
        }

        protected override IQueryable<Workspace> ApplyIncludes(IQueryable<Workspace> query) =>
            query.Include(w => w.Center).Include(w => w.Product).Include(w => w.User);

        public async Task<Workspace> GetByKeyAsync(int idCenter, int idProduct, int idUser, CancellationToken cancellationToken = default)
        {
            return await ApplyIncludes(_dbSet.AsQueryable())
                .FirstOrDefaultAsync(w => w.IdCenter == idCenter && w.IdProduct == idProduct && w.IdUser == idUser && w.deletedAt == null, cancellationToken);
        }

        public async Task ClearByUserAsync(int idUser, CancellationToken cancellationToken = default)
        {
            var workspaces = await _dbSet.Where(w => w.IdUser == idUser && w.deletedAt == null).ToListAsync(cancellationToken);
            if (workspaces.Count == 0)
                return;

            var now = DateTime.UtcNow;
            var deletedBy = _currentUser.UserId;
            foreach (var workspace in workspaces)
            {
                workspace.deletedAt = now;
                workspace.deletedBy = deletedBy;
            }

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
