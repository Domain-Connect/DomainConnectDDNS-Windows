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
            bool result = OAuthHelper.OAuthHelper.GetTokens("6a2f8c665126444d88ca3b95f3340bdd");
        }
    }
}
