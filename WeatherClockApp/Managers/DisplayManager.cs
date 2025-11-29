using nanoFramework.Hardware.Esp32;
using System;
using System.Collections;
using System.Device.Spi;
using System.Diagnostics;
using System.Threading;
using WeatherClockApp.Display;
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
        private static Hashtable _selectedFont;

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
            _selectedFont = Font1.Characters; // Default to Font1

            Console.WriteLine("DisplayManager initialized.");
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

        /// <summary>
        /// Renders the static time and temperature to the display.
        /// </summary>
        public static void UpdateTimeAndTemp()
        {
            if (_isScrolling) return; // Don't interfere with scrolling

            Clear();
            DateTime now = DateTime.UtcNow.AddSeconds(_utcOffsetSeconds);

            string timeStr = now.ToString("HH mm"); // Use space to be replaced by colon later
            if (_isColonVisible)
            {
                timeStr = now.ToString("HH:mm");
            }

            int leftHalfWidth = _screenWidth / 2;
            int timeWidth = GetTextWidth(timeStr);
            int timeX = (leftHalfWidth - timeWidth) / 2;
            DrawText(timeStr, timeX, 0);

            int rightHalfX = _screenWidth / 2;
            int tempWidth = GetTextWidth(_temperature);
            int tempX = rightHalfX + (leftHalfWidth - tempWidth) / 2;
            DrawText(_temperature, tempX, 0);

            Render();
        }

        /// <summary>
        /// Scrolls a message across the right half of the display.
        /// </summary>
        public static void ScrollRightHalf(string message)
        {
            if (_isScrolling) return;

            _scrollThread = new Thread(() => ScrollTextOnRightHalf(message));
            _scrollThread.Start();
        }

        private static void ScrollTextOnRightHalf(object messageObj)
        {
            string message = (string)messageObj;
            _isScrolling = true;

            // Pre-render the time on the left half to a temporary buffer
            byte[] leftHalfBuffer = new byte[_screenWidth / 2];
            DateTime now = DateTime.UtcNow.AddSeconds(_utcOffsetSeconds);
            string timeStr = now.ToString("HH:mm");
            int timeWidth = GetTextWidth(timeStr);
            int timeX = (_screenWidth / 2 - timeWidth) / 2;
            RenderTextToBuffer(timeStr, timeX, 0, leftHalfBuffer);

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
            foreach (char c in text)
            {
                if (_selectedFont.Contains(c))
                {
                    width += ((byte[])_selectedFont[c]).Length + 1; // +1 for spacing
                }
            }
            return width > 0 ? width - 1 : 0; // No spacing after last char
        }

        private static void DrawText(string text, int x, int y)
        {
            int currentX = x;
            foreach (char c in text)
            {
                currentX += DrawChar(c, currentX, y) + 1; // +1 for spacing
            }
        }

        private static int DrawChar(char c, int x, int y)
        {
            if (_selectedFont.Contains(c))
            {
                byte[] charData = (byte[])_selectedFont[c];
                for (int i = 0; i < charData.Length; i++)
                {
                    if (x + i >= 0 && x + i < _screenWidth)
                    {
                        _displayBuffer[x + i] = charData[i];
                    }
                }
                return charData.Length;
            }
            return 0;
        }

        private static void RenderTextToBuffer(string text, int x, int y, byte[] buffer)
        {
            int currentX = x;
            foreach (char c in text)
            {
                if (_selectedFont.Contains(c))
                {
                    byte[] charData = (byte[])_selectedFont[c];
                    for (int i = 0; i < charData.Length; i++)
                    {
                        if (currentX + i >= 0 && currentX + i < buffer.Length)
                        {
                            buffer[currentX + i] = charData[i];
                        }
                    }
                    currentX += charData.Length + 1; // +1 for spacing
                }
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