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
    private readonly IRefreshTokenRepository _refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();
    private readonly IAuthenticate _authenticate = Substitute.For<IAuthenticate>();
    private readonly AuthenticateService _sut;

    public AuthenticateServiceTests()
    {
        _authenticate.GenerateToken(Arg.Any<int>(), Arg.Any<string>()).Returns("fake-access-token");
        _authenticate.GetTokenExpiration().Returns(DateTime.UtcNow.AddHours(1));
        _authenticate.GenerateRefreshToken().Returns("fake-refresh-token");
        _authenticate.GetRefreshTokenExpiration().Returns(DateTime.UtcNow.AddDays(7));

        _sut = new AuthenticateService(_userRepository, _refreshTokenRepository, _authenticate);
    }

    private static User BuildUser() => new()
    {
        Id = 1,
        Name = "Matheus",
        Email = "matheus@test.com",
        Password = BCrypt.Net.BCrypt.HashPassword("correct-password")
    };

    [Fact]
    public async Task AuthenticateAsync_ThrowsBadRequestException_WhenUserNotFound()
    {
        _userRepository.GetByEmail("missing@test.com", Arg.Any<CancellationToken>()).Returns((User)null!);

        await Assert.ThrowsAsync<BadRequestException>(() => _sut.AuthenticateAsync("missing@test.com", "any"));
    }

    [Fact]
    public async Task AuthenticateAsync_ThrowsBadRequestException_WhenPasswordIsWrong()
    {
        var user = BuildUser();
        _userRepository.GetByEmail(user.Email, Arg.Any<CancellationToken>()).Returns(user);

        await Assert.ThrowsAsync<BadRequestException>(() => _sut.AuthenticateAsync(user.Email, "wrong-password"));
    }

    [Fact]
    public async Task AuthenticateAsync_ReturnsTokens_WhenCredentialsAreValid()
    {
        var user = BuildUser();
        _userRepository.GetByEmail(user.Email, Arg.Any<CancellationToken>()).Returns(user);

        var result = await _sut.AuthenticateAsync(user.Email, "correct-password");

        Assert.Equal(user.Id, result.Id);
        Assert.Equal(user.Name, result.Name);
        Assert.Equal("fake-access-token", result.AccessToken);
        Assert.Equal("fake-refresh-token", result.RefreshToken);
        await _refreshTokenRepository.Received(1).AddAsync(
            Arg.Is<RefreshToken>(r => r.Token == "fake-refresh-token" && r.UserId == user.Id),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RefreshTokenAsync_ReturnsNewTokens_AndRevokesOldOne_WhenTokenIsActive()
    {
        var user = BuildUser();
        var existing = new RefreshToken { Id = 1, Token = "old-token", UserId = user.Id, User = user, ExpiresAt = DateTime.UtcNow.AddDays(1) };
        _refreshTokenRepository.GetByTokenAsync("old-token", Arg.Any<CancellationToken>()).Returns(existing);

        var result = await _sut.RefreshTokenAsync("old-token");

        Assert.Equal("fake-access-token", result.AccessToken);
        Assert.Equal("fake-refresh-token", result.RefreshToken);
        await _refreshTokenRepository.Received(1).UpdateAsync(
            Arg.Is<RefreshToken>(r => r.Token == "old-token" && r.RevokedAt != null && r.ReplacedByToken == "fake-refresh-token"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RefreshTokenAsync_ThrowsUnauthorizedAccessException_WhenTokenDoesNotExist()
    {
        _refreshTokenRepository.GetByTokenAsync("missing", Arg.Any<CancellationToken>()).Returns((RefreshToken)null!);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _sut.RefreshTokenAsync("missing"));
    }

    [Fact]
    public async Task RefreshTokenAsync_ThrowsUnauthorizedAccessException_WhenTokenIsExpired()
    {
        var user = BuildUser();
        var expired = new RefreshToken { Id = 1, Token = "expired-token", UserId = user.Id, User = user, ExpiresAt = DateTime.UtcNow.AddDays(-1) };
        _refreshTokenRepository.GetByTokenAsync("expired-token", Arg.Any<CancellationToken>()).Returns(expired);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _sut.RefreshTokenAsync("expired-token"));
    }

    [Fact]
    public async Task RefreshTokenAsync_ThrowsUnauthorizedAccessException_WhenTokenIsRevoked()
    {
        var user = BuildUser();
        var revoked = new RefreshToken { Id = 1, Token = "revoked-token", UserId = user.Id, User = user, ExpiresAt = DateTime.UtcNow.AddDays(1), RevokedAt = DateTime.UtcNow };
        _refreshTokenRepository.GetByTokenAsync("revoked-token", Arg.Any<CancellationToken>()).Returns(revoked);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _sut.RefreshTokenAsync("revoked-token"));
    }

    [Fact]
    public async Task RevokeTokenAsync_RevokesToken_WhenItBelongsToUserAndIsActive()
    {
        var existing = new RefreshToken { Id = 1, Token = "my-token", UserId = 1, ExpiresAt = DateTime.UtcNow.AddDays(1) };
        _refreshTokenRepository.GetByTokenAsync("my-token", Arg.Any<CancellationToken>()).Returns(existing);

        await _sut.RevokeTokenAsync(1, "my-token");

        await _refreshTokenRepository.Received(1).UpdateAsync(
            Arg.Is<RefreshToken>(r => r.Token == "my-token" && r.RevokedAt != null),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RevokeTokenAsync_ThrowsBadRequestException_WhenTokenBelongsToAnotherUser()
    {
        var existing = new RefreshToken { Id = 1, Token = "someone-elses-token", UserId = 2, ExpiresAt = DateTime.UtcNow.AddDays(1) };
        _refreshTokenRepository.GetByTokenAsync("someone-elses-token", Arg.Any<CancellationToken>()).Returns(existing);

        await Assert.ThrowsAsync<BadRequestException>(() => _sut.RevokeTokenAsync(1, "someone-elses-token"));

        await _refreshTokenRepository.DidNotReceive().UpdateAsync(Arg.Any<RefreshToken>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RevokeTokenAsync_ThrowsBadRequestException_WhenTokenDoesNotExist()
    {
        _refreshTokenRepository.GetByTokenAsync("missing", Arg.Any<CancellationToken>()).Returns((RefreshToken)null!);

        await Assert.ThrowsAsync<BadRequestException>(() => _sut.RevokeTokenAsync(1, "missing"));
    }
}
