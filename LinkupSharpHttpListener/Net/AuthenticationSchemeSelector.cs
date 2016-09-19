using System.Net;

namespace LinkupSharpHttpListener.Net
{
    public delegate AuthenticationSchemes AuthenticationSchemeSelector(HttpListenerRequest httpRequest);
}
