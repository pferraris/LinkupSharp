using LinkupSharp.Channels;
using LinkupSharp.Modules;
using LinkupSharp.Security.Authentication;
using LinkupSharp.Security.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace LinkupSharp.Management
{
    public static class ExtensionHelper
    {
        private static List<Extension<IChannelListener>> listeners;
        private static List<Extension<IAuthenticator>> authenticators;
        private static List<Extension<IAuthorizer>> authorizers;
        private static List<Extension<IServerModule>> modules;

        public static IEnumerable<Extension<IChannelListener>> Listeners { get { return listeners.ToArray(); } }
        public static IEnumerable<Extension<IAuthenticator>> Authenticators { get { return authenticators.ToArray(); } }
        public static IEnumerable<Extension<IAuthorizer>> Authorizers { get { return authorizers.ToArray(); } }
        public static IEnumerable<Extension<IServerModule>> Modules { get { return modules.ToArray(); } }

        static ExtensionHelper()
        {
            listeners = GetBuiltInExtensions<IChannelListener>().ToList();
            authenticators = GetBuiltInExtensions<IAuthenticator>().ToList();
            authorizers = GetBuiltInExtensions<IAuthorizer>().ToList();
            modules = GetBuiltInExtensions<IServerModule>().ToList();
        }

        public static Extension<IChannelListener> GetListener(Type type)
        {
            return listeners.LastOrDefault(x => x.FullName.Equals(type.FullName));
        }

        public static Extension<IAuthenticator> GetAuthenticator(Type type)
        {
            return authenticators.LastOrDefault(x => x.FullName.Equals(type.FullName));
        }

        public static Extension<IAuthorizer> GetAuthorizer(Type type)
        {
            return authorizers.LastOrDefault(x => x.FullName.Equals(type.FullName));
        }

        public static Extension<IServerModule> GetModule(Type type)
        {
            return modules.LastOrDefault(x => x.FullName.Equals(type.FullName));
        }

        public static Extension<IChannelListener> GetListener(string name)
        {
            return listeners.LastOrDefault(x => x.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
        }

        public static Extension<IAuthenticator> GetAuthenticator(string name)
        {
            return authenticators.LastOrDefault(x => x.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
        }

        public static Extension<IAuthorizer> GetAuthorizer(string name)
        {
            return authorizers.LastOrDefault(x => x.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
        }

        public static Extension<IServerModule> GetModule(string name)
        {
            return modules.LastOrDefault(x => x.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
        }

        public static void LoadAssembly(byte[] content)
        {
            var assembly = Assembly.Load(content);
            listeners.AddRange(assembly.GetTypes<IChannelListener>().Select(x => new Extension<IChannelListener>(x, false)));
            authenticators.AddRange(assembly.GetTypes<IAuthenticator>().Select(x => new Extension<IAuthenticator>(x, false)));
            authorizers.AddRange(assembly.GetTypes<IAuthorizer>().Select(x => new Extension<IAuthorizer>(x, false)));
            modules.AddRange(assembly.GetTypes<IServerModule>().Select(x => new Extension<IServerModule>(x, false)));
        }

        private static IEnumerable<Extension<T>> GetBuiltInExtensions<T>()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                foreach (var type in assembly.GetTypes<T>())
                    yield return new Extension<T>(type, true);
        }

        private static IEnumerable<Type> GetTypes<T>(this Assembly assembly)
        {
            foreach (var type in assembly.GetTypes())
                if (typeof(T).IsAssignableFrom(type) &&
                    type.IsClass &&
                    !type.IsAbstract &&
                    !type.ContainsGenericParameters &&
                    type.GetConstructor(Type.EmptyTypes) != null)
                    yield return type;
        }
    }
}
