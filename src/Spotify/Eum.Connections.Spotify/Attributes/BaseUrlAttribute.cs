using System;

namespace Eum.Connections.Spotify.Attributes
{
    /// <summary>
    ///     Specify a Base URL for an entire Refit interface
    /// </summary>
    [AttributeUsage(AttributeTargets.Interface)]
    public class BaseUrlAttribute
        : Attribute
    {
        /// <summary>
        ///     Initializes the <see cref="BaseUrlAttribute" />
        /// </summary>
        /// <param name="baseUrl"></param>
        public BaseUrlAttribute(
            string baseUrl)
        {
            BaseUrl = baseUrl;
        }

        /// <summary>
        ///     The stored base URL
        /// </summary>
        public string BaseUrl { get; set; }
    }
}