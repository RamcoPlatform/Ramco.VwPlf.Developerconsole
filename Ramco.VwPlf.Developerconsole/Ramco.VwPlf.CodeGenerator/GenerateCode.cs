using System;
using System.Collections.Generic;
using System.Linq;
using System.CodeDom.Compiler;
using Microsoft.CSharp;
using System.Data;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

using Ramco.VwPlf.CodeGenerator.AppLayer;
using Ramco.VwPlf.CodeGenerator.WebLayer;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Ramco.VwPlf.CodeGenerator
{
    /// <summary>
    /// 
    /// </summary>    
    public class GenerateCode
    {
        //As of now, Compilation errors will be available here.
        //private ILookup<string, ErrorCollection> Errors;
        private Logger _logger = null;
        private ECRLevelOptions _ecrOptions = null;
        private DataPrepartion _dataPreparation = null;
        string _sReferenceDllDirectory = string.Empty;
        private IProgress<string> _progress = new Progress<string>();

        public GenerateCode(DataPrepartion dataPreparation, ref ECRLevelOptions ecrOptions)
        {
            this._ecrOptions = ecrOptions;
            this._dataPreparation = dataPreparation;
            this._sReferenceDllDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, this._ecrOptions.ReferenceDllSourcePath);
            this._logger = new Logger(System.IO.Path.Combine(this._ecrOptions.GenerationPath, this._ecrOptions.Platform, this._ecrOptions.Customer, this._ecrOptions.Project, this._ecrOptions.Ecrno, this._ecrOptions.Ecrno + ".txt"));
        }

        public GenerateCode(DataPrepartion dataPreparation, ref ECRLevelOptions ecrOptions, IProgress<string> progress)
        {
            this._progress = progress;
            this._ecrOptions = ecrOptions;
            this._dataPreparation = dataPreparation;
            this._sReferenceDllDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, this._ecrOptions.ReferenceDllSourcePath);
            this._logger = new Logger(System.IO.Path.Combine(this._ecrOptions.GenerationPath, this._ecrOptions.Platform, this._ecrOptions.Customer, this._ecrOptions.Project, this._ecrOptions.Ecrno, this._ecrOptions.Ecrno + ".txt"));
        }

        /// <summary>
        /// Function that starts actual generation
        /// </summary>
        /// <returns></returns>
        public bool Generate(UserInputs userInputs)
        {
            bool bStatus = false;
            try
            {
                _logger.WriteLogToFile("Generate", "Code Generation Starts...");
                List<Task> tasks = new List<Task>();

                #region Resource & Service generation
                Task taskGeneratesService = Task.Run(() =>
                {
                    try
                    {
                        if (_ecrOptions.GenerateService || _ecrOptions.SeperateErrorDll)
                        {
                            this._progress.Report(string.Format("Generating resources for {0}...", _ecrOptions.Ecrno));
                            _dataPreparation.DownloadResourceInfo(userInputs);
                            this.GenerateResourceFiles();
                            this.GenerateErrHdlrCS();

                            this._progress.Report(string.Format("Compiling resource dll for {0}...", _ecrOptions.Ecrno));
                            this.CompileResource();
                        }
                    }
                    catch (Exception ex2)
                    {
                        _ecrOptions.ErrorCollection.Add(new Error(ObjectType.Activity, string.Empty, ex2.InnerException != null ? ex2.InnerException.Message : ex2.Message));
                        _logger.WriteLogToFile("Generate", ex2.InnerException != null ? ex2.InnerException.Message : ex2.Message, bError: true);
                    }

                    try
                    {
                        if (_ecrOptions.GenerateService)
                        {
                            this._ecrOptions.generation.ServiceInfo = this._dataPreparation.DownloadService(userInputs);
                            this.GenerateService();
                            this.GenerateBROAndBulk();
                            this.CompileErrHdlr();
                            this.CompileISDump();
                            this.CompileService();
                        }
                    }
                    catch (Exception ex3)
                    {
                        _ecrOptions.ErrorCollection.Add(new Error(ObjectType.Activity, string.Empty, ex3.InnerException != null ? ex3.InnerException.Message : ex3.Message));
                        _logger.WriteLogToFile("Generate", ex3.InnerException != null ? ex3.InnerException.Message : ex3.Message, bError: true);
                    }
                });
                tasks.Add(taskGeneratesService);
                #endregion

                #region Activity Generation
                Task taskGeneratesActivity = Task.Run(() =>
                {
                    try
                    {
                        if (_ecrOptions.GenerateActivity)
                        {
                            this._progress.Report(string.Format("Structuring activity metadata for {0}...", _ecrOptions.Ecrno));
                            _dataPreparation.DownloadActivity(userInputs);

                            this._progress.Report(string.Format("Generating activity for {0}...", _ecrOptions.Ecrno));
                            this.GenerateActivity();

                            this._progress.Report(string.Format("Compiling activity for {0}...", _ecrOptions.Ecrno));
                            this.CompileActivity();
                        }
                    }
                    catch (Exception ex1)
                    {
                        _ecrOptions.ErrorCollection.Add(new Error(ObjectType.Activity, string.Empty, ex1.InnerException != null ? ex1.InnerException.Message : ex1.Message));
                        _logger.WriteLogToFile("Generate", ex1.InnerException != null ? ex1.InnerException.Message : ex1.Message, bError: true);
                    }
                });
                tasks.Add(taskGeneratesActivity);
                #endregion

                Task.WaitAll(tasks.ToArray());

                bStatus = true;
            }

            catch (Exception ex)
            {
                bStatus = false;
                throw new Exception(string.Format("GenerateCode->Generate->{0}", ex.InnerException != null ? ex.InnerException.Message : ex.Message));

            }
            return bStatus;
        }

        /// <summary>
        /// Generates Activity
        /// </summary>
        /// <returns></returns>
        private bool GenerateActivity()
        {
            Boolean bStatus;
            try
            {
                _logger.WriteLogToFile("GenerateActivity", "Activity Generation starts...");

                #region basecallout activities
                foreach (Activity activity in _ecrOptions.generation.activities.Where(a => a.IsBaseCallout == true))
                {
                    this._progress.Report(string.Format("Generating basecallout for activity - {0}", activity.Name));
                    try
                    {
                        _logger.WriteLogToFile(string.Empty, string.Format("generating basecallout activity - {0}", activity.Name));
                        BaseCalloutActivityTemplate _BaseCalloutActivityTemplate = new BaseCalloutActivityTemplate
                        {
                            sNamespaceName = activity.Name,
                            sTargetDir = activity.SourceDirectory,
                            ecrOptions = this._ecrOptions
                        };
                        Common.CreateDirectory(_BaseCalloutActivityTemplate.sTargetDir);
                        File.WriteAllText(Path.Combine(_BaseCalloutActivityTemplate.sTargetDir, "activity.cs"), _BaseCalloutActivityTemplate.TransformText());

                        foreach (ILBO ilbo in activity.ILBOs)
                        {
                            if (ilbo.HasBaseCallout)
                            {
                                try
                                {
                                    BaseCalloutILBOTemplate _BaseCalloutILBOTemplate = new BaseCalloutILBOTemplate
                                    {
                                        sNamespaceName = activity.Name,
                                        sClassName = ilbo.Code,
                                        sTargetDir = activity.SourceDirectory,
                                        ecrOptions = this._ecrOptions
                                    };
                                    File.WriteAllText(Path.Combine(_BaseCalloutILBOTemplate.sTargetDir, string.Format("{0}.cs", ilbo.Code)), _BaseCalloutILBOTemplate.TransformText());
                                }
                                catch (Exception ex)
                                {
                                    _logger.WriteLogToFile("GenerateCode->GenerateActivity", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message, bError: true);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _ecrOptions.ErrorCollection.Add(new Error(ObjectType.Activity, string.Empty, ex.InnerException != null ? ex.InnerException.Message : ex.Message));
                        _logger.WriteLogToFile("GenerateCode->GenerateActivity", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message, bError: true);
                    }
                }
                #endregion

                #region other activities
                foreach (Activity activity in _ecrOptions.generation.activities.Where(a => a.IsBaseCallout == false))
                {
                    this._progress.Report(string.Format("Generating activity - {0}", activity.Name));
                    //condition based activity generation
                    //if (activity.Name.ToLower() == "fincalclosurehub")
                    //{
                    try
                    {
                        GenerateActivityClass objActGenerator = new GenerateActivityClass(activity, ref this._ecrOptions);
                        objActGenerator.Generate(this._dataPreparation._dbManager.ConnectionString);
                    }
                    catch (Exception ex)
                    {
                        _ecrOptions.ErrorCollection.Add(new Error(ObjectType.Activity, string.Empty, ex.InnerException != null ? ex.InnerException.Message : ex.Message));
                        _logger.WriteLogToFile("Generate->GenerateActivity", !(object.Equals(ex.InnerException, null)) ? ex.InnerException.Message : ex.Message, bError: true);
                    }
                    //}
                }
                #endregion

                bStatus = true;
            }
            catch (Exception ex)
            {
                _logger.WriteLogToFile("GenerateActivity", !(object.Equals(ex.InnerException, null)) ? ex.InnerException.Message : ex.Message, bError: true);
                bStatus = false;
            }
            return bStatus;
        }

        private bool GenerateResourceFiles()
        {
            Boolean bStatus;
            string sErrSrcPath;

            try
            {
                _logger.WriteLogToFile("GenerateResourceAsTxtFiles", "Resource file generation starts...");

                //regex = new System.Text.RegularExpressions.Regex();
                sErrSrcPath = Path.Combine(_ecrOptions.GenerationPath, _ecrOptions.Platform, _ecrOptions.Customer, _ecrOptions.Project, _ecrOptions.Ecrno, "Updated", _ecrOptions.Component, "Source", "Error");

                //creating source path for the resource files
                Common.CreateDirectory(sErrSrcPath);

                //generating placeholder info ehs.phinfo[LANGID].txt && Error info ehs.[LANGID].txt
                _ecrOptions.generation.ResourcedllRelPath = Path.Combine(_ecrOptions.GenerationPath, _ecrOptions.Platform, _ecrOptions.Customer, _ecrOptions.Project, _ecrOptions.Ecrno, "Updated", _ecrOptions.Component, "Release", "Service", string.Format("{0}.Resources.dll", _ecrOptions.Component));
                foreach (string langid in _ecrOptions.LangIdList)
                {
                    List<string> sbErrInfo = new List<string>();

                    Resource resource = new Resource
                    {
                        ErrSrc_FullFilepath = Path.Combine(sErrSrcPath, string.Format("ehs.{0}.txt", langid)),
                        ErrTarg_FullFilepath = Path.Combine(sErrSrcPath, string.Format("ehs.{0}.resources", langid)),
                        StartTime = DateTime.Now,
                        Status = GenerationStatus.InProgress
                    };
                    try
                    {
                        FileStream fSERRInfo = new FileStream(resource.ErrSrc_FullFilepath, FileMode.Create, FileAccess.Write);

                        //writer for error info
                        //Encoding utf8WithoutBOM = new UTF8Encoding(false);
                        StreamWriter SWERRInfo = new StreamWriter(fSERRInfo, Encoding.UTF8);

                        StringBuilder sbPhInfo = new StringBuilder();

                        string left = string.Empty;
                        string right = string.Empty;
                        string PhInfoLine1 = string.Empty;
                        string ErrInfoLine1 = string.Empty;
                        string ErrInfoLine2 = string.Empty;

                        #region writing err & ph info
                        if (_ecrOptions.generation.dsResourceInfo.Tables["Error"].Rows.Count > 0)
                        {
                            var errorInfo = (from row in _ecrOptions.generation.dsResourceInfo.Tables["Error"].AsEnumerable()
                                             select new
                                             {
                                                 serviceName = row["servicename"],
                                                 methodId = row["methodid"],
                                                 code = Convert.ToInt64(row["sperrorcode"]),
                                                 id = row["brerrorid"],
                                                 message = row["errormessage"],
                                                 correctiveAction = row["correctiveaction"],
                                                 severityId = row["severityid"],
                                                 focusSegment = row["focussegment"],
                                                 focusDataItem = row["focusdataitem"],
                                                 placeHolderDetails = row.GetChildRows("error_placeholder")
                                             });
                            #region error
                            foreach (var grpByMethodId in errorInfo.GroupBy(r => r.methodId))//groupby to avoid resource key duplication.
                            {
                                foreach (var grpByErrId in grpByMethodId.GroupBy(e => e.id))
                                {
                                    var error = grpByErrId.First();

                                    string sErrorMessage = (string)error.message;

                                    if (error.placeHolderDetails.Count() > 0)
                                    {
                                        //for each Placeholder info
                                        foreach (var ph in (from row in error.placeHolderDetails
                                                            select new
                                                            {
                                                                name = row["placeholdername"],
                                                                shortPLText = row["shortpltext"],
                                                                segmentname = row["segmentname"],
                                                                dataitemname = row["dataitemname"]
                                                            }).Distinct())
                                        {
                                            sErrorMessage = Regex.Replace(sErrorMessage, string.Format("\\^{0}\\!", ph.name), (string)ph.shortPLText, RegexOptions.IgnoreCase);
                                        }
                                    }

                                    sErrorMessage = Regex.Replace(sErrorMessage, @"\n", "");
                                    sErrorMessage = Regex.Replace(sErrorMessage, @"\r", "");
                                    sErrorMessage = Regex.Replace(sErrorMessage, @"\`", "\\");
                                    sErrorMessage = Regex.Replace(sErrorMessage, @"\\", "\\\\");
                                    sErrorMessage = Regex.Replace(sErrorMessage, @"\<(.*?)\>", match => match.ToString().ToLowerInvariant());

                                    ErrInfoLine1 = string.Format("m{0}e{1}={2}", error.methodId, error.code, sErrorMessage);
                                    ErrInfoLine2 = string.Format("m{0}c{1}={2}$::${3}$::${4}", error.methodId, error.code, error.correctiveAction, error.id, error.severityId);

                                    if (!string.IsNullOrEmpty(error.focusSegment.ToString()))
                                        ErrInfoLine2 = ErrInfoLine2 + string.Format("$::${0}", error.focusSegment);

                                    if (!string.IsNullOrEmpty((string)error.focusDataItem.ToString()))
                                        ErrInfoLine2 = ErrInfoLine2 + string.Format("$::${0}", error.focusDataItem);

                                    sbErrInfo.Add(ErrInfoLine1);
                                    sbErrInfo.Add(ErrInfoLine2);
                                }
                            }
                            #endregion

                            #region forming placeholder string
                            foreach (var error in errorInfo)
                            {
                                if (error.placeHolderDetails.Count() > 0)
                                {
                                    left = string.Empty;
                                    right = string.Empty;
                                    left = string.Format("i{0}m{1}ps{2}b{3}s{4}", error.code, error.methodId, error.placeHolderDetails.First()["psseqno"], error.placeHolderDetails.First()["brseqno"], error.serviceName.ToString().ToLowerInvariant());

                                    //for each Placeholder info
                                    foreach (var ph in (from row in error.placeHolderDetails
                                                        select new
                                                        {
                                                            name = row["placeholdername"],
                                                            shortPLText = row["shortpltext"],
                                                            segmentname = row["segmentname"],
                                                            dataitemname = row["dataitemname"]
                                                        }).Distinct())
                                    {
                                        right = right + string.Format("{0}$::${1}$::${2}#::#", ph.name.ToString().ToLowerInvariant(), ph.segmentname, ph.dataitemname);
                                    }

                                    PhInfoLine1 = string.Format("{0}={1}", left, right);
                                    sbPhInfo.AppendLine(PhInfoLine1);
                                }
                            }
                            #endregion  
                        }

                        sbErrInfo.Add("brerror=PlaceHolder details are not found for the BR Sequence Number[{0}], Process Section Sequence Number = {1},SP Error ID = {2}, Method ID = {3},ServiceName = {4}.");
                        sbErrInfo.Add("pserror=PlaceHolder details are not found for the Process Section Sequence Number[{0}], SP Error ID = {1}, Method ID = {2},ServiceName = {3}.");
                        sbErrInfo.Add("sperror=Error detail is not found for the SP Error ID [{0}], Method ID = {1},ServiceName = {2}.");
                        sbErrInfo.Add("merror=Error detail is not found for the Method ID [{0}], ServiceName = {1}.");
                        sbErrInfo.Add("sererror=Service[{0}] is not mapped to any Error.");
                        sbErrInfo.Add("non_num_err=Error number returned from procedure should be numeric.");
                        sbErrInfo.Add("err_desc_not_found=Error Description is not found in EHS file or not returned from procedure");

                        //to remove duplicate entries.
                        foreach (string errinfo in sbErrInfo.Distinct())
                        {
                            SWERRInfo.WriteLine(errinfo);
                        }
                        #endregion

                        //writing phinfo
                        if (sbPhInfo.Length > 1)
                        {
                            resource.PhSrc_FullFilepath = Path.Combine(sErrSrcPath, string.Format("ehs.phinfo{0}.txt", _ecrOptions.LangIdList.First()));
                            resource.PhTarg_FullFilepath = Path.Combine(sErrSrcPath, string.Format("ehs.phinfo{0}.resources", _ecrOptions.LangIdList.First()));
                            using (FileStream fSPhInfo = new FileStream(resource.PhSrc_FullFilepath, FileMode.Create, FileAccess.Write))
                            {
                                using (StreamWriter SWPhInfo = new StreamWriter(fSPhInfo, Encoding.UTF8))
                                {
                                    SWPhInfo.Write(sbPhInfo.ToString());
                                }
                            }
                        }
                        //SWPhInfo.Close();
                        //SWPhInfo.Dispose();

                        SWERRInfo.Close();
                        SWERRInfo.Dispose();

                        resource.Status = GenerationStatus.SourceCodeGenerated;
                    }
                    catch (Exception ex)
                    {
                        resource.Status = GenerationStatus.Failure;
                        throw ex;
                    }
                    finally
                    {
                        resource.EndTime = DateTime.Now;
                        _ecrOptions.generation.resources.Add(resource);
                    }
                }

                bStatus = true;
                _logger.WriteLogToFile("GenerateResourceAsTxtFiles", "Resource file generation ends...");
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("GenerateResourceAsTxtFiles->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
            return bStatus;
        }

        /// <summary>
        /// Generates Error Handler CS
        /// </summary>
        /// <returns></returns>
        private bool GenerateErrHdlrCS()
        {
            ErrorHandler newErrHdlr = new ErrorHandler
            {
                SourcePath = Path.Combine(_ecrOptions.GenerationPath, _ecrOptions.Platform, _ecrOptions.Customer, _ecrOptions.Project, _ecrOptions.Ecrno, "Updated", _ecrOptions.Component, "Source", "Error", string.Format("{0}_ErrHdlr.cs", _ecrOptions.Component)),
                ReleasePath = Path.Combine(_ecrOptions.GenerationPath, _ecrOptions.Platform, _ecrOptions.Customer, _ecrOptions.Project, _ecrOptions.Ecrno, "Updated", _ecrOptions.Component, "Release", "Service", string.Format("{0}_ErrHdlr.netmodule", _ecrOptions.Component)),
                StartTime = DateTime.Now,
                Status = GenerationStatus.InProgress
            };

            try
            {
                ErrHandlerTemplate errorHandler = new ErrHandlerTemplate();
                errorHandler.sComponentName = _ecrOptions.Component;
                errorHandler.ServiceWithError = _ecrOptions.generation.dsResourceInfo.Tables["Error"]
                                                                            .AsEnumerable()
                                                                            .Select(x => x.Field<string>("servicename").ToLower())
                                                                            .Distinct();
                errorHandler.ServiceWithoutError = _ecrOptions.generation.dsResourceInfo.Tables["Service"]
                                                                                        .AsEnumerable()
                                                                                        .Select(x => x.Field<string>("servicename").ToLower())
                                                                                                            .Except(errorHandler.ServiceWithError);
                errorHandler.ecrOptions = this._ecrOptions;
                //errorHandler.ServiceWithoutError = GlobalVar.generation.services.Select(s=>s.Name).Except(errorHandler.ServiceWithError);
                string sErrorHandlerSource = errorHandler.TransformText();
                File.WriteAllText(newErrHdlr.SourcePath, sErrorHandlerSource);

                newErrHdlr.Status = GenerationStatus.SourceCodeGenerated;

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("GenerateErrHdlrCS->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
            finally
            {
                newErrHdlr.EndTime = DateTime.Now;
                newErrHdlr.Status = GenerationStatus.Failure;
                _ecrOptions.generation.errorHandler = newErrHdlr;
            }
        }

        /// <summary>
        /// Generates Service
        /// </summary>
        /// <returns></returns>
        private bool GenerateService()
        {
            Boolean bStatus;
            try
            {
                _logger.WriteLogToFile("GenerateService", "Service dll generation starts...");

                //getting servicce list
                DataTable dtServiceList = _ecrOptions.generation.ServiceInfo.Tables["Service"];

                //form source and target directory
                string sTemplateSrcDir = Path.Combine(_ecrOptions.GenerationPath, _ecrOptions.Platform, _ecrOptions.Customer, _ecrOptions.Project, _ecrOptions.Ecrno, "Updated", _ecrOptions.Component, "Source", "Service");
                string sTemplateRelDir = Path.Combine(_ecrOptions.GenerationPath, _ecrOptions.Platform, _ecrOptions.Customer, _ecrOptions.Project, _ecrOptions.Ecrno, "Updated", _ecrOptions.Component, "Release", "Service");
                Common.CreateDirectory(sTemplateSrcDir);


                //validation for components without service
                if (dtServiceList.Rows.Count == 0)
                {
                    _ecrOptions.GenerateService = false;
                    throw new Exception(string.Format("Service not available for the given component:{0} ecrno:{1}", _ecrOptions.Component, _ecrOptions.Ecrno));
                }

                //copy source files from old codegen path
                if (!String.IsNullOrEmpty(_ecrOptions.PreviousGenerationPath)
                    && dtServiceList.AsEnumerable()
                                    .Where(r => r.Field<int>("isselected").Equals(0))
                                    .Any())
                {
                    string sOldSrcDir = Path.Combine(_ecrOptions.PreviousGenerationPath, _ecrOptions.Platform, _ecrOptions.Customer, _ecrOptions.Project, _ecrOptions.Ecrno, "Updated", _ecrOptions.Component, "Source", "Service");

                    if (!Directory.Exists(sOldSrcDir))
                        throw new Exception(string.Format("service source path - [{0}] not available in the given directory - [{1}].", sOldSrcDir, _ecrOptions.PreviousGenerationPath));

                    Common.CopyDirectory(sOldSrcDir, sTemplateSrcDir, true);

                }

                //Parallel.ForEach(dtServiceList.AsEnumerable(), (drServiceList) =>
                //{
                foreach (DataRow drServiceList in dtServiceList.AsEnumerable().Where(r => r.Field<string>("componentname").ToLower().Equals(_ecrOptions.Component.ToLower())))
                {
                    Service _service = new Service
                    {
                        Name = Convert.ToString(drServiceList["name"]),
                        Type = Convert.ToString(drServiceList["type"]),
                        IsIntegService = Convert.ToString(drServiceList["isintegser"]).Equals("1"),
                        IsZipped = Convert.ToString(drServiceList["iszipped"]).Equals("1"),
                        IsCached = Convert.ToString(drServiceList["iscached"]).Equals("1"),
                        SourcePath = Path.Combine(_ecrOptions.GenerationPath, _ecrOptions.Platform, _ecrOptions.Customer, _ecrOptions.Project, _ecrOptions.Ecrno, "Updated", _ecrOptions.Component, "Source", "Service", string.Format("C{0}.cs", Convert.ToString(drServiceList["name"]).ToLower())),
                        ReleasePath = Path.Combine(_ecrOptions.GenerationPath, _ecrOptions.Platform, _ecrOptions.Customer, _ecrOptions.Project, _ecrOptions.Ecrno, "Updated", _ecrOptions.Component, "Release", "Service", string.Format("C{0}.netmodule", Convert.ToString(drServiceList["name"]).ToLower())),
                        StartTime = DateTime.Now.ToShortTimeString(),
                        IsSelected = Convert.ToString(drServiceList["isselected"]).Equals("1")
                    };


                    this._progress.Report(string.Format("{0} service - {1} {2}/{3} for {4}...", _service.IsSelected == true ? "Generating" : "Copying", _service.Name, dtServiceList.Rows.IndexOf(drServiceList) + 1, dtServiceList.Rows.Count, _ecrOptions.Ecrno));

                    GenerateServiceClass _newService;
                    _newService = new GenerateServiceClass(ref _service, this._ecrOptions);
                    _newService.Generate();
                    _ecrOptions.generation.services.Add(_service);

                    //condition based service generation
                    //if (string.Compare(_service.Name, "cuseden_ser_approv", true) == 0)
                    //{
                    //    GenerateServiceClass _newService;
                    //    _newService = new GenerateServiceClass(ref _service, this._ecrOptions);
                    //    _newService.Generate();
                    //    _ecrOptions.generation.services.Add(_service);
                    //}
                }
                //});

                #region Generating Transaction Files
                try
                {
                    Template0 _template0 = new Template0();
                    _ecrOptions.generation.Template0SrcPath = Path.Combine(sTemplateSrcDir, _ecrOptions.Component.ToLower() + "0.cs");
                    _ecrOptions.generation.Template0RelPath = Path.Combine(sTemplateRelDir, _ecrOptions.Component.ToLower() + "0.netmodule");
                    _template0.ecrOptions = this._ecrOptions;
                    File.WriteAllText(_ecrOptions.generation.Template0SrcPath, _template0.TransformText());
                }
                catch (Exception ex0)
                {
                    _ecrOptions.ErrorCollection.Add(new Error(ObjectType.Service, string.Empty, ex0.InnerException != null ? ex0.InnerException.Message : ex0.Message));
                }

                try
                {
                    Template1 _template1 = new Template1();
                    _ecrOptions.generation.Template1SrcPath = Path.Combine(sTemplateSrcDir, _ecrOptions.Component.ToLower() + "1.cs");
                    _ecrOptions.generation.Template1RelPath = Path.Combine(sTemplateRelDir, _ecrOptions.Component.ToLower() + "1.netmodule");
                    _template1.ecrOptions = this._ecrOptions;
                    File.WriteAllText(_ecrOptions.generation.Template1SrcPath, _template1.TransformText());
                }
                catch (Exception ex1)
                {
                    _ecrOptions.ErrorCollection.Add(new Error(ObjectType.Service, string.Empty, ex1.InnerException != null ? ex1.InnerException.Message : ex1.Message));
                }

                try
                {
                    Template_ltm _templateltm = new Template_ltm();
                    _ecrOptions.generation.TemplateLtmSrcPath = Path.Combine(sTemplateSrcDir, _ecrOptions.Component.ToLower() + "_ltm.cs");
                    _ecrOptions.generation.TemplateLtmRelPath = Path.Combine(sTemplateRelDir, _ecrOptions.Component.ToLower() + "_ltm.netmodule");
                    _templateltm.ecrOptions = this._ecrOptions;
                    File.WriteAllText(_ecrOptions.generation.TemplateLtmSrcPath, _templateltm.TransformText());
                }
                catch (Exception exLtm)
                {
                    _ecrOptions.ErrorCollection.Add(new Error(ObjectType.Service, string.Empty, exLtm.InnerException != null ? exLtm.InnerException.Message : exLtm.Message));
                }

                try
                {
                    Template_ltmcr _templateltmcr = new Template_ltmcr(_ecrOptions.generation.services);
                    _ecrOptions.generation.TemplateLtmcrSrcPath = Path.Combine(sTemplateSrcDir, _ecrOptions.Component.ToLower() + "_ltmcr.cs");
                    _ecrOptions.generation.TemplateLtmcrRelPath = Path.Combine(sTemplateRelDir, _ecrOptions.Component.ToLower() + "_ltmcr.netmodule");
                    _templateltmcr.ecrOptions = this._ecrOptions;
                    File.WriteAllText(_ecrOptions.generation.TemplateLtmcrSrcPath, _templateltmcr.TransformText());
                }
                catch (Exception exLtmcr)
                {
                    _ecrOptions.ErrorCollection.Add(new Error(ObjectType.Service, string.Empty, exLtmcr.InnerException != null ? exLtmcr.InnerException.Message : exLtmcr.Message));
                }

                try
                {
                    Template_rb _templaterb = new Template_rb();
                    _ecrOptions.generation.TemplateRbSrcPath = Path.Combine(sTemplateSrcDir, _ecrOptions.Component.ToLower() + "_rb.cs");
                    _ecrOptions.generation.TemplateRbRelPath = Path.Combine(sTemplateRelDir, _ecrOptions.Component.ToLower() + "_rb.netmodule");
                    _templaterb.ecrOptions = this._ecrOptions;
                    File.WriteAllText(_ecrOptions.generation.TemplateRbSrcPath, _templaterb.TransformText());
                }
                catch (Exception exrb)
                {
                    _ecrOptions.ErrorCollection.Add(new Error(ObjectType.Service, string.Empty, exrb.InnerException != null ? exrb.InnerException.Message : exrb.Message));
                }

                try
                {
                    Template_cr _templatecr = new Template_cr(_ecrOptions.generation.services);
                    _ecrOptions.generation.TemplateCrSrcPath = Path.Combine(sTemplateSrcDir, _ecrOptions.Component.ToLower() + "_cr.cs");
                    _ecrOptions.generation.TemplateCrRelPath = Path.Combine(sTemplateRelDir, _ecrOptions.Component.ToLower() + "_cr.netmodule");
                    _templatecr.ecrOptions = this._ecrOptions;
                    File.WriteAllText(_ecrOptions.generation.TemplateCrSrcPath, _templatecr.TransformText());
                }
                catch (Exception excr)
                {
                    _ecrOptions.ErrorCollection.Add(new Error(ObjectType.Service, string.Empty, excr.InnerException != null ? excr.InnerException.Message : excr.Message));
                }
                #endregion

                bStatus = true;
            }
            catch (Exception ex)
            {
                _logger.WriteLogToFile("GenerateService", !(object.Equals(ex.InnerException, null)) ? ex.InnerException.Message : ex.Message, bError: true);
                bStatus = false;
            }
            return bStatus;
        }

        private bool GenerateBROAndBulk()
        {

            try
            {
                #region Generating type based BRO
                List<Method> brMethods = (from s in _ecrOptions.generation.services
                                          from ps in s.ProcessSections
                                          from mt in ps.Methods
                                          where mt.IsIntegService == false
                                          && (mt.SystemGenerated == "0" || mt.SystemGenerated == "2")
                                          select mt).Distinct().ToList<Method>();
                if (brMethods.Any())
                {
                    new BROGenerator(brMethods, this._ecrOptions).Generate();
                    _ecrOptions.generation.BRDll = this.CompileBRO();
                }
                #endregion

                #region Generating bulk cs
                brMethods = (from s in _ecrOptions.generation.services
                             from ps in s.ProcessSections
                             from mt in ps.Methods
                                 //where mt.IsIntegService == false
                             where mt.SystemGenerated == "3"
                             select mt).Distinct().ToList<Method>();
                if (brMethods.Any())
                {
                    new BulkBRGenerator(brMethods, this._ecrOptions).Generate();
                    _ecrOptions.generation.BulkDll = this.CompileBulkDll();
                }
                #endregion

                return true;
            }
            catch (Exception ex)
            {
                _logger.WriteLogToFile("GenerateBRO", !(object.Equals(ex.InnerException, null)) ? ex.InnerException.Message : ex.Message, bError: true);
                return false;
            }
        }

        private string CompileBRO()
        {
            try
            {
                bool bServiceConsumesApi = false;
                string sBROSrcDir = Path.Combine(_ecrOptions.GenerationPath, _ecrOptions.Platform, _ecrOptions.Customer, _ecrOptions.Project, _ecrOptions.Ecrno, "Updated", _ecrOptions.Component, "Source", "BRO");
                string sBROTarDir = Path.Combine(_ecrOptions.GenerationPath, _ecrOptions.Platform, _ecrOptions.Customer, _ecrOptions.Project, _ecrOptions.Ecrno, "Updated", _ecrOptions.Component, "Release", "BRO");
                string sBRDll = Path.Combine(sBROTarDir, string.Format("{0}BR.dll", Common.InitCaps(_ecrOptions.Component)));
                if (Directory.Exists(sBROSrcDir))
                {
                    _logger.WriteLogToFile("CompileBRO", "Compiling BRO...");

                    List<string> referenceDlls = new List<string>();
                    referenceDlls.Add("System.dll");
                    referenceDlls.Add("System.Transactions.dll");
                    referenceDlls.Add("System.Data.dll");

                    string[] sourceFiles = Directory.GetFiles(sBROSrcDir, "*.cs", System.IO.SearchOption.TopDirectoryOnly);

                    bServiceConsumesApi = _ecrOptions.generation.services.Where(s => s.ConsumesApi.Equals(true)).Any();

                    CompileAssembly(sourceFiles, Path.Combine(sBROTarDir, string.Format("{0}BR.dll", Common.InitCaps(_ecrOptions.Component))), referenceDlls, ObjectType.BR, bServiceConsumesApi);
                }
                return sBRDll;
            }
            catch (Exception ex)
            {
                _logger.WriteLogToFile("CompileBRO", string.Format("CompileBRO->{0}", ex.InnerException != null ? ex.InnerException.Message : ex.Message), bError: true);
                throw ex;
            }
        }

        private string CompileBulkDll()
        {
            try
            {
                bool bServiceConsumesApi = false;
                string sBulkSrcFile = Path.Combine(_ecrOptions.GenerationPath, _ecrOptions.Platform, _ecrOptions.Customer, _ecrOptions.Project, _ecrOptions.Ecrno, "Updated", _ecrOptions.Component, "Source", "Bulk", string.Format("{0}_bulk.cs", _ecrOptions.Component));
                string sBulkTarDir = Path.Combine(_ecrOptions.GenerationPath, _ecrOptions.Platform, _ecrOptions.Customer, _ecrOptions.Project, _ecrOptions.Ecrno, "Updated", _ecrOptions.Component, "Release", "Service");
                string sBulkDll = Path.Combine(sBulkTarDir, string.Format("{0}_bulk.dll", _ecrOptions.Component.ToLower()));

                _logger.WriteLogToFile("CompileBulkDll", "Compiling Bulk...");

                List<string> referenceDlls = new List<string>();
                referenceDlls.Add("System.dll");
                referenceDlls.Add("System.Data.dll");

                bServiceConsumesApi = _ecrOptions.generation.services.Where(s => s.ConsumesApi.Equals(true)).Any();

                CompileAssembly(new string[] { sBulkSrcFile }, Path.Combine(sBulkTarDir, string.Format("{0}_bulk.dll", _ecrOptions.Component.ToLower())), referenceDlls, ObjectType.Bulk, bServiceConsumesApi);

                return sBulkDll;
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("CompileBulkDll->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }

        /// <summary>
        /// compiles cs file(s) to .netmodule or dll.
        /// </summary>
        /// <param name="sourceDir">Source directory of the cs files.</param>
        /// <param name="targetFile">Full FilePath of the target file.</param>
        /// <param name="referenceDlls">reference dll as string list</param>
        /// <param name="deliverablesType">specifies whether it is activity/service</param>
        private GenerationStatus CompileAssembly(string[] sourceFiles, string targetFile, List<string> referenceDlls, ObjectType deliverablesType, bool useLatestCompiler)
        {
            try
            {
                string targetDir = Path.GetDirectoryName(targetFile);
                string targetFileName = Path.GetFileNameWithoutExtension(targetFile);
                string targetFileType = Path.GetExtension(targetFile);
                System.Text.StringBuilder sCompilerOptions = new System.Text.StringBuilder();

                bool bSrcHasNetModules = (from srcFile in sourceFiles
                                          where Path.GetExtension(srcFile).Equals(".netmodule")
                                          select srcFile).Any() || (from refFile in referenceDlls where Path.GetExtension(refFile).Equals(".netmodule") select refFile).Any();

                Common.CreateDirectory(targetDir);

                CSharpCodeProvider csc = new CSharpCodeProvider(new Dictionary<string, string> { { "CompilerVersion", useLatestCompiler ? "v4.0" : "v3.5" } });

                CompilerParameters cp = new CompilerParameters();

                //output assembly should not be an executable
                cp.GenerateExecutable = false;

                //full filepath of the output assembly
                cp.OutputAssembly = targetFile;

                //sets whether warning should be treat as error or not
                cp.TreatWarningsAsErrors = false;

                //tells compiler to generate assembly in smallest and fastest way

                if (bSrcHasNetModules)
                {
                    sCompilerOptions.Append(string.Format(" /addmodule:{0}", string.Join(",", (from srcFile in sourceFiles where Path.GetExtension(srcFile).Equals(".netmodule") select srcFile)
                                                                                       .Concat(from refFile in referenceDlls where Path.GetExtension(refFile).Equals(".netmodule") select refFile))));
                }

                if (targetFileType.Equals(".netmodule"))
                    sCompilerOptions.Append(" /optimize /target:module");
                else
                    sCompilerOptions.Append(" /optimize /target:library");

                if (deliverablesType != ObjectType.Activity)
                {
                    sCompilerOptions.Append(string.Format(" /keyfile:{0}", Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _ecrOptions.ReferenceDllSourcePath, "snk", "RVWSnk.snk")));
                }


                //sCompilerOptions.Append(" /debug");
                sCompilerOptions.Append(" /warn:0");

                cp.CompilerOptions = Convert.ToString(sCompilerOptions);

                //setting assemblies referenced by the assembly
                foreach (string referencedll in referenceDlls.Where(r => !r.Contains(".netmodule")))
                    cp.ReferencedAssemblies.Add(referencedll);

                //includes debug information
                //cp.IncludeDebugInformation = true;
                cp.IncludeDebugInformation = false;

                //dont treat warning as errors
                cp.TreatWarningsAsErrors = false;

                //Compiles assembly from source files(Invoke compilation).
                sourceFiles = (from srcFile in sourceFiles
                               where !Path.GetExtension(srcFile).Equals(".netmodule")
                               select srcFile).ToArray();

                //_logger.WriteCompilationString(string.Format("{0}:csc {4} /out:{1} {2} /r:{3}  /debug /warn:0", Path.GetFileName(targetFile), targetFile, string.Join(",", sourceFiles), string.Join(",", referenceDlls), cp.CompilerOptions));
                _logger.WriteCompilationString(string.Format("{0}:csc {4} /out:{1} {2} /r:{3}  /warn:0", Path.GetFileName(targetFile), targetFile, string.Join(",", sourceFiles), string.Join(",", referenceDlls), cp.CompilerOptions));

                CompilerResults cr = csc.CompileAssemblyFromFile(cp, sourceFiles);

                if (cr.Errors.HasErrors)
                {
                    //logger.WriteLogToFile("CompileAssembly", string.Format("ERROR while building {0} : {1}-----------", deliverablesType.ToString(), targetFileName));
                    Error Error = new Error(deliverablesType, targetFileName, string.Empty);
                    foreach (CompilerError ce in cr.Errors)
                    {
                        if (!ce.IsWarning)
                        {
                            _logger.WriteLogToFile("CompileAssembly", string.Format("File : {0}({1},{2})\n{3}", ce.FileName, ce.Line, ce.Column, ce.ErrorText), bError: true);
                            Error.Description = string.Format("{0}-({1},{2})\n{3}", ce.FileName, ce.Line, ce.Column, ce.ErrorText);
                        }
                    }
                    _ecrOptions.ErrorCollection.Add(Error);
                    return GenerationStatus.Failure;
                }
                return GenerationStatus.Success;
            }
            catch (Exception ex)    
            {
                throw new Exception(string.Format("CompileAssembly->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }

        /// <summary>
        /// Compiles Activity
        /// </summary>
        public bool CompileActivity()
        {
            List<string> referenceDlls = new List<string>();

            try
            {
                referenceDlls = new List<string>();
                referenceDlls.Add("System.dll");
                referenceDlls.Add("System.Web.dll");
                referenceDlls.Add("System.Xml.dll");
                referenceDlls.Add(Path.Combine(_sReferenceDllDirectory, "Ramco.VW.RT.Web.Core.dll"));
                referenceDlls.Add(Path.Combine(_sReferenceDllDirectory, "Plf.Ramco.WebCallout.Interface.dll"));
                referenceDlls.Add(Path.Combine(_sReferenceDllDirectory, "Plf.Ramco.Instrumentation.dll"));
                referenceDlls.Add(Path.Combine(_sReferenceDllDirectory, "Plf.Ui.Ramco.Utility.dll"));
                referenceDlls.Add(Path.Combine(_sReferenceDllDirectory, "Plf.Itk.Ramco.Callout.dll"));
                referenceDlls.Add(Path.Combine(_sReferenceDllDirectory, "Ramco.VW.RT.State.dll"));
                referenceDlls.Add(Path.Combine(_sReferenceDllDirectory, "Ramco.VW.RT.AsyncResult.dll"));

                _logger.WriteLogToFile("CompileActivity", "Compiling Activity dll(s)..");

                List<Activity> activities = _ecrOptions.generation.activities.OrderByDescending(e => e.IsBaseCallout).ToList<Activity>();
                foreach (Activity activity in activities)
                {
                    this._progress.Report(string.Format("Compiling activity - {0}/{1}", activities.IndexOf(activity) + 1, activities.Count()));
                    _logger.WriteLogToFile("CompileActivity", string.Format("Compiling activity - {0}", activity.Name));

                    string[] sourceFiles = Directory.GetFiles(activity.SourceDirectory, "*.cs", System.IO.SearchOption.TopDirectoryOnly);
                    string sTargetFilePath = System.IO.Path.Combine(activity.TargetDirectory, string.Format("{0}.dll", activity.Name));

                    foreach (string referencedll in activity.AdditionalReferenceDlls)
                    {
                        referenceDlls.Add(referencedll);
                    }

                    CompileAssembly(sourceFiles, sTargetFilePath, referenceDlls, ObjectType.Activity, false);

                    //moving pdb to source directory
                    Directory.GetFiles(activity.TargetDirectory, "*.pdb", SearchOption.AllDirectories).ToList<string>().ForEach(f => File.Move(f, Path.Combine(activity.SourceDirectory, Path.GetFileName(f))));
                }

                _logger.WriteLogToFile("CompileActivity", "Activity compilation completed..");

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("CompileActivity->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }


        public List<string> GetCommonServiceReferences(bool bTakeRefAssemblyFromFourZero = false)
        {
            List<string> referenceDlls = new List<string>();
            referenceDlls.Add("System.dll");
            referenceDlls.Add("System.Web.dll");
            referenceDlls.Add("System.Xml.dll");
            referenceDlls.Add("System.EnterpriseServices.dll");
            referenceDlls.Add("System.Transactions.dll");

            if (bTakeRefAssemblyFromFourZero)
                referenceDlls.Add(Path.Combine(_sReferenceDllDirectory, "V4.0", "CUtil.Dll"));
            else
                referenceDlls.Add(Path.Combine(_sReferenceDllDirectory, "CUtil.Dll"));

            referenceDlls.Add(Path.Combine(_sReferenceDllDirectory, "VirtualWorksRT.Dll"));
            referenceDlls.Add(Path.Combine(_sReferenceDllDirectory, "VWHelper.dll"));
            referenceDlls.Add(Path.Combine(_sReferenceDllDirectory, "Plf.Itk.Ramco.Logicalextension.dll"));
            referenceDlls.Add(Path.Combine(_sReferenceDllDirectory, "Plf.Itk.Ramco.Callout.dll"));
            referenceDlls.Add(Path.Combine(_sReferenceDllDirectory, "Plf.Ramco.AppCallout.Interface.dll"));
            return referenceDlls;
        }

        /// <summary>
        /// Compiles services
        /// </summary>
        public bool CompileService()
        {
            List<string> lstReferenceDlls, lstServiceSrc;
            bool compHasApiConsumerService = false;

            try
            {
                compHasApiConsumerService = _ecrOptions.generation.services.Where(s => s.ConsumesApi).Any();

                string sBasePath = Path.Combine(_ecrOptions.GenerationPath, _ecrOptions.Platform, _ecrOptions.Customer, _ecrOptions.Project, _ecrOptions.Ecrno, "Updated", _ecrOptions.Component);
                _ecrOptions.generation.ServicedllRelPath = Path.Combine(sBasePath, "Release", "Service", string.Format("{0}.dll", _ecrOptions.Component));
                _ecrOptions.generation.ServicedllSrcPath = Path.Combine(sBasePath, "Source", "Service");

                _logger.WriteLogToFile("CompileService", "Service compilation starts..");

                #region  compiling each service to netmodule                
                foreach (Service service in _ecrOptions.generation.services)
                {
                    _logger.WriteLogToFile("CompileService", string.Format("compiling service - {0}", service.Name));

                    //specify necessary source files for the service module compilation
                    lstServiceSrc = new List<string>();
                    lstServiceSrc.Add(_ecrOptions.generation.errorHandler.ReleasePath);
                    lstServiceSrc.Add(service.SourcePath);

                    //adding references needed for the service module
                    lstReferenceDlls = GetCommonServiceReferences(service.ConsumesApi);

                    foreach (Method mt in (from ps in service.ProcessSections
                                           from mt in ps.Methods
                                           where mt.IsIntegService == true
                                           select mt))
                    {
                        lstReferenceDlls.Add(mt.ISDump.ReleasePath);
                    }

                    //giving br as reference dll
                    if ((from ps in service.ProcessSections
                         from mt in ps.Methods
                         where mt.SystemGenerated == BRTypes.CUSTOM_BR || mt.SystemGenerated == BRTypes.HANDCODED_BR || mt.SystemGenerated == BRTypes.BULK_BR
                         select mt).Any())
                    {
                        if (!string.IsNullOrEmpty(_ecrOptions.generation.BRDll))
                            lstReferenceDlls.Add(_ecrOptions.generation.BRDll);

                        if (!string.IsNullOrEmpty(_ecrOptions.generation.BulkDll))
                            lstReferenceDlls.Add(_ecrOptions.generation.BulkDll);
                    }

                    if (service.ProcessSections.Where(ps => ps.Methods.Where(m => m.IsApiConsumerService).Any()).Any())
                    {
                        lstReferenceDlls.Add(Path.Combine(_sReferenceDllDirectory,"V4.0", "Newtonsoft.Json.dll"));
                        lstReferenceDlls.Add(Path.Combine(_sReferenceDllDirectory,"V4.0", "Ramco.VW.RT.ApiMetadata.dll"));
                        lstReferenceDlls.Add(Path.Combine(_sReferenceDllDirectory,"V4.0", "Ramco.VW.RT.ApiProxy.dll"));
                        lstReferenceDlls.Add("System.Core.dll");
                    }

                    //giveing universal personalization dll as reference
                    if (service.HasUniversalPersonalization)
                    {
                        string[] upSourceFiles = new string[] { Path.Combine(_ecrOptions.GenerationPath, _ecrOptions.Platform, _ecrOptions.Customer, _ecrOptions.Project, _ecrOptions.Ecrno, "Updated", _ecrOptions.Component, "Source", "Service", "UP", $"{service.Name}", $"C{service.Name}_ml_br.cs") };
                        List<string> lstUPReferenceDlls = new List<string>();
                        lstUPReferenceDlls.Add("System.dll");
                        lstUPReferenceDlls.Add("System.Xml.dll");
                        lstUPReferenceDlls.Add("System.Core.dll");
                        lstUPReferenceDlls.Add(Path.Combine(_sReferenceDllDirectory, "VirtualWorksRT.Dll"));
                        lstUPReferenceDlls.Add(Path.Combine(_sReferenceDllDirectory, "CUtil.Dll"));
                        string upRelDir = Path.Combine(_ecrOptions.GenerationPath, _ecrOptions.Platform, _ecrOptions.Customer, _ecrOptions.Project, _ecrOptions.Ecrno, "Updated", _ecrOptions.Component, "Release", "Service", $"C{service.Name}_ml_br.dll");
                        CompileAssembly(upSourceFiles, upRelDir, lstUPReferenceDlls, ObjectType.UniversalPersonalization, false);

                        lstReferenceDlls.Add(upRelDir);
                    }

                    //setting target directory
                    service.ReleasePath = Path.Combine(sBasePath, "Release", "Service", string.Format("C{0}.netmodule", service.Name));

                    //                    
                    //actual module compilation
                    //service.Status =  CompileAssembly(lstServiceSrc.ToArray(), service.ReleasePath, lstReferenceDlls, ObjectType.Service);

                    if (service.IsSelected)
                    {
                        this._progress.Report(String.Format("Compiling service - {0} {1}/{2} for {3}...", service.Name, _ecrOptions.generation.services.IndexOf(service) + 1, _ecrOptions.generation.services.Count, _ecrOptions.Ecrno));
                        service.Status = CompileAssembly(lstServiceSrc.ToArray(), service.ReleasePath, lstReferenceDlls, ObjectType.Service, service.ConsumesApi);
                    }
                    else
                    {
                        this._progress.Report(String.Format("Copying netmodule - {0} {1}/{2} for {3}...", service.Name, _ecrOptions.generation.services.IndexOf(service) + 1, _ecrOptions.generation.services.Count, _ecrOptions.Ecrno));
                        File.Copy(service.ReleasePath.Replace(_ecrOptions.GenerationPath, _ecrOptions.PreviousGenerationPath), service.ReleasePath, true);
                        service.Status = GenerationStatus.Copied;
                    }
                }
                #endregion

                #region Compiling Transaction Modules
                _logger.WriteLogToFile("CompileService", "Compiling Transaction modules..");
                lstServiceSrc = new List<string>();
                lstServiceSrc.Add(_ecrOptions.generation.Template0SrcPath);
                lstReferenceDlls = GetCommonServiceReferences();
                CompileAssembly(lstServiceSrc.ToArray(), _ecrOptions.generation.Template0RelPath, lstReferenceDlls, ObjectType.Service, compHasApiConsumerService);

                lstServiceSrc = new List<string>();
                lstServiceSrc.Add(_ecrOptions.generation.Template1SrcPath);
                lstReferenceDlls = GetCommonServiceReferences();
                CompileAssembly(lstServiceSrc.ToArray(), _ecrOptions.generation.Template1RelPath, lstReferenceDlls, ObjectType.Service, compHasApiConsumerService);

                lstServiceSrc = new List<string>();
                lstServiceSrc.Add(_ecrOptions.generation.TemplateLtmSrcPath);
                lstReferenceDlls = GetCommonServiceReferences();
                CompileAssembly(lstServiceSrc.ToArray(), _ecrOptions.generation.TemplateLtmRelPath, lstReferenceDlls, ObjectType.Service, compHasApiConsumerService);

                lstServiceSrc = new List<string>();
                lstServiceSrc.Add(_ecrOptions.generation.TemplateLtmcrSrcPath);
                lstServiceSrc.Add(_ecrOptions.generation.TemplateLtmRelPath);
                lstReferenceDlls = GetCommonServiceReferences();
                CompileAssembly(lstServiceSrc.ToArray(), _ecrOptions.generation.TemplateLtmcrRelPath, lstReferenceDlls, ObjectType.Service, compHasApiConsumerService);

                lstServiceSrc = new List<string>();
                lstServiceSrc.Add(_ecrOptions.generation.TemplateCrSrcPath);
                lstServiceSrc.Add(_ecrOptions.generation.Template0RelPath);
                lstServiceSrc.Add(_ecrOptions.generation.Template1RelPath);
                lstReferenceDlls = GetCommonServiceReferences();
                CompileAssembly(lstServiceSrc.ToArray(), _ecrOptions.generation.TemplateCrRelPath, lstReferenceDlls, ObjectType.Service, compHasApiConsumerService);

                lstServiceSrc = new List<string>();
                lstServiceSrc.Add(_ecrOptions.generation.TemplateRbSrcPath);
                lstReferenceDlls = GetCommonServiceReferences();
                CompileAssembly(lstServiceSrc.ToArray(), _ecrOptions.generation.TemplateRbRelPath, lstReferenceDlls, ObjectType.Service, compHasApiConsumerService);
                #endregion

                #region  form and write service compilation text
                IEnumerable<string> netModules = (_ecrOptions.generation.services.Select(s => s.ReleasePath).Distinct()).Concat(new List<string> {_ecrOptions.generation.Template0RelPath,
                                                                                                                                                    _ecrOptions.generation.Template1RelPath,
                                                                                                                                                    _ecrOptions.generation.TemplateCrRelPath,
                                                                                                                                                    _ecrOptions.generation.TemplateRbRelPath,
                                                                                                                                                    _ecrOptions.generation.TemplateLtmRelPath,
                                                                                                                                                    _ecrOptions.generation.TemplateLtmcrRelPath,
                                                                                                                                                    _ecrOptions.generation.errorHandler.ReleasePath
                                                                                                                                                    });
                using (FileStream fs = new FileStream(Path.Combine(_ecrOptions.generation.ServicedllSrcPath, "compile.txt"), FileMode.Create))
                {
                    _logger.WriteLogToFile("CompileService", string.Format("Writing compilation string to {0}", Path.Combine(_ecrOptions.generation.ServicedllSrcPath, "compile.txt")));
                    using (StreamWriter sw = new StreamWriter(fs))
                    {
                        StringBuilder sb = new StringBuilder();

                        //sb.AppendFormat(" /t:library /out:{0} /keyfile:\"{1}\" /addmodule:{2} /debug /warn:0", _ecrOptions.generation.ServicedllRelPath, Path.Combine(_sReferenceDllDirectory, "snk", "RVWSnk.snk"), string.Join(",", netModules.Select(s => Path.GetFileName(s))));
                        sb.AppendFormat(" /t:library /out:{0} /keyfile:\"{1}\" /addmodule:{2} /warn:0", _ecrOptions.generation.ServicedllRelPath, Path.Combine(_sReferenceDllDirectory, "snk", "RVWSnk.snk"), string.Join(",", netModules.Select(s => Path.GetFileName(s))));
                        sw.WriteLine(sb.ToString());
                    }
                }
                #endregion

                #region service compilation using inbuilt csharp compiler class
                CompileAssembly(netModules.ToArray<string>(), _ecrOptions.generation.ServicedllRelPath, new List<string>(), ObjectType.Service, compHasApiConsumerService);
                #endregion

                //#region service compilation from the compilation text
                //ShellPrompt commandPrompt = new ShellPrompt(this._logger);
                //commandPrompt.OpenInstance();
                //commandPrompt.WorkingDirectory = Path.GetDirectoryName(_ecrOptions.generation.ServicedllRelPath);
                //commandPrompt.ExeName = "csc.exe";
                //commandPrompt.Arguments.Clear();
                //commandPrompt.Arguments.Add(string.Format("@{0}", Path.Combine(_ecrOptions.generation.ServicedllSrcPath, "compile.txt")));
                //commandPrompt.Run();
                //if (commandPrompt.Error.Length.Equals(0))
                //{
                //    _ecrOptions.generation.ServiceCompilationStatus = GenerationStatus.Success;
                //}
                //else
                //{
                //    _ecrOptions.generation.ServiceCompilationStatus = GenerationStatus.Failure;
                //}
                //commandPrompt.CloseInstance();

                ////moving pdb files from release to source directory.
                //Directory.GetFiles(Path.GetDirectoryName(_ecrOptions.generation.ServicedllRelPath), "*.pdb", SearchOption.AllDirectories).ToList<string>().ForEach(f => File.Move(f, Path.Combine(_ecrOptions.generation.ServicedllSrcPath, Path.GetFileName(f))));

                //#endregion

                _logger.WriteLogToFile("CompileService", "Service compilation completed..");
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("CompileService->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }

        /// <summary>
        /// Compiles Resource files
        /// </summary>
        public bool CompileResource()
        {
            GenerationStatus errStatus, phStatus;

            try
            {
                _logger.WriteLogToFile("CompileResource", "Compiling Resource files..");

                ShellPrompt commandPrompt;

                #region Converting text files to resource file.
                foreach (Resource resource in _ecrOptions.generation.resources)
                {

                    //converting errorinfo txt file to resource file
                    commandPrompt = new ShellPrompt(this._logger);
                    commandPrompt.OpenInstance();
                    commandPrompt.ExeName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _ecrOptions.ThirdPartyGeneratorSourcePath, "resgen.exe");
                    commandPrompt.Arguments.Clear();
                    commandPrompt.Arguments.Add(resource.ErrSrc_FullFilepath);
                    commandPrompt.Arguments.Add(resource.ErrTarg_FullFilepath);
                    commandPrompt.Run();

                    if (commandPrompt.Error.Length.Equals(0))
                        errStatus = GenerationStatus.Success;
                    else
                    {
                        ErrorCollection error = new ErrorCollection(ObjectType.Resource, resource.ErrSrc_FullFilepath, string.Empty);
                        error.AddDescription(commandPrompt.Error);

                        _logger.WriteLogToFile("compileresource", commandPrompt.Error, bError: true);

                        errStatus = GenerationStatus.Failure;
                    }

                    commandPrompt.CloseInstance();

                    //converting phinfo txt file to resource file
                    if (!string.IsNullOrEmpty(resource.PhSrc_FullFilepath) && !string.IsNullOrEmpty(resource.PhTarg_FullFilepath))
                    {
                        commandPrompt.OpenInstance();
                        commandPrompt.ExeName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _ecrOptions.ThirdPartyGeneratorSourcePath, "resgen.exe");
                        commandPrompt.Arguments.Clear();
                        commandPrompt.Arguments.Add(resource.PhSrc_FullFilepath);
                        commandPrompt.Arguments.Add(resource.PhTarg_FullFilepath);
                        commandPrompt.Run();

                        if (commandPrompt.Error.Length.Equals(0))
                            phStatus = GenerationStatus.Success;
                        else
                        {
                            ErrorCollection error = new ErrorCollection(ObjectType.Resource, resource.PhSrc_FullFilepath, string.Empty);
                            error.AddDescription(commandPrompt.Error);

                            _logger.WriteLogToFile("compileresource", commandPrompt.Error, bError: true);

                            phStatus = GenerationStatus.Failure;
                        }

                        if (errStatus.Equals(GenerationStatus.Success) && phStatus.Equals(GenerationStatus.Success))
                            resource.Status = GenerationStatus.Success;
                        else
                            resource.Status = GenerationStatus.Failure;

                        commandPrompt.CloseInstance();
                    }
                }
                #endregion

                #region Embed resource file and Generate resource dll
                Common.CreateDirectory(Path.GetDirectoryName(_ecrOptions.generation.ResourcedllRelPath));
                commandPrompt = new ShellPrompt(this._logger);
                commandPrompt.OpenInstance();
                commandPrompt.ExeName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _ecrOptions.ThirdPartyGeneratorSourcePath, "al.exe");
                commandPrompt.Arguments.Clear();
                commandPrompt.Arguments.Add("/t:lib");
                foreach (Resource resource in _ecrOptions.generation.resources)
                {
                    commandPrompt.Arguments.Add(string.Format("/embed:\"{0}\"", resource.ErrTarg_FullFilepath));
                    if (!string.IsNullOrEmpty(resource.PhSrc_FullFilepath) && !string.IsNullOrEmpty(resource.PhTarg_FullFilepath))
                        commandPrompt.Arguments.Add(string.Format("/embed:\"{0}\"", resource.PhTarg_FullFilepath));
                }
                commandPrompt.Arguments.Add("/culture:Neutral");
                commandPrompt.Arguments.Add(string.Format("/Version:\"{0}\"", "1.4.3.12"));
                commandPrompt.Arguments.Add(string.Format("/out:\"{0}\"", _ecrOptions.generation.ResourcedllRelPath));
                //commandPrompt.Arguments.Add(string.Format("/template:{0}", string.Format("{0}.dll", GlobalVar.Component)));
                commandPrompt.Arguments.Add(string.Format("/keyfile:\"{0}\"", Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _ecrOptions.ReferenceDllSourcePath, "snk", "RVWSnk.snk")));
                commandPrompt.Arguments.Add(string.Format("/descr:\"{0}\"", _ecrOptions.AssemblyDescription));
                commandPrompt.Run();

                if (commandPrompt.Error.Length.Equals(0))
                    _ecrOptions.generation.ResourceCompilationStatus = GenerationStatus.Success;
                else
                {
                    ErrorCollection error = new ErrorCollection(ObjectType.Resource, _ecrOptions.generation.ResourcedllRelPath, string.Empty);
                    error.AddDescription(commandPrompt.Error);

                    _logger.WriteLogToFile("compileresource", commandPrompt.Error, bError: true);

                    _ecrOptions.generation.ResourceCompilationStatus = GenerationStatus.Failure;
                }

                commandPrompt.CloseInstance();
                #endregion

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("CompileResource->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }

        /// <summary>
        /// Compiles Error Handler cs to netmodule
        /// </summary>
        /// <returns></returns>
        public bool CompileErrHdlr()
        {
            Boolean compHasApiConsumerService;

            try
            {
                _logger.WriteLogToFile("CompileErrHdlr", "Compiling Error Handler..");

                ErrorHandler errorHdlr = _ecrOptions.generation.errorHandler;
                compHasApiConsumerService = _ecrOptions.generation.services.Where(s => s.ConsumesApi).Any();
                List<string> referencedlls = GetCommonServiceReferences(compHasApiConsumerService);

                //if (errorHdlr.Status.Equals(GenerationStatus.SourceCodeGenerated))
                _ecrOptions.generation.errorHandler.Status = CompileAssembly(new string[] { errorHdlr.SourcePath }, errorHdlr.ReleasePath, referencedlls, ObjectType.ErrorHandler, compHasApiConsumerService);

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("CompileErrHdlr->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }

        private bool CompileISDump()
        {
            try
            {
                List<string> referenceDlls = null;
                Boolean compHasApiConsumerService;

                compHasApiConsumerService = _ecrOptions.generation.services.Where(s => s.ConsumesApi).Any();

                string sPredefinedISDirStruct = Path.Combine(_ecrOptions.GenerationPath, _ecrOptions.Platform, _ecrOptions.Customer, _ecrOptions.Project, _ecrOptions.Ecrno, "Updated", _ecrOptions.Component, "Source", "Service", "IS");
                if (Directory.Exists(sPredefinedISDirStruct))
                {
                    foreach (string sISfolderPath in Directory.EnumerateDirectories(sPredefinedISDirStruct))
                    {
                        DirectoryInfo diISDirectory = new DirectoryInfo(sISfolderPath);
                        string sTargetFilePath = String.Empty;

                        #region  for compiling is service within same component
                        if (diISDirectory.Name.ToLower().Equals(_ecrOptions.Component.ToLower()))
                        {
                            foreach (string sISService in Directory.GetFiles(Path.Combine(sISfolderPath, "S"), "*.cs"))
                            {
                                referenceDlls = new List<string>();
                                referenceDlls.Add("System.dll");
                                referenceDlls.Add("System.Web.dll");
                                referenceDlls.Add("System.Xml.dll");
                                referenceDlls.Add(Path.Combine(_sReferenceDllDirectory, "CUtil.dll"));
                                referenceDlls.Add(Path.Combine(_sReferenceDllDirectory, "VirtualWorksRT.dll"));
                                sTargetFilePath = Path.Combine(_ecrOptions.GenerationPath, _ecrOptions.Platform, _ecrOptions.Customer, _ecrOptions.Project, _ecrOptions.Ecrno, "Updated", _ecrOptions.Component, "Source", "Service", "IS", (Path.GetFileNameWithoutExtension(sISService) + ".netmodule"));
                                CompileAssembly(new string[] { sISService }, sTargetFilePath, referenceDlls, ObjectType.IS, compHasApiConsumerService);
                            }
                            continue;
                        }
                        #endregion


                        else
                            sTargetFilePath = System.IO.Path.Combine(diISDirectory.Parent.FullName, string.Format("{0}.dll", diISDirectory.Name));

                        referenceDlls = new List<string>();
                        referenceDlls.Add("System.dll");
                        referenceDlls.Add("System.Web.dll");
                        referenceDlls.Add("System.Xml.dll");
                        referenceDlls.Add(Path.Combine(_sReferenceDllDirectory, "CUtil.dll"));
                        referenceDlls.Add(Path.Combine(_sReferenceDllDirectory, "VirtualWorksRT.dll"));

                        string[] sourceFiles = Directory.GetFiles(Path.Combine(diISDirectory.FullName, "S"), "*.cs", System.IO.SearchOption.TopDirectoryOnly);
                        CompileAssembly(sourceFiles, sTargetFilePath, referenceDlls, ObjectType.IS, compHasApiConsumerService);
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("CompileISDump->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }
    }
}
