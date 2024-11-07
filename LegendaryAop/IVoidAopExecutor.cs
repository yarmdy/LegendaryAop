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
        void Exec(Delegate method,params object[] parameters);
    }
}
