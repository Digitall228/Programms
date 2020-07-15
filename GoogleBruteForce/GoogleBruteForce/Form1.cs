using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GoogleBruteForce
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            Task mainTask = new Task(Main);
            mainTask.Start();
        }
        void Main()
        {
            IWebDriver Browser = StartBrowser();
            Browser.Navigate().GoToUrl("https://accounts.google.com/signin/v2/identifier?service=accountsettings&continue=https%3A%2F%2Fmyaccount.google.com%2F%3Futm_source%3Dsign_in_no_continue%26pli%3D1&csig=AF-SEnbsB19nRsk4eV0Y%3A1584895481&flowName=GlifWebSignIn&flowEntry=AddSession");
            //
            IWebElement emailBox = Browser.FindElement(By.CssSelector("#identifierId"));
            emailBox.SendKeys("sashakrasava13arsenal@gmail.com");

            IWebElement nextBTN = Browser.FindElement(By.CssSelector("#identifierNext"));
            nextBTN.Click();

            IWebElement passwordBox = Browser.FindElement(By.CssSelector("[class='whsOnd zHQkBf']"));
            passwordBox.SendKeys("Sasna2008fofi");
        }
        IWebDriver StartBrowser()
        {
            try
            {
                var driverService = ChromeDriverService.CreateDefaultService();
                driverService.HideCommandPromptWindow = true;
                ChromeOptions options = new ChromeOptions();
                options.AddExtensions("3.22.1_0.crx");
                //if (!checkBox1.Checked)
                    //options.AddArgument("--headless");
                IWebDriver Browsers = new OpenQA.Selenium.Chrome.ChromeDriver(options);
                Browsers.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(8);
                Browsers.Manage().Window.Maximize();
                return Browsers;
            }
            catch (Exception ex)
            {
                //LogAdd("ERROR: " + ex.Message + Environment.NewLine + "STACKTRACE: " + ex.StackTrace);
                return null;
            }
        }
    }
}
