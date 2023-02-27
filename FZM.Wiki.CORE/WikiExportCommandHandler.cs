using Eco.Gameplay.Components;
using Eco.Gameplay.Housing.PropertyValues;
using Eco.Gameplay.Housing;
using Eco.Gameplay.Items;
using Eco.Gameplay.Objects;
using Eco.Gameplay.Players;
using Eco.Gameplay.Property;
using Eco.Gameplay.Systems;
using Eco.Gameplay.Systems.Messaging.Chat.Commands;
using Eco.Shared;
using Eco.Gameplay.DynamicValues;
using Eco.Shared.Localization;
using Eco.Shared.Math;
using Eco.Shared.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Eco.Gameplay.Skills;
using Eco.Gameplay.Blocks;
using Eco.Simulation.Types;
using Eco.Simulation;
using Eco.World.Blocks;
using static Eco.Simulation.Types.PlantSpecies;
using Eco.Gameplay.Systems.Messaging.Chat;
using Eco.Shared.IoC;
using System.Diagnostics.Tracing;
using System.Reflection;
using Eco.Gameplay.EcopediaRoot;
using Eco.Core.Controller;
using Eco.Core.Systems;
using Eco.Shared.View;
using Eco.Mods.TechTree;
using Eco.Gameplay;
using Eco.World;
using System.Runtime.CompilerServices;
using Eco.Gameplay.Pipes.LiquidComponents;

namespace FZM.Wiki
{
    internal class WikiExportCommandHandler
    {

        [ChatCommandHandler]
        public class WikiExportCommands
        {

            private static SortedDictionary<string, Dictionary<string, string>> EverySkill = new SortedDictionary<string, Dictionary<string, string>>();
            private static Dictionary<string, (string, Exception)> ErrorSkills = new();
            private static User dummy;
            private static Dictionary<string, (string, Exception)> ErrorItems = new();
            private static SortedDictionary<string, Dictionary<string, string>> EveryAnimal = new SortedDictionary<string, Dictionary<string, string>>();
            private static string space2 = "        ";
            private static string space3 = "            ";
            private static SortedDictionary<string, Dictionary<string, string>> EveryPage = new SortedDictionary<string, Dictionary<string, string>>();
            private static SortedDictionary<string, Dictionary<string, string>> EveryCommand = new SortedDictionary<string, Dictionary<string, string>>();
            private static SortedDictionary<string, Dictionary<string, string>> EveryPlant = new SortedDictionary<string, Dictionary<string, string>>();
            private static SortedDictionary<string, Dictionary<string, string>> EveryRecipe = new SortedDictionary<string, Dictionary<string, string>>();
            private static SortedDictionary<string, Dictionary<string, string>> EveryTalent = new SortedDictionary<string, Dictionary<string, string>>();
            private static SortedDictionary<string, Dictionary<string, string>> EveryItem = new();
            private static SortedDictionary<string, Dictionary<string, string>> EveryTree = new SortedDictionary<string, Dictionary<string, string>>();
            private static SortedDictionary<string, SortedDictionary<string, string>> RecipeIngedientVariantDic = new SortedDictionary<string, SortedDictionary<string, string>>();
            private static SortedDictionary<string, SortedDictionary<string, string>> RecipeProductVariantDic = new SortedDictionary<string, SortedDictionary<string, string>>();
            private static SortedDictionary<string, SortedDictionary<string, string>> tableRecipeFamilyDic = new SortedDictionary<string, SortedDictionary<string, string>>();
            static Vector3i spawnPoint = new Vector3i(0, 75, 0);
            static Vector3i cellSize = new Vector3i(10, 10, 10);

            public static bool IsInstanceOfGenericType(Type genericType, object instance)
            {
                Type type = instance.GetType();
                while (type != null)
                {
                    if (type.IsGenericType &&
                        type.GetGenericTypeDefinition() == genericType)
                    {
                        return true;
                    }
                    type = type.BaseType;
                }
                return false;
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

            private static void AddRecipeIngredientRelation(string item, string recipeVariant)
            {
                if (!RecipeIngedientVariantDic.ContainsKey(item))
                    RecipeIngedientVariantDic.Add(item, new SortedDictionary<string, string>());
                if (RecipeIngedientVariantDic[item].ContainsKey(recipeVariant))
                    return;
                RecipeIngedientVariantDic[item].Add(recipeVariant, recipeVariant);
            }

            public static object GetFieldValue(object obj, string field)
            {
                return obj.GetType().GetField(field, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance).GetValue(obj);
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

            private static string GetTalentString(WorldObject obj)
            {
                var cc = obj.GetComponent<CraftingComponent>();
                string talentString = "{";
                foreach (var talent in TalentManager.AllTalents.Where(x => x.TalentType == typeof(CraftingTalent) && x.Base))
                {
                    talentString += "'[[" + Localizer.DoStr(SplitName(talent.GetType().Name)) + "]]'";
                    if (talent != TalentManager.AllTalents.Where(x => x.TalentType == typeof(CraftingTalent) && x.Base).Last())
                        talentString += ", ";
                }
                talentString += "}";

                return talentString;
            }

            public static object GetPropertyValue(object obj, string property)
            {
                return obj.GetType().GetProperty(property, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance).GetValue(obj);
            }

            public static string JSONStringSafe(string s)
            {
                string[] NameSplit = Regex.Split(s, @"(?=['?])");
                var sb = new StringBuilder();
                foreach (string str in NameSplit)
                {
                    sb.Append(str);
                    if (str != NameSplit.Last())
                        sb.Append("\\");
                }

                return sb.ToString();
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

            private static void PrepGround(User user, Vector3i position)
            {
                var insideType = BlockManager.FromTypeName("DirtRoadBlock");
                var borderType = BlockManager.FromTypeName("StoneRoadBlock");

                WorldObjectDebugUtil.LevelTerrain(cellSize.XZ, position, insideType, user.Player);
                WorldObjectDebugUtil.LevelTerrain(new Vector2i(0, cellSize.Z), position, borderType, user.Player);
                WorldObjectDebugUtil.LevelTerrain(new Vector2i(cellSize.X, 0), position, borderType, user.Player);
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

            public static WorldObject SpawnOnFlattenedGround(Type worldObjectType, User user, Vector3i pos)
            {
                var obj = WorldObjectManager.ForceAdd(worldObjectType, user, pos, Quaternion.Identity, false);
                foreach (var groundPos in obj.GroundBelow())
                    World.SetBlock(typeof(GrassBlock), groundPos);
                return obj;
            }

            private static void AddTableRecipeRelation(string table, string recipeFamily)
            {
                if (!tableRecipeFamilyDic.ContainsKey(table))
                    tableRecipeFamilyDic.Add(table, new SortedDictionary<string, string>());
                if (tableRecipeFamilyDic[table].ContainsKey(recipeFamily))
                    return;
                tableRecipeFamilyDic[table].Add(recipeFamily, recipeFamily);
            }

            private static void AddRecipeProductRelation(string item, string recipeVariant)
            {
                if (!RecipeProductVariantDic.ContainsKey(item))
                    RecipeProductVariantDic.Add(item, new SortedDictionary<string, string>());
                if (RecipeProductVariantDic[item].ContainsKey(recipeVariant))
                    return;
                RecipeProductVariantDic[item].Add(recipeVariant, recipeVariant);
            }

            public static string WriteDictionaryAsSubObject(Dictionary<string, string> dict, int depth)
            {
                string spaces = space2 + space3;

                for (int i = 0; i < depth; i++)
                {
                    spaces += space2;
                }

                StringBuilder sb = new StringBuilder();
                sb.AppendLine(" {");
                foreach (KeyValuePair<string, string> kvp in dict)
                {
                    sb.AppendLine(spaces + "['" + kvp.Key + "'] = {" + kvp.Value + "},");
                }
                sb.Append(spaces + "}");

                return sb.ToString();
            }

            private static string CleanTags(string hasTags)
            {
                Regex regex = new Regex("<[^>]*>");
                return regex.Replace(hasTags, "");
            }

            public static string SplitName(string name)
            {
                string[] NameSplit = { };

                NameSplit = (name.Contains("Lv") || name.Contains("CO2"))
                    ? Regex.Split(name, @"(?<!(^|[A-Z]))(?=[A-Z])|(?<!^)(?=[A-Z][a-z])")
                    : Regex.Split(name, @"(?<!(^|[A-Z]))(?=[A-Z])|(?<!^)(?=[A-Z][a-z]|(?<!^|[0-9])(?=[0-9]))");

                int count = 0;
                var sb = new StringBuilder();
                foreach (string str in NameSplit)
                {
                    sb.Append(str);
                    count++;
                    if (count != NameSplit.Length)
                        sb.Append(" ");
                }

                Regex regex = new Regex("[ ]{2,}");

                return regex.Replace(sb.ToString(), " ");
            }

            public static string RemoveItemTag(string item)
            {
                string cleanItem = item.Substring(0, item.Length - 4);
                return cleanItem;
            }

            public static string LogExceptionAndNotify(User user, Exception e, string dump)
            {
                Log.WriteErrorLine(Localizer.DoStr(e.Message));
                return $"{dump},  no dump generated!";
            }

            private static void AddToErrorLog(ref Dictionary<string, (string, Exception)> log, string key, string prop, Exception ex)
            {
                log.Add(key, (prop, ex));
            }

            public static void WriteDictionaryToFile(string filename, string type, SortedDictionary<string, Dictionary<string, string>> dictionary, bool final = true)
            {
                var lang = LocalizationPlugin.Config.Language;

                // writes to the Eco Server directory.
                if (!Directory.Exists(@"FZM\DataExports" + $@"{lang}\"))
                    Directory.CreateDirectory(@"FZM\DataExports" + $@"{lang}\");

                string path = @"FZM\DataExports" + $@"{lang}\" + filename;

                using (StreamWriter streamWriter = new StreamWriter(path, false))
                {
                    streamWriter.WriteLine("-- Eco Version : " + EcoVersion.Version);
                    streamWriter.WriteLine("-- Export Language: " + lang);
                    streamWriter.WriteLine();
                    streamWriter.WriteLine("return {\n    " + type + " = {");
                    foreach (string key in dictionary.Keys)
                    {
                        streamWriter.WriteLine(string.Format("{0}['{1}'] = {{", space2, key));
                        foreach (KeyValuePair<string, string> keyValuePair in dictionary[key])
                            streamWriter.WriteLine(string.Format("{0}{1}['{2}'] = {3},", space2, space3, keyValuePair.Key, keyValuePair.Value));
                        streamWriter.WriteLine(string.Format("{0}}},", space2));
                    }
                    streamWriter.Write("    },");
                    if (final)
                        streamWriter.Write("\n}");
                    streamWriter.Close();
                }
            }

            public static void WriteErrorLogToFile(string filename, string type, Dictionary<string, (string, Exception)> errors)
            {
                // writes to the Eco Server directory.
                if (!Directory.Exists(@"FZM\DataExports" + $@"Errors\"))
                    Directory.CreateDirectory(@"FZM\DataExports" + $@"Errors\");

                string path = @"FZM\DataExports" + $@"Errors\" + filename;

                using (StreamWriter streamWriter = new StreamWriter(path, false))
                {
                    streamWriter.WriteLine("-- Eco Version : " + EcoVersion.Version);
                    streamWriter.WriteLine("-- Export Date: " + DateTime.Today);
                    streamWriter.WriteLine();
                    streamWriter.WriteLine($"Errors for {type}");
                    foreach (KeyValuePair<string, (string, Exception)> kvp in errors)
                    {
                        streamWriter.WriteLine($"{kvp.Key} failed at property {kvp.Value.Item1}. Error: {kvp.Value.Item2.Message}");
                    }
                    streamWriter.Close();
                }
            }


            [ChatCommand("Lists All Commands", "we", ChatAuthorizationLevel.Admin)]
            public static void WikiExport(User user)
            {

            }

            [ChatSubCommand("WikiExport", "Discovers all in game items", "discoverall", ChatAuthorizationLevel.Admin)]
            public static void discoverall(User user)
            {
                IEnumerable<Type> types = ((IEnumerable<Item>)Item.AllItems).Select<Item, Type>(item => item.Type);
                DiscoveryManager.Obj.DiscoveredThings.UnionWith(types);
                DiscoveryManager.Obj.UpdateDiscoveredItems();

            }

            [ChatSubCommand("WikiExport", "Export All Data", "dumpdetails", ChatAuthorizationLevel.Admin)]
            public static void dumpdetails(User user)
            {

                User choice;

                if (user == null)
                {
                    UserManager.RequireAuthentication = false;
                    dummy = UserManager.GetOrCreateUser("1234", "1234", "Dummy");
                    UserManager.RequireAuthentication = true;
                    choice = dummy;
                }
                else
                {
                    choice = user;
                }

                StringBuilder alert = new StringBuilder();

                alert.AppendLine("Errors: ");

                try { DiscoverAll(); } catch (Exception e) { alert.AppendLine(LogExceptionAndNotify(user, e, "Discover All")); }
                try { EcoDetails(); } catch (Exception e) { alert.AppendLine(LogExceptionAndNotify(user, e, "Eco Details")); }
                try { ItemDetails(choice); } catch (Exception e) { alert.AppendLine(LogExceptionAndNotify(user, e, "Item Details")); }
                try { RecipesDetails(); } catch (Exception e) { alert.AppendLine(LogExceptionAndNotify(user, e, "Recipe Details")); }
                try { SkillsDetails(); } catch (Exception e) { alert.AppendLine(LogExceptionAndNotify(user, e, "Skills Details")); }
                try { TalentDetails(); } catch (Exception e) { alert.AppendLine(LogExceptionAndNotify(user, e, "Talent Details")); }
                try { PlantDetails(); } catch (Exception e) { alert.AppendLine(LogExceptionAndNotify(user, e, "Plant Details")); }
                try { TreeDetails(); } catch (Exception e) { alert.AppendLine(LogExceptionAndNotify(user, e, "Tree Details")); }
                try { AnimalDetails(); } catch (Exception e) { alert.AppendLine(LogExceptionAndNotify(user, e, "Animal Details")); }
                try { CommandDetails(); } catch (Exception e) { alert.AppendLine(LogExceptionAndNotify(user, e, "Command Details")); }
                try { EcopediaDetails(); } catch (Exception e) { alert.AppendLine(LogExceptionAndNotify(user, e, "Ecopedia Details")); }

                ProcessStartInfo info = new ProcessStartInfo
                {
                    Arguments = @"FZM\DataExports",
                    FileName = "explorer.exe"
                };

                Process.Start(info);

                alert.AppendLine("");
                alert.AppendLine("INFO: ");
                alert.AppendLine("Dump folder is open, alt-tab to check dumps.");
                alert.AppendLine("Check logs for error details.");
                alert.AppendLine("");
                alert.AppendLine("DUMP FOLDER: ");
                alert.AppendLine(@"FZM\DataExports");

                if (choice == user)
                    user.Player.InfoBoxLocStr(alert.ToString());

            }

            public static void EcopediaDetails()
            {
                // dictionary of page details
                Dictionary<string, string> entry = new Dictionary<string, string>()
            {
                { "displayName", "nil" },
                { "displayNameUntranslated", "nil" },
                { "summary", "nil" },
                { "subpages", "nil" },
                { "associatedTypes", "nil" },
                { "sectionsText", "nil" },
            };

                foreach (var cat in Ecopedia.Obj.Categories.Values)
                {
                    foreach (var page in cat.Pages)
                    {
                        EcopediaPage p = page.Value;
                        string pageName = p.DisplayName;
                        if (!EveryPage.ContainsKey(p.DisplayName))
                        {
                            EveryPage.Add(pageName, new Dictionary<string, string>(entry));

                            StringBuilder sb = new StringBuilder();
                            if (p.Sections != null)
                            {
                                foreach (var sec in p.Sections)
                                {
                                    if (sec is EcopediaBanner || sec is EcopediaButton)
                                        continue;

                                    sb.Append($"{{'{sec.GetType().Name}', '{Regex.Replace(JSONStringSafe(CleanTags(sec.Text)), "[\n\r]+", "\\n\\n")}'}}");

                                    if (sec != p.Sections.Last())
                                        sb.Append(", ");
                                }
                                EveryPage[pageName]["sectionsText"] = $"{{{sb}}}";
                            }

                            if (p.SubPages != null)
                            {

                                sb = new StringBuilder();
                                foreach (var sp in p.SubPages)
                                {
                                    sb.Append($"'{Localizer.DoStr(sp.Key)}'");

                                    if (sp.Key != p.SubPages.Last().Key)
                                        sb.Append(", ");
                                }
                                EveryPage[pageName]["subpages"] = $"{{{sb}}}";
                            }

                            EveryPage[pageName]["displayName"] = $"'{p.DisplayName}'";
                            EveryPage[pageName]["displayNameUntranslated"] = p.DisplayName.NotTranslated != null ? $"'{p.DisplayName.NotTranslated}'" : $"nil";

                            if (p.Summary != null && p.Summary != "")
                            {
                                var sum = p.Summary.Trim().TrimEnd('\r', '\n').Trim();
                                EveryPage[pageName]["summary"] = $"'{sum}'";
                            }

                            var types = p.TypesForThisPage?.ToList();
                            if (types != null && types?.Count > 0)
                            {
                                sb = new StringBuilder();
                                foreach (var type in types)
                                {
                                    if (sb.Length > 0)
                                    {
                                        sb.Append(", ");
                                    }
                                    sb.Append($"'{Localizer.DoStr(type.Name)}'");
                                }
                                EveryPage[pageName]["associatedTypes"] = $"{{{sb}}}";
                            }
                        }
                    }
                }

                // writes to WikiItems.txt to the Eco Server directory.
                WriteDictionaryToFile("Wiki_Module_Ecopedia.txt", "ecopedia", EveryPage);
            }

            public static void CommandDetails()
            {
                // dictionary of commands
                Dictionary<string, string> commandDetails = new Dictionary<string, string>()
            {
                { "command", "nil" },
                { "parent", "nil" },
                { "helpText", "nil" },
                { "shortCut", "nil" },
                { "level", "nil" },
                { "parameters", "nil" }
            };

                Regex regex = new Regex("\t\n\v\f\r");

                //var chatServer = ChatServer.Obj;
                //var chatManager = GetFieldValue(chatServer, "netChatManager");
                ChatManager chatManager = ServiceHolder<IChatManager>.Obj as ChatManager;
                ChatCommandService chatCommandService = (ChatCommandService)GetFieldValue(chatManager, "chatCommandService");

                IEnumerable<ChatCommand> commands = chatCommandService.GetAllCommands();

                foreach (var com in commands)
                {
                    if (com.Key == "dumpdetails")
                        continue;

                    var command = $"/{Localizer.DoStr(com.ParentKey)}{(Localizer.DoStr(com.ParentKey) == "" ? Localizer.DoStr(com.Name) : " " + Localizer.DoStr(com.Name))}";
                    if (!EveryCommand.ContainsKey(command))
                    {
                        EveryCommand.Add(command, new Dictionary<string, string>(commandDetails));
                        EveryCommand[command]["command"] = "'" + Localizer.DoStr(com.Key) + "'";

                        if (com.ParentKey != null && com.ParentKey != "")
                            EveryCommand[command]["parent"] = "'" + Localizer.DoStr(com.ParentKey) + "'";

                        EveryCommand[command]["helpText"] = "'" + Localizer.DoStr(JSONStringSafe(com.HelpText)) + "'";
                        EveryCommand[command]["shortCut"] = "'" + Localizer.DoStr(com.ShortCut) + "'";
                        EveryCommand[command]["level"] = "'" + Localizer.DoStr(com.AuthLevel.ToString()) + "'";


                        MethodInfo method = com.Method;
                        if (method == null)
                            continue;

                        ParameterInfo[] parameters = method.GetParameters();

                        if (parameters == null)
                            continue;

                        Dictionary<string, string> pars = new Dictionary<string, string>();

                        foreach (var p in parameters)
                        {
                            if (p.Name == "user")
                                continue;

                            string pos = "Arg" + p.Position.ToString();
                            pars[pos] = "'" + p.Name + "', '" + p.ParameterType.Name + "'";

                            if (p.HasDefaultValue)
                                pars[pos] += ", '" + p.DefaultValue + "'";
                        }
                        EveryCommand[command]["parameters"] = WriteDictionaryAsSubObject(pars, 1);
                    }
                }

                // writes to WikiItems.txt to the Eco Server directory.
                WriteDictionaryToFile("Wiki_Module_CommandData.txt", "commands", EveryCommand);
            }

            public static void AnimalDetails()
            {
                // dictionary of animal properties
                Dictionary<string, string> animalDetails = new Dictionary<string, string>()
            {
                { "untranslated", "nil" },

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

                            EveryAnimal[animalName]["untranslated"] = $"'{animal.DisplayName.NotTranslated}'";

                            #region LIFETIME
                            EveryAnimal[animalName]["maturity"] = "'" + animal.MaturityAgeDays.ToString("F1") + "'";
                            #endregion

                            #region MOVEMENT
                            EveryAnimal[animalName]["isSwimming"] = animal.Swimming ? $"'{Localizer.DoStr("Swimming")}'" : "nil"; // Does the animal swin.
                            EveryAnimal[animalName]["isFlying"] = animal.Flying ? $"'{Localizer.DoStr("Flying")}'" : "nil"; // Does the animal fly.
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

                            EveryAnimal[animalName]["flees"] = animal.FleePlayers ? $"'{Localizer.DoStr("Flees")}'" : "nil"; // Will this animal flee players / predators.
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
                                    string foodName = "";
                                    foreach (string str in foodNameSplit)
                                    {
                                        foodName += str;
                                        count++;
                                        if (count != foodNameSplit.Length)
                                            foodName += " ";
                                    }
                                    if (LocalizationPlugin.Config.Language == SupportedLanguage.English)
                                        sb.Append(Localizer.DoStr(foodName));
                                    else
                                        sb.Append(Localizer.DoStr(meal.Name));
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
                                string itemName = "";
                                foreach (string str in itemNameSplit)
                                {

                                    itemName += str;
                                    count++;
                                    if (count != itemNameSplit.Length)
                                        itemName += " ";
                                    else
                                        sb.Append(Localizer.DoStr(itemName));
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
                WriteDictionaryToFile("Wiki_Module_AnimalData.txt", "animals", EveryAnimal);
            }

            public static void TreeDetails()
            {
                // dictionary of plant properties
                Dictionary<string, string> treeDetails = new Dictionary<string, string>()
            {
                // INFO
                { "untranslated", "nil" },
                { "isDecorative", "nil" },
                { "doesSpread", "nil" },

                // LIFETIME
                { "maturity", "nil" },
                { "treeHealth", "nil" }, // The health of the tree for chopping.
                { "logHealth", "nil" }, // The health of the log for chopping.

                // GENERATION
                { "isWater", "nil" },
                { "height", "nil" },

                // FOOD
                { "calorieValue", "nil" },

                // RESOURCES
                { "requireHarvestable", "nil" },
                { "pickableAtPercent", "nil" },
                { "experiencePerHarvest", "nil" },
                { "harvestTool", "nil" },
                { "killOnHarvest", "nil" },
                { "postHarvestGrowth", "nil" },
                { "scytheKills", "nil" },
                { "resourceItem", "nil" },
                { "resourceMin", "nil" },
                { "resourceMax", "nil" },
                { "debrisSpawnChance", "nil" }, // Chance to spawn debris.
                { "debrisType", "nil" }, // The debris created when chopping this tree. BlockType.
                { "debrisResources", "nil" }, // The resources returned for chopping the debris.
                { "trunkResources", "nil" }, // The resources returned for chopping the trunk.

                // WORLD LAYERS
                { "carbonRelease", "nil" },
                { "idealGrowthRate", "nil" },
                { "idealDeathRate", "nil" },
                { "spreadRate", "nil" },
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
                { "consumedUnderwaterFertileGround", "nil" },
                { "consumedShrubSpace", "nil" },
                { "extremeTempMin", "nil" },
                { "idealTempMin", "nil" },
                { "idealTempMax", "nil" },
                { "extremeTempMax", "nil" },
                { "extremeMoistureMin", "nil" },
                { "idealMoistureMin", "nil" },
                { "idealMoistureMax", "nil" },
                { "extremeMoistureMax", "nil" },
                { "extremeSaltMin", "nil" },
                { "idealSaltMin", "nil" },
                { "idealSaltMax", "nil" },
                { "extremeSaltMax", "nil" },
                { "maxPollutionDensity", "nil" },
                { "pollutionTolerance", "nil" }
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
                            EveryTree[treeName]["untranslated"] = $"'{tree.DisplayName.NotTranslated}'";
                            EveryTree[treeName]["isDecorative"] = tree.Decorative ? $"'{Localizer.DoStr("Decorative")}'" : "nil";
                            EveryTree[treeName]["doesSpread"] = tree.NoSpread ? $"'{Localizer.DoStr("No")}'" : $"'{Localizer.DoStr("Yes")}'";
                            #endregion

                            #region LIFETIME
                            EveryTree[treeName]["maturity"] = "'" + tree.MaturityAgeDays.ToString("F1") + "'";
                            EveryTree[treeName]["treeHealth"] = "'" + tree.TreeHealth.ToString("F1") + "'";
                            EveryTree[treeName]["logHealth"] = "'" + tree.LogHealth.ToString("F1") + "'";
                            #endregion

                            #region GENERATION
                            EveryTree[treeName]["isWater"] = tree.Water ? $"'{Localizer.DoStr("Underwater")}'" : "nil";
                            EveryTree[treeName]["height"] = "'" + tree.Height.ToString("F1") + "'";
                            #endregion

                            #region FOOD
                            EveryTree[treeName]["calorieValue"] = "'" + tree.CalorieValue.ToString("F1") + "'";
                            #endregion

                            #region RESOURCES
                            EveryTree[treeName]["requireHarvestable"] = tree.RequireHarvestable ? $"'{Localizer.DoStr("Yes")}'" : "nil";
                            EveryTree[treeName]["pickableAtPercent"] = "'" + (tree.PickableAtPercent * 100).ToString("F0") + "'";
                            EveryTree[treeName]["experiencePerHarvest"] = "'" + (tree.ExperiencePerHarvest).ToString("F1") + "'";

                            if (tree.PostHarvestingGrowth == 0)
                                EveryTree[treeName]["killOnHarvest"] = $"'{Localizer.DoStr("Yes")}'";
                            else
                                EveryTree[treeName]["killOnHarvest"] = $"'{Localizer.DoStr("No")}'";

                            if (tree.PostHarvestingGrowth != 0)
                                EveryTree[treeName]["postHarvestGrowth"] = "'" + (tree.PostHarvestingGrowth * 100).ToString("F0") + "'";

                            EveryTree[treeName]["scytheKills"] = tree.ScythingKills ? $"'{Localizer.DoStr("Yes")}'" : "nil";

                            if (tree.ResourceItemType != null) { EveryTree[treeName]["resourceItem"] = "'[[" + Localizer.DoStr(SplitName(RemoveItemTag(tree.ResourceItemType.Name))) + "]]'"; }

                            EveryTree[treeName]["resourceMin"] = "'" + tree.ResourceRange.Min.ToString("F1") + "'";
                            EveryTree[treeName]["resourceMax"] = "'" + tree.ResourceRange.Max.ToString("F1") + "'";
                            EveryTree[treeName]["resourceBonus"] = "'" + (tree.ResourceBonusAtGrowth * 100).ToString("F0") + "'";

                            // Debris
                            EveryTree[treeName]["debrisSpawnChance"] = "'" + (tree.ChanceToSpawnDebris * 100).ToString("F0") + "'";
                            EveryTree[treeName]["debrisType"] = "'" + Localizer.DoStr(SplitName(RemoveItemTag(tree.DebrisType.Name))) + "'";

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
                            EveryTree[treeName]["debrisResources"] = "{" + debrisResources + "}";

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
                            EveryTree[treeName]["trunkResources"] = "{" + trunkResources + "}";

                            #endregion

                            #region VISUALS

                            #endregion

                            #region WORLDLAYERS
                            EveryTree[treeName]["carbonRelease"] = "'" + tree.ReleasesCO2TonsPerDay.ToString("F4") + "'";

                            EveryTree[treeName]["idealGrowthRate"] = "'" + tree.MaxGrowthRate.ToString("F4") + "'";

                            EveryTree[treeName]["idealDeathRate"] = "'" + tree.MaxDeathRate.ToString("F4") + "'";

                            EveryTree[treeName]["spreadRate"] = "'" + tree.SpreadRate.ToString("F4") + "'";

                            // The resource constraints that slow growth rate.
                            #region Resource Constraints
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
                            #endregion

                            // The capacity constraints which slow growth.
                            #region Capacity Constraints
                            if (tree.CapacityConstraints != null)
                            {
                                foreach (CapacityConstraint c in tree.CapacityConstraints)
                                {
                                    if (c.CapacityLayerName == "FertileGorund")
                                        EveryTree[treeName]["consumedFertileGround"] = "'" + (c.ConsumedCapacityPerPop).ToString("F1") + "'";
                                    if (c.CapacityLayerName == "CanopySpace")
                                        EveryTree[treeName]["consumedCanopySpace"] = "'" + (c.ConsumedCapacityPerPop).ToString("F1") + "'";
                                    if (c.CapacityLayerName == "UnderwaterFertileGorund")
                                        EveryTree[treeName]["consumedUnderwaterFertileGround"] = "'" + (c.ConsumedCapacityPerPop).ToString("F1") + "'";
                                    if (c.CapacityLayerName == "ShrubSpace")
                                        EveryTree[treeName]["consumedShrubSpace"] = "'" + (c.ConsumedCapacityPerPop).ToString("F1") + "'";
                                }
                            }
                            #endregion

                            // The environmental ranges this plant can tolerate.
                            #region Environment Ranges

                            // Temperature
                            EveryTree[treeName]["extremeTempMin"] = "'" + tree.TemperatureExtremes.Min.ToString("F1") + "'";
                            EveryTree[treeName]["idealTempMin"] = "'" + tree.IdealTemperatureRange.Min.ToString("F1") + "'";
                            EveryTree[treeName]["idealTempMax"] = "'" + tree.IdealTemperatureRange.Max.ToString("F1") + "'";
                            EveryTree[treeName]["extremeTempMax"] = "'" + tree.TemperatureExtremes.Max.ToString("F1") + "'";

                            // Moisture
                            EveryTree[treeName]["extremeMoistureMin"] = "'" + tree.MoistureExtremes.Min.ToString("F1") + "'";
                            EveryTree[treeName]["idealMoistureMin"] = "'" + tree.IdealMoistureRange.Min.ToString("F1") + "'";
                            EveryTree[treeName]["idealMoistureMax"] = "'" + tree.IdealMoistureRange.Max.ToString("F1") + "'";
                            EveryTree[treeName]["extremeMoistureMax"] = "'" + tree.MoistureExtremes.Max.ToString("F1") + "'";

                            // Salt Content
                            EveryTree[treeName]["extremeSaltMin"] = "'" + tree.WaterExtremes.Min.ToString("F1") + "'";
                            EveryTree[treeName]["idealSaltMin"] = "'" + tree.IdealWaterRange.Min.ToString("F1") + "'";
                            EveryTree[treeName]["idealSaltMax"] = "'" + tree.IdealWaterRange.Max.ToString("F1") + "'";
                            EveryTree[treeName]["extremeSaltMax"] = "'" + tree.WaterExtremes.Max.ToString("F1") + "'";

                            #endregion

                            EveryTree[treeName]["maxPollutionDensity"] = "'" + tree.MaxPollutionDensity.ToString("F4") + "'";
                            EveryTree[treeName]["pollutionTolerance"] = "'" + tree.PollutionDensityTolerance.ToString("F4") + "'";

                            #endregion

                            #region UNCATEGORISED
                            #endregion

                            #region OBSOLETE
                            #endregion
                        }
                    }
                }
                WriteDictionaryToFile("Wiki_Module_TreeData.txt", "trees", EveryTree);
            }

            public static void PlantDetails()
            {
                // dictionary of plant properties
                Dictionary<string, string> plantDetails = new Dictionary<string, string>()
            {             
                // INFO
                { "untranslated","nil" },
                { "isDecorative", "nil" }, // Is the plant considered decorative. Not simulated after spawn.
                { "doesSpread", "nil" }, // The plant will spawn others like it nearby given enough time not dying and not harvested

                // LIFETIME
                { "maturity", "nil" }, // Age for full maturity and reproduction.

                // GENERATION
                { "isWater", "nil" }, // Does the species live underwater.
                { "height", "nil" }, // Plant height in meters.

                // FOOD
                { "calorieValue", "nil" }, // The base calories this species provides to it's consumers.

                // RESOURCES
                { "requireHarvestable", "nil" }, // Does this plant require to have reached a harvestable stage before you can harvest it, you will get no resources for this if its not at a harvestable stage. 
                { "pickableAtPercent", "nil" }, // This plant will be pickable at this percent and you will get some resources.
                { "experiencePerHarvest", "nil" }, // Base experience you get per harvest.
                { "harvestTool", "nil" }, // The tool required to harvest this plant, nil means hands.
                { "killOnHarvest", "nil" }, // Does the plant die on harvest.
                { "postHarvestGrowth", "nil" }, // What % growth does the plant return to after harvest.
                { "scytheKills", "nil" }, // Will using a Scythe/Sickle on this plant kill it.
                { "resourceItem", "nil" }, // The item you get from harvesting this plant.
                { "resourceMin", "nil" }, // The minimum number of items returned.
                { "resourceMax", "nil" }, // The maximum number of items returned.
                { "resourceBonus", "nil" }, // The bonus items returned for allowing it to grow.

                // WORLD LAYERS
                { "carbonRelease", "nil" }, // The amount of carbon dioxide released by this species. (Plants & Trees are negative values)
                { "idealGrowthRate", "nil" }, // In ideal conditions, what is the rate of growth. (%)
                { "idealDeathRate", "nil" }, // In ideal conditions what is the rate of death. (%)
                { "spreadRate", "nil" }, // In ideal conditions what is the rate of spread, if it does spread.
                { "nitrogenHalfSpeed", "nil" }, // At what nitrogen value will the growth speed reduce to half.
                { "nitrogenContent", "nil" }, // What nitrogen content is ideal.
                { "phosphorusHalfSpeed", "nil" }, // At what phosphorus value will the growth speed reduce to half.
                { "phosphorusContent", "nil" }, // What phosphorus content is ideal.
                { "potassiumHalfSpeed", "nil" }, // At what potassium value will the growth speed reduce to half.
                { "potassiumContent", "nil" }, // What potassium content is ideal.
                { "soilMoistureHalfSpeed", "nil" }, // At what moisture value will the growth speed reduce to half.
                { "soilMoistureContent", "nil" }, // What moisture content is ideal.
                { "consumedFertileGround", "nil" }, // How much of the area deemed Fertile Ground does this plant take up, this is almost always more than the in game physical space.
                { "consumedCanopySpace", "nil" }, // How much of the area deemed Canopy Space does this plant take up, this is almost always more than the in game physical space.
                { "consumedUnderwaterFertileGround", "nil" }, // How much of the area deemed Underwater Fertile Ground does this plant take up, this is almost always more than the in game physical space.
                { "consumedShrubSpace", "nil" }, // How much of the area deemed Shrub Space does this plant take up, this is almost always more than the in game physical space.
                { "extremeTempMin", "nil" }, // The lowest temperature before this plant stops growth.
                { "idealTempMin", "nil" }, // The lowest temperature of the ideal growth range (max growth).
                { "idealTempMax", "nil" }, // The highest temperature of the ideal growth range (max growth).
                { "extremeTempMax", "nil" }, // The highest temperature before this plant stops growth.
                { "extremeMoistureMin", "nil" }, // The lowest moisture content before this plant stops growth.
                { "idealMoistureMin", "nil" }, // The lowest moisture content of the ideal growth range (max growth).
                { "idealMoistureMax", "nil" }, // The highest moisture content of the ideal growth range (max growth).
                { "extremeMoistureMax", "nil" },// The highest moisture content before this plant stops growth.
                { "extremeSaltMin", "nil" }, // The lowest salt content before this plant stops growth.
                { "idealSaltMin", "nil" }, // The lowest salt contente of the ideal growth range (max growth).
                { "idealSaltMax", "nil" }, // The highest salt content of the ideal growth range (max growth).
                { "extremeSaltMax", "nil" }, // The highest Sslt content before this plant stops growth.
                { "maxPollutionDensity", "nil" }, // The highest pollution density before this plant stops growing.
                { "pollutionTolerance", "nil" } // The pollution density at which this plant slows growth, spread and carbon dioxide absorbtion.
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
                            EveryPlant[plantName]["untranslated"] = $"'{plant.DisplayName.NotTranslated}'";
                            EveryPlant[plantName]["isDecorative"] = plant.Decorative ? $"'{Localizer.DoStr("Decorative")}'" : "nil";
                            EveryPlant[plantName]["doesSpread"] = plant.NoSpread ? $"'{Localizer.DoStr("No")}'" : $"'{Localizer.DoStr("Yes")}'";
                            #endregion

                            #region LIFETIME

                            EveryPlant[plantName]["maturity"] = "'" + plant.MaturityAgeDays.ToString("F1") + "'";
                            #endregion

                            #region GENERATION
                            EveryPlant[plantName]["isWater"] = plant.Water ? $"'{Localizer.DoStr("Underwater")}'" : "nil";
                            EveryPlant[plantName]["height"] = "'" + plant.Height.ToString("F1") + "'";
                            #endregion

                            #region FOOD
                            EveryPlant[plantName]["calorieValue"] = "'" + plant.CalorieValue.ToString("F1") + "'";
                            #endregion

                            #region RESOURCES                       
                            EveryPlant[plantName]["requireHarvestable"] = plant.RequireHarvestable ? $"'{Localizer.DoStr("Yes")}'" : "nil";

                            EveryPlant[plantName]["pickableAtPercent"] = "'" + (plant.PickableAtPercent * 100).ToString("F0") + "'";

                            EveryPlant[plantName]["experiencePerHarvest"] = "'" + (plant.ExperiencePerHarvest).ToString("F1") + "'";

                            if (Block.Is<Reapable>(plant.BlockType))
                                EveryPlant[plantName]["harvestTool"] = $"'{Localizer.DoStr("Scythe")}'";
                            else if (Block.Is<Diggable>(plant.BlockType))
                                EveryPlant[plantName]["harvestTool"] = $"'{Localizer.DoStr("Shovel")}'";

                            if (plant.PostHarvestingGrowth == 0)
                                EveryPlant[plantName]["killOnHarvest"] = $"'{Localizer.DoStr("Yes")}'";
                            else
                                EveryPlant[plantName]["killOnHarvest"] = $"'{Localizer.DoStr("No")}'";

                            if (plant.PostHarvestingGrowth != 0)
                                EveryPlant[plantName]["postHarvestGrowth"] = "'" + (plant.PostHarvestingGrowth * 100).ToString("F0") + "'";

                            EveryPlant[plantName]["scytheKills"] = plant.ScythingKills ? $"'{Localizer.DoStr("Yes")}'" : "nil";

                            if (plant.ResourceItemType != null) { EveryPlant[plantName]["resourceItem"] = "'[[" + Localizer.DoStr(SplitName(RemoveItemTag(plant.ResourceItemType.Name))) + "]]'"; }

                            EveryPlant[plantName]["resourceMin"] = "'" + plant.ResourceRange.Min.ToString("F1") + "'";
                            EveryPlant[plantName]["resourceMax"] = "'" + plant.ResourceRange.Max.ToString("F1") + "'";
                            EveryPlant[plantName]["resourceBonus"] = "'" + (plant.ResourceBonusAtGrowth * 100).ToString("F0") + "'";

                            #endregion

                            #region VISUALS

                            #endregion

                            #region WORLDLAYERS
                            EveryPlant[plantName]["carbonRelease"] = "'" + plant.ReleasesCO2TonsPerDay.ToString("F4") + "'";

                            EveryPlant[plantName]["idealGrowthRate"] = "'" + plant.MaxGrowthRate.ToString("F4") + "'";

                            EveryPlant[plantName]["idealDeathRate"] = "'" + plant.MaxDeathRate.ToString("F4") + "'";

                            EveryPlant[plantName]["spreadRate"] = "'" + plant.SpreadRate.ToString("F4") + "'";

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
                                        EveryPlant[plantName]["consumedUnderwaterFertileGround"] = "'" + (c.ConsumedCapacityPerPop).ToString("F1") + "'";
                                    if (c.CapacityLayerName == "ShrubSpace")
                                        EveryPlant[plantName]["consumedShrubSpace"] = "'" + (c.ConsumedCapacityPerPop).ToString("F1") + "'";
                                }
                            }
                            #endregion

                            #region Environment Ranges

                            // Temperature
                            EveryPlant[plantName]["extremeTempMin"] = "'" + plant.TemperatureExtremes.Min.ToString("F1") + "'";
                            EveryPlant[plantName]["idealTempMin"] = "'" + plant.IdealTemperatureRange.Min.ToString("F1") + "'";
                            EveryPlant[plantName]["idealTempMax"] = "'" + plant.IdealTemperatureRange.Max.ToString("F1") + "'";
                            EveryPlant[plantName]["extremeTempMax"] = "'" + plant.TemperatureExtremes.Max.ToString("F1") + "'";

                            // Moisture
                            EveryPlant[plantName]["extremeMoistureMin"] = "'" + plant.MoistureExtremes.Min.ToString("F1") + "'";
                            EveryPlant[plantName]["idealMoistureMin"] = "'" + plant.IdealMoistureRange.Min.ToString("F1") + "'";
                            EveryPlant[plantName]["idealMoistureMax"] = "'" + plant.IdealMoistureRange.Max.ToString("F1") + "'";
                            EveryPlant[plantName]["extremeMoistureMax"] = "'" + plant.MoistureExtremes.Max.ToString("F1") + "'";

                            // Salt Content
                            EveryPlant[plantName]["extremeSaltMin"] = "'" + plant.WaterExtremes.Min.ToString("F1") + "'";
                            EveryPlant[plantName]["idealSaltMin"] = "'" + plant.IdealWaterRange.Min.ToString("F1") + "'";
                            EveryPlant[plantName]["idealSaltMax"] = "'" + plant.IdealWaterRange.Max.ToString("F1") + "'";
                            EveryPlant[plantName]["extremeSaltMax"] = "'" + plant.WaterExtremes.Max.ToString("F1") + "'";

                            #endregion

                            EveryPlant[plantName]["maxPollutionDensity"] = "'" + plant.MaxPollutionDensity.ToString("F4") + "'";
                            EveryPlant[plantName]["pollutionTolerance"] = "'" + plant.PollutionDensityTolerance.ToString("F4") + "'";

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
                WriteDictionaryToFile("Wiki_Module_PlantData.txt", "plants", EveryPlant);

            }

            public static void TalentDetails()
            {
                Dictionary<string, string> TalentDetails = new Dictionary<string, string>()
            {
                { "untranslated", "nil" },

                { "category", "nil" },
                { "group", "nil" },
                { "name" , "nil" },
                { "description", "nil" },
                { "talentType", "nil" },
                { "owningSkill", "nil" },
                { "activeLevel", "nil" },

            };

                foreach (Talent talent in TalentManager.AllTalents)
                {
                    TalentGroup talentGroup;
                    if (talent.TalentGroupType != null)
                    {
                        talentGroup = Item.Get(TalentManager.TypeToTalent[talent.GetType()].TalentGroupType) as TalentGroup;
                        string displayName = talentGroup.DisplayName.ToString();
                        if (!EveryTalent.ContainsKey(displayName))
                        {
                            EveryTalent.Add(displayName, new Dictionary<string, string>(TalentDetails));
                            EveryTalent[displayName]["untranslated"] = $"'{talentGroup.DisplayName.NotTranslated}'";
                            EveryTalent[displayName]["category"] = $"'{Localizer.DoStr(talentGroup.Category)}'";
                            EveryTalent[displayName]["group"] = $"'{Localizer.DoStr(talentGroup.Group)}'";
                            EveryTalent[displayName]["name"] = $"'{talentGroup.DisplayName}'";
                            EveryTalent[displayName]["description"] = $"'{talentGroup.DisplayDescription}'";

                            EveryTalent[displayName]["talentType"] = $"'{Localizer.DoStr(SplitName(talent.TalentType.Name))}'";

                            //Connected Skill and Level Unlock
                            EveryTalent[displayName]["owningSkill"] = $"'{Localizer.DoStr(SplitName(talentGroup.OwningSkill.Name))}'";
                            EveryTalent[displayName]["activeLevel"] = $"'{talentGroup.Level}'";
                        }
                    }
                }

                WriteDictionaryToFile("Wiki_Module_TalentData.txt", "talents", EveryTalent);
            }
            public static void SkillsDetails()
            {
                // dictionary of item properties
                Dictionary<string, string> skillDetails = new Dictionary<string, string>()
            {
                // INFO 
                { "untranslated", "nil" },
                { "title", "nil" },
                { "description", "nil" },
                { "skillID", "nil" },
                { "skillIDNum", "nil" },
                { "maxLevel", "nil" },
                { "root", "nil" },
                { "rootSkill", "nil" },
                { "specialty", "nil" },
                { "specialtySkill", "nil" },
                { "prerequisites", "nil" },
                { "childSkills", "nil" },

                // RESEARCH
                { "specialtySkillBook", "nil" },
                { "specialtySkillScroll", "nil" },
                { "itemsGiven", "nil" },

                // TALENTS BY LEVEL
                { "talents", "nil" },

                // BENEFITS BY LEVEL
                { "benefits", "nil" },

                // UNLOCKS BY LEVEL
                { "recipes", "nil" }

            };

                FieldInfo skillUnlocksField = typeof(Skill).GetField("skillUnlocksTooltips", BindingFlags.Static | BindingFlags.NonPublic);
                var skillUnlocks = skillUnlocksField.GetValue(typeof(Skill)) as Dictionary<Type, Dictionary<int, List<LocString>>>;

                foreach (Skill skill in Item.AllItems.OfType<Skill>())
                {
                    string displayName = skill.DisplayName;
                    string prop = "";
                    try
                    {
                        if (!EverySkill.ContainsKey(skill.DisplayName))
                        {
                            var skillD = new Dictionary<string, string>(skillDetails);
                            //INFO
                            prop = "untranslated"; skillD[prop] = $"'{skill.DisplayName.NotTranslated}'";
                            prop = "title"; if (skill.Title != "") skillD[prop] = $"'{Localizer.DoStr(skill.Title)}'"; // The title conferred by this skill.

                            Regex regex = new Regex("[\t\n\v\f\r]");
                            prop = "description"; skillD[prop] = $"'{regex.Replace(JSONStringSafe(skill.DisplayDescription), " ")}'"; // The description of the skill.
                            prop = "skillID"; skillD[prop] = $"'{Localizer.DoStr(skill.Type.Name)}'"; // For linking purposes in the wiki.
                            prop = "skillIDNum"; skillD[prop] = $"'{skill.TypeID}'"; // For linking purposes in the wiki.
                            prop = "maxLevel"; skillD[prop] = $"'{skill.MaxLevel}'"; // The maximum level of this skill
                            prop = "root"; skillD[prop] = skill.IsRoot.ToString().ToLower(); // If the skill is a ROOT Skill, these are overarching categories that house the actual learnable skills.                 
                            prop = "rootSkill"; if (!skill.IsRoot) skillD[prop] = $"'[[{skill.RootSkillTree.StaticSkill}]]'"; // What is the skills root skill if this is not one.
                            prop = "specialty"; skillD[prop] = skill.IsSpecialty.ToString().ToLower(); // Skills under ROOT skills, these are learnable.
                            prop = "specialtySkill"; if (!skill.IsSpecialty) skillD[prop] = $"'[[{skill.SpecialtySkillTree.StaticSkill}]]'"; // This property is likely OBSOLETE, but currently still exists in the SLG code. May be useful to modders.
                            prop = "prerequisites"; if (!skill.IsRoot && !skill.IsSpecialty) skillD[prop] = $"'{skill.SkillTree.Parent.StaticSkill}'"; // This property is likely OBSOLETE, but currently still exists in the SLG code. May be useful to modders.

                            // Check if the skill has child skills (common with ROOT skills) and create a string to list them out.
                            prop = "childSkills";
                            if (skill.SkillTree.ProfessionChildren != null && skill.SkillTree.ProfessionChildren.Count() != 0)
                            {
                                int track = 0;
                                StringBuilder sb = new StringBuilder();
                                foreach (SkillTree child in skill.SkillTree.ProfessionChildren)
                                {
                                    Skill childSkill = child.StaticSkill;
                                    sb.Append(string.Format("'[[{0}]]'", childSkill));
                                    track++;
                                    if (track != skill.SkillTree.ProfessionChildren.Count())
                                    {
                                        sb.Append(",");
                                    }
                                }
                                skillD[prop] = "{" + sb.ToString() + "}";
                            }

                            //RESEARCH                   
                            // check the skill is a speciality (can be learned) and has a SkillBook associated with it.
                            if (skill.IsSpecialty && (Item.GetSkillbookForSkillType(skill.Type) is SkillBook skillBook))
                            {
                                prop = "specialtySkillBook"; skillD[prop] = $"'[[{skillBook.DisplayName}]]'";
                                prop = "specialtySkillScroll"; skillD[prop] = $"'[[{Item.Get(skillBook.SkillScrollType).DisplayName}]]'";
                            }

                            /* OBSOLETE??
                            // Check if the skill has items given and create a string to list them out.
                            if (skill.ItemTypesGiven != null && skill.ItemTypesGiven.Count() != 0)
                            {
                                int track = 0;
                                StringBuilder sb = new StringBuilder();
                                foreach (Type type in skill.ItemTypesGiven)
                                {
                                    string t = Localizer.DoStr(RemoveItemTag(type.Name));
                                    sb.Append(string.Format("[[{0}]]", t));
                                    track++;
                                    if (track != skill.ItemTypesGiven.Count())
                                    {
                                        sb.Append(", ");
                                    }
                                }

                                EverySkill[friendlyName]["itemsGiven"]; skillD[prop] = "'" + sb.ToString() + "'";
                            }
                            */

                            // TALENTS
                            // sub dictionary for data building
                            Dictionary<string, string> levelTalents = new Dictionary<string, string>();
                            prop = "talents";
                            if (skill.Talents != null)
                            {
                                skill.Talents.ForEach(group =>
                                {
                                    string key = "level" + group.Level.ToString();

                                    // We really only want the levels the talents are at (3 and 6 currently but this should protect if that changes)
                                    if (group.TalentStrings.Count() == 1)
                                    {
                                        if (!levelTalents.ContainsKey(key))
                                        {
                                            levelTalents.Add(key, "'[[" + Localizer.DoStr(SplitName(group.TalentStrings.FirstOrDefault())) + "]]'");
                                        }
                                        else
                                        {
                                            levelTalents[key] += ", '[[" + Localizer.DoStr(SplitName(group.TalentStrings.FirstOrDefault())) + "]]'";
                                        }
                                    }

                                    //TODO: Currently there are no TalentGroups that have more than 1 Talents, but this assumedly could change and thus this it will need to be built eventually to accept it.
                                });
                                skillD[prop] = WriteDictionaryAsSubObject(levelTalents, 1);
                            }

                            // BENEFITS BY LEVEL

                            // sub dictionary for data building
                            Dictionary<string, string> levelBenefits = new Dictionary<string, string>()
                        {
                            { "level1", "" },
                            { "level2", "" },
                            { "level3", "" },
                            { "level4", "" },
                            { "level5", "" },
                            { "level6", "" },
                            { "level7", "" }
                        };

                            prop = "benefits";
                            var benefits = SkillModifiedValueManager.GetBenefitsFor(skill);
                            if (benefits != null)
                            {
                                foreach (KeyValuePair<Type, List<SkillModifiedValue>> kvp in benefits)
                                {
                                    LocString locString = SkillModifiedValueManager.GetBenefitNameForType(kvp.Key);
                                    string keyString = locString.ToString().StripTags().Replace(" ", "");
                                    foreach (SkillModifiedValue smv in kvp.Value)
                                    {
                                        for (int i = 1; i <= skill.MaxLevel; i++)
                                        {
                                            string benefitTarget;

                                            // Formatting for the wiki.
                                            if (keyString == "You")
                                            {
                                                benefitTarget = "'You'";
                                            }
                                            else if (keyString.Length > 6 && keyString.Substring(keyString.Length - 6) == "Recipe")
                                            {
                                                benefitTarget = "'[[" + SplitName(keyString.Substring(0, keyString.Length - 6)) + "]]'";
                                            }
                                            else
                                            {
                                                benefitTarget = "'[[" + SplitName(keyString) + "]]'";
                                            }

                                            string addLine = "\n" + space2 + space3 + space2 + space2 + "{" + benefitTarget + ", '" + smv.Verb + "', '" + (string)GetPropertyValue(smv, "BenefitsDescription") + "', '" + smv.ValueAt(i) + "'},";
                                            levelBenefits["level" + i.ToString()] += addLine;

                                            if (benefits.Last().Key == kvp.Key)
                                                levelBenefits["level" + i.ToString()] += "\n" + space2 + space3 + space2 + space2;
                                        }
                                    }
                                }
                            }
                            skillD[prop] = WriteDictionaryAsSubObject(levelBenefits, 1);

                            // UNLOCKS BY LEVEL
                            Dictionary<string, string> levelUnlocks = new Dictionary<string, string>()
                        {
                            { "level0", "" },
                            { "level1", "" },
                            { "level2", "" },
                            { "level3", "" },
                            { "level4", "" },
                            { "level5", "" },
                            { "level6", "" },
                            { "level7", "" }
                        };
                            prop = "recipes";
                            // if we can't get unlocks or there is 0 listed or this is a root skill we move on
                            if (!skillUnlocks.TryGetValue(skill.GetType(), out var unlocks)) continue;
                            if (unlocks.Count() == 0) continue;
                            if (skill.IsRoot) continue;

                            foreach (KeyValuePair<int, List<LocString>> kvp in unlocks)
                            {
                                kvp.Value.Sort();
                                foreach (LocString s in kvp.Value)
                                {
                                    // Formatting for the wiki.
                                    if (s.ToString().StripTags().Length > 6 && s.ToString().StripTags().Substring(s.ToString().StripTags().Length - 6) == "Recipe")
                                    {
                                        levelUnlocks["level" + kvp.Key.ToString()] += "'[[" + s.ToString().StripTags().Substring(0, s.ToString().StripTags().Length - 7) + "]]'";
                                        if (s != kvp.Value.Last())
                                        {
                                            levelUnlocks["level" + kvp.Key.ToString()] += ", ";
                                        }
                                    }
                                }
                            }
                            skillD[prop] = WriteDictionaryAsSubObject(levelUnlocks, 1);
                            EverySkill.Add(displayName, skillD);
                        }
                    }
                    catch (Exception e)
                    {
                        AddToErrorLog(ref ErrorItems, displayName, prop, e);
                    }
                }
                WriteErrorLogToFile("Wiki_Module_Skills_Errors.txt", "skills", ErrorItems);
                WriteDictionaryToFile("Wiki_Module_Skills.txt", "skills", EverySkill);
            }

            public static void RecipesDetails()
            {
                // dictionary of recipe properties
                Dictionary<string, string> recipeDetails = new Dictionary<string, string>()
            {
                // Legacy? Maybe OBSOLETE?
                { "dispCraftStn", "'1'" },
                { "checkImage", "'1'"},

                // Info
                { "untranslated", "nil" },
                { "craftStn", "nil"},
                { "skillNeeds", "nil"},
                { "moduleNeeds", "nil"},
                { "baseCraftTime", "nil"},
                { "baseLaborCost", "nil" },
                { "baseXPGain", "nil" },

                // Variants
                { "defaultVariant", "nil"},
                { "defaultVariantUntranslated","nil" },
                { "numberOfVariants", "nil"},
                { "variants", "nil"},

            };

                Dictionary<string, string> variantDetails = new Dictionary<string, string>()
            {
                { "untranslated", "nil" },
                { "ingredients", "nil" },
                { "products", "nil" }
            };

                // collect all the recipes
                var famalies = RecipeFamily.AllRecipes;

                foreach (RecipeFamily family in famalies)
                {
                    string familyName = Localizer.DoStr(family.RecipeName);
                    string familyNameUntrans = family.RecipeName;
                    if (!EveryRecipe.ContainsKey(familyName))
                    {
                        EveryRecipe.Add(familyName, new Dictionary<string, string>(recipeDetails));

                        EveryRecipe[familyName]["untranslated"] = $"'{familyNameUntrans}'";

                        // Crafting Stations.
                        StringBuilder tables = new StringBuilder();
                        tables.Append("{");
                        foreach (Type type in CraftingComponent.TablesForRecipe(family.GetType()))
                        {
                            WorldObjectItem creatingItem = WorldObjectItem.GetCreatingItemTemplateFromType(type);

                            string table = creatingItem.DisplayName;
                            string untransTable = creatingItem.DisplayName.NotTranslated;
                            tables.Append($"{{'{table}', '{untransTable}'}}");
                            AddTableRecipeRelation(table, familyName);

                            if (type != CraftingComponent.TablesForRecipe(family.GetType()).Last())
                                tables.Append(", ");
                        }
                        tables.Append("}");
                        EveryRecipe[familyName]["craftStn"] = tables.ToString();

                        // Skills required
                        StringBuilder skillNeeds = new StringBuilder();
                        skillNeeds.Append("{");
                        foreach (RequiresSkillAttribute req in family.RequiredSkills)
                        {
                            skillNeeds.Append("{'" + req.SkillItem.DisplayName + "','" + req.Level.ToString() + "','" + req.SkillItem.DisplayName.NotTranslated + "'}");
                            if (req != family.RequiredSkills.Last())
                                skillNeeds.Append(", ");
                        }
                        skillNeeds.Append("}");
                        EveryRecipe[familyName]["skillNeeds"] = skillNeeds.ToString();

                        // Modules Required
                        StringBuilder moduleNeeds = new StringBuilder();
                        moduleNeeds.Append("{");
                        foreach (var module in family.RequiredModules)
                        {
                            moduleNeeds.Append("'" + module.ModuleName + "'");
                            if (module != family.RequiredModules.Last())
                                moduleNeeds.Append(", ");
                        }
                        moduleNeeds.Append("}");
                        EveryRecipe[familyName]["moduleNeeds"] = moduleNeeds.ToString();

                        // Base craft time.
                        EveryRecipe[familyName]["baseCraftTime"] = (family.CraftMinutes != null) ? "'" + family.CraftMinutes.GetBaseValue.ToString() + "'" : "'0'";

                        // Base labor cost
                        EveryRecipe[familyName]["baseLaborCost"] = "'" + family.Labor.ToString() + "'";

                        // Base XP gain
                        EveryRecipe[familyName]["baseXPGain"] = "'" + family.ExperienceOnCraft.ToString() + "'";

                        // Default Recipe
                        EveryRecipe[familyName]["defaultVariant"] = "'" + Localizer.DoStr(SplitName(family.DefaultRecipe.Name)) + "'";
                        EveryRecipe[familyName]["defaultVariantUntranslated"] = "'" + SplitName(family.DefaultRecipe.Name) + "'";

                        EveryRecipe[familyName]["numberOfVariants"] = "'" + family.Recipes.Count + "'";

                        SortedDictionary<string, Dictionary<string, string>> variant = new SortedDictionary<string, Dictionary<string, string>>();
                        foreach (Recipe r in family.Recipes)
                        {
                            var recipe = r.DisplayName;
                            if (!variant.ContainsKey(recipe))
                            {
                                variant.Add(recipe, new Dictionary<string, string>(variantDetails));
                                variant[recipe]["untranslated"] = $"'{r.DisplayName.NotTranslated}'";
                                // Ingredients required
                                StringBuilder ingredients = new StringBuilder();
                                ingredients.Append("{");
                                foreach (var e in r.Ingredients)
                                {
                                    ingredients.Append("{");
                                    LocString element;
                                    if (e.IsSpecificItem)
                                    {
                                        ingredients.Append("'ITEM', ");
                                        element = e.Item.DisplayName;
                                        AddRecipeIngredientRelation(e.Item.DisplayName, r.DisplayName);
                                    }
                                    else
                                    {
                                        ingredients.Append("'TAG', ");
                                        element = Localizer.DoStr(SplitName(e.Tag.DisplayName));
                                    }

                                    bool isStatic = false;

                                    if (e.Quantity is ConstantValue)
                                        isStatic = true;

                                    ingredients.Append("'" + element + "', '" + e.Quantity.GetBaseValue + "', '" + isStatic.ToString() + "', '" + element.NotTranslated + "'}");

                                    if (e != r.Ingredients.Last())
                                        ingredients.Append(", ");
                                }
                                ingredients.Append("}");
                                variant[recipe]["ingredients"] = ingredients.ToString();
                                // Products recieved
                                StringBuilder products = new StringBuilder();
                                products.Append("{");
                                foreach (var e in r.Items)
                                {
                                    products.Append("{");
                                    products.Append("'" + e.Item.DisplayName + "', '" + e.Quantity.GetBaseValue + "', '" + e.Item.DisplayName.NotTranslated + "'}");

                                    if (e != r.Items.Last())
                                        products.Append(", ");
                                    AddRecipeProductRelation(e.Item.DisplayName, r.DisplayName);
                                }
                                products.Append("}");
                                variant[recipe]["products"] = products.ToString();
                            }
                        }
                        StringBuilder builder = new StringBuilder();
                        builder.AppendLine(" {");
                        string space = space2 + space2 + space3;
                        foreach (string key in variant.Keys)
                        {
                            builder.AppendLine(string.Format("{0}['{1}'] = {{", space, key));
                            foreach (KeyValuePair<string, string> keyValuePair in variant[key])
                                builder.AppendLine(string.Format("{0}{1}['{2}'] = {3},", space, space2, keyValuePair.Key, keyValuePair.Value));
                            builder.Append(string.Format("{0}}}", space));

                            //if (key != variant.Keys.Last())
                            builder.AppendLine(",");
                        }
                        builder.Append(space2 + space3 + "}");
                        EveryRecipe[familyName]["variants"] = builder.ToString();
                    }
                }

                var lang = LocalizationPlugin.Config.Language;

                // writes to the Eco Server directory.
                if (!Directory.Exists(@"FZM\DataExports" + $@"{lang}\"))
                    Directory.CreateDirectory(@"FZM\DataExports" + $@"{lang}\");

                // writes to WikiItems.txt to the Eco Server directory.
                string path = @"FZM\DataExports" + $@"{lang}\" + "Wiki_Module_CraftingRecipes.txt";
                using (StreamWriter streamWriter = new StreamWriter(path, false))
                {
                    streamWriter.WriteLine("-- Eco Version : " + EcoVersion.Version);
                    streamWriter.WriteLine();
                    streamWriter.WriteLine("return {\n    recipes = {");
                    foreach (string key in EveryRecipe.Keys)
                    {
                        streamWriter.WriteLine(string.Format("{0}['{1}'] = {{", space2, key));
                        foreach (KeyValuePair<string, string> keyValuePair in EveryRecipe[key])
                            streamWriter.WriteLine(string.Format("{0}{1}['{2}'] = {3},", space2, space3, keyValuePair.Key, keyValuePair.Value));
                        streamWriter.WriteLine(string.Format("{0}}},", space2));
                    }

                    // write the recipe ingredients to recipe variant data
                    streamWriter.WriteLine("    },\n    ingredients = {");
                    foreach (string key1 in RecipeIngedientVariantDic.Keys)
                    {
                        streamWriter.Write(string.Format("{0}['{1}'] = {{ ", space2, key1));
                        foreach (string key2 in RecipeIngedientVariantDic[key1].Keys)
                        {
                            if (key2 != RecipeIngedientVariantDic[key1].Keys.Last())
                                streamWriter.Write(string.Format("'{0}', ", key2));
                            else
                                streamWriter.Write(string.Format("'{0}'", key2));
                        }
                        streamWriter.WriteLine("},");
                    }

                    // write the recipe products to recipe variant data
                    streamWriter.WriteLine("    },\n    products = {");
                    foreach (string key1 in RecipeProductVariantDic.Keys)
                    {
                        streamWriter.Write(string.Format("{0}['{1}'] = {{ ", space2, key1));
                        foreach (string key2 in RecipeProductVariantDic[key1].Keys)
                        {
                            if (key2 != RecipeProductVariantDic[key1].Keys.Last())
                                streamWriter.Write(string.Format("'{0}', ", key2));
                            else
                                streamWriter.Write(string.Format("'{0}'", key2));
                        }
                        streamWriter.WriteLine("},");
                    }

                    // write the table to recipe family data
                    streamWriter.WriteLine("    },\n    tables = {");
                    foreach (string key1 in tableRecipeFamilyDic.Keys)
                    {
                        streamWriter.Write(string.Format("{0}['{1}'] = {{ ", space2, key1));
                        foreach (string key2 in tableRecipeFamilyDic[key1].Keys)
                        {
                            if (key2 != tableRecipeFamilyDic[key1].Keys.Last())
                                streamWriter.Write(string.Format("'{0}', ", key2));
                            else
                                streamWriter.Write(string.Format("'{0}'", key2));
                        }
                        streamWriter.WriteLine("},");
                    }

                    /*
                    // write the group(tag) to recipe data
                    streamWriter.WriteLine("    },\n    groups = {");
                    foreach (string key1 in groupRecipeDic.Keys)
                    {
                        streamWriter.Write(string.Format("{0}['{1}'] = {{ ", space2, key1));
                        foreach (string key2 in groupRecipeDic[key1].Keys)
                            streamWriter.Write(string.Format("'{0}', ", key2));
                        streamWriter.WriteLine("},");
                    }
                    */

                    streamWriter.Write("    },\n}");
                    streamWriter.Close();
                }
            }

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
                { "NutrientElement", "nil" },
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

                            prop = "untranslated"; itemD[prop] = $"'{item.DisplayName.NotTranslated}'";
                            prop = "category"; itemD[prop] = $"'{Localizer.DoStr(item.Category)}'";
                            prop = "group"; itemD[prop] = $"'{Localizer.DoStr(item.Group)}'";
                            prop = "type"; itemD[prop] = $"'{item.Type.ToString().Substring(item.Type.ToString().LastIndexOf('.') + 1)}'";
                            prop = "typeID"; itemD[prop] = $"'{item.TypeID}'";

                            Regex regex = new Regex("[\t\n\v\f\r]");
                            prop = "description"; itemD[prop] = $"'{regex.Replace(CleanTags(item.DisplayDescription), " ").Replace("'", "\\'")}'";
                            prop = "tagGroups"; itemD[prop] = GetItemTags(item);
                            prop = "maxStack"; itemD[prop] = $"'{item.MaxStackSize}'";
                            prop = "carried"; itemD[prop] = item.IsCarried ? $"'{Localizer.DoStr("Hands")}'" : $"'{Localizer.DoStr("Backpack")}'";
                            prop = "currency"; itemD[prop] = item.CanBeCurrency ? $"'{Localizer.DoStr("Yes")}'" : "nil";
                            prop = "weight"; if (item.HasWeight) itemD[prop] = $"'{(decimal)item.Weight / 1000}'";
                            prop = "fuel"; if (item.IsFuel) itemD[prop] = $"'{item.Fuel}'";
                            prop = "yield"; if (item.HasYield) itemD[prop] = $"'[[{item.Yield.Skill.DisplayName}]]'";

                            // if the item is a block then add it's tier
                            prop = "materialTier"; if (item.Group == "Block Items") itemD[prop] = $"'{(int)GetPropertyValue(item, "Tier")}'";

                            #region Food Items
                            // if the item is also a food item get the nutrient values
                            if (item is FoodItem foodItem)
                            {
                                prop = "calories"; itemD[prop] = $"'{foodItem.Calories:F1}'";
                                prop = "carbs"; itemD[prop] = $"'{foodItem.Nutrition.Carbs:F1}'";
                                prop = "protein"; itemD[prop] = $"'{foodItem.Nutrition.Protein:F1}'";
                                prop = "fat"; itemD[prop] = $"'{foodItem.Nutrition.Fat:F1}'";
                                prop = "vitamins"; itemD[prop] = $"'{foodItem.Nutrition.Vitamins:F1}'";
                                prop = "density"; itemD[prop] = float.IsNaN(foodItem.Nutrition.Values().Sum() / foodItem.Calories)
                                ? "'0.0'"
                                : $"'{(foodItem.Nutrition.Values().Sum() / foodItem.Calories) * 100:F1}'";
                            }
                            #endregion

                            #region Fertilizers

                            if (IsInstanceOfGenericType(typeof(FertilizerItem<>), item))
                            {
                                prop = "NutrientElement";
                                var desc = (LocString)item.GetType().GetMethod("FertilizerTooltip").Invoke(item, null);
                                var stringDesc = desc.ToString();

                                Regex r = new Regex("[:\n]");
                                var nutrients = r.Split(stringDesc);

                                string final = "";
                                for (int i = 0; i < nutrients.Length - 1; i++)
                                {
                                    string currentLine = nutrients[i];
                                    string nextLine = nutrients[++i];
                                    if(final.Length > 0)
                                    {
                                        final += ", ";
                                    }

                                    final += $"{{'{currentLine.Replace(":", "")}','{nextLine.Replace("\r", "").Replace("\n", "")}'}}";
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
                                prop = "fluidsUded"; itemD[prop] = $"'{GetConsumedFluids(obj)}'";
                                prop = "fluidsProduced"; itemD[prop] = $"'{GetProducedFluids(obj)}'";
                                prop = "fuelsUsed"; itemD[prop] = $"'{GetFuelsUsed(obj)}'";

                                if (!(obj is PhysicsWorldObject) || obj.DisplayName == "Wooden Elevator")
                                { prop = "footprint"; itemD[prop] = $"'{GetFootprint(obj)}'"; }

                                if (obj.HasComponent<CraftingComponent>())
                                { prop = "validTalents"; itemD[prop] = $"{GetTalentString(obj)}"; }

                                if (obj.HasComponent<PowerGridComponent>())
                                {
                                    var gridComponent = obj.GetComponent<PowerGridComponent>();
                                    prop = "energyProduced"; itemD[prop] = $"'{gridComponent.EnergySupply}'";
                                    prop = "energyUsed"; itemD[prop] = $"'{gridComponent.EnergyDemand}'";
                                    prop = "energyType"; itemD[prop] = $"'{gridComponent.EnergyType.Name}'";
                                    prop = "gridRadius"; itemD[prop] = $"'{gridComponent.Radius}'";
                                }

                                if (obj.HasComponent<HousingComponent>())
                                {
                                    var v = obj.GetComponent<HousingComponent>().HomeValue;
                                    prop = "roomCategory"; itemD[prop] = $"'{Localizer.DoStr(v.Category.ToString())}'";
                                    if (v.Category != RoomCategory.Industrial)
                                    {
                                        prop = "skillValue"; itemD[prop] = $"'{v.HouseValue}'";
                                        prop = "furnitureType"; itemD[prop] = $"'{v.TypeForRoomLimit}'";
                                        prop = "repeatsDepreciation"; itemD[prop] = $"'{v.DiminishingReturnPercent}'";
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
                string path = @"FZM\DataExports" + $@"{lang}\" + filename;
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

            private static SortedDictionary<string, Dictionary<string, string>> tagItemDic = new();

            public static void EcoDetails()
            {
                // dictionary of commands
                Dictionary<string, string> EcoDetails = new Dictionary<string, string>()
            {
                { "ecoVersion", "nil" },
                { "fullInfo", "nil" },
                { "dataExportDate", "nil" },
            };

                Details["eco"] = EcoDetails;

                Details["eco"]["ecoVersion"] = $"'{EcoVersion.Version}'";
                Details["eco"]["fullInfo"] = $"'{EcoVersion.FullInfo.Replace("\r\n", " ")}'";
                Details["eco"]["dataExportDate"] = $"'{DateTime.Now.Date}'";

                WriteDictionaryToFile("Wiki_Module_EcoVersion.txt", "eco", Details);
            }

            public static SortedDictionary<string, Dictionary<string, string>> Details = new SortedDictionary<string, Dictionary<string, string>>();

        private static void DiscoverAll()
            {
                IEnumerable<Type> types = ((IEnumerable<Item>)Item.AllItems).Select<Item, Type>(item => item.Type);
                DiscoveryManager.Obj.DiscoveredThings.UnionWith(types);
                DiscoveryManager.Obj.UpdateDiscoveredItems();
            }
        }




    }
}
