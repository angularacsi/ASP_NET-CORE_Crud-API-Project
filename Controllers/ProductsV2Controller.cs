using Asp.Versioning;
using Crud.Dtos;
using Crud.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Crud.Controllers
{
    [ApiVersion("2.0")]
    [Route("api/v{version:apiVersion}/products")]
    [ApiController]
    public class ProductsV2Controller : ControllerBase
    {
        private readonly IProductRepository _productRepository;

        public ProductsV2Controller(IProductRepository productRepository)
        {
            _productRepository = productRepository;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProductV2ResponseDto>>> GetAllProducts()
        {
            var products = await _productRepository.GetAllAsync();

            var response = products.Select(product => new ProductV2ResponseDto
            {
                ProductId = product.Id,
                ProductName = product.Name,
                UnitPrice = product.Price,
                Currency = "ETB",
            });

            return Ok(response);
        }
    }
}
