using Service.Domain.Enums;

namespace Service.Domain.AllocationGroups
{
    // Pure, in-memory redistribution algorithm - no DB access here, so it can be unit tested directly.
    // Walks the group's approved workspace quantities one PackQuantity at a time, always picking the
    // item furthest from the target buffer percentage first, until the group's total approved quantity
    // reaches `limit` (or every item is exhausted).
    public static class EfficientDistributionEngine
    {
        public static void Run(List<EfficientDistributionAllocationItem> items, decimal limit, EfficientDistributionStopCondition stopCondition)
        {
            foreach (var item in items.Where(i => i.PackQuantity <= 0))
                item.Finished = true;

            var total = items.Sum(i => i.ApprovedQuantity);

            if (total > limit)
                RunDown(items, limit, stopCondition);
            else if (total < limit)
                RunUp(items, limit);
        }

        private static void RunUp(List<EfficientDistributionAllocationItem> items, decimal limit)
        {
            while (true)
            {
                var total = items.Sum(i => i.ApprovedQuantity);
                if (total > limit)
                    break;

                var item = items
                    .Where(i => !i.Finished)
                    .OrderBy(i => i.Percentage)
                    .ThenBy(i => i.PackQuantity)
                    .FirstOrDefault();
                if (item == null)
                    break;

                if (total + item.PackQuantity > limit)
                {
                    item.Finished = true;
                    continue;
                }

                item.ApprovedQuantity += item.PackQuantity;
            }
        }

        private static void RunDown(List<EfficientDistributionAllocationItem> items, decimal limit, EfficientDistributionStopCondition stopCondition)
        {
            while (true)
            {
                var total = items.Sum(i => i.ApprovedQuantity);
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
                if (total - item.PackQuantity < limit || item.ApprovedQuantity - item.PackQuantity < floor)
                {
                    item.Finished = true;
                    continue;
                }

                item.ApprovedQuantity -= item.PackQuantity;
            }
        }

        private static decimal GetFloor(EfficientDistributionAllocationItem item, EfficientDistributionStopCondition stopCondition) => stopCondition switch
        {
            EfficientDistributionStopCondition.Zero => 0,
            EfficientDistributionStopCondition.Moq => item.Moq,
            EfficientDistributionStopCondition.OnePackQuantity => item.PackQuantity,
            _ => 0
        };
    }
}
