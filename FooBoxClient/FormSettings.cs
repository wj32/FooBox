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
using FooBox;

namespace FooBoxClient
{
    public partial class FormSettings : Form
    {
        private object _syncObject;
        private SyncEngine _engine;

        public FormSettings(object syncObject, SyncEngine s)
        {
            InitializeComponent();
            _syncObject = syncObject;
            _engine = s;
        }

        private void buttonBrowse_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            fbd.Description = "Select the directory to synchronise to.";
            fbd.SelectedPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            DialogResult result = fbd.ShowDialog();
            if (result == DialogResult.OK)
            {
                textBoxDirLoc.Text = fbd.SelectedPath;
            }
        }

        private void buttonSave_Click(object sender, EventArgs e)
        {
            string location = textBoxDirLoc.Text.Trim();

            if (!Directory.Exists(location))
            {
                labelError.Text = "Invalid directory selected.";
                return;
            }

            location = Path.GetFullPath(location);

            if (location == Path.GetFullPath(Properties.Settings.Default.Root))
            {
                this.Close();
                return;
            }

            if (Directory.EnumerateFileSystemEntries(location).Any())
            {
                if (MessageBox.Show(
                    "The directory you have selected is not empty and will be erased. Do you want to continue?",
                    "FooBox",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning,
                    MessageBoxDefaultButton.Button2
                    ) == System.Windows.Forms.DialogResult.No)
                    return;
            }

            try
            {
                Utilities.DeleteDirectoryRecursive(location);
            }
            catch (Exception ex)
            {
                labelError.Text = "Unable to access the directory: " + ex.Message;
                return;
            }

            // Lock the sync object to pause syncing.
            lock (_syncObject)
            {
                Directory.Move(Properties.Settings.Default.Root, location);
                _engine.RootDirectory = location;
            }

            Properties.Settings.Default.Root = location;
            Properties.Settings.Default.Save();

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
