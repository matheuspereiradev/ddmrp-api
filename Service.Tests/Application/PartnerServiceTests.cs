using NSubstitute;
using Service.Application.DTOs.Partner;
using Service.Application.Exceptions;
using Service.Application.Services;
using Service.Domain.Entities;
using Service.Domain.Interfaces;

namespace Service.Tests.Application;

public class PartnerServiceTests
{
    private readonly IPartnerRepository _partnerRepository = Substitute.For<IPartnerRepository>();
    private readonly PartnerService _sut;

    public PartnerServiceTests()
    {
        _sut = new PartnerService(_partnerRepository);
    }

    private static PartnerPostDto BuildPostDto() => new()
    {
        Code = "P001",
        Description = "Fornecedor Teste"
    };

    [Fact]
    public async Task GetByIdAsync_ReturnsDto_WhenPartnerExists()
    {
        var partner = new Partner { Id = 1, Code = "P001", Description = "Fornecedor Teste" };
        _partnerRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(partner);

        var result = await _sut.GetByIdAsync(1);

        Assert.Equal(partner.Id, result.Id);
        Assert.Equal(partner.Code, result.Code);
        Assert.Equal(partner.Description, result.Description);
    }

    [Fact]
    public async Task GetByIdAsync_ThrowsNotFoundException_WhenPartnerDoesNotExist()
    {
        _partnerRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns((Partner)null!);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetByIdAsync(1));
    }

    [Fact]
    public async Task AddAsync_PersistsMappedEntity()
    {
        var postDto = BuildPostDto();
        _partnerRepository.AddAsync(Arg.Any<Partner>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<Partner>());

        var result = await _sut.AddAsync(postDto);

        Assert.Equal("P001", result.Code);
        Assert.Equal("Fornecedor Teste", result.Description);
        await _partnerRepository.Received(1).AddAsync(
            Arg.Is<Partner>(p => p.Code == "P001" && p.Description == "Fornecedor Teste"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_AppliesChanges_WhenPartnerExists()
    {
        var existing = new Partner { Id = 1, Code = "P001", Description = "Old" };
        var putDto = new PartnerPutDto { Code = "P002", Description = "New" };
        _partnerRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(existing);
        _partnerRepository.UpdateAsync(Arg.Any<Partner>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<Partner>());

        var result = await _sut.UpdateAsync(1, putDto);

        Assert.Equal("P002", result.Code);
        Assert.Equal("New", result.Description);
    }

    [Fact]
    public async Task UpdateAsync_ThrowsNotFoundException_WhenPartnerDoesNotExist()
    {
        _partnerRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns((Partner)null!);

        await Assert.ThrowsAsync<NotFoundException>(
            () => _sut.UpdateAsync(1, new PartnerPutDto { Code = "X", Description = "Y" }));
    }

    [Fact]
    public async Task DeleteAsync_ThrowsNotFoundException_WhenPartnerDoesNotExist()
    {
        _partnerRepository.DeleteAsync(1, Arg.Any<CancellationToken>()).Returns((Partner)null!);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.DeleteAsync(1));
    }
}
