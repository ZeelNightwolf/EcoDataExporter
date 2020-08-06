using Eco.Gameplay.Players;
using Eco.Gameplay.Systems.Chat;
using Eco.Shared.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using Eco.Shared.Authentication;
using System.Linq;
using System.Reflection;
using Eco.Gameplay.Items;
using System.Text.RegularExpressions;
using Eco.Shared;
using Eco.Shared.Localization;
using System.Text;
using System.Collections;

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

        /// <summary>
        /// Retrieves the commands from Eco.
        /// </summary>
        /// <param name="user"></param>
        [ChatCommand("Creates a dump file of all commands", ChatAuthorizationLevel.Admin)]
        public static void CommandDetails(User user)
        {
            // dictionary of commands
            Dictionary<string, string> commandDetails = new Dictionary<string, string>()
            {
                { "command", "nil" },
                { "helpText", "nil" },
                { "level", "nil" },
                { "commandIsMethod", "'true'" }
            };

            MethodInfo[] methods = AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypes()).SelectMany(x => x.GetMethods().Where(y => y.IsStatic && y.IsPublic)).ToArray();
            foreach (MethodInfo m in methods)
            {
                m.CustomAttributes.ForEach(x =>
                {
                    if (x.AttributeType.Name == "ChatCommandAttribute" && m.ReflectedType.Namespace != "FZM.Wiki")
                    {
                        string commandName = m.Name;
                        ParameterInfo[] parameters = m.GetParameters();
                        if (!EveryCommand.ContainsKey(commandName))
                        {
                            EveryCommand.Add(commandName, new Dictionary<string, string>(commandDetails));
                            EveryCommand[commandName]["command"] = "'" + commandName + "'";
                            int count = 1;
                            if (x.ConstructorArguments.Count == 3)
                            {
                                EveryCommand[commandName]["commandIsMethod"] = "'false'"; // set a toggle to include changing the command name
                                count = 0;
                            }
                            foreach (CustomAttributeTypedArgument a in x.ConstructorArguments)
                            {
                                Regex regex = new Regex("\t\n\v\f\r");
                                if (count == 0) // if toggled
                                    EveryCommand[commandName]["command"] = "'" + a.Value.ToString() + "'";
                                if (count == 1)
                                    EveryCommand[commandName]["helpText"] = "'" + regex.Replace(CleanTags(a.Value.ToString()), " ").Replace("'", "\\'") +  "'";
                                if (count == 2)
                                {
                                    switch ((int)a.Value)
                                    {
                                        case 0:
                                        case 1:
                                            EveryCommand[commandName]["level"] = "'user'";
                                            break;
                                        case 2:
                                            EveryCommand[commandName]["level"] = "'admin'";
                                            break;
                                        case 3:
                                            EveryCommand[commandName]["level"] = "'developer'";
                                            break;
                                        default:
                                            EveryCommand[commandName]["level"] = "'user'";
                                            break;
                                    }
                                }
                                count++;
                            }

                            // return the parameter types the command expects to be passed to it
                            EveryCommand[commandName].Add("parameters", "'" + (parameters.Length).ToString() + "'");
                            int pos = 1;
                            foreach (ParameterInfo p in parameters)
                            {
                                if (p.Name == "user")
                                {
                                    EveryCommand[commandName]["parameters"] = "'" + (parameters.Length - 1).ToString() + "'";
                                }
                                if (p.Name != "user")
                                {
                                    EveryCommand[commandName].Add("arg" + pos + "Type", "'" + p.ParameterType.Name + "'");
                                    EveryCommand[commandName].Add("arg" + pos + "Name", "'" + p.Name + "'");
                                    EveryCommand[commandName].Add("arg" + pos + "Optional", p.IsOptional ? "'true'" : "'false'");
                                    if (p.HasDefaultValue && p.DefaultValue != null)
                                        EveryCommand[commandName].Add("arg" + pos + "Default", "'" + p.DefaultValue + "'");
                                    pos++;
                                }
                            }
                        }
                    }
                });
            }

            // writes to WikiItems.txt to the Eco Server directory.
            WriteDictionaryToFile(user, "Wiki_Module_CommandData.txt", "commands", EveryCommand);
        }

        
    }
}