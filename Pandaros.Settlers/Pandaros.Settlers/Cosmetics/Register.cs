﻿using System.Collections.Generic;
using Pipliz;
using Pipliz.JSON;

namespace Pandaros.Settlers.Cosmetics
{
    public static class Register
    {
        public const string DYER_JOB = "pipliz.dyer";

        public static void AddCarpetTextures(string key)
        {
            var textureMapping = new ItemTypesServer.TextureMapping(new JSONNode());

            textureMapping.AlbedoPath   = GameLoader.BLOCKS_ALBEDO_PATH + key + ".png";
            textureMapping.NormalPath   = "gamedata/textures/materials/blocks/normal/carpet.png";
            textureMapping.EmissivePath = "gamedata/textures/materials/blocks/emissiveMaskAlpha/neutral.png";
            textureMapping.HeightPath   = "gamedata/textures/materials/blocks/heightSmoothnessSpecularity/carpet.png";

            ItemTypesServer.SetTextureMapping(GameLoader.NAMESPACE + "." + key, textureMapping);
        }

        public static ItemTypesServer.ItemTypeRaw AddCarpetTypeTypes(Dictionary<string, ItemTypesServer.ItemTypeRaw> itemTypes, string key)
        {
            var carpet = new JSONNode()
                        .SetAs("icon", GameLoader.ICON_PATH + key + ".png")
                        .SetAs("onPlaceAudio", "dirtPlace")
                        .SetAs("onRemoveAudio", "grassDelete")
                        .SetAs("sideall", GameLoader.NAMESPACE + "." + key)
                        .SetAs("maxStackSize", 200);

            var categories = new JSONNode(NodeType.Array);
            categories.AddToArray(new JSONNode("decorative"));
            carpet.SetAs("categories", categories);

            var item = new ItemTypesServer.ItemTypeRaw(GameLoader.NAMESPACE + "." + key, carpet);
            itemTypes.Add(GameLoader.NAMESPACE + "." + key, item);

            return item;
        }

        public static void AddGrassTextures(string key)
        {
            var textureMapping = new ItemTypesServer.TextureMapping(new JSONNode());

            textureMapping.AlbedoPath = GameLoader.BLOCKS_ALBEDO_PATH + key + ".png";
            textureMapping.NormalPath = "gamedata/textures/materials/blocks/normal/grassGeneric.png";
            textureMapping.EmissivePath = "gamedata/textures/materials/blocks/emissiveMaskAlpha/neutral.png";
            textureMapping.HeightPath = "gamedata/textures/materials/blocks/heightSmoothnessSpecularity/grassGeneric.png";

            ItemTypesServer.SetTextureMapping(GameLoader.NAMESPACE + "." + key, textureMapping);
        }

        public static ItemTypesServer.ItemTypeRaw AddGrassTypeTypes(Dictionary<string, ItemTypesServer.ItemTypeRaw> itemTypes, string key, bool fertile, List<TupleStruct<int, float, string>> lootTable)
        {
            var carpet = new JSONNode()
                        .SetAs("icon", GameLoader.ICON_PATH + key + ".png")
                        .SetAs("isFertile", fertile)
                        .SetAs("onPlaceAudio", "dirtPlace")
                        .SetAs("onRemoveAudio", "grassDelete")
                        .SetAs("sideall", GameLoader.NAMESPACE + "." + key)
                        .SetAs("maxStackSize", 200);

            var categories = new JSONNode(NodeType.Array);
            categories.AddToArray(new JSONNode("decorative"));
            categories.AddToArray(new JSONNode("grass"));
            carpet.SetAs("categories", categories);

            if (lootTable != null && lootTable.Count != 0)
            {
                var onRemove = new JSONNode(NodeType.Array);
            }

            var item = new ItemTypesServer.ItemTypeRaw(GameLoader.NAMESPACE + "." + key, carpet);
            itemTypes.Add(GameLoader.NAMESPACE + "." + key, item);

            return item;
        }
    }
}