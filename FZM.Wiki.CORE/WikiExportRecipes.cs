using Eco.Gameplay.Components;
using Eco.Gameplay.DynamicValues;
using Eco.Gameplay.Items;
using Eco.Gameplay.Objects;
using Eco.Gameplay.Players;
using Eco.Gameplay.Skills;
using Eco.Gameplay.Systems.Chat;
using Eco.Shared;
using Eco.Shared.Localization;
using Eco.Shared.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

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
        private static SortedDictionary<string, Dictionary<string, string>> EveryRecipe = new SortedDictionary<string, Dictionary<string, string>>();

        private static SortedDictionary<string, StringBuilder> recipeBuilder = new SortedDictionary<string, StringBuilder>();
        private static SortedDictionary<string, SortedDictionary<string, string>> itemRecipeDic = new SortedDictionary<string, SortedDictionary<string, string>>();
        private static SortedDictionary<string, SortedDictionary<string, string>> tableRecipeDic = new SortedDictionary<string, SortedDictionary<string, string>>();
        private static SortedDictionary<string, SortedDictionary<string, string>> groupRecipeDic = new SortedDictionary<string, SortedDictionary<string, string>>();
        // Extracting Recipes is complex and requires collection and sorting.

        [ChatCommand("Creates a dump file of all discovered recipes", ChatAuthorizationLevel.Admin)]
        public static void RecipesDetails(User user)
        {
            // dictionary of recipe properties
            Dictionary<string, string> recipeDetails = new Dictionary<string, string>()
            {
                // Legacy? Maybe OBSOLETE?
                { "dispCraftStn", "'1'" },
                { "checkImage", "'1'"},
                
                // Info
                { "craftStn", "nil"},
                { "skillNeeds", "nil"},
                { "moduleNeeds", "nil"},
                { "baseCraftTime", "nil"},

                // Variants
                { "defaultVariant", "nil"},
                { "numberOfVariants", "nil"},
                { "variants", "nil"},

            };

            Dictionary<string, string> variantDetails = new Dictionary<string, string>()
            {
                { "ingredients", "nil" },
                { "products", "nil" }
            };

            // collect all the recipes
            var famalies = RecipeFamily.AllRecipes;

            foreach (RecipeFamily family in famalies)
            {
                string familyName = family.RecipeName;
                if (!EveryRecipe.ContainsKey(family.RecipeName))
                {
                    EveryRecipe.Add(familyName, new Dictionary<string, string>(recipeDetails));

                    // Crafting Stations.
                    StringBuilder tables = new StringBuilder();
                    tables.Append("{");             
                    foreach (Type type in CraftingComponent.TablesForRecipe(family.GetType()))
                    {
                        string str = WorldObject.UILink(type, false);
                        int startIndex1 = str.IndexOf("</");
                        int startIndex2 = str.LastIndexOf(">", startIndex1) + 1;
                        string table = str.Substring(startIndex2, startIndex1 - startIndex2);
                        tables.Append("'" + table + "'");
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
                        skillNeeds.Append("{'" + req.SkillItem.DisplayName + "','" + req.Level.ToString() + "'}");
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

                    // Base craft time.
                    EveryRecipe[familyName]["baseCraftTime"] = "'" + family.CraftMinutes.GetBaseValue.ToString() + "'";

                    // Default Recipe
                    EveryRecipe[familyName]["defaultVariant"] = "'" + family.DefaultRecipe.DisplayName + "'";

                    EveryRecipe[familyName]["numberOfVariants"] = "'" + family.Recipes.Count + "'";

                    SortedDictionary<string, Dictionary<string,string>> variant = new SortedDictionary<string, Dictionary<string,string>>();
                    foreach (Recipe r in family.Recipes)
                    {
                        var recipe = r.DisplayName;
                        if (!variant.ContainsKey(recipe))
                        {
                            variant.Add(recipe, new Dictionary<string, string>(variantDetails));

                            // Ingredients required
                            StringBuilder ingredients = new StringBuilder();
                            ingredients.Append("{");
                            foreach (var e in r.Ingredients)
                            {
                                ingredients.Append("{");
                                string element;
                                if (e.IsSpecificItem)
                                {
                                    ingredients.Append("'ITEM', ");
                                    element = e.Item.DisplayName;
                                }
                                else
                                {
                                    ingredients.Append("'TAG', ");
                                    element = e.Tag.DisplayName;
                                }

                                ingredients.Append("'" + element + "', '" + e.Quantity.GetBaseValue + "'}");

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
                                products.Append("'" + e.Item.DisplayName + "', " + e.Quantity.GetBaseValue + "'}");

                                if (e != r.Items.Last())
                                    products.Append(", ");

                                if (e != r.Items.Last())
                                    products.Append(", ");
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
                        builder.Append(string.Format("{0}{1}}}", space, space2));

                        if (key != variant.Keys.Last())
                            builder.AppendLine(",");
                    }
                    EveryRecipe[familyName]["variants"] = builder.ToString();
                }                   
            }

            // writes to WikiItems.txt to the Eco Server directory.
            string path = AppDomain.CurrentDomain.BaseDirectory + "Wiki_Module_CraftingRecipes.txt";
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
                    streamWriter.WriteLine(string.Format("{0}{1}}},", space2, space3));
                }
                /*
                streamWriter.WriteLine("    },\n    items = {");
                foreach (string key1 in itemRecipeDic.Keys)
                {
                    streamWriter.Write(string.Format("{0}['{1}'] = {{ ", space2, key1));
                    foreach (string key2 in itemRecipeDic[key1].Keys)
                        streamWriter.Write(string.Format("'{0}', ", key2));
                    streamWriter.WriteLine("},");
                }
                streamWriter.WriteLine("    },\n    tables = {");
                foreach (string key1 in tableRecipeDic.Keys)
                {
                    streamWriter.Write(string.Format("{0}['{1}'] = {{ ", space2, key1));
                    foreach (string key2 in tableRecipeDic[key1].Keys)
                        streamWriter.Write(string.Format("'{0}', ", key2));
                    streamWriter.WriteLine("},");
                }
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
                user.Player.Msg(Localizer.Do($"Dumped to {AppDomain.CurrentDomain.BaseDirectory} Wiki_Module_CraftingRecipes.txt"));
            }
        }

        private static void AddItemRecipeRelation(string item, string recipe)
        {
            if (!itemRecipeDic.ContainsKey(item))
                itemRecipeDic.Add(item, new SortedDictionary<string, string>());
            if (itemRecipeDic[item].ContainsKey(recipe))
                return;
            itemRecipeDic[item].Add(recipe, recipe);
        }

        private static void AddTableRecipeRelation(string table, string recipe)
        {
            if (!tableRecipeDic.ContainsKey(table))
                tableRecipeDic.Add(table, new SortedDictionary<string, string>());
            if (tableRecipeDic[table].ContainsKey(recipe))
                return;
            tableRecipeDic[table].Add(recipe, recipe);
        }

        private static void AddGroupRecipeRelation(string group, string recipe)
        {
            if (!groupRecipeDic.ContainsKey(group))
                groupRecipeDic.Add(group, new SortedDictionary<string, string>());
            if (groupRecipeDic[group].ContainsKey(recipe))
                return;
            groupRecipeDic[group].Add(recipe, recipe);
        }

        private static string WriteTime(float floatminutes)
        {
            StringBuilder stringBuilder = new StringBuilder("");
            int num1 = (int)Math.Floor(floatminutes) / 60;
            int num2 = (int)Math.Floor(floatminutes) % 60;
            int num3 = (int)Math.Floor((floatminutes - Math.Floor(floatminutes)) * 60.0);
            if (num1 > 0)
            {
                stringBuilder.Append(string.Format("{0} hour{1}", num1, num1 == 1 ? "" : "s"));
                if (num2 > 0)
                    stringBuilder.Append(string.Format(" <br> {0} minute{1}", num2, num2 == 1 ? "" : "s"));
                if (num3 > 0)
                    stringBuilder.Append(string.Format(" <br> {0} second{1}", num3, num3 == 1 ? "" : "s"));
            }
            else if (num2 > 0)
            {
                stringBuilder.Append(string.Format("{0} minute{1}", num2, num2 == 1 ? "" : "s"));
                if (num3 > 0)
                    stringBuilder.Append(string.Format(" <br> {0} second{1}", num3, num3 == 1 ? "" : "s"));
            }
            else if (num3 > 0)
                stringBuilder.Append(string.Format("{0} second{1}", num3, num3 == 1 ? "" : "s"));
            return stringBuilder.ToString();
        }

        private static float GetIDynamicValue(IDynamicValue value, User user)
        {
            if (value is MultiDynamicValue)
            {               
                return (value as MultiDynamicValue).GetCurrentValue(user);
            }
                
            if (value is SkillModifiedValue)
                return (value as SkillModifiedValue).Values[0];
            if (value is ConstantValue)
                return (value as ConstantValue).GetBaseValue;
            return 0.0f;
        }

        private static string WriteAffectedBy(IDynamicValue value)
        {
            if (value is SkillModifiedValue)
                return (value as SkillModifiedValue).Skill.DisplayName;
            return "";
        }

        
    }
}
