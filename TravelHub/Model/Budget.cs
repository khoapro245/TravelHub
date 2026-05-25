using System;
namespace TravelHub.Model
{
    public class Budget
    {
        public int BudgetID { get; set; }
        public int ItineraryID { get; set; }
        public string Category { get; set; } = null!;
        public decimal PlannedAmountVND { get; set; }
        public decimal? ActualAmountVND { get; set; }
        public DateTime? TransactionDate { get; set; }
        public string? Notes { get; set; }

        public virtual Itinerary Itinerary { get; set; } = null!;
    }
}