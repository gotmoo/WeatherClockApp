using System;
using System.IO;
using WeatherClockApp.Models;

namespace WeatherClockApp.Managers
{
    /// <summary>
    /// Manages loading and saving application settings.
    /// In a real application, this would read from and write to non-volatile storage (NVS).
    /// </summary>
    internal static class SettingsManager
    {
        private const string SettingsFilePath = "I:\\settings.ini";
        public static void GetSettingsFileContent()
        {
            var fileContents =   GetFileContents(SettingsFilePath);
            Console.WriteLine("======= INI CONTENTS ========");
            Console.WriteLine(fileContents);
            Console.WriteLine("======= END CONTENTS ========");
        }

      
        public static AppSettings Load() => LoadSettings();
        // Made this method static
        public static AppSettings LoadSettings()
        {
            try
            {
                var settingsContent = GetFileContents(SettingsFilePath);
                return settingsContent == null ? new AppSettings() : AppSettings.Deserialize(settingsContent);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading settings: {ex.Message}");
            }

            // Return default settings if file doesn't exist or fails to load
            return new AppSettings();
        }

        public static void Save(AppSettings settings) => SaveSettings(settings);
        // Made this method static
        public static void SaveSettings(AppSettings settings)
        {
            var wasSaved = WriteFileContents(SettingsFilePath, settings.Serialize());
            Console.WriteLine($"Settings saved: {wasSaved}");
        }
        private static string GetFileContents(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                    var buffer = new byte[fs.Length];
                    fs.Read(buffer, 0, buffer.Length);
                    return new string(System.Text.Encoding.UTF8.GetChars(buffer));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading file: {ex.Message}");
            }
            return null;
        }
        private static bool WriteFileContents(string filePath, string contents)
        {
            Console.WriteLine("======= ATTEMPTING TO WRITE ========");
            Console.WriteLine(contents);
            Console.WriteLine("======= END ATTEMPT ========");
            try
            {
                using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write);
                var buffer = System.Text.Encoding.UTF8.GetBytes(contents);
                fs.Write(buffer, 0, buffer.Length);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving settings: {ex.Message}");
            }
            return false;
        }
    }
}