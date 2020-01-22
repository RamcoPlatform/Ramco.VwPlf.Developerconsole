using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Xml.Linq;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using System.Xml;

using Ramco.VwPlf.DataAccess;
using Interop.Preview20;
using Interop.RVWRepAspxGen;
using Ramco.Plf.XamlGenerator;
using Ramco.VwPlf.Generator;
using Ramco.Plf.Layout.Generator;
using Ramco.Glance.UiGenerator;
using Ramco.VW.PLF.MDCFGenerator;



namespace Ramco.VwPlf.CodeGenerator
{
    internal class ThirdPartyGenerator
    {
        string _sLogFile = string.Empty;
        Logger _logger = null;
        ECRLevelOptions _ecrOptions = null;
        DBManager _dbManager = null;
        Guid _guid;


        public ThirdPartyGenerator(Guid guid, ECRLevelOptions ecrOptions, Ramco.VwPlf.DataAccess.DBManager dbManager)
        {
            this._guid = guid;
            this._ecrOptions = ecrOptions;
            this._sLogFile = System.IO.Path.Combine(_ecrOptions.GenerationPath, _ecrOptions.Platform, _ecrOptions.Customer, _ecrOptions.Project, _ecrOptions.Ecrno, _ecrOptions.Ecrno + ".txt");
            this._logger = new Logger(_sLogFile, ObjectType.ThirdParty);
            this._dbManager = dbManager;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="htm"></param>
        /// <param name="sLayoutXmlFilePath"></param>
        /// <returns></returns>
        private string BuildOptionsXmlForExt2(Htm htm)
        {
            XDocument OptionXml = XDocument.Parse("<options/>");
            try
            {
                string sReleaseDir = Path.Combine(_ecrOptions.GenerationPath, _ecrOptions.Platform, _ecrOptions.Customer, _ecrOptions.Project, _ecrOptions.Ecrno, "Updated", _ecrOptions.Component, "Release");

                XElement xElmOptions = Common.GetSingleElembyName(OptionXml, "options");
                Common.AddElementAndAttribute(xElmOptions, "option", "componentname", _ecrOptions.Component);
                Common.AddElementAndAttribute(xElmOptions, "option", "componentdescription", _ecrOptions.ComponentDesc);
                Common.AddElementAndAttribute(xElmOptions, "option", "activityname", htm.activityname);
                Common.AddElementAndAttribute(xElmOptions, "option", "activitydescription", htm.activitydesc);
                Common.AddElementAndAttribute(xElmOptions, "option", "ilbocode", htm.uiname);
                Common.AddElementAndAttribute(xElmOptions, "option", "ilbodescription", htm.uidesc);
                Common.AddElementAndAttribute(xElmOptions, "option", "physicaldirectory", Path.Combine(_ecrOptions.GenerationPath, _ecrOptions.Platform, _ecrOptions.Customer, _ecrOptions.Project, _ecrOptions.Ecrno, "Updated", _ecrOptions.Component, "Release"));
                Common.AddElementAndAttribute(xElmOptions, "option", "uiconfigxmlfilename", htm.LayoutXmlFile);
                Common.AddElementAndAttribute(xElmOptions, "option", "generationleveloption", "");
                Common.AddElementAndAttribute(xElmOptions, "option", "version", "");
                Common.AddElementAndAttribute(xElmOptions, "option", "generatehtm", Convert.ToString(string.Compare(htm.html, "y", true) == 0));
                Common.AddElementAndAttribute(xElmOptions, "option", "generatejs", Convert.ToString(string.Compare(htm.html, "y", true) == 0));
                Common.AddElementAndAttribute(xElmOptions, "option", "generatexml", Convert.ToString(_ecrOptions.OptionXML.options.Where(att => att.alltaskdata != null && att.alltaskdata.ToLower().Equals("y")).Any()));
                Common.AddElementAndAttribute(xElmOptions, "option", "rthtml", "True");
                Common.AddElementAndAttribute(xElmOptions, "option", "rtgif", Convert.ToString(_ecrOptions.OptionXML.options.Where(att => att.rtgif != null && att.rtgif.ToLower().Equals("y")).Any()));
                Common.AddElementAndAttribute(xElmOptions, "option", "ilrtlite", "False");
                //Common.AddElementAndAttribute(xElmOptions, "option", "inplacegeneration", "False");
                Common.AddElementAndAttribute(xElmOptions, "option", "selectlink", "False");
                Common.AddElementAndAttribute(xElmOptions, "option", "extensionjs", "False");
                Common.AddElementAndAttribute(xElmOptions, "option", "tabheading", "False");
                Common.AddElementAndAttribute(xElmOptions, "option", "environment", _ecrOptions.Platform);
                Common.AddElementAndAttribute(xElmOptions, "option", "wizardhtml", "False");
                Common.AddElementAndAttribute(xElmOptions, "option", "pintab", "False");
                Common.AddElementAndAttribute(xElmOptions, "option", "abstarget", "False");
                Common.AddElementAndAttribute(xElmOptions, "option", "impdeliverables", "False");
                Common.AddElementAndAttribute(xElmOptions, "option", "selallforgridcheckbox", Convert.ToString(_ecrOptions.OptionXML.options.Where(att => att.selallforgridcheckbox != null && att.selallforgridcheckbox.ToLower().Equals("y")).Any()));
                Common.AddElementAndAttribute(xElmOptions, "option", "contextmenu", Convert.ToString(_ecrOptions.OptionXML.options.Where(att => att.contextmenu != null && att.contextmenu.ToLower().Equals("y")).Any()));
                Common.AddElementAndAttribute(xElmOptions, "option", "customisedhtmldeployment", "False");
                Common.AddElementAndAttribute(xElmOptions, "option", "extuipath", string.Empty);
                Common.AddElementAndAttribute(xElmOptions, "option", "allstyle", Convert.ToString(_ecrOptions.OptionXML.options.Where(att => att.allstyle != null && att.allstyle.ToLower().Equals("y")).Any()));
                Common.AddElementAndAttribute(xElmOptions, "option", "comments", true.ToString());
                Common.AddElementAndAttribute(xElmOptions, "option", "extjs", Convert.ToString(_ecrOptions.OptionXML.options.Where(att => att.extjs != null && att.extjs.ToLower().Equals("y")).Any()));
                Common.AddElementAndAttribute(xElmOptions, "option", "labelselect", Convert.ToString(_ecrOptions.OptionXML.options.Where(att => att.labelselect != null && att.labelselect.ToLower().Equals("y")).Any()));
                Common.AddElementAndAttribute(xElmOptions, "option", "alltaskdata", Convert.ToString(_ecrOptions.OptionXML.options.Where(att => att.alltaskdata != null && att.alltaskdata.ToLower().Equals("y")).Any()));
                Common.AddElementAndAttribute(xElmOptions, "option", "cellspacing", Convert.ToString(_ecrOptions.OptionXML.options.Where(att => att.cellspacing != null && att.cellspacing.ToLower().Equals("y")).Any()));
                Common.AddElementAndAttribute(xElmOptions, "option", "compresshtml", Convert.ToString(_ecrOptions.OptionXML.options.Where(att => att.compresshtml != null && att.compresshtml.ToLower().Equals("y")).Any()));
                Common.AddElementAndAttribute(xElmOptions, "option", "compressjs", Convert.ToString(_ecrOptions.OptionXML.options.Where(att => att.compressjs != null && att.compressjs.ToLower().Equals("y")).Any()));
                Common.AddElementAndAttribute(xElmOptions, "option", "inlinetab", "True");
                Common.AddElementAndAttribute(xElmOptions, "option", "split", Convert.ToString(_ecrOptions.OptionXML.options.Where(att => att.split != null && att.split.ToLower().Equals("y")).Any()));
            }
            catch (Exception ex)
            {
                _logger.WriteLogToFile("BuildOptionXml", ex.InnerException != null ? ex.InnerException.Message : ex.Message, bError: true);
            }
            return OptionXml.ToString();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sConnectionString"></param>
        /// <param name="lang"></param>
        /// <param name="htm"></param>
        /// <returns></returns>
        private string GenerateLayoutXml(string sConnectionString, Language lang, Htm htm)
        {
            string sReturnValue = string.Empty;
            try
            {
                this._logger.WriteLogToFile("GenerateLayoutXml", string.Format("Generating layout xml {0}_{1}.xml", htm.activityname, htm.uiname));

                int iCtxtLanguage = 1;
                int iCtxtOuInst = 1;
                //string sTrialBar = this._ecrOptions.OptionXML.options.Where(o => o.trialbar != null).First().trialbar;//value to be confirm
                string sTrialBar = "Both";
                string sSmartSpan = "y";
                string sStyleSheet = "RSGlobalStyles";
                string sSampleDataFrom = string.Empty;
                string sHtmlGenPath = Path.Combine(this._ecrOptions.GenerationPath, this._ecrOptions.Platform, this._ecrOptions.Customer, this._ecrOptions.Project, this._ecrOptions.Ecrno, "Updated", this._ecrOptions.Component, "Release", string.Format("{0}_{1}.xml", Common.InitCaps(htm.activityname), Common.InitCaps(htm.uiname)));
                bool bItkFlg = false;
                int iErrorId = 0;

                DataTable dtLayoutXml = null;
                List<string> lLayoutXml = new List<string>();

                DBManager dbManager = new DBManager(sConnectionString);
                var parameters = dbManager.CreateParameters(23);
                dbManager.AddParamters(parameters, 0, "@ctxt_language_in", iCtxtLanguage);
                dbManager.AddParamters(parameters, 1, "@ctxt_ouinstance_in", iCtxtOuInst);
                dbManager.AddParamters(parameters, 2, "@ctxt_service_in", "BulkGenerate");
                dbManager.AddParamters(parameters, 3, "@ctxt_user_in", "rvw20user");
                dbManager.AddParamters(parameters, 4, "@engg_act_descr_in", htm.activitydesc.Replace("&amp;", "&"));
                dbManager.AddParamters(parameters, 5, "@engg_actname_in", htm.activityname);
                dbManager.AddParamters(parameters, 6, "@engg_att_ui_cap_align_in", DBNull.Value);
                dbManager.AddParamters(parameters, 7, "@engg_att_ui_format_in", DBNull.Value);
                dbManager.AddParamters(parameters, 8, "@engg_att_ui_trail_bar_in", sTrialBar);
                dbManager.AddParamters(parameters, 9, "@engg_component_descr_in", this._ecrOptions.ComponentDesc);
                dbManager.AddParamters(parameters, 10, "@engg_customer_name_in", this._ecrOptions.Customer);
                dbManager.AddParamters(parameters, 11, "@engg_language_name_in", lang.desc);
                dbManager.AddParamters(parameters, 12, "@engg_project_name_in", this._ecrOptions.Project);
                dbManager.AddParamters(parameters, 13, "@engg_req_no_in", this._ecrOptions.Ecrno);
                dbManager.AddParamters(parameters, 14, "@engg_smartspan_in", sSmartSpan);
                dbManager.AddParamters(parameters, 15, "@engg_stylesheet_in", sStyleSheet);
                dbManager.AddParamters(parameters, 16, "@engg_virdir_in", DBNull.Value);
                dbManager.AddParamters(parameters, 17, "@guid_in", this._guid);
                dbManager.AddParamters(parameters, 18, "@engg_ui_descr_in", htm.uidesc.Replace("&amp;", "&"));
                dbManager.AddParamters(parameters, 19, "@sample_data_from", sSampleDataFrom);
                dbManager.AddParamters(parameters, 20, "@html_gen_path", this._ecrOptions.GenerationPath);
                dbManager.AddParamters(parameters, 21, "@itk_flag", bItkFlg);
                dbManager.AddParamters(parameters, 22, "@m_errorid", iErrorId);
                try
                {
                    dbManager.Open();
                    dtLayoutXml = dbManager.ExecuteDataTable(CommandType.StoredProcedure, "de_generate_uixml", parameters);
                    dbManager.Close();
                }
                catch (SqlException ex)
                {
                    this._logger.WriteLogToFile("SP Error", Convert.ToString(ex));
                }

                int i = 0;
                XElement configNode = null;
                foreach (DataRow drLayoutXml in dtLayoutXml.Rows)
                {
                    if (i != 0)
                        lLayoutXml.Add(Convert.ToString(drLayoutXml[0]));
                    else
                        configNode = XDocument.Parse(Convert.ToString(drLayoutXml[0])).Root;
                    i++;
                }
                if (lLayoutXml.Count != 0)
                {
                    //get details from <configuration> node
                    //htm.isDevice = configNode.Attribute("IsDevice").Value.ToLower().Equals("y");

                    try
                    {
                        Common.CreateDirectory(Path.GetDirectoryName(sHtmlGenPath), true);
                        //File.WriteAllLines(sHtmlGenPath, lLayoutXml, System.Text.Encoding.Unicode);
                        using (FileStream fs = File.Create(sHtmlGenPath))
                        {
                            byte[] info = new UTF8Encoding(true).GetBytes(string.Join("\n", lLayoutXml.ToArray()));
                            fs.Write(info, 0, info.Length);
                        }

                        System.Xml.Linq.XDocument xdoc = System.Xml.Linq.XDocument.Load(sHtmlGenPath);
                        sReturnValue = sHtmlGenPath;
                    }
                    catch (System.Xml.XmlException ex)
                    {
                        this._logger.WriteLogToFile("GenerateLayoutXml", string.Format("Invalid layout xml for the component:{0},activity:{1},ui:{2},exception{3}", this._ecrOptions.Component, htm.activityname, htm.uiname, ex.ToString()), bError: true);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error while writing layout xml.");
                this._logger.WriteLogToFile("GenerateLayoutXml", Convert.ToString(ex));
            }
            return sReturnValue;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sConnectionString"></param>
        /// <returns></returns>
        public bool GenerateDesktopXMLs(string sConnectionString)
        {
            bool bSuccessFlg = false;
            try
            {
                this._logger.WriteLogToFile("GenerateDesktopXMLs", "Desktop XML generation starts...", bLogTiming: true);

                XDocument _Element = new XDocument(new XElement("codegeneration",
                                                        new XElement("model",
                                                                new XAttribute("customer", this._ecrOptions.OptionXML.model.customer), new XAttribute("project", this._ecrOptions.OptionXML.model.project),
                                                                new XAttribute("ecrno", this._ecrOptions.OptionXML.model.ecrno), new XAttribute("component", this._ecrOptions.OptionXML.model.component),
                                                                new XAttribute("componentdesc", this._ecrOptions.OptionXML.model.componentdesc), new XAttribute("appliation_rm_type", this._ecrOptions.OptionXML.model.project),
                                                                new XAttribute("generationpath", this._ecrOptions.OptionXML.model.generationpath), new XAttribute("platform", this._ecrOptions.OptionXML.model.platform),
                                                                new XAttribute("connectionstring", sConnectionString),
                                                                new XAttribute("requestid", this._ecrOptions.OptionXML.model.requestid)),
                                                         new XElement("languages", from a in this._ecrOptions.OptionXML.languages
                                                                                   select new XElement("language", new XAttribute("id", a.id), new XAttribute("language", a.desc))),
                                                        new XElement("xml", new XAttribute("service", this._ecrOptions.OptionXML.service.dll.Equals("y") ? true : false),
                                                        new XAttribute("activity", _ecrOptions.OptionXML.activities.Count() > 0),
                                                        new XAttribute("error", this._ecrOptions.OptionXML.service.error.Equals("y") ? true : false),
                                                        new XAttribute("taskservice", _ecrOptions.OptionXML.activities.Count() > 0),
                                                        new XAttribute("mhub2", _ecrOptions.OptionXML.options.Where(o => o.mhub2 != null && o.mhub2 == "y").Any() ? true : false)),
                                                        new XElement("activities", from act in this._ecrOptions.OptionXML.activities
                                                                                   select new XElement("activity", new XAttribute("name", act.name), new XAttribute("value", true))),
                                                        new XElement("htms", from htm in this._ecrOptions.OptionXML.htmls.Where(h => string.Compare(h.html, "y", true) == 0)
                                                                             select new XElement("htm", new XAttribute("activityname", htm.activityname), new XAttribute("uiname", htm.uiname))),
                                                        new XElement("logging", new XAttribute("trace", true), new XAttribute("log", true))));

                if ((new PlfGenerator()).Generator(_Element.ToString()))
                {
                    this._logger.WriteLogToFile("GenerateDesktopDeliverables", "Desktop deliverables generation ends.");
                    bSuccessFlg = true;
                }
            }
            catch (Exception ex)
            {
                bSuccessFlg = false;

                this._ecrOptions.ErrorCollection.Add(new Error(ObjectType.DesktopXml, "", ex.InnerException != null ? ex.InnerException.Message : ex.Message));

                this._logger.WriteLogToFile("GenerateDesktopDeliverables", Convert.ToString(ex), bError: true);
            }
            return bSuccessFlg;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="htm"></param>
        /// <param name="sLayoutXMLFullFilePath"></param>
        /// <returns></returns>
        public bool GenerateDesktopXAML(Htm htm)
        {
            bool bReturnValue = false;
            try
            {
                this._logger.WriteLogToFile("GenerateDesktopXAML", string.Format("Generating Desktop XAML for Activity: {0}, UI:{1}", htm.activityname, htm.uiname));

                string sSourceDir = Path.Combine(this._ecrOptions.GenerationPath, this._ecrOptions.Platform, this._ecrOptions.Customer, this._ecrOptions.Project, this._ecrOptions.Ecrno, "Updated", this._ecrOptions.Component, "Source", "Presentation");
                string sReleaseDir = Path.Combine(this._ecrOptions.GenerationPath, this._ecrOptions.Platform, this._ecrOptions.Customer, this._ecrOptions.Project, this._ecrOptions.Ecrno, "Updated", this._ecrOptions.Component, "Release", "VWDesktop_Data", "PL_Data", this._ecrOptions.Component, "Presentation");
                string sMajorFileVersion = "1";
                string sMinorFileVersion = "0";
                string sBuildVersion = "0";
                string sBinDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, this._ecrOptions.ThirdPartyGeneratorSourcePath);
                bool bGridAutoGrowVertically = true;
                bool bGridAutoGrowHorizontally = true;

                Common.CreateDirectory(sSourceDir);
                Common.CreateDirectory(sReleaseDir);

                CRvwILBOWPFXAMLGen XAMLGenerator = new CRvwILBOWPFXAMLGen();
                XAMLGenerator.LinkType = 1;
                if (XAMLGenerator.GenerateXamlFromFile(htm.LayoutXmlFile, sReleaseDir, sMajorFileVersion, sMinorFileVersion, sBuildVersion, sBinDir, sSourceDir, bGridAutoGrowVertically, bGridAutoGrowHorizontally))
                {
                    bReturnValue = true;
                }
                else
                {
                    this._logger.WriteLogToFile("GenerateDesktopXAML", "XAML generation failed");
                    bReturnValue = false;
                }
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("GenerateDesktopXAML->{0}", ex.InnerException != null ? ex.InnerException.Message : ex.Message));
            }
            return bReturnValue;
        }

        private bool GenerateHtmVersionUserJs(Htm htm)
        {
            bool bReturnValue = false;
            try
            {
                StringBuilder sb = new StringBuilder();
                string sTargetDir = Path.Combine(Path.GetDirectoryName(htm.LayoutXmlFile), "ILBO");

                sb.AppendLine("/**********************************************************************************************");
                sb.AppendLine("*  Copyright @ 2000 RAMCO SYSTEMS,  All rights reserved.");
                sb.AppendLine(string.Format("*  File Name            : {0}_{1}_user.js", Common.InitCaps(htm.activityname), Common.InitCaps(htm.uiname)));
                sb.AppendLine("*  Author(s) Name(s)    : Platform Code Generator");
                sb.AppendLine(string.Format("*  Platform             : {0}/{1}/{2}/{3}/{4}", _ecrOptions.Model, _ecrOptions.DB, _ecrOptions.Customer, _ecrOptions.Project, _ecrOptions.Ecrno));
                sb.AppendLine("* *********************************************************************************************/");

                sb.AppendLine(" //  Global variables");
                sb.AppendLine(string.Format("componentName = \"{0}\";", _ecrOptions.Component.ToLower()));
                sb.AppendLine(string.Format("componentDesc = \"{0}\";", _ecrOptions.ComponentDesc));
                sb.AppendLine(string.Format("activityName = \"{0}\";", htm.activityname.ToLower()));
                sb.AppendLine(string.Format("activityDesc = \"{0}\";", htm.activitydesc));
                sb.AppendLine(string.Format("ilboName = \"{0}\";", htm.uiname.ToLower()));
                sb.AppendLine(string.Format("ilboDesc = \"{0}\";", htm.uidesc));
                sb.AppendLine(string.Format("TrailILBODesc = \"{0}\";", htm.uidesc));
                sb.AppendLine("bMainPage = 1;");
                sb.AppendLine("var isOcx = 0;");
                sb.AppendLine("inlineTab = 1;");

                sb.AppendLine("//---------------------------------------------------------------------------------------------");
                sb.AppendLine("//  Function Name   :   preTaskSubmit");
                sb.AppendLine("//  Description     :   This function defines the actions which are performed before the task ");
                sb.AppendLine("//                      submit");
                sb.AppendLine("//  Arguments       :   sTaskName");
                sb.AppendLine("//  Return Value    :   true/false");
                sb.AppendLine("//---------------------------------------------------------------------------------------------");
                sb.AppendLine("function preTaskSubmit(sTaskName)");
                sb.AppendLine("{");
                sb.AppendLine(string.Format("{0}var bFlg = true;", String.Concat(Enumerable.Repeat("\t", 1))));
                sb.AppendLine(string.Format("{0}sTaskName = sTaskName.toLowerCase();", String.Concat(Enumerable.Repeat("\t", 1))));

                sb.AppendLine(string.Format("{0}return bFlg;", String.Concat(Enumerable.Repeat("\t", 1))));
                sb.AppendLine("}");

                sb.AppendLine("//---------------------------------------------------------------------------------------------");
                sb.AppendLine("//  Function Name   :   postTaskResultProcess");
                sb.AppendLine("//  Description     :   This function defines the actions which are performed after the task");
                sb.AppendLine("//                      submit");
                sb.AppendLine("//  Arguments       :   sTaskName");
                sb.AppendLine("//  Return Value    :   true/false");
                sb.AppendLine("//---------------------------------------------------------------------------------------------");
                sb.AppendLine("function postTaskResultProcess(sTaskName)");
                sb.AppendLine("{");
                sb.AppendLine(string.Format("{0}var bFlg = true;", String.Concat(Enumerable.Repeat("\t", 1))));
                sb.AppendLine(string.Format("{0}sTaskName = sTaskName.toLowerCase();", String.Concat(Enumerable.Repeat("\t", 1))));

                sb.AppendLine(string.Format("{0}if (CheckError() == false)", String.Concat(Enumerable.Repeat("\t", 1))));
                sb.AppendLine(string.Format("{0}return;", String.Concat(Enumerable.Repeat("\t", 1))));

                XDocument xLayoutXmlDoc = XDocument.Load(htm.LayoutXmlFile);
                IEnumerable<XElement> tasks = xLayoutXmlDoc
                                                .Descendants("Activity")
                                                .Where(act => string.Compare(htm.activityname, act.Attribute("Name").Value, true) == 0)
                                                .Descendants("ILBOs")
                                                .Descendants("ILBO")
                                                .Where(ui => string.Compare(htm.uiname, ui.Attribute("Name").Value, true) == 0)
                                                .Descendants("Tasks")
                                                .Descendants("Task")
                                                .Where(task => !string.IsNullOrEmpty(task.Attribute("StatusMessage").Value))
                                                .Concat(new List<XElement>() { new XElement("Task", new XAttribute[] { new XAttribute("Name", "tskstdscreeninit"), new XAttribute("StatusMessage", "Default Fetch Successfully Completed") }) });



                if (tasks.Count() > 0)
                {
                    sb.AppendLine(string.Format("{0}switch (sTaskName)", string.Concat(Enumerable.Repeat("\t", 1))));
                    sb.AppendLine(string.Concat(Enumerable.Repeat("\t", 1)) + "{");
                    foreach (XElement task in tasks)
                    {
                        sb.AppendLine(string.Format("{0}case \"{1}\"", string.Concat(Enumerable.Repeat("\t", 2)), task.Attribute("Name").Value));
                        sb.AppendLine(string.Format("{0}sTaskStatusMsg = \"{1}\";", string.Concat(Enumerable.Repeat("\t", 2)), task.Attribute("StatusMessage").Value));
                        sb.AppendLine(string.Format("{0}break;", string.Concat(Enumerable.Repeat("\t", 2))));
                    }
                    sb.AppendLine(string.Concat(Enumerable.Repeat("\t", 1)) + "}");
                }
                sb.AppendLine("}");

                sb.AppendLine("\n");

                sb.AppendLine("//---------------------------------------------------------------------------------------------");
                sb.AppendLine("//  Function Name   :   onVisibleDataSetChanged");
                sb.AppendLine("//  Description     :   This function defines the actions which are performed after changing");
                sb.AppendLine("//                      the visible data set in a grid");
                sb.AppendLine("//  Arguments       :   sGridName, nVisibleSetStartIndex");
                sb.AppendLine("//  Return Value    :   true/false");
                sb.AppendLine("//---------------------------------------------------------------------------------------------");
                sb.AppendLine("function onVisibleDataSetChanged(sGridName, nVisibleSetStartIndex)");
                sb.AppendLine("{");
                sb.AppendLine("}");

                sb.AppendLine("\n");

                sb.AppendLine("function CheckError()");
                sb.AppendLine("{");
                sb.AppendLine(string.Format("{0}var eltMessageInfo = xmlTaskResponseInfo.selectSingleNode(\"mi\");", String.Concat(Enumerable.Repeat("\t", 1))));
                sb.AppendLine(string.Format("{0}if (eltMessageInfo)", String.Concat(Enumerable.Repeat("\t", 1))));
                sb.AppendLine(string.Concat(Enumerable.Repeat("\t", 1)) + "{");
                sb.AppendLine(string.Format("{0}var eltMessage = eltMessageInfo.getElementsByTagName(\"msg\");", String.Concat(Enumerable.Repeat("\t", 2))));
                sb.AppendLine(string.Format("{0}if (eltMessage.length > 0)", String.Concat(Enumerable.Repeat("\t", 2))));
                sb.AppendLine(string.Concat(Enumerable.Repeat("\t", 2)) + "{");
                sb.AppendLine(string.Format("{0}if (eltMessageInfo.selectSingleNode(\"msg[@sr='FRX']\") || eltMessageInfo.selectSingleNode(\"msg[@sr='frx']\"))", String.Concat(Enumerable.Repeat("\t", 3))));
                sb.AppendLine(string.Format("{0}return false;", String.Concat(Enumerable.Repeat("\t", 3))));
                sb.AppendLine(string.Format("{0}else", String.Concat(Enumerable.Repeat("\t", 3))));
                sb.AppendLine(string.Concat(Enumerable.Repeat("\t", 3)) + "{");
                sb.AppendLine(string.Format("{0}if (eltMessageInfo.selectSingleNode(\"msg[@sv='5']\") || eltMessageInfo.selectSingleNode(\"msg[@sv='Error']\"))", String.Concat(Enumerable.Repeat("\t", 4))));
                sb.AppendLine(string.Format("{0}return false;", String.Concat(Enumerable.Repeat("\t", 4))));
                sb.AppendLine(string.Concat(Enumerable.Repeat("\t", 3)) + "}");

                sb.AppendLine(string.Concat(Enumerable.Repeat("\t", 2)) + "}");
                sb.AppendLine(string.Concat(Enumerable.Repeat("\t", 1)) + "}");
                sb.AppendLine(string.Format("{0}return true;", string.Concat(Enumerable.Repeat("\t", 1))));
                sb.AppendLine("}");

                Common.CreateDirectory(sTargetDir);
                File.WriteAllText(Path.Combine(sTargetDir, string.Format("{0}_{1}.js", Common.InitCaps(htm.activityname), Common.InitCaps(htm.uiname))), sb.ToString());

                bReturnValue = true;
            }
            catch (Exception ex)
            {
                _ecrOptions.ErrorCollection.Add(new Error(ObjectType.Ext2, "GenerateHtmVersionUserJs", ex.InnerException != null ? ex.InnerException.Message : ex.Message));
                _logger.WriteLogToFile("GenerateHtmVersionUserJs", ex.InnerException != null ? ex.InnerException.Message : ex.Message, bError: true);
                bReturnValue = false;
            }
            return bReturnValue;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="htm"></param>
        /// <param name="sLayoutXmlFilePath"></param>
        /// <returns></returns>
        private bool GenerateExt2Deliverables(Htm htm)
        {
            bool bReturnValue = false;
            try
            {
                string sReformedOptionXml = BuildOptionsXmlForExt2(htm);

                _logger.WriteLogToFile("preview", sReformedOptionXml);

                Preview preview = new Preview();
                preview.SetOptions(sReformedOptionXml);
                preview.GeneratePreview();

            }
            catch (Exception ex)
            {
                _ecrOptions.ErrorCollection.Add(new Error(ObjectType.Ext2, "Ext2Deliverables", ex.InnerException != null ? ex.InnerException.Message : ex.Message));

                _logger.WriteLogToFile("GenerateExt2Deliverables", ex.InnerException != null ? ex.InnerException.Message : ex.Message, bError: true);

                bReturnValue = false;
            }
            return bReturnValue;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="htm"></param>
        /// <param name="sLayoutXmlFile"></param>
        /// <returns></returns>
        private bool GenerateMhubDeliverables(Htm htm)
        {
            bool bSuccessFlg = false;
            try
            {
                _logger.WriteLogToFile("GenerateMhubDeliverables", "Generating MHUB deliverables...");

                bool bIpad = false;
                bool bIphone = true;
                string sDeviceConfigXml = (from node in _ecrOptions.OptionXML.options
                                           where !Object.Equals(node.deviceconfigpath, null)
                                           select node).First().deviceconfigpath;
                string sMode = "html";
                string sVersion = "2.0";
                string sReleaseDir = Path.Combine(_ecrOptions.GenerationPath, _ecrOptions.Platform, _ecrOptions.Customer, _ecrOptions.Project, _ecrOptions.Ecrno, "Updated", _ecrOptions.Component, "Release");
                string sInputXml = htm.LayoutXmlFile;
                string sTargetDir = Path.Combine(sReleaseDir, "mHub");
                string sLanguage = "1";
                string sIsProto = "false";
                string sLogPath = this._sLogFile;

                if (!string.IsNullOrEmpty(sDeviceConfigXml))
                {
                    try
                    {
                        sDeviceConfigXml = Path.Combine(sDeviceConfigXml, String.Format("{0}_{1}.xml", htm.activityname, htm.uiname));
                        if (!File.Exists(sDeviceConfigXml))
                            sDeviceConfigXml = string.Empty;
                        else
                        {
                            System.Xml.XmlDocument xdoc = new System.Xml.XmlDocument();
                            xdoc.Load(sDeviceConfigXml);
                        }
                    }
                    catch
                    {
                        sDeviceConfigXml = string.Empty;
                    }
                }

                Common.CreateDirectory(sTargetDir);
                try
                {
                    if (bIpad)
                    {
                        DeviceGenerator20.DeviceGen.Main(new string[] { "-tpad2",
                                                                            string.Format("-m{0}",sMode),
                                                                            string.Format("-v{0}",sVersion),
                                                                            string.Format("-i{0}",sInputXml),
                                                                            string.Format("-c{0}",sDeviceConfigXml),
                                                                            string.Format("-o{0}",sTargetDir),
                                                                            string.Format("-l{0}",sLanguage),
                                                                            string.Format("-p{0}",sIsProto),
                                                                            string.Format("-e{0}",sLogPath )});
                    }
                    if (bIphone)
                    {
                        DeviceGenerator20.DeviceGen.Main(new string[] { "-tphone",
                                                                            string.Format("-m{0}",sMode),
                                                                            string.Format("-v{0}",sVersion),
                                                                            string.Format("-i{0}",sInputXml),
                                                                            string.Format("-c{0}",sDeviceConfigXml),
                                                                            string.Format("-o{0}",sTargetDir),
                                                                            string.Format("-l{0}",sLanguage),
                                                                            string.Format("-p{0}",sIsProto),
                                                                            string.Format("-e{0}",sLogPath )});
                    }
                    bSuccessFlg = true;
                }
                catch (Exception ex)
                {
                    _logger.WriteLogToFile("DeviceGenerator20.DeviceGen.Main", Convert.ToString(ex), bError: true);
                    bSuccessFlg = false;
                }
            }
            catch (Exception ex)
            {
                bSuccessFlg = false;

                _ecrOptions.ErrorCollection.Add(new Error(ObjectType.MHUB, "MHub", ex.InnerException != null ? ex.InnerException.Message : ex.Message));

                _logger.WriteLogToFile("GenerateMhubDeliverables", Convert.ToString(ex));
            }
            return bSuccessFlg;
        }


        private bool GenerateGlanceDeliverables(Htm htm, Language language)
        {
            bool bSuccessFlg = false;

            bool bGlanceXmlGenerated;
            string sReleaseDir = Path.Combine(_ecrOptions.GenerationPath, _ecrOptions.Platform, _ecrOptions.Customer, _ecrOptions.Project, _ecrOptions.Ecrno, "Updated", _ecrOptions.Component, "Release", "Glance");
            string sGlanceXmlFullFilePath = Path.Combine(sReleaseDir, string.Format("{0}_{1}.xml", Common.InitCaps(htm.activityname), Common.InitCaps(htm.uiname)));
            string sContextMenu = _ecrOptions.OptionXML.options.Where(o => string.Compare(o.contextmenu, "y", true) == 0).Any() ? "true" : "false";
            string sGridCheck = _ecrOptions.OptionXML.options.Where(o => string.Compare(o.selallforgridcheckbox, "y", true) == 0).Any() ? "true" : "false";
            string sCompressJson = _ecrOptions.OptionXML.options.Where(o => string.Compare(o.compressjs, "y", true) == 0).Any() ? "true" : "false";
            if (!Directory.Exists(sReleaseDir))
                Directory.CreateDirectory(sReleaseDir);

            try
            {
                _logger.WriteLogToFile("GenerateGlanceDeliverables", string.Format("Starting GlanceXmlGeneration for the activity:{0},ui:{1}", htm.activityname, htm.uiname));

                UILayout layout = new UILayout(_dbManager.ConnectionString);
                string xml = layout.GetAsXml(_ecrOptions.Customer, _ecrOptions.Project, _ecrOptions.Ecrno, _ecrOptions.Component, htm.activityname, htm.uiname, language.id, "CODEGENERATION");
                layout.Save(sGlanceXmlFullFilePath, xml);

                bGlanceXmlGenerated = true;
            }
            catch (Exception ex1)
            {
                _ecrOptions.ErrorCollection.Add(new Error(ObjectType.Glance, "Glance", ex1.InnerException != null ? ex1.InnerException.Message : ex1.Message));
                _logger.WriteLogToFile("GenerateGlanceDeliverables", ex1.InnerException != null ? ex1.InnerException.Message : ex1.Message);
                bGlanceXmlGenerated = false;
            }

            if (bGlanceXmlGenerated)
            {
                try
                {
                    _logger.WriteLogToFile("GenerateGlanceDeliverables", string.Format("Starting Glance Generation for the activity:{0},ui:{1}", htm.activityname, htm.uiname));
                    cGenerator.GenerateLayout(sGlanceXmlFullFilePath, sReleaseDir, sContextMenu, sGridCheck, sCompressJson);
                    bSuccessFlg = true;
                }
                catch (Exception ex2)
                {
                    _ecrOptions.ErrorCollection.Add(new Error(ObjectType.Glance, "Glance", ex2.InnerException != null ? ex2.InnerException.Message : ex2.Message));
                    _logger.WriteLogToFile("GenerateGlanceDeliverables", ex2.InnerException != null ? ex2.InnerException.Message : ex2.Message);
                }
            }

            return bSuccessFlg;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="htm"></param>
        /// <param name="sLayoutXml"></param>
        /// <returns></returns>
        private bool GenerateMhub2Deliverables(Htm htm, string sLayoutXml)
        {
            bool bSuccessFlg = false;
            try
            {
                _logger.WriteLogToFile("GenerateMhub2Deliverables", "Generating MHUB2 deliverables...");

                string sReleaseDir = Path.Combine(_ecrOptions.GenerationPath, _ecrOptions.Platform, _ecrOptions.Customer, _ecrOptions.Project, _ecrOptions.Ecrno, "Updated", _ecrOptions.Component, "Release");

                string sDeviceConfigPath = (from node in _ecrOptions.OptionXML.options
                                            where !Object.Equals(node.deviceconfigpath, null)
                                            select node).First().deviceconfigpath;

                string sInputPath = sReleaseDir;
                string sOutputPath = Path.Combine(sReleaseDir, "mhub2");
                string sLangId = "1";
                string sVersion = "2.0";
                bool bIsProto = false;
                Common.CreateDirectory(sOutputPath);

                //note: when u r enabling the multi-language generation, provide the appropriate layout xml as input.
                //foreach (Language lang in GlobalVar.OptionXML.languages.language)
                //{
                //sLangId = lang.Id;
                //Ramco.mHub.MobileLayoutGenerator.PhoneUILayout phoneUILayout = new mHub.MobileLayoutGenerator.PhoneUILayout();
                //phoneUILayout.GenerateLayout(sInputFile, sDeviceConfigPath, sInputPath, sOutputPath, sLangId, sVersion, bIsProto);
                //}                    
                try
                {
                    if (string.Compare(htm.isnativeui, "y", true) == 0)
                    {
                        _logger.WriteLogToFile("GenerateMhub2Deliverables", "Xamarin Generation..");
                        _logger.WriteLogToFile("GenerateMhub2Deliverables", string.Format("LayoutXML:{0},DeviceConfigPath:{1},InputPath:{2},OutputPath:{3},LangId:{4},Version:{5},IsProto:{6}", sLayoutXml, sDeviceConfigPath, sInputPath, sOutputPath, sLangId, sVersion, bIsProto));
                        Ramco.Xam.MobileLayoutGenerator.XamPhoneUILayout xamPhoneUILayout = new Ramco.Xam.MobileLayoutGenerator.XamPhoneUILayout();
                        xamPhoneUILayout.GenerateLayout(sLayoutXml, sDeviceConfigPath, sInputPath, sOutputPath, sLangId, sVersion, bIsProto);
                    }
                    else
                    {
                        _logger.WriteLogToFile("GenerateMhub2Deliverables", "JS Generation..");
                        _logger.WriteLogToFile("GenerateMhub2Deliverables", string.Format("LayoutXML:{0},DeviceConfigPath:{1},InputPath:{2},OutputPath:{3},LangId:{4},Version:{5},IsProto:{6}", sLayoutXml, sDeviceConfigPath, sInputPath, sOutputPath, sLangId, sVersion, bIsProto));
                        Ramco.mHub.MobileLayoutGenerator.PhoneUILayout phoneUILayout = new mHub.MobileLayoutGenerator.PhoneUILayout();
                        phoneUILayout.GenerateLayout(sLayoutXml, sDeviceConfigPath, sInputPath, sOutputPath, sLangId, sVersion, bIsProto);
                    }

                    bSuccessFlg = true;
                }
                catch (Exception ex)
                {
                    _logger.WriteLogToFile("Ramco.mHub.MobileLayoutGenerator.PhoneUILayout.GenerateLayout", Convert.ToString(ex), bError: true);
                    bSuccessFlg = false;
                }

            }
            catch (Exception ex)
            {
                _ecrOptions.ErrorCollection.Add(new Error(ObjectType.MHUB2, "MHub2", ex.InnerException != null ? ex.InnerException.Message : ex.Message));
                _logger.WriteLogToFile("GenerateMhub2Deliverables", Convert.ToString(ex), bError: true);
                bSuccessFlg = false;
            }
            return bSuccessFlg;
        }

        private bool GenerateMdcfTemplates()
        {
            bool bSuccessFlg = false;
            try
            {
                string releaseDir = Path.Combine(_ecrOptions.GenerationPath, _ecrOptions.Platform, _ecrOptions.Customer, _ecrOptions.Project, _ecrOptions.Ecrno, "Updated", _ecrOptions.Component, "Release");
                string outputPath = Path.Combine(releaseDir, "MDCF");
                string logFilePath = Path.Combine(_ecrOptions.GenerationPath,_ecrOptions.Platform,_ecrOptions.Customer,_ecrOptions.Project,_ecrOptions.Ecrno);
                string logFileName = $"{_ecrOptions.Ecrno}.txt";


                if (!Directory.Exists(outputPath))
                    Directory.CreateDirectory(outputPath);

                GenerateMDCF mdcfGeneratorInstance = new GenerateMDCF();
                mdcfGeneratorInstance.GenerateMDCFTemplates(this._dbManager.ConnectionString, _ecrOptions.Customer, _ecrOptions.Project, _ecrOptions.Ecrno, outputPath , logFilePath, logFileName);
                bSuccessFlg = true;
            }
            catch (Exception ex)
            {
                _ecrOptions.ErrorCollection.Add(new Error(ObjectType.MDCFTemplates, "MDCF", ex.InnerException != null ? ex.InnerException.Message : ex.Message));
                _logger.WriteLogToFile("GenerateMdcfTemplates", Convert.ToString(ex), bError: true);
                bSuccessFlg = false;
            }
            return bSuccessFlg;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="htm"></param>
        /// <param name="sLayoutXml"></param>
        /// <returns></returns>
        private bool GenerateExjts6Deliverables(Htm htm)
        {
            bool bSuccessFlg = false;
            try
            {
                _logger.WriteLogToFile("GenerateExtjs6Deliverables", "Generating extjs6 deliverables....");

                string sReleaseDir = Path.Combine(_ecrOptions.GenerationPath, _ecrOptions.Platform, _ecrOptions.Customer, _ecrOptions.Project, _ecrOptions.Ecrno, "Updated", _ecrOptions.Component, "Release");
                string sOutputPath = Path.Combine(sReleaseDir, "hub");
                string sType = "model";
                string sContextMenu = (from node in _ecrOptions.OptionXML.options
                                       where !Object.Equals(node.contextmenu, null) && node.contextmenu.ToLower().Equals("y")
                                       select node).Any().ToString();
                string sGridCheck = (from node in _ecrOptions.OptionXML.options
                                     where !Object.Equals(node.selallforgridcheckbox, null) && node.selallforgridcheckbox.ToLower().Equals("y")
                                     select node).Any().ToString();
                string sCompression = (from node in _ecrOptions.OptionXML.options
                                       where !Object.Equals(node.compressjs, null) && node.compressjs.ToLower().Equals("y")
                                       select node).Any().ToString();
                string sLayoutType = "t";
                string sV3 = Boolean.FalseString;
                string bIsProto = Boolean.FalseString;
                string sConfigPath = (from node in _ecrOptions.OptionXML.options
                                      where !Object.Equals(node.deviceconfigpath, null)
                                      select node).First().deviceconfigpath;
                Common.CreateDirectory(sOutputPath);
                try
                {
                    Ramco.Plf.UiGenerator.Data.Generator.GenerateLayout(htm.LayoutXmlFile, sOutputPath, sType, sContextMenu, sGridCheck, sCompression, sLayoutType, sV3, "false", "dotnet", "");
                    bSuccessFlg = true;
                }
                catch (Exception ex)
                {
                    bSuccessFlg = false;

                    _ecrOptions.ErrorCollection.Add(new Error(ObjectType.Extjs6, "Extjs6", ex.InnerException != null ? ex.InnerException.Message : ex.Message));

                    _logger.WriteLogToFile("Ramco.Plf.UiGenerator.Data.Generator.GenerateLayout", Convert.ToString(ex), bError: true);
                }
            }
            catch (Exception ex)
            {
                bSuccessFlg = false;
                Console.WriteLine(ex);
                _logger.WriteLogToFile("GenerateExtjs6Deliverables", Convert.ToString(ex), bError: true);
            }
            return bSuccessFlg;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="htm"></param>
        /// <returns></returns>
        private bool GenerateReportAspx(Htm htm)
        {
            bool bReturnFlg = false;
            try
            {
                var Parameters = this._dbManager.CreateParameters(2);
                this._dbManager.AddParamters(Parameters, 0, "@Guid", this._guid);
                this._dbManager.AddParamters(Parameters, 1, "@EcrNo", this._ecrOptions.Ecrno);

                System.Data.DataTable dtAspxList = this._dbManager.ExecuteDataTable(System.Data.CommandType.StoredProcedure, "engg_devcon_gendeliv_AspxXml", Parameters);
                foreach (System.Data.DataRow drAspx in dtAspxList.Rows)
                {
                    string sActivity = Convert.ToString(drAspx["ActivityName"]);
                    string sUI = Convert.ToString(drAspx["UIName"]);
                    string sPage = Convert.ToString(drAspx["PageName"]);
                    string sTask = Convert.ToString(drAspx["TaskName"]);
                    string sReportFormat = string.Empty;
                    sReportFormat = this._ecrOptions.OptionXML.options.Where(o => !object.Equals(o.reportformat, null)).First().reportformat;

                    if (string.Compare(sReportFormat, "generic type", true) == 0)
                        sReportFormat = "vwreports";
                    else
                        sReportFormat = "Crystal";

                    Parameters = this._dbManager.CreateParameters(13);
                    this._dbManager.AddParamters(Parameters, 0, "@customer_name", this._ecrOptions.Customer);
                    this._dbManager.AddParamters(Parameters, 1, "@project_name", this._ecrOptions.Project);
                    this._dbManager.AddParamters(Parameters, 2, "@ecr_no", this._ecrOptions.Ecrno);
                    this._dbManager.AddParamters(Parameters, 3, "@process_name", this._ecrOptions.Process);
                    this._dbManager.AddParamters(Parameters, 4, "@component_name", this._ecrOptions.Component);
                    this._dbManager.AddParamters(Parameters, 5, "@activity_name", htm.activityname);
                    this._dbManager.AddParamters(Parameters, 6, "@ui_name", htm.uiname);
                    this._dbManager.AddParamters(Parameters, 7, "@page_name", sPage);
                    this._dbManager.AddParamters(Parameters, 8, "@task_name", sTask);
                    this._dbManager.AddParamters(Parameters, 9, "@Path", this._ecrOptions.GenerationPath);
                    this._dbManager.AddParamters(Parameters, 10, "@IncludePrintDate", true);
                    this._dbManager.AddParamters(Parameters, 11, "@extjs", this._ecrOptions.Extjs);
                    this._dbManager.AddParamters(Parameters, 12, "@report_type", sReportFormat);

                    System.Data.DataTable dtAspxXML = this._dbManager.ExecuteDataTable(System.Data.CommandType.StoredProcedure, "engg_generate_aspx_xml", Parameters);


                    if (dtAspxXML.Rows.Count > 2)//by default the resultset will contain two of these line <report></report>
                    {
                        string sXmlFileDir = Path.Combine(this._ecrOptions.GenerationPath, this._ecrOptions.Platform, this._ecrOptions.Customer, this._ecrOptions.Project, this._ecrOptions.Ecrno, "Updated", this._ecrOptions.Component, "Source", "Report");
                        string sReportDir = Path.Combine(this._ecrOptions.GenerationPath, this._ecrOptions.Platform, this._ecrOptions.Customer, this._ecrOptions.Project, this._ecrOptions.Ecrno, "Updated", this._ecrOptions.Component, "Release", "Report");
                        string sXmlFileName = string.Concat(sUI, "_", sTask, ".xml");

                        Common.CreateDirectory(sXmlFileDir);
                        Common.CreateDirectory(sReportDir);

                        //writing xml data required for aspx generation
                        using (FileStream fs = new FileStream(Path.Combine(sXmlFileDir, sXmlFileName), FileMode.Create))
                        {
                            using (StreamWriter sw = new StreamWriter(fs))
                            {
                                foreach (System.Data.DataRow drAspxXML in dtAspxXML.Rows)
                                {
                                    sw.WriteLine(Convert.ToString(drAspxXML[0]));
                                }
                            }
                        }

                        //calling third party report generator
                        clsRepASPX aspxGenerator = new clsRepASPX();
                        if (aspxGenerator.ProcessReport(File.ReadAllText(Path.Combine(sXmlFileDir, sXmlFileName))).Equals(1))
                        {
                            string serrorCode = aspxGenerator.GetErrorMsgCode;
                            string serrorMsg = aspxGenerator.GetErrorMsgDesc;
                            _logger.WriteLogToFile("GenerateReportAspx", string.Format("Process Report of RVWRepAspxGen failed. Error Code:{0}, Error Desc:{1} for the Activity:{2} Ui: {3}", serrorCode, serrorMsg, sActivity, sUI), bError: true);
                        }

                    }
                }
            }
            catch (Exception ex)
            {
                bReturnFlg = false;

                _ecrOptions.ErrorCollection.Add(new Error(ObjectType.Report, "ReportAspx", ex.InnerException != null ? ex.InnerException.Message : ex.Message));

                _logger.WriteLogToFile("GenerateReportAspx", ex.InnerException != null ? ex.InnerException.Message : ex.Message, bError: true);
            }
            return bReturnFlg;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public bool Generate()
        {
            bool bSuccessFlg = false;
            try
            {
                bool bDesktopDlv = _ecrOptions.OptionXML.options.Where(o => o.desktopdlv != null && o.desktopdlv.Equals("y")).Any();

                if (bDesktopDlv)
                    this.GenerateDesktopXMLs(this._dbManager.ConnectionString);

                foreach (Htm htm in this._ecrOptions.generation.htms.Where(h => h.html == "y"))
                {
                    foreach (Language lang in this._ecrOptions.OptionXML.languages)
                    {
                        string sLayoutXmlFilePath = this.GenerateLayoutXml(this._dbManager.ConnectionString, lang, htm);
                        htm.LayoutXmlFile = sLayoutXmlFilePath;

                        if (bDesktopDlv)
                            this.GenerateDesktopXAML(htm);

                        if (string.Compare(htm.isglanceui, "y", true) == 0)
                        {
                            this.GenerateGlanceDeliverables(htm, lang);
                        }
                        else if (string.IsNullOrEmpty(htm.isglanceui) || string.Compare(htm.isglanceui, "n", true) == 0)
                        {
                            if (_ecrOptions.OptionXML.options.Where(o => o.allstyle != null && o.allstyle.Equals("y")).Any())
                                this.GenerateHtmVersionUserJs(htm);

                            if (_ecrOptions.OptionXML.options.Where(o => o.extjs != null && o.extjs.Equals("y")).Any())
                                this.GenerateExt2Deliverables(htm);

                            if (_ecrOptions.OptionXML.options.Where(o => o.extjs6 != null && o.extjs6.Equals("y")).Any())
                                this.GenerateExjts6Deliverables(htm);

                            if (_ecrOptions.OptionXML.options.Where(o => o.iphone != null && o.iphone.Equals("y")).Any())
                                this.GenerateMhubDeliverables(htm);
                        }
                    }

                    if (htm.aspx.Equals("y") && htm.IsReportAspxGenerated == false)
                    {
                        this.GenerateReportAspx(htm);
                        htm.IsReportAspxGenerated = true;
                    }
                }

                if (_ecrOptions.OptionXML.options.Where(o => o.mhub2 != null && o.mhub2.Equals("y")).Any())
                {
                    foreach (Htm htm in this._ecrOptions.generation.htms.Where(h => h.html == "y"))
                    {
                        _logger.WriteLogToFile("htm.LayoutXmlFile : ", htm.LayoutXmlFile);
                        this.GenerateMhub2Deliverables(htm, htm.LayoutXmlFile);
                    }
                }

                //2.0.0.2 - for mdcf template generation
                this.GenerateMdcfTemplates();

                bSuccessFlg = true;
            }
            catch (Exception ex)
            {
                bSuccessFlg = false;
                Console.WriteLine(Convert.ToString(ex));
                _logger.WriteLogToFile("Generate", Convert.ToString(ex), bError: true);
            }
            return bSuccessFlg;
        }
    }
}
