using System;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Threading;
using nanoFramework.Networking;
using nanoFramework.Runtime.Native;
using System.Device.Wifi;
using WeatherClockApp.Models;
using AuthenticationType = System.Net.NetworkInformation.AuthenticationType;

namespace WeatherClockApp.Managers
{
    internal static class NetworkManager
    {
        public const string ApSsid = "WeerCfg";
        public static string IpAddress { get; private set; }
        public static string ApIpAddress { get; private set; } = "192.168.4.1";

        /// <summary>
        /// Initializes the network in client mode.
        /// </summary>
        /// <returns>True if connection is successful, otherwise false.</returns>
        public static bool Initialize(AppSettings settings)
        {
            if (!settings.IsConfigured || string.IsNullOrEmpty(settings.Ssid))
            {
                Debug.WriteLine("Wi-Fi is not configured.");
                return false;
            }

            // Set up the Wi-Fi client.
            var success = WifiNetworkHelper.ConnectDhcp(settings.Ssid, settings.Password, requiresDateTime: true, token: new CancellationTokenSource(60000).Token);
            if (success)
            {
                // Wait for the IP address to be assigned.
                int retries = 0;
                while (retries < 20) // Timeout after ~10 seconds
                {
                    IpAddress = NetworkInterface.GetAllNetworkInterfaces()[0].IPv4Address;
                    if (!string.IsNullOrEmpty(IpAddress))
                    {
                        break;
                    }
                    Thread.Sleep(500);
                    retries++;
                }

                if (string.IsNullOrEmpty(IpAddress))
                {
                    Debug.WriteLine("FATAL: Connected to Wi-Fi but failed to get an IP address.");
                    return false;
                }

                // Once connected, ensure the AP is disabled for the next boot
                DisableAccessPoint();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Starts the ESP32 in Access Point mode and waits for it to be ready.
        /// </summary>
        public static string StartAccessPoint()
        {
            // Configure the AP. This may trigger a reboot if settings are new.
            if (SetupAPConfiguration() == false)
            {
                Debug.WriteLine($"Soft AP configured. Rebooting device to apply settings.");
                Power.RebootDevice();
            }

            Debug.WriteLine("Waiting for Access Point to be ready...");

            // Wait for the network interface to be up and have the correct IP.
            int retries = 0;
            while (retries < 20) // Timeout after ~10 seconds
            {
                NetworkInterface ni = GetAPInterface();
                if (ni != null && ni.IPv4Address == ApIpAddress)
                {
                    Debug.WriteLine($"Access Point is active with IP {ni.IPv4Address}.");
                    var apConfig = GetAPConfiguration();
                    return apConfig.Ssid; // AP is ready, exit the method.
                }

                Thread.Sleep(500);
                retries++;
            }

            Debug.WriteLine("FATAL: Timed out waiting for Access Point interface. Rebooting.");
            Power.RebootDevice();
            return ""; // This line will never be reached but satisfies the compiler.
        }

        /// <summary>
        /// Disables the Soft AP for the next restart.
        /// </summary>
        public static void DisableAccessPoint()
        {
            WirelessAPConfiguration wapconf = GetAPConfiguration();
            if (wapconf != null)
            {
                wapconf.Options = WirelessAPConfiguration.ConfigurationOptions.None;
                wapconf.SaveConfiguration();
            }
        }

        /// <summary>
        /// Configures and saves the Wireless AP settings.
        /// </summary>
        /// <returns>True if already configured, false if a reboot is required.</returns>
        private static bool SetupAPConfiguration()
        {
            NetworkInterface ni = GetAPInterface();
            if (ni == null)
            {
                Debug.WriteLine("ERROR: No Wireless AP interface found.");
                return true; // Return true to prevent reboot loop
            }

            WirelessAPConfiguration wapconf = GetAPConfiguration(ni);

            // Check if already enabled and return true
            if (wapconf.Options == (WirelessAPConfiguration.ConfigurationOptions.Enable | WirelessAPConfiguration.ConfigurationOptions.AutoStart) &&
                ni.IPv4Address == ApIpAddress)
            {
                return true;
            }

            ni.EnableStaticIPv4(ApIpAddress, "255.255.255.0", ApIpAddress);

            wapconf.Options = WirelessAPConfiguration.ConfigurationOptions.AutoStart | 
                              WirelessAPConfiguration.ConfigurationOptions.Enable;
            
            wapconf.Ssid = ApSsid;
            wapconf.MaxConnections = 1;
            wapconf.Authentication = AuthenticationType.Open;
            wapconf.Password = "";

            wapconf.SaveConfiguration();

            return false;
        }

        /// <summary>
        /// Gets the network interface for the wireless access point.
        /// </summary>
        private static NetworkInterface GetAPInterface()
        {
            NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();

            foreach (NetworkInterface ni in interfaces)
            {
                if (ni.NetworkInterfaceType == NetworkInterfaceType.WirelessAP)
                {
                    return ni;
                }
            }
            return null;
        }

        /// <summary>
        /// Gets the Wireless AP configuration for the default AP interface.
        /// </summary>
        private static WirelessAPConfiguration GetAPConfiguration()
        {
            NetworkInterface ni = GetAPInterface();
            if (ni != null)
            {
                return GetAPConfiguration(ni);
            }
            return null;
        }

        /// <summary>
        /// Gets the Wireless AP configuration for a specific network interface.
        /// </summary>
        private static WirelessAPConfiguration GetAPConfiguration(NetworkInterface ni)
        {
            return WirelessAPConfiguration.GetAllWirelessAPConfigurations()[ni.SpecificConfigId];
        }

        /// <summary>
        /// Scans for available Wi-Fi networks.
        /// </summary>
        /// <returns>A JSON string representing an array of network SSIDs.</returns>
        public static string ScanWifiNetworks()
        {
            var wifiAdapter = WifiAdapter.FindAllAdapters()[0];
            wifiAdapter.ScanAsync();

            // Give the scan a moment to complete.
            Thread.Sleep(2000);

            var networks = wifiAdapter.NetworkReport.AvailableNetworks;
            string json = "[";
            foreach (var network in networks)
            {
                // Simple JSON serialization by hand to save memory
                json += "\"" + network.Ssid + "\",";
            }

            if (networks.Length > 0)
            {
                json = json.Substring(0, json.Length - 1); // Remove last comma
            }
            json += "]";

            Debug.WriteLine($"Found {networks.Length} networks.");
            return json;
        }
    }
}

