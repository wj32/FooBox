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
namespace FooBoxClient
{
    public partial class FormSysTray : Form
    {
        private string _syncLocation;

        public FormSysTray()
        {
            InitializeComponent();
        }

        public FormSysTray(string syncLocation)
        {
            InitializeComponent();
            _syncLocation = syncLocation;
        }

        private void FormSysTray_FormClosed(object sender, FormClosedEventArgs e)
        {
           // Application.Exit();
        }
    }
}
