using System;
using System.Reflection;

namespace GuildSaber.Mod.Helpers;

public static class MethodInfoExtensions
{
    extension(MethodInfo self)
    {
        public T ToDelegate<T>() where T : Delegate => (T)Delegate.CreateDelegate(typeof(T), self);
    }
}