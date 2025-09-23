namespace WeatherClockApp.Models
{
    /// <summary>
    /// Represent an URL parameter Name=Value
    /// </summary>
    internal class UrlParameter
    {
        /// <summary>
        /// Name of the parameter
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Value of the parameter
        /// </summary>
        public string Value { get; set; }
    }
}