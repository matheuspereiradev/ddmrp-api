using NSubstitute;
using Service.Application.DTOs.Reason;
using Service.Application.Exceptions;
using Service.Application.Services;
using Service.Domain.Entities;
using Service.Domain.Interfaces;

namespace Service.Tests.Application;

public class ReasonServiceTests
{
    private readonly IReasonRepository _reasonRepository = Substitute.For<IReasonRepository>();
    private readonly ReasonService _sut;

    public ReasonServiceTests()
    {
        _sut = new ReasonService(_reasonRepository);
    }

    private static ReasonPostDto BuildPostDto() => new()
    {
        Name = "Avaria",
        Description = "Produto avariado no transporte"
    };

    [Fact]
    public async Task GetByIdAsync_ReturnsDto_WhenReasonExists()
    {
        var reason = new Reason { Id = 1, Name = "Avaria", Description = "Produto avariado", IsFromSystem = true };
        _reasonRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(reason);

        var result = await _sut.GetByIdAsync(1);

        Assert.Equal(reason.Id, result.Id);
        Assert.Equal(reason.Name, result.Name);
        Assert.True(result.IsFromSystem);
    }

    [Fact]
    public async Task GetByIdAsync_ThrowsNotFoundException_WhenReasonDoesNotExist()
    {
        _reasonRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns((Reason)null!);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetByIdAsync(1));
    }

    [Fact]
    public async Task AddAsync_AlwaysPersistsWithIsFromSystemFalse()
    {
        var postDto = BuildPostDto();
        _reasonRepository.AddAsync(Arg.Any<Reason>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<Reason>());

        var result = await _sut.AddAsync(postDto);

        Assert.False(result.IsFromSystem);
        await _reasonRepository.Received(1).AddAsync(
            Arg.Is<Reason>(r => r.Name == "Avaria" && r.IsFromSystem == false),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_AppliesChanges_WhenReasonIsNotFromSystem()
    {
        var existing = new Reason { Id = 1, Name = "Old", Description = "Old desc", IsFromSystem = false };
        var putDto = new ReasonPutDto { Name = "New", Description = "New desc" };
        _reasonRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(existing);
        _reasonRepository.UpdateAsync(Arg.Any<Reason>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<Reason>());

        var result = await _sut.UpdateAsync(1, putDto);

        Assert.Equal("New", result.Name);
        Assert.Equal("New desc", result.Description);
    }

    [Fact]
    public async Task UpdateAsync_ThrowsHttpException_WhenReasonIsFromSystem()
    {
        var existing = new Reason { Id = 1, Name = "System Reason", IsFromSystem = true };
        var putDto = new ReasonPutDto { Name = "Hacked" };
        _reasonRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(existing);

        var ex = await Assert.ThrowsAsync<HttpException>(() => _sut.UpdateAsync(1, putDto));
        Assert.Equal(403, ex.StatusCode);
        await _reasonRepository.DidNotReceive().UpdateAsync(Arg.Any<Reason>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_ThrowsNotFoundException_WhenReasonDoesNotExist()
    {
        _reasonRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns((Reason)null!);

        await Assert.ThrowsAsync<NotFoundException>(
            () => _sut.UpdateAsync(1, new ReasonPutDto { Name = "X" }));
    }

    [Fact]
    public async Task DeleteAsync_DeletesReason_WhenNotFromSystem()
    {
        var existing = new Reason { Id = 1, Name = "Avaria", IsFromSystem = false };
        _reasonRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(existing);
        _reasonRepository.DeleteAsync(1, Arg.Any<CancellationToken>()).Returns(existing);

        var result = await _sut.DeleteAsync(1);

        Assert.Equal(1, result.Id);
    }

    [Fact]
    public async Task DeleteAsync_ThrowsHttpException_WhenReasonIsFromSystem()
    {
        var existing = new Reason { Id = 1, Name = "System Reason", IsFromSystem = true };
        _reasonRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(existing);

        var ex = await Assert.ThrowsAsync<HttpException>(() => _sut.DeleteAsync(1));
        Assert.Equal(403, ex.StatusCode);
        await _reasonRepository.DidNotReceive().DeleteAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_ThrowsNotFoundException_WhenReasonDoesNotExist()
    {
        _reasonRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns((Reason)null!);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.DeleteAsync(1));
    }
}
