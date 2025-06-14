using System.Collections.Generic;
using System.Xml.Linq;
using System.IO;

namespace MCGalaxy.Gui
{
    public static class PluginLibraryLoader
    {
        public static List<PluginCardData> LoadFromXml(string path)
        {
            var plugins = new List<PluginCardData>();
            if (!File.Exists(path)) return plugins;

            var doc = XDocument.Load(path);
            foreach (var x in doc.Descendants("Plugin"))
            {
                // Default to false if missing or not "true"
                bool isCommand = false;
                var cmdElem = x.Element("Command");
                if (cmdElem != null && (cmdElem.Value.Equals("true", System.StringComparison.OrdinalIgnoreCase) || cmdElem.Value == "1"))
                    isCommand = true;

                plugins.Add(new PluginCardData {
                    Title = (string)x.Element("Title") ?? "",
                    Description = (string)x.Element("Description") ?? "",
                    Credits = (string)x.Element("Credits") ?? "",
                    ThumbnailUrl = (string)x.Element("ThumbnailUrl") ?? "",
                    DownloadUrl = (string)x.Element("DownloadUrl") ?? "",
                    Command = isCommand
                });
            }
            return plugins;
        }
    }
}