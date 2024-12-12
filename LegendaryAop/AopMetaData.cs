using System;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LegendaryAop
{
    internal class AopMetaData : IAopMetaData
    {
        private static readonly Regex reg=new Regex(@"^(?:get_|set_)(\S+)",RegexOptions.Compiled);
        private Func<object?, object[], Task<object?>> _next;
        public AopMetaData(MethodInfo method,object? obj,Func<object?, object[], Task<object?>> next, object[] parameters)
        {
            Method = method;
            _next = next;
            Parameters = parameters;
            Obj= obj;
        }
        public object[] Parameters { get; set; }

        public MethodInfo Method { get; init;}
        public object? Obj { get; init;}

        bool getProperty()
        {
            if (!Method.IsSpecialName)
            {
                return false;
            }
            var match = reg.Match(Method.Name);
            if (match == null)
            {
                return false;
            }
            var name = match.Groups[1].Value;
            var prop = Method.DeclaringType!.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
            if (prop == null)
            {
                return false;
            }
            property = prop;
            return true;
        }
        bool? isProperty;
        public bool IsProperty
        {
            get
            {
                isProperty ??= getProperty();
                return (bool)isProperty;
            }
        }

        private PropertyInfo? property;
        public PropertyInfo? Property =>IsProperty? property! : null;

        public bool IsGetMethod => IsProperty ? property!.GetMethod == Method : false;

        public Task<object?> NextAsync()
        {
            return _next(Obj,Parameters);
        }
    }
}
