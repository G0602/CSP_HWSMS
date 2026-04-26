using HSMS.API.Configuration;

namespace HSMS.Tests;

public class CorsOriginPolicyTests
{
	[Theory]
	[InlineData("https://delightful-tree-0e4ad5000.7.azurestaticapps.net")]
	[InlineData("https://csp-hwsms-hnqk.vercel.app")]
	[InlineData("https://my-frontend-app.azurewebsites.net")]
	public void IsAllowedFrontendOrigin_Should_Accept_Known_Production_Origins(string origin)
	{
		bool allowed = CorsOriginPolicy.IsAllowedFrontendOrigin(origin, Array.Empty<string>());

		Assert.True(allowed);
	}

	[Fact]
	public void IsAllowedFrontendOrigin_Should_Reject_Unknown_Origins()
	{
		bool allowed = CorsOriginPolicy.IsAllowedFrontendOrigin("https://evil.example.com", Array.Empty<string>());

		Assert.False(allowed);
	}

	[Fact]
	public void IsAllowedFrontendOrigin_Should_Allow_Explicit_Configured_Origins()
	{
		bool allowed = CorsOriginPolicy.IsAllowedFrontendOrigin(
			"https://custom-frontend.example.com",
			["https://custom-frontend.example.com"]);

		Assert.True(allowed);
	}
}