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
        private bool _paused = false;
        private Size _correctSize;
        private File _currentFile = null;

        public FormSysTray()
        {
            InitializeComponent();

            notifyFooBox.Icon = FooBoxIcon.FooBox;
            this.Icon = FooBoxIcon.FooBox;

            if (Properties.Settings.Default.UserID == 0 ||
                !System.IO.Directory.Exists(Properties.Settings.Default.Root))
            {
                FormStart startForm = new FormStart();

                if (startForm.ShowDialog() == System.Windows.Forms.DialogResult.Cancel)
                    Environment.Exit(0);
            }

            _engine = new SyncEngine(Properties.Settings.Default.Root, Properties.Settings.Default.UserID);
            _syncThread = new Thread(this.SyncThreadStart);
            _cancellationTokenSource = new CancellationTokenSource();
            notifyFooBox.Visible = true;
            _correctSize = this.Size;

            _syncThread.Start();
        }

        private void hideSelf()
        {
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

                if (!_paused && _engine != null)
                {
                    try
                    {
                        lock (_engineSyncObject)
                        {
                            if (_engine != null)
                                noDelay = _engine.Run(_cancellationTokenSource.Token);
                        }
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
                Rectangle r = WinAPI.GetTrayRectangle();
                Rectangle resolution = Screen.PrimaryScreen.Bounds;

                if (!this.IsHandleCreated)
                    this.CreateHandle();
                if (this.WindowState == FormWindowState.Minimized)
                    this.WindowState = FormWindowState.Normal;
                this.Size = _correctSize;

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
            ShutDown();
            _syncThread.Join();
            notifyFooBox.Visible = false;
            Application.Exit();
        }

        private void changeUserToolStripMenuItem_Click(object sender, EventArgs e)
        {
            lock (_engineSyncObject)
            {
                FormStart startForm = new FormStart();

                if (startForm.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    _engine = new SyncEngine(Properties.Settings.Default.Root, Properties.Settings.Default.UserID);
                }
            }
        }

        private void settingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FormSettings frm = new FormSettings(_engineSyncObject, _engine);
            frm.ShowDialog();
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

        private void FormSysTray_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
        }

        private void FormSysTray_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            foreach (string file in files)
            {
                    File f = _engine.FileExists(file);
                    if (f != null)
                    {
                        _currentFile = f;
                        if (f.IsFolder)
                        {

                            contextMenuDropFolder.Show(new Point(e.X, e.Y));
                            //show share popup
                        }
                        else
                        {
                            contextMenuDropFiles.Show(new Point(e.X, e.Y));
                            //show previos version and get link popup

                        }
                    }
                    else
                    {
                        string fileName = file.Substring(file.LastIndexOf("\\"));
                        if (!System.IO.File.Exists(_engine.RootDirectory + fileName)){
                            System.IO.File.Copy(file, _engine.RootDirectory + fileName);
                        } else {
                            notifyFooBox.BalloonTipText = "File already exists in FooBox and was not copied";
                            notifyFooBox.ShowBalloonTip(3000);

                        }
                        //sync engine will now do rest

                    }
         
            }

        }

        private void shareToolStripMenuItem_Click(object sender, EventArgs e)
        {
            
        }

        private void getPublicLinkToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string url = Requests.GetShareLink("/" + _currentFile.DisplayName);
            if (url != "")
            { 
                System.Windows.Forms.Clipboard.SetText(url);
                notifyFooBox.BalloonTipText = "Public link copied to clip board";
            }
            else
            {
                notifyFooBox.BalloonTipText = "Failed to get shareable link";
            }
            notifyFooBox.ShowBalloonTip(3000);
        }

        private void viewPreviousVersionsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            bool success = Requests.PreviousVersions("/" + _currentFile.DisplayName);
        }


    }
}