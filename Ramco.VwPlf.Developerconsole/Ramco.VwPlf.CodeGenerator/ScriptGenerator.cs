using System;
using System.Linq;
using System.Text;
using System.Data;
using System.Collections;
using Ramco.VwPlf.DataAccess;
using System.IO;

namespace Ramco.VwPlf.CodeGenerator
{
    internal class ScriptGenerator
    {
        Guid _guid;
        Logger _logger = null;
        ECRLevelOptions _ecrOptions = null;
        DBManager _dbManager = null;

        public ScriptGenerator(Guid guid, ECRLevelOptions ecrOptions, DBManager dbManager)
        {
            this._guid = guid;
            this._ecrOptions = ecrOptions;
            this._dbManager = dbManager;
            this._logger = new Logger(System.IO.Path.Combine(_ecrOptions.GenerationPath, _ecrOptions.Platform, _ecrOptions.Customer, _ecrOptions.Project, _ecrOptions.Ecrno, _ecrOptions.Ecrno + ".txt"), ObjectType.Script);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private bool GenerateDepScript()
        {
            bool bSuccessFlg = false;
            string sDirectory = string.Empty;
            string sFileNameWithExtn = string.Empty;
            Hashtable paramAndValues = null;
            try
            {
                _logger.WriteLogToFile("GenerateDepScript", "Depscript generation starts");
                foreach (Language language in _ecrOptions.OptionXML.languages)
                {
                    bool bIsMultiLangFile = false;

                    paramAndValues = new Hashtable();
                    paramAndValues.Add("@CustomerName", _ecrOptions.Customer);
                    paramAndValues.Add("@ProjectName", _ecrOptions.Project);
                    paramAndValues.Add("@ComponentName", _ecrOptions.Component);
                    paramAndValues.Add("@ActivityOffset", _ecrOptions.OptionXML.scripts.activityoffset);
                    paramAndValues.Add("@ReqExposure", 0);
                    paramAndValues.Add("@EcrNo", _ecrOptions.Ecrno);
                    paramAndValues.Add("@Flag", 1);
                    paramAndValues.Add("@LangId", Convert.ToString(language.id));
                    paramAndValues.Add("@logical_edk", 0);
                    paramAndValues.Add("@itkFlag", 0);
                    paramAndValues.Add("@releaseversion", _ecrOptions.ReleaseVersion);

                    sDirectory = System.IO.Path.Combine(_ecrOptions.GenerationPath, _ecrOptions.Platform, _ecrOptions.Customer, _ecrOptions.Project, _ecrOptions.Ecrno, "Updated", _ecrOptions.Component, "Release", "Depmetadata", "SQL");


                    if (language.id == "1")//english
                    {
                        sFileNameWithExtn = string.Format("{0}_depmetadata.sql", _ecrOptions.Component);
                        bIsMultiLangFile = false;
                    }
                    else
                    {
                        sFileNameWithExtn = string.Format("{0}_depmetadata_{1}.sql", _ecrOptions.Component, language.id);
                        bIsMultiLangFile = true;
                    }

                    _logger.WriteLogToFile("GenerateDepScript", string.Format("Writing Metadata file: {0}", sFileNameWithExtn));
                    Common.WriteResultSetToFile(this._dbManager.ConnectionString, "engg_fw_admin_RDC_metadata2_gen_license", CommandType.StoredProcedure, paramAndValues, System.IO.Path.Combine(sDirectory, sFileNameWithExtn),bIsMultiLangFile);
                }

                paramAndValues = new Hashtable();
                paramAndValues.Add("@CustomerName", _ecrOptions.Customer);
                paramAndValues.Add("@ProjectName", _ecrOptions.Project);
                paramAndValues.Add("@ComponentName", _ecrOptions.Component);
                paramAndValues.Add("@ActivityOffset", _ecrOptions.OptionXML.scripts.activityoffset);
                paramAndValues.Add("@ReqExposure", 0);
                paramAndValues.Add("@EcrNo", _ecrOptions.Ecrno);
                paramAndValues.Add("@Flag", 1);
                sFileNameWithExtn = string.Format("{0}_depmetadata_Language.sql", _ecrOptions.Component);
                _logger.WriteLogToFile("GenerateDepScript", string.Format("Writing Metadata file: {0}", sFileNameWithExtn));
                Common.WriteResultSetToFile(this._dbManager.ConnectionString, "engg_fw_admin_RDC_metadata2_Lang_gen", CommandType.StoredProcedure, paramAndValues, System.IO.Path.Combine(sDirectory, sFileNameWithExtn),false);

                bSuccessFlg = true;
            }
            catch (Exception ex)
            {
                bSuccessFlg = false;

                Error error = new Error(ObjectType.Script, Path.Combine(sDirectory, sFileNameWithExtn), string.Empty);
                error.Description = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                _ecrOptions.ErrorCollection.Add(error);

                _logger.WriteLogToFile("GenerateDepScript", Convert.ToString(ex), bError: true);
            }

            return bSuccessFlg;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private bool GenerateWorkFlowScript()
        {
            bool bSuccessFlg = false;
            string sDirectory = string.Empty;
            string sFile1WithExtn = string.Empty;
            string sFile2WithExtn = string.Empty;
            DataSet ds = null;
            try
            {
                _logger.WriteLogToFile("GenerateWorkFlowScript", "Workflow generation starts");
                foreach (Language language in _ecrOptions.OptionXML.languages)
                {
                    ds = new DataSet();
                    DBManager dbManager = this._dbManager;
                    var parameters = dbManager.CreateParameters(7);
                    dbManager.AddParamters(parameters, 0, "@cust", _ecrOptions.Customer);
                    dbManager.AddParamters(parameters, 1, "@proj", _ecrOptions.Project);
                    dbManager.AddParamters(parameters, 2, "@comp", _ecrOptions.Component);
                    dbManager.AddParamters(parameters, 3, "@ecrno", _ecrOptions.Ecrno);
                    dbManager.AddParamters(parameters, 4, "@catkey", _ecrOptions.OptionXML.scripts.wflowoffset);
                    dbManager.AddParamters(parameters, 5, "@activityoffset", _ecrOptions.OptionXML.scripts.activityoffset);
                    dbManager.AddParamters(parameters, 6, "@language", language.id);
                    //dbManager.Open();
                    ds = dbManager.ExecuteDataSet(CommandType.StoredProcedure, "wkf_metasp_compwise_rvw20", parameters);
                    //dbManager.Close();

                    if (ds.Tables.Count > 0)
                    {
                        DataTable dtInputMetasp = new DataTable();
                        DataTable dtReqwbMetasp = new DataTable();
                        sDirectory = System.IO.Path.Combine(_ecrOptions.GenerationPath, _ecrOptions.Platform, _ecrOptions.Customer, _ecrOptions.Project, _ecrOptions.Ecrno, "Updated", _ecrOptions.Component, "Release", "WorkFlowScript", "SQL");

                        if (language.id != "1")
                        {
                            sFile1WithExtn = String.Format("{0}_inp_MetaSp_{1}.sql", _ecrOptions.Component, language.id);
                            sFile2WithExtn = String.Format("{0}_reqwb_MetaSp_{1}.sql", _ecrOptions.Component, language.id);
                        }
                        else
                        {
                            sFile1WithExtn = String.Format("{0}_inp_MetaSp.sql", _ecrOptions.Component);
                            sFile2WithExtn = String.Format("{0}_reqwb_MetaSp.sql", _ecrOptions.Component);
                        }

                        if (ds.Tables.Count > 0)
                        {
                            dtInputMetasp = ds.Tables[0];
                            if (dtInputMetasp.Rows.Count > 0)
                            {
                                Common.CreateDirectory(sDirectory);
                                string sFullFilePath = Path.Combine(sDirectory, sFile1WithExtn);
                                using (FileStream fs = new FileStream(sFullFilePath, FileMode.Create))
                                {
                                    using (StreamWriter sw = new StreamWriter(fs, Encoding.UTF8))
                                    {
                                        _logger.WriteLogToFile("GenerateWorkFlowScript", string.Format("Writing Meta file: {0}", sFullFilePath));
                                        foreach (DataRow dr in dtInputMetasp.Rows)
                                            sw.WriteLine(Convert.ToString(dr[0]));
                                    }
                                }
                            }
                        }

                        if (ds.Tables.Count > 1)
                        {
                            dtReqwbMetasp = ds.Tables[1];
                            if (dtReqwbMetasp.Rows.Count > 0)
                            {
                                Common.CreateDirectory(sDirectory);
                                string sFullFilePath = Path.Combine(sDirectory, sFile2WithExtn);
                                using (FileStream fs = new FileStream(sFullFilePath, FileMode.Create))
                                {
                                    using (StreamWriter sw = new StreamWriter(fs, Encoding.UTF8))
                                    {
                                        _logger.WriteLogToFile("GenerateWorkFlowScript", string.Format("Writing Meta file: {0}", sFullFilePath));
                                        foreach (DataRow dr in dtReqwbMetasp.Rows)
                                            sw.WriteLine(Convert.ToString(dr[0]));
                                    }
                                }
                            }
                        }

                        //Common.WriteResultSetToFile(GlobalVar.Connectionstring, "wkf_metasp_compwise_rvw20", CommandType.StoredProcedure, paramAndValues, System.IO.Path.Combine(sDirectory, sFile1WithExtn));
                    }
                    bSuccessFlg = true;
                }
            }
            catch (Exception ex)
            {
                bSuccessFlg = false;

                Error error = new Error(ObjectType.Script, "WorkflowScript", string.Empty);
                error.Description = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                _ecrOptions.ErrorCollection.Add(error);

                _logger.WriteLogToFile("GenerateWorkFlowScript", Convert.ToString(ex), bError: true);
            }
            //logger.WriteLogToFile("GenerateWorkFlowScript", "Workflow generation ends");

            return bSuccessFlg;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private bool GenerateEdkScript()
        {
            bool bSuccessFlg = false;
            DataTable dt = new DataTable();
            StringBuilder sb = new StringBuilder();
            String sContent = string.Empty;
            try
            {
                //DataPrepartion dataPreparation = new DataPrepartion(GlobalVar.Connectionstring);
                //dataPreparation.PrepareHashTables();

                _logger.WriteLogToFile("GenerateEdkScript", "Edk Script Generation starts");
                DBManager dbManager = this._dbManager;
                var parameters = dbManager.CreateParameters(3);
                dbManager.AddParamters(parameters, 0, "@CustomerName", _ecrOptions.Customer);
                dbManager.AddParamters(parameters, 1, "@ProjectName", _ecrOptions.Project);
                dbManager.AddParamters(parameters, 2, "@ECRNo", _ecrOptions.Ecrno);
                //dbManager.Open();
                dt = dbManager.ExecuteDataTable(CommandType.StoredProcedure, "engg_edk_script_gen_sp", parameters);
                //dbManager.Close();

                if (dt.Rows.Count > 0)
                {
                    foreach (DataRow dr in dt.Rows)
                    {
                        sb.AppendLine(Convert.ToString(dr[0]));
                    }
                    sContent = sb.ToString();
                    string[] splittedString = sContent.Split(new string[] { "--------- Page Break -------------" }, StringSplitOptions.RemoveEmptyEntries);
                    if (splittedString.Count() > 1)
                    {
                        for (int i = 0; i < splittedString.Length; i++)
                        {
                            string sFullFilePath = Path.Combine(_ecrOptions.GenerationPath, _ecrOptions.Platform, _ecrOptions.Customer, _ecrOptions.Project, _ecrOptions.Ecrno, "Updated", _ecrOptions.Component, "Release", "EdkScript", string.Format("{0}_EDK_Script_{1}.sql", Common.InitCaps(_ecrOptions.Component), i + 1));
                            Common.CreateDirectory(Path.GetDirectoryName(sFullFilePath));
                            using (FileStream fs = new FileStream(sFullFilePath, FileMode.Create))
                            {
                                using (StreamWriter sw = new StreamWriter(fs))
                                {
                                    _logger.WriteLogToFile("GenerateEdkScript", string.Format("Creating Edkscript file: {0}", sFullFilePath));
                                    sw.WriteLine(splittedString[i]);
                                }
                            }

                        }
                    }
                    else
                    {
                        string sFullFilePath = Path.Combine(_ecrOptions.GenerationPath, _ecrOptions.Platform, _ecrOptions.Customer, _ecrOptions.Project, _ecrOptions.Ecrno, "Updated", _ecrOptions.Component, "Release", "EdkScript", string.Format("{0}_EDK_Script.sql", Common.InitCaps(_ecrOptions.Component)));
                        Common.CreateDirectory(Path.GetDirectoryName(sFullFilePath));
                        using (FileStream fs = new FileStream(sFullFilePath, FileMode.Create))
                        {
                            using (StreamWriter sw = new StreamWriter(fs))
                            {
                                _logger.WriteLogToFile("GenerateEdkScript", string.Format("Creating Edkscript file: {0}", sFullFilePath));
                                sw.WriteLine(splittedString[0]);
                            }
                        }
                    }
                }
                bSuccessFlg = true;
            }
            catch (Exception ex)
            {
                bSuccessFlg = false;

                Error error = new Error(ObjectType.Script, "EdkScript", string.Empty);
                error.Description = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                _ecrOptions.ErrorCollection.Add(error);

                _logger.WriteLogToFile("GenerateEdkScript", Convert.ToString(ex), bError: true);
            }
            return bSuccessFlg;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private bool GenerateExtDeploymentScript()
        {
            bool bSuccessFlg = false;
            try
            {

                string sQuery = "select 'x' from de_ezwiz_wizard where customer_name = @customer and project_name = @project";
                DBManager dbManager = this._dbManager;
                var parameters = dbManager.CreateParameters(2);
                dbManager.AddParamters(parameters, 0, "@customer", _ecrOptions.Customer);
                dbManager.AddParamters(parameters, 1, "@project", _ecrOptions.Project);
                //dbManager.Open();
                object result = dbManager.ExecuteScalar(CommandType.Text, sQuery, parameters);
                //dbManager.Close();
                if (result != DBNull.Value)
                {
                    if (!string.IsNullOrEmpty(Convert.ToString(result)))
                    {
                        _logger.WriteLogToFile("GenerateEdkDeploymentScript", "Extension data script generation starts..");
                        Hashtable paramAndValues = new Hashtable();
                        paramAndValues.Add("@customer", _ecrOptions.Customer);
                        paramAndValues.Add("@project", _ecrOptions.Project);
                        paramAndValues.Add("@Target", "prod");

                        string sFullFilePath = Path.Combine(_ecrOptions.GenerationPath, "Wizard", String.Format("{0}_{1}_Extension.sql", _ecrOptions.Customer, _ecrOptions.Project));
                        _logger.WriteLogToFile("GenerateExtDeploymentScript", string.Format("Writing datascript {0}", sFullFilePath));
                        Common.WriteResultSetToFile(this._dbManager, "eZeeWizard_Script_gen", CommandType.StoredProcedure, paramAndValues, sFullFilePath,false);
                        //logger.WriteLogToFile("GenerateEdkDeploymentScript", "Extension data script generation ends..");

                        _logger.WriteLogToFile("GenerateEdkDeploymentScript", "Deployment metadata script generation starts..");
                        sFullFilePath = Path.Combine(_ecrOptions.GenerationPath, "Wizard", String.Format("{0}_{1}_Deployment.sql", _ecrOptions.Customer, _ecrOptions.Project));
                        _logger.WriteLogToFile("GenerateExtDeploymentScript", string.Format("Writing datascript {0}", sFullFilePath));
                        Common.WriteResultSetToFile(this._dbManager, "eZeeWizard_DepMetaData_gen", CommandType.StoredProcedure, paramAndValues, sFullFilePath,false);
                        //logger.WriteLogToFile("GenerateEdkDeploymentScript", "Deployment metadata script generation ends..");
                    }
                }
                bSuccessFlg = true;
            }
            catch (Exception ex)
            {
                bSuccessFlg = false;
                _ecrOptions.ErrorCollection.Add(new Error(ObjectType.Script, "DeploymentScript", ex.InnerException != null ? ex.InnerException.Message : ex.Message));
                _logger.WriteLogToFile("GenerateExtDeploymentScript", Convert.ToString(ex), bError: true);
            }
            return bSuccessFlg;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private bool GenerateSchedulerScript()
        {
            bool bStatusFlg = false;
            try
            {
                _logger.WriteLogToFile("GenerateSchedulerScript", "Generating scheduler script..");

                DBManager dbManager = this._dbManager;
                var parameters = dbManager.CreateParameters(5);
                dbManager.AddParamters(parameters, 0, "@customer_name", _ecrOptions.Customer);
                dbManager.AddParamters(parameters, 1, "@Project_Name", _ecrOptions.Project);
                dbManager.AddParamters(parameters, 2, "@Process_Name", _ecrOptions.Process);
                dbManager.AddParamters(parameters, 3, "@Component_Name", _ecrOptions.Component);
                dbManager.AddParamters(parameters, 4, "@langid", 1);

                DataTable dt = dbManager.ExecuteDataTable(CommandType.StoredProcedure, "edkreport_info", parameters);

                if (dt.Rows.Count > 0)
                {
                    string sDirectory = Path.Combine(_ecrOptions.GenerationPath, _ecrOptions.Platform, _ecrOptions.Customer, _ecrOptions.Project, _ecrOptions.Ecrno, "Updated", _ecrOptions.Component, "Release", "SchedulerScript");
                    string sFileName = string.Format("{0}.sql", _ecrOptions.Component);
                    Common.CreateDirectory(sDirectory);
                    using (FileStream fs = new FileStream(Path.Combine(sDirectory, sFileName), FileMode.Create))
                    {
                        using (StreamWriter sw = new StreamWriter(fs))
                        {
                            foreach (DataRow dr in dt.Rows)
                            {
                                sw.WriteLine(Convert.ToString(dr[0]));
                            }
                        }
                    }
                }

                bStatusFlg = true;
            }
            catch (Exception ex)
            {
                bStatusFlg = false;
                _ecrOptions.ErrorCollection.Add(new Error(ObjectType.Script, "SchedulerScript", ex.InnerException != null ? ex.InnerException.Message : ex.Message));
                _logger.WriteLogToFile("GenerateSchedulerScript", ex.InnerException != null ? ex.InnerException.Message : ex.Message, bError: true);
            }
            return bStatusFlg;
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
                if (_ecrOptions.OptionXML.scripts.depscript.Equals("y"))
                    this.GenerateDepScript();
                if (_ecrOptions.OptionXML.scripts.wflowscript.Equals("y"))
                    this.GenerateWorkFlowScript();
                if (_ecrOptions.OptionXML.scripts.depscript.Equals("y"))
                    this.GenerateExtDeploymentScript();
                if (_ecrOptions.OptionXML.htmls.Where(h => h.aspx.Equals("y")).Any())
                    this.GenerateSchedulerScript();
                if (_ecrOptions.OptionXML.scripts.edksscript.Equals("y"))
                    this.GenerateEdkScript();

                bSuccessFlg = true;
            }
            catch (Exception ex)
            {
                bSuccessFlg = false;
                Console.WriteLine(Convert.ToString(ex));
                _logger.WriteLogToFile("ScriptGenerator.Generate", Convert.ToString(ex), bError: true);
            }
            return bSuccessFlg;
        }
    }
}
