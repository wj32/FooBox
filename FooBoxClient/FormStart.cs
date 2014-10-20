﻿using System;
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
namespace FooBoxClient
{
    public partial class FormStart : Form
    {
        public FormStart(FormWindowState a)
        {
            InitializeComponent();
            this.WindowState = a;
            if (a == FormWindowState.Minimized)
            {
                this.ShowIcon = false;
            }
            checkStatus();
        }
        public FormSysTray _sender = null;

      

        public void checkStatus()
        {
            if (Properties.Settings.Default.UserName != "")
            {
                this.Hide();
                FormSysTray sysTray = new FormSysTray();
                sysTray._sender = this;
                sysTray.ShowDialog();
            }
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

            try
            {
                if (Directory.EnumerateFileSystemEntries(textBoxDirLoc.Text).Any())
                {
                    if (MessageBox.Show(
                        "The directory you have selected is not empty and will be erased. Do you want to continue?",
                        "FooBox",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning,
                        MessageBoxDefaultButton.Button2
                        ) == System.Windows.Forms.DialogResult.No)
                        return;

                    Directory.Delete(textBoxDirLoc.Text, true);
                    Directory.CreateDirectory(textBoxDirLoc.Text);
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
            
            

            this.Hide();
            FormSysTray frm = new FormSysTray();
            frm._sender = this;
            frm.Show();
            
        }



    }
}
