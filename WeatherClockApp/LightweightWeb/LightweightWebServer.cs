using nanoFramework.Runtime.Native;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using WeatherClockApp.Helpers;
using WeatherClockApp.Managers;
using WeatherClockApp.Models;

namespace WeatherClockApp.LightweightWeb
{
    public class LightweightWebServer
    {
        private readonly int _port;
        private Thread _serverThread;
        private bool _isRunning = false;
        private Socket _listenSocket;
        private AppSettings _settings;
        private readonly bool _isApMode;

        public delegate void SettingsUpdatedHandler(object sender, AppSettings newSettings);

        public event SettingsUpdatedHandler SettingsUpdated;

        public LightweightWebServer(AppSettings settings, bool isApMode, int port = 80)
        {
            _settings = settings;
            _isApMode = isApMode;
            _port = port;
        }

        public void Start(string ipAddress)
        {
            if (_isRunning) return;
            _isRunning = true;
            _serverThread = new Thread(() => ListenLoop(ipAddress));
            _serverThread.Start();
            Debug.WriteLine($"Web server started on port {_port} (AP Mode: {_isApMode})");
        }

        public void Stop()
        {
            if (!_isRunning) return;
            _isRunning = false;
            _listenSocket?.Close();
            _serverThread.Join();
        }

        private void ListenLoop(string ipAddress)
        {
            _listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _listenSocket.Bind(new IPEndPoint(IPAddress.Parse(ipAddress), _port));
            _listenSocket.Listen(2);

            while (_isRunning)
            {
                try
                {
                    using (Socket clientSocket = _listenSocket.Accept())
                    {
                        HandleClient(clientSocket);
                    }

                    // Critical for stability: Force GC after every request to compact heap
                    nanoFramework.Runtime.Native.GC.Run(true);
                    Debug.WriteLine($"Free Mem: {nanoFramework.Runtime.Native.GC.Run(false)}");
                }
                catch (Exception ex)
                {
                    if (_isRunning) Debug.WriteLine($"Listen error: {ex.Message}");
                }
            }
        }

        private void HandleClient(Socket clientSocket)
        {
            try
            {
                using (var stream = new NetworkStream(clientSocket, true))
                {
                    byte[] buffer = new byte[1024];
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead == 0) return;

                    string request = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    string[] requestLines = request.Split('\r', '\n');
                    string[] requestLine = requestLines[0].Split(' ');

                    if (requestLine.Length < 2) return;

                    string method = requestLine[0];
                    string url = requestLine[1].ToLower();

                    if (method == "POST")
                    {
                        string body = GetPostBody(request, requestLines);

                        if (url == "/save-wifi")
                        {
                            ParseWifiFormData(body);
                            SettingsUpdated?.Invoke(this, _settings);
                            ServeRebootPage(stream, "Wi-Fi saved. Rebooting...");
                        }
                        else if (url == "/save-location")
                        {
                            ParseLocationFormData(body);
                            SettingsUpdated?.Invoke(this, _settings);

                            // Immediate update
                            WeatherClock.TriggerWeatherUpdate();

                            SendRedirectResponse(stream, "/"); // Reloads page, logic will determine active tab
                        }
                        else if (url == "/save-clock")
                        {
                            var oldWeatherUnit = _settings.WeatherUnit;
                            ParseClockFormData(body);

                            SettingsUpdated?.Invoke(this, _settings);

                            // Trigger redraw to show new time format immediately
                            _settings = SettingsManager.LoadSettings();
                            DisplayManager.UpdateSettings(_settings);

                            if (oldWeatherUnit != _settings.WeatherUnit)
                            {
                                WeatherClock.TriggerWeatherUpdate(); // Re-fetch weather if unit changed
                            }

                            WeatherClock.TriggerWeatherScroll();

                            SendRedirectResponse(stream, "/");
                        }
                        else if (url == "/save-display")
                        {
                            ParseDisplayFormData(body);
                            SettingsUpdated?.Invoke(this, _settings);

                            // Immediate update
                            _settings = SettingsManager.LoadSettings();
                            DisplayManager.UpdateSettings(_settings);

                            SendRedirectResponse(stream, "/");
                        }
                        else if (url == "/reboot")
                        {
                            ServeRebootPage(stream, "Rebooting...");
                        }
                        else
                        {
                            SendRedirectResponse(stream, "/");
                        }
                    }
                    else if (method == "GET")
                    {
                        if (url == "/")
                        {
                            // Determine default tab based on state
                            string activeTab = "display"; // Default

                            if (_isApMode || !_settings.IsConfigured)
                            {
                                activeTab = "network";
                            }
                            else if (string.IsNullOrEmpty(_settings.WeatherApiKey))
                            {
                                activeTab = "location";
                            }

                            StreamIndexPage(stream, _settings, activeTab);
                        }
                        else if (url.StartsWith("/api/scan-wifi"))
                        {
                            SendResponse(stream, "200 OK", "application/json", NetworkManager.ScanWifiNetworks());
                        }
                        else if (url.StartsWith("/api/geo-resolve"))
                        {
                            var location = WebUtility.GetParamValue(url, "location");
                            var content = WeatherManager.GeoLocate(location, _settings.WeatherApiKey);
                            SendResponse(stream, "200 OK", "application/json", content);
                        }
                        else
                        {
                            // Captive portal fallback: redirect to root
                            SendRedirectResponse(stream, "/");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Client error: {ex.Message}");
            }
        }

        private void StreamIndexPage(NetworkStream stream, AppSettings settings, string activeTab)
        {
            string headers =
                "HTTP/1.1 200 OK\r\nContent-Type: text/html\r\nTransfer-Encoding: chunked\r\nConnection: close\r\n\r\n";
            byte[] headerBytes = Encoding.UTF8.GetBytes(headers);
            stream.Write(headerBytes, 0, headerBytes.Length);

            WebServerPages.WriteIndexPage(stream, settings, activeTab);

            byte[] finalChunk = Encoding.UTF8.GetBytes("0\r\n\r\n");
            stream.Write(finalChunk, 0, finalChunk.Length);
        }

        private string GetPostBody(string rawRequest, string[] requestLines)
        {
            int contentLength = 0;
            foreach (var line in requestLines)
            {
                if (line.StartsWith("Content-Length:"))
                {
                    contentLength = int.Parse(line.Substring(15).Trim());
                    break;
                }
            }

            if (contentLength > 0)
            {
                int bodyStartIndex = rawRequest.IndexOf("\r\n\r\n") + 4;
                if (bodyStartIndex > 3 && rawRequest.Length >= bodyStartIndex + contentLength)
                {
                    return rawRequest.Substring(bodyStartIndex, contentLength);
                }
            }

            return "";
        }

        private void ServeRebootPage(NetworkStream stream, string message)
        {
            SendResponse(stream, "200 OK", "text/html", WebServerPages.RebootPage(message));
            new Thread(() =>
            {
                Thread.Sleep(2000);
                Power.RebootDevice();
            }).Start();
        }

        private void SendRedirectResponse(NetworkStream stream, string location)
        {
            string headers = $"HTTP/1.1 302 Found\r\nLocation: {location}\r\nConnection: close\r\n\r\n";
            byte[] headerBytes = Encoding.UTF8.GetBytes(headers);
            stream.Write(headerBytes, 0, headerBytes.Length);
        }

        private void SendResponse(NetworkStream stream, string status, string contentType, string content)
        {
            byte[] contentBytes = Encoding.UTF8.GetBytes(content);
            string headers =
                $"HTTP/1.1 {status}\r\nContent-Type: {contentType}\r\nContent-Length: {contentBytes.Length}\r\nConnection: close\r\n\r\n";
            byte[] headerBytes = Encoding.UTF8.GetBytes(headers);
            stream.Write(headerBytes, 0, headerBytes.Length);
            if (contentBytes.Length > 0) stream.Write(contentBytes, 0, contentBytes.Length);
        }

        private void ParseWifiFormData(string formData)
        {
            var pairs = formData.Split('&');
            foreach (var pair in pairs)
            {
                var keyValue = pair.Split('=');
                if (keyValue.Length != 2) continue;
                string key = WebUtility.UrlDecode(keyValue[0]);
                string value = WebUtility.UrlDecode(keyValue[1]);

                if (key == "ssid") _settings.Ssid = value;
                if (key == "password") _settings.Password = value;
            }

            if (!string.IsNullOrEmpty(_settings.Ssid)) _settings.IsConfigured = true;
        }

        private void ParseLocationFormData(string formData)
        {
            var pairs = formData.Split('&');
            foreach (var pair in pairs)
            {
                var keyValue = pair.Split('=');
                if (keyValue.Length != 2) continue;
                string key = WebUtility.UrlDecode(keyValue[0]);
                string value = WebUtility.UrlDecode(keyValue[1]);

                if (key == "weatherApiKey") _settings.WeatherApiKey = value;
                if (key == "locationName") _settings.LocationName = value;
                if (key == "latitude" && double.TryParse(value, out var lat)) _settings.Latitude = lat;
                if (key == "longitude" && double.TryParse(value, out var lon)) _settings.Longitude = lon;
                if (key == "weatherRefreshMinutes" && int.TryParse(value, out var upd))
                    _settings.WeatherRefreshMinutes = upd;
            }
        }

        private void ParseClockFormData(string formData)
        {
            var pairs = formData.Split('&');
            // Checkbox defaults (unchecked checkboxes are not sent)
            _settings.ShowDescription = false;
            _settings.ShowFeelsLike = false;
            _settings.ShowMinTemp = false;
            _settings.ShowMaxTemp = false;
            _settings.ShowHumidity = false;
            _settings.ShowDegreesSymbol = false;

            foreach (var pair in pairs)
            {
                var keyValue = pair.Split('=');
                if (keyValue.Length != 2) continue;
                string key = WebUtility.UrlDecode(keyValue[0]);
                string value = WebUtility.UrlDecode(keyValue[1]);

                if (key == "is24HourFormat") _settings.Is24HourFormat = value == "true";
                if (key == "showDegrees") _settings.ShowDegreesSymbol = value == "true";
                if (key == "weatherUnit") _settings.WeatherUnit = value;
                if (key == "scrollFrequencyMinutes" && int.TryParse(value, out var f))
                    _settings.ScrollFrequencyMinutes = f;

                if (key == "showDescription") _settings.ShowDescription = value == "true";
                if (key == "showFeelsLike") _settings.ShowFeelsLike = value == "true";
                if (key == "showMinTemp") _settings.ShowMinTemp = value == "true";
                if (key == "showMaxTemp") _settings.ShowMaxTemp = value == "true";
                if (key == "showHumidity") _settings.ShowHumidity = value == "true";
            }
        }

        private void ParseDisplayFormData(string formData)
        {
            var pairs = formData.Split('&');
            // Checkbox default check
            _settings.PanelReversed = false;

            foreach (var pair in pairs)
            {
                var keyValue = pair.Split('=');
                if (keyValue.Length != 2) continue;
                string key = WebUtility.UrlDecode(keyValue[0]);
                string value = WebUtility.UrlDecode(keyValue[1]);

                if (key == "displayPanels" && int.TryParse(value, out var p)) _settings.DisplayPanels = p;
                if (key == "panelRotation" && int.TryParse(value, out var r)) _settings.PanelRotation = r;
                if (key == "panelBrightness" && int.TryParse(value, out var b)) _settings.PanelBrightness = b;
                if (key == "fontName") _settings.FontName = value;
                if (key == "panelReversed") _settings.PanelReversed = value == "true";
            }
        }
    }
}