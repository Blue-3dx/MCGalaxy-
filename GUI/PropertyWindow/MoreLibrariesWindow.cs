using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Collections.Generic;
using Newtonsoft.Json; // If you use Newtonsoft for XML, else adjust accordingly

namespace MCGalaxy.Gui
{
    public class MoreLibrariesWindow : Form
    {
        // Example hardcoded recommended libraries data structure
        private class LibraryInfo
        {
            public string Title;
            public string Description;
            public string ThumbnailUrl;
            public string DownloadUrl;
            public string JsonFileName; // for download
        }

        private readonly List<LibraryInfo> recommendedLibraries = new List<LibraryInfo>
        {
            new LibraryInfo {
                Title = "Essentials",
                Description = "A collection of must-have plugins for every server.",
                ThumbnailUrl = "https://raw.githubusercontent.com/Blue-3dx/MCGalaxy-/refs/heads/master/PluginStore/Thumbnails/AntiCheat.png",
                DownloadUrl = "https://example.com/externallibraries/essentials.xml",
                JsonFileName = "essentials.xml"
            },
            new LibraryInfo {
                Title = "FunPack",
                Description = "Fun and games plugins for your players.",
                ThumbnailUrl = "https://raw.githubusercontent.com/Blue-3dx/MCGalaxy-/refs/heads/master/PluginStore/Thumbnails/Herobrine.png",
                DownloadUrl = "https://example.com/externallibraries/funpack.xml",
                JsonFileName = "funpack.xml"
            },
            new LibraryInfo {
                Title = "AdminTools",
                Description = "Admin utilities to keep your server safe.",
                ThumbnailUrl = "https://raw.githubusercontent.com/Blue-3dx/MCGalaxy-/refs/heads/master/PluginStore/Thumbnails/NoTexture.png",
                DownloadUrl = "https://example.com/externallibraries/admintools.xml",
                JsonFileName = "admintools.xml"
            },
            new LibraryInfo {
                Title = "MiniGames",
                Description = "A set of mini-games for community fun.",
                ThumbnailUrl = "https://raw.githubusercontent.com/Blue-3dx/MCGalaxy-/refs/heads/master/PluginStore/Thumbnails/NoTexture.png",
                DownloadUrl = "https://example.com/externallibraries/minigames.xml",
                JsonFileName = "minigames.xml"
            },
            new LibraryInfo {
                Title = "EconomyPlus",
                Description = "Advanced economy plugins and shop systems.",
                ThumbnailUrl = "https://raw.githubusercontent.com/Blue-3dx/MCGalaxy-/refs/heads/master/PluginStore/Thumbnails/NoTexture.png",
                DownloadUrl = "https://example.com/externallibraries/economyplus.xml",
                JsonFileName = "economyplus.xml"
            },
            new LibraryInfo {
                Title = "RoleplayKit",
                Description = "Plugins for roleplay and immersive experiences.",
                ThumbnailUrl = "https://raw.githubusercontent.com/Blue-3dx/MCGalaxy-/refs/heads/master/PluginStore/Thumbnails/NoTexture.png",
                DownloadUrl = "https://example.com/externallibraries/roleplaykit.xml",
                JsonFileName = "roleplaykit.xml"
            }
        };

        private Panel panelCards;
        private Button btnAddLibrary, btnSearchLibrary, btnShowDownloaded;

        public MoreLibrariesWindow()
        {
            this.Text = "More Libraries";
            this.Size = new Size(700, 500);
            this.StartPosition = FormStartPosition.CenterParent;

            // Title label at the top-center
            Label lblTitle = new Label();
            lblTitle.Text = "Recommended Libraries";
            lblTitle.Font = new Font("Calibri", 16, FontStyle.Bold);
            lblTitle.AutoSize = false;
            lblTitle.TextAlign = ContentAlignment.MiddleCenter;
            lblTitle.Dock = DockStyle.Top;
            lblTitle.Height = 40;
            this.Controls.Add(lblTitle);

            // Panel for library cards
            panelCards = new Panel();
            panelCards.Location = new Point(0, 50);
            panelCards.Size = new Size(this.ClientSize.Width, 320);
            panelCards.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            panelCards.AutoScroll = true;
            this.Controls.Add(panelCards);

            CreateRecommendedLibraryCards();

            // Add a Library button
            btnAddLibrary = new Button();
            btnAddLibrary.Text = "Add a Library";
            btnAddLibrary.Size = new Size(120, 28);
            btnAddLibrary.Location = new Point(20, this.ClientSize.Height - 48);
            btnAddLibrary.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
            btnAddLibrary.Click += BtnAddLibrary_Click;
            this.Controls.Add(btnAddLibrary);

            // Search for a Library button
            btnSearchLibrary = new Button();
            btnSearchLibrary.Text = "Search for a Library";
            btnSearchLibrary.Size = new Size(150, 28);
            btnSearchLibrary.Location = new Point(btnAddLibrary.Right + 20, btnAddLibrary.Top);
            btnSearchLibrary.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
            btnSearchLibrary.Click += BtnSearchLibrary_Click;
            this.Controls.Add(btnSearchLibrary);

            // Show My Downloaded Libraries button
            btnShowDownloaded = new Button();
            btnShowDownloaded.Text = "Show My Downloaded Libraries";
            btnShowDownloaded.Size = new Size(220, 28);
            btnShowDownloaded.Location = new Point(btnSearchLibrary.Right + 20, btnAddLibrary.Top);
            btnShowDownloaded.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
            btnShowDownloaded.Click += BtnShowDownloaded_Click;
            this.Controls.Add(btnShowDownloaded);

            // Make sure resizing doesn't break layout
            this.Resize += (s, e) => 
            {
                panelCards.Size = new Size(this.ClientSize.Width - 10, this.ClientSize.Height - 120);
                btnAddLibrary.Location = new Point(20, this.ClientSize.Height - 48);
                btnSearchLibrary.Location = new Point(btnAddLibrary.Right + 20, btnAddLibrary.Top);
                btnShowDownloaded.Location = new Point(btnSearchLibrary.Right + 20, btnAddLibrary.Top);
            };
        }

        private void CreateRecommendedLibraryCards()
        {
            panelCards.Controls.Clear();
            int x0 = 30, y0 = 20, dx = 210, dy = 140;
            int perRow = 3;
            for (int i = 0; i < recommendedLibraries.Count; i++)
            {
                var lib = recommendedLibraries[i];
                int col = i % perRow, row = i / perRow;

                Panel card = new Panel();
                card.Width = 200;
                card.Height = 130;
                card.Left = x0 + col * dx;
                card.Top = y0 + row * dy;
                card.BorderStyle = BorderStyle.FixedSingle;

                PictureBox pb = new PictureBox();
                pb.Width = 64;
                pb.Height = 64;
                pb.Location = new Point(10, 10);
                pb.SizeMode = PictureBoxSizeMode.Zoom;
                try { pb.Load(lib.ThumbnailUrl); } catch { }

                Label lblTitle = new Label();
                lblTitle.Text = lib.Title;
                lblTitle.Font = new Font("Calibri", 11, FontStyle.Bold);
                lblTitle.Location = new Point(80, 10);
                lblTitle.Size = new Size(110, 18);

                Label lblDesc = new Label();
                lblDesc.Text = lib.Description;
                lblDesc.Font = new Font("Calibri", 9);
                lblDesc.Location = new Point(80, 30);
                lblDesc.Size = new Size(110, 38);
                lblDesc.AutoEllipsis = true;

                Button btnDownload = new Button();
                btnDownload.Text = "Download";
                btnDownload.Size = new Size(80, 26);
                btnDownload.Location = new Point(110, 80);
                btnDownload.Click += (s, e) => DownloadLibraryJson(lib.DownloadUrl, lib.JsonFileName);

                card.Controls.Add(pb);
                card.Controls.Add(lblTitle);
                card.Controls.Add(lblDesc);
                card.Controls.Add(btnDownload);

                panelCards.Controls.Add(card);
            }
        }

        private void DownloadLibraryJson(string url, string fileName)
        {
            string rootDir = AppDomain.CurrentDomain.BaseDirectory;
            string targetDir = Path.Combine(rootDir, "pluginlibrarydata", "externallibraries");
            Directory.CreateDirectory(targetDir);
            string targetPath = Path.Combine(targetDir, fileName);

            try
            {
                using (var wc = new System.Net.WebClient())
                {
                    wc.DownloadFile(url, targetPath);
                }
                MessageBox.Show("Library downloaded as {fileName}!", "Library Download");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Download failed: " + ex.Message, "Library Download");
            }
        }

        private void BtnAddLibrary_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "XML Library Files (*.xml)|*.xml";
            ofd.Title = "Select a .xml library file to add";
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                string selectedFile = ofd.FileName;
                // Prompt for new name
                InputBox("Choose a name for the new library XML (without extension):", "Name Library", Path.GetFileNameWithoutExtension(selectedFile), (newName) =>
                {
                    if (string.IsNullOrWhiteSpace(newName)) return;
                    string rootDir = AppDomain.CurrentDomain.BaseDirectory;
                    string targetDir = Path.Combine(rootDir, "pluginlibrarydata", "externallibraries");
                    Directory.CreateDirectory(targetDir);
                    string targetPath = Path.Combine(targetDir, newName + ".xml");
                    try
                    {
                        File.Copy(selectedFile, targetPath, true);
                        MessageBox.Show("Library added!", "Add Library");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Could not add: " + ex.Message, "Add Library");
                    }
                });
            }
        }

        private void BtnSearchLibrary_Click(object sender, EventArgs e)
        {
            var win = new SearchLibraryWindow();
            win.Show(this);
        }

        private void BtnShowDownloaded_Click(object sender, EventArgs e)
        {
            var win = new DownloadedLibrariesWindow();
            win.Show(this);
        }

        // Simple input box for getting a string value from user, calls onOk if user accepts (thread safe for this use)
        private void InputBox(string prompt, string title, string defaultValue, Action<string> onOk)
        {
            Form promptForm = new Form()
            {
                Width = 380,
                Height = 170,
                Text = title,
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MinimizeBox = false,
                MaximizeBox = false
            };
            Label lbl = new Label() { Left = 20, Top = 18, Width = 320, Text = prompt };
            TextBox textBox = new TextBox() { Left = 20, Top = 48, Width = 320, Text = defaultValue };
            Button btnOk = new Button() { Text = "OK", Left = 170, Width = 80, Top = 90, DialogResult = DialogResult.OK };
            Button btnCancel = new Button() { Text = "Cancel", Left = 260, Width = 80, Top = 90, DialogResult = DialogResult.Cancel };
            promptForm.Controls.Add(lbl);
            promptForm.Controls.Add(textBox);
            promptForm.Controls.Add(btnOk);
            promptForm.Controls.Add(btnCancel);
            promptForm.AcceptButton = btnOk;
            promptForm.CancelButton = btnCancel;
            if (promptForm.ShowDialog() == DialogResult.OK)
            {
                onOk(textBox.Text);
            }
        }
    }
}