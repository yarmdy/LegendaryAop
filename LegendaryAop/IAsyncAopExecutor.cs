﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace LegendaryAop
{
    public interface IAsyncAopExecutor
    {
        Task<T?> ExecAsync<T>(object? obj, MethodBase method, params object[] parameters);
    }
}
