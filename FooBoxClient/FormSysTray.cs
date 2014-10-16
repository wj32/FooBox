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
using System.Xml.Serialization;
using System.Timers;
using FooBox.Common;
namespace FooBoxClient
{
    public partial class FormSysTray : Form
    {
        FileSystem fs;
        System.Timers.Timer timerPoll;
        //in milliseconds
        int pollInterval = 20000;
        public FormSysTray()
        {
            InitializeComponent();
            getRootFolder(Properties.Settings.Default.ID);
            //run every 20 seconds;
            timerPoll = new System.Timers.Timer(pollInterval);
            timerPoll.Elapsed += timerPollTick;
            timerPoll.Enabled = true;
        }

        /*
         *  This method SHOULD only occur on the very first time run of the form
         * 
         */
        private void getRootFolder(string ID){
            string url = @"http://" + Properties.Settings.Default.Server +":" + Properties.Settings.Default.Port + "/Client/Sync";

           

            string postContent = "id=" + ID + "&secret=" + Properties.Settings.Default.Secret +  "&baseChangelistId=";
            MessageBox.Show(postContent);
            HttpWebRequest req = WebRequest.Create(url) as HttpWebRequest;

            byte[] dataBytes = UTF8Encoding.UTF8.GetBytes(postContent);
            
            req.KeepAlive = true;
            req.Method = "POST";
            req.ContentLength = dataBytes.Length;
            req.ContentType = "application/x-www-form-urlencoded";
            try
            {
                using (Stream postStream = req.GetRequestStream())
                {
                    postStream.Write(dataBytes, 0, dataBytes.Length);
                }
            }
            catch (WebException)
            {
                this.Text = "Server name or port incorrect";
                return;
            }
            try
            {
                HttpWebResponse response = req.GetResponse() as HttpWebResponse;
                var encoding = UTF8Encoding.UTF8;
                string responseText = "";
                using (var reader = new System.IO.StreamReader(response.GetResponseStream(), encoding))
                {
                    this.Text = responseText = reader.ReadToEnd();
                }


                ClientSyncResult c = (new System.Web.Script.Serialization.JavaScriptSerializer()).Deserialize<ClientSyncResult>(responseText);
                //loop through changes
            
                fs = new FileSystem(Properties.Settings.Default.Root);
                foreach (ClientChange change in c.Changes)
                {
                    fs.execChange(change);
                }
                return;
            }
            catch (WebException) {
                this.Text = "Authentification Failed!";
                return;
            }
    
        }

        private void timerPollTick(Object source, ElapsedEventArgs e)
        {
            //Okay doesn't block GUI
            sync();
            System.Threading.Thread.Sleep(10000);
        }

        private void FormSysTray_FormClosed(object sender, FormClosedEventArgs e)
        {
            Application.Exit();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Properties.Settings.Default.Reset();
        }

        /*
         * Called by timerPollTick will check for changes every pollInterval
         * Iterates over the fs starting at root and continuing down compares files and 
         * directory FileInfo, also searches for new files
         */
        private void checkForChanges(DirectoryInfo root, File fsRoot)
        {
           
            System.IO.FileInfo[] files = null;
            System.IO.DirectoryInfo[] subDirs = null;
            try
            {
                files = root.GetFiles();
            }
            catch (UnauthorizedAccessException)
            {

            }
            catch (DirectoryNotFoundException)
            {
                // lol no
            }

            if (files != null)
            {
                List<string> l = fsRoot.getFileNameList();
                foreach (System.IO.FileInfo fi in files)
                {
                    File temp = fsRoot.subExists(fi.Name);

                    if (temp != null)
                    {
                        l.Remove(temp.Name);
                        if (temp.Info.LastWriteTime != fi.LastWriteTime)
                        {
                            //Found change however assumes that user didn't rename a file 
                            //and add a file with the same name
                        }
                    }
                    else
                    {
                        //new file created
                        //needs to be added
                    }
                }
                foreach (string name in l)
                {
                    //file was deleted or renamed
                }

                // Now find all the subdirectories under this directory.
                subDirs = root.GetDirectories();
                List<string> fsDirs = fsRoot.getDirectoryNameList();
                foreach (System.IO.DirectoryInfo dirInfo in subDirs)
                {
                    // Resursive call for each subdirectory.
                    File  temp = fsRoot.subExists(dirInfo.Name);
                    if (temp != null){
                        fsDirs.Remove(temp.Name);
                        //directory exists
                        //go another level deeper
                        checkForChanges(dirInfo, temp);
                    }
                    else
                    {
                        //directory does not exist could be rename or created
                        //needs to be added
                    }
                    
                }
                foreach (string name in fsDirs)
                {
                    //directory was deleted or renamed
                }
            }
        }

        /*
         * Syncs with the syncserver thing 
         */
        private void sync()
        {
           
        }

        private void button2_Click(object sender, EventArgs e)
        {
            getRootFolder("342");
        }

    }
}
