using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace a24Bot
{
    public partial class Form1 : Form
    {
        IWebDriver Browser;
        public Form1()
        {
            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false;
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            LogAdd("Программа запущена");
            //Task maintask = new Task(Main);
            //maintask.Start();
            Main();
        }
        void Main()
        {
            if(Auth() == false)
            {
                LogAdd("Логин или пароль указаны не верно");
                return;
            }
            try
            {
                Browser.Navigate().GoToUrl("https://a24.biz/order/search");
            }
            catch { }
            Task.Delay(2000).Wait();
            int page = 1;
            IWebElement table1;
            try
            {
                table1 = FindElement(By.CssSelector(".sc-dRCTWM.bpVoIf"));
            }
            catch { Task.Delay(3000).Wait(); }
            IWebElement table = FindElement(By.CssSelector(".sc-dRCTWM.bpVoIf"));
            C: List<IWebElement> children = table.FindElements(By.XPath("./*")).ToList();
            int not = 0;
            for (int i = 0; i < children.Count; i++)
            {
                if (children[i].GetAttribute("class") == "orderA__order")
                {//
                    string title = children[i].FindElement(By.CssSelector(".orderA__nameHolder")).Text;
                    IWebElement btn = children[i].FindElement(By.CssSelector(".orderA__actions button"));
                    btn.Click();
                    Q: Task.Delay(3000).Wait();
                    try
                    {
                        children[i].FindElement(By.CssSelector("[class='sc-kEmuub eQFKbU sc-jzJRlG jbrkUl']")).Click();
                    }
                    catch { children[i].FindElement(By.CssSelector(".orderA__actions button")).Click(); goto Q; }

                    if (costNum.Value != 0)
                    {
                        try
                        {
                            Browser.FindElements(By.CssSelector(".ui-modal-body .sc-htpNat.gMCOQB"))[(i - not) * 2].SendKeys(costNum.Value.ToString());
                        }
                        catch { goto C; }
                    }
                    Browser.FindElements(By.CssSelector(".ui-modal-body textarea"))[i-not].SendKeys(patternBox.Text.Replace("{title}", title));
                    //
                    Browser.FindElements(By.CssSelector(".a24-makeOffer__group .sc-jzJRlG.jbrkUl"))[i-not].Click();
                    Task.Delay(3000).Wait();
                    if (Browser.FindElements(By.CssSelector(".ui-message-notice-content")).Count > 0)
                    {
                        Browser.FindElements(By.CssSelector(".sc-jzJRlG.iFRflT"))[i - not].Click();
                        Task.Delay(3000).Wait();
                    }
                }
                else if (children[i].GetAttribute("class") == "auctionnotify__notify auctionnotify__notify--warn")
                {
                    goto END;
                }
                else not++;
            }

            List<IWebElement> pages = FindElements(By.CssSelector(".auctionNumericPagination__numeric__list a")).ToList();
            for (int i = 0; i < pages.Count; i++)
            {
                if (pages[i].Text == (page+1).ToString())
                {
                    page++;
                    try
                    {
                        pages[i].Click();
                        Task.Delay(2000).Wait();
                        new WebDriverWait(Browser, TimeSpan.FromMinutes(8)).Until(
    d => ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState").Equals("complete"));
                    }
                    catch { }
                    Task.Delay(10000).Wait();
                    goto C;
                }
            }
            
            END:  LogAdd("Готово");
        }//
        bool Auth()
        {
            StartBrowser("https://a24.biz/", false);
            F: try
            {
                List<IWebElement> fields = FindElements(By.CssSelector(".form__input")).ToList();
                fields[0].SendKeys(loginBox.Text);
                fields[1].SendKeys(passBox.Text);
            }
            catch { Task.Delay(6000).Wait(); goto F; }
            Browser.FindElement(By.CssSelector(".form__button")).Click();
            Task.Delay(2000).Wait();
            List<IWebElement> check;
            while (true)
            {
                try
                {
                    check = FindElements(By.CssSelector(".userInfo__main-avatar"));
                    break;
                }
                catch { 
                }Task.Delay(5000).Wait();
            }
            List<IWebElement> error = FindElements(By.CssSelector(".form__button"));
            while (true)
            {
                Task.Delay(2500).Wait();
                check = FindElements(By.CssSelector(".userInfo__main-avatar")).ToList();
                error = FindElements(By.CssSelector(".form__button")).ToList();
                if (check.Count > 0)
                    return true;
                else if (error.Count > 0)
                {
                    List<IWebElement> fields = FindElements(By.CssSelector(".form__input")).ToList();
                    fields[0].SendKeys(loginBox.Text);
                    fields[1].SendKeys(passBox.Text);
                    try
                    {
                        FindElement(By.CssSelector(".form__button")).Click();
                    }
                    catch { return true; }
                    Task.Delay(2000).Wait();
                    while (true)
                    {
                        try
                        {
                            check = FindElements(By.CssSelector(".userInfo__main-avatar"));
                            break;
                        }
                        catch
                        {
                            Task.Delay(5000).Wait();
                        }
                    }
                    check = FindElements(By.CssSelector(".userInfo__main-avatar")).ToList();
                    error = FindElements(By.CssSelector(".form__button")).ToList();
                    if (check.Count > 0)
                        return true;
                    return false;
                }
            }
        }
        public IWebElement FindElement(By by)
        {
            if (300 > 0)
            {
                //var wait = new OpenQA.Selenium.Support.UI.WebDriverWait(Browser, TimeSpan.FromSeconds(300));
                while (true)
                {
                    try
                    {
                        
                        var l = Browser.FindElement(by);
                        return l;
                    }
                    catch
                    {
                        Task.Delay(5000).Wait();
                    }
                }
            }
            return Browser.FindElement(by);
        }
        public List<IWebElement> FindElements(By by)
        {
            if (300 > 0)
            {
                while (true)
                {
                    try
                    {
                        //var wait = new WebDriverWait(Browser, TimeSpan.FromSeconds(300));
                        //var l = wait.Until(drv => (drv.FindElements(by).Count > 0) ? drv.FindElements(by) : null).ToList();
                        var l = Browser.FindElements(by).ToList();
                        return l;
                    }
                    catch
                    {
                        Task.Delay(5000).Wait();
                    }
                }
            }
            return Browser.FindElements(by).ToList();
        }
        void Wait(Action action)
        {
            while (true)
            {
                try
                {
                    action.Invoke();
                    break;
                }
                catch { }
            }
        }
        void StartBrowser(string url, bool IsHead = true)
        {
            ChromeOptions options = new ChromeOptions();
            if (IsHead)
            {
                options.AddArgument("--headless");
            }
            Browser = new OpenQA.Selenium.Chrome.ChromeDriver(options);
            Browser.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(8);
            Browser.Manage().Window.Maximize();
            try
            {
                Browser.Navigate().GoToUrl(url);
            }catch
            { }
        }

        void LogAdd(string text)
        {
            LogBox.AppendText(DateTime.Now + " " + text + Environment.NewLine);
        }
    }
}
