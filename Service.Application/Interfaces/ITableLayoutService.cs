using Service.Application.DTOs.TableLayout;

namespace Service.Application.Interfaces
{
    public interface ITableLayoutService
    {
        Task<TableLayoutGetDto?> GetAsync(string tableName, CancellationToken cancellationToken = default);
        Task<TableLayoutGetDto> SaveAsync(string tableName, TableLayoutPutDto putDto, CancellationToken cancellationToken = default);
        Task DeleteAsync(string tableName, CancellationToken cancellationToken = default);
    }
}
