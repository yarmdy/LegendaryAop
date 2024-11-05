namespace LegendaryAop
{
    public interface IAsyncAopAttribute
    {
        string Name { get; }
        int Sort { get; }
        Task<object> InvokeAsync(IAopMetaData data);
    }
}
