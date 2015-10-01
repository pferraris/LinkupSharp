#region License
/*
 * The MIT License (MIT)
 *
 * Copyright (c) 2015 Pablo Ferraris
 *
 * Permission is hereby granted, free of charge, to any person
 * obtaining a copy of this software and associated documentation
 * files (the "Software"), to deal in the Software without
 * restriction, including without limitation the rights to use,
 * copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following
 * conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
 * OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
 * HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
 * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
 * OTHER DEALINGS IN THE SOFTWARE.
*/
#endregion License

using log4net;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace LinkupSharp
{
    public class Packet
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(Packet));

        public Id Sender { get; set; }
        public Id Recipient { get; set; }
        public string Content { get; set; }
        public string TypeName { get; set; }

        internal Type ContentType { get { return GetType(TypeName); } }

        public Packet()
        {
        }

        public Packet(object content)
        {
            SetContent(content, content.GetType());
        }

        public void SetContent<T>(T content)
        {
            SetContent(content, typeof(T));
        }

        public void SetContent(object content, Type type)
        {
            Content = JsonConvert.SerializeObject(content);
            TypeName = GetTypeName(type);
            RegisterType(type);
        }

        public object GetContent()
        {
            try
            {
                return JsonConvert.DeserializeObject(Content, ContentType);
            }
            catch (Exception ex)
            {
                log.Error(string.Format("Error getting packet content: {0}", this), ex);
                return default(object);
            }
        }

        public T GetContent<T>()
        {
            try
            {
                RegisterType(typeof(T));
                return JsonConvert.DeserializeObject<T>(Content);
            }
            catch (Exception ex)
            {
                log.Error(string.Format("Error getting packet content: {0}", this), ex);
                return default(T);
            }
        }

        public bool Is<T>()
        {
            return Is(typeof(T));
        }

        public bool Is(Type type)
        {
            RegisterType(type);
            return type.IsAssignableFrom(ContentType);
        }

        #region RegisteredTypes

        private static Dictionary<string, Type> registeredTypes = new Dictionary<string, Type>();

        public static void RegisterType<T>()
        {
            RegisterType(typeof(T));
        }

        public static void RegisterType(Type type)
        {
            lock (registeredTypes)
                if (!registeredTypes.ContainsKey(GetTypeName(type)))
                    registeredTypes.Add(GetTypeName(type), type);
        }

        private static string GetTypeName(Type type)
        {
            return type.FullName;
        }

        private Type GetType(string alias)
        {
            if (registeredTypes.ContainsKey(alias))
                return registeredTypes[alias];
            else
                return typeof(object);
        }

        #endregion RegisteredTypes

        public override string ToString()
        {
            return String.Format("{0}: ({1}) {2}", Sender, TypeName, Content);
        }
    }
}
