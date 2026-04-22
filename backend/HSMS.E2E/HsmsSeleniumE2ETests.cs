using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace HSMS.E2E;

public class HsmsSeleniumE2ETests
{
    private static readonly TimeSpan WaitTimeout = TimeSpan.FromSeconds(15);

    [Fact]
    public void LoginFlow_Should_Navigate_To_Authorized_Landing_Page()
    {
        using var driver = CreateDriverIfConfigured();
        if (driver is null)
        {
            return;
        }

        Login(driver);

        var wait = new WebDriverWait(driver, WaitTimeout);
        wait.Until(d =>
            d.Url.Contains("/dashboard", StringComparison.OrdinalIgnoreCase) ||
            d.Url.Contains("/sales", StringComparison.OrdinalIgnoreCase) ||
            d.Url.Contains("/access-denied", StringComparison.OrdinalIgnoreCase));

        Assert.DoesNotContain("/login", driver.Url, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SalesFlow_Should_Show_Sales_Workspace_And_Empty_Cart_State()
    {
        using var driver = CreateDriverIfConfigured();
        if (driver is null)
        {
            return;
        }

        Login(driver);
        driver.Navigate().GoToUrl($"{GetBaseUrl()}/sales");

        var wait = new WebDriverWait(driver, WaitTimeout);
        wait.Until(d => d.FindElement(By.TagName("body")).Text.Contains("Sales Transaction", StringComparison.OrdinalIgnoreCase));

        string bodyText = driver.FindElement(By.TagName("body")).Text;
        Assert.Contains("Cart", bodyText);
        Assert.Contains("Confirm Sale", bodyText);
    }

    [Fact]
    public void ReportViewingFlow_Should_Render_Analytics_Dashboard()
    {
        using var driver = CreateDriverIfConfigured();
        if (driver is null)
        {
            return;
        }

        Login(driver);
        driver.Navigate().GoToUrl($"{GetBaseUrl()}/reports/daily");

        var wait = new WebDriverWait(driver, WaitTimeout);
        wait.Until(d => d.FindElement(By.TagName("body")).Text.Contains("Sales Analytics Dashboard", StringComparison.OrdinalIgnoreCase));

        string bodyText = driver.FindElement(By.TagName("body")).Text;
        Assert.Contains("Export Daily CSV", bodyText);
        Assert.Contains("From date", bodyText);
    }

    private static IWebDriver? CreateDriverIfConfigured()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("HSMS_E2E_BASE_URL")) ||
            string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("HSMS_E2E_USERNAME")) ||
            string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("HSMS_E2E_PASSWORD")))
        {
            return null;
        }

        var options = new ChromeOptions();
        options.AddArgument("--headless=new");
        options.AddArgument("--no-sandbox");
        options.AddArgument("--disable-dev-shm-usage");
        options.AddArgument("--window-size=1440,1000");

        return new ChromeDriver(options);
    }

    private static void Login(IWebDriver driver)
    {
        string username = Environment.GetEnvironmentVariable("HSMS_E2E_USERNAME")!;
        string password = Environment.GetEnvironmentVariable("HSMS_E2E_PASSWORD")!;

        driver.Navigate().GoToUrl($"{GetBaseUrl()}/login");

        var wait = new WebDriverWait(driver, WaitTimeout);
        wait.Until(d => d.FindElement(By.TagName("body")).Text.Contains("Sign In", StringComparison.OrdinalIgnoreCase));

        driver.FindElement(By.CssSelector("input[autocomplete='username']")).SendKeys(username);
        driver.FindElement(By.CssSelector("input[autocomplete='current-password']")).SendKeys(password);
        driver.FindElement(By.CssSelector("button[type='submit']")).Click();
    }

    private static string GetBaseUrl()
    {
        return Environment.GetEnvironmentVariable("HSMS_E2E_BASE_URL")!.TrimEnd('/');
    }
}
