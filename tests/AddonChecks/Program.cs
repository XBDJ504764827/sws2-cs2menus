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
    var commands = new List<string>();
    var config = new MenuConfig();
    var mount = new HudAddonMountService(path, NullLogger.Instance, () => list, command => commands.Add(command), _ => ready, () => time);
    mount.Check(config);
    Check(commands.Count == 0 && mount.Status.Contains("unavailable"), "Missing MAM waits without executing unknown commands");
    list = "42,88";
    mount.Check(config);
    Check(commands.SequenceEqual(new[] { "mm_add_addon 123" }) && list == "42,88", "Late-loaded MAM receives append command without overwriting other addons");
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
    Check(commands.Last() == "mm_add_addon 456", "Custom publisher ID replaces default selection");
    config.Menu.HudLayoutWorkshopId = "invalid";
    mount.Reset(); mount.Check(config);
    Check(commands.Count == 4 && mount.Status.Contains("failed"), "Invalid configuration reports failure without executing anything");
    config.Menu.HudLayoutWorkshopId = "";
    File.WriteAllText(path, """{"WorkshopId":""}""");
    mount.Reset(); mount.Check(config);
    Check(commands.Count == 4 && mount.Status.Contains("not registered"), "Unpublished addon reports that one-time registration is still needed");
    Console.WriteLine($"Passed {checks} HUD addon mounting checks.");
}
finally { File.Delete(path); }
void Check(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
    checks++; Console.WriteLine("PASS: " + message);
}
