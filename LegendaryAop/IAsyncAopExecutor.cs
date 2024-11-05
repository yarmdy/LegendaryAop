using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace LegendaryAop
{
    public interface IAsyncAopExecutor
    {
        Task<object> Exec(MethodBase method, object[] parameters);
    }
}
