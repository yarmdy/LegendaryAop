using System.Reflection;
using System.Threading.Tasks;

namespace LegendaryAop
{
    public interface IAopMetaData
    {
        public object? Obj { get; init; }
        MethodInfo Method { get; }
        bool IsProperty { get; }
        PropertyInfo? Property { get; }
        bool IsGetMethod { get; }
        object[] Parameters { get; set; }
        Task<object?> NextAsync();
    }
}
