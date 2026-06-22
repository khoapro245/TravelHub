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
                .Where(p => !p.IsHidden)
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

        [HttpPost("posts/{id}/report")]
        public async Task<IActionResult> ReportPost(int id, [FromBody] ReportPostRequest request)
        {
            try
            {
                int userId = GetCurrentUserId();

                var postExists = await _context.Posts.AnyAsync(p => p.PostID == id);
                if (!postExists)
                    return NotFound("Post not found.");

                var existingReport = await _context.Reports.FirstOrDefaultAsync(r => r.PostID == id && r.ReporterID == userId);
                if (existingReport != null)
                    return BadRequest("You have already reported this post.");

                var report = new Report
                {
                    PostID = id,
                    ReporterID = userId,
                    Reason = request.Reason,
                    Status = "Pending",
                    ReportDate = DateTime.UtcNow
                };

                _context.Reports.Add(report);
                await _context.SaveChangesAsync();

                return Ok(new { Message = "Post reported successfully." });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
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
                    Content = request.Content,
                    CreationDate = DateTime.UtcNow
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

        [HttpDelete("posts/{id}")]
        public async Task<IActionResult> DeletePost(int id)
        {
            try
            {
                int userId = GetCurrentUserId();

                var post = await _context.Posts.FindAsync(id);
                if (post == null)
                    return NotFound("Post not found.");

                // Only the author may delete their own post.
                if (post.UserID != userId)
                    return Forbid();

                // Remove dependent records first to avoid FK constraint issues.
                var likes = _context.PostLikes.Where(pl => pl.PostID == id);
                _context.PostLikes.RemoveRange(likes);

                var comments = _context.Comments.Where(c => c.PostID == id);
                _context.Comments.RemoveRange(comments);

                var companions = _context.TravelCompanions.Where(tc => tc.PostID == id);
                _context.TravelCompanions.RemoveRange(companions);

                _context.Posts.Remove(post);
                await _context.SaveChangesAsync();

                return Ok(new { Message = "Post deleted successfully.", PostID = id });
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
                    Content = request.Content,
                    CommentDate = DateTime.UtcNow
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
