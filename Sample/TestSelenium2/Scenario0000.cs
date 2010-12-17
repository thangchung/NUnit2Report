using System;
using System.Configuration;
using System.Diagnostics;
using System.Text;
using System.Threading;
using NUnit.Framework;
using Selenium;

namespace TestSelenium2
{
    [TestFixture]
    public class Scenario0000
    {
        #region Configure Test Environment

        private ISelenium _selenium;
        private Process _seleniumServer;

        private StringBuilder _verificationErrors;

        public void SetUp()
        {
            if (_seleniumServer != null)
                _seleniumServer.Kill();

            var startInfo = new ProcessStartInfo("java.exe", "-jar selenium-server.jar -interactive")
                                {
                                    WorkingDirectory = ConfigurationManager.AppSettings["SeleniumServer"]
                                };
            _seleniumServer = Process.Start(startInfo);
            Thread.Sleep(5000);
        }

        [TestFixtureSetUp]
        public void SetupTest()
        {
            SetUp();

            var host = ConfigurationManager.AppSettings["Host"];
            var port = ConfigurationManager.AppSettings["Port"];
            var browser = ConfigurationManager.AppSettings["Browser"];
            var url = ConfigurationManager.AppSettings["BasedUrl2"];

            _selenium = new DefaultSelenium(host, int.Parse(port), browser, url);
            _selenium.Start();
            _selenium.SetSpeed("5000");
            _verificationErrors = new StringBuilder();
        }

        #endregion Configure Test Environment

        #region Implement Testcase

        [Test]
        public void TC001_VerifyDatainCountryLivingSite()
        {
            string error = "";
            try
            {
                OpenNewBrowser();
                //check menu decorating & home improvement
                _selenium.IsElementPresent("//div[@id='m_cat_tn2']/a/img");
                _selenium.Click("//div[@id='m_cat_tn2']/a/img");

                Thread.Sleep(5000);
                Assert.IsTrue(_selenium.IsTextPresent("More Decorating and Home Improvement Articles"));
            }
            catch (AssertionException e)
            {
                error = e.ToString();
                Console.WriteLine("Test case T001 failed" + error);
                Console.WriteLine("---------------------------------------------------------------------------------------");
            }
            Assert.AreEqual("", error);
        }

        #endregion Implement Testcase

        #region Create Additional Functions

        public void OpenNewBrowser()
        {
            _selenium.Open("/");
            _selenium.WindowMaximize();
            Thread.Sleep(5000);
        }

        #endregion Create Additional Functions

        #region TearDown

        public void TearDown()
        {
            if (_seleniumServer != null)
                _seleniumServer.Kill();
        }

        [TestFixtureTearDown]
        public void TeardownTest()
        {
            try
            {
                _selenium.Stop();
                TearDown();
            }
            catch (Exception)
            {
                Console.WriteLine(_verificationErrors.ToString());
            }
            Assert.AreEqual("", _verificationErrors.ToString());
        }

        #endregion TearDown
    }
}