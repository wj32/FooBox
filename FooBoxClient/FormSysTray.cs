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
        private Point _location;
        public FormSysTray()
        {
         
            InitializeComponent();
            notifyFooBox.Icon = FooBoxIcon.FooBox;
            this.Icon = FooBoxIcon.FooBox;
            _engine = new SyncEngine(Properties.Settings.Default.Root);
            _syncThread = new Thread(this.SyncThreadStart);
            _event = new AutoResetEvent(false);
            notifyFooBox.Visible = true;
            hideSelf();
        }

        private void hideSelf()
        {
            this.WindowState = FormWindowState.Minimized;
            this.ShowInTaskbar = false;
            this.Hide();
        }

        private void showSelf()
        {
            this.WindowState = FormWindowState.Normal;
            this.Location = _location;
            this.Show();
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

        
        /*
         * Sets the location of the form for popping up
         */
        private void notifyFooBox_MouseClick(object sender, MouseEventArgs e)
        {
            if (_location == new Point(0, 0))
            {
                Rectangle r = WinAPI.GetTrayRectangle();
                Rectangle resolution = Screen.PrimaryScreen.Bounds;
               // this.Height = 300;
              //  this.Width = 300;
                if (r.X < resolution.Width / 2)
                {
                    _location = new Point(r.X + r.Width, r.Y - this.Height - 150);
                    //sys tray is in bottom right corner
                }
                else if (r.Y < resolution.Height / 2)
                {
                  
                    _location = new Point(r.X - this.Width, r.Y + r.Height);
                    //systray is im top right coerner
                }
                else
                {
                    _location = new Point(Screen.PrimaryScreen.Bounds.Width - this.Width - 220, Screen.PrimaryScreen.Bounds.Height - this.Height - 350);
                    //sys tray is in bottom corner
                }
               
            }
            showSelf();
        }

        private void FormSysTray_Deactivate(object sender, EventArgs e)
        {
            hideSelf();
        }

        private void FormSysTray_Resize(object sender, EventArgs e)
        {
        }

    }
}