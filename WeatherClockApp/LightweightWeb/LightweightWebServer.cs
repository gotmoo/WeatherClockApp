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
    /// <summary>
    /// An extremely lightweight, memory-efficient web server for .NET nanoFramework.
    /// It handles basic GET and POST requests to serve a configuration page.
    /// </summary>
    public class LightweightWebServer
    {
        private readonly int _port;
        private Thread _serverThread;
        private bool _isRunning = false;
        private Socket _listenSocket;
        private readonly AppSettings _settings;

        /// <summary>
        /// Delegate for handling updated settings.
        /// </summary>
        public delegate void SettingsUpdatedHandler(object sender, AppSettings newSettings);

        /// <summary>
        /// Fired when a user submits new settings via the web form.
        /// </summary>
        public event SettingsUpdatedHandler SettingsUpdated;


        public LightweightWebServer(AppSettings settings, int port = 80)
        {
            _settings = settings;
            _port = port;
        }

        public void Start(string ipAddress)
        {
            if (_isRunning) return;

            _isRunning = true;
            _serverThread = new Thread(() => ListenLoop(ipAddress));
            _serverThread.Start();
            Debug.WriteLine($"Web server started on port {_port}");
        }

        public void Stop()
        {
            if (!_isRunning) return;

            _isRunning = false;
            _listenSocket?.Close();
            _serverThread.Join();
            Debug.WriteLine("Web server stopped.");
        }

        private void ListenLoop(string ipAddress)
        {
            _listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _listenSocket.Bind(new IPEndPoint(IPAddress.Parse(ipAddress), _port));
            _listenSocket.Listen(1); // Listen for 1 connection at a time to save resources

            while (_isRunning)
            {
                try
                {
                    using (Socket clientSocket = _listenSocket.Accept())
                    {
                        HandleClient(clientSocket);
                    }
                }
                catch (Exception ex)
                {
                    if (_isRunning)
                    {
                        Debug.WriteLine($"Error in listen loop: {ex.Message}");
                    }
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

                    Debug.WriteLine($"Request: {method} {url}");

                    if (method == "POST")
                    {
                        string body = GetPostBody(request, requestLines);

                        if (url == "/save-wifi")
                        {
                            ParseWifiFormData(body);
                            SettingsUpdated?.Invoke(this, _settings);
                            ServeRebootPage(stream, "Wi-Fi settings saved. Device is rebooting.");
                        }
                        else if (url == "/save-app-settings")
                        {
                            ParseAppSettingsFormData(body);
                            SettingsUpdated?.Invoke(this, _settings);
                            SendRedirectResponse(stream, "/");
                        }
                        else if (url == "/reboot")
                        {
                            ServeRebootPage(stream, "Device is rebooting now.");
                        }
                    }
                    else if (method == "GET")
                    {
                        if (url == "/")
                        {
                            StreamIndexPage(stream, _settings);
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
                            SendRedirectResponse(stream, "/");
                        }
                    }
                    else
                    {
                        SendResponse(stream, "405 Method Not Allowed", "text/plain", "Method not allowed.");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error handling client: {ex.Message}");
            }
        }

        private void StreamIndexPage(NetworkStream stream, AppSettings settings)
        {
            string headers = "HTTP/1.1 200 OK\r\n" +
                             "Content-Type: text/html\r\n" +
                             "Transfer-Encoding: chunked\r\n" +
                             "Connection: close\r\n\r\n";
            byte[] headerBytes = Encoding.UTF8.GetBytes(headers);
            stream.Write(headerBytes, 0, headerBytes.Length);

            WebServerPages.WriteIndexPage(stream, settings);

            // Send the final zero-length chunk to terminate the response
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
            string headers = $"HTTP/1.1 302 Found\r\n" +
                             $"Location: {location}\r\n" +
                             "Connection: close\r\n\r\n";

            byte[] headerBytes = Encoding.UTF8.GetBytes(headers);
            stream.Write(headerBytes, 0, headerBytes.Length);
        }

        private void SendResponse(NetworkStream stream, string status, string contentType, string content)
        {
            byte[] contentBytes = Encoding.UTF8.GetBytes(content);
            string headers = $"HTTP/1.1 {status}\r\n" +
                             $"Content-Type: {contentType}\r\n" +
                             $"Content-Length: {contentBytes.Length}\r\n" +
                             "Connection: close\r\n\r\n";

            byte[] headerBytes = Encoding.UTF8.GetBytes(headers);
            stream.Write(headerBytes, 0, headerBytes.Length);
            if (contentBytes.Length > 0)
            {
                stream.Write(contentBytes, 0, contentBytes.Length);
            }
        }

        private void ParseWifiFormData(string formData)
        {
            var pairs = formData.Split('&');
            foreach (var pair in pairs)
            {
                var keyValue = pair.Split('=');
                if (keyValue.Length == 2)
                {
                    string key = WebUtility.UrlDecode(keyValue[0]);
                    string value = WebUtility.UrlDecode(keyValue[1]);

                    switch (key)
                    {
                        case "ssid": _settings.Ssid = value; break;
                        case "password": _settings.Password = value; break;
                    }
                }
            }

            if (!string.IsNullOrEmpty(_settings.Ssid)) _settings.IsConfigured = true;
        }

        private void ParseAppSettingsFormData(string formData)
        {
            var pairs = formData.Split('&');

            // IMPORTANT: HTML forms do NOT submit unchecked checkboxes.
            // We must reset this to false before parsing. If the box is checked, 
            // the "panelReversed" key will appear in the loop and set it to true.
            _settings.PanelReversed = false;

            foreach (var pair in pairs)
            {
                var keyValue = pair.Split('=');
                if (keyValue.Length == 2)
                {
                    string key = WebUtility.UrlDecode(keyValue[0]);
                    string value = WebUtility.UrlDecode(keyValue[1]);

                    switch (key)
                    {
                        case "weatherApiKey": _settings.WeatherApiKey = value; break;
                        case "locationName": _settings.LocationName = value; break;
                        case "latitude": double.TryParse(value, out var lat); _settings.Latitude = lat; break;
                        case "longitude": double.TryParse(value, out var lon); _settings.Longitude = lon; break;
                        // --- Re-added missing fields ---
                        case "displayPanels": int.TryParse(value, out var panels); _settings.DisplayPanels = panels; break;
                        case "panelRotation": int.TryParse(value, out var rot); _settings.PanelRotation = rot; break;
                        case "panelBrightness": int.TryParse(value, out var bri); _settings.PanelBrightness = bri; break;
                        case "panelReversed": _settings.PanelReversed = value == "true"; break;
                    }
                }
            }
        }
    }
}