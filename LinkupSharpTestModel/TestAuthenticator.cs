using System;
using LinkupSharp.Security;
using LinkupSharp.Security.Authentication;

namespace LinkupSharpTestModel
{
    public class TestAuthenticator : IAuthenticator
    {
        public Session Authenticate(SignIn signIn)
        {
            if (signIn == null) return null;
            if (!"tests".Equals(signIn.Id.Domain, StringComparison.InvariantCultureIgnoreCase)) return null;
            return new Session(signIn.Id);
        }
    }
}