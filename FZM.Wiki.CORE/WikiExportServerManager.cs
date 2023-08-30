using Eco.Core.Plugins.Interfaces;
using Eco.Shared.Localization;
using System;
using System.Collections.Generic;

namespace FZM.Wiki
{
    [LocDisplayName("FZM Wiki Export")]
    public class WikiExportPlugin : IModKitPlugin, ICommandablePlugin
    {
        public void GetCommands(Dictionary<string, Action> nameToFunction)
        {
            nameToFunction.Add((string)Localizer.DoStr("Export All Data"), new Action(() => WikiExportCommandHandler.WikiExportCommands.dumpdetails(null)));
        }
        string status = "Loaded.";
        public override string ToString() => (string)Localizer.DoStr("FZM Wiki");

        public string GetStatus() => this.status;
        public string GetCategory() => "Mods";
    }
}