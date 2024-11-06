
using System.Reflection;

namespace LegendaryAop
{
    internal class AopMetaData : IAopMetaData
    {
        internal Delegate _method;
        public AopMetaData(Delegate method, object[] parameters)
        {
            _method = method;
            Parameters = parameters;
        }
        public object[] Parameters { get; set; }

        public Task<object?> NextAsync()
        {
            return (Task<object?>)_method.DynamicInvoke(Parameters)!;
        }
    }
}
