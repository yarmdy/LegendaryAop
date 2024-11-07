using LegendaryAop;
using System.Diagnostics;

new MyClassAop().Bark(4);

public class XingnengAttribute : AsyncAopAttribute
{
    public XingnengAttribute(int sort = 9999) : base(sort)
    {

    }
    public override string Name => "性能";
    public override async Task<object?> InvokeAsync(IAopMetaData data)
    {
        Console.WriteLine($"【{data.Method.DeclaringType?.Name??""}.{data.Method.Name}】：{DateTime.Now}：性能检测开始");
        Stopwatch sw = Stopwatch.StartNew();
        var result = await data.NextAsync();
        sw.Stop();
        Console.WriteLine($"【{data.Method.DeclaringType?.Name ?? ""}.{data.Method.Name}】：{DateTime.Now}：性能检测结束，运行了{sw.ElapsedMilliseconds}ms");
        return result;
    }
}
public class LogAttribute : AsyncAopAttribute
{
    public LogAttribute(int sort = 0) : base(sort)
    {

    }
    public override string Name => "日志";
    public override async Task<object?> InvokeAsync(IAopMetaData data)
    {
        Console.WriteLine("日志开始");
        var result = await data.NextAsync();
        Console.WriteLine("日志结束");
        return result;
    }
}
public class FilterAttribute : AsyncAopAttribute
{
    public FilterAttribute(int sort = 0) : base(sort)
    {

    }
    public override string Name => "过滤";
    public override async Task<object?> InvokeAsync(IAopMetaData data)
    {
        Console.WriteLine($"过滤开始{data.Parameters[0]}");
        if (data.Parameters[0].Equals(1))
        {
            Console.WriteLine("过滤中断");
            return await Task.FromResult("你好");
        }
        var result = await data.NextAsync();
        Console.WriteLine("过滤结束");
        return result;
    }
}
public class MyClassAop
{
    [Xingneng]
    [Filter(0)]
    [Log(0)]
    public string Bark(int i)
    {
        var str = $"bark!{i}";
        Console.WriteLine(str);
        if (i == 4)
        {
            Console.WriteLine("4");
        }
        return str;
    }
    [Xingneng]
    [Filter(0)]
    [Log(0)]
    public async Task<string> BarkAsync(int i)
    {
        await barkTask();
        if (i == 4)
        {
            Console.WriteLine("4");
        }
        return "BarkAsync";
    }
    [Xingneng]
    [Log(0)]
    public void Void() {
        Console.WriteLine("Void!");
    }
    [Xingneng]
    [Log(0)]
    public async Task VoidAsync() {
        Console.WriteLine("Void!");
        await Task.CompletedTask;
    }

    private Task barkTask()
    {
        return Task.Run(() =>
        {
            Thread.Sleep(1000);
            Console.WriteLine("barkasync!");
        });
    }

    public static string Barks(int i)
    {
        var str = $"bark!{i}";
        Console.WriteLine(str);
        return str;
    }
    public static string Bark2(int i)
    {
        return new DefaultAopExecutor().Exec<string>(Barks, i)!;
    }
}