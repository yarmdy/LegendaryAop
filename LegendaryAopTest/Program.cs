﻿using LegendaryAop;
using System.Collections.Concurrent;
using System.Diagnostics;

var aop = new MyClassAop();
var str = "";
do
{
    Console.WriteLine("请输入数字：");
    str = Console.ReadLine();
    if (str=="cls")
    {
        Console.Clear();
        continue;
    }
    if (!int.TryParse(str,out int key))
    {
        continue;
    }
    Console.WriteLine($"{key}的结果是:{await aop.GetValue(key)}");
    Console.WriteLine();
} while (str != "exit");

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
        await Task.Delay(1000);
        Console.WriteLine("日志记录成功");
        var result = await data.NextAsync();
        Console.WriteLine("开始后日志");
        await Task.Delay(1000);
        Console.WriteLine("后日志记录成功");
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
        await Task.Delay(1000);
        Console.WriteLine("过滤完成");
        var result = await data.NextAsync();
        Console.WriteLine("后过滤");
        await Task.Delay(1000);
        Console.WriteLine("后过滤完成");
        return result;
    }
}
class CacheAttribute : AsyncAopAttribute
{
    static ConcurrentDictionary<object, Task<object?>> _dic = new();
    public override string Name => "缓存";

    public override Task<object?> InvokeAsync(IAopMetaData data)
    {
        if (data.Parameters.Length == 0 || data.Parameters[0]==null)
        {
            return data.NextAsync();
        }
        bool notfound = false;
        var rest = _dic.GetOrAdd(data.Parameters[0], o => {
            notfound = true;
            return data.NextAsync();
        });
        if (!notfound)
        {
            Console.WriteLine("恭喜：发现缓存^_^");
        }
        return rest;
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
        Console.WriteLine("我是异步方法主体，开始");
        await barkTask();
        if (i == 4)
        {
            Console.WriteLine("4");
        }
        Console.WriteLine("我是异步方法主体，结束");
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
    [Cache]
    [Log]
    [Xingneng]
    public async Task<int> GetValue(int key)
    {
        await Task.Delay(1000);
        return key * 2 - 10;
    }
}