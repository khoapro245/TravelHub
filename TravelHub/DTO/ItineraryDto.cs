using System;
using System.Collections.Generic;

namespace TravelHub.DTO
{
    public class ItineraryDto
    {
        public int ItineraryID { get; set; }
        public int UserID { get; set; }
        public string TripName { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal? TotalBudgetEstimatedVND { get; set; }
        public string Status { get; set; } = string.Empty;
        public List<ItineraryDetailDto> Details { get; set; } = new List<ItineraryDetailDto>();
    }

    public class ItineraryDetailDto
    {
        public int DetailID { get; set; }
        public int DestinationID { get; set; }
        public string DestinationName { get; set; } = string.Empty;
        public int DayNumber { get; set; }
        public string? TimeSlot { get; set; }
        public string? ActivityDescription { get; set; }
        public decimal? EstimatedCostVND { get; set; }
    }

    public class CreateItineraryRequest
    {
        public string TripName { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal? TotalBudgetEstimatedVND { get; set; }
        public List<CreateItineraryDetailRequest> Details { get; set; } = new List<CreateItineraryDetailRequest>();
    }

    public class CreateItineraryDetailRequest
    {
        public int DestinationID { get; set; }
        public int DayNumber { get; set; }
        public string? TimeSlot { get; set; }
        public string? ActivityDescription { get; set; }
        public decimal? EstimatedCostVND { get; set; }
    }

    public class UpdateItineraryRequest
    {
        public string? TripName { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public decimal? TotalBudgetEstimatedVND { get; set; }
        public string? Status { get; set; }
    }
}
