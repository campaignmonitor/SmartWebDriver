using NUnit.Framework;
using SmartWebDriver;

namespace Sandbox
{
    [TestFixture]
    public class SandboxTests
    {
        private WebBrowser _browser;

        [TearDown]
        public void TestCleanup()
        {
            if (_browser != null)
            {
                _browser.Quit();
            }
        }

        [Test]
        public void SampleTest()
        {
            _browser = new WebBrowser(new BrowserOptions(Browsers.Firefox));
            _browser.NavigateTo("http://www.google.com");

        }
    }
}
