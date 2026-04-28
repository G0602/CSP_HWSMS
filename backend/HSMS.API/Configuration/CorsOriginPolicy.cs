namespace HSMS.API.Configuration;

public static class CorsOriginPolicy
{
	public static bool IsAllowedFrontendOrigin(string? origin, IReadOnlyCollection<string> allowedOrigins)
	{
		if (string.IsNullOrWhiteSpace(origin))
		{
			return false;
		}

		string normalizedOrigin = origin.Trim().TrimEnd('/');
		if (allowedOrigins.Contains(normalizedOrigin, StringComparer.OrdinalIgnoreCase))
		{
			return true;
		}

		if (!Uri.TryCreate(normalizedOrigin, UriKind.Absolute, out var uri))
		{
			return false;
		}

		if (uri.Scheme != Uri.UriSchemeHttps)
		{
			return false;
		}

		string host = uri.Host;
		return host.EndsWith(".azurewebsites.net", StringComparison.OrdinalIgnoreCase)
			|| (host.StartsWith("csp-hwsms", StringComparison.OrdinalIgnoreCase)
				&& host.EndsWith(".vercel.app", StringComparison.OrdinalIgnoreCase))
			|| host.EndsWith(".azurestaticapps.net", StringComparison.OrdinalIgnoreCase);
	}
}