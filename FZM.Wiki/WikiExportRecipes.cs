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
            Dictionary<string, string> recipeDetails = new Dictionary<string, string>() { };

            // collect all the recipes
            IEnumerable<Recipe> recipes = ReflectionUtils.DerivedTypes(typeof(WorldObject), null, false).SelectMany(type => CraftingComponent.RecipesOnWorldObject(type)).Distinct();

            foreach (Recipe recipe in recipes)
            {
                string recipeName = recipe.RecipeName;
                if (!EveryRecipe.ContainsKey(recipe.RecipeName))
                {
                    EveryRecipe.Add(recipeName, new Dictionary<string, string>(recipeDetails));

                    // these are left here from old write, not sure if they are used in the wiki modules
                    EveryRecipe[recipeName].Add("dispCraftStn", "1");
                    EveryRecipe[recipeName].Add("checkImage", "1");

                    StringBuilder tables = new StringBuilder();
                    tables.Append("{");
                    int tableCount = 0;
                    //return the crafting stations
                    foreach (Type type in CraftingComponent.TablesForRecipe(recipe.GetType()))
                    {
                        string str = WorldObject.UILink(type, false);
                        int startIndex1 = str.IndexOf("</");
                        int startIndex2 = str.LastIndexOf(">", startIndex1) + 1;
                        string table = str.Substring(startIndex2, startIndex1 - startIndex2);
                        tables.Append("'" + table + "'");
                        AddTableRecipeRelation(table, recipeName);

                        tableCount++;

                        if (tableCount != CraftingComponent.TablesForRecipe(recipe.GetType()).Count())
                            tables.Append(",");
                    }
                    tables.Append("}");
                    EveryRecipe[recipeName].Add("craftStn", tables.ToString());


                    // return each product from the recipe and add them to the relations
                    int itemCount = 0;
                    StringBuilder products = new StringBuilder();
                    products.Append("{");
                    foreach (CraftingElement e in recipe.Products)
                    {
                        products.Append("{'" + e.Item.DisplayName + "','" + e.Quantity.GetBaseValue + "'}");
                        AddItemRecipeRelation(e.Item.DisplayName, recipeName);
                        AddGroupRecipeRelation(e.Item.Group, recipeName);
                        itemCount++;
                        if (itemCount != recipe.Products.Length)
                            products.Append(",");
                    }
                    products.Append("}");
                    EveryRecipe[recipeName].Add("products", products.ToString());

                    // return the required skills for the recipe
                    int skillNeedCount = 1;
                    StringBuilder skillNeeds = new StringBuilder();
                    skillNeeds.Append("{");
                    foreach (RequiresSkillAttribute req in recipe.RequiredSkills)
                    {
                        skillNeeds.Append("{'" + req.SkillItem.DisplayName + "','" + req.Level.ToString() + "'}");
                        skillNeedCount++;
                        if (skillNeedCount != recipe.RequiredSkills.Count())
                            products.Append(",");
                    }
                    skillNeeds.Append("}");
                    EveryRecipe[recipeName].Add("skillNeeds", skillNeeds.ToString());

                    // return each ingredient from the recipe and add them to the relations
                    int materCount = 0;
                    StringBuilder ingredients = new StringBuilder();
                    ingredients.Append("{");
                    string consistancyCheck = null;
                    bool multipleAffectedSkills = false;
                    foreach (CraftingElement e in recipe.Ingredients)
                    {
                        if (consistancyCheck == null)
                            if (e.Quantity is SkillModifiedValue)
                                consistancyCheck = (e.Quantity as SkillModifiedValue).Skill.DisplayName;

                        if (e.Quantity is SkillModifiedValue)
                            if (consistancyCheck != (e.Quantity as SkillModifiedValue).Skill.DisplayName)
                                multipleAffectedSkills = true;

                        ingredients.Append("{'" + e.Item.DisplayName + "','" + e.Quantity.GetBaseValue + "'}");
                        AddItemRecipeRelation(e.Item.DisplayName, recipeName);
                        materCount++;
                        if (materCount != recipe.Ingredients.Length)
                            ingredients.Append(",");

                    }
                    ingredients.Append("}");
                    EveryRecipe[recipeName].Add("ingredients", ingredients.ToString());

                    EveryRecipe[recipeName].Add("ctime", "'" + GetIDynamicValue(recipe.CraftMinutes, user) + "'");

                    if (multipleAffectedSkills)
                    {
                        int track = 0;
                        StringBuilder sb = new StringBuilder();
                        sb.Append("{");
                        foreach (CraftingElement e in recipe.Ingredients)
                        {
                            if (!(e.Quantity is SkillModifiedValue))
                                continue;

                            sb.Append("'" + ((SkillModifiedValue)e.Quantity).Skill.DisplayName + "'");

                            track++;

                            if (track != recipe.Ingredients.Length)
                            {
                                sb.Append(",");
                            }
                        }
                        sb.Append("}");

                        EveryRecipe[recipeName].Add("efficiencySkills", sb.ToString());
                    }
                    else
                    {
                        if (recipe.Ingredients.Any(x => x.Quantity is SkillModifiedValue))
                            EveryRecipe[recipeName].Add("efficiencySkills", "{'" + (recipe.Ingredients.First(x => x.Quantity is SkillModifiedValue).Quantity as SkillModifiedValue).Skill.DisplayName + "'}");
                        else
                            EveryRecipe[recipeName].Add("efficiencySkills", "{'nil'}");
                    }

                    if (recipe.CraftMinutes is SkillModifiedValue)
                        EveryRecipe[recipeName].Add("speedSkills", "'" + ((SkillModifiedValue)recipe.CraftMinutes).Skill.DisplayName + "'");
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
                streamWriter.Write("    },\n}");
                streamWriter.Close();
                user.Player.SendTemporaryMessage(Localizer.Do($"Dumped to {AppDomain.CurrentDomain.BaseDirectory} Wiki_Module_CraftingRecipes.txt"));
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
                return (value as ConstantValue).Value;
            return 0.0f;
        }

        private static string WriteAffectedBy(IDynamicValue value)
        {
            if (value is SkillModifiedValue)
                return (value as SkillModifiedValue).Skill.DisplayName;
            return "";
        }

        private static string CleanTags(string hasTags)
        {
            string str;
            StringBuilder stringBuilder1;
            for (str = hasTags; str.Contains(" <b>"); str = stringBuilder1.ToString())
            {
                stringBuilder1 = new StringBuilder();
                stringBuilder1.Append(str.Substring(0, str.IndexOf(" <b>") + 1));
                stringBuilder1.Append(str.Substring(str.IndexOf("'>") + 2, str.IndexOf("</") - (str.IndexOf("'>") + 2)));
                if (str.IndexOf("</b>") != str.Length - 4)
                    stringBuilder1.Append(str.Substring(str.IndexOf("</b>") + 4));
            }
            StringBuilder stringBuilder2;
            for (; str.Contains(" <style="); str = stringBuilder2.ToString())
            {
                stringBuilder2 = new StringBuilder();
                stringBuilder2.Append(str.Substring(0, str.IndexOf(" <style=") + 1));
                stringBuilder2.Append(str.Substring(str.IndexOf("\">") + 2, str.IndexOf("</") - (str.IndexOf("\">") + 2)));
                if (str.IndexOf("</style>") != str.Length - 8)
                    stringBuilder2.Append(str.Substring(str.IndexOf("</style>") + 8));
            }
            return str;
        }
    }
}
