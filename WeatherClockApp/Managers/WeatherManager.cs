using System;
using System.Diagnostics;
using System.Net.Http;

namespace WeatherClockApp.Managers
{
    internal static class WeatherManager
    {
        /// <summary>
        /// Gets geolocation data from the OpenWeatherMap API for a given search term.
        /// </summary>
        /// <param name="searchTerm">The city or location to search for.</param>
        /// <param name="apiKey">The OpenWeatherMap API key.</param>
        /// <returns>A JSON string with the API response, or an empty JSON array on error.</returns>
        public static string GeoLocate(string searchTerm, string apiKey)
        {
            if (string.IsNullOrEmpty(searchTerm) || string.IsNullOrEmpty(apiKey))
            {
                return "[]";
            }

            var url = $"http://api.openweathermap.org/geo/1.0/direct?q={searchTerm}&limit=5&appid={apiKey}";

            try
            {
                using (var client = new HttpClient())
                {
                    // It's important to set a timeout
                    client.Timeout = TimeSpan.FromSeconds(10);

                    var response = client.Get(url);
                    var responseBody = response.Content.ReadAsString();
                    response.Dispose();

                    Debug.WriteLine($"Geolocate response: {responseBody}");
                    return responseBody;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GeoLocate: {ex.Message}");
                return "[]"; // Return an empty JSON array on error
            }
        }
    }
}