using Microsoft.EntityFrameworkCore;
using Service.Domain.Account;
using Service.Domain.AllocationGroups;
using Service.Domain.Entities;
using Service.Domain.Interfaces;
using Service.Infra.Data.Context;

namespace Service.Infra.Data.Repositories
{
    public class AllocationGroupRepository : BaseRepository<AllocationGroup>, IAllocationGroupRepository
    {
        public AllocationGroupRepository(ApplicationDbContext context, ICurrentUserService currentUser) : base(context, currentUser)
        {
        }

        public async Task<List<EfficientDistributionRow>> GetEfficientDistributionAsync(int idUser, CancellationToken cancellationToken = default)
        {
            var items = await _context.Workspace
                .Where(w => w.deletedAt == null && w.IdUser == idUser && w.Approved)
                .Join(
                    _context.CenterProduct.Where(cp =>
                        cp.deletedAt == null &&
                        cp.IdAllocationGroup != null &&
                        cp.AllocationGroup!.deletedAt == null &&
                        cp.Product.deletedAt == null),
                    w => new { w.IdProduct, w.IdCenter },
                    cp => new { cp.IdProduct, cp.IdCenter },
                    (w, cp) => new
                    {
                        w.OptimizedQuantity,
                        IdAllocationGroup = cp.IdAllocationGroup!.Value,
                        GroupName = cp.AllocationGroup!.Name,
                        ProductWeight = cp.Product.Weight,
                        ProductVolume = cp.Product.Volume,
                        ProductValue = cp.Product.Value,
                        ProductPallet = cp.Product.Pallet
                    })
                .ToListAsync(cancellationToken);

            return items
                .GroupBy(x => new { x.IdAllocationGroup, x.GroupName })
                .Select(g => new EfficientDistributionRow
                {
                    Id = g.Key.IdAllocationGroup,
                    Name = g.Key.GroupName,
                    ApprovedQuantityUnit = g.Sum(x => x.OptimizedQuantity),
                    ApprovedQuantityWeight = g.Any(x => !x.ProductWeight.HasValue)
                        ? null
                        : g.Sum(x => x.OptimizedQuantity * x.ProductWeight!.Value),
                    ApprovedQuantityVolume = g.Any(x => !x.ProductVolume.HasValue)
                        ? null
                        : g.Sum(x => x.OptimizedQuantity * x.ProductVolume!.Value),
                    ApprovedQuantityValue = g.Any(x => !x.ProductValue.HasValue)
                        ? null
                        : g.Sum(x => x.OptimizedQuantity * x.ProductValue!.Value),
                    ApprovedQuantityPallet = g.Any(x => !x.ProductPallet.HasValue || x.ProductPallet.Value == 0)
                        ? null
                        : g.Sum(x => x.OptimizedQuantity / x.ProductPallet!.Value)
                })
                .OrderBy(r => r.Name)
                .ToList();
        }
    }
}
