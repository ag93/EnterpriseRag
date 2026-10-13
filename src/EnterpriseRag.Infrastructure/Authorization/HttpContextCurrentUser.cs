
using System.Security.Claims;
using EnterpriseRag.Application.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace EnterpriseRag.Infrastructure.Authorization;

public class HttpContextCurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IReadOnlyDictionary<string, string> _groupMappings;

    public HttpContextCurrentUser(IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
    {
        _httpContextAccessor = httpContextAccessor;

        _groupMappings = configuration
            .GetSection("EntraId:GroupMappings")
            .GetChildren()
            .Where(section => !string.IsNullOrWhiteSpace(section.Value))
            .ToDictionary(
                section => section.Key,
                section => section.Value!,
                StringComparer.OrdinalIgnoreCase);
    }

    private ClaimsPrincipal? CurrentPrincipal => _httpContextAccessor.HttpContext?.User;
    public bool IsAuthenticated => CurrentPrincipal?.Identity?.IsAuthenticated == true;

    public IReadOnlyCollection<string> Groups =>
        CurrentPrincipal?
            .FindAll("groups")
            .Select(claim => claim.Value)
            .Where(groupId => _groupMappings.ContainsKey(groupId))
            .Select(groupId => _groupMappings[groupId])
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray()
        ?? Array.Empty<string>();
}
