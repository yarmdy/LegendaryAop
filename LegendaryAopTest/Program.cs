﻿using LegendaryAop;
using System.Reflection;

new MyClassAop().Bark();

public class LogAttribute : AsyncAopAttribute
{
    public LogAttribute(int sort = 0) : base(sort)
    {

    }
    public override string Name => "日志";
    public override async Task<object> InvokeAsync(IAopMetaData data)
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
    public override async Task<object> InvokeAsync(IAopMetaData data)
    {
        Console.WriteLine("过滤开始");
        var result = await data.NextAsync();
        Console.WriteLine("过滤结束");
        return result;
    }
}
public class MyClassAop
{
    [Filter(0)]
    [Log(0)]
    public void Bark()
    {
        
        Console.WriteLine("bark!");
    }
}