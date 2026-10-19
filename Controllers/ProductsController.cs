using Crud.Data;
using Crud.Dtos;
using Crud.Interfaces;
using Crud.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Crud.Controllers
{
    [Authorize(Roles = "Admin")]
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly IProductRepository _productRepository;
        private readonly ILogger<ProductsController> _logger;
        private readonly APiDbContext _aPiDb;

        public ProductsController(APiDbContext aPiDbContext, IProductRepository productRepository, ILogger<ProductsController> logger)
        {
            _aPiDb = aPiDbContext;
            _productRepository = productRepository;
            _logger = logger;
        }
        [Authorize(Policy = "CanViewProducts")]
        [HttpGet]

        public async Task<ActionResult<IEnumerable<ProductResponseDto>>> GetAllProducts()
        {
            var products = await _productRepository.GetAllAsync();
            return Ok(products);

        }
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetProductById(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            return Ok(product);
        }
        [HttpPost]
        public async Task<ActionResult<ProductResponseDto>> CreateProduct(ProductCreateDto productDto)
        {
            try
            {
                _logger.LogInformation("Creating a new product");

                // Map ProductCreateDto to Product model
                var product = new Product
                {
                    Name = productDto.Name,
                    Description = productDto.Description,
                    Price = productDto.Price,
                    StockQuantity = productDto.StockQuantity,
                    Category = productDto.Category
                };

                await _productRepository.AddAsync(product);
                await _productRepository.SaveAsync();
                return CreatedAtAction(nameof(GetProductById), new { id = product.Id }, product);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while creating the product.");
                return StatusCode(StatusCodes.Status500InternalServerError, "Internal server error");
            }
        }
        [HttpPut]

        public async Task<IActionResult> UpdateProduct(int id, ProductResponseDto productDto)
        {
            var existingProduct = await _productRepository.GetByIdAsync(id);
            if (existingProduct == null)
            {
                return NotFound();
            }
            // Update the existing product with the new values
            existingProduct.Name = productDto.Name;
            existingProduct.Description = productDto.Description;
            existingProduct.Price = productDto.Price;
            existingProduct.StockQuantity = productDto.StockQuantity;
            existingProduct.Category = productDto.Category;
            _productRepository.Update(existingProduct);
            await _productRepository.SaveAsync();
            return NoContent();
        }
        //i need to include the policy for the delete method to be accessible only by users with the "Admin" role. You can do this by adding the [Authorize(Roles = "Admin")] attribute above the DeleteProduct method. Here's how you can modify the code:



        [Authorize(Policy = "CanDeleteProduct")]
        [HttpDelete("{id}")]

        public async Task<IActionResult> DeleteProduct(int id)
        {
            var existingProduct = await _productRepository.GetByIdAsync(id);
            if (existingProduct == null)
            {
                return NotFound();
            }
            _productRepository.Delete(existingProduct);
            await _productRepository.SaveAsync();
            return NoContent();

        }
    }
}
