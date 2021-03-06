﻿using System.Collections.Generic;
using System.Linq;
using BlockTypes.Builtin;
using Pandaros.Settlers.Entities;
using Pandaros.Settlers.Jobs;
using Pandaros.Settlers.Managers;
using Pipliz;
using Pipliz.JSON;
using Server;
using Shared;

namespace Pandaros.Settlers.Items.Machines
{
    [ModLoader.ModManagerAttribute]
    public static class Miner
    {
        private const double MinerCooldown = 4;

        public static ItemTypesServer.ItemTypeRaw Item { get; private set; }

        [ModLoader.ModCallbackAttribute(ModLoader.EModCallbackType.AfterItemTypesDefined,
            GameLoader.NAMESPACE + ".Items.Machines.Miner.RegisterMachines")]
        public static void RegisterMachines()
        {
            MachineManager.RegisterMachineType(new MachineManager.MachineSettings(nameof(Miner), Item.ItemIndex, Repair,
                                                                                  MachineManager.Refuel, Reload, DoWork,
                                                                                  10, 4, 5, 4));
        }

        public static ushort Repair(Players.Player player, MachineState machineState)
        {
            var retval = GameLoader.Repairing_Icon;

            if (!player.IsConnected && Configuration.OfflineColonies || player.IsConnected)
            {
                var ps = PlayerState.GetPlayerState(player);

                if (machineState.Durability < .75f)
                {
                    var repaired       = false;
                    var requiredForFix = new List<InventoryItem>();
                    var stockpile      = Stockpile.GetStockPile(player);

                    requiredForFix.Add(new InventoryItem(BuiltinBlocks.Planks, 1));
                    requiredForFix.Add(new InventoryItem(BuiltinBlocks.CopperNails, 1));

                    if (machineState.Durability < .10f)
                    {
                        requiredForFix.Add(new InventoryItem(BuiltinBlocks.IronWrought, 1));
                        requiredForFix.Add(new InventoryItem(BuiltinBlocks.CopperParts, 4));
                        requiredForFix.Add(new InventoryItem(BuiltinBlocks.IronRivet, 1));
                        requiredForFix.Add(new InventoryItem(BuiltinBlocks.CopperTools, 1));
                    }
                    else if (machineState.Durability < .30f)
                    {
                        requiredForFix.Add(new InventoryItem(BuiltinBlocks.IronWrought, 1));
                        requiredForFix.Add(new InventoryItem(BuiltinBlocks.CopperParts, 2));
                        requiredForFix.Add(new InventoryItem(BuiltinBlocks.CopperTools, 1));
                    }
                    else if (machineState.Durability < .50f)
                    {
                        requiredForFix.Add(new InventoryItem(BuiltinBlocks.CopperParts, 1));
                        requiredForFix.Add(new InventoryItem(BuiltinBlocks.CopperTools, 1));
                    }

                    if (stockpile.Contains(requiredForFix))
                    {
                        stockpile.TryRemove(requiredForFix);
                        repaired = true;
                    }
                    else
                    {
                        foreach (var item in requiredForFix)
                            if (!stockpile.Contains(item))
                            {
                                retval = item.Type;
                                break;
                            }
                    }

                    if (!MachineState.MAX_DURABILITY.ContainsKey(player))
                        MachineState.MAX_DURABILITY[player] = MachineState.DEFAULT_MAX_DURABILITY;

                    if (repaired)
                        machineState.Durability = MachineState.MAX_DURABILITY[player];
                }
            }

            return retval;
        }

        public static ushort Reload(Players.Player player, MachineState machineState)
        {
            return GameLoader.Waiting_Icon;
        }

        public static void DoWork(Players.Player player, MachineState machineState)
        {
            if (!player.IsConnected && Configuration.OfflineColonies || player.IsConnected)
                if (machineState.Durability > 0 &&
                    machineState.Fuel > 0 &&
                    machineState.NextTimeForWork < Time.SecondsSinceStartDouble)
                {
                    machineState.Durability -= 0.02f;
                    machineState.Fuel       -= 0.05f;

                    if (machineState.Durability < 0)
                        machineState.Durability = 0;

                    if (machineState.Fuel <= 0)
                        machineState.Fuel = 0;

                    if (World.TryGetTypeAt(machineState.Position.Add(0, -1, 0), out var itemBelow))
                    {
                        var itemList = ItemTypes.GetType(itemBelow).OnRemoveItems;

                        Indicator.SendIconIndicatorNear(machineState.Position.Add(0, 1, 0).Vector,
                                                        new IndicatorState((float) MinerCooldown,
                                                                           itemList.FirstOrDefault().item.Type));

                        for (var i = 0; i < itemList.Count; i++)
                            if (Random.NextDouble() <= itemList[i].chance)
                                Stockpile.GetStockPile(player).Add(itemList[i].item);

                        ServerManager.SendAudio(machineState.Position.Vector,
                                                GameLoader.NAMESPACE + ".MiningMachineAudio");
                    }

                    machineState.NextTimeForWork = machineState.MachineSettings.WorkTime + Time.SecondsSinceStartDouble;
                }
        }

        [ModLoader.ModCallbackAttribute(ModLoader.EModCallbackType.AfterItemTypesDefined,
            GameLoader.NAMESPACE + ".Items.Machines.Miner.RegisterMiner")]
        public static void RegisterMiner()
        {
            var rivets      = new InventoryItem(BuiltinBlocks.IronRivet, 6);
            var iron        = new InventoryItem(BuiltinBlocks.IronWrought, 2);
            var copperParts = new InventoryItem(BuiltinBlocks.CopperParts, 6);
            var copperNails = new InventoryItem(BuiltinBlocks.CopperNails, 6);
            var tools       = new InventoryItem(BuiltinBlocks.CopperTools, 1);
            var planks      = new InventoryItem(BuiltinBlocks.Planks, 4);
            var pickaxe     = new InventoryItem(BuiltinBlocks.BronzePickaxe, 2);

            var recipe = new Recipe(Item.name,
                                    new List<InventoryItem>
                                    {
                                        planks,
                                        iron,
                                        rivets,
                                        copperParts,
                                        copperNails,
                                        tools,
                                        planks,
                                        pickaxe
                                    },
                                    new InventoryItem(Item.ItemIndex),
                                    5);

            RecipeStorage.AddOptionalLimitTypeRecipe(AdvancedCrafterRegister.JOB_NAME, recipe);
        }

        [ModLoader.ModCallbackAttribute(ModLoader.EModCallbackType.AfterSelectedWorld,
            GameLoader.NAMESPACE + ".Items.Machines.Miner.AddTextures")]
        [ModLoader.ModCallbackProvidesForAttribute("pipliz.server.registertexturemappingtextures")]
        public static void AddTextures()
        {
            var minerTextureMapping = new ItemTypesServer.TextureMapping(new JSONNode());
            minerTextureMapping.AlbedoPath = GameLoader.BLOCKS_ALBEDO_PATH + "MiningMachine.png";

            ItemTypesServer.SetTextureMapping(GameLoader.NAMESPACE + ".Miner", minerTextureMapping);
        }

        [ModLoader.ModCallbackAttribute(ModLoader.EModCallbackType.AfterAddingBaseTypes,
            GameLoader.NAMESPACE + ".Items.Machines.Miner.AddMiner")]
        [ModLoader.ModCallbackDependsOnAttribute("pipliz.blocknpcs.addlittypes")]
        public static void AddMiner(Dictionary<string, ItemTypesServer.ItemTypeRaw> items)
        {
            var minerName     = GameLoader.NAMESPACE + ".Miner";
            var minerFlagNode = new JSONNode();
            minerFlagNode["icon"]        = new JSONNode(GameLoader.ICON_PATH + "MiningMachine.png");
            minerFlagNode["isPlaceable"] = new JSONNode(true);
            minerFlagNode.SetAs("onRemoveAmount", 1);
            minerFlagNode.SetAs("onPlaceAudio", "stonePlace");
            minerFlagNode.SetAs("onRemoveAudio", "stoneDelete");
            minerFlagNode.SetAs("isSolid", true);
            minerFlagNode.SetAs("sideall", "SELF");
            minerFlagNode.SetAs("mesh", GameLoader.MESH_PATH + "MiningMachine.obj");

            var categories = new JSONNode(NodeType.Array);
            categories.AddToArray(new JSONNode("machine"));
            minerFlagNode.SetAs("categories", categories);

            Item = new ItemTypesServer.ItemTypeRaw(minerName, minerFlagNode);
            items.Add(minerName, Item);
        }

        [ModLoader.ModCallbackAttribute(ModLoader.EModCallbackType.OnTryChangeBlock,
            GameLoader.NAMESPACE + ".Items.Machines.Miner.OnTryChangeBlockUser")]
        public static void OnTryChangeBlockUser(ModLoader.OnTryChangeBlockData d)
        {
            if (d.CallbackState == ModLoader.OnTryChangeBlockData.ECallbackState.Cancelled)
                return;

            if (d.TypeNew == Item.ItemIndex && d.TypeOld == BuiltinBlocks.Air)
            {
                if (World.TryGetTypeAt(d.Position.Add(0, -1, 0), out var itemBelow))
                    if (CanMineBlock(itemBelow))
                    {
                        MachineManager.RegisterMachineState(d.RequestedByPlayer,
                                                            new MachineState(d.Position, d.RequestedByPlayer,
                                                                             nameof(Miner)));

                        return;
                    }

                PandaChat.Send(d.RequestedByPlayer, "The mining machine must be placed on stone or ore.",
                               ChatColor.orange);

                d.CallbackState = ModLoader.OnTryChangeBlockData.ECallbackState.Cancelled;
            }
        }

        public static bool CanMineBlock(ushort itemMined)
        {
            return ItemTypes.TryGetType(itemMined, out var item) &&
                   item.CustomDataNode.TryGetAs("minerIsMineable", out bool minable) &&
                   minable;
        }
    }
}