using System.Net.Mail;

namespace OpenQMS.Services;

public class EmailOptions
{
    public const string SectionName = "Email";

    public string? From { get; set; }

    public string? PublicBaseUrl { get; set; }

    public string? Host { get; set; }

    public int Port { get; set; } = 587;

    public string? UserName { get; set; }

    public string? Password { get; set; }

    public bool UseSsl { get; set; } = true;

    public bool IsValidForProduction()
    {
        return !string.IsNullOrWhiteSpace(From)
            && MailAddress.TryCreate(From, out _)
            && !string.IsNullOrWhiteSpace(Host)
            && Uri.TryCreate(PublicBaseUrl, UriKind.Absolute, out var publicUri)
            && publicUri.Scheme == Uri.UriSchemeHttps
            && publicUri.AbsolutePath == "/"
            && string.IsNullOrEmpty(publicUri.UserInfo)
            && string.IsNullOrEmpty(publicUri.Query)
            && string.IsNullOrEmpty(publicUri.Fragment)
            && Port is > 0 and <= 65535
            && UseSsl
            && (string.IsNullOrWhiteSpace(UserName) == string.IsNullOrWhiteSpace(Password));
    }
}
