using System;
using System.Threading.Tasks;

namespace LegendaryAop
{
    public interface IAsyncAopExecutor
    {
        Task<T?> ExecAsync<T>(Delegate method, params object[] parameters);
    }
}
