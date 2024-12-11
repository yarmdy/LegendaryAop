using System.Reflection;
using System.Threading.Tasks;

namespace LegendaryAop
{
    public interface IAopMetaData
    {
        MethodInfo Method { get; }
        object[] Parameters { get; set; }
        Task<object?> NextAsync();
    }
}
