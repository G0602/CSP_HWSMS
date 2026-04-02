using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace HSMS.E2E;

public class SmokeE2ETests
{
[Fact]
public void HomePage_Loads_And_Shows_Title()
{
var options = new ChromeOptions();
options.AddArgument("--start-maximized");
 using IWebDriver driver = new ChromeDriver(options);
    driver.Navigate().GoToUrl("http://localhost:5173");

    var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
    wait.Until(d => !string.IsNullOrWhiteSpace(d.Title));

    Assert.False(string.IsNullOrWhiteSpace(driver.Title));
}
}