using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ilk_projem.Services;

public sealed class AdMobSsvVerifier
{
    private const string GoogleVerifierKeysUrl =
        "https://www.gstatic.com/admob/reward/verifier-keys.json";
    private static readonly SemaphoreSlim KeyLock = new(1, 1);
    private static Dictionary<string, string> _cachedKeys = new();
    private static DateTime _keysExpireAt = DateTime.MinValue;

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AdMobSsvVerifier> _logger;

    public AdMobSsvVerifier(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<AdMobSsvVerifier> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<VerifiedAdReward?> VerifyAsync(
        HttpRequest request,
        CancellationToken cancellationToken = default)
    {
        var rawQuery = request.QueryString.Value?.TrimStart('?') ?? "";
        var signatureMarker = rawQuery.IndexOf("&signature=", StringComparison.Ordinal);
        if (signatureMarker <= 0)
            return null;

        var signedContent = rawQuery[..signatureMarker];
        var signature = request.Query["signature"].ToString();
        var keyId = request.Query["key_id"].ToString();
        if (string.IsNullOrWhiteSpace(signature) || string.IsNullOrWhiteSpace(keyId))
            return null;

        var keys = await GetVerifierKeysAsync(cancellationToken);
        if (!keys.TryGetValue(keyId, out var pem))
        {
            await RefreshVerifierKeysAsync(cancellationToken);
            keys = _cachedKeys;
            if (!keys.TryGetValue(keyId, out pem))
                return null;
        }

        byte[] signatureBytes;
        try
        {
            signatureBytes = DecodeBase64Url(signature);
        }
        catch (FormatException)
        {
            return null;
        }

        using var ecdsa = ECDsa.Create();
        ecdsa.ImportFromPem(pem);
        var verified = ecdsa.VerifyData(
            Encoding.UTF8.GetBytes(signedContent),
            signatureBytes,
            HashAlgorithmName.SHA256,
            DSASignatureFormat.Rfc3279DerSequence);
        if (!verified) return null;

        var userId = request.Query["user_id"].ToString();
        var customData = request.Query["custom_data"].ToString();
        var transactionId = request.Query["transaction_id"].ToString();
        var adUnitId = request.Query["ad_unit"].ToString();
        _ = long.TryParse(request.Query["reward_amount"], out var rewardAmount);

        var allowedAdUnits = _configuration
            .GetSection("AdMob:RewardedAdUnitIds")
            .Get<string[]>() ?? [];
        if (allowedAdUnits.Length == 0
            || !allowedAdUnits.Contains(adUnitId, StringComparer.Ordinal))
        {
            _logger.LogWarning("AdMob SSV rejected unknown ad unit {AdUnitId}", adUnitId);
            return null;
        }

        if (string.IsNullOrWhiteSpace(userId)
            || string.IsNullOrWhiteSpace(transactionId)
            || customData != "safeguardian-12h")
            return null;

        return new VerifiedAdReward(userId, transactionId, adUnitId, rewardAmount);
    }

    private async Task<IReadOnlyDictionary<string, string>> GetVerifierKeysAsync(
        CancellationToken cancellationToken)
    {
        if (_cachedKeys.Count > 0 && _keysExpireAt > DateTime.UtcNow)
            return _cachedKeys;

        await RefreshVerifierKeysAsync(cancellationToken);
        return _cachedKeys;
    }

    private async Task RefreshVerifierKeysAsync(CancellationToken cancellationToken)
    {
        await KeyLock.WaitAsync(cancellationToken);
        try
        {
            if (_cachedKeys.Count > 0 && _keysExpireAt > DateTime.UtcNow)
                return;

            using var client = _httpClientFactory.CreateClient();
            using var response = await client.GetAsync(GoogleVerifierKeysUrl, cancellationToken);
            response.EnsureSuccessStatusCode();
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            var refreshed = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var key in document.RootElement.GetProperty("keys").EnumerateArray())
            {
                var keyId = key.GetProperty("keyId").ToString();
                var pem = key.GetProperty("pem").GetString() ?? "";
                if (keyId.Length > 0 && pem.Length > 0)
                    refreshed[keyId] = pem;
            }

            _cachedKeys = refreshed;
            _keysExpireAt = DateTime.UtcNow.AddHours(12);
        }
        finally
        {
            KeyLock.Release();
        }
    }

    private static byte[] DecodeBase64Url(string value)
    {
        var normalized = value.Replace('-', '+').Replace('_', '/');
        normalized = normalized.PadRight(normalized.Length + (4 - normalized.Length % 4) % 4, '=');
        return Convert.FromBase64String(normalized);
    }
}

public sealed record VerifiedAdReward(
    string ElderlyId,
    string TransactionId,
    string AdUnitId,
    long RewardAmount);
