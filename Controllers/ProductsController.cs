using System.Text.Json;
using Asp.Versioning;
using Crud.Data;
using Crud.Dtos;
using Crud.Interfaces;
using Crud.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Caching.Distributed; // for redis
using Microsoft.Extensions.Caching.Memory;

namespace Crud.Controllers
{
    [ApiVersion("1.0", Deprecated = true)]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    //i want enable rate limiting for the GetAllProducts method, so that it can limit the number of requests per minute for a given IP address. This will help prevent abuse and ensure fair usage of the API. I will use the [EnableRateLimiting] attribute to enable rate limiting for this method.
    [EnableRateLimiting("FixedWindow")]
    public class ProductsController : ControllerBase
    {
        private readonly IProductRepository _productRepository;
        private readonly ILogger<ProductsController> _logger;
        private readonly APiDbContext _aPiDb;
        private readonly IAuthorizationService _authorizationService;
        private readonly IMemoryCache _memoryCache;
        private readonly IDistributedCache _distributedCache;
        private const string ProductCacheVersionKey = "products_cache_version";

        public ProductsController(
            APiDbContext aPiDbContext,
            IProductRepository productRepository,
            ILogger<ProductsController> logger,
            IAuthorizationService authorizationService,
            IMemoryCache memoryCache,
            IDistributedCache distributedCache
        )
        {
            _aPiDb = aPiDbContext;
            _productRepository = productRepository;
            _logger = logger;
            _authorizationService = authorizationService;
            _memoryCache = memoryCache;
            _distributedCache = distributedCache;
        }

        //used for cache versioning, so that when the data changes, we can increment the cache version and invalidate the cache
        private int GetCurrentCacheVersion()
        {
            if (!_memoryCache.TryGetValue(ProductCacheVersionKey, out int cacheVersion))
            {
                cacheVersion = 1; // Default version
                _memoryCache.Set(ProductCacheVersionKey, cacheVersion);
            }
            return cacheVersion;
        }

        //invalidate the cache version when the data changes, so that we can increment the cache version and invalidate the cache
        private void InvalidateCacheVersion()
        {
            var currentVersion = GetCurrentCacheVersion();
            _memoryCache.Set(ProductCacheVersionKey, currentVersion + 1);
        }

        [Authorize(Policy = "CanViewProducts")]
        [HttpGet]
        public async Task<ActionResult<PagedResponseDto<ProductResponseDto>>> GetAllProducts(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? category = null
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
            //store the current cache version in a variable, so that we can use it to invalidate the cache when the data changes
            var currentCacheVersion = GetCurrentCacheVersion();
            //i will implement caching for the GetAllProducts method, so that it can cache the paginated list of products for a given page number and page size. This will improve the performance of the API by reducing the number of database calls for frequently requested pages.
            var cacheKey =
                $"products_page_{pageNumber}_size_{pageSize}_category_{category}_version_{currentCacheVersion}";
            // check redis cache first — gracefully fall back if Redis is unavailable
            try
            {
                var distributedCacheResult = await _distributedCache.GetStringAsync(cacheKey);
                if (distributedCacheResult != null)
                {
                    var RediscachedProducts = JsonSerializer.Deserialize<
                        PagedResponseDto<ProductResponseDto>
                    >(distributedCacheResult);
                    _logger.LogInformation(
                        $"Returning cached products for page {pageNumber} and size {pageSize} from Redis."
                    );
                    return Ok(RediscachedProducts);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis is unavailable. Falling back to database.");
            }
            // the following code is commented out because it is not needed, since we are already checking the redis cache first, and if not found then we will check the memory cache. This is because the memory cache is faster than the redis cache, so we want to check it first before checking the redis cache.
            // if (
            //     _memoryCache.TryGetValue(
            //         cacheKey,
            //         out PagedResponseDto<ProductResponseDto> cachedProducts
            //     )
            // )
            // {
            //     _logger.LogInformation(
            //         $"Returning cached products for page {pageNumber} and size {pageSize}."
            //     );
            //     return Ok(cachedProducts);
            // }
            var products = await _productRepository.GetPagedAsync(pageNumber, pageSize, category);
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
            // Set the cache options and cache the response for 5 minutes
            var cacheEntryOptions = new MemoryCacheEntryOptions().SetSlidingExpiration(
                TimeSpan.FromMinutes(5)
            );
            _memoryCache.Set(cacheKey, response, cacheEntryOptions);
            // Also cache in Redis (best-effort — skip if Redis is unavailable)
            try
            {
                var redisCacheEntry = JsonSerializer.Serialize(response);
                var redisCacheOptions = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5),
                };
                await _distributedCache.SetStringAsync(
                    cacheKey,
                    redisCacheEntry,
                    redisCacheOptions
                );
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Redis is unavailable. Response will not be cached in Redis."
                );
            }
            return Ok(response);
        }

        //implement keyset pagination for the GetAllProducts method, so that it can return the next page of products based on the last product Id and the given page size. This method is useful for implementing infinite scrolling or "load more" functionality in a client application.
        [HttpGet("next")]
        public async Task<ActionResult<KeysetPagedResponseDto<ProductResponseDto>>> GetNextPage(
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
            var products = (await _productRepository.GetNextPageAsync(lastId, pageSize)).ToList();

            var hasMore = products.Count > pageSize;

            if (hasMore)
            {
                products.RemoveAt(products.Count - 1);
            }

            var response = new KeysetPagedResponseDto<ProductResponseDto>
            {
                Data = products
                    .Select(p => new ProductResponseDto
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Description = p.Description,
                        Price = p.Price,
                        StockQuantity = p.StockQuantity,
                        Category = p.Category,
                    })
                    .ToList(),

                HasMore = hasMore,

                NextCursor = products.Count > 0 ? products[^1].Id : null,
            };

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
                // Invalidate the cache version to ensure that the next request fetches fresh data
                InvalidateCacheVersion();
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
            // Invalidate the cache version to ensure that the next request fetches fresh data
            InvalidateCacheVersion();
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
            // Invalidate the cache version to ensure that the next request fetches fresh data
            InvalidateCacheVersion();

            return NoContent();
        }
    }
}
