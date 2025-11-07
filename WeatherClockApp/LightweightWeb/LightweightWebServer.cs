using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using WeatherClockApp.Helpers;
using WeatherClockApp.Managers;
using WeatherClockApp.Models;
using nanoFramework.Runtime.Native;


namespace WeatherClockApp.LightweightWeb
{
    internal class LightweightWebServer
    {
        private readonly HttpListener _listener;
        private readonly AppSettings _settings;

        public event EventHandler<AppSettings> SettingsUpdated;

        public LightweightWebServer(AppSettings settings)
        {
            _settings = settings;
            _listener = new HttpListener("http");
        }

        public void Start(string ipAddress)
        {
            // HttpListener in nanoFramework binds to the port on all available IPs.
            // The Prefixes collection is not used.
            _listener.Start();
            Debug.WriteLine($"Web server started on http://{ipAddress}/");

            var listenerThread = new Thread(ListenLoop);
            listenerThread.Start();
        }

        private void ListenLoop()
        {
            while (true)
            {
                try
                {
                    var context = _listener.GetContext();
                    // nanoFramework does not have ThreadPool, so we create a new thread for each request.
                    new Thread(() => HandleClient(context)).Start();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Listener loop error: {ex.Message}");
                }
            }
        }

        private void HandleClient(object contextObj)
        {
            var context = (HttpListenerContext)contextObj;
            var request = context.Request;
            var response = context.Response;
            string body = null;

            Debug.WriteLine($"Request: {request.HttpMethod} {request.Url.AbsolutePath}");

            if (request.HttpMethod == "POST")
            {
                // Corrected line: Removed the second parameter which is not available in nanoFramework
                using (var reader = new StreamReader(request.InputStream))
                {
                    body = reader.ReadToEnd();
                }
            }

            try
            {
                switch (request.Url.AbsolutePath)
                {
                    case "/":
                        response.ContentType = "text/html";
                        WebServerPages.WriteIndexPage(response.OutputStream, _settings);
                        break;

                    case "/wifiscan":
                        response.ContentType = "application/json";
                        var networksJson = NetworkManager.ScanWifiNetworks();
                        byte[] buffer = Encoding.UTF8.GetBytes(networksJson);
                        response.ContentLength64 = buffer.Length;
                        response.OutputStream.Write(buffer, 0, buffer.Length);
                        break;

                    case "/locationsearch":
                        string query = WebUtility.GetParamValue(request.Url.Query, "q");
                        response.ContentType = "application/json";
                        var locationsJson = WeatherManager.GeoLocate(query, _settings.WeatherApiKey);
                        byte[] locBuffer = Encoding.UTF8.GetBytes(locationsJson);
                        response.ContentLength64 = locBuffer.Length;
                        response.OutputStream.Write(locBuffer, 0, locBuffer.Length);
                        break;

                    case "/save-wifi":
                        ParseWifiFormData(body);
                        SettingsUpdated?.Invoke(this, _settings);
                        ServeRebootPage(response, "Wi-Fi settings saved. Device is rebooting.");
                        break;

                    case "/save-app-settings":
                        ParseAppSettingsFormData(body);
                        SettingsUpdated?.Invoke(this, _settings);
                        Redirect(response, "/");
                        break;

                    case "/reboot":
                        ServeRebootPage(response, "Rebooting device now.");
                        break;

                    case "/refresh-weather":
                        WeatherClock.TriggerWeatherUpdate();
                        response.StatusCode = 200; // OK
                        break;

                    case "/favicon.ico":
                        response.StatusCode = 404; // Not Found
                        break;

                    default:
                        Redirect(response, "/");
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Request handler error: {ex.Message}");
                response.StatusCode = 500;
            }
            finally
            {
                response.OutputStream.Close();
            }
        }
        private void ParseWifiFormData(string body)
        {
            _settings.Ssid = WebUtility.UrlDecode(WebUtility.GetParamValue(body, "ssid"));
            _settings.Password = WebUtility.UrlDecode(WebUtility.GetParamValue(body, "password"));
            _settings.IsConfigured = true;
        }

        private void ParseAppSettingsFormData(string body)
        {
            _settings.WeatherApiKey = WebUtility.UrlDecode(WebUtility.GetParamValue(body, "weatherApiKey"));
            _settings.LocationName = WebUtility.UrlDecode(WebUtility.GetParamValue(body, "locationName"));

            if (double.TryParse(WebUtility.UrlDecode(WebUtility.GetParamValue(body, "latitude")), out var lat))
                _settings.Latitude = lat;
            if (double.TryParse(WebUtility.UrlDecode(WebUtility.GetParamValue(body, "longitude")), out var lon))
                _settings.Longitude = lon;

            // New settings
            if (int.TryParse(WebUtility.UrlDecode(WebUtility.GetParamValue(body, "panelRotation")), out var rotation))
                _settings.PanelRotation = rotation;
            if (int.TryParse(WebUtility.UrlDecode(WebUtility.GetParamValue(body, "panelClock")), out var clockPin))
                _settings.PanelClock = clockPin;
            if (int.TryParse(WebUtility.UrlDecode(WebUtility.GetParamValue(body, "panelMosi")), out var mosiPin))
                _settings.PanelMosi = mosiPin;
            if (int.TryParse(WebUtility.UrlDecode(WebUtility.GetParamValue(body, "panelBrightness")), out var brightness))
                _settings.PanelBrightness = brightness;

            _settings.WeatherUnit = WebUtility.UrlDecode(WebUtility.GetParamValue(body, "weatherUnit"));
            _settings.WeatherDisplay = WebUtility.UrlDecode(WebUtility.GetParamValue(body, "weatherDisplay"));
            _settings.ShowDegreesSymbol = WebUtility.UrlDecode(WebUtility.GetParamValue(body, "showDegrees")) == "on";
        }

        private void Redirect(HttpListenerResponse response, string url)
        {
            response.StatusCode = 302;
            response.RedirectLocation = url;
        }

        private void ServeRebootPage(HttpListenerResponse response, string message)
        {
            response.ContentType = "text/html";
            WebServerPages.WriteRebootPage(response.OutputStream, message);

            var rebootThread = new Thread(() =>
            {
                Thread.Sleep(3000);
                Power.RebootDevice();
            });
            rebootThread.Start();
        }
    }
}

