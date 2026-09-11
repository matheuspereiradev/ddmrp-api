using Service.API.Extensions;
using Service.API.Models;
using Service.Application.DTOs.Role;
using Service.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Service.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RoleController : Controller
    {
        private readonly IRoleService _roleService;

        public RoleController(IRoleService roleService)
        {
            _roleService = roleService;
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult> CreateRole(RolePostDto rolePostDto, CancellationToken cancellationToken)
        {
            var role = await _roleService.AddAsync(rolePostDto, cancellationToken);
            return Ok(role);
        }

        [HttpGet]
        [Authorize]
        public async Task<ActionResult> GetAllRoles([FromQuery] PaginationParams paginationParams, CancellationToken cancellationToken)
        {
            var roles = await _roleService.GetAllAsync(paginationParams.PageNumber, paginationParams.PageSize, cancellationToken);

            Response.AddPaginationHeader(
                new PaginationHeader(paginationParams.PageNumber, paginationParams.PageSize, roles.TotalCount, roles.TotalPages));

            return Ok(roles);
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<ActionResult> UpdateRole(int id, RolePutDto rolePutDto, CancellationToken cancellationToken)
        {
            var role = await _roleService.UpdateAsync(id, rolePutDto, cancellationToken);
            return Ok(role);
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<ActionResult> DeleteRole(int id, CancellationToken cancellationToken)
        {
            var role = await _roleService.DeleteAsync(id, cancellationToken);
            return Ok(role);
        }
    }
}
