namespace mDNSDiscovery.WebApp.Services;

/// <summary>
/// Interface for vendor-specific device parsers that can query and parse device information
/// </summary>
public interface IDeviceParser
{
    /// <summary>
    /// Determines if this parser can handle the given device based on vendor or service type
    /// </summary>
    bool CanParse(DeviceInfo device);

    /// <summary>
    /// Queries the device and returns vendor-specific parsed information
    /// </summary>
    /// <param name="device">The device to query</param>
    /// <param name="cancellationToken">Cancellation token to abort the operation if the request is cancelled</param>
    /// <returns>Parsed device information or null if parsing failed</returns>
    Task<object?> QueryDeviceAsync(DeviceInfo device, CancellationToken cancellationToken = default);
}
