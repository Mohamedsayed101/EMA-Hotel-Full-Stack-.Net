namespace Hotel_MVC.ViewModels.Admin
{
    public class DashboardVM
    {
        public int TotalRooms { get; set; }
        public int AvailableRooms { get; set; }
        public int MaintenanceRooms { get; set; }
        public int TotalBookings { get; set; }
        public int ArrivalsToday { get; set; }
        public int DeparturesToday { get; set; }
        public decimal RevenueThisMonth { get; set; }
        public double OccupancyRate { get; set; }
        public List<string> ChartLabels { get; set; } = new();
        public List<int> ChartBookings { get; set; } = new();
    }
}
