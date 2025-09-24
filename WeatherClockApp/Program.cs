using System;
using System.Diagnostics;
using System.Net;
using System.Threading;
using Iot.Device.DhcpServer;
using nanoFramework.Runtime.Native;
using WeatherClockApp.LightweightWeb;
using WeatherClockApp.Managers;
using WeatherClockApp.Models;

namespace WeatherClockApp
{
    public class Program
    {
        private static LightweightWebServer _webServer;
        private static DhcpServer _dhcpServer;
        private static DnsServer _dnsServer;
        private static AppSettings _settings;

        public static void Main()
        {
            Debug.WriteLine("Starting Weather Clock Application...");

            try
            {
                Debug.WriteLine("Loading settings...");
                _settings = SettingsManager.Load();

                // Initialize the display as early as possible
                DisplayManager.Initialize(_settings);
                DisplayManager.ShowStatus("Hello", "World");


                if (!NetworkManager.Initialize(_settings))
                {
                    // Configuration Mode
                    Debug.WriteLine("Could not connect to Wi-Fi. Starting in Configuration Mode.");
                    NetworkManager.StartAccessPoint();

                    Debug.WriteLine($"Access Point is ready. Starting servers...");

                    // Start DHCP Server
                    _dhcpServer = new DhcpServer { CaptivePortalUrl = $"http://{NetworkManager.ApIpAddress}" };
                    if (!_dhcpServer.Start(IPAddress.Parse(NetworkManager.ApIpAddress), new IPAddress(new byte[] { 255, 255, 255, 0 })))
                    {
                        Debug.WriteLine("FATAL: Failed to start DHCP server. Rebooting.");
                        Thread.Sleep(1000);
                        Power.RebootDevice();
                    }
                    Debug.WriteLine("DHCP Server started.");

                    // Start DNS Server
                    _dnsServer = new DnsServer(IPAddress.Parse(NetworkManager.ApIpAddress));
                    _dnsServer.Start();
                    Debug.WriteLine("DNS Server started.");

                    // Start Web Server
                    _webServer = new LightweightWebServer(_settings);
                    _webServer.SettingsUpdated += OnSettingsUpdated; // Subscribe to the event
                    _webServer.Start(NetworkManager.ApIpAddress);
                    Debug.WriteLine("Web Server started.");
                    Debug.WriteLine($"Connect to SSID '{NetworkManager.ApSsid}' to configure.");

                }
                else
                {
                    // Normal Mode
                    Debug.WriteLine($"Successfully connected to Wi-Fi with IP {NetworkManager.IpAddress}");

                    _webServer = new LightweightWebServer(_settings);
                    _webServer.Start(NetworkManager.IpAddress);
                    _webServer.SettingsUpdated += OnSettingsUpdated; // Subscribe to the event
                    Debug.WriteLine("Web Server started for remote management.");

                    var clock = new WeatherClock(_settings);
                    clock.Run();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"FATAL ERROR: {ex.Message}");
                Debug.WriteLine(ex.StackTrace);
            }

            Thread.Sleep(Timeout.Infinite);
        }
        private static void OnSettingsUpdated(object sender, AppSettings newSettings)
        {
            Debug.WriteLine("Settings updated event received. Saving to flash...");
            _settings = newSettings;
            SettingsManager.Save(_settings);
        }
    }
}

