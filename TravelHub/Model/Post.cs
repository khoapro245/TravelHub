using System;
using System.Collections.Generic;
namespace TravelHub.Model
{
    public class Post
    {
        public int PostID { get; set; }
        public int UserID { get; set; }
        public int? ItineraryID { get; set; }
        public string PostType { get; set; } = null!;
        public string Title { get; set; } = null!;
        public string? Content { get; set; }
        public int LikesCount { get; set; } = 0;
        public bool IsHidden { get; set; } = false;
        public DateTime CreationDate { get; set; }

        public virtual User User { get; set; } = null!;
        public virtual Itinerary? Itinerary { get; set; }

        public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();
        public virtual ICollection<TravelCompanion> TravelCompanions { get; set; } = new List<TravelCompanion>();
        public virtual ICollection<GuideApplication> GuideApplications { get; set; } = new List<GuideApplication>();
    }
}