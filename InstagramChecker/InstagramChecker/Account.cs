using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstagramChecker
{
    public class Account
    {
        public string login;
        public string password;

        public string cookies;

        public Status status;

        public Account(string Login, string Password)
        {
            login = Login;
            password = Password;

            status = Status.IN_QUEU;
        }
        public override string ToString()
        {
            return login + ":" + password;
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
