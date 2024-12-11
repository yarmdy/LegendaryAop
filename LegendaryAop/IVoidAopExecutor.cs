using System;

namespace LegendaryAop
{
    public interface IVoidAopExecutor
    {
        void Exec(Delegate method,params object[] parameters);
    }
}
