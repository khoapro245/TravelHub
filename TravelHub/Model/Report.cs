using System;

namespace TravelHub.Model
{
    public class Report
    {
        public int ReportID { get; set; }
        public int PostID { get; set; }
        public int ReporterID { get; set; }
        public string Reason { get; set; } = null!;
        public string Status { get; set; } = "Pending"; // Pending, Resolved, Rejected
        public DateTime ReportDate { get; set; }

        public virtual Post Post { get; set; } = null!;
        public virtual User Reporter { get; set; } = null!;
    }
}
