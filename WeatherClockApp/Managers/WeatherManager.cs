using System;
using System.Diagnostics;
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
                return new WeatherData() { IsNullObject = true };
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
                    // The explicit response.Dispose() was removed from here.
                    // The 'using' block for HttpClient will correctly manage the connection lifetime.

                    Debug.WriteLine($"Weather API Response: {json.Substring(0, Math.Min(json.Length, 200))}...");

                    var mainObject = MiniJsonParser.FindObject(json, "main");


                    // The "weather" property is an array. We need to find the first object within it.
                    string firstWeatherObject = null;
                    string weatherArrayKey = "\"weather\":[";
                    int weatherArrayIndex = json.IndexOf(weatherArrayKey);
                    if (weatherArrayIndex > -1)
                    {
                        // Find the first opening brace '{' after the array key
                        int firstBraceIndex = json.IndexOf('{', weatherArrayIndex);
                        if (firstBraceIndex > -1)
                        {
                            // Find the matching closing brace '}' to extract the complete object string
                            int braceCount = 1;
                            for (int i = firstBraceIndex + 1; i < json.Length; i++)
                            {
                                if (json[i] == '{') braceCount++;
                                if (json[i] == '}') braceCount--;
                                if (braceCount == 0)
                                {
                                    firstWeatherObject = json.Substring(firstBraceIndex, i - firstBraceIndex + 1);
                                    break;
                                }
                            }
                        }
                    }


                    if (mainObject != null && firstWeatherObject != null)
                    {
                        var data = new WeatherData
                        {
                            Temperature = double.Parse(MiniJsonParser.FindValue(mainObject, "temp")),
                            FeelsLike = double.Parse(MiniJsonParser.FindValue(mainObject, "feels_like")),
                            TempMin = double.Parse(MiniJsonParser.FindValue(mainObject, "temp_min")),
                            TempMax = double.Parse(MiniJsonParser.FindValue(mainObject, "temp_max")),
                            Humidity = int.Parse(MiniJsonParser.FindValue(mainObject, "humidity")),
                            Description = MiniJsonParser.FindValue(firstWeatherObject, "description"),
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

            return new WeatherData(){IsNullObject = true};
        }

        /// <summary>
        /// Gets geolocation data from the OpenWeatherMap API for a given search term.
        /// </summary>
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
                    client.Timeout = TimeSpan.FromSeconds(10);
                    var response = client.Get(url);
                    var responseBody = response.Content.ReadAsString();
                    // The explicit response.Dispose() was also removed from here for the same reason.

                    Debug.WriteLine($"Geolocate response: {responseBody}");
                    return responseBody;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GeoLocate: {ex.Message}");
                return "[]";
            }
        }
    }
}

