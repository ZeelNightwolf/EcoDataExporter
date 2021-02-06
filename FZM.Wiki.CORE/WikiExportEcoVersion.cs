using Eco.Gameplay.Players;
using Eco.Gameplay.Systems.Chat;
using Eco.Shared;
using System;
using System.Collections.Generic;
using System.Text;

namespace FZM.Wiki
{
    public partial class WikiDetails : IChatCommandHandler
    {

        private static SortedDictionary<string, Dictionary<string, string>> Details = new SortedDictionary<string, Dictionary<string, string>>();

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
            Details["eco"]["fullInfo"] = $"'{EcoVersion.FullInfo}'";
            Details["eco"]["dataExportDate"] = $"'{DateTime.Now.Date}'";

            WriteDictionaryToFile("Wiki_Module_EcoVersion.txt", "eco", Details);
        }
    }
}
