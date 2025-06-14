using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace MCGalaxy.Gui
{
    public class ExternalLibraryViewerWindow : Form
    {
        private List<PluginCardData> allPlugins;
        private List<PluginCardData> filteredPlugins;
        private int pluginPage = 0;
        private const int pluginsPerPage = 6;
        private Button btnNextPage, btnPrevPage;
        private Label lblPageIndicator, lblTitle;
        private CheckBox chkShowCommandPlugins;

        public ExternalLibraryViewerWindow(List<PluginCardData> plugins, string libraryName)
        {
            this.allPlugins = plugins ?? new List<PluginCardData>();
            this.Text = "External Library: " + libraryName;
            this.Size = new Size(700, 500);
            this.StartPosition = FormStartPosition.CenterParent;
            this.MinimumSize = new Size(500, 300);

            lblTitle = new Label();
            lblTitle.Text = "Library: " + libraryName;
            lblTitle.Font = new Font("Calibri", 14, FontStyle.Bold);
            lblTitle.Left = 10;
            lblTitle.Top = 10;
            lblTitle.Width = 600;
            this.Controls.Add(lblTitle);

            chkShowCommandPlugins = new CheckBox();
            chkShowCommandPlugins.Text = "Show Commands";
            chkShowCommandPlugins.Left = 10;
            chkShowCommandPlugins.Top = lblTitle.Bottom + 8;
            chkShowCommandPlugins.Checked = false;
            chkShowCommandPlugins.CheckedChanged += (s, e) => { pluginPage = 0; RefreshFilter(); };
            this.Controls.Add(chkShowCommandPlugins);

            btnPrevPage = new Button();
            btnPrevPage.Text = "Previous";
            btnPrevPage.Width = 90;
            btnPrevPage.Height = 28;
            btnPrevPage.Left = 10;
            btnPrevPage.Top = this.Height - 80;
            btnPrevPage.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnPrevPage.Click += BtnPrevPage_Click;
            this.Controls.Add(btnPrevPage);

            btnNextPage = new Button();
            btnNextPage.Text = "Next";
            btnNextPage.Width = 90;
            btnNextPage.Height = 28;
            btnNextPage.Left = 110;
            btnNextPage.Top = this.Height - 80;
            btnNextPage.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnNextPage.Click += BtnNextPage_Click;
            this.Controls.Add(btnNextPage);

            lblPageIndicator = new Label();
            lblPageIndicator.Width = 160;
            lblPageIndicator.Height = 25;
            lblPageIndicator.Left = 220;
            lblPageIndicator.Top = this.Height - 75;
            lblPageIndicator.TextAlign = ContentAlignment.MiddleLeft;
            lblPageIndicator.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            this.Controls.Add(lblPageIndicator);

            this.Padding = new Padding(0, 40, 0, 0);
            this.Resize += (s, e) => SafeShowPluginLibraryView();
            RefreshFilter();
        }

        private void RefreshFilter()
        {
            // Filter plugins by checkbox
            if (chkShowCommandPlugins.Checked)
                filteredPlugins = allPlugins.FindAll(p => p.Command == true);
            else
                filteredPlugins = allPlugins.FindAll(p => p.Command == false);
            SafeShowPluginLibraryView();
        }

        private void BtnPrevPage_Click(object sender, EventArgs e)
        {
            if (pluginPage > 0)
            {
                pluginPage--;
                SafeShowPluginLibraryView();
            }
        }

        private void BtnNextPage_Click(object sender, EventArgs e)
        {
            int maxPage = (filteredPlugins.Count - 1) / pluginsPerPage;
            if (pluginPage < maxPage)
            {
                pluginPage++;
                SafeShowPluginLibraryView();
            }
        }

        private void SafeShowPluginLibraryView()
        {
            try { ShowPluginLibraryView(); }
            catch (Exception ex)
            {
                MessageBox.Show("View error: " + ex.ToString(), "Error");
            }
        }

        private void ShowPluginLibraryView()
        {
            for (int i = this.Controls.Count - 1; i >= 0; i--)
            {
                Control c = this.Controls[i];
                if (c != lblTitle && c != btnNextPage && c != btnPrevPage && c != lblPageIndicator && c != chkShowCommandPlugins)
                    this.Controls.RemoveAt(i);
            }

            int maxPage = (filteredPlugins.Count - 1) / pluginsPerPage;
            if (pluginPage > maxPage) pluginPage = maxPage;
            if (pluginPage < 0) pluginPage = 0;

            lblPageIndicator.Text = string.Format("Page {0} of {1}", pluginPage + 1, maxPage + 1);

            int total = filteredPlugins.Count - pluginPage * pluginsPerPage;
            int cardsInThisPage = Math.Max(0, Math.Min(pluginsPerPage, total));

            if (cardsInThisPage == 0)
            {
                Label empty = new Label();
                empty.Text = "No plugins in this library!";
                empty.Left = 40;
                empty.Top = 80;
                empty.Width = 300;
                this.Controls.Add(empty);
                return;
            }

            int x0 = 20, y0 = chkShowCommandPlugins.Bottom + 10, dx = 150, dy = 200;
            int perRow = 3;

            for (int i = 0; i < cardsInThisPage; i++)
            {
                int idx = pluginPage * pluginsPerPage + i;
                PluginCardData plugin = filteredPlugins[idx];
                int col = i % perRow, row = i / perRow;

                Panel card = new Panel();
                card.Width = 140;
                card.Height = 190;
                card.Left = x0 + col * dx;
                card.Top = y0 + row * dy;

                PictureBox pb = new PictureBox();
                pb.Width = 128;
                pb.Height = 80;
                pb.SizeMode = PictureBoxSizeMode.Zoom;
                pb.Cursor = Cursors.Hand;
                pb.Image = SystemIcons.Application.ToBitmap(); // fallback

                if (plugin != null && !string.IsNullOrEmpty(plugin.ThumbnailUrl))
                {
                    try { pb.Load(plugin.ThumbnailUrl); }
                    catch { pb.Image = SystemIcons.Application.ToBitmap(); }
                }

                Label lbl = new Label();
                lbl.Text = plugin != null && plugin.Title != null ? plugin.Title : "<no title>";
                lbl.Width = 128;
                lbl.Top = pb.Bottom + 2;
                lbl.TextAlign = ContentAlignment.TopCenter;
                lbl.Font = new Font("Calibri", 9, FontStyle.Bold);

                Label desc = new Label();
                desc.Text = plugin != null && plugin.Description != null ? plugin.Description : "";
                desc.Width = 128;
                desc.Top = lbl.Bottom + 2;
                desc.TextAlign = ContentAlignment.TopCenter;
                desc.Font = new Font("Calibri", 8);
                desc.ForeColor = Color.DimGray;
                desc.MaximumSize = new Size(128, 40);
                desc.AutoSize = false;
                desc.Height = 36;

                Label credits = new Label();
                credits.Text = "By: " + (plugin != null && plugin.Credits != null ? plugin.Credits : "");
                credits.Width = 128;
                credits.Top = desc.Bottom + 2;
                credits.TextAlign = ContentAlignment.TopCenter;
                credits.Font = new Font("Calibri", 7, FontStyle.Italic);
                credits.ForeColor = Color.Gray;
                credits.AutoSize = false;
                credits.Height = 16;

                string downloadPath = plugin.Command
                    ? "extra/commands/source"
                    : "plugins";

                pb.Click += delegate(object sender2, EventArgs e2)
                {
                    try
                    {
                        if (plugin != null && !string.IsNullOrEmpty(plugin.DownloadUrl))
                        {
                            using (var wc = new System.Net.WebClient())
                            {
                                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                                string destDir = System.IO.Path.Combine(baseDir, downloadPath.Replace('/', System.IO.Path.DirectorySeparatorChar));
                                System.IO.Directory.CreateDirectory(destDir);
                                string ext = System.IO.Path.GetExtension(plugin.DownloadUrl);
                                string dest = System.IO.Path.Combine(destDir, plugin.Title + ext);
                                wc.DownloadFile(plugin.DownloadUrl, dest);
                                MessageBox.Show("Downloaded " + plugin.Title + "!", "Plugin Library");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Failed to open download: " + ex.ToString(), "Error");
                    }
                };

                card.Controls.Add(pb);
                card.Controls.Add(lbl);
                card.Controls.Add(desc);
                card.Controls.Add(credits);

                lbl.Top = pb.Bottom + 2;
                desc.Top = lbl.Bottom + 2;
                credits.Top = desc.Bottom + 2;

                this.Controls.Add(card);
            }

            btnPrevPage.Enabled = (pluginPage > 0);
            btnNextPage.Enabled = (pluginPage < maxPage);
        }
    }
}