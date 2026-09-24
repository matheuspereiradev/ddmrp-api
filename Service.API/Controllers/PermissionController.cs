using Service.API.Filters;
using Service.Application.DTOs.Permission;
using Service.Application.Interfaces;
using Service.Domain.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace Service.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PermissionController : Controller
    {
        private readonly IPermissionService _permissionService;
        private readonly IActionDescriptorCollectionProvider _actionDescriptorCollectionProvider;

        public PermissionController(IPermissionService permissionService, IActionDescriptorCollectionProvider actionDescriptorCollectionProvider)
        {
            _permissionService = permissionService;
            _actionDescriptorCollectionProvider = actionDescriptorCollectionProvider;
        }

        [HttpGet]
        [Authorize]
        public async Task<ActionResult> GetAll(CancellationToken cancellationToken)
        {
            var permissions = await _permissionService.GetAllAsync(cancellationToken);
            return Ok(permissions);
        }

        [HttpGet("role/{idRole}")]
        [Authorize]
        public async Task<ActionResult> GetByRole(int idRole, CancellationToken cancellationToken)
        {
            var permissions = await _permissionService.GetByRoleAsync(idRole, cancellationToken);
            return Ok(permissions);
        }

        [HttpPut("role/{idRole}")]
        [Authorize]
        public async Task<ActionResult> ReplaceRolePermissions(int idRole, ReplaceRolePermissionsDto dto, CancellationToken cancellationToken)
        {
            await _permissionService.ReplaceRolePermissionsAsync(idRole, dto, cancellationToken);
            return Ok();
        }

        [HttpGet("discover")]
        [Authorize]
        public ActionResult Discover()
        {
            var keys = new List<string>();

            foreach (var descriptor in _actionDescriptorCollectionProvider.ActionDescriptors.Items.OfType<ControllerActionDescriptor>())
            {
                if (descriptor.AttributeRouteInfo?.Template is not string template)
                    continue;

                if (!descriptor.EndpointMetadata.Any(m => m is RequirePermissionAttribute))
                    continue;

                var httpMethods = descriptor.ActionConstraints?.OfType<HttpMethodActionConstraint>()
                    .SelectMany(c => c.HttpMethods) ?? Enumerable.Empty<string>();

                foreach (var httpMethod in httpMethods)
                    keys.Add(PermissionKeyUtils.BuildKey(template, httpMethod));
            }

            return Ok(keys.Distinct().OrderBy(k => k).ToList());
        }
    }
}
