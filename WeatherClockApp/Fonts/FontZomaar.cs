using System.Collections;

namespace WeatherClockApp.Fonts
{
    /// <summary>
    /// Implementation of IFont for the original variable-width Font1.
    /// This wraps the original Hashtable character definitions in an IFont compatible class.
    /// </summary>
    public class FontZomaar : IFont
    {
        // Cache the space character data to avoid repeated lookups/allocations for fallbacks
        private static readonly byte[] _spaceBytes = new byte[] { 0x00 };
        private static readonly ListByte _spaceList = new ListByte(_spaceBytes);

        // Lazy cache for wrapped ListByte instances. Entries are created on first access to avoid
        // allocating many byte[] instances during static initialization (reduces heap pressure).
        private static readonly Hashtable _characters = new Hashtable();

        public ListByte this[char chr]
        {
            get
            {
                if (_characters.Contains(chr))
                {
                    return (ListByte)_characters[chr];
                }

                var bytes = GetBytesForChar(chr);
                if (bytes == null)
                {
                    return _spaceList;
                }

                var list = new ListByte(bytes);
                _characters[chr] = list;
                return list;
            }
        }

        // Returns the raw byte[] for a character when first needed.
        // Keeping the byte[] creation lazy avoids allocating all font glyph arrays at class load time.
        private static byte[] GetBytesForChar(char chr)
        {
            switch (chr)
            {
                case ' ': return _spaceBytes;
                case '!': return new byte[] { 0x5F };
                case '"': return new byte[] { 0x03, 0x03 };
                case '#': return new byte[] { 0x14, 0x3E, 0x14, 0x3E, 0x14 };
                case '$': return new byte[] { 0x24, 0x6A, 0x2B, 0x12 };
                case '%': return new byte[] { 0x63, 0x13, 0x08, 0x64, 0x63 };
                case '&': return new byte[] { 0x36, 0x49, 0x56, 0x20, 0x50 };
                case '\'': return new byte[] { 0x03 };
                case '(': return new byte[] { 0x1C, 0x22, 0x41 };
                case ')': return new byte[] { 0x41, 0x22, 0x1C };
                case '*': return new byte[] { 0x28, 0x18, 0x0E, 0x18, 0x28 };
                case '+': return new byte[] { 0x08, 0x08, 0x3E, 0x08, 0x08 };
                case ',': return new byte[] { 0x80, 0x40 };
                case '-': return new byte[] { 0x08, 0x08, 0x08, 0x08, 0x08 };
                case '.': return new byte[] { 0x40 };
                case '/': return new byte[] { 0x60, 0x18, 0x06, 0x01 };
                case '0': return new byte[] { 0x3E, 0x41, 0x41, 0x3E };
                case '1': return new byte[] { 0x42, 0x7F, 0x40 };
                case '2': return new byte[] { 0x62, 0x51, 0x49, 0x46 };
                case '3': return new byte[] { 0x22, 0x41, 0x49, 0x36 };
                case '4': return new byte[] { 0x18, 0x14, 0x12, 0x7F };
                case '5': return new byte[] { 0x27, 0x45, 0x45, 0x39 };
                case '6': return new byte[] { 0x3E, 0x49, 0x49, 0x30 };
                case '7': return new byte[] { 0x61, 0x11, 0x09, 0x07 };
                case '8': return new byte[] { 0x36, 0x49, 0x49, 0x36 };
                case '9': return new byte[] { 0x06, 0x49, 0x49, 0x3E };
                case ':': return new byte[] { 0x14 };
                case ';': return new byte[] { 0x20, 0x14 };
                case '<': return new byte[] { 0x08, 0x14, 0x22 };
                case '=': return new byte[] { 0x14, 0x14, 0x14 };
                case '>': return new byte[] { 0x22, 0x14, 0x08 };
                case '?': return new byte[] { 0x02, 0x59, 0x09, 0x06 };
                case '@': return new byte[] { 0x3E, 0x49, 0x55, 0x5D, 0x0E };
                case 'A': return new byte[] { 0x7E, 0x11, 0x11, 0x7E };
                case 'B': return new byte[] { 0x7F, 0x49, 0x49, 0x36 };
                case 'C': return new byte[] { 0x3E, 0x41, 0x41, 0x22 };
                case 'D': return new byte[] { 0x7F, 0x41, 0x41, 0x3E };
                case 'E': return new byte[] { 0x7F, 0x49, 0x49, 0x41 };
                case 'F': return new byte[] { 0x7F, 0x09, 0x09, 0x01 };
                case 'G': return new byte[] { 0x3E, 0x41, 0x49, 0x7A };
                case 'H': return new byte[] { 0x7F, 0x08, 0x08, 0x7F };
                case 'I': return new byte[] { 0x41, 0x7F, 0x41 };
                case 'J': return new byte[] { 0x30, 0x40, 0x41, 0x3F };
                case 'K': return new byte[] { 0x7F, 0x08, 0x14, 0x63 };
                case 'L': return new byte[] { 0x7F, 0x40, 0x40, 0x40 };
                case 'M': return new byte[] { 0x7F, 0x02, 0x0C, 0x02, 0x7F };
                case 'N': return new byte[] { 0x7F, 0x04, 0x08, 0x10, 0x7F };
                case 'O': return new byte[] { 0x3E, 0x41, 0x41, 0x3E };
                case 'P': return new byte[] { 0x7F, 0x09, 0x09, 0x06 };
                case 'Q': return new byte[] { 0x3E, 0x41, 0x41, 0xBE };
                case 'R': return new byte[] { 0x7F, 0x09, 0x09, 0x76 };
                case 'S': return new byte[] { 0x46, 0x49, 0x49, 0x32 };
                case 'T': return new byte[] { 0x01, 0x01, 0x7F, 0x01, 0x01 };
                case 'U': return new byte[] { 0x3F, 0x40, 0x40, 0x3F };
                case 'V': return new byte[] { 0x0F, 0x30, 0x40, 0x30, 0x0F };
                case 'W': return new byte[] { 0x3F, 0x40, 0x38, 0x40, 0x3F };
                case 'X': return new byte[] { 0x63, 0x14, 0x08, 0x14, 0x63 };
                case 'Y': return new byte[] { 0x07, 0x08, 0x70, 0x08, 0x07 };
                case 'Z': return new byte[] { 0x61, 0x51, 0x49, 0x47 };
                case '[': return new byte[] { 0x7F, 0x41 };
                case '\\': return new byte[] { 0x01, 0x06, 0x18, 0x60 };
                case ']': return new byte[] { 0x41, 0x7F };
                case '_': return new byte[] { 0x40, 0x40, 0x40, 0x40 };
                case '`': return new byte[] { 0x01, 0x02 };
                case 'a': return new byte[] { 0x20, 0x54, 0x54, 0x78 };
                case 'b': return new byte[] { 0x7F, 0x44, 0x44, 0x38 };
                case 'c': return new byte[] { 0x38, 0x44, 0x44, 0x28 };
                case 'd': return new byte[] { 0x38, 0x44, 0x44, 0x7F };
                case 'e': return new byte[] { 0x38, 0x54, 0x54, 0x18 };
                case 'f': return new byte[] { 0x04, 0x7E, 0x05 };
                case 'g': return new byte[] { 0x98, 0xA4, 0xA4, 0x78 };
                case 'h': return new byte[] { 0x7F, 0x04, 0x04, 0x78 };
                case 'i': return new byte[] { 0x44, 0x7D, 0x40 };
                case 'j': return new byte[] { 0x40, 0x80, 0x84, 0x7D };
                case 'k': return new byte[] { 0x7F, 0x10, 0x28, 0x44 };
                case 'l': return new byte[] { 0x41, 0x7F, 0x40 };
                case 'm': return new byte[] { 0x7C, 0x04, 0x7C, 0x04, 0x78 };
                case 'n': return new byte[] { 0x7C, 0x04, 0x04, 0x78 };
                case 'o': return new byte[] { 0x38, 0x44, 0x44, 0x38 };
                case 'p': return new byte[] { 0xFC, 0x24, 0x24, 0x18 };
                case 'q': return new byte[] { 0x18, 0x24, 0x24, 0xFC };
                case 'r': return new byte[] { 0x7C, 0x08, 0x04, 0x04 };
                case 's': return new byte[] { 0x48, 0x54, 0x54, 0x24 };
                case 't': return new byte[] { 0x04, 0x3F, 0x44 };
                case 'u': return new byte[] { 0x3C, 0x40, 0x40, 0x7C };
                case 'v': return new byte[] { 0x1C, 0x20, 0x40, 0x20, 0x1C };
                case 'w': return new byte[] { 0x3C, 0x40, 0x3C, 0x40, 0x3C };
                case 'x': return new byte[] { 0x44, 0x28, 0x10, 0x28, 0x44 };
                case 'y': return new byte[] { 0x9C, 0xA0, 0xA0, 0x7C };
                case 'z': return new byte[] { 0x64, 0x54, 0x4C };
                case '{': return new byte[] { 0x08, 0x36, 0x41 };
                case '|': return new byte[] { 0x7F };
                case '}': return new byte[] { 0x41, 0x36, 0x08 };
                case '~': return new byte[] { 0x08, 0x04, 0x08, 0x04 };
                case '^': return new byte[] { 0x02, 0x01, 0x02 };
                case '°': return new byte[] { 0x02, 0x05, 0x02 };
                case '\u00A0': return new byte[] { 0x00 }; // non-breaking space
                default: return null;
            }
        }
    }
}