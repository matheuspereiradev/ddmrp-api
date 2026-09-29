using System.Text.RegularExpressions;
using Service.Application.DTOs.TableLayout;
using Service.Application.Exceptions;
using Service.Application.Interfaces;
using Service.Domain.Account;
using Service.Domain.Entities;
using Service.Domain.Interfaces;

namespace Service.Application.Services
{
    public class TableLayoutService : ITableLayoutService
    {
        private const int LabelMaxLength = 100;
        private static readonly Regex TableNameRegex = new("^[A-Za-z0-9._-]{1,100}$", RegexOptions.Compiled);

        private readonly ITableLayoutRepository _repository;
        private readonly ICurrentUserService _currentUser;

        public TableLayoutService(ITableLayoutRepository repository, ICurrentUserService currentUser)
        {
            _repository = repository;
            _currentUser = currentUser;
        }

        public async Task<TableLayoutGetDto?> GetAsync(string tableName, CancellationToken cancellationToken = default)
        {
            ValidateTableName(tableName);
            var tableLayout = await _repository.GetAsync(_currentUser.UserId, tableName, cancellationToken);
            return tableLayout == null ? null : ToGetDto(tableLayout);
        }

        public async Task<TableLayoutGetDto> SaveAsync(string tableName, TableLayoutPutDto putDto, CancellationToken cancellationToken = default)
        {
            ValidateTableName(tableName);
            var columns = ToColumns(putDto.Columns);

            var now = DateTime.UtcNow;
            var tableLayout = await _repository.GetAsync(_currentUser.UserId, tableName, cancellationToken);
            if (tableLayout == null)
            {
                tableLayout = new TableLayout
                {
                    UserId = _currentUser.UserId,
                    TableName = tableName,
                    Columns = columns,
                    CreatedAt = now,
                    UpdatedAt = now
                };
                await _repository.AddAsync(tableLayout, cancellationToken);
            }
            else
            {
                tableLayout.Columns = columns;
                tableLayout.UpdatedAt = now;
                await _repository.UpdateAsync(tableLayout, cancellationToken);
            }

            return ToGetDto(tableLayout);
        }

        public async Task DeleteAsync(string tableName, CancellationToken cancellationToken = default)
        {
            ValidateTableName(tableName);
            var tableLayout = await _repository.GetAsync(_currentUser.UserId, tableName, cancellationToken);
            if (tableLayout != null)
                await _repository.DeleteAsync(tableLayout, cancellationToken);
        }

        private static void ValidateTableName(string tableName)
        {
            if (string.IsNullOrEmpty(tableName) || !TableNameRegex.IsMatch(tableName))
                throw new BadRequestException("Invalid table name.");
        }

        private static List<TableLayoutColumn> ToColumns(List<TableLayoutColumnDto> columnDtos)
        {
            var columnIds = new HashSet<string>(StringComparer.Ordinal);
            var orders = new HashSet<int>();
            var columns = new List<TableLayoutColumn>(columnDtos.Count);

            foreach (var dto in columnDtos)
            {
                if (!columnIds.Add(dto.ColumnId))
                    throw new BadRequestException($"Duplicated columnId '{dto.ColumnId}'.");

                if (!orders.Add(dto.Order))
                    throw new BadRequestException($"Duplicated order '{dto.Order}'.");

                var label = string.IsNullOrWhiteSpace(dto.Label) ? null : dto.Label.Trim();
                if (label?.Length > LabelMaxLength)
                    throw new BadRequestException($"Label of column '{dto.ColumnId}' exceeds {LabelMaxLength} characters.");

                columns.Add(new TableLayoutColumn
                {
                    ColumnId = dto.ColumnId,
                    Order = dto.Order,
                    Visible = dto.Visible,
                    IsFixed = dto.IsFixed,
                    Label = label,
                    Color = dto.Color
                });
            }

            return columns;
        }

        private static TableLayoutGetDto ToGetDto(TableLayout tableLayout) => new()
        {
            TableName = tableLayout.TableName,
            UpdatedAt = tableLayout.UpdatedAt,
            Columns = tableLayout.Columns.OrderBy(c => c.Order).Select(c => new TableLayoutColumnDto
            {
                ColumnId = c.ColumnId,
                Order = c.Order,
                Visible = c.Visible,
                IsFixed = c.IsFixed,
                Label = c.Label,
                Color = c.Color
            }).ToList()
        };
    }
}
