using NSubstitute;
using Service.Application.DTOs.Tag;
using Service.Application.Exceptions;
using Service.Application.Services;
using Service.Domain.Entities;
using Service.Domain.Interfaces;

namespace Service.Tests.Application;

public class TagServiceTests
{
    private readonly ITagRepository _tagRepository = Substitute.For<ITagRepository>();
    private readonly TagService _sut;

    public TagServiceTests()
    {
        _sut = new TagService(_tagRepository);
    }

    private static TagPostDto BuildPostDto() => new()
    {
        Name = "Promoção",
        Description = "Produtos em promoção"
    };

    [Fact]
    public async Task GetByIdAsync_ReturnsDto_WhenTagExists()
    {
        var tag = new Tag { Id = 1, Name = "Promoção", Description = "Produtos em promoção" };
        _tagRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(tag);

        var result = await _sut.GetByIdAsync(1);

        Assert.Equal(tag.Id, result.Id);
        Assert.Equal(tag.Name, result.Name);
        Assert.Equal(tag.Description, result.Description);
    }

    [Fact]
    public async Task GetByIdAsync_ThrowsNotFoundException_WhenTagDoesNotExist()
    {
        _tagRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns((Tag)null!);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetByIdAsync(1));
    }

    [Fact]
    public async Task AddAsync_PersistsMappedEntity()
    {
        var postDto = BuildPostDto();
        _tagRepository.AddAsync(Arg.Any<Tag>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<Tag>());

        var result = await _sut.AddAsync(postDto);

        Assert.Equal("Promoção", result.Name);
        Assert.Equal("Produtos em promoção", result.Description);
        await _tagRepository.Received(1).AddAsync(
            Arg.Is<Tag>(t => t.Name == "Promoção"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddAsync_AllowsNullDescription()
    {
        var postDto = new TagPostDto { Name = "Sem descrição" };
        _tagRepository.AddAsync(Arg.Any<Tag>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<Tag>());

        var result = await _sut.AddAsync(postDto);

        Assert.Null(result.Description);
    }

    [Fact]
    public async Task UpdateAsync_AppliesChanges_WhenTagExists()
    {
        var existing = new Tag { Id = 1, Name = "Old", Description = "Old desc" };
        var putDto = new TagPutDto { Name = "New", Description = "New desc" };
        _tagRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(existing);
        _tagRepository.UpdateAsync(Arg.Any<Tag>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<Tag>());

        var result = await _sut.UpdateAsync(1, putDto);

        Assert.Equal("New", result.Name);
        Assert.Equal("New desc", result.Description);
    }

    [Fact]
    public async Task UpdateAsync_ThrowsNotFoundException_WhenTagDoesNotExist()
    {
        _tagRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns((Tag)null!);

        await Assert.ThrowsAsync<NotFoundException>(
            () => _sut.UpdateAsync(1, new TagPutDto { Name = "X" }));
    }

    [Fact]
    public async Task DeleteAsync_ThrowsNotFoundException_WhenTagDoesNotExist()
    {
        _tagRepository.DeleteAsync(1, Arg.Any<CancellationToken>()).Returns((Tag)null!);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.DeleteAsync(1));
    }
}
