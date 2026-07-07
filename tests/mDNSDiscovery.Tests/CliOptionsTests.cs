using mDNSDiscovery.Cli.Commands;

namespace mDNSDiscovery.Tests;

[TestClass]
public sealed class CliOptionsTests
{
    [TestMethod]
    public void ResolveServiceTypes_EmptyFilters_ReturnsFullCatalog()
    {
        var result = CliOptions.ResolveServiceTypes([]);

        CollectionAssert.AreEqual(ServiceCatalog.Default.ToList(), result.ToList());
    }

    [TestMethod]
    public void ResolveServiceTypes_Shorthand_ExpandsToCatalogEntry()
    {
        var result = CliOptions.ResolveServiceTypes(["airplay"]);

        Assert.Contains("_airplay._tcp.local.", result);
    }

    [TestMethod]
    public void ResolveServiceTypes_FullType_UsedLiterally()
    {
        var result = CliOptions.ResolveServiceTypes(["_custom._udp.local."]);

        Assert.HasCount(1, result);
        Assert.AreEqual("_custom._udp.local.", result[0]);
    }

    [TestMethod]
    public void ResolveServiceTypes_UnknownShorthand_FallsBackToTcpConstruction()
    {
        var result = CliOptions.ResolveServiceTypes(["madeup"]);

        Assert.Contains("_madeup._tcp.local.", result);
    }

    [TestMethod]
    public void ResolveServiceTypes_DuplicateFilters_AreDeduplicated()
    {
        var result = CliOptions.ResolveServiceTypes(["airplay", "airplay"]);

        Assert.HasCount(1, result);
    }
}
