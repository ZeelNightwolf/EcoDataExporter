using Eco.Core.Utils;
using Eco.Gameplay.Items;
using Eco.Gameplay.Players;
using Eco.Gameplay.Systems.Chat;
using Eco.Shared;
using Eco.Shared.Localization;
using Eco.Shared.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
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
        public static NLogWrapper logger = NLogWriter.GetConcreteLogger("WikiExporter");

        // outputting formats
        private static string space2 = "        ";
        private static string space3 = "            ";

        /// <summary>
        /// Get dumps of all the data
        /// </summary>
        /// <param name="user"></param>
        [ChatCommand("Creates all 8 dump files", ChatAuthorizationLevel.Admin)]
        public static void DumpDetails(User user)
        {
            try { DiscoverAll(user); } catch (Exception e) { LogExceptionAndNotify(user,e,"Discover All"); }
            try { ItemDetails(user); } catch (Exception e) { LogExceptionAndNotify(user, e, "Item Details"); }
            try { RecipesDetails(user); } catch (Exception e) { LogExceptionAndNotify(user, e, "Recipe Details"); }
            try { SkillsDetails(user); } catch (Exception e) { LogExceptionAndNotify(user, e, "Skills Details"); }
            try { TalentDetails(user); } catch (Exception e) { LogExceptionAndNotify(user, e, "Talent Details"); }
            try { PlantDetails(user); } catch (Exception e) { LogExceptionAndNotify(user, e, "Plant Details"); }
            try { TreeDetails(user); } catch (Exception e) { LogExceptionAndNotify(user, e, "Tree Details"); }
            try { AnimalDetails(user); } catch (Exception e) { LogExceptionAndNotify(user, e, "Animal Details"); }
            try { CommandDetails(user); } catch (Exception e) { LogExceptionAndNotify(user, e, "Command Details"); }  
        }

        public static void LogExceptionAndNotify(User user, Exception e, string dump)
        {
            Log.WriteErrorLine(Localizer.DoStr(e.Message));
            user.Player.MsgLocStr("ERROR: " + dump + " returned an error, check log for details, no dump generated!");
        }

        /// <summary>
        /// Discover all items and skills in the game to enable query.
        /// </summary>
        /// <param name="user"></param>
        [ChatCommand("Discovers all items in game", ChatAuthorizationLevel.Admin)]
        public static void DiscoverAll(User user)
        {
            IEnumerable<Type> types = ((IEnumerable<Item>)Item.AllItems).Select<Item, Type>(item => item.Type);
            DiscoveryManager.Obj.DiscoveredThings.UnionWith(types);
            DiscoveryManager.Obj.UpdateDiscoveredItems();
            user.Player.Msg(Localizer.Do($"All discovered"));
        }

        /// <summary>
        /// Split up the Pascal Case to something readable
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static string SplitName(string name)
        {
            string[] NameSplit = Regex.Split(name, @"(?<!(^|[A-Z0-9]))(?=[A-Z0-9])|(?<!(^|[^A-Z]))(?=[0-9])|(?<!(^|[^0-9]))(?=[A-Za-z])|(?<!^)(?=[A-Z][a-z])");
            int count = 0;
            var sb = new StringBuilder();
            foreach (string str in NameSplit)
            {
                sb.Append(str);
                count++;
                if (count != NameSplit.Length)
                    sb.Append(" ");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Remove the annoying 'Item' and the end of type names
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public static string RemoveItemTag(string item)
        {
            string cleanItem = item.Substring(0, item.Length - 4);
            return cleanItem;
        }

        public static string WriteDictionaryAsSubObject(Dictionary<string,string> dict)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(" {");
            foreach (KeyValuePair<string,string> kvp in dict)
            {
                sb.AppendLine(space2 + space3 + space2 + "['" + kvp.Key + "'] = {" + kvp.Value + "},");
            }
            sb.Append(space2 + space3 + space2 + "}");

            return sb.ToString();
        }

        /// <summary>
        /// Helper method as reflection is used a number of times to get private fields
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="field"></param>
        /// <returns></returns>
        public static object GetFieldValue(object obj, string field)
        {
            var _value = obj.GetType().GetField(field, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance).GetValue(obj);

            return _value;
        }

        public static object GetPropertyValue(object obj, string property)
        {
            var _value = obj.GetType().GetProperty(property, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance).GetValue(obj);

            return _value;
        }

        /// <summary>
        /// Method for writing the created dictionaries to file
        /// </summary>
        /// <param name="user"></param>
        /// <param name="filename"> filename to dump to</param>
        /// <param name="type"> for the lua table initial name</param>
        /// <param name="dictionary"> the dictionary to write</param>
        public static void WriteDictionaryToFile(User user, string filename, string type, SortedDictionary<string, Dictionary<string, string>> dictionary)
        {
            // writes to the Eco Server directory.
            string path = AppDomain.CurrentDomain.BaseDirectory + filename;
            using (StreamWriter streamWriter = new StreamWriter(path, false))
            {
                streamWriter.WriteLine("-- Eco Version : " + EcoVersion.Version);
                streamWriter.WriteLine();
                streamWriter.WriteLine("return {\n    " + type + " = {");
                foreach (string key in dictionary.Keys)
                {
                    streamWriter.WriteLine(string.Format("{0}['{1}'] = {{", space2, key));
                    foreach (KeyValuePair<string, string> keyValuePair in dictionary[key])
                        streamWriter.WriteLine(string.Format("{0}{1}['{2}'] = {3},", space2, space3, keyValuePair.Key, keyValuePair.Value));
                    streamWriter.WriteLine(string.Format("{0}{1}}},", space2, space3));
                }
                streamWriter.WriteLine("    },\n}");
                streamWriter.Close();
                user.Player.Msg(Localizer.Do($"Dumped to {AppDomain.CurrentDomain.BaseDirectory}{filename}"));
            }
        }
    }

}
