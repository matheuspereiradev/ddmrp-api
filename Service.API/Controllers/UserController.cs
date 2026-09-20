using Service.API.Extensions;
using Service.API.Models;
using Service.Application.DTOs.Auth;
using Service.Application.DTOs.User;
using Service.Application.Interfaces;
using Service.Infra.Ioc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Service.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : Controller
    {
        private readonly IUserService _userService;
        private readonly IAuthenticateService _authenticate;

        public UserController(IUserService userService, IAuthenticateService authenticate)
        {
            _userService = userService;
            _authenticate = authenticate;
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult> CreateUser(UserPostDto userPostDto, CancellationToken cancellationToken)
        {
            var user = await _userService.AddAsync(userPostDto, cancellationToken);
            return Ok(user);
        }

        [HttpGet]
        [Authorize]
        public async Task<ActionResult> GetAllUsers([FromQuery] PaginationParams paginationParams, CancellationToken cancellationToken)
        {
            var users = await _userService.GetAllAsync(paginationParams.PageNumber, paginationParams.PageSize, cancellationToken);

            Response.AddPaginationHeader(
                new PaginationHeader(paginationParams.PageNumber, paginationParams.PageSize, users.TotalCount, users.TotalPages));

            return Ok(users);
        }

        [HttpPost("login")]
        public async Task<ActionResult> GetTokenUser(UserLogin userLogin, CancellationToken cancellationToken)
        {
            var result = await _authenticate.AuthenticateAsync(userLogin.Email, userLogin.Password, cancellationToken);
            return Ok(result);
        }

        [HttpPost("refresh-token")]
        public async Task<ActionResult> RefreshToken(RefreshTokenRequestDto refreshTokenRequestDto, CancellationToken cancellationToken)
        {
            var result = await _authenticate.RefreshTokenAsync(refreshTokenRequestDto.RefreshToken, cancellationToken);
            return Ok(result);
        }

        [HttpPost("revoke-token")]
        [Authorize]
        public async Task<ActionResult> RevokeToken(RefreshTokenRequestDto refreshTokenRequestDto, CancellationToken cancellationToken)
        {
            await _authenticate.RevokeTokenAsync(User.GetUserId(), refreshTokenRequestDto.RefreshToken, cancellationToken);
            return Ok();
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<ActionResult> UpdateUser(int id, UserPutDto userPutDto, CancellationToken cancellationToken)
        {
            var user = await _userService.UpdateAsync(id, userPutDto, cancellationToken);
            return Ok(user);
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<ActionResult> DeleteUser(int id, CancellationToken cancellationToken)
        {
            var user = await _userService.DeleteAsync(id, cancellationToken);
            return Ok(user);
        }
    }
}