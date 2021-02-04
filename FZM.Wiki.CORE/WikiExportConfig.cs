using Eco.Core.Plugins;
using Eco.Core.Plugins.Interfaces;
using Eco.Core.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace FZM.Wiki.CORE
{
    public class WikiExportConfig : IConfigurablePlugin
    {
        public IPluginConfig PluginConfig => throw new NotImplementedException();

        public ThreadSafeAction<object, string> ParamChanged { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public object GetEditObject()
        {
            throw new NotImplementedException();
        }

        public string GetStatus() => "Eco Data Extrator Active.";

        public void OnEditObjectChanged(object o, string param)
        {
            throw new NotImplementedException();
        }
    }
}
