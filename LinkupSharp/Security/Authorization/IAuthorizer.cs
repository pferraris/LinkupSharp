namespace LinkupSharp.Security.Authorization
{
    public interface IAuthorizer
    {
        bool IsAuthorized(Id id, params object[] roles);
    }
}
