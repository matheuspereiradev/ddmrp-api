using Service.Application.Ingestion;
using Service.Domain.Ingestion;

namespace Service.Tests.Application;

public class FieldMappingEngineTests
{
    private static IngestionSourceConfig BuildSource(params FieldMappingConfig[] mappings) => new()
    {
        View = "TestView",
        Type = "Csv",
        Path = "test.csv",
        FieldMappings = mappings.ToList()
    };

    [Fact]
    public void MapRows_UsesSourceValue_WhenPresent()
    {
        var source = BuildSource(new FieldMappingConfig { Source = "SKU", Target = "Reference" });
        var rows = new List<IReadOnlyDictionary<string, string?>> { new Dictionary<string, string?> { ["SKU"] = "ABC123" } };

        var result = FieldMappingEngine.MapRows(rows, source, new Dictionary<(string, string), Dictionary<string, string>>());

        Assert.Single(result);
        Assert.Null(result[0].Error);
        Assert.Equal("ABC123", result[0].Values["Reference"]);
    }

    [Fact]
    public void MapRows_FallsBackToDefault_WhenSourceValueMissing()
    {
        var source = BuildSource(new FieldMappingConfig { Source = "UnitOfMeasure", Target = "UnitOfMeasure", Default = "UN" });
        var rows = new List<IReadOnlyDictionary<string, string?>> { new Dictionary<string, string?> { ["UnitOfMeasure"] = "" } };

        var result = FieldMappingEngine.MapRows(rows, source, new Dictionary<(string, string), Dictionary<string, string>>());

        Assert.Equal("UN", result[0].Values["UnitOfMeasure"]);
    }

    [Fact]
    public void MapRows_AlwaysUsesDefault_WhenSourceIsAbsent()
    {
        var source = BuildSource(new FieldMappingConfig { Source = null, Target = "Segment", Default = "BR" });
        var rows = new List<IReadOnlyDictionary<string, string?>>
        {
            new Dictionary<string, string?> { ["Segment"] = "US" }
        };

        var result = FieldMappingEngine.MapRows(rows, source, new Dictionary<(string, string), Dictionary<string, string>>());

        Assert.Equal("BR", result[0].Values["Segment"]);
    }

    [Fact]
    public void MapRows_ResolvesLookup_WhenValueIsMapped()
    {
        var source = BuildSource(new FieldMappingConfig
        {
            Source = "ProductReference",
            Target = "IdProduct",
            Lookup = new LookupConfig { Entity = "Product", By = "Reference" }
        });
        var rows = new List<IReadOnlyDictionary<string, string?>> { new Dictionary<string, string?> { ["ProductReference"] = "ABC123" } };
        var lookups = new Dictionary<(string, string), Dictionary<string, string>>
        {
            [("Product", "Reference")] = new() { ["ABC123"] = "42" }
        };

        var result = FieldMappingEngine.MapRows(rows, source, lookups);

        Assert.Null(result[0].Error);
        Assert.Equal("42", result[0].Values["IdProduct"]);
    }

    [Fact]
    public void MapRows_ReturnsError_WhenLookupValueCannotBeResolved()
    {
        var source = BuildSource(new FieldMappingConfig
        {
            Source = "ProductReference",
            Target = "IdProduct",
            Lookup = new LookupConfig { Entity = "Product", By = "Reference" }
        });
        var rows = new List<IReadOnlyDictionary<string, string?>> { new Dictionary<string, string?> { ["ProductReference"] = "UNKNOWN" } };
        var lookups = new Dictionary<(string, string), Dictionary<string, string>>
        {
            [("Product", "Reference")] = new()
        };

        var result = FieldMappingEngine.MapRows(rows, source, lookups);

        Assert.NotNull(result[0].Error);
    }

    [Fact]
    public void MapRows_ReturnsError_WhenLookupSourceValueIsMissing()
    {
        var source = BuildSource(new FieldMappingConfig
        {
            Source = "ProductReference",
            Target = "IdProduct",
            Lookup = new LookupConfig { Entity = "Product", By = "Reference" }
        });
        var rows = new List<IReadOnlyDictionary<string, string?>> { new Dictionary<string, string?>() };

        var result = FieldMappingEngine.MapRows(rows, source, new Dictionary<(string, string), Dictionary<string, string>>());

        Assert.NotNull(result[0].Error);
    }

    [Fact]
    public void MapRows_LeavesTargetAbsent_WhenOptionalLookupSourceValueIsMissing()
    {
        var source = BuildSource(new FieldMappingConfig
        {
            Source = "OriginCenterCode",
            Target = "IdOriginCenter",
            Lookup = new LookupConfig { Entity = "Center", By = "Code" },
            Required = false
        });
        var rows = new List<IReadOnlyDictionary<string, string?>> { new Dictionary<string, string?> { ["OriginCenterCode"] = "" } };

        var result = FieldMappingEngine.MapRows(rows, source, new Dictionary<(string, string), Dictionary<string, string>>());

        Assert.Null(result[0].Error);
        Assert.False(result[0].Values.ContainsKey("IdOriginCenter"));
    }

    [Fact]
    public void MapRows_StillResolvesOptionalLookup_WhenValueIsPresent()
    {
        var source = BuildSource(new FieldMappingConfig
        {
            Source = "OriginCenterCode",
            Target = "IdOriginCenter",
            Lookup = new LookupConfig { Entity = "Center", By = "Code" },
            Required = false
        });
        var rows = new List<IReadOnlyDictionary<string, string?>> { new Dictionary<string, string?> { ["OriginCenterCode"] = "C1" } };
        var lookups = new Dictionary<(string, string), Dictionary<string, string>>
        {
            [("Center", "Code")] = new() { ["C1"] = "7" }
        };

        var result = FieldMappingEngine.MapRows(rows, source, lookups);

        Assert.Null(result[0].Error);
        Assert.Equal("7", result[0].Values["IdOriginCenter"]);
    }
}
