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

namespace MCGalaxy.Gui 
{
    public partial class PropertyWindow : Form 
    {
        ZombieProperties zsSettings = new ZombieProperties();
        
        public PropertyWindow() {
            InitializeComponent();
            zsSettings.LoadFromServer();
            propsZG.SelectedObject = zsSettings;

            // Wire up dark mode checkbox event handler, if checkbox exists
            if (this.Controls.Find("chkDarkMode", true).Length > 0)
            {
                var chk = this.Controls.Find("chkDarkMode", true)[0] as CheckBox;
                chk.CheckedChanged += chkDarkMode_CheckedChanged;
                // Apply dark mode initially to match checkbox
                MCGalaxy.Gui.ColorUtils.ApplyDarkMode(this, chk.Checked);
            }
        }
        
        // Add the dark mode event handler
        private void chkDarkMode_CheckedChanged(object sender, EventArgs e)
        {
            var chk = sender as CheckBox;
            if (chk != null)
            {
                MCGalaxy.Gui.ColorUtils.ApplyDarkMode(this, chk.Checked);
            }
        }

        public void RunOnUI_Async(UIAction act) { BeginInvoke(act); }

        void PropertyWindow_Load(object sender, EventArgs e) {
            // try to use same icon as main window
            // must be done in OnLoad, otherwise icon doesn't show on Mono
            GuiUtils.SetIcon(this);
            
            OnMapsChangedEvent.Register(HandleMapsChanged, Priority.Low);
            OnStateChangedEvent.Register(HandleStateChanged, Priority.Low);
            GuiPerms.UpdateRanks();

            GuiPerms.SetRanks(blk_cmbMin);
            GuiPerms.SetRanks(cmd_cmbMin);

            //Load server stuff
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
}