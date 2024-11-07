
using System.Reflection;

namespace LegendaryAop
{
    internal class AopMetaData : IAopMetaData
    {
        private Func<object?, object[], Task<object?>> _next;
        public AopMetaData(MethodInfo method,object? obj,Func<object?, object[], Task<object?>> next, object[] parameters)
        {
            Method = method;
            _next = next;
            Parameters = parameters;
            Obj= obj;
        }
        public object[] Parameters { get; set; }

        public MethodInfo Method { get; init;}
        public object? Obj { get; init;}

        public Task<object?> NextAsync()
        {
            return _next(Obj,Parameters);
        }
    }
}
