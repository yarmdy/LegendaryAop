
using System.Reflection;

namespace LegendaryAop
{
    internal class AopMetaData : IAopMetaData
    {
        private Func<object[], Task<object?>> _next;
        public AopMetaData(MethodBase method,Func<object[], Task<object?>> next, object[] parameters)
        {
            Method = method;
            _next = next;
            Parameters = parameters;
        }
        public object[] Parameters { get; set; }

        public MethodBase Method { get; init;}

        public Task<object?> NextAsync()
        {
            return _next(Parameters);
        }
    }
}
