using System.Collections.Concurrent;
using System.Reflection;

namespace LegendaryAop
{
    public class DefaultAopExecutor : IAsyncAopExecutor, IAopExecutor, IVoidAopExecutor, IAsyncVoidAopExecutor
    {
        private static ConcurrentDictionary<MethodInfo, Func<object?, object[], Task<object?>>> _cacheFunc = new();
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
            var func = _cacheFunc.GetOrAdd(method.Method, createFunc);
            return func(method.Target, parameters).ContinueWith(a =>
            {
                if (a.Result == null)
                {
                    return default;
                }
                return (T)a.Result;
            });
        }
        private Func<object?, object[], Task<object?>> createFunc(MethodInfo method)
        {
            Func<object?, object[], Task<object?>> func = (obj,data) =>
            {
                var ret = method.Invoke(obj, data);
                if(ret is not Task task)
                {
                    return Task.FromResult(ret);
                }
                return task.ContinueWith(a => {
                    if (!method.ReturnType.IsConstructedGenericType)
                    {
                        return null;
                    }
                    return (object?)((dynamic)ret).Result;
                });
            };
            foreach (var aop in method.GetCustomAttributes().OfType<IAsyncAopAttribute>().Select((a, i) => new { aop = a, index = i }).OrderByDescending(a => a.aop.Sort).ThenByDescending(a => a.index).Select(a => a.aop))
            {
                func = addFunc(method, func,aop);
            }
            return func;
        }

        private Func<object?, object[], Task<object?>> addFunc(MethodInfo method ,Func<object?, object[], Task<object?>>  func,IAsyncAopAttribute aop)
        {
            return (obj,parameters) => {
                return aop.InvokeAsync(new AopMetaData(method,obj,func, parameters));
            };
        }

        public Task ExecAsync(Delegate method, params object[] parameters)
        {
            return ExecAsync<object>(method, parameters);
        }
    }
}
