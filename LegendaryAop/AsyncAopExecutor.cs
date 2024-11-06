using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace LegendaryAop
{
    public class DefaultAopExecutor : IAsyncAopExecutor, IAopExecutor, IVoidAopExecutor
    {
        private static ConcurrentDictionary<MethodBase, Func<object[], Task<object?>>> _cacheFunc = new();
        public T? Exec<T>(object? obj, MethodBase method, params object[] parameters)
        {
            var result = Task.Run(async () => await ExecAsync<T>(obj, method, parameters)).Result;
            if (result == null)
            {
                return default;
            }
            return result;
        }

        public void Exec(object? obj, MethodBase method, params object[] parameters)
        {
            Exec<object?>(obj,method,parameters);
        }

        public Task<T?> ExecAsync<T>(object? obj, MethodBase method, params object[] parameters)
        {
            var func = _cacheFunc.GetOrAdd(method, a => createFunc(obj,method,parameters));
            return Task.Run(async () => {
                var result = await func(parameters);
                if (result == null)
                {
                    return default;
                }
                return (T)result;
            });
        }
        private Func<object[], Task<object?>> createFunc(object? obj, MethodBase method, params object[] parameters)
        {
            Func<object[], Task<object?>> func = data =>
            {
                var ret = method.Invoke(obj,parameters);
                return Task.FromResult(ret);
            };
            foreach (var aop in method.GetCustomAttributes<AsyncAopAttribute>().Select((a, i) => new { aop = a, index = i }).OrderByDescending(a => a.aop.Sort).ThenByDescending(a => a.index).Select(a => a.aop))
            {
                func = addFunc(method, func,aop);
            }
            return func;
        }

        private Func<object[], Task<object?>> addFunc(MethodBase method ,Func<object[], Task<object?>>  func,AsyncAopAttribute aop)
        {
            return parameters => {
                return aop.InvokeAsync(new AopMetaData(method,func, parameters));
            };
        }
    }
}
