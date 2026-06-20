namespace TravelHub.DTO
{
    public class DestinationDto
    {
        public int DestinationID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string CityProvince { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal? Rate { get; set; }
        public string? Image { get; set; }
        public string? KeyMain { get; set; }
        public decimal? EntranceFee { get; set; }
        public decimal? AccommodationCost { get; set; }
        public decimal? TotalTourCost { get; set; }
        public decimal? TourPricePerPerson { get; set; }
    }

    public class PaginatedList<T>
    {
        public List<T> Items { get; set; } = new List<T>();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    }
}
