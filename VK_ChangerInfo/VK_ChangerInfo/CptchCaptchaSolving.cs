﻿using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace VK_ChangerInfo
{
    public class CptchCaptchaSolver
    {

        //Ключ нужно заменить на свой со страницы https://cptch.net/profile
        private String CPTCH_API_KEY = "ac8765c7a7f2e45a39bb4506211f8230";
        //Ваш идентификатор приложения (soft_id). Его можно получить, создав приложение на странице https://cptch.net/profile/soft
        private const String CPTCH_SOFT_ID = "1";

        private const String CPTCH_UPLOAD_URL = "https://cptch.net/in.php";
        private const String CPTCH_RESULT_URL = "https://cptch.net/res.php";

        public CptchCaptchaSolver(string API_KEY)
        {
            CPTCH_API_KEY = API_KEY;
        }

        public string Solve(string url)
        {
            //Скачиваем файл капчи из Вконтакте
            byte[] captcha = DownloadCaptchaFromVk(url);
            if (captcha != null)
            {
                //Загружаем файл на cptch.net
                string uploadResponse = UploadCaptchaToCptch(captcha);
                //Получаем из ответа id капчи
                string captchaId = ParseUploadResponse(uploadResponse);
                if (captchaId != null)
                {
                    Console.WriteLine("Id капчи: " + captchaId);
                    //Ждем несколько секунд
                    Task.Delay(2000).Wait();
                    //Делаем запрос на получение ответа до тех пор пока ответ не будет получен
                    string solution = null;
                    do
                    {
                        string solutionResponse = GetCaptchaSolution(getCaptchaRequestUri(captchaId));
                        solution = ParseSolutionResponse(solutionResponse);
                    } while (solution == null);

                    return solution;
                }
            }
            else
            {
                Console.WriteLine("Не удалось скачать капчу с Вконтакте");
            }

            return null;
        }

        private string getCaptchaRequestUri(string captchaId)
        {
            return CPTCH_RESULT_URL + "?" + "key=" + CPTCH_API_KEY + "&action=get" + "&id=" + captchaId;
        }

        private byte[] DownloadCaptchaFromVk(string captchaUrl)
        {
            using (WebClient client = new WebClient())
            using (Stream s = client.OpenRead(captchaUrl))
            {
                return client.DownloadData(captchaUrl);
            }
        }

        private string UploadCaptchaToCptch(byte[] captcha)
        {
            using (HttpClient httpClient = new HttpClient())
            {
                MultipartFormDataContent form = new MultipartFormDataContent();

                form.Add(new StringContent(CPTCH_API_KEY), "key");
                form.Add(new StringContent("post"), "method");
                form.Add(new StringContent(CPTCH_SOFT_ID), "soft_id");
                form.Add(new ByteArrayContent(captcha, 0, captcha.Length), "file", "captcha");
                var response = httpClient.PostAsync(CPTCH_UPLOAD_URL, form).Result;
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = response.Content;
                    return responseContent.ReadAsStringAsync().Result;
                }
                else
                {
                    return null;
                }
            }
        }

        private string ParseUploadResponse(string uploadResponse)
        {
            if (uploadResponse.Contains("ERROR"))
            {
                return null;
            }
            else if (uploadResponse.Contains("OK"))
            {
                return uploadResponse.Split('|')[1];
            }
            return null;
        }

        public static String GetCaptchaSolution(string captchaSolutionUrl)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(captchaSolutionUrl);

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            using (Stream stream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }

        private string ParseSolutionResponse(string response)
        {
            if (response.Equals("ERROR"))
            {
                return "error: " + response;
            }
            else if (response.Equals("CAPCHA_NOT_READY"))
            {
                Task.Delay(1000).Wait();
                return null;
            }
            else if (response.Contains("OK"))
            {
                return response.Split('|')[1];
            }
            return null;
        }
    }
}
