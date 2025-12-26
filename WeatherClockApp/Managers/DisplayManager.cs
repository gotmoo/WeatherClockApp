using nanoFramework.Hardware.Esp32;
using System;
using System.Collections;
using System.Device.Spi;
using System.Diagnostics;
using System.Text;
using System.Threading;
using WeatherClockApp.Fonts;
using WeatherClockApp.Models;

namespace WeatherClockApp.Managers
{
    public static class DisplayManager
    {
        private static Max7219.Max7219 _displayDriver;
        private static AppSettings _settings;
        private static int _screenWidth;
        private const int ScreenHeight = 8;
        private static byte[] _displayBuffer;

        // Changed from Hashtable to IFont interface
        private static IFont _selectedFont;

        private static bool _isScrolling = false;
        private static Thread _scrollThread;

        // State variables for the clock display
        private static bool _isColonVisible = true;
        private static int _utcOffsetSeconds = 0;
        private static string _temperature = "--°F";

        public static void Initialize(AppSettings settings)
        {
            _settings = settings;

            const int spiBus = 1;
            int csPin = Gpio.IO17;
            Configuration.SetPinFunction(Gpio.IO18, DeviceFunction.SPI1_MOSI);
            Configuration.SetPinFunction(Gpio.IO20, DeviceFunction.SPI1_MISO); //miso not actually used, but defined anyway
            Configuration.SetPinFunction(Gpio.IO19, DeviceFunction.SPI1_CLOCK);

            Debug.WriteLine($"Using SPI pins: MOSI={Gpio.IO02}, CLK={Gpio.IO04}], CS={csPin}");

            var spiSettings = new SpiConnectionSettings(spiBus, csPin)
            {
                Mode = SpiMode.Mode0,
                ClockFrequency = 1_000_000
            };
            Console.WriteLine("SPI Settings initialized.");
            var spiDevice = new SpiDevice(spiSettings);
            Console.WriteLine("SPI initialized.");

            _displayDriver = new Max7219.Max7219(spiDevice, _settings.DisplayPanels);
            Console.WriteLine("Created display driver.");

            _displayDriver.Rotation = _settings.PanelRotation;
            _displayDriver.PanelOrderReversed = _settings.PanelReversed; // Apply reversed setting
            _displayDriver.Init();

            Console.WriteLine("Display initialized.");
            _displayDriver.SetIntensity((byte)_settings.PanelBrightness);

            _screenWidth = _settings.DisplayPanels * 8;
            _displayBuffer = new byte[_screenWidth];

            // Select Font based on settings
            SetFont(_settings.FontName);

            Console.WriteLine("DisplayManager initialized.");
        }

        public static void TakeoffSequence()
        {
            SetFont( "zomaar");
            ShowStatus("zomaar", "weer");

            var targetBrightness = _settings.PanelBrightness;
            for (byte intensity = 0; intensity <= 15; intensity++)
            {
                _displayDriver.SetIntensity(intensity);
                Render();

                Thread.Sleep(100);
            }
            for (byte intensity = 15; intensity >= targetBrightness; intensity--)
            {
                _displayDriver.SetIntensity(intensity);
                Render();
                Thread.Sleep(100);
            }
            //SetFont(_settings.FontName);
        }

        public static void DisplayApInfo(string apName, string serverIp)
        {
            ScrollMessage($"Configure at http://{serverIp} on AP {apName}");
            Thread.Sleep(4000);
            ScrollMessage($"Configure at http://{serverIp} on AP {apName}");
        }
        public static void DisplayConfigInfo(string serverIp)
        {
            var sb = new StringBuilder();

            sb.Append("Connect to http://");
            sb.Append(serverIp);
            sb.Append(" to configure... ");
            ScrollFullWidth(sb.ToString());
            Thread.Sleep(200);
        }
        /// <summary>
        /// Updates the display settings immediately without requiring a full re-initialization.
        /// </summary>
        public static void UpdateSettings(AppSettings newSettings)
        {
            _settings = newSettings;
            if (_displayDriver != null)
            {
                _displayDriver.Rotation = _settings.PanelRotation;
                _displayDriver.PanelOrderReversed = _settings.PanelReversed;
                _displayDriver.SetIntensity((byte)_settings.PanelBrightness);
                // Also update font in case it changed
                SetFont(_settings.FontName);

                // Re-calculate screen width in case panel count changed (though that usually requires a reboot/re-init of SPI, 
                // we'll assume physical hardware hasn't changed dynamically, but logical handling might)
                _screenWidth = _settings.DisplayPanels * 8;
                if (_displayBuffer.Length != _screenWidth)
                {
                    _displayBuffer = new byte[_screenWidth];
                }

                // Force a redraw
                UpdateTimeAndTemp();
            }
        }

        public static void SetFont(string fontName)
        {
            switch (fontName?.ToLower())
            {
                case "lcd":
                    _selectedFont = Fonts.Fonts.LCD;
                    break;
                case "sinclair":
                    _selectedFont = Fonts.Fonts.Sinclair;
                    break;
                case "tiny":
                    _selectedFont = Fonts.Fonts.Tiny;
                    break;
                case "cyrillic":
                case "cyrillicukrainian":
                    _selectedFont = Fonts.Fonts.CyrillicUkrainian;
                    break;
                case "font1":
                    _selectedFont = new Font1();
                    break;
                case "zomaar":
                    _selectedFont = new FontZomaar();
                    break;
                case "default":
                case "cp437":
                default:
                    _selectedFont = Fonts.Fonts.Default;
                    break;
            }
            // Console.WriteLine($"Font set to: {fontName}");
        }

        static int PinNumber(char port, byte pin)
        {
            if (port < 'A' || port > 'J')
                throw new ArgumentException();

            return ((port - 'A') * 16) + pin;
        }

        public static void SetUtcOffset(int seconds) => _utcOffsetSeconds = seconds;
        public static void ToggleColon() => _isColonVisible = !_isColonVisible;
        public static void SetTemperature(string newTemperature) => _temperature = newTemperature;

        private static string GetTimeFormatString(bool showColon)
        {
            var colon = showColon ? " " : ":";
            return _settings.Is24HourFormat ? "HH" + colon + "mm" : "h" + colon + "mm";
        }
        /// <summary>
        /// Renders the static time and temperature to the display.
        /// </summary>
        public static void UpdateTimeAndTemp()
        {
            if (_isScrolling) return; // Don't interfere with scrolling

            Clear();
            DateTime now = DateTime.UtcNow.AddSeconds(_utcOffsetSeconds);

            // Time Formatting Logic
            string timeStr = now.ToString(GetTimeFormatString(showColon: false));

            if (_isColonVisible)
            {
                timeStr = now.ToString(GetTimeFormatString(showColon: true));
            }

            // Draw Time
            int leftHalfWidth = _screenWidth / 2;
            int timeWidth = GetTextWidth(timeStr);
            int timeX = (leftHalfWidth - timeWidth) / 2;
            DrawText(timeStr, timeX, 0);

            // Draw Temperature
            int rightHalfX = _screenWidth / 2;
            int tempWidth = GetTextWidth(_temperature);
            int tempX = rightHalfX + (leftHalfWidth - tempWidth) / 2;
            DrawText(_temperature, tempX, 0);


            Render();
        }

        /// <summary>
        /// Scrolls a message across the right half of the display.
        /// </summary>
        public static void ScrollRightHalf(string message, string leftText = null)
        {
            if (_isScrolling) return;

            _scrollThread = new Thread(() => ScrollTextOnRightHalf(message, leftText));
            _scrollThread.Start();
        }

        private static void ScrollTextOnRightHalf(object messageObj, string leftText = null)
        {
            string message = (string)messageObj;
            _isScrolling = true;

            // Pre-render the time on the left half to a temporary buffer
            byte[] leftHalfBuffer = new byte[_screenWidth / 2];
            if (leftText == null) // If no left text provided, use current time
            {
                DateTime now = DateTime.UtcNow.AddSeconds(_utcOffsetSeconds);
                leftText = now.ToString(GetTimeFormatString(showColon: true));
            }
            int leftWidth = GetTextWidth(leftText);
            int timeX = (_screenWidth / 2 - leftWidth) / 2;
            RenderTextToBuffer(leftText, timeX, 0, leftHalfBuffer);

            // Pre-render the full scroll message to an off-screen buffer
            int textWidth = GetTextWidth(message);
            byte[] textBuffer = new byte[textWidth];
            RenderTextToBuffer(message, 0, 0, textBuffer);

            int rightHalfStart = _screenWidth / 2;
            int rightHalfWidth = _screenWidth / 2;

            // Animate scroll
            for (int x = -rightHalfWidth; x < textWidth; x++)
            {
                lock (_displayBuffer)
                {
                    Array.Clear(_displayBuffer, 0, _displayBuffer.Length);
                    Array.Copy(leftHalfBuffer, 0, _displayBuffer, 0, leftHalfBuffer.Length);

                    int start = x > 0 ? x : 0;
                    int end = (x + rightHalfWidth) < textWidth ? (x + rightHalfWidth) : textWidth;
                    int destStart = rightHalfStart + (-x > 0 ? -x : 0);

                    if (start < end)
                    {
                        Array.Copy(textBuffer, start, _displayBuffer, destStart, end - start);
                    }
                }
                Render();
                Thread.Sleep(35);
            }

            _isScrolling = false;
        }


        public static void ShowStatus(string left, string right)
        {
            if (_isScrolling)
            {
                _isScrolling = false;
                Thread.Sleep(100);
            }
            ShowTwoWords(left, right);
        }

        public static void ScrollMessage(string message)
        {
            if (_isScrolling)
            {
                _isScrolling = false;
                Thread.Sleep(100);
            }
            _scrollThread = new Thread(() => ScrollFullWidth(message));
            _scrollThread.Start();
        }

        private static void ScrollFullWidth(object textObj)
        {
            string text = (string)textObj;
            _isScrolling = true;

            int textWidth = GetTextWidth(text);
            byte[] textBuffer = new byte[textWidth];
            RenderTextToBuffer(text, 0, 0, textBuffer);

            for (int x = -_screenWidth; x < textWidth && _isScrolling; x++)
            {
                lock (_displayBuffer)
                {
                    Array.Clear(_displayBuffer, 0, _displayBuffer.Length);
                    int start = x > 0 ? x : 0;
                    int end = x + _screenWidth < textWidth ? x + _screenWidth : textWidth;
                    int destStart = -x > 0 ? -x : 0;

                    if (start < end)
                    {
                        Array.Copy(textBuffer, start, _displayBuffer, destStart, end - start);
                    }
                }
                Render();
                Thread.Sleep(35);
            }
            _isScrolling = false;
        }

        private static void ShowTwoWords(string leftWord, string rightWord)
        {
            Clear();
            int leftHalfWidth = _screenWidth / 2;
            int leftWordWidth = GetTextWidth(leftWord);
            int leftX = (leftHalfWidth - leftWordWidth) / 2;
            DrawText(leftWord, leftX, 0);

            int rightHalfX = _screenWidth / 2;
            int rightWordWidth = GetTextWidth(rightWord);
            int rightX = rightHalfX + (leftHalfWidth - rightWordWidth) / 2;
            DrawText(rightWord, rightX, 0);
            Render();
        }

        #region Low Level Drawing
        private static int GetTextWidth(string text)
        {
            int width = 0;
            if (_selectedFont == null) return 0;

            foreach (char c in text)
            {
                var charData = _selectedFont[c];
                width += charData.Count + 1; // +1 for spacing
            }
            return width > 0 ? width - 1 : 0; // No spacing after last char
        }

        private static void DrawText(string text, int x, int y)
        {
            if (_selectedFont == null) return;
            int currentX = x;
            foreach (char c in text)
            {
                currentX += DrawChar(c, currentX, y) + 1; // +1 for spacing
            }
        }

        private static int DrawChar(char c, int x, int y)
        {
            var charData = _selectedFont[c];
            for (int i = 0; i < charData.Count; i++)
            {
                if (x + i >= 0 && x + i < _screenWidth)
                {
                    _displayBuffer[x + i] = charData[i];
                }
            }
            return charData.Count;
        }

        private static void RenderTextToBuffer(string text, int x, int y, byte[] buffer)
        {
            if (_selectedFont == null) return;
            int currentX = x;
            foreach (char c in text)
            {
                var charData = _selectedFont[c];
                for (int i = 0; i < charData.Count; i++)
                {
                    if (currentX + i >= 0 && currentX + i < buffer.Length)
                    {
                        buffer[currentX + i] = charData[i];
                    }
                }
                currentX += charData.Count + 1; // +1 for spacing
            }
        }

        private static void Clear()
        {
            Array.Clear(_displayBuffer, 0, _displayBuffer.Length);
        }

        private static void Render()
        {
            lock (_displayBuffer)
            {
                _displayDriver.Render(_displayBuffer);
            }
        }
        #endregion
    }
}