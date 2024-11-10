using LegendaryAop;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;

const string _prefix_ = "LegendaryAopTemp_";
HashSet<string> myNames= new HashSet<string>(StringComparer.InvariantCultureIgnoreCase) { "LegendaryAop", "LegendaryAopProcess" };
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

var rcfg = new ReaderParameters { ReadSymbols = true };
var wcfg = new WriterParameters { WriteSymbols = true };

Environment.CurrentDirectory = fileInfo.DirectoryName!;
var assembly = Assembly.LoadFrom(fileInfo.Name);
var assNames = assembly.GetReferencedAssemblies()
    .Where(a => !myNames.Contains(a.Name!))
    .Select(a => new FileInfo($"{a.Name}.dll"))
    .Where(a => a.Exists)
    .Select(a => new { ass = Assembly.LoadFrom(a.FullName), mono = AssemblyDefinition.ReadAssembly(a.Name, rcfg) })
    .Concat([new { ass = assembly, mono = AssemblyDefinition.ReadAssembly(fileInfo.Name, rcfg) }])
    .SelectMany(a => a.mono.Modules.Select(b => new { a.ass, mono = b }))
    .Where(a => a.mono != null)
    .SelectMany(a => a.mono.Types.Select(b => new { a.ass, Mono = b }))
    .Where(a => a.Mono != null)
    .SelectMany(a => a.Mono.Methods.Select(b => new { a.ass, mono = b }))
    .Where(a => a.mono != null)
    .Select(a => new { ass = a.ass, methodMono = a.mono, aops = a.mono.CustomAttributes.Select(b => b.AttributeType).Where(IsInterface) })
    .Where(a => a.aops.Any()).ToList();
assNames.Select(item =>
    {
        var methodMono = item.methodMono!;
        var ass = item.ass;

        processMethod(methodMono, ass);

        var assMono = methodMono!.DeclaringType.Module.Assembly;
        return new { Name = assMono.FullName, ass.Location, assMono };
    })
    .GroupBy(a => a.Name)
    .Select(a =>
    {
        var ass = a.First();
        var fileInfo = new FileInfo(ass.Location);
        var newFileInfo = new FileInfo(Path.Combine(fileInfo.DirectoryName!, $"{_prefix_}{fileInfo.Name}"));
        if (newFileInfo.Exists)
        {
            newFileInfo.Delete();
        }
        ass.assMono.Write(newFileInfo.FullName, wcfg);
        return a.Key;
    }).ToList();
return;

void processMethod(MethodDefinition originalMethod,Assembly ass)
{
    try
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

            // 以下是调试符号
            //foreach()
        }

        foreach (var seq in originalMethod.DebugInformation.SequencePoints)
        {
            clonedMethod.DebugInformation.SequencePoints.Add(seq);
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
        clonedMethod.Attributes &= ~Mono.Cecil.MethodAttributes.Public;
        clonedMethod.Attributes &= ~Mono.Cecil.MethodAttributes.Virtual;
        clonedMethod.Attributes |= Mono.Cecil.MethodAttributes.Private;
        clonedMethod.Attributes |= Mono.Cecil.MethodAttributes.SpecialName;


        originalMethod.CustomAttributes.Clear();

        originalMethod.Body.Instructions.Clear();
        var ilp = originalMethod.Body.GetILProcessor();
        //开始
        var execType = typeof(DefaultAopExecutor);
        var execRef = originalMethod.Module.ImportReference(execType);
        var execCon = originalMethod.Module.ImportReference(execType.GetConstructor([]));
        var taskRef = originalMethod.Module.ImportReference(typeof(Task));
        var voidRef = originalMethod.Module.ImportReference(typeof(void));
        var objRef = originalMethod.Module.ImportReference(typeof(object));
        var nintRef = originalMethod.Module.ImportReference(typeof(nint));

        MethodReference exec = originalMethod.Module.ImportReference(execType.GetMethod("Exec", 0, [typeof(Delegate), typeof(object[])]));

        if (originalMethod.ReturnType.FullName == taskRef.FullName)
        {
            exec = originalMethod.Module.ImportReference(execType.GetMethod("ExecAsync", 0, [typeof(Delegate), typeof(object[])]));
        }
        else if (originalMethod.ReturnType.HasGenericParameters && originalMethod.ReturnType.Resolve().BaseType.FullName == taskRef.FullName)
        {
            var genMethod = originalMethod.Module.ImportReference(execType.GetMethod("ExecAsync", 1, [typeof(Delegate), typeof(object[])])!);
            var genInsMethod = new GenericInstanceMethod(genMethod);
            genInsMethod.GenericArguments.Add(originalMethod.ReturnType);
            exec = genInsMethod;
        }
        else if (originalMethod.ReturnType.FullName != voidRef.FullName)
        {
            var genMethod = originalMethod.Module.ImportReference(execType.GetMethod("Exec", 1, [typeof(Delegate), typeof(object[])])!);
            var genInsMethod = new GenericInstanceMethod(genMethod);
            genInsMethod.GenericArguments.Add(originalMethod.ReturnType);
            exec = genInsMethod;
        }
        var argCount = originalMethod.Parameters.Count;
        var paramTypes = originalMethod.Parameters.Select(a => a.ParameterType).ToList();
        if (originalMethod.ReturnType.FullName != voidRef.FullName)
        {
            paramTypes.Add(originalMethod.ReturnType);
        }
        var funcType = originalMethod.ReturnType.FullName == voidRef.FullName ? Type.GetType($"System.Action`{paramTypes.Count}")! : Type.GetType($"System.Func`{paramTypes.Count}")!;
        if (funcType == null)
        {
            funcType = typeof(Action);
        }
        var funcRef = (TypeReference)originalMethod.Module.ImportReference(funcType).Resolve();
        if (paramTypes.Count > 0)
        {
            funcRef = funcRef.MakeGenericInstanceType(paramTypes.ToArray());
        }
        
        var cons = funcRef.Resolve().GetConstructors();
        var funcCon = originalMethod.Module.ImportReference(cons.First());

        ilp.Append(ilp.Create(OpCodes.Newobj, execCon));
        if (originalMethod.IsStatic)
        {
            ilp.Append(ilp.Create(OpCodes.Ldnull));
        }
        else
        {
            ilp.Append(ilp.Create(OpCodes.Ldarg_0));
        }

        ilp.Append(ilp.Create(OpCodes.Ldftn, clonedMethod));
        ilp.Append(ilp.Create(OpCodes.Newobj, funcCon));

        //for (int i = method.IsStatic?0:1; i < argCount; i++) {
        //    ilp.Append(ilp.Create(OpCodes.Ldarg, i));
        //}
        ilp.Append(ilp.Create(OpCodes.Ldc_I4, argCount));
        ilp.Append(ilp.Create(OpCodes.Newarr, objRef));

        for (int i = 0; i < argCount; i++)
        {
            var p = originalMethod.Parameters[i];
            ilp.Append(ilp.Create(OpCodes.Dup));
            ilp.Append(ilp.Create(OpCodes.Ldc_I4, i));
            ilp.Append(ilp.Create(OpCodes.Ldarg, originalMethod.IsStatic ? i : i + 1));
            if (p.ParameterType.IsValueType)
            {
                ilp.Append(ilp.Create(OpCodes.Box, p.ParameterType));
            }
            ilp.Append(ilp.Create(OpCodes.Stelem_Ref));
        }



        ilp.Append(ilp.Create(OpCodes.Call, exec));
        ilp.Append(ilp.Create(OpCodes.Ret));


        originalMethod.DebugInformation.SequencePoints.Clear();
        originalMethod.DeclaringType.Methods.Add(clonedMethod);
    }
    catch (Exception ex) {
        Console.WriteLine(ex);
    }
    
}
bool IsInterface(TypeReference type)
{
    var i = type.Module.ImportReference(typeof(IAsyncAopAttribute));
    var a = type.Module.ImportReference(typeof(Attribute));
    var stack = new Stack<TypeReference>([type]);
    while (stack.Count > 0) { 
        var tref = stack.Pop();
        var tdef = tref.Resolve();
        if (tdef.Interfaces.Any(a => a.InterfaceType.FullName == i.FullName))
        {
            return true;
        }
        if (tdef.BaseType.FullName!=a.FullName && tdef.BaseType != null)
        {
            stack.Push(tdef.BaseType);
        }
    }
    return false;
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