# SmartWebDriver
Smart extensions for Selenium.WebDriver. This wrapper is intended to extend upon the built-in browser functions to add logging, retry functionality, better error logging and generally simplify the code required for interacting with the browser, while also making it more powerful.

## Getting Started

These instructions will get you a copy of the project up and running on your local machine for development and testing purposes. See deployment for notes on how to deploy the project on a live system.

### Prerequisites

None

### Installing

Using NuGet (recommended):
Run `Install-Package smartwebdriver` from the Package Manager Console in Visual Studio. See the [NuGet documentation]() for further details.

## How to use

We recommend using the PageObject model approach to developing your tests. See below for an example Page model, and Test, using the SmartWebDriver code for testing a LogIn page.

### LoginPage.cs
```
using System;
using SmartWebDriver;
using SmartWebDriver.Extensions;

namespace TestProject.Pages
{
	public class LoginPage
	{
		public static string Url = "http://mywebsite.com/login";
		
		private readonly PageElement UsernameTxt = new PageElement("Username text field") { ID = "username" };
		private readonly PageElement PasswordTxt = new PageElement("Password text field") { ID = "password" };
		private readonly PageElement LoginBtn = new PageElement("Login button") { ID = "log-in" };
		
		private readonly WebBrowser _browser;
		
		public LoginPage(WebBrowser browser)
		{
			_browser = browser;
		}
		
		public void LoginAs(string username, string password)
		{
			_browser.EnterText(UsernameTxt, username);
			_browser.EnterText(PasswordTxt, password);
			
			_browser.Click(LoginBtn);
		}
		
		// There are simpler ways to demonstrate a successful login, but this
		// approach was chosen to show how to use the 'TestResponse' object
		public TestResponse VerifyLoginHasSucceeded()
		{
			// a successful login is indicated by the url parameter 'success'
			// wait for that, and include an error message to help the user know
			// what we were trying to do and what went wrong
			var currentUrl = _browser.GetUrl();
			return new TestResponse(currentUrl.Contains("success"), "Was trying to verify the login was successful, expected to see a url parameter of 'success' but didn't. Current url is: " + currentUrl + "\nEnsure the login details are correct.");
		}
	}
}
```
### LoginTests.cs
The test below uses this Login Page model, and can complete the Login functionality, without needing to know what steps are involved in logging in, how the page elements were found and interacted with, or even which framework was being used. (Which means we can swap this out later if we wanted to)
```
using SmartWebDriver;
using TestProject.Pages;

namespace TestProject.Tests
{
	public class LoginTests()
	{
		private TestData testData;
		
		[SetUp]
		public void TestSetup()
		{
			// whatever method you use to access login credentials
			testData = TestAccounts.GetLoginAccountData();
		}
	
		[Test]
		public void VerifyLoginWithValidData()
		{
			var browser = new WebBrowser();
			browser.NavigateTo(LoginPage.Url);
			var loginPage = new LoginPage(browser);
			loginPage.LoginAs(testData.Username, testData.password);
			
			// verify the login was successful
			Wait.UpTo(20.Seconds()).For(() => loginPage.VerifyLoginHasSucceeded());
		}
	}
}
```
Further examples will be added over time.

## Contributing

Please read [CONTRIBUTING.md](https://github.com/campaignmonitor/smartwebdriver/blob/master/CONTRIBUTING.md) for guidelines for contributing to this repository.

## Authors

See the list of [contributors](https://github.com/campaignmonitor/SmartWebDriver/graphs/contributors) who participated in this project.