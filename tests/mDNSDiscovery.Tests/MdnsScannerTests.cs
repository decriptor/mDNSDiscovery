using System.Collections.Concurrent;
using mDNSDiscovery.Tests.Fakes;
using Zeroconf;

namespace mDNSDiscovery.Tests;

[TestClass]
public sealed class MdnsScannerTests
{
    private static MdnsScanner ScannerReturning(params IZeroconfHost[] hosts)
        => new(resolver: (_, _, _, _) => Task.FromResult<IReadOnlyList<IZeroconfHost>>(hosts));

    [TestMethod]
    public async Task ScanAsync_SingleHostWithTwoServices_ProducesOneDeviceWithTwoEndpoints()
    {
        var host = FakeZeroconfHost.Create("Printer", "192.168.1.20",
            ("_ipp._tcp.local.", 631, null),
            ("_http._tcp.local.", 80, null));
        var scanner = ScannerReturning(host);

        var devices = await scanner.ScanAsync(["_ipp._tcp.local.", "_http._tcp.local."], TimeSpan.FromSeconds(1));

        Assert.HasCount(1, devices);
        Assert.HasCount(2, devices[0].Endpoints);
    }

    [TestMethod]
    public async Task ScanAsync_DerivesServiceTypeFromServiceNameNotInstanceKey()
    {
        var host = FakeZeroconfHost.Create("Mac", "192.168.1.10", ("_airplay._tcp.local.", 7000, null));
        var scanner = ScannerReturning(host);

        var devices = await scanner.ScanAsync(["_airplay._tcp.local."], TimeSpan.FromSeconds(1));

        Assert.AreEqual("_airplay._tcp.local.", devices[0].ServiceType);
        Assert.AreEqual("_airplay._tcp.local.", devices[0].Endpoints[0].ServiceType);
    }

    [TestMethod]
    public async Task ScanAsync_TwoResponsesSameNameAndIp_MergeIntoOneDevice()
    {
        var a = FakeZeroconfHost.Create("Mac", "192.168.1.10", ("_airplay._tcp.local.", 7000, null));
        var b = FakeZeroconfHost.Create("Mac", "192.168.1.10", ("_raop._tcp.local.", 5000, null));
        var scanner = ScannerReturning(a, b);

        var devices = await scanner.ScanAsync(["_airplay._tcp.local.", "_raop._tcp.local."], TimeSpan.FromSeconds(1));

        Assert.HasCount(1, devices);
        Assert.HasCount(2, devices[0].Endpoints);
    }

    [TestMethod]
    public async Task ScanAsync_OrdersDevicesByName()
    {
        var scanner = ScannerReturning(
            FakeZeroconfHost.Create("Zebra", "192.168.1.3", ("_http._tcp.local.", 80, null)),
            FakeZeroconfHost.Create("Apple", "192.168.1.1", ("_http._tcp.local.", 80, null)));

        var devices = await scanner.ScanAsync(["_http._tcp.local."], TimeSpan.FromSeconds(1));

        Assert.AreEqual("Apple", devices[0].Name);
        Assert.AreEqual("Zebra", devices[1].Name);
    }

    [TestMethod]
    public async Task ScanAsync_PassesRequestedRetriesToResolver()
    {
        int? captured = null;
        var scanner = new MdnsScanner(resolver: (_, _, retries, _) =>
        {
            captured = retries;
            return Task.FromResult<IReadOnlyList<IZeroconfHost>>([]);
        });

        await scanner.ScanAsync(["_http._tcp.local."], TimeSpan.FromSeconds(1), retries: 4);

        Assert.AreEqual(4, captured);
    }

    [TestMethod]
    public async Task ScanAsync_DuplicateTxtKeysAcrossPropertySets_DoNotThrowAndLastWins()
    {
        var service = new FakeService
        {
            Name = "_http._tcp.local.",
            Port = 80,
            Properties =
            [
                new Dictionary<string, string> { ["k"] = "first" },
                new Dictionary<string, string> { ["k"] = "second" },
            ],
        };
        var host = new FakeZeroconfHost
        {
            DisplayName = "Dup",
            IPAddress = "192.168.1.5",
            Services = new Dictionary<string, IService> { ["Dup._http._tcp.local."] = service },
        };
        var scanner = ScannerReturning(host);

        var devices = await scanner.ScanAsync(["_http._tcp.local."], TimeSpan.FromSeconds(1));

        Assert.AreEqual("second", devices[0].Properties["k"]);
    }

    [TestMethod]
    public void EvictOlderThan_RemovesStaleDevicesButKeepsFresh()
    {
        var cache = new ConcurrentDictionary<string, DeviceInfo>();
        cache["fresh"] = new DeviceInfo { Name = "fresh", LastSeen = DateTime.UtcNow };
        cache["stale"] = new DeviceInfo { Name = "stale", LastSeen = DateTime.UtcNow.AddMinutes(-10) };

        MdnsScanner.EvictOlderThan(cache, TimeSpan.FromMinutes(5));

        Assert.IsTrue(cache.ContainsKey("fresh"));
        Assert.IsFalse(cache.ContainsKey("stale"));
    }

    [TestMethod]
    public async Task ScanAsync_NoServiceTypes_ReturnsEmptyAndDoesNotCallResolver()
    {
        var called = false;
        var scanner = new MdnsScanner(resolver: (_, _, _, _) =>
        {
            called = true;
            return Task.FromResult<IReadOnlyList<IZeroconfHost>>([]);
        });

        var devices = await scanner.ScanAsync([], TimeSpan.FromSeconds(1));

        Assert.IsEmpty(devices);
        Assert.IsFalse(called);
    }
}
