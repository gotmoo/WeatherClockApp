using System;
using System.Device.Spi;

namespace Max7219
{
    /// <summary>
    /// Represents a single or chained set of MAX7219 8x8 LED matrix displays.
    /// </summary>
    public class Max7219 : IDisposable
    {
        private readonly SpiDevice _spiDevice;
        private readonly int _deviceCount;

        // MAX7219 command registers
        private const byte RegNoOp = 0x00;
        private const byte RegDigit0 = 0x01;
        private const byte RegDigit1 = 0x02;
        private const byte RegDigit2 = 0x03;
        private const byte RegDigit3 = 0x04;
        private const byte RegDigit4 = 0x05;
        private const byte RegDigit5 = 0x06;
        private const byte RegDigit6 = 0x07;
        private const byte RegDigit7 = 0x08;
        private const byte RegDecodeMode = 0x09;
        private const byte RegIntensity = 0x0A;
        private const byte RegScanLimit = 0x0B;
        private const byte RegShutdown = 0x0C;
        private const byte RegDisplayTest = 0x0F;

        /// <summary>
        /// Gets or sets the rotation of the display content.
        /// 0 = No rotation (default)
        /// 2 = 180 degrees (upside down)
        /// Other values are treated as 0.
        /// </summary>
        public int Rotation { get; set; } = 0;

        /// <summary>
        /// Initializes a new instance of the MAX7219 driver.
        /// </summary>
        /// <param name="spiDevice">The SPI device for communication.</param>
        /// <param name="deviceCount">The number of MAX7219 devices chained together.</param>
        public Max7219(SpiDevice spiDevice, int deviceCount = 1)
        {
            _spiDevice = spiDevice ?? throw new ArgumentNullException(nameof(spiDevice));
            _deviceCount = deviceCount > 0 ? deviceCount : throw new ArgumentOutOfRangeException(nameof(deviceCount));
        }

        /// <summary>
        /// Initializes the MAX7219 display(s) with default settings.
        /// </summary>
        public void Init()
        {
            // Turn off display test
            SendCommand(RegDisplayTest, 0x00);
            // Set scan limit to all 8 digits (rows)
            SendCommand(RegScanLimit, 0x07);
            // Set decode mode to no-decode for all digits
            SendCommand(RegDecodeMode, 0x00);
            // Set medium intensity
            SetIntensity(7);
            // Wake up the display
            Shutdown(false);
            // Clear the display
            Clear();
        }

        /// <summary>
        /// Sets the display brightness.
        /// </summary>
        /// <param name="intensity">The brightness level (0-15).</param>
        public void SetIntensity(byte intensity)
        {
            if (intensity > 15)
            {
                intensity = 15;
            }
            SendCommand(RegIntensity, intensity);
        }

        /// <summary>
        /// Turns the display on or off.
        /// </summary>
        /// <param name="shutdown">True to shut down, false to turn on.</param>
        public void Shutdown(bool shutdown)
        {
            SendCommand(RegShutdown, (byte)(shutdown ? 0x00 : 0x01));
        }

        /// <summary>
        /// Clears the entire display.
        /// </summary>
        public void Clear()
        {
            for (byte i = 1; i <= 8; i++)
            {
                SendCommand((byte)(RegDigit0 + i - 1), 0x00);
            }
        }

        /// <summary>
        /// Reverses the bits in a byte. Used for 180-degree rotation.
        /// </summary>
        private byte ReverseByte(byte b)
        {
            byte r = 0;
            for (int i = 0; i < 8; i++)
            {
                if ((b & 1 << i) != 0)
                {
                    r |= (byte)(1 << 7 - i);
                }
            }
            return r;
        }

        /// <summary>
        /// Renders a buffer of pixel data to the display chain.
        /// Applies rotation if specified.
        /// </summary>
        /// <param name="buffer">A byte array representing the display content. Length must be 8 * deviceCount.</param>
        public void Render(byte[] buffer)
        {
            if (buffer.Length != 8 * _deviceCount)
            {
                throw new ArgumentException($"Buffer length must be {8 * _deviceCount} for {_deviceCount} device(s).");
            }

            if (Rotation == 2) // 180 degrees
            {
                var rotatedBuffer = new byte[buffer.Length];
                for (int i = 0; i < buffer.Length; i++)
                {
                    // Read the source buffer backwards and reverse the bits of each byte
                    rotatedBuffer[i] = ReverseByte(buffer[buffer.Length - 1 - i]);
                }
                RenderInternal(rotatedBuffer);
            }
            else
            {
                RenderInternal(buffer);
            }
        }

        /// <summary>
        /// Internal render method that sends the final buffer to the SPI device.
        /// </summary>
        private void RenderInternal(byte[] buffer)
        {
            for (byte row = 0; row < 8; row++)
            {
                var spiBuffer = new byte[_deviceCount * 2];
                int spiIndex = 0;

                for (int device = _deviceCount - 1; device >= 0; device--)
                {
                    spiBuffer[spiIndex++] = (byte)(RegDigit0 + row);
                    byte rowData = 0;
                    for (int col = 0; col < 8; col++)
                    {
                        byte columnByte = buffer[device * 8 + col];
                        if ((columnByte & 1 << row) != 0)
                        {
                            rowData |= (byte)(1 << 7 - col);
                        }
                    }
                    spiBuffer[spiIndex++] = rowData;
                }
                _spiDevice.Write(spiBuffer);
            }
        }

        /// <summary>
        /// Sends a command (address and data) to all devices in the chain.
        /// </summary>
        private void SendCommand(byte address, byte data)
        {
            var buffer = new byte[_deviceCount * 2];
            for (int i = 0; i < _deviceCount; i++)
            {
                buffer[i * 2] = address;
                buffer[i * 2 + 1] = data;
            }
            _spiDevice.Write(buffer);
        }

        public void Dispose()
        {
            Shutdown(true);
            _spiDevice?.Dispose();
        }
    }
}

