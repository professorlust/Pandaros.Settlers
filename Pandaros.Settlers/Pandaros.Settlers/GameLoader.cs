﻿using System;
using System.Collections.Generic;
using System.IO;
using ChatCommands;
using Pandaros.Settlers.AI;
using Pandaros.Settlers.Items;
using Pandaros.Settlers.Jobs;
using Pandaros.Settlers.Managers;
using Pandaros.Settlers.Monsters;
using Pipliz.JSON;
using Pipliz.Threading;

namespace Pandaros.Settlers
{
    [ModLoader.ModManagerAttribute]
    public static class GameLoader
    {
        public const string NAMESPACE = "Pandaros.Settlers";
        public const string SETTLER_INV = "Pandaros.Settlers.Inventory";
        public const string ALL_SKILLS = "Pandaros.Settlers.ALLSKILLS";
        public static string MESH_PATH = "gamedata/meshes/";
        public static string AUDIO_PATH = "gamedata/Audio/";
        public static string ICON_PATH = "gamedata/textures/icons/";
        public static string BLOCKS_ALBEDO_PATH = "Textures/albedo/";
        public static string BLOCKS_EMISSIVE_PATH = "Textures/emissive/";
        public static string BLOCKS_HEIGHT_PATH = "Textures/height/";
        public static string BLOCKS_NORMAL_PATH = "Textures/normal/";
        public static string TEXTURE_FOLDER_PANDA = "Textures";
        public static string NPC_PATH = "gamedata/textures/materials/npc/";
        public static string MOD_FOLDER = @"gamedata/mods/Pandaros/settlers";
        public static string MODS_FOLDER = @"";
        public static string GAMEDATA_FOLDER = @"";
        public static string GAME_ROOT = @"";
        public static string SAVE_LOC = "";

        public static readonly Version MOD_VER = new Version(0, 8, 1, 10);
        public static bool RUNNING { get; private set; }
        public static bool WorldLoaded { get; private set; }

        public static ushort MissingMonster_Icon { get; private set; }
        public static ushort Repairing_Icon { get; private set; }
        public static ushort Refuel_Icon { get; private set; }
        public static ushort Waiting_Icon { get; private set; }
        public static ushort Reload_Icon { get; private set; }
        public static ushort Broken_Icon { get; private set; }
        public static ushort Empty_Icon { get; private set; }
        public static ushort NOAMMO_Icon { get; private set; }
        public static ushort Poisoned_Icon { get; private set; }
        public static ushort Bow_Icon { get; private set; }

        [ModLoader.ModCallbackAttribute(ModLoader.EModCallbackType.AfterSelectedWorld,
            NAMESPACE + ".AfterSelectedWorld")]
        public static void AfterSelectedWorld()
        {
            WorldLoaded                 = true;
            SAVE_LOC                    = GAMEDATA_FOLDER + "savegames/" + ServerManager.WorldName + "/";
            MachineManager.MACHINE_JSON = $"{SAVE_LOC}/{NAMESPACE}.Machines.json";
            PandaLogger.Log(ChatColor.lime, "World load detected. Starting monitor...");
        }

        [ModLoader.ModCallbackAttribute(ModLoader.EModCallbackType.OnAssemblyLoaded, NAMESPACE + ".OnAssemblyLoaded")]
        public static void OnAssemblyLoaded(string path)
        {
            MOD_FOLDER = Path.GetDirectoryName(path);
            PandaLogger.Log("Found mod in {0}", MOD_FOLDER);

            GAME_ROOT = path.Substring(0, path.IndexOf("gamedata")).Replace("\\", "/") + "/";

            GAMEDATA_FOLDER =
                path.Substring(0, path.IndexOf("gamedata") + "gamedata".Length).Replace("\\", "/") + "/";

            MODS_FOLDER          = GAMEDATA_FOLDER + "/mods/";
            ICON_PATH            = Path.Combine(MOD_FOLDER, "icons").Replace("\\", "/") + "/";
            MESH_PATH            = Path.Combine(MOD_FOLDER, "Meshes").Replace("\\", "/") + "/";
            AUDIO_PATH           = Path.Combine(MOD_FOLDER, "Audio").Replace("\\", "/") + "/";
            TEXTURE_FOLDER_PANDA = Path.Combine(MOD_FOLDER, "Textures").Replace("\\", "/") + "/";
            BLOCKS_ALBEDO_PATH   = Path.Combine(TEXTURE_FOLDER_PANDA, "albedo").Replace("\\", "/") + "/";
            BLOCKS_EMISSIVE_PATH = Path.Combine(TEXTURE_FOLDER_PANDA, "emissive").Replace("\\", "/") + "/";
            BLOCKS_HEIGHT_PATH   = Path.Combine(TEXTURE_FOLDER_PANDA, "height").Replace("\\", "/") + "/";
            BLOCKS_NORMAL_PATH   = Path.Combine(TEXTURE_FOLDER_PANDA, "normal").Replace("\\", "/") + "/";

            var fileWasCopied = false;

            foreach (var file in Directory.GetFiles(MOD_FOLDER + "/ZipSupport"))
            {
                var destFile = GAME_ROOT + "colonyserver_Data/Managed/" + new FileInfo(file).Name;

                if (!File.Exists(destFile))
                {
                    fileWasCopied = true;
                    File.Copy(file, destFile);
                }
            }

            if (fileWasCopied)
                PandaLogger.Log(ChatColor.red,
                                "For settlers mod to fully be installed the Colony Survival surver needs to be restarted.");
        }

        [ModLoader.ModCallbackAttribute(ModLoader.EModCallbackType.AfterAddingBaseTypes, NAMESPACE + ".addlittypes")]
        public static void AddLitTypes(Dictionary<string, ItemTypesServer.ItemTypeRaw> items)
        {
            var monsterNode = new JSONNode();
            monsterNode["icon"] = new JSONNode(ICON_PATH + "NoMonster.png");
            var monster = new ItemTypesServer.ItemTypeRaw(NAMESPACE + ".Monster", monsterNode);
            MissingMonster_Icon = monster.ItemIndex;

            items.Add(NAMESPACE + ".Monster", monster);

            var repairingNode = new JSONNode();
            repairingNode["icon"] = new JSONNode(ICON_PATH + "Repairing.png");
            var repairing = new ItemTypesServer.ItemTypeRaw(NAMESPACE + ".Repairing", repairingNode);
            Repairing_Icon = repairing.ItemIndex;

            items.Add(NAMESPACE + ".Repairing", repairing);

            var refuelNode = new JSONNode();
            refuelNode["icon"] = new JSONNode(ICON_PATH + "Refuel.png");
            var refuel = new ItemTypesServer.ItemTypeRaw(NAMESPACE + ".Refuel", refuelNode);
            Refuel_Icon = refuel.ItemIndex;

            items.Add(NAMESPACE + ".Refuel", refuel);

            var waitingNode = new JSONNode();
            waitingNode["icon"] = new JSONNode(ICON_PATH + "Waiting.png");
            var waiting = new ItemTypesServer.ItemTypeRaw(NAMESPACE + ".Waiting", waitingNode);
            Waiting_Icon = waiting.ItemIndex;

            items.Add(NAMESPACE + ".Waiting", waiting);

            var reloadNode = new JSONNode();
            reloadNode["icon"] = new JSONNode(ICON_PATH + "Reload.png");
            var reload = new ItemTypesServer.ItemTypeRaw(NAMESPACE + ".Reload", reloadNode);
            Reload_Icon = reload.ItemIndex;

            items.Add(NAMESPACE + ".Reload", reload);

            var brokenNode = new JSONNode();
            brokenNode["icon"] = new JSONNode(ICON_PATH + "Broken.png");
            var broken = new ItemTypesServer.ItemTypeRaw(NAMESPACE + ".Broken", brokenNode);
            Broken_Icon = broken.ItemIndex;

            items.Add(NAMESPACE + ".Broken", broken);

            var emptyNode = new JSONNode();
            emptyNode["icon"] = new JSONNode(ICON_PATH + "Empty.png");
            var empty = new ItemTypesServer.ItemTypeRaw(NAMESPACE + ".Empty", emptyNode);
            Empty_Icon = empty.ItemIndex;

            items.Add(NAMESPACE + ".Empty", empty);

            var noAmmoNode = new JSONNode();
            noAmmoNode["icon"] = new JSONNode(ICON_PATH + "NoAmmo.png");
            var noAmmo = new ItemTypesServer.ItemTypeRaw(NAMESPACE + ".NoAmmo", emptyNode);
            NOAMMO_Icon = noAmmo.ItemIndex;

            items.Add(NAMESPACE + ".NoAmmo", noAmmo);

            var poisonedNode = new JSONNode();
            poisonedNode["icon"] = new JSONNode(ICON_PATH + "Poisoned.png");
            var poisoned = new ItemTypesServer.ItemTypeRaw(NAMESPACE + ".Poisoned", poisonedNode);
            Poisoned_Icon = poisoned.ItemIndex;

            items.Add(NAMESPACE + ".Poisoned", poisoned);

            var bowNode = new JSONNode();
            bowNode["icon"] = new JSONNode(ICON_PATH + "bow.png");
            var bow = new ItemTypesServer.ItemTypeRaw(NAMESPACE + ".BowIcon", bowNode);
            Bow_Icon = bow.ItemIndex;

            items.Add(NAMESPACE + ".BowIcon", bow);

            MachinistJob.OkStatus = new List<uint>
            {
                Refuel_Icon,
                Reload_Icon,
                Repairing_Icon,
                Waiting_Icon
            };
        }

        [ModLoader.ModCallbackAttribute(ModLoader.EModCallbackType.AfterStartup, NAMESPACE + ".AfterStartup")]
        public static void AfterStartup()
        {
            RUNNING = true;
            CommandManager.RegisterCommand(new GameDifficultyChatCommand());
            CommandManager.RegisterCommand(new CalltoArms());
            CommandManager.RegisterCommand(new ArmorCommand());
            CommandManager.RegisterCommand(new VersionChatCommand());
            CommandManager.RegisterCommand(new ColonyArchiver());
            CommandManager.RegisterCommand(new ConfigurationChatCommand());
            CommandManager.RegisterCommand(new BossesChatCommand());
            CommandManager.RegisterCommand(new MonstersChatCommand());
            CommandManager.RegisterCommand(new SettlersChatCommand());

            VersionChecker.WriteVersionsToConsole();
#if Debug
            ChatCommands.CommandManager.RegisterCommand(new Research.PandaResearchCommand());
#endif
        }

        [ModLoader.ModCallbackAttribute(ModLoader.EModCallbackType.OnQuitLate, NAMESPACE + ".OnQuitLate")]
        public static void OnQuitLate()
        {
            RUNNING     = false;
            WorldLoaded = false;
        }

        [ModLoader.ModCallbackAttribute(ModLoader.EModCallbackType.AfterSelectedWorld,
            NAMESPACE + ".GameLoader.LoadAudioFiles")]
        [ModLoader.ModCallbackDependsOnAttribute("pipliz.server.registeraudiofiles")]
        [ModLoader.ModCallbackProvidesForAttribute("pipliz.server.loadaudiofiles")]
        private static void RegisterAudioFiles()
        {
            var files = JSON.Deserialize(MOD_FOLDER + "/Audio/audioFiles.json", false);

            foreach (var current in files.LoopArray())
            {
                foreach (var current2 in current["fileList"].LoopArray())
                    current2["path"] = new JSONNode(AUDIO_PATH + current2.GetAs<string>("path"));

                ItemTypesServer.AudioFilesJSON.AddToArray(current);
            }
        }

        [ModLoader.ModCallbackAttribute(ModLoader.EModCallbackType.OnTryChangeBlock,
            NAMESPACE + ".GameLoader.trychangeblock")]
        public static void OnTryChangeBlockUser(ModLoader.OnTryChangeBlockData userData)
        {
            if (userData.CallbackState == ModLoader.OnTryChangeBlockData.ECallbackState.Cancelled)
                return;

            if (userData.CallbackOrigin == ModLoader.OnTryChangeBlockData.ECallbackOrigin.ClientPlayerManual)
            {
                var side    = userData.PlayerClickedData.VoxelSideHit;
                var newType = userData.TypeNew;
                var suffix  = string.Empty;

                switch (side)
                {
                    case VoxelSide.xPlus:
                        suffix = "right";
                        break;

                    case VoxelSide.xMin:
                        suffix = "left";
                        break;

                    case VoxelSide.yPlus:
                        suffix = "bottom";
                        break;

                    case VoxelSide.yMin:
                        suffix = "top";
                        break;

                    case VoxelSide.zPlus:
                        suffix = "front";
                        break;

                    case VoxelSide.zMin:
                        suffix = "back";
                        break;
                }

                if (newType != userData.TypeOld && ItemTypes.IndexLookup.TryGetName(newType, out var typename))
                {
                    var otherTypename = typename + suffix;

                    if (ItemTypes.IndexLookup.TryGetIndex(otherTypename, out var otherIndex))
                    {
                        userData.TypeNew = otherIndex;
                    }
                }
            }
        }

        public static string GetUpdatableBlocksJSONPath()
        {
            return string.Format("gamedata/savegames/{0}/updatableblocks.json", ServerManager.WorldName);
        }

        public static void AddSoundFile(string key, List<string> fileNames)
        {
            var node = new JSONNode();
            node.SetAs("clipCollectionName", key);

            var fileListNode = new JSONNode(NodeType.Array);

            foreach (var fileName in fileNames)
            {
                var audoFileNode = new JSONNode()
                                  .SetAs("path", fileName)
                                  .SetAs("audioGroup", "Effects");

                fileListNode.AddToArray(audoFileNode);
            }

            node.SetAs("fileList", fileListNode);

            ItemTypesServer.AudioFilesJSON.AddToArray(node);
        }
    }
}
