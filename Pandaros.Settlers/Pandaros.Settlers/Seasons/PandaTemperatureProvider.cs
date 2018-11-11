﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerrainGeneration;

namespace Pandaros.Settlers.Seasons
{
    public class PandaTemperatureProvider : TerrainGenerator.ITemperatureProvider
    {
        public TerrainGenerator.ITemperatureProvider InnerGenerator { get; set; }
        private TerrainGenerator.ITemperatureProvider _defaultProvider;

        public PandaTemperatureProvider(TerrainGenerator.ITemperatureProvider defaultProvider)
        {
            _defaultProvider = defaultProvider;
            InnerGenerator = this;
        }

        public float GetTemperature(float height, float worldX, float worldZ, ref TerrainGenerator.MetaBiomePreciseStruct metaBiomeData)
        {
            double temp = _defaultProvider.GetTemperature(height, worldX, worldZ, ref metaBiomeData);

            if (TimeCycle.IsDay)
            {
                temp += SeasonsFactory.CurrentSeason.DayTemperatureDifferance;
            }
            else
            {
                temp += SeasonsFactory.CurrentSeason.NightTemperatureDifferance;
            }

            return (float)Math.Round(temp, 2);
        }
    }
}