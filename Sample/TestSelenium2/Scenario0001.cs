using System;
using System.Configuration;
using System.Threading;
using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.IE;

namespace TestSelenium2
{
    [TestFixture]
    public class Scenario0001
    {
        private IWebDriver _driver;

        #region Configure Test Environment

        [SetUp]

        public void Setup()
        {
            _driver = new InternetExplorerDriver();
            _driver.Manage().Timeouts().ImplicitlyWait(new TimeSpan(0, 0, 30));
        }

        #endregion Configure Test Environment

        #region Implement Testcase

        [Test]

        public void TC001_TestSearchGoogleForTheAutomatedTester()
        {
            //Navigate to the site
            var url = ConfigurationManager.AppSettings["BasedUrl"];

            _driver.Navigate().GoToUrl(url);

            //Find the Element and create an object so we can use it

            var queryBox = _driver.FindElement(By.Name("q"));

            //Work with the Element that's on the page

            queryBox.SendKeys("The Automated Tester");

            queryBox.SendKeys(Keys.ArrowDown);

            queryBox.Submit();
            Thread.Sleep(3000);

            //Check that the Title is what we are expecting
            Console.WriteLine(_driver.Title);
            var title = _driver.Title;

            var no = _driver.Title.IndexOf(title);
            Assert.True(no > -1);
        }

        //[Test]
        //public void TC002_TestLivingCountrySite()
        //{
        //    //Navigate to the site
        //    string url = ConfigurationSettings.AppSettings["BasedUrl2"];

        //    driver.Navigate().GoToUrl(url);

        //    //Find the Element and create an object so we can use it
        //    IWebElement queryBox = driver.FindElement(By.Name("search_term"));

        //    queryBox.Click();
        //    queryBox.SendKeys("christmas");

        //    queryBox.SendKeys(Keys.ArrowDown);

        //    queryBox.Submit();
        //    Thread.Sleep(3000);
        //}

        #endregion Implement Testcase

        # region Create Additional Functions

        #endregion Create Additional Functions

        #region TearDown

        [TearDown]

        public void Teardown()
        {
            if (_driver != null) _driver.Close();
            //driver.Quit();
        }

        #endregion TearDown
    }
}