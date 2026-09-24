using Service.API.Extensions;
using Service.API.Filters;
using Service.API.Models;
using Service.Application.DTOs.Product;
using Service.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Service.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductController : Controller
    {
        private readonly IProductService _productService;

        public ProductController(IProductService productService)
        {
            _productService = productService;
        }

        [HttpPost]
        [Authorize]
        [RequirePermission]
        public async Task<ActionResult> CreateProduct(ProductPostDto productPostDto, CancellationToken cancellationToken)
        {
            var product = await _productService.AddAsync(productPostDto, cancellationToken);
            return Ok(product);
        }

        [HttpGet]
        [Authorize]
        [RequirePermission]
        public async Task<ActionResult> GetAllProducts([FromQuery] int? idCenter, [FromQuery] PaginationParams paginationParams, CancellationToken cancellationToken)
        {
            var products = idCenter.HasValue
                ? await _productService.GetByCenterAsync(idCenter.Value, paginationParams.PageNumber, paginationParams.PageSize, cancellationToken)
                : await _productService.GetAllAsync(paginationParams.PageNumber, paginationParams.PageSize, cancellationToken);

            Response.AddPaginationHeader(
                new PaginationHeader(paginationParams.PageNumber, paginationParams.PageSize, products.TotalCount, products.TotalPages));

            return Ok(products);
        }

        [HttpPut("{id}")]
        [Authorize]
        [RequirePermission]
        public async Task<ActionResult> UpdateProduct(int id, ProductPutDto productPutDto, CancellationToken cancellationToken)
        {
            var product = await _productService.UpdateAsync(id, productPutDto, cancellationToken);
            return Ok(product);
        }

        [HttpDelete("{id}")]
        [Authorize]
        [RequirePermission]
        public async Task<ActionResult> DeleteProduct(int id, CancellationToken cancellationToken)
        {
            var product = await _productService.DeleteAsync(id, cancellationToken);
            return Ok(product);
        }
    }
}
