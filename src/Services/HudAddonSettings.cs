using System.Globalization;
using System.Text.Json;

namespace Sws2Cs2Menus.Services;

/// <summary>Pure policy shared with offline checks. No Workshop discovery by ambiguous titles.</summary>
internal static class HudAddonSettings
{
    public static string Resolve(string? configuredId, string manifestPath)
    {
        if (!string.IsNullOrWhiteSpace(configuredId)) return Normalize(configuredId);
        if (!File.Exists(manifestPath)) return "";
        using var document = JsonDocument.Parse(File.ReadAllText(manifestPath));
        return Normalize(document.RootElement.GetProperty("WorkshopId").GetString());
    }

    private static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "";
        if (!ulong.TryParse(value.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var id) || id == 0)
            throw new InvalidDataException("HUD WorkshopId must be a positive numeric PublishedFileId.");
        return id.ToString(CultureInfo.InvariantCulture);
    }

    public static bool Contains(string list, string id) => list.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
        .Any(item => item == id);

    public static string MountList(string list, string id)
    {
        if (list.Any(character => !char.IsAsciiDigit(character) && character != ',' && character != ' ')
            || Normalize(id) != id)
            throw new InvalidDataException("HUD addon list must contain only numeric Workshop IDs separated by commas.");
        if (!Contains(list, id)) return string.IsNullOrWhiteSpace(list) ? id : list.TrimEnd(',') + "," + id;

        // Setting an identical value may not run a ConVar's change callback. MAM ignores
        // empty comma-delimited entries, so toggle a trailing delimiter to refresh the
        // exact same addon list, without temporarily removing an addon or changing order.
        return list.EndsWith(',') ? list.TrimEnd(',') : list + ",";
    }
}
