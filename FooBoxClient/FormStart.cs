using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FooBoxClient
{
    public partial class FormStart : Form
    {
        public FormStart()
        {
            InitializeComponent();
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

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void buttonNext_Click(object sender, EventArgs e)
        {
            //do the stuff here
        }


    }
}
