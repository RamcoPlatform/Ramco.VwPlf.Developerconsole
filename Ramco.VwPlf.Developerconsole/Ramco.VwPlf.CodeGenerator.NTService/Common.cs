using System;
using System.IO;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace Ramco.VwPlf.CodeGenerator.NTService
{
    internal interface ILogger
    {
        void Write(string sMessage);
    }

    internal class TraceLogger : ILogger
    {
        public TraceLogger()
        {

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sMessage"></param>
        public void Write(string sMessage)
        {
            TraceListener trace = new DefaultTraceListener();
            trace.WriteLine(string.Format("{0} :   {1}", DateTime.Now, sMessage));
        }
    }

    internal class FileLogger : TraceLogger
    {
        string sTargetFile = string.Empty;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sFullFilePath"></param>
        public FileLogger(string sFullFilePath)
        {
            sTargetFile = sFullFilePath;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sMessage"></param>
        public new void Write(string sMessage)
        {
            using (FileStream fs = new FileStream(sTargetFile, FileMode.Append, FileAccess.Write))
            {
                using (StreamWriter sw = new StreamWriter(fs))
                {
                    sw.WriteLine(string.Format("{0} :   {1}", DateTime.Now, sMessage));
                }
            }
            base.Write(sMessage);
        }
    }

    public class LoginHelpers
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sModelURL"></param>
        /// <param name="sUserName"></param>
        /// <param name="sPassword"></param>
        /// <param name="sOU"></param>
        /// <param name="sRole"></param>
        /// <param name="sLangID"></param>
        /// <returns></returns>
        public bool AuthenticateUser(string sModelURL, string sUserName, string sPassword, string sOU, string sRole, string sLangID)
        {
            try
            {
                string sURL = string.Empty;
                string sOutMtd = string.Empty;

                sURL = string.Format("{0}/DeveloperConsole/DeveloperConsole.aspx?method=AuthenticateUser&username={1}&password={2}&ouid={3}&role={4}&languageid={5}", sModelURL, sUserName, sPassword, sOU, sRole, sLangID);
                Console.WriteLine(sURL);
                HttpWebResponse response = GetResponse(sURL, string.Empty, "POST");
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    Stream responseStream = response.GetResponseStream();
                    StreamReader streamReader = new StreamReader(responseStream);
                    sOutMtd = streamReader.ReadToEnd();
                    if (!string.IsNullOrEmpty(sOutMtd) && sOutMtd.Contains("RVWDS"))
                    {
                        string[] splittedResponse = Regex.Split(sOutMtd, "RVWDS");
                        if (!object.ReferenceEquals(splittedResponse[0], null))
                        {
                            return true;
                        }
                    }
                }

                return false;
            }
            catch
            {
                throw new Exception("Problem in authenticating user!.");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sModelURL"></param>
        /// <param name="sComponentName"></param>
        /// <param name="sOU"></param>
        /// <param name="sComponentInst"></param>
        /// <returns></returns>
        public string GetRMInfo(string sModelURL, string sComponentName, string sOU, string sComponentInst)
        {
            try
            {
                string sConnectionString = string.Empty;
                string sURL = string.Empty;

                sURL = string.Format("{0}/DeveloperConsole/DeveloperConsole.aspx?method=GetRMInfo&component={1}&ouid={2}&componentinstance={3}", sModelURL, sComponentName, sOU, sComponentInst);
                Console.WriteLine(sURL);
                HttpWebResponse response = GetResponse(sURL, string.Empty, "POST");

                if (response.StatusCode == HttpStatusCode.Accepted || response.StatusCode == HttpStatusCode.OK)
                {
                    Stream responseStream = response.GetResponseStream();
                    StreamReader streamReader = new StreamReader(responseStream);
                    sConnectionString = streamReader.ReadToEnd();
                }

                return sConnectionString;
            }
            catch
            {
                throw new Exception("Problem in getting connectionstring!.");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="modelURL"></param>
        /// <param name="component"></param>
        /// <param name="ou"></param>
        /// <param name="componentInst"></param>
        /// <returns></returns>
        public string GetDepDBConnectionString(string modelURL, string component, string ou, string componentInst)
        {            
            string depdbConnectionString = GetRMInfo(modelURL, component, ou, componentInst);

            if (!string.IsNullOrEmpty(depdbConnectionString))
                return depdbConnectionString;

            throw new Exception("Problem in getting depdb connectionstring!.");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="modelURL"></param>
        /// <param name="component"></param>
        /// <param name="ou"></param>
        /// <param name="componentInst"></param>
        /// <returns></returns>
        public string GetRMConnectionString(string modelURL, string component, string ou, string componentInst)
        {
            string rmConnectionString = GetRMInfo(modelURL, component, ou, componentInst);

            if (!string.IsNullOrEmpty(rmConnectionString))
                return rmConnectionString;

            throw new Exception("Problem in getting RM connectionstring!.");
        }

        /// <summary>
        /// sends a request and returns reponse from the url
        /// </summary>
        /// <param name="sURL">url from which u need response</param>
        /// <returns>returns HttpWebResponse </returns>
        private HttpWebResponse GetResponse(string sURL, string sContent, string sRequestMethod)
        {
            if (sURL == null)
                throw new ArgumentNullException("URL");

            HttpWebRequest newRequest = (HttpWebRequest)HttpWebRequest.Create(sURL);
            newRequest.Method = sRequestMethod;
            newRequest.AllowAutoRedirect = true;
            newRequest.UseDefaultCredentials = true;
            if (sRequestMethod == "POST")
            {
                byte[] contentAsBytes = Encoding.UTF8.GetBytes(sContent);

                newRequest.ContentType = "application/x-www-form-urlencoded";
                newRequest.ContentLength = contentAsBytes.Length;

                Stream requestStream = newRequest.GetRequestStream();
                requestStream.Write(contentAsBytes, 0, contentAsBytes.Length);
                requestStream.Close();
            }

            return (HttpWebResponse)newRequest.GetResponse();
        }
    }
}
