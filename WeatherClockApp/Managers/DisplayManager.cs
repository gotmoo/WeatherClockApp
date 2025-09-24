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

        public static void Initialize(AppSettings settings)
        {
            _settings = settings;

            const int spiBus = 1;
            const int chipSelectPin = 5; // CS Pin must be hardcoded

            // Get hardware-specific pins for SPI1
            Configuration.SetPinFunction(23, DeviceFunction.SPI1_MOSI); // GPIO21
            Configuration.SetPinFunction(19, DeviceFunction.SPI1_CLOCK); // GPIO22

            Debug.WriteLine($"Using SPI pins: MOSI=23, CLK=19, CS={chipSelectPin}");

            var spiSettings = new SpiConnectionSettings(spiBus, chipSelectPin)
            {
                Mode = SpiMode.Mode0,
                ClockFrequency = 1_000_000
            };
            Console.WriteLine("SPI Settings initialized.");
            var spiDevice = new SpiDevice(spiSettings);
            Console.WriteLine("SPI initialized.");

            //Blank config guard
            if (_settings.DisplayPanels == 0) _settings.DisplayPanels = 8;
            if (_settings.PanelRotation == 0) _settings.PanelRotation = 2;
            Console.WriteLine("Blank Config guarded.");

            _displayDriver = new Max7219.Max7219(spiDevice, _settings.DisplayPanels);
            Console.WriteLine("Created display driver.");

            _displayDriver.Rotation = _settings.PanelRotation;
            _displayDriver.Init();
            Console.WriteLine("Display initialized.");
            _displayDriver.SetIntensity(1);

            _screenWidth = _settings.DisplayPanels * 8;
            _displayBuffer = new byte[_screenWidth];
            _selectedFont = Font1.Characters; // Default to Font1

            Console.WriteLine("DisplayManager initialized.");
        }

        public static void ShowStatus(string left, string right)
        {
            if (_isScrolling)
            {
                _isScrolling = false;
                Thread.Sleep(100); // Give the scroll thread a moment to exit
            }
            ShowTwoWords(left, right);
        }

        public static void ScrollMessage(string message)
        {
            if (_isScrolling)
            {
                _isScrolling = false;
                Thread.Sleep(100); // Give the scroll thread a moment to exit
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
    }
}
