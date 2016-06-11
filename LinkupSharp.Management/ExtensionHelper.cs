using LinkupSharp.Channels;
using LinkupSharp.Modules;
using LinkupSharp.Security.Authentication;
using LinkupSharp.Security.Authorization;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.IO.Compression;
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
        public static IFileStorage ZipStorage { get; private set; }

        static ExtensionHelper()
        {
            listeners = GetBuiltInExtensions<IChannelListener>().ToList();
            authenticators = GetBuiltInExtensions<IAuthenticator>().ToList();
            authorizers = GetBuiltInExtensions<IAuthorizer>().ToList();
            modules = GetBuiltInExtensions<IServerModule>().ToList();
            ZipStorage = new FileSystemStorage(ConfigurationManager.AppSettings["FileStorage"] ?? "Extensions");
            ZipStorage.Created += ZipStorage_Created;
            ZipStorage.Deleted += ZipStorage_Deleted;
            foreach (var filename in ZipStorage.List())
                LoadZipFile(ZipStorage.Get(filename));
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

        public static Extension<IChannelListener> GetListener(string type)
        {
            if (type.Contains("."))
                return listeners.LastOrDefault(x => x.FullName.Equals(type, StringComparison.InvariantCultureIgnoreCase));
            else
                return listeners.LastOrDefault(x => x.Name.Equals(type, StringComparison.InvariantCultureIgnoreCase));
        }

        public static Extension<IAuthenticator> GetAuthenticator(string type)
        {
            if (type.Contains("."))
                return authenticators.LastOrDefault(x => x.FullName.Equals(type, StringComparison.InvariantCultureIgnoreCase));
            else
                return authenticators.LastOrDefault(x => x.Name.Equals(type, StringComparison.InvariantCultureIgnoreCase));
        }

        public static Extension<IAuthorizer> GetAuthorizer(string type)
        {
            if (type.Contains("."))
                return authorizers.LastOrDefault(x => x.FullName.Equals(type, StringComparison.InvariantCultureIgnoreCase));
            else
                return authorizers.LastOrDefault(x => x.Name.Equals(type, StringComparison.InvariantCultureIgnoreCase));
        }

        public static Extension<IServerModule> GetModule(string type)
        {
            if (type.Contains("."))
                return modules.LastOrDefault(x => x.FullName.Equals(type, StringComparison.InvariantCultureIgnoreCase));
            else
                return modules.LastOrDefault(x => x.Name.Equals(type, StringComparison.InvariantCultureIgnoreCase));
        }

        private static void ZipStorage_Created(object sender, FileEventArgs e)
        {
            LoadZipFile(e.Content);
        }

        private static void ZipStorage_Deleted(object sender, FileEventArgs e)
        {

        }

        private static void LoadZipFile(byte[] content)
        {
            var file = new ZipArchive(new MemoryStream(content));
            var assemblies = new List<Assembly>();
            foreach (var entry in file.Entries)
                if (Path.GetExtension(entry.Name) == ".dll")
                    using (var stream = entry.Open())
                    {
                        var buffer = new byte[1024 * 1024 * 100];
                        int count = stream.Read(buffer, 0, buffer.Length);
                        assemblies.Add(AppDomain.CurrentDomain.Load(buffer.Take(count).ToArray()));
                    }
            foreach (var assembly in assemblies)
                LoadAssembly(assembly);
        }

        public static void LoadAssembly(Assembly assembly)
        {
            try
            {
                listeners.AddRange(assembly.GetTypes<IChannelListener>().Select(x => new Extension<IChannelListener>(x, false)));
                authenticators.AddRange(assembly.GetTypes<IAuthenticator>().Select(x => new Extension<IAuthenticator>(x, false)));
                authorizers.AddRange(assembly.GetTypes<IAuthorizer>().Select(x => new Extension<IAuthorizer>(x, false)));
                modules.AddRange(assembly.GetTypes<IServerModule>().Select(x => new Extension<IServerModule>(x, false)));
            }
            catch (Exception ex)
            {

            }
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

        public static bool TypeEquals(this object source, string name)
        {
            if (name.Contains("."))
            {
                if (source.GetType().FullName.Equals(name, StringComparison.InvariantCultureIgnoreCase))
                    return true;
            }
            else
            {
                if (source.GetType().Name.Equals(name, StringComparison.InvariantCultureIgnoreCase))
                    return true;
            }
            return false;
        }
    }
}
