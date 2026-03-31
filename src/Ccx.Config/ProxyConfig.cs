using System.Net;

namespace Ccx.Config;

/// <summary>
/// Configures HTTP proxy for corporate environments.
/// </summary>
public static class ProxyConfig
{
    public static HttpClientHandler CreateHandler(CcxSettings? settings = null)
    {
        var handler = new HttpClientHandler();

        var proxyUrl = settings?.Proxy?.Url
            ?? Environment.GetEnvironmentVariable("HTTPS_PROXY")
            ?? Environment.GetEnvironmentVariable("HTTP_PROXY");

        if (string.IsNullOrEmpty(proxyUrl))
            return handler;

        var proxy = new WebProxy(proxyUrl);

        if (settings?.Proxy?.NoProxy is { Count: > 0 } noProxy)
        {
            proxy.BypassList = noProxy.ToArray();
        }

        handler.Proxy = proxy;
        handler.UseProxy = true;

        return handler;
    }
}
