using Eco.Gameplay.Players;
using Eco.Gameplay.Systems.Chat;
using Eco.Shared.Localization;
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
        private static SortedDictionary<string, Dictionary<string, string>> EveryCommand = new SortedDictionary<string, Dictionary<string, string>>();

        public static void CommandDetails()
        {
            // dictionary of commands
            Dictionary<string, string> commandDetails = new Dictionary<string, string>()
            {
                { "command", "nil" },
                { "parent", "nil" },
                { "helpText", "nil" },
                { "shortCut", "nil" },
                { "level", "nil" },
                { "parameters", "nil" }
            };

            Regex regex = new Regex("\t\n\v\f\r");

            var chatServer = ChatServer.Obj;
            var chatManager = GetFieldValue(chatServer, "netChatManager");
            ChatCommandService chatCommandService = (ChatCommandService)GetFieldValue(chatManager, "chatCommandService");

            IEnumerable<ChatCommand> commands = chatCommandService.GetAllCommands();

            foreach (var com in commands)
            {
                if (com.Key == "dumpdetails")
                    continue;

                var command = $"/{Localizer.DoStr(com.ParentKey)} {Localizer.DoStr(com.Name)}";
                if (!EveryCommand.ContainsKey(command))
                {
                    EveryCommand.Add(command, new Dictionary<string, string>(commandDetails));
                    EveryCommand[command]["command"] = "'" + Localizer.DoStr(com.Key) + "'";

                    if (com.ParentKey != null && com.ParentKey != "" )
                        EveryCommand[command]["parent"] = "'" + Localizer.DoStr(com.ParentKey) + "'";

                    EveryCommand[command]["helpText"] = "'" + Localizer.DoStr(JSONStringSafe(com.HelpText)) + "'";
                    EveryCommand[command]["shortCut"] = "'" + Localizer.DoStr(com.ShortCut) + "'";
                    EveryCommand[command]["level"] = "'" + Localizer.DoStr(com.AuthLevel.ToString()) + "'";

                    
                    MethodInfo method = com.Method;
                    if (method == null)
                        continue;

                    ParameterInfo[] parameters = method.GetParameters();

                    if (parameters == null)
                        continue;

                    Dictionary<string, string> pars = new Dictionary<string, string>();

                    foreach (var p in parameters)
                    {
                        if (p.Name == "user")
                            continue;

                        string pos = "Arg" + p.Position.ToString();
                        pars[pos] = "'" + p.Name + "', '" + p.ParameterType.Name + "'";
                        
                        if (p.HasDefaultValue)
                            pars[pos] += ", '" + p.DefaultValue + "'";
                    }
                    EveryCommand[command]["parameters"] = WriteDictionaryAsSubObject(pars,1);
                }
            }

            // writes to WikiItems.txt to the Eco Server directory.
            WriteDictionaryToFile("Wiki_Module_CommandData.txt", "commands", EveryCommand);
        }      
    }
}