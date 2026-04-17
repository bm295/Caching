using System.Net;
using System.Net.Http.Json;
using DistributedCache.Api.Models;
using Microsoft.Extensions.Options;

namespace DistributedCache.Api.Services;

public sealed class PeerNodeClient(
    IHttpClientFactory httpClientFactory,
    IOptions<CacheClusterOptions> options,
    ILogger<PeerNodeClient> logger) : IPeerNodeClient
{
    private readonly CacheClusterOptions _options = options.Value;

    public async Task<bool> SetAsync(PeerNode peer, string key, string value, TimeSpan? ttl, CancellationToken cancellationToken)
    {
        var client = CreateClient(peer);
        var response = await client.PutAsJsonAsync($"/internal/cache/{Uri.EscapeDataString(key)}", new
        {
            value,
            ttlSeconds = ttl.HasValue ? (int?)Math.Ceiling(ttl.Value.TotalSeconds) : null
        }, cancellationToken);

        return response.IsSuccessStatusCode;
    }

    public async Task<CacheReadResult?> GetAsync(PeerNode peer, string key, CancellationToken cancellationToken)
    {
        var client = CreateClient(peer);

        using var response = await client.GetAsync($"/internal/cache/{Uri.EscapeDataString(key)}", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Peer read failed from {Peer} for key {Key} ({Code})", peer.NodeId, key, response.StatusCode);
            return null;
        }

        var payload = await response.Content.ReadFromJsonAsync<CacheReadResult>(cancellationToken: cancellationToken);
        return payload;
    }

    public async Task<bool> DeleteAsync(PeerNode peer, string key, CancellationToken cancellationToken)
    {
        var client = CreateClient(peer);
        using var response = await client.DeleteAsync($"/internal/cache/{Uri.EscapeDataString(key)}", cancellationToken);
        return response.IsSuccessStatusCode;
    }

    private HttpClient CreateClient(PeerNode peer)
    {
        var client = httpClientFactory.CreateClient(nameof(PeerNodeClient));
        client.BaseAddress = peer.BaseAddress;
        client.Timeout = TimeSpan.FromMilliseconds(Math.Max(200, _options.RequestTimeoutMilliseconds));
        return client;
    }
}
