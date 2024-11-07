using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace LegendaryAop
{
    public class DefaultAopExecutor : IAsyncAopExecutor, IAopExecutor, IVoidAopExecutor, IAsyncVoidAopExecutor
    {
        private static ConcurrentDictionary<MethodInfo, Func<object[], Task<object?>>> _cacheFunc = new();
        public T? Exec<T>(Delegate method, params object[] parameters)
        {
            var result = Task.Run(async () => await ExecAsync<T>(method, parameters)).Result;
            if (result == null)
            {
                return default;
            }
            return result;
        }

        public void Exec(Delegate method, params object[] parameters)
        {
            Exec<object?>(method,parameters);
        }

        public Task<T?> ExecAsync<T>(Delegate method, params object[] parameters)
        {
            var func = _cacheFunc.GetOrAdd(method.Method, a => createFunc(method.Target,method.Method,parameters));
            return Task.Run(async () => {
                var result = await func(parameters);
                if (result == null)
                {
                    return default;
                }
                return (T)result;
            });
        }
        private Func<object[], Task<object?>> createFunc(object? obj, MethodInfo method, params object[] parameters)
        {
            Func<object[], Task<object?>> func = data =>
            {
                var ret = method.Invoke(obj,parameters);
                if(ret is not Task task)
                {
                    return Task.FromResult(ret);
                }
                
                return Task.Run(async () =>
                {
                    await task;
                    if(!method.ReturnType.IsGenericParameter)
                    {
                        return null;
                    }
                    return (object?)((dynamic)ret).Result;
                });
            };
            foreach (var aop in method.GetCustomAttributes<AsyncAopAttribute>().Select((a, i) => new { aop = a, index = i }).OrderByDescending(a => a.aop.Sort).ThenByDescending(a => a.index).Select(a => a.aop))
            {
                func = addFunc(method, func,aop);
            }
            return func;
        }

        private Func<object[], Task<object?>> addFunc(MethodInfo method ,Func<object[], Task<object?>>  func,AsyncAopAttribute aop)
        {
            return parameters => {
                return aop.InvokeAsync(new AopMetaData(method,func, parameters));
            };
        }

        public Task ExecAsync(Delegate method, params object[] parameters)
        {
            return ExecAsync<object>(method, parameters);
        }
    }
}
