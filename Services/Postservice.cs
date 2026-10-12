using Crud.Dtos;
using Crud.Interfaces;
using Crud.Models;
using Newtonsoft.Json;

namespace Crud.Services
{
    public class Postservice
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly IPostDetailRepository _postDetail;

        public Postservice(IHttpClientFactory clientFactory, IPostDetailRepository postDetail)
        {
            _clientFactory = clientFactory;
            _postDetail = postDetail;
        }
        public async Task<string> GetPostsAsync()
        {
            var client = _clientFactory.CreateClient();
            var response = await client.GetAsync("https://jsonplaceholder.typicode.com/posts");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return content;
            }
            else
            {
                throw new Exception("Failed to fetch posts");
            }
        }
        public async Task<List<Post>> GetPostsListAsync()
        {
            var client = _clientFactory.CreateClient();
            var response = await client.GetAsync("https://jsonplaceholder.typicode.com/posts");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var posts = JsonConvert.DeserializeObject<List<Post>>(content); // Newtonsoft.Json.JsonConvert replaced with JsonConvert
                return posts;
            }
            else
            {
                throw new Exception("Failed to fetch posts");
            }
        }
        //i want to get by the ID

        public async Task<Post> GetPostByIdAsync(int id)
        {
            var client = _clientFactory.CreateClient();
            var response = await client.GetAsync($"https://jsonplaceholder.typicode.com/posts/{id}");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var post = JsonConvert.DeserializeObject<Post>(content); // Newtonsoft.Json.JsonConvert replaced with JsonConvert
                return post;
            }
            else
            {
                throw new Exception($"Failed to fetch post with id {id}");
            }
        }
        //i want to create a new post
        public async Task<Post> CreatePostAsync(Post newPost)
        {
            var client = _clientFactory.CreateClient();
            var postContent = new StringContent(JsonConvert.SerializeObject(newPost), System.Text.Encoding.UTF8, "application/json");
            var response = await client.PostAsync("https://jsonplaceholder.typicode.com/posts", postContent);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var createdPost = JsonConvert.DeserializeObject<Post>(content); // Newtonsoft.Json.JsonConvert replaced with JsonConvert
                return createdPost;
            }
            else
            {
                throw new Exception("Failed to create post");
            }
        }

        // i want to return the data from my DTO here 
        public async Task<List<PostDto>> GetPostsFromDtoAsync()
        {
            var client = _clientFactory.CreateClient();
            var response = await client.GetAsync("https://jsonplaceholder.typicode.com/posts");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var posts = JsonConvert.DeserializeObject<List<PostDto>>(content);
                //do the mapping here from post to postdto
                var postDtos = posts.Select(p => new PostDto
                {
                    Id = p.Id,
                    Title = p.Title
                }).ToList();
                return posts;
            }
            else
            {
                throw new Exception("Failed to fetch posts");
            }


        }

        // here i wnt to fetch the data from the external Api and save it into the database using the method AddPostDetails() in the PostDetailRepository class

        public async Task SavePostsToDatabaseAsync()
        {
            var client = _clientFactory.CreateClient();
            var response = await client.GetAsync("https://jsonplaceholder.typicode.com/posts");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var posts = JsonConvert.DeserializeObject<List<Post>>(content);
                foreach (var post in posts)
                {
                    var postDetails = new Postdetails
                    {
                        Id = post.Id,
                        Title = post.Title,
                        Body = post.Body
                    };
                    await _postDetail.AddPostDetails(postDetails);
                }
            }
            else
            {
                throw new Exception("Failed to fetch posts");
            }

        }
    }
}
