using NSubstitute;
using Service.Application.DTOs.Note;
using Service.Application.Exceptions;
using Service.Application.Services;
using Service.Domain.Account;
using Service.Domain.Entities;
using Service.Domain.Interfaces;

namespace Service.Tests.Application;

public class NoteServiceTests
{
    private readonly INoteRepository _noteRepository = Substitute.For<INoteRepository>();
    private readonly ICenterProductRepository _centerProductRepository = Substitute.For<ICenterProductRepository>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly NoteService _sut;

    public NoteServiceTests()
    {
        _centerProductRepository.Exists(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(true);
        _currentUser.UserId.Returns(1);
        _sut = new NoteService(_noteRepository, _centerProductRepository, _currentUser);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsDto_WhenNoteExists()
    {
        var note = new Note { Id = 1, Content = "Nota", CenterProductId = 1, createdBy = 1 };
        _noteRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(note);

        var result = await _sut.GetByIdAsync(1);

        Assert.Equal(note.Id, result.Id);
        Assert.Equal(note.Content, result.Content);
        Assert.Equal(1, result.CreatedBy);
    }

    [Fact]
    public async Task GetByIdAsync_ThrowsNotFoundException_WhenNoteDoesNotExist()
    {
        _noteRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns((Note)null!);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetByIdAsync(1));
    }

    [Fact]
    public async Task GetByIdAsync_IncludesCreatedByUser_WhenLoaded()
    {
        var note = new Note
        {
            Id = 1,
            Content = "Nota",
            CenterProductId = 1,
            createdBy = 1,
            CenterProduct = new CenterProduct { Id = 1, IdProduct = 1, IdCenter = 1 },
            CreatedByUser = new User { Id = 1, Name = "Matheus", Email = "matheus@test.com" }
        };
        _noteRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(note);

        var result = await _sut.GetByIdAsync(1);

        Assert.NotNull(result.CreatedByUser);
        Assert.Equal("Matheus", result.CreatedByUser!.Name);
    }

    [Fact]
    public async Task AddAsync_PersistsMappedEntity_WhenCenterProductExists()
    {
        var postDto = new NotePostDto { Content = "Nota", CenterProductId = 1 };
        _noteRepository.AddAsync(Arg.Any<Note>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<Note>());

        var result = await _sut.AddAsync(postDto);

        Assert.Equal("Nota", result.Content);
        await _noteRepository.Received(1).AddAsync(
            Arg.Is<Note>(n => n.Content == "Nota" && n.CenterProductId == 1),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddAsync_ThrowsBadRequestException_WhenCenterProductDoesNotExist()
    {
        var postDto = new NotePostDto { Content = "Nota", CenterProductId = 99 };
        _centerProductRepository.Exists(99, Arg.Any<CancellationToken>()).Returns(false);

        await Assert.ThrowsAsync<BadRequestException>(() => _sut.AddAsync(postDto));

        await _noteRepository.DidNotReceive().AddAsync(Arg.Any<Note>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_AppliesChanges_WhenCallerIsCreator()
    {
        var existing = new Note { Id = 1, Content = "Old", CenterProductId = 1, createdBy = 1 };
        var putDto = new NotePutDto { Content = "New" };
        _noteRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(existing);
        _noteRepository.UpdateAsync(Arg.Any<Note>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<Note>());

        var result = await _sut.UpdateAsync(1, putDto);

        Assert.Equal("New", result.Content);
    }

    [Fact]
    public async Task UpdateAsync_ThrowsHttpException_WhenCallerIsNotCreator()
    {
        var existing = new Note { Id = 1, Content = "Old", CenterProductId = 1, createdBy = 2 };
        var putDto = new NotePutDto { Content = "Hacked" };
        _noteRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(existing);

        var ex = await Assert.ThrowsAsync<HttpException>(() => _sut.UpdateAsync(1, putDto));

        Assert.Equal(403, ex.StatusCode);
        await _noteRepository.DidNotReceive().UpdateAsync(Arg.Any<Note>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_ThrowsNotFoundException_WhenNoteDoesNotExist()
    {
        _noteRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns((Note)null!);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.UpdateAsync(1, new NotePutDto { Content = "X" }));
    }

    [Fact]
    public async Task DeleteAsync_ThrowsNotFoundException_WhenNoteDoesNotExist()
    {
        _noteRepository.DeleteAsync(1, Arg.Any<CancellationToken>()).Returns((Note)null!);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.DeleteAsync(1));
    }
}
