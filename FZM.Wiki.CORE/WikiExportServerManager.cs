using Eco.Core.Plugins.Interfaces;
using Eco.Shared.Localization;
using System;
using System.Collections.Generic;

namespace FZM.Wiki
{
    [LocDisplayName("FZM Wiki Export")]
    public partial class WikiDetails
    {  
        public void GetCommands(Dictionary<string, Action> nameToFunction)
        {
            nameToFunction.Add(Localizer.DoStr("Dump Details"), () => DumpDetails(null));
        }

        public string GetStatus() => "Eco Data Extrator Active.";
    }
}
