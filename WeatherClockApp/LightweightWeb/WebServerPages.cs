using System.Net.Sockets;
using System.Text;
using WeatherClockApp.Models;

namespace WeatherClockApp.LightweightWeb
{
    internal static class WebServerPages
    {
        private static void Send(NetworkStream stream, string content)
        {
            if (string.IsNullOrEmpty(content)) return;
            byte[] bytes = Encoding.UTF8.GetBytes(content);
            byte[] lengthHeader = Encoding.UTF8.GetBytes($"{bytes.Length:X}\r\n");
            stream.Write(lengthHeader, 0, lengthHeader.Length);
            stream.Write(bytes, 0, bytes.Length);
            stream.Write(new byte[] { 13, 10 }, 0, 2);
        }

        internal static void WriteIndexPage(NetworkStream stream, AppSettings settings, string activeTab = "clock")
        {
            Send(stream, @"<!DOCTYPE html><html><head><title>ESP32 Weather</title><meta name='viewport' content='width=device-width, initial-scale=1'>
<style>body{font-family:sans-serif;background:#f0f2f5;margin:0;padding:10px}.container{max-width:600px;margin:0 auto;background:#fff;border-radius:8px;box-shadow:0 2px 4px rgba(0,0,0,0.1);overflow:hidden}
.header{background:#007bff;color:white;padding:15px;text-align:center}h1{margin:0;font-size:1.5em}.tabs{display:flex;background:#eee;border-bottom:1px solid #ddd}
.tab{flex:1;padding:12px;text-align:center;cursor:pointer;border-bottom:3px solid transparent;font-weight:500;color:#555}
.tab.active{border-bottom-color:#007bff;color:#007bff;background:#fff}.content{padding:20px;display:none}.content.active{display:block}
.form-group{margin-bottom:15px}label{display:block;font-weight:bold;margin-bottom:5px;font-size:0.9em}
input,select{width:100%;padding:10px;border:1px solid #ddd;border-radius:4px;box-sizing:border-box}
.btn{background:#007bff;color:#fff;padding:12px;border:none;border-radius:4px;cursor:pointer;width:100%;font-size:16px;font-weight:bold}
.btn-secondary{background:#6c757d}.inline-group{display:flex;gap:10px}.inline-group input{flex-grow:1}.inline-group button{width:auto}
.spinner{border:3px solid #f3f3f3;border-top:3px solid #007bff;border-radius:50%;width:20px;height:20px;animation:spin 1s linear infinite;display:none;margin:10px auto}
@keyframes spin{0%{transform:rotate(0deg)}100%{transform:rotate(360deg)}}.radio-label{padding:8px;border-bottom:1px solid #eee;cursor:pointer}
</style></head><body><div class='container'><div class='header'><h1>Weather Clock</h1></div>");

            Send(stream, "<div class='tabs'>");
            Send(stream, "<div class='tab " + (activeTab == "network" ? "active" : "") + "' onclick=\"openTab('network')\">Network</div>");
            Send(stream, "<div class='tab " + (activeTab == "location" ? "active" : "") + "' onclick=\"openTab('location')\">Location</div>");
            Send(stream, "<div class='tab " + (activeTab == "clock" ? "active" : "") + "' onclick=\"openTab('clock')\">Clock & Weather</div>");
            Send(stream, "<div class='tab " + (activeTab == "display" ? "active" : "") + "' onclick=\"openTab('display')\">Display</div>");
            Send(stream, "</div>");

            // --- Network Tab ---
            Send(stream, "<div id='network' class='content " + (activeTab == "network" ? "active" : "") + "'>");
            Send(stream, "<form action='/save-wifi' method='post'><div class='form-group'><label>SSID</label><div class='inline-group'>");
            Send(stream, "<input type='text' id='ssid' name='ssid' value='" + (settings.Ssid ?? "") + "'>");
            Send(stream, "<button type='button' id='scan-btn' class='btn btn-secondary'>Scan</button></div><div id='wifi-results'></div><div id='wifi-spinner' class='spinner'></div></div>");
            Send(stream, "<div class='form-group'><label>Password</label><input type='password' name='password' value='" + (settings.Password ?? "") + "'></div>");
            Send(stream, "<button type='submit' class='btn'>Save & Connect</button></form><br>");
            Send(stream, "<form action='/reboot' method='post' onsubmit=\"return confirm('Reboot?');\"><button type='submit' class='btn btn-secondary'>Reboot</button></form></div>");

            // --- Location Tab ---
            Send(stream, "<div id='location' class='content " + (activeTab == "location" ? "active" : "") + "'>");
            Send(stream, "<form action='/save-location' method='post'><div class='form-group'><label>API Key</label>");
            Send(stream, "<input type='text' name='weatherApiKey' value='" + (settings.WeatherApiKey ?? "") + "'></div>");
            Send(stream, "<div class='form-group'><label>Search</label><div class='inline-group'><input type='text' id='loc-search' placeholder='City'><button type='button' id='search-btn' class='btn btn-secondary'>Find</button></div>");
            Send(stream, "<div id='loc-results'></div><div id='loc-spinner' class='spinner'></div></div>");
            Send(stream, "<div class='form-group'><label>Location</label><input type='text' id='loc-name' name='locationName' readonly value='" + (settings.LocationName ?? "") + "'></div>");
            Send(stream, "<div class='inline-group'><div class='form-group' style='flex:1'><label>Lat</label><input type='number' step='any' id='lat' name='latitude' value='" + settings.Latitude + "'></div>");
            Send(stream, "<div class='form-group' style='flex:1'><label>Lon</label><input type='number' step='any' id='lon' name='longitude' value='" + settings.Longitude + "'></div></div>");
            // Update Frequency
            Send(stream, "<div class='form-group'><label>Forecast Update Frequency</label><select name='weatherRefreshMinutes'>");
            Send(stream, "<option value='20'" + (settings.WeatherRefreshMinutes == 20 ? " selected" : "") + ">Every 20 Minutes</option>");
            Send(stream, "<option value='30'" + (settings.WeatherRefreshMinutes == 30 ? " selected" : "") + ">Every 30 Minutes</option>");
            Send(stream, "<option value='40'" + (settings.WeatherRefreshMinutes == 40 ? " selected" : "") + ">Every 40 Minutes</option>");
            Send(stream, "<option value='60'" + (settings.WeatherRefreshMinutes == 60 ? " selected" : "") + ">Every Hour</option>");
            Send(stream, "<option value='120'" + (settings.WeatherRefreshMinutes == 120 ? " selected" : "") + ">Every 2 Hours</option>");
            
            Send(stream, "</select></div>");
            Send(stream, "<button type='submit' class='btn'>Save Location</button></form></div>");

            // --- Clock & Weather Tab ---
            Send(stream, "<div id='clock' class='content " + (activeTab == "clock" ? "active" : "") + "'>");
            Send(stream, "<form action='/save-clock' method='post'>");

            // Format & Units
            Send(stream, "<div class='inline-group'><div class='form-group' style='flex:1'><label>Time Format</label><select name='is24HourFormat'>");
            Send(stream, "<option value='true'" + (settings.Is24HourFormat ? " selected" : "") + ">24 Hour (14:30)</option>");
            Send(stream, "<option value='false'" + (!settings.Is24HourFormat ? " selected" : "") + ">12 Hour (2:30)</option></select></div>");

            Send(stream, "<div class='form-group' style='flex:1'><label>Units</label><select name='weatherUnit'>");
            Send(stream, "<option value='imperial'" + (settings.WeatherUnit == "imperial" ? " selected" : "") + ">Imperial (&deg;F)</option>");
            Send(stream, "<option value='metric'" + (settings.WeatherUnit == "metric" ? " selected" : "") + ">Metric (&deg;C)</option>");
            Send(stream, "</select></div>");

            Send(stream, "<div class='form-group' style='flex:1'><label>Degrees Symbol</label>");
            Send(stream, "<label style='font-weight:normal'><input type='checkbox' name='showDegrees' id='showDegrees' value='true' style='width:auto' " + (settings.ShowDegreesSymbol ? "checked" : "") + "/> Show</label>");
            Send(stream, "</div></div>");

            // Scroll Frequency
            Send(stream, "<div class='form-group'><label>Forecast Scroll Frequency</label><select name='scrollFrequencyMinutes'>");
            Send(stream, "<option value='1'" + (settings.ScrollFrequencyMinutes == 1 ? " selected" : "") + ">Every Minute</option>");
            Send(stream, "<option value='2'" + (settings.ScrollFrequencyMinutes == 2 ? " selected" : "") + ">Every 2 Minutes</option>");
            Send(stream, "<option value='3'" + (settings.ScrollFrequencyMinutes == 3 ? " selected" : "") + ">Every 3 Minutes</option>");
            Send(stream, "<option value='5'" + (settings.ScrollFrequencyMinutes == 5 ? " selected" : "") + ">Every 5 Minutes</option>");
            Send(stream, "<option value='10'" + (settings.ScrollFrequencyMinutes == 10 ? " selected" : "") + ">Every 10 Minutes</option>");
            Send(stream, "</select></div>");

            // Scroll Contents
            Send(stream, "<div class='form-group'><label>Scroll Contents</label>");
            Send(stream, "<div style='display:grid;grid-template-columns:1fr 1fr;gap:10px;'>");
            Send(stream, "<label style='font-weight:normal'><input type='checkbox' name='showDescription' value='true' style='width:auto' " + (settings.ShowDescription ? "checked" : "") + "> Description</label>");
            Send(stream, "<label style='font-weight:normal'><input type='checkbox' name='showFeelsLike' value='true' style='width:auto' " + (settings.ShowFeelsLike ? "checked" : "") + "> Feels Like</label>");
            Send(stream, "<label style='font-weight:normal'><input type='checkbox' name='showMinTemp' value='true' style='width:auto' " + (settings.ShowMinTemp ? "checked" : "") + "> Min Temp</label>");
            Send(stream, "<label style='font-weight:normal'><input type='checkbox' name='showMaxTemp' value='true' style='width:auto' " + (settings.ShowMaxTemp ? "checked" : "") + "> Max Temp</label>");
            Send(stream, "<label style='font-weight:normal'><input type='checkbox' name='showHumidity' value='true' style='width:auto' " + (settings.ShowHumidity ? "checked" : "") + "> Humidity</label>");
            Send(stream, "</div></div>");

            Send(stream, "<button type='submit' class='btn'>Save Configuration</button></form></div>");

            // --- Display Tab ---
            Send(stream, "<div id='display' class='content " + (activeTab == "display" ? "active" : "") + "'>");
            Send(stream, "<form action='/save-display' method='post'><div class='form-group'><label>Font</label><select name='fontName'>");
            Send(stream, "<option value='Default'" + (settings.FontName == "Default" ? " selected" : "") + ">Default</option>");
            Send(stream, "<option value='LCD'" + (settings.FontName == "LCD" ? " selected" : "") + ">LCD</option>");
            Send(stream, "<option value='Sinclair'" + (settings.FontName == "Sinclair" ? " selected" : "") + ">Sinclair</option>");
            Send(stream, "<option value='Tiny'" + (settings.FontName == "Tiny" ? " selected" : "") + ">Tiny</option>");
            Send(stream, "<option value='Font1'" + (settings.FontName == "Font1" ? " selected" : "") + ">Old School</option>");
            Send(stream, "</select></div>");
            Send(stream, "<div class='form-group'><label>Panels</label><select name='displayPanels'>");
            Send(stream, "<option value='4'" + (settings.DisplayPanels == 4 ? " selected" : "") + ">4</option>");
            Send(stream, "<option value='8'" + (settings.DisplayPanels == 8 ? " selected" : "") + ">8</option></select></div>");
            Send(stream, "<div class='form-group'><label>Rotation</label><select name='panelRotation'>");
            Send(stream, "<option value='0'" + (settings.PanelRotation == 0 ? " selected" : "") + ">0</option>");
            Send(stream, "<option value='1'" + (settings.PanelRotation == 1 ? " selected" : "") + ">90</option>");
            Send(stream, "<option value='2'" + (settings.PanelRotation == 2 ? " selected" : "") + ">180</option>");
            Send(stream, "<option value='3'" + (settings.PanelRotation == 3 ? " selected" : "") + ">270</option></select></div>");
            Send(stream, "<div class='form-group'><label>Brightness</label><input type='number' name='panelBrightness' min='0' max='15' value='" + settings.PanelBrightness + "'></div>");
            Send(stream, "<div class='form-group'><label><input type='checkbox' name='panelReversed' style='width:auto' value='true'" + (settings.PanelReversed ? " checked" : "") + "> Reverse Order</label></div>");
            Send(stream, "<button type='submit' class='btn'>Apply</button></form></div>");

            // JavaScript
            Send(stream, @"</div><script>
function openTab(n){document.querySelectorAll('.content').forEach(c=>c.classList.remove('active'));document.querySelectorAll('.tab').forEach(t=>t.classList.remove('active'));
document.getElementById(n).classList.add('active');if(n=='network')document.querySelectorAll('.tab')[0].classList.add('active');
if(n=='location')document.querySelectorAll('.tab')[1].classList.add('active');
if(n=='clock')document.querySelectorAll('.tab')[2].classList.add('active');
if(n=='display')document.querySelectorAll('.tab')[3].classList.add('active');}
document.getElementById('scan-btn').onclick=async()=>{const r=document.getElementById('wifi-results'),s=document.getElementById('wifi-spinner');r.innerHTML='';s.style.display='block';
try{const d=await(await fetch('/api/scan-wifi')).json();s.style.display='none';d.forEach(x=>{const e=document.createElement('div');e.className='radio-label';e.innerText=x;e.onclick=()=>document.getElementById('ssid').value=x;r.appendChild(e)})}
catch{s.style.display='none';r.innerText='Error'}}
document.getElementById('search-btn').onclick=async()=>{const q=document.getElementById('loc-search').value;if(!q)return;
const r=document.getElementById('loc-results'),s=document.getElementById('loc-spinner');r.innerHTML='';s.style.display='block';
try{const d=await(await fetch('/api/geo-resolve?location='+encodeURIComponent(q))).json();s.style.display='none';d.forEach(x=>{const e=document.createElement('div');e.className='radio-label';e.innerText=`${x.name}, ${x.country}`;
e.onclick=()=>{document.getElementById('loc-name').value=e.innerText;document.getElementById('lat').value=x.lat;document.getElementById('lon').value=x.lon};r.appendChild(e)})}
catch{s.style.display='none';r.innerText='Error'}}
</script></body></html>");
        }

        internal static string RebootPage(string message)
        {
            return "<html><head><meta http-equiv='refresh' content='10;url=/'></head><body><h2>" + message + "</h2></body></html>";
        }
    }
}