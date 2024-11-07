using System.Reflection;

namespace LegendaryAop
{
    public interface IAopMetaData
    {
        MethodInfo Method { get; }
        object[] Parameters { get; set; }
        Task<object?> NextAsync();
    }
}
