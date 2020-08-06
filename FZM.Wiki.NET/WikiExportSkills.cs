using Eco.Gameplay.DynamicValues;
using Eco.Gameplay.Items;
using Eco.Gameplay.Players;
using Eco.Gameplay.Skills;
using Eco.Gameplay.Systems.Chat;
using Eco.Shared.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
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
        // dictionary of skills and their dictionary of stats
        private static SortedDictionary<string, Dictionary<string, string>> EverySkill = new SortedDictionary<string, Dictionary<string, string>>();

        [ChatCommand("Creates a dump file of all discovered skills", ChatAuthorizationLevel.Admin)]
        public static void SkillsDetails(User user)
        {
            var itemsAndRecipes = typeof(Recipe).DerivedTypes().Concat(typeof(Item).DerivedTypes());
            
            // dictionary of item properties
            Dictionary<string, string> skillDetails = new Dictionary<string, string>();

            foreach (Skill skill in Item.AllItems.OfType<Skill>())
            {
                if (!EverySkill.ContainsKey(skill.DisplayName))
                {
                    string friendlyName = skill.DisplayName;
                    EverySkill.Add(friendlyName, new Dictionary<string, string>(skillDetails));
                    EverySkill[friendlyName].Add("description", "'" + skill.DisplayDescription + "'");
                    EverySkill[friendlyName].Add("root", skill.IsRoot.ToString().ToLower());
                    if (!skill.IsRoot)
                        EverySkill[friendlyName].Add("rootSkill", "'[[" + skill.RootSkillTree.StaticSkill.ToString() + "]]'");
                    EverySkill[friendlyName].Add("specialty", skill.IsSpecialty.ToString().ToLower());
                    if (!skill.IsSpecialty)
                        EverySkill[friendlyName].Add("specialitySkill", "'[[" + skill.SpecialtySkillTree.StaticSkill.ToString() + "]]'");
                    if (skill.IsSpecialty && (Item.GetSkillbookForSkillType(skill.Type) is SkillBook skillBook))
                    {
                        EverySkill[friendlyName].Add("specialitySkillBook", "'[[" + skillBook.DisplayName + "]]'");
                        EverySkill[friendlyName].Add("specialitySkillScroll", "'[[" + Item.Get(skillBook.SkillScrollType).DisplayName + "]]'");
                    }
                    EverySkill[friendlyName].Add("maxLevel", "'" + skill.MaxLevel.ToString() + "'");
                    if (!skill.IsRoot && !skill.IsSpecialty)
                        EverySkill[friendlyName].Add("prerequisites", "'" + skill.SkillTree.Parent.StaticSkill.ToString() + "'");
                    if (skill.SkillTree.Children != null)
                    {
                        int track = 0;
                        if (skill.SkillTree.Children.Count() == 0)
                        {
                            EverySkill[friendlyName].Add("childSkills", "nil");
                        }
                        else
                        {
                            // builds a string to contain each of the child skills
                            StringBuilder sb = new StringBuilder();
                            foreach (SkillTree child in skill.SkillTree.Children)
                            {
                                Skill childSkill = child.StaticSkill;
                                sb.Append(string.Format("[[{0}]]", childSkill));
                                track++;
                                if (track != skill.SkillTree.Children.Count())
                                {
                                    sb.Append(", ");
                                }
                            }
                            EverySkill[friendlyName].Add("childSkills", "'" + sb.ToString() + "'");
                        }
                    }
                    if (skill.ItemTypesGiven != null)
                    {
                        int track = 0;
                        if (skill.ItemTypesGiven.Count() == 0)
                        {
                            EverySkill[friendlyName].Add("itemsGiven", "nil");
                        }
                        else
                        {
                            // builds a string to contain each of the items given
                            StringBuilder sb = new StringBuilder();
                            foreach (Type type in skill.ItemTypesGiven)
                            {
                                string t = type.Name.Substring(0, type.Name.Length - 4);
                                sb.Append(string.Format("[[{0}]]", t));
                                track++;
                                if (track != skill.ItemTypesGiven.Count())
                                {
                                    sb.Append(", ");
                                }
                            }
                            EverySkill[friendlyName].Add("itemsGiven", "'" + sb.ToString() + "'");
                        }
                    }

                    #region SkillPointCosts Removed in 8.0
                    /*
                    // need to get skill point cost directly from field on each skill as it is a static field not overriden by each derived skill class
                    FieldInfo skillPointCost = skill.GetType().GetField("SkillPointCost");
                    if (skillPointCost != null)
                    {
                        int[] spc = skillPointCost.GetValue(skill) as int[];
                        StringBuilder pointCostString = new StringBuilder();
                        pointCostString.Append("{");
                        for (int pc = 0; pc < spc.Length; pc++)
                        {
                            pointCostString.Append("'" + spc[pc].ToString() + "'");

                            if (pc < spc.Length - 1)
                                pointCostString.Append(",");
                        }
                        EverySkill[friendlyName].Add("skillPointCosts", pointCostString.ToString() + "}");
                        EverySkill[friendlyName].Add("skillPointTotal", "'" + spc.Sum().ToString() + "'");
                    }
                    */
                    #endregion

                    //looks for all the skills that have benefits and gives us fields for our dictionary
                    var benefits = SkillModifiedValueManager.GetBenefitsFor(skill);
                    if (benefits != null)
                    {
                        for (int i = 1; i <= skill.MaxLevel; i++)
                        {
                            if (!EverySkill[friendlyName].ContainsKey("benefitsLevel" + i.ToString()))
                                EverySkill[friendlyName].Add("benefitsLevel" + i.ToString(), "{");
                        }

                        foreach (KeyValuePair<LocString, List<SkillModifiedValue>> kvp in benefits)
                        {
                            // this handles the skills on the player.. might be a better way to handle these.
                            //TODO: chances are many values are now MultiDynamic Values and this needs recoding completely
                            string stringValue = kvp.Key.ToString().StripTags().Replace(" ", "");
                            if (stringValue == "You")
                            {

                                foreach (SkillModifiedValue smv in kvp.Value)
                                {
                                    int levelCount = 1;
                                    for (int i = 1; i <= skill.MaxLevel; i++)
                                    {
                                        EverySkill[friendlyName]["benefitsLevel" + i.ToString()] += "\n" + space3 + space2 + space3 + "['" + skill.DisplayName + "'] = ";

                                    }

                                    foreach (float f in smv.Values)
                                    {
                                        if (f == smv.GetBaseValue)
                                            continue;
                                        StringBuilder sb = new StringBuilder();
                                        sb.Append("{");
                                        sb.Append("'" + smv.Verb + "',");
                                        sb.Append("'" + CharacterSkills(smv.SkillType, smv.GetBaseValue, f) + "'");
                                        sb.Append("},");
                                        EverySkill[friendlyName]["benefitsLevel" + levelCount.ToString()] += sb.ToString();
                                        levelCount++;
                                    }
                                }
                            }

                            Item item = Item.Get(stringValue + "Item");
                            if (item != null && item.Yield != null )
                            {
                                int levelCount = 1;
                                for (int i = 1; i <= skill.MaxLevel; i++)
                                {
                                    EverySkill[friendlyName]["benefitsLevel" + i.ToString()] += "\n" + space3 + space2 + space3 + "['" + item.DisplayName + "'] = ";
                                }

                                foreach (float f in item.Yield.Values)
                                {
                                    if (f == item.Yield.GetBaseValue)
                                        continue;

                                    StringBuilder sb = new StringBuilder();
                                    sb.Append("{");
                                    sb.Append("'" + item.Yield.Verb + "',");
                                    sb.Append("'yield of harvest by " + ((Math.Abs(item.Yield.GetBaseValue - f) / item.Yield.GetBaseValue) * 100).ToString("F0") + "%'");
                                    sb.Append("},");
                                    EverySkill[friendlyName]["benefitsLevel" + levelCount.ToString()] += sb.ToString();
                                    levelCount++;
                                }
                            }
                        }
                    }

                    // TODO: figure out how to retrieve Calorie and Damage benfits from tools, used to be much simpler
                    #region Tool Skills
                    /*
                    // handles skill benefits not on recipes and applied on something else such as tools
                    if (SpecialSkills(skill))
                    {
                        for (int i = 1; i <= skill.MaxLevel; i++)
                        {
                            EverySkill[friendlyName]["benefitsLevel" + i.ToString()] = "{\n" + space3 + space2 + space3 + "['" + skill.DisplayName + "'] = ";
                        }

                        SkillModifiedValue smv = null;

                        if (skill.Type == typeof(HuntingSkill)) //currently hunting effects bow efficiency
                        {
                            smv = typeof(BowItem).GetField("caloriesBurn", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance).GetValue(typeof(BowItem)) as SkillModifiedValue;                               
                        }                     

                        if (smv != null)
                        {
                            int levelCount = 1;
                            foreach (float f in smv.Values)
                            {
                                if (f == smv.GetBaseValue)
                                    continue;

                                StringBuilder sb = new StringBuilder();
                                sb.Append("{");
                                sb.Append("'" + smv.Verb + "',");
                                sb.Append("'" + CharacterSkills(smv.SkillType, smv.GetBaseValue, f) + "'");
                                sb.Append("},");
                                EverySkill[friendlyName]["benefitsLevel" + levelCount.ToString()] += sb.ToString();
                                levelCount++;
                            }
                        }
                    }
                    */
                    #endregion // Skill

                    EverySkill[friendlyName].Add("skillID", "'" + skill.Type.Name + "'");
                    EverySkill[friendlyName].Add("skillIDNum", "'" + skill.TypeID + "'");
                }

                FieldInfo skillUnlocksField = typeof(Skill).GetField("skillUnlocksTooltips", BindingFlags.Static | BindingFlags.NonPublic);
                var skillUnlocks = skillUnlocksField.GetValue(typeof(Skill)) as Dictionary<Type, Dictionary<int, List<LocString>>>;
                foreach (KeyValuePair<Type, Dictionary<int, List<LocString>>> kvp in skillUnlocks)
                {
                    var friendlyName = Item.Get(kvp.Key).DisplayName;
                    if (EverySkill.ContainsKey(friendlyName))
                    {
                        SortedDictionary<int, List<LocString>> sortedUnlocks = SortSkillUnlocks(kvp.Value as Dictionary<int, List<LocString>>);
                        foreach (KeyValuePair<int, List<LocString>> subkvp in sortedUnlocks)
                        {
                            int track = 0;
                            StringBuilder recipes = new StringBuilder();
                            StringBuilder skills = new StringBuilder();
                            if (!EverySkill[friendlyName].ContainsKey("level" + subkvp.Key.ToString() + "Unlocks"))
                            {
                                subkvp.Value.Sort();
                                foreach (LocString s in subkvp.Value)
                                {

                                    if (s.ToString().StripTags().Length > 6 && s.ToString().StripTags().Substring(s.ToString().StripTags().Length - 6) == "Recipe")
                                    {
                                        recipes.Append("'[[" + s.ToString().StripTags().Substring(0, s.ToString().StripTags().Length - 7) + "]] Recipe'");
                                        track++;
                                        if (track != subkvp.Value.Count)
                                        {
                                            recipes.Append(", ");
                                        }
                                    }
                                    else
                                    {
                                        skills.Append("'[[" + s.ToString().StripTags() + "]]'");
                                        track++;
                                        if (track != subkvp.Value.Count)
                                        {
                                            skills.Append(", ");
                                        }
                                    }
                                }
                                EverySkill[friendlyName]["level" + subkvp.Key.ToString() + "Unlocks"] = "{\n";
                                EverySkill[friendlyName]["level" + subkvp.Key.ToString() + "Unlocks"] += space2 + space3 + space3;
                                EverySkill[friendlyName]["level" + subkvp.Key.ToString() + "Unlocks"] += "['recipeUnlocks'] = {" + recipes.ToString() + "},\n";
                                EverySkill[friendlyName]["level" + subkvp.Key.ToString() + "Unlocks"] += space2 + space3 + space3;
                                EverySkill[friendlyName]["level" + subkvp.Key.ToString() + "Unlocks"] += "['skillUnlocks'] = {" + skills.ToString() + "},\n";
                                EverySkill[friendlyName]["level" + subkvp.Key.ToString() + "Unlocks"] += space2 + space3 + space3 + "}";
                            }
                        }
                    }
                }
            }

            // gets the recipe benefits for mat costs and time
            var allRecipes = RecipeFamily.AllRecipes;
            foreach (RecipeFamily r in allRecipes)
            {
                bool firstNotSmv = false;

                // get time bonus' if it is an smv
                if (r.CraftMinutes is SkillModifiedValue time)
                {
                    var friendlyName = time.Skill.DisplayName;
                    int levelCount = 1;
                    foreach (float f in time.Values)
                    {
                        if (f == time.GetBaseValue)
                            continue;

                        StringBuilder sb = new StringBuilder();
                        sb.Append("{");
                        sb.Append("'Craft time',");
                        sb.Append("'" + time.Verb + "',");
                        sb.Append("'" + ((Math.Abs(time.GetBaseValue - time.Values[levelCount]) / time.GetBaseValue) * 100).ToString("F0") + "%'");
                        sb.Append("},");

                        if (!EverySkill[friendlyName].ContainsKey("benefitsLevel" + levelCount.ToString()))
                            EverySkill[friendlyName]["benefitsLevel" + levelCount.ToString()] = "";
                       
                        EverySkill[friendlyName]["benefitsLevel" + levelCount.ToString()] += "\n" + space2 + space3 + space3 + "['" + r.RecipeName + "'] = ";
                        EverySkill[friendlyName]["benefitsLevel" + levelCount.ToString()] += sb.ToString();
                        levelCount++;
                    }
                }

                // get material bonus' if they are smv
                foreach (IngredientElement e in r.Ingredients)
                {
                    // ensure the first item not being an smv does not break our output
                    if (!(e.Quantity is SkillModifiedValue) && e == r.Ingredients.First())
                    {
                        firstNotSmv = true;
                    }

                    if (e.Quantity is SkillModifiedValue qty)
                    {
                        var friendlyName = qty.Skill.DisplayName;
                        int levelCount = 1;
                        foreach (float f in qty.Values)
                        {
                            if (f == qty.GetBaseValue)
                                continue;

                            StringBuilder sb = new StringBuilder();
                            sb.Append("{");
                            sb.Append("'" + e.Item.DisplayName + "',");
                            sb.Append("'" + qty.Verb + "',");
                            sb.Append("'" + ((Math.Abs(qty.GetBaseValue - qty.Values[levelCount]) / qty.GetBaseValue) * 100).ToString("F0") + "%'");
                            sb.Append("}");

                            if (e != r.Ingredients.Last())
                                sb.Append(",");
                            else
                                sb.Append("},");

                            if (!EverySkill[friendlyName].ContainsKey("benefitsLevel" + levelCount.ToString()))
                                EverySkill[friendlyName]["benefitsLevel" + levelCount.ToString()] = "{";
                            if (e == r.Ingredients.First() || firstNotSmv)
                            {
                                EverySkill[friendlyName]["benefitsLevel" + levelCount.ToString()] += "\n" + space2 + space3 + space3 + "['" + r.RecipeName + "'] = {";                               
                            }

                            EverySkill[friendlyName]["benefitsLevel" + levelCount.ToString()] += sb.ToString();
                            levelCount++;
                        }
                        firstNotSmv = false;
                    }
                }                
            }

            // clean up some formatting on each string built because we didn't know how many recipes would be added in each skill.
            foreach (Skill skill in Item.AllItems.OfType<Skill>())
            {
                if (EverySkill.ContainsKey(skill.DisplayName))
                {
                    string friendlyName = skill.DisplayName;

                    for (int i = 1; i <= skill.MaxLevel; i++)
                    {
                        if (EverySkill[friendlyName].ContainsKey("benefitsLevel" + i.ToString()) && EverySkill[friendlyName]["benefitsLevel" + i.ToString()].ToString().Length > 2)
                        {
                            if (EverySkill[friendlyName]["benefitsLevel" + i.ToString()].ToString().Substring(EverySkill[friendlyName]["benefitsLevel" + i.ToString()].ToString().Length - 2).Contains(","))
                            {
                                var trimmedString = EverySkill[friendlyName]["benefitsLevel" + i.ToString()].ToString().TrimEndString(",,");
                                EverySkill[friendlyName]["benefitsLevel" + i.ToString()] = trimmedString + "\n" + space2 + space3 + space3 + "}";
                            }
                        }
                        else if (EverySkill[friendlyName].ContainsKey("benefitsLevel" + i.ToString()) && EverySkill[friendlyName]["benefitsLevel" + i.ToString()].ToString().Length < 2)
                        {
                            EverySkill[friendlyName]["benefitsLevel" + i.ToString()] += "}";
                        }
                    }
                }
            }

            WriteDictionaryToFile(user, "Wiki_Module_Skills.txt", "skills", EverySkill);
        }

        public static SortedDictionary<int, List<LocString>> SortSkillUnlocks(Dictionary<int, List<LocString>> toSort)
        {
            SortedDictionary<int, List<LocString>> sortedUnlocks = new SortedDictionary<int, List<LocString>>();

            foreach (KeyValuePair<int, List<LocString>> kvp in toSort)
            {
                sortedUnlocks.Add(kvp.Key, kvp.Value);
            }

            return sortedUnlocks;
        }

        //TODO: check for 'not implemented' in output and see what needs fixing.
        #region Might be obsolete in 8.0
        public static string CharacterSkills(Type t, float b, float f)
        {
            /*
            if (t == typeof(CalorieEfficiencySkill))
            {
                return "calorie costs by " + ((Math.Abs(b - f) / b) * 100).ToString("F0") + "%";
            }

            if (t == typeof(StrongBackSkill))
            {
                return "carry weight by " + (b + f / 1000).ToString("F1") + "kg";
            }

            if (t == typeof(PredatoryInstinctsSkill))
            {
                return "the distance beyond 16m to approach prey by " + (b + f).ToString() + "m";
            }

            if (t == typeof(BigStomachSkill))
            {
                return "max calories by " + (b + f).ToString() + " calories";
            }
            
            if (t == typeof(BowDamageSkill))
            {                
                return "damage by " + ((Math.Abs(b - f) / b) * 100).ToString("F0") + "% using bow";
            }

            if (t == typeof(HuntingSkill)) // currently hunting effects bow efficiency. 
            {
                return "calorie costs by " + ((Math.Abs(b - f) / b) * 100).ToString("F0") + "% using a bow";
            }
            
            if (t == typeof(HoeEfficiencySkill))
            {
                return "calorie costs by " + ((Math.Abs(b - f) / b) * 100).ToString("F0") + "% using a hoe";
            }

            if (t == typeof(ShovelEfficiencySkill))
            {
                return "calorie costs by " + ((Math.Abs(b - f) / b) * 100).ToString("F0") + "% using a shovel";
            }

            if (t == typeof(ScytheEfficiencySkill))
            {
                return "calorie costs by " + ((Math.Abs(b - f) / b) * 100).ToString("F0") + "% using a scythe";
            }

            if (t == typeof(MiningEfficiencySkill))
            {
                return "calorie costs by " + ((Math.Abs(b - f) / b) * 100).ToString("F0") + "% using a pickaxe";
            }

            if (t == typeof(LoggingEfficiencySkill))
            {
                return "calorie costs by " + ((Math.Abs(b - f) / b) * 100).ToString("F0") + "% using an axe";
            }

            if (t == typeof(LoggingDamageSkill))
            {
                return "Damage by " + ((Math.Abs(b - f) / b) * 100).ToString("F0") + "% using an axe";
            }
            */
            return "NOT IMPLEMENTED";
        }

        // add new special skills to this list.
        public static bool SpecialSkills(Skill skill)
        {
            bool toReturn = false;
            //if (skill.Type == typeof(BowDamageSkill)) { toReturn = true; }
            //if (skill.Type == typeof(BowEfficiencySkill)) { toReturn = true; }
            //if (skill.Type == typeof(HoeEfficiencySkill)) { toReturn = true; }
            //if (skill.Type == typeof(ShovelEfficiencySkill)) { toReturn = true; }
            //if (skill.Type == typeof(LoggingEfficiencySkill)) { toReturn = true; }
            //if (skill.Type == typeof(MiningEfficiencySkill)) { toReturn = true; }
            //if (skill.Type == typeof(HuntingSkill)) { toReturn = true; }
            //if (skill.Type == typeof(FarmingSkill)) { toReturn = true; }
            //if (skill.Type == typeof(CalorieEfficiencySkill)) { toReturn = true; }
            //if (skill.Type == typeof(LoggingDamageSkill)) { toReturn = true; }
            //if (skill.Type == typeof(ScytheEfficiencySkill)) { toReturn = true; }
            //if (skill.Type == typeof(WetlandsWandererSkill)) { toReturn = true; }
            //if (skill.Type == typeof(DesertDrifterSkill)) { toReturn = true; }
            //if (skill.Type == typeof(TundraTravellerSkill)) { toReturn = true; }
            //if (skill.Type == typeof(GrasslandGathererSkill)) { toReturn = true; }
            //if (skill.Type == typeof(ForestForagerSkill)) { toReturn = true; }

            return toReturn;
        }
        #endregion

    }
}
