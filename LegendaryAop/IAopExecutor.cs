﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace LegendaryAop
{
    public interface IAopExecutor
    {
        T? Exec<T>(Delegate method, params object[] parameters);
    }
}
