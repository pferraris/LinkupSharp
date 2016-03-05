namespace LinkupSharp.Security.Authorization
{
    public interface IAuthorizer
    {
        bool IsAuthorized(Session session, params object[] roles);
    }
}
