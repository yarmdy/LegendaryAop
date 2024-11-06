using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace LegendaryAop
{
    public interface IVoidAopExecutor
    {
        void Exec(object? obj, MethodBase method,params object[] parameters);
    }
}
