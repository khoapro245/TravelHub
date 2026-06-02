using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TravelHub.DTO;
using TravelHub.Model;

namespace TravelHub.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class FeedController : ControllerBase
    {
        private readonly AppDbContext _context;

        public FeedController(AppDbContext context)
        {
            _context = context;
        }

        private int GetCurrentUserId()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdString, out int userId))
            {
                return userId;
            }
            throw new UnauthorizedAccessException("Invalid user token.");
        }

        [HttpGet("posts")]
        public async Task<IActionResult> GetPosts([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var query = _context.Posts
                .Include(p => p.User)
                .OrderByDescending(p => p.CreationDate);

            var totalCount = await query.CountAsync();

            int userId;
            try { userId = GetCurrentUserId(); } catch { userId = 0; }

            var posts = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new PostDto
                {
                    PostID = p.PostID,
                    UserID = p.UserID,
                    Username = p.User.Username,
                    AvatarURL = p.User.AvatarURL,
                    ItineraryID = p.ItineraryID,
                    PostType = p.PostType,
                    Title = p.Title,
                    Content = p.Content,
                    LikesCount = p.LikesCount,
                    IsLikedByCurrentUser = userId > 0 && _context.PostLikes.Any(pl => pl.PostID == p.PostID && pl.UserID == userId),
                    CreationDate = p.CreationDate
                })
                .ToListAsync();

            var result = new PaginatedList<PostDto>
            {
                Items = posts,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };

            return Ok(result);
        }

        [HttpPost("posts")]
        public async Task<IActionResult> CreatePost([FromBody] CreatePostRequest request)
        {
            try
            {
                int userId = GetCurrentUserId();

                var post = new Post
                {
                    UserID = userId,
                    ItineraryID = request.ItineraryID,
                    PostType = request.PostType,
                    Title = request.Title,
                    Content = request.Content
                };

                _context.Posts.Add(post);
                await _context.SaveChangesAsync();

                return Ok(new { Message = "Post created successfully.", PostID = post.PostID });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
        }

        [HttpPost("posts/{id}/like")]
        public async Task<IActionResult> ToggleLike(int id)
        {
            try
            {
                int userId = GetCurrentUserId();
                
                var post = await _context.Posts.FindAsync(id);
                if (post == null)
                    return NotFound("Post not found.");

                var existingLike = await _context.PostLikes
                    .FirstOrDefaultAsync(pl => pl.PostID == id && pl.UserID == userId);

                if (existingLike != null)
                {
                    // Unlike
                    _context.PostLikes.Remove(existingLike);
                    if (post.LikesCount > 0) post.LikesCount--;
                }
                else
                {
                    // Like
                    var newLike = new PostLike
                    {
                        PostID = id,
                        UserID = userId,
                        LikedDate = DateTime.UtcNow
                    };
                    _context.PostLikes.Add(newLike);
                    post.LikesCount++;
                }
                
                await _context.SaveChangesAsync();

                return Ok(new { Message = existingLike != null ? "Post unliked." : "Post liked.", LikesCount = post.LikesCount });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("posts/{id}/comments")]
        public async Task<IActionResult> GetComments(int id, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var query = _context.Comments
                .Include(c => c.User)
                .Where(c => c.PostID == id)
                .OrderBy(c => c.CommentDate);

            var totalCount = await query.CountAsync();

            var comments = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new CommentDto
                {
                    CommentID = c.CommentID,
                    PostID = c.PostID,
                    UserID = c.UserID,
                    Username = c.User.Username,
                    AvatarURL = c.User.AvatarURL,
                    Content = c.Content,
                    CommentDate = c.CommentDate
                })
                .ToListAsync();

            var result = new PaginatedList<CommentDto>
            {
                Items = comments,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };

            return Ok(result);
        }

        [HttpPost("posts/{id}/comments")]
        public async Task<IActionResult> AddComment(int id, [FromBody] CreateCommentRequest request)
        {
            try
            {
                int userId = GetCurrentUserId();

                var postExists = await _context.Posts.AnyAsync(p => p.PostID == id);
                if (!postExists)
                    return NotFound("Post not found.");

                var comment = new Comment
                {
                    PostID = id,
                    UserID = userId,
                    Content = request.Content
                };

                _context.Comments.Add(comment);
                await _context.SaveChangesAsync();

                return Ok(new { Message = "Comment added successfully.", CommentID = comment.CommentID });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
        }
    }
}
