using System;
using System.Collections.Generic;

namespace TravelHub.DTO
{
    public class PostDto
    {
        public int PostID { get; set; }
        public int UserID { get; set; }
        public string Username { get; set; } = string.Empty;
        public string? AvatarURL { get; set; }
        public int? ItineraryID { get; set; }
        public string PostType { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Content { get; set; }
        public int LikesCount { get; set; }
        public bool IsLikedByCurrentUser { get; set; }
        public DateTime CreationDate { get; set; }
    }

    public class CreatePostRequest
    {
        public int? ItineraryID { get; set; }
        public string PostType { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Content { get; set; }
    }

    public class CommentDto
    {
        public int CommentID { get; set; }
        public int PostID { get; set; }
        public int UserID { get; set; }
        public string Username { get; set; } = string.Empty;
        public string? AvatarURL { get; set; }
        public string? Content { get; set; }
        public DateTime CommentDate { get; set; }
    }

    public class CreateCommentRequest
    {
        public string Content { get; set; } = string.Empty;
    }
}
