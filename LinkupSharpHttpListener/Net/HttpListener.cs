// TODO: Logging.
using System;
using System.Collections;
using System.Security.Cryptography.X509Certificates;
using System.IO;
using System.Net;

namespace LinkupSharpHttpListener.Net
{
    public sealed class HttpListener : IDisposable
    {
        AuthenticationSchemes auth_schemes;
        HttpListenerPrefixCollection prefixes;
        AuthenticationSchemeSelector auth_selector;
        string realm;
        bool ignore_write_exceptions;
        bool unsafe_ntlm_auth;
        bool listening;
        bool disposed;

        Hashtable registry;   // Dictionary<HttpListenerContext,HttpListenerContext> 
        Hashtable connections;
        private X509Certificate2 _certificate;

        public Action<HttpListenerContext> OnContext { get; set; }

        public HttpListener()
        {
            prefixes = new HttpListenerPrefixCollection(this);
            registry = new Hashtable();
            connections = Hashtable.Synchronized(new Hashtable());
            auth_schemes = AuthenticationSchemes.Anonymous;
        }

        public HttpListener(X509Certificate2 certificate)
            : this()
        {
            _certificate = certificate;
        }

        public HttpListener(string certificateLocation)
            :this()
        {
            LoadCertificateAndKey(certificateLocation);
        }

        // TODO: Digest, NTLM and Negotiate require ControlPrincipal
        public AuthenticationSchemes AuthenticationSchemes
        {
            get { return auth_schemes; }
            set
            {
                CheckDisposed();
                auth_schemes = value;
            }
        }

        public AuthenticationSchemeSelector AuthenticationSchemeSelectorDelegate
        {
            get { return auth_selector; }
            set
            {
                CheckDisposed();
                auth_selector = value;
            }
        }

        public bool IgnoreWriteExceptions
        {
            get { return ignore_write_exceptions; }
            set
            {
                CheckDisposed();
                ignore_write_exceptions = value;
            }
        }

        public bool IsListening
        {
            get { return listening; }
        }

        public static bool IsSupported
        {
            get { return true; }
        }

        public HttpListenerPrefixCollection Prefixes
        {
            get
            {
                CheckDisposed();
                return prefixes;
            }
        }

        // TODO: use this
        public string Realm
        {
            get { return realm; }
            set
            {
                CheckDisposed();
                realm = value;
            }
        }

        public bool UnsafeConnectionNtlmAuthentication
        {
            get { return unsafe_ntlm_auth; }
            set
            {
                CheckDisposed();
                unsafe_ntlm_auth = value;
            }
        }

        void LoadCertificateAndKey(string certificateLocation)
        {
            // Actually load the certificate
            try
            {
                if (!File.Exists(certificateLocation))
                {
                    return;
                }

                X509Certificate2 localCert = new X509Certificate2(certificateLocation);
                //localCert.PrivateKey = PrivateKey.CreateFromFile(pvk_file).RSA;
                if (localCert.PrivateKey == null)
                {
                    return;
                }

                _certificate = localCert;
            }
            catch (Exception e)
            {
                // ignore errors
            }
        }

        //internal IMonoSslStream CreateSslStream(Stream innerStream, bool ownsStream, MSI.MonoRemoteCertificateValidationCallback callback)
        //{
        //    lock (registry)
        //    {
        //        if (tlsProvider == null)
        //            tlsProvider = MonoTlsProviderFactory.GetProviderInternal();
        //        if (tlsSettings == null)
        //            tlsSettings = MSI.MonoTlsSettings.CopyDefaultSettings();
        //        if (tlsSettings.RemoteCertificateValidationCallback == null)
        //            tlsSettings.RemoteCertificateValidationCallback = callback;
        //        return tlsProvider.CreateSslStream(innerStream, ownsStream, tlsSettings);
        //    }
        //}

        internal X509Certificate2 Certificate
        {
            get { return _certificate; }
        }

        public void Abort()
        {
            if (disposed)
                return;

            if (!listening)
            {
                return;
            }

            Close(true);
        }

        public void Close()
        {
            if (disposed)
                return;

            if (!listening)
            {
                disposed = true;
                return;
            }

            Close(true);
            disposed = true;
        }

        void Close(bool force)
        {
            CheckDisposed();
            EndPointManager.RemoveListener(this);
            Cleanup(force);
        }

        void Cleanup(bool close_existing)
        {
            lock (registry)
            {
                if (close_existing)
                {
                    // Need to copy this since closing will call UnregisterContext
                    ICollection keys = registry.Keys;
                    var all = new HttpListenerContext[keys.Count];
                    keys.CopyTo(all, 0);
                    registry.Clear();
                    for (int i = all.Length - 1; i >= 0; i--)
                        all[i].Connection.Close(true);
                }

                lock (connections.SyncRoot)
                {
                    ICollection keys = connections.Keys;
                    var conns = new HttpConnection[keys.Count];
                    keys.CopyTo(conns, 0);
                    connections.Clear();
                    for (int i = conns.Length - 1; i >= 0; i--)
                        conns[i].Close(true);
                }
            }
        }

        internal AuthenticationSchemes SelectAuthenticationScheme(HttpListenerContext context)
        {
            if (AuthenticationSchemeSelectorDelegate != null)
                return AuthenticationSchemeSelectorDelegate(context.Request);
            else
                return auth_schemes;
        }

        public void Start()
        {
            CheckDisposed();
            if (listening)
                return;

            EndPointManager.AddListener(this);
            listening = true;
        }

        public void Stop()
        {
            CheckDisposed();
            listening = false;
            Close(false);
        }

        void IDisposable.Dispose()
        {
            if (disposed)
                return;

            Close(true); //TODO: Should we force here or not?
            disposed = true;
        }

        internal void CheckDisposed()
        {
            if (disposed)
                throw new ObjectDisposedException(GetType().ToString());
        }

        internal void RegisterContext(HttpListenerContext context)
        {
            if (OnContext != null && IsListening)
            {
                OnContext(context);
            }

            lock (registry)
                registry[context] = context;
        }

        internal void UnregisterContext(HttpListenerContext context)
        {
            lock (registry)
                registry.Remove(context);
        }

        internal void AddConnection(HttpConnection cnc)
        {
            connections[cnc] = cnc;
        }

        internal void RemoveConnection(HttpConnection cnc)
        {
            connections.Remove(cnc);
        }
    }
}