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
                                settings.Latitude = double.Parse(value);
                                break;
                            case nameof(Longitude):
                                settings.Longitude = double.Parse(value);
                                break;
                        }
                    }
                }
            }
            return settings;
        }
    }
}