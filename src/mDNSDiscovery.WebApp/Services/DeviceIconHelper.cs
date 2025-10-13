namespace mDNSDiscovery.WebApp.Services;

public static class DeviceIconHelper
{
    public static string GetDeviceIcon(DeviceInfo device)
    {
        var name = device.Name.ToLowerInvariant();
        var serviceType = device.ServiceType.ToLowerInvariant();
        var properties = device.Properties.Values.Select(v => v.ToLowerInvariant());

        // Check for specific device types by service
        if (serviceType.Contains("_airplay") || serviceType.Contains("_raop"))
            return "bi-speaker";

        if (serviceType.Contains("_googlecast"))
            return "bi-cast";

        if (serviceType.Contains("_printer") || serviceType.Contains("_ipp"))
            return "bi-printer";

        if (serviceType.Contains("_ssh") || serviceType.Contains("_sftp"))
            return "bi-terminal";

        if (serviceType.Contains("_smb"))
            return "bi-folder-symlink";

        if (serviceType.Contains("_homekit") || serviceType.Contains("_hap"))
            return "bi-house-door";

        if (serviceType.Contains("_sonos"))
            return "bi-speaker-fill";

        if (serviceType.Contains("_spotify-connect"))
            return "bi-music-note-beamed";

        if (serviceType.Contains("_roku") || serviceType.Contains("_ecp"))
            return "bi-tv-fill";

        if (serviceType.Contains("_hue"))
            return "bi-lightbulb";

        if (serviceType.Contains("_companion-link") || serviceType.Contains("_airpointer"))
            return "bi-phone";

        if (serviceType.Contains("_matter"))
            return "bi-house-heart";

        // Check device name for clues
        if (name.Contains("apple") || name.Contains("iphone") || name.Contains("ipad") ||
            name.Contains("macbook") || name.Contains("airpods") || name.Contains("homepod"))
            return "bi-phone";

        if (name.Contains("tv") || name.Contains("roku") || name.Contains("chromecast"))
            return "bi-tv";

        if (name.Contains("speaker") || name.Contains("sonos") || name.Contains("alexa") ||
            name.Contains("echo") || name.Contains("homepod"))
            return "bi-speaker";

        if (name.Contains("printer") || name.Contains("hp") || name.Contains("canon") ||
            name.Contains("epson") || name.Contains("brother"))
            return "bi-printer";

        if (name.Contains("camera") || name.Contains("ring") || name.Contains("nest cam"))
            return "bi-camera-video";

        if (name.Contains("nas") || name.Contains("storage"))
            return "bi-hdd-network";

        if (name.Contains("bridge") || name.Contains("hub"))
            return "bi-hdd-network";

        // Default
        return "bi-router";
    }

    public static string GetDeviceVendor(DeviceInfo device)
    {
        var name = device.Name.ToLowerInvariant();
        var properties = device.Properties;

        // Check properties for vendor info
        if (properties.ContainsKey("manufacturer") || properties.ContainsKey("model"))
        {
            var manufacturer = properties.GetValueOrDefault("manufacturer", "").ToLowerInvariant();
            var model = properties.GetValueOrDefault("model", "").ToLowerInvariant();

            if (manufacturer.Contains("apple") || model.Contains("apple"))
                return "Apple";
            if (manufacturer.Contains("google") || model.Contains("google"))
                return "Google";
            if (manufacturer.Contains("samsung") || model.Contains("samsung"))
                return "Samsung";
            if (manufacturer.Contains("lg"))
                return "LG";
            if (manufacturer.Contains("sony"))
                return "Sony";
            if (manufacturer.Contains("amazon") || model.Contains("echo") || model.Contains("alexa"))
                return "Amazon";
        }

        // Check device name
        if (name.Contains("apple") || name.Contains("iphone") || name.Contains("ipad") ||
            name.Contains("macbook") || name.Contains("airpods") || name.Contains("homepod") ||
            name.Contains("apple tv"))
            return "Apple";

        if (name.Contains("chromecast") || name.Contains("google") || name.Contains("nest"))
            return "Google";

        if (name.Contains("samsung") || name.Contains("galaxy"))
            return "Samsung";

        if (name.Contains("lg ") || name.Contains("[lg]"))
            return "LG";

        if (name.Contains("sony"))
            return "Sony";

        if (name.Contains("alexa") || name.Contains("echo") || name.Contains("fire tv"))
            return "Amazon";

        if (name.Contains("roku"))
            return "Roku";

        if (name.Contains("hp ") || name.Contains("hewlett"))
            return "HP";

        if (name.Contains("canon"))
            return "Canon";

        if (name.Contains("epson"))
            return "Epson";

        if (name.Contains("brother"))
            return "Brother";

        if (name.Contains("synology") || name.Contains("diskstation"))
            return "Synology";

        if (name.Contains("netgear"))
            return "Netgear";

        if (name.Contains("tp-link") || name.Contains("tplink"))
            return "TP-Link";

        if (name.Contains("sonos"))
            return "Sonos";

        if (name.Contains("philips") || name.Contains("hue"))
            return "Philips";

        return "";
    }

    public static string GetDeviceType(DeviceInfo device)
    {
        var serviceType = device.ServiceType.ToLowerInvariant();
        var name = device.Name.ToLowerInvariant();

        if (serviceType.Contains("_printer") || serviceType.Contains("_ipp"))
            return "Printer";

        if (serviceType.Contains("_airplay") || serviceType.Contains("_raop"))
            return "AirPlay Device";

        if (serviceType.Contains("_googlecast"))
            return "Chromecast";

        if (serviceType.Contains("_ssh"))
            return "SSH Server";

        if (serviceType.Contains("_smb"))
            return "File Share";

        if (serviceType.Contains("_homekit") || serviceType.Contains("_hap"))
            return "HomeKit Device";

        if (serviceType.Contains("_sonos"))
            return "Sonos Speaker";

        if (serviceType.Contains("_spotify-connect"))
            return "Spotify Connect Device";

        if (serviceType.Contains("_roku") || serviceType.Contains("_ecp"))
            return "Roku Device";

        if (serviceType.Contains("_hue"))
            return "Philips Hue Bridge";

        if (serviceType.Contains("_companion-link"))
            return "Apple Companion Device";

        if (serviceType.Contains("_matter"))
            return "Matter Device";

        if (name.Contains("tv") || name.Contains("roku"))
            return "Smart TV";

        if (name.Contains("speaker") || name.Contains("sonos"))
            return "Smart Speaker";

        if (name.Contains("camera"))
            return "Camera";

        if (name.Contains("nas") || name.Contains("storage"))
            return "Network Storage";

        if (name.Contains("bridge") || name.Contains("hub"))
            return "Smart Home Bridge";

        return "Network Device";
    }
}
