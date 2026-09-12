using Service.API.Extensions;
using Service.API.Models;
using Service.Application.DTOs.CenterProduct;
using Service.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Service.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CenterProductController : Controller
    {
        private readonly ICenterProductService _centerProductService;

        public CenterProductController(ICenterProductService centerProductService)
        {
            _centerProductService = centerProductService;
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult> CreateCenterProduct(CenterProductPostDto centerProductPostDto, CancellationToken cancellationToken)
        {
            var centerProduct = await _centerProductService.AddAsync(centerProductPostDto, cancellationToken);
            return Ok(centerProduct);
        }

        [HttpGet]
        [Authorize]
        public async Task<ActionResult> GetAllCenterProducts([FromQuery] PaginationParams paginationParams, CancellationToken cancellationToken)
        {
            var centerProducts = await _centerProductService.GetAllAsync(paginationParams.PageNumber, paginationParams.PageSize, cancellationToken);

            Response.AddPaginationHeader(
                new PaginationHeader(paginationParams.PageNumber, paginationParams.PageSize, centerProducts.TotalCount, centerProducts.TotalPages));

            return Ok(centerProducts);
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<ActionResult> UpdateCenterProduct(int id, CenterProductPutDto centerProductPutDto, CancellationToken cancellationToken)
        {
            var centerProduct = await _centerProductService.UpdateAsync(id, centerProductPutDto, cancellationToken);
            return Ok(centerProduct);
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<ActionResult> DeleteCenterProduct(int id, CancellationToken cancellationToken)
        {
            var centerProduct = await _centerProductService.DeleteAsync(id, cancellationToken);
            return Ok(centerProduct);
        }
    }
}
