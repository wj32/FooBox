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
        //this is really lazy and bad i guess
        public bool isRoot = false;


        private FileInfo _info;
        private string _fileName;
        private bool _isDirectory;
        public HashSet<File> subFiles;
        private File _parent;
        public File(string fileName, bool isDirectory)
        {
            _isDirectory = isDirectory;
            _fileName = fileName;
            if (isDirectory)
            {
                subFiles = new HashSet<File>();
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

        public void addFile(File f)
        {
            f._parent = this;
            if (!System.IO.File.Exists(f.getFullPath()))
            {
                if (f._isDirectory)
                {
                    System.IO.Directory.CreateDirectory(f.getFullPath());
                }
                else
                {
                    System.IO.File.Create(f.getFullPath(), 43224);
                }
            }
            //create actual file if doean't exist
            subFiles.Add(f);
        }

        public string getFullPath()
        {
            File current = this;
            string path = "";
            while (current.isRoot == false) {
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

        public FileSystem(string rootFolder)
        {
            _rootFolder = rootFolder;
            _root = new File(rootFolder, true);
            _root.isRoot = true;
        }

     

        public bool execChange(ClientChange c)
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
                                    current.addFile(new File(files[i], true));
                                }
                            }
                            else
                            {
                                if (i == files.Length - 1)
                                {
                                    //add the file
             
                                    current.addFile(new File(files[i], false));
                               
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
            
            return false;
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
