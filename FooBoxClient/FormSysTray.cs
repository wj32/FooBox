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
using System.Diagnostics;
using System.ComponentModel;

namespace FooBoxClient
{
    public partial class FormSysTray : Form
    {
        private SyncEngine _engine;
        private Thread _syncThread;
        private AutoResetEvent _event;
        private bool _closing = false;
        private Point _location;
        public FormStart _sender = null; 
        
        public FormSysTray()
        {
         
            InitializeComponent();
            notifyFooBox.Icon = FooBoxIcon.FooBox;
            this.Icon = FooBoxIcon.FooBox;
            _engine = new SyncEngine(Properties.Settings.Default.Root);
            _syncThread = new Thread(this.SyncThreadStart);
            _event = new AutoResetEvent(false);
  
        }

        private void hideSelf()
        {
           // this.WindowState = FormWindowState.Minimized;
            this.Hide();
        }

        private void showSelf()
        {
            this.Location = _location;
            this.Focus();
            this.Show();
        }

        private void FormSysTray_Load(object sender, EventArgs e)
        {
            _syncThread.Start();
            notifyFooBox.Visible = true;
            this.Visible = false;
        }

        private void FormSysTray_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!_closing)
            {
                e.Cancel = true;
                hideSelf();
            }

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
            if (e.Button == MouseButtons.Right)
            {
            }
            else
            {
                //This code is need for an initial state
                if (this.WindowState == FormWindowState.Minimized)
                {
                    this.WindowState = FormWindowState.Normal;
                }

                Rectangle r = WinAPI.GetTrayRectangle();
                Rectangle resolution = Screen.PrimaryScreen.Bounds;

                if (r.X < resolution.Width / 2)
                {
                    _location = new Point(r.X + r.Width, r.Y - this.Height + 80);
                    //sys tray is in bottom right corner
                }
                else if (r.Y < resolution.Height / 2)
                {

                    _location = new Point(r.X - this.Width + 80, r.Y + r.Height);
                    //systray is im top right coerner
                }
                else
                {
                    _location = new Point(Screen.PrimaryScreen.Bounds.Width - this.Width - 75, Screen.PrimaryScreen.Bounds.Height - this.Height - 75);
                    //sys tray is in bottom corner
                }


                showSelf();
            }
        }


        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _closing = true;
            _sender.Close();
            this.Close();
        }

        protected override void SetVisibleCore(bool value)
        {
            if (!this.IsHandleCreated)
            {
                value = false;
                CreateHandle();
            }
            base.SetVisibleCore(value);
        }

        private void logOutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Properties.Settings.Default.UserName = "";
            _closing = true;
            notifyFooBox.Visible = false;
    
            
            _sender.Show();
            _sender.WindowState = FormWindowState.Normal;
            _sender.ShowIcon = true;
            this.Close();
        }

        private void settingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FormSettings frm = new FormSettings(_syncThread, _engine);
            frm.Show();
        }

        private void notifyFooBox_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            hideSelf();
            Process.Start("explorer", Properties.Settings.Default.Root);
        }

    }
}