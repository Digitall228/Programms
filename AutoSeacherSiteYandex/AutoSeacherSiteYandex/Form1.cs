using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

namespace AutoSeacherSiteYandex
{
    public partial class Form1 : Form
    {
        IWebDriver Browser;
        List<string> path = new List<string>();
        string pathToRequests = "";
        Thread mainTask;
        public Form1()
        {
            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false;
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            if(openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                pathToRequests = openFileDialog1.FileName;
            }
        }

        private void Button3_Click(object sender, EventArgs e)
        {
            try
            {
                mainTask.Resume();
            }
            catch { }
            mainTask.Abort();
            Browser.Quit();
            LogAdd("Программа остановлена");
        }

        private void Button2_Click(object sender, EventArgs e)
        {
            LogAdd("Программа запущена");
            mainTask = new Thread(Main);
            mainTask.Start();
        }
        void Main()
        {
            try
            {
                List<Request> requests = new List<Request>();
                string[] reqs = GetRequests(pathToRequests).ToArray();
                for (int z = 0; z < path.Count; z++)
                {
                    Request req = new Request(dataGridView1.Rows[z].Cells[0].Value.ToString(), dataGridView1.Rows[z].Cells[1].Value.ToString(), int.Parse(dataGridView1.Rows[z].Cells[2].Value.ToString()));
                    requests.Add(req);
                }
                
                for (int q = 0; q < reqs.Length; q++)
                {
                    Browser = GetBrowser();
                    //Thread.Sleep(10000);
                    Browser.Navigate().GoToUrl("https://yandex.ru/tune/geo/?retpath=https%3A%2F%2Fyandex.ru%2F%3Fdomredir%3D1&nosync=1");
                    ChangeRegion();

                    if (Browser.Url != "https://yandex.ru/")
                        Browser.Navigate().GoToUrl("https://yandex.ru/");
                    Thread.Sleep(1000);
                    Browser.FindElement(By.CssSelector(".input__control.input__input")).SendKeys(reqs[q] + OpenQA.Selenium.Keys.Enter);
                    LogAdd("Запрос введен.");
                    Thread.Sleep(1500);
                    string url = Browser.Url;
                    for (int a = 0; a < requests.Count; a++)
                    {
                        LogAdd("Ищем сайт");
                        IWebElement site = null;
                        Thread.Sleep(1500);
                        if (a != 0)
                        Browser.Navigate().GoToUrl(url + "&p=" + 0);
                        for (int i = 1; (site = SearchSite(requests[a].url)) == null && i <= 25; i++)
                        {
                            Thread.Sleep(400);
                            Browser.Navigate().GoToUrl(url + "&p=" + i);
                            Thread.Sleep(300);
                        }
                        if (site == null)
                        {
                            LogAdd("Сайт " + requests[a].url + " не найден");
                            continue;
                        }
                        LogAdd("Сайт найден");
                        site.Click();
                        StartFile(requests[a]);
                        LogAdd($"Ожидаем {requests[a].delayTime} миллисекунд");
                        Thread.Sleep(requests[a].delayTime);
                        Browser.SwitchTo().Window(Browser.WindowHandles[1]);
                        Browser.Close();
                        Browser.SwitchTo().Window(Browser.WindowHandles[0]);
                    }
                    Browser.Quit();
                }
                LogAdd("Программа завершила работу");
            }
            catch(Exception ex) { LogAdd("ERROR: " + ex.Message + Environment.NewLine + "STACKTRACE: " + ex.StackTrace); }
        }
        IEnumerable<string> GetRequests(string path)
        {
            using (StreamReader sr = new StreamReader(path))
            {
                string l = "";
                while((l = sr.ReadLine()) != null)
                {
                    yield return l;
                }
            }
        }
        IWebElement SearchSite(string site)
        {
            IWebElement[] links = Browser.FindElements(By.CssSelector(".link.link_theme_normal.organic__url.link_cropped_no.i-bem")).ToArray();
            int y = 0;
            for (int i = 0; i < links.Length; i++)
            {
                for (int x = 1; x < 3; x++)
                {
                    ((IJavaScriptExecutor)Browser).ExecuteScript($"scroll(0, {x*10*(i+1)+y});");
                    Thread.Sleep(50);
                }
                y += 2 * 10 * (i + 1);
                if (links[i].GetAttribute("href").Contains(site))
                {
                    Thread.Sleep(700);
                    return links[i];
                }
                Thread.Sleep(500);
            }
            return null;
        }
        void ChangeRegion()
        {
            IWebElement input = Browser.FindElement(By.CssSelector(".input__box input"));
            input.Clear();
            input.SendKeys("Москва");
            Browser.FindElement(By.CssSelector(".popup__content li")).Click();
            LogAdd("Регион сменен");
        }
        IWebDriver GetBrowser()
        {
            var driverService = ChromeDriverService.CreateDefaultService();
            ChromeOptions options = new ChromeOptions();
            //driverService.HideCommandPromptWindow = true;
            if (!checkBox1.Checked)
            {
                options.AddArgument("--headless");
            }
            options.AddArgument("--incognito");
            //options.AddExtension("3.22.1_0.crx");
            IWebDriver Browser = new OpenQA.Selenium.Chrome.ChromeDriver(driverService, options);
            Browser.Manage().Window.Maximize();
            Browser.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(18);
            return Browser;
        }
        void StartFile(Request req)
        {
            try
            {
                Process.Start(req.pathToFile);
                LogAdd("Файл запущен");
            }
            catch(Exception ex) { LogAdd("FILE_ERROR: " + ex.Message + Environment.NewLine + "STACKTRACE: " + ex.StackTrace); }
        }
        void LogAdd(string text)
        {
            logBox.AppendText($"{DateTime.Now} {text} {Environment.NewLine}");
        }

        private void Button4_Click(object sender, EventArgs e)
        {
            if(openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                path.Add(openFileDialog1.FileName);
                LogAdd("Файл " + openFileDialog1.FileName + " добавлен");
                dataGridView1.Rows.Add(openFileDialog1.FileName);
            }
        }

        private void Button5_Click(object sender, EventArgs e)
        {
            mainTask.Suspend();
        }

        private void Button6_Click(object sender, EventArgs e)
        {
            mainTask.Resume();
        }
    }
    public class Request
    {
        public string pathToFile = "";
        public string url = "";
        public int delayTime = 0;

        public Request(string PathToFile, string Url, int DelayTime)
        {
            pathToFile = PathToFile;
            url = Url;
            delayTime = DelayTime;
        }
    }
}
