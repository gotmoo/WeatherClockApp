using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using WeatherClockApp.Helpers;
using WeatherClockApp.Models;

namespace WeatherClockApp.Managers
{
    internal static class WeatherManager
    {
        /// <summary>
        /// Fetches the current weather data from the OpenWeatherMap API.
        /// </summary>
        public static WeatherData FetchWeatherData(AppSettings settings)
        {
            if (string.IsNullOrEmpty(settings.WeatherApiKey) || settings.Latitude == 0 || settings.Longitude == 0)
            {
                Debug.WriteLine("Cannot fetch weather, API key or location not set.");
                return null;
            }

            var url = $"http://api.openweathermap.org/data/2.5/weather" +
                      $"?lat={settings.Latitude}" +
                      $"&lon={settings.Longitude}" +
                      $"&units={settings.WeatherUnit}" +
                      $"&appid={settings.WeatherApiKey}";

            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(20);
                    var response = client.Get(url);
                    var json = response.Content.ReadAsString();
                    response.Dispose();

                    Debug.WriteLine($"Weather API Response:");
                    Debug.WriteLine(json);
                    // Manually parse the JSON to save memory
                    var mainObject = MiniJsonParser.FindObject(json, "main");
                    var weatherArray = MiniJsonParser.FindObject(json, "weather"); // This will find the first object in the array

                    if (mainObject != null && weatherArray != null)
                    {
                        var data = new WeatherData
                        {
                            Temperature = double.Parse(MiniJsonParser.FindValue(mainObject, "temp")),
                            FeelsLike = double.Parse(MiniJsonParser.FindValue(mainObject, "feels_like")),
                            TempMin = double.Parse(MiniJsonParser.FindValue(mainObject, "temp_min")),
                            TempMax = double.Parse(MiniJsonParser.FindValue(mainObject, "temp_max")),
                            Humidity = int.Parse(MiniJsonParser.FindValue(mainObject, "humidity")),
                            Description = MiniJsonParser.FindValue(weatherArray, "description"),
                            UtcOffsetSeconds = int.Parse(MiniJsonParser.FindValue(json, "timezone")),
                            CityName = MiniJsonParser.FindValue(json, "name")
                        };
                        return data;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in FetchWeatherData: {ex.Message}");
            }

            return null; // Return null on error
        }

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