using Crud.Models;
using Crud.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Crud.Controllers
{
    [Route("api/ExternalAPICall")]
    [ApiController]
    public class PostController : ControllerBase
    {
        //here i have injected the Postservice class to use its method to call the external API
        private readonly Postservice _postservice;

        public PostController(Postservice postservice)
        {
            _postservice = postservice;
        }

        [HttpGet]
        public async Task<IActionResult> GetPosts()
        {
            try
            {
                var posts = await _postservice.GetPostsAsync();
                return Ok(posts);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
        [HttpGet]  //get the post by id
        [Route("{id}")]
        public async Task<IActionResult> GetPostById(int id)
        {
            try
            {
                var post = await _postservice.GetPostByIdAsync(id);
                if (post == null)
                {
                    return NotFound($"Post with id {id} not found");
                }
                return Ok(post);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }

        }
        [HttpGet]  // i want to get the list of posts by making a call to the method GetPostsListAsync() in the Postservice class
        [Route("list")]
        public async Task<IActionResult> GetPostsList()
        {
            try
            {
                var posts = await _postservice.GetPostsListAsync();
                return Ok(posts);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
        //now i want to create a new post by making a call to the method CreatePostAsync() in the Postservice class
        [HttpPost]
        public async Task<IActionResult> CreatePost([FromBody] Post newPost)
        {
            try
            {
                var createdPost = await _postservice.CreatePostAsync(newPost);
                return CreatedAtAction(nameof(GetPostById), new { id = createdPost.Id }, createdPost);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }


        }
        [HttpGet]  // i want to display the data from my Dtos
        [Route("dto")]
        public async Task<IActionResult> GetPostsDto()
        {
            try
            {
                var posts = await _postservice.GetPostsFromDtoAsync();

                return Ok(posts);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }

        }
        // heere iwant to save the posts which are fetched from the external API to my database by making a call to the method SavePostsToDatabaseAsync() in the Postservice class
        [HttpPost]
        [Route("saveApi")]
        public async Task<IActionResult> SavePostsToDatabase()
        {
            try
            {
                await _postservice.SavePostsToDatabaseAsync();
                return Ok("Posts saved to database successfully");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }

        }
    }
}