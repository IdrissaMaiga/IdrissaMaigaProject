using Microsoft.Maui.Devices.Sensors;
using ProductAssistant.Core.Services;
using Location = ProductAssistant.Core.Services.Location;
using DistanceUnits = ProductAssistant.Core.Services.DistanceUnits;

namespace ShopAssistant.Services;

public class GeolocationService : IGeolocationService
{
    public async Task<Location?> GetCurrentLocationAsync()
    {
        try
        {
            var request = new GeolocationRequest
            {
                DesiredAccuracy = GeolocationAccuracy.Medium,
                Timeout = TimeSpan.FromSeconds(10)
            };

            var mauiLocation = await Geolocation.Default.GetLocationAsync(request);
            if (mauiLocation == null)
                return null;
                
            return new Location(mauiLocation.Latitude, mauiLocation.Longitude);
        }
        catch (Exception)
        {
            return null;
        }
    }

    public Task<double?> GetDistanceAsync(double latitude1, double longitude1, double latitude2, double longitude2)
    {
        try
        {
            var location1 = new Location(latitude1, longitude1);
            var location2 = new Location(latitude2, longitude2);
            var distance = Location.CalculateDistance(location1, location2, DistanceUnits.Kilometers);
            return Task.FromResult<double?>(distance);
        }
        catch (Exception)
        {
            return Task.FromResult<double?>(null);
        }
    }
}

