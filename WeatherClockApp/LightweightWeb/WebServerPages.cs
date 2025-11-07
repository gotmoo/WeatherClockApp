using System.IO;
using System.Text;
using WeatherClockApp.Models;

namespace WeatherClockApp.LightweightWeb
{
    internal static class WebServerPages
    {
        /// <summary>
        /// Writes the main configuration page directly to the network stream in chunks.
        /// </summary>
        public static void WriteIndexPage(Stream stream, AppSettings settings)
        {
            using (var writer = new StreamWriter(stream)) // Correct constructor for nanoFramework
            {
                writer.Write("<!DOCTYPE html><html><head><title>ESP32 Weather Clock</title>");
                writer.Write("<meta name='viewport' content='width=device-width, initial-scale=1'>");
                writer.Write("<style>body{font-family:sans-serif;background:#f0f2f5;color:#333;margin:0;padding:20px}h1,h2{text-align:center}.container{max-width:600px;margin:20px auto;background:#fff;padding:20px;border-radius:8px;box-shadow:0 2px 4px rgba(0,0,0,.1)}.form-group{margin-bottom:15px}label{display:block;font-weight:700;margin-bottom:5px}input[type=text],input[type=password],select{width:100%;padding:10px;border:1px solid #ddd;border-radius:4px;box-sizing:border-box}.btn{background:#007bff;color:#fff;padding:12px 20px;border:none;border-radius:4px;cursor:pointer;width:100%;font-size:16px;font-weight:700}.btn:hover{background:#0056b3}.btn-secondary{background:#6c757d;margin-top:10px}.btn-secondary:hover{background:#5a6268}.hidden{display:none}#location-results{border:1px solid #ddd;border-radius:4px;margin-top:5px}#location-results div{padding:10px;cursor:pointer}#location-results div:hover{background:#f0f0f0}</style>");
                writer.Write("</head><body><div class='container'><h1>Weather Clock Setup</h1>");

                // Wifi Form
                writer.Write("<h2>Wi-Fi Configuration</h2><form action='/save-wifi' method='post' id='wifi-form'>");
                writer.Write($"<div class='form-group'><label for='ssid'>Wi-Fi SSID</label><input type='text' id='ssid' name='ssid' value='{settings.Ssid}' required></div>");
                writer.Write("<div class='form-group'><label>Available Networks:</label><select id='wifi-networks' size='3' style='height:80px;'></select><button type='button' class='btn-secondary' onclick='scanWifi()'>Scan for Networks</button></div>");
                writer.Write("<div class='form-group'><label for='password'>Wi-Fi Password</label><input type='password' id='password' name='password'><input type='checkbox' onclick='togglePassword()'> Show Password</div>");
                writer.Write("<button type='submit' class='btn'>Save Wi-Fi & Reboot</button></form>");
                writer.Write("<hr style='margin: 30px 0;'>");

                // App Settings Form
                writer.Write("<h2>Application Settings</h2><form action='/save-app-settings' method='post' id='settings-form'>");
                writer.Write($"<div class='form-group'><label for='weatherApiKey'>Weather API Key</label><input type='text' id='weatherApiKey' name='weatherApiKey' value='{settings.WeatherApiKey}'></div>");
                writer.Write("<div class='form-group'><label for='location-search'>Search for Location</label><input type='text' id='location-search' onkeyup='searchLocation(this.value)'><div id='location-results'></div></div>");
                writer.Write($"<div class='form-group'><label for='locationName'>Location Name</label><input type='text' id='locationName' name='locationName' value='{settings.LocationName}'></div>");
                writer.Write($"<div class='form-group'><label for='latitude'>Latitude</label><input type='text' id='latitude' name='latitude' value='{settings.Latitude}'></div>");
                writer.Write($"<div class='form-group'><label for='longitude'>Longitude</label><input type='text' id='longitude' name='longitude' value='{settings.Longitude}'></div>");

                // Advanced Display Settings
                writer.Write("<hr style='margin: 30px 0;'><h2>Advanced Display Settings</h2>");
                writer.Write($"<div class='form-group'><label for='panelRotation'>Panel Rotation</label><select id='panelRotation' name='panelRotation'><option value='0' {(settings.PanelRotation == 0 ? "selected" : "")}>Normal (0&deg;)</option><option value='2' {(settings.PanelRotation == 2 ? "selected" : "")}>Flipped (180&deg;)</option></select></div>");
                writer.Write($"<div class='form-group'><label for='panelClock'>Panel Clock Pin</label><input type='text' id='panelClock' name='panelClock' value='{settings.PanelClock}'></div>");
                writer.Write($"<div class='form-group'><label for='panelMosi'>Panel MOSI Pin</label><input type='text' id='panelMosi' name='panelMosi' value='{settings.PanelMosi}'></div>");
                writer.Write($"<div class='form-group'><label for='panelBrightness'>Brightness (1-8)</label><select id='panelBrightness' name='panelBrightness'>");
                for (int i = 1; i <= 8; i++) { writer.Write($"<option value='{i}' {(settings.PanelBrightness == i ? "selected" : "")}>{i}</option>"); }
                writer.Write("</select></div>");

                // Advanced Weather Settings
                writer.Write("<hr style='margin: 30px 0;'><h2>Advanced Weather Settings</h2>");
                writer.Write($"<div class='form-group'><label for='weatherUnit'>Units</label><select id='weatherUnit' name='weatherUnit'><option value='imperial' {(settings.WeatherUnit == "imperial" ? "selected" : "")}>Fahrenheit</option><option value='metric' {(settings.WeatherUnit == "metric" ? "selected" : "")}>Celsius</option></select></div>");
                writer.Write($"<div class='form-group'><label for='showDegrees'>Show &deg; Symbol</label><input type='checkbox' id='showDegrees' name='showDegrees' {(settings.ShowDegreesSymbol ? "checked" : "")}></div>");
                writer.Write($"<div class='form-group'><label for='weatherDisplay'>Custom Scroll Text</label><input type='text' id='weatherDisplay' name='weatherDisplay' value='{settings.WeatherDisplay}'><small>Placeholders: {{temp}}, {{feels_like}}, {{temp_min}}, {{temp_max}}, {{description}}, {{humidity}}, {{city}}</small></div>");
                writer.Write("<button type='submit' class='btn'>Save Application Settings</button>");
                writer.Write("<button type='button' class='btn btn-secondary' onclick='refreshWeather()'>Refresh Weather Data</button>");
                writer.Write("</form>");

                // Manual Reboot
                writer.Write("<hr style='margin: 30px 0;'><form action='/reboot' method='post'><button type='submit' class='btn btn-secondary'>Reboot Device</button></form>");
                writer.Write("</div>");

                // JavaScript
                writer.Write("<script>");
                writer.Write("function scanWifi(){var e=document.getElementById('wifi-networks');e.innerHTML='<option>Scanning...</option>',fetch('/wifiscan').then(t=>t.json()).then(t=>{e.innerHTML='',t.forEach(t=>{var n=document.createElement('option');n.value=t,n.innerText=t,e.appendChild(n)})}).catch(()=>{e.innerHTML='<option>Scan failed</option>'})}");
                writer.Write("document.getElementById('wifi-networks').ondblclick=function(){var e=document.getElementById('ssid');e.value=this.value};");
                writer.Write("function togglePassword(){var e=document.getElementById('password');'password'===e.type?e.type='text':e.type='password'}");
                writer.Write("function searchLocation(e){if(e.length<3)return;var t=document.getElementById('location-results');t.innerHTML='<div>Searching...</div>',fetch('/locationsearch?q='+e).then(e=>e.json()).then(e=>{t.innerHTML='',e.forEach(e=>{var n=document.createElement('div');n.innerText=e.name+', '+e.state+', '+e.country,n.onclick=function(){selectLocation(e)},t.appendChild(n)})}).catch(()=>{t.innerHTML='<div>Search failed</div>'})}");
                writer.Write("function selectLocation(e){document.getElementById('locationName').value=e.name+', '+e.state+', '+e.country,document.getElementById('latitude').value=e.lat,document.getElementById('longitude').value=e.lon,document.getElementById('location-results').innerHTML=''}");
                writer.Write("function refreshWeather(){fetch('/refresh-weather',{method:'POST'}).then(e=>{e.ok?alert('Weather refresh triggered!'):alert('Failed to trigger refresh.')})}");
                writer.Write("</script></body></html>");
            }
        }

        /// <summary>
        /// Writes the rebooting message page directly to the network stream.
        /// </summary>
        public static void WriteRebootPage(Stream stream, string message)
        {
            using (var writer = new StreamWriter(stream)) // Correct constructor
            {
                writer.Write("<!DOCTYPE html><html><head><title>Rebooting...</title><meta http-equiv='refresh' content='10;url=/'><style>body{font-family:sans-serif;background:#f0f2f5;display:flex;justify-content:center;align-items:center;height:100vh;margin:0}.container{text-align:center;padding:20px;background:#fff;border-radius:8px;box-shadow:0 2px 4px rgba(0,0,0,.1)}</style></head>");
                writer.Write($"<body><div class='container'><h1>{message}</h1><p>Please wait a moment and then reconnect to the device.</p><p>You will be redirected automatically in 10 seconds.</p></div></body></html>");
            }
        }
    }
}

