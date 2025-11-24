namespace ProductAssistant.Core.Services;

// Location class for geolocation service
public class Location
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    
    public Location(double latitude, double longitude)
    {
        Latitude = latitude;
        Longitude = longitude;
    }
    
    public static double CalculateDistance(Location location1, Location location2, DistanceUnits units)
    {
        // Haversine formula for distance calculation
        const double earthRadiusKm = 6371.0;
        const double earthRadiusMiles = 3959.0;
        
        var dLat = ToRadians(location2.Latitude - location1.Latitude);
        var dLon = ToRadians(location2.Longitude - location1.Longitude);
        
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(location1.Latitude)) * Math.Cos(ToRadians(location2.Latitude)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        
        var radius = units == DistanceUnits.Kilometers ? earthRadiusKm : earthRadiusMiles;
        return radius * c;
    }
    
    private static double ToRadians(double degrees)
    {
        return degrees * Math.PI / 180.0;
    }
}

public enum DistanceUnits
{
    Kilometers,
    Miles
}

public interface IGeolocationService
{
    Task<Location?> GetCurrentLocationAsync();
    Task<double?> GetDistanceAsync(double latitude1, double longitude1, double latitude2, double longitude2);
}

