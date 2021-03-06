﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BlockTypes.Builtin;
using ChatCommands;
using NPC;
using Pandaros.Settlers.Entities;
using Pipliz.JSON;
using static Pandaros.Settlers.Entities.SettlerInventory;

namespace Pandaros.Settlers.Items
{
    public class ArmorCommand : IChatCommand
    {
        public bool IsCommand(string chat)
        {
            return chat.StartsWith("/armor", StringComparison.OrdinalIgnoreCase);
        }

        public bool TryDoCommand(Players.Player player, string chat)
        {
            var colony = Colony.Get(player);
            var counts = new Dictionary<MetalType, Dictionary<Armor.ArmorSlot, int>>();
            var slots  = new Dictionary<Armor.ArmorSlot, int>();

            foreach (Armor.ArmorSlot slot in Armor.ArmorSlotEnum)
                slots.Add(slot, 0);

            foreach (MetalType metal in Enum.GetValues(typeof(MetalType)))
                counts.Add(metal, new Dictionary<Armor.ArmorSlot, int>(slots));

            foreach (var npc in colony.Followers)
            {
                var inv = GetSettlerInventory(npc);

                foreach (var item in inv.Armor)
                    if (!item.Value.IsEmpty())
                    {
                        var armor = Armor.ArmorLookup[item.Value.Id];
                        counts[armor.Metal][armor.Slot]++;
                    }
            }

            var state = PlayerState.GetPlayerState(player);
            var psb   = new StringBuilder();
            psb.Append("Player =>");

            foreach (var armor in state.Armor)
                if (armor.Value.IsEmpty())
                    psb.Append($" {armor.Key}: None |");
                else
                    psb.Append($" {armor.Key}: {Armor.ArmorLookup[armor.Value.Id].Metal} | ");

            PandaChat.Send(player, psb.ToString());

            foreach (var type in counts)
            {
                var sb = new StringBuilder();
                sb.Append($"{type.Key} =>");
                foreach (var slot in type.Value) sb.Append($" {slot.Key}: {slot.Value} |");

                PandaChat.Send(player, sb.ToString());
            }

            return true;
        }
    }

    [ModLoader.ModManagerAttribute]
    public static class Armor
    {
        public enum ArmorSlot
        {
            Helm,
            Chest,
            Gloves,
            Legs,
            Boots,
            Shield
        }

        public const string JOB_METALSMITH = "pipliz.metalsmithjob";
        public static DateTime _nextUpdate = DateTime.MinValue;

        private static readonly Dictionary<ArmorSlot, int> _hitChance = new Dictionary<ArmorSlot, int>
        {
            {ArmorSlot.Helm, 10},
            {ArmorSlot.Chest, 55},
            {ArmorSlot.Gloves, 65},
            {ArmorSlot.Legs, 90},
            {ArmorSlot.Boots, 100}
        };

        private static readonly Dictionary<ArmorSlot, int> _hitChanceShield = new Dictionary<ArmorSlot, int>
        {
            {ArmorSlot.Helm, 10},
            {ArmorSlot.Chest, 30},
            {ArmorSlot.Gloves, 35},
            {ArmorSlot.Legs, 45},
            {ArmorSlot.Boots, 50},
            {ArmorSlot.Shield, 100}
        };

        private static readonly Random _rand = new Random();

        public static Array ArmorSlotEnum { get; } = Enum.GetValues(typeof(ArmorSlot));

        public static Dictionary<ushort, ArmorMetadata> ArmorLookup { get; set; } =
            new Dictionary<ushort, ArmorMetadata>();

        [ModLoader.ModCallbackAttribute(ModLoader.EModCallbackType.OnUpdate, GameLoader.NAMESPACE + ".Armor.GetArmor")]
        public static void GetArmor()
        {
            if (_nextUpdate < DateTime.Now)
            {
                Players.PlayerDatabase.ForeachValue(p =>
                {
                    var colony    = Colony.Get(p);
                    var state     = PlayerState.GetPlayerState(p);
                    var stockpile = Stockpile.GetStockPile(p);

                    /// Load up player first.
                    foreach (ArmorSlot slot in ArmorSlotEnum)
                    {
                        var bestArmor = GetBestArmorFromStockpile(stockpile, slot, 0);

                        if (bestArmor != default(ushort))
                        {
                            if (!state.Armor.ContainsKey(slot))
                                state.Armor.Add(slot, new ArmorState());

                            // Check if we need one or if there is an upgrade.
                            if (state.Armor[slot].IsEmpty())
                            {
                                stockpile.TryRemove(bestArmor);
                                state.Armor[slot].Id         = bestArmor;
                                state.Armor[slot].Durability = ArmorLookup[bestArmor].Durability;
                            }
                            else
                            {
                                var currentArmor   = ArmorLookup[state.Armor[slot].Id];
                                var stockpileArmor = ArmorLookup[bestArmor];

                                if (stockpileArmor.ArmorRating > currentArmor.ArmorRating)
                                {
                                    // Upgrade armor.
                                    stockpile.TryRemove(bestArmor);
                                    stockpile.Add(state.Armor[slot].Id);
                                    state.Armor[slot].Id         = bestArmor;
                                    state.Armor[slot].Durability = stockpileArmor.Durability;
                                }
                            }
                        }
                    }

                    foreach (var npc in colony.Followers)
                    {
                        var inv = GetSettlerInventory(npc);
                        GetBestArmorForNPC(stockpile, npc, inv, 4);
                    }
                });

                _nextUpdate = DateTime.Now + TimeSpan.FromSeconds(30);
            }
        }

        public static void GetBestArmorForNPC(Stockpile stockpile, NPCBase npc, SettlerInventory inv, int limit)
        {
            foreach (ArmorSlot slot in ArmorSlotEnum)
            {
                var bestArmor = GetBestArmorFromStockpile(stockpile, slot, limit);

                if (bestArmor != default(ushort))
                {
                    if (!inv.Armor.ContainsKey(slot))
                        inv.Armor.Add(slot, new ArmorState());

                    // Check if we need one or if there is an upgrade.
                    if (inv.Armor[slot].IsEmpty())
                    {
                        stockpile.TryRemove(bestArmor);
                        inv.Armor[slot].Id         = bestArmor;
                        inv.Armor[slot].Durability = ArmorLookup[bestArmor].Durability;
                    }
                    else
                    {
                        var currentArmor   = ArmorLookup[inv.Armor[slot].Id];
                        var stockpileArmor = ArmorLookup[bestArmor];

                        if (stockpileArmor.ArmorRating > currentArmor.ArmorRating)
                        {
                            // Upgrade armor.
                            stockpile.TryRemove(bestArmor);
                            stockpile.Add(inv.Armor[slot].Id);
                            inv.Armor[slot].Id         = bestArmor;
                            inv.Armor[slot].Durability = stockpileArmor.Durability;
                        }
                    }
                }
            }
        }

        public static ushort GetBestArmorFromStockpile(Stockpile s, ArmorSlot slot, int limit)
        {
            var best = default(ushort);

            foreach (var armor in ArmorLookup.Where(a => a.Value.Slot == slot))
                if (s.Contains(armor.Key) && s.AmountContained(armor.Key) > limit)
                    if (best == default(ushort) || armor.Value.ArmorRating > ArmorLookup[best].ArmorRating)
                        best = armor.Key;

            return best;
        }

        [ModLoader.ModCallbackAttribute(ModLoader.EModCallbackType.OnPlayerHit,
            GameLoader.NAMESPACE + ".Armor.OnPlayerHit")]
        [ModLoader.ModCallbackDependsOnAttribute(GameLoader.NAMESPACE + ".Managers.MonsterManager.OnPlayerHit")]
        public static void OnPlayerHit(Players.Player player, ModLoader.OnHitData box)
        {
            var state = PlayerState.GetPlayerState(player);
            DeductArmor(box, state.Armor, player, "Your");
        }

        [ModLoader.ModCallbackAttribute(ModLoader.EModCallbackType.OnNPCHit, GameLoader.NAMESPACE + ".Armor.OnNPCHit")]
        [ModLoader.ModCallbackDependsOnAttribute(GameLoader.NAMESPACE + ".Managers.MonsterManager.OnNPCHit")]
        public static void OnNPCHit(NPCBase npc, ModLoader.OnHitData box)
        {
            var inv = GetSettlerInventory(npc);
            DeductArmor(box, inv.Armor, npc.Colony.Owner, inv.SettlerName);
        }

        private static void DeductArmor(ModLoader.OnHitData box,    Dictionary<ArmorSlot, ArmorState> entityArmor,
                                        Players.Player      player, string                            name)
        {
            if (box.ResultDamage > 0)
            {
                float armor = 0;

                foreach (ArmorSlot armorSlot in ArmorSlotEnum)
                {
                    if (!entityArmor.ContainsKey(armorSlot))
                        entityArmor.Add(armorSlot, new ArmorState());

                    if (!entityArmor[armorSlot].IsEmpty())
                        armor += ArmorLookup[entityArmor[armorSlot].Id].ArmorRating;
                }

                if (armor != 0)
                {
                    box.ResultDamage = box.ResultDamage - box.ResultDamage * armor;

                    var hitLocation = _rand.Next(1, 100);

                    var dic = _hitChance;

                    if (!entityArmor[ArmorSlot.Shield].IsEmpty())
                        dic = _hitChanceShield;

                    foreach (var loc in dic)
                        if (!entityArmor[loc.Key].IsEmpty() && loc.Value >= hitLocation)
                        {
                            entityArmor[loc.Key].Durability--;

                            if (entityArmor[loc.Key].Durability <= 0)
                            {
                                entityArmor[loc.Key].Durability = 0;
                                entityArmor[loc.Key].Id         = default(ushort);

                                PandaChat.Send(player,
                                               $"{name} {loc.Key} broke! If you have a spare one it will be automatically equipt within 30 seconds.",
                                               ChatColor.white);
                            }

                            break;
                        }
                }
            }
        }

        [ModLoader.ModCallbackAttribute(ModLoader.EModCallbackType.AfterItemTypesDefined,
            GameLoader.NAMESPACE + ".Armor.RegisterRecipes")]
        public static void RegisterRecipes()
        {
            var coppertools = new InventoryItem(BuiltinBlocks.CopperTools, 1);
            var clothing    = new InventoryItem(BuiltinBlocks.Clothing, 1);

            var copperParts = new InventoryItem(BuiltinBlocks.CopperParts, 5);
            var copper      = new InventoryItem(BuiltinBlocks.Copper, 5);

            var bronzePlate = new InventoryItem(BuiltinBlocks.BronzePlate, 5);
            var bronze      = new InventoryItem(BuiltinBlocks.BronzeIngot, 5);

            var ironRivet = new InventoryItem(BuiltinBlocks.IronRivet, 5);
            var iron      = new InventoryItem(BuiltinBlocks.IronWrought, 5);

            var steelParts = new InventoryItem(BuiltinBlocks.SteelParts, 5);
            var steel      = new InventoryItem(BuiltinBlocks.SteelIngot, 5);

            List<InventoryItem> items;

            foreach (var a in ArmorLookup)
            {
                items = new List<InventoryItem>();

                // ----------------------------------------
                // Copper
                // ----------------------------------------

                if (a.Value.Metal == MetalType.Copper && a.Value.Slot == ArmorSlot.Helm)
                {
                    copperParts = new InventoryItem(BuiltinBlocks.CopperParts, 3);
                    copper      = new InventoryItem(BuiltinBlocks.Copper, 2);
                    items.AddRange(new[] {copper, copperParts, coppertools, clothing});
                }

                if (a.Value.Metal == MetalType.Copper && a.Value.Slot == ArmorSlot.Chest)
                {
                    copperParts = new InventoryItem(BuiltinBlocks.CopperParts, 5);
                    copper      = new InventoryItem(BuiltinBlocks.Copper, 5);
                    items.AddRange(new[] {copper, copperParts, coppertools, clothing});
                }

                if (a.Value.Metal == MetalType.Copper && a.Value.Slot == ArmorSlot.Gloves)
                {
                    copperParts = new InventoryItem(BuiltinBlocks.CopperParts, 2);
                    copper      = new InventoryItem(BuiltinBlocks.Copper, 2);
                    items.AddRange(new[] {copper, copperParts, coppertools, clothing});
                }

                if (a.Value.Metal == MetalType.Copper && a.Value.Slot == ArmorSlot.Legs)
                {
                    copperParts = new InventoryItem(BuiltinBlocks.CopperParts, 3);
                    copper      = new InventoryItem(BuiltinBlocks.Copper, 3);
                    items.AddRange(new[] {copper, copperParts, coppertools, clothing});
                }

                if (a.Value.Metal == MetalType.Copper && a.Value.Slot == ArmorSlot.Boots)
                {
                    copperParts = new InventoryItem(BuiltinBlocks.CopperParts, 2);
                    copper      = new InventoryItem(BuiltinBlocks.Copper, 2);
                    items.AddRange(new[] {copper, copperParts, coppertools, clothing});
                }

                if (a.Value.Metal == MetalType.Copper && a.Value.Slot == ArmorSlot.Shield)
                {
                    copperParts = new InventoryItem(BuiltinBlocks.CopperParts, 2);
                    copper      = new InventoryItem(BuiltinBlocks.Copper, 2);
                    items.AddRange(new[] {copper, copperParts, coppertools, clothing});
                }

                // ----------------------------------------
                // Bronze
                // ----------------------------------------

                if (a.Value.Metal == MetalType.Bronze && a.Value.Slot == ArmorSlot.Helm)
                {
                    bronzePlate = new InventoryItem(BuiltinBlocks.BronzePlate, 3);
                    bronze      = new InventoryItem(BuiltinBlocks.BronzeIngot, 2);
                    items.AddRange(new[] {bronze, bronzePlate, coppertools, clothing});
                }

                if (a.Value.Metal == MetalType.Bronze && a.Value.Slot == ArmorSlot.Chest)
                {
                    bronzePlate = new InventoryItem(BuiltinBlocks.BronzePlate, 5);
                    bronze      = new InventoryItem(BuiltinBlocks.BronzeIngot, 5);
                    items.AddRange(new[] {bronze, bronzePlate, coppertools, clothing});
                }

                if (a.Value.Metal == MetalType.Bronze && a.Value.Slot == ArmorSlot.Gloves)
                {
                    bronzePlate = new InventoryItem(BuiltinBlocks.BronzePlate, 2);
                    bronze      = new InventoryItem(BuiltinBlocks.BronzeIngot, 2);
                    items.AddRange(new[] {bronze, bronzePlate, coppertools, clothing});
                }

                if (a.Value.Metal == MetalType.Bronze && a.Value.Slot == ArmorSlot.Legs)
                {
                    bronzePlate = new InventoryItem(BuiltinBlocks.BronzePlate, 3);
                    bronze      = new InventoryItem(BuiltinBlocks.BronzeIngot, 3);
                    items.AddRange(new[] {bronze, bronzePlate, coppertools, clothing});
                }

                if (a.Value.Metal == MetalType.Bronze && a.Value.Slot == ArmorSlot.Boots)
                {
                    bronzePlate = new InventoryItem(BuiltinBlocks.BronzePlate, 2);
                    bronze      = new InventoryItem(BuiltinBlocks.BronzeIngot, 2);
                    items.AddRange(new[] {bronze, bronzePlate, coppertools, clothing});
                }

                if (a.Value.Metal == MetalType.Bronze && a.Value.Slot == ArmorSlot.Shield)
                {
                    bronzePlate = new InventoryItem(BuiltinBlocks.BronzePlate, 2);
                    bronze      = new InventoryItem(BuiltinBlocks.BronzeIngot, 2);
                    items.AddRange(new[] {bronze, bronzePlate, coppertools, clothing});
                }

                // ----------------------------------------
                // Iron
                // ----------------------------------------

                if (a.Value.Metal == MetalType.Iron && a.Value.Slot == ArmorSlot.Helm)
                {
                    ironRivet = new InventoryItem(BuiltinBlocks.IronRivet, 3);
                    iron      = new InventoryItem(BuiltinBlocks.IronIngot, 2);
                    items.AddRange(new[] {iron, ironRivet, coppertools, clothing});
                }

                if (a.Value.Metal == MetalType.Iron && a.Value.Slot == ArmorSlot.Chest)
                {
                    ironRivet = new InventoryItem(BuiltinBlocks.IronRivet, 5);
                    iron      = new InventoryItem(BuiltinBlocks.IronIngot, 5);
                    items.AddRange(new[] {iron, ironRivet, coppertools, clothing});
                }

                if (a.Value.Metal == MetalType.Iron && a.Value.Slot == ArmorSlot.Gloves)
                {
                    ironRivet = new InventoryItem(BuiltinBlocks.IronRivet, 2);
                    iron      = new InventoryItem(BuiltinBlocks.IronIngot, 2);
                    items.AddRange(new[] {iron, ironRivet, coppertools, clothing});
                }

                if (a.Value.Metal == MetalType.Iron && a.Value.Slot == ArmorSlot.Legs)
                {
                    ironRivet = new InventoryItem(BuiltinBlocks.IronRivet, 3);
                    iron      = new InventoryItem(BuiltinBlocks.IronIngot, 3);
                    items.AddRange(new[] {iron, ironRivet, coppertools, clothing});
                }

                if (a.Value.Metal == MetalType.Iron && a.Value.Slot == ArmorSlot.Boots)
                {
                    ironRivet = new InventoryItem(BuiltinBlocks.IronRivet, 2);
                    iron      = new InventoryItem(BuiltinBlocks.IronIngot, 2);
                    items.AddRange(new[] {iron, ironRivet, coppertools, clothing});
                }

                if (a.Value.Metal == MetalType.Iron && a.Value.Slot == ArmorSlot.Shield)
                {
                    ironRivet = new InventoryItem(BuiltinBlocks.IronRivet, 2);
                    iron      = new InventoryItem(BuiltinBlocks.IronIngot, 2);
                    items.AddRange(new[] {iron, ironRivet, coppertools, clothing});
                }

                // ----------------------------------------
                // Steel
                // ----------------------------------------

                if (a.Value.Metal == MetalType.Steel && a.Value.Slot == ArmorSlot.Helm)
                {
                    steelParts = new InventoryItem(BuiltinBlocks.SteelParts, 3);
                    steel      = new InventoryItem(BuiltinBlocks.SteelIngot, 2);
                    items.AddRange(new[] {steel, steelParts, coppertools, clothing});
                }

                if (a.Value.Metal == MetalType.Steel && a.Value.Slot == ArmorSlot.Chest)
                {
                    steelParts = new InventoryItem(BuiltinBlocks.SteelParts, 5);
                    steel      = new InventoryItem(BuiltinBlocks.SteelIngot, 5);
                    items.AddRange(new[] {steel, steelParts, coppertools, clothing});
                }

                if (a.Value.Metal == MetalType.Steel && a.Value.Slot == ArmorSlot.Gloves)
                {
                    steelParts = new InventoryItem(BuiltinBlocks.SteelParts, 2);
                    steel      = new InventoryItem(BuiltinBlocks.SteelIngot, 2);
                    items.AddRange(new[] {steel, steelParts, coppertools, clothing});
                }

                if (a.Value.Metal == MetalType.Steel && a.Value.Slot == ArmorSlot.Legs)
                {
                    steelParts = new InventoryItem(BuiltinBlocks.SteelParts, 3);
                    steel      = new InventoryItem(BuiltinBlocks.SteelIngot, 3);
                    items.AddRange(new[] {steel, steelParts, coppertools, clothing});
                }

                if (a.Value.Metal == MetalType.Steel && a.Value.Slot == ArmorSlot.Boots)
                {
                    steelParts = new InventoryItem(BuiltinBlocks.SteelParts, 2);
                    steel      = new InventoryItem(BuiltinBlocks.SteelIngot, 2);
                    items.AddRange(new[] {steel, steelParts, coppertools, clothing});
                }

                if (a.Value.Metal == MetalType.Steel && a.Value.Slot == ArmorSlot.Shield)
                {
                    steelParts = new InventoryItem(BuiltinBlocks.SteelParts, 2);
                    steel      = new InventoryItem(BuiltinBlocks.SteelIngot, 2);
                    items.AddRange(new[] {steel, steelParts, coppertools, clothing});
                }

                var invItem = new InventoryItem(a.Value.ItemType.ItemIndex);
                var recipe  = new Recipe(a.Value.ItemType.name, items, invItem, 5, false, -100);

                RecipeStorage.AddOptionalLimitTypeRecipe(JOB_METALSMITH, recipe);
            }
        }

        [ModLoader.ModCallbackAttribute(ModLoader.EModCallbackType.AfterAddingBaseTypes,
            GameLoader.NAMESPACE + ".Armor.AddArmor")]
        [ModLoader.ModCallbackDependsOnAttribute("pipliz.blocknpcs.addlittypes")]
        public static void AddArmor(Dictionary<string, ItemTypesServer.ItemTypeRaw> items)
        {
            try
            {
                // ----------------------------------------
                // Copper
                // ----------------------------------------

                // Helm
                var copperHelmName = GameLoader.NAMESPACE + ".CopperHelm";
                var copperHelmNode = new JSONNode();
                copperHelmNode["icon"]        = new JSONNode(GameLoader.ICON_PATH + "CopperHelm.png");
                copperHelmNode["isPlaceable"] = new JSONNode(false);

                var categories = new JSONNode(NodeType.Array);
                categories.AddToArray(new JSONNode("armor"));
                copperHelmNode.SetAs("categories", categories);

                var copperHelm = new ItemTypesServer.ItemTypeRaw(copperHelmName, copperHelmNode);
                items.Add(copperHelmName, copperHelm);

                ArmorLookup.Add(copperHelm.ItemIndex,
                                new ArmorMetadata(0.05f, 15, MetalType.Copper, copperHelm, ArmorSlot.Helm));

                // Chest
                var copperChestName = GameLoader.NAMESPACE + ".CopperChest";
                var copperChestNode = new JSONNode();
                copperChestNode["icon"]        = new JSONNode(GameLoader.ICON_PATH + "CopperChest.png");
                copperChestNode["isPlaceable"] = new JSONNode(false);

                copperChestNode.SetAs("categories", categories);

                var copperChest = new ItemTypesServer.ItemTypeRaw(copperChestName, copperChestNode);
                items.Add(copperChestName, copperChest);

                ArmorLookup.Add(copperChest.ItemIndex,
                                new ArmorMetadata(.1f, 25, MetalType.Copper, copperChest, ArmorSlot.Chest));

                // Gloves
                var copperGlovesName = GameLoader.NAMESPACE + ".CopperGloves";
                var copperGlovesNode = new JSONNode();
                copperGlovesNode["icon"]        = new JSONNode(GameLoader.ICON_PATH + "CopperGloves.png");
                copperGlovesNode["isPlaceable"] = new JSONNode(false);

                copperGlovesNode.SetAs("categories", categories);

                var copperGloves = new ItemTypesServer.ItemTypeRaw(copperGlovesName, copperGlovesNode);
                items.Add(copperGlovesName, copperGloves);

                ArmorLookup.Add(copperGloves.ItemIndex,
                                new ArmorMetadata(0.025f, 10, MetalType.Copper, copperGloves, ArmorSlot.Gloves));

                // Legs
                var copperLegsName = GameLoader.NAMESPACE + ".CopperLegs";
                var copperLegsNode = new JSONNode();
                copperLegsNode["icon"]        = new JSONNode(GameLoader.ICON_PATH + "CopperLegs.png");
                copperLegsNode["isPlaceable"] = new JSONNode(false);

                copperLegsNode.SetAs("categories", categories);

                var copperLegs = new ItemTypesServer.ItemTypeRaw(copperLegsName, copperLegsNode);
                items.Add(copperLegsName, copperLegs);

                ArmorLookup.Add(copperLegs.ItemIndex,
                                new ArmorMetadata(0.07f, 20, MetalType.Copper, copperLegs, ArmorSlot.Legs));

                // Boots
                var copperBootsName = GameLoader.NAMESPACE + ".CopperBoots";
                var copperBootsNode = new JSONNode();
                copperBootsNode["icon"]        = new JSONNode(GameLoader.ICON_PATH + "CopperBoots.png");
                copperBootsNode["isPlaceable"] = new JSONNode(false);

                copperBootsNode.SetAs("categories", categories);

                var copperBoots = new ItemTypesServer.ItemTypeRaw(copperBootsName, copperBootsNode);
                items.Add(copperBootsName, copperBoots);

                ArmorLookup.Add(copperBoots.ItemIndex,
                                new ArmorMetadata(0.025f, 10, MetalType.Copper, copperBoots, ArmorSlot.Boots));

                // Shield
                var copperShieldName = GameLoader.NAMESPACE + ".CopperShield";
                var copperShieldNode = new JSONNode();
                copperShieldNode["icon"]        = new JSONNode(GameLoader.ICON_PATH + "CopperShield.png");
                copperShieldNode["isPlaceable"] = new JSONNode(false);

                copperShieldNode.SetAs("categories", categories);

                var copperShield = new ItemTypesServer.ItemTypeRaw(copperShieldName, copperShieldNode);
                items.Add(copperShieldName, copperShield);

                ArmorLookup.Add(copperShield.ItemIndex,
                                new ArmorMetadata(0.05f, 30, MetalType.Copper, copperShield, ArmorSlot.Shield));

                // ----------------------------------------
                // Bronze
                // ----------------------------------------

                // Helm
                var bronzeHelmName = GameLoader.NAMESPACE + ".BronzeHelm";
                var bronzeHelmNode = new JSONNode();
                bronzeHelmNode["icon"]        = new JSONNode(GameLoader.ICON_PATH + "BronzeHelm.png");
                bronzeHelmNode["isPlaceable"] = new JSONNode(false);

                bronzeHelmNode.SetAs("categories", categories);

                var bronzeHelm = new ItemTypesServer.ItemTypeRaw(bronzeHelmName, bronzeHelmNode);
                items.Add(bronzeHelmName, bronzeHelm);

                ArmorLookup.Add(bronzeHelm.ItemIndex,
                                new ArmorMetadata(0.07f, 20, MetalType.Bronze, bronzeHelm, ArmorSlot.Helm));

                // Chest
                var bronzeChestName = GameLoader.NAMESPACE + ".BronzeChest";
                var bronzeChestNode = new JSONNode();
                bronzeChestNode["icon"]        = new JSONNode(GameLoader.ICON_PATH + "BronzeChest.png");
                bronzeChestNode["isPlaceable"] = new JSONNode(false);

                bronzeChestNode.SetAs("categories", categories);

                var bronzeChest = new ItemTypesServer.ItemTypeRaw(bronzeChestName, bronzeChestNode);
                items.Add(bronzeChestName, bronzeChest);

                ArmorLookup.Add(bronzeChest.ItemIndex,
                                new ArmorMetadata(.15f, 30, MetalType.Bronze, bronzeChest, ArmorSlot.Chest));

                // Gloves
                var bronzeGlovesName = GameLoader.NAMESPACE + ".BronzeGloves";
                var bronzeGlovesNode = new JSONNode();
                bronzeGlovesNode["icon"]        = new JSONNode(GameLoader.ICON_PATH + "BronzeGloves.png");
                bronzeGlovesNode["isPlaceable"] = new JSONNode(false);

                bronzeGlovesNode.SetAs("categories", categories);

                var bronzeGloves = new ItemTypesServer.ItemTypeRaw(bronzeGlovesName, bronzeGlovesNode);
                items.Add(bronzeGlovesName, bronzeGloves);

                ArmorLookup.Add(bronzeGloves.ItemIndex,
                                new ArmorMetadata(0.04f, 15, MetalType.Bronze, bronzeGloves, ArmorSlot.Gloves));

                // Legs
                var bronzeLegsName = GameLoader.NAMESPACE + ".BronzeLegs";
                var bronzeLegsNode = new JSONNode();
                bronzeLegsNode["icon"]        = new JSONNode(GameLoader.ICON_PATH + "BronzeLegs.png");
                bronzeLegsNode["isPlaceable"] = new JSONNode(false);

                bronzeLegsNode.SetAs("categories", categories);

                var bronzeLegs = new ItemTypesServer.ItemTypeRaw(bronzeLegsName, bronzeLegsNode);
                items.Add(bronzeLegsName, bronzeLegs);

                ArmorLookup.Add(bronzeLegs.ItemIndex,
                                new ArmorMetadata(0.09f, 25, MetalType.Bronze, bronzeLegs, ArmorSlot.Legs));

                // Boots
                var bronzeBootsName = GameLoader.NAMESPACE + ".BronzeBoots";
                var bronzeBootsNode = new JSONNode();
                bronzeBootsNode["icon"]        = new JSONNode(GameLoader.ICON_PATH + "BronzeBoots.png");
                bronzeBootsNode["isPlaceable"] = new JSONNode(false);

                bronzeBootsNode.SetAs("categories", categories);

                var bronzeBoots = new ItemTypesServer.ItemTypeRaw(bronzeBootsName, bronzeBootsNode);
                items.Add(bronzeBootsName, bronzeBoots);

                ArmorLookup.Add(bronzeBoots.ItemIndex,
                                new ArmorMetadata(0.04f, 15, MetalType.Bronze, bronzeBoots, ArmorSlot.Boots));

                // Shield
                var bronzeShieldName = GameLoader.NAMESPACE + ".BronzeShield";
                var bronzeShieldNode = new JSONNode();
                bronzeShieldNode["icon"]        = new JSONNode(GameLoader.ICON_PATH + "BronzeShield.png");
                bronzeShieldNode["isPlaceable"] = new JSONNode(false);

                bronzeShieldNode.SetAs("categories", categories);

                var bronzeShield = new ItemTypesServer.ItemTypeRaw(bronzeShieldName, bronzeShieldNode);
                items.Add(bronzeShieldName, bronzeShield);

                ArmorLookup.Add(bronzeShield.ItemIndex,
                                new ArmorMetadata(0.07f, 40, MetalType.Bronze, bronzeShield, ArmorSlot.Shield));

                // ----------------------------------------
                // Iron
                // ----------------------------------------

                // Helm
                var ironHelmName = GameLoader.NAMESPACE + ".IronHelm";
                var ironHelmNode = new JSONNode();
                ironHelmNode["icon"]        = new JSONNode(GameLoader.ICON_PATH + "IronHelm.png");
                ironHelmNode["isPlaceable"] = new JSONNode(false);

                ironHelmNode.SetAs("categories", categories);

                var ironHelm = new ItemTypesServer.ItemTypeRaw(ironHelmName, ironHelmNode);
                items.Add(ironHelmName, ironHelm);

                ArmorLookup.Add(ironHelm.ItemIndex,
                                new ArmorMetadata(0.09f, 30, MetalType.Iron, ironHelm, ArmorSlot.Helm));

                // Chest
                var ironChestName = GameLoader.NAMESPACE + ".IronChest";
                var ironChestNode = new JSONNode();
                ironChestNode["icon"]        = new JSONNode(GameLoader.ICON_PATH + "IronChest.png");
                ironChestNode["isPlaceable"] = new JSONNode(false);

                ironChestNode.SetAs("categories", categories);

                var ironChest = new ItemTypesServer.ItemTypeRaw(ironChestName, ironChestNode);
                items.Add(ironChestName, ironChest);

                ArmorLookup.Add(ironChest.ItemIndex,
                                new ArmorMetadata(.2f, 40, MetalType.Iron, ironChest, ArmorSlot.Chest));

                // Gloves
                var ironGlovesName = GameLoader.NAMESPACE + ".IronGloves";
                var ironGlovesNode = new JSONNode();
                ironGlovesNode["icon"]        = new JSONNode(GameLoader.ICON_PATH + "IronGloves.png");
                ironGlovesNode["isPlaceable"] = new JSONNode(false);

                ironGlovesNode.SetAs("categories", categories);

                var ironGloves = new ItemTypesServer.ItemTypeRaw(ironGlovesName, ironGlovesNode);
                items.Add(ironGlovesName, ironGloves);

                ArmorLookup.Add(ironGloves.ItemIndex,
                                new ArmorMetadata(0.055f, 25, MetalType.Iron, ironGloves, ArmorSlot.Gloves));

                // Legs
                var ironLegsName = GameLoader.NAMESPACE + ".IronLegs";
                var ironLegsNode = new JSONNode();
                ironLegsNode["icon"]        = new JSONNode(GameLoader.ICON_PATH + "IronLegs.png");
                ironLegsNode["isPlaceable"] = new JSONNode(false);

                ironLegsNode.SetAs("categories", categories);

                var ironLegs = new ItemTypesServer.ItemTypeRaw(ironLegsName, ironLegsNode);
                items.Add(ironLegsName, ironLegs);

                ArmorLookup.Add(ironLegs.ItemIndex,
                                new ArmorMetadata(0.11f, 35, MetalType.Iron, ironLegs, ArmorSlot.Legs));

                // Boots
                var ironBootsName = GameLoader.NAMESPACE + ".IronBoots";
                var ironBootsNode = new JSONNode();
                ironBootsNode["icon"]        = new JSONNode(GameLoader.ICON_PATH + "IronBoots.png");
                ironBootsNode["isPlaceable"] = new JSONNode(false);

                ironBootsNode.SetAs("categories", categories);

                var ironBoots = new ItemTypesServer.ItemTypeRaw(ironBootsName, ironBootsNode);
                items.Add(ironBootsName, ironBoots);

                ArmorLookup.Add(ironBoots.ItemIndex,
                                new ArmorMetadata(0.055f, 25, MetalType.Iron, ironBoots, ArmorSlot.Boots));

                // Shield
                var ironShieldName = GameLoader.NAMESPACE + ".IronShield";
                var ironShieldNode = new JSONNode();
                ironShieldNode["icon"]        = new JSONNode(GameLoader.ICON_PATH + "IronShield.png");
                ironShieldNode["isPlaceable"] = new JSONNode(false);

                ironShieldNode.SetAs("categories", categories);

                var ironShield = new ItemTypesServer.ItemTypeRaw(ironShieldName, ironShieldNode);
                items.Add(ironShieldName, ironShield);

                ArmorLookup.Add(ironShield.ItemIndex,
                                new ArmorMetadata(0.1f, 50, MetalType.Iron, ironShield, ArmorSlot.Shield));

                // ----------------------------------------
                // Steel
                // ----------------------------------------

                // Helm
                var steelHelmName = GameLoader.NAMESPACE + ".SteelHelm";
                var steelHelmNode = new JSONNode();
                steelHelmNode["icon"]        = new JSONNode(GameLoader.ICON_PATH + "SteelHelm.png");
                steelHelmNode["isPlaceable"] = new JSONNode(false);

                steelHelmNode.SetAs("categories", categories);

                var steelHelm = new ItemTypesServer.ItemTypeRaw(steelHelmName, steelHelmNode);
                items.Add(steelHelmName, steelHelm);

                ArmorLookup.Add(steelHelm.ItemIndex,
                                new ArmorMetadata(0.11f, 40, MetalType.Steel, steelHelm, ArmorSlot.Helm));

                // Chest
                var steelChestName = GameLoader.NAMESPACE + ".SteelChest";
                var steelChestNode = new JSONNode();
                steelChestNode["icon"]        = new JSONNode(GameLoader.ICON_PATH + "SteelChest.png");
                steelChestNode["isPlaceable"] = new JSONNode(false);

                steelChestNode.SetAs("categories", categories);

                var steelChest = new ItemTypesServer.ItemTypeRaw(steelChestName, steelChestNode);
                items.Add(steelChestName, steelChest);

                ArmorLookup.Add(steelChest.ItemIndex,
                                new ArmorMetadata(.3f, 50, MetalType.Steel, steelChest, ArmorSlot.Chest));

                // Gloves
                var steelGlovesName = GameLoader.NAMESPACE + ".SteelGloves";
                var steelGlovesNode = new JSONNode();
                steelGlovesNode["icon"]        = new JSONNode(GameLoader.ICON_PATH + "SteelGloves.png");
                steelGlovesNode["isPlaceable"] = new JSONNode(false);

                steelGlovesNode.SetAs("categories", categories);

                var steelGloves = new ItemTypesServer.ItemTypeRaw(steelGlovesName, steelGlovesNode);
                items.Add(steelGlovesName, steelGloves);

                ArmorLookup.Add(steelGloves.ItemIndex,
                                new ArmorMetadata(0.07f, 35, MetalType.Steel, steelGloves, ArmorSlot.Gloves));

                // Legs
                var steelLegsName = GameLoader.NAMESPACE + ".SteelLegs";
                var steelLegsNode = new JSONNode();
                steelLegsNode["icon"]        = new JSONNode(GameLoader.ICON_PATH + "SteelLegs.png");
                steelLegsNode["isPlaceable"] = new JSONNode(false);

                steelLegsNode.SetAs("categories", categories);

                var steelLegs = new ItemTypesServer.ItemTypeRaw(steelLegsName, steelLegsNode);
                items.Add(steelLegsName, steelLegs);

                ArmorLookup.Add(steelLegs.ItemIndex,
                                new ArmorMetadata(0.13f, 40, MetalType.Steel, steelLegs, ArmorSlot.Legs));

                // Boots
                var steelBootsName = GameLoader.NAMESPACE + ".SteelBoots";
                var steelBootsNode = new JSONNode();
                steelBootsNode["icon"]        = new JSONNode(GameLoader.ICON_PATH + "SteelBoots.png");
                steelBootsNode["isPlaceable"] = new JSONNode(false);

                steelBootsNode.SetAs("categories", categories);

                var steelBoots = new ItemTypesServer.ItemTypeRaw(steelBootsName, steelBootsNode);
                items.Add(steelBootsName, steelBoots);

                ArmorLookup.Add(steelBoots.ItemIndex,
                                new ArmorMetadata(0.07f, 35, MetalType.Steel, steelBoots, ArmorSlot.Boots));

                // Shield
                var steelShieldName = GameLoader.NAMESPACE + ".SteelShield";
                var steelShieldNode = new JSONNode();
                steelShieldNode["icon"]        = new JSONNode(GameLoader.ICON_PATH + "SteelShield.png");
                steelShieldNode["isPlaceable"] = new JSONNode(false);

                steelShieldNode.SetAs("categories", categories);

                var steelShield = new ItemTypesServer.ItemTypeRaw(steelShieldName, steelShieldNode);
                items.Add(steelShieldName, steelShield);

                ArmorLookup.Add(steelShield.ItemIndex,
                                new ArmorMetadata(0.12f, 60, MetalType.Steel, steelShield, ArmorSlot.Shield));

                ArmorLookup = ArmorLookup.OrderBy(kvp => kvp.Value.Metal).ThenBy(kvp => kvp.Value.ArmorRating)
                                         .ToDictionary(k => k.Key, v => v.Value);
            }
            catch (Exception ex)
            {
                PandaLogger.LogError(ex);
            }
        }

        public class ArmorMetadata
        {
            public ArmorMetadata(float                       armorRating, int       durability, MetalType metal,
                                 ItemTypesServer.ItemTypeRaw itemType,    ArmorSlot slot)
            {
                ArmorRating = armorRating;
                Durability  = durability;
                Metal       = metal;
                ItemType    = itemType;
                Slot        = slot;
            }

            public float ArmorRating { get; }

            public int Durability { get; set; }

            public MetalType Metal { get; }

            public ItemTypesServer.ItemTypeRaw ItemType { get; }

            public ArmorSlot Slot { get; }
        }
    }
}