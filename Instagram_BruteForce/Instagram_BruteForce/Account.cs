using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Instagram_BruteForce
{
    public class Account
    {
        public string login { get; set; }
        public string password{ get; set; }
        public string cookie{ get; set; }

        public Status status { get; set; }

        public Account(string _login, string _password)
        {
            login = _login;
            password = _password;
            status = Status.IN_QUEU;
        }
    }
    public enum Status
    {
        GOOD,
        BAD,
        IN_QUEU,
        IN_PROCESS,
        ERROR
    }
}
