/*
Copyright 2010 MCSharp team (Modified for use with MCZall/MCLawl/MCForge)
Dual-licensed under the Educational Community License, Version 2.0 and
the GNU General Public License, Version 3 (the "Licenses"); you may
not use this file except in compliance with the Licenses. You may
obtain a copy of the Licenses at
https://opensource.org/license/ecl-2-0/
https://www.gnu.org/licenses/gpl-3.0.html
Unless required by applicable law or agreed to in writing,
software distributed under the Licenses are distributed on an "AS IS"
BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
or implied. See the Licenses for the specific language governing
permissions and limitations under the Licenses.
 */
using System;
using System.Drawing;
using System.Windows.Forms;
using MCGalaxy.Commands;
using MCGalaxy.Eco;
using MCGalaxy.Events.GameEvents;
using MCGalaxy.Games;
using MCGalaxy.Gui; // For ColorUtils

namespace MCGalaxy.Gui 
{
    public partial class PropertyWindow : Form 
    {
        ZombieProperties zsSettings = new ZombieProperties();

        // Plugin Library: Track which view is shown
        private bool showingCommandPlugins = false;
        private CheckBox chkShowCommandPlugins;

        public PropertyWindow() {
            InitializeComponent();
            zsSettings.LoadFromServer();
            propsZG.SelectedObject = zsSettings;

            // --- DARK MODE SUPPORT START ---
            bool dark = ColorUtils.LoadDarkMode();
            if (chkDarkMode != null) chkDarkMode.Checked = dark;
            ColorUtils.ApplyDarkMode(this, dark);
            if (chkDarkMode != null)
                chkDarkMode.CheckedChanged += chkDarkMode_CheckedChanged;
            // --- DARK MODE SUPPORT END ---

            // --- PLUGIN LIBRARY INIT ---
            InitPluginLibrary();
        }

        private void chkDarkMode_CheckedChanged(object sender, EventArgs e)
        {
            bool dark = chkDarkMode.Checked;
            ColorUtils.SaveDarkMode(dark);
            ColorUtils.ApplyDarkMode(this, dark);
            foreach (Form f in Application.OpenForms)
                ColorUtils.ApplyDarkMode(f, dark);
        }

        public void RunOnUI_Async(UIAction act) { BeginInvoke(act); }

        void PropertyWindow_Load(object sender, EventArgs e) {
            GuiUtils.SetIcon(this);
            OnMapsChangedEvent.Register(HandleMapsChanged, Priority.Low);
            OnStateChangedEvent.Register(HandleStateChanged, Priority.Low);
            GuiPerms.UpdateRanks();
            GuiPerms.SetRanks(blk_cmbMin);
            GuiPerms.SetRanks(cmd_cmbMin);
            LoadProperties();
            LoadRanks();
            try {
                LoadCommands();
                LoadBlocks();
            } catch (Exception ex) {
                Logger.LogError("Error loading commands and blocks", ex);
            }
            LoadGameProps();
        }

        void PropertyWindow_Unload(object sender, EventArgs e) {
            OnMapsChangedEvent.Unregister(HandleMapsChanged);
            OnStateChangedEvent.Unregister(HandleStateChanged);
            Window.hasPropsForm = false;
        }

        void LoadProperties() {
            SrvProperties.Load();
            LoadGeneralProps();
            LoadChatProps();
            LoadRelayProps();
            LoadSqlProps();
            LoadEcoProps();
            LoadMiscProps();
            LoadRankProps();
            LoadSecurityProps();
            zsSettings.LoadFromServer();
        }

        void SaveProperties() {
            try {
                ApplyGeneralProps();
                ApplyChatProps();
                ApplyRelayProps();
                ApplySqlProps();
                ApplyEcoProps();
                ApplyMiscProps();
                ApplyRankProps();
                ApplySecurityProps();
                zsSettings.ApplyToServer();
                SrvProperties.Save();
                Economy.Save();                
            } catch (Exception ex) {
                Logger.LogError(ex);
                Logger.Log(LogType.Warning, "SAVE FAILED! properties/server.properties");
            }
            SaveDiscordProps();
        }

        void btnSave_Click(object sender, EventArgs e) { SaveChanges(); Dispose(); }
        void btnApply_Click(object sender, EventArgs e) { SaveChanges(); }
        void SaveChanges() {
            SaveProperties();
            SaveRanks();
            SaveCommands();
            SaveBlocks();
            SaveGameProps();
            SrvProperties.ApplyChanges();
        }
        void btnDiscard_Click(object sender, EventArgs e) { Dispose(); }

        // --- PLUGIN LIBRARY METHODS START ---
        private void InitPluginLibrary()
        {
            chkShowCommandPlugins = new CheckBox();
            chkShowCommandPlugins.Text = "Show Commands";
            chkShowCommandPlugins.AutoSize = true;
            chkShowCommandPlugins.Location = new Point(10, 10);
            chkShowCommandPlugins.Checked = false;
            chkShowCommandPlugins.Parent = pagePluginStore; // or pageLibrary if you renamed
            chkShowCommandPlugins.BringToFront();
            chkShowCommandPlugins.CheckedChanged += new EventHandler(this.PluginLibraryViewChanged);

            panelPluginStore.Padding = new Padding(0, 30, 0, 0);
            ShowPluginLibraryView();
        }

        private void PluginLibraryViewChanged(object sender, EventArgs e)
        {
            showingCommandPlugins = chkShowCommandPlugins.Checked;
            ShowPluginLibraryView();
        }

        private void ShowPluginLibraryView()
        {
            panelPluginStore.Controls.Clear();
            System.Collections.Generic.List<PluginCardData> plugins;
            if (showingCommandPlugins)
                plugins = GetCommandPlugins();
            else
                plugins = GetNormalPlugins();

            for (int i = 0; i < plugins.Count; i++)
            {
                PluginCardData plugin = plugins[i];
                PictureBox pb = new PictureBox();
                pb.Width = 128;
                pb.Height = 80;
                pb.SizeMode = PictureBoxSizeMode.Zoom;
                pb.Cursor = Cursors.Hand;
                try { pb.Load(plugin.ThumbnailUrl); } catch { }

                Label lbl = new Label();
                lbl.Text = plugin.Title;
                lbl.Width = 128;
                lbl.Top = pb.Bottom + 2;
                lbl.TextAlign = ContentAlignment.TopCenter;
                lbl.Font = new Font("Calibri", 9, FontStyle.Bold);

                Label desc = new Label();
                desc.Text = plugin.Description;
                desc.Width = 128;
                desc.Top = lbl.Bottom + 2;
                desc.TextAlign = ContentAlignment.TopCenter;
                desc.Font = new Font("Calibri", 8);
                desc.ForeColor = Color.DimGray;
                desc.MaximumSize = new Size(128, 40);
                desc.AutoSize = false;
                desc.Height = 36;

                Label credits = new Label();
                credits.Text = "By: " + plugin.Credits;
                credits.Width = 128;
                credits.Top = desc.Bottom + 2;
                credits.TextAlign = ContentAlignment.TopCenter;
                credits.Font = new Font("Calibri", 7, FontStyle.Italic);
                credits.ForeColor = Color.Gray;
                credits.AutoSize = false;
                credits.Height = 16;

                Panel card = new Panel();
                card.Width = 140;
                card.Height = 190;

                string downloadPath = showingCommandPlugins
                    ? "extra/commands/source"
                    : "plugins";

                pb.Click += delegate(object sender2, EventArgs e2)
                {
                    DownloadPlugin(plugin.DownloadUrl, plugin.Title, downloadPath);
                };

                card.Controls.Add(pb);
                card.Controls.Add(lbl);
                card.Controls.Add(desc);
                card.Controls.Add(credits);

                lbl.Top = pb.Bottom + 2;
                desc.Top = lbl.Bottom + 2;
                credits.Top = desc.Bottom + 2;

                panelPluginStore.Controls.Add(card);
            }
        }

        private System.Collections.Generic.List<PluginCardData> GetNormalPlugins()
        {
            System.Collections.Generic.List<PluginCardData> plugins = new System.Collections.Generic.List<PluginCardData>();

PluginCardData plugin1 = new PluginCardData();
plugin1.Title = "AntiCheat";
plugin1.Description = "Detect Basic Modified Clients";
plugin1.Credits = "Blue_3dx";
plugin1.ThumbnailUrl = "https://raw.githubusercontent.com/Blue-3dx/MCGalaxy-/refs/heads/master/PluginStore/Thumbnails/AntiCheat.png";
plugin1.DownloadUrl = "https://github.com/Blue-3dx/MCGalaxy-/raw/master/PluginStore/AntiCheat.cs";
plugins.Add(plugin1);

PluginCardData plugin2 = new PluginCardData();
plugin2.Title = "Unable To Load";
plugin2.Description = "Unable To Load";
plugin2.Credits = "Unable To Load";
plugin2.ThumbnailUrl = "https://raw.githubusercontent.com/Blue-3dx/MCGalaxy-/refs/heads/master/PluginStore/Thumbnails/NoTexture.png";
plugin2.DownloadUrl = "https://github.com/Blue-3dx/MCGalaxy-/raw/master/PluginStore/SamplePlugin2.dll";
plugins.Add(plugin2);

PluginCardData plugin3 = new PluginCardData();
plugin3.Title = "Herobrine";
plugin3.Description = "Talk To Herobrine";
plugin3.Credits = "Blue_3dx";
plugin3.ThumbnailUrl = "https://raw.githubusercontent.com/Blue-3dx/MCGalaxy-/refs/heads/master/PluginStore/Thumbnails/NoTexture.png";
plugin3.DownloadUrl = "https://github.com/Blue-3dx/MCGalaxy-/raw/master/PluginStore/Herobrine.cs";
plugins.Add(plugin3);

PluginCardData plugin4 = new PluginCardData();
plugin4.Title = "Rainbow";
plugin4.Description = "Adds Rainbow Animated Text For The Chat And This Version Adds New Colors";
plugin4.Credits = "Blue 3dx (New version), UknownShadow200 (Original)";
plugin4.ThumbnailUrl = "https://raw.githubusercontent.com/Blue-3dx/MCGalaxy-/refs/heads/master/PluginStore/Thumbnails/NoTexture.png";
plugin4.DownloadUrl = "https://github.com/Blue-3dx/MCGalaxy-/raw/master/PluginStore/Rainbow.cs";
plugins.Add(plugin4);

PluginCardData plugin5 = new PluginCardData();
plugin5.Title = "Rate";
plugin5.Description = "Rate A Server With /Rate!";
plugin5.Credits = "Blue 3dx";
plugin5.ThumbnailUrl = "https://raw.githubusercontent.com/Blue-3dx/MCGalaxy-/refs/heads/master/PluginStore/Thumbnails/Rate.png";
plugin5.DownloadUrl = "https://github.com/Blue-3dx/MCGalaxy-/raw/master/PluginStore/Rate.cs";
plugins.Add(plugin5);

PluginCardData plugin6 = new PluginCardData();
plugin6.Title = "Sprint";
plugin6.Description = "Sprint Anywhere! Bye Bye Custom Motd (maxspeed=x) Now Non-Keyboard Users Can Sprint :D";
plugin6.Credits = "Blue 3dx";
plugin6.ThumbnailUrl = "https://raw.githubusercontent.com/Blue-3dx/MCGalaxy-/refs/heads/master/PluginStore/Thumbnails/Sprint.png";
plugin6.DownloadUrl = "https://github.com/Blue-3dx/MCGalaxy-/raw/master/PluginStore/Sprint.cs";
plugins.Add(plugin6);

PluginCardData plugin7 = new PluginCardData();
plugin7.Title = "Exp";
plugin7.Description = "Xp Plugin! With A Non Working Promotion System :( (not level up btw)";
plugin7.Credits = "Blue 3dx";
plugin7.ThumbnailUrl = "https://raw.githubusercontent.com/Blue-3dx/MCGalaxy-/refs/heads/master/PluginStore/Thumbnails/NoTexture.png";
plugin7.DownloadUrl = "https://github.com/Blue-3dx/MCGalaxy-/raw/master/PluginStore/exp.cs";
plugins.Add(plugin7);

            // Add more plugins as needed

            return plugins;
        }

        private System.Collections.Generic.List<PluginCardData> GetCommandPlugins()
        {
            System.Collections.Generic.List<PluginCardData> plugins = new System.Collections.Generic.List<PluginCardData>();

            PluginCardData cmdPlugin1 = new PluginCardData();
            cmdPlugin1.Title = "Cmdtransition";
            cmdPlugin1.Description = "Lets you add a Fade In+Out to your actions!";
            cmdPlugin1.Credits = "Blue-3dx";
            cmdPlugin1.ThumbnailUrl = "https://raw.githubusercontent.com/Blue-3dx/MCGalaxy-/refs/heads/master/PluginStore/Thumbnails/Transitions.png";
            cmdPlugin1.DownloadUrl = "https://github.com/Blue-3dx/MCGalaxy-/raw/master/PluginStore/CmdTransition.cs";
            plugins.Add(cmdPlugin1);

            PluginCardData cmdPlugin2 = new PluginCardData();
            cmdPlugin2.Title = "Unable To Load";
            cmdPlugin2.Description = "Unable To Load";
            cmdPlugin2.Credits = "Unable To Load";
            cmdPlugin2.ThumbnailUrl = "https://raw.githubusercontent.com/Blue-3dx/MCGalaxy-/refs/heads/master/PluginStore/Thumbnails/NoTexture.png";
            cmdPlugin2.DownloadUrl = "https://github.com/Blue-3dx/MCGalaxy-/raw/master/PluginStore/CommandPlugin2.dll";
            plugins.Add(cmdPlugin2);

            // Add more command plugins as needed

            return plugins;
        }

        private void DownloadPlugin(string url, string title, string destSubDir)
        {
            System.Net.WebClient wc = new System.Net.WebClient();
            string destDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, destSubDir.Replace('/', System.IO.Path.DirectorySeparatorChar));
            System.IO.Directory.CreateDirectory(destDir);
            string ext = System.IO.Path.GetExtension(url);
            string dest = System.IO.Path.Combine(destDir, title + ext);
            try
            {
                wc.DownloadFile(url, dest);
                MessageBox.Show("Downloaded " + title + "!", "Plugin Library");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Download failed: " + ex.Message, "Plugin Library");
            }
            wc.Dispose();
        }
        // --- PLUGIN LIBRARY METHODS END ---

        void GetHelp(string toHelp) {
            ConsoleHelpPlayer p = new ConsoleHelpPlayer();
            Command.Find("Help").Use(p, toHelp);
            Popup.Message(Colors.StripUsed(p.Messages), "Help for /" + toHelp);
        }
    }
    
    sealed class ConsoleHelpPlayer : Player {
        public string Messages = "";
        public ConsoleHelpPlayer() : base("(console)") {
            group = Group.ConsoleRank;
            SuperName = "Console";
        }
        public override void Message(string message) {
            message = Chat.Format(message, this);
            Messages += message + "\r\n";
        }
    }

    public class PluginCardData
    {
        public string Title;
        public string Description;
        public string Credits;
        public string ThumbnailUrl;
        public string DownloadUrl;
    }
}