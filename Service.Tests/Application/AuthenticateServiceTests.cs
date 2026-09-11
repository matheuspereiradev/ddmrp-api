using NSubstitute;
using Service.Application.Exceptions;
using Service.Application.Services;
using Service.Domain.Account;
using Service.Domain.Entities;
using Service.Domain.Interfaces;

namespace Service.Tests.Application;

public class AuthenticateServiceTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IAuthenticate _authenticate = Substitute.For<IAuthenticate>();
    private readonly AuthenticateService _sut;

    public AuthenticateServiceTests()
    {
        _sut = new AuthenticateService(_userRepository, _authenticate);
    }

    [Fact]
    public async Task AuthenticateAsync_ThrowsBadRequestException_WhenUserNotFound()
    {
        _userRepository.GetByEmail("missing@test.com", Arg.Any<CancellationToken>()).Returns((User)null!);

        await Assert.ThrowsAsync<BadRequestException>(() => _sut.AuthenticateAsync("missing@test.com", "any"));
    }

    [Fact]
    public async Task AuthenticateAsync_ThrowsBadRequestException_WhenPasswordIsWrong()
    {
        var user = new User
        {
            Id = 1,
            Name = "Matheus",
            Email = "matheus@test.com",
            Password = BCrypt.Net.BCrypt.HashPassword("correct-password")
        };
        _userRepository.GetByEmail(user.Email, Arg.Any<CancellationToken>()).Returns(user);

        await Assert.ThrowsAsync<BadRequestException>(() => _sut.AuthenticateAsync(user.Email, "wrong-password"));
    }

    [Fact]
    public async Task AuthenticateAsync_ReturnsDto_WhenCredentialsAreValid()
    {
        var user = new User
        {
            Id = 1,
            Name = "Matheus",
            Email = "matheus@test.com",
            Password = BCrypt.Net.BCrypt.HashPassword("correct-password")
        };
        _userRepository.GetByEmail(user.Email, Arg.Any<CancellationToken>()).Returns(user);

        var result = await _sut.AuthenticateAsync(user.Email, "correct-password");

        Assert.Equal(user.Id, result.Id);
        Assert.Equal(user.Name, result.Name);
    }

    [Fact]
    public void GenerateToken_DelegatesToAuthenticateProvider()
    {
        _authenticate.GenerateToken(1, "matheus@test.com").Returns("fake-token");

        var token = _sut.GenerateToken(1, "matheus@test.com");

        Assert.Equal("fake-token", token);
    }
}
