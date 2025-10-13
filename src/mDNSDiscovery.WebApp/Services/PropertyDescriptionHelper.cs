namespace mDNSDiscovery.WebApp.Services;

public static class PropertyDescriptionHelper
{
    private static readonly Dictionary<string, PropertyDescription> _propertyDescriptions = new()
    {
        // General Properties
        ["txtvers"] = new("TXT Record Version", "Version number of the TXT record format (typically '1')"),
        ["manufacturer"] = new("Manufacturer", "The company that manufactured the device"),
        ["model"] = new("Model", "The model name or number of the device"),
        ["md"] = new("Model", "Short form of model name"),
        ["serialNumber"] = new("Serial Number", "Unique serial number of the device"),
        ["sn"] = new("Serial Number", "Short form of serial number"),
        ["firmware"] = new("Firmware Version", "Version of the device firmware"),
        ["fv"] = new("Firmware Version", "Short form of firmware version"),
        ["version"] = new("Software Version", "Version of the software running on the device"),
        ["vn"] = new("Vendor Name", "Name of the device vendor"),
        ["vs"] = new("Vendor String", "Vendor-specific information string"),

        // AirPlay & RAOP Properties
        ["deviceid"] = new("Device ID", "Unique identifier for the device, typically MAC address"),
        ["features"] = new("Feature Flags", "Hexadecimal bitfield indicating device capabilities and supported features"),
        ["srcvers"] = new("Source Version", "Version of the AirPlay source software"),
        ["acl"] = new("Access Control", "Access control settings for the device"),
        ["rsf"] = new("Receiver Session Flags", "Flags indicating receiver session capabilities"),
        ["flags"] = new("Status Flags", "General status flags for the device"),
        ["protovers"] = new("Protocol Version", "Version of the protocol supported"),
        ["pi"] = new("Pairing ID", "UUID used for device pairing"),
        ["gid"] = new("Group ID", "UUID for device grouping"),
        ["gcgl"] = new("Group Contains Group Leader", "Indicates if group contains the group leader"),
        ["pk"] = new("Public Key", "Public key for secure communications"),
        ["ch"] = new("Channels", "Number of audio channels supported"),
        ["cn"] = new("Codec Names", "Supported audio codec names"),
        ["da"] = new("Digital Audio", "Digital audio format support"),
        ["et"] = new("Encryption Type", "Type of encryption used"),
        ["pw"] = new("Password Required", "Indicates if password authentication is required"),
        ["sr"] = new("Sample Rate", "Audio sample rate in Hz"),
        ["ss"] = new("Sample Size", "Audio sample size in bits"),
        ["tp"] = new("Transport", "Network transport protocol used"),
        ["am"] = new("AirPlay Model", "AirPlay-specific model identifier"),
        ["sf"] = new("Status Flags", "Current status of the device"),

        // HomeKit Properties
        ["id"] = new("Accessory ID", "Unique identifier for HomeKit accessory (MAC address)"),
        ["pv"] = new("Protocol Version", "HomeKit protocol version"),
        ["s#"] = new("State Number", "State number that increments when accessory updates"),
        ["c#"] = new("Configuration Number", "Configuration number that changes when accessory setup changes"),
        ["ci"] = new("Category Identifier", "HomeKit accessory category"),

        // HTTP Service Properties
        ["u"] = new("Username", "Username for HTTP authentication"),
        ["p"] = new("Password", "Password for HTTP authentication"),
        ["path"] = new("Path", "URL path for HTTP service (everything after port number)"),

        // Printer Properties
        ["ty"] = new("Type", "Printer type or description"),
        ["product"] = new("Product", "Printer product name"),
        ["pdl"] = new("Page Description Languages", "Supported page description languages (PDF, PostScript, etc.)"),
        ["qtotal"] = new("Queue Total", "Total number of jobs in print queue"),
        ["rp"] = new("Resource Path", "Resource path for IPP printing"),
        ["adminurl"] = new("Admin URL", "URL for printer administration interface"),
        ["note"] = new("Note", "Administrator notes about the printer"),
        ["priority"] = new("Priority", "Printer priority level"),
        ["UUID"] = new("Unique ID", "Universally unique identifier"),

        // SSH/SFTP Properties
        ["ssh"] = new("SSH Support", "Indicates SSH protocol support"),
        ["sftp"] = new("SFTP Support", "Indicates SFTP (SSH File Transfer Protocol) support"),

        // SMB/File Sharing Properties
        ["sys"] = new("System Type", "Type of operating system"),
        ["wg"] = new("Workgroup", "Windows workgroup name"),
        ["domain"] = new("Domain", "Network domain name"),

        // Chromecast Properties
        ["ve"] = new("Version", "Chromecast firmware version"),
        ["ic"] = new("Icon Path", "Path to device icon"),
        ["fn"] = new("Friendly Name", "User-friendly device name"),
        ["ca"] = new("Capabilities", "Device capability flags"),
        ["st"] = new("Status", "Current device status"),

        // Network Properties
        ["mac"] = new("MAC Address", "Physical network adapter address"),
        ["ip"] = new("IP Address", "Network IP address"),
        ["hostname"] = new("Hostname", "Network hostname"),
    };

    public static PropertyDescription? GetDescription(string propertyKey)
    {
        if (_propertyDescriptions.TryGetValue(propertyKey.ToLowerInvariant(), out var description))
        {
            return description;
        }
        return null;
    }

    public static IEnumerable<PropertyGroup> GetAllPropertyGroups()
    {
        return new[]
        {
            new PropertyGroup
            {
                Name = "General Device Properties",
                Description = "Common properties found across various device types",
                Properties = new[]
                {
                    ("txtvers", _propertyDescriptions["txtvers"]),
                    ("manufacturer", _propertyDescriptions["manufacturer"]),
                    ("model", _propertyDescriptions["model"]),
                    ("md", _propertyDescriptions["md"]),
                    ("serialNumber", _propertyDescriptions["serialNumber"]),
                    ("sn", _propertyDescriptions["sn"]),
                    ("firmware", _propertyDescriptions["firmware"]),
                    ("fv", _propertyDescriptions["fv"]),
                    ("version", _propertyDescriptions["version"]),
                    ("vn", _propertyDescriptions["vn"]),
                    ("vs", _propertyDescriptions["vs"]),
                }
            },
            new PropertyGroup
            {
                Name = "AirPlay & RAOP Properties",
                Description = "Properties specific to Apple AirPlay and Remote Audio Output Protocol devices",
                Properties = new[]
                {
                    ("deviceid", _propertyDescriptions["deviceid"]),
                    ("features", _propertyDescriptions["features"]),
                    ("srcvers", _propertyDescriptions["srcvers"]),
                    ("acl", _propertyDescriptions["acl"]),
                    ("rsf", _propertyDescriptions["rsf"]),
                    ("flags", _propertyDescriptions["flags"]),
                    ("protovers", _propertyDescriptions["protovers"]),
                    ("pi", _propertyDescriptions["pi"]),
                    ("gid", _propertyDescriptions["gid"]),
                    ("gcgl", _propertyDescriptions["gcgl"]),
                    ("pk", _propertyDescriptions["pk"]),
                    ("ch", _propertyDescriptions["ch"]),
                    ("cn", _propertyDescriptions["cn"]),
                    ("da", _propertyDescriptions["da"]),
                    ("et", _propertyDescriptions["et"]),
                    ("pw", _propertyDescriptions["pw"]),
                    ("sr", _propertyDescriptions["sr"]),
                    ("ss", _propertyDescriptions["ss"]),
                    ("tp", _propertyDescriptions["tp"]),
                    ("am", _propertyDescriptions["am"]),
                    ("sf", _propertyDescriptions["sf"]),
                }
            },
            new PropertyGroup
            {
                Name = "HomeKit Properties",
                Description = "Properties used by Apple HomeKit smart home accessories",
                Properties = new[]
                {
                    ("id", _propertyDescriptions["id"]),
                    ("pv", _propertyDescriptions["pv"]),
                    ("s#", _propertyDescriptions["s#"]),
                    ("c#", _propertyDescriptions["c#"]),
                    ("ci", _propertyDescriptions["ci"]),
                }
            },
            new PropertyGroup
            {
                Name = "HTTP Service Properties",
                Description = "Properties for HTTP-based services",
                Properties = new[]
                {
                    ("u", _propertyDescriptions["u"]),
                    ("p", _propertyDescriptions["p"]),
                    ("path", _propertyDescriptions["path"]),
                }
            },
            new PropertyGroup
            {
                Name = "Printer Properties",
                Description = "Properties for network printers and IPP services",
                Properties = new[]
                {
                    ("ty", _propertyDescriptions["ty"]),
                    ("product", _propertyDescriptions["product"]),
                    ("pdl", _propertyDescriptions["pdl"]),
                    ("qtotal", _propertyDescriptions["qtotal"]),
                    ("rp", _propertyDescriptions["rp"]),
                    ("adminurl", _propertyDescriptions["adminurl"]),
                    ("note", _propertyDescriptions["note"]),
                    ("priority", _propertyDescriptions["priority"]),
                    ("UUID", _propertyDescriptions["UUID"]),
                }
            },
            new PropertyGroup
            {
                Name = "Chromecast Properties",
                Description = "Properties specific to Google Chromecast devices",
                Properties = new[]
                {
                    ("ve", _propertyDescriptions["ve"]),
                    ("ic", _propertyDescriptions["ic"]),
                    ("fn", _propertyDescriptions["fn"]),
                    ("ca", _propertyDescriptions["ca"]),
                    ("st", _propertyDescriptions["st"]),
                }
            },
            new PropertyGroup
            {
                Name = "Network Properties",
                Description = "Network-related properties",
                Properties = new[]
                {
                    ("mac", _propertyDescriptions["mac"]),
                    ("ip", _propertyDescriptions["ip"]),
                    ("hostname", _propertyDescriptions["hostname"]),
                }
            },
        };
    }
}

public record PropertyDescription(string Name, string Description);

public record PropertyGroup
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required IEnumerable<(string Key, PropertyDescription Description)> Properties { get; init; }
}
