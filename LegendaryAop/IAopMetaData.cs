using System.Reflection;

namespace LegendaryAop
{
    public interface IAopMetaData
    {
        MethodBase Method { get; }
        object[] Parameters { get; set; }
        Task<object?> NextAsync();
    }
}
