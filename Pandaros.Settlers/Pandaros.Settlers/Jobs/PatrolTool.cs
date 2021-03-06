﻿using System;
using System.Collections.Generic;
using BlockTypes.Builtin;
using NPC;
using Pandaros.Settlers.Entities;
using Pandaros.Settlers.Items;
using Pandaros.Settlers.Research;
using Pipliz;
using Pipliz.JSON;
using Shared;

namespace Pandaros.Settlers.Jobs
{
    public enum PatrolType
    {
        RoundRobin,
        Zipper
    }

    [ModLoader.ModManagerAttribute]
    public static class PatrolTool
    {
        private static readonly Dictionary<Players.Player, List<KnightState>> _loadedKnights =
            new Dictionary<Players.Player, List<KnightState>>();

        public static ItemTypesServer.ItemTypeRaw Item { get; private set; }
        public static ItemTypesServer.ItemTypeRaw PatrolFlag { get; private set; }

        [ModLoader.ModCallbackAttribute(ModLoader.EModCallbackType.AfterItemTypesDefined,
            GameLoader.NAMESPACE + ".Jobs.PatrolTool.RegisterPatrolTool")]
        public static void RegisterPatrolTool()
        {
            var planks = new InventoryItem(BuiltinBlocks.Planks, 2);
            var carpet = new InventoryItem(BuiltinBlocks.CarpetRed, 2);

            var recipe = new Recipe(PatrolFlag.name,
                                    new List<InventoryItem> {planks, carpet},
                                    new InventoryItem(PatrolFlag.ItemIndex, 2),
                                    5);

            RecipeStorage.AddOptionalLimitTypeRecipe(ItemFactory.JOB_CRAFTER, recipe);
        }


        [ModLoader.ModCallbackAttribute(ModLoader.EModCallbackType.AfterSelectedWorld,
            GameLoader.NAMESPACE + ".Jobs.PatrolTool.AddTextures")]
        [ModLoader.ModCallbackProvidesForAttribute("pipliz.server.registertexturemappingtextures")]
        public static void AddTextures()
        {
            var flagTextureMapping = new ItemTypesServer.TextureMapping(new JSONNode());
            flagTextureMapping.AlbedoPath = GameLoader.BLOCKS_ALBEDO_PATH + "PatrolFlag.png";

            ItemTypesServer.SetTextureMapping(GameLoader.NAMESPACE + ".PatrolFlag", flagTextureMapping);
        }


        [ModLoader.ModCallbackAttribute(ModLoader.EModCallbackType.AfterAddingBaseTypes,
            GameLoader.NAMESPACE + ".Jobs.PatrolTool.AddPatrolTool")]
        [ModLoader.ModCallbackDependsOnAttribute("pipliz.blocknpcs.addlittypes")]
        public static void AddPatrolTool(Dictionary<string, ItemTypesServer.ItemTypeRaw> items)
        {
            var patrolToolName = GameLoader.NAMESPACE + ".PatrolTool";
            var patrolToolNode = new JSONNode();
            patrolToolNode["icon"]        = new JSONNode(GameLoader.ICON_PATH + "KnightPatrolTool.png");
            patrolToolNode["isPlaceable"] = new JSONNode(false);

            var categories = new JSONNode(NodeType.Array);
            categories.AddToArray(new JSONNode("job"));
            patrolToolNode.SetAs("categories", categories);

            Item = new ItemTypesServer.ItemTypeRaw(patrolToolName, patrolToolNode);
            items.Add(patrolToolName, Item);

            var patrolFlagName = GameLoader.NAMESPACE + ".PatrolFlag";
            var patrolFlagNode = new JSONNode();
            patrolFlagNode["icon"]        = new JSONNode(GameLoader.ICON_PATH + "PatrolFlagItem.png");
            patrolFlagNode["isPlaceable"] = new JSONNode(false);
            patrolFlagNode.SetAs("onRemoveAmount", 0);
            patrolFlagNode.SetAs("isSolid", false);
            patrolFlagNode.SetAs("sideall", "SELF");
            patrolFlagNode.SetAs("mesh", GameLoader.MESH_PATH + "PatrolFlag.obj");

            var patrolFlagCategories = new JSONNode(NodeType.Array);
            patrolFlagCategories.AddToArray(new JSONNode("job"));
            patrolFlagNode.SetAs("categories", patrolFlagCategories);

            PatrolFlag = new ItemTypesServer.ItemTypeRaw(patrolFlagName, patrolFlagNode);
            items.Add(patrolFlagName, PatrolFlag);
        }

        [ModLoader.ModCallbackAttribute(ModLoader.EModCallbackType.OnPlayerConnectedLate,
            GameLoader.NAMESPACE + ".Jobs.PatrolTool.OnPlayerConnectedLate")]
        [ModLoader.ModCallbackDependsOnAttribute(GameLoader.NAMESPACE + ".SettlerManager.OnPlayerConnectedLate")]
        public static void OnPlayerConnectedLate(Players.Player p)
        {
            if (p.GetTempValues(true).GetOrDefault(PandaResearch.GetResearchKey(PandaResearch.Knights), 0f) == 1f)
                GivePlayerPatrolTool(p);
        }

        [ModLoader.ModCallbackAttribute(ModLoader.EModCallbackType.OnPlayerRespawn,
            GameLoader.NAMESPACE + ".Jobs.PatrolTool.OnPlayerRespawn")]
        public static void OnPlayerRespawn(Players.Player p)
        {
            if (p.GetTempValues(true).GetOrDefault(PandaResearch.GetResearchKey(PandaResearch.Knights), 0f) == 1f)
                GivePlayerPatrolTool(p);
        }

        public static void GivePlayerPatrolTool(Players.Player p)
        {
            var playerStockpile = Stockpile.GetStockPile(p);
            var hasTool         = false;

            foreach (var item in Inventory.GetInventory(p).Items)
                if (item.Type == Item.ItemIndex)
                {
                    hasTool = true;
                    break;
                }

            if (!hasTool)
                hasTool = playerStockpile.Contains(Item.ItemIndex);

            if (!hasTool)
                playerStockpile.Add(new InventoryItem(Item.ItemIndex));
        }

        [ModLoader.ModCallbackAttribute(ModLoader.EModCallbackType.OnLoadingPlayer,
            GameLoader.NAMESPACE + ".Jobs.OnLoadingPlayer")]
        public static void OnLoadingPlayer(JSONNode n, Players.Player p)
        {
            if (n.TryGetChild(GameLoader.NAMESPACE + ".Knights", out var knightsNode))
                foreach (var knightNode in knightsNode.LoopArray())
                {
                    var points = new List<Vector3Int>();

                    foreach (var point in knightNode["PatrolPoints"].LoopArray())
                        points.Add((Vector3Int) point);

                    if (knightNode.TryGetAs("PatrolType", out string patrolTypeStr))
                    {
                        var patrolMode = (PatrolType) Enum.Parse(typeof(PatrolType), patrolTypeStr);

                        if (!_loadedKnights.ContainsKey(p))
                            _loadedKnights.Add(p, new List<KnightState>());

                        _loadedKnights[p].Add(new KnightState {PatrolPoints = points, patrolType = patrolMode});
                    }
                }

            JobTracker.Update();
        }

        [ModLoader.ModCallbackAttribute(ModLoader.EModCallbackType.AfterWorldLoad,
            GameLoader.NAMESPACE + ".Jobs.PatrolTool.AfterWorldLoad")]
        [ModLoader.ModCallbackProvidesForAttribute("pipliz.apiprovider.jobs.load")]
        public static void AfterWorldLoad()
        {
            foreach (var k in _loadedKnights)
            foreach (var kp in k.Value)
            {
                var knight = new Knight(kp.PatrolPoints, k.Key);
                knight.PatrolType = kp.patrolType;
                JobTracker.Add(knight);
            }
        }

        [ModLoader.ModCallbackAttribute(ModLoader.EModCallbackType.OnSavingPlayer,
            GameLoader.NAMESPACE + ".Jobs.PatrolTool.OnSavingPlayer")]
        public static void OnSavingPlayer(JSONNode n, Players.Player p)
        {
            if (Knight.Knights.ContainsKey(p))
            {
                if (n.HasChild(GameLoader.NAMESPACE + ".Knights"))
                    n.RemoveChild(GameLoader.NAMESPACE + ".Knights");

                var knightsNode = new JSONNode(NodeType.Array);

                foreach (var knight in Knight.Knights[p])
                {
                    var knightNode = new JSONNode()
                       .SetAs(nameof(knight.PatrolType), knight.PatrolType);

                    var patrolPoints = new JSONNode(NodeType.Array);

                    foreach (var point in knight.PatrolPoints)
                        patrolPoints.AddToArray((JSONNode) point);

                    knightNode.SetAs(nameof(knight.PatrolPoints), patrolPoints);

                    knightsNode.AddToArray(knightNode);
                }

                n[GameLoader.NAMESPACE + ".Knights"] = knightsNode;
            }
        }

        [ModLoader.ModCallbackAttribute(ModLoader.EModCallbackType.OnPlayerClicked,
            GameLoader.NAMESPACE + ".Jobs.PlacePatrol")]
        public static void PlacePatrol(Players.Player player, Box<PlayerClickedData> boxedData)
        {
            if (boxedData.item1.IsConsumed)
                return;

            var click      = boxedData.item1;
            var rayCastHit = click.rayCastHit;
            var state      = PlayerState.GetPlayerState(player);

            if (rayCastHit.rayHitType == RayHitType.Block &&
                click.typeSelected == Item.ItemIndex)
            {
                var stockpile = Stockpile.GetStockPile(player);

                if (click.typeHit != PatrolFlag.ItemIndex)
                {
                    var flagPoint = rayCastHit.voxelHit.Add(0, 1, 0);

                    if (click.clickType == PlayerClickedData.ClickType.Left)
                    {
                        var hasFlags = player.TakeItemFromInventory(PatrolFlag.ItemIndex);

                        if (!hasFlags)
                        {
                            var playerStock = Stockpile.GetStockPile(player);

                            if (playerStock.Contains(PatrolFlag.ItemIndex))
                            {
                                hasFlags = true;
                                playerStock.TryRemove(PatrolFlag.ItemIndex);
                            }
                        }

                        if (!hasFlags)
                        {
                            PandaChat.Send(player, "You have no patrol flags in your stockpile or inventory.",
                                           ChatColor.orange);
                        }
                        else
                        {
                            state.FlagsPlaced.Add(flagPoint);
                            ServerManager.TryChangeBlock(flagPoint, PatrolFlag.ItemIndex);

                            PandaChat.Send(player,
                                           $"Patrol Point number {state.FlagsPlaced.Count} Registered! Right click to create Job.",
                                           ChatColor.orange);
                        }
                    }
                }
                else
                {
                    foreach (var knight in Knight.Knights[player])
                        if (knight.PatrolPoints.Contains(rayCastHit.voxelHit))
                        {
                            var patrol = string.Empty;

                            if (knight.PatrolType == PatrolType.RoundRobin)
                            {
                                patrol =
                                    "The knight will patrol from the first to last point, then, work its way backwords to the first. Good for patrolling a secion of a wall";

                                knight.PatrolType = PatrolType.Zipper;
                            }
                            else
                            {
                                patrol =
                                    "The knight will patrol from the first to last point, start over at the first point. Good for circles";

                                knight.PatrolType = PatrolType.RoundRobin;
                            }

                            PandaChat.Send(player, $"Patrol type set to {knight.PatrolType}!", ChatColor.orange);
                            PandaChat.Send(player, patrol, ChatColor.orange);
                            break;
                        }
                }
            }

            if (click.typeSelected == Item.ItemIndex && click.clickType == PlayerClickedData.ClickType.Right)
            {
                if (state.FlagsPlaced.Count == 0)
                {
                    PandaChat.Send(player, "You must place patrol flags using left click before setting the patrol.",
                                   ChatColor.orange);
                }
                else
                {
                    var knight = new Knight(new List<Vector3Int>(state.FlagsPlaced), player);
                    state.FlagsPlaced.Clear();
                    JobTracker.Add(knight);

                    PandaChat.Send(player,
                                   "Patrol Active! To stop the patrol pick up any of the patrol flags in the patrol.",
                                   ChatColor.orange);

                    JobTracker.Update();
                }
            }
        }

        [ModLoader.ModCallbackAttribute(ModLoader.EModCallbackType.OnTryChangeBlock,
            GameLoader.NAMESPACE + ".Jobs.OnTryChangeBlockUser")]
        public static void OnTryChangeBlockUser(ModLoader.OnTryChangeBlockData d)
        {
            if (d.CallbackState == ModLoader.OnTryChangeBlockData.ECallbackState.Cancelled)
                return;

            if (d.TypeOld == PatrolFlag.ItemIndex)
            {
                var toRemove = default(Knight);

                var state     = PlayerState.GetPlayerState(d.RequestedByPlayer);
                var stockpile = Stockpile.GetStockPile(d.RequestedByPlayer);

                if (!Knight.Knights.ContainsKey(d.RequestedByPlayer))
                    Knight.Knights.Add(d.RequestedByPlayer, new List<Knight>());

                foreach (var knight in Knight.Knights[d.RequestedByPlayer])
                    try
                    {
                        if (knight.PatrolPoints.Contains(d.Position))
                        {
                            knight.OnRemove();

                            foreach (var flagPoint in knight.PatrolPoints)
                                if (flagPoint != d.Position)
                                    if (World.TryGetTypeAt(flagPoint, out var objType) &&
                                        objType == PatrolFlag.ItemIndex)
                                    {
                                        ServerManager.TryChangeBlock(flagPoint, BuiltinBlocks.Air);
                                        stockpile.Add(PatrolFlag.ItemIndex);
                                    }

                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        PandaLogger.LogError(ex);
                    }

                if (toRemove != default(Knight))
                {
                    PandaChat.Send(d.RequestedByPlayer,
                                   $"Patrol with {toRemove.PatrolPoints.Count} patrol points no longer active.",
                                   ChatColor.orange);

                    Knight.Knights[d.RequestedByPlayer].Remove(toRemove);

                    if (((JobTracker.JobFinder) JobTracker.GetOrCreateJobFinder(d.RequestedByPlayer))
                       .openJobs.Contains(toRemove))
                        ((JobTracker.JobFinder) JobTracker.GetOrCreateJobFinder(d.RequestedByPlayer))
                           .openJobs.Remove(toRemove);

                    JobTracker.Update();
                }

                if (state.FlagsPlaced.Contains(d.Position))
                {
                    state.FlagsPlaced.Remove(d.Position);
                    ServerManager.TryChangeBlock(d.Position, BuiltinBlocks.Air);
                }

                stockpile.Add(PatrolFlag.ItemIndex);
            }
        }

        internal class KnightState
        {
            internal List<Vector3Int> PatrolPoints;
            internal PatrolType patrolType;
        }
    }
}