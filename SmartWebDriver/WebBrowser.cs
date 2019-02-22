using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;
using SmartWebDriver.Extensions;

namespace SmartWebDriver
{
    public class WebBrowser
    {
        private IWebDriver _webdriver;

        public WebBrowser(IWebDriver driver)
        {
            _webdriver = driver;
        }

        public WebBrowser(BrowserOptions browserOptions = null)
        {
            // the drivers folder containing the .exe will be in the same folder as the dll running the tests, find it
            var driverDirectory =
                Path.GetDirectoryName(
                    Uri.UnescapeDataString(new UriBuilder(Assembly.GetExecutingAssembly().CodeBase).Path)) +
                @"\Drivers";
            StartWebbrowser(driverDirectory, browserOptions ?? new BrowserOptions());
        }

        public WebBrowser(string path, BrowserOptions browserOptions)
        {
            StartWebbrowser(path, browserOptions);
        }

        private void StartWebbrowser(string path, BrowserOptions browerOptions)
        {
            switch (browerOptions.BrowserType)
            {
                case (Browsers.Chrome):
                    var chromeOptions = new ChromeOptions();
                    chromeOptions.AddArguments("--dns-prefetch-disable", "start-maximized", "test-type");
                        if (browerOptions.RunInIncognito)
                        {
                            // opens a private browser window, immune to cookies of other open windows (unless also private)
                            chromeOptions.AddArgument("--incognito");
                        }
                        if (browerOptions.AllowInsecureContent)
                        {
                            chromeOptions.AddArgument("--allow-running-insecure-content");
                        }
                    _webdriver = new ChromeDriver(path, chromeOptions);
                    break;
                case (Browsers.Firefox):
                    _webdriver = new FirefoxDriver(path);
                    _webdriver.Manage().Window.Maximize();
                    break;
            }
        }

        public void AcceptAlert()
        {
            _webdriver.SwitchTo().Alert().Accept();
        }

        public void CaptureWebPageToFile(string filePath)
        {
            try
            {
                // for some reason, we have to convert the script response to a 'long' before we can convert it to an 'int'
                var totalWidth =
                    (int)(long)((IJavaScriptExecutor)_webdriver).ExecuteScript("return document.body.offsetWidth");

                var totalHeight =
                    (int)(long)
                        ((IJavaScriptExecutor)_webdriver).ExecuteScript("return  document.body.parentNode.scrollHeight");

                // Get the Size of the Viewport
                var viewportWidth = (int)
                    (long)
                        ((IJavaScriptExecutor)_webdriver).ExecuteScript("return document.body.clientWidth");

                var viewportHeight =
                    (int)(long)((IJavaScriptExecutor)_webdriver).ExecuteScript("return window.innerHeight");

                if (totalHeight == 0)
                {
                    totalHeight = viewportHeight;
                }

                // Split the Screen in multiple Rectangles
                var rectangles = new List<Rectangle>();
                // Loop until the Total Height is reached
                for (var i = 0; i < totalHeight; i += viewportHeight)
                {
                    var newHeight = viewportHeight;
                    // Fix if the Height of the Element is too big
                    if (i + viewportHeight > totalHeight)
                    {
                        newHeight = totalHeight - i;
                    }
                    // Loop until the Total Width is reached
                    for (var ii = 0; ii < totalWidth; ii += viewportWidth)
                    {
                        var newWidth = viewportWidth;
                        // Fix if the Width of the Element is too big
                        if (ii + viewportWidth > totalWidth)
                        {
                            newWidth = totalWidth - ii;
                        }

                        // Create and add the Rectangle
                        var currRect = new Rectangle(ii, i, newWidth, newHeight);
                        rectangles.Add(currRect);
                    }
                }

                // Build the Image
                using (var stitchedImage = new Bitmap(totalWidth, totalHeight))
                {
                    // Get all Screenshots and stitch them together
                    var previous = Rectangle.Empty;
                    foreach (var rectangle in rectangles)
                    {
                        // Calculate the Scrolling (if needed)
                        if (previous != Rectangle.Empty)
                        {
                            var xDiff = rectangle.Right - previous.Right;
                            var yDiff = rectangle.Bottom - previous.Bottom;
                            // Scroll
                            ((IJavaScriptExecutor)_webdriver).ExecuteScript($"window.scrollBy({xDiff}, {yDiff})");
                            Thread.Sleep(100);
                        }

                        // Take Screenshot
                        var screenshot = ((ITakesScreenshot)_webdriver).GetScreenshot();

                        // Build an Image out of the Screenshot
                        Image screenshotImage;
                        using (var memStream = new MemoryStream(screenshot.AsByteArray))
                        {
                            screenshotImage = Image.FromStream(memStream);
                        }

                        // Calculate the Source Rectangle
                        var sourceRectangle = new Rectangle(viewportWidth - rectangle.Width,
                            viewportHeight - rectangle.Height, rectangle.Width,
                            rectangle.Height);

                        // Copy the Image
                        using (var g = Graphics.FromImage(stitchedImage))
                        {
                            g.DrawImage(screenshotImage, rectangle, sourceRectangle, GraphicsUnit.Pixel);
                        }

                        // Set the Previous Rectangle
                        previous = rectangle;
                    }
                    // save the stitched image
                    stitchedImage.Save(filePath, ImageFormat.Jpeg);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to take a full-page screenshot, got this error:\n" + ex.Message + "\n" +
                                  ex.StackTrace);
            }
        }

        public void Check(PageElement pageElement)
        {
            WaitForAny(pageElement, 30.Seconds());
            var element = GetElement(pageElement);

            if (!element.Selected)
            {
                element.Click();
            }
        }

        public void Clear(PageElement pageElement)
        {
            var element = GetElement(pageElement);
            try
            {
                element.Clear();
                Thread.Sleep(500);
                // forcing the sleep here as the clear action is slightly delayed and caused problems in the next actions like sendkeys
            }
            catch (Exception e)
            {
                throw new Exception("Tried to clear: " + pageElement.Description + ",\n\nbut got an error: " +
                                    e.Message + ",\n\nStack Trace: \n" + e.StackTrace);
            }
        }

        public void Click(PageElement pageElement, bool scrollIntoViewBeforeClick = true)
        {
            // Adding a micro sleep to provide a little more robustness before trying to execute a click
            Thread.Sleep(200);

            var webElement = GetElement(pageElement);
            if (scrollIntoViewBeforeClick)
            {
                ScrollIntoView(webElement);
            }
            Click(webElement, pageElement.Description);
        }

        /// <summary>
        /// Click the web element previously found by using a PageElement reference
        /// </summary>
        /// <param name="webElement"></param>
        /// <param name="description"></param>
        public void Click(IWebElement webElement, string description)
        {
            try
            {
                webElement.Click();
            }
            catch (WebDriverTimeoutException)
            {
                // if you get a web driver timeout, try and ignore it and see if we can continue, the page might've
                // already progressed and hence a false failure.
                // Log the error in case we couldn't progress so we know this might have been a cause
                Log("Attempted to click the element '" + description + "' and got a timeout exception, trying to continue anyway");
            }
            catch (Exception e)
            {
                throw new Exception("Attempted to click the element '" + description + "' but failed", e);
            }
        }

        /// <summary>
        /// Close the current window, and quit the browser if no windows left
        /// </summary>
        public void Close()
        {
            _webdriver.Close();
        }

        public void DismissAlert()
        {
            _webdriver.SwitchTo().Alert().Dismiss();
        }

        public void EnterText(PageElement pageElement, string text)
        {
            var webElement = GetElement(pageElement);
            try
            {
                webElement.Clear();
                // short delay to let the clear event take affect
                Thread.Sleep(500.Milliseconds());
                webElement.SendKeys(text);
            }
            catch (Exception e)
            {
                throw new Exception("Tried to enter the text '" + text + "' into element '" + pageElement.Description + "' but failed", e);
            }
        }

        public void ExecuteScript(string script, params object[] args)
        {
            ((IJavaScriptExecutor) _webdriver).ExecuteScript(script, args);
        }

        public bool Exists(PageElement pageElement)
        {
            try
            {
                var elements = GetElements(pageElement);
                return elements.Count > 0;
            }
            catch
            {
                return false;
            }
        }

        public void FileUpload(PageElement pageElement, string filePath)
        {
            var webElement = GetElement(pageElement);
            try
            {
                ScrollIntoView(webElement);
                webElement.SendKeys(filePath);
            }
            catch (Exception e)
            {
                throw new Exception("Tried to upload a file from '" + filePath + "' into element: " + pageElement.Description + "\nBut I got an error", e);
            }
        }

        public ReadOnlyCollection<IWebElement> FindAll(PageElement pageElement)
        {
            return GetElements(pageElement);
        }

        public string GetAttributeValue(PageElement pageElement, string attributeName)
        {
            var webElement = GetElement(pageElement);
            try
            {
                return webElement.GetAttribute(attributeName);
            }
            catch (Exception e)
            {
                throw new Exception("Failed to get the '" + attributeName + "' attribute value of the element: " + pageElement.Description, e);
            }
        }

        private IWebElement GetElement(PageElement element)
        {
            var selectorTried = "";
            var searchContext = GetElementFinder(element, out selectorTried);
            try
            {
               return _webdriver.FindElement(searchContext);
            }
            catch (Exception e)
            {
                throw new Exception("Failed to find element '" + element.Description + "'.\nSelector used: " +
                                    selectorTried + "\nException thrown: " +
                                    e.Message);
            }
        }

        private static By GetElementFinder(PageElement element, out string selectorTried)
        {
            switch (element.LocatorUsed)
            {
                case ElementLocators.ID:
                    selectorTried = "id: " + element.ID;
                    return By.Id(element.ID);
                case ElementLocators.Name:
                    selectorTried = "name: " + element.Name;
                    return By.Name(element.Name);
                case ElementLocators.LinkInnerText:
                    selectorTried = "Link Innertext: " + element.LinkInnerText;
                    return By.PartialLinkText(element.LinkInnerText);
                case ElementLocators.Href:
                    selectorTried = "href: " + element.Href;
                    return By.CssSelector("a[href*=\"" + element.Href + "\"]");
                case ElementLocators.Class:
                    selectorTried = "class: " + element.Class;
                    return By.ClassName(element.Class);
                case ElementLocators.Tag:
                    selectorTried = "tag: " + element.Tag;
                    return By.TagName(element.Tag);
                case ElementLocators.Css:
                    selectorTried = "css: " + element.Css;
                    return By.CssSelector(element.Css);
                case ElementLocators.XPath:
                    selectorTried = "xPath: " + element.XPath;
                    return By.XPath(element.XPath);
                default:
                    throw new Exception("Need to specify at least 1 method (ID, Name, XPath, etc...) for finding the page element");
            }
        }

        private ReadOnlyCollection<IWebElement> GetElements(PageElement element)
        {
            var selectorTried = "";
            var searchContext = GetElementFinder(element, out selectorTried);
            try
            {
                return _webdriver.FindElements(searchContext);
            }
            catch (Exception e)
            {
                throw new Exception("Failed to find elements '" + element.Description + "'.\nSelector used: " +
                                    selectorTried + "\nException thrown: " +
                                    e.Message);
            }
        }

        public string GetInnerHtml(PageElement pageElement)
        {
            return GetAttributeValue(pageElement, "innerHTML");
        }

        /// <summary>
        /// Return number of open tabs
        /// </summary>
        /// <returns></returns>
        public int GetNumberOfOpenTabs()
        {
            return _webdriver.WindowHandles.Count;
        }

        /// <summary>
        /// Get the Text within the body of the HTML page
        /// </summary>
        /// <returns>Text of the HTML page</returns>
        public string GetPageBodyText()
        {
            try
            {
                // Set the page load timeout so the page has a bit of time to
                // load but also timeout when it takes too long
                _webdriver.Manage().Timeouts().PageLoad = 5.Seconds();
                return _webdriver.FindElement(By.TagName("body")).Text;
            }
            catch
            {
                return "";
            }
        }

        /// <summary>
        /// Get the full html source of the page
        /// </summary>
        /// <returns></returns>
        public string GetPageSource()
        {
            return _webdriver.PageSource;
        }

        /// <summary>
        /// Get the section by section number <paramref name="sectionIndex"/> within <paramref name="pageElement"/>
        /// </summary>
        /// <param name="pageElement">The element with sections</param>
        /// <param name="sectionIndex">The index of the desired section</param>
        /// <returns>The section at the desired index of the element</returns>
        public IWebElement GetSection(PageElement pageElement, int sectionIndex)
        {
            var webElementsFound = GetElements(pageElement);
            return webElementsFound.ElementAt(sectionIndex);
        }

        public List<string> GetSelectedOptions(PageElement pageElement)
        {
            var webElement = GetElement(pageElement);
            try
            {
                var selectElement = new SelectElement(webElement);
                var options = selectElement.AllSelectedOptions;
                return options.Select(option => option.Text).ToList();
            }
            catch (Exception e)
            {
                throw new Exception(
                    "Tried to get the currently selected option(s) from '" + pageElement.Description +
                    "' but there was an error", e);
            }
        }

        public List<string> GetSelectOptions(PageElement pageElement)
        {
            WaitForAny(pageElement, 30.Seconds());
            var webElement = GetElement(pageElement);
            try
            {
                var selectElement = new SelectElement(webElement);
                return selectElement.Options.Select(option => option.Text).ToList();
            }
            catch (Exception e)
            {
                throw new Exception(
                    "Tried to get the list of options for the select element '" + pageElement.Description +
                    "' but got an error.", e);
            }
        }

        public string GetText(PageElement pageElement)
        {
            var webElement = GetElement(pageElement);
            try
            {
                return webElement.Text;
            }
            catch (Exception e)
            {
                throw new Exception("Tried to get the text for the element '" + pageElement.Description +
                    "' but got an error.", e);
            }
        }

        public string GetTitle()
        {
            return _webdriver.Title;
        }

        public string GetUrl()
        {
            return _webdriver.Url;
        }

        public void GoBack()
        {
            _webdriver.Navigate().Back();
        }

        private static bool DoesElementHaveNoOutstandingAngularRequests(IWebDriver driver, string cssSelector)
        {
            var executor = driver as IJavaScriptExecutor;

            if (driver == null)
            {
                throw new ArgumentException("Must be a javascript executor");
            }
            const string script = @"
                return (function(selector) {
                    var b = false;
                    var callback = function() { b = true; };
                    var el = document.querySelector(selector);
                    angular.element(el).injector().get('$browser').
                          notifyWhenNoOutstandingRequests(callback);
                    return b;
                })(arguments[0]);``
                ";
            var done = (bool)executor.ExecuteScript(script, cssSelector);

            if (done)
            {
                Thread.Sleep(500);
            }

            return done;
        }

        public bool IsEnabled(PageElement pageElement)
        {
            var webElement = GetElement(pageElement);
            try
            {
                ScrollIntoView(webElement);
                return webElement.Enabled;
            }
            catch
            {
                return false;
            }
        }

        public bool IsSelected(PageElement pageElement)
        {
            var webElement = GetElement(pageElement);
            try
            {
                return webElement.Selected;
            }
            catch (Exception e)
            {
                throw new Exception(
                    "Tried to determine if the '" + pageElement.Description +
                    "' element was selected, but go an error.", e);
            }
        }

        public bool IsVisible(PageElement pageElement)
        {
            try
            {
                var webElement = GetElement(pageElement);
                ScrollIntoView(webElement);
                return webElement.Displayed;
            }
            catch
            {
                return false;
            }
        }

        public void JavascriptClick(PageElement pageElement)
        {
            var webElement = GetElement(pageElement);
            JavascriptClick(webElement);
        }

        public void JavascriptClick(IWebElement webElement)
        {
            try
            {
                ExecuteScript("arguments[0].click();", webElement);
            }
            catch (Exception e)
            {
                throw new Exception("Tried to click '" + webElement.Text + "' via Javascript, but got an error.", e);
            }
        }

        private static void Log(string message)
        {
            var time = DateTime.Now.ToString("hh:mm:s");
            Console.WriteLine(time + ": " + message);
        }

        public void NavigateTo(string url)
        {
            try
            {
                Log("Navigating to: " + url);
                _webdriver.Manage().Timeouts().PageLoad = 30.Seconds();
                _webdriver.Navigate().GoToUrl(url);
            }
            catch (WebDriverTimeoutException)
            {
                // sometimes we get a timeout error, even though the page has actually loaded
                // if this is the case, ignore the error and continue, otherwise throw exception
                if (!GetUrl().Contains(url))
                {
                    throw;
                }
            }
            catch (WebDriverException wde)
            {
                throw new Exception($"Tried to navigate to '{url}' but got an exception", wde);
            }
        }

        /// <summary>
        /// Navigate to the given URL and confirm that it redirects you to the second provided URL
        /// </summary>
        /// <param name="url">The URL to navigate to</param>
        /// <param name="expectedRedirectUrl">The URL that you should be redirected to</param>
        public void NavigateToAndConfirmRedirect(string url, string expectedRedirectUrl)
        {
            var timeout = 30.Seconds();
            try
            {
                NavigateTo(url);
                WaitForUrl(expectedRedirectUrl, timeout);
            }
            catch (WebDriverTimeoutException)
            {
                // sometimes we get a timeout error, even though the page has actually loaded
                // if this is the case, ignore the error and continue, otherwise try one more time,
                // then throw exception
                var currentUrl = GetUrl();
                if (!currentUrl.Contains(expectedRedirectUrl))
                {
                    try
                    {
                        NavigateTo(url);
                        WaitForUrl(expectedRedirectUrl, timeout);
                    }
                    catch (Exception e)
                    {
                        throw new Exception(
                            "Tried to navigate to:\n" + url + ",\nexpecting it to redirect to:\n" +
                            expectedRedirectUrl + ".\nWaited for " + timeout.TotalSeconds +
                            " seconds, but instead I got an error.\bnCurrent url: " + currentUrl, e);
                    }
                }
            }
        }

        public void PressEnter(PageElement pageElement)
        {
            var webElement = GetElement(pageElement);
            try
            {
                ScrollIntoView(webElement);
                webElement.SendKeys(Keys.Enter);
            }
            catch (Exception e)
            {
                throw new Exception(
                    "Tried to send 'ENTER' to element: " + pageElement.Description + ".\nBut got an error", e);
            }
        }

        public void PressEscape()
        {
            new Actions(_webdriver).SendKeys(Keys.Escape).Build().Perform();
        }

        public void PressTab(PageElement pageElement)
        {
            var webElement = GetElement(pageElement);
            try
            {
                ScrollIntoView(webElement);
                webElement.SendKeys(Keys.Tab);
            }
            catch (Exception e)
            {
                throw new Exception(
                    "Tried to send 'TAB' to element: " + pageElement.Description + ".\nBut got an error", e);
            }
        }

        /// <summary>
        /// Close the driver and every associated window
        /// </summary>
        public void Quit()
        {
            _webdriver.Quit();
        }

        public void Refresh()
        {
            try
            {
                _webdriver.Navigate().Refresh();
            }
            catch (WebDriverTimeoutException)
            {
                // sometimes we get a timeout error, even though the page has actually loaded
                // ignore this error, and try to proceed anyway
            }
        }
        
        public void RightClick(PageElement pageElement, bool scrollIntoViewBeforeClick = true)
        {
            // Adding a micro sleep to provide a little more robustness before trying to execute a click
            Thread.Sleep(200);

            var webElement = GetElement(pageElement);
            if (scrollIntoViewBeforeClick)
            {
                ScrollIntoView(webElement);
            }
            RightClick(webElement, pageElement.Description);
        }

        /// <summary>
        /// Right click the web element previously found by using a PageElement reference
        /// </summary>
        /// <param name="webElement"></param>
        /// <param name="description"></param>
        public void RightClick(IWebElement webElement, string description)
        {
            try
            {
                var actions = new Actions(_webdriver);
                actions.ContextClick(webElement).Build().Perform();
            }
            catch (Exception e)
            {
                throw new Exception("Attempted to right click the element '" + description + "' but failed", e);
            }
        }
        /// <summary>
        /// Drag and Drop PageElement to target PageElement
        /// </summary>
        /// <param name="elementToDrag"></param>
        /// <param name="targetElement"></param>
        public void DragAndDrop(PageElement elementToDrag, PageElement targetElement)
        {
            var webElementToDrag = GetElement(elementToDrag);
            var targetWebElement = GetElement(targetElement);

            DragAndDrop(webElementToDrag, elementToDrag.Description, targetWebElement, targetElement.Description);
        }

        /// <summary>
        /// Drag and Drop previously found Element to previously found target element.
        /// </summary>
        /// <param name="elementToDrag"></param>
        /// <param name="elementToDragDescription"></param>
        /// <param name="targetElement"></param>
        /// <param name="targetElementDescription"></param>
        public void DragAndDrop(IWebElement elementToDrag, string elementToDragDescription, IWebElement targetElement, string targetElementDescription)
        {
            try
            {
                var actions = new Actions(_webdriver);
                actions.DragAndDrop(elementToDrag, targetElement).Build().Perform();
            }
            catch (Exception e)
            {
                throw new Exception($"Attempted to drag and drop '{elementToDragDescription}' to '{targetElementDescription}' but failed", e);
            }
        }

        public void ScrollIntoView(PageElement pageElement, bool alignToTop = false)
        {
            var webElement = GetElement(pageElement);
            ScrollIntoView(webElement, alignToTop);
        }

        private void ScrollIntoView(IWebElement webElement, bool alignToTop = false)
        {
            try
            {
                ExecuteScript("arguments[0].scrollIntoView(arguments[1]);", webElement, alignToTop);
            }
            catch (Exception e)
            {
                throw new Exception("Tried to scroll to element '" + webElement.Text + "', but got an error:\n" +
                                    e.Message + ", \nStack trace:\n" + e.StackTrace);
            }
        }

        /// <summary>
        /// Select option <paramref name="optionToSelect"/> from the drop down element <paramref name="pageElement"/>
        /// </summary>
        public void Select(PageElement pageElement, string optionToSelect, bool partialMatch = false)
        {
            WaitForAny(pageElement, 30.Seconds());
            var webElement = GetElement(pageElement);

            var selectElement = new SelectElement(webElement);
            Wait.UpTo(10.Seconds()).For(() =>
                new TestResponse(selectElement.Options.Count > 0, $"Expected the Select element '{pageElement.Description}' to have at least 1 option, so i could select '{optionToSelect}' but it had none available"));
            try
            {
                selectElement.SelectByText(optionToSelect, partialMatch);
            }
            catch (Exception e)
            {
                throw new Exception("Tried to select the option '" + optionToSelect + "' from the select element '" +
                                    pageElement.Description + "'.\nBut it failed, current options: " + string.Join(", ",
                                        selectElement.Options.Select(option => option.Text)), e);
            }
        }

        /// <summary>
        /// Clear out a textbox by using Ctrl+A, Delete. Particularly useful for custom editors/inputs
        /// </summary>
        /// <param name="pageElement"></param>
        public void SelectAllThenDelete(PageElement pageElement)
        {
            var webElement = GetElement(pageElement);
            try
            {
                try
                {
                    // some page elements may not be directly clickable, and already have focus, so don't fail on this
                    webElement.Click();
                    Thread.Sleep(500.Milliseconds());
                }
                catch (Exception e)
                {
                    Log($"Tried to click on '{pageElement.Description}' in order to clear it, but the click failed. Continuing.\nException details: {e.Message}");
                }
                var action = new Actions(_webdriver)
                    .KeyDown(Keys.Control)
                    .SendKeys("a")
                    .KeyUp(Keys.Control)
                    .SendKeys(Keys.Delete);
                action.Build().Perform();
                Thread.Sleep(500.Milliseconds());
            }
            catch (Exception e)
            {
                throw new Exception("Tried to remove the text from '" + pageElement.Description + "' using Ctrl+A, Delete but it failed!", e);
            }
        }

        /// <summary>
        /// Send the provided keys to the given element
        /// </summary>
        /// <param name="pageElement"></param>
        /// <param name="text"></param>
        public void SendKeys(PageElement pageElement, string text)
        {
            var webElement = GetElement(pageElement);
            try
            {
                webElement.SendKeys(text);
            }
            catch (Exception e)
            {
                throw new Exception(
                    "Tried to submit text '" + text + "' to element '" + pageElement.Description + "' but got an error",
                    e);
            }
        }

        /// <summary>
        /// Send the provided keys straight to the DOM, not a particular element
        /// </summary>
        /// <param name="text"></param>
        public void SendKeys(string text)
        {
            try
            {
                var actions = new Actions(_webdriver);
                actions.SendKeys(text);
            }
            catch (Exception e)
            {
                throw new Exception("Tried to submit text '" + text + "' but it failed", e);
            }
        }

        public void SendKeysOneCharAtATime(string text)
        {
            try
            {
                var actions = new Actions(_webdriver);
                actions.SendKeys(text).Perform();
            }
            catch (Exception e)
            {
                throw new Exception("Tried to submit text '" + text + "' one char at a time but it failed", e);
            }
        }

        public void SetAttributeValue(PageElement pageElement, string attribute, string value)
        {
            var webElement = GetElement(pageElement);
            ExecuteScript("arguments[0].setAttribute(arguments[1], arguments[2]);", webElement, attribute, value);
        }

        /// <summary>
        /// Set the text attribute of an element directly when SendKeys() doesn't work
        /// </summary>
        /// <param name="pageElement"></param>
        /// <param name="text"></param>
        public void SetText(PageElement pageElement, string text)
        {
            SetAttributeValue(pageElement, "value", text);
        }

        /// <summary>
        /// Wait for the element to be visible, but don't throw exception if it doesn't load
        /// in that time
        /// </summary>
        /// <param name="pageElement"></param>
        /// <param name="timeout"></param>
        public void SilentWaitForVisible(PageElement pageElement, TimeSpan timeout)
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            while (stopWatch.Elapsed < timeout)
            {
                if (IsVisible(pageElement))
                {
                    stopWatch.Stop();
                    return;
                }
                Thread.Sleep(1.Seconds());
            }
            stopWatch.Stop();
        }

        public void SwitchToDefaultFrame()
        {
            _webdriver.SwitchTo().DefaultContent();
        }

        public void SwitchToIFrame(int iFrameIndex)
        {
            var desiredCount = iFrameIndex + 1;
            var iFrameElement = new PageElement("Iframe") { Css = "iframe" };
            
            Wait.UpTo(10.Seconds()).For(() =>
            {
                var iFrameCount = GetElements(iFrameElement).Count;
                return new TestResponse(iFrameCount >= desiredCount,
                        "Failed to wait for enough iFrames, current count '" + iFrameCount + "', required: " + desiredCount);
            });

            _webdriver.SwitchTo().Frame(iFrameIndex);
        }

        public void SwitchToIFrame(PageElement pageElement)
        {
            WaitForAny(pageElement, 30.Seconds());
            var webElement = GetElement(pageElement);
            _webdriver.SwitchTo().Frame(webElement);
        }

        /// <summary>
        /// Wait for the given tab index to exist and then switches to it
        /// </summary>
        /// <param name="tabIndex"></param>
        public void SwitchToTab(int tabIndex)
        {
            var wait = new WebDriverWait(_webdriver, 30.Seconds());
            wait.Until(d => tabIndex + 1 <= d.WindowHandles.Count);
            _webdriver.SwitchTo().Window(_webdriver.WindowHandles[tabIndex]);
        }

        /// <summary>
        /// Open a pop up and perform an action to interrogate the pop up
        /// </summary>
        /// <param name="causePopupAction">The action that causes the pop up to open</param>
        /// <param name="inPopupActivityAction">Any action you'd like to to do within the pop up - for example any further data entry and assertions</param>
        public void TriggerPopupAndPerformActionWithinIt(Action causePopupAction, Action inPopupActivityAction)
        {
            var currentWindowHandle = _webdriver.CurrentWindowHandle;
            var originalWindowHandles = _webdriver.WindowHandles;

            causePopupAction();

            string popupHandle = null;
            Wait.UpTo(5.Seconds()).For(() =>
            {
                var newHandles = _webdriver.WindowHandles.Except(originalWindowHandles).ToList();
                var testResponse = new TestResponse(newHandles.Count > 0, "Was expecting a popup window to load, but it didn't");
                if (testResponse.PassFailResult)
                {
                    popupHandle = newHandles[0];
                }

                return testResponse;
            });

            _webdriver.SwitchTo().Window(popupHandle);

            // Do whatever you need to in the popup browser
            inPopupActivityAction();

            // Close the pop up and go back to where you were
            _webdriver.Close();
            _webdriver.SwitchTo().Window(currentWindowHandle);
        }

        public void Uncheck(PageElement pageElement)
        {
            var webElement = GetElement(pageElement);

            if (webElement.Selected)
            {
                webElement.Click();
            }
        }

        public TestResponse VerifySelectOptionExists(PageElement pageElement, string optionName)
        {
            SilentWaitForVisible(pageElement, 5.Seconds());
            if (Exists(pageElement))
            {
                ScrollIntoView(pageElement);
            }

            if (!IsVisible(pageElement))
            {
                return new TestResponse(false,
                    "Couldn't find the select element '" + pageElement.Description +
                    "', was waiting for it to verify it had the option: " + optionName);
            }
            var dropDownOptions = GetSelectOptions(pageElement);
            return new TestResponse(dropDownOptions.Contains(optionName),
                $"Option '{optionName}' not found in select list '{pageElement.Description}'.\nList contains:\n" +
                (dropDownOptions.Count == 0 ? "n/a" : string.Join(", ", dropDownOptions)));
        }

        /// <summary>
        /// Wait for the element to have one of the desired values in the css style
        /// </summary>
        /// <param name="pageElement"></param>
        /// <param name="styleProperty"></param>
        /// <param name="desiredValues"></param>
        public void WaitForACssStyleValue(PageElement pageElement, string styleProperty,
            params string[] desiredValues)
        {
            var webElement = GetElement(pageElement);
            Wait.UpTo(10.Seconds()).For(() =>
            {
                var cssValue = webElement.GetCssValue(styleProperty);
                return new TestResponse(desiredValues.Any(finalValue => finalValue == cssValue),
                    $"Failed to wait for the '{pageElement.Description}' element to have one of the expected values ({string.Join(", ", desiredValues)}) in the '{styleProperty}' property, but it was: {cssValue}");
            });
        }

        public void WaitForAngularRequestsToStop(PageElement pageElement)
        {
            if (pageElement.Css == null)
            {
                throw new Exception("Unable to wait for angular on '" + pageElement.Description +
                                    "' since it doesn't have a css selector value");
            }
            Wait.UpTo(30.Seconds()).For(() => new TestResponse(DoesElementHaveNoOutstandingAngularRequests(_webdriver, pageElement.Css),
                    "Tried to wait for the element: " + pageElement.Description + "' to have no more angular requests but it was still busy. Consider increasing the timeout or checking network speed."));
        }

        /// <summary>
        /// Wait for any instance of this element to be found
        /// </summary>
        /// <param name="element"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public void WaitForAny(PageElement element, TimeSpan timeout)
        {
            var searchContext = GetElementFinder(element, out _);
            try
            {
                var wait = new WebDriverWait(_webdriver, timeout);
                wait.Until(d => d.FindElements(searchContext).Any());
            }
            catch (Exception e)
            {
                throw new Exception("Waiting for element: " + element.Description + " for " + timeout.TotalSeconds + " seconds, but got an error:\n" +
                                    e.Message + ",\nStack Trace: \n" + e.StackTrace);
            }
        }

        public void WaitForClassNotContains(IWebElement webElement, string nonDesiredClass)
        {
            Wait.UpTo(10.Seconds()).For(() =>
            {
                var currentClass = webElement.GetAttribute("class");
                return new TestResponse(!currentClass.Contains(nonDesiredClass),
                        $"Failed to wait for element to not have the class {nonDesiredClass}. Current class: {currentClass}");
            });
        }

        /// <summary>
        /// Wait for the element to have the desired value in the css style
        /// </summary>
        /// <param name="pageElement"></param>
        /// <param name="styleProperty"></param>
        /// <param name="desiredValue"></param>
        public void WaitForCssStyleValue(PageElement pageElement, string styleProperty, string desiredValue)
        {
            var webElement = GetElement(pageElement);
            Wait.UpTo(10.Seconds()).For(() => new TestResponse(desiredValue, webElement.GetCssValue(styleProperty),
                $"Failed to wait for the '{pageElement.Description}' element to have the expected value in the '{styleProperty}' property"));
        }

        public void WaitForCssStyleValuesNotPresent(PageElement pageElement, string styleProperty,
            params string[] nonDesiredValues)
        {
            var webElement = GetElement(pageElement);

            Wait.UpTo(10.Seconds()).For(() =>
            {
                var cssValue = webElement.GetCssValue(styleProperty);
                return new TestResponse(!nonDesiredValues.Contains(cssValue),
                    $"Waiting for '{pageElement.Description}' to not have any of these values '{string.Join(", ", nonDesiredValues)}' in the style attribute: {styleProperty}.\nBut it had the value: {cssValue}");
            });
        }

        public void WaitForDisabled(PageElement pageElement, TimeSpan timeout)
        {
            WaitForAny(pageElement, timeout);
            var webElement = GetElement(pageElement);
            Wait.UpTo(timeout).For(() =>
                new TestResponse(!webElement.Enabled, $"Failed waiting for '{pageElement.Description}' to be disabled"));
        }

        public void WaitForEnabled(PageElement pageElement, TimeSpan timeout)
        {
            WaitForAny(pageElement, timeout);
            var webElement = GetElement(pageElement);
            Wait.UpTo(timeout).For(() =>
                new TestResponse(webElement.Enabled, $"Failed waiting for '{pageElement.Description}' to be enabled"));
        }

        public void WaitForExists(PageElement pageElement, TimeSpan timeout)
        {
            Wait.UpTo(timeout).For(() => new TestResponse(Exists(pageElement),
                $"'{pageElement.Description}' doesn't exist, waited {timeout.TotalSeconds} seconds"));
        }

        public void WaitForLocatedOnScreen(IWebElement webElement, string elementDescription)
        {
            Wait.UpTo(10.Seconds()).For(() =>
            {
                var xLocation = webElement.Location.X;
                return new TestResponse(xLocation >= 0,
                    $"Tried to wait for the element {elementDescription} to be located on screen but it wasn't.\nCurrent X location: {xLocation}");
            });
        }

        public void WaitForLocatedOnScreen(PageElement pageElement, TimeSpan timeout = default(TimeSpan))
        {
            WaitForAny(pageElement, timeout == default(TimeSpan)? 30.Seconds() : timeout);
            var webElement = GetElement(pageElement);
            WaitForLocatedOnScreen(webElement, pageElement.Description);
        }

        public void WaitForNotVisible(PageElement pageElement, TimeSpan timeout = default(TimeSpan))
        {
            Wait.UpTo(timeout == default(TimeSpan) ? 30.Seconds() : timeout).For(() =>
                new TestResponse(!IsVisible(pageElement),
                    $"Waited for the '{pageElement.Description}' element to disappear but it didn't"));
        }

        public void WaitForPageLoad()
        {
            try
            {
                _webdriver.FindElement(By.TagName("html"));
            }
            catch (Exception e)
            {
                throw new Exception(
                    $"Tried to wait for page to load, but got an error:\n{e.Message}\nStack trace:\n{e.StackTrace}");
            }
        }

        public void WaitForText(PageElement pageElement, string text, TimeSpan timeout)
        {
            WaitForExists(pageElement, timeout);
            var webElement = GetElement(pageElement);
            try
            {
                new WebDriverWait(_webdriver, timeout).Until(d => webElement.Text.ToLower().Contains(text.ToLower()));
            }
            catch (Exception e)
            {
                throw new Exception(
                    $"Tried to wait for the element '{pageElement.Description}' to have the text '{text}'.\nCurrent text: {webElement.Text}",
                    e);
            }
        }

        public void WaitForTitle(String title, TimeSpan timeout)
        {
            var wait = new WebDriverWait(_webdriver, timeout);
            try
            {
                wait.Until(d => d.Title.ToLower().Contains(title.ToLower()));
            }
            catch (Exception e)
            {
                throw new Exception(
                    $"Tried to wait for the page title {title} for {timeout.TotalSeconds} seconds.\nCurrent title: {_webdriver.Title}",
                    e);
            }
        }

        public void WaitForUrl(string partialUrl, TimeSpan timeout)
        {
            var wait = new WebDriverWait(_webdriver, timeout);
            try
            {
                wait.Until(d => d.Url.ToLower().Contains(partialUrl.ToLower()));
            }
            catch (Exception e)
            {
                // if there is a webdriver timeout error, but the url is correct, chances are
                // the page has actually loaded fine, so it's worth trying to continue
                if (e.Message.Contains("The HTTP request to the remote WebDriver serve") &&
                    GetUrl().Contains(partialUrl))
                {
                    return;
                }

                throw new Exception(
                    $"Tried to wait for the url '{partialUrl}' for {timeout.TotalSeconds} seconds.\nCurrent url: {_webdriver.Url}",
                    e);
            }
        }

        public void WaitForVisible(PageElement pageElement, TimeSpan timeout)
        {
            Wait.UpTo(timeout).For(() => new TestResponse(IsVisible(pageElement),
                $"Failed to wait for the element '{pageElement.Description}' to be visible"));
        }

        public void WaitWhileEnsuringNotVisible(PageElement pageElement, TimeSpan timeout = default(TimeSpan))
        {
            Wait.UpTo(timeout == default(TimeSpan) ? 30.Seconds() : timeout).WhileEnsuring(() =>
                new TestResponse(!IsVisible(pageElement),
                    $"Expected '{pageElement.Description}' to remain not visible, but it became visible"));
        }
    }
}
