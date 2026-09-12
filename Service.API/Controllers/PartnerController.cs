using Service.API.Extensions;
using Service.API.Models;
using Service.Application.DTOs.Partner;
using Service.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Service.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PartnerController : Controller
    {
        private readonly IPartnerService _partnerService;

        public PartnerController(IPartnerService partnerService)
        {
            _partnerService = partnerService;
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult> CreatePartner(PartnerPostDto partnerPostDto, CancellationToken cancellationToken)
        {
            var partner = await _partnerService.AddAsync(partnerPostDto, cancellationToken);
            return Ok(partner);
        }

        [HttpGet]
        [Authorize]
        public async Task<ActionResult> GetAllPartners([FromQuery] PaginationParams paginationParams, CancellationToken cancellationToken)
        {
            var partners = await _partnerService.GetAllAsync(paginationParams.PageNumber, paginationParams.PageSize, cancellationToken);

            Response.AddPaginationHeader(
                new PaginationHeader(paginationParams.PageNumber, paginationParams.PageSize, partners.TotalCount, partners.TotalPages));

            return Ok(partners);
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<ActionResult> UpdatePartner(int id, PartnerPutDto partnerPutDto, CancellationToken cancellationToken)
        {
            var partner = await _partnerService.UpdateAsync(id, partnerPutDto, cancellationToken);
            return Ok(partner);
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<ActionResult> DeletePartner(int id, CancellationToken cancellationToken)
        {
            var partner = await _partnerService.DeleteAsync(id, cancellationToken);
            return Ok(partner);
        }
    }
}
