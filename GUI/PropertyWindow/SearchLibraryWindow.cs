using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace MCGalaxy.Gui
{
    public class SearchLibraryWindow : Form
    {
        private TextBox txtSearch;
        private Panel panelResults;

        // Hardcoded library data for the example
        private List<LibraryInfo> allLibraries = new List<LibraryInfo>
        {
            new LibraryInfo {
                Title = "Essentials",
                Description = "A collection of must-have plugins for every server.",
                ThumbnailUrl = "https://raw.githubusercontent.com/Blue-3dx/MCGalaxy-/refs/heads/master/PluginStore/Thumbnails/AntiCheat.png",
                DownloadUrl = "https://example.com/externallibraries/essentials.json",
                JsonFileName = "essentials.json"
            },
            new LibraryInfo {
                Title = "FunPack",
                Description = "Fun and games plugins for your players.",
                ThumbnailUrl = "https://raw.githubusercontent.com/Blue-3dx/MCGalaxy-/refs/heads/master/PluginStore/Thumbnails/Herobrine.png",
                DownloadUrl = "https://example.com/externallibraries/funpack.json",
                JsonFileName = "funpack.json"
            },
            new LibraryInfo {
                Title = "AdminTools",
                Description = "Admin utilities to keep your server safe.",
                ThumbnailUrl = "https://raw.githubusercontent.com/Blue-3dx/MCGalaxy-/refs/heads/master/PluginStore/Thumbnails/NoTexture.png",
                DownloadUrl = "https://example.com/externallibraries/admintools.json",
                JsonFileName = "admintools.json"
            },
            new LibraryInfo {
                Title = "MiniGames",
                Description = "A set of mini-games for community fun.",
                ThumbnailUrl = "https://raw.githubusercontent.com/Blue-3dx/MCGalaxy-/refs/heads/master/PluginStore/Thumbnails/NoTexture.png",
                DownloadUrl = "https://example.com/externallibraries/minigames.json",
                JsonFileName = "minigames.json"
            },
            new LibraryInfo {
                Title = "EconomyPlus",
                Description = "Advanced economy plugins and shop systems.",
                ThumbnailUrl = "https://raw.githubusercontent.com/Blue-3dx/MCGalaxy-/refs/heads/master/PluginStore/Thumbnails/NoTexture.png",
                DownloadUrl = "https://example.com/externallibraries/economyplus.json",
                JsonFileName = "economyplus.json"
            },
            new LibraryInfo {
                Title = "RoleplayKit",
                Description = "Plugins for roleplay and immersive experiences.",
                ThumbnailUrl = "https://raw.githubusercontent.com/Blue-3dx/MCGalaxy-/refs/heads/master/PluginStore/Thumbnails/NoTexture.png",
                DownloadUrl = "https://example.com/externallibraries/roleplaykit.json",
                JsonFileName = "roleplaykit.json"
            }
        };

        public SearchLibraryWindow()
        {
            this.Text = "Search for a Library";
            this.Size = new Size(700, 500);
            this.StartPosition = FormStartPosition.CenterParent;

            Label lblSearch = new Label();
            lblSearch.Text = "Search:";
            lblSearch.Font = new Font("Calibri", 12, FontStyle.Bold);
            lblSearch.Location = new Point(20, 18);
            lblSearch.Size = new Size(60, 22);
            this.Controls.Add(lblSearch);

            txtSearch = new TextBox();
            txtSearch.Font = new Font("Calibri", 12);
            txtSearch.Location = new Point(90, 15);
            txtSearch.Size = new Size(400, 28);
            txtSearch.TextChanged += TxtSearch_TextChanged;
            this.Controls.Add(txtSearch);

            panelResults = new Panel();
            panelResults.Location = new Point(0, 50);
            panelResults.Size = new Size(this.ClientSize.Width - 20, this.ClientSize.Height - 60);
            panelResults.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            panelResults.AutoScroll = true;
            this.Controls.Add(panelResults);

            this.Resize += (s, e) => {
                panelResults.Size = new Size(this.ClientSize.Width - 20, this.ClientSize.Height - 60);
            };

            ShowSearchResults("");
        }

        private void TxtSearch_TextChanged(object sender, EventArgs e)
        {
            ShowSearchResults(txtSearch.Text.Trim());
        }

        private void ShowSearchResults(string filter)
        {
            panelResults.Controls.Clear();
            List<LibraryInfo> match = allLibraries;
            if (!string.IsNullOrEmpty(filter))
            {
                string f = filter.ToLower();
                match = allLibraries.FindAll(
                    l => (l.Title != null && l.Title.ToLower().Contains(f)) ||
                         (l.Description != null && l.Description.ToLower().Contains(f)));
            }

            int y = 10;
            foreach (var lib in match)
            {
                Panel card = new Panel();
                card.Width = 620;
                card.Height = 90;
                card.Left = 30;
                card.Top = y;
                card.BorderStyle = BorderStyle.FixedSingle;

                PictureBox pb = new PictureBox();
                pb.Width = 64;
                pb.Height = 64;
                pb.Location = new Point(10, 10);
                pb.SizeMode = PictureBoxSizeMode.Zoom;
                try { pb.Load(lib.ThumbnailUrl); } catch { }

                Label lblTitle = new Label();
                lblTitle.Text = lib.Title;
                lblTitle.Font = new Font("Calibri", 12, FontStyle.Bold);
                lblTitle.Location = new Point(80, 10);
                lblTitle.Size = new Size(300, 20);

                Label lblDesc = new Label();
                lblDesc.Text = lib.Description;
                lblDesc.Font = new Font("Calibri", 10);
                lblDesc.Location = new Point(80, 35);
                lblDesc.Size = new Size(350, 40);

                Button btnDownload = new Button();
                btnDownload.Text = "Download";
                btnDownload.Size = new Size(100, 32);
                btnDownload.Location = new Point(480, 28);
                btnDownload.Click += (s, e) => DownloadLibraryJson(lib.DownloadUrl, lib.JsonFileName);

                card.Controls.Add(pb);
                card.Controls.Add(lblTitle);
                card.Controls.Add(lblDesc);
                card.Controls.Add(btnDownload);

                panelResults.Controls.Add(card);

                y += 100;
            }

            if (match.Count == 0)
            {
                Label lblNo = new Label();
                lblNo.Text = "No libraries found for your search.";
                lblNo.Location = new Point(50, 30);
                lblNo.Font = new Font("Calibri", 11, FontStyle.Italic);
                lblNo.ForeColor = Color.Gray;
                lblNo.AutoSize = true;
                panelResults.Controls.Add(lblNo);
            }
        }

        private void DownloadLibraryJson(string url, string fileName)
        {
            string rootDir = AppDomain.CurrentDomain.BaseDirectory;
            string targetDir = System.IO.Path.Combine(rootDir, "pluginlibrarydata", "externallibraries");
            System.IO.Directory.CreateDirectory(targetDir);
            string targetPath = System.IO.Path.Combine(targetDir, fileName);

            try
            {
                using (var wc = new System.Net.WebClient())
                {
                    wc.DownloadFile(url, targetPath);
                }
                MessageBox.Show("Library downloaded as " + fileName + "!", "Library Download");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Download failed: " + ex.Message, "Library Download");
            }
        }

        private class LibraryInfo
        {
            public string Title;
            public string Description;
            public string ThumbnailUrl;
            public string DownloadUrl;
            public string JsonFileName;
        }
    }
}