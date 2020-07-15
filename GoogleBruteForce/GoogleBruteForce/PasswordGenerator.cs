using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleBruteForce
{
    public class PasswordGenerator
    {
        public string dictionary = "QWERTYUIOPASDFGHJKLZXCVBNMqwertyuiopasdfghjklzxcvbnm123456789";
        public int currentIndex = 0;
        public int countSymbols = 1;
        public string currentString;

        public string GetNext()
        {
            if(currentIndex >= dictionary.Length)
            {
                countSymbols++;
            }
            currentString = dictionary[currentIndex].ToString();
            return currentString;
        }

    }
}
