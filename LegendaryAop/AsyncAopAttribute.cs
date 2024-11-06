namespace LegendaryAop
{
    public abstract class AsyncAopAttribute : Attribute, IAsyncAopAttribute
    {
        public AsyncAopAttribute(int sort=0) 
        { 
            Sort = sort;
        }
        public abstract string Name { get; }
        public virtual int Sort { get; }
        public abstract Task<object?> InvokeAsync(IAopMetaData data);
    }
}
