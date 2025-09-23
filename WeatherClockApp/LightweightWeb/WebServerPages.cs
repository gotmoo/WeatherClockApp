using System.Net.Sockets;
using System.Text;
using WeatherClockApp.Models;

namespace WeatherClockApp.LightweightWeb
{
    internal static class WebServerPages
    {
        /// <summary>
        /// Writes the main index page to the network stream using chunked encoding to save memory.
        /// </summary>
        internal static void WriteIndexPage(NetworkStream stream, AppSettings settings)
        {
            WriteChunk(stream, @"
<!DOCTYPE html>
<html>
<head>
    <title>ESP32 Weather Clock Config</title>
    <meta name='viewport' content='width=device-width, initial-scale=1'>
    <style>
        body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; background-color: #f0f2f5; color: #333; margin: 0; padding: 20px; }
        .container { max-width: 600px; margin: 20px auto; background-color: #fff; padding: 20px; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }
        h1, h2 { color: #1c1e21; text-align: center; border-bottom: 1px solid #ddd; padding-bottom: 10px; margin-bottom: 20px; }
        .form-group { margin-bottom: 15px; }
        label { display: block; font-weight: bold; margin-bottom: 5px; }
        input[type='text'], input[type='password'], input[type='number'] { width: 100%; padding: 10px; border: 1px solid #ddd; border-radius: 4px; box-sizing: border-box; }
        .btn { background-color: #007bff; color: #fff; padding: 12px 20px; border: none; border-radius: 4px; cursor: pointer; width: 100%; font-size: 16px; font-weight: bold; }
        .btn-secondary { background-color: #6c757d; }
        .btn-secondary:hover { background-color: #5a6268; }
        .btn:hover { background-color: #0056b3; }
        .inline-group { display: flex; align-items: center; }
        .inline-group input { flex-grow: 1; }
        .inline-group button { margin-left: 10px; width: auto; }
        .footer { text-align: center; margin-top: 20px; font-size: 12px; color: #888; }
        #wifi-scan-results, #location-results { margin-top: 15px; }
        .radio-label { display: block; padding: 5px; border: 1px solid transparent; border-radius: 4px; }
        .radio-label:hover { background-color: #f0f2f5; }
        .spinner { border: 4px solid #f3f3f3; border-top: 4px solid #3498db; border-radius: 50%; width: 20px; height: 20px; animation: spin 2s linear infinite; display: none; margin: 10px auto; }
        @keyframes spin { 0% { transform: rotate(0deg); } 100% { transform: rotate(360deg); } }
    </style>
</head>
<body>
    <div class='container'>
        <h1>Weather Clock Setup</h1>
        
        <!-- Wi-Fi Settings Form -->
        <div class='wifi-form'>
            <h2>Wi-Fi Configuration</h2>
            <form action='/save-wifi' method='post'>
                <div class='form-group inline-group'>
                    <input type='text' id='ssid' name='ssid' required placeholder='Select or type Wi-Fi SSID' value='");
            WriteChunk(stream, settings.Ssid ?? "");
            WriteChunk(stream, @"'>
                    <button type='button' id='scan-wifi-btn' class='btn btn-secondary'>Scan</button>
                </div>
                <div class='form-group'>
                    <div id='wifi-scan-results'></div>
                    <div id='wifi-spinner' class='spinner'></div>
                </div>
                <div class='form-group inline-group'>
                    <input type='password' id='password' name='password' placeholder='Wi-Fi Password' value='");
            WriteChunk(stream, settings.Password ?? "");
            WriteChunk(stream, @"'>
                    <button type='button' id='show-pw-btn' class='btn btn-secondary'>Show</button>
                </div>
                <button type='submit' class='btn'>Save Wi-Fi & Reboot</button>
            </form>
        </div>
        
        <!-- App Settings Form -->
        <div class='app-settings-form'>
            <h2>Application Settings</h2>
            <form action='/save-app-settings' method='post'>
                <div class='form-group'>
                    <label for='weatherApiKey'>Weather API Key</label>
                    <input type='text' id='weatherApiKey' name='weatherApiKey' value='");
            WriteChunk(stream, settings.WeatherApiKey ?? "");
            WriteChunk(stream, @"'>
                </div>
                <div class='form-group'>
                    <label for='locationSearch'>Location Search</label>
                    <div class='inline-group'>
                        <input type='text' id='locationSearch' placeholder='e.g., London, UK'>
                        <button type='button' id='search-location-btn' class='btn btn-secondary'>Search</button>
                    </div>
                </div>
                <div class='form-group'>
                     <div id='location-results'></div>
                     <div id='location-spinner' class='spinner'></div>
                </div>
                <div class='form-group'>
                    <label for='locationName'>Selected Location</label>
                    <input type='text' id='locationName' name='locationName' readonly value='");
            WriteChunk(stream, settings.LocationName ?? "");
            WriteChunk(stream, @"'>
                </div>
                <div class='form-group'>
                    <label for='latitude'>Latitude</label>
                    <input type='number' step='any' id='latitude' name='latitude' value='");
            WriteChunk(stream, settings.Latitude.ToString());
            WriteChunk(stream, @"'>
                </div>
                <div class='form-group'>
                    <label for='longitude'>Longitude</label>
                    <input type='number' step='any' id='longitude' name='longitude' value='");
            WriteChunk(stream, settings.Longitude.ToString());
            WriteChunk(stream, @"'>
                </div>
                <button type='submit' class='btn'>Save App Settings</button>
            </form>
        </div>
        
        <!-- Reboot Form -->
        <div class='reboot-form'>
             <h2>Device Management</h2>
            <form action='/reboot' method='post' onsubmit='return confirm(""Are you sure you want to reboot?"");'>
                <button type='submit' class='btn btn-secondary'>Reboot Device</button>
            </form>
        </div>

    </div>
    <div class='footer'>ESP32 nanoFramework Server</div>

    <script>
        // --- Wi-Fi Scan ---
        const scanBtn = document.getElementById('scan-wifi-btn');
        const ssidInput = document.getElementById('ssid');
        const wifiResultsDiv = document.getElementById('wifi-scan-results');
        const wifiSpinner = document.getElementById('wifi-spinner');

        scanBtn.addEventListener('click', async () => {
            wifiResultsDiv.innerHTML = '';
            wifiSpinner.style.display = 'block';
            try {
                const response = await fetch('/api/scan-wifi');
                const networks = await response.json();
                wifiSpinner.style.display = 'none';

                if(networks.length > 0) {
                    networks.forEach(net => {
                        const radio = document.createElement('input');
                        radio.type = 'radio';
                        radio.name = 'selected_ssid';
                        radio.value = net;
                        radio.id = 'ssid_' + net;
                        radio.addEventListener('change', () => ssidInput.value = net);

                        const label = document.createElement('label');
                        label.htmlFor = 'ssid_' + net;
                        label.textContent = net;
                        label.classList.add('radio-label');
                        
                        const container = document.createElement('div');
                        container.appendChild(radio);
                        container.appendChild(label);
                        wifiResultsDiv.appendChild(container);
                    });
                } else {
                    wifiResultsDiv.textContent = 'No networks found.';
                }
            } catch (e) {
                wifiSpinner.style.display = 'none';
                wifiResultsDiv.textContent = 'Failed to scan for networks.';
            }
        });

        // --- Show Password ---
        const showPwBtn = document.getElementById('show-pw-btn');
        const passwordInput = document.getElementById('password');
        showPwBtn.addEventListener('click', () => {
            if(passwordInput.type === 'password') {
                passwordInput.type = 'text';
                showPwBtn.textContent = 'Hide';
            } else {
                passwordInput.type = 'password';
                showPwBtn.textContent = 'Show';
            }
        });

        // --- Location Search ---
        const searchLocationBtn = document.getElementById('search-location-btn');
        const locationSearchInput = document.getElementById('locationSearch');
        const locationResultsDiv = document.getElementById('location-results');
        const locationSpinner = document.getElementById('location-spinner');
        const locationNameInput = document.getElementById('locationName');
        const latitudeInput = document.getElementById('latitude');
        const longitudeInput = document.getElementById('longitude');

        searchLocationBtn.addEventListener('click', async () => {
            const query = locationSearchInput.value;
            if(!query) return;

            locationResultsDiv.innerHTML = '';
            locationSpinner.style.display = 'block';

            try {
                const response = await fetch(`/api/geo-resolve?location=${encodeURIComponent(query)}`);
                const locations = await response.json();
                locationSpinner.style.display = 'none';

                if (locations.length > 0) {
                    locations.forEach((loc, index) => {
                        const radioId = `loc_${index}`;
                        const displayName = `${loc.name}, ${loc.state}, ${loc.country}`;
                        
                        const radio = document.createElement('input');
                        radio.type = 'radio';
                        radio.name = 'selected_location';
                        radio.id = radioId;
                        radio.dataset.lat = loc.lat;
                        radio.dataset.lon = loc.lon;
                        radio.dataset.name = displayName;
                        radio.addEventListener('change', () => {
                            locationNameInput.value = radio.dataset.name;
                            latitudeInput.value = radio.dataset.lat;
                            longitudeInput.value = radio.dataset.lon;
                        });

                        const label = document.createElement('label');
                        label.htmlFor = radioId;
                        label.textContent = displayName;
                        label.classList.add('radio-label');
                        
                        const container = document.createElement('div');
                        container.appendChild(radio);
                        container.appendChild(label);
                        locationResultsDiv.appendChild(container);
                    });
                } else {
                    locationResultsDiv.textContent = 'No locations found.';
                }
            } catch (e) {
                locationSpinner.style.display = 'none';
                locationResultsDiv.textContent = 'Failed to search for locations.';
            }
        });
    </script>
</body>
</html>");
        }

        internal static string RebootPage(string message)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <title>Rebooting...</title>
    <meta http-equiv='refresh' content='10;url=/'>
    <style>
        body {{ font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; background-color: #f0f2f5; color: #333; display: flex; justify-content: center; align-items: center; height: 100vh; margin: 0; }}
        .container {{ text-align: center; padding: 20px; }}
        h1 {{ color: #1c1e21; }}
        p {{ font-size: 1.2em; }}
        .spinner {{ border: 8px solid #f3f3f3; border-top: 8px solid #007bff; border-radius: 50%; width: 60px; height: 60px; animation: spin 2s linear infinite; margin: 20px auto; }}
        @keyframes spin {{ 0% {{ transform: rotate(0deg); }} 100% {{ transform: rotate(360deg); }} }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='spinner'></div>
        <h1>{message}</h1>
        <p>You will be redirected shortly. Please reconnect to the Wi-Fi if necessary.</p>
    </div>
</body>
</html>";
        }

        private static void WriteChunk(NetworkStream stream, string data)
        {
            if (string.IsNullOrEmpty(data)) return;

            byte[] bytes = Encoding.UTF8.GetBytes(data);
            byte[] lengthHeader = Encoding.UTF8.GetBytes($"{bytes.Length:X}\r\n");
            stream.Write(lengthHeader, 0, lengthHeader.Length);
            stream.Write(bytes, 0, bytes.Length);
            stream.Write(new byte[] { 13, 10 }, 0, 2); // \r\n
        }
    }
}

