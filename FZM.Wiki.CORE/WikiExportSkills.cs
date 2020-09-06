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
using System.Text.RegularExpressions;

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
            // dictionary of item properties
            Dictionary<string, string> skillDetails = new Dictionary<string, string>()
            {
                // INFO 
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

            //var itemsAndRecipes = typeof(Recipe).DerivedTypes().Concat(typeof(Item).DerivedTypes());

            // used to get the recipe unlocks from each skill
            FieldInfo skillUnlocksField = typeof(Skill).GetField("skillUnlocksTooltips", BindingFlags.Static | BindingFlags.NonPublic);
            var skillUnlocks = skillUnlocksField.GetValue(typeof(Skill)) as Dictionary<Type, Dictionary<int, List<LocString>>>;

            foreach (Skill skill in Item.AllItems.OfType<Skill>())
            {
                if (!EverySkill.ContainsKey(skill.DisplayName))
                {                   
                    string friendlyName = skill.DisplayName;
                    //Log.WriteLine(Localizer.DoStr(friendlyName));
                    EverySkill.Add(friendlyName, new Dictionary<string, string>(skillDetails));

                    //INFO
                    if (skill.Title != "")
                        EverySkill[friendlyName]["title"] = "'" + skill.Title + "'"; // The title conferred by this skill.

                    Regex regex = new Regex("[\t\n\v\f\r]");
                    EverySkill[friendlyName]["description"] = "'" + regex.Replace(JSONStringSafe(skill.DisplayDescription), " ") + "'"; // The description of the skill.
                    EverySkill[friendlyName]["skillID"] = "'" + skill.Type.Name + "'"; // For linking purposes in the wiki.
                    EverySkill[friendlyName]["skillIDNum"] = "'" + skill.TypeID + "'"; // For linking purposes in the wiki.
                    EverySkill[friendlyName]["maxLevel"] = "'" + skill.MaxLevel.ToString() + "'"; // The maximum level of this skill

                    EverySkill[friendlyName]["root"] = skill.IsRoot.ToString().ToLower(); // If the skill is a ROOT Skill, these are overarching categories that house the actual learnable skills.                 
                    if (!skill.IsRoot)
                        EverySkill[friendlyName]["rootSkill"] = "'[[" + skill.RootSkillTree.StaticSkill.ToString() + "]]'"; // What is the skills root skill if this is not one.

                    EverySkill[friendlyName]["specialty"] = skill.IsSpecialty.ToString().ToLower(); // Skills under ROOT skills, these are learnable.
                    if (!skill.IsSpecialty)
                        EverySkill[friendlyName]["specialtySkill"] = "'[[" + skill.SpecialtySkillTree.StaticSkill.ToString() + "]]'"; // This property is likely OBSOLETE, but currently still exists in the SLG code. May be useful to modders.

                    if (!skill.IsRoot && !skill.IsSpecialty)
                        EverySkill[friendlyName]["prerequisites"] = "'" + skill.SkillTree.Parent.StaticSkill.ToString() + "'"; // This property is likely OBSOLETE, but currently still exists in the SLG code. May be useful to modders.

                    // Check if the skill has child skills (common with ROOT skills) and create a string to list them out.
                    if (skill.SkillTree.Children != null && skill.SkillTree.Children.Count() != 0)
                    {
                        int track = 0;
                        StringBuilder sb = new StringBuilder();
                        foreach (SkillTree child in skill.SkillTree.Children)
                        {
                            Skill childSkill = child.StaticSkill;
                            sb.Append(string.Format("'[[{0}]]'", childSkill));
                            track++;
                            if (track != skill.SkillTree.Children.Count())
                            {
                                sb.Append(",");
                            }
                        }

                        EverySkill[friendlyName]["childSkills"] = "{" + sb.ToString() + "}";
                    }

                    //RESEARCH                   

                    // check the skill is a speciality (can be learned) and has a SkillBook associated with it.
                    if (skill.IsSpecialty && (Item.GetSkillbookForSkillType(skill.Type) is SkillBook skillBook))
                    {
                        EverySkill[friendlyName]["specialtySkillBook"] = "'[[" + skillBook.DisplayName + "]]'";
                        EverySkill[friendlyName]["specialtySkillScroll"] = "'[[" + Item.Get(skillBook.SkillScrollType).DisplayName + "]]'";
                    }

                    // Check if the skill has items given and create a string to list them out.
                    if (skill.ItemTypesGiven != null && skill.ItemTypesGiven.Count() != 0)
                    {
                        int track = 0;
                        StringBuilder sb = new StringBuilder();
                        foreach (Type type in skill.ItemTypesGiven)
                        {
                            string t = RemoveItemTag(type.Name);
                            sb.Append(string.Format("[[{0}]]", t));
                            track++;
                            if (track != skill.ItemTypesGiven.Count())
                            {
                                sb.Append(", ");
                            }
                        }

                        EverySkill[friendlyName]["itemsGiven"] = "'" + sb.ToString() + "'";
                    }

                    // TALENTS

                    // sub dictionary for data building
                    Dictionary<string, string> levelTalents = new Dictionary<string, string>(); 

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
                                    levelTalents.Add(key, "'[[" + SplitName(group.TalentStrings.FirstOrDefault()) + "]]'");
                                }
                                else
                                {
                                    levelTalents[key] += ", '[[" + SplitName(group.TalentStrings.FirstOrDefault()) + "]]'";
                                }
                            }

                            //TODO: Currently there are no TalentGroups that have more than 1 Talents, but this assumedly could change and thus this it will need to be built eventually to accept it.
                        });
                        EverySkill[friendlyName]["talents"] = WriteDictionaryAsSubObject(levelTalents,1);
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

                    var benefits = SkillModifiedValueManager.GetBenefitsFor(skill);
                    if (benefits != null)
                    {
                        foreach (KeyValuePair<LocString, List<SkillModifiedValue>> kvp in benefits)
                        {
                            string keyString = kvp.Key.ToString().StripTags().Replace(" ", "");
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
                    EverySkill[friendlyName]["benefits"] = WriteDictionaryAsSubObject(levelBenefits,1);

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
                    EverySkill[friendlyName]["recipes"] = WriteDictionaryAsSubObject(levelUnlocks,1);
                }
            }
            WriteDictionaryToFile(user, "Wiki_Module_Skills.txt", "skills", EverySkill);
        }
    }
}
