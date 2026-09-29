using NSubstitute;
using Service.Application.Exceptions;
using Service.Application.Services;
using Service.Domain.Account;
using Service.Domain.Entities;
using Service.Domain.Interfaces;
using Service.Domain.Pagination;

namespace Service.Tests.Application;

public class ExporterServiceTests : IDisposable
{
    private readonly IExporterRepository _exporterRepository = Substitute.For<IExporterRepository>();
    private readonly IProcedureCatalogService _procedureCatalog = Substitute.For<IProcedureCatalogService>();
    private readonly IExportProcedureRunner _exportRunner = Substitute.For<IExportProcedureRunner>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly string _exportsDirectory = Path.Combine(Path.GetTempPath(), $"exports-tests-{Guid.NewGuid()}");
    private readonly ExporterService _sut;

    public ExporterServiceTests()
    {
        _currentUser.UserId.Returns(1);
        _sut = new ExporterService(_exporterRepository, _procedureCatalog, _exportRunner, _currentUser, _exportsDirectory, 24);
    }

    public void Dispose()
    {
        if (Directory.Exists(_exportsDirectory))
            Directory.Delete(_exportsDirectory, recursive: true);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsDto_WhenExporterExists()
    {
        var exporter = new Exporter { Id = 1, Name = "Export Inventory", ProcedureName = "dbo.ExportInventory" };
        _exporterRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(exporter);

        var result = await _sut.GetByIdAsync(1);

        Assert.Equal(exporter.Name, result.Name);
        Assert.Equal(exporter.ProcedureName, result.ProcedureName);
    }

    [Fact]
    public async Task GetByIdAsync_ThrowsNotFoundException_WhenExporterDoesNotExist()
    {
        _exporterRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns((Exporter)null!);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetByIdAsync(1));
    }

    [Fact]
    public async Task GetAllAsync_MapsPagedResult()
    {
        var exporter = new Exporter { Id = 1, Name = "Export Inventory", ProcedureName = "dbo.ExportInventory" };
        _exporterRepository.GetAllAsync(1, 10, Arg.Any<CancellationToken>())
            .Returns(new PagedList<Exporter>([exporter], 1, 10, 1));

        var result = await _sut.GetAllAsync(1, 10);

        Assert.Single(result);
        Assert.Equal("Export Inventory", result[0].Name);
    }

    [Fact]
    public async Task GetParametersAsync_ThrowsNotFoundException_WhenExporterDoesNotExist()
    {
        _exporterRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns((Exporter)null!);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetParametersAsync(1));
    }

    [Fact]
    public async Task RunAsync_WritesFileAndReturnsDownloadUrl_OnSuccess()
    {
        var exporter = new Exporter { Id = 1, Name = "Export Inventory", ProcedureName = "dbo.ExportInventory" };
        _exporterRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(exporter);
        _exportRunner.RunAsync(exporter, Arg.Any<IReadOnlyDictionary<string, string?>>(), 1, Arg.Any<Stream>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var stream = callInfo.Arg<Stream>();
                var bytes = "id,name\n1,foo\n"u8.ToArray();
                stream.Write(bytes, 0, bytes.Length);
                return Task.FromResult(1);
            });

        var result = await _sut.RunAsync(1, []);

        Assert.Equal(1, result.RowCount);
        Assert.StartsWith("/api/exporter/download/", result.DownloadUrl);
        Assert.True(Directory.Exists(_exportsDirectory));
        Assert.Single(Directory.GetFiles(_exportsDirectory, "*.csv"));
    }

    [Fact]
    public async Task RunAsync_ThrowsNotFoundException_WhenExporterDoesNotExist()
    {
        _exporterRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns((Exporter)null!);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.RunAsync(1, []));
    }

    [Fact]
    public async Task RunAsync_DeletesFileAndThrowsBadRequestException_WhenRunnerThrowsInvalidOperationException()
    {
        var exporter = new Exporter { Id = 1, Name = "Export Inventory", ProcedureName = "dbo.ExportInventory" };
        _exporterRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(exporter);
        _exportRunner.RunAsync(exporter, Arg.Any<IReadOnlyDictionary<string, string?>>(), 1, Arg.Any<Stream>(), Arg.Any<CancellationToken>())
            .Returns<Task<int>>(_ => throw new InvalidOperationException("Procedure 'dbo.ExportInventory' failed."));

        await Assert.ThrowsAsync<BadRequestException>(() => _sut.RunAsync(1, []));

        Assert.True(!Directory.Exists(_exportsDirectory) || Directory.GetFiles(_exportsDirectory, "*.csv").Length == 0);
    }

    [Fact]
    public async Task OpenDownloadStreamAsync_ThrowsBadRequestException_ForInvalidToken()
    {
        await Assert.ThrowsAsync<BadRequestException>(() => _sut.OpenDownloadStreamAsync("../secrets.csv"));
    }

    [Fact]
    public async Task OpenDownloadStreamAsync_ThrowsNotFoundException_WhenFileDoesNotExist()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _sut.OpenDownloadStreamAsync($"{Guid.NewGuid()}.csv"));
    }
}
