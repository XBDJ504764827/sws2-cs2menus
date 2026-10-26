using SwiftlyS2.Shared.Commands;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.Translation;
using Sws2Cs2Menus.Contracts;

namespace Sws2Cs2Menus;

/// <summary>
/// Demo command. It is intentionally small and self-contained: it proves
/// pagination, nested menus, permissions, selection, back and close without
/// requiring any other plugin.
/// </summary>
public partial class Sws2Cs2Menus
{
    private const string DemoAdminPermission = "sws2.cs2menus.demo.admin";

    [Command("menudemo", true, "", "Open the sws2-cs2menus demo menu")]
    [CommandAlias("menutest", registerRaw: true)]
    public void MenuDemoCommand(ICommandContext context)
    {
        var localizer = context.Sender is not null
            ? Core.Translation.GetPlayerLocalizer(context.Sender)
            : Core.Localizer;

        if (!context.IsSentByPlayer || context.Sender is null)
        {
            context.Reply($"{localizer["cs2menus.prefix"]} {localizer["cs2menus.only_players"]}");
            return;
        }

        if (!_config.Demo.EnableCommand)
        {
            context.Reply($"{localizer["cs2menus.prefix"]} {localizer["cs2menus.demo.disabled"]}");
            return;
        }

        var player = context.Sender;

        if (!string.IsNullOrWhiteSpace(_config.Demo.CommandPermission)
            && !Core.Permission.PlayerHasPermission(player.SteamID, _config.Demo.CommandPermission))
        {
            context.Reply($"{localizer["cs2menus.prefix"]} {localizer["cs2menus.no_permission"]}");
            return;
        }

        var result = _menus.Open(player, BuildDemoMenu(localizer, player));
        if (result == MenuOpenResult.Opened)
        {
            return;
        }

        context.Reply($"{localizer["cs2menus.prefix"]} {localizer[ResultKey(result)]}");
    }

    private IMenuDefinition BuildDemoMenu(ILocalizer localizer, IPlayer player)
    {
        var subBuilder = _menus.CreateMenu(localizer["cs2menus.demo.submenu.title"])
            .Id("demo.submenu")
            .PageSize(3)
            .ShowBack()
            .CloseOnSelect();

        for (var i = 1; i <= 7; i++)
        {
            var index = i;
            subBuilder.AddOption($"sub-{i}", localizer["cs2menus.demo.submenu.option", index], context =>
            {
                context.Player.SendChat($"{localizer["cs2menus.prefix"]} {localizer["cs2menus.demo.selected", $"sub-{index}"]}");
            });
        }

        var subMenu = subBuilder.Build();

        var rootBuilder = _menus.CreateMenu(localizer["cs2menus.demo.title"])
            .Id("demo.root")
            .Comment(localizer["cs2menus.demo.comment"])
            .PageSize(5)
            .ShowClose()
            .CloseOnSelect(false);

        for (var i = 1; i <= 10; i++)
        {
            var index = i;
            rootBuilder.AddOption($"item-{i}", localizer["cs2menus.demo.option", index], context =>
            {
                context.Player.SendChat($"{localizer["cs2menus.prefix"]} {localizer["cs2menus.demo.selected", $"item-{index}"]}");
            });
        }

        rootBuilder.AddSubMenu("submenu", localizer["cs2menus.demo.submenu"], subMenu);

        rootBuilder.AddOption(option => option
            .Id("disabled")
            .Text(localizer["cs2menus.demo.disabled_option"])
            .Disabled());

        if (Core.Permission.PlayerHasPermission(player.SteamID, DemoAdminPermission))
        {
            rootBuilder.AddOption(option => option
                .Id("admin")
                .Text(localizer["cs2menus.demo.admin_option"])
                .Color("#FFAA00")
                .Permission(DemoAdminPermission)
                .OnSelected(context =>
                {
                    context.Player.SendChat($"{localizer["cs2menus.prefix"]} {localizer["cs2menus.demo.admin_selected"]}");
                }));
        }

        rootBuilder.AddOption(option => option
            .Id("close")
            .Text(localizer["cs2menus.demo.close"])
            .OnSelected(context => context.Service.Close(context.Player)));

        return rootBuilder.Build();
    }

    private static string ResultKey(MenuOpenResult result)
    {
        return result switch
        {
            MenuOpenResult.NoPermission => "cs2menus.no_permission",
            MenuOpenResult.InvalidPlayer => "cs2menus.invalid_player",
            MenuOpenResult.NoOptions => "cs2menus.no_options",
            _ => "cs2menus.unavailable"
        };
    }
}
