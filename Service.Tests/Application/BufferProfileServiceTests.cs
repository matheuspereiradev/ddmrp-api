using NSubstitute;
using Service.Application.DTOs.BufferProfile;
using Service.Application.Exceptions;
using Service.Application.Services;
using Service.Domain.Entities;
using Service.Domain.Enums;
using Service.Domain.Interfaces;

namespace Service.Tests.Application;

public class BufferProfileServiceTests
{
    private readonly IBufferProfileRepository _bufferProfileRepository = Substitute.For<IBufferProfileRepository>();
    private readonly BufferProfileService _sut;

    public BufferProfileServiceTests()
    {
        _sut = new BufferProfileService(_bufferProfileRepository);
    }

    private static BufferProfilePostDto BuildPostDto() => new()
    {
        ProfileName = "P1",
        SupplyType = SupplyType.Distributed,
        LeadTimeCategory = LeadTimeCategory.Medium,
        VariabilityCategory = VariabilityCategory.Low,
        LeadTimeFactor = 1.5m,
        VariabilityFactor = 2.5m,
        AduCalculationDays = 30,
        AduFutureDays = 15,
        Frequency = 7,
        GreenZoneParametrizationUseAduXLeadTimeXFactLeadTime = true,
        GreenZoneParametrizationUseMoq = false,
        GreenZoneParametrizationUseAduXFrequency = true,
        SpikeHorizonType = SpikeHorizonType.Dlt,
        SpikeHorizonValue = 10,
        SpikeHorizonLTDays = 5,
        SpikeThresholdType = SpikeThresholdType.Adu,
        SpikeThresholdAdu = 3,
        SpikeThresholdPercentageRedZone = 50m,
        IsActive = true,
        IsMakeToOrder = false
    };

    [Fact]
    public async Task GetByIdAsync_ReturnsDto_WhenBufferProfileExists()
    {
        var entity = new BufferProfile { Id = 1, ProfileName = "P1", SupplyType = SupplyType.Purchased };
        _bufferProfileRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(entity);

        var result = await _sut.GetByIdAsync(1);

        Assert.Equal(entity.Id, result.Id);
        Assert.Equal(SupplyType.Purchased, result.SupplyType);
    }

    [Fact]
    public async Task GetByIdAsync_ThrowsNotFoundException_WhenBufferProfileDoesNotExist()
    {
        _bufferProfileRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns((BufferProfile)null!);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetByIdAsync(1));
    }

    [Fact]
    public async Task AddAsync_PersistsMappedEntity()
    {
        var postDto = BuildPostDto();
        _bufferProfileRepository.AddAsync(Arg.Any<BufferProfile>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<BufferProfile>());

        var result = await _sut.AddAsync(postDto);

        Assert.Equal("P1", result.ProfileName);
        Assert.Equal(SupplyType.Distributed, result.SupplyType);
        Assert.Equal(SpikeHorizonType.Dlt, result.SpikeHorizonType);
        Assert.Equal(SpikeThresholdType.Adu, result.SpikeThresholdType);
        await _bufferProfileRepository.Received(1).AddAsync(
            Arg.Is<BufferProfile>(b => b.ProfileName == "P1" && b.LeadTimeCategory == LeadTimeCategory.Medium),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_AppliesChanges_WhenBufferProfileExists()
    {
        var existing = new BufferProfile { Id = 1, ProfileName = "Old", SupplyType = SupplyType.Distributed };
        var putDto = new BufferProfilePutDto
        {
            ProfileName = "New1",
            SupplyType = SupplyType.Manufactured,
            LeadTimeCategory = LeadTimeCategory.Long,
            VariabilityCategory = VariabilityCategory.High,
            LeadTimeFactor = 3m,
            VariabilityFactor = 4m,
            AduCalculationDays = 10,
            AduFutureDays = 5,
            Frequency = 3,
            SpikeHorizonType = SpikeHorizonType.Days,
            SpikeHorizonValue = 20,
            SpikeHorizonLTDays = 8,
            SpikeThresholdType = SpikeThresholdType.PlanningRedZone,
            SpikeThresholdAdu = 6,
            SpikeThresholdPercentageRedZone = 80m,
            IsActive = false,
            IsMakeToOrder = true
        };
        _bufferProfileRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(existing);
        _bufferProfileRepository.UpdateAsync(Arg.Any<BufferProfile>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<BufferProfile>());

        var result = await _sut.UpdateAsync(1, putDto);

        Assert.Equal("New1", result.ProfileName);
        Assert.Equal(SupplyType.Manufactured, result.SupplyType);
        Assert.True(result.IsMakeToOrder);
        Assert.False(result.IsActive);
    }

    [Fact]
    public async Task UpdateAsync_ThrowsNotFoundException_WhenBufferProfileDoesNotExist()
    {
        _bufferProfileRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns((BufferProfile)null!);

        await Assert.ThrowsAsync<NotFoundException>(
            () => _sut.UpdateAsync(1, new BufferProfilePutDto { ProfileName = "X" }));
    }

    [Fact]
    public async Task DeleteAsync_ThrowsNotFoundException_WhenBufferProfileDoesNotExist()
    {
        _bufferProfileRepository.DeleteAsync(1, Arg.Any<CancellationToken>()).Returns((BufferProfile)null!);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.DeleteAsync(1));
    }

    [Fact]
    public async Task SetActiveAsync_UpdatesOnlyIsActive_WhenBufferProfileExists()
    {
        var existing = new BufferProfile { Id = 1, ProfileName = "P1", IsActive = true };
        _bufferProfileRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(existing);
        _bufferProfileRepository.UpdateAsync(Arg.Any<BufferProfile>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<BufferProfile>());

        var result = await _sut.SetActiveAsync(1, false);

        Assert.False(result.IsActive);
        Assert.Equal("P1", result.ProfileName);
    }

    [Fact]
    public async Task SetActiveAsync_ThrowsNotFoundException_WhenBufferProfileDoesNotExist()
    {
        _bufferProfileRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns((BufferProfile)null!);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.SetActiveAsync(1, true));
    }
}
