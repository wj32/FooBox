using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace FooBoxClient
{
    public partial class FormSysTray : Form
    {
        private SyncEngine _engine;
        private Thread _syncThread;
        private object _engineSyncObject = new object();
        private bool _closing = false;
        private CancellationTokenSource _cancellationTokenSource;
        private Point _location;
        public FormStart _sender = null;
        private bool _paused = false;

        public FormSysTray()
        {
         
            InitializeComponent();
            notifyFooBox.Icon = FooBoxIcon.FooBox;
            this.Icon = FooBoxIcon.FooBox;
            _engine = new SyncEngine(Properties.Settings.Default.Root, Properties.Settings.Default.UserID);
            _syncThread = new Thread(this.SyncThreadStart);
            _cancellationTokenSource = new CancellationTokenSource();
            notifyFooBox.Visible = true;
            this.Visible = false;
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
        }

        private void FormSysTray_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!_closing)
            {
                e.Cancel = true;
                hideSelf();
            }
        }

        private void SyncThreadStart()
        {
            while (!_closing)
            {
                bool noDelay = false;

                if (!_paused)
                {
                    try
                    {
                        lock (_engineSyncObject)
                            noDelay = _engine.Run(_cancellationTokenSource.Token);
                    }
                    catch
                    { }
                }

                if (!noDelay)
                    _cancellationTokenSource.Token.WaitHandle.WaitOne(3000);
            }
        }

        private void ShutDown()
        {
            _closing = true;
            _cancellationTokenSource.Cancel();
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
              //  if (_location == new Point(0, 0))
              //  {
                    Rectangle r = WinAPI.GetTrayRectangle();
                    Rectangle resolution = Screen.PrimaryScreen.Bounds;
                    // this.Height = 300;
                    //  this.Width = 300;
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

               // }
                showSelf();
            }
        }


        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShutDown();
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
            ShutDown();
            notifyFooBox.Visible = false;
    
            
            _sender.Show();
            _sender.WindowState = FormWindowState.Normal;
            _sender.ShowIcon = true;
            this.Close();
        }

        private void settingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FormSettings frm = new FormSettings(_engineSyncObject, _engine);
            frm.Show();
        }

        private void notifyFooBox_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            hideSelf();
            Process.Start("explorer", Properties.Settings.Default.Root);
        }

        private void pauseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_paused)
            {
                _paused = false;
                pauseToolStripMenuItem.Text = "Pause syncing";
            }
            else
            {
                _paused = true;
                pauseToolStripMenuItem.Text = "Resuming syncing";
            }
        }
    }
}