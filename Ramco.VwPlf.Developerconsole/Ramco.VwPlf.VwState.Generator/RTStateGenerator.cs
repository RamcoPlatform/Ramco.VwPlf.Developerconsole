/***************************************************************************************************
 * Bug Id           :   PLF2.0_11668          
 * Fixed By         :   Madhan Sekar M
 * Modified On      :   12-Feb-2015
 * Fix Description  :   code fix for Trace and error log
 ***************************************************************************************************
 * Bug Id           :   PLF2.0_12171
 * Fixed By         :   Madhan Sekar M
 * Modified On      :   18-Mar-2015
 * Fix Description  :   'type' attribute added for template node
 ***************************************************************************************************
 * Bug Id           :   PLF2.0_15017
 * Fixed By         :   Madhan Sekar M
 * Modified On      :   15-Sep-2015
 * Fix Description  :   1)name format for the state file has been changed.
 *                  :   2)Button & Link controls should come as LayoutControls.
 *                  :   3)'se' property is added for grid columns.
 *                  :   4)'at' property is changed to 'sat'.
 ***************************************************************************************************
 * Bug Id           :   PLF2.0_15267
 * Fixed By         :   Madhan Sekar M
 * Modified On      :   20-Oct-2015
 * Fix Description  :   'sat' should be printed only in the case of 'y'
 ***************************************************************************************************/
using System;
using System.Linq;
using System.Data;
using System.Collections;
using System.IO;
using System.Xml.Linq;
using Extensions;


namespace Ramco.VwPlf.VwState.Generator
{
    public class RTStateGenerator
    {
        public SetOptions _setOptions = null;
        private string connectionString = string.Empty;
        private DataSet ds = null;

        //public static void Main(string[] args)
        //{
        //    if (args.Length > 0)
        //    {
        //        try
        //        {
        //            SetOptions _setOptions = new SetOptions();

        //            //'db' or 'directory'
        //            _setOptions.Type = args[0];

        //            _setOptions.Customer = args[1];
        //            _setOptions.Project = args[2];
        //            _setOptions.Process = args[3];
        //            _setOptions.Component = args[4];
        //            _setOptions.ECRNo = args[5];
        //            _setOptions.Activity = args[6];
        //            _setOptions.Ui = args[7];

        //            if (!string.IsNullOrEmpty(args[8]))
        //                _setOptions.OutputPath = args[8];

        //            if (!string.IsNullOrEmpty(args[9]))
        //                _setOptions.Server = args[9];

        //            if (!string.IsNullOrEmpty(args[10]))
        //                _setOptions.Username = args[10];

        //            if (!string.IsNullOrEmpty(args[11]))
        //                _setOptions.Password = args[11];

        //            if (!string.IsNullOrEmpty(args[12]))
        //                _setOptions.Database = args[12];

        //            if (!string.IsNullOrEmpty(args[13]))
        //                _setOptions.InputPath = args[13];

        //            if (!string.IsNullOrEmpty(args[14]))
        //                _setOptions.IsSegmentation = args[14];

        //            //PLF2.0_11668
        //            if (!string.IsNullOrEmpty(args[15]))
        //                _setOptions.LogPath = args[15];

        //            _setOptions.LogError = "true";
        //            _setOptions.LogSteps = "true";

        //            Logger.LogTrackMessage("State xml generation started");
        //            Generate _generateStateXml = new Generate(_setOptions);

        //        }
        //        catch (Exception ex)
        //        {
        //            Logger.LogTrackMessage("ERROR in State xml generation.", "\tError Source:" + ex.Source, "\tError Description:" + ex.Message);
        //        }
        //    }
        //}

        /// <summary>
        /// Constructor to set Options
        /// </summary>
        /// <param name="_options"></param>
        public RTStateGenerator(SetOptions _options)
        {
            this._setOptions = _options;
        }

        public bool Generate()
        {
            if (_setOptions.Type.ToLower().Equals("db"))
                return GenerateXmlFromDB();
            else
                return GenerateXmlFromPath();

            //else if (_setOptions.Type.ToLower().Equals("directory"))
        }

        /// <summary>
        /// Generates statexml from db
        /// </summary>
        private bool GenerateXmlFromDB()
        {
            bool bStatusFlg = false;
            Logger.LogTrackMessage("------------------------------------------------------------------");
            Logger.LogTrackMessage("Generating StateXml for:Customer:" + _setOptions.Customer + ",Project:" + _setOptions.Project + ",Process:" + _setOptions.Process + ",Component:" + _setOptions.Component + ",ECRNO:" + _setOptions.ECRNo + ",Activity:" + _setOptions.Activity + ",UI:" + _setOptions.Ui);

            try
            {
                this.connectionString = "Data Source=" + _setOptions.Server + ";Initial Catalog=" + _setOptions.Database + ";User ID=" + _setOptions.Username + ";Password=" + _setOptions.Password;

                Hashtable ht = new Hashtable();
                ht.Add("@customer", _setOptions.Customer);
                ht.Add("@project", _setOptions.Project);
                ht.Add("@process", _setOptions.Process);
                ht.Add("@component", _setOptions.Component);
                ht.Add("@ecrno", _setOptions.ECRNo);
                ht.Add("@act", _setOptions.Activity);
                ht.Add("@ui", _setOptions.Ui);

                ds = Common.FetchDataSetFromSp(_setOptions.dbManager, "de_generate_rtstate_xml_latest", ht);
                ds.Tables[0].TableName = "Control";
                ds.Tables[1].TableName = "Column";
                ds.Tables[2].TableName = "Section";
                ds.Tables[3].TableName = "Page";


                if (!ds.IsEmpty())
                {
                    Common.CreateDirectory(_setOptions.OutputPath);
                    Common.CreateStateXmlFile(Path.Combine(_setOptions.OutputPath, string.Format("{0}_{1}.xml", Common.InitCaps(_setOptions.Activity), Common.InitCaps(_setOptions.Ui))));

                    if (GenerateControlXml() == true
                        && GenerateColumnXml() == true
                        && GenerateSectionXml() == true
                        && GeneratePageXml() == true)
                    {
                        _setOptions.IsGenSucceed = true;
                        bStatusFlg = true;
                    }
                }
                else
                {
                    Logger.LogTrackMessage("State Unvailable for:Customer:" + _setOptions.Customer + ",Project:" + _setOptions.Project + ",Process:" + _setOptions.Process + ",Component:" + _setOptions.Component + ",ECRNO:" + _setOptions.ECRNo + ",Activity:" + _setOptions.Activity + ",UI:" + _setOptions.Ui);
                }
            }
            catch (Exception ex)
            {
                bStatusFlg = false;
                Logger.LogTrackMessage("StateXml Generation Failed", "\tError Source:" + ex.Source, "\tError Description:" + ex.Message);
            }

            Logger.LogTrackMessage("StateXml Generation completed for :Customer:" + _setOptions.Customer + ",Project:" + _setOptions.Project + ",Process:" + _setOptions.Process + ",Component:" + _setOptions.Component + ",ECRNO:" + _setOptions.ECRNo + ",Activity:" + _setOptions.Activity + ",UI:" + _setOptions.Ui);
            Logger.LogTrackMessage("------------------------------------------------------------------");
            return bStatusFlg;
        }


        ///// <summary>
        ///// Generates Statexml files required
        ///// </summary>
        //private bool GenerateFiles()
        //{

        //    bool _fileGenSuccess = false;
        //    try
        //    {
        //        DataTable dt = ds.Tables[0];

        //        //if resultset is available
        //        if (dt.Rows.Count > 0)
        //        {
        //            //to create list of files needed
        //            foreach (DataRow dr in dt.Rows)
        //            {
        //                string filename = Convert.ToString(dr["activity_name"]) + '_' + Convert.ToString(dr["ui_name"]) + ".xml"; //PLF2.0_15017
        //                string filepath = Path.Combine(_setOptions.OutputPath, filename);

        //                //if file not exists
        //                if (!File.Exists(@filepath))
        //                {
        //                    Common.CreateStateXmlFile(@filepath);
        //                    Logger.LogTrackMessage(filepath + " created.");
        //                }

        //                //if file already exists
        //                else
        //                {
        //                    FileInfo xmlFile = new FileInfo(@filepath);
        //                    xmlFile.Delete();
        //                    Common.CreateStateXmlFile(@filepath);
        //                    Logger.LogTrackMessage(filepath + " created.");
        //                }
        //            }
        //        }
        //        _fileGenSuccess = true;
        //    }
        //    catch (Exception ex)
        //    {
        //        Logger.LogTrackMessage("ERROR generating xml file", "\tError Source:" + ex.Source, "\tError Description:" + ex.Message);
        //    }
        //    return _fileGenSuccess;
        //}



        /// <summary>
        /// Function to generate statexml for control.
        /// </summary>
        /// <param name="connectionString"></param>
        private bool GenerateControlXml()
        {
            Logger.LogTrackMessage("Generating state contents for controls..");
            bool _CtrXmlGenSuccess = false;
            try
            {
                DataTable dt = ds.Tables["Control"];
                if (dt.Rows.Count > 0)
                {
                    string filename = string.Format("{0}_{1}.xml", _setOptions.Activity, _setOptions.Ui);
                    string filepath = Path.Combine(_setOptions.OutputPath, filename);

                    XDocument xmlDoc = XDocument.Load(@filepath);
                    XElement elmTemplates = Common.GetSingleElembyName(xmlDoc, "Templates");

                    DataRow[] rows = dt.Select("activity_name='" + _setOptions.Activity + "' and ui_name='" + _setOptions.Ui + "'");
                    if (rows.Count() > 0)
                    {
                        foreach (DataRow row in rows)
                        {

                            string stateId = Convert.ToString(row["state_id"]).ToLower();
                            string controlId = Convert.ToString(row["control_id"]).ToLower();
                            string controltype = Convert.ToString(row["control_type"]); //PLF2.0_15017
                            string type = Convert.ToString(row["type"]);
                            string visible = Convert.ToString(row["visible"]).ToLower();
                            string enable = Convert.ToString(row["enable"]).ToLower();

                            //PLF2.0_15017 **starts**
                            string nodeName = string.Empty;
                            nodeName = (controltype.ToLower().Equals("button") || controltype.ToLower().Equals("link")) ? "LayoutControl" : "Control";
                            //PLF2.0_15017 **ends**


                            //if there is no Template node for the given stateId
                            //add Template node and take reference
                            XElement elmTemplate = Common.GetElembyAttValue(xmlDoc, "Template", "name", stateId);
                            if (elmTemplate == null)
                            {
                                elmTemplate = Common.AddElement(elmTemplates, "Template");
                                Common.AddAttribute(elmTemplate, "name", stateId);
                                //Logger.LogTrackMessage("Template node for stateid:" + stateId + " added.");
                            }
                            Common.AddAttribute(elmTemplate, "type", "4");//PLF2.0_12171

                            //Add Control Node
                            XElement elmControl = Common.AddElement(elmTemplate, nodeName); //PLF2.0_15017
                            Common.AddAttribute(elmControl, "name", controlId);
                            //Logger.LogTrackMessage("control node for controlid:" + controlId + " added.");


                            //Add SetProperty node for visible
                            Common.AddSetProperyElement(elmControl, type, "sv", visible);
                            //Add SetProperty node for enable
                            Common.AddSetProperyElement(elmControl, type, "se", enable);

                            //save document to reflect changes
                            Common.SaveXmlDocument(filepath, xmlDoc);
                        }
                    }
                }
                else
                {
                    Logger.LogTrackMessage("No Controls are defined in state.");
                }
            }
            catch (Exception ex)
            {
                Logger.LogTrackMessage("ERROR generating statexml for controls", "\tError Source:" + ex.Source, "\tError Description: " + ex.Message);
            }
            _CtrXmlGenSuccess = true;
            return _CtrXmlGenSuccess;

        }



        /// <summary>
        /// Function to Generate statexml for gridcolumn(s).
        /// </summary>
        private bool GenerateColumnXml()
        {
            bool _ColXmlGenSuccess = false;
            Logger.LogTrackMessage("Generating state contents for grid columns..");
            try
            {
                DataTable dt = ds.Tables["Column"];
                if (dt.Rows.Count > 0)
                {
                    string filename = string.Format("{0}_{1}.xml", _setOptions.Activity, _setOptions.Ui);
                    string filepath = Path.Combine(_setOptions.OutputPath, filename);

                    XDocument xmlDoc = XDocument.Load(@filepath);
                    XElement elmTemplates = Common.GetSingleElembyName(xmlDoc, "Templates");


                    DataRow[] rows = dt.Select("activity_name='" + _setOptions.Activity + "' and ui_name='" + _setOptions.Ui + "'");
                    if (rows.Count() > 0)
                    {
                        foreach (DataRow row in rows)
                        {
                            string stateId = Convert.ToString(row["state_id"]).ToLower();
                            string controlId = Convert.ToString(row["control_id"]).ToLower();
                            string type = Convert.ToString(row["type"]);
                            string view_name = Convert.ToString(row["view_name"]).ToLower();
                            string visible = Convert.ToString(row["visible"]).ToLower();
                            string enable = Convert.ToString(row["enable"]).ToLower(); //PLF2.0_15017


                            //if there is no Template node for the given stateId
                            //add Template node and take reference
                            XElement elmTemplate = Common.GetElembyAttValue(xmlDoc, "Template", "name", stateId);
                            if (elmTemplate == null)
                            {
                                elmTemplate = Common.AddElement(elmTemplates, "Template");
                                Common.AddAttribute(elmTemplate, "name", stateId);
                                //Logger.LogTrackMessage("Template node for stateid:" + stateId + " added.");
                            }
                            Common.AddAttribute(elmTemplate, "type", "4");//PLF2.0_15017


                            //if there is no Grid node for the given stateId
                            //add Grid node and take reference
                            XElement elmGrid = Common.GetElembyAttValue(elmTemplate, "Grid", "name", controlId);
                            if (elmGrid == null)
                            {
                                elmGrid = Common.AddElement(elmTemplate, "Grid");
                                Common.AddAttribute(elmGrid, "name", controlId);
                                //Logger.LogTrackMessage("Grid node for controlid:" + controlId + " added.");
                            }

                            //Add View Node
                            XElement elmView = Common.AddElement(elmGrid, "View");
                            Common.AddAttribute(elmView, "name", view_name);
                            //Logger.LogTrackMessage("view node for view_name:" + view_name + " added.");

                            //Add SetProperty node for visible
                            Common.AddSetProperyElement(elmView, type, "sv", visible);
                            if (!String.IsNullOrEmpty(enable))
                                Common.AddSetProperyElement(elmView, type, "se", enable); //PLF2.0_15017

                            Common.SaveXmlDocument(filepath, xmlDoc);
                        }
                    }
                }
                else
                {
                    Logger.LogTrackMessage("No Grid columns are defined in state.");
                }
            }
            catch (Exception ex)
            {
                Logger.LogTrackMessage("ERROR generating statexml for grid columns", "\tError Source:" + ex.Source, "\tError Description:" + ex.Message);
            }
            _ColXmlGenSuccess = true;
            return _ColXmlGenSuccess;
        }



        /// <summary>
        /// Function to Generate statexml for Section(s).
        /// </summary>
        private bool GenerateSectionXml()
        {
            bool _SecXmlGenSuccess = false;
            Logger.LogTrackMessage("Generating state contents for section..");
            try
            {
                DataTable dt = ds.Tables["Section"];
                if (dt.Rows.Count > 0)
                {
                    string filename = string.Format("{0}_{1}.xml", _setOptions.Activity, _setOptions.Ui);
                    string filepath = Path.Combine(_setOptions.OutputPath, filename);

                    XDocument xmlDoc = XDocument.Load(@filepath);
                    XElement elmTemplates = Common.GetSingleElembyName(xmlDoc, "Templates");

                    DataRow[] rows = dt.Select("activity_name='" + _setOptions.Activity + "' and ui_name='" + _setOptions.Ui + "'");
                    if (rows.Count() > 0)
                    {
                        foreach (DataRow row in rows)
                        {
                            string stateId = Convert.ToString(row["state_id"]).ToLower();
                            string section_bt_synonym = Convert.ToString(row["section_bt_synonym"]).ToLower();
                            string type = Convert.ToString(row["type"]);
                            string visible = Convert.ToString(row["visible"]).ToLower();
                            string enable = Convert.ToString(row["enable"]).ToLower();
                            string collapse = Convert.ToString(row["collapse"]).ToLower();


                            //if there is no Template node for the given stateId
                            //add Template node and take reference
                            XElement elmTemplate = Common.GetElembyAttValue(xmlDoc, "Template", "name", stateId);
                            if (elmTemplate == null)
                            {
                                elmTemplate = Common.AddElement(elmTemplates, "Template");
                                Common.AddAttribute(elmTemplate, "name", stateId);
                                //Logger.LogTrackMessage("Template node for stateid:" + stateId + " added.");
                            }
                            Common.AddAttribute(elmTemplate, "type", "4");//PLF2.0_15017

                            //if there is no LayoutControl node for the given stateId
                            //add LayoutControl node and take reference
                            XElement elmlayoutCtrl = Common.GetElembyAttValue(elmTemplate, "LayoutControl", "name", section_bt_synonym);
                            if (elmlayoutCtrl == null)
                            {
                                elmlayoutCtrl = Common.AddElement(elmTemplate, "LayoutControl");
                                Common.AddAttribute(elmlayoutCtrl, "name", section_bt_synonym);
                                //Logger.LogTrackMessage("LayoutControl node for section_bt_synonym:" + section_bt_synonym + " added.");
                            }


                            //Add SetProperty node for visible
                            Common.AddSetProperyElement(elmlayoutCtrl, type, "sv", visible);

                            //Add SetProperty node for enable
                            Common.AddSetProperyElement(elmlayoutCtrl, type, "se", enable);

                            //Add SetProperty node for collapse
                            Common.AddSetProperyElement(elmlayoutCtrl, type, "sc", collapse);

                            Common.SaveXmlDocument(filepath, xmlDoc);
                        }
                    }
                }
                else
                {
                    Logger.LogTrackMessage("No sections are defined in state.");
                }
            }
            catch (Exception ex)
            {
                Logger.LogTrackMessage("ERROR in Generate.GenerateSectionXml():" + ex.Message);
            }
            _SecXmlGenSuccess = true;
            return _SecXmlGenSuccess;
        }



        /// <summary>
        /// Function to Generate statexml for TabPage(s)
        /// </summary>
        private bool GeneratePageXml()
        {
            bool _PageXmlGenSuccess = false;
            Logger.LogTrackMessage("Generating state contents for tab pages..");
            try
            {
                DataTable dt = ds.Tables["Page"];
                if (dt.Rows.Count > 0)
                {
                    string filename = string.Format("{0}_{1}.xml", _setOptions.Activity, _setOptions.Ui);
                    string filepath = Path.Combine(_setOptions.OutputPath, filename);

                    XDocument xmlDoc = XDocument.Load(@filepath);
                    XElement elmTemplates = Common.GetSingleElembyName(xmlDoc, "Templates");

                    DataRow[] rows = dt.Select("activity_name='" + _setOptions.Activity + "' and ui_name='" + _setOptions.Ui + "'");
                    if (rows.Count() > 0)
                    {
                        foreach (DataRow row in rows)
                        {
                            string stateId = Convert.ToString(row["state_id"]).ToLower();
                            string page_bt_synonym = Convert.ToString(row["page_bt_synonym"]).ToLower();
                            string type = Convert.ToString(row["type"]);
                            string visible = Convert.ToString(row["visible"]).ToLower();
                            string enable = Convert.ToString(row["enable"]).ToLower();
                            string active_tab = Convert.ToString(row["focus"]).ToLower();

                            //if there is no Template node for the given stateId
                            //add Template node and take reference
                            XElement elmTemplate = Common.GetElembyAttValue(xmlDoc, "Template", "name", stateId);
                            if (elmTemplate == null)
                            {
                                elmTemplate = Common.AddElement(elmTemplates, "Template");
                                //Logger.LogTrackMessage("Template node for stateid:" + stateId + " added.");
                                Common.AddAttribute(elmTemplate, "name", stateId);
                            }
                            Common.AddAttribute(elmTemplate, "type", "4");//PLF2.0_15017

                            //if there is no TabControl node for the given stateId
                            //add TabControl node and take reference
                            XElement elmTabCtrl = Common.GetElembyAttValue(elmTemplate, "TabControl", "name", page_bt_synonym);
                            if (elmTabCtrl == null)
                            {
                                elmTabCtrl = Common.AddElement(elmTemplate, "TabControl");
                                Logger.LogTrackMessage("TabControl node for page_bt_synonym:" + page_bt_synonym + " added.");
                                Common.AddAttribute(elmTabCtrl, "name", page_bt_synonym);
                            }

                            //Add SetProperty node for visible
                            Common.AddSetProperyElement(elmTabCtrl, type, "sv", visible);

                            //Add SetProperty node for enable
                            Common.AddSetProperyElement(elmTabCtrl, type, "se", enable);

                            //Add SetProperty node for ActiveTab
                            if (active_tab.Equals("y")) //PLF2.0_15267
                                Common.AddSetProperyElement(elmTabCtrl, type, "sat", active_tab); //PLF2.0_15017

                            Common.SaveXmlDocument(filepath, xmlDoc);
                        }
                    }
                }
                else
                {
                    Logger.LogTrackMessage("No tab pages are defined in state.");
                }
            }
            catch (Exception ex)
            {
                Logger.LogTrackMessage("ERROR generating statexml for page", "\tError Source:" + ex.Source, "\tError Description:" + ex.Message);
            }
            _PageXmlGenSuccess = true;
            return _PageXmlGenSuccess;
        }



        /// <summary>
        /// Generate xml from the input path
        /// </summary>
        /// 
        private bool GenerateXmlFromPath()
        {
            bool bStatusFlg = false;
            try
            {
                _setOptions.IsGenSucceed = false;

                //string outputBasePath = Environment.GetEnvironmentVariable("_ITKPATH_");
                string outputBasePath = _setOptions.InputPath;

                //create personalization folder
                string personalization = Path.Combine(outputBasePath, "Personalization");
                Directory.CreateDirectory(personalization);

                //itk folder path
                string itkFolder = Path.Combine(_setOptions.InputPath, "_ITK_");

                //list of segmentation
                string[] segmfolders = Directory.GetDirectories(itkFolder);

                if (_setOptions.IsSegmentation.ToLower().Equals("true"))
                {
                    foreach (string segmFolder in segmfolders)
                    {
                        //create segmentation folder
                        string segmFoldName = Common.GetFolderName(segmFolder);
                        string segmFoldPath = Path.Combine(personalization, segmFoldName);
                        Directory.CreateDirectory(segmFoldPath);

                        //create enterprise folder
                        string enterpriseFoldPath = Path.Combine(segmFoldPath, "Enterprise");
                        Directory.CreateDirectory(enterpriseFoldPath);

                        //list of component
                        string[] compFolders = Directory.GetDirectories(segmFolder);

                        Common.GenerateAndSaveFileFromPath(compFolders, enterpriseFoldPath);
                    }
                    _setOptions.IsGenSucceed = true;
                    bStatusFlg = true;
                }

                else if (_setOptions.IsSegmentation.ToLower().Equals("false"))
                {
                    string[] compFolders = Directory.GetDirectories(itkFolder);
                    Common.GenerateAndSaveFileFromPath(compFolders, personalization);
                    _setOptions.IsGenSucceed = true;
                    bStatusFlg = true;
                }
                else
                {
                    bStatusFlg = false;
                }

            }
            catch (Exception ex)
            {
                Logger.LogTrackMessage("ERROR in Generate.GenerateXmlFromPath(): " + ex.Message);
                bStatusFlg = false;
            }
            return bStatusFlg;
        }
    }
}
