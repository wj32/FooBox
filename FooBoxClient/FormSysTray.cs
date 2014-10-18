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
using System.Threading;

namespace FooBoxClient
{
    public partial class FormSysTray : Form
    {
        private SyncEngine _engine;
        private Thread _syncThread;
        private AutoResetEvent _event;
        private bool _closing;

        public FormSysTray()
        {
            InitializeComponent();

            _engine = new SyncEngine(Properties.Settings.Default.Root);
            _syncThread = new Thread(this.SyncThreadStart);
            _event = new AutoResetEvent(false);
        }

        private void FormSysTray_Load(object sender, EventArgs e)
        {
            _syncThread.Start();
        }

        private void FormSysTray_FormClosing(object sender, FormClosingEventArgs e)
        {
            _closing = true;
            _event.Set();
        }

        private void SyncThreadStart()
        {
            try
            {
                _engine.LoadState();
            }
            catch
            { }

            while (!_closing)
            {
                _engine.Sync();
                _event.WaitOne(3000);
            }
        }
    }
}
