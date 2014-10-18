using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Forms;
using System.Runtime.Serialization;
using System.Net;
using FooBox.Common;

namespace FooBoxClient
{
    [Serializable]
    [DataContract]
    class File
    {
        [DataMember]
        private string _hash;

        //uhm would use long here but seems to be an issue with Stream.Read();
        [DataMember]
        private long _fileSize;


        [DataMember]
        private DateTime _lastModified;

        [DataMember]
        private string _fileName;
        [DataMember]
        private bool _isDirectory;
        [DataMember]
        public List<File> subFiles;
        private File _parent;


        public File() { DateTime.SpecifyKind(_lastModified, DateTimeKind.Utc); }
        public File(string fileName, bool isDirectory, long fileSize, string hash)
        {
            _hash = hash;
            _fileSize = fileSize;
            _isDirectory = isDirectory;
            _fileName = fileName;
            if (isDirectory)
            {
                subFiles = new List<File>();
            }
            DateTime.SpecifyKind(_lastModified, DateTimeKind.Utc);
        }

        public File subExists(string name)
        {

            foreach (File files in subFiles)
            {
                if (name == files.Name) {
                    return files;
                }
            }
            return null;
        }

        /*
         * Returns a list of file names in the directory
         */
        public List<string> getFileNameList()
        {
            List<string> l = new List<string>();
            foreach (File f in subFiles)
            {
                
                if (!f._isDirectory)
                {
                    l.Add(f.Name);
                }
            }
            return l;
        }

        public List<string> getDirectoryNameList()
        {
            List<string> l = new List<string>();
            foreach (File f in subFiles)
            {
                if (f._isDirectory)
                {
                    l.Add(f.Name);
                }
            }
            return l;
        }

        public bool downloadTo(string destinationFileName)
        {
            string url = @"http://" + Properties.Settings.Default.Server + ":" + Properties.Settings.Default.Port + "/Client/Download";
            string parameters = "id=" + Properties.Settings.Default.ID + "&secret=" + Properties.Settings.Default.Secret + "&hash=" + this.Hash;
            HttpWebRequest req = WebRequest.Create(url + "?" + parameters) as HttpWebRequest;

            req.KeepAlive = true;
            req.Method = "GET";

            try
            {
                byte[] buffer = new byte[4096 * 4];
                int bytesRead;

                using (var response = req.GetResponse())
                using (var responseStream = response.GetResponseStream())
                using (var outStream = new FileStream(destinationFileName, FileMode.Create))
                {
                    while ((bytesRead = responseStream.Read(buffer, 0, buffer.Length)) != 0)
                        outStream.Write(buffer, 0, bytesRead);
                }

                return true;
            }
            catch
            { }

            return false;
        }

        public void addFile(File f, bool instantiate)
        {
            f._parent = this;
            
            if (!System.IO.File.Exists(f.getFullPath()) && instantiate)
            {
                if (f._isDirectory)
                {
                    System.IO.Directory.CreateDirectory(f.getFullPath());
                }
                else
                {
                    //Download the file 

                    if (!f.downloadTo(f.getFullPath()))
                    {
                        MessageBox.Show("Download  failed");
                    }
   
                }
            }
            //ultra hacky line that fixes odd bug which seems to be a result of File.Create

            while (DateTime.Compare(DateTime.MinValue, f.LastModified) == 0)
            {
                f.LastModified = new System.IO.DirectoryInfo(f.getFullPath()).LastWriteTime.ToUniversalTime();
            }
            //create actual file if doean't exist
            subFiles.Add(f);
        }
        /*
         * Returns number of FILES not directories
         */
        public int getFileCount()
        {
            int count = 0;
            foreach (File f in subFiles){
                if (!f._isDirectory)
                {
                    count++;
                }
            }
            return count;
        }

        public int getDirectoryCount()
        {
            int count = 0;
            foreach (File f in subFiles)
            {
                if (f._isDirectory)
                {
                    count++;
                }
            }
            return count;
        }
        public string getFullPath()
        {
            File current = this;
            string path = "";
            while (current._parent != null) {
                if (current._parent.Name[current._parent.Name.Length - 1] == '\\')
                {
                    path = current.Name + path;
                }
                else
                {
                    path = "\\" + current.Name + path;
                }
                current = current._parent;
            }
            path = current.Name + path;
            return path;
        }


        #region GETSET
        public DateTime LastModified
        {
            get { return _lastModified; }
            set { _lastModified = value; }
        }
        public bool Directory
        {
            get { return _isDirectory; }
            set { _isDirectory = value; }
        }
        public string Name
        {
          get { return _fileName; }
          set { _fileName = value; }
        }
        internal File Parent
        {
            get { return _parent; }
            set { _parent = value; }
        }
        public string Hash
        {
            get { return _hash; }
            set { _hash = value; }
        }
        public long FileSize
        {
          get { return _fileSize; }
          set { _fileSize = value; }
        }

        #endregion
    }
    class FileSystem
    {
        private string _rootFolder;
        private long _changeListID;

        private File _root;
        private Dictionary<string, File> file;
        
        public FileSystem(string rootFolder, File root)
        {
            _rootFolder = rootFolder;
            _root = root;
            DateTime.SpecifyKind(_root.LastModified, DateTimeKind.Utc);
        }

        public FileSystem(string rootFolder)
        {
            _rootFolder = rootFolder;
            _root = new File(rootFolder, true, 0, "");
            _root.LastModified = DateTime.SpecifyKind(_root.LastModified, DateTimeKind.Utc);
        }

        public void executeClientSync(ClientSyncResult s, bool instantiate)
        {
            this._changeListID = s.LastChangelistId;
            executeChangeList(s.Changes, instantiate);
        }

        public void executeChangeList(ICollection<ClientChange> l, bool instantiate)
        {
            foreach (ClientChange change in l)
            {
                this.executeChange(change, instantiate);
            }
        }

        public void executeChange(ClientChange c, bool instantiate)
        {
            //do magic
           
            File current = _root;
            string[] files =  c.FullName.Split('/');
            for (int i = 2; i < files.Length; ++ i)
            {
                if (Properties.Settings.Default.ServerRoot == "")
                {
                    Properties.Settings.Default.ServerRoot = "/" + files[1];
                    Properties.Settings.Default.Save();
                }
                if (!String.IsNullOrWhiteSpace(files[i]))
                {
                            
                    if (c.IsFolder){
                        File temp = null;
                        temp = current.subExists(files[i]);
                        if (temp != null)
                        {
                            current = temp;
                        }
                        else
                        {
                            if (c.Type == ChangeType.Delete)
                            {
                                //delete folder code goes here
                            } else if (c.Type == ChangeType.Add){
                                current.addFile(new File(files[i], true, 0, ""), instantiate);
                            }
                            else if (c.Type == ChangeType.SetDisplayName)
                            {

                            }
                            else if (c.Type == ChangeType.None)
                            {
                                MessageBox.Show("no change to folder" + c.FullName);
                            }
                        }
                    }
                    else
                    {
                        if (i == files.Length - 1)
                        {
                            //add the file
                            //File temp = new File(files[i], false){
                                //  temp.LastModifed = c.
                            // }
                            current.addFile(new File(files[i], false, c.Size, c.Hash), instantiate);
                               
                        }
                        else
                        {
                            File temp = null;
                            temp = current.subExists(files[i]);
                            if (temp == null)
                            {
                                //shouldn't happen
                                MessageBox.Show("world is ending");
                            }
                            else
                            {
                                current = temp;
                            }
                        }
                    }
                }
            }
            
            return;
        }

        internal File Root
        {
            get { return _root; }
            set { _root = value; }
        }

        public string RootFolder
        {
            get { return _rootFolder; }
            set { _rootFolder = value; }
        }

        public long ChangeListID
        {
            get { return _changeListID; }
            set { _changeListID = value; }
        }

    }
}
