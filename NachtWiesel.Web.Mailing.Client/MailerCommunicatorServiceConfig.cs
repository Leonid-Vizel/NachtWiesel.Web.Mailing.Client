namespace NachtWiesel.Web.Mailing.Client;

public sealed class MailerCommunicatorServiceConfig
{
    public bool Disabled { get; set; }
    public string Host { get; set; } = null!;
    public int Port { get; set; }
}
