using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Threading;

namespace FooBoxClient
{
    public partial class FormSettings : Form
    {
        private Thread _syncThread;
        private SyncEngine _engine;
        public FormSettings(Thread t, SyncEngine s)
        {
            InitializeComponent();
            _syncThread = t;
            _engine = s;
        }

        private void buttonBrowse_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            fbd.Description = "Select the directory to synchronise to.";
            fbd.RootFolder = Environment.SpecialFolder.Personal;
            DialogResult result = fbd.ShowDialog();
            if (result == DialogResult.OK)
            {
                textBoxDirLoc.Text = fbd.SelectedPath;
            }
            else
            {
                labelError.Text = "Invalid directory selected";
            }
        }

        private void buttonSave_Click(object sender, EventArgs e)
        {
            string location = textBoxDirLoc.Text.Trim();
            if (location != Properties.Settings.Default.Root)
            {
                if (Directory.Exists(location))
                {
                    //need to stop syncing change directory and resume syncing leaving this for now
                    
                    //copy files across 
                    foreach (string dirPath in Directory.GetDirectories(Properties.Settings.Default.Root, "*",
                        SearchOption.AllDirectories))
                        Directory.CreateDirectory(dirPath.Replace(Properties.Settings.Default.Root, location));

                    //Copy all the files & Replaces any files with the same name
                    foreach (string newPath in Directory.GetFiles(Properties.Settings.Default.Root, "*.*",
                        SearchOption.AllDirectories))
                        System.IO.File.Copy(newPath, newPath.Replace(Properties.Settings.Default.Root, location), true);

                    Properties.Settings.Default.Root = location;
                    Properties.Settings.Default.Save();
                }
            }
            this.Close();
        }

        private void FormSettings_Load(object sender, EventArgs e)
        {
            textBoxDirLoc.Text = Properties.Settings.Default.Root;
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
