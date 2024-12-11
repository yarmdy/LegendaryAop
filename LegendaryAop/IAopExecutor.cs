using System;

namespace LegendaryAop
{
    public interface IAopExecutor
    {
        T? Exec<T>(Delegate method, params object[] parameters);
    }
}
