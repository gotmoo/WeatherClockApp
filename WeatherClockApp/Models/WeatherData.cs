namespace WeatherClockApp.Models
{
    /// <summary>
    /// A data class to hold weather information fetched from the OpenWeatherMap API.
    /// </summary>
    public class WeatherData
    {
        // From "main" object
        public double Temperature { get; set; }
        public double FeelsLike { get; set; }
        public double TempMin { get; set; }
        public double TempMax { get; set; }
        public int Humidity { get; set; }

        // From "weather" array
        public string Description { get; set; }

        // Top-level fields
        public int UtcOffsetSeconds { get; set; }
        public string CityName { get; set; }
        public bool IsNullObject { get; set; }
    }
}