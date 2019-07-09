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
using Eco.Shared.Localization;
using Eco.Shared.Utils;
using Eco.Core.Utils;

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
                { "extremeTempMin", "nil" },
                { "idealTempMin", "nil" },
                { "idealTempMax", "nil" },
                { "extremeTempMax", "nil" },
                { "extremeMoistureMin", "nil" },
                { "idealMoistureMin", "nil" },
                { "idealMoistureMax", "nil" },
                { "extremeMoistureMax", "nil" },
                { "pickableAtPercent", "nil" },
                { "resourceItem", "nil" },
                { "resourceMin", "nil" },
                { "resourceMax", "nil" },
                { "maturity", "nil" },
                { "isWater", "nil" },
                { "isDecorative", "nil" },
                { "calorieValue", "nil" },
                /*
                { "seedDrop", "nil" },
                { "seedDropChance", "nil" },
                { "seedAtGrowth", "nil" },
                { "seedBonusGrowth", "nil" },
                { "seedMax", "nil" },
                { "seedMin", "nil" },
                */
                { "harvestTool", "nil" },
                { "killOnHarvest", "nil" },
                { "postHarvestGrowth", "nil" },
                { "scytheKills", "nil" },
                { "idealGrowthRate", "nil" },
                { "idealDeathRate", "nil" },
                { "spreadRate", "nil" },
                { "maxPollutionDensity", "nil" },
                { "pollutionTolerance", "nil" },
                { "carbonRelease", "nil" },
                { "nitrogenHalfSpeed", "nil" },
                { "nitrogenContent", "nil" },
                { "phosphorusHalfSpeed", "nil" },
                { "phosphorusContent", "nil" },
                { "potassiumHalfSpeed", "nil" },
                { "potassiumContent", "nil" },
                { "soilMoistureHalfSpeed", "nil" },
                { "soilMoistureContent", "nil" },
                { "consumedFertileGround", "nil" },
                { "consumedCanopySpace", "nil" },
                { "consumedUnderwaterFertileGorund", "nil" },
                { "consumedShrubSpace", "nil" }
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
                        EveryPlant[plantName]["extremeTempMin"] = "'" + plant.TemperatureExtremes.Min.ToString("F1") + "'";
                        EveryPlant[plantName]["idealTempMin"] = "'" + plant.IdealTemperatureRange.Min.ToString("F1") + "'";
                        EveryPlant[plantName]["idealTempMax"] = "'" + plant.IdealTemperatureRange.Max.ToString("F1") + "'";
                        EveryPlant[plantName]["extremeTempMax"] = "'" + plant.TemperatureExtremes.Max.ToString("F1") + "'";
                        EveryPlant[plantName]["extremeMoistureMin"] = "'" + plant.MoistureExtremes.Min.ToString("F1") + "'";
                        EveryPlant[plantName]["idealMoistureMin"] = "'" + plant.IdealMoistureRange.Min.ToString("F1") + "'";
                        EveryPlant[plantName]["idealMoistureMax"] = "'" + plant.IdealMoistureRange.Max.ToString("F1") + "'";
                        EveryPlant[plantName]["extremeMoistureMax"] = "'" + plant.MoistureExtremes.Max.ToString("F1") + "'";
                        EveryPlant[plantName]["maturity"] = "'" + plant.MaturityAgeDays.ToString("F1") + "'";
                        EveryPlant[plantName]["isDecorative"] = plant.Decorative ? "'Decorative'" : "nil";
                        EveryPlant[plantName]["isWater"] = plant.Water ? "'Underwater'" : "nil";
                        EveryPlant[plantName]["calorieValue"] = "'" + plant.CalorieValue.ToString("F1") + "'";

                        /* Seeds no longer a stat ??
                        if (plant.SeedItemType != null) { EveryPlant[plantName]["seedDrop"] = "'" + SplitName(RemoveItemTag(plant.SeedItemType.Name)) + "'"; }
                        
                        EveryPlant[plantName]["seedDropChance"] = "'" + (plant.SeedDropChance * 100).ToString("F0") + "'";
                        EveryPlant[plantName]["seedAtGrowth"] = "'" + (plant.SeedsAtGrowth * 100).ToString("F0") + "'";
                        EveryPlant[plantName]["seedBonusGrowth"] = "'" + (plant.SeedsBonusAtGrowth * 100).ToString("F0") + "'";
                        EveryPlant[plantName]["seedMax"] = "'" + plant.SeedRange.Max.ToString("F1") + "'";
                        EveryPlant[plantName]["seedMin"] = "'" + plant.SeedRange.Min.ToString("F1") + "'";
                        */

                        if (Block.Is<Reapable>(plant.BlockType))
                            EveryPlant[plantName]["harvestTool"] = "'Scythe'";
                        else if (Block.Is<Diggable>(plant.BlockType))
                            EveryPlant[plantName]["harvestTool"] = "'Shovel'";

                        if (plant.PostHarvestingGrowth == 0)
                            EveryPlant[plantName]["killOnHarvest"] = "'Yes'";
                        else
                            EveryPlant[plantName]["killOnHarvest"] = "'No'";

                        if (plant.PostHarvestingGrowth != 0)
                            EveryPlant[plantName]["postHarvestGrowth"] = "'" + (plant.PostHarvestingGrowth * 100).ToString("F0") + "'";

                        EveryPlant[plantName]["scytheKills"] = plant.ScythingKills ? "'Yes'" : "nil";
                        EveryPlant[plantName]["pickableAtPercent"] = "'" + (plant.PickableAtPercent * 100).ToString("F0") + "'";
                        EveryPlant[plantName]["carbonRelease"] = "'" + plant.ReleasesCO2ppmPerDay.ToString("F4") + "'";
                        EveryPlant[plantName]["resourceMin"] = "'" + plant.ResourceRange.Min.ToString("F1") + "'";
                        EveryPlant[plantName]["resourceMax"] = "'" + plant.ResourceRange.Max.ToString("F1") + "'";
                        EveryPlant[plantName]["resourceBonus"] = "'" + (plant.ResourceBonusAtGrowth * 100).ToString("F0") + "'";
                        EveryPlant[plantName]["idealGrowthRate"] = "'" + plant.MaxGrowthRate.ToString("F4") + "'";
                        EveryPlant[plantName]["idealDeathRate"] = "'" + plant.MaxDeathRate.ToString("F4") + "'";
                        EveryPlant[plantName]["spreadRate"] = "'" + plant.SpreadRate.ToString("F4") + "'";
                        EveryPlant[plantName]["maxPollutionDensity"] = "'" + plant.MaxPollutionDensity.ToString("F4") + "'";
                        EveryPlant[plantName]["pollutionTolerance"] = "'" + plant.PollutionDensityTolerance.ToString("F4") + "'";

                        if (plant.ResourceItemType != null) { EveryPlant[plantName]["resourceItem"] = "'[[" + SplitName(RemoveItemTag(plant.ResourceItemType.Name)) + "]]'"; }

                        #region Resource Constraints
                        if (plant.ResourceConstraints != null)
                        {
                            foreach (ResourceConstraint r in plant.ResourceConstraints)
                            {
                                if (r.LayerName == "Nitrogen")
                                {
                                    EveryPlant[plantName]["nitrogenHalfSpeed"] = "'" + (r.HalfSpeedConcentration * 100).ToString("F0") + "'";
                                    EveryPlant[plantName]["nitrogenContent"] = "'" + (r.MaxResourceContent * 100).ToString("F0") + "'";
                                }
                                if (r.LayerName == "Phosphorus")
                                {
                                    EveryPlant[plantName]["phosphorusHalfSpeed"] = "'" + (r.HalfSpeedConcentration * 100).ToString("F0") + "'";
                                    EveryPlant[plantName]["phosphorusContent"] = "'" + (r.MaxResourceContent * 100).ToString("F0") + "'";
                                }
                                if (r.LayerName == "Potassium")
                                {
                                    EveryPlant[plantName]["potassiumHalfSpeed"] = "'" + (r.HalfSpeedConcentration * 100).ToString("F0") + "'";
                                    EveryPlant[plantName]["potassiumContent"] = "'" + (r.MaxResourceContent * 100).ToString("F0") + "'";
                                }
                                if (r.LayerName == "SoilMoisture")
                                {
                                    EveryPlant[plantName]["soilMoistureHalfSpeed"] = "'" + (r.HalfSpeedConcentration * 100).ToString("F0") + "'";
                                    EveryPlant[plantName]["soilMoistureContent"] = "'" + (r.MaxResourceContent * 100).ToString("F0") + "'";
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
                                    EveryPlant[plantName]["consumedFertileGround"] = "'" + (c.ConsumedCapacityPerPop).ToString("F1") + "'";
                                if (c.CapacityLayerName == "CanopySpace")
                                    EveryPlant[plantName]["consumedCanopySpace"] = "'" + (c.ConsumedCapacityPerPop).ToString("F1") + "'";
                                if (c.CapacityLayerName == "UnderwaterFertileGorund")
                                    EveryPlant[plantName]["consumedUnderwaterFertileGorund"] = "'" + (c.ConsumedCapacityPerPop).ToString("F1") + "'";
                                if (c.CapacityLayerName == "ShrubSpace")
                                    EveryPlant[plantName]["consumedShrubSpace"] = "'" + (c.ConsumedCapacityPerPop).ToString("F1") + "'";
                            }
                        }
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
                { "extremeTempMin", "nil" },
                { "idealTempMin", "nil" },
                { "idealTempMax", "nil" },
                { "extremeTempMax", "nil" },
                { "extremeMoistureMin", "nil" },
                { "idealMoistureMin", "nil" },
                { "idealMoistureMax", "nil" },
                { "extremeMoistureMax", "nil" },
                { "pickableAtPercent", "nil" },
                { "resourceItem", "nil" },
                { "resourceMin", "nil" },
                { "resourceMax", "nil" },
                { "debrisSpawnChance", "nil" },
                { "treeHealth", "nil" },
                { "logHealth", "nil" },
                { "maturity", "nil" },
                { "isWater", "nil" },
                { "isDecorative", "nil" },
                { "calorieValue", "nil" },
                /*
                { "seedDrop", "nil" },
                { "seedDropChance", "nil" },
                { "seedAtGrowth", "nil" },
                { "seedBonusGrowth", "nil" },
                { "seedMax", "nil" },
                { "seedMin", "nil" },
                */
                { "killOnHarvest", "nil" },
                { "postHarvestGrowth", "nil" },
                { "scytheKills", "nil" },
                { "idealGrowthRate", "nil" },
                { "idealDeathRate", "nil" },
                { "spreadRate", "nil" },
                { "maxPollutionDensity", "nil" },
                { "pollutionTolerance", "nil" },
                { "carbonRelease", "nil" },
                { "nitrogenHalfSpeed", "nil" },
                { "nitrogenContent", "nil" },
                { "phosphorusHalfSpeed", "nil" },
                { "phosphorusContent", "nil" },
                { "potassiumHalfSpeed", "nil" },
                { "potassiumContent", "nil" },
                { "soilMoistureHalfSpeed", "nil" },
                { "soilMoistureContent", "nil" },
                { "consumedFertileGround", "nil" },
                { "consumedCanopySpace", "nil" },
                { "consumedUnderwaterFertileGorund", "nil" },
                { "consumedShrubSpace", "nil" }
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
                        EveryTree[treeName]["extremeTempMin"] = "'" + tree.TemperatureExtremes.Min.ToString("F1") + "'";
                        EveryTree[treeName]["idealTempMin"] = "'" + tree.IdealTemperatureRange.Min.ToString("F1") + "'";
                        EveryTree[treeName]["idealTempMax"] = "'" + tree.IdealTemperatureRange.Max.ToString("F1") + "'";
                        EveryTree[treeName]["extremeTempMax"] = "'" + tree.TemperatureExtremes.Max.ToString("F1") + "'";
                        EveryTree[treeName]["extremeMoistureMin"] = "'" + tree.MoistureExtremes.Min.ToString("F1") + "'";
                        EveryTree[treeName]["idealMoistureMin"] = "'" + tree.IdealMoistureRange.Min.ToString("F1") + "'";
                        EveryTree[treeName]["idealMoistureMax"] = "'" + tree.IdealMoistureRange.Max.ToString("F1") + "'";
                        EveryTree[treeName]["extremeMoistureMax"] = "'" + tree.MoistureExtremes.Max.ToString("F1") + "'";
                        EveryTree[treeName]["maturity"] = "'" + tree.MaturityAgeDays.ToString("F1") + "'";
                        EveryTree[treeName]["isDecorative"] = tree.Decorative ? "'Decorative'" : "nil";
                        EveryTree[treeName]["isWater"] = tree.Water ? "'Underwater'" : "nil";
                        EveryTree[treeName]["calorieValue"] = "'" + tree.CalorieValue.ToString("F1") + "'";

                        /* Seeds no longer a stat ??
                        if (tree.SeedItemType != null) { EveryTree[treeName]["seedDrop"] = "'[[" + SplitName(RemoveItemTag(tree.SeedItemType.Name)) + "]]'"; }

                        EveryTree[treeName]["seedDropChance"] = "'" + (tree.SeedDropChance * 100).ToString("F0") + "'";
                        EveryTree[treeName]["seedAtGrowth"] = "'" + (tree.SeedsAtGrowth * 100).ToString("F0") + "'";
                        EveryTree[treeName]["seedBonusGrowth"] = "'" + (tree.SeedsBonusAtGrowth * 100).ToString("F0") + "'";
                        EveryTree[treeName]["seedMax"] = "'" + tree.SeedRange.Max.ToString("F1") + "'";
                        EveryTree[treeName]["seedMin"] = "'" + tree.SeedRange.Min.ToString("F1") + "'";
                        */

                        if (tree.PostHarvestingGrowth == 0)
                            EveryTree[treeName]["killOnHarvest"] = "'Yes'";
                        else
                            EveryTree[treeName]["killOnHarvest"] = "'No'";

                        if (tree.PostHarvestingGrowth != 0)
                            EveryTree[treeName]["postHarvestGrowth"] = "'" + (tree.PostHarvestingGrowth * 100).ToString("F0") + "'";

                        EveryTree[treeName]["scytheKills"] = tree.ScythingKills ? "'Yes'" : "nil";
                        EveryTree[treeName]["pickableAtPercent"] = "'" + (tree.PickableAtPercent * 100).ToString("F0") + "'";
                        EveryTree[treeName]["carbonRelease"] = "'" + tree.ReleasesCO2ppmPerDay.ToString("F4") + "'";
                        EveryTree[treeName]["resourceMin"] = "'" + tree.ResourceRange.Min.ToString("F1") + "'";
                        EveryTree[treeName]["resourceMax"] = "'" + tree.ResourceRange.Max.ToString("F1") + "'";
                        EveryTree[treeName]["resourceBonus"] = "'" + (tree.ResourceBonusAtGrowth * 100).ToString("F0") + "'";
                        EveryTree[treeName]["debrisSpawnChance"] = "'" + (tree.ChanceToSpawnDebris * 100).ToString("F0") + "'";
                        EveryTree[treeName]["treeHealth"] = "'" + tree.TreeHealth.ToString("F1") + "'";
                        EveryTree[treeName]["logHealth"] = "'" + tree.LogHealth.ToString("F1") + "'";
                        EveryTree[treeName]["idealGrowthRate"] = "'" + tree.MaxGrowthRate.ToString("F4") + "'";
                        EveryTree[treeName]["idealDeathRate"] = "'" + tree.MaxDeathRate.ToString("F4") + "'";
                        EveryTree[treeName]["spreadRate"] = "'" + tree.SpreadRate.ToString("F4") + "'";
                        EveryTree[treeName]["maxPollutionDensity"] = "'" + tree.MaxPollutionDensity.ToString("F4") + "'";
                        EveryTree[treeName]["pollutionTolerance"] = "'" + tree.PollutionDensityTolerance.ToString("F4") + "'";

                        if (tree.ResourceItemType != null) { EveryTree[treeName]["resourceItem"] = "'[[" + SplitName(RemoveItemTag(tree.ResourceItemType.Name)) + "]]'"; }

                        if (tree.ResourceConstraints != null)
                        {
                            foreach (ResourceConstraint r in tree.ResourceConstraints)
                            {
                                if (r.LayerName == "Nitrogen")
                                {
                                    EveryTree[treeName]["nitrogenHalfSpeed"] = "'" + (r.HalfSpeedConcentration * 100).ToString("F0") + "'";
                                    EveryTree[treeName]["nitrogenContent"] = "'" + (r.MaxResourceContent * 100).ToString("F0") + "'";
                                }
                                if (r.LayerName == "Phosphorus")
                                {
                                    EveryTree[treeName]["phosphorusHalfSpeed"] = "'" + (r.HalfSpeedConcentration * 100).ToString("F0") + "'";
                                    EveryTree[treeName]["phosphorusContent"] = "'" + (r.MaxResourceContent * 100).ToString("F0") + "'";
                                }
                                if (r.LayerName == "Potassium")
                                {
                                    EveryTree[treeName]["potassiumHalfSpeed"] = "'" + (r.HalfSpeedConcentration * 100).ToString("F0") + "'";
                                    EveryTree[treeName]["potassiumContent"] = "'" + (r.MaxResourceContent * 100).ToString("F0") + "'";
                                }
                                if (r.LayerName == "SoilMoisture")
                                {
                                    EveryTree[treeName]["soilMoistureHalfSpeed"] = "'" + (r.HalfSpeedConcentration * 100).ToString("F0") + "'";
                                    EveryTree[treeName]["soilMoistureContent"] = "'" + (r.MaxResourceContent * 100).ToString("F0") + "'";
                                }
                            }
                        }

                        if (tree.CapacityConstraints != null)
                        {
                            foreach (CapacityConstraint c in tree.CapacityConstraints)
                            {
                                if (c.CapacityLayerName == "FertileGorund")
                                    EveryTree[treeName]["consumedFertileGround"] = "'" + (c.ConsumedCapacityPerPop).ToString("F1") + "'";
                                if (c.CapacityLayerName == "CanopySpace")
                                    EveryTree[treeName]["consumedCanopySpace"] = "'" + (c.ConsumedCapacityPerPop).ToString("F1") + "'";
                                if (c.CapacityLayerName == "UnderwaterFertileGorund")
                                    EveryTree[treeName]["consumedUnderwaterFertileGorund"] = "'" + (c.ConsumedCapacityPerPop).ToString("F1") + "'";
                                if (c.CapacityLayerName == "ShrubSpace")
                                    EveryTree[treeName]["consumedShrubSpace"] = "'" + (c.ConsumedCapacityPerPop).ToString("F1") + "'";
                            }
                        }
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
                { "speed", "nil" },
                { "wanderingSpeed", "nil" },
                { "health", "nil" },
                { "isSwimming", "nil" },
                { "isFlying", "nil" },
                { "flees", "nil" },
                { "fearFactor", "nil" },
                { "minAttackDelay", "nil" },
                { "maxAttackDelay", "nil" },
                { "resourceItem", "nil" },
                { "resourceMin", "nil" },
                { "resourceMax", "nil" },
                { "resourceBonus", "nil" },
                { "maturity", "nil" },
                { "damage", "nil" },
                { "attackRange", "nil" },
                { "calorieValue", "nil" },
                { "foodSources", "nil" },
                { "carbonRelease", "nil" }
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
                        EveryAnimal[animalName]["speed"] = "'" + animal.Speed.ToString("F1") + "'";
                        EveryAnimal[animalName]["wanderingSpeed"] = "'" + animal.WanderingSpeed.ToString("F1") + "'";
                        EveryAnimal[animalName]["health"] = "'" + animal.Health.ToString("F1") + "'";
                        EveryAnimal[animalName]["isSwimming"] = animal.Swimming ? "'Swimming'" : "nil";
                        EveryAnimal[animalName]["isFlying"] = animal.Flying ? "'Flying'" : "nil";
                        EveryAnimal[animalName]["flees"] = animal.FleePlayers ? "'Flees'" : "nil";
                        EveryAnimal[animalName]["fearFactor"] = "'" + animal.FearFactor.ToString("F1") + "'";
                        EveryAnimal[animalName]["maturity"] = "'" + animal.MaturityAgeDays.ToString("F1") + "'";
                        EveryAnimal[animalName]["damage"] = "'" + animal.Damage.ToString("F1") + "'";
                        EveryAnimal[animalName]["attackRange"] = "'" + animal.AttackRange.ToString("F1") + "'";
                        EveryAnimal[animalName]["minAttackDelay"] = "'" + animal.DelayBetweenAttacksRangeSec.Min.ToString("F1") + "'";
                        EveryAnimal[animalName]["maxAttackDelay"] = "'" + animal.DelayBetweenAttacksRangeSec.Max.ToString("F1") + "'";
                        EveryAnimal[animalName]["calorieValue"] = "'" + animal.CalorieValue.ToString("F1") + "'";
                        EveryAnimal[animalName]["carbonRelease"] = "'" + animal.ReleasesCO2ppmPerDay.ToString("F4") + "'";
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
                    }
                }
            }
            WriteDictionaryToFile(user, "Wiki_Module_AnimalData.txt", "animals", EveryAnimal);
        }
    }
}