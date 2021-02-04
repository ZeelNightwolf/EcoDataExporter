using Eco.Gameplay.Players;
using Eco.Gameplay.Systems.Chat;
using Eco.Shared.Localization;
using Eco.Shared.Utils;
using System.Collections.Generic;
using System.Reflection;
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
        // dictionary of animals and their dictionary of stats
        private static SortedDictionary<string, Dictionary<string, string>> WorldGen = new SortedDictionary<string, Dictionary<string, string>>();

        public static void WorldGenDetails(User user)
        {
            // dictionary of commands
            Dictionary<string, string> biomeDetails = new Dictionary<string, string>()
            {
                { "biome", "nil" },
                { "parent", "nil" },
            };


            // TODO:
            // load worldgen.eco
            // parse, sort and categorise json
            // populate list of Biomes data
            // interrogate modules



            // writes to WikiItems.txt to the Eco Server directory.
            WriteDictionaryToFile(user, "Wiki_Module_WorldGenData.txt", "worldgen", WorldGen);
        }
    }
}