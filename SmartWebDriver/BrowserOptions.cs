namespace SmartWebDriver
{
    public class BrowserOptions
    {
        public bool RunInIncognito;
        public bool AllowInsecureContent;
        public Browsers BrowserType;

        public BrowserOptions()
        {
            BrowserType = Browsers.Chrome;
        }
        public BrowserOptions(Browsers browserType)
        {
            BrowserType = browserType;
        }
    }
}
