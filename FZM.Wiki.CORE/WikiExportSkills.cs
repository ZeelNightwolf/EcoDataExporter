using Eco.Gameplay.DynamicValues;
using Eco.Gameplay.Items;
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
        private static Dictionary<string, (string, Exception)> ErrorSkills = new();

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

            FieldInfo skillUnlocksField = typeof(SkillTooltips).GetField("skillUnlocksTooltips", BindingFlags.Static | BindingFlags.NonPublic);
            var skillUnlocks = skillUnlocksField.GetValue(typeof(SkillTooltips)) as Dictionary<Type, Dictionary<int, List<LocString>>>;

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
                        prop = "untranslated";                                            skillD[prop] = $"'{skill.DisplayName.NotTranslated}'";
                        prop = "title";          if (skill.Title != "")                   skillD[prop] = $"'{Localizer.DoStr(skill.Title)}'"; // The title conferred by this skill.

                        Regex regex = new Regex("[\t\n\v\f\r]");
                        prop = "description";                                             skillD[prop] = $"'{regex.Replace(JSONStringSafe(skill.DisplayDescription), " ")}'"; // The description of the skill.
                        prop = "skillID";                                                 skillD[prop] = $"'{Localizer.DoStr(skill.Type.Name)}'"; // For linking purposes in the wiki.
                        prop = "skillIDNum";                                              skillD[prop] = $"'{skill.TypeID}'"; // For linking purposes in the wiki.
                        prop = "maxLevel";                                                skillD[prop] = $"'{skill.MaxLevel}'"; // The maximum level of this skill
                        prop = "root";                                                    skillD[prop] = skill.IsRoot.ToString().ToLower(); // If the skill is a ROOT Skill, these are overarching categories that house the actual learnable skills.                 
                        prop = "rootSkill";      if (!skill.IsRoot)                       skillD[prop] = $"'[[{skill.RootSkillTree.StaticSkill}]]'"; // What is the skills root skill if this is not one.
                        prop = "specialty";                                               skillD[prop] = skill.IsSpecialty.ToString().ToLower(); // Skills under ROOT skills, these are learnable.
                        prop = "specialtySkill"; if (!skill.IsSpecialty)                  skillD[prop] = $"'[[{skill.SpecialtySkillTree.StaticSkill}]]'"; // This property is likely OBSOLETE, but currently still exists in the SLG code. May be useful to modders.
                        prop = "prerequisites";  if (!skill.IsRoot && !skill.IsSpecialty) skillD[prop] = $"'{skill.SkillTree.Parent.StaticSkill}'"; // This property is likely OBSOLETE, but currently still exists in the SLG code. May be useful to modders.

                        // Check if the skill has child skills (common with ROOT skills) and create a string to list them out.
                        prop = "childSkills";
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
                             skillD[prop] = "{" + sb.ToString() + "}";
                        }

                        //RESEARCH                   
                        // check the skill is a speciality (can be learned) and has a SkillBook associated with it.
                        if (skill.IsSpecialty && (Item.GetSkillbookForSkillType(skill.Type) is SkillBook skillBook))
                        {
                            prop = "specialtySkillBook";   skillD[prop] = $"'[[{skillBook.DisplayName}]]'";
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
    }
}
