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
namespace FooBoxClient
{
    public partial class FormSysTray : Form
    {


        public FormSysTray()
        {
            InitializeComponent();
            getRootFolder();
        }


        private void getRootFolder(){
            string url = @"http://" + Properties.Settings.Default.Server +":" + Properties.Settings.Default.Port + "/File/ClientRoot";

           

            string postContent = "id=" + Properties.Settings.Default.ID + "&secret=" + Properties.Settings.Default.Secret;
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

            HttpWebResponse response = req.GetResponse() as HttpWebResponse;
            var encoding = UTF8Encoding.UTF8;
            string responseText = "";
            using (var reader = new System.IO.StreamReader(response.GetResponseStream(), encoding))
            {
               this.Text =responseText = reader.ReadToEnd();
            }
            if (responseText == "fail")
            {
                this.Text = "Authentification failed";
                return;
            }
        }

        private void FormSysTray_FormClosed(object sender, FormClosedEventArgs e)
        {
            Application.Exit();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Properties.Settings.Default.Reset();
        }

        private void sync()
        {
           
        }
    }
}
