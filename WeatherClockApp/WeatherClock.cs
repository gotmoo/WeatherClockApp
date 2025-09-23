using System;
using System.Diagnostics;
using System.Threading;
using nanoFramework.Networking;
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

        public WeatherClock(AppSettings settings)
        {
            _settings = settings;
        }

        public void Run()
        {
            // 1. Synchronize the time
            try
            {
                // Using SNTP is very memory-efficient.
                Sntp.Start();
                Console.WriteLine($"Time synchronized. Current UTC time is: {DateTime.UtcNow}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SNTP failed: {ex.Message}");
            }


            // 2. TODO: Initialize the dot-matrix display here.

            // 3. Main application loop
            while (true)
            {
                Console.WriteLine("Fetching weather data...");
                // TODO: Call a WeatherManager to get and parse weather data.

                Console.WriteLine("Updating display...");
                // TODO: Update the dot-matrix display with time and weather.

                // Wait for 20 minutes before the next update.
                Thread.Sleep(20 * 60 * 1000);
            }
        }
    }
}