namespace eAccountingServer.Domain.ValueObjects;
public sealed record Database(
    string Server,
    string DatabaseName,
    string Username,
    string Password);
