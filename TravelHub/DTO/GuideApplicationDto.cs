using System;

namespace TravelHub.DTO
{
    public class GuideApplicationDto
    {
        public int ApplicationID { get; set; }
        public int GuideID { get; set; }
        public string GuideUsername { get; set; } = string.Empty;
        public string? GuideAvatarURL { get; set; }
        public int PostID { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Message { get; set; }
        public decimal? ProposedPriceVND { get; set; }
        public DateTime AppliedDate { get; set; }
    }
}
