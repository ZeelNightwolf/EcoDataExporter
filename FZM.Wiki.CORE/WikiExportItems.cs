using Eco.Gameplay;
using Eco.Gameplay.Components;
using Eco.Gameplay.Items;
using Eco.Gameplay.Objects;
using Eco.Gameplay.Players;
using Eco.Gameplay.Property;
using Eco.Gameplay.Skills;
using Eco.Gameplay.Systems.Chat;
using Eco.Gameplay.Pipes.LiquidComponents;
using Eco.Gameplay.Systems;
using Eco.Shared.Localization;
using Eco.Shared.Utils;
using Eco.Mods.TechTree;
using Eco.Shared.Math;
using Eco.World;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text;
using System.IO;
using Eco.World.Blocks;
using Eco.Gameplay.Housing.PropertyValues;
using Eco.Shared.IoC;
using Eco.Gameplay.Housing;
using Eco.Gameplay.Systems.Messaging.Chat.Commands;

/*
 * This script is an extension by FZM based on the work done by Pradoxzon.
 * 
 * Most code was re-written to make use of changed or new additions to the Eco source code
 * and to change the reliance on Pradoxzon Core Utilities mod.
 *  
 */

namespace FZM.Wiki
{
    public partial class WikiDetails
    {
        // required for clearing space for objects
        static Vector3i cellSize = new Vector3i(10, 10, 10);
        static Vector3i spawnPoint = new Vector3i(0, 75, 0);

        // dictionary of items and their dictionary of stats
        private static SortedDictionary<string, Dictionary<string, string>> EveryItem = new();
        private static SortedDictionary<string, Dictionary<string, string>> tagItemDic = new();
        private static Dictionary<string, (string, Exception)> ErrorItems = new();

        public static void ItemDetails(User user)
        {
            // dictionary of item properties
            Dictionary<string, string> itemDetails = new Dictionary<string, string>()
            {
                { "untranslated", "nil" },
                { "category", "nil" },
                { "group", "nil" },
                { "description", "nil" },
                { "tagGroups", "nil"},
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
                { "inventoryRestrictions", "nil" },
                { "fertilizerNutrients", "nil" },
                { "type", "nil" },
                { "typeID", "nil" }
            };

            if (user.IsOnline)
            {
                PrepGround(user, (Vector3i)user.Position + new Vector3i(12, 0, 12));
                PrepGround(user, (Vector3i)user.Position + new Vector3i(-12, 0, -12));
            }

            string displayName;
            foreach (Item item in Item.AllItems)
            {
                displayName = item.DisplayName == "" && item is ToolItem ? SplitName(RemoveItemTag(item.Type.Name)) : item.DisplayName;
                string prop = "";
                try
                {
                    if (!EveryItem.ContainsKey(displayName)
                        && (item.DisplayName != "Chat Log")
                        && (item.DisplayName != "Vehicle Tool Toggle")
                        && (item.Group != "Skills")
                        && (item.Group != "Talents")
                        && item.Group != "Actionbar Items")
                    {
                        var itemD = new Dictionary<string, string>(itemDetails);

                        prop = "untranslated";                 itemD[prop] = $"'{item.DisplayName.NotTranslated}'";
                        prop = "category";                     itemD[prop] = $"'{Localizer.DoStr(item.Category)}'";
                        prop = "group";                        itemD[prop] = $"'{Localizer.DoStr(item.Group)}'";
                        prop = "type";                         itemD[prop] = $"'{item.Type.ToString().Substring(item.Type.ToString().LastIndexOf('.') + 1)}'";
                        prop = "typeID";                       itemD[prop] = $"'{item.TypeID}'";

                        Regex regex = new Regex("[\t\n\v\f\r]");
                        prop = "description";                  itemD[prop] = $"'{regex.Replace(CleanTags(item.DisplayDescription), " ").Replace("'", "\\'")}'";
                        prop = "tagGroups";                    itemD[prop] = GetItemTags(item);
                        prop = "maxStack";                     itemD[prop] = $"'{item.MaxStackSize}'";
                        prop = "carried";                      itemD[prop] = item.IsCarried ? $"'{Localizer.DoStr("Hands")}'" : $"'{Localizer.DoStr("Backpack")}'";
                        prop = "currency";                     itemD[prop] = item.CanBeCurrency ? $"'{Localizer.DoStr("Yes")}'" : "nil";
                        prop = "weight"; if (item.HasWeight)   itemD[prop] = $"'{(decimal)item.Weight / 1000}'";
                        prop = "fuel";   if (item.IsFuel)      itemD[prop] = $"'{item.Fuel}'";
                        prop = "yield";  if (item.HasYield)    itemD[prop] = $"'[[{item.Yield.Skill.DisplayName}]]'";

                        // if the item is a block then add it's tier
                        prop = "materialTier"; if (item.Group == "Block Items") itemD[prop] = $"'{(int)GetPropertyValue(item, "Tier")}'";

                        #region Food Items
                        // if the item is also a food item get the nutrient values
                        if (item is FoodItem foodItem)
                        {
                            prop = "calories"; itemD[prop] = $"'{foodItem.Calories:F1}'";
                            prop = "carbs";    itemD[prop] = $"'{foodItem.Nutrition.Carbs:F1}'";
                            prop = "protein";  itemD[prop] = $"'{foodItem.Nutrition.Protein:F1}'";
                            prop = "fat";      itemD[prop] = $"'{foodItem.Nutrition.Fat:F1}'";
                            prop = "vitamins"; itemD[prop] = $"'{foodItem.Nutrition.Vitamins:F1}'";
                            prop = "density";  itemD[prop] = float.IsNaN(foodItem.Nutrition.Values().Sum() / foodItem.Calories)
                            ? "'0.0'"
                            : $"'{(foodItem.Nutrition.Values().Sum() / foodItem.Calories) * 100:F1}'";
                        }
                        #endregion

                        #region Fertilizers

                        if (IsInstanceOfGenericType(typeof(FertilizerItem<>), item))
                        {
                            prop = "fertilizerNutrients";
                            var desc = (LocString)item.GetType().GetMethod("FertilizerTooltip").Invoke(item, null);
                            var stringDesc = desc.ToString();

                            Regex r = new Regex("[:\n]");
                            var nutrients = r.Split(stringDesc);

                            string final = "";
                            for (int i = 0; i < nutrients.Length; i++)
                            {
                                final += $"{{'{nutrients[i].Replace(":", "")}','{nutrients[++i].Replace("\r", "").Replace("\n", "")}'}}";
                                if (nutrients[i + 1] != "") final += ", "; else break;
                            }
                            itemD[prop] = $"{{{final}}}";
                        }
                        #endregion

                        #region World Objects
                        // for world objects we need to get the object placed in world to access it's properties, each object is destroyed at the end of it's read.
                        if (item.Group == "World Object Items" || item.Group == "Road Items" || item.Group == "Modules")
                        {
                            WorldObjectItem i = item as WorldObjectItem;
                            WorldObject obj = user.IsOnline
                                ? WorldObjectManager.ForceAdd(i.WorldObjectType, user, (Vector3i)user.Position + new Vector3i(12, 0, 12), Quaternion.Identity, false)
                                : SpawnOnFlattenedGround(i.WorldObjectType, user, spawnPoint);

                            // Couldn't Place the obj
                            if (obj == null)
                            {
                                obj = SpecialPlacement(user, i.WorldObjectType); // Attempt a special placement                          
                                if (obj == null) { Log.WriteLine(Localizer.DoStr("Unable to create instance of " + i.WorldObjectType.Name)); continue; } // Still couldn't place the obj
                            }

                            prop = "mobile"; if (obj is PhysicsWorldObject) itemD[prop] = $"'{Localizer.DoStr("Yes")}'";
                            prop = "fluidsUded";                            itemD[prop] = $"'{GetConsumedFluids(obj)}'";
                            prop = "fluidsProduced";                        itemD[prop] = $"'{GetProducedFluids(obj)}'";
                            prop = "fuelsUsed";                             itemD[prop] = $"'{GetFuelsUsed(obj)}'";

                            if (!(obj is PhysicsWorldObject) || obj.DisplayName == "Wooden Elevator")
                            { prop = "footprint";                           itemD[prop] = $"'{GetFootprint(obj)}'"; }

                            if (obj.HasComponent<CraftingComponent>())
                            { prop = "validTalents";                        itemD[prop] = $"{GetTalentString(obj)}"; }

                            if (obj.HasComponent<PowerGridComponent>())
                            {
                                var gridComponent = obj.GetComponent<PowerGridComponent>();
                                prop = "energyProduced";                    itemD[prop] = $"'{gridComponent.EnergySupply}'";
                                prop = "energyUsed";                        itemD[prop] = $"'{gridComponent.EnergyDemand}'";
                                prop = "energyType";                        itemD[prop] = $"'{gridComponent.EnergyType.Name}'";
                                prop = "gridRadius";                        itemD[prop] = $"'{gridComponent.Radius}'";
                            }

                            if (obj.HasComponent<HousingComponent>())
                            {
                                var v = obj.GetComponent<HousingComponent>().HomeValue;
                                prop = "roomCategory";                      itemD[prop] = $"'{Localizer.DoStr(v.Category.ToString())}'";
                                if (v.Category != RoomCategory.Industrial)
                                {
                                    prop = "skillValue";                    itemD[prop] = $"'{v.HouseValue}'";
                                    prop = "furnitureType";                 itemD[prop] = $"'{v.TypeForRoomLimit}'";
                                    prop = "repeatsDepreciation";           itemD[prop] = $"'{v.DiminishingReturnPercent}'";
                                }
                            }

                            #region World Object Room Requirements
                            if (obj.HasComponent<RoomRequirementsComponent>())
                            {
                                var roomRequirementsComponent = obj.GetComponent<RoomRequirementsComponent>();
                                var requirements = RoomRequirements.Get(obj.GetType());
                                if (requirements != null)
                                {
                                    foreach (RoomRequirementAttribute a in requirements.Requirements)
                                    {
                                        if (a.GetType() == typeof(RequireRoomMaterialTierAttribute))
                                        {
                                            prop = "roomMatReq"; itemD[prop] = $"'{Localizer.DoStr("Tier")} {(a as RequireRoomMaterialTierAttribute).Tier}'";
                                        }
                                        if (a.GetType() == typeof(RequireRoomVolumeAttribute))
                                        {
                                            prop = "roomSizeReq"; itemD[prop] = $"'{(a as RequireRoomVolumeAttribute).Volume}'";
                                        }
                                        if (a.GetType() == typeof(RequireRoomContainmentAttribute))
                                        {
                                            prop = "roomContainReq"; itemD[prop] = $"'{Localizer.DoStr("Yes")}'";
                                        }
                                    }
                                }
                            }
                            #endregion

                            #region World Object Storage Components
                            if (obj.HasComponent<PublicStorageComponent>())
                            {
                                var psc = obj.GetComponent<PublicStorageComponent>();
                                string wr = "nil";
                                prop = "inventorySlots"; itemD[prop] = $"'{psc.Inventory.Stacks.Count()}'";
                                prop = "inventoryRestrictions"; itemD[prop] = $"{GetInventoryRestrictions(psc, out wr)}";
                                prop = "inventoryMaxWeight"; itemD[prop] = $"{wr}";
                            }
                            #endregion
                            obj.Destroy();
                        }

                        EveryItem.Add(displayName, itemD);
                        #endregion
                    }
                }
                catch (Exception e)
                {
                    AddToErrorLog(ref ErrorItems, displayName, prop, e);
                }
            }
            WriteErrorLogToFile("Wiki_Module_ItemData_Errors.txt", "items", ErrorItems);
            WriteDictionaryToFile("Wiki_Module_ItemData.txt", "items", EveryItem, false);

            var lang = LocalizationPlugin.Config.Language;

            // Append Tag info
            string filename = "Wiki_Module_ItemData.txt";
            string path = SaveLocation + $@"{lang}\" + filename;
            using (StreamWriter streamWriter = new StreamWriter(path, true))
            {
                streamWriter.WriteLine("\n    " + "tags = {");
                foreach (string key in tagItemDic.Keys)
                {
                    streamWriter.Write(string.Format("{0}['{1}'] = {{ ", space2, key));
                    foreach (string value in tagItemDic[key].Keys)
                        streamWriter.Write(string.Format("'{0}', ", value));
                    streamWriter.WriteLine("},");
                }
                streamWriter.WriteLine("    },\n}");
                streamWriter.Close();
            }
        }

        private static string GetTalentString(WorldObject obj)
        {
            var cc = obj.GetComponent<CraftingComponent>();
            string talentString = "";

            foreach (var talent in TalentManager.AllTalents.Where(x => x.TalentType == typeof(CraftingTalent) && x.Base))
            {
                if (talentString.Length > 0)
                {
                    talentString += ", ";
                }
                talentString += "'[[" + Localizer.DoStr(SplitName(talent.GetType().Name)) + "]]'";
            }

            return "{" + talentString + "}";
        }

        private static string GetFootprint(WorldObject obj)
        {
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
            return (xList.Max() - xList.Min() + 1).ToString() + " X " + (yList.Max() - yList.Min() + 1).ToString() + " X " + (zList.Max() - zList.Min() + 1).ToString();
        }

        private static string GetInventoryRestrictions(PublicStorageComponent psc, out string wr)
        {
            wr = "nil";

            if (!psc.Inventory.Restrictions.Any()) return "nil";

            StringBuilder restrictions = new StringBuilder();
            foreach (InventoryRestriction res in psc.Inventory.Restrictions)
            {
                if (res is WeightRestriction)
                {
                    WeightRestriction wres = res as WeightRestriction;
                    WeightComponent wc = (WeightComponent)GetFieldValue(wres, "weightComponent");
                    wr = $"'{wc.MaxWeight}'";
                }

                restrictions.Append($"{{'{Localizer.DoStr(SplitName(res.GetType().Name))}', '{JSONStringSafe(Localizer.DoStr(res.Message))}'}}");
                if (res != psc.Inventory.Restrictions.Last()) restrictions.Append(", ");
            }

            return restrictions.ToString();
        }

        private static WorldObject SpecialPlacement(User user, Type worldObjectType)
        {
            Vector3i placePos = !user.IsOnline ? spawnPoint : (Vector3i)user.Position;
            return worldObjectType == typeof(WoodenElevatorObject)
                ? PlaceWoodenElevator(placePos)
                : worldObjectType == typeof(WindmillObject) || worldObjectType == typeof(WaterwheelObject)
                ? PlaceWindmill(placePos, worldObjectType)
                : null;
        }

        private static WorldObject PlaceWindmill(Vector3i placePos, Type worldObjectType)
        {
            int height = 0;
            while (height < 6)
            {
                World.SetBlock(typeof(Eco.World.Blocks.DirtBlock), placePos + new Vector3i(-12, height, -12));
                height++;
            }

            return WorldObjectManager.ForceAdd(worldObjectType, null, placePos + new Vector3i(-11, 5, -12), Quaternion.Identity);
        }

        private static WorldObject PlaceWoodenElevator(Vector3i placePos)
        {
            var position = placePos + Vector3i.Up;
            WorldObjectDebugUtil.LevelTerrain(new Vector2i(5, 4), position + new Vector3i(-1, 0, -1), typeof(Eco.World.Blocks.DirtBlock), null);
            WorldObjectDebugUtil.CreateShaft(new Vector2i(1, 2), position + new Vector3i(1, 0, 0), null);
            return WorldObjectManager.ForceAdd(ServiceHolder<IWorldObjectManager>.Obj.GetTypeFromName("WoodenElevatorObject"), null, position, Quaternion.Identity, false);
        }

        //Flatten ground, add a border
        private static void PrepGround(User user, Vector3i position)
        {
            var insideType = BlockManager.FromTypeName("DirtRoadBlock");
            var borderType = BlockManager.FromTypeName("StoneRoadBlock");

            WorldObjectDebugUtil.LevelTerrain(cellSize.XZ, position, insideType, user.Player);
            WorldObjectDebugUtil.LevelTerrain(new Vector2i(0, cellSize.Z), position, borderType, user.Player);
            WorldObjectDebugUtil.LevelTerrain(new Vector2i(cellSize.X, 0), position, borderType, user.Player);
        }

        public static WorldObject SpawnOnFlattenedGround(Type worldObjectType, User user, Vector3i pos)
        {
            var obj = WorldObjectManager.ForceAdd(worldObjectType, user, pos, Quaternion.Identity, false);
            foreach (var groundPos in obj.GroundBelow())
                World.SetBlock(typeof(GrassBlock), groundPos);
            return obj;
        }

        private static void AddTagItemRelation(string tag, string item)
        {
            tag = SplitName(tag);

            if (!tagItemDic.ContainsKey(tag)) tagItemDic.Add(tag, new Dictionary<string, string>());
            if (tagItemDic[tag].ContainsKey(item)) return;
            tagItemDic[tag].Add(item, item);
        }

        private static string GetItemTags(Item allItem)
        {
            StringBuilder tags = new StringBuilder();
            tags.Append('{');
            foreach (Tag tag in allItem.Tags())
            {
                tags.Append($"'{SplitName(tag.DisplayName)}'");
                if (tag != allItem.Tags().Last()) tags.Append(", ");
                AddTagItemRelation(tag.DisplayName, allItem.DisplayName);
            }
            tags.Append('}');
            return tags.ToString();
        }

        private static string GetProducedFluids(WorldObject obj)
        {
            // Checks the object for the three liquid components and returns the private fields of those components to the dictionary.
            // first create a list item and rate strings to attach                          
            List<string> producedFluids = new List<string>();
            var lp = obj.GetComponent<LiquidProducerComponent>();
            if (lp != null)
            {
                Type producesType = (Type)GetFieldValue(lp, "producesType");
                float productionRate = (float)GetFieldValue(lp, "constantProductionRate");
                producedFluids.Add("{'[[" + SplitName(RemoveItemTag(producesType.Name) + "]]', '" + productionRate + "'}"));
            }

            var lconv = obj.GetComponent<LiquidConverterComponent>();
            if (lconv != null)
            {
                LiquidProducerComponent convLP = (LiquidProducerComponent)GetFieldValue(lconv, "producer");
                Type producesType = (Type)GetFieldValue(convLP, "producesType");
                float productionRate = (float)GetFieldValue(convLP, "constantProductionRate");
                producedFluids.Add("{'[[" + SplitName(RemoveItemTag(producesType.Name) + "]]', '" + productionRate + "'}"));
            }

            if (producedFluids.Count <= 0) return "nil";

            string produced = "";
            foreach (string str in producedFluids)
            {
                produced += str == producedFluids.First() ? "{" + Localizer.DoStr(str) : Localizer.DoStr(str);
                produced += str != producedFluids.Last() ? "," : "}";
            }

            return produced;
        }

        private static string GetConsumedFluids(WorldObject obj)
        {
            // Checks the objectfor the three liquid components and returns the private fields of those components to the dictionary.
            // first create a list item and rate strings to attach                          
            List<string> consumedFluids = new List<string>();
            var lc = obj.GetComponent<LiquidConsumerComponent>();
            if (lc != null)
            {
                Type acceptedType = lc.AcceptedType;
                float consumptionRate = (float)GetFieldValue(lc, "constantConsumptionRate");
                consumedFluids.Add("{'[[" + SplitName(RemoveItemTag(acceptedType.Name) + "]]', '" + consumptionRate + "'}"));
            }

            var lconv = obj.GetComponent<LiquidConverterComponent>();
            if (lconv != null)
            {
                LiquidConsumerComponent convLC = (LiquidConsumerComponent)GetFieldValue(lconv, "consumer");
                Type acceptedType = convLC.AcceptedType;
                float consumptionRate = (float)GetFieldValue(convLC, "constantConsumptionRate");
                consumedFluids.Add("{'[[" + SplitName(RemoveItemTag(acceptedType.Name) + "]]', '" + consumptionRate + "'}"));
            }

            if (consumedFluids.Count <= 0) return "nil";

            string used = "";
            foreach (string str in consumedFluids)
            {
                used += str == consumedFluids.First() ? "{" + Localizer.DoStr(str) : Localizer.DoStr(str);
                used += str != consumedFluids.Last() ? "," : "}";
            }

            return used;
        }

        private static string GetFuelsUsed(WorldObject obj)
        {
            if (obj.HasComponent<FuelSupplyComponent>())
            {
                var fuelComponent = obj.GetComponent<FuelSupplyComponent>();
                var fuelTags = GetFieldValue(fuelComponent, "fuelTags") as string[];
                string fuelsString = "";

                foreach (string t in fuelTags)
                {
                    fuelsString += t != fuelTags.Last() ? $"[[{Localizer.DoStr(t)}]]," : $"[[{Localizer.DoStr(t)}]]";
                }

                return fuelsString;
            }

            return "nil";
        }
    }
}