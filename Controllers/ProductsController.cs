using Asp.Versioning;
using Crud.Data;
using Crud.Dtos;
using Crud.Interfaces;
using Crud.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Crud.Controllers
{
    [ApiVersion("1.0", Deprecated = true)]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly IProductRepository _productRepository;
        private readonly ILogger<ProductsController> _logger;
        private readonly APiDbContext _aPiDb;
        private readonly IAuthorizationService _authorizationService;

        public ProductsController(
            APiDbContext aPiDbContext,
            IProductRepository productRepository,
            ILogger<ProductsController> logger,
            IAuthorizationService authorizationService
        )
        {
            _aPiDb = aPiDbContext;
            _productRepository = productRepository;
            _logger = logger;
            _authorizationService = authorizationService;
        }

        [Authorize(Policy = "CanViewProducts")]
        [HttpGet]
        public async Task<ActionResult<PagedResponseDto<ProductResponseDto>>> GetAllProducts(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10
        )
        {
            // var products = await _productRepository.GetAllAsync();
            // return Ok(products);
            //i wanted to implement pagination for the GetAllProducts method, so i will add the pageNumber and pageSize parameters to the method and then use them to get the paginated list of products from the repository
            // add some validation for the pageNumber and pageSize parameters, so that they cannot be less than 1
            if (pageNumber < 1 || pageSize < 1)
            {
                return BadRequest("Page number and page size must be greater than 0.");
            }
            if (pageSize > 100)
            {
                return BadRequest("Page size cannot be greater than 100.");
            }
            if (pageNumber > 100)
            {
                return BadRequest("Page number cannot be greater than 100.");
            }
            var products = await _productRepository.GetPagedAsync(pageNumber, pageSize);
            var totalRecords = await _productRepository.GetCountAsync();
            var totalPages = (int)Math.Ceiling((double)totalRecords / pageSize);
            var response = new PagedResponseDto<ProductResponseDto>
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalRecords = totalRecords,
                TotalPages = totalPages,
                Data = products.Select(p => new ProductResponseDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Price = p.Price,
                    StockQuantity = p.StockQuantity,
                    Category = p.Category,
                }),
            };
            return Ok(response);
        }

        //implement keyset pagination for the GetAllProducts method, so that it can return the next page of products based on the last product Id and the given page size. This method is useful for implementing infinite scrolling or "load more" functionality in a client application.
        [HttpGet("next")]
        public async Task<ActionResult<IEnumerable<ProductResponseDto>>> GetNextPage(
            [FromQuery] int lastId,
            [FromQuery] int pageSize = 10
        )
        {
            if (lastId < 0 || pageSize < 1)
            {
                return BadRequest(
                    "Last Id must be greater than or equal to 0 and page size must be greater than 0."
                );
            }
            if (pageSize > 100)
            {
                return BadRequest("Page size cannot be greater than 100.");
            }
            var products = await _productRepository.GetNextPageAsync(lastId, pageSize);
            var response = products
                .Select(p => new ProductResponseDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Price = p.Price,
                    StockQuantity = p.StockQuantity,
                    Category = p.Category,
                })
                .ToList(); // Convert to list to avoid deferred execution issues
            return Ok(response);
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
        public async Task<ActionResult<ProductResponseDto>> CreateProduct(
            ProductCreateDto productDto
        )
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
                    Category = productDto.Category,
                };

                await _productRepository.AddAsync(product);
                await _productRepository.SaveAsync();
                return CreatedAtAction(nameof(GetProductById), new { id = product.Id }, product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while creating the product.");
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    "Internal server error"
                );
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

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var existingProduct = await _productRepository.GetByIdAsync(id);

            if (existingProduct == null)
            {
                return NotFound();
            }

            var authorizationResult = await _authorizationService.AuthorizeAsync(
                User,
                existingProduct,
                "DeleteProductOwner"
            );

            if (!authorizationResult.Succeeded)
            {
                return Forbid();
            }

            _productRepository.Delete(existingProduct);
            await _productRepository.SaveAsync();

            return NoContent();
        }
    }
}
