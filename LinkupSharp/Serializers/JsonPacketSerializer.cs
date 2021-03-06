﻿#region License
/*
 * The MIT License (MIT)
 *
 * Copyright (c) 2017 Pablo Ferraris
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
using Newtonsoft.Json.Serialization;
using System;
using System.Text;

namespace LinkupSharp.Serializers
{
    public class JsonPacketSerializer : IPacketSerializer
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(JsonPacketSerializer));
        private static readonly JsonSerializerSettings settings;

        static JsonPacketSerializer()
        {
            settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                Formatting = Formatting.None,
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };
        }

        public byte[] Serialize(Packet packet)
        {
            return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(packet, settings));
        }

        public Packet Deserialize(byte[] packet)
        {
            try
            {
                if (packet != null && packet.Length > 0)
                    return JsonConvert.DeserializeObject<Packet>(Encoding.UTF8.GetString(packet));
            }
            catch (Exception ex)
            {
                log.Error("Cannot deserialize JSON packet", ex);
            }
            return null;
        }
    }
}
