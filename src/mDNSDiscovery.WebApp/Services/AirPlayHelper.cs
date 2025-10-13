namespace mDNSDiscovery.WebApp.Services;

public static class AirPlayHelper
{
    /// <summary>
    /// Decodes AirPlay features bitfield into human-readable capabilities
    /// Based on https://openairplay.github.io/airplay-spec/features.html
    /// </summary>
    public static List<string> DecodeFeatures(string hexValue)
    {
        var features = new List<string>();

        if (string.IsNullOrEmpty(hexValue))
            return features;

        long featureBits = 0;

        // Check if it's a decimal value (all digits, no 0x prefix, no hex letters)
        if (hexValue.All(c => char.IsDigit(c)))
        {
            // Parse as decimal
            if (!long.TryParse(hexValue.Trim(), out featureBits))
                return features;
        }
        else
        {
            // Remove "0x" prefix if present
            hexValue = hexValue.Replace("0x", "").Replace("0X", "");

            // Handle comma-separated 64-bit features (e.g., "0x22222222,0x11111111")
            var parts = hexValue.Split(',');

            if (parts.Length == 2)
            {
                // 64-bit format: low bits, high bits
                if (long.TryParse(parts[1].Trim(), System.Globalization.NumberStyles.HexNumber, null, out var highBits) &&
                    long.TryParse(parts[0].Trim(), System.Globalization.NumberStyles.HexNumber, null, out var lowBits))
                {
                    featureBits = (highBits << 32) | (lowBits & 0xFFFFFFFF);
                }
            }
            else
            {
                // Single hex value
                if (!long.TryParse(hexValue.Trim(), System.Globalization.NumberStyles.HexNumber, null, out featureBits))
                    return features;
            }
        }

        // Decode feature bits
        if (IsBitSet(featureBits, 0)) features.Add("Video");
        if (IsBitSet(featureBits, 1)) features.Add("Photo");
        if (IsBitSet(featureBits, 2)) features.Add("Video FairPlay DRM");
        if (IsBitSet(featureBits, 3)) features.Add("Video Volume Control");
        if (IsBitSet(featureBits, 4)) features.Add("HTTP Live Streaming");
        if (IsBitSet(featureBits, 5)) features.Add("Slideshow");
        if (IsBitSet(featureBits, 7)) features.Add("Screen Mirroring");
        if (IsBitSet(featureBits, 8)) features.Add("Screen Rotation");
        if (IsBitSet(featureBits, 9)) features.Add("Audio");
        if (IsBitSet(featureBits, 11)) features.Add("Audio Redundancy");
        if (IsBitSet(featureBits, 12)) features.Add("FairPlay Secure Auth");
        if (IsBitSet(featureBits, 13)) features.Add("Photo Caching");
        if (IsBitSet(featureBits, 14)) features.Add("Authentication Type 4");
        if (IsBitSet(featureBits, 15)) features.Add("Metadata: Artwork");
        if (IsBitSet(featureBits, 16)) features.Add("Metadata: Progress");
        if (IsBitSet(featureBits, 17)) features.Add("Metadata: Text");
        if (IsBitSet(featureBits, 18)) features.Add("Audio Format 1");
        if (IsBitSet(featureBits, 19)) features.Add("Audio Format 2 (AirPlay 2)");
        if (IsBitSet(featureBits, 20)) features.Add("Audio Format 3 (AirPlay 2)");
        if (IsBitSet(featureBits, 21)) features.Add("Audio Format 4");
        if (IsBitSet(featureBits, 23)) features.Add("RSA Authentication");
        if (IsBitSet(featureBits, 26)) features.Add("Unified Advertiser Info");
        if (IsBitSet(featureBits, 27)) features.Add("Legacy Pairing");
        if (IsBitSet(featureBits, 30)) features.Add("RAOP");
        if (IsBitSet(featureBits, 32)) features.Add("CarPlay / Volume Support");
        if (IsBitSet(featureBits, 33)) features.Add("Video Play Queue");
        if (IsBitSet(featureBits, 34)) features.Add("AirPlay from Cloud");
        if (IsBitSet(featureBits, 38)) features.Add("CoreUtils Pairing & Encryption");
        if (IsBitSet(featureBits, 40)) features.Add("Buffered Audio (Multi-room)");
        if (IsBitSet(featureBits, 41)) features.Add("PTP (Multi-room)");
        if (IsBitSet(featureBits, 42)) features.Add("Screen Multi-Codec");
        if (IsBitSet(featureBits, 43)) features.Add("System Pairing");
        if (IsBitSet(featureBits, 46)) features.Add("HomeKit Pairing & Access Control");
        if (IsBitSet(featureBits, 48)) features.Add("Transient Pairing");
        if (IsBitSet(featureBits, 50)) features.Add("Metadata: Binary plist");
        if (IsBitSet(featureBits, 51)) features.Add("MFi Authentication");
        if (IsBitSet(featureBits, 52)) features.Add("SetPeers Extended Message");

        return features;
    }

    /// <summary>
    /// Decodes AirPlay status flags into human-readable states
    /// Based on https://openairplay.github.io/airplay-spec/status_flags.html
    /// </summary>
    public static List<string> DecodeStatusFlags(string hexValue)
    {
        var flags = new List<string>();

        if (string.IsNullOrEmpty(hexValue))
            return flags;

        int statusBits = 0;

        // Check if it's a decimal value (all digits, no 0x prefix, no hex letters)
        if (hexValue.All(c => char.IsDigit(c)))
        {
            // Parse as decimal
            if (!int.TryParse(hexValue.Trim(), out statusBits))
                return flags;
        }
        else
        {
            // Remove "0x" prefix if present
            hexValue = hexValue.Replace("0x", "").Replace("0X", "");

            if (!int.TryParse(hexValue.Trim(), System.Globalization.NumberStyles.HexNumber, null, out statusBits))
                return flags;
        }

        // Decode status flag bits
        if (IsBitSet(statusBits, 0)) flags.Add("Problem Detected");
        if (IsBitSet(statusBits, 1)) flags.Add("Not Configured");
        if (IsBitSet(statusBits, 2)) flags.Add("Audio Cable Attached");
        if (IsBitSet(statusBits, 3)) flags.Add("PIN Required");
        if (IsBitSet(statusBits, 6)) flags.Add("Supports AirPlay from Cloud");
        if (IsBitSet(statusBits, 7)) flags.Add("Password Required");
        if (IsBitSet(statusBits, 9)) flags.Add("One-Time Pairing Required");
        if (IsBitSet(statusBits, 10)) flags.Add("Setup for HomeKit Access Control");
        if (IsBitSet(statusBits, 11)) flags.Add("Supports Relay");
        if (IsBitSet(statusBits, 12)) flags.Add("Silent Primary");
        if (IsBitSet(statusBits, 13)) flags.Add("TightSync Group Leader");
        if (IsBitSet(statusBits, 14)) flags.Add("TightSync Buddy Not Reachable");
        if (IsBitSet(statusBits, 15)) flags.Add("Apple Music Subscriber");
        if (IsBitSet(statusBits, 16)) flags.Add("Cloud Library On");
        if (IsBitSet(statusBits, 17)) flags.Add("Receiver Session Active (AirPlay Receiving)");

        return flags;
    }

    private static bool IsBitSet(long value, int bit)
    {
        return (value & (1L << bit)) != 0;
    }
}
