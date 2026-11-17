using Microsoft.Extensions.Logging.Abstractions;
using Sws2Cs2Menus.Configuration;
using Sws2Cs2Menus.Services;

var path = Path.GetTempFileName();
var checks = 0;
try
{
    File.WriteAllText(path, """{"WorkshopId":"123"}""");
    Check(HudAddonSettings.Resolve("", path) == "123", "Packaged Workshop ID is the default");
    Check(HudAddonSettings.Resolve(" 00456 ", path) == "456", "Configured ID overrides packaged ID and normalizes");
    try { HudAddonSettings.Resolve("123;quit", path); throw new Exception("Unsafe ID accepted"); }
    catch (InvalidDataException) { Check(true, "Invalid IDs cannot inject engine commands"); }
    Check(!HudAddonSettings.Contains("1234,5123", "123") && HudAddonSettings.Contains("42, 123 ,88", "123"), "Addon membership compares full IDs");
    string? list = null;
    var ready = false;
    var time = DateTimeOffset.UtcNow;
    var commands = new List<string>(); // Captures ConVar writes, not engine commands.
    var config = new MenuConfig();
    var mount = new HudAddonMountService(path, NullLogger.Instance, () => list, command => commands.Add(command), _ => ready, () => time);
    mount.Check(config);
    Check(commands.Count == 0 && mount.Status.Contains("unavailable"), "Missing MAM waits without executing unknown commands");
    list = "42,88";
    mount.Check(config);
    Check(commands.SequenceEqual(new[] { "42,88,123" }) && list == "42,88", "Late-loaded MAM writes merged ConVar value to trigger registration AND refresh");
    mount.Check(config);
    Check(commands.Count == 1, "Pending registration is rate-limited");
    time = time.AddSeconds(31);
    mount.Check(config);
    Check(commands.Count == 2, "Failed registration retries after a delay");
    list = "42,123,88";
    mount.Check(config);
    Check(commands.Count == 2 && mount.Status.Contains("waiting for layout"), "Registered addon waits for asynchronous download");
    ready = true;
    mount.Check(config);
    Check(mount.Status.Contains("layout ready") && commands.Count == 2, "Readiness is re-evaluated without duplicate registration");
    mount.Reset(); list = "42,88";
    mount.Check(config);
    Check(commands.Count == 3, "Map/config resets re-register an addon removed by external configuration");
    config.Menu.AutoMountHudAddon = false;
    mount.Reset(); mount.Check(config);
    Check(commands.Count == 3, "Local development can disable automatic mounting");
    config.Menu.AutoMountHudAddon = true; config.Menu.Backend = "classic";
    mount.Check(config);
    Check(commands.Count == 3, "Explicit classic mode does not mount HUD resources");
    config.Menu.Backend = "auto"; config.Menu.HudLayoutWorkshopId = "456";
    mount.Reset(); mount.Check(config);
    Check(commands.Last() == "42,88,456", "Custom publisher ID replaces default selection");
    config.Menu.HudLayoutWorkshopId = "invalid";
    mount.Reset(); mount.Check(config);
    Check(commands.Count == 4 && mount.Status.Contains("failed"), "Invalid configuration reports failure without executing anything");
    config.Menu.HudLayoutWorkshopId = "";
    File.WriteAllText(path, """{"WorkshopId":""}""");
    mount.Reset(); mount.Check(config);
    Check(commands.Count == 4 && mount.Status.Contains("not registered"), "Unpublished addon reports that one-time registration is still needed");
    // Reproduce the server report: mm_add_addon already registered the ID, but
    // resources never mounted. Simulate the actual ConVar callback contract:
    // equal values do not refresh, changed values refresh the parsed addon list.
    File.WriteAllText(path, """{"WorkshopId":"123"}""");
    var current = "3469155349,123";
    var refreshed = new List<string>();
    var resourcesReady = false;
    var recovery = new HudAddonMountService(path, NullLogger.Instance, () => current, value =>
    {
        if (value != current) refreshed.Add(value);
        current = value;
    }, _ => resourcesReady, () => time);
    recovery.Check(config);
    Check(current == "3469155349,123," && refreshed.Count == 1, "Previously registered but unmounted addon triggers a real ConVar change");
    recovery.Check(config);
    time = time.AddSeconds(61); recovery.Check(config);
    Check(refreshed.Count == 1 && recovery.Status.Contains("still unavailable"), "Stalled download gets actionable diagnostics without restarting refresh repeatedly");
    resourcesReady = true; recovery.Check(config);
    Check(recovery.Status.Contains("layout ready") && refreshed.Count == 1, "Only actual layout readiness reports success");
    recovery.Reset(); recovery.Check(config);
    Check(refreshed.Count == 1, "Already-ready resources are left mounted");
    resourcesReady = false; recovery.Reset(); recovery.Check(config);
    Check(current == "3469155349,123" && refreshed.Count == 2, "Refresh also works when the current list already has a trailing comma");
    Check(current.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
        .SequenceEqual(new[] { "3469155349", "123" }), "Refresh preserves addon membership and precedence without duplicate IDs");
    Check(HudAddonSettings.MountList("", "123") == "123", "Empty addon list starts with the HUD ID");
    Check(HudAddonSettings.MountList("42,88,", "123") == "42,88,123", "Append tolerates a trailing delimiter");
    try { HudAddonSettings.MountList("42;quit", "123"); throw new Exception("Unsafe list accepted"); }
    catch (InvalidDataException) { Check(true, "Untrusted addon list cannot inject engine commands"); }
    var attempts = 0;
    var failure = new HudAddonMountService(path, NullLogger.Instance, () => "42", _ =>
    {
        attempts++;
        throw new IOException("simulated setter failure");
    }, _ => false, () => time);
    failure.Check(config); failure.Check(config);
    Check(attempts == 1 && failure.Status.Contains("failed"), "Failed setter is reported and rate-limited");
    time = time.AddSeconds(31); failure.Check(config);
    Check(attempts == 2, "Failed setter remains retryable");
    Console.WriteLine($"Passed {checks} HUD addon mounting checks.");
}
finally { File.Delete(path); }
void Check(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
    checks++; Console.WriteLine("PASS: " + message);
}
