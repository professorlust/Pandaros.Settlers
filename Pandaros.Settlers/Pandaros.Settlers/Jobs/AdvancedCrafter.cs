﻿using System.Collections.Generic;
using BlockTypes.Builtin;
using Pandaros.Settlers.Items;
using Pipliz.JSON;
using Pipliz.Mods.APIProvider.Jobs;
using Server.NPCs;
using UnityEngine;

namespace Pandaros.Settlers.Jobs
{
    [ModLoader.ModManagerAttribute]
    public static class AdvancedCrafterRegister
    {
        public static string JOB_NAME = GameLoader.NAMESPACE + ".AdvancedCrafter";
        public static string JOB_ITEM_KEY = GameLoader.NAMESPACE + ".AdvancedCraftingTable";
        public static string JOB_RECIPE = JOB_ITEM_KEY + ".recipe";

        [ModLoader.ModCallbackAttribute(ModLoader.EModCallbackType.AfterItemTypesDefined,
            GameLoader.NAMESPACE + ".AdvancedCrafterRegister.RegisterJobs")]
        [ModLoader.ModCallbackProvidesForAttribute("pipliz.apiprovider.jobs.resolvetypes")]
        public static void RegisterJobs()
        {
            BlockJobManagerTracker.Register<AdvancedCrafterJob>(JOB_ITEM_KEY);
        }

        [ModLoader.ModCallbackAttribute(ModLoader.EModCallbackType.AfterSelectedWorld,
            GameLoader.NAMESPACE + ".AdvancedCrafterRegister.AddTextures")]
        [ModLoader.ModCallbackProvidesForAttribute("pipliz.server.registertexturemappingtextures")]
        public static void AddTextures()
        {
            var textureMapping = new ItemTypesServer.TextureMapping(new JSONNode());
            textureMapping.AlbedoPath = GameLoader.BLOCKS_ALBEDO_PATH + "AdvancedCraftingTableTop.png";
            textureMapping.NormalPath = GameLoader.BLOCKS_NORMAL_PATH + "AdvancedCraftingTableTop.png";
            textureMapping.HeightPath = GameLoader.BLOCKS_HEIGHT_PATH + "AdvancedCraftingTableTop.png";

            ItemTypesServer.SetTextureMapping(GameLoader.NAMESPACE + "AdvancedCraftingTableTop", textureMapping);
        }

        [ModLoader.ModCallbackAttribute(ModLoader.EModCallbackType.AfterAddingBaseTypes,
            GameLoader.NAMESPACE + ".AdvancedCrafterRegister.AfterAddingBaseTypes")]
        public static void AfterAddingBaseTypes(Dictionary<string, ItemTypesServer.ItemTypeRaw> itemTypes)
        {
            var item = new JSONNode()
                      .SetAs("icon", GameLoader.ICON_PATH + "AdvancedCraftingTable.png")
                      .SetAs("onPlaceAudio", "woodPlace")
                      .SetAs("onRemoveAudio", "woodDeleteLight")
                      .SetAs("sideall", "planks")
                      .SetAs("sidey+", GameLoader.NAMESPACE + "AdvancedCraftingTableTop")
                      .SetAs("npcLimit", 0);

            var categories = new JSONNode(NodeType.Array);
            categories.AddToArray(new JSONNode("job"));
            item.SetAs("categories", categories);

            itemTypes.Add(JOB_ITEM_KEY, new ItemTypesServer.ItemTypeRaw(JOB_ITEM_KEY, item));
        }

        [ModLoader.ModCallbackAttribute(ModLoader.EModCallbackType.AfterWorldLoad,
            GameLoader.NAMESPACE + ".AdvancedCrafterRegister.AfterWorldLoad")]
        public static void AfterWorldLoad()
        {
            var iron   = new InventoryItem(BuiltinBlocks.BronzeIngot, 2);
            var tools  = new InventoryItem(BuiltinBlocks.CopperTools, 1);
            var planks = new InventoryItem(BuiltinBlocks.Planks, 4);

            var recipe = new Recipe(JOB_RECIPE,
                                    new List<InventoryItem> {iron, tools, planks},
                                    new InventoryItem(JOB_ITEM_KEY, 1), 2);

            //ItemTypesServer.LoadSortOrder(JOB_ITEM_KEY, GameLoader.GetNextItemSortIndex());
            RecipePlayer.AddOptionalRecipe(recipe);
            RecipeStorage.AddOptionalLimitTypeRecipe(ItemFactory.JOB_CRAFTER, recipe);
        }
    }

    public class AdvancedCrafterJob : CraftingJobBase, IBlockJobBase, INPCTypeDefiner
    {
        private static readonly NPCTypeStandardSettings _type = new NPCTypeStandardSettings
        {
            keyName    = AdvancedCrafterRegister.JOB_NAME,
            printName  = "Advanced Crafter",
            maskColor1 = new Color32(101, 121, 123, 255),
            type       = NPCTypeID.GetNextID()
        };

        public static float StaticCraftingCooldown = 5f;

        public override string NPCTypeKey => AdvancedCrafterRegister.JOB_NAME;

        public override int MaxRecipeCraftsPerHaul => 1;

        public override float CraftingCooldown
        {
            get => StaticCraftingCooldown;
            set => StaticCraftingCooldown = value;
        }

        NPCTypeStandardSettings INPCTypeDefiner.GetNPCTypeDefinition()
        {
            return _type;
        }

        protected override void OnRecipeCrafted()
        {
            base.OnRecipeCrafted();
            ServerManager.SendAudio(position.Vector, ".crafting");
        }
    }
}