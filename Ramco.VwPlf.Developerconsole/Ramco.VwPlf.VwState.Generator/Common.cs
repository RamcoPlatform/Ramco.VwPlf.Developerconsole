using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.Data;
using System.Data.SqlClient;
using System.Collections;
using Microsoft.Win32;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Ramco.VwPlf.DataAccess;


namespace Ramco.VwPlf.VwState.Generator
{
    internal class Common
    {
        public static string InitCaps(string inputstring)
        {
            if (string.IsNullOrEmpty(inputstring))
            {
                return string.Empty;
            }
            return string.Concat(Char.ToUpper(inputstring[0]), inputstring.Substring(1).ToLower());
        }

        /// <summary>
        /// Creates a Xml File with Default Nodes(Root/NextLevel)
        /// </summary>
        /// <param name="strFilePath">Filepath</param>
        public static void CreateStateXmlFile(string strFilePath)
        {
            File.Create(strFilePath).Close();

            XDocument xmlFile = new XDocument();
            XElement rootElm = AddRootElement(xmlFile, "VWRules");
            AddElement(rootElm, "Templates");
            //xmlFile.Save(@strFilePath);
            SaveXmlDocument(strFilePath, xmlFile);
        }

        public static void CreateDirectory(string sDirectory)
        {
            if (!File.Exists(sDirectory))
                Directory.CreateDirectory(sDirectory);
        }

        /// <summary>
        /// Save Xml Document with new Settings.
        /// This Settings omits Xml Declaration.
        /// </summary>
        /// <param name="strFilePath">Full Filepath</param>
        /// <param name="xmlDoc">XDocument</param>
        public static void SaveXmlDocument(string strFilePath, XDocument xmlDoc)
        {
            string strDirectory = Path.GetDirectoryName(@strFilePath);
            if (!Directory.Exists(@strDirectory))
            {
                Directory.CreateDirectory(@strDirectory);
            }
            XmlWriterSettings xws = new XmlWriterSettings { OmitXmlDeclaration = true, Indent = true, IndentChars = " ", NewLineChars = "\r\n", NewLineHandling = NewLineHandling.Replace };
            using (XmlWriter xw = XmlWriter.Create(@strFilePath, xws))
            {
                xmlDoc.Save(xw);
            }
        }


        /// <summary>
        /// Adds RootElement to the XDocument
        /// </summary>
        /// <param name="xmlDoc">XDocument</param>
        /// <param name="strElmName">Root XElement</param>
        /// <returns>XElement</returns>
        public static XElement AddRootElement(XContainer xmlDoc, string strElmName)
        {
            XElement xElm;

            xElm = new XElement(strElmName);
            xmlDoc.Add(xElm);

            return xElm;
        }


        /// <summary>
        /// Adds new XElement to Another XElment
        /// </summary>
        /// <param name="xElmParent">Parent Element</param>
        /// <param name="strElmName">New Elements Name example: test for <test/> tag </param>
        /// <returns>XElement</returns>
        public static XElement AddElement(XContainer xElmParent, string strElmName)
        {
            XElement xElm;
            xElm = new XElement(strElmName);
            xElmParent.Add(xElm);
            return xElm;
        }


        /// <summary>
        /// Add a new attribute to an existing Xelement.
        /// </summary>
        /// <param name="xElm">Xelement for which you add attribute.</param>
        /// <param name="strAttributeName">attribute name.</param>
        /// <param name="strAttributeValue">attribute value.</param>
        /// <returns>XAttribute</returns>
        public static XAttribute AddAttribute(XElement xElm, string strAttributeName, string strAttributeValue)
        {
            XAttribute xmlAttribe;
            xmlAttribe = xElm.Attribute(strAttributeName);

            if (xmlAttribe == null)
            {
                xmlAttribe = new XAttribute(strAttributeName, strAttributeValue);
                xElm.Add(xmlAttribe);
            }
            else
            {
                xmlAttribe.Value = strAttributeValue;
            }

            return xmlAttribe;
        }


        /// <summary>
        /// Adds SetPropery Node to an element
        /// </summary>
        /// <param name="xElmParent">Parent element for which u add the setproperty node</param>
        /// <param name="type">pass the value for the attributte called 'type'</param>
        /// <param name="name">pass the value for the attributte called 'name'</param>
        /// <param name="value">pass the value for the attributte called 'value'</param>
        /// <returns></returns>
        public static XElement AddSetProperyElement(XElement xElmParent, string type, string name, string value)
        {
            XElement xElm = null;

            if (xElmParent != null)
            {
                xElm = AddElement(xElmParent, "SetProperty");
                AddAttribute(xElm, "type", type);
                AddAttribute(xElm, "name", name);
                AddAttribute(xElm, "value", value);
            }
            return xElm;
        }


        /// <summary>
        /// Finds an XElement with the given name.
        /// </summary>
        /// <param name="xCont">Parent XElement, Can be XDocument.</param>
        /// <param name="strElmName">XElement Name to find. Here XElement represents a tag.</param>
        /// <returns>XElement</returns>
        public static XElement GetSingleElembyName(XContainer xCont, string strElmName)
        {
            XElement xElm;

            xElm = (from xml in xCont.Descendants(strElmName)
                    select xml).FirstOrDefault();

            return xElm;
        }


        /// <summary>
        /// Function to find list of XElement with the specified element name.
        /// </summary>
        /// <param name="xCont">Container XElement or XDocument.</param>
        /// <param name="strElmName">Element name to be find.</param>
        /// <returns>Returns list of XElments that you can iterate through.</returns>
        public static IEnumerable<XElement> GetElementsbyName(XContainer xCont, string strElmName)
        {
            IEnumerable<XElement> xElms;
            xElms = (from xml in xCont.Descendants(strElmName)
                     select xml);
            return xElms;
        }


        /// <summary>
        /// Get an Element by its Attribute value.
        /// </summary>
        /// <param name="xCont">Parent XElement,Can be XDocument.</param>
        /// <param name="strElmName">XElement name to be find.</param>
        /// <param name="strAttName">XAttribute name to be find.</param>
        /// <param name="strAttVal">XAttribute value to be find.</param>
        /// <returns>XElement</returns>
        public static XElement GetElembyAttValue(XContainer xCont, string strElmName, string strAttName, string strAttVal)
        {
            XElement xElm;

            xElm = (from xml in xCont.Descendants(strElmName)
                    where xml.Attribute(strAttName).Value == strAttVal
                    select xml).FirstOrDefault();

            return xElm;
        }


        /// <summary>
        /// Get an Attribute value.
        /// </summary>
        /// <param name="xElm">Element</param>
        /// <param name="strAttName">Attribute name</param>
        /// <returns>Returns Attribute value.</returns>
        public static string GetAttributeValue(XElement xElm, string strAttName)
        {
            string attrVal = string.Empty;

            XAttribute xAttr = xElm.Attribute(strAttName);

            if (xAttr != null)
            {
                attrVal = xAttr.Value;
            }

            return attrVal;
        }


        public static IEnumerable<IEnumerable<XElement>> GetNodesWithSpecificChild(XContainer xElmCont, string strAncestorName, string strChildName)
        {
            IEnumerable<IEnumerable<XElement>> xElms;
            xElms = (from xml in xElmCont.Descendants(strChildName)
                     select xml.Ancestors(strAncestorName));
            return xElms;
        }


        /// <summary>
        /// Fetch DataTable from the provided stored procedure
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="spName"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static DataTable FetchDataTableFromSp(string connectionString, string spName, IDictionary parameters)
        {
            SqlConnection _SqlConn = new SqlConnection(connectionString);
            SqlCommand _command = new SqlCommand(spName, _SqlConn);
            _command.CommandType = CommandType.StoredProcedure;

            if (parameters != null)
            {
                IDictionaryEnumerator param = parameters.GetEnumerator();
                while (param.MoveNext())
                {
                    _command.Parameters.AddWithValue(param.Key.ToString(), param.Value);
                }
            }

            _SqlConn.Open();
            DataSet ds = new DataSet();
            SqlDataAdapter _dataAdapter = new SqlDataAdapter(_command);
            _dataAdapter.Fill(ds);
            _SqlConn.Close();

            DataTable dt = ds.Tables[0];
            return dt;
        }


        /// <summary>
        /// Function to Fetch Dataset from an sp
        /// </summary>
        /// <param name="connectionString">Connectionstring</param>
        /// <param name="spName">stored procedure name</param>
        /// <returns></returns>
        public static DataSet FetchDataSetFromSp(DBManager dbManager, string spName, IDictionary parameters)
        {
            DataSet ds = null;
            try
            {
                int i = 0;
                var dbParameters = dbManager.CreateParameters(parameters.Count);
                IDictionaryEnumerator param = parameters.GetEnumerator();

                while (param.MoveNext())
                {
                    dbManager.AddParamters(dbParameters, i, param.Key.ToString(), param.Value);
                    i++;
                }

                ds = dbManager.ExecuteDataSet(CommandType.StoredProcedure, spName, dbParameters);
            }
            catch (Exception e)
            {
                Logger.LogTrackMessage("true", "FetchDataSetFromSp Exception :" + e.Message.ToString());
            }

            return ds;
        }

        /// <summary>
        /// Function to get the folderName
        /// </summary>
        /// <param name="path">folder path</param>
        /// <returns></returns>
        public static string GetFolderName(string path)
        {
            string folderName = string.Empty;

            string[] str = path.Split('\\');
            int len = str.Length;

            return str[len - 1];
        }


        /// <summary>
        /// Generates statexml files 
        /// </summary>
        /// <param name="compSource">string array of component source path</param>
        /// <param name="compDestination">component folder's destination</param>
        public static void GenerateAndSaveFileFromPath(string[] compSource, string compDestination)
        {
            Logger.LogTrackMessage("true", DateTime.Now.ToString() + " : Function Common.GenerateAndSaveFileFromPath() Starts.");
            try
            {
                foreach (string compFolder in compSource)
                {

                    //create component folder
                    string componentName = Common.GetFolderName(compFolder);
                    string compoFoldPath = Path.Combine(compDestination, componentName);
                    Directory.CreateDirectory(compoFoldPath);

                    //state folder path
                    string stateXmlDir = Path.Combine(compFolder, "State");

                    //list of xml files with suffix '_offline'
                    string[] files = Directory.GetFiles(stateXmlDir, "*_offline.xml");

                    foreach (string file in files)
                    {
                        AddXmlForeachState(compoFoldPath, file);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogTrackMessage("true", "ERROR in Common.GenerateAndSaveFileFromPath(): " + ex.Message);
            }
            Logger.LogTrackMessage("true", DateTime.Now.ToString() + " : Function Common.GenerateAndSaveFileFromPath() Ends.");
        }


        /// <summary>
        /// Add xml for state from the old document.
        /// </summary>
        /// <param name="compoFoldPath">source component folder path.</param>
        /// <param name="file">source filename in the component folder.</param>
        public static void AddXmlForeachState(string compoFoldPath, string file)
        {

            Logger.LogTrackMessage("true", DateTime.Now.ToString() + " : Function Common.AddXmlForeachState() Starts.");
            try
            {
                XDocument xOldDoc = XDocument.Load(@file);

                XElement elmRoot = Common.GetSingleElembyName(xOldDoc, "states");

                //create statexml file
                string activityName = Common.GetAttributeValue(elmRoot, "activity");
                string uiName = Common.GetAttributeValue(elmRoot, "ui");
                string filename = activityName + "_" + uiName + "_state.xml";
                string filepath = Path.Combine(compoFoldPath, filename);
                Common.CreateStateXmlFile(@filepath);

                //get templates nodes from new document
                XDocument xNewDoc = XDocument.Load(@filepath);
                XElement elmTemplates = Common.GetSingleElembyName(xNewDoc, "Templates");

                //Get all service node in old document
                IEnumerable<XElement> elmStates = Common.GetElementsbyName(xOldDoc, "state");
                foreach (XElement elmState in elmStates)
                {

                    ///get state id from old Document.
                    string stateId = Common.GetAttributeValue(elmState, "id");

                    ///to avoid duplication in state
                    XElement xElmTemplate = Common.GetElembyAttValue(elmTemplates, "Template", "name", stateId);
                    if (xElmTemplate == null)
                    {
                        xElmTemplate = Common.AddElement(elmTemplates, "Template");
                        Logger.LogTrackMessage("true", "\tTemplate node for stateid:" + stateId + " added.");
                        Common.AddAttribute(xElmTemplate, "name", stateId);

                        //Add state xml for controls                            
                        AddControlXml(elmState, xElmTemplate);

                        //Add state xml for sections
                        AddSectionXml(elmState, xElmTemplate);

                        //Add state xml for page
                        AddPageXml(elmState, xElmTemplate);

                        //Add state xml for grid columns
                        AddGridColumnXml(elmState, xElmTemplate);

                    }
                    Common.SaveXmlDocument(@filepath, xNewDoc);
                }
            }
            catch (Exception ex)
            {
                Logger.LogTrackMessage("true", "\tERROR adding Template Node--Error Source:" + ex.Source + "\n\tError Description:" + ex.Message);
            }
            Logger.LogTrackMessage("true", DateTime.Now.ToString() + " : Function Common.AddXmlForeachState() Ends.");
        }


        /// <summary>
        /// add state xml for control.
        /// </summary>
        /// <param name="elmState">state element in old xml document.</param>
        /// <param name="xTemplate">template element in new xml document.</param>
        public static void AddControlXml(XElement elmState, XElement xTemplate)
        {
            Logger.LogTrackMessage("true", DateTime.Now.ToString() + " : Function Common.AddControlXml() Starts.");
            try
            {
                IEnumerable<XElement> elmControls = Common.GetElementsbyName(elmState, "control");
                foreach (XElement Control in elmControls)
                {
                    //taking values from old doc
                    string controlId = Common.GetAttributeValue(Control, "id");
                    string type = "Control";
                    string visible = Common.GetAttributeValue(Control, "visible");
                    string enable = Common.GetAttributeValue(Control, "enable");

                    //adding control nodes in new doc
                    XElement elmControl = Common.AddElement(xTemplate, "Control");
                    Common.AddAttribute(elmControl, "name", controlId);
                    Common.AddSetProperyElement(elmControl, type, "sv", visible);
                    Common.AddSetProperyElement(elmControl, type, "se", enable);
                    Logger.LogTrackMessage("true", "\tControl node added for controlid:" + controlId + " added.");

                }
            }
            catch (Exception ex)
            {
                Logger.LogTrackMessage("true", "\tERROR generating statexml for controls--Error Source:" + ex.Source + "\n\tError Description:" + ex.Message);
            }
            Logger.LogTrackMessage("true", DateTime.Now.ToString() + " : Function Common.AddControlXml() Ends.");
        }


        /// <summary>
        /// Add state xml for section.
        /// </summary>
        /// <param name="elmState">state element in old xml document.</param>
        /// <param name="xTemplate">template element in new xml document.</param>
        public static void AddSectionXml(XElement elmState, XElement xTemplate)
        {
            Logger.LogTrackMessage("true", DateTime.Now.ToString() + " : Function Common.AddSectionXml() Starts.");
            try
            {
                IEnumerable<XElement> elmSections = Common.GetElementsbyName(elmState, "section");
                foreach (XElement Section in elmSections)
                {
                    //taking values from old doc
                    string sectionName = Common.GetAttributeValue(Section, "name");
                    string type = "Control";
                    string visible = Common.GetAttributeValue(Section, "visible");
                    string enable = Common.GetAttributeValue(Section, "enable");
                    string collapse = Common.GetAttributeValue(Section, "collapse");

                    //adding section nodes in new doc
                    XElement elmSection = Common.AddElement(xTemplate, "LayoutControl");
                    Common.AddAttribute(elmSection, "name", sectionName);
                    Common.AddSetProperyElement(elmSection, type, "sv", visible);
                    Common.AddSetProperyElement(elmSection, type, "se", enable);
                    Common.AddSetProperyElement(elmSection, type, "sc", collapse);
                    Logger.LogTrackMessage("true", "\tLayControl node added for sectionname:" + sectionName + " added.");
                }
            }
            catch (Exception ex)
            {
                Logger.LogTrackMessage("true", "\tERROR generating statexml for sections--Error Source:" + ex.Source + "\n\tError Description:" + ex.Message);
            }
            Logger.LogTrackMessage("true", DateTime.Now.ToString() + " : Function Common.AddSectionXml() Ends.");
        }


        /// <summary>
        /// Add state xml for page.
        /// </summary>
        /// <param name="elmState">state element in old xml document.</param>
        /// <param name="xTemplate">template element in new xml document.</param>
        public static void AddPageXml(XElement elmState, XElement xTemplate)
        {
            Logger.LogTrackMessage("true", DateTime.Now.ToString() + " : Function Common.AddPageXml() Starts.");
            try
            {
                IEnumerable<XElement> elmPages = Common.GetElementsbyName(elmState, "page");
                foreach (XElement Page in elmPages)
                {
                    //taking values from old doc
                    string pageName = Common.GetAttributeValue(Page, "name");
                    string type = "Control";
                    string visible = Common.GetAttributeValue(Page, "visible");
                    string enable = Common.GetAttributeValue(Page, "enable");
                    string focus = Common.GetAttributeValue(Page, "focus");

                    //adding page nodes in new doc
                    XElement elmPage = Common.AddElement(xTemplate, "TabControl");
                    Common.AddAttribute(elmPage, "name", pageName);
                    Common.AddSetProperyElement(elmPage, type, "sv", visible);
                    Common.AddSetProperyElement(elmPage, type, "se", enable);
                    Common.AddSetProperyElement(elmPage, type, "at", focus);
                    Logger.LogTrackMessage("true", "\tTabControl node for pagename:" + pageName + " added.");
                }
            }
            catch (Exception ex)
            {
                Logger.LogTrackMessage("true", "\tERROR generating statexml for tab pages--Error Source:" + ex.Source + "\n\tError Description:" + ex.Message);
            }
            Logger.LogTrackMessage("true", DateTime.Now.ToString() + " : Function Common.AddPageXml() Ends.");
        }


        /// <summary>
        /// Add state xml for grid columns.
        /// </summary>
        /// <param name="elmState">state elment in old xml document.</param>
        /// <param name="xTemplate">template element in new xml document.</param>
        public static void AddGridColumnXml(XElement elmState, XElement xTemplate)
        {

            Logger.LogTrackMessage("true", DateTime.Now.ToString() + " : Function Common.AddGridColumnXml() Starts.");
            try
            {
                //reverse traversing for each <views> tag to find the gridControl specifically
                IEnumerable<IEnumerable<XElement>> elmViews = Common.GetNodesWithSpecificChild(elmState, "control", "views");
                foreach (IEnumerable<XElement> view in elmViews)
                {
                    foreach (XElement gridControl in view)
                    {
                        string gridControlId = Common.GetAttributeValue(gridControl, "id");
                        string IsgridVisible = Common.GetAttributeValue(gridControl, "visible");

                        XElement xelmGrid = Common.AddElement(xTemplate, "Grid");
                        Common.AddAttribute(xelmGrid, "name", gridControlId);
                        Common.AddSetProperyElement(xelmGrid, "Control", "sv", IsgridVisible);

                        IEnumerable<XElement> elmVWs = Common.GetElementsbyName(gridControl, "vw");
                        foreach (XElement vw in elmVWs)
                        {
                            string view_name = Common.GetAttributeValue(vw, "n");
                            string vwVisible = Common.GetAttributeValue(vw, "visible");

                            XElement xelmView = Common.AddElement(xelmGrid, "View");
                            Common.AddAttribute(xelmView, "name", view_name);

                            Common.AddSetProperyElement(xelmView, "Column", "sv", vwVisible);

                            Logger.LogTrackMessage("true", "\tView node added for viewname:" + view_name + " added.");
                        }
                        Logger.LogTrackMessage("true", "\tGrid node added for controlid:" + gridControlId + " added.");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogTrackMessage("true", "\tERROR generating statexml for gridcolumns--Error Source:" + ex.Source + "\n\tError Description:" + ex.Message);
            }
            Logger.LogTrackMessage("true", DateTime.Now.ToString() + " : Function Common.AddGridColumnXml() Ends.");
        }
    }

    /// <summary>
    /// Class has essential members to log message.
    /// </summary>
    public class Logger
    {

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern void OutputDebugString(string message);

        public static string keyPath = "HKEY_CURRENT_USER\\Software\\VB and VBA Program Settings\\Ramco-PTech\\DeveloperConsole";
        public static string path = (string)Registry.GetValue(keyPath, "Log Path", "");
        public static DefaultTraceListener consoleOut = new DefaultTraceListener();
        public static string LogPath = string.Empty;

        public static void LogTrackMessage(params string[] strMessageLines)
        {
            LogMessage(LogPath, strMessageLines);
        }


        public static void LogMessage(string strLogFile, params string[] strMessageLines)
        {
            try
            {
                FileInfo logFile = new FileInfo(@strLogFile);

                if (!Directory.Exists(logFile.DirectoryName))
                    Directory.CreateDirectory(logFile.DirectoryName);

                if (!File.Exists(@strLogFile))
                    File.Create(@strLogFile).Close();

                consoleOut.WriteLine("RT State :: " + strMessageLines[0]);


                using (StreamWriter writer = new StreamWriter(@strLogFile, true))
                {
                    foreach (string line in strMessageLines)
                    {
                        writer.WriteLine(DateTime.Now.ToString() + ":" + line);
                    }
                    writer.Close();
                    writer.Dispose();
                }
            }
            catch (Exception e)
            {
                consoleOut.WriteLine("LogMessage :: Exception :: " + strMessageLines[0] + " Exception :: " + e.Message.ToString());
            }
        }
    }
}
namespace Extensions
{
    public static class ExtensionMethods
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ds"></param>
        /// <returns></returns>
        public static bool IsEmpty(this DataSet ds)
        {
            return !ds.Tables.Cast<DataTable>().Any(x => x.DefaultView.Count > 0);
        }
    }
}