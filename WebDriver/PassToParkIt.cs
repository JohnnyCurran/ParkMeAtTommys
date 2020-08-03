using OpenQA.Selenium;
using Contracts;
using System.Drawing;
using WebDriver.Constants;
using OpenQA.Selenium.Chrome;
using System.Threading;
using System;
using System.IO;
using OpenQA.Selenium.Support.UI;

namespace WebRunner
{
    public class PassToParkIt : IPassToParkIt
    {
	IWebDriver driver;
	ChromeOptions options;
	private const int iPhoneXWidth = 375;
	private const int iPhoneXHeight = 812;

	public PassToParkIt()
	{
	    this.options = new ChromeOptions();
	    options.AddArgument("--headless");
	}

	public void ChromedriverSmokeTest()
	{
	    try
	    {
		this.driver = new ChromeDriver(options);
		driver.Manage().Window.Size = new Size(iPhoneXWidth, iPhoneXHeight);

		driver.Navigate().GoToUrl("https://news.ycombinator.com");

		var pass = ((ITakesScreenshot)driver).GetScreenshot();
		var imageLocation = $"{Directory.GetCurrentDirectory()}/smoketest.png";
		pass.SaveAsFile(imageLocation, ScreenshotImageFormat.Png);
	    }
	    catch (Exception e)
	    {
	        Console.WriteLine("Smoke test failed: " + e.Message);
	    }
	    finally
	    {
	        driver.Quit();
		this.driver = null;
	    }
	}

	public ParkingResult ParkMyCar(ParkingInformation parkInfo)
	{
	    var parkingAttemptResult = new ParkingResult();

	    try
	    {
	        this.driver = new ChromeDriver(options);
	        driver.Manage().Window.Size = new Size(iPhoneXWidth, iPhoneXHeight);
		var visitResult = VerifyMyVisit(parkInfo.PhoneNumber, parkInfo.ApartmentNumber);
		var detailsResult = EnterVehicleDetails(parkInfo.PlateNumber, parkInfo.PlateState, parkInfo.CarMake, parkInfo.CarModel, parkInfo.CarColor, parkInfo.CarYear);
		var confirmResult = ConfirmRules();

		var ssLocation = Path.GetFullPath(GetPass());
		Console.WriteLine(ssLocation);

		parkingAttemptResult.Success = (visitResult.Success && detailsResult.Success && confirmResult.Success);
		parkingAttemptResult.ParkingPassLocation = ssLocation;
	    }
	    catch (Exception e)
	    {
		parkingAttemptResult.Success = false;
		parkingAttemptResult.ErrorMessage = $"{e.Message} ::: {e.StackTrace}";
	    }
	    finally
	    {
		driver.Quit();
		this.driver = null;
	    }
	    return parkingAttemptResult;
	}

	private ParkingResult VerifyMyVisit(string phoneNumber, string apartmentNumber)
	{
	    driver.Navigate().GoToUrl(WebDriverConstants.VerifyVisitUrl);

	    IWebElement phoneNumberField = driver.FindElement(By.Id(WebDriverConstants.PhoneNumberId));
	    IWebElement apartmentNumberField = driver.FindElement(By.Id(WebDriverConstants.ApartmentNumberId));
	    IWebElement authorizeButton = driver.FindElement(By.TagName(WebDriverConstants.VerifyDetailsButton));

	    phoneNumberField.Click();
	    phoneNumberField.SendKeys(phoneNumber);

	    apartmentNumberField.Click();
	    apartmentNumberField.SendKeys(apartmentNumber);

	    authorizeButton.Click();

	    // Give time for Modal to appear
	    Thread.Sleep(1000);

	    IWebElement confirmLocationBtn = driver.FindElement(By.ClassName(WebDriverConstants.ConfirmLocationClassName));
	    confirmLocationBtn.Click();

	    return new ParkingResult { Success = true };
	}

	private ParkingResult EnterVehicleDetails(string licensePlateNumber, string state, string make, string model, string color, string year)
	{
	    IWebElement licensePlateField = driver.FindElement(By.Id(WebDriverConstants.LicensePlateNumberField));
	    IWebElement stateField = driver.FindElement(By.Id(WebDriverConstants.StateField));
	    IWebElement makeField = driver.FindElement(By.Id(WebDriverConstants.MakeField));
	    IWebElement modelField = driver.FindElement(By.Id(WebDriverConstants.ModelField));
	    IWebElement colorField = driver.FindElement(By.Id(WebDriverConstants.ColorField));
	    IWebElement yearField = driver.FindElement(By.Id(WebDriverConstants.YearField));
	    IWebElement authorizeButton = driver.FindElement(By.TagName(WebDriverConstants.VerifyDetailsButton));

	    licensePlateField.Click();
	    licensePlateField.SendKeys(licensePlateNumber);

	    stateField.Click();
	    stateField.SendKeys(state);

	    makeField.Click();
	    makeField.SendKeys(make);

	    modelField.Click();
	    modelField.SendKeys(model);

	    colorField.Click();
	    colorField.SendKeys(color);

	    yearField.Click();
	    yearField.SendKeys(year);

	    authorizeButton.Click();

	    // Give time for modal to appear
	    Thread.Sleep(1000);

	    IWebElement confirmBtn = driver.FindElement(By.ClassName(WebDriverConstants.ConfirmLocationClassName));
	    confirmBtn.Click();

	    return new ParkingResult { Success = true };
	}

	private ParkingResult ConfirmRules()
	{
	    WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(5));
	    bool title = wait.Until(ExpectedConditions.TitleContains(WebDriverConstants.ParkingRulesTitle));

	    if (title)
	    {
		IJavaScriptExecutor jsExecutor = (IJavaScriptExecutor)driver;
		jsExecutor.ExecuteScript(WebDriverConstants.GetPassFn);
	    }
	    else
	    {
		return new ParkingResult
		{
		    Success = false,
		    ErrorMessage = "Title not found. Failure to confirm rules."
		};
	    }

	    return new ParkingResult { Success = true };
	}

	private string GetPass()
	{
	    WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(5));
	    bool title = wait.Until(ExpectedConditions.TitleContains(WebDriverConstants.ParkingApprovedTitle));
	    
	    // Take screenshot of pass
	    var pass = ((ITakesScreenshot)driver).GetScreenshot();
	    var imageLocation = $"{Directory.GetCurrentDirectory()}/ParkingPass-{DateTime.Now.ToShortDateString().Replace('/', '-')}.png";
	    pass.SaveAsFile(imageLocation, ScreenshotImageFormat.Png);
	    return imageLocation;
	}
    }
}
