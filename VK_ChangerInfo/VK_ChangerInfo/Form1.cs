using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Leaf.xNet;
using Newtonsoft.Json;

namespace VK_ChangerInfo
{
    public partial class Form1 : Form
    {

        CptchCaptchaSolver solver;
        List<string> tokens = new List<string>();
        List<string> proxies = new List<string>();
        List<string> names = new List<string>();
        List<string> surnames = new List<string>();

        Statistics statistics = new Statistics();

        Random rnd = new Random();

        object filelocker = new object();
        public Form1()
        {
            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false;
        }

        #region Start
        private void Button3_Click(object sender, EventArgs e)
        {
            if(textBox1.Text == string.Empty)
            {
                LogAdd("ERROR: ApiKey = null, return");
                MessageBox.Show("Установите ApiKey");
                return;
            }
            if(tokens.Count < 1)
            {
                LogAdd("ERROR: tokens count < 1, return");
                MessageBox.Show("Добавьте токены");
                return;
            }

            solver = new CptchCaptchaSolver(textBox1.Text);

            Task startTask = new Task(Change);
            startTask.Start();
            Task update = new Task(Update);
            update.Start();
            //Change();
        }
        void Change()
        {
            LogAdd("Started");
            string response = string.Empty;

            for (int i = 0; i < tokens.Count; i++)
            {
                try
                {
                    string name = names[rnd.Next(0, names.Count)];
                    string surname = surnames[rnd.Next(0, surnames.Count)];
                    int sex = 1;

                    string captcha = string.Empty;
                    string token = tokens[i];

                    HttpRequest request = new HttpRequest();
                    request.UserAgentRandomize();
                    request.Proxy = ProxyClient.Parse(ProxyType.HTTP, proxies[1]);
                    REQUEST:
                    string req = $"https://api.vk.com/method/account.saveProfileInfo?first_name={name}&last_name={surname}&sex={sex}&access_token={token}&v=5.103" + captcha;
                    response = request.Get(req).ToString();

                    ChangeResponse serializeResponse = JsonConvert.DeserializeObject<ChangeResponse>(response);
                    if (serializeResponse.response.name_request.status == "success")
                    {
                        statistics.good++;
                        LogAdd("GOOD account info changed");
                    }
                    else if (serializeResponse.response.name_request.status == "declined")
                    {
                        statistics.bad++;
                        LogAdd("BAD info not changed");
                    }
                    else
                    {
                        statistics.errors++;
                        LogAdd("NONE INFO response: " + response);
                    }
                }
                catch(Exception ex)
                {
                    statistics.errors++;
                    LogAdd($"ERROR while changing info: {Environment.NewLine}\tMessage: {ex.Message}{Environment.NewLine}\tStackTrace: {ex.StackTrace}{Environment.NewLine}Response: {response}");
                }
                finally
                {
                    statistics.left--;
                }
            }

            LogAdd("Ended");
        }
        void Update()
        {
            while (true)
            {
                label2.Text = $"Всего аккаунтов: {statistics.all}";
                label3.Text = $"Изменено аккаунтов: {statistics.good}";
                label5.Text = $"Отклонено: {statistics.bad}";
                label4.Text = $"Осталось  аккаунтов: {statistics.left}";
                label8.Text = $"Ошибок: {statistics.errors}";
                Task.Delay(250).Wait();
            }
        }
        string SolveCaptcha(CaptchaError captchaError)
        {
            ZERO:
            string sid = captchaError.error.captcha_sid;
            string result = solver.Solve($"https://api.vk.com///captcha.php?sid={sid}&s=1");
            if (result.Contains("error"))
            {
                LogAdd("Captcha result ERROR: " + result);
                goto ZERO;
            }
            string captchaParametr = $"&captcha_key={result}&captcha_sid={sid}";
            return captchaParametr;
        }
        #endregion

        #region Proxy
        private void Button2_Click(object sender, EventArgs e)
        {
            try
            {
                if(openFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    LogAdd("Start proxy load");
                    proxies = ParseProxy(openFileDialog1.FileName).ToList();
                    LogAdd("Proxy loaded");
                }
            }
            catch(Exception ex)
            {
                LogAdd($"ERROR while load proxy: {Environment.NewLine}\tMessage: {ex.Message}{Environment.NewLine}\tStackTrace: {ex.StackTrace}");
            }
        }

        IEnumerable<string> ParseProxy(string path)
        {
            using (StreamReader sr = new StreamReader(path))
            {
                string l = "l";

                while ((l = sr.ReadLine()) != null && l != "")
                {
                    yield return l;
                }
            }
        }
        #endregion

        #region Tokens
        private void Button1_Click(object sender, EventArgs e)
        {
            try
            {
                if (openFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    LogAdd("Start tokens load");
                    tokens = ParseTokens(openFileDialog1.FileName).ToList();
                    statistics.all = tokens.Count;
                    statistics.left = tokens.Count;
                    LogAdd("Tokens loaded");
                }
            }
            catch (Exception ex)
            {
                LogAdd($"ERROR while load tokens: {Environment.NewLine}\tMessage: {ex.Message}{Environment.NewLine}\tStackTrace: {ex.StackTrace}");
            }
        }
        IEnumerable<string> ParseTokens(string path)
        {
            using (StreamReader sr = new StreamReader(path))
            {
                string l = "l";

                while ((l = sr.ReadLine()) != null && l != "")
                {
                    yield return l;
                }
            }
        }
        #endregion

        public void LogAdd(string text)
        {
            lock(filelocker)
            {
                using (StreamWriter sw = new StreamWriter("log.txt"))
                {
                    sw.WriteLine($"{DateTime.Now}: {text}");
                }
            }
        }

        private void Button4_Click(object sender, EventArgs e)
        {
            try
            {
                if (openFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    LogAdd("Start names load");
                    names = ParseNames(openFileDialog1.FileName).ToList();
                    LogAdd("Names loaded");
                }
            }
            catch (Exception ex)
            {
                LogAdd($"ERROR while load names: {Environment.NewLine}\tMessage: {ex.Message}{Environment.NewLine}\tStackTrace: {ex.StackTrace}");
            }
        }

        IEnumerable<string> ParseNames(string path)
        {
            using (StreamReader sr = new StreamReader(path))
            {
                string l = "l";

                while ((l = sr.ReadLine()) != null && l != "")
                {
                    yield return l;
                }
            }
        }

        private void Button5_Click(object sender, EventArgs e)
        {
            try
            {
                if (openFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    LogAdd("Start surnames load");
                    surnames = ParseNames(openFileDialog1.FileName).ToList();
                    LogAdd("Surnames loaded");
                }
            }
            catch (Exception ex)
            {
                LogAdd($"ERROR while load surnames: {Environment.NewLine}\tMessage: {ex.Message}{Environment.NewLine}\tStackTrace: {ex.StackTrace}");
            }
        }
    }
}
