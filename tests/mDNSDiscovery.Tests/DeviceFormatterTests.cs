using mDNSDiscovery.Cli.Output;

namespace mDNSDiscovery.Tests;

[TestClass]
public sealed class DeviceFormatterTests
{
    [TestMethod]
    public void ShortServiceNames_ShortensDeduplicatesAndSorts()
    {
        var device = new DeviceInfo
        {
            Name = "Mac",
            ServiceType = "_airplay._tcp.local.",
            Endpoints =
            [
                new ServiceEndpoint { ServiceType = "_raop._tcp.local.", Port = 5000 },
                new ServiceEndpoint { ServiceType = "_airplay._tcp.local.", Port = 7000 },
            ],
        };

        var names = DeviceFormatter.ShortServiceNames(device).ToList();

        CollectionAssert.AreEqual(new List<string> { "airplay", "raop" }, names);
    }

    [TestMethod]
    public void ShortServiceNames_IgnoresEmptyServiceTypes()
    {
        var device = new DeviceInfo { Name = "x", ServiceType = "" };

        Assert.IsEmpty(DeviceFormatter.ShortServiceNames(device));
    }
}

[TestClass]
public sealed class ServiceCatalogTests
{
    [TestMethod]
    public void Default_EntriesAreDistinct()
    {
        var distinct = ServiceCatalog.Default.Distinct(StringComparer.OrdinalIgnoreCase).Count();

        Assert.AreEqual(ServiceCatalog.Default.Count, distinct);
    }

    [TestMethod]
    public void Default_EntriesAreWellFormedMdnsTypes()
    {
        foreach (var type in ServiceCatalog.Default)
        {
            Assert.StartsWith("_", type);
            Assert.EndsWith(".local.", type);
        }
    }
}
