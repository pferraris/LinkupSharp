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

using System;

namespace LinkupSharp
{
    public class Id
    {
        public string Username { get; set; }
        public string Domain { get; set; }

        public Id() { }

        public Id(string username, string domain)
        {
            if (string.IsNullOrEmpty(username)) throw new ArgumentException("Username cannot be null");
            if (string.IsNullOrEmpty(domain)) throw new ArgumentException("Domain cannot be null");
            Username = username.ToLower();
            Domain = domain.ToLower();
        }

        public Id(string id)
        {
            if (string.IsNullOrEmpty(id)) throw new ArgumentException("ID cannot be null");
            if (!id.Contains("@")) id = string.Format("{0}@anonymous", id);
            string[] values = id.Split("@".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            Username = values[0].ToLower();
            Domain = values[1].ToLower();
        }

        public override bool Equals(object obj)
        {
            if ((obj == null) || !(obj is Id)) return false;
            return (ToString().Equals(obj.ToString(), StringComparison.OrdinalIgnoreCase));
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override string ToString()
        {
            return string.Format("{0}@{1}", Username, Domain);
        }

        public static bool operator ==(Id a, Id b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(Id a, Id b)
        {
            return !a.Equals(b);
        }
        public static implicit operator Id(string id)
        {
            if (id == null) return null;
            return new Id(id);
        }

        public static implicit operator String(Id id)
        {
            if (id == null) return null;
            return id.ToString();
        }
    }
}
