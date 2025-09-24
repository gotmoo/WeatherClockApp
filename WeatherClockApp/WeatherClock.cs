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
        private const int WeatherUpdateIntervalMinutes = 20;

        public WeatherClock(AppSettings settings)
        {
            _settings = settings;
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
                if ((now - _lastWeatherUpdate).TotalMinutes >= WeatherUpdateIntervalMinutes)
                {
                    UpdateWeatherData();
                }

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

                // Update the static display every second for the blinking colon
                DisplayManager.ToggleColon();
                DisplayManager.UpdateTimeAndTemp();

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
            if (newData != null)
            {
                _weatherData = newData;
                _lastWeatherUpdate = DateTime.UtcNow;

                DisplayManager.SetUtcOffset(_weatherData.UtcOffsetSeconds);
                string unit = _settings.WeatherUnit == "imperial" ? "F" : "C";
                DisplayManager.SetTemperature($"{Math.Round(_weatherData.Temperature)}°{unit}");

                // Briefly show the city and temp as confirmation
                Thread.Sleep(2500);
            }
            else
            {
                DisplayManager.ShowStatus("Update", "Failed");
                Thread.Sleep(2500);
            }
        }
    }
}

