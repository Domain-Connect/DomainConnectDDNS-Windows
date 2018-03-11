using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Web.Script.Serialization;

namespace Tester
{
    class Program
    {
        
        static void Main(string[] args)
        {
            bool result = OAuthHelper.OAuthHelper.GetTokens("4f6446f08ff64e28b1a2631372d0cda3");
        }
    }
}
