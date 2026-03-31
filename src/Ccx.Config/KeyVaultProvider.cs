namespace Ccx.Config;

/// <summary>
/// Azure Key Vault stub for API key storage.
/// In production, integrate with Azure.Security.KeyVault.Secrets.
/// </summary>
public sealed class KeyVaultProvider
{
    private readonly string? _vaultUri;

    public KeyVaultProvider(string? vaultUri = null)
    {
        _vaultUri = vaultUri ?? Environment.GetEnvironmentVariable("CCX_KEYVAULT_URI");
    }

    public bool IsConfigured => !string.IsNullOrEmpty(_vaultUri);

    public Task<string?> GetSecretAsync(string name, CancellationToken ct = default)
    {
        // Stub: In production, use Azure.Security.KeyVault.Secrets
        // var client = new SecretClient(new Uri(_vaultUri), new DefaultAzureCredential());
        // var secret = await client.GetSecretAsync(name, cancellationToken: ct);
        // return secret.Value.Value;

        // Fallback to environment variable
        var envKey = $"CCX_{name.ToUpperInvariant().Replace('-', '_')}";
        return Task.FromResult(Environment.GetEnvironmentVariable(envKey));
    }

    public Task<string?> GetApiKeyAsync(CancellationToken ct = default)
    {
        return GetSecretAsync("api-key", ct);
    }
}
