﻿using System;
using System.Collections.Generic;
using System.Linq;
using ChatCommands;
using NPC;
using Pandaros.Settlers.Entities;
using Pandaros.Settlers.Items;
using Pandaros.Settlers.Jobs;
using Pandaros.Settlers.Managers;
using Pipliz;
using Pipliz.Collections;
using Pipliz.Mods.APIProvider.Jobs;
using Server.AI;
using Server.Monsters;
using Server.NPCs;
using Shared;
using UnityEngine;
using Physics = General.Physics.Physics;

namespace Pandaros.Settlers.AI
{
    [ModLoader.ModManagerAttribute]
    public class CalltoArmsJob : Job
    {
        private const int CALL_RAD = 500;

        private static readonly string COOLDOWN_KEY = GameLoader.NAMESPACE + ".CallToArmsCooldown";
        private static readonly Dictionary<InventoryItem, bool> _hadAmmo = new Dictionary<InventoryItem, bool>();

        private static readonly NPCTypeStandardSettings _callToArmsNPCSettings = new NPCTypeStandardSettings
        {
            type       = NPCTypeID.GetNextID(),
            keyName    = GameLoader.NAMESPACE + ".CalledToArms",
            printName  = "Called to Arms",
            maskColor0 = Color.red,
            maskColor1 = Color.magenta
        };

        public static NPCType CallToArmsNPCType;
        private Colony _colony;
        private PlayerState _playerState;
        private Stockpile _stock;
        private IMonster _target;
        private BoxedDictionary _tmpVals;
        private int _waitingFor;

        private GuardBaseJob.GuardSettings _weapon;

        public override bool ToSleep => false;

        public override NPCType NPCType => CallToArmsNPCType;

        public override bool NeedsItems => _weapon == null;

        public override Vector3Int KeyLocation => position;

        [ModLoader.ModCallbackAttribute(ModLoader.EModCallbackType.AfterItemTypesDefined,
            GameLoader.NAMESPACE + ".CalltoArms.Init")]
        [ModLoader.ModCallbackProvidesForAttribute("pipliz.apiprovider.jobs.resolvetypes")]
        [ModLoader.ModCallbackDependsOnAttribute("pipliz.blocknpcs.registerjobs")]
        public static void Init()
        {
            NPCType.AddSettings(_callToArmsNPCSettings);
            CallToArmsNPCType = NPCType.GetByKeyNameOrDefault(_callToArmsNPCSettings.keyName);
        }

        public override void OnAssignedNPC(NPCBase npc)
        {
            owner        = npc.Colony.Owner;
            _tmpVals     = npc.GetTempValues(true);
            _colony      = npc.Colony;
            _playerState = PlayerState.GetPlayerState(_colony.Owner);
            _stock       = Stockpile.GetStockPile(_colony.Owner);
            base.OnAssignedNPC(npc);
        }

        public override Vector3Int GetJobLocation()
        {
            var currentPos = usedNPC.Position;

            if (_playerState.CallToArmsEnabled && _weapon != null)
            {
                _target = MonsterTracker.Find(currentPos, _weapon.range, _weapon.shootDamage);

                if (_target != null)
                {
                    return currentPos;
                }

                _target = MonsterTracker.Find(currentPos, CALL_RAD, _weapon.shootDamage);

                if (_target != null)
                {
                    var ranged = _weapon.range - 5;

                    if (ranged < 0)
                        ranged = 1;

                    position = new Vector3Int(_target.Position).Add(ranged, 0, ranged);
                    position = AIManager.ClosestPosition(position, currentPos);

                    if (!AIManager.CanStandAt(position))
                    {
                        _tmpVals.Set(COOLDOWN_KEY, _weapon.cooldownMissingItem);
                        _waitingFor++;
                    }
                    else
                    {
                        return position;
                    }
                }
                else
                {
                    _tmpVals.Set(COOLDOWN_KEY, _weapon.cooldownMissingItem);
                    _waitingFor++;
                }
            }

            if (_waitingFor > 10)
            {
                var banner = BannerTracker.GetClosest(usedNPC.Colony.Owner, currentPos);

                if (banner != null)
                    return banner.KeyLocation;
            }

            return currentPos;
        }

        public static GuardBaseJob.GuardSettings GetWeapon(NPCBase npc)
        {
            GuardBaseJob.GuardSettings weapon = null;
            var                        inv    = SettlerInventory.GetSettlerInventory(npc);

            foreach (var w in ItemFactory.WeaponGuardSettings)
                if (npc.Inventory.Contains(w.recruitmentItem) || inv.Weapon.Id == w.recruitmentItem.Type)
                {
                    weapon = w;
                    break;
                }

            return weapon;
        }

        public override void OnNPCAtJob(ref NPCBase.NPCState state)
        {
            try
            {
                var currentposition = usedNPC.Position;
                _hadAmmo.Clear();

                if (_target == null || !_target.IsValid || !Physics.CanSee(usedNPC.Position.Vector, _target.Position))
                    _target = MonsterTracker.Find(currentposition, _weapon.range, _weapon.shootDamage);

                if (_target != null && Physics.CanSee(usedNPC.Position.Vector, _target.Position))
                {
                    foreach (var projectile in _weapon.shootItem)
                    {
                        _hadAmmo[projectile] = false;

                        if (usedNPC.Inventory.Contains(projectile))
                        {
                            _hadAmmo[projectile] = true;
                            continue;
                        }

                        if (_stock.Contains(projectile))
                            _hadAmmo[projectile] = true;
                    }

                    if (!_hadAmmo.Any(a => !a.Value))
                    {
                        state.SetIndicator(new IndicatorState(_weapon.cooldownShot, _weapon.shootItem[0].Type));

                        foreach (var ammo in _hadAmmo)
                        {
                            if (usedNPC.Inventory.Contains(ammo.Key))
                            {
                                usedNPC.Inventory.TryRemove(ammo.Key);
                                continue;
                            }

                            if (_stock.Contains(ammo.Key))
                                _stock.TryRemove(ammo.Key);
                        }

                        usedNPC.LookAt(_target.Position);

                        if (_weapon.OnShootAudio != null)
                            ServerManager.SendAudio(position.Vector, _weapon.OnShootAudio);

                        if (_weapon.OnHitAudio != null)
                            ServerManager.SendAudio(_target.PositionToAimFor, _weapon.OnHitAudio);

                        if (_weapon.shootItem.Count > 0)
                            foreach (var proj in _weapon.shootItem)
                            {
                                var projName = ItemTypes.IndexLookup.GetName(proj.Type);

                                if (AnimationManager.AnimatedObjects.ContainsKey(projName))
                                {
                                    AnimationManager
                                       .AnimatedObjects[projName]
                                       .SendMoveToInterpolatedOnce(position.Vector, _target.PositionToAimFor);

                                    break;
                                }
                            }

                        _target.OnHit(_weapon.shootDamage);
                        state.SetCooldown(_weapon.cooldownShot);
                        _waitingFor = 0;
                    }
                    else
                    {
                        state.SetIndicator(new IndicatorState(_weapon.cooldownMissingItem, _weapon.shootItem[0].Type,
                                                              true));

                        state.SetCooldown(_weapon.cooldownMissingItem);
                    }
                }
                else
                {
                    state.SetIndicator(new IndicatorState(_weapon.cooldownSearchingTarget,
                                                          GameLoader.MissingMonster_Icon, true));

                    state.SetCooldown(_weapon.cooldownMissingItem);
                    _target = null;
                }
            }
            catch (Exception)
            {
                state.SetIndicator(new IndicatorState(_weapon.cooldownSearchingTarget, GameLoader.MissingMonster_Icon,
                                                      true));

                state.SetCooldown(_weapon.cooldownMissingItem);
                _target = null;
            }
        }

        public override void OnNPCAtStockpile(ref NPCBase.NPCState state)
        {
            if (_weapon != null)
                return;

            if (_playerState.CallToArmsEnabled && ItemFactory.WeaponGuardSettings.Count != 0)
            {
                _weapon = GetWeapon(usedNPC);

                if (_weapon == null)
                    foreach (var w in ItemFactory.WeaponGuardSettings)
                        if (_stock.Contains(w.recruitmentItem))
                        {
                            _stock.TryRemove(w.recruitmentItem);
                            usedNPC.Inventory.Add(w.recruitmentItem);
                            _weapon = w;
                            break;
                        }
            }
        }

        public override NPCBase.NPCGoal CalculateGoal(ref NPCBase.NPCState state)
        {
            if (_weapon == null)
                return NPCBase.NPCGoal.Stockpile;

            return NPCBase.NPCGoal.Job;
        }

        public override void OnRemove()
        {
            isValid = false;

            if (usedNPC != null)
            {
                usedNPC.ClearJob();
                usedNPC = null;
            }
        }

        public override void OnRemovedNPC()
        {
            usedNPC = null;
        }

        public new void InitializeJob(Players.Player owner, Vector3Int position, int desiredNPCID)
        {
            this.position = position;
            this.owner    = owner;

            if (desiredNPCID != 0 && NPCTracker.TryGetNPC(desiredNPCID, out usedNPC))
                usedNPC.TakeJob(this);
            else
                desiredNPCID = 0;
        }
    }

    [ModLoader.ModManagerAttribute]
    public class CalltoArms : IChatCommand
    {
        private readonly List<CalltoArmsJob> _callToArmsJobs = new List<CalltoArmsJob>();
        private readonly Dictionary<NPCBase, IJob> _Jobs = new Dictionary<NPCBase, IJob>();

        public bool IsCommand(string chat)
        {
            return chat.StartsWith("/arms", StringComparison.OrdinalIgnoreCase) ||
                   chat.StartsWith("/cta", StringComparison.OrdinalIgnoreCase) ||
                   chat.StartsWith("/call", StringComparison.OrdinalIgnoreCase);
        }

        public bool TryDoCommand(Players.Player player, string chat)
        {
            if (player == null || player.ID == NetworkID.Server)
                return true;

            var array  = CommandManager.SplitCommand(chat);
            var colony = Colony.Get(player);
            var state  = PlayerState.GetPlayerState(player);
            state.CallToArmsEnabled = !state.CallToArmsEnabled;

            if (state.CallToArmsEnabled)
            {
                PandaChat.Send(player, "Call to arms activated!", ChatColor.red, ChatStyle.bold);

                foreach (var follower in colony.Followers)
                {
                    var job = follower.Job;

                    if (!CanCallToArms(job))
                        continue;

                    try
                    {
                        if (job != null)
                        {
                            if (job.GetType() != typeof(CalltoArmsJob))
                                _Jobs[follower] = job;

                            job.NPC = null;
                            follower.ClearJob();
                        }
                    }
                    catch (Exception ex)
                    {
                        PandaLogger.LogError(ex);
                    }

                    var armsJob = new CalltoArmsJob();
                    _callToArmsJobs.Add(armsJob);
                    armsJob.OnAssignedNPC(follower);
                    follower.TakeJob(armsJob);
                }
            }
            else
            {
                PandaChat.Send(player, "Call to arms deactivated.", ChatColor.green, ChatStyle.bold);
                var assignedWorkers = new List<NPCBase>();

                foreach (var follower in colony.Followers)
                {
                    var job = follower.Job;

                    if (job != null && job.GetType() == typeof(CalltoArmsJob))
                    {
                        follower.ClearJob();
                        job.NPC = null;
                        ((JobTracker.JobFinder) JobTracker.GetOrCreateJobFinder(player)).openJobs.Remove(job);
                    }

                    if (_Jobs.ContainsKey(follower) && _Jobs[follower].NeedsNPC)
                    {
                        assignedWorkers.Add(follower);
                        follower.TakeJob(_Jobs[follower]);
                        _Jobs[follower].NPC = follower;
                        JobTracker.Remove(player, _Jobs[follower].KeyLocation);
                    }
                }

                _Jobs.Clear();
            }

            foreach (var armsJob in _callToArmsJobs)
                ((JobTracker.JobFinder) JobTracker.GetOrCreateJobFinder(player)).openJobs.Remove(armsJob);

            _callToArmsJobs.Clear();
            JobTracker.Update();
            Colony.SendColonistCount(player);

            return true;
        }

        [ModLoader.ModCallbackAttribute(ModLoader.EModCallbackType.OnPlayerDisconnected,
            GameLoader.NAMESPACE + ".CallToArms.OnPlayerDisconnected")]
        public void OnPlayerDisconnected(Players.Player p)
        {
            var state = PlayerState.GetPlayerState(p);

            if (state.CallToArmsEnabled)
                TryDoCommand(p, "");
        }

        public static bool CanCallToArms(IJob job)
        {
            return !(job is GuardBaseJob) &&
                   !(job is Knight) &&
                   !(job is MachinistJob);
        }
    }
}