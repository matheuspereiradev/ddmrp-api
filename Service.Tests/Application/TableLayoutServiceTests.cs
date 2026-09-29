using NSubstitute;
using Service.Application.DTOs.TableLayout;
using Service.Application.Exceptions;
using Service.Application.Services;
using Service.Domain.Account;
using Service.Domain.Entities;
using Service.Domain.Interfaces;

namespace Service.Tests.Application;

public class TableLayoutServiceTests
{
    private readonly ITableLayoutRepository _repository = Substitute.For<ITableLayoutRepository>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly TableLayoutService _sut;

    public TableLayoutServiceTests()
    {
        _currentUser.UserId.Returns(1);
        _sut = new TableLayoutService(_repository, _currentUser);
    }

    private static TableLayoutColumnDto Column(string columnId, int order, string? label = null, string? color = null) =>
        new() { ColumnId = columnId, Order = order, Visible = true, IsFixed = order == 0, Label = label, Color = color };

    [Fact]
    public async Task GetAsync_ReturnsNull_WhenNoLayoutSaved()
    {
        _repository.GetAsync(1, "inventoryBufferManagement", Arg.Any<CancellationToken>()).Returns((TableLayout?)null);

        var result = await _sut.GetAsync("inventoryBufferManagement");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAsync_ReturnsColumnsSortedByOrder()
    {
        _repository.GetAsync(1, "inventoryBufferManagement", Arg.Any<CancellationToken>()).Returns(new TableLayout
        {
            UserId = 1,
            TableName = "inventoryBufferManagement",
            Columns =
            [
                new TableLayoutColumn { ColumnId = "b", Order = 1 },
                new TableLayoutColumn { ColumnId = "a", Order = 0 }
            ]
        });

        var result = await _sut.GetAsync("inventoryBufferManagement");

        Assert.NotNull(result);
        Assert.Equal(["a", "b"], result.Columns.Select(c => c.ColumnId));
    }

    [Theory]
    [InlineData("invalid name")]
    [InlineData("invalid/name")]
    [InlineData("")]
    public async Task GetAsync_Throws_WhenTableNameIsInvalid(string tableName)
    {
        await Assert.ThrowsAsync<BadRequestException>(() => _sut.GetAsync(tableName));
    }

    [Fact]
    public async Task SaveAsync_CreatesLayout_WhenNoneExists()
    {
        _repository.GetAsync(1, "inventoryBufferManagement.inbounds", Arg.Any<CancellationToken>()).Returns((TableLayout?)null);
        var dto = new TableLayoutPutDto { Columns = [Column("productCode", 0), Column("redZone", 1, "  Zona Vermelha  ", "#FFE5E5")] };

        var result = await _sut.SaveAsync("inventoryBufferManagement.inbounds", dto);

        Assert.Equal("inventoryBufferManagement.inbounds", result.TableName);
        Assert.Equal("Zona Vermelha", result.Columns[1].Label);
        Assert.True(result.Columns[0].IsFixed);
        Assert.False(result.Columns[1].IsFixed);
        await _repository.Received(1).AddAsync(
            Arg.Is<TableLayout>(t => t.UserId == 1 && t.TableName == "inventoryBufferManagement.inbounds" && t.Columns.Count == 2),
            Arg.Any<CancellationToken>());
        await _repository.DidNotReceive().UpdateAsync(Arg.Any<TableLayout>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SaveAsync_ReplacesAllColumns_WhenLayoutExists()
    {
        var existing = new TableLayout
        {
            Id = 5,
            UserId = 1,
            TableName = "inventoryBufferManagement",
            Columns = [new TableLayoutColumn { ColumnId = "old", Order = 0 }]
        };
        _repository.GetAsync(1, "inventoryBufferManagement", Arg.Any<CancellationToken>()).Returns(existing);

        await _sut.SaveAsync("inventoryBufferManagement", new TableLayoutPutDto { Columns = [Column("new", 0)] });

        Assert.Equal(["new"], existing.Columns.Select(c => c.ColumnId));
        await _repository.Received(1).UpdateAsync(existing, Arg.Any<CancellationToken>());
        await _repository.DidNotReceive().AddAsync(Arg.Any<TableLayout>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SaveAsync_ConvertsBlankLabelToNull()
    {
        _repository.GetAsync(1, "t", Arg.Any<CancellationToken>()).Returns((TableLayout?)null);

        var result = await _sut.SaveAsync("t", new TableLayoutPutDto { Columns = [Column("a", 0, "   ")] });

        Assert.Null(result.Columns[0].Label);
    }

    [Fact]
    public async Task SaveAsync_Throws_WhenColumnIdIsDuplicated()
    {
        var dto = new TableLayoutPutDto { Columns = [Column("a", 0), Column("a", 1)] };

        await Assert.ThrowsAsync<BadRequestException>(() => _sut.SaveAsync("t", dto));
        await _repository.DidNotReceive().AddAsync(Arg.Any<TableLayout>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SaveAsync_Throws_WhenOrderIsDuplicated()
    {
        var dto = new TableLayoutPutDto { Columns = [Column("a", 0), Column("b", 0)] };

        await Assert.ThrowsAsync<BadRequestException>(() => _sut.SaveAsync("t", dto));
    }

    [Fact]
    public async Task SaveAsync_Throws_WhenTrimmedLabelExceedsMaxLength()
    {
        var dto = new TableLayoutPutDto { Columns = [Column("a", 0, new string('x', 101))] };

        await Assert.ThrowsAsync<BadRequestException>(() => _sut.SaveAsync("t", dto));
    }

    [Fact]
    public async Task DeleteAsync_RemovesLayout_WhenItExists()
    {
        var existing = new TableLayout { Id = 5, UserId = 1, TableName = "t" };
        _repository.GetAsync(1, "t", Arg.Any<CancellationToken>()).Returns(existing);

        await _sut.DeleteAsync("t");

        await _repository.Received(1).DeleteAsync(existing, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_DoesNothing_WhenNoLayoutSaved()
    {
        _repository.GetAsync(1, "t", Arg.Any<CancellationToken>()).Returns((TableLayout?)null);

        await _sut.DeleteAsync("t");

        await _repository.DidNotReceive().DeleteAsync(Arg.Any<TableLayout>(), Arg.Any<CancellationToken>());
    }
}
