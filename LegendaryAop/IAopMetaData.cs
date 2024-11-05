namespace LegendaryAop
{
    public interface IAopMetaData
    {
        object[] Parameters { get; set; }
        Task<object> NextAsync();
    }
}
