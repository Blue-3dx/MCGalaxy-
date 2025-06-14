using System;
using System.IO;
using System.Windows.Forms;
using System.Drawing;
using System.Collections.Generic;

namespace MCGalaxy.Gui
{
    public class DownloadedLibrariesWindow : Form
    {
        private ListView listView;
        private Button btnOpen, btnRefresh;
        private Label lblInfo;

        public DownloadedLibrariesWindow()
        {
            this.Text = "My Downloaded Libraries";
            this.Size = new Size(650, 420);
            this.StartPosition = FormStartPosition.CenterParent;

            listView = new ListView();
            listView.View = View.Details;
            listView.FullRowSelect = true;
            listView.GridLines = true;
            listView.Columns.Add("Library Name", 220);
            listView.Columns.Add("File", 350);
            listView.Dock = DockStyle.Top;
            listView.Height = 260;

            btnOpen = new Button();
            btnOpen.Text = "Open";
            btnOpen.Width = 90;
            btnOpen.Top = listView.Bottom + 10;
            btnOpen.Left = 10;
            btnOpen.Click += BtnOpen_Click;

            btnRefresh = new Button();
            btnRefresh.Text = "Refresh";
            btnRefresh.Width = 90;
            btnRefresh.Top = listView.Bottom + 10;
            btnRefresh.Left = 110;
            btnRefresh.Click += (s, e) => LoadLibraries();

            lblInfo = new Label();
            lblInfo.Text = "Double-click or select then click Open to view a downloaded plugin library.";
            lblInfo.Top = btnOpen.Top + 40;
            lblInfo.Left = 10;
            lblInfo.AutoSize = true;

            this.Controls.Add(listView);
            this.Controls.Add(btnOpen);
            this.Controls.Add(btnRefresh);
            this.Controls.Add(lblInfo);

            listView.DoubleClick += BtnOpen_Click;
            LoadLibraries();
        }

        private void LoadLibraries()
        {
            listView.Items.Clear();
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string libsDir = Path.Combine(baseDir, "pluginlibrarydata", "externallibraries");

            if (!Directory.Exists(libsDir))
            {
                Directory.CreateDirectory(libsDir);
            }

            var files = Directory.GetFiles(libsDir, "*.xml");
            foreach (var file in files)
            {
                string name = Path.GetFileNameWithoutExtension(file);
                var item = new ListViewItem(name);
                item.SubItems.Add(file);
                listView.Items.Add(item);
            }

            if (listView.Items.Count == 0)
            {
                var item = new ListViewItem("No external libraries downloaded yet!");
                item.SubItems.Add("");
                listView.Items.Add(item);
            }
        }

        private void BtnOpen_Click(object sender, EventArgs e)
        {
            if (listView.SelectedItems.Count != 1) return;
            var item = listView.SelectedItems[0];
            string file = item.SubItems[1].Text;
            if (!File.Exists(file)) return;

            try
            {
                var plugins = PluginLibraryLoader.LoadFromXml(file);
                if (plugins == null || plugins.Count == 0)
                {
                    MessageBox.Show("Library file is empty or invalid format.", "Invalid Data");
                    return;
                }

                var viewer = new ExternalLibraryViewerWindow(plugins, Path.GetFileNameWithoutExtension(file));
                viewer.ShowDialog(this);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load or parse library:\n" + ex.ToString(), "Error");
            }
        }
    }
}