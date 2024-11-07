using LegendaryAop;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Collections.Immutable;
using System.Reflection;

const string _prefix_ = "LegendaryAopTemp_";
if (args.Length < 1)
{
    return;
}
var fileInfo = new FileInfo(args[0]);
if (Directory.Exists(fileInfo.FullName))
{
    rename(new DirectoryInfo(fileInfo.FullName));
    return;
}
if (!fileInfo.Exists)
{
    return;
}

Environment.CurrentDirectory = fileInfo.DirectoryName!;
var assembly = Assembly.LoadFrom(fileInfo.Name);
var assNames = assembly.GetReferencedAssemblies()
    .Select(a => new FileInfo($"{a.Name}.dll"))
    .Where(a => a.Exists)
    .Select(a => new { reflection = Assembly.LoadFrom(a.Name), mono = AssemblyDefinition.ReadAssembly(a.Name) })
    .Concat([new { reflection = assembly, mono = AssemblyDefinition.ReadAssembly(fileInfo.Name) }])
    .SelectMany(a => a.reflection.Modules.Select(b => new { reflection = b, mono = a.mono.Modules.FirstOrDefault(c => c.Name == b.Name) }))
    .Where(a => a.mono != null)
    .SelectMany(a => a.reflection.GetTypes().Select(b => new { reflection = b, mono = a.mono!.Types.FirstOrDefault(c => c.FullName == b.FullName) }))
    .Where(a => a.mono != null)
    .SelectMany(a => a.reflection.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.DeclaredOnly).Select(b => new { reflection = b, mono = a.mono!.Methods.FirstOrDefault(c => c.Name == b.Name) }))
    .Where(a => a.mono != null)
    .Select(a => new { method = a.reflection, methodMono = a.mono, aops = a.reflection.GetCustomAttributes().OfType<IAsyncAopAttribute>().Select((b, i) => new { order = i, aop = b }).OrderByDescending(b => b.aop.Sort).ThenByDescending(b => b.order).Select(b => new { reflection = b.aop, mono = a.mono!.CustomAttributes.FirstOrDefault(c => c.AttributeType.FullName == b.aop.GetType().FullName) }).ToArray() })
    .Where(a => a.aops.Any())
    .Select(item =>
    {
        var method = item.method;
        var methodMono = item.methodMono!;

        processMethod(methodMono,method);

        var ass = method.DeclaringType!.Assembly;
        var assMono = methodMono!.DeclaringType.Module.Assembly;
        return new { Name = assMono.FullName, Location = ass.Location, assMono };
    }).GroupBy(a => a.Name).Select(a =>
    {
        var ass = a.First();
        var fileInfo = new FileInfo(ass.Location);
        var newFileInfo = new FileInfo(Path.Combine(fileInfo.DirectoryName!, $"{_prefix_}{fileInfo.Name}"));
        if (newFileInfo.Exists)
        {
            newFileInfo.Delete();
        }
        ass.assMono.Write(newFileInfo.FullName);
        return a.Key;
    }).ToList();
return;

void processMethod(MethodDefinition originalMethod,MethodInfo method)
{
    var clonedMethod = new MethodDefinition(originalMethod.Name, originalMethod.Attributes, originalMethod.ReturnType);
    // 克隆方法的参数
    foreach (var param in originalMethod.Parameters)
    {
        var p = new ParameterDefinition(param.Name, param.Attributes, param.ParameterType);
        clonedMethod.Parameters.Add(p);
    }

    // 克隆方法的局部变量
    foreach (var local in originalMethod.Body.Variables)
    {
        var v = new VariableDefinition(local.VariableType);
        clonedMethod.Body.Variables.Add(v);
    }

    // 克隆原始方法的IL代码
    var ilProcessor = clonedMethod.Body.GetILProcessor();
    foreach (var instruction in originalMethod.Body.Instructions)
    {
        ilProcessor.Append(instruction);
    }

    // 克隆原始方法的特性
    foreach (var attribute in originalMethod.CustomAttributes)
    {
        clonedMethod.CustomAttributes.Add(attribute);
    }

    // 克隆方法的异常处理（如果有的话）
    foreach (var handler in originalMethod.Body.ExceptionHandlers)
    {
        clonedMethod.Body.ExceptionHandlers.Add(handler);
    }


    clonedMethod.Name = $"{_prefix_}{clonedMethod.Name}";
    clonedMethod.Attributes &=~Mono.Cecil.MethodAttributes.Public;
    clonedMethod.Attributes &=~Mono.Cecil.MethodAttributes.Virtual;
    clonedMethod.Attributes |= Mono.Cecil.MethodAttributes.Private;
    clonedMethod.Attributes |= Mono.Cecil.MethodAttributes.SpecialName;

    
    originalMethod.CustomAttributes.Clear();

    originalMethod.Body.Instructions.Clear();
    var ilp = originalMethod.Body.GetILProcessor();
    //开始
    var execType = typeof(DefaultAopExecutor);
    var execRef = originalMethod.Module.ImportReference(execType);
    var execCon = originalMethod.Module.ImportReference(execType.GetConstructor([]));

    MethodReference exec = originalMethod.Module.ImportReference(execType.GetMethod("Exec", 0, [typeof(Delegate), typeof(object[])]));

    if (method.ReturnType==typeof(Task)) 
    {
        exec = originalMethod.Module.ImportReference(execType.GetMethod("ExecAsync", 0, [typeof(Delegate), typeof(object[])]));
    }
    else if(method.ReturnType.IsGenericType && method.ReturnType.BaseType == (typeof(Task)))
    {
        exec = originalMethod.Module.ImportReference(execType.GetMethod("ExecAsync", 1, [typeof(Delegate), typeof(object[])])!.MakeGenericMethod(method.ReturnType.GenericTypeArguments.First()));
    }
    else if(method.ReturnType!=typeof(void))
    {
        exec = originalMethod.Module.ImportReference(execType.GetMethod("Exec", 1, [typeof(Delegate), typeof(object[])])!.MakeGenericMethod(method.ReturnType));
    }
    var argCount = originalMethod.Parameters.Count;
    var paramTypes = method.GetParameters().Select(a => a.ParameterType).ToList();
    if (method.ReturnType != typeof(void))
    {
        paramTypes.Add(method.ReturnType);
    }
    var funcType = method.ReturnType==typeof(void)? Type.GetType($"System.Action`{paramTypes.Count}")!: Type.GetType($"System.Func`{paramTypes.Count}")!;
    if (funcType == null)
    {
        funcType = typeof(Action);
    }
    if (paramTypes.Count > 0)
    {
        funcType = funcType.MakeGenericType(paramTypes.ToArray());
    }
    var funcRef = originalMethod.Module.ImportReference(funcType);
    var funcCon = originalMethod.Module.ImportReference(funcType.GetConstructors().First());
    var objRef = originalMethod.Module.ImportReference(typeof(object));

    ilp.Append(ilp.Create(OpCodes.Newobj,execCon));
    ilp.Append(ilp.Create(OpCodes.Ldftn,clonedMethod));
    ilp.Append(ilp.Create(OpCodes.Newobj, funcCon));

    
    for (int i = 0; i < argCount; i++) {
        ilp.Append(ilp.Create(OpCodes.Ldarg, i));
    }
    ilp.Append(ilp.Create(OpCodes.Newarr, objRef));

    ilp.Append(ilp.Create(OpCodes.Call, exec));
    ilp.Append(ilp.Create(OpCodes.Ret));
    

    originalMethod.DeclaringType.Methods.Add(clonedMethod);
}
void rename(DirectoryInfo dir)
{
    dir.GetFiles(_prefix_ + "*.dll").AsParallel().ForAll(a =>
    {
        var newFile = new FileInfo(Path.Combine(a.DirectoryName!, a.Name.Substring(_prefix_.Length)));
        if (newFile.Exists)
        {
            newFile.Delete();
        }
        a.MoveTo(newFile.FullName,true);
    });
}