using System;

namespace TravelHub.DTO
{
    public class ReportDto
    {
        public int ReportID { get; set; }
        public int PostID { get; set; }
        public string PostTitle { get; set; } = null!;
        public string PostContent { get; set; } = null!;
        public string ReporterName { get; set; } = null!;
        public string Reason { get; set; } = null!;
        public string Status { get; set; } = null!;
        public DateTime ReportDate { get; set; }
    }

    public class UpdateReportStatusRequest
    {
        public string Status { get; set; } = null!; // "Resolved" or "Rejected"
    }
}
