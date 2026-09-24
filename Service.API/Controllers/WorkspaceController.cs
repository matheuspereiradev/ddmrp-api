using Service.API.Filters;
using Service.Application.DTOs.Workspace;
using Service.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Service.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WorkspaceController : Controller
    {
        private readonly IWorkspaceService _workspaceService;

        public WorkspaceController(IWorkspaceService workspaceService)
        {
            _workspaceService = workspaceService;
        }

        [HttpPut]
        [Authorize]
        [RequirePermission]
        public async Task<ActionResult> UpdateWorkspace(WorkspaceUpdateDto workspaceUpdateDto, CancellationToken cancellationToken)
        {
            var workspace = await _workspaceService.UpdateWorkspaceAsync(workspaceUpdateDto, cancellationToken);
            return Ok(workspace);
        }

        [HttpDelete]
        [Authorize]
        [RequirePermission]
        public async Task<ActionResult> ClearWorkspace(CancellationToken cancellationToken)
        {
            await _workspaceService.ClearWorkspaceAsync(cancellationToken);
            return Ok();
        }
    }
}
