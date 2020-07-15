using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VK_ChangerInfo
{
    public class CaptchaError
    {
        public Error error { get; set; }
    }

    public class Error
    {
        public int error_code { get; set; }
        public string error_msg { get; set; }
        public Request_Params[] request_params { get; set; }
        public string captcha_sid { get; set; }
        public string captcha_img { get; set; }
    }

    public class Request_Params
    {
        public string key { get; set; }
        public string value { get; set; }
    }

}
