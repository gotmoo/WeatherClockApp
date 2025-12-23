using nanoFramework.Networking;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using WeatherClockApp.Managers;
using WeatherClockApp.Models;

namespace WeatherClockApp
{
    /// <summary>
    /// Represents the main operational logic of the weather clock.
    /// This class is instantiated only after a successful network connection.
    /// </summary>
    internal class WeatherClock
    {
        private readonly AppSettings _settings;
        private WeatherData _weatherData;
        private DateTime _lastWeatherUpdate = DateTime.MinValue;
        private DateTime _lastMinuteScroll = DateTime.MinValue;
        private DateTime _lastTimeUpdate = DateTime.MinValue;

        // A flag to allow external triggers for weather updates
        private static bool _forceWeatherUpdate = false;
        private static bool _forceWeatherScroll = false;

        public WeatherClock(AppSettings settings)
        {
            _settings = settings;
        }

        /// <summary>
        /// Public method to allow the web server to trigger a weather update.
        /// </summary>
        public static void TriggerWeatherUpdate()
        {
            _forceWeatherUpdate = true;
        }
        /// <summary>
        /// Public method to allow the web server to trigger a weather scroll.
        /// </summary>
        public static void TriggerWeatherScroll()
        {
            _forceWeatherScroll = true;
        }

        public void Run()
        {
            // 1. Synchronize the time
            try
            {
                DisplayManager.ShowStatus("Sync", "Time");
                Sntp.Start();
                Sntp.UpdateNow();
                _lastTimeUpdate = DateTime.UtcNow;
                Debug.WriteLine($"Time synchronized. Current UTC time is: {DateTime.UtcNow}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"SNTP failed: {ex.Message}");
                DisplayManager.ShowStatus("Time", "Fail");
                Thread.Sleep(2000);
            }

            // 2. Initial Weather Fetch
            UpdateWeatherData();

            // 3. Main application loop
            while (true)
            {
                DateTime now = DateTime.UtcNow;

                // Check for periodic weather update
                if (_forceWeatherUpdate || (now - _lastWeatherUpdate).TotalMinutes >= _settings.WeatherRefreshMinutes)
                {
                    _forceWeatherUpdate = false;
                    UpdateWeatherData();
                }

                // Update the time display FIRST, so the new minute is shown immediately.
                DisplayManager.ToggleColon();
                DisplayManager.UpdateTimeAndTemp();

                bool shouldScroll = false;
                // Check if the weather description scroll should be triggered
                if (_forceWeatherScroll || 
                    (now.Second == 0 && (now - _lastMinuteScroll).TotalSeconds > _settings.ScrollFrequencyMinutes * 60 - 50))
                {
                    shouldScroll = true;
                    _forceWeatherScroll = false;
                }
                // We start it at 0 seconds to let the minute tick over right before the scroll starts.
                if (shouldScroll && _weatherData != null && !_weatherData.IsNullObject)
                {
                    string scrollText = BuildScrollText();
                    if (!string.IsNullOrEmpty(scrollText))
                    {
                        DisplayManager.ScrollRightHalf(scrollText);
                        _lastMinuteScroll = now;
                    }
                }

                // Sleep until the start of the next second
                var sleepForMs = 1000 - (DateTime.UtcNow - now).Milliseconds;
                Thread.Sleep(sleepForMs);
            }
        }
        private string BuildScrollText()
        {
            var sb = new StringBuilder();
            string unit = _settings.WeatherUnit == "imperial" ? "F" : "C";
            bool hasContent = false;

            if (_settings.ShowDescription && !string.IsNullOrEmpty(_weatherData.Description))
            {
                sb.Append(_weatherData.Description);
                hasContent = true;
            }

            if (_settings.ShowFeelsLike)
            {
                if (hasContent) sb.Append(", ");
                sb.Append("Feels like " + Math.Round(_weatherData.FeelsLike).ToString() + "°" + unit);
                hasContent = true;
            }

            if (_settings.ShowMinTemp)
            {
                if (hasContent) sb.Append(", ");
                sb.Append("Low " + Math.Round(_weatherData.TempMin).ToString() + "°");
                hasContent = true;
            }

            if (_settings.ShowMaxTemp)
            {
                if (hasContent) sb.Append(", ");
                sb.Append("High " + Math.Round(_weatherData.TempMax).ToString() + "°");
                hasContent = true;
            }

            if (_settings.ShowHumidity)
            {
                if (hasContent) sb.Append(", ");
                sb.Append("Hum " + _weatherData.Humidity.ToString() + "%");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Fetches new weather data and updates the display manager.
        /// </summary>
        private void UpdateWeatherData()
        {
            var newData = WeatherManager.FetchWeatherData(_settings);
            Console.WriteLine($"Fetched Weather Data: {!newData.IsNullObject} Temp: {newData.Temperature}");
            if (!newData.IsNullObject)
            {
                _weatherData = newData;
                _lastWeatherUpdate = DateTime.UtcNow;

                DisplayManager.SetUtcOffset(_weatherData.UtcOffsetSeconds);
                string unit = _settings.WeatherUnit == "imperial" ? "F" : "C";
                string degreesSymbol = _settings.ShowDegreesSymbol ? "°" : "";
                DisplayManager.SetTemperature($"{Math.Round(_weatherData.Temperature)}{degreesSymbol}{unit}");

                // Briefly show the city and temp as confirmation
                DisplayManager.ScrollRightHalf($"Weather Updated: {Math.Round(_weatherData.Temperature)}{degreesSymbol}{unit}");
                Thread.Sleep(2500);
            }
            else
            {
                DisplayManager.ShowStatus("Update", "Failed");
                Thread.Sleep(2500);
            }
        }

        /// <summary>
        /// Replaces placeholders in a template string with actual weather data.
        /// </summary>
        private string FormatWeatherString(string template, WeatherData data)
        {
            string unit = _settings.WeatherUnit == "imperial" ? "F" : "C";
            string degreeSymbol = _settings.ShowDegreesSymbol ? "°" : "";
            string currentString = template;

            currentString = ReplacePlaceholder(currentString, "{temp}",
                $"{Math.Round(data.Temperature)}{degreeSymbol}{unit}");
            currentString = ReplacePlaceholder(currentString, "{feels_like}",
                $"{Math.Round(data.FeelsLike)}{degreeSymbol}{unit}");
            currentString = ReplacePlaceholder(currentString, "{temp_min}",
                $"{Math.Round(data.TempMin)}{degreeSymbol}{unit}");
            currentString = ReplacePlaceholder(currentString, "{temp_max}",
                $"{Math.Round(data.TempMax)}{degreeSymbol}{unit}");
            currentString = ReplacePlaceholder(currentString, "{description}", data.Description);
            currentString = ReplacePlaceholder(currentString, "{humidity}", $"{data.Humidity}%");
            currentString = ReplacePlaceholder(currentString, "{city}", data.CityName);

            return currentString;
        }

        private static string ReplacePlaceholder(string original, string placeholder, string value)
        {
            int index = original.IndexOf(placeholder);
            if (index == -1) return original;
            return original.Substring(0, index) + value + original.Substring(index + placeholder.Length);
        }
    }
}