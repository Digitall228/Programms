using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Instagram_BruteForce
{
    public class PasswordGenerator
    {
        public string keys { get; set; } = "QWERTYUIOPASDFGHJKLZXCVBNMqwertyuiopasdfghjklzxcvbnm123456789";
        public string password { get; set; } = "QQQQQQ";
        public int currentKeysCount = 6;
        public int currentKey = 0;
        public int currentChangingKey = 0;

        public void Generate()
        {
            char[] _keys = keys.ToCharArray();

            currentKey++;
            if(currentKey >= _keys.Length)
            {

            }

            password.ToCharArray()[currentChangingKey] = _keys[currentKey];
        }
    }
}
