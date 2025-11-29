using System;
using System.Text;

namespace WeatherClockApp.Models
{
    public class AppSettings
    {
        public bool IsConfigured { get; set; }
        public string Ssid { get; set; }
        public string Password { get; set; }
        public string WeatherApiKey { get; set; }
        public string LocationName { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public int DisplayPanels { get; set; } = 8;
        public int PanelRotation { get; set; } = 2; // 0 = normal, 2 = 180 degrees
        public int PanelBrightness { get; set; } = 1;
        public bool PanelReversed { get; set; } = false;
        public bool ShowDegreesSymbol { get; set; } = true;
        public string WeatherUnit { get; set; } = "imperial";
        public int WeatherRefreshMinutes { get; set; } = 20;
        public string FontName { get; set; } = "Default"; // Default, LCD, Sinclair, Tiny, Cyrillic

        /// <summary>
        /// Serializes the current instance to an INI-formatted string.
        /// </summary>
        /// <returns>An INI-formatted string representing the object's properties.</returns>
        public string Serialize()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"[{nameof(AppSettings)}]");
            sb.AppendLine($"{nameof(IsConfigured)}={IsConfigured}");
            sb.AppendLine($"{nameof(Ssid)}={Ssid}");
            sb.AppendLine($"{nameof(Password)}={Password}");
            sb.AppendLine($"{nameof(WeatherApiKey)}={WeatherApiKey}");
            sb.AppendLine($"{nameof(LocationName)}={LocationName}");
            sb.AppendLine($"{nameof(Latitude)}={Latitude}");
            sb.AppendLine($"{nameof(Longitude)}={Longitude}");
            sb.AppendLine($"{nameof(DisplayPanels)}={DisplayPanels}");
            sb.AppendLine($"{nameof(PanelRotation)}={PanelRotation}");
            sb.AppendLine($"{nameof(PanelBrightness)}={PanelBrightness}");
            sb.AppendLine($"{nameof(PanelReversed)}={PanelReversed}");
            sb.AppendLine($"{nameof(WeatherUnit)}={WeatherUnit}");
            sb.AppendLine($"{nameof(WeatherRefreshMinutes)}={WeatherRefreshMinutes}");
            sb.AppendLine($"{nameof(ShowDegreesSymbol)}={ShowDegreesSymbol}");
            sb.AppendLine($"{nameof(FontName)}={FontName}");
            return sb.ToString();
        }

        /// <summary>
        /// Deserializes an INI-formatted string into a new AppSettings instance.
        /// </summary>
        /// <param name="content">The INI-formatted string.</param>
        /// <returns>A new AppSettings object populated with data from the string.</returns>
        public static AppSettings Deserialize(string content)
        {
            var settings = new AppSettings();
            if (string.IsNullOrEmpty(content))
            {
                return settings;
            }

            string[] lines = content.Split('\n');
            string sectionName = $"[{nameof(AppSettings)}]";
            bool inSection = false;

            foreach (var rawLine in lines)
            {
                string line = rawLine.Trim();

                if (string.IsNullOrEmpty(line) || line.StartsWith(";") || line.StartsWith("#"))
                {
                    continue; // Skip comments and empty lines
                }

                if (line.Equals(sectionName))
                {
                    inSection = true;
                    continue;
                }

                if (inSection)
                {
                    // Stop if we hit a new section
                    if (line.StartsWith("["))
                    {
                        inSection = false;
                        continue;
                    }

                    int separatorIndex = line.IndexOf('=');
                    if (separatorIndex > 0)
                    {
                        string key = line.Substring(0, separatorIndex).Trim();
                        string value = line.Substring(separatorIndex + 1).Trim();
                        // Console.WriteLine($"Key: {key}, Value: {value}");
                        // Use a switch for efficient property setting
                        switch (key)
                        {
                            case nameof(IsConfigured):
                                settings.IsConfigured = value.ToLower() == "true" || value == "1";
                                break;
                            case nameof(Ssid):
                                settings.Ssid = value;
                                break;
                            case nameof(Password):
                                settings.Password = value;
                                break;
                            case nameof(WeatherApiKey):
                                settings.WeatherApiKey = value;
                                break;
                            case nameof(LocationName):
                                settings.LocationName = value;
                                break;
                            case nameof(Latitude):
                                settings.Latitude = double.TryParse(value, out var lat) ? lat : 0;
                                break;
                            case nameof(Longitude):
                                settings.Longitude = double.TryParse(value, out var lon) ? lon : 0;
                                break;
                            case nameof(PanelRotation):
                                settings.PanelRotation = int.TryParse(value, out var rotation) ? rotation : 0;
                                break;
                            case nameof(PanelBrightness):
                                settings.PanelBrightness = int.TryParse(value, out var brightness) ? brightness : 0;
                                break;
                            case nameof(DisplayPanels):
                                settings.DisplayPanels = int.TryParse(value, out var cnt) ? cnt : 0;
                                break;
                            case nameof(PanelReversed):
                                settings.PanelReversed = value.ToLower() == "true" || value == "1";
                                break;
                            case nameof(ShowDegreesSymbol):
                                settings.ShowDegreesSymbol = value.ToLower() == "true" || value == "1";
                                break;
                            case nameof(WeatherUnit):
                                settings.WeatherUnit = value;
                                break;
                            case nameof(WeatherRefreshMinutes):
                                settings.WeatherRefreshMinutes = int.TryParse(value, out var refreshMinutes) ? refreshMinutes : 0;
                                break;
                            case nameof(FontName):
                                settings.FontName = value;
                                break;
                        }
                    }
                }
            }

            Console.WriteLine($"Settings null: {settings == null}");
            return settings;
        }
    }
}