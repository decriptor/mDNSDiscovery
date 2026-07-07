using System.Text.Json;
using mDNSDiscovery.Cli;
using mDNSDiscovery.Cli.Output;

namespace mDNSDiscovery.Tests;

[TestClass]
public sealed class ServiceTypeFormatterTests
{
    private static readonly string[] Types = ["_http._tcp.local.", "_ipp._tcp.local."];

    [TestMethod]
    public void FormatServiceTypes_Ndjson_EmitsOneJsonStringPerLine()
    {
        var result = DeviceFormatter.FormatServiceTypes(Types, OutputFormat.Ndjson);

        var lines = result.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.HasCount(2, lines);
        Assert.AreEqual("\"_http._tcp.local.\"", lines[0].TrimEnd('\r'));
        Assert.AreEqual("\"_ipp._tcp.local.\"", lines[1].TrimEnd('\r'));
    }

    [TestMethod]
    public void FormatServiceTypes_Json_EmitsASingleParseableArray()
    {
        var result = DeviceFormatter.FormatServiceTypes(Types, OutputFormat.Json);

        var parsed = JsonSerializer.Deserialize<string[]>(result);
        Assert.IsNotNull(parsed);
        Assert.HasCount(2, parsed);
        Assert.AreEqual("_http._tcp.local.", parsed[0]);
    }
}
