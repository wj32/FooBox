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
using System.Web.Script.Serialization;
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
            File temp = new File();
            //check if data file exists 
            if (System.IO.Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\FooBox")){
                if (System.IO.File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\FooBox\\info.dat"))
                {
                    //deserialize file
                    
                    temp = new JavaScriptSerializer().Deserialize(
                        System.IO.File.ReadAllText(
                        System.Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
                        + "\\FooBox\\info.dat")
                        , temp.GetType()
                        ) as File;
                }
            }
            else
            {
                //create directory for serialised version
                Directory.CreateDirectory(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\FooBox");
                temp = null;
            }


            merge(temp);
            //run every 20 seconds;
            timerPoll = new System.Timers.Timer(pollInterval);
            timerPoll.Elapsed += timerPollTick;
            timerPoll.Enabled = true;
        }


        /*
         * Handles all of the cases
         * Merges stored list with directory on computer
         * then merges that with the server side list
         * then instantiates all changes and pushes changelist to server
         * at the end of the merge the current fs will be serialised and dumped into file
         */
        private void merge(File f)
        {
            if (f != null)
            {
                fs = new FileSystem(Properties.Settings.Default.Root, f);
                //have a file list from last run 
                // need to merge this with the current files
            }
            else
            {
                fs = new FileSystem(Properties.Settings.Default.Root);
                fs.executeChangeList(getSyncData(""));
            }
        }

        /*
         *  Gets a changelist from the server 
         *  can be given a change list id other is assumed to be blank
         */
        private ClientSyncResult getSyncData(string changeListID){
            string url = @"http://" + Properties.Settings.Default.Server +":" + Properties.Settings.Default.Port + "/Client/Sync";
            string postContent = "id=" + Properties.Settings.Default.ID + "&secret=" + Properties.Settings.Default.Secret +  "&baseChangelistId=" + changeListID;
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
                return null;
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
            
             /*   fs = new FileSystem(Properties.Settings.Default.Root);
            */
                return c ;
            }
            catch (WebException) {
                this.Text = "Authentification Failed!";
                return null;
            }
    
        }

        private void timerPollTick(Object source, ElapsedEventArgs e)
        {
            //Okay doesn't block GUI
            checkForChanges(new DirectoryInfo(fs.RootFolder),fs.Root);

        }

        private void FormSysTray_FormClosed(object sender, FormClosedEventArgs e)
        {
            Application.Exit();
        }

        //test button plz ignore
        private void button1_Click(object sender, EventArgs e)
        {
            var json = new JavaScriptSerializer().Serialize(fs.Root);
            System.IO.File.WriteAllText(@"C:\Users\Luke\Desktop\temp.txt", json);
            File temp = new File("temp", false);
            temp = new JavaScriptSerializer().Deserialize(System.IO.File.ReadAllText(@"C:\Users\Luke\Desktop\temp.txt"), temp.GetType()) as File;
            //Properties.Settings.Default.Reset();
            temp = null;
        }

        /*
         * check for changes between the local directory and a given file tree
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
                return;
            }
            catch (DirectoryNotFoundException)
            {
                return;
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
         * Actually doesn't do anything yet
         */
        private void sync()
        {
           
        }
    }
}
