using Service.Domain.Enums;

namespace Service.Domain.AllocationGroups
{
    // Pure, in-memory redistribution algorithm - no DB access here, so it can be unit tested directly.
    // Walks the group's approved workspace quantities one PackQuantity at a time, always picking the
    // item furthest from the target buffer percentage first, until the group's total approved quantity
    // (converted to `adjustmentType`'s unit of measure - Units/Weight/Volume/Value/Pallet - for the limit
    // comparison only) reaches `limit`, or every item is exhausted. ApprovedQuantity itself, and the floor
    // enforced by `stopCondition`, always stay in raw units - only the comparison against `limit` changes.
    public static class PriorizedAllocationEngine
    {
        public static void Run(
            List<PriorizedAllocationItem> items,
            decimal limit,
            PriorizedAllocationStopCondition stopCondition,
            PriorizedAllocationAdjustmentType adjustmentType)
        {
            foreach (var item in items.Where(i => i.PackQuantity <= 0 || i.GetValorizationFactor(adjustmentType) is null))
                item.Finished = true;

            var total = items.Sum(i => i.GetValorizedApprovedQuantity(adjustmentType) ?? 0);

            if (total > limit)
                RunDown(items, limit, stopCondition, adjustmentType);
            else if (total < limit)
                RunUp(items, limit, adjustmentType);
        }

        private static void RunUp(List<PriorizedAllocationItem> items, decimal limit, PriorizedAllocationAdjustmentType adjustmentType)
        {
            while (true)
            {
                var total = items.Sum(i => i.GetValorizedApprovedQuantity(adjustmentType) ?? 0);
                if (total > limit)
                    break;

                var item = items
                    .Where(i => !i.Finished)
                    .OrderBy(i => i.Percentage)
                    .ThenBy(i => i.PackQuantity)
                    .FirstOrDefault();
                if (item == null)
                    break;

                var valorizedPack = item.GetValorizedPackQuantity(adjustmentType)!.Value;
                if (total + valorizedPack > limit)
                {
                    item.Finished = true;
                    continue;
                }

                item.ApprovedQuantity += item.PackQuantity;
            }
        }

        private static void RunDown(
            List<PriorizedAllocationItem> items,
            decimal limit,
            PriorizedAllocationStopCondition stopCondition,
            PriorizedAllocationAdjustmentType adjustmentType)
        {
            while (true)
            {
                var total = items.Sum(i => i.GetValorizedApprovedQuantity(adjustmentType) ?? 0);
                if (total < limit)
                    break;

                var item = items
                    .Where(i => !i.Finished)
                    .OrderByDescending(i => i.Percentage)
                    .ThenByDescending(i => i.PackQuantity)
                    .FirstOrDefault();
                if (item == null)
                    break;

                var floor = GetFloor(item, stopCondition);
                var valorizedPack = item.GetValorizedPackQuantity(adjustmentType)!.Value;
                if (total - valorizedPack < limit || item.ApprovedQuantity - item.PackQuantity < floor)
                {
                    if (item.ApprovedQuantity > item.Moq)
                        item.ApprovedQuantity = item.Moq;
                    item.Finished = true;
                    continue;
                }

                item.ApprovedQuantity -= item.PackQuantity;
            }
        }

        private static decimal GetFloor(PriorizedAllocationItem item, PriorizedAllocationStopCondition stopCondition) => stopCondition switch
        {
            PriorizedAllocationStopCondition.Zero => 0,
            PriorizedAllocationStopCondition.Moq => item.Moq,
            PriorizedAllocationStopCondition.OnePackQuantity => item.PackQuantity,
            _ => 0
        };
    }
}
