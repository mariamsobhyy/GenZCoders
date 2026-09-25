namespace GenZCoders.Extensions
{
    public static class SafeUrl
    {
        // Links are rendered and opened in the browser, so only http(s) or our own "/uploads/..." paths
        // are accepted; anything else (javascript:, data:, ...) could run script in another user's session.
        public static bool IsSafe(string? url)
        {
            if (string.IsNullOrWhiteSpace(url)) return true;

            var value = url.Trim();
            if (value.StartsWith('/')) return !value.StartsWith("//") && !value.StartsWith("/\\");

            return Uri.TryCreate(value, UriKind.Absolute, out var uri)
                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
        }
    }
}
