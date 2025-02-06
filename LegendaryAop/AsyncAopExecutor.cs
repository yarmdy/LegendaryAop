using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace LegendaryAop
{
    public class DefaultAopExecutor : IAsyncAopExecutor, IAopExecutor, IVoidAopExecutor, IAsyncVoidAopExecutor
    {
        MethodInfo caller;
        public DefaultAopExecutor() {
            caller = (new StackTrace().GetFrame(1)!.GetMethod() as MethodInfo)!;
        }
        private static ConcurrentDictionary<MethodInfo, Func<object?, object[], Task<object?>>> _cacheFunc = new();
        public T? Exec<T>(Delegate method, params object[] parameters)
        {
            var task = ExecAsync<T>(method, parameters);
            if (SynchronizationContext.Current == null)
            {
                return task.Result;
            }
            return Task.Run(async () => await task).Result;
        }

        public void Exec(Delegate method, params object[] parameters)
        {
            Exec<object?>(method,parameters);
        }

        public Task<T?> ExecAsync<T>(Delegate method, params object[] parameters)
        {
            var func = _cacheFunc.GetOrAdd(method.Method, RuntimeFeature.IsDynamicCodeCompiled? compileFunc<T>: createFunc<T>);
            return func(method.Target, parameters).ContinueWith(a =>
            {
                if (a.Result == null)
                {
                    return default;
                }
                return (T)a.Result;
            });
        }
        private Func<object?, object[], Task<object?>> compileFunc<T>(MethodInfo method)
        {
            
            var objParameter = Expression.Parameter(typeof(object),"instance");
            var parameters = Expression.Parameter(typeof(object[]),"params");
            var execParameters = method.GetParameters().Select((a, i) => {
                var convert = Expression.ArrayIndex(parameters, Expression.Constant(i));
                return Expression.Convert(convert, a.ParameterType);
            });
            var owner = Expression.Convert(objParameter,method.DeclaringType!);
            Expression body = Expression.Call(method.IsStatic?null: owner, method, execParameters);
            if (method.ReturnType == typeof(void))
            {
                var label = Expression.Label(typeof(Task<object?>));
                body = Expression.Block(
                    body,
                    Expression.Return(label, Expression.Constant(Task.FromResult<object?>(null))),
                    Expression.Label(label, Expression.Constant(Task.FromResult<object?>(null)))
                );
            }else if (!method.ReturnType.IsAssignableTo(typeof(Task)))
            {
                Expression<Func<object?,Task<object?>>> convert = a=>Task.FromResult(a);
                body = Expression.Invoke(convert,Expression.Convert(body,typeof(object)));
            }
            else
            {
                Expression<Func<Task<T>, Task<object?>>> convert = a => a.ContinueWith(b=>(object?)b.Result);
                body = Expression.Invoke(convert, body);
            }
            var lambda = Expression.Lambda<Func<object?, object[], Task<object?>>>(body, objParameter, parameters);
            Func<object?, object[], Task<object?>> func = lambda.Compile();
            return addAop(func, method);
        }
        private Func<object?, object[], Task<object?>> createFunc<T>(MethodInfo method)
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
                    return (object?)((Task<T>)ret).Result;
                });
            };
            
            return addAop(func,method);
        }
        private Func<object?, object[], Task<object?>> addAop(Func<object?, object[], Task<object?>> func, MethodInfo method)
        {
            foreach (var aop in method.GetCustomAttributes().OfType<IAsyncAopAttribute>().Select((a, i) => new { aop = a, index = i }).OrderByDescending(a => a.aop.Sort).ThenByDescending(a => a.index).Select(a => a.aop))
            {
                func = addFunc(func, aop);
            }
            return func;
        }

        private Func<object?, object[], Task<object?>> addFunc(Func<object?, object[], Task<object?>>  func,IAsyncAopAttribute aop)
        {
            return (obj,parameters) => {
                return aop.InvokeAsync(new AopMetaData(caller, obj,func, parameters));
            };
        }

        public Task ExecAsync(Delegate method, params object[] parameters)
        {
            return ExecAsync<object>(method, parameters);
        }
    }
}
