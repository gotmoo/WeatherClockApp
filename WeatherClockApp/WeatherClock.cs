using nanoFramework.Networking;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using WeatherClockApp.Managers;
using WeatherClockApp.Models;

namespace WeatherClockApp
{
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

            UpdateWeatherData();

            while (true)
            {
                DateTime now = DateTime.UtcNow;

                if (_forceWeatherUpdate || (now - _lastWeatherUpdate).TotalMinutes >= _settings.WeatherRefreshMinutes)
                {
                    _forceWeatherUpdate = false;
                    UpdateWeatherData();
                }

                if (now.Second == 0 && (now - _lastMinuteScroll).TotalSeconds > 58)
                {
                    if (_weatherData != null && !_weatherData.IsNullObject)
                    {
                        string scrollText = FormatWeatherString(_settings.WeatherDisplay, _weatherData);
                        DisplayManager.ScrollRightHalf(scrollText);
                        _lastMinuteScroll = now;
                    }
                }

                DisplayManager.ToggleColon();
                DisplayManager.UpdateTimeAndTemp();
                Thread.Sleep(1000);
            }
        }

        private void UpdateWeatherData()
        {
            DisplayManager.ShowStatus("Updating", "Weather");
            var newData = WeatherManager.FetchWeatherData(_settings);

            if (!newData.IsNullObject)
            {
                _weatherData = newData;
                _lastWeatherUpdate = DateTime.UtcNow;

                if (_settings.TimeZoneOffset != _weatherData.UtcOffsetSeconds)
                {
                    _settings.TimeZoneOffset = _weatherData.UtcOffsetSeconds;
                    SettingsManager.Save(_settings);
                }

                DisplayManager.SetUtcOffset(_weatherData.UtcOffsetSeconds);
                string tempString = FormatWeatherString("{temp}", _weatherData);
                DisplayManager.SetTemperature(tempString);

                DisplayManager.ShowStatus(_weatherData.CityName, tempString);
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

