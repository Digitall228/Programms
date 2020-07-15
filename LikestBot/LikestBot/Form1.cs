using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using OpenQA.Selenium;
using Leaf.xNet;
using OpenQA.Selenium.Chrome;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using CptchCaptchaSolving;

namespace LikestBot
{
    public partial class Form1 : Form
    {
        string pathToAccounts = "";
        List<Account> accounts;
        bool captcha = true;
        object locker = new object();
        object lockerStream = new object();
        Random rnd = new Random();
        public Form1()
        {
            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false;
        }

        private void Button2_Click(object sender, EventArgs e)
        {
            Task factory = new Task(ThreadFactory);
            factory.Start();
        }
        void ThreadFactory()
        {
            int threadCount = (int)threadCountNum.Value;
            Task[] tasks = new Task[threadCount];
            accounts = Accounts(pathToAccounts).ToList();
            for (int i = 0; i < tasks.Length; i++)
            {
                IEnumerable<Account> accs = accounts.Skip(i * (accounts.Count() / threadCount)).Take(accounts.Count() / threadCount);
                tasks[i] = new Task(Main);
                tasks[i].Start();
                Task.Delay(rnd.Next((int)minThreadTimeOut.Value, (int)maxThreadTimeOut.Value)).Wait();
            }
            Task.WaitAll(tasks);
            LogAdd("Программа завершила работу");
        }
        void Main()
        {
            Account account = accounts.SkipWhile(x => x.status != Status.IN_QUEUE).First();
            if (account == null)
            {
                LogAdd("Аккаунты закончились, поток закончил работу");
                return;
            }
            account.status = Status.IN_PROCESS;
            account.Logging += LogAdd;
            account.captchaSolver = CaptchaSolver;
            IWebDriver Browser = StartBrowser();
            Browser.Manage().Cookies.DeleteAllCookies();
            Browser.Url = "http://samy.pl/evercookie/";
            ReadOnlyCollection<Cookie> cookies = account.Cookie;

            foreach (var cookie in cookies)
            {
                Browser.Manage().Cookies.AddCookie(cookie);
            }

            if (cookies.Count == 0)
            {
                if (!Auth(account, ref Browser))
                {
                    account.status = Status.ACCESS_DENIED;
                    return;
                }
            }

            Browser.Navigate().GoToUrl("https://likest.ru/");
            Task.Delay(500).Wait();
            if (Browser.Url != "https://likest.ru/earn")
            {
                if (!Auth(account, ref Browser))
                {
                    account.status = Status.ACCESS_DENIED;
                    return;
                }
            }
            try
            {
                BuyCoupon(account, Browser);

                StartLikingTask(account, Browser);
                StartFriendsTask(account, Browser);
                Task.Delay(360000).Wait();
                Browser.Navigate().Refresh();
                string balance = Browser.FindElement(By.CssSelector("#user-balance")).Text;
                account.earned += int.Parse(balance) - int.Parse(account.balance);
                account.balance = balance;

                BuyCoupon(account, Browser);
            }
            catch(Exception ex) { LogAdd("Something gone wrong " + account.login + Environment.NewLine + "MESSAGE: " + ex.Message + Environment.NewLine + "STACKTRACE: " + Environment.NewLine); }
            account.Logging -= LogAdd;
            account.captchaSolver -= CaptchaSolver;
            account.status = Status.COMPLETED;
            Browser.Quit();
        }
        void StartFriendsTask(Account account, IWebDriver Browser)
        {
            Browser.Navigate().GoToUrl("https://likest.ru/friends");
            IWebElement[] Tasks = Browser.FindElements(By.CssSelector(".friend-container a")).ToArray();
            while (Tasks.Length > 0)
            {
                Tasks = Browser.FindElements(By.CssSelector(".friend-container a")).ToArray();
                Tasks[0].Click();
                Browser.SwitchTo().Window(Browser.WindowHandles[1]);
                Regex regex = new Regex(@"https://vk.com/(.*)([/]*)");
                string name = regex.Match(Browser.Url).Groups[1].Value;
                account.AddFriend(name, ref locker);
                Browser.Close();
                Browser.SwitchTo().Window(Browser.WindowHandles[0]);
                Task.Delay(rnd.Next((int)minTimeOut.Value, (int)maxTimeOut.Value)).Wait();
                string balance = Browser.FindElement(By.CssSelector("#user-balance")).Text;

                account.earned += int.Parse(balance)-int.Parse(account.balance);
                account.balance = balance;
            }

        }
        void StartLikingTask(Account account, IWebDriver Browser)
        {
            while (true)
            {
                Browser.Navigate().GoToUrl("https://likest.ru/like.php");
                if (Browser.Url.Contains("https://likest.ru/like.php"))
                {
                    return;
                }
                string url = Browser.Url;
                string type = "post";
                string owner = "";
                string item = "";
                Regex regex;

                if (url.Contains("photo"))
                {
                    type = "photo";
                    regex = new Regex(@"photo([-]*\d*)_(\d*)");
                    owner = regex.Match(url).Groups[1].Value;
                    item = regex.Match(url).Groups[2].Value;
                }
                else
                {
                    regex = new Regex(@"wall([-]*\d*)_(\d*)");
                    owner = regex.Match(url).Groups[1].Value;
                    item = regex.Match(url).Groups[2].Value;
                }
                account.AddLike(type, owner, item, ref locker);
                Task.Delay(rnd.Next((int)minTimeOut.Value, (int)maxTimeOut.Value)).Wait();
                account.balance = (int.Parse(account.balance) + 1).ToString();
                account.earned++;
            }
        }

        bool Auth(Account account, ref IWebDriver Browser)
        {//
            Browser.Navigate().GoToUrl("https://likest.ru/");
            Browser.FindElement(By.CssSelector("#ulogin-button")).Click();
            Browser.SwitchTo().Window(Browser.WindowHandles[1]);

            Browser.FindElements(By.CssSelector(".oauth_form_input.dark"))[0].SendKeys(account.login);
            Browser.FindElements(By.CssSelector(".oauth_form_input.dark"))[1].SendKeys(account.password);
            Browser.FindElement(By.CssSelector("#install_allow")).Click();
            Task.Delay(3000).Wait();
            IWebElement[] Allow = Browser.FindElements(By.CssSelector(".flat_button.fl_r.button_indent")).ToArray();
            if(Allow.Length > 0)
            {
                Allow[0].Click();
            }
            Browser.SwitchTo().Window(Browser.WindowHandles[0]);
            for (int i = 0; i < 6; i++)
            {
                if (Browser.WindowHandles.Count == 1)
                {
                    ReadOnlyCollection<OpenQA.Selenium.Cookie> cookies = Browser.Manage().Cookies.AllCookies;
                    account.SetCookie(cookies);
                    return true;
                }
                Task.Delay(3000).Wait();
            }
            return false;
        }
        IWebDriver StartBrowser()
        {
            try
            {
                var driverService = ChromeDriverService.CreateDefaultService();
                driverService.HideCommandPromptWindow = true;
                ChromeOptions options = new ChromeOptions();
                options.AddExtensions("3.22.1_0.crx");
                if (!checkBox1.Checked)
                    options.AddArgument("--headless");
                IWebDriver Browsers = new OpenQA.Selenium.Chrome.ChromeDriver(options);
                Browsers.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(8);
                Browsers.Manage().Window.Maximize();
                return Browsers;
            }
            catch (Exception ex)
            {
                LogAdd(ex.ToString());
                return null;
            }
        }
        private void Button1_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                LogAdd("Это правильный путь? " + Environment.NewLine + openFileDialog1.FileName);
                pathToAccounts = openFileDialog1.FileName;
            }
        }
        public void LogAdd(string text)
        {
            logBox.AppendText($"{DateTime.Now} {text} {Environment.NewLine}");
        }
        public IEnumerable<Account> Accounts(string path)
        {
            using (StreamReader sr = new StreamReader(path))
            {
                string p = "";
                Account acc;
                while ((p = sr.ReadLine()) != null)
                {
                    string[] data = p.Split(':');
                    acc = new Account(data[0], data[1], data[2], dataGridView1);
                    yield return acc;
                }
            }
        }
        string CaptchaSolver(string response)
        {
            Regex regex = new Regex(@"""captcha_sid"":""(\d*)""");
            string sid = regex.Match(response).Groups[1].Value;
            //regex = new Regex(@"""captcha_img"":""(\S*)""");
            //string url = regex.Match(response).Groups[1].Value;
            string captchaParametr = "";

            HttpRequest request = new HttpRequest();
            request.UserAgentRandomize();
            //request.Proxy = ProxyClient.Parse(ProxyType.Socks5, "91.188.242.95:9918:ZwEfgS:DDBbkb");

            if (apiKeyBox.Text == String.Empty)
            {
                HttpResponse img;
                while (true)
                {
                    try
                    {
                        img = request.Get($"https://api.vk.com///captcha.php?sid={sid}&s=1");
                        break;
                    }
                    catch (Exception ex)
                    {
                        if (ex.Message.Contains("Have exceeded the maximum number of consecutive redirects"))
                        {
                            return null;
                        }
                        Task.Delay(2000).Wait();
                    }
                }

                var stream = img.ToMemoryStream();
                pictureBox1.Image = System.Drawing.Image.FromStream(stream);
                captcha = true;
                while (captcha)
                {
                    Task.Delay(1000).Wait();
                }

                captcha = true;
                captchaParametr = $"&captcha_key={captchaBox.Text}&captcha_sid={sid}";
                captchaBox.Text = "";
                pictureBox1.Image = null;
            }
            else
            {
                ZERO:
                CptchCaptchaSolver solver = new CptchCaptchaSolver(apiKeyBox.Text);
                string result = solver.Solve($"https://api.vk.com///captcha.php?sid={sid}&s=1");
                if (result.Contains("error"))
                {
                    LogAdd(result);
                    goto ZERO;
                }
                captchaParametr = $"&captcha_key={result}&captcha_sid={sid}";
            }
            return captchaParametr;
        }
        void BuyCoupon(Account account, IWebDriver Browser)
        {
            string balance = Browser.FindElement(By.CssSelector("#user-balance")).Text;
            account.balance = balance;
            if (int.Parse(balance) > 9)
            {
                Browser.Navigate().GoToUrl("https://likest.ru/coupons/add");
                Browser.FindElement(By.CssSelector("#edit-submit")).Click();
                account.couponBalance = (int.Parse(account.balance) + int.Parse(account.couponBalance)).ToString();
                account.balance = "0";
                Browser.Navigate().GoToUrl("https://likest.ru/coupons/list");
                string coupon = Browser.FindElements(By.CssSelector("#coupon-container-wrapper")).Last().FindElements(By.CssSelector("td"))[2].Text;
                lock (lockerStream)
                    using (StreamWriter sw = new StreamWriter("coupons.txt", true))
                    {
                        sw.WriteLine(coupon);
                    }
            }
        }
        private void CaptchaBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == System.Windows.Forms.Keys.Enter)
            {
                captcha = false;
            }
        }
    }

    public class Account
    {
        private HttpRequest request;
        public string login { get; private set; }
        public string password { get; private set; }
        public string token { get; private set; }
        public int likes { get; private set; } = 0;
        public int friends { get; private set; } = 0;
        private int _earned = 0;
        public int earned
        {
            get { return _earned; }
            set { _earned = value; ChangeColumnData(); }
        }
        private string _balance = "0";
        public string balance
        {
            get { return _balance; }
            set { _balance = value; ChangeColumnData();}
        }
        private string _couponBalance = "0";
        public string couponBalance
        {
            get { return _couponBalance; }
            set { _couponBalance = value; ChangeColumnData(); }
        }
        public DataGridViewRow row { get; private set; }
        private Status _status = Status.IN_QUEUE;
        public ReadOnlyCollection<OpenQA.Selenium.Cookie> Cookie = null;
        public Status status
        {
            get { return _status; }
            set { _status = value; ChangeColumnData(); }
        }
        public delegate void AccountEvents(string message);
        public delegate string Captcha(string response);
        public Captcha captchaSolver;
        public event AccountEvents Logging;
        public Account(string Login, string Password, string Token, DataGridView dgv)
        {
            login = Login;
            password = Password;
            token = Token;
            int index = dgv.Rows.Count - 1;
            dgv.Rows.Add();
            row = dgv.Rows[index];
            GetCookie();
            ChangeColumnData();

            request = new HttpRequest();
            request.UserAgentRandomize();
            request.Proxy = ProxyClient.Parse(ProxyType.Socks5, "91.188.242.95:9918:ZwEfgS:DDBbkb");

        }
        public bool AddLike(string type, string owner, string item, ref object locker)
        {
            if (SendRequest($"https://api.vk.com/method/likes.add?type={type}&owner_id={owner}&item_id={item}&access_token={token}&v=5.103", ref locker))
            {
                likes++;
                ChangeColumnData();
                return true;
            }
            return false;
        }
        public bool AddFriend(string name, ref object locker)
        {
            string id = GetUserId(name, ref locker);
            if (SendRequest($"https://api.vk.com/method/friends.add?user_id={id}&follow=0&access_token={token}&v=5.103", ref locker))
            {
                friends++;
                ChangeColumnData();
                return true;
            }
            return false;
        }
        string GetUserId(string name, ref object locker)
        {
            string response = "", id = "";
            Regex regex = new Regex(@"id\"":(\d*)");
            if (name.StartsWith("id"))
            {
                name = name.Substring(2);
                return name;
            }
            else
                SendRequest($"https://api.vk.com/method/users.get?fields=has_mobile&user_id={name}&name_case=nom&access_token={token}&v=5.103", ref locker, out response);
            id = regex.Match(response).Groups[1].Value;
            return id;
        }
        private void ChangeColumnData()
        {
            row.Cells[0].Value = login;
            row.Cells[1].Value = _earned;
            row.Cells[2].Value = _couponBalance;
            row.Cells[3].Value = likes;
            row.Cells[4].Value = friends;
            row.Cells[5].Value = status.ToString();
        }
        void GetCookie()
        {
            var c = new List<Cookie>();
            
            if (File.Exists($"{login}.txt"))
                using (StreamReader sr = new StreamReader($"{login}.txt"))
                {
                    string l = "";
                    while ((l = sr.ReadLine()) != null)
                    {
                        string[] data = l.Split('|');
                        DateTime expiresIn;
                        DateTime? expireTime = null;
                        if (DateTime.TryParse(data[4], out expiresIn))
                            expireTime = expiresIn;
                        c.Add(new OpenQA.Selenium.Cookie(data[0], data[1], data[2], data[3], expireTime));
                    }
                }
            Cookie = new ReadOnlyCollection<Cookie>(c);
        }
        public void SetCookie(ReadOnlyCollection<Cookie> cookies)
        {
            using (StreamWriter sw = new StreamWriter($"{login}.txt"))
            {
                Cookie = cookies;
                for (int i = 0; i < Cookie.Count; i++)
                {
                    string data = Cookie[i].Name + "|" + Cookie[i].Value + "|" + Cookie[i].Domain + "|" + Cookie[i].Path + "|" + Cookie[i].Expiry.ToString();
                    sw.WriteLine(data);
                }
            }
        }
        bool SendRequest(string req, ref object locker)
        {
            string captchaParametr = "";
            string response = "";

            StartRequest:

            while (true)
            {
                try
                {
                    lock(locker)
                    response = request.Get(req + captchaParametr).ToString();
                    break;
                }
                catch
                {
                    Task.Delay(4000).Wait();
                }
            }

            captchaParametr = "";
            string checkResponse = response;
            if (checkResponse.Contains("captcha"))
            {
                CAPTCHA:
                lock (locker)
                {
                    captchaParametr = CaptchaSolve(response);
                    if (captchaParametr == null)
                    {
                        captchaParametr = "";
                        Logging?.Invoke("Have exceeded the maximum number of consecutive redirects");
                        goto CAPTCHA;
                    }
                }
                goto StartRequest;
            }
            else if(checkResponse.Contains("error"))
            {
                Logging?.Invoke(checkResponse);
                return false;
            }
            else if (checkResponse.Contains("likes"))
            {
                return true;
            }
            else if(checkResponse.Contains("response"))
            {
                return true;
            }
            else if (checkResponse.Contains("Cannot add this user to friends as user not found"))
            {
                Logging?.Invoke("Ошибка поиска пользователя");
            }
            else if (checkResponse.Contains("User authorization failed: invalid access_token") || checkResponse.Contains("User authorization failed: invalid session"))
            {
                Logging?.Invoke("Неверный токен");
            }
            Logging?.Invoke(checkResponse);
            return false;

        }
        bool SendRequest(string req, ref object locker, out string response)
        {
            string captchaParametr = "";

            StartRequest:

            while (true)
            {
                try
                {
                    lock (locker)
                        response = request.Get(req + captchaParametr).ToString();
                    break;
                }
                catch
                {
                    Task.Delay(4000).Wait();
                }
            }

            captchaParametr = "";
            string checkResponse = response;
            if (checkResponse.Contains("captcha"))
            {
                CAPTCHA:
                lock (locker)
                {
                    captchaParametr = CaptchaSolve(response);
                    if (captchaParametr == null)
                    {
                        captchaParametr = "";
                        Logging?.Invoke("Have exceeded the maximum number of consecutive redirects");
                        goto CAPTCHA;
                    }
                }
                goto StartRequest;
            }
            else if (checkResponse.Contains("likes"))
            {
                return true;
            }
            else if (checkResponse.Contains("Cannot add this user to friends as user not found"))
            {
                Logging?.Invoke("Ошибка поиска пользователя");
            }
            else if (checkResponse.Contains("User authorization failed: invalid access_token") || checkResponse.Contains("User authorization failed: invalid session"))
            {
                Logging?.Invoke("Неверный токен");
            }
            Logging?.Invoke(checkResponse);
            return false;

        }
        string CaptchaSolve(string response)
        {
            return captchaSolver(response);
        }
    }
    public enum Status
    {
        IN_PROCESS,
        ACCESS_DENIED,
        COMPLETED,
        IN_QUEUE,
    }
}
