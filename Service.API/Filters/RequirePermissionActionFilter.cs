using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Service.Application.Exceptions;
using Service.Application.Interfaces;
using Service.Domain.Utils;
using Service.Infra.Ioc;

namespace Service.API.Filters
{
    public class RequirePermissionActionFilter : IAsyncActionFilter
    {
        private readonly IPermissionService _permissionService;

        public RequirePermissionActionFilter(IPermissionService permissionService)
        {
            _permissionService = permissionService;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var requiresPermission = context.ActionDescriptor.EndpointMetadata
                .Any(m => m is RequirePermissionAttribute);

            if (!requiresPermission)
            {
                await next();
                return;
            }

            if (context.ActionDescriptor is not ControllerActionDescriptor controllerActionDescriptor
                || controllerActionDescriptor.AttributeRouteInfo?.Template is not string routeTemplate)
                throw new HttpException("Route has no attribute route template to derive a permission key from.", StatusCodes.Status500InternalServerError);

            var permissionKey = PermissionKeyUtils.BuildKey(routeTemplate, context.HttpContext.Request.Method);
            var idRole = context.HttpContext.User.GetRoleId();
            var permissionKeys = await _permissionService.GetPermissionKeysForRoleAsync(idRole, context.HttpContext.RequestAborted);

            if (!permissionKeys.Contains(permissionKey))
                throw new HttpException("You don't have permission to perform this action.", StatusCodes.Status403Forbidden);

            await next();
        }
    }
}
