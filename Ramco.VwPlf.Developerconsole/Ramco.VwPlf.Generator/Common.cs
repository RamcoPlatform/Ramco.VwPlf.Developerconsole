//-----------------------------------------------------------------------
// <copyright file="Common.cs" company="Ramco Systems">
//     Copyright (c) . All rights reserved.
// Release id 	: PLF2.0_02398
// Description 	: Coded for desktop generator.
// By			: Karthikeyan V S
// Date 		: 23-Nov-2012
//-----------------------------------------------------------------------
// Bug id 	    : PLF2.0_03462
// Description 	: Desktop code generation changes for adding default 
//                enumerated value in schema.
// By			: Vidhyalakshmi N R
// Date 		: 14-Feb-2013
// </copyright>
//-----------------------------------------------------------------------

namespace Ramco.VwPlf.Generator
{
    using System;
    using System.Data.SqlClient;
    using System.Linq;
    using System.IO;

    /// <summary>
    /// 
    /// </summary>
    public class Common
    {
        /// <summary>
        /// 
        /// </summary>
        private System.Diagnostics.DefaultTraceListener Out = null;

        /// <summary>
        /// 
        /// </summary>
        private SqlCommand command = null;

        /// <summary>
        /// 
        /// </summary>
        private SqlDataReader reader = null;

        /// <summary>
        /// 
        /// </summary>
        public Common()
        {
            this.Out = new System.Diagnostics.DefaultTraceListener();
        }

        /// <summary>
        /// 
        /// </summary>
        public void CreateCommand()
        {
            try
            {
                this.command = null;
                if (object.ReferenceEquals(this.reader, null).Equals(false) && this.reader.IsClosed.Equals(false))
                {
                    this.reader.Close();
                }

                this.reader = null;
                this.command = GlobalVar.oConnection.CreateCommand();
                this.command.CommandTimeout = 0;
                this.command.CommandType = System.Data.CommandType.Text;
            }
            catch (Exception e)
            {
                WriteProfiler("General Exception in CreateCommand", e.Message);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void CloseCommand()
        {
            if (object.ReferenceEquals(this.command, null).Equals(false))
            {
                this.command.Dispose();
            }

            this.command = null;
        }

        /// <summary>
        /// 
        /// </summary>
        public void ExecuteQueryResult()
        {
            try
            {
                this.CreateCommand();
                this.command.CommandText = GlobalVar.Query;
                this.reader = this.command.ExecuteReader();
                this.CloseCommand();
            }
            catch (Exception e)
            { 
                WriteProfiler("General Exception in ExecuteQueryResult", e.Message); 
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="query"></param>
        public void ExecuteNonQueryResult(string query)
        {
            try
            {
                this.CreateCommand();
                this.command.CommandText = query;
                this.command.ExecuteNonQuery();
                this.CloseCommand();
            }
            catch (Exception e)
            {
                WriteProfiler("General Exception in ExecuteNonQueryResult", e.Message);
                this.WriteProfiler("Exception raised query : " + query);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        public void BindToLinq(string key)
        {
            try
            {
                IQueryable<System.Xml.Linq.XElement> values = null;
                this.CreateCommand();
                this.command.CommandText = GlobalVar.Query;
                System.Xml.XmlReader xmlReader = this.command.ExecuteXmlReader();
                xmlReader.Read();
                string xmlvalue = xmlReader.ReadOuterXml();
                if (xmlvalue.Length > 0)
                {
                    System.Xml.Linq.XDocument document = System.Xml.Linq.XDocument.Parse(xmlvalue);
                    values = (from Row in document.Descendants("row") select Row).AsQueryable();
                    if (GlobalVar.DataCollection.ContainsKey(key))
                    {
                        GlobalVar.DataCollection.Remove(key);
                    }

                    GlobalVar.DataCollection.Add(key, values);
                }
            }
            catch (System.Xml.XmlException exml)
            {
                this.WriteProfiler(string.Format("{0} : {1}", GlobalVar.Query, exml.Message));
            }
            catch (Exception e)
            {
                this.WriteProfiler(string.Format("General Exception in BindToLinq for Query : {0} : Error : {1}", GlobalVar.Query, e.Message));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="xelement"></param>
        /// <returns></returns>
        public bool DataExists(IQueryable<System.Xml.Linq.XElement> xelement)
        {
            try
            {
                return xelement.Count().Equals(0) ? false : true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public bool OpenConnection()
        {
            try
            {
                GlobalVar.oConnection = new SqlConnection();
                GlobalVar.ConnectionString = string.Format("MultipleActiveResultsets=True;{0}", GlobalVar.ConnectionString);
                GlobalVar.oConnection.ConnectionString = GlobalVar.ConnectionString;
                GlobalVar.oConnection.Open();
                return true;
            }
            catch (Exception e)
            {
                WriteProfiler("General Exception in OpenConnection - Connection opening failed", e.Message);
                return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public bool CloseConnection()
        {
            try
            {
                if (object.ReferenceEquals(GlobalVar.oConnection, null).Equals(false))
                {
                    if (GlobalVar.oConnection.State.Equals(System.Data.ConnectionState.Open))
                    {
                        this.WriteProfiler("Connection closing");
                        GlobalVar.oConnection.Close();
                        GlobalVar.oConnection.Dispose();
                        GlobalVar.oConnection = null;
                    }
                }

                return true;
            }
            catch (Exception e)
            {
                WriteProfiler("General Exception in CloseConnection - Connection closing failed", e.Message);
                return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        public void WriteProfiler(string message)
        {
            GlobalVar.ProfilerLog.Add(GlobalVar.Key++, string.Format("{0} : {1}", message, DateTime.Now));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="error"></param>
        public void WriteProfiler(string message, string error)
        {
            GlobalVar.ProfilerLog.Add(GlobalVar.Key++, string.Format("{0} : {1} : Error : {2}", message, error, DateTime.Now));
        }

        /// <summary>
        /// 
        /// </summary>
        public void WriteLogToFile()
        {
            try
            {
                System.IO.StreamWriter oSw = new System.IO.StreamWriter(string.Format(@"{0}\{1}.log", GlobalVar.GenerationPath, GlobalVar.Ecrno), false, System.Text.Encoding.UTF8);
                for (int i = 0; i < GlobalVar.ProfilerLog.Count - 1; i++)
                {
                    oSw.WriteLine(GlobalVar.ProfilerLog[i]);
                }

                GlobalVar.ProfilerLog.Clear();
                oSw.Flush();
                oSw.Close();
            }
            catch (Exception e)
            {
                this.Out.WriteLine("General Exception in WriteToFile - Failed to write log file" + e.Message);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void CreateDirectoryStructure()
        {
            string path = string.Empty;
            // GlobalVar.ReleasePath = string.Format(@"{0}\{1}\{2}\{3}\{4}\Updated\{5}\Release\xml\", GlobalVar.GenerationPath, GlobalVar.Platform, GlobalVar.Customer, GlobalVar.Project, GlobalVar.Ecrno, GlobalVar.Component); // Code commented for bug id: PLF2.0_03462
            GlobalVar.ReleasePath = string.Format(@"{0}\{1}\{2}\{3}\{4}\Updated\{5}\Release\VWDesktop_Data", GlobalVar.GenerationPath, GlobalVar.Platform, GlobalVar.Customer, GlobalVar.Project, GlobalVar.Ecrno, GlobalVar.Component); // Code added for bug id: PLF2.0_03462
            this.CreateDirectory(new System.IO.DirectoryInfo(GlobalVar.ReleasePath));
            // path = string.Format(@"{0}\service\1", GlobalVar.ReleasePath); // Code commented for bug id: PLF2.0_03462
            path = string.Format(@"{0}\ServiceXML\{1}\1", GlobalVar.ReleasePath, GlobalVar.Component); // Code added for bug id: PLF2.0_03462
            this.CreateDirectory(new System.IO.DirectoryInfo(path));
            // path = string.Format(@"{0}\ilbo", GlobalVar.ReleasePath); // Code commented for bug id: PLF2.0_03462
            path = string.Format(@"{0}\Model\{1}", GlobalVar.ReleasePath, GlobalVar.Component); // Code added for bug id: PLF2.0_03462
            this.CreateDirectory(new System.IO.DirectoryInfo(path));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        public void CreateDirectory(string path)
        {
            if (System.IO.Directory.Exists(path).Equals(false))
            {
                this.CreateDirectory(new System.IO.DirectoryInfo(path));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dirInfo"></param>
        private void CreateDirectory(System.IO.DirectoryInfo dirInfo)
        {
            if (object.ReferenceEquals(dirInfo.Parent, null).Equals(false))
            {
                this.CreateDirectory(dirInfo.Parent);
            }

            if (dirInfo.Exists.Equals(false))
            {
                dirInfo.Create();
            }
        }
    }
}
