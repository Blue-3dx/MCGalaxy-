using System;
using MCGalaxy;
using MCGalaxy.Network;
using MCGalaxy.Tasks;

namespace PluginRainbowColors
{
    public sealed class RainbowPlugin : Plugin 
    {
        public override string creator { get { return "Not UnknownShadow200"; } }
        public override string MCGalaxy_Version { get { return "1.9.1.4"; } }
        public override string name { get { return "Rainbow"; } }

        SchedulerTask task;

        public override void Load(bool startup) {
            task = Server.MainScheduler.QueueRepeat(RainbowCallback, null,
                                                    TimeSpan.FromMilliseconds(80));
        }

        public override void Unload(bool shutdown) {
            Server.MainScheduler.Cancel(task);
        }

        // Rainbow
        static string[] rainbowColors = { "9400D3", "4B0082", "0000FF", "00FF00", "FFFF00", "FF7F00", "FF0000" };
        // Pastel
        static string[] pastelColors = { "FFB3BA", "FFDFBA", "FFFFBA", "BAFFC9", "BAE1FF", "D5BAFF", "FFBAEC" };
        // Synthwave
        static string[] synthwaveColors = { "FF71CE", "01CDFE", "05FFA1", "B967FF", "FFFB96" };
        // Sunset
        static string[] sunsetColors = { "FF9A8B", "FF6A88", "FF99AC", "FFD6A5", "FDFFB6", "CAFFBF" };
        // Galaxy
        static string[] galaxyColors = { "3B0A45", "5D1451", "A12059", "F5386A", "3B82F6", "7F00FF" };
        // Emerald
        static string[] emeraldColors = { "0B3D2E", "117A65", "1ABC9C", "48C9B0", "17A589", "145A32", "0E6655" };
        // Diamond
        static string[] diamondColors = { "E0F7FA", "B2EBF2", "81D4FA", "4FC3F7", "B3E5FC", "E1F5FE", "FFFFFF" };

        static int index;

        static void RainbowCallback(SchedulerTask task) {
            index = (index + 1) % rainbowColors.Length;

            ColorDesc rainbow = Colors.ParseHex(rainbowColors[index]);
            rainbow.Code = 'r';

            ColorDesc pastel = Colors.ParseHex(pastelColors[index % pastelColors.Length]);
            pastel.Code = 'p';

            ColorDesc synthwave = Colors.ParseHex(synthwaveColors[index % synthwaveColors.Length]);
            synthwave.Code = 'v';

            ColorDesc sunset = Colors.ParseHex(sunsetColors[index % sunsetColors.Length]);
            sunset.Code = 's';

            ColorDesc galaxy = Colors.ParseHex(galaxyColors[index % galaxyColors.Length]);
            galaxy.Code = 'g';

            ColorDesc emerald = Colors.ParseHex(emeraldColors[index % emeraldColors.Length]);
            emerald.Code = 'm';

            ColorDesc diamond = Colors.ParseHex(diamondColors[index % diamondColors.Length]);
            diamond.Code = 'i';

            Player[] players = PlayerInfo.Online.Items;
            foreach (Player p in players) {
                p.Session.SendSetTextColor(rainbow);
                p.Session.SendSetTextColor(pastel);
                p.Session.SendSetTextColor(synthwave);
                p.Session.SendSetTextColor(sunset);
                p.Session.SendSetTextColor(galaxy);
                p.Session.SendSetTextColor(emerald);
                p.Session.SendSetTextColor(diamond);
            }
        }
    }
}
