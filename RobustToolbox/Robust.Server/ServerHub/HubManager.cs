using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Network;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace Robust.Server.ServerHub;

internal sealed class HubManager
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ILogManager _log = default!;

    private ISawmill _sawmill = default!;

    private string? _advertiseUrl;
    private IReadOnlyList<string> _hubUrls = Array.Empty<string>();
    private IReadOnlyList<string> _hubProxies = Array.Empty<string>();
    private TimeSpan _nextPing;
    private TimeSpan _interval;

    private bool _active;
    private readonly HashSet<string> _hubUrlsAdvertisedTo = new HashSet<string>();
    private HttpClient? _httpClient;
    private readonly List<HttpClient> _proxyClients = new();

    public async void Start()
    {
        _sawmill = _log.GetSawmill("hub");
        _sawmill.Info("SF diag: HubManager.Start enter");

        var activate = _cfg.GetCVar(CVars.HubAdvertise);
        _sawmill.Info("SF diag: HubAdvertise = {Activate}", activate);
        if (!activate)
        {
            _sawmill.Info("SF diag: hub advertise disabled, exiting");
            return;
        }

        _sawmill.Info("SF diag: hub.proxies manager value = '{Proxies}'", _cfg.GetCVar(CVars.HubProxies));

        _cfg.OnValueChanged(CVars.HubProxies, s =>
        {
            _sawmill.Info("SF diag: OnValueChanged hub.proxies fired with '{Value}'", s);
            _hubProxies = s.Split(",", StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .ToList();
            RefreshClients();
        }, true);

        _cfg.OnValueChanged(CVars.HubUrls, s => _hubUrls = s.Split(",", StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .ToList()
        , true);

        _cfg.OnValueChanged(CVars.HubAdvertiseInterval, UpdateInterval, true);

        var url = _cfg.GetCVar(CVars.HubServerUrl);
        if (string.IsNullOrEmpty(url))
        {
            _sawmill.Info("hub.server_url unset. Trying to determine IP address automatically...");
            try
            {
                url = await GuessAddress();
            }
            catch (Exception e)
            {
                _sawmill.Log(LogLevel.Error, e, "Failed to determine address for hub advertisement!");
                return;
            }

            _sawmill.Info("Guessed server address to be {ServerHubAddress}", url);
        }

        _active = true;
        _advertiseUrl = url;
    }

    public void AdvertiseNow()
    {
        // Next heartbeat will immediately advertise to hub.
        _nextPing = TimeSpan.Zero;
    }

    private void UpdateInterval(int interval)
    {
        _interval = TimeSpan.FromSeconds(interval);
        RefreshClients();
    }

    private void RefreshClients()
    {
        _httpClient?.Dispose();
        foreach (var c in _proxyClients)
            c.Dispose();
        _proxyClients.Clear();

        var proxies = _hubProxies;
        if (proxies.Count == 0)
            proxies = new[] { "" };

        _sawmill.Info("SF diag: hub proxies loaded: {ProxyCount}, raw={Raw}", proxies.Count, string.Join("|", _hubProxies));
        foreach (var proxy in proxies)
        {
            SocketsHttpHandler socketsHandler;
            if (string.IsNullOrEmpty(proxy))
            {
                socketsHandler = HappyEyeballsHttp.CreateHttpHandler();
            }
            else
            {
                // HappyEyeballs uses a custom ConnectCallback which bypasses Proxy entirely,
                // so proxies get a plain handler.
                socketsHandler = new SocketsHttpHandler();
                try
                {
                    socketsHandler.Proxy = new WebProxy("http://" + proxy);
                    socketsHandler.UseProxy = true;
                }
                catch (Exception e)
                {
                    _sawmill.Warning("Invalid hub proxy {HubProxy}: {Error}", proxy, e.Message);
                }
            }

            // Keep-alive connections stay open for longer than the advertise interval.
            // This way the same HTTPS connection can be re-used.
            socketsHandler.PooledConnectionIdleTimeout = _interval + TimeSpan.FromSeconds(10);

            var client = new HttpClient(socketsHandler);
            HttpClientUserAgent.AddUserAgent(client);
            _proxyClients.Add(client);
        }

        // GuessAddress должен ходить НАПРЯМУЮ (ipify), а не через прокси:
        // иначе определяется выходной IP VPN-провайдера вместо нашего.
        var directHandler = HappyEyeballsHttp.CreateHttpHandler();
        directHandler.PooledConnectionIdleTimeout = _interval + TimeSpan.FromSeconds(10);
        _httpClient = new HttpClient(directHandler);
        HttpClientUserAgent.AddUserAgent(_httpClient);
    }

    public void Heartbeat()
    {
        if (!_active || _advertiseUrl == null)
            return;

        if (_nextPing > _timing.RealTime)
            return;

        _nextPing = _timing.RealTime + _interval;

        SendPing();
    }

    private async void SendPing()
    {
        DebugTools.AssertNotNull(_advertiseUrl);
        DebugTools.AssertNotNull(_httpClient);

        foreach (var hubUrl in _hubUrls)
        {
            var apiUrl = $"{hubUrl}api/servers/advertise";

            try
            {
                // SF fix: прямой пост с таймаутом — HappyEyeballs-клиенты зависали навсегда.
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
                HttpClientUserAgent.AddUserAgent(client);

                _sawmill.Info("SF diag: advertise attempt (direct) to {HubUrl}", hubUrl);
                using var response = await client.PostAsJsonAsync(apiUrl, new AdvertiseRequest(_advertiseUrl!));

                if (!response.IsSuccessStatusCode)
                {
                    var errorText = await response.Content.ReadAsStringAsync();
                    _sawmill.Error("Error status while advertising server: [{StatusCode}] {ErrorText}, from {HubUrl}",
                        response.StatusCode,
                        errorText,
                        hubUrl);
                    continue;
                }

                if (!_hubUrlsAdvertisedTo.Contains(hubUrl))
                {
                    _sawmill.Info("Successfully advertised to {HubUrl} with address {AdvertiseUrl}",
                        hubUrl,
                        _advertiseUrl);
                    _hubUrlsAdvertisedTo.Add(hubUrl);
                }
            }
            catch (Exception e)
            {
                _sawmill.Error("Exception while trying to advertise server to {HubUrl}: {Error}",
                    hubUrl,
                    e.Message);
            }
        }
    }

    private async Task<string?> GuessAddress()
    {
        DebugTools.AssertNotNull(_httpClient);

        var ipifyUrl = _cfg.GetCVar(CVars.HubIpifyUrl);

        var req = await _httpClient!.GetFromJsonAsync<IpResponse>(ipifyUrl);

        return $"ss14://{req!.Ip}:{_cfg.GetCVar(CVars.NetPort)}/";
    }

    private sealed record IpResponse(string Ip);

    // ReSharper disable once NotAccessedPositionalProperty.Local
    private sealed record AdvertiseRequest(string Address);
}
