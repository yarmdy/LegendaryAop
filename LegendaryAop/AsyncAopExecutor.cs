using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace LegendaryAop
{
    public class AsyncAopExecutor : IAsyncAopExecutor
    {
        public Task<object> Exec(MethodBase method, object[] parameters)
        {
            
            return null;
        }
    }
}
