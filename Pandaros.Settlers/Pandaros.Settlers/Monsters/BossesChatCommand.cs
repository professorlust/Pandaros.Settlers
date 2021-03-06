﻿using System;
using System.Collections.Generic;
using ChatCommands;
using Pandaros.Settlers.Entities;
using Pipliz;
using Pipliz.JSON;

namespace Pandaros.Settlers.Monsters
{
    [ModLoader.ModManager]
    public class BossesChatCommand : IChatCommand
    {
        private static string _Bosses = GameLoader.NAMESPACE + ".Bosses";

        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnConstructWorldSettingsUI, GameLoader.NAMESPACE + "Bosses.AddSetting")]
        public static void AddSetting(Players.Player player, NetworkUI.NetworkMenu menu)
        {
            menu.Items.Add(new NetworkUI.Items.DropDown("Settlers Bosses", _Bosses, new List<string>() { "Disabled", "Enabled" }));
            var ps = PlayerState.GetPlayerState(player);

            if (ps != null)
                menu.LocalStorage.SetAs(_Bosses, Convert.ToInt32(ps.BossesEnabled));
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnPlayerChangedNetworkUIStorage, GameLoader.NAMESPACE + "Bosses.ChangedSetting")]
        public static void ChangedSetting(TupleStruct<Players.Player, JSONNode, string> data)
        {
            switch (data.item3)
            {
                case "world_settings":
                    var ps = PlayerState.GetPlayerState(data.item1);

                    if (ps != null && data.item2.GetAsOrDefault(_Bosses, Convert.ToInt32(ps.BossesEnabled)) != Convert.ToInt32(ps.BossesEnabled))
                    {
                        if (!Configuration.GetorDefault("BossesCanBeDisabled", true))
                            PandaChat.Send(data.item1, "The server administrator had disabled the changing of bosses.", ChatColor.red);
                        else
                            ps.BossesEnabled = data.item2.GetAsOrDefault(_Bosses, Convert.ToInt32(ps.BossesEnabled)) != 0;

                        PandaChat.Send(data.item1, "Settlers! Mod Bosses are now " + (ps.BossesEnabled ? "on" : "off"), ChatColor.green);
                    }

                    break;
            }
        }

        public bool IsCommand(string chat)
        {
            return chat.StartsWith("/bosses", StringComparison.OrdinalIgnoreCase);
        }

        public bool TryDoCommand(Players.Player player, string chat)
        {
            if (player == null || player.ID == NetworkID.Server)
                return true;

            var array  = CommandManager.SplitCommand(chat);
            var colony = Colony.Get(player);
            var state  = PlayerState.GetPlayerState(player);

            if (array.Length == 1)
            {
                PandaChat.Send(player, "Settlers! Bosses are {0}.", ChatColor.green,
                               state.BossesEnabled ? "on" : "off");

                return true;
            }

            if (array.Length == 2 && Configuration.GetorDefault("BossesCanBeDisabled", true))
            {
                if (array[1].ToLower().Trim() == "on" || array[1].ToLower().Trim() == "true")
                {
                    state.BossesEnabled = true;
                    PandaChat.Send(player, "Settlers! Mod Bosses are now on.", ChatColor.green);
                }
                else
                {
                    state.BossesEnabled = false;
                    PandaChat.Send(player, "Settlers! Mod Bosses are now off.", ChatColor.green);
                }
            }

            NetworkUI.NetworkMenuManager.SendWorldSettingsUI(player);
            if (!Configuration.GetorDefault("BossesCanBeDisabled", true))
                PandaChat.Send(player, "The server administrator had disabled the changing of bosses.",
                                ChatColor.red);
            

            return true;
        }
    }
}