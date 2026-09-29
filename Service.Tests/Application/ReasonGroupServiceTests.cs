using NSubstitute;
using Service.Application.Services;
using Service.Domain.Entities;
using Service.Domain.Interfaces;
using Service.Domain.Pagination;

namespace Service.Tests.Application;

public class ReasonGroupServiceTests
{
    private readonly IReasonGroupRepository _reasonGroupRepository = Substitute.For<IReasonGroupRepository>();
    private readonly ReasonGroupService _sut;

    public ReasonGroupServiceTests()
    {
        _sut = new ReasonGroupService(_reasonGroupRepository);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsMappedPagedList()
    {
        var groups = new List<ReasonGroup>
        {
            new() { Id = 1, Name = "Qualidade", IsFromSystem = true },
            new() { Id = 2, Name = "Logística", IsFromSystem = false }
        };
        _reasonGroupRepository.GetAllAsync(1, 10, Arg.Any<CancellationToken>())
            .Returns(new PagedList<ReasonGroup>(groups, 1, 10, 2));

        var result = await _sut.GetAllAsync(1, 10);

        Assert.Equal(2, result.TotalCount);
        Assert.Equal("Qualidade", result[0].Name);
        Assert.True(result[0].IsFromSystem);
        Assert.False(result[1].IsFromSystem);
    }
}
