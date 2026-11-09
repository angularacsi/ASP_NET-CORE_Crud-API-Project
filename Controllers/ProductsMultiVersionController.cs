using Asp.Versioning;
using Crud.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Crud.Controllers
{
    [ApiVersion("1.0")]
    [ApiVersion("2.0")]
    [Route("api/v{version:apiVersion}/products-multi")]
    [ApiController]
    public class ProductsMultiVersionController : ControllerBase
    {
        [HttpGet]
        [MapToApiVersion("1.0")]
        public IActionResult GetProductsV1()
        {
            var response = new ProductResponseDto
            {
                Id = 1,
                Name = "Laptop",
                Description = "Dell Laptop",
                Price = 50000,
                StockQuantity = 10,
                Category = "Electronics",
                IsActive = true,
                CreatedOn = DateTime.UtcNow,
            };

            return Ok(response);
        }

        [HttpGet]
        [MapToApiVersion("2.0")]
        public IActionResult GetProductsV2()
        {
            var response = new ProductV2ResponseDto
            {
                ProductId = 1,
                ProductName = "Laptop",
                UnitPrice = 50000,
                Currency = "ETB",
            };

            return Ok(response);
        }
    }
}
