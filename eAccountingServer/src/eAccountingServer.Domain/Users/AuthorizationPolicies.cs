namespace eAccountingServer.Domain.Users;

/// <summary>
/// Names shared between the place a policy is registered and the endpoints that ask
/// for it, so a rename cannot silently leave an endpoint unprotected.
/// </summary>
public static class AuthorizationPolicies
{
    public const string Admin = "Admin";
}
