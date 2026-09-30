namespace PitchProAPI.Helpers
{
    public static class HttpRequestHelper
    {
        public static string? GetIpAddress(HttpContext context)
        {
            return context.Connection.RemoteIpAddress?.ToString();
        }

        public static string? GetDeviceName(HttpContext context)
        {
            return context.Request.Headers.UserAgent.ToString();
        }
    }
}
