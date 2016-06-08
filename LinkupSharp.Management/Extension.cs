using System;
using System.Linq.Expressions;

namespace LinkupSharp.Management
{
    public class Extension<T>
    {
        private Type type;
        private Func<T> constructor;

        public Extension(Type type, bool builtIn)
        {
            this.type = type;
            BuiltIn = builtIn;
            constructor = Expression.Lambda<Func<T>>(Expression.New(type.GetConstructor(Type.EmptyTypes))).Compile();
        }

        public bool BuiltIn { get; private set; }
        public string Name { get { return type.Name; } }
        public string FullName { get { return type.FullName; } }

        public T Create()
        {
            return constructor();
        }
    }
}
