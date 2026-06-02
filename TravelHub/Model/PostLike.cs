using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TravelHub.Model
{
    public class PostLike
    {
        public int UserID { get; set; }
        public int PostID { get; set; }
        public DateTime LikedDate { get; set; } = DateTime.UtcNow;

        [ForeignKey("UserID")]
        public virtual User User { get; set; } = null!;

        [ForeignKey("PostID")]
        public virtual Post Post { get; set; } = null!;
    }
}
