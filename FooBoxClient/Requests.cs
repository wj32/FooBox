using FooBox.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace FooBoxClient
{
    public static class Requests
    {
        public static string MakeUrl(string actionName, string parameters = null)
        {
            string url = "http://" + Properties.Settings.Default.Server + ":" + Properties.Settings.Default.Port + "/Client/" + actionName;

            if (!string.IsNullOrEmpty(parameters))
                url += "?" + parameters;

            return url;
        }

        public static void Download(string hash, string destinationFileName)
        {
            string parameters = "id=" + Properties.Settings.Default.ID + "&secret=" + Properties.Settings.Default.Secret + "&hash=" + hash;
            HttpWebRequest req = WebRequest.Create(MakeUrl("Download", parameters)) as HttpWebRequest;

            req.KeepAlive = true;
            req.Method = "GET";

            byte[] buffer = new byte[4096 * 4];
            int bytesRead;

            try
            {
                using (var response = req.GetResponse())
                using (var responseStream = response.GetResponseStream())
                using (var outStream = new FileStream(destinationFileName, FileMode.Create))
                {
                    while ((bytesRead = responseStream.Read(buffer, 0, buffer.Length)) != 0)
                        outStream.Write(buffer, 0, bytesRead);
                }
            }
            catch
            {
                try
                {
                    System.IO.File.Delete(destinationFileName);
                }
                catch
                { }

                throw;
            }
        }

        public static string GetShareLink(string relativeFullName)
        {
            string parameters = "id=" + Properties.Settings.Default.ID + "&secret=" + Properties.Settings.Default.Secret + "&relativeFullName=" + Uri.EscapeDataString(relativeFullName);
            HttpWebRequest req = WebRequest.Create(MakeUrl("GetShareLink", parameters)) as HttpWebRequest;

            req.KeepAlive = true;
            req.Method = "GET";

            using (var response = req.GetResponse())
            using (var reader = new System.IO.StreamReader(response.GetResponseStream(), Encoding.UTF8))
            {
                return reader.ReadToEnd();
            }
        }

        public static string Upload(string sourceFileName)
        {
            string parameters = "id=" + Properties.Settings.Default.ID + "&secret=" + Properties.Settings.Default.Secret;
            HttpWebRequest req = WebRequest.Create(MakeUrl("Upload", parameters)) as HttpWebRequest;

            req.KeepAlive = true;
            req.Method = "POST";

            byte[] buffer = new byte[4096 * 4];
            int bytesRead;

            using (var requestStream = req.GetRequestStream())
            using (var inStream = new FileStream(sourceFileName, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                while ((bytesRead = inStream.Read(buffer, 0, buffer.Length)) != 0)
                    requestStream.Write(buffer, 0, bytesRead);
            }

            using (var response = req.GetResponse())
            using (var reader = new System.IO.StreamReader(response.GetResponseStream(), Encoding.UTF8))
            {
                return reader.ReadToEnd();
            }
        }

        public static ClientSyncResult Sync(ClientSyncData data)
        {
            HttpWebRequest req = WebRequest.Create(MakeUrl("Sync")) as HttpWebRequest;
            var serializer = new JavaScriptSerializer();
            var serial = serializer.Serialize(new ClientSyncPostData
            {
                Id = Properties.Settings.Default.ID,
                Secret = Properties.Settings.Default.Secret,
                Data = data
            });
            byte[] dataBytes = UTF8Encoding.UTF8.GetBytes(serial);

            req.KeepAlive = true;
            req.Method = "POST";
            req.ContentLength = dataBytes.Length;
            req.ContentType = "application/x-www-form-urlencoded";

            using (Stream postStream = req.GetRequestStream())
                postStream.Write(dataBytes, 0, dataBytes.Length);

            using (var response = req.GetResponse())
            using (var reader = new System.IO.StreamReader(response.GetResponseStream(), Encoding.UTF8))
            {
                return serializer.Deserialize<ClientSyncResult>(reader.ReadToEnd());
            }
        }
    }
}
