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
using System.Net;
using System.Runtime.Serialization.Json;
using System.Timers;
using FooBox.Common;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;
namespace FooBoxClient
{
    public partial class FormSysTray : Form
    {
        private SyncEngine _engine;

        public FormSysTray()
        {
            InitializeComponent();

            _engine = new SyncEngine(Properties.Settings.Default.Root);
        }

        private void FormSysTray_Load(object sender, EventArgs e)
        {
            try
            {
                _engine.LoadState();
            }
            catch
            { }

            var result = Requests.Sync(new ClientSyncData { BaseChangelistId = 0 });

            _engine.Apply(result.Changes);
            _engine.ChangelistId = result.NewChangelistId;
            _engine.SaveState();
        }
    }
}
