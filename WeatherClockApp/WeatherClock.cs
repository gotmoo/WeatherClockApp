using nanoFramework.Networking;
using System;
using System.Diagnostics;
using System.IO;
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
        // A flag to allow external triggers for weather updates
        private static bool _forceWeatherUpdate = false;

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
        public void Run()
        {
            // 1. Synchronize the time
            try
            {
                DisplayManager.ShowStatus("Syncing", "Time");
                Sntp.Start();
                Sntp.UpdateNow();
                Debug.WriteLine($"Time synchronized. Current UTC time is: {DateTime.UtcNow}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"SNTP failed: {ex.Message}");
                DisplayManager.ShowStatus("Time", "Failed");
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

                // Check to start the once-per-minute description scroll
                // We start it at 58 seconds to give it time to render before the minute ticks over
                if (now.Second == 58 && (now - _lastMinuteScroll).TotalSeconds > 58)
                {
                    if (_weatherData != null && !string.IsNullOrEmpty(_weatherData.Description))
                    {
                        string unit = _settings.WeatherUnit == "imperial" ? "F" : "C";
                        string scrollText = $"{_weatherData.Description}, feels like {Math.Round(_weatherData.FeelsLike)}°{unit}";
                        DisplayManager.ScrollRightHalf(scrollText);
                        _lastMinuteScroll = now;
                    }
                }

                Thread.Sleep(1000);
            }
        }

        /// <summary>
        /// Fetches new weather data and updates the display manager.
        /// </summary>
        private void UpdateWeatherData()
        {
            DisplayManager.ShowStatus("Updating", "Weather");
            var newData = WeatherManager.FetchWeatherData(_settings);
            Console.WriteLine($"Fetched Weather Data: {!newData.IsNullObject} Temp: {newData.Temperature}");
            if (!newData.IsNullObject)
            {
                _weatherData = newData;
                _lastWeatherUpdate = DateTime.UtcNow;

                DisplayManager.SetUtcOffset(_weatherData.UtcOffsetSeconds);
                string unit = _settings.WeatherUnit == "imperial" ? "F" : "C";
                DisplayManager.SetTemperature($"{Math.Round(_weatherData.Temperature)}°{unit}");

                // Briefly show the city and temp as confirmation
                DisplayManager.ShowStatus(_weatherData.CityName, $"{Math.Round(_weatherData.Temperature)}°{unit}");
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

            currentString = ReplacePlaceholder(currentString, "{temp}", $"{Math.Round(data.Temperature)}{degreeSymbol}{unit}");
            currentString = ReplacePlaceholder(currentString, "{feels_like}", $"{Math.Round(data.FeelsLike)}{degreeSymbol}{unit}");
            currentString = ReplacePlaceholder(currentString, "{temp_min}", $"{Math.Round(data.TempMin)}{degreeSymbol}{unit}");
            currentString = ReplacePlaceholder(currentString, "{temp_max}", $"{Math.Round(data.TempMax)}{degreeSymbol}{unit}");
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

