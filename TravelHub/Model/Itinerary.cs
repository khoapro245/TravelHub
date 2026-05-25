using System;
using System.Collections.Generic;
namespace TravelHub.Model
{
    public class Itinerary
    {
        public int ItineraryID { get; set; }
        public int UserID { get; set; }
        public string TripName { get; set; } = null!;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal? TotalBudgetEstimatedVND { get; set; }
        public string Status { get; set; } = "Planned";

        public virtual User User { get; set; } = null!;

        // Quan hệ với các bảng chi tiết
        public virtual ICollection<ItineraryDetail> ItineraryDetails { get; set; } = new List<ItineraryDetail>();
        public virtual ICollection<Budget> Budgets { get; set; } = new List<Budget>();
        public virtual ICollection<Post> Posts { get; set; } = new List<Post>();
    }
}