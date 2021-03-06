﻿using System.Collections.Generic;
using Pandaros.Settlers.Jobs;
using Pandaros.Settlers.Managers;
using Pipliz;
using Pipliz.Collections;
using Pipliz.JSON;
using Random = System.Random;

namespace Pandaros.Settlers.Items.Machines
{
    public class MachineState
    {
        public const float DEFAULT_MAX_DURABILITY = 1f;
        public const float DEFAULT_MAX_FUEL = 1f;
        public const float DEFAULT_MAX_LOAD = 1f;
        private static readonly Random _rand = new Random();

        public MachineState(Vector3Int pos, Players.Player owner, string machineType, IMachineSettings settings = null)
        {
            Position    = pos;
            MachineType = machineType;
            Owner       = owner;

            if (settings == null)
                MachineSettings = MachineManager.GetCallbacks(machineType);
            else
                MachineSettings = settings;
        }

        public MachineState(JSONNode baseNode, Players.Player owner)
        {
            MAX_DURABILITY[owner] = DEFAULT_MAX_DURABILITY;
            MAX_FUEL[owner]       = DEFAULT_MAX_FUEL;
            MAX_LOAD[owner]       = DEFAULT_MAX_LOAD;

            Position    = (Vector3Int) baseNode[nameof(Position)];
            Durability  = baseNode.GetAs<float>(nameof(Durability));
            Fuel        = baseNode.GetAs<float>(nameof(Fuel));
            MachineType = baseNode.GetAs<string>(nameof(MachineType));

            if (baseNode.TryGetAs<float>(nameof(Load), out var load))
                Load = load;

            MachineSettings = MachineManager.GetCallbacks(MachineType);
        }

        public static Dictionary<Players.Player, float> MAX_DURABILITY { get; set; } =
            new Dictionary<Players.Player, float>();

        public static Dictionary<Players.Player, float> MAX_FUEL { get; set; } =
            new Dictionary<Players.Player, float>();

        public static Dictionary<Players.Player, float> MAX_LOAD { get; set; } =
            new Dictionary<Players.Player, float>();

        public Vector3Int Position { get; } = Vector3Int.invalidPos;
        public float Durability { get; set; } = DEFAULT_MAX_DURABILITY;
        public float Fuel { get; set; } = DEFAULT_MAX_FUEL;
        public float Load { get; set; } = DEFAULT_MAX_LOAD;

        public string MachineType { get; }
        public Players.Player Owner { get; }
        public double NextTimeForWork { get; set; } = Time.SecondsSinceStartDouble + _rand.NextDouble(0, 5);
        public MachinistJob Machinist { get; set; }

        public IMachineSettings MachineSettings { get; }

        public BoxedDictionary TempValues { get; } = new BoxedDictionary();

        public bool PositionIsValid()
        {
            if (Position != null && World.TryGetTypeAt(Position, out var objType))
            {
#if Debug
                PandaLogger.Log(ChatColor.lime, $"PositionIsValid {ItemTypes.IndexLookup.GetName(objType)}. POS {Position}");
#endif
                return objType == MachineSettings.ItemIndex;
            }
#if Debug
                PandaLogger.Log(ChatColor.lime, $"PositionIsValid Trt Get Failed {Position == null}.");
#endif
            return Position != null;
        }

        public virtual JSONNode ToJsonNode()
        {
            var baseNode = new JSONNode();

            baseNode.SetAs(nameof(Position), (JSONNode) Position);
            baseNode.SetAs(nameof(Durability), Durability);
            baseNode.SetAs(nameof(Fuel), Fuel);
            baseNode.SetAs(nameof(Load), Load);
            baseNode.SetAs(nameof(MachineType), MachineType);

            return baseNode;
        }
    }
}