using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassPeriodDetail
{
    class Permissions
    {
        public static string 班級缺曠獎懲總表 { get { return "班級缺曠獎懲總表C0E8B463-2832-447F-802F-4DE75DB4A749"; } }

        public static bool 班級缺曠獎懲總表權限
        {
            get
            {
                return FISCA.Permission.UserAcl.Current[班級缺曠獎懲總表].Executable;
            }
        }
    }
}
