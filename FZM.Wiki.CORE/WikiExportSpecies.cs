using Eco.Gameplay.Players;
using Eco.Gameplay.Systems.Chat;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Eco.Simulation.Types;
using Eco.Simulation;
using static Eco.Simulation.Types.PlantSpecies;
using Eco.World;
using Eco.Gameplay.Plants;
using Eco.World.Blocks;
using Eco.Shared.Utils;
using System.Linq;
using Eco.Gameplay.Items;
using Eco.Shared.Localization;

/*
 * This script is an extension by FZM based on the work done by Pradoxzon.
 * 
 * Most code was re-written to make use of changed or new additions to the Eco source code
 * and to change the reliance on Pradoxzon Core Utilities mod.
 *  
 */

namespace FZM.Wiki
{
    public partial class WikiDetails : IChatCommandHandler
    {
        // dictionary of plants and their dictionary of stats
        private static SortedDictionary<string, Dictionary<string, string>> EveryPlant = new SortedDictionary<string, Dictionary<string, string>>();

        // dictionary of trees and their dictionary of stats
        private static SortedDictionary<string, Dictionary<string, string>> EveryTree = new SortedDictionary<string, Dictionary<string, string>>();

        // dictionary of animals and their dictionary of stats
        private static SortedDictionary<string, Dictionary<string, string>> EveryAnimal = new SortedDictionary<string, Dictionary<string, string>>();

        [ChatCommand("Creates a dump file of all plant conditions", ChatAuthorizationLevel.Admin)]
        public static void PlantDetails(User user)
        {
            // dictionary of plant properties
            Dictionary<string, string> plantDetails = new Dictionary<string, string>()
            {
                // INFO
                { Localizer.DoStr("isDecorative"), "nil" }, // Is the plant considered decorative. Not simulated after spawn.
                { Localizer.DoStr("doesSpread"), "nil" }, // The plant will spawn others like it nearby given enough time not dying and not harvested

                // LIFETIME
                { Localizer.DoStr("maturity"), "nil" }, // Age for full maturity and reproduction.

                // GENERATION
                { Localizer.DoStr("isWater"), "nil" }, // Does the species live underwater.
                { Localizer.DoStr("height"), "nil" }, // Plant height in meters.

                // FOOD
                { Localizer.DoStr("calorieValue"), "nil" }, // The base calories this species provides to it's consumers.

                // RESOURCES
                { Localizer.DoStr("requireHarvestable"), "nil" }, // Does this plant require to have reached a harvestable stage before you can harvest it, you will get no resources for this if its not at a harvestable stage. 
                { Localizer.DoStr("pickableAtPercent"), "nil" }, // This plant will be pickable at this percent and you will get some resources.
                { Localizer.DoStr("experiencePerHarvest"), "nil" }, // Base experience you get per harvest.
                { Localizer.DoStr("harvestTool"), "nil" }, // The tool required to harvest this plant, nil means hands.
                { Localizer.DoStr("killOnHarvest"), "nil" }, // Does the plant die on harvest.
                { Localizer.DoStr("postHarvestGrowth"), "nil" }, // What % growth does the plant return to after harvest.
                { Localizer.DoStr("scytheKills"), "nil" }, // Will using a Scythe/Sickle on this plant kill it.
                { Localizer.DoStr("resourceItem"), "nil" }, // The item you get from harvesting this plant.
                { Localizer.DoStr("resourceMin"), "nil" }, // The minimum number of items returned.
                { Localizer.DoStr("resourceMax"), "nil" }, // The maximum number of items returned.
                { Localizer.DoStr("resourceBonus"), "nil" }, // The bonus items returned for allowing it to grow.

                // WORLD LAYERS
                { Localizer.DoStr("carbonRelease"), "nil" }, // The amount of carbon dioxide released by this species. (Plants & Trees are negative values)
                { Localizer.DoStr("idealGrowthRate"), "nil" }, // In ideal conditions, what is the rate of growth. (%)
                { Localizer.DoStr("idealDeathRate"), "nil" }, // In ideal conditions what is the rate of death. (%)
                { Localizer.DoStr("spreadRate"), "nil" }, // In ideal conditions what is the rate of spread, if it does spread.
                { Localizer.DoStr("nitrogenHalfSpeed"), "nil" }, // At what nitrogen value will the growth speed reduce to half.
                { Localizer.DoStr("nitrogenContent"), "nil" }, // What nitrogen content is ideal.
                { Localizer.DoStr("phosphorusHalfSpeed"), "nil" }, // At what phosphorus value will the growth speed reduce to half.
                { Localizer.DoStr("phosphorusContent"), "nil" }, // What phosphorus content is ideal.
                { Localizer.DoStr("potassiumHalfSpeed"), "nil" }, // At what potassium value will the growth speed reduce to half.
                { Localizer.DoStr("potassiumContent)"), "nil" }, // What potassium content is ideal.
                { Localizer.DoStr("soilMoistureHalfSpeed"), "nil" }, // At what moisture value will the growth speed reduce to half.
                { Localizer.DoStr("soilMoistureContent"), "nil" }, // What moisture content is ideal.
                { Localizer.DoStr("consumedFertileGround"), "nil" }, // How much of the area deemed Fertile Ground does this plant take up, this is almost always more than the in game physical space.
                { Localizer.DoStr("consumedCanopySpace"), "nil" }, // How much of the area deemed Canopy Space does this plant take up, this is almost always more than the in game physical space.
                { Localizer.DoStr("consumedUnderwaterFertileGround"), "nil" }, // How much of the area deemed Underwater Fertile Ground does this plant take up, this is almost always more than the in game physical space.
                { Localizer.DoStr("consumedShrubSpace"), "nil" }, // How much of the area deemed Shrub Space does this plant take up, this is almost always more than the in game physical space.
                { Localizer.DoStr("extremeTempMin"), "nil" }, // The lowest temperature before this plant stops growth.
                { Localizer.DoStr("idealTempMin"), "nil" }, // The lowest temperature of the ideal growth range (max growth).
                { Localizer.DoStr("idealTempMax"), "nil" }, // The highest temperature of the ideal growth range (max growth).
                { Localizer.DoStr("extremeTempMax"), "nil" }, // The highest temperature before this plant stops growth.
                { Localizer.DoStr("extremeMoistureMin"), "nil" }, // The lowest moisture content before this plant stops growth.
                { Localizer.DoStr("idealMoistureMin"), "nil" }, // The lowest moisture content of the ideal growth range (max growth).
                { Localizer.DoStr("idealMoistureMax"), "nil" }, // The highest moisture content of the ideal growth range (max growth).
                { Localizer.DoStr("extremeMoistureMax"), "nil" },// The highest moisture content before this plant stops growth.
                { Localizer.DoStr("extremeSaltMin"), "nil" }, // The lowest salt content before this plant stops growth.
                { Localizer.DoStr("idealSaltMin"), "nil" }, // The lowest salt contente of the ideal growth range (max growth).
                { Localizer.DoStr("idealSaltMax"), "nil" }, // The highest salt content of the ideal growth range (max growth).
                { Localizer.DoStr("extremeSaltMax"), "nil" }, // The highest Sslt content before this plant stops growth.
                { Localizer.DoStr("maxPollutionDensity"), "nil" }, // The highest pollution density before this plant stops growing.
                { Localizer.DoStr("pollutionTolerance"), "nil" } // The pollution density at which this plant slows growth, spread and carbon dioxide absorbtion.
            };

            IEnumerable<Species> species = EcoSim.AllSpecies;

            foreach (Species s in species)
            {
                if (s is PlantSpecies && !(s is TreeSpecies))
                {
                    PlantSpecies plant = s as PlantSpecies;
                    if (!EveryPlant.ContainsKey(plant.DisplayName))
                    {
                        string plantName = plant.DisplayName;
                        EveryPlant.Add(plantName, new Dictionary<string, string>(plantDetails));

                        #region INFO
                        EveryPlant[plantName][Localizer.DoStr("isDecorative")] = plant.Decorative ? $"'{Localizer.DoStr("Decorative")}'" : "nil"; 
                        EveryPlant[plantName][Localizer.DoStr("doesSpread")] = plant.NoSpread ? $"'{Localizer.DoStr("No Spread")}'" : $"'{Localizer.DoStr("Spread")}'"; 
                        #endregion

                        #region LIFETIME

                        EveryPlant[plantName][Localizer.DoStr("maturity")] = "'" + plant.MaturityAgeDays.ToString("F1") + "'"; 
                        #endregion

                        #region GENERATION
                        EveryPlant[plantName][Localizer.DoStr("isWater")] = plant.Water ? $"'{Localizer.DoStr("Underwater")}'" : "nil"; 
                        EveryPlant[plantName][Localizer.DoStr("height")] = "'" + plant.Height.ToString("F1") + "'"; 
                        #endregion

                        #region FOOD
                        EveryPlant[plantName][Localizer.DoStr("calorieValue")] = "'" + plant.CalorieValue.ToString("F1") + "'"; 
                        #endregion

                        #region RESOURCES                       
                        EveryPlant[plantName][Localizer.DoStr("requireHarvestable")] = plant.RequireHarvestable ? $"'{Localizer.DoStr("Yes")}'" : "nil";

                        EveryPlant[plantName][Localizer.DoStr("pickableAtPercent")] = "'" + (plant.PickableAtPercent * 100).ToString("F0") + "'";

                        EveryPlant[plantName][Localizer.DoStr("experiencePerHarvest")] = "'" + (plant.ExperiencePerHarvest).ToString("F1") + "'";

                        if (Block.Is<Reapable>(plant.BlockType))
                            EveryPlant[plantName][Localizer.DoStr("harvestTool")] = $"'{Localizer.DoStr("Scythe")}'";
                        else if (Block.Is<Diggable>(plant.BlockType))
                            EveryPlant[plantName][Localizer.DoStr("harvestTool")] = $"'{Localizer.DoStr("Shovel")}'";

                        if (plant.PostHarvestingGrowth == 0)
                            EveryPlant[plantName][Localizer.DoStr("killOnHarvest")] = $"'{Localizer.DoStr("Yes")}'";
                        else
                            EveryPlant[plantName][Localizer.DoStr("killOnHarvest")] = $"'{Localizer.DoStr("Yes")}'";

                        if (plant.PostHarvestingGrowth != 0)
                            EveryPlant[plantName][Localizer.DoStr("postHarvestGrowth")] = "'" + (plant.PostHarvestingGrowth * 100).ToString("F0") + "'";

                        EveryPlant[plantName][Localizer.DoStr("scytheKills")] = plant.ScythingKills ? $"'{Localizer.DoStr("Yes")}'" : "nil";

                        if (plant.ResourceItemType != null) { EveryPlant[plantName][Localizer.DoStr("resourceItem")] = "'[[" + Localizer.DoStr(SplitName(RemoveItemTag(plant.ResourceItemType.Name))) + "]]'"; }

                        EveryPlant[plantName][Localizer.DoStr("resourceMin")] = "'" + plant.ResourceRange.Min.ToString("F1") + "'"; 
                        EveryPlant[plantName][Localizer.DoStr("resourceMax")] = "'" + plant.ResourceRange.Max.ToString("F1") + "'"; 
                        EveryPlant[plantName][Localizer.DoStr("resourceBonus")] = "'" + (plant.ResourceBonusAtGrowth * 100).ToString("F0") + "'"; 

                        #endregion

                        #region VISUALS

                        #endregion

                        #region WORLDLAYERS
                        EveryPlant[plantName][Localizer.DoStr("carbonRelease")] = "'" + plant.ReleasesCO2TonsPerDay.ToString("F4") + "'"; 

                        EveryPlant[plantName][Localizer.DoStr("idealGrowthRate")] = "'" + plant.MaxGrowthRate.ToString("F4") + "'";

                        EveryPlant[plantName][Localizer.DoStr("idealDeathRate")] = "'" + plant.MaxDeathRate.ToString("F4") + "'";

                        EveryPlant[plantName][Localizer.DoStr("spreadRate")] = "'" + plant.SpreadRate.ToString("F4") + "'";

                        #region Resource Constraints
                        if (plant.ResourceConstraints != null)
                        {
                            foreach (ResourceConstraint r in plant.ResourceConstraints)
                            {
                                if (r.LayerName == "Nitrogen")
                                {
                                    EveryPlant[plantName][Localizer.DoStr("nitrogenHalfSpeed")] = "'" + (r.HalfSpeedConcentration * 100).ToString("F0") + "'";
                                    EveryPlant[plantName][Localizer.DoStr("nitrogenContent")] = "'" + (r.MaxResourceContent * 100).ToString("F0") + "'";
                                }
                                if (r.LayerName == "Phosphorus")
                                {
                                    EveryPlant[plantName][Localizer.DoStr("phosphorusHalfSpeed")] = "'" + (r.HalfSpeedConcentration * 100).ToString("F0") + "'";
                                    EveryPlant[plantName][Localizer.DoStr("phosphorusContent")] = "'" + (r.MaxResourceContent * 100).ToString("F0") + "'";
                                }
                                if (r.LayerName == "Potassium")
                                {
                                    EveryPlant[plantName][Localizer.DoStr("potassiumHalfSpeed")] = "'" + (r.HalfSpeedConcentration * 100).ToString("F0") + "'";
                                    EveryPlant[plantName][Localizer.DoStr("potassiumContent")] = "'" + (r.MaxResourceContent * 100).ToString("F0") + "'";
                                }
                                if (r.LayerName == "SoilMoisture")
                                {
                                    EveryPlant[plantName][Localizer.DoStr("soilMoistureHalfSpeed")] = "'" + (r.HalfSpeedConcentration * 100).ToString("F0") + "'";
                                    EveryPlant[plantName][Localizer.DoStr("soilMoistureContent")] = "'" + (r.MaxResourceContent * 100).ToString("F0") + "'";
                                }
                            }
                        }
                        #endregion

                        #region Capacity Constraints
                        if (plant.CapacityConstraints != null)
                        {
                            foreach (CapacityConstraint c in plant.CapacityConstraints)
                            {
                                if (c.CapacityLayerName == "FertileGorund")
                                    EveryPlant[plantName][Localizer.DoStr("consumedFertileGround")] = "'" + (c.ConsumedCapacityPerPop).ToString("F1") + "'";
                                if (c.CapacityLayerName == "CanopySpace")
                                    EveryPlant[plantName][Localizer.DoStr("consumedCanopySpace")] = "'" + (c.ConsumedCapacityPerPop).ToString("F1") + "'";
                                if (c.CapacityLayerName == "UnderwaterFertileGorund")
                                    EveryPlant[plantName][Localizer.DoStr("consumedUnderwaterFertileGround")] = "'" + (c.ConsumedCapacityPerPop).ToString("F1") + "'";
                                if (c.CapacityLayerName == "ShrubSpace")
                                    EveryPlant[plantName][Localizer.DoStr("consumedShrubSpace")] = "'" + (c.ConsumedCapacityPerPop).ToString("F1") + "'";
                            }
                        }
                        #endregion

                        #region Environment Ranges

                        // Temperature
                        EveryPlant[plantName][Localizer.DoStr("extremeTempMin")] = "'" + plant.TemperatureExtremes.Min.ToString("F1") + "'";
                        EveryPlant[plantName][Localizer.DoStr("idealTempMin")] = "'" + plant.IdealTemperatureRange.Min.ToString("F1") + "'";
                        EveryPlant[plantName][Localizer.DoStr("idealTempMax")] = "'" + plant.IdealTemperatureRange.Max.ToString("F1") + "'";
                        EveryPlant[plantName][Localizer.DoStr("extremeTempMax")] = "'" + plant.TemperatureExtremes.Max.ToString("F1") + "'";

                        // Moisture
                        EveryPlant[plantName][Localizer.DoStr("extremeMoistureMin")] = "'" + plant.MoistureExtremes.Min.ToString("F1") + "'";
                        EveryPlant[plantName][Localizer.DoStr("idealMoistureMin")] = "'" + plant.IdealMoistureRange.Min.ToString("F1") + "'";
                        EveryPlant[plantName][Localizer.DoStr("idealMoistureMax")] = "'" + plant.IdealMoistureRange.Max.ToString("F1") + "'";
                        EveryPlant[plantName][Localizer.DoStr("extremeMoistureMax")] = "'" + plant.MoistureExtremes.Max.ToString("F1") + "'";

                        // Salt Content
                        EveryPlant[plantName][Localizer.DoStr("extremeSaltMin")] = "'" + plant.WaterExtremes.Min.ToString("F1") + "'";
                        EveryPlant[plantName][Localizer.DoStr("idealSaltMin")] = "'" + plant.IdealWaterRange.Min.ToString("F1") + "'";
                        EveryPlant[plantName][Localizer.DoStr("idealSaltMax")] = "'" + plant.IdealWaterRange.Max.ToString("F1") + "'";
                        EveryPlant[plantName][Localizer.DoStr("extremeSaltMax")] = "'" + plant.WaterExtremes.Max.ToString("F1") + "'";

                        #endregion

                        EveryPlant[plantName][Localizer.DoStr("maxPollutionDensity")] = "'" + plant.MaxPollutionDensity.ToString("F4") + "'";
                        EveryPlant[plantName][Localizer.DoStr("pollutionTolerance")] = "'" + plant.PollutionDensityTolerance.ToString("F4") + "'";

                        #endregion

                        #region UNCATEGORISED
                        #endregion

                        #region OBSOLETE

                        /*
                         
                         * SEEDS
                         
                        if (plant.SeedItemType != null) { EveryPlant[plantName]["seedDrop"] = "'" + SplitName(RemoveItemTag(plant.SeedItemType.Name)) + "'"; }   
                        EveryPlant[plantName]["seedDropChance"] = "'" + (plant.SeedDropChance * 100).ToString("F0") + "'";
                        EveryPlant[plantName]["seedAtGrowth"] = "'" + (plant.SeedsAtGrowth * 100).ToString("F0") + "'";
                        EveryPlant[plantName]["seedBonusGrowth"] = "'" + (plant.SeedsBonusAtGrowth * 100).ToString("F0") + "'";
                        EveryPlant[plantName]["seedMax"] = "'" + plant.SeedRange.Max.ToString("F1") + "'";
                        EveryPlant[plantName]["seedMin"] = "'" + plant.SeedRange.Min.ToString("F1") + "'";


                        */

                        #endregion
                    }
                }
            }
            WriteDictionaryToFile(user, "Wiki_Module_PlantData.txt", "plants", EveryPlant);

        }

        [ChatCommand("Creates a dump file of all Tree conditions", ChatAuthorizationLevel.Admin)]
        public static void TreeDetails(User user)
        {
            // dictionary of plant properties
            Dictionary<string, string> treeDetails = new Dictionary<string, string>()
            {
                // INFO
                { Localizer.DoStr("isDecorative"), "nil" },
                { Localizer.DoStr("doesSpread"), "nil" },

                // LIFETIME
                { Localizer.DoStr("maturity"), "nil" },
                { Localizer.DoStr("treeHealth"), "nil" }, // The health of the tree for chopping.
                { Localizer.DoStr("logHealth"), "nil" }, // The health of the log for chopping.

                // GENERATION
                { Localizer.DoStr("isWater"), "nil" },
                { Localizer.DoStr("height"), "nil" },

                // FOOD
                { Localizer.DoStr("calorieValue"), "nil" },

                // RESOURCES
                { Localizer.DoStr("requireHarvestable"), "nil" },
                { Localizer.DoStr("pickableAtPercent"), "nil" },
                { Localizer.DoStr("experiencePerHarvest"), "nil" },
                { Localizer.DoStr("harvestTool"), "nil" },
                { Localizer.DoStr("killOnHarvest"), "nil" },
                { Localizer.DoStr("postHarvestGrowth"), "nil" },
                { Localizer.DoStr("scytheKills"), "nil" },
                { Localizer.DoStr("resourceItem"), "nil" },
                { Localizer.DoStr("resourceMin"), "nil" },
                { Localizer.DoStr("resourceMax"), "nil" },
                { Localizer.DoStr("debrisSpawnChance"), "nil" }, // Chance to spawn debris.
                { Localizer.DoStr("debrisType"), "nil" }, // The debris created when chopping this tree. BlockType.
                { Localizer.DoStr("debrisResources"), "nil" }, // The resources returned for chopping the debris.
                { Localizer.DoStr("trunkResources"), "nil" }, // The resources returned for chopping the trunk.

                // WORLD LAYERS
                { Localizer.DoStr("carbonRelease"), "nil" },
                { Localizer.DoStr("idealGrowthRate"), "nil" },
                { Localizer.DoStr("idealDeathRate"), "nil" },
                { Localizer.DoStr("spreadRate"), "nil" },
                { Localizer.DoStr("nitrogenHalfSpeed"), "nil" },
                { Localizer.DoStr("nitrogenContent"), "nil" },
                { Localizer.DoStr("phosphorusHalfSpeed"), "nil" },
                { Localizer.DoStr("phosphorusContent"), "nil" },
                { Localizer.DoStr("potassiumHalfSpeed"), "nil" },
                { Localizer.DoStr("potassiumContent"), "nil" },
                { Localizer.DoStr("soilMoistureHalfSpeed"), "nil" },
                { Localizer.DoStr("soilMoistureContent"), "nil" },
                { Localizer.DoStr("consumedFertileGround"), "nil" },
                { Localizer.DoStr("consumedCanopySpace"), "nil" },
                { Localizer.DoStr("consumedUnderwaterFertileGround"), "nil" },
                { Localizer.DoStr("consumedShrubSpace"), "nil" },
                { Localizer.DoStr("extremeTempMin"), "nil" },
                { Localizer.DoStr("idealTempMin"), "nil" },
                { Localizer.DoStr("idealTempMax"), "nil" },
                { Localizer.DoStr("extremeTempMax"), "nil" },
                { Localizer.DoStr("extremeMoistureMin"), "nil" },
                { Localizer.DoStr("idealMoistureMin"), "nil" },
                { Localizer.DoStr("idealMoistureMax"), "nil" },
                { Localizer.DoStr("extremeMoistureMax"), "nil" },
                { Localizer.DoStr("extremeSaltMin"), "nil" },
                { Localizer.DoStr("idealSaltMin"), "nil" },
                { Localizer.DoStr("idealSaltMax"), "nil" },
                { Localizer.DoStr("extremeSaltMax"), "nil" },
                { Localizer.DoStr("maxPollutionDensity"), "nil" },
                { Localizer.DoStr("pollutionTolerance"), "nil" }
            };

            IEnumerable<Species> species = EcoSim.AllSpecies;
            foreach (Species s in species)
            {
                if (s is TreeSpecies)
                {
                    TreeSpecies tree = s as TreeSpecies;
                    //Console.WriteLine(tree.Name);
                    if (!EveryTree.ContainsKey(tree.DisplayName))
                    {
                        string treeName = tree.DisplayName;
                        EveryTree.Add(treeName, new Dictionary<string, string>(treeDetails));

                        #region INFO
                        EveryTree[treeName][Localizer.DoStr("isDecorative")] = tree.Decorative ? $"'{Localizer.DoStr("Decorative")}'" : "nil"; 
                        EveryTree[treeName][Localizer.DoStr("doesSpread")] = tree.NoSpread ? $"'{Localizer.DoStr("No Spread")}'" : $"'{Localizer.DoStr("Spread")}'"; 
                        #endregion

                        #region LIFETIME
                        EveryTree[treeName][Localizer.DoStr("maturity")] = "'" + tree.MaturityAgeDays.ToString("F1") + "'"; 
                        EveryTree[treeName][Localizer.DoStr("treeHealth")] = "'" + tree.TreeHealth.ToString("F1") + "'"; 
                        EveryTree[treeName][Localizer.DoStr("logHealth")] = "'" + tree.LogHealth.ToString("F1") + "'"; 
                        #endregion

                        #region GENERATION
                        EveryTree[treeName][Localizer.DoStr("isWater")] = tree.Water ? $"'{Localizer.DoStr("Underwater")}'" : "nil"; 
                        EveryTree[treeName][Localizer.DoStr("height")] = "'" + tree.Height.ToString("F1") + "'"; 
                        #endregion

                        #region FOOD
                        EveryTree[treeName][Localizer.DoStr("calorieValue")] = "'" + tree.CalorieValue.ToString("F1") + "'"; 
                        #endregion

                        #region RESOURCES
                        EveryTree[treeName][Localizer.DoStr("requireHarvestable")] = tree.RequireHarvestable ? $"'{Localizer.DoStr("Yes")}'" : "nil";
                        EveryTree[treeName][Localizer.DoStr("pickableAtPercent")] = "'" + (tree.PickableAtPercent * 100).ToString("F0") + "'";
                        EveryTree[treeName][Localizer.DoStr("experiencePerHarvest")] = "'" + (tree.ExperiencePerHarvest).ToString("F1") + "'";

                        if (tree.PostHarvestingGrowth == 0)
                            EveryTree[treeName][Localizer.DoStr("killOnHarvest")] = $"'{Localizer.DoStr("Yes")}'";
                        else
                            EveryTree[treeName][Localizer.DoStr("killOnHarvest")] = $"'{Localizer.DoStr("No")}'";

                        if (tree.PostHarvestingGrowth != 0)
                            EveryTree[treeName][Localizer.DoStr("postHarvestGrowth")] = "'" + (tree.PostHarvestingGrowth * 100).ToString("F0") + "'";

                        EveryTree[treeName][Localizer.DoStr("scytheKills")] = tree.ScythingKills ? $"'{Localizer.DoStr("Yes")}'" : "nil";

                        if (tree.ResourceItemType != null) { EveryTree[treeName][Localizer.DoStr("resourceItem")] = "'[[" + Localizer.DoStr(SplitName(RemoveItemTag(tree.ResourceItemType.Name))) + "]]'"; }

                        EveryTree[treeName][Localizer.DoStr("resourceMin")] = "'" + tree.ResourceRange.Min.ToString("F1") + "'"; 
                        EveryTree[treeName][Localizer.DoStr("resourceMax")] = "'" + tree.ResourceRange.Max.ToString("F1") + "'"; 
                        EveryTree[treeName][Localizer.DoStr("resourceBonus")] = "'" + (tree.ResourceBonusAtGrowth * 100).ToString("F0") + "'"; 

                        // Debris
                        EveryTree[treeName][Localizer.DoStr("debrisSpawnChance")] = "'" + (tree.ChanceToSpawnDebris * 100).ToString("F0") + "'"; 
                        EveryTree[treeName][Localizer.DoStr("debrisType")] = "'" + Localizer.DoStr(SplitName(RemoveItemTag(tree.DebrisType.Name))) + "'"; 

                        // The resources returned for chopping the debris.
                        var debrisResources = new StringBuilder();
                        tree.DebrisResources.ForEach(kvp =>
                        {
                            debrisResources.Append("'[[" + Item.Get(kvp.Key).DisplayName + "]]'");
                            if (tree.DebrisResources.Last().Key != kvp.Key)
                            {
                                debrisResources.Append(",");
                            }
                        });
                        EveryTree[treeName][Localizer.DoStr("debrisResources")] = "{" + debrisResources + "}";

                        // The resources returned for chopping the trunk.
                        var trunkResources = new StringBuilder();
                        tree.TrunkResources.ForEach(kvp =>
                        {
                            var item = Item.Get(kvp.Key);
                            if (item != null)
                            {
                                debrisResources.Append("'[[" + item.DisplayName + "]]'");
                                if (tree.TrunkResources.Last().Key != kvp.Key)
                                {
                                    trunkResources.Append(",");
                                }
                            }
                        });
                        EveryTree[treeName][Localizer.DoStr("trunkResources")] = "{" + trunkResources + "}";

                        #endregion

                        #region VISUALS

                        #endregion

                        #region WORLDLAYERS
                        EveryTree[treeName][Localizer.DoStr("carbonRelease")] = "'" + tree.ReleasesCO2TonsPerDay.ToString("F4") + "'"; 

                        EveryTree[treeName][Localizer.DoStr("idealGrowthRate")] = "'" + tree.MaxGrowthRate.ToString("F4") + "'";

                        EveryTree[treeName][Localizer.DoStr("idealDeathRate")] = "'" + tree.MaxDeathRate.ToString("F4") + "'";

                        EveryTree[treeName][Localizer.DoStr("spreadRate")] = "'" + tree.SpreadRate.ToString("F4") + "'";

                        // The resource constraints that slow growth rate.
                        #region Resource Constraints
                        if (tree.ResourceConstraints != null)
                        {
                            foreach (ResourceConstraint r in tree.ResourceConstraints)
                            {
                                if (r.LayerName == "Nitrogen")
                                {
                                    EveryTree[treeName][Localizer.DoStr("nitrogenHalfSpeed")] = "'" + (r.HalfSpeedConcentration * 100).ToString("F0") + "'";
                                    EveryTree[treeName][Localizer.DoStr("nitrogenContent")] = "'" + (r.MaxResourceContent * 100).ToString("F0") + "'";
                                }
                                if (r.LayerName == "Phosphorus")
                                {
                                    EveryTree[treeName][Localizer.DoStr("phosphorusHalfSpeed")] = "'" + (r.HalfSpeedConcentration * 100).ToString("F0") + "'";
                                    EveryTree[treeName][Localizer.DoStr("phosphorusContent")] = "'" + (r.MaxResourceContent * 100).ToString("F0") + "'";
                                }
                                if (r.LayerName == "Potassium")
                                {
                                    EveryTree[treeName][Localizer.DoStr("potassiumHalfSpeed")] = "'" + (r.HalfSpeedConcentration * 100).ToString("F0") + "'";
                                    EveryTree[treeName][Localizer.DoStr("potassiumContent")] = "'" + (r.MaxResourceContent * 100).ToString("F0") + "'";
                                }
                                if (r.LayerName == "SoilMoisture")
                                {
                                    EveryTree[treeName][Localizer.DoStr("soilMoistureHalfSpeed")] = "'" + (r.HalfSpeedConcentration * 100).ToString("F0") + "'";
                                    EveryTree[treeName][Localizer.DoStr("soilMoistureContent")] = "'" + (r.MaxResourceContent * 100).ToString("F0") + "'";
                                }
                            }
                        }
                        #endregion

                        // The capacity constraints which slow growth.
                        #region Capacity Constraints
                        if (tree.CapacityConstraints != null)
                        {
                            foreach (CapacityConstraint c in tree.CapacityConstraints)
                            {
                                if (c.CapacityLayerName == "FertileGorund")
                                    EveryTree[treeName][Localizer.DoStr("consumedFertileGround")] = "'" + (c.ConsumedCapacityPerPop).ToString("F1") + "'";
                                if (c.CapacityLayerName == "CanopySpace")
                                    EveryTree[treeName][Localizer.DoStr("consumedCanopySpace")] = "'" + (c.ConsumedCapacityPerPop).ToString("F1") + "'";
                                if (c.CapacityLayerName == "UnderwaterFertileGorund")
                                    EveryTree[treeName][Localizer.DoStr("consumedUnderwaterFertileGround")] = "'" + (c.ConsumedCapacityPerPop).ToString("F1") + "'";
                                if (c.CapacityLayerName == "ShrubSpace")
                                    EveryTree[treeName][Localizer.DoStr("consumedShrubSpace")] = "'" + (c.ConsumedCapacityPerPop).ToString("F1") + "'";
                            }
                        }
                        #endregion

                        // The environmental ranges this plant can tolerate.
                        #region Environment Ranges

                        // Temperature
                        EveryTree[treeName][Localizer.DoStr("extremeTempMin")] = "'" + tree.TemperatureExtremes.Min.ToString("F1") + "'";
                        EveryTree[treeName][Localizer.DoStr("idealTempMin")] = "'" + tree.IdealTemperatureRange.Min.ToString("F1") + "'";
                        EveryTree[treeName][Localizer.DoStr("idealTempMax")] = "'" + tree.IdealTemperatureRange.Max.ToString("F1") + "'";
                        EveryTree[treeName][Localizer.DoStr("extremeTempMax")] = "'" + tree.TemperatureExtremes.Max.ToString("F1") + "'";

                        // Moisture
                        EveryTree[treeName][Localizer.DoStr("extremeMoistureMin")] = "'" + tree.MoistureExtremes.Min.ToString("F1") + "'";
                        EveryTree[treeName][Localizer.DoStr("idealMoistureMin")] = "'" + tree.IdealMoistureRange.Min.ToString("F1") + "'";
                        EveryTree[treeName][Localizer.DoStr("idealMoistureMax")] = "'" + tree.IdealMoistureRange.Max.ToString("F1") + "'";
                        EveryTree[treeName][Localizer.DoStr("extremeMoistureMax")] = "'" + tree.MoistureExtremes.Max.ToString("F1") + "'";

                        // Salt Content
                        EveryTree[treeName][Localizer.DoStr("extremeSaltMin")] = "'" + tree.WaterExtremes.Min.ToString("F1") + "'";
                        EveryTree[treeName][Localizer.DoStr("idealSaltMin")] = "'" + tree.IdealWaterRange.Min.ToString("F1") + "'";
                        EveryTree[treeName][Localizer.DoStr("idealSaltMax")] = "'" + tree.IdealWaterRange.Max.ToString("F1") + "'";
                        EveryTree[treeName][Localizer.DoStr("extremeSaltMax")] = "'" + tree.WaterExtremes.Max.ToString("F1") + "'";

                        #endregion

                        EveryTree[treeName][Localizer.DoStr("maxPollutionDensity")] = "'" + tree.MaxPollutionDensity.ToString("F4") + "'";
                        EveryTree[treeName][Localizer.DoStr("pollutionTolerance")] = "'" + tree.PollutionDensityTolerance.ToString("F4") + "'";

                        #endregion

                        #region UNCATEGORISED
                        #endregion

                        #region OBSOLETE
                        #endregion
                    }
                }
            }
            WriteDictionaryToFile(user, "Wiki_Module_TreeData.txt", "trees", EveryTree);
        }

        [ChatCommand("Creates a dump file of all Animal conditions", ChatAuthorizationLevel.Admin)]
        public static void AnimalDetails(User user)
        {
            // dictionary of animal properties
            Dictionary<string, string> animalDetails = new Dictionary<string, string>()
            {
                // LIFETIME
                { "maturity", "nil" }, // Age for full maturity and reproduction.

                // MOVEMENT
                { "isSwimming", "nil" }, // Is the animal a swimming one.
                { "isFlying", "nil" }, // Is the animal a flying one.
                { "climbHeight", "nil" }, // What height in meters can this animal effectively climb.

                // BEHAVIOUR
                { "wanderingSpeed", "nil" }, // The animals speed when idle.
                { "speed", "nil" }, // The animals speed when active (hunting, fleeing etc).
                { "health", "nil" }, // The animals health.
                { "damage", "nil" }, // The damage the animal inflicts.
                { "chanceToAttack", "nil" }, //The chance the animal will attack.
                { "attackRange", "nil" }, // The distance at which animal needs to be from its prey to attack.
                { "detectRange", "nil" }, // This distance at which the animal can detect prey.
                { "flees", "nil" }, // Does the animal flee from players by default (not being attacked).
                { "fearFactor", "nil" }, // How quickly the animmal reaches the point where it wants to flee.
                { "headDistance", "nil" }, // The space the animal require around its head (used to figure out pack behaviour for sleeping and wandering etc.)

                { "minAttackDelay", "nil" }, // Minimum possible time before the animal is ready to attack again after making an attack.
                { "maxAttackDelay", "nil" }, // Maximum possible time before the animal is ready to attack again after making an attack.

                // FOOD
                { "calorieValue", "nil" }, // The base calories this species provides to it's consumers.

                // FOOD SOURCES
                { "foodSources", "nil" }, // The species sources this animal eats.

                // RESOURCES
                { "resourceItem", "nil" }, // The item you get from harvesting this animal.
                { "resourceMin", "nil" }, // The minimum number of items returned.
                { "resourceMax", "nil" }, // The maximum number of items returned.
                { "resourceBonus", "nil" }, // The bonus items returned for allowing it to grow.

                // WORLD LAYERS
                { "carbonRelease", "nil" } // The amount of carbon dioxide released by this species. (Animals are postive values)
            };

            IEnumerable<Species> species = EcoSim.AllSpecies;
            foreach (Species s in species)
            {
                if (s is AnimalSpecies)
                {
                    AnimalSpecies animal = s as AnimalSpecies;
                    //Console.WriteLine(animal.Name);
                    if (!EveryAnimal.ContainsKey(animal.DisplayName))
                    {
                        string animalName = animal.DisplayName;
                        EveryAnimal.Add(animalName, new Dictionary<string, string>(animalDetails));

                        #region LIFETIME
                        EveryAnimal[animalName]["maturity"] = "'" + animal.MaturityAgeDays.ToString("F1") + "'";
                        #endregion

                        #region MOVEMENT
                        EveryAnimal[animalName]["isSwimming"] = animal.Swimming ? "'Swimming'" : "nil"; // Does the animal swin.
                        EveryAnimal[animalName]["isFlying"] = animal.Flying ? "'Flying'" : "nil"; // Does the animal fly.
                        EveryAnimal[animalName]["climbHeight"] = "'" + animal.ClimbHeight.ToString("F1") + "'"; // The height the animal can climb up
                        #endregion

                        #region BEHAVIOUR
                        EveryAnimal[animalName]["wanderingSpeed"] = "'" + animal.WanderingSpeed.ToString("F1") + "'"; // The general wandering speed of the animal.
                        EveryAnimal[animalName]["speed"] = "'" + animal.Speed.ToString("F1") + "'"; // The non-wandering speed of the animal.
                        EveryAnimal[animalName]["health"] = "'" + animal.Health.ToString("F1") + "'"; // The health of the animal.

                        EveryAnimal[animalName]["damage"] = "'" + animal.Damage.ToString("F1") + "'"; // The damage the animal inflicts.
                        EveryAnimal[animalName]["chanceToAttack"] = "'" + animal.ChanceToAttack.ToString("F1") + "'"; // The chance the animal will attack.
                        EveryAnimal[animalName]["attackRange"] = "'" + animal.AttackRange.ToString("F1") + "'"; // The range the animal attacks from.
                        EveryAnimal[animalName]["detectRange"] = "'" + animal.DetectRange.ToString("F1") + "'"; // The range the animal detects others from, default is 5X the attack range.

                        // Time between attacks
                        EveryAnimal[animalName]["minAttackDelay"] = "'" + animal.DelayBetweenAttacksRangeSec.Min.ToString("F1") + "'";
                        EveryAnimal[animalName]["maxAttackDelay"] = "'" + animal.DelayBetweenAttacksRangeSec.Max.ToString("F1") + "'";

                        EveryAnimal[animalName]["flees"] = animal.FleePlayers ? "'Flees'" : "nil"; // Will this animal flee players / predators.
                        EveryAnimal[animalName]["fearFactor"] = "'" + animal.FearFactor.ToString("F1") + "'"; // How quickly will the animal flee.
                        EveryAnimal[animalName]["headDistance"] = "'" + animal.HeadDistance.ToString("F1") + "'"; // The default distance from the animals head for calculating various behaviours
                        #endregion

                        #region FOOD
                        EveryAnimal[animalName]["calorieValue"] = "'" + animal.CalorieValue.ToString("F1") + "'"; // Calorie value to consumers.
                        #endregion

                        #region FOOD SOURCES
                        if (animal.FoodSources != null && animal.FoodSources.Count > 0)
                        {
                            var sb = new StringBuilder();
                            int sourceCount = 0;
                            sb.Append("'");
                            foreach (Type meal in animal.FoodSources)
                            {
                                int count = 0;
                                string[] foodNameSplit = Regex.Split(meal.Name, @"(?<!^)(?=[A-Z])");
                                sb.Append("[[");
                                foreach (string str in foodNameSplit)
                                {
                                    sb.Append(str);
                                    count++;
                                    if (count != foodNameSplit.Length)
                                        sb.Append(" ");
                                }

                                sb.Append("]]");
                                sourceCount++;
                                if (sourceCount != animal.FoodSources.Count)
                                    sb.Append(", ");

                            }
                            sb.Append("'");
                            EveryAnimal[animalName]["foodSources"] = sb.ToString();
                        }
                        #endregion

                        #region RESOURCES
                        // Resources returned.
                        EveryAnimal[animalName]["resourceMin"] = "'" + animal.ResourceRange.Min.ToString("F1") + "'";
                        EveryAnimal[animalName]["resourceMax"] = "'" + animal.ResourceRange.Max.ToString("F1") + "'";
                        EveryAnimal[animalName]["resourceBonus"] = "'" + (animal.ResourceBonusAtGrowth * 100).ToString("F0") + "'";

                        if (animal.ResourceItemType != null)
                        {
                            string item = animal.ResourceItemType.Name.Substring(0, animal.ResourceItemType.Name.Length - 4);
                            string[] itemNameSplit = Regex.Split(item, @"(?<!^)(?=[A-Z])");
                            int count = 0;
                            var sb = new StringBuilder();
                            sb.Append("[[");
                            foreach (string str in itemNameSplit)
                            {
                                sb.Append(str);
                                count++;
                                if (count != itemNameSplit.Length)
                                    sb.Append(" ");
                            }
                            sb.Append("]]");
                            EveryAnimal[animalName]["resourceItem"] = "'" + sb.ToString() + "'";
                        }
                        #endregion

                        #region WOLRD LAYERS
                        EveryAnimal[animalName]["carbonRelease"] = "'" + animal.ReleasesCO2TonsPerDay.ToString("F4") + "'";
                        #endregion
                    }
                }
            }
            WriteDictionaryToFile(user, "Wiki_Module_AnimalData.txt", "animals", EveryAnimal);
        }
    }
}