namespace BackedFramework
{
    /// <summary>
    /// The configuration class for the framework
    /// </summary>
    public class BackedConfig
    {
        /// <summary>
        /// The default path for routing API calls, must include the trailing slash.
        /// </summary>
        /// <remarks>Example: "/api"</remarks>
        public string ApiPath { get; set; } = "/api";
        /// <summary>
        /// The default API version, doesnt have to be used, but still here for completeness.
        /// </summary>
        public string ApiVersion { get; set; } = "v1";
        
    }
}