using Eco.Gameplay.Components;
using Eco.Gameplay.DynamicValues;
using Eco.Gameplay.Items;
using Eco.Gameplay.Players;
using Eco.Gameplay.Skills;
using Eco.Gameplay.Systems.Chat;
using Eco.Shared.Localization;
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

        public static void TalentDetails(User user)
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

            foreach (Talent talent in Talent.AllTalents)
            {
                TalentGroup talentGroup;
                if (talent.TalentGroupType != null)
                {
                    talentGroup = Item.Get(Talent.TypeToTalent[talent.GetType()].TalentGroupType) as TalentGroup;
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

            WriteDictionaryToFile(user, "Wiki_Module_TalentData.txt", "talents", EveryTalent);
        }
    }
}
