using Eco.Core.Plugins;
using Eco.Core.Plugins.Interfaces;
using Eco.Core.Utils;
using Eco.Gameplay.Players;
using Eco.Shared.Localization;
using System;
using System.Collections.Generic;
using System.Text;

namespace FZM.Wiki
{
    public class DataExportConfig
    {

    }

    [LocDisplayName("FZM Wiki Export")]
    public partial class WikiDetails : IModKitPlugin, ICommandablePlugin
    {  
        public void GetCommands(Dictionary<string, Action> nameToFunction)
        {
            nameToFunction.Add(Localizer.DoStr("Dump Details"), () => DumpDetails(null));
        }

        public string GetStatus() => "Eco Data Extrator Active.";
    }
}
