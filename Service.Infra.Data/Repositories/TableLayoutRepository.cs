using Microsoft.EntityFrameworkCore;
using Service.Domain.Entities;
using Service.Domain.Interfaces;
using Service.Infra.Data.Context;

namespace Service.Infra.Data.Repositories
{
    public class TableLayoutRepository : ITableLayoutRepository
    {
        private readonly ApplicationDbContext _context;

        public TableLayoutRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<TableLayout?> GetAsync(int userId, string tableName, CancellationToken cancellationToken = default)
        {
            return await _context.TableLayout
                .FirstOrDefaultAsync(t => t.UserId == userId && t.TableName == tableName, cancellationToken);
        }

        public async Task AddAsync(TableLayout tableLayout, CancellationToken cancellationToken = default)
        {
            await _context.TableLayout.AddAsync(tableLayout, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task UpdateAsync(TableLayout tableLayout, CancellationToken cancellationToken = default)
        {
            _context.TableLayout.Update(tableLayout);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteAsync(TableLayout tableLayout, CancellationToken cancellationToken = default)
        {
            _context.TableLayout.Remove(tableLayout);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
