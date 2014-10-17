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
                    var ser = new DataContractJsonSerializer(typeof(File));
                    string text = System.IO.File.ReadAllText(
                        System.Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
                        + "\\FooBox\\info.dat");
                    MemoryStream stream = new MemoryStream();
                    StreamWriter writer = new StreamWriter(stream);
                    writer.Write(text);
                    writer.Flush();
                    stream.Position = 0;

                    
                    temp = ser.ReadObject(stream) as File;
                    
                    reconstructParents(temp);
                }
                else
                {
                    temp = null;
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
         * Due to serialisation parents are null would have serialised parent
         * but got stuck due to cycles in the Object tree
         */
        private void reconstructParents(File f)
        {
            foreach (File fi in f.subFiles)
            {
                fi.Parent = f;
                if (fi.Directory)
                {
                    reconstructParents(fi);
                }
            }
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

                List<ClientChange> changeList = checkForChanges( new System.IO.DirectoryInfo(Properties.Settings.Default.Root), fs.Root);
                fs.executeChangeList(changeList, true);

                //Send list of changes along with base change list ID, if theres a conflict server sends conflict resolve by renaming
                //List of hashes that I need to uplaod

              //  var fsOnline = new FileSystem(Properties.Settings.Default.Root);
              //  fsOnline.executeChangeList(getSyncData(""), false);
               // List<ChangeItem> changes = createChangeList(fs, fsOnline);
               // fs.executeChangeList(getSyncData(""), false);
                //have a file list from last run 
                // need to merge this with the current files
            }
            else
            {
                fs = new FileSystem(Properties.Settings.Default.Root);
                fs.executeClientSync(getSyncData(""), true);
            }
            //serialise  the file 
            var serialiser = new DataContractJsonSerializer(typeof(File));
            
            MemoryStream stream1 = new MemoryStream();
            
            serialiser.WriteObject(stream1, fs.Root);
            stream1.Position = 0;
            StreamReader sr = new StreamReader(stream1);
            string json = sr.ReadToEnd();
            System.IO.File.WriteAllText(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\FooBox\\info.dat", json);
        }

        /*
         *  Gets a changelist from the server 
         *  can be given a change list id other is assumed to be blank
         */
        private ClientSyncResult getSyncData(string changeListID){
            string url = @"http://" + Properties.Settings.Default.Server +":" + Properties.Settings.Default.Port + "/Client/Sync";
            string postContent = "id=" + Properties.Settings.Default.ID + "&secret=" + Properties.Settings.Default.Secret +  "&baseChangelistId=" + changeListID;
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

        /*
         * Create a list of changes between two filesystems
         * 
         *
        private List<ChangeItem> createChangeList(FileSystem l, FileSystem r)
        {
            List<ChangeItem> changes = new List<ChangeItem>();
            changes.AddRange(findDiff(l.Root, r.Root));
            return changes;
        }

        *
        * Make l a copy of r and return the changes that occured
         
        private List<ChangeItem> findDiff(File l, File r)
        {
            List<ChangeItem> changes = new List<ChangeItem>();
            List<string> dirsL = l.getDirectoryNameList();
            List<string> docsL = l.getFileNameList();
            List<string> dirsR = r.getDirectoryNameList();
            List<string> docsR = r.getFileNameList();

            foreach (string doc in docsR)
            {
                ChangeItem tempChange = null;
                if (docsL.Contains(doc))
                {
                    //file exists in l
                    File tempL = l.subExists(doc);
                    File tempR = r.subExists(doc);
                    //determine which file is newer
                    if (tempL.LastModified != tempR.LastModified)
                    {
                        if (tempL.LastModified < tempR.LastModified)
                        {
                            tempChange = new ChangeItem();
                            tempChange.IsFolder = false;
                            tempChange.FullName =
                            tempChange.Type = ChangeType.Add;
                            //temp L is older
                            //have to get tempR
                        }
                        else
                        {
                            //tempL is newer
                            //have to upload tempL
                        }
                    }
                }
            }
            return changes;
        }*/


        public string getServerPath(string fullName)
        {
     
            fullName = Properties.Settings.Default.ServerRoot + fullName.Substring(Properties.Settings.Default.Root.Length);
            fullName = fullName.Replace('\\', '/');
            return fullName;
        }

        /*
         * Get the hash of the file defined by fileName
         * fileName is the full directory on the system.
         */
        private string getHash(string fileName)
        {
            string hash = "";
            return hash;
        }

        /*
         * check for changes between the local directory and a given file tree
         * Iterates over the fs starting at root and continuing down compares files and 
         * directory FileInfo, also searches for new files
         */
        private List<ClientChange> checkForChanges(DirectoryInfo root, File fsRoot)
        {
            List<ClientChange> changeList = new List<ClientChange>();
            System.IO.FileInfo[] files = null;
            System.IO.DirectoryInfo[] subDirs = null;
            try
            {
                files = root.GetFiles();
            }
            catch (UnauthorizedAccessException)
            {
                return changeList;
            }
            catch (DirectoryNotFoundException)
            {
                return changeList;
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
                        
         

                        if ((temp.LastModified-fi.LastWriteTimeUtc).TotalSeconds < -1)
                        {
                            ClientChange addDoc = new ClientChange();
                            addDoc.FullName = getServerPath(fi.FullName);
                            addDoc.IsFolder = false;
                            addDoc.Hash = getHash(fi.FullName);
                            addDoc.Type = ChangeType.Add;
                            changeList.Add(addDoc);
                            //fi is a newer version of temp
                            //Found change however assumes that user didn't rename a file 
                            //and add a file with the same name
                        }
                    }
                    else
                    {
                        ClientChange addDoc = new ClientChange();
                        addDoc.FullName = getServerPath(fi.FullName);
                        addDoc.IsFolder = false;
                        addDoc.Hash = getHash(fi.FullName);
                        addDoc.Type = ChangeType.Add;
                        changeList.Add(addDoc);
                        //new file created
                        //needs to be added
                    }
                }
                foreach (string name in l)
                {
                    //file was deleted or renamed
                    ClientChange deleteDoc = new ClientChange();
                    File del = fsRoot.subExists(name);
                    deleteDoc.FullName = getServerPath(del.getFullPath());
                    deleteDoc.IsFolder = false;
                    deleteDoc.Type = ChangeType.Delete;
                    changeList.Add(deleteDoc);
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

                        changeList.AddRange(checkForChanges(dirInfo, temp));
                    }
                    else
                    {
                        //directory does not exist could be rename or created
                        //needs to be added
                        ClientChange addDir = new ClientChange();
                       
                        addDir.FullName = getServerPath(dirInfo.FullName);
                        addDir.IsFolder = false;
                        addDir.Type = ChangeType.Add;
                        
                        changeList.Add(addDir);
                    }
                    
                }
                foreach (string name in fsDirs)
                {
                    ClientChange deleteDir = new ClientChange();
                    File del = fsRoot.subExists(name);
                    deleteDir.FullName = getServerPath(del.getFullPath());
                    deleteDir.IsFolder = false;
                    deleteDir.Type = ChangeType.Delete;
                    changeList.Add(deleteDir);
                    //directory was deleted or renamed
                }
            }
            return changeList;
        }

        /*
         * Actually doesn't do anything yet
         */
        private void sync()
        {
           
        }
    }
}
