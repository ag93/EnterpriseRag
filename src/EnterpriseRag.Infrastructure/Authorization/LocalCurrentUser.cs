using EnterpriseRag.Application.Abstractions;
using Microsoft.Extensions.Configuration;

namespace EnterpriseRag.Infrastructure.Authorization;

public class LocalCurrentUser : ICurrentUser
{
    public bool IsAuthenticated { get; }

    public IReadOnlyCollection<string> Groups { get; }

    public LocalCurrentUser(IConfiguration configuration)
    {
        IsAuthenticated = bool.TryParse(configuration["LocalIdentity:IsAuthenticated"], out var isAuthenticated)
        && isAuthenticated;

        Groups = configuration
            .GetSection("LocalIdentity:Groups")
            .GetChildren()
            .Select(section => section.Value)
            .Where(group => !string.IsNullOrWhiteSpace(group))
            .Select(group => group!)
            .ToArray();
    }
}
