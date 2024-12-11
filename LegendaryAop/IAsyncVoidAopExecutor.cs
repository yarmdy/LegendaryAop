using System;
using System.Threading.Tasks;

namespace LegendaryAop
{
    public interface IAsyncVoidAopExecutor
    {
        Task ExecAsync(Delegate method, params object[] parameters);
    }
}
