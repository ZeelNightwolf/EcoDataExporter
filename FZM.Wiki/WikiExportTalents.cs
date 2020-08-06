using Eco.Gameplay.Components;
using Eco.Gameplay.DynamicValues;
using Eco.Gameplay.Items;
using Eco.Gameplay.Players;
using Eco.Gameplay.Skills;
using Eco.Gameplay.Systems.Chat;
using System;
using System.Collections.Generic;

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
        private static SortedDictionary<string, Dictionary<string, string>> EveryTalent = new SortedDictionary<string, Dictionary<string, string>>();

        [ChatCommand("Creates a dump file of all discovered Talents", ChatAuthorizationLevel.Admin)]
        public static void TalentDetails(User user)
        {
            Dictionary<string, string> TalentDetails = new Dictionary<string, string>()
            {
                { "category", "nil" },
                { "group", "nil" },
                { "name" , "nil" },
                { "description", "nil" },
                { "talentType", "nil" },
                { "owningSkill", "nil" },
                { "activeLevel", "nil" },

            };

            foreach (Talent talent in Talent.AllTalents)
            {
                TalentGroup talentGroup;
                if (talent.TalentGroupType != null)
                {
                    talentGroup = Item.Get(Talent.TypeToTalent[talent.GetType()].TalentGroupType) as TalentGroup;
                    string displayName = talentGroup.DisplayName.ToString();
                    if (!EveryTalent.ContainsKey(displayName))
                    {
                        Console.WriteLine("C");
                        EveryTalent.Add(displayName, new Dictionary<string, string>(TalentDetails));
                        EveryTalent[displayName]["category"] = string.Format($"'{talentGroup.Category}'");
                        EveryTalent[displayName]["group"] = string.Format($"'{talentGroup.Group}'");
                        EveryTalent[displayName]["name"] = string.Format($"'{talentGroup.DisplayName}'");
                        EveryTalent[displayName]["description"] = string.Format($"'{talentGroup.DisplayDescription}'");

                        EveryTalent[displayName]["talentType"] = string.Format($"'{SplitName(talent.TalentType.Name)}'");

                        //Connected Skill and Level Unlock
                        EveryTalent[displayName]["owningSkill"] = string.Format($"'{SplitName(talentGroup.OwningSkill.Name)}'");
                        EveryTalent[displayName]["activeLevel"] = string.Format($"'{talentGroup.Level}'");
                    }
                }
            }

            WriteDictionaryToFile(user, "Wiki_Module_TalentData.txt", "talents", EveryTalent);
        }
    }
}
