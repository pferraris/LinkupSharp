using System;
using System.Collections.Generic;

namespace LinkupSharp.Management
{
    public static class DependencyHelper
    {
        public static IEnumerable<Type> GetClasses<T>()
        {
            var result = new List<Type>();
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                foreach (var type in assembly.GetTypes())
                    if (typeof(T).IsAssignableFrom(type) && type.IsClass && !type.IsAbstract)
                        if (type.GetConstructor(Type.EmptyTypes) != null)
                            result.Add(type);
            return result;
        }
    }
}
