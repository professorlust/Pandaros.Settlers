﻿using System;
using NPC;
using Pandaros.Settlers.Entities;
using Pandaros.Settlers.Managers;
using Pipliz;

namespace Pandaros.Settlers.AI
{
    public static class SettlerEvaluation
    {
        private static readonly double _minFoodHours = TimeSpan.FromDays(3).TotalHours;

        public static float SpawnChance(Players.Player p, Colony c, PlayerState state)
        {
            var chance        = .3f;
            var remainingBeds = BedBlockTracker.GetCount(p) - c.FollowerCount;

            if (remainingBeds < 1)
                chance -= 0.1f;

            if (remainingBeds >= state.MaxPerSpawn)
                chance += 0.3f;
            else if (remainingBeds > SettlerManager.MIN_PERSPAWN)
                chance += 0.15f;

            var hoursofFood = Stockpile.GetStockPile(p).TotalFood / c.FoodUsePerHour;

            if (hoursofFood > _minFoodHours)
                chance += 0.2f;

            var jobCount = JobTracker.GetOpenJobCount(p);

            if (jobCount > state.MaxPerSpawn)
                chance += 0.4f;
            else if (jobCount > SettlerManager.MIN_PERSPAWN)
                chance += 0.1f;
            else
                chance -= 0.2f;

            if (state.Difficulty != GameDifficulty.Easy)
                if (c.InSiegeMode ||
                    c.LastSiegeModeSpawn != 0 &&
                    Time.SecondsSinceStartDouble - c.LastSiegeModeSpawn > TimeSpan.FromMinutes(5).TotalSeconds)
                    chance -= 0.4f;

            return chance;
        }
    }
}