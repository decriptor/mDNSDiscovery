using mDNSDiscovery.Cli;
using mDNSDiscovery.Cli.Commands;
using mDNSDiscovery.Cli.Output;

namespace mDNSDiscovery.Tests;

[TestClass]
public sealed class OutputFormatParsingTests
{
    [TestMethod]
    [DataRow("table", OutputFormat.Table)]
    [DataRow("json", OutputFormat.Json)]
    [DataRow("ndjson", OutputFormat.Ndjson)]
    [DataRow("JSON", OutputFormat.Json)]
    public void ParseFormat_AcceptsKnownValuesCaseInsensitively(string input, OutputFormat expected)
        => Assert.AreEqual(expected, CliOptions.ParseFormat(input));

    [TestMethod]
    public void ParseFormat_UnknownValue_ReturnsNull()
        => Assert.IsNull(CliOptions.ParseFormat("xml"));
}

[TestClass]
public sealed class ServiceShorthandTests
{
    [TestMethod]
    public void UnknownShorthands_FlagsTypoedShorthand()
        => Assert.Contains("airply", CliOptions.UnknownShorthands(["airply"]));

    [TestMethod]
    public void UnknownShorthands_IgnoresKnownShorthandsAndFullTypes()
        => Assert.IsEmpty(CliOptions.UnknownShorthands(["airplay", "_custom._tcp.local."]));

    [TestMethod]
    public void SuggestShorthand_ReturnsClosestKnown()
        => Assert.AreEqual("airplay", CliOptions.SuggestShorthand("airply"));

    [TestMethod]
    public void SuggestShorthand_NoCloseMatch_ReturnsNull()
        => Assert.IsNull(CliOptions.SuggestShorthand("zzzzzzz"));
}

[TestClass]
public sealed class OptionValidationTests
{
    [TestMethod]
    [DataRow("-t", "0")]
    [DataRow("-t", "-5")]
    [DataRow("-r", "-1")]
    public void Scan_RejectsInvalidNumericOptions(string option, string value)
    {
        var result = ScanCommand.Create().Parse([option, value]);
        Assert.IsTrue(result.Errors.Count > 0, $"expected a parse error for {option} {value}");
    }

    [TestMethod]
    public void Scan_AcceptsValidNumericOptions()
    {
        var result = ScanCommand.Create().Parse(["-t", "5", "-r", "2"]);
        Assert.IsEmpty(result.Errors);
    }
}

[TestClass]
public sealed class TruncateTests
{
    [TestMethod]
    public void Truncate_TextWithinLimit_Unchanged()
        => Assert.AreEqual("abc", DeviceFormatter.Truncate("abc", 10));

    [TestMethod]
    public void Truncate_LongText_FitsLimitAndEndsWithEllipsis()
    {
        var result = DeviceFormatter.Truncate("abcdefghij", 5);

        Assert.AreEqual(5, result.Length);
        Assert.AreEqual("abcd…", result);
    }
}
