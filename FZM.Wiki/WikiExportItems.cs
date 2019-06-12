using Eco.Gameplay.Components;
using Eco.Gameplay.Housing;
using Eco.Gameplay.Items;
using Eco.Gameplay.Objects;
using Eco.Gameplay.Players;
using Eco.Gameplay.Property;
using Eco.Gameplay.Systems.Chat;
using Eco.Shared.Math;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Eco.Shared;
using Eco.Shared.Localization;
using Eco.Shared.Services;
using Eco.Gameplay.Pipes.LiquidComponents;

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
        // dictionary of items and their dictionary of stats
        private static SortedDictionary<string, Dictionary<string, string>> EveryItem = new SortedDictionary<string, Dictionary<string, string>>();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="user"></param>
        [ChatCommand("Creates a dump file of all discovered items", ChatAuthorizationLevel.Admin)]
        public static void ItemDetails(User user)
        {
            // dictionary of item properties
            Dictionary<string, string> itemDetails = new Dictionary<string, string>()
            {
                { "category", "nil" },
                { "group", "nil" },
                { "description", "nil" },
                { "maxStack", "nil" },
                { "carried", "nil" },
                { "weight", "nil" },
                { "calories", "nil" },
                { "carbs", "nil" },
                { "protein", "nil" },
                { "fat", "nil" },
                { "vitamins", "nil" },
                { "density", "nil" },
                { "fuel", "nil" },
                { "yield", "nil" },
                { "currency", "nil" },
                { "skillValue", "nil" },
                { "roomCategory", "nil" },
                { "furnitureType", "nil" },
                { "repeatsDepreciation", "nil" },
                { "materialTier", "nil" },
                { "fuelsUsed", "nil" },
                { "gridRadius", "nil" },
                { "energyUsed", "nil" },
                { "energyProduced", "nil" },
                { "energyType", "nil" },
                { "fluidsUsed", "nil"},
                { "fluidsProduced" , "nil"},
                { "validTalents", "nil" },
                { "footprint", "nil" },
                { "mobile", "nil" },
                { "roomSizeReq", "nil" },
                { "roomMatReq", "nil" },
                { "roomContainReq", "nil" },
                { "inventorySlots", "nil" },
                { "inventoryMaxWeight", "nil" },
                { "type", "nil" },
                { "typeID", "nil" }
            };

            foreach (Item allItem in Item.AllItems)
            {
                //Console.WriteLine("Item: " + allItem.DisplayName);
                if (!EveryItem.ContainsKey(allItem.DisplayName) && (allItem.DisplayName != "Chat Log") && (allItem.DisplayName != "Vehicle Tool Toggle") && (allItem.Group != "Skills") && (allItem.Group != "Talents"))
                {
                    string displayName = allItem.DisplayName;
                    EveryItem.Add(displayName, new Dictionary<string, string>(itemDetails));
                    EveryItem[displayName]["category"] = "'" + allItem.Category + "'";
                    EveryItem[displayName]["group"] = "'" + allItem.Group + "'";
                    EveryItem[displayName]["type"] = "'" + allItem.Type.ToString().Substring(allItem.Type.ToString().LastIndexOf('.') + 1) + "'";
                    EveryItem[displayName]["typeID"] = "'" + allItem.TypeID.ToString() + "'";

                    Regex regex = new Regex("\t\n\v\f\r");
                    EveryItem[displayName]["description"] = "'" + regex.Replace(CleanTags(allItem.DisplayDescription), " ").Replace("'", "\\'") + "'";

                    EveryItem[displayName]["maxStack"] = "'" + allItem.MaxStackSize.ToString() + "'";
                    EveryItem[displayName]["carried"] = allItem.IsCarried ? "'Hands'" : "'Backpack'";
                    EveryItem[displayName]["currency"] = allItem.CanBeCurrency ? "'Yes'" : "nil";
                    if (allItem.HasWeight) { EveryItem[displayName]["weight"] = "'" + ((Decimal)allItem.Weight / 1000).ToString() + "'"; }
                    if (allItem.IsFuel) { EveryItem[displayName]["fuel"] = "'" + allItem.Fuel.ToString() + "'"; }
                    if (allItem.HasYield) { EveryItem[displayName]["yield"] = "'[[" + allItem.Yield.Skill.DisplayName + "]]'"; }

                    #region Food Items

                    // if the item is also a food item get the nutrient values
                    if (allItem is FoodItem)
                    {
                        FoodItem foodItem = allItem as FoodItem;
                        EveryItem[displayName]["calories"] = "'" + foodItem.Calories.ToString("F1") + "'";
                        EveryItem[displayName]["carbs"] = "'" + foodItem.Nutrition.Carbs.ToString("F1") + "'";
                        EveryItem[displayName]["protein"] = "'" + foodItem.Nutrition.Protein.ToString("F1") + "'";
                        EveryItem[displayName]["fat"] = "'" + foodItem.Nutrition.Fat.ToString("F1") + "'";
                        EveryItem[displayName]["vitamins"] = "'" + foodItem.Nutrition.Vitamins.ToString("F1") + "'";
                        if (float.IsNaN(foodItem.Nutrition.Values.Sum() / foodItem.Calories))
                            EveryItem[displayName]["density"] = "'0.0'";
                        else
                            EveryItem[displayName]["density"] = "'" + ((foodItem.Nutrition.Values.Sum() / foodItem.Calories) * 100).ToString("F1") + "'";
                    }

                    #endregion

                    #region Housing Values

                    // if the item is a world item that has a housing category, housing value details still sit on the item so needs to be seperated from Object properties
                    if (allItem.Group == "World Object Items" || allItem.Group == "Modules") //&& allItem.Type != typeof(GasGeneratorItem)?
                    {
                        PropertyInfo[] props = allItem.Type.GetProperties();
                        foreach (var prop in props)
                        {
                            //if (prop.GetValue(allItem) != null) { Console.WriteLine("ItemProperties - " + prop.Name + ": " + prop.GetValue(allItem).ToString()); }
                            if (prop.Name == "HousingVal")
                            {
                                HousingValue v = prop.GetValue(allItem) as HousingValue;
                                EveryItem[displayName]["skillValue"] = "'" + v.Val.ToString() + "'";
                                EveryItem[displayName]["roomCategory"] = "'" + v.Category.ToString() + "'";
                                if (v.Category.ToString() != "Industrial")
                                {
                                    EveryItem[displayName]["furnitureType"] = "'" + v.TypeForRoomLimit.ToString() + "'";
                                    EveryItem[displayName]["repeatsDepreciation"] = "'" + v.DiminishingReturnPercent.ToString() + "'";
                                }
                            }
                        }
                    }

                    #endregion

                    #region Materials & Tiers

                    // if the item is a block then add it's tier
                    if (allItem.Group == "Block Items")
                    {
                        PropertyInfo[] props = allItem.Type.GetProperties();
                        foreach (var prop in props)
                        {
                            if (prop.Name == "Tier")
                            {
                                int t = (int)prop.GetValue(allItem);
                                EveryItem[displayName]["materialTier"] = "'" + t.ToString() + "'";
                            }
                        }
                    }
                    #endregion

                    #region World Objects

                    // for world objects we need to get the object placed in world to access it's properties, each object is destroyed at the end of it's read.
                    if (allItem.Group == "World Object Items" || allItem.Group == "Road Items" || allItem.Group == "Modules") //&& allItem.Type != typeof(GasGeneratorItem)
                    {
                        WorldObjectItem i = allItem as WorldObjectItem;
                        WorldObject obj = Activator.CreateInstance(i.WorldObjectType, true) as WorldObject;
                        WorldObjectManager.Add(obj, user, user.Player.Position, Quaternion.Identity);
                        PropertyInfo[] props = obj.GetType().GetProperties();

                        EveryItem[displayName]["mobile"] = obj.Mobile ? "'Yes'" : "nil";

                        #region World Object Liquid Components

                        // Checks the objectfor the three liquid components and returns the private fields of those components to the dictionary.

                        // first create a list item and rate strings to attach
                        List<string> consumedFluids = new List<string>();
                        List<string> producedFluids = new List<string>();

                        // We assume each component will only be on the WorldObject once... dangerous with SLG devs.
                        var lp = obj.GetComponent<LiquidProducerComponent>();
                        if (lp != null)
                        {
                            Type producesType = (Type)GetFieldValue(lp, "producesType");
                            int productionRate = (int)GetFieldValue(lp, "constantProductionRate");

                            producedFluids.Add("{'[[" + SplitName(RemoveItemTag(producesType.Name) + "]]', " + productionRate + "}"));
                        }

                        var lc = obj.GetComponent<LiquidConsumerComponent>();
                        if (lc != null)
                        {
                            Type acceptedType = lc.AcceptedType;
                            int consumptionRate = (int)GetFieldValue(lc, "constantConsumptionRate");

                            consumedFluids.Add("{'[[" + SplitName(RemoveItemTag(acceptedType.Name) + "]], " + consumptionRate + "}"));
                        }

                        var lconv = obj.GetComponent<LiquidConverterComponent>();
                        if (lconv != null)
                        {
                            LiquidProducerComponent convLP = (LiquidProducerComponent)GetFieldValue(lconv, "producer");
                            LiquidConsumerComponent convLC = (LiquidConsumerComponent)GetFieldValue(lconv, "consumer");

                            Type producesType = (Type)GetFieldValue(convLP, "producesType");
                            int productionRate = (int)GetFieldValue(convLP, "constantProductionRate");

                            producedFluids.Add("{'[[" + SplitName(RemoveItemTag(producesType.Name) + "]]', " + productionRate + "}"));

                            Type acceptedType = convLC.AcceptedType;
                            int consumptionRate = (int)GetFieldValue(convLC, "constantConsumptionRate");
                            consumedFluids.Add("{'[[" + SplitName(RemoveItemTag(acceptedType.Name) + "]]', " + consumptionRate + "}"));
                        }

                        // combine the strings to add to the dictionary
                        foreach (string str in consumedFluids)
                        {
                            if (str == consumedFluids.First())
                                EveryItem[displayName]["fluidsUsed"] = "{" + str;
                            else
                                EveryItem[displayName]["fluidsUsed"] += str;

                            if (str != consumedFluids.Last())
                                EveryItem[displayName]["fluidsUsed"] += ",";
                            else
                                EveryItem[displayName]["fluidsUsed"] += "}";
                        }

                        foreach (string str in producedFluids)
                        {
                            if (str == producedFluids.First())
                                EveryItem[displayName]["fluidsProduced"] = "{" + str;
                            else
                                EveryItem[displayName]["fluidsProduced"] += str;
                            if (str != producedFluids.Last())
                                EveryItem[displayName]["fluidsProduced"] += ",";
                            else
                                EveryItem[displayName]["fluidsProduced"] += "}";
                        }

                        #endregion

                        #region World Object Fuel Supply


                        if (obj.HasComponent<FuelSupplyComponent>())
                        {
                            var fuelComponent = obj.GetComponent<FuelSupplyComponent>();
                            string fuelsString = "[[";
                            foreach (Type t in fuelComponent.FuelTypes)
                            {
                                fuelsString += t.Name.Substring(0, t.Name.Length - 4);
                                if (t != fuelComponent.FuelTypes.Last())
                                    fuelsString += "]], [[";
                            }
                            EveryItem[displayName]["fuelsUsed"] = "'" + fuelsString + "]]'";
                        }
                        #endregion

                        #region World Object Power Grid

                        if (obj.HasComponent<PowerGridComponent>())
                        {
                            var gridComponent = obj.GetComponent<PowerGridComponent>();
                            EveryItem[displayName]["energyProduced"] = "'" + gridComponent.EnergySupply.ToString() + "'";
                            EveryItem[displayName]["energyUsed"] = "'" + gridComponent.EnergyDemand.ToString() + "'";
                            EveryItem[displayName]["energyType"] = "'" + gridComponent.EnergyType.Name.ToString() + "'";
                            EveryItem[displayName]["gridRadius"] = "'" + gridComponent.Radius.ToString() + "'";
                        }
                        #endregion

                        #region World Object Room Requirements


                        if (obj.HasComponent<RoomRequirementsComponent>())
                        {
                            var roomRequirementsComponent = obj.GetComponent<RoomRequirementsComponent>();
                            var requirements = RoomRequirements.Get(roomRequirementsComponent.GetType());
                            if (requirements != null)
                            {
                                foreach (RoomRequirementAttribute a in requirements.Requirements)
                                {
                                    if (a.GetType() == typeof(RequireRoomMaterialTierAttribute))
                                    {
                                        EveryItem[displayName]["roomMatReq"] = "'Tier " + (a as RequireRoomMaterialTierAttribute).Tier + "'";
                                    }
                                    if (a.GetType() == typeof(RequireRoomVolumeAttribute))
                                    {
                                        EveryItem[displayName]["roomSizeReq"] = "'" + (a as RequireRoomVolumeAttribute).Volume + "'";
                                    }
                                    if (a.GetType() == typeof(RequireRoomContainmentAttribute))
                                    {
                                        EveryItem[displayName]["roomContainReq"] = "'Yes'";
                                    }
                                }
                            }
                        }
                        #endregion

                        #region World Object Storage Components

                        if (obj.HasComponent<PublicStorageComponent>())
                        {
                            var psc = obj.GetComponent<PublicStorageComponent>();
                            EveryItem[displayName]["inventorySlots"] = "'" + psc.Inventory.Stacks.Count().ToString() + "'";

                            foreach (InventoryRestriction res in psc.Inventory.Restrictions)
                            {
                                if (res is WeightRestriction)
                                {
                                    WeightRestriction wres = res as WeightRestriction;
                                    WeightComponent wc = (WeightComponent)GetFieldValue(wres, "weightComponent");
                                    EveryItem[displayName]["inventoryMaxWeight"] = "'" + wc.MaxWeight.ToString() + "'";
                                }
                            }
                        }

                        #endregion

                        #region World Object Occupancy

                        if (!obj.Mobile || obj.DisplayName == "Wooden Elevator") // removes vehicles from getting a footprint as they don't have an occupancy
                        {
                            //Console.WriteLine("          Occupancy:");
                            List<BlockOccupancy> Occ = obj.Occupancy;
                            List<int> xList = new List<int>();
                            List<int> yList = new List<int>();
                            List<int> zList = new List<int>();

                            // add the int values of all the blocks of the object to the lists
                            foreach (BlockOccupancy bo in Occ)
                            {
                                xList.Add(bo.Offset.X);
                                yList.Add(bo.Offset.Y);
                                zList.Add(bo.Offset.Z);
                            }

                            // as position 0 is a block we need to add '1' to the range to see the correct footprint size
                            string footprint = (xList.Max() - xList.Min() + 1).ToString() + " X " + (yList.Max() - yList.Min() + 1).ToString() + " X " + (zList.Max() - zList.Min() + 1).ToString();
                            EveryItem[displayName]["footprint"] = "'" + footprint + "'";
                        }

                        #endregion

                        #region Talents

                        if (obj.HasComponent<CraftingComponent>())
                        {
                            var cc = obj.GetComponent<CraftingComponent>();
                            string talentString = "[[";
                            foreach (string talent in cc.ValidTalents)
                            {
                                talentString += talent;
                                if (talent != cc.ValidTalents.Last())
                                    talentString += "]], [[";
                            }
                            EveryItem[displayName]["validTalents"] = "'" + talentString + "]]'";
                        }

                        #endregion

                        if (!obj.Name.Contains("Ramp"))
                            obj.Destroy();
                    }

                    #endregion
                }
            }

            #region WriteToFile
            // writes to WikiItems.txt to the Eco Server directory.
            string path = AppDomain.CurrentDomain.BaseDirectory + "Wiki_Module_ItemData.txt";
            using (StreamWriter streamWriter = new StreamWriter(path, false))
            {
                streamWriter.WriteLine("-- Eco Version : " + EcoVersion.Version);
                streamWriter.WriteLine();
                streamWriter.WriteLine("return {\n    items = {");
                foreach (string key in EveryItem.Keys)
                {
                    streamWriter.WriteLine(string.Format("{0}['{1}'] = {{", space2, key));
                    foreach (KeyValuePair<string, string> keyValuePair in EveryItem[key])
                        streamWriter.WriteLine(string.Format("{0}{1}['{2}'] = {3},", space2, space3, keyValuePair.Key, keyValuePair.Value));
                    streamWriter.WriteLine(string.Format("{0}{1}}},", space2, space3));
                }
                streamWriter.WriteLine("    },\n}");
                streamWriter.Close();
                user.Player.SendTemporaryMessage(Localizer.Do($"Dumped to  {AppDomain.CurrentDomain.BaseDirectory} Wiki_Module_ItemData.txt"), ChatCategory.Info);
            }
            #endregion

        }
    }
}