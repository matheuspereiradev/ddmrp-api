using NSubstitute;
using Service.Application.Exceptions;
using Service.Application.Services;
using Service.Domain.Ingestion;
using Service.Domain.Interfaces;

namespace Service.Tests.Application;

public class IngestionServiceTests
{
    private readonly IIngestionConfigProvider _configProvider = Substitute.For<IIngestionConfigProvider>();
    private readonly IIngestionSourceReader _reader = Substitute.For<IIngestionSourceReader>();
    private readonly IIngestionWriter _writer = Substitute.For<IIngestionWriter>();
    private readonly ILookupValueProvider _lookupProvider = Substitute.For<ILookupValueProvider>();
    private readonly IngestionService _sut;

    public IngestionServiceTests()
    {
        _reader.CanHandle("Csv").Returns(true);
        _sut = new IngestionService(_configProvider, [_reader], [_writer], _lookupProvider);
    }

    private static IngestionSourceConfig BuildSource(string view, params FieldMappingConfig[] mappings) => new()
    {
        View = view,
        Type = "Csv",
        Path = "test.csv",
        FieldMappings = mappings.ToList()
    };

    private static async IAsyncEnumerable<IReadOnlyDictionary<string, string?>> ToAsyncEnumerable(IEnumerable<IReadOnlyDictionary<string, string?>> rows)
    {
        foreach (var row in rows)
        {
            yield return row;
        }
        await Task.CompletedTask;
    }

    [Fact]
    public async Task RunAsync_ThrowsBadRequestException_WhenNoSourceMatchesRequestedView()
    {
        _configProvider.GetSourcesAsync(Arg.Any<CancellationToken>()).Returns([BuildSource("Products")]);

        await Assert.ThrowsAsync<BadRequestException>(() => _sut.RunAsync("Centers"));
    }

    [Fact]
    public async Task RunAsync_WritesMappedRows_AndReturnsAggregatedCounts()
    {
        var source = BuildSource("Centers", new FieldMappingConfig { Source = "Code", Target = "Code" });
        _configProvider.GetSourcesAsync(Arg.Any<CancellationToken>()).Returns([source]);
        _writer.CanHandle(source).Returns(true);

        var rows = new List<IReadOnlyDictionary<string, string?>>
        {
            new Dictionary<string, string?> { ["Code"] = "C1" },
            new Dictionary<string, string?> { ["Code"] = "C2" }
        };
        _reader.ReadAsync(source, Arg.Any<CancellationToken>()).Returns(ToAsyncEnumerable(rows));
        _writer.WriteAsync(source, Arg.Any<List<Dictionary<string, string?>>>(), Arg.Any<CancellationToken>())
            .Returns(new IngestionWriteResult { Inserted = 2 });

        var results = await _sut.RunAsync("Centers");

        Assert.Single(results);
        Assert.Equal(2, results[0].RowsRead);
        Assert.Equal(2, results[0].RowsInserted);
        Assert.Equal(0, results[0].RowsFailed);
        await _writer.Received(1).WriteAsync(
            source,
            Arg.Is<List<Dictionary<string, string?>>>(rows => rows.Count == 2),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_PassesDeleteNonSentFromConfig_ToWriter_AndAggregatesDeletedCount()
    {
        var source = BuildSource("Centers", new FieldMappingConfig { Source = "Code", Target = "Code" });
        source.DeleteNonSent = true;
        _configProvider.GetSourcesAsync(Arg.Any<CancellationToken>()).Returns([source]);
        _writer.CanHandle(source).Returns(true);

        var rows = new List<IReadOnlyDictionary<string, string?>> { new Dictionary<string, string?> { ["Code"] = "C1" } };
        _reader.ReadAsync(source, Arg.Any<CancellationToken>()).Returns(ToAsyncEnumerable(rows));
        _writer.WriteAsync(source, Arg.Any<List<Dictionary<string, string?>>>(), Arg.Any<CancellationToken>())
            .Returns(new IngestionWriteResult { Inserted = 1, Deleted = 3 });

        var result = (await _sut.RunAsync("Centers")).Single();

        Assert.Equal(3, result.RowsDeleted);
        await _writer.Received(1).WriteAsync(source, Arg.Any<List<Dictionary<string, string?>>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_ResolvesLookupsBeforeMapping_AndReportsUnresolvedRowsAsFailed()
    {
        var source = BuildSource("Forecast",
            new FieldMappingConfig { Source = "ProductReference", Target = "IdProduct", Lookup = new LookupConfig { Entity = "Product", By = "Reference" } });
        _configProvider.GetSourcesAsync(Arg.Any<CancellationToken>()).Returns([source]);
        _writer.CanHandle(source).Returns(true);

        var rows = new List<IReadOnlyDictionary<string, string?>>
        {
            new Dictionary<string, string?> { ["ProductReference"] = "KNOWN" },
            new Dictionary<string, string?> { ["ProductReference"] = "UNKNOWN" }
        };
        _reader.ReadAsync(source, Arg.Any<CancellationToken>()).Returns(ToAsyncEnumerable(rows));
        _lookupProvider.ResolveAsync("Product", "Reference", Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, string> { ["KNOWN"] = "1" });
        _writer.WriteAsync(source, Arg.Any<List<Dictionary<string, string?>>>(), Arg.Any<CancellationToken>())
            .Returns(new IngestionWriteResult { Inserted = 1 });

        var result = (await _sut.RunAsync("Forecast")).Single();

        Assert.Equal(2, result.RowsRead);
        Assert.Equal(1, result.RowsInserted);
        Assert.Equal(1, result.RowsFailed);
        Assert.Single(result.Errors);
    }

    [Fact]
    public async Task RunAsync_AggregatesValuesAcrossFieldsSharingTheSameLookup()
    {
        var source = BuildSource("CenterProducts",
            new FieldMappingConfig { Source = "CenterCode", Target = "IdCenter", Lookup = new LookupConfig { Entity = "Center", By = "Code" } },
            new FieldMappingConfig { Source = "OriginCenterCode", Target = "IdOriginCenter", Lookup = new LookupConfig { Entity = "Center", By = "Code" }, Required = false });
        _configProvider.GetSourcesAsync(Arg.Any<CancellationToken>()).Returns([source]);
        _writer.CanHandle(source).Returns(true);

        var rows = new List<IReadOnlyDictionary<string, string?>>
        {
            new Dictionary<string, string?> { ["CenterCode"] = "C1", ["OriginCenterCode"] = "C2" }
        };
        _reader.ReadAsync(source, Arg.Any<CancellationToken>()).Returns(ToAsyncEnumerable(rows));
        _lookupProvider.ResolveAsync("Center", "Code", Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, string> { ["C1"] = "1", ["C2"] = "2" });
        _writer.WriteAsync(source, Arg.Any<List<Dictionary<string, string?>>>(), Arg.Any<CancellationToken>())
            .Returns(new IngestionWriteResult { Inserted = 1 });

        var result = (await _sut.RunAsync("CenterProducts")).Single();

        Assert.Equal(0, result.RowsFailed);
        await _lookupProvider.Received(1).ResolveAsync(
            "Center", "Code",
            Arg.Is<IEnumerable<string>>(values => values.Contains("C1") && values.Contains("C2")),
            Arg.Any<CancellationToken>());
        await _writer.Received(1).WriteAsync(
            source,
            Arg.Is<List<Dictionary<string, string?>>>(rows => rows[0]["IdCenter"] == "1" && rows[0]["IdOriginCenter"] == "2"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_ThrowsBadRequestException_WhenNoWriterHandlesView()
    {
        var source = BuildSource("Unknown", new FieldMappingConfig { Source = "X", Target = "Y" });
        _configProvider.GetSourcesAsync(Arg.Any<CancellationToken>()).Returns([source]);
        _reader.ReadAsync(source, Arg.Any<CancellationToken>()).Returns(ToAsyncEnumerable([]));

        await Assert.ThrowsAsync<BadRequestException>(() => _sut.RunAsync("Unknown"));
    }

    [Fact]
    public async Task RunAsync_ThrowsBadRequestException_WhenDeclaredKeyHasNoFieldMapping()
    {
        var source = BuildSource("Centers", new FieldMappingConfig { Source = "Description", Target = "Description" });
        source.Key = ["Code"];
        _configProvider.GetSourcesAsync(Arg.Any<CancellationToken>()).Returns([source]);
        _writer.CanHandle(source).Returns(true);

        await Assert.ThrowsAsync<BadRequestException>(() => _sut.RunAsync("Centers"));
    }

    [Fact]
    public async Task RunAsync_ThrowsBadRequestException_WhenDeclaredKeyIsMappedToAConstant()
    {
        var source = BuildSource("Centers", new FieldMappingConfig { Source = null, Target = "Code", Default = "C1" });
        source.Key = ["Code"];
        _configProvider.GetSourcesAsync(Arg.Any<CancellationToken>()).Returns([source]);
        _writer.CanHandle(source).Returns(true);

        await Assert.ThrowsAsync<BadRequestException>(() => _sut.RunAsync("Centers"));
    }
}
