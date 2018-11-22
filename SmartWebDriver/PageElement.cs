using System;

namespace SmartWebDriver
{
    public class PageElement
    {
        public PageElement(string description)
        {
            Description = description;
        }

        public string Description;
        private ElementLocators _locatorUsed = ElementLocators.None;
        public ElementLocators LocatorUsed
        {
            get { return _locatorUsed; }
            set
            {
                // make sure we aren't trying to set the locator more than once
                if (_locatorUsed == ElementLocators.None)
                {
                    _locatorUsed = value;
                }
                else
                {
                    throw new Exception("You can only set 1 locator for a page element");
                }
            }
        }

        private string _id;
        private string _name;
        private string _linkInnerText;
        private string _href;
        private string _class;
        private string _tag;
        private string _css;
        private string _xPath;

        public string ID
        {
            get { return _id; }
            set
            {
                LocatorUsed = ElementLocators.ID;
                _id = value;
            }
        }

        public string Name
        {
            get { return _name; }
            set
            {
                LocatorUsed = ElementLocators.Name;
                _name = value;
            }
        }
        public string LinkInnerText
        {
            get { return _linkInnerText; }
            set
            {
                LocatorUsed = ElementLocators.LinkInnerText;
                _linkInnerText = value;
            }
        }
        public string Href
        {
            get { return _href; }
            set
            {
                LocatorUsed = ElementLocators.Href;
                _href = value;
            }
        }
        public string Class
        {
            get { return _class; }
            set
            {
                LocatorUsed = ElementLocators.Class;
                _class = value;
            }
        }

        public string Tag
        {
            get { return _tag; }
            set
            {
                LocatorUsed = ElementLocators.Tag;
                _tag = value;
            }
        }
        public string Css
        {
            get { return _css; }
            set
            {
                LocatorUsed = ElementLocators.Css;
                _css = value;
            }
        }
        public string XPath
        {
            get { return _xPath; }
            set
            {
                LocatorUsed = ElementLocators.XPath;
                _xPath = value;
            }
        }
    }
}
