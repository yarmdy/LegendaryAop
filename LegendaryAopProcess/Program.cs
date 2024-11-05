using LegendaryAop;
using Mono.Cecil;
using Mono.Cecil.Cil;
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
    .Select(a => new { method = a.reflection, methodMono = a.mono, aops = a.reflection.GetCustomAttributes<AsyncAopAttribute>().Select((b, i) => new { order = i, aop = b }).OrderByDescending(b => b.aop.Sort).ThenByDescending(b => b.order).Select(b => new { reflection = b.aop, mono = a.mono!.CustomAttributes.FirstOrDefault(c => c.AttributeType.FullName == b.aop.GetType().FullName) }).ToArray() })
    .Where(a => a.aops.Any())
    .Select(item =>
    {
        var method = item.method;
        var methodMono = item.methodMono!;
        var aops = item.aops.Select(a =>
        {
            var attr = a.reflection;
            var attrMono = a.mono;
            //这里开始填充il代码，进行aop切面
            var ilp = methodMono.Body.GetILProcessor();
            var logMethod = typeof(Console).GetMethod("WriteLine", new[] { typeof(string) });
            ilp.InsertBefore(methodMono.Body.Instructions.First(),
                ilp.Create(OpCodes.Ldstr, "Method started at: " + DateTime.Now)
            );
            ilp.InsertAfter(methodMono.Body.Instructions.First(),
                ilp.Create(OpCodes.Call, ilp.Body.Method.Module.ImportReference(logMethod))
            );

            return a;
        }).ToArray();

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