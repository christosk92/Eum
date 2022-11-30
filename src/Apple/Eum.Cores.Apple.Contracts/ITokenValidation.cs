namespace Eum.Cores.Apple.Contracts
{
    public interface ITokenValidation
    {
        Task<bool> ValidateMediaTokenAsync(string developerToken,
            string accessToken, CancellationToken ct = default);
        Task<bool> ValidateDeveloperTokenAsync(string developerToken, CancellationToken ct = default);
    }
}
