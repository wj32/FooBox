using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.IO;
using System.Web.Script.Serialization;
using FooBox.Common;
using FooBox;
namespace FooBoxClient
{
    public partial class FormStart : Form
    {
        public FormStart()
        {
            InitializeComponent();
        }

        private void buttonBrowse_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            fbd.Description = "Select the directory to synchronise to.";
            fbd.SelectedPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            DialogResult result = fbd.ShowDialog();
            if (result == DialogResult.OK)
            {
                textBoxDirLoc.Text = fbd.SelectedPath;
            }
            else
            {
                labelError.Text = "Invalid directory selected";
            }
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.Close();
        }

        private void buttonNext_Click(object sender, EventArgs e)
        {
            if (String.IsNullOrWhiteSpace(textBoxDirLoc.Text.Trim()))
            {
                labelError.Text = "Please select a directory to sync files to";
                return;
            }

            string stateFileName = textBoxDirLoc.Text + "\\" + SyncEngine.SpecialFolderName + "\\" + SyncEngine.StateFileName;

            try
            {
                if (Directory.EnumerateFileSystemEntries(textBoxDirLoc.Text).Any())
                {
                    if (!System.IO.File.Exists(stateFileName))
                    {
                        if (MessageBox.Show(
                            "The directory you have selected is not empty and will be erased. Do you want to continue?",
                            "FooBox",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Warning,
                            MessageBoxDefaultButton.Button2
                            ) == System.Windows.Forms.DialogResult.No)
                            return;
                    }
                }
            }
            catch (Exception ex)
            {
                labelError.Text = "Unable to access the directory: " + ex.Message;
                return;
            }

            string url = @"http://" + textBoxServerLoc.Text.Trim() +":" + textBoxServerPort.Text.Trim() + "/Account/ClientLogin";

           

            string postContent =
                "userName=" + textBoxUsername.Text.Trim() +
                "&password=" + textBoxPassword.Text +
                "&clientName=" + Environment.MachineName;

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
                labelError.Text = "Server name or port incorrect";
                return;
            }

            HttpWebResponse response = req.GetResponse() as HttpWebResponse;
            var encoding = UTF8Encoding.UTF8;
            string responseText = "";
            using (var responseStream = response.GetResponseStream())
            using (var reader = new System.IO.StreamReader(responseStream, encoding))
            {
               responseText = reader.ReadToEnd();
            }
            if (responseText == "fail")
            {
                labelError.Text = "Username or password incorrect";
                return;
            }
            //IF WE'VE GOT HERE WE'VE SUCCESFULLY AUTH'D
            var result = (new JavaScriptSerializer()).Deserialize<ClientLoginResult>(responseText);
            Properties.Settings.Default.ID = result.Id;
            Properties.Settings.Default.Secret = result.Secret;
            Properties.Settings.Default.Port = int.Parse(textBoxServerPort.Text);
            Properties.Settings.Default.Server = textBoxServerLoc.Text;
            Properties.Settings.Default.Root = textBoxDirLoc.Text;
            Properties.Settings.Default.ClientName = Environment.MachineName;
            Properties.Settings.Default.UserID = result.UserId;
            Properties.Settings.Default.UserName = textBoxUsername.Text;
            Properties.Settings.Default.Save();
            
            try
            {
                bool keep = false;

                if (System.IO.File.Exists(stateFileName))
                {
                    JavaScriptSerializer serializer = new JavaScriptSerializer();
                    var state = serializer.Deserialize<SyncEngine.State>(System.IO.File.ReadAllText(stateFileName));

                    if (state.UserId == Properties.Settings.Default.UserID)
                        keep = true;
                }

                if (!keep)
                {
                    Utilities.DeleteDirectoryRecursive(Properties.Settings.Default.Root);

                    // This sometimes doesn't work...
                    for (int attempts = 0; attempts < 4; attempts++)
                    {
                        Directory.CreateDirectory(Properties.Settings.Default.Root);

                        if (Directory.Exists(Properties.Settings.Default.Root))
                            break;

                        System.Threading.Thread.Sleep(500);
                    }
                }
            }
            catch (Exception ex)
            {
                labelError.Text = "Unable to access the directory: " + ex.Message;
                return;
            }

            this.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.Close();
        }
    }
}
