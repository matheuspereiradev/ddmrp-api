using NSubstitute;
using Service.Application.Exceptions;
using Service.Application.Services;
using Service.Domain.Account;
using Service.Domain.Entities;
using Service.Domain.Interfaces;
using Service.Domain.Pagination;
using Service.Domain.Procedures;

namespace Service.Tests.Application;

public class ImporterServiceTests
{
    private readonly IImporterRepository _importerRepository = Substitute.For<IImporterRepository>();
    private readonly IProcedureCatalogService _procedureCatalog = Substitute.For<IProcedureCatalogService>();
    private readonly IImportProcedureRunner _importRunner = Substitute.For<IImportProcedureRunner>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly ImporterService _sut;

    public ImporterServiceTests()
    {
        _currentUser.UserId.Returns(1);
        _sut = new ImporterService(_importerRepository, _procedureCatalog, _importRunner, _currentUser);
    }

    private static ProcedureParameterInfo BuildTableParameter() => new()
    {
        Name = "Rows",
        SqlTypeName = "table type",
        IsTableType = true,
        TableTypeFullName = "dbo.ZafImportType",
        Columns = [new ProcedureParameterColumnInfo { Name = "IdProduct", SqlTypeName = "int", IsNullable = false }]
    };

    [Fact]
    public async Task GetByIdAsync_ReturnsDto_WhenImporterExists()
    {
        var importer = new Importer { Id = 1, Name = "Import ZAF", Description = "Bulk ZAF import", ProcedureName = "dbo.ImportZaf" };
        _importerRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(importer);

        var result = await _sut.GetByIdAsync(1);

        Assert.Equal(importer.Name, result.Name);
        Assert.Equal(importer.ProcedureName, result.ProcedureName);
    }

    [Fact]
    public async Task GetByIdAsync_ThrowsNotFoundException_WhenImporterDoesNotExist()
    {
        _importerRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns((Importer)null!);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetByIdAsync(1));
    }

    [Fact]
    public async Task GetAllAsync_MapsPagedResult()
    {
        var importer = new Importer { Id = 1, Name = "Import ZAF", ProcedureName = "dbo.ImportZaf" };
        _importerRepository.GetAllAsync(1, 10, Arg.Any<CancellationToken>())
            .Returns(new PagedList<Importer>([importer], 1, 10, 1));

        var result = await _sut.GetAllAsync(1, 10);

        Assert.Single(result);
        Assert.Equal("Import ZAF", result[0].Name);
        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public async Task GetParametersAsync_ThrowsNotFoundException_WhenImporterDoesNotExist()
    {
        _importerRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns((Importer)null!);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetParametersAsync(1));
    }

    [Fact]
    public async Task GetParametersAsync_MapsTableTypeColumns()
    {
        var importer = new Importer { Id = 1, Name = "Import ZAF", ProcedureName = "dbo.ImportZaf" };
        _importerRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(importer);
        _procedureCatalog.GetParametersAsync("dbo.ImportZaf", Arg.Any<CancellationToken>())
            .Returns([BuildTableParameter()]);

        var result = await _sut.GetParametersAsync(1);

        Assert.Single(result);
        Assert.True(result[0].IsTableType);
        Assert.Equal("IdProduct", result[0].Columns![0].Name);
    }

    [Fact]
    public async Task RunAsync_ReturnsRowCount_OnSuccess()
    {
        var importer = new Importer { Id = 1, Name = "Import ZAF", ProcedureName = "dbo.ImportZaf" };
        _importerRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(importer);
        _importRunner.RunAsync(importer, Arg.Any<Stream>(), 1, Arg.Any<CancellationToken>())
            .Returns(new ImportRunResult { RowCount = 3 });

        using var stream = new MemoryStream();
        var result = await _sut.RunAsync(1, stream);

        Assert.Equal(3, result.RowCount);
    }

    [Fact]
    public async Task RunAsync_ThrowsNotFoundException_WhenImporterDoesNotExist()
    {
        _importerRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns((Importer)null!);

        using var stream = new MemoryStream();
        await Assert.ThrowsAsync<NotFoundException>(() => _sut.RunAsync(1, stream));
    }

    [Fact]
    public async Task RunAsync_ThrowsBadRequestException_WhenRunnerThrowsInvalidOperationException()
    {
        var importer = new Importer { Id = 1, Name = "Import ZAF", ProcedureName = "dbo.ImportZaf" };
        _importerRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(importer);
        _importRunner.RunAsync(importer, Arg.Any<Stream>(), 1, Arg.Any<CancellationToken>())
            .Returns<Task<ImportRunResult>>(_ => throw new InvalidOperationException("CSV is missing required column(s): IdProduct."));

        using var stream = new MemoryStream();
        await Assert.ThrowsAsync<BadRequestException>(() => _sut.RunAsync(1, stream));
    }
}
