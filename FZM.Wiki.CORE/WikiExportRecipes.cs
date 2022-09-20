using Eco.Gameplay.Components;
using Eco.Gameplay.DynamicValues;
using Eco.Gameplay.Items;
using Eco.Gameplay.Players;
using Eco.Gameplay.Skills;
using Eco.Gameplay.Systems;
using Eco.Gameplay.Systems.Chat;
using Eco.Gameplay.Systems.Messaging.Chat.Commands;
using Eco.Shared;
using Eco.Shared.Localization;
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
    public partial class WikiDetails
    {
        // dictionary of items and their dictionary of stats
        private static SortedDictionary<string, Dictionary<string, string>> EveryRecipe = new SortedDictionary<string, Dictionary<string, string>>();

        private static SortedDictionary<string, StringBuilder> recipeBuilder = new SortedDictionary<string, StringBuilder>();
        private static SortedDictionary<string, SortedDictionary<string, string>> RecipeIngedientVariantDic = new SortedDictionary<string, SortedDictionary<string, string>>();
        private static SortedDictionary<string, SortedDictionary<string, string>> RecipeProductVariantDic = new SortedDictionary<string, SortedDictionary<string, string>>();
        private static SortedDictionary<string, SortedDictionary<string, string>> tableRecipeFamilyDic = new SortedDictionary<string, SortedDictionary<string, string>>();
        private static SortedDictionary<string, SortedDictionary<string, string>> groupRecipeDic = new SortedDictionary<string, SortedDictionary<string, string>>();
        // Extracting Recipes is complex and requires collection and sorting.

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
                        string untransTable= creatingItem.DisplayName.NotTranslated;                      
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
                    EveryRecipe[familyName]["baseCraftTime"] = (family.CraftMinutes != null) ? "'" + family.CraftMinutes.GetBaseValue.ToString() + "'" : "'0'" ;

                    // Base labor cost
                    EveryRecipe[familyName]["baseLaborCost"] = "'" + family.Labor.ToString() + "'";

                    // Base XP gain
                    EveryRecipe[familyName]["baseXPGain"] = "'" + family.ExperienceOnCraft.ToString() + "'";

                    // Default Recipe
                    EveryRecipe[familyName]["defaultVariant"] = "'" + Localizer.DoStr(SplitName(family.DefaultRecipe.Name)) + "'";
                    EveryRecipe[familyName]["defaultVariantUntranslated"] = "'" + SplitName(family.DefaultRecipe.Name) + "'";

                    EveryRecipe[familyName]["numberOfVariants"] = "'" + family.Recipes.Count + "'";

                    SortedDictionary<string, Dictionary<string,string>> variant = new SortedDictionary<string, Dictionary<string,string>>();
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
            if (!Directory.Exists(SaveLocation + $@"{lang}\"))
                Directory.CreateDirectory(SaveLocation + $@"{lang}\");

            // writes to WikiItems.txt to the Eco Server directory.
            string path = SaveLocation + $@"{lang}\" + "Wiki_Module_CraftingRecipes.txt";
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

        private static void AddRecipeIngredientRelation(string item, string recipeVariant)
        {
            if (!RecipeIngedientVariantDic.ContainsKey(item))
                RecipeIngedientVariantDic.Add(item, new SortedDictionary<string, string>());
            if (RecipeIngedientVariantDic[item].ContainsKey(recipeVariant))
                return;
            RecipeIngedientVariantDic[item].Add(recipeVariant, recipeVariant);
        }

        private static void AddRecipeProductRelation(string item, string recipeVariant)
        {
            if (!RecipeProductVariantDic.ContainsKey(item))
                RecipeProductVariantDic.Add(item, new SortedDictionary<string, string>());
            if (RecipeProductVariantDic[item].ContainsKey(recipeVariant))
                return;
            RecipeProductVariantDic[item].Add(recipeVariant, recipeVariant);
        }

        private static void AddTableRecipeRelation(string table, string recipeFamily)
        {
            if (!tableRecipeFamilyDic.ContainsKey(table))
                tableRecipeFamilyDic.Add(table, new SortedDictionary<string, string>());
            if (tableRecipeFamilyDic[table].ContainsKey(recipeFamily))
                return;
            tableRecipeFamilyDic[table].Add(recipeFamily, recipeFamily);
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
