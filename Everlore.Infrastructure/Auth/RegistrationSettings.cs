namespace Everlore.Infrastructure.Auth;

public class RegistrationSettings
{
    public RegistrationMode Mode { get; set; } = RegistrationMode.Open;
}

public enum RegistrationMode
{
    Open,
    InviteOnly,
    Disabled
}
