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
            fbd.RootFolder = Environment.SpecialFolder.Personal;
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
            this.Close();
        }

        private void buttonNext_Click(object sender, EventArgs e)
        {
            if (String.IsNullOrWhiteSpace(textBoxDirLoc.Text.Trim()))
            {
                labelError.Text = "Please select a directory to sync files to";
                return;
            }
            string url = @"http://" + textBoxServerLoc.Text.Trim() +":" + textBoxServerPort.Text.Trim() + "/Account/ClientLogin";

           

            string postContent = "username=" + textBoxUsername.Text.Trim() + "&password=" + textBoxPassword.Text;

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
            using (var reader = new System.IO.StreamReader(response.GetResponseStream(), encoding))
            {
               responseText = reader.ReadToEnd();
            }
            if (responseText == "fail")
            {
                labelError.Text = "Username or password incorrect";
                return;
            }
            //IF WE'VE GOT HERE WE'VE SUCCESFULLY AUTH'D
            string[] content = responseText.Split(',');
            Properties.Settings.Default.ID = content[0];
            Properties.Settings.Default.Secret = content[1];

            Properties.Settings.Default.Save();
            
            //NOT SURE IF THIS HOW ITS SUPPOSED TO BE DONE
            this.Hide();
            FormSysTray frm = new FormSysTray(textBoxDirLoc.Text.Trim());
            frm.Show();
        }



    }
}
