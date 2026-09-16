namespace Hotel_MVC.Services
{
    public static class HotelFacts
    {
        public static string GetSheet()
        {
            return @"
--- HOTEL OVERVIEW & POLICIES ---
- Hotel Name: EMA Hotel & Suites
- Location: Downtown Luxury District
- Check-in Time: 3:00 PM (Early check-in available upon request)
- Check-out Time: 11:00 AM
- Front Desk & Concierge: Available 24/7

--- DINING & BREAKFAST ---
- Restaurant Name: The Grand Azure Dining Room
- Breakfast Hours: 6:30 AM – 10:30 AM daily
- Breakfast Options: Full international hot buffet, continental pastries, fresh seasonal fruit, and made-to-order omelets. Included in select packages or available for $25 per guest.

--- PARKING & TRANSPORTATION ---
- Parking Rates: Free secure underground parking for all registered hotel guests. Valet parking is available 24/7 at the main entrance.
- Airport Shuttle: Available upon request for an additional fee. Contact the front desk to schedule.

--- AMENITIES & FACILITIES ---
- Pool & Spa: Rooftop infinity pool open from 8:00 AM to 10:00 PM. Full-service spa and wellness center available.
- Wi-Fi: Complimentary high-speed Wi-Fi access throughout the entire property (Network: EMA-Guest, Password provided at check-in).
";
        }
    }
}