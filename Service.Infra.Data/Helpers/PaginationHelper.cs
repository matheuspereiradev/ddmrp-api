using Service.Domain.Pagination;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace Service.Infra.Data.Helpers
{
    public static class PaginationHelper
    {
       public static async Task<PagedList<T>> CreateAsync<T>(
           IQueryable<T> source,
           int pageNumber,
           int pageSize,
           CancellationToken cancellationToken = default
       ) where T : class
        {
            var count = await source.CountAsync(cancellationToken);
            var items = await source.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
            return new PagedList<T>(items, pageNumber, pageSize, count);
        }
    }
}
