namespace EnterpriseRag.Application.Abstractions
{
    public interface ICitationSupportChecker
    {
        Task<bool> IsSupportedAsync(
            string answer,
            string evidence,
            CancellationToken cancellationToken);
    }
}