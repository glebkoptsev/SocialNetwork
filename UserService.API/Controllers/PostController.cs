using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;
using UserService.API.DTOs;
using UserService.API.Services;
using UserService.Database.Entities;

namespace UserService.API.Controllers
{
    [ApiController]
    [Route("api/post")]
    public class PostController(PostService postService, UsersService usersService) : ControllerBase
    {
        [HttpGet, Route("get/{post_id}"), Authorize]
        public async Task<ActionResult<PostResponse>> GetPost(Guid post_id)
        {
            var post = await postService.GetPostAsync(post_id);
            return post is null ? NotFound() : Ok(post.ToResponse());
        }

        [HttpGet, Route("feed"), Authorize]
        public async Task<ActionResult<PostResponse[]>> GetFeed(int offset = 0, int limit = 50, string? user_id = null)
        {
            IEnumerable<Post> posts;
            if (user_id is not null)
            {
                var authorId = await usersService.ResolveUserIdAsync(user_id);
                if (authorId == Guid.Empty) return NotFound();
                posts = await postService.GetUserPostsAsync(authorId, offset, limit);
            }
            else
            {
                var currentUserId = Guid.Parse(User.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value);
                posts = await postService.GetFeedAsync(currentUserId, currentUserId, offset, limit);
            }
            return Ok(posts.Select(p => p.ToResponse()).ToArray());
        }

        [HttpPost, Route("create"), Authorize, EnableRateLimiting("PostCreatePolicy")]
        public async Task<ActionResult<Guid>> AddPost([FromBody] AddPostRequest request)
        {
            var currentUserId = Guid.Parse(User.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value);
            return Ok(await postService.AddPostAsync(currentUserId, request.Text));
        }

        [HttpDelete, Route("delete/{post_id}"), Authorize]
        public async Task<IActionResult> DeletePost(Guid post_id)
        {
            var currentUserId = Guid.Parse(User.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value);
            await postService.DeletePostAsync(post_id, currentUserId);
            return Ok();
        }

        [HttpPut, Route("update/{post_id}"), Authorize]
        public async Task<IActionResult> UpdatePost(Guid post_id, [FromBody] AddPostRequest request)
        {
            var currentUserId = Guid.Parse(User.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value);
            await postService.UpdatePostAsync(post_id, request.Text, currentUserId);
            return Ok();
        }

        [HttpPost, Route("{post_id}/like"), Authorize]
        public async Task<ActionResult<object>> ToggleLike(Guid post_id)
        {
            var currentUserId = Guid.Parse(User.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value);
            var (liked, count) = await postService.ToggleLikeAsync(post_id, currentUserId);
            return Ok(new { liked, like_count = count });
        }
    }
}