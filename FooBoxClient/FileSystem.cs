using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Forms;
using FooBox.Common;

namespace FooBoxClient
{
    class File
    {


        private FileInfo _info;
        private string _fileName;
        private bool _isDirectory;
        public List<File> subFiles;
        private File _parent;
        public File() { }
        public File(string fileName, bool isDirectory)
        {
            _isDirectory = isDirectory;
            _fileName = fileName;
            if (isDirectory)
            {
                subFiles = new List<File>();
            }
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
                    f.Info = new System.IO.FileInfo(f.getFullPath());
                    System.IO.File.Create(f.getFullPath(), 43224);
                }
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
        public bool Directory
        {
            get { return _isDirectory; }
            set { _isDirectory = value; }
        }
        public FileInfo Info
        {
            get { return _info; }
            set { _info = value; }
        }
        public string Name
        {
          get { return _fileName; }
          set { _fileName = value; }
        }
        #endregion
    }
    class FileSystem
    {
        private string _rootFolder;
        private File _root;

        public FileSystem(string rootFolder, File root)
        {
            _rootFolder = rootFolder;
            _root = root;
        }

        public FileSystem(string rootFolder)
        {
            _rootFolder = rootFolder;
            _root = new File(rootFolder, true);
        }

        public void executeChangeList(ClientSyncResult s)
        {
            foreach (ClientChange change in s.Changes)
            {
                this.executeChange(change);
            }
        }

        public void executeChange(ClientChange c)
        {
            //do magic
            File current = _root;
            string[] files =  c.FullName.Split('/');
                    for (int i = 2; i < files.Length; ++ i)
                    {
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
                                    current.addFile(new File(files[i], true), true);
                                }
                            }
                            else
                            {
                                if (i == files.Length - 1)
                                {
                                    //add the file
             
                                    current.addFile(new File(files[i], false), true);
                               
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
    }
}
