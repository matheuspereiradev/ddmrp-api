using Service.API.Extensions;
using Service.API.Filters;
using Service.API.Models;
using Service.Application.DTOs.Auth;
using Service.Application.DTOs.User;
using Service.Application.DTOs.UserCenter;
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
        private readonly IUserCenterService _userCenterService;

        public UserController(IUserService userService, IAuthenticateService authenticate, IUserCenterService userCenterService)
        {
            _userService = userService;
            _authenticate = authenticate;
            _userCenterService = userCenterService;
        }

        [HttpPost]
        [Authorize]
        [RequirePermission]
        public async Task<ActionResult> CreateUser(UserPostDto userPostDto, CancellationToken cancellationToken)
        {
            var user = await _userService.AddAsync(userPostDto, cancellationToken);
            return Ok(user);
        }

        [HttpGet]
        [Authorize]
        [RequirePermission]
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
        [RequirePermission]
        public async Task<ActionResult> UpdateUser(int id, UserPutDto userPutDto, CancellationToken cancellationToken)
        {
            var user = await _userService.UpdateAsync(id, userPutDto, cancellationToken);
            return Ok(user);
        }

        [HttpDelete("{id}")]
        [Authorize]
        [RequirePermission]
        public async Task<ActionResult> DeleteUser(int id, CancellationToken cancellationToken)
        {
            var user = await _userService.DeleteAsync(id, cancellationToken);
            return Ok(user);
        }

        [HttpGet("me/centers")]
        [Authorize]
        public async Task<ActionResult> GetMyCenters(CancellationToken cancellationToken)
        {
            var centers = await _userCenterService.GetAccessibleAsync(User.GetUserId(), cancellationToken);
            return Ok(centers);
        }

        [HttpGet("{id:int}/centers")]
        [Authorize]
        [RequirePermission("user:GET")]
        public async Task<ActionResult> GetUserCenters(int id, CancellationToken cancellationToken)
        {
            var centers = await _userCenterService.GetAssignedAsync(id, cancellationToken);
            return Ok(centers);
        }

        [HttpPut("{id:int}/centers")]
        [Authorize]
        [RequirePermission("user:PUT")]
        public async Task<ActionResult> SetUserCenters(int id, UserCentersPutDto userCentersPutDto, CancellationToken cancellationToken)
        {
            var centers = await _userCenterService.SetCentersAsync(id, userCentersPutDto, cancellationToken);
            return Ok(centers);
        }

        [HttpPatch("{id:int}/all-centers-access")]
        [Authorize]
        [RequirePermission("user:PUT")]
        public async Task<ActionResult> SetAllCentersAccess(int id, SetAllCentersAccessDto setAllCentersAccessDto, CancellationToken cancellationToken)
        {
            var centers = await _userCenterService.SetAllCentersAccessAsync(id, setAllCentersAccessDto.HasAllCentersAccess!.Value, cancellationToken);
            return Ok(centers);
        }

        [HttpPut("password")]
        [Authorize]
        public async Task<ActionResult> ChangePassword(ChangePasswordDto changePasswordDto, CancellationToken cancellationToken)
        {
            await _userService.ChangePasswordAsync(User.GetUserId(), changePasswordDto, cancellationToken);
            return Ok();
        }
    }
}