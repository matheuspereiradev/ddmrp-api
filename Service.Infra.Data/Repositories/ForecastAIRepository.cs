using Microsoft.EntityFrameworkCore;
using Service.Domain.Account;
using Service.Domain.Entities;
using Service.Domain.Enums;
using Service.Domain.Interfaces;
using Service.Infra.Data.Context;

namespace Service.Infra.Data.Repositories
{
    public class ForecastAIRepository : BaseRepository<ForecastAI>, IForecastAIRepository
    {
        public ForecastAIRepository(ApplicationDbContext context, ICurrentUserService currentUser) : base(context, currentUser)
        {
        }

        protected override IQueryable<ForecastAI> ApplyIncludes(IQueryable<ForecastAI> query) =>
            query.Include(f => f.Product).Include(f => f.Center);

        public async Task<List<ForecastAI>> GetFilteredAsync(int? idCenter, int? idProduct, DateTime? monthYear, ForecastPreviewType? previewType, ForecastPreviewState? previewState, CancellationToken cancellationToken = default)
        {
            var query = ApplyIncludes(_dbSet.AsQueryable()).Where(f => f.deletedAt == null);

            if (idCenter.HasValue)
                query = query.Where(f => f.IdCenter == idCenter.Value);

            if (idProduct.HasValue)
                query = query.Where(f => f.IdProduct == idProduct.Value);

            if (monthYear.HasValue)
                query = query.Where(f => f.MonthYear == monthYear.Value);

            if (previewType.HasValue)
                query = query.Where(f => f.PreviewType == previewType.Value);

            if (previewState.HasValue)
                query = query.Where(f => f.PreviewState == previewState.Value);

            return await query
                .OrderBy(f => f.IdCenter).ThenBy(f => f.IdProduct).ThenByDescending(f => f.MonthYear)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<ForecastAI>> GetNotReviewedBySameItemAsync(int idProduct, int idCenter, DateTime monthYear, int? excludeId = null, CancellationToken cancellationToken = default)
        {
            var query = _dbSet.Where(f => f.deletedAt == null
                && f.IdProduct == idProduct
                && f.IdCenter == idCenter
                && f.MonthYear == monthYear
                && f.PreviewState == ForecastPreviewState.NotReviewed);

            if (excludeId.HasValue)
                query = query.Where(f => f.Id != excludeId.Value);

            return await query.ToListAsync(cancellationToken);
        }
    }
}
