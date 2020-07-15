using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Instagram_BruteForce
{
    public class AuthData
    {
        public bool authenticated { get; set; }
        public bool user { get; set; }
        public string userId { get; set; }
        public bool oneTapPrompt { get; set; }
        public bool reactivated { get; set; }
        public string status { get; set; }
    }
}
