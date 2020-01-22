using System;
using System.Linq;
using Ramco.VwPlf.DataAccess;
using System.Data;
using System.IO;
using System.Collections;
using Ramco.VwPlf.VwState.Generator;



namespace Ramco.VwPlf.CodeGenerator
{
    internal class XmlGenerator
    {
        Logger _logger = null;
        ECRLevelOptions _ecrOptions = null;
        DBManager _dbManager = null;
        Guid _guid;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="guid"></param>
        /// <param name="ecrOptions"></param>
        /// <param name="dbManager"></param>
        public XmlGenerator(Guid guid, ECRLevelOptions ecrOptions, DBManager dbManager)
        {
            this._guid = guid;
            this._ecrOptions = ecrOptions;
            this._logger = new Logger(System.IO.Path.Combine(_ecrOptions.GenerationPath, _ecrOptions.Platform, _ecrOptions.Customer, _ecrOptions.Project, _ecrOptions.Ecrno, _ecrOptions.Ecrno + ".txt"));
            this._dbManager = dbManager;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="htm"></param>
        /// <returns></returns>
        private bool GenerateChart(Htm htm)
        {
            bool bSuccessFlg = false;
            string sDirectory = string.Empty;
            string sFileNameWithExtn = string.Empty;
            try
            {
                _logger.WriteLogToFile("GenerateChartXml", String.Format("chart xml generation starts for activity:{0},ui:{1}", htm.activityname, htm.uiname));
                Hashtable paramAndValue = new Hashtable();
                paramAndValue.Add("@engg_customer_name", _ecrOptions.Customer);
                paramAndValue.Add("@engg_project_name", _ecrOptions.Project);
                paramAndValue.Add("@engg_req_no", _ecrOptions.Ecrno);
                paramAndValue.Add("@engg_component_name", _ecrOptions.Component);
                paramAndValue.Add("@engg_activity_name", htm.activityname);
                paramAndValue.Add("@engg_ui_name", htm.uiname);
                paramAndValue.Add("@seqno_tmp", 1000);
                paramAndValue.Add("@guid_tmp", this._guid);

                sDirectory = Path.Combine(_ecrOptions.GenerationPath, _ecrOptions.Platform, _ecrOptions.Customer, _ecrOptions.Project, _ecrOptions.Ecrno, "Updated", _ecrOptions.Component, "Release", "ILBO");
                sFileNameWithExtn = String.Format("{0}_{1}_ChartConfig.xml", Common.InitCaps(htm.activityname), Common.InitCaps(htm.uiname));

                Common.WriteResultSetToFile(this._dbManager.ConnectionString, "engg_devcon_gen_chartconfigxml", CommandType.StoredProcedure, paramAndValue, Path.Combine(sDirectory, sFileNameWithExtn),false);

                bSuccessFlg = true;
            }
            catch (Exception ex)
            {
                bSuccessFlg = false;

                _ecrOptions.ErrorCollection.Add(new Error(ObjectType.ChartXml, sFileNameWithExtn, ex.InnerException != null ? ex.InnerException.Message : ex.Message));

                _logger.WriteLogToFile("GenerateChartXml", ex.ToString(), bError: true);
            }

            return bSuccessFlg;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="htm"></param>
        /// <returns></returns>
        private bool GenerateRtState(Htm htm)
        {
            string sDirectory = string.Empty;
            string sFileNameWithExtn = string.Empty;
            bool bSuccessFlg = false;
            try
            {
                _logger.WriteLogToFile("GenerateRtStateXml", String.Format("RTState xml generation starts for activity:{0},ui:{1}", htm.activityname, htm.uiname));

                sDirectory = Path.Combine(_ecrOptions.GenerationPath, _ecrOptions.Platform, _ecrOptions.Customer, _ecrOptions.Project, _ecrOptions.Ecrno, "Updated", _ecrOptions.Component, "Release", "State");
                sFileNameWithExtn = String.Format("{0}_{1}.xml", htm.activityname, htm.uiname);
                var connectionKeyValuePairs = this._dbManager.ConnectionString.Split(';').Select(x => x.Split('=')).ToDictionary(x => x[0].ToLower(), x => x[1].ToLower());
                SetOptions _options = new SetOptions
                {
                    Type = "db",
                    Customer = _ecrOptions.Customer,
                    Project = _ecrOptions.Project,
                    Process = _ecrOptions.Process,
                    Component = _ecrOptions.Component,
                    ECRNo = _ecrOptions.Ecrno,
                    Activity = htm.activityname,
                    Ui = htm.uiname,
                    OutputPath = sDirectory,
                    Server = connectionKeyValuePairs["data source"],
                    Username = connectionKeyValuePairs["user id"],
                    Password = connectionKeyValuePairs["password"],
                    Database = connectionKeyValuePairs["database"],
                    InputPath = "INPUTPATH_DUMMY",
                    IsSegmentation = "SEGMENTATIONVALUE_DUMMY",
                    LogPath = Path.Combine(_ecrOptions.GenerationPath, _ecrOptions.Platform, _ecrOptions.Customer, _ecrOptions.Project, _ecrOptions.Ecrno, _ecrOptions.Ecrno + ".txt"),
                    LogError = "true",
                    LogSteps = "true",
                    dbManager = this._dbManager
                };

                RTStateGenerator rtStateGenerator = new RTStateGenerator(_options);
                rtStateGenerator.Generate();

                bSuccessFlg = true;
            }
            catch (Exception ex)
            {
                bSuccessFlg = false;

                _ecrOptions.ErrorCollection.Add(new Error(ObjectType.StateXml, sFileNameWithExtn, ex.InnerException != null ? ex.InnerException.Message : ex.Message));

                _logger.WriteLogToFile("GenerateRtStateXml", ex.InnerException != null ? ex.InnerException.Message : ex.ToString(), bError: true);
            }
            //logger.WriteLogToFile("GenerateRtStateXml", String.Format("----ends for activity:{0},ui:{1}", htm.activityname, htm.uiname));
            return bSuccessFlg;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="lang"></param>
        /// <param name="htm"></param>
        /// <returns></returns>
        private bool GeneratePivot(Language lang, Htm htm)
        {
            bool bStatusFlag = false;
            string sDirectory = string.Empty;
            string sFileNameWithExtension = string.Empty;
            try
            {
                this._logger.WriteLogToFile("GeneratePivotXml", string.Format("Generating pivot xml for the activity:{0},ui:{1},lang-id:{2}..", htm.activityname, htm.uiname, lang.id));

                Hashtable paramAndValue = new Hashtable();
                paramAndValue.Add("@engg_customer_name", this._ecrOptions.Customer);
                paramAndValue.Add("@engg_project_name", this._ecrOptions.Project);
                paramAndValue.Add("@engg_req_no", this._ecrOptions.Ecrno);
                paramAndValue.Add("@engg_component_name", this._ecrOptions.Component);
                paramAndValue.Add("@engg_activity_name", htm.activityname);
                paramAndValue.Add("@engg_ui_name", htm.uiname);
                paramAndValue.Add("@seqno_tmp", 1000);
                paramAndValue.Add("@guid_tmp", this._guid);
                paramAndValue.Add("@lang_id", lang.id);

                //Creating target directory
                sDirectory = Path.Combine(_ecrOptions.GenerationPath, _ecrOptions.Platform, _ecrOptions.Customer, _ecrOptions.Project, _ecrOptions.Ecrno, "Updated", _ecrOptions.Component, "Release", "ILBO");
                sFileNameWithExtension = string.Format("{0}_{1}{2}_RichCtlConfig.xml", htm.activityname, htm.uiname, lang.id.Equals("1") ? string.Empty : "_" + lang.id);

                Common.WriteResultSetToFile(this._dbManager.ConnectionString, "engg_devcon_gen_PivotConfigXML", CommandType.StoredProcedure, paramAndValue, Path.Combine(sDirectory, sFileNameWithExtension),false);
            }
            catch (Exception ex)
            {
                _ecrOptions.ErrorCollection.Add(new Error(ObjectType.PivotXml, sFileNameWithExtension, ex.InnerException != null ? ex.InnerException.Message : ex.Message));
                this._logger.WriteLogToFile("GeneratePivotXml", ex.InnerException != null ? ex.InnerException.Message : ex.Message, bError: true);
                bStatusFlag = false;
            }
            return bStatusFlag;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="htm"></param>
        /// <returns></returns>
        private bool GenerateDDT(Htm htm)
        {
            bool bFlag = false;
            string sDirectory = string.Empty;
            string sFileNameWithExtension = string.Empty;
            try
            {
                this._logger.WriteLogToFile("GenerateDDTXml", string.Format("Generating DDT xml for the Activity:{0},Ui:{1}...", htm.activityname, htm.uiname));

                Hashtable paramAndValue = new Hashtable();
                paramAndValue.Add("@engg_customer_name", this._ecrOptions.Customer);
                paramAndValue.Add("@engg_project_name", this._ecrOptions.Project);
                paramAndValue.Add("@engg_req_no", this._ecrOptions.Ecrno);
                paramAndValue.Add("@engg_component_name", this._ecrOptions.Component);
                paramAndValue.Add("@engg_activity_name", htm.activityname);
                paramAndValue.Add("@engg_ui_name", htm.uiname);
                paramAndValue.Add("@seqno_tmp", 1000);
                paramAndValue.Add("@guid_tmp", this._guid);

                //Creating target directory
                sDirectory = Path.Combine(_ecrOptions.GenerationPath, _ecrOptions.Platform, _ecrOptions.Customer, _ecrOptions.Project, _ecrOptions.Ecrno, "Updated", _ecrOptions.Component, "Release", "ILBO");
                sFileNameWithExtension = string.Format("{0}_{1}_datadriventask.xml", htm.activityname, htm.uiname);

                Common.WriteResultSetToFile(this._dbManager.ConnectionString, "engg_devcon_gen_datadriventaskxml", CommandType.StoredProcedure, paramAndValue, Path.Combine(sDirectory, sFileNameWithExtension),false);

                bFlag = true;
            }
            catch (Exception ex)
            {
                _ecrOptions.ErrorCollection.Add(new Error(ObjectType.DataDrivenTaskXml, sFileNameWithExtension, ex.InnerException != null ? ex.InnerException.Message : ex.Message));
                _logger.WriteLogToFile("GenerateDDTXml", ex.InnerException != null ? ex.InnerException.Message : ex.Message, bError: true);
                bFlag = false;
            }
            return bFlag;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="htm"></param>
        /// <returns></returns>
        private bool GenerateControlExtension(Htm htm)
        {
            bool bFlag = false;
            string sDirectory = string.Empty;
            string sFileNameWithExtension = string.Empty;
            try
            {
                this._logger.WriteLogToFile("GenerateControlExtensionXml", string.Format("Generating Control Extension xml for the Activity:{0},Ui:{1}...", htm.activityname, htm.uiname));

                Hashtable paramAndValue = new Hashtable();
                paramAndValue.Add("@customer", this._ecrOptions.Customer);
                paramAndValue.Add("@project", this._ecrOptions.Project);
                paramAndValue.Add("@ecrno", this._ecrOptions.Ecrno);
                paramAndValue.Add("@component", this._ecrOptions.Component);
                paramAndValue.Add("@activity", htm.activityname);
                paramAndValue.Add("@userinterface", htm.uiname);

                //Creating target directory
                sDirectory = Path.Combine(_ecrOptions.GenerationPath, _ecrOptions.Platform, _ecrOptions.Customer, _ecrOptions.Project, _ecrOptions.Ecrno, "Updated", _ecrOptions.Component, "Release", "ILBO");
                sFileNameWithExtension = string.Format("{0}_{1}_links.xml", Common.InitCaps(htm.activityname), Common.InitCaps(htm.uiname));

                Common.WriteResultSetToFile(this._dbManager.ConnectionString, "de_published_controlextension", CommandType.StoredProcedure, paramAndValue, Path.Combine(sDirectory, sFileNameWithExtension),false);

                bFlag = true;
            }
            catch (Exception ex)
            {
                _ecrOptions.ErrorCollection.Add(new Error(ObjectType.ControlExtensionXml, sFileNameWithExtension, ex.InnerException != null ? ex.InnerException.Message : ex.Message));
                _logger.WriteLogToFile("GenerateControlExtensionXml", ex.InnerException != null ? ex.InnerException.Message : ex.Message, bError: true);
                bFlag = false;
            }
            return bFlag;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="htm"></param>
        /// <returns></returns>
        private bool GenerateControlExtensionData(Htm htm)
        {
            bool bFlag = false;
            string sDirectory = string.Empty;
            string sFileNameWithExtension = string.Empty;
            try
            {
                this._logger.WriteLogToFile("GenerateControlExtensionDataXml", string.Format("Generating Control Extension Data xml for the Activity:{0},Ui:{1}...", htm.activityname, htm.uiname));

                Hashtable paramAndValue = new Hashtable();
                paramAndValue.Add("@customer", this._ecrOptions.Customer);
                paramAndValue.Add("@project", this._ecrOptions.Project);
                paramAndValue.Add("@ecrno", this._ecrOptions.Ecrno);
                paramAndValue.Add("@component", this._ecrOptions.Component);
                paramAndValue.Add("@activity", htm.activityname);
                paramAndValue.Add("@userinterface", htm.uiname);

                //Creating target directory
                sDirectory = Path.Combine(_ecrOptions.GenerationPath, _ecrOptions.Platform, _ecrOptions.Customer, _ecrOptions.Project, _ecrOptions.Ecrno, "Updated", _ecrOptions.Component, "Release", "ILBO");
                sFileNameWithExtension = string.Format("{0}_{1}_link_data.xml", Common.InitCaps(htm.activityname), Common.InitCaps(htm.uiname));

                Common.WriteResultSetToFile(this._dbManager.ConnectionString, "de_published_controlextension_data", CommandType.StoredProcedure, paramAndValue, Path.Combine(sDirectory, sFileNameWithExtension),false);

                bFlag = true;
            }
            catch (Exception ex)
            {
                _ecrOptions.ErrorCollection.Add(new Error(ObjectType.ControlExtensionDataXml, sFileNameWithExtension, ex.InnerException != null ? ex.InnerException.Message : ex.Message));
                _logger.WriteLogToFile("GenerateControlExtensionDataXml", ex.InnerException != null ? ex.InnerException.Message : ex.Message, bError: true);
                bFlag = false;
            }
            return bFlag;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sActivityName"></param>
        /// <returns></returns>
        private bool GenerateErrorLookup(string sActivityName)
        {
            bool bFlag = false;
            string sDirectory = string.Empty;
            string sFileNameWithExtension = string.Empty;
            try
            {
                this._logger.WriteLogToFile("GenerateErrorLookup", string.Format("Generating Error Lookup xml for the Activity:{0}...", sActivityName));

                Hashtable paramAndValue = new Hashtable();
                paramAndValue.Add("@engg_customer_name", this._ecrOptions.Customer);
                paramAndValue.Add("@engg_project_name", this._ecrOptions.Project);
                paramAndValue.Add("@engg_ecrno", this._ecrOptions.Ecrno);
                paramAndValue.Add("@engg_component_name", this._ecrOptions.Component);
                paramAndValue.Add("@engg_activity_name", sActivityName);

                //Creating target directory
                sDirectory = Path.Combine(_ecrOptions.GenerationPath, _ecrOptions.Platform, _ecrOptions.Customer, _ecrOptions.Project, _ecrOptions.Ecrno, "Updated", _ecrOptions.Component, "Release", "ILBO");
                sFileNameWithExtension = string.Format("{0}_errorlinkdata.xml", Common.InitCaps(sActivityName));

                Common.WriteResultSetToFile(this._dbManager.ConnectionString, "engg_devcon_gen_errorlookupconfigxml", CommandType.StoredProcedure, paramAndValue, Path.Combine(sDirectory, sFileNameWithExtension),false);

                bFlag = true;
            }
            catch (Exception ex)
            {
                _ecrOptions.ErrorCollection.Add(new Error(ObjectType.ErrorLookupXml, sFileNameWithExtension, ex.InnerException != null ? ex.InnerException.Message : ex.Message));
                _logger.WriteLogToFile("GenerateErrorLookup", ex.InnerException != null ? ex.InnerException.Message : ex.Message, bError: true);
                bFlag = false;
            }
            return bFlag;
        }
        public bool Generate()
        {
            try
            {
                foreach (string activityName in this._ecrOptions.generation.htms.Where(h => h.html == "y").Select(h => h.activityname).Distinct())
                {
                    foreach (Htm htm in this._ecrOptions.generation.htms.Where(h => string.Compare(h.activityname, activityName, true) == 0))
                    {
                        if (_ecrOptions.OptionXML.configxmls.chart.Equals("y"))
                            this.GenerateChart(htm);
                        if (_ecrOptions.OptionXML.configxmls.state.Equals("y"))
                            this.GenerateRtState(htm);
                        if (_ecrOptions.OptionXML.configxmls.ddt.Equals("y"))
                            this.GenerateDDT(htm);

                        if (_ecrOptions.OptionXML.configxmls.cexml.Equals("y"))
                        {
                            this.GenerateControlExtension(htm);
                            this.GenerateControlExtensionData(htm);
                        }

                        //foreach (Language lang in _ecrOptions.OptionXML.languages)
                        //    this.GeneratePivot(lang, htm);
                    }

                    if (_ecrOptions.OptionXML.configxmls.errorlookup.Equals("y"))
                        this.GenerateErrorLookup(activityName);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.WriteLogToFile("XmlGenerator.Generate", ex.ToString(), bError: true);
                return false;
            }
        }

    }
}
