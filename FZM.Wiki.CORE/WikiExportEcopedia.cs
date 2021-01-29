using Eco.Gameplay.Players;
using Eco.Gameplay.Systems.Chat;
using Eco.Shared.Localization;
using Eco.Shared.Utils;
using System.Collections.Generic;
using Eco.Core.Ecopedia;
using System.Reflection;
using System.Text.RegularExpressions;
using Eco.Gameplay.EcopediaRoot;

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
        // dictionary of animals and their dictionary of stats
        private static SortedDictionary<string, Dictionary<string, string>> EcopediaDict = new SortedDictionary<string, Dictionary<string, string>>();

        /// <summary>
        /// Retrieves the commands from Eco.
        /// </summary>
        /// <param name="user"></param>
        [ChatCommand("Creates a dump file of all Ecopaedia details", ChatAuthorizationLevel.Admin)]

        public static void EcopediaDetails(User user)
        {
            // dictionary of commands
            Dictionary<string, string> entry = new Dictionary<string, string>()
            {
                { "biome", "nil" },
                { "parent", "nil" },
            };


            // writes to WikiItems.txt to the Eco Server directory.
            WriteDictionaryToFile(user, "Wiki_Module_Ecopedia.txt", "ecopedia", EcopediaDict);
        }
    }
}
