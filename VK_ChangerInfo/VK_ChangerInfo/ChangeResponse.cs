using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VK_ChangerInfo
{
    public class ChangeResponse
    {
        public Response response { get; set; }
    }

    public class Response
    {
        public int changed { get; set; }
        public Name_Request name_request { get; set; }
    }

}
