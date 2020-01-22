using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ramco.VwPlf.DataAccess;
using System.Data;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.Threading.Tasks;


namespace Ramco.VwPlf.CodeGenerator
{

    public enum DataMode
    {
        Model = 0,
        Xml = 1
    }

    public class UserInputs
    {
        private DataMode _DataMode;
        private string _OptionXmlPath;

        public DataMode DataMode
        {
            get
            {
                return this._DataMode;
            }
            set
            {
                this._DataMode = value;
            }
        }

        public string OptionXmlPath
        {
            get
            {
                return this._OptionXmlPath;
            }
            set
            {
                this._OptionXmlPath = value;
            }
        }
    }

    public class CodegenWrapper
    {
        private Guid _guid;
        private string _codegenPath;
        private string _connectionString;
        private UserInputs _userInputs;
        //private Logger _logger;
        public List<string> _errCollection;
        private static object locker = new object();

        private IProgress<string> _progress = new Progress<string>();

        public string CodegenPath
        {
            get
            {
                return _codegenPath;
            }

            set
            {
                _codegenPath = value;
            }
        }

        public string ConnectionString
        {
            get
            {
                return _connectionString;
            }

            set
            {
                _connectionString = value;
            }
        }

        //public CodegenWrapper()
        //{

        //}

        /// <summary>
        /// Constructor to Instantiate Wrapper
        /// </summary>
        /// <param name="guid">guid associated with the codegeneration.</param>
        /// <param name="constr">connectionstring for the model backend from which codegeneration has to be done.</param>
        public CodegenWrapper(string constr)
        {
            //this._guid = guid;
            this._errCollection = new List<string>();

            if (!String.IsNullOrEmpty(constr))
            {
                this._connectionString = constr;
            }
            else if (!String.IsNullOrEmpty(_connectionString))
            {

            }
            else
            {
                //throw new Exception("Connection string should not be empty while initializing wrapper");
            }
        }

        public CodegenWrapper(Guid guid, string constr)
        {
            this._guid = guid;
            this._errCollection = new List<string>();

            if (!String.IsNullOrEmpty(constr))
            {
                this._connectionString = constr;
            }
            else if (!String.IsNullOrEmpty(_connectionString))
            {

            }
            else
            {
                // throw new Exception("Connection string should not be empty while initializing wrapper");
            }
        }

        public CodegenWrapper(Guid guid, string constr, IProgress<string> progress)
        {
            this._guid = guid;
            this._errCollection = new List<string>();
            this._progress = progress;

            if (!String.IsNullOrEmpty(constr))
            {
                this._connectionString = constr;
            }
            else if (!String.IsNullOrEmpty(_connectionString))
            {

            }
            else
            {
                // throw new Exception("Connection string should not be empty while initializing wrapper");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sOptionXMLPath"></param>
        /// <param name="ecrOptions"></param>
        /// <returns></returns>
        public bool SetECROptions(string sOptionXMLPath, ref ECRLevelOptions ecrOptions)
        {
            string sLastProcessedOption = string.Empty;
            try
            {
                codegeneration deSerializedOptions = (codegeneration)Common.DeserializeFromXML(typeof(codegeneration), sOptionXMLPath);

                ecrOptions.OptionXML = deSerializedOptions;
                this._guid = new Guid(ecrOptions.OptionXML.model.guid);
                ecrOptions.GenerationPath = this._codegenPath = deSerializedOptions.model.generationpath;
                ecrOptions.Ecrno = deSerializedOptions.model.ecrno;

                //first time log
                Logger.WriteLogToTraceListener("SetUserOptions", "Setting User Options...");

                ecrOptions.Customer = deSerializedOptions.model.customer;
                ecrOptions.Project = deSerializedOptions.model.project;
                ecrOptions.Process = deSerializedOptions.model.process;
                ecrOptions.Component = deSerializedOptions.model.component.ToLower();
                ecrOptions.ComponentDesc = deSerializedOptions.model.componentdesc;
                ecrOptions.Appliation_rm_type = deSerializedOptions.model.appliation_rm_type;
                ecrOptions.Platform = deSerializedOptions.model.platform;
                ecrOptions.RequestId = deSerializedOptions.model.requestid;
                ecrOptions.CodegenClient = deSerializedOptions.model.codegenclient;
                ecrOptions.RequestStart_Datetime = deSerializedOptions.model.requeststart_datetime;
                //ecrOptions.EncodedConnectionstring = deSerializedOptions.model.connectionstring;

                //htm options
                ecrOptions.Extjs = (from node in deSerializedOptions.options
                                    where !object.Equals(node.extjs, null) && node.extjs == "y"
                                    select node).Any();

                //activity
                //initially setting activity name from option xml
                ecrOptions.generation.activities = (from node in deSerializedOptions.activities
                                                    select new Activity(node.name.ToLower())
                                                   ).ToList<Activity>();

                //chosen activity
                ecrOptions.generation.htms = (from node in deSerializedOptions.htmls
                                              select node).ToList<Htm>();

                ecrOptions.GenerateActivity = ecrOptions.generation.activities.Count > 0;
                ecrOptions.GenerateService = deSerializedOptions.service.dll.Equals("y");
                ecrOptions.SeperateErrorDll = deSerializedOptions.service.error.Equals("y");
                ecrOptions.InTD = deSerializedOptions.service.intd.Equals("y");
                ecrOptions.PreviousGenerationPath = deSerializedOptions.model.previousgenerationpath;
                ecrOptions.TreatDefaultAsNull = (string.Compare(deSerializedOptions.service.defaultasnull, "y", true) == 0);

                try
                {
                    ecrOptions.LangIdList = (from node in deSerializedOptions.languages
                                             select node.id.ToString()).ToList<string>();
                }
                catch
                {
                    throw new Exception("Language info not available!");
                }

                //configXmlNode = deSerializedOptions.configxmls.configxml.First();
                //ecrOptions.ChartXml = configXmlNode.chart.Equals("y");
                //GlobalVar.MHub = GlobalVar.MHub2 = GlobalVar.Extjs6 = GlobalVar.DesktopDeliverables = true;

                ecrOptions.AssemblyDescription = string.Format("{0}/{1}/{2}/{3}/{4}/{5}/{6}",
                                                                ecrOptions.Customer,
                                                                ecrOptions.Project,
                                                                ecrOptions.Process,
                                                                ecrOptions.Ecrno,
                                                                ecrOptions.Component,
                                                                Environment.MachineName,
                                                                Convert.ToString(DateTime.Now.ToString()));
                //todo - check where it is used
                ecrOptions.ReleaseVersion = string.Empty;
                ecrOptions.CodegenClient = System.Environment.MachineName;

                if (_userInputs.DataMode.Equals(DataMode.Model))
                {
                    IEnumerable<KeyValuePair<string, string>> dicConString = this._connectionString.Split(';').Select(s => new KeyValuePair<string, string>(s.Split('=')[0].ToLower(), s.Split('=')[1]));
                    Dictionary<string, string> testConString = this._connectionString.Split(';').Select(s => new KeyValuePair<string, string>(s.Split('=')[0].ToLower(), s.Split('=')[1])).ToDictionary(x => x.Key, x => x.Value);
                    ecrOptions.Model = testConString["data source"].ToString();
                    ecrOptions.User = testConString["user id"];
                    ecrOptions.DB = testConString["database"];
                }


                //_logger = new Logger(Path.Combine(@"c:\temp", "bin", "Ramco_VwPlf_CodeGenerator", string.Format("{0}.txt", this._guid)));

                return true;
            }
            catch (NullReferenceException)
            {
                throw new Exception(string.Format("SetUserOptions->Some Option missing in Codegen Option XML."));
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("SetUserOptions->{0}",
                    !(object.Equals(ex.InnerException, null)) ? ex.InnerException.Message : ex.Message));
            }
        }

        #region needs to be place somewhere
        /// <summary>
        /// Generates necessary htm,xml and scripts
        /// </summary>
        /// <returns></returns>
        private bool GenerateHtmXmlAndScripts(DataPrepartion dataPreparation, UserInputs userInputs, ref ECRLevelOptions ecrOptions, Logger logger)
        {
            bool bStatus = false;
            try
            {
                //if (userInputs.DataMode == DataMode.Model)
                //{
                this._progress.Report(string.Format("Generating xml(s) from {0}...", ecrOptions.Ecrno));
                XmlGenerator xmlGenerator = new XmlGenerator(this._guid, ecrOptions, dataPreparation._dbManager);
                xmlGenerator.Generate();

                this._progress.Report(string.Format("Generating 3rd party deliverables for {0}...", ecrOptions.Ecrno));
                ThirdPartyGenerator thirdPartyGenerator = new ThirdPartyGenerator(this._guid, ecrOptions, dataPreparation._dbManager);
                thirdPartyGenerator.Generate();

                this._progress.Report(string.Format("Generating scripts for {0}...", ecrOptions.Ecrno));
                ScriptGenerator scriptGenerator = new ScriptGenerator(this._guid, ecrOptions, dataPreparation._dbManager);
                scriptGenerator.Generate();
                //}

                bStatus = true;
            }
            catch (Exception ex)
            {
                logger.WriteLogToFile("CodegenWrapper.GenerateHtmXmlAndScripts", string.Format("GenerateHtmXmlAndScripts->{0}", ex.InnerException != null ? ex.InnerException.Message : ex.Message));
            }
            return bStatus;
        }
        #endregion needs to be place somewhere

        /// <summary>
        /// Generates option xml
        /// </summary>
        /// <param name="ecrno">ecrno</param>
        /// <returns></returns>
        private string GenerateOptionXml(string ecrno)
        {
            string sOptionXmlPath = string.Empty;
            string sOptionxml = string.Empty;
            try
            {
                if (_userInputs.DataMode.Equals(DataMode.Model))
                {
                    DataTable dtOptionXml = null;
                    DBManager dbManager = new DBManager(this._connectionString);
                    var parameters = dbManager.CreateParameters(2);
                    dbManager.AddParamters(parameters, 0, "@guid", _guid);
                    dbManager.AddParamters(parameters, 1, "@ecrno", ecrno);
                    dbManager.Open();
                    dtOptionXml = dbManager.ExecuteDataTable(CommandType.StoredProcedure, "vw_netgen_optionxml_sp", parameters);
                    dbManager.Close();

                    sOptionxml = Convert.ToString(dtOptionXml.Rows[0]["gen_xml"]);

                }
                else if (_userInputs.DataMode.Equals(DataMode.Xml))
                {
                    sOptionxml = File.ReadAllText(_userInputs.OptionXmlPath);
                }

                XmlDocument xdoc = new XmlDocument();
                xdoc.LoadXml(sOptionxml);

                //getting target directory
                XmlNode nModel = xdoc.SelectSingleNode("/codegeneration/model");
                if (nModel == null)
                {
                    Logger.WriteLogToTraceListener("GenerateOptionXml", "Node < model > is missing from option XML.Unable to Proceed!.");
                    throw new Exception("Node <model> is missing from option XML. Unable to Proceed!.");
                }
                this._codegenPath = nModel.Attributes["generationpath"].Value;
                sOptionXmlPath = Path.Combine(this._codegenPath, ecrno + ".xml");

                //creating target directory
                Common.CreateDirectory(Path.GetDirectoryName(sOptionXmlPath));

                //Save option xml
                //xdoc.Save(sOptionXmlPath);

                Common.SaveXmlDocument(sOptionXmlPath, XDocument.Parse(sOptionxml));

                return sOptionxml;
            }
            catch (XmlException)
            {
                throw new Exception("Error while loading xml file:" + sOptionXmlPath);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// starts actual generation for an ecr those info has been previously saved in globalvar from setoptions function. 
        /// It Finally rollbacks transaction
        /// </summary>
        /// <returns></returns>
        public bool GenerateECRDeliverables(ECRLevelOptions ecrOptions, Logger logger)
        {
            DBManager dbManager = null;
            DataPrepartion dataPreparation = null;
            try
            {
                dbManager = new DBManager(this._connectionString);
                dataPreparation = new DataPrepartion(this._guid, dbManager, ref ecrOptions);

                if (this._userInputs.DataMode.Equals(DataMode.Model))
                {
                    _progress.Report("Populating Hashtables for " + ecrOptions.Ecrno + "...");
                    dataPreparation.PrepareHashTables();
                }

                List<Task> consolidatedTasks = new List<Task>();
                Task task1 = Task.Run(() =>
                {
                    GenerateCode generateCode = new GenerateCode(dataPreparation, ref ecrOptions, this._progress);
                    generateCode.Generate(_userInputs);
                });
                consolidatedTasks.Add(task1);

                Task task2 = Task.Run(() =>
                {
                    this.GenerateHtmXmlAndScripts(dataPreparation, this._userInputs, ref ecrOptions, logger);
                });
                consolidatedTasks.Add(task2);
                Task.WaitAll(consolidatedTasks.ToArray());


                return (ecrOptions.ErrorCollection != null ? ecrOptions.ErrorCollection.Count == 0 : true);

            }
            catch (Exception ex)
            {
                Logger.WriteLogToTraceListener("Codegenwrapper->Generate->", string.Format("{0}", ex.InnerException != null ? ex.InnerException.Message : ex.Message));
                return false;
            }
            finally
            {
                Logger.WriteLogToTraceListener("CodegenWrapper->Generate", string.Format("Rollback Transaction..."));
                if (dbManager != null && dbManager.Connection != null)
                {
                    dbManager.RollbackTransaction();
                    dbManager.Close();
                }
            }
        }

        private DataTable GetEcrList()
        {
            DataTable dtEcrList = new DataTable();
            dtEcrList.Columns.Add(new DataColumn { ColumnName = "EcrNo" });

            DBManager dbManager = null;

            if (this._userInputs.DataMode.Equals(DataMode.Model))
            {
                dbManager = new DBManager(this._connectionString);
                var parameters = dbManager.CreateParameters(1);
                dbManager.AddParamters(parameters, 0, "@guid", this._guid);
                dbManager.Open();
                dtEcrList = dbManager.ExecuteDataTable(CommandType.StoredProcedure, "engg_devcon_codegen_options_ecr", parameters);
                dbManager.Close();
            }
            else if (this._userInputs.DataMode.Equals(DataMode.Xml))
            {
                if (File.Exists(_userInputs.OptionXmlPath))
                {
                    XDocument xDocument = XDocument.Load(_userInputs.OptionXmlPath);
                    var modelNodes = xDocument.Descendants("model");
                    foreach (var modelNode in modelNodes)
                    {
                        DataRow drEcrno = dtEcrList.NewRow();
                        drEcrno["EcrNo"] = modelNode.Attribute("ecrno").Value;
                        dtEcrList.Rows.Add(drEcrno);
                    }
                }
            }

            return dtEcrList;
        }

        /// <summary>
        /// starts generation for the guid.
        /// </summary>
        /// <returns></returns>
        public List<String> StartGeneration(UserInputs userInputs)
        {
            string sMessage = string.Empty;
            DataTable dtEcrList = null;
            try
            {
                this._userInputs = userInputs;

                //if (userInputs.DataMode == DataMode.Model)
                dtEcrList = GetEcrList();
                //else
                //{
                //    dtEcrList = new DataTable();
                //    dtEcrList.Columns.Add(new DataColumn("EcrNo"));
                //    foreach (string xmlFilepath in Directory.GetFiles(userInputs.OptionXmlPath, "*.xml", SearchOption.TopDirectoryOnly))
                //    {
                //        DataRow dr = dtEcrList.NewRow();
                //        dr["EcrNo"] = Path.GetFileName(xmlFilepath);
                //        dtEcrList.Rows.Add(dr);
                //    }
                //}

                string sGuidLog = Path.Combine(@"c:\temp", "bin", "Ramco_VwPlf_CodeGenerator", string.Format("{0}.txt", this._guid));
                Common.CreateDirectory(Path.GetDirectoryName(sGuidLog));
                File.Create(sGuidLog).Close();

                //Parallel.ForEach(dtEcrList.AsEnumerable(), new ParallelOptions { MaxDegreeOfParallelism = 1 }, drEcr =>
                //        {
                foreach (DataRow drEcr in dtEcrList.Rows)
                {
                    try
                    {
                        string sEcrNo = Convert.ToString(drEcr["EcrNo"]);

                        _progress.Report("Generating Option xml for " + sEcrNo + "...");

                        if (!string.IsNullOrEmpty(GenerateOptionXml(sEcrNo)))
                        {
                            ECRLevelOptions ecrOptions = new ECRLevelOptions();

                            _progress.Report("Setting Options for " + sEcrNo + "...");
                            if (this.SetECROptions(Path.Combine(this._codegenPath, sEcrNo + ".xml"), ref ecrOptions))
                            {

                                string slogFile = Path.Combine(ecrOptions.GenerationPath, ecrOptions.Platform, ecrOptions.Customer, ecrOptions.Project, ecrOptions.Ecrno, string.Format("{0}.txt", ecrOptions.Ecrno));
                                Common.CreateDirectory(Path.GetDirectoryName(slogFile));
                                Logger logger = new Logger(slogFile);

                                if (this.GenerateECRDeliverables(ecrOptions, logger))
                                    sMessage = string.Format("{0} - {1}", Convert.ToString(drEcr["EcrNo"]), "Success");
                                else
                                {
                                    sMessage = string.Format("{0} - {1}", Convert.ToString(drEcr["EcrNo"]), "Failed");
                                    foreach (var er in ecrOptions.ErrorCollection)
                                    {
                                        using (FileStream file = new FileStream(sGuidLog, FileMode.Append, FileAccess.Write, FileShare.Read))
                                        {
                                            using (StreamWriter writer = new StreamWriter(file, Encoding.Unicode))
                                            {
                                                writer.WriteLine(string.Format("{0}:{1}", er.Name, er.Description));
                                            }
                                        }
                                    }
                                    this._errCollection.Add(sMessage);
                                }
                            }
                        }

                    }
                    catch (Exception e)
                    {
                        sMessage = string.Format("{0} - {1} - {2}", Convert.ToString(drEcr["EcrNo"]), "Failed", e.InnerException != null ? e.InnerException.Message : e.Message);
                        sMessage = string.Format("{0}\n", sMessage);
                        sMessage = string.Format("StackTrace : {0}", sMessage);
                        this._errCollection.Add(sMessage);
                    }
                    lock (locker)
                    {
                        using (FileStream file = new FileStream(sGuidLog, FileMode.Append, FileAccess.Write, FileShare.Read))
                        {
                            using (StreamWriter writer = new StreamWriter(file, Encoding.Unicode))
                            {
                                writer.WriteLine(sMessage);
                            }
                        }
                    }
                }
                //});
                this._progress.Report(string.Format("Generation Completed {0}.", this._errCollection.Count == 0 ? "Successfully" : "with Errors"));

                return this._errCollection;
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("CodegenWrapper->StartGeneration->{0}", ex.InnerException != null ? ex.InnerException.Message : ex.Message));
            }


        }
    }
}
