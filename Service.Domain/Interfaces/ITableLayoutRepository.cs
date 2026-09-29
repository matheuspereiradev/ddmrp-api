using Service.Domain.Entities;

namespace Service.Domain.Interfaces
{
    public interface ITableLayoutRepository
    {
        Task<TableLayout?> GetAsync(int userId, string tableName, CancellationToken cancellationToken = default);
        Task AddAsync(TableLayout tableLayout, CancellationToken cancellationToken = default);
        Task UpdateAsync(TableLayout tableLayout, CancellationToken cancellationToken = default);
        Task DeleteAsync(TableLayout tableLayout, CancellationToken cancellationToken = default);
    }
}
