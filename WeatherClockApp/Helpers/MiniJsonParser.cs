using System;

namespace WeatherClockApp.Helpers
{
    /// <summary>
    /// A lightweight, memory-efficient parser for extracting specific values from a JSON string.
    /// This avoids the overhead of a full deserialization library.
    /// </summary>
    internal static class MiniJsonParser
    {
        /// <summary>
        /// Finds a string value for a given key within a JSON string.
        /// </summary>
        public static string FindValue(string json, string key)
        {
            string searchKey = $"\"{key}\":\"";
            int keyIndex = json.IndexOf(searchKey);
            if (keyIndex == -1)
            {
                // Also try to find non-string values (numbers, booleans)
                searchKey = $"\"{key}\":";
                keyIndex = json.IndexOf(searchKey);
                if (keyIndex == -1) return null;

                int valueStartIndex = keyIndex + searchKey.Length;
                int valueEndIndex = json.IndexOf(',', valueStartIndex);
                int braceEndIndex = json.IndexOf('}', valueStartIndex);

                // Find the first terminator (',' or '}')
                if (valueEndIndex == -1 && braceEndIndex == -1) return null;
                if (valueEndIndex == -1) valueEndIndex = braceEndIndex;
                if (braceEndIndex != -1 && braceEndIndex < valueEndIndex) valueEndIndex = braceEndIndex;

                return json.Substring(valueStartIndex, valueEndIndex - valueStartIndex).Trim();
            }
            else
            {
                int valueStartIndex = keyIndex + searchKey.Length;
                int valueEndIndex = json.IndexOf('"', valueStartIndex);
                if (valueEndIndex == -1) return null;
                return json.Substring(valueStartIndex, valueEndIndex - valueStartIndex);
            }
        }

        /// <summary>
        /// Finds the content of a JSON object for a given key.
        /// </summary>
        public static string FindObject(string json, string key)
        {
            string searchKey = $"\"{key}\":{{";
            int keyIndex = json.IndexOf(searchKey);
            if (keyIndex == -1) return null;

            int valueStartIndex = keyIndex + searchKey.Length;
            int braceCount = 1;
            for (int i = valueStartIndex; i < json.Length; i++)
            {
                if (json[i] == '{') braceCount++;
                if (json[i] == '}') braceCount--;
                if (braceCount == 0)
                {
                    return json.Substring(valueStartIndex, i - valueStartIndex);
                }
            }
            return null; // Closing brace not found
        }
    }
}
