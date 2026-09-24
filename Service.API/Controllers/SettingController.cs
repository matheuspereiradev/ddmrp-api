using Service.API.Filters;
using Service.Application.DTOs.Setting;
using Service.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Service.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SettingController : Controller
    {
        private readonly ISettingService _settingService;

        public SettingController(ISettingService settingService)
        {
            _settingService = settingService;
        }

        [HttpGet]
        [Authorize]
        public async Task<ActionResult> Get(CancellationToken cancellationToken)
        {
            var setting = await _settingService.GetAsync(cancellationToken);
            return Ok(setting);
        }

        [HttpPut]
        [Authorize]
        [RequirePermission]
        public async Task<ActionResult> Update(SettingPutDto settingPutDto, CancellationToken cancellationToken)
        {
            var setting = await _settingService.UpdateWorkingDaysAsync(settingPutDto, cancellationToken);
            return Ok(setting);
        }
    }
}
