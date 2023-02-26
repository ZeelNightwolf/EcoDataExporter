using Eco.Core.Utils;
using Eco.Gameplay.Items;
using Eco.Gameplay.Players;
using Eco.Gameplay.Systems;
using Eco.Gameplay.Systems.Chat;
using Eco.Gameplay.Systems.Messaging.Chat.Commands;
using Eco.Shared;
using Eco.Shared.Localization;
using Eco.Shared.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    public partial class WikiDetails
    {
        //public static NLogWrapper logger => NLogWriter.GetConcreteLogger("WikiExporter");

        private static User dummy;

        // outputting formats
        private static string space2 = "        ";
        private static string space3 = "            ";

        internal const string saveLocation = @"\FZM\DataExports\";
        public static string SaveLocation => GetRelevantDirectory();
        public static string AssemblyLocation => Directory.GetCurrentDirectory();

        static string GetRelevantDirectory()
        {
            if (saveLocation.StartsWith(@"\"))
                return AssemblyLocation + saveLocation;
            return saveLocation;
        }

        /// <summary>
        /// Get dumps of all the data
        /// </summary>
        /// <param name="user"></param>
        [ChatCommand("Creates all dump files", ChatAuthorizationLevel.Admin)]
        public static void DumpDetails(User user)
        {
            StringBuilder alert = new StringBuilder();

            alert.AppendLine("Errors: ");

            try { DiscoverAll(); }       catch (Exception e) { alert.AppendLine(LogExceptionAndNotify(user, e, "Discover All")); }
            try { EcoDetails(); }        catch (Exception e) { alert.AppendLine(LogExceptionAndNotify(user, e, "Eco Details")); }
            try { ItemDetails(user); } catch (Exception e) { alert.AppendLine(LogExceptionAndNotify(user, e, "Item Details")); }
            try { RecipesDetails(); }    catch (Exception e) { alert.AppendLine(LogExceptionAndNotify(user, e, "Recipe Details")); }
            try { SkillsDetails(); }     catch (Exception e) { alert.AppendLine(LogExceptionAndNotify(user, e, "Skills Details")); }
            try { TalentDetails(); }     catch (Exception e) { alert.AppendLine(LogExceptionAndNotify(user, e, "Talent Details")); }
            try { PlantDetails(); }      catch (Exception e) { alert.AppendLine(LogExceptionAndNotify(user, e, "Plant Details")); }
            try { TreeDetails(); }       catch (Exception e) { alert.AppendLine(LogExceptionAndNotify(user, e, "Tree Details")); }
            try { AnimalDetails(); }     catch (Exception e) { alert.AppendLine(LogExceptionAndNotify(user, e, "Animal Details")); }
            try { CommandDetails(); }    catch (Exception e) { alert.AppendLine(LogExceptionAndNotify(user, e, "Command Details")); }
            try { EcopediaDetails(); }   catch (Exception e) { alert.AppendLine(LogExceptionAndNotify(user, e, "Ecopedia Details")); }

            alert.AppendLine("");
            alert.AppendLine("INFO: ");
            alert.AppendLine("Dump folder is open, alt-tab to check dumps."); 
            alert.AppendLine("Check logs for error details.");
            alert.AppendLine("");
            alert.AppendLine("DUMP FOLDER: ");
            alert.AppendLine($"{SaveLocation}");

            user.Player.InfoBoxLocStr(alert.ToString());
        }

        public static string LogExceptionAndNotify(User user, Exception e, string dump)
        {
            Log.WriteErrorLine(Localizer.DoStr(e.Message));
            return $"{dump},  no dump generated!";
        }

        public static void Debug(string s) { Log.WriteLine(Localizer.DoStr($"{s}")); }
        
        /// <summary>
        /// Discover all items and skills in the game to enable query.
        /// </summary>
        /// <param name="user"></param>
        [ChatCommand("Discovers all items in game", ChatAuthorizationLevel.Admin)]
        public static void DiscoverAll()
        {
            IEnumerable<Type> types = ((IEnumerable<Item>)Item.AllItems).Select<Item, Type>(item => item.Type);
            DiscoveryManager.Obj.DiscoveredThings.UnionWith(types);
            DiscoveryManager.Obj.UpdateDiscoveredItems();
        }

        #region StringMethods
        /// <summary>
        /// Split up the Pascal Case to something readable
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static string SplitName(string name)
        {
            string[] NameSplit = { };

            NameSplit = (name.Contains("Lv") || name.Contains("CO2"))
                ? Regex.Split(name, @"(?<!(^|[A-Z]))(?=[A-Z])|(?<!^)(?=[A-Z][a-z])")
                : Regex.Split(name, @"(?<!(^|[A-Z]))(?=[A-Z])|(?<!^)(?=[A-Z][a-z]|(?<!^|[0-9])(?=[0-9]))");

            int count = 0;
            var sb = new StringBuilder();
            foreach (string str in NameSplit)
            {
                sb.Append(str);
                count++;
                if (count != NameSplit.Length)
                    sb.Append(" ");
            }

            Regex regex = new Regex("[ ]{2,}");

            return regex.Replace(sb.ToString(), " ");
        }

        public static string JSONStringSafe(string s)
        {
            string[] NameSplit = Regex.Split(s, @"(?=['?])");
            var sb = new StringBuilder();
            foreach (string str in NameSplit)
            {
                sb.Append(str);
                if (str != NameSplit.Last())
                    sb.Append("\\");
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

        private static string CleanTags(string hasTags)
        {
            Regex regex = new Regex("<[^>]*>");
            return regex.Replace(hasTags, "");
        }
        #endregion

        #region ReflectionMethods
        /// <summary>
        /// Helper method as reflection is used a number of times to get private fields
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="field"></param>
        /// <returns></returns>
        public static object GetFieldValue(object obj, string field)
        {
            return obj.GetType().GetField(field, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance).GetValue(obj);
        }

        public static object GetPropertyValue(object obj, string property)
        {
            return obj.GetType().GetProperty(property, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance).GetValue(obj);
        }

        public static bool IsInstanceOfGenericType(Type genericType, object instance)
        {
            Type type = instance.GetType();
            while (type != null)
            {
                if (type.IsGenericType &&
                    type.GetGenericTypeDefinition() == genericType)
                {
                    return true;
                }
                type = type.BaseType;
            }
            return false;
        }
        #endregion

        #region Writers
        // wrties out a Dictionary To be used as a Next Level Object
        public static string WriteDictionaryAsSubObject(Dictionary<string, string> dict, int depth)
        {
            string spaces = space2 + space3;

            for (int i = 0; i < depth; i++)
            {
                spaces += space2;
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine(" {");
            foreach (KeyValuePair<string, string> kvp in dict)
            {
                sb.AppendLine(spaces + "['" + kvp.Key + "'] = {" + kvp.Value + "},");
            }
            sb.Append(spaces + "}");

            return sb.ToString();
        }

        private static void AddToErrorLog(ref Dictionary<string, (string, Exception)> log, string key, string prop, Exception ex)
        {
            log.Add(key, (prop, ex));
        }

        /// <summary>
        /// Method for writing the created dictionaries to file
        /// </summary>
        /// <param name="user"></param>
        /// <param name="filename"> filename to dump to</param>
        /// <param name="type"> for the lua table initial name</param>
        /// <param name="dictionary"> the dictionary to write</param>
        public static void WriteDictionaryToFile(string filename, string type, SortedDictionary<string, Dictionary<string, string>> dictionary, bool final = true)
        {
            var lang = LocalizationPlugin.Config.Language;

            // writes to the Eco Server directory.
            if (!Directory.Exists(SaveLocation + $@"{lang}\"))
                Directory.CreateDirectory(SaveLocation + $@"{lang}\");

            string path = SaveLocation + $@"{lang}\" + filename;

            using (StreamWriter streamWriter = new StreamWriter(path, false))
            {
                streamWriter.WriteLine("-- Eco Version : " + EcoVersion.Version);
                streamWriter.WriteLine("-- Export Language: " + lang);
                streamWriter.WriteLine();
                streamWriter.WriteLine("return {\n    " + type + " = {");
                foreach (string key in dictionary.Keys)
                {
                    streamWriter.WriteLine(string.Format("{0}['{1}'] = {{", space2, key));
                    foreach (KeyValuePair<string, string> keyValuePair in dictionary[key])
                        streamWriter.WriteLine(string.Format("{0}{1}['{2}'] = {3},", space2, space3, keyValuePair.Key, keyValuePair.Value));
                    streamWriter.WriteLine(string.Format("{0}}},", space2));
                }
                streamWriter.Write("    },");
                if (final)
                    streamWriter.Write("\n}");
                streamWriter.Close();
            }
        }

        /// <summary>
        /// Method for writing the created error dictionaries to file
        /// </summary>
        /// <param name="user"></param>
        /// <param name="filename"> filename to dump to</param>
        /// <param name="type"> for the export type name</param>
        /// <param name="errors"> the dictionary to write</param>
        public static void WriteErrorLogToFile(string filename, string type, Dictionary<string,(string,Exception)> errors)
        {           
            // writes to the Eco Server directory.
            if (!Directory.Exists(SaveLocation + $@"Errors\"))
                Directory.CreateDirectory(SaveLocation + $@"Errors\");

            string path = SaveLocation + $@"Errors\" + filename;

            using (StreamWriter streamWriter = new StreamWriter(path, false))
            {
                streamWriter.WriteLine("-- Eco Version : " + EcoVersion.Version);
                streamWriter.WriteLine("-- Export Date: " + DateTime.Today);
                streamWriter.WriteLine();
                streamWriter.WriteLine($"Errors for {type}");
                foreach (KeyValuePair<string,(string,Exception)> kvp in errors)
                {
                    streamWriter.WriteLine($"{kvp.Key} failed at property {kvp.Value.Item1}. Error: {kvp.Value.Item2.Message}");
                }
                streamWriter.Close();
            }
        }
        #endregion
    }

}
