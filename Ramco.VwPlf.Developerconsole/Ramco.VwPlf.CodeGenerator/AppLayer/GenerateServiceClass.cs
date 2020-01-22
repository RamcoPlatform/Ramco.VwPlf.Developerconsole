using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.CodeDom;
using System.Data;
using System.Collections.Specialized;
using System.IO;
using System.Collections;
using System.Reflection;
using Newtonsoft.Json;
using System.Diagnostics;

namespace Ramco.VwPlf.CodeGenerator.AppLayer
{
    internal class GenerateServiceClass : AbstractCSFileGenerator
    {
        private Service _service;
        private ECRLevelOptions _ecrOptions;

        private DataTable _fw_context_parameters = new DataTable { Columns = { "parametername", "dbtype", { "length", typeof(int) }, "parametervalue" } };

        //Constants
        private const string CONTEXT_SEGMENT = "fw_context";
        private const string ERR_SEGMENT_NAME = "errordetails";
        private const string SYSFPROWNO = "sysfprowno";

        private const string IN_SEGMENT = "0";
        private const string OUT_SEGMENT = "1";
        private const string IO_SEGMENT = "2";
        private const string SCRATCH_SEGMENT = "3";

        private const string IN_DATAITEM = "0";
        private const string OUT_DATAITEM = "1";
        private const string IO_DATAITEM = "2";
        private const string SCRATCH_DATAITEM = "3";

        //Instance Types
        private const string MULTI_INSTANCE = "1";
        private const string SINGLE_INSTANCE = "0";

        private string[] _ado_double = new string[] { "DOUBLE", "NUMERIC" };
        private string[] _ado_int = new string[] { "LONG", "INT", "INTEGER" };
        private string[] _ado_string = new string[] { "STRING", "DATETIME", "CHAR", "NVARCHAR", "DATE", "TIME", "ENUMERATED", "DATE-TIME" };
        static string _isobjectname = String.Empty;

        public string[] Ado_string
        {
            get
            {
                return _ado_string;
            }

            set
            {
                _ado_string = value;
            }
        }

        public GenerateServiceClass(ref Service service, ECRLevelOptions ecrOptions) : base(ecrOptions)
        {
            this._service = service;
            this._ecrOptions = ecrOptions;
            base._objectType = ObjectType.Service;
            base._targetFileName = string.Format("C{0}", service.Name);
            base._targetDir = Path.Combine(_ecrOptions.GenerationPath, _ecrOptions.Platform, _ecrOptions.Customer, _ecrOptions.Project, _ecrOptions.Ecrno, "Updated", _ecrOptions.Component, "Source", "Service");

            InitializeContextParameter();
        }

        public void InitializeContextParameter()
        {
            _fw_context_parameters.Rows.Add("@ctxt_ouinstance", "DBType.NVarchar", 4, "szOUI");
            _fw_context_parameters.Rows.Add("@ctxt_user", "DBType.NVarchar", 32, "szUser");
            _fw_context_parameters.Rows.Add("@ctxt_language", "DBType.Int", 32, "szLangID");
            _fw_context_parameters.Rows.Add("@ctxt_service", "DBType.NVarchar", 32, "szServiceName");
            _fw_context_parameters.Rows.Add("@ctxt_role", "DBType.NVarchar", 32, "szRole");
            _fw_context_parameters.Rows.Add("@ctxt_component", "DBType.NVarchar", 32, "szComponentName");
        }

        public override void CreateNamespace()
        {
            base._csFile.NameSpace.Name = String.Format("com.ramco.vw.{0}.service", _ecrOptions.Component.ToLower());
        }

        public override void ImportNamespace()
        {
            base._csFile.ReferencedNamespace.Add("System");
            base._csFile.ReferencedNamespace.Add("System.Collections");
            base._csFile.ReferencedNamespace.Add("System.Collections.Specialized");
            base._csFile.ReferencedNamespace.Add("System.Diagnostics");
            base._csFile.ReferencedNamespace.Add("System.IO");
            base._csFile.ReferencedNamespace.Add("System.Text");
            base._csFile.ReferencedNamespace.Add("System.Xml");
            base._csFile.ReferencedNamespace.Add("com.ramco.vw.tp");
            if (_service.HasCusBRO || _service.HasTypeBasedBRO || _service.HasUniversalPersonalization)
                base._csFile.ReferencedNamespace.Add("System.Collections.Generic");
            if (_service.IsZipped)
                base._csFile.ReferencedNamespace.Add("System.IO.Compression");
            if (_service.HasTypeBasedBRO || _service.HasCusBRO)
                base._csFile.ReferencedNamespace.Add($"com.ramco.vw.{_ecrOptions.Component.ToLower()}.br");
            if (_service.HasUniversalPersonalization)
                base._csFile.ReferencedNamespace.Add($"com.ramco.vw.C{_service.Name}.ml.br");
            //else if (_service.HasCusBRO)
            //    base._csFile.ReferencedNamespace.Add(String.Format("com.ramco.vw.{0}.cus", GlobalVar.Component.ToLower()));
            base._csFile.ReferencedNamespace.Add(String.Format("com.ramco.vw.{0}.ehs", _ecrOptions.Component.ToLower()));

            if ((from ps in _service.ProcessSections
                 from mt in ps.Methods
                 where mt.IsApiConsumerService.Equals(true)
                 select mt).Any())
            {
                base._csFile.ReferencedNamespace.Add("System.Collections.Generic");
                base._csFile.ReferencedNamespace.Add("Ramco.VW.RT.ApiProxy");
                base._csFile.ReferencedNamespace.Add("Newtonsoft.Json.Linq");
                base._csFile.ReferencedNamespace.Add("Ramco.VW.RT.ApiMetadata");
            }
        }

        public override void CreateClasses()
        {
            try
            {
                CodeTypeDeclaration serviceCls = new CodeTypeDeclaration
                {
                    Name = string.Format("C{0}", _service.Name),
                    IsClass = true,
                    Attributes = MemberAttributes.Public
                };
                serviceCls.BaseTypes.Add(new CodeTypeReference("CUtil"));

                this.AddMemberFields(ref serviceCls);
                this.AddMemberFunctions(ref serviceCls);

                base._csFile.UserDefinedTypes.Add(serviceCls);

            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("CreateClasses->{0}", !Object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }

        public override void AddCustomAttributes()
        {
            //throw new NotImplementedException();
        }

        public override void AddMemberFields(ref CodeTypeDeclaration classObj)
        {
            try
            {
                List<String> PreviouslyDeclared_DIs = new List<String>();

                #region Common Varaibles
                DeclareMemberField(MemberAttributes.Private, classObj, "result", typeof(double), false, null);
                DeclareMemberField(MemberAttributes.Private, classObj, "lSPErrorID", typeof(long), false, null);
                if (_service.IsZipped)
                    DeclareMemberField(MemberAttributes.Private, classObj, "serviceElapsedTimeinms", typeof(long), false);

                DeclareMemberField(MemberAttributes.Private, classObj, "nRecCount", typeof(long), true, null, PrimitiveExpression(0));
                DeclareMemberField(MemberAttributes.Private, classObj, "nLoop", typeof(long), false);
                DeclareMemberField(MemberAttributes.Private, classObj, "nMax", typeof(long), true, null, PrimitiveExpression(1));
                DeclareMemberField(MemberAttributes.Private, classObj, "nErrMax", typeof(long), true, null, PrimitiveExpression(0));
                DeclareMemberField(MemberAttributes.Private, classObj, "lISLoop", typeof(long), true, null, PrimitiveExpression(0));
                DeclareMemberField(MemberAttributes.Private, classObj, "lISOutRecCount", typeof(long), true, null, PrimitiveExpression(0));
                DeclareMemberField(MemberAttributes.Private, classObj, "lInstExists", typeof(long), true, null, PrimitiveExpression(0));
                DeclareMemberField(MemberAttributes.Private, classObj, "lRetVal", typeof(long), true, null, PrimitiveExpression(0));
                DeclareMemberField(MemberAttributes.Private, classObj, "lValue", typeof(long), true, null, PrimitiveExpression(0));
                DeclareMemberField(MemberAttributes.Private, classObj, "dValue", typeof(double?), true, null, PrimitiveExpression(0));
                DeclareMemberField(MemberAttributes.Private, classObj, "defaultValue", typeof(string), true, null, GetProperty(typeof(string), "Empty"));
                DeclareMemberField(MemberAttributes.Private, classObj, "sISKeyValue", typeof(string), true, null, GetProperty(typeof(string), "Empty"));
                DeclareMemberField(MemberAttributes.Private, classObj, "szErrorDesc", typeof(string), false);
                DeclareMemberField(MemberAttributes.Private, classObj, "szErrSrc", typeof(string), false);
                DeclareMemberField(MemberAttributes.Private, classObj, "sValue", typeof(string), true, null, GetProperty(typeof(string), "Empty"));
                DeclareMemberField(MemberAttributes.Private, classObj, "mStream", typeof(MemoryStream), true, null, SnippetExpression("null"));
                DeclareMemberField(MemberAttributes.Private, classObj, "nvcTmp", typeof(NameValueCollection), true, null, SnippetExpression("null"));
                DeclareMemberField(MemberAttributes.Private, classObj, "nvcISTmp", typeof(NameValueCollection), true, null, SnippetExpression("null"));
                DeclareMemberField(MemberAttributes.Private, classObj, "nvcISTmpIS", typeof(NameValueCollection), true, null, SnippetExpression("null"));
                DeclareMemberField(MemberAttributes.Private, classObj, "nvcFilterText", typeof(NameValueCollection), true, null, SnippetExpression("null"));
                DeclareMemberField(MemberAttributes.Private, classObj, "nvcTmpCrtl", typeof(NameValueCollection), true, null, SnippetExpression("null"));
                #endregion

                #region Declarations for Segments
                foreach (Segment segment in _service.Segments)
                {

                    if (segment.Inst.Equals(SINGLE_INSTANCE))
                        DeclareMemberField(MemberAttributes.Public, classObj, String.Format("nvc{0}", segment.Name.ToUpper()), typeof(NameValueCollection), true);
                    else if (segment.Inst.Equals(MULTI_INSTANCE))
                        DeclareMemberField(MemberAttributes.Public, classObj, String.Format("ht{0}", segment.Name.ToUpper()), typeof(Hashtable), true);

                }
                #endregion

                #region Object Declarations for Bulk & BRO Service
                //Bulk
                if (_service.IsBulkService)
                    DeclareMemberField(MemberAttributes.Private, classObj, "objBulk", String.Format("com.ramco.vw.{0}.bulk.C{0}_bulk", _ecrOptions.Component.ToLowerInvariant()), true);

                //Type Based BRO
                if (_service.HasTypeBasedBRO)
                    DeclareMemberField(MemberAttributes.Private, classObj, "objBRO", String.Format("I{0}BR", Common.InitCaps(_ecrOptions.Component)), false);
                //Custom BRO
                else if (_service.HasCusBRO)
                    DeclareMemberField(MemberAttributes.Private, classObj, "objBRO", String.Format("I{0}BR", Common.InitCaps(_ecrOptions.Component)), false);

                //If intd or hasbulk
                if (_ecrOptions.InTD || _service.IsBulkService)
                    DeclareMemberField(MemberAttributes.Private, classObj, "szInTD", typeof(String), true, null, GetProperty(typeof(String), "Empty"));
                #endregion

                //Universal Personlization
                if (_service.HasUniversalPersonalization)
                    DeclareMemberField(MemberAttributes.Private, classObj, "universalPersonalization", typeof(bool), true, null, PrimitiveExpression(false));

                #region BRO Related declarations
                PreviouslyDeclared_DIs.Clear();
                foreach (ProcessSection ps in _service.ProcessSections)
                {
                    foreach (Method mt in ps.Methods)
                    {
                        bool bSystemGeneratedMethod = mt.SystemGenerated.Equals(BRTypes.SYSTEMGENERATED);
                        foreach (Parameter p in mt.Parameters.Where(p => p.Seg != null && string.Compare(p.Seg.Name, CONTEXT_SEGMENT, true) != 0))
                        {
                            string sSegmentName = p.Seg.Name;
                            string sDataitemName = p.DI.Name;
                            string sParamType = p.CategorizedDataType;
                            string sParamFlowDirection = p.FlowDirection;

                            if (!PreviouslyDeclared_DIs.Contains(String.Concat(sSegmentName, sDataitemName).ToLower()))
                            {
                                if (!bSystemGeneratedMethod)
                                {
                                    if (sParamType == DataType.STRING)
                                    {
                                        DeclareMemberField(MemberAttributes.Private, classObj, String.Format("sz{0}{1}", sSegmentName, sDataitemName).ToLowerInvariant(), typeof(string), true, null, GetProperty(typeof(String), "Empty"));
                                        PreviouslyDeclared_DIs.Add(String.Concat(sSegmentName, sDataitemName).ToLower());
                                    }
                                    else
                                    {
                                        if (_ado_int.Contains(sParamType))
                                        {
                                            DeclareMemberField(MemberAttributes.Private, classObj, String.Format("l{0}{1}", sSegmentName, sDataitemName).ToLowerInvariant(), typeof(long), true, null, PrimitiveExpression(0));
                                            DeclareMemberField(MemberAttributes.Private, classObj, String.Format("sz{0}{1}", sSegmentName, sDataitemName).ToLowerInvariant(), typeof(string), true, null, PrimitiveExpression("0"));
                                            PreviouslyDeclared_DIs.Add(String.Concat(sSegmentName, sDataitemName).ToLower());
                                        }
                                        else if (_ado_double.Contains(sParamType))
                                        {
                                            DeclareMemberField(MemberAttributes.Private, classObj, String.Format("dbl{0}{1}", sSegmentName, sDataitemName).ToLowerInvariant(), typeof(double), true, null, PrimitiveExpression(0));
                                            DeclareMemberField(MemberAttributes.Private, classObj, String.Format("sz{0}{1}", sSegmentName, sDataitemName).ToLowerInvariant(), typeof(string), true, null, PrimitiveExpression("0"));
                                            PreviouslyDeclared_DIs.Add(String.Concat(sSegmentName, sDataitemName).ToLower());
                                        }
                                    }
                                }
                            }

                        }
                    }
                }
                #endregion

                #region For Backward compatiblility
                foreach (string variableName in (from ps in _service.ProcessSections
                                                 from mt in ps.Methods
                                                 from p in mt.Parameters
                                                 where string.Compare(p.Seg.Name, CONTEXT_SEGMENT, true) != 0
                                                 && (p.FlowDirection == "0" || p.FlowDirection == "2")
                                                 //&& p.Seg != null && p.DI != null 
                                                 && (p.CategorizedDataType == DataType.INT || p.CategorizedDataType == DataType.DOUBLE)
                                                 select p.Seg.Name.ToUpper() + p.DI.Name.ToLower()).Distinct())
                {
                    //DeclareMemberField(MemberAttributes.Private, classObj, "l" + variableName, typeof(long), true, null, PrimitiveExpression(0));
                    DeclareMemberField(MemberAttributes.Private, classObj, "s" + variableName, typeof(string), true, null, PrimitiveExpression("0"));
                }
                #endregion

                #region rowno change
                //DeclareMemberField(MemberAttributes.Private, classObj, "s_HSEGSYSFPROWNO", typeof(String), true, null, PrimitiveExpression("0"));
                DeclareMemberField(MemberAttributes.Private, classObj, "modeFlagValue", typeof(String), true, null, GetProperty(typeof(String), "Empty"));
                #endregion

                #region fields for default as null
                if (_ecrOptions.TreatDefaultAsNull)
                {
                    //DeclareMemberField(MemberAttributes.Private, classObj, "dValue", typeof(double?), true, InitExpression: PrimitiveExpression(0));
                    DeclareMemberField(MemberAttributes.Private, classObj, "iValue", typeof(Int32?), true, InitExpression: PrimitiveExpression(0));
                    DeclareMemberField(MemberAttributes.Private, classObj, "dateValue", typeof(DateTime?), false);
                }
                #endregion  
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("AddMemberFields->{0}", !Object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }

        //Universal Personalization
        private List<CodeMemberProperty> AddProperties()
        {
            List<CodeMemberProperty> properties = new List<CodeMemberProperty>();

            if (_service.HasUniversalPersonalization)
            {
                CodeMemberProperty property = new CodeMemberProperty();
                property.Name = "EnableUniversalPersonalization";
                property.Type = new CodeTypeReference(typeof(bool));
                property.Attributes = MemberAttributes.Public;

                property.HasGet = true;
                property.GetStatements.Add(ReturnExpression(VariableReferenceExp("universalPersonalization")));
                property.HasSet = true;
                property.SetStatements.Add(AssignVariable(VariableReferenceExp("universalPersonalization"), VariableReferenceExp("value")));

                properties.Add(property);
            }

            return properties;
        }

        public override void AddMemberFunctions(ref CodeTypeDeclaration classObj)
        {
            classObj.Members.Add(this.Constructor());

            foreach (var property in AddProperties())
                classObj.Members.Add(property);

            classObj.Members.Add(this.GetBOD());
            classObj.Members.Add(this.BuildOutSegments());
            classObj.Members.Add(this.GetFieldValue());
            classObj.Members.Add(this.GetSegmentType());
            classObj.Members.Add(this.GetSegmentRecCount());
            classObj.Members.Add(this.GetMultiSegment());
            classObj.Members.Add(this.GetSingleSegment());
            classObj.Members.Add(this.GetSegmentValue());
            classObj.Members.Add(this.ValidateMandatorySegment());
            classObj.Members.Add(this.FillUnMappedDataItems());

            if (_service.IsZipped)
            {
                classObj.Members.Add(this.ProcessZippedService_Zipped());
                classObj.Members.Add(this.GetBODBytes_Zipped());
                classObj.Members.Add(this.ProcessService_Zipped());
                classObj.Members.Add(this.ProcessServiceSections_Zipped());
            }
            else
            {
                classObj.Members.Add(this.ProcessService());
            }

            foreach (ProcessSection ps in _service.ProcessSections)
            {
                if (ps.IsUniversalPersonalizedSection != true)
                    classObj.Members.Add(this.ProcessSection(ps));
                else
                    classObj.Members.Add(ProcessUniversalPersonalization(ps));
            }

            classObj.Members.Add(FillPlaceholdervalue());
            classObj.Members.Add(Process_MethodError_Info());

            IEnumerable<Segment> segmentWithKeyDI = from seg in _service.Segments
                                                    from di in seg.DataItems
                                                    where seg.Inst == MULTI_INSTANCE
                                                    && di.PartOfKey == true
                                                    select seg;
            if (segmentWithKeyDI.Any())
            {
                classObj.Members.Add(this.SetRecordPtrEx(segmentWithKeyDI));
            }

            classObj.Members.Add(this.LogMessage());
        }


        /// <summary>
        /// Sets service level options
        /// </summary>
        /// <returns></returns>
        public bool SetServiceLevelOptions()
        {
            bool bStatusFlg;
            StringBuilder sbQuery = new StringBuilder();

            try
            {
                _service.HasCusBRO = (from Method in _ecrOptions.generation.ServiceInfo.Tables["Method"].AsEnumerable()
                                      where string.Compare(Method.Field<String>("servicename"), _service.Name, true) == 0
                                      && string.Compare(Method.Field<String>("systemgenerated"), "0", true) == 0
                                      && string.Compare(Method.Field<String>("ismethod"), "1", true) == 0
                                      && !String.IsNullOrEmpty(Method.Field<String>("accessesdatabase"))
                                      select Method).Any();

                _service.HasTypeBasedBRO = (from Method in _ecrOptions.generation.ServiceInfo.Tables["Method"].AsEnumerable()
                                            where string.Compare(Method.Field<String>("servicename"), _service.Name, true) == 0
                                            && string.Compare(Method.Field<String>("systemgenerated"), "2", true) == 0
                                            && string.Compare(Method.Field<String>("ismethod"), "1", true) == 0
                                            select Method).Any();

                _service.HasTypeBasedBRO = _ecrOptions.generation.ServiceInfo.Tables["Method"].AsEnumerable().Where(m => m.Field<string>("servicename").ToLower().Equals(_service.Name) && m.Field<string>("systemgenerated").Equals("2")).Any();

                _service.IsBulkService = (from Method in _ecrOptions.generation.ServiceInfo.Tables["Method"].AsEnumerable()
                                          where string.Compare(Method.Field<String>("servicename"), _service.Name, true) == 0
                                          && string.Compare(Method.Field<String>("systemgenerated"), "3", true) == 0
                                          && string.Compare(Method.Field<String>("ismethod"), "1", true) == 0
                                          select Method).Any();

                _service.HasRollbackProcessSections = (from PS in _ecrOptions.generation.ServiceInfo.Tables["ProcessSection"].AsEnumerable()
                                                       where string.Compare(PS.Field<String>("servicename"), _service.Name, true) == 0
                                                       && string.Compare(PS.Field<String>("type"), "2", true) == 0
                                                       select PS).Any();

                _service.HasCommitProcessSection = (from PS in _ecrOptions.generation.ServiceInfo.Tables["ProcessSection"].AsEnumerable()
                                                    where string.Compare(PS.Field<String>("servicename"), _service.Name, true) == 0
                                                    && string.Compare(PS.Field<String>("type"), "1", true) == 0
                                                    select PS).Any();

                //DI property mappings
                if (_ecrOptions.generation.ServiceInfo.Tables["DIPropertyMapping"] != null)
                {
                    var tmp = _ecrOptions.generation.ServiceInfo.Tables["DIPropertyMapping"].AsEnumerable().Where(r => string.Compare(r.Field<string>("servicename"), _service.Name, true) == 0);
                    _service.dtDIPropetyMappings = tmp.Any() ? tmp.CopyToDataTable() : _ecrOptions.generation.ServiceInfo.Tables["DIPropertyMapping"].Clone();
                }

                //Validation message
                //_service.HasValidationMessage = (from validationMessage in _ecrOptions.generation.ServiceInfo.Tables["ValidationMessage"].AsEnumerable()
                //                                 where validationMessage.Field<string>("service_name").ToLower().Equals(_service.Name)
                //                                 select validationMessage).Any();

                bStatusFlg = true;
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("SetServiceLevelOptions->{0}", !Object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }

            return bStatusFlg;
        }

        /// <summary>
        /// 
        /// </summary>
        private void FillServiceObject()
        {
            try
            {

                #region adding segment for the service
                foreach (DataRow dr in (from row in _ecrOptions.generation.ServiceInfo.Tables["Segment"].AsEnumerable()
                                        where String.Equals(row.Field<String>("servicename"), _service.Name, StringComparison.OrdinalIgnoreCase)
                                        orderby int.Parse(row.Field<String>("sequence"))
                                        select row))
                {
                    Segment _segment = new Segment();
                    _segment.Name = Convert.ToString(dr["name"]).ToUpperInvariant();
                    _segment.Inst = Convert.ToString(dr["instanceflag"]);
                    _segment.Sequence = Convert.ToString(dr["sequence"]);
                    _segment.MandatoryFlg = Convert.ToString(dr["mandatoryflag"]);
                    _segment.Process_sel_rows = Convert.ToString(dr["process_selrows"]);
                    _segment.Process_upd_rows = Convert.ToString(dr["process_updrows"]);
                    _segment.Process_sel_upd_rows = Convert.ToString(dr["process_selupdrows"]);
                    _segment.FlowDirection = Convert.ToString(dr["flowattribute"]);

                    #region adding dataitems of the segment
                    IEnumerable<DataItem> _dataitems = (from di in _ecrOptions.generation.ServiceInfo.Tables["DataItem"].AsEnumerable()
                                                        where String.Equals(di.Field<String>("servicename"), _service.Name, StringComparison.OrdinalIgnoreCase) && String.Equals(di.Field<String>("segmentname"), _segment.Name, StringComparison.OrdinalIgnoreCase)
                                                        select new DataItem
                                                        {
                                                            Name = di.Field<String>("name"),
                                                            DataType = di.Field<String>("type"),
                                                            FlowDirection = di.Field<String>("flowattribute"),
                                                            PartOfKey = di.Field<String>("ispartofkey").Equals("1"),
                                                            IsMandatory = di.Field<String>("mandatoryflag").Equals("1"),
                                                            DefaultValue = di.Field<String>("defaultvalue")
                                                        }).Distinct();


                    IEnumerable<DataItem> _usedOutDI = (from di in _ecrOptions.generation.ServiceInfo.Tables["OutDataItem"].AsEnumerable()
                                                        where String.Equals(di.Field<String>("servicename"), _service.Name, StringComparison.OrdinalIgnoreCase)
                                                        && String.Equals(di.Field<String>("segmentname"), _segment.Name, StringComparison.OrdinalIgnoreCase)

                                                        select new DataItem
                                                        {
                                                            Name = di.Field<String>("name"),
                                                            DataType = di.Field<String>("type"),
                                                            FlowDirection = di.Field<String>("flowattribute"),
                                                            PartOfKey = di.Field<String>("ispartofkey").Equals("1"),
                                                            IsMandatory = di.Field<String>("mandatoryflag").Equals("1"),
                                                            DefaultValue = di.Field<String>("defaultvalue")
                                                        }).Distinct();

                    _segment.DataItems = new List<DataItem>();
                    _segment.DataItems = _dataitems.ToList<DataItem>();

                    _segment.UsedOutDI = new List<DataItem>();
                    _segment.UsedOutDI = _usedOutDI.ToList<DataItem>(); ;
                    #endregion

                    var dataitemsWithoutScratch = _dataitems.Where(d => d.FlowDirection != FlowAttribute.SCRATCH);

                    _service.Segments.Add(_segment);
                }
                #endregion

                #region adding process section
                foreach (DataRow psInfo in (from processSection in _ecrOptions.generation.ServiceInfo.Tables["ProcessSection"].AsEnumerable()
                                            where String.Equals(processSection["servicename"].ToString(), _service.Name, StringComparison.OrdinalIgnoreCase)
                                            orderby processSection["seqno"] ascending
                                            select processSection))
                {
                    //todo
                    Console.WriteLine("before process section...");

                    ProcessSection ps = new ProcessSection
                    {
                        Name = Convert.ToString(psInfo["name"]),
                        Type = Convert.ToString(psInfo["type"]),
                        SeqNO = Convert.ToString(psInfo["seqno"]),
                        ProcessingType = Convert.ToString(psInfo["loopingstyle"]),
                        Expression = Convert.ToString(psInfo["controlexpression"]),
                        //IsUniversalPersonalizedSection = string.Compare(psInfo.Field<string>("IsUniversalPersonalization"), "y", true) == 0,
                        RelationalData = psInfo.GetChildRows("R_ps_method")
                    };

                    if (ps.IsUniversalPersonalizedSection)
                        _service.HasUniversalPersonalization = true;

                    ControlExpression psControlExpression = new ControlExpression();
                    psControlExpression.Expression = ps.Expression;
                    psControlExpression.Seg = new Segment();
                    psControlExpression.DI = new DataItem();

                    //Validating control expression
                    if (psControlExpression.IsValid)
                    {
                        //split segment & di name from ctrl expression
                        string[] CtrlExpSegAndDIName = psControlExpression.Expression.Split('.');
                        try
                        {
                            psControlExpression.Seg.Name = CtrlExpSegAndDIName[0].Trim();
                            psControlExpression.DI.Name = CtrlExpSegAndDIName[1];
                        }
                        catch
                        {
                            throw new Exception(String.Format("Error while splitting seg & di value from control expression : {0}", CtrlExpSegAndDIName));
                        }

                        //getting Ctrl expression segment's instance
                        var tmp = _service.Segments.Where(seg => seg.Name.ToLower() == psControlExpression.Seg.Name.ToLower());
                        psControlExpression.Seg.Inst = tmp.Any() ? tmp.First().Inst : string.Empty;
                        //dtTemp = null;
                        //dtTemp = _service.GetSegmentInfo(_service.Name, psControlExpression.Seg.Name).AsEnumerable();
                        //psControlExpression.Seg.Inst = dtTemp.Any().Equals(true) ? Convert.ToString(dtTemp.First()["InstanceFlag"]) : String.Empty;

                        ps.CtrlExp = psControlExpression;
                    }

                    #region adding method
                    foreach (DataRow mtInfo in ps.RelationalData)
                    {
                        //todo
                        Console.WriteLine("before method...");
                        Method mt = new Method
                        {
                            Name = Convert.ToString(mtInfo["name"]),
                            ID = Convert.ToString(mtInfo["id"]),
                            SeqNO = Convert.ToString(mtInfo["seqno"]),
                            SectionName = ps.Name,
                            PSSeqNO = ps.SeqNO,
                            IsIntegService = Convert.ToString(mtInfo["ismethod"]).Equals("0"),
                            IsApiConsumerService = Convert.ToString(mtInfo["ismethod"]).Equals("3"),
                            //ExecutionFlg = Convert.ToString(mtInfo["ExecutionFlag"]).Equals("1") ? "1" : "0",
                            //ConnectivityFlg = Convert.ToString(mtInfo["ConnectivityFlag"]).Equals("1"),
                            AccessDatabase = Convert.ToString(mtInfo["accessesdatabase"]).Equals("1"),
                            SystemGenerated = !string.IsNullOrEmpty(Convert.ToString(mtInfo["name"])) ? Convert.ToString(mtInfo["systemgenerated"]) : "",
                            method_exec_cont = string.Compare(Convert.ToString(mtInfo["method_exec_cont"]), "y", true) == 0,
                            OperationType = Convert.ToString(mtInfo["operationtype"]).Equals("1"),
                            SPName = Convert.ToString(mtInfo["spname"]),
                            SPErrorProtocol = Object.Equals(mtInfo["sperrorprotocol"], null) ? "0" : mtInfo["sperrorprotocol"].ToString(),
                            ISMappings = mtInfo.GetChildRows("R_method_ismapping").Any() ? mtInfo.GetChildRows("R_method_ismapping").CopyToDataTable() : new DataTable(),
                            RelationData_Params = mtInfo.GetChildRows("R_method_params").Any() ? mtInfo.GetChildRows("R_method_params").CopyToDataTable() : new DataTable(),
                            RelationData_ApiRequest = mtInfo.GetChildRows("R_method_apirequest").Any() ? mtInfo.GetChildRows("R_method_apirequest").CopyToDataTable() : new DataTable(),
                            RelationData_ApiResponse = mtInfo.GetChildRows("R_method_apiresponse").Any() ? mtInfo.GetChildRows("R_method_apiresponse").CopyToDataTable() : new DataTable(),
                            RelationData_ApiParameter = mtInfo.GetChildRows("R_method_apiparameter").Any() ? mtInfo.GetChildRows("R_method_apiparameter").CopyToDataTable() : new DataTable(),
                            //SpecID = Convert.ToString(mtInfo["specid"]),
                            //SpecName = Convert.ToString(mtInfo["specname"]),
                            //SpecVersion = Convert.ToString(mtInfo["specversion"]),
                            //RoutePath = Convert.ToString(mtInfo["path"]),
                            //OperationID = Convert.ToString(mtInfo["operationid"]),
                            //OperationVerb = Convert.ToString(mtInfo["operationverb"])
                        };

                        if (mt.RelationData_ApiRequest.Rows.Count > 0)
                            mt.ApiRequestInfo = new List<ApiRequest>();
                        foreach (DataRow dr in mt.RelationData_ApiRequest.Rows)
                        {
                            ApiRequest apiRequest = new ApiRequest
                            {
                                DisplayName = dr.Field<string>("displayname"),
                                Identifier = dr.Field<string>("identifier"),
                                NodeID = dr.Field<string>("nodeid"),
                                ParentNodeID = dr.Field<string>("parentnodeid"),
                                ParentSchemaName = dr.Field<string>("parentschemaname"),
                                SchemaCategory = dr.Field<string>("schemacategory"),
                                SchemaName = dr.Field<string>("schemaname"),
                                Type = dr.Field<string>("type"),
                                SchemaType = dr.Field<string>("schematype")
                            };
                            apiRequest.Segment = !string.IsNullOrEmpty(dr.Field<string>("segmentname")) ? _service.Segments.SingleOrDefault(s => string.Compare(s.Name, dr.Field<string>("segmentname"), true) == 0) : null;
                            apiRequest.DataItem = !string.IsNullOrEmpty(dr.Field<string>("dataitemname")) ? apiRequest.Segment.DataItems.Single(d => string.Compare(d.Name, dr.Field<string>("dataitemname"), true) == 0) : null;
                            mt.ApiRequestInfo.Add(apiRequest);
                        }

                        if (mt.RelationData_ApiResponse.Rows.Count > 0)
                            mt.ApiResponseInfo = new List<ApiResponse>();
                        foreach (DataRow dr in mt.RelationData_ApiResponse.Rows)
                        {
                            ApiResponse apiResponse = new ApiResponse
                            {
                                DisplayName = dr.Field<string>("displayname"),
                                Identifier = dr.Field<string>("identifier"),
                                MediaType = dr.Field<string>("mediatype"),
                                NodeID = dr.Field<string>("nodeid"),
                                ParentNodeID = dr.Field<string>("parentnodeid"),
                                ParentSchemaName = dr.Field<string>("parentschemaname"),
                                ResponseCode = dr.Field<string>("responsecode"),
                                SchemaCategory = dr.Field<string>("schemacategory"),
                                SchemaName = dr.Field<string>("schemaname"),
                                Type = dr.Field<string>("type"),
                                SchemaType = dr.Field<string>("schematype")
                            };
                            apiResponse.Segment = !string.IsNullOrEmpty(dr.Field<string>("segmentname")) ? _service.Segments.SingleOrDefault(s => string.Compare(s.Name, dr.Field<string>("segmentname"), true) == 0) : null;
                            apiResponse.DataItem = !string.IsNullOrEmpty(dr.Field<string>("dataitemname")) ? apiResponse.Segment.DataItems.Single(d => string.Compare(d.Name, dr.Field<string>("dataitemname"), true) == 0) : null;
                            mt.ApiResponseInfo.Add(apiResponse);
                        }


                        if (mt.RelationData_ApiParameter.Rows.Count > 0)
                            mt.ApiParameters = new List<ApiParameter>();
                        foreach (DataRow dr in mt.RelationData_ApiParameter.Rows)
                        {
                            ApiParameter apiParameter = new ApiParameter
                            {
                                ParameterName = dr.Field<string>("parametername"),
                                In = dr.Field<string>("in")
                            };

                            apiParameter.Segment = !string.IsNullOrEmpty(dr.Field<string>("segmentname")) ? _service.Segments.SingleOrDefault(s => string.Compare(s.Name, dr.Field<string>("segmentname"), true) == 0) : null;
                            apiParameter.DataItem = !string.IsNullOrEmpty(dr.Field<string>("dataitemname")) ? apiParameter.Segment.DataItems.Single(d => string.Compare(d.Name, dr.Field<string>("dataitemname"), true) == 0) : null;
                            mt.ApiParameters.Add(apiParameter);
                        }

                        if (mtInfo.GetChildRows("R_method_apioperationalparameter").Count() > 0)
                        {
                            if (mt.ApiParameters == null || mt.ApiParameters.Count == 0)
                                mt.ApiParameters = new List<ApiParameter>();
                            foreach (DataRow dr in mtInfo.GetChildRows("R_method_apioperationalparameter"))
                            {
                                ApiParameter apiParameter = new ApiParameter
                                {
                                    ParameterName = dr.Field<string>("parametername"),
                                    In = dr.Field<string>("in")
                                };

                                apiParameter.Segment = !string.IsNullOrEmpty(dr.Field<string>("segmentname")) ? _service.Segments.SingleOrDefault(s => string.Compare(s.Name, dr.Field<string>("segmentname"), true) == 0) : null;
                                apiParameter.DataItem = !string.IsNullOrEmpty(dr.Field<string>("dataitemname")) ? apiParameter.Segment.DataItems.Single(d => string.Compare(d.Name, dr.Field<string>("dataitemname"), true) == 0) : null;
                                mt.ApiParameters.Add(apiParameter);
                            }
                        }

                        if (mt.method_exec_cont == true)
                            _service.Implement_New_Method_Of_ParamAddition = true;

                        string sControlExpression = Convert.ToString(mtInfo["controlexpression"]);
                        ControlExpression mtControlExpression = new ControlExpression();
                        mtControlExpression.Expression = sControlExpression;
                        mtControlExpression.Seg = new Segment();
                        mtControlExpression.DI = new DataItem();
                        //Validating control expression
                        if (mtControlExpression.IsValid)
                        {
                            //split segment & di name from ctrl expression
                            string[] CtrlExpSegAndDIName = mtControlExpression.Expression.Split('.');
                            try
                            {
                                mtControlExpression.Seg.Name = CtrlExpSegAndDIName[0];
                                mtControlExpression.DI.Name = CtrlExpSegAndDIName[1];
                            }
                            catch
                            {
                                throw new Exception(String.Format("Error while splitting seg & di value from control expression : {0}", CtrlExpSegAndDIName));
                            }

                            var tmp = _service.Segments.Where(seg => seg.Name.ToLower() == mtControlExpression.Seg.Name.ToLower());
                            mtControlExpression.Seg.Inst = tmp.Any() ? tmp.First().Inst : string.Empty;

                            mt.CtrlExp = mtControlExpression;
                        }

                        #region adding parameters of the method
                        mt.Parameters = (from param in mt.RelationData_Params.AsEnumerable()
                                             //where !String.Equals(param.Field<String>("DataSegmentName"), CONTEXT_SEGMENT, StringComparison.OrdinalIgnoreCase)
                                         select new Parameter
                                         {
                                             Name = param.Field<String>("physicalparametername"),
                                             MethodID = mt.ID,
                                             MethodSequenceNo = param.Field<string>("sequenceno"),
                                             DataType = param.Field<String>("brparamtype").ToUpperInvariant(),
                                             CategorizedDataType = Common.CategorizeDIType(param.Field<String>("brparamtype").ToUpperInvariant()),
                                             Length = param.Field<String>("length"),
                                             DecimalLength = param.Field<String>("decimallength"),
                                             FlowDirection = param.Field<String>("flowdirection"),
                                             SequenceNo = param.Field<String>("seqno"),
                                             Seg = _service.Segments.Where(seg => String.Equals(seg.Name.Trim(), param.Field<String>("datasegmentname").Trim(), StringComparison.OrdinalIgnoreCase)).Any() ? _service.Segments.Where(seg => String.Equals(seg.Name.Trim(), param.Field<String>("datasegmentname").Trim(), StringComparison.OrdinalIgnoreCase)).First() : new Segment { Name = param.Field<String>("datasegmentname").Trim(), Inst = SINGLE_INSTANCE },
                                             DI = new DataItem
                                             {
                                                 Name = param.Field<String>("dataitemname"),
                                                 DataType = param.Field<String>("brparamtype"),
                                             },
                                             RecordSetName = param.Field<String>("recordsetname")
                                         }).OrderBy(p => int.Parse(p.SequenceNo)).ToList<Parameter>();
                        #endregion

                        #region set loop segment details for the method

                        string sLoopCausingSegmentName = Convert.ToString(mtInfo["loopcausingsegment"]);
                        if (!string.IsNullOrEmpty(sLoopCausingSegmentName))
                            mt.LoopSegment = _service.Segments.Where(s => s.Name.ToLower() == sLoopCausingSegmentName.ToLower()).First();

                        #endregion

                        //#region Set BR Execution flag
                        //bool bSegmentInThisBrEqualsToLoopSegment = !Object.Equals(mt.LoopSegment, null) ? mt.Parameters.Where(p => p.Seg != null && p.Seg.Name == mt.LoopSegment.Name).Any() : false;
                        //bool bAllKeyParamAreIN = mt.Parameters.Where(p => p.DI.PartOfKey == true).All(p => p.FlowDirection == FlowAttribute.IN || p.FlowDirection == FlowAttribute.INOUT);
                        ////bool bHasKeyParams = mt.Parameters.Where(p => p.DI.PartOfKey == true);

                        //foreach (Parameter p in mt.Parameters.Where(tp => tp.FlowDirection == FlowAttribute.OUT || tp.FlowDirection == FlowAttribute.INOUT))
                        //{
                        //    string executionFlag = string.Empty;
                        //    string loopsegment = string.Empty;
                        //    bool executionFlagHasBeenSet = false;

                        //    #region non - record set parameter
                        //    if (string.IsNullOrEmpty(p.RecordSetName))
                        //    {
                        //        #region single instance
                        //        if (p.Seg.Inst.Equals(SINGLE_INSTANCE))
                        //        {
                        //            executionFlag = ExecutionFlag.CURRENT;
                        //        }
                        //        #endregion single instance

                        //        #region multi instance
                        //        else
                        //        {
                        //            if (ps.ProcessingType == ProcessingType.ALTERNATE)
                        //                loopsegment = ps.LoopCausingSegment;
                        //            else
                        //                loopsegment = mt.LoopSegment.Name;

                        //            if (string.Compare(p.Seg.Name, loopsegment, true) == 0)
                        //            {
                        //                executionFlag = ExecutionFlag.CURRENT;
                        //                executionFlagHasBeenSet = true;
                        //            }
                        //            else
                        //            {
                        //                #region segment has no key di
                        //                if (_service.Segments.Where(seg => string.Compare(seg.Name, loopsegment, true) == 0).First().DataItems.All(di => di.PartOfKey == false))
                        //                {
                        //                    executionFlag = ExecutionFlag.NEW;
                        //                    executionFlagHasBeenSet = true;
                        //                }
                        //                else
                        //                {
                        //                    if (bAllKeyParamAreIN)
                        //                    {
                        //                        executionFlag = ExecutionFlag.CURRENT;
                        //                        executionFlagHasBeenSet = true;
                        //                    }
                        //                    else
                        //                    {
                        //                        if (p.Seg.FlowDirection == FlowAttribute.INOUT)
                        //                        {
                        //                            executionFlag = ExecutionFlag.LOCATE;
                        //                            executionFlagHasBeenSet = true;
                        //                        }
                        //                        else if (p.Seg.FlowDirection == FlowAttribute.OUT || p.Seg.FlowDirection == FlowAttribute.SCRATCH)
                        //                        {
                        //                            if (ps.SeqNO == "1" && mt.SeqNO == "1")
                        //                            {
                        //                                executionFlag = ExecutionFlag.NEW;
                        //                                executionFlagHasBeenSet = true;
                        //                            }
                        //                            else
                        //                            {
                        //                                executionFlag = ExecutionFlag.LOCATE;
                        //                                executionFlagHasBeenSet = true;
                        //                            }
                        //                        }
                        //                        else
                        //                        {
                        //                            executionFlag = ExecutionFlag.LOCATE;
                        //                        }
                        //                    }
                        //                }
                        //                #endregion segment has no key di
                        //            }
                        //        }
                        //        #endregion multi instance
                        //    }
                        //    #endregion non - record set parameter

                        //    #region record set parameter
                        //    else
                        //    {
                        //        if (p.Seg.Inst == SINGLE_INSTANCE)
                        //        {
                        //            executionFlag = ExecutionFlag.CURRENT;
                        //        }
                        //        else
                        //        {
                        //            if (_service.Segments.Where(seg => string.Compare(seg.Name, p.Seg.Name, true) == 0).First().DataItems.All(di => di.PartOfKey == false))
                        //            {
                        //                executionFlag = ExecutionFlag.NEW;
                        //                executionFlagHasBeenSet = true;
                        //            }
                        //            else
                        //            {
                        //                if (bAllKeyParamAreIN == false)
                        //                {
                        //                    if (p.Seg.FlowDirection == FlowAttribute.INOUT)
                        //                    {
                        //                        executionFlag = ExecutionFlag.LOCATE;
                        //                        executionFlagHasBeenSet = true;
                        //                    }
                        //                    else if (p.Seg.FlowDirection == FlowAttribute.OUT || p.Seg.FlowDirection == FlowAttribute.SCRATCH)
                        //                    {
                        //                        if (ps.SeqNO.Equals("1") && mt.SeqNO.Equals("1"))
                        //                        {
                        //                            executionFlag = ExecutionFlag.NEW;
                        //                            executionFlagHasBeenSet = true;
                        //                        }
                        //                        else
                        //                        {
                        //                            executionFlag = ExecutionFlag.LOCATE;
                        //                            executionFlagHasBeenSet = true;
                        //                        }
                        //                    }
                        //                    else
                        //                    {
                        //                        executionFlag = ExecutionFlag.LOCATE;
                        //                    }
                        //                }
                        //            }
                        //        }
                        //    }
                        //    #endregion
                        //    if (executionFlagHasBeenSet)
                        //    {
                        //        mt.ExecutionFlg_r = executionFlag;
                        //        break;
                        //    }
                        //}
                        //#endregion


                        #region for IS mapping
                        if (mt.ISMappings.Rows.Count > 0)
                        {
                            DataRow mapping = mt.ISMappings.AsEnumerable().First();
                            IntegrationService IS = new IntegrationService
                            {
                                ISComponent = Convert.ToString(mapping["icomponentname"]),
                                ISServiceName = Convert.ToString(mapping["integservicename"]),
                                ISServiceType = Convert.ToString(mapping["is_servicetype"]),

                                //0-Batch,1-Regular
                                ISProcessingType = Convert.ToString(mapping["iser_pr_type"]),
                                ISIntegService = Convert.ToString(mapping["is_isintegser"]).Equals("1"),

                                CallerComponent = _ecrOptions.Component,
                                CallerService = _service.Name
                            };
                            foreach (DataRow drISMappingInfo in mt.ISMappings.Rows)
                            {
                                string sCallerSegName = Convert.ToString(drISMappingInfo["callersegname"]).ToLower();
                                string sCallerDIName = Convert.ToString(drISMappingInfo["callerdiname"]).ToLower();
                                string sISSegName = Convert.ToString(drISMappingInfo["issegname"]).ToLower();
                                string sISSegInst = Convert.ToString(drISMappingInfo["issegmentinst"]);
                                string sISDIName = Convert.ToString(drISMappingInfo["isdiname"]).ToLower();
                                string sISDIflowAtt = Convert.ToString(drISMappingInfo["flowattribute"]);
                                bool bISDIPartOfKey = Convert.ToString(drISMappingInfo["ispartofkey"]).Equals("1");
                                string sISSeqNo = Convert.ToString(drISMappingInfo["seqno"]);
                                string sDefaultValue = Convert.ToString(drISMappingInfo["defaultvalue"]);

                                Segment CallerSeg;
                                DataItem CallerDI;

                                if (sCallerSegName.ToLower().Equals(CONTEXT_SEGMENT))
                                {
                                    CallerSeg = new Segment { Name = CONTEXT_SEGMENT, Inst = SINGLE_INSTANCE };
                                    CallerDI = new DataItem { Name = sCallerDIName };
                                }
                                else
                                {
                                    try
                                    {
                                        CallerSeg = _service.Segments.Where(s => s.Name.ToLower().Equals(sCallerSegName)).First();
                                    }
                                    catch
                                    {
                                        throw new Exception(string.Format("Caller service:{0} segment:{1} combination unavailable in des_service but available under integ_serv_map", _service.Name, sCallerSegName));
                                    }
                                    try
                                    {
                                        CallerDI = CallerSeg.DataItems.Where(d => d.Name.ToLower().Equals(sCallerDIName)).First();
                                    }
                                    catch
                                    {
                                        throw new Exception(string.Format("Caller service:{0} segment:{1} dataitem:{2} combination unavailable in des_service but available under integ_serv_map", _service.Name, sCallerSegName, sCallerDIName));
                                    }

                                }

                                Segment ISSeg = new Segment
                                {
                                    Name = sISSegName,
                                    Inst = sISSegInst,
                                    Sequence = sISSeqNo,
                                };

                                DataItem ISDI = new DataItem
                                {
                                    Name = sISDIName,
                                    FlowDirection = sISDIflowAtt,
                                    PartOfKey = bISDIPartOfKey,
                                    DefaultValue = sDefaultValue
                                };

                                ISMapping _mapping = new ISMapping
                                {
                                    CallerSection = Convert.ToString(drISMappingInfo["sectionname"]),
                                    CallerSegment = CallerSeg, // new Segment { Name = Convert.ToString(drISMappingInfo["CallerSegName"]), Inst = String.Empty, FlowAttribute = String.Empty },
                                    CallerDataItem = CallerDI, // new DataItem { Name = Convert.ToString(drISMappingInfo["CallerDIName"]), FlowDirection = String.Empty },

                                    ISSegment = ISSeg,
                                    ISDataItem = ISDI
                                };

                                IS.Mappings.Add(_mapping);
                            }
                            mt.IS = IS;
                        }
                        #endregion

                        ps.Methods.Add(mt);
                    }
                    #endregion

                    var loopsegments = from m in ps.Methods
                                       where m.LoopSegment != null
                                       select m.LoopSegment;
                    if (loopsegments.Any())
                        ps.LoopCausingSegment = loopsegments.First().Name;

                    _service.ProcessSections.Add(ps);
                }
                this.SetServiceLevelOptions();
                #endregion
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("FillServiceObject()->{0}", ex.InnerException != null ? ex.InnerException.Message : ex.Message));
            }
        }


        /// <summary>
        /// Forms constructor method
        /// </summary>
        /// <returns></returns>
        private CodeConstructor Constructor()
        {
            CodeConstructor constructor;

            constructor = new CodeConstructor
            {
                Attributes = MemberAttributes.Public,
                Name = string.Format("C{0}", _service.Name)
            };

            try
            {
                //logger.WriteLogToFile(String.Empty, String.Format("Generating member function \"Constructor() for the service {0}\"", _service.Name));

                CodeMethodInvokeExpression MethodInvokation;
                //MethodInvokation = MethodInvocationExp(BaseReferenceExp(), "iEDKESEngineInit").AddParameters(new CodeExpression[] { PrimitiveExpression(_service.Name) });
                MethodInvokation = MethodInvocationExp(BaseReferenceExp(), "iEDKESEngineInit").AddParameters(new CodeExpression[] { PrimitiveExpression(_service.Name), PrimitiveExpression(_ecrOptions.Component) });
                constructor.Statements.Add(MethodInvokation);

            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("Constructor->{0}", !Object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
            return constructor;
        }

        private CodeMemberMethod ProcessUniversalPersonalization(ProcessSection ps)
        {
            CodeMemberMethod ProcessUniversalPersonalization;
            String sContext = "ProcessUniversalPersonalization";
            string universalPersonalizationCodeContent = string.Empty;
            string upTargetFullFilePath = string.Empty;

            try
            {
                ProcessUniversalPersonalization = new CodeMemberMethod
                {
                    Attributes = MemberAttributes.Private,
                    Name = sContext
                };
                CodeParameterDeclarationExpression pSessionToken = ParameterDeclarationExp(new CodeTypeReference(typeof(string)), "sessionToken");
                ProcessUniversalPersonalization.Parameters.Add(pSessionToken);

                CodeConditionStatement checkForUniversalPersonalization = IfCondition();
                checkForUniversalPersonalization.Condition = BinaryOpertorExpression(VariableReferenceExp("universalPersonalization"), CodeBinaryOperatorType.IdentityEquality, PrimitiveExpression(false));
                checkForUniversalPersonalization.TrueStatements.Add(MethodInvocationExp(BaseReferenceExp(), "WriteProfiler").AddParameter(PrimitiveExpression("Universal Personalization is disabled")));
                checkForUniversalPersonalization.TrueStatements.Add(ReturnExpression());
                ProcessUniversalPersonalization.Statements.Add(checkForUniversalPersonalization);

                ProcessUniversalPersonalization.Statements.Add(MethodInvocationExp(BaseReferenceExp(), "WriteProfiler").AddParameter(PrimitiveExpression("Universal Personalization is disabled")));
                foreach (var mt in ps.Methods)
                {
                    ProcessUniversalPersonalization.Statements.Add(DeclareVariableAndAssign($"C{_service.Name}_ml_br", "personalization", true, ObjectCreateExpression($"C{_service.Name}_ml_br")));
                    ProcessUniversalPersonalization.Statements.Add(DeclareVariableAndAssign("Dictionary<string,object>", "personalizationInput", true, ObjectCreateExpression("Dictionary<string,object>")));
                    IEnumerable<Segment> segments = mt.Parameters.Where(p => string.Compare(p.Seg.Name, CONTEXT_SEGMENT, true) != 0 && string.Compare(p.Seg.Name, ERR_SEGMENT_NAME, true) != 0).Select(p => p.Seg);
                    IEnumerable<Segment> outSegments = segments.Where(s => (s.FlowDirection == FlowAttribute.OUT || s.FlowDirection == FlowAttribute.INOUT));
                    IEnumerable<Segment> inSegments = segments.Where(s => (s.FlowDirection == FlowAttribute.IN || s.FlowDirection == FlowAttribute.INOUT));
                    if (inSegments.Any())
                    {
                        Segment seg = inSegments.First();

                        string variableNameForSeg = string.Empty;
                        if (seg.Inst == MULTI_INSTANCE)
                            variableNameForSeg = $"ht{seg.Name}";
                        else
                            variableNameForSeg = $"nvc{seg.Name}";
                        ProcessUniversalPersonalization.Statements.Add(MethodInvocationExp(VariableReferenceExp("personalizationInput"), "Add").AddParameters(new CodeExpression[] { PrimitiveExpression("inputsegment"), VariableReferenceExp(variableNameForSeg) }));
                    }
                    if (outSegments.Any())
                    {
                        Segment seg = outSegments.First();

                        string variableNameForSeg = string.Empty;
                        if (seg.Inst == MULTI_INSTANCE)
                            variableNameForSeg = $"ht{seg.Name}";
                        else
                            variableNameForSeg = $"nvc{seg.Name}";
                        ProcessUniversalPersonalization.Statements.Add(MethodInvocationExp(VariableReferenceExp("personalizationInput"), "Add").AddParameters(new CodeExpression[] { PrimitiveExpression("outputsegment"), VariableReferenceExp(variableNameForSeg) }));
                    }
                    ProcessUniversalPersonalization.Statements.Add(MethodInvocationExp(VariableReferenceExp("personalizationInput"), "Add").AddParameters(new CodeExpression[] { PrimitiveExpression("contextparameter"), VariableReferenceExp("nvcFW_CONTEXT") }));
                    ProcessUniversalPersonalization.Statements.Add(MethodInvocationExp(VariableReferenceExp("personalizationInput"), "Add").AddParameters(new CodeExpression[] { PrimitiveExpression("sessiontoken"), VariableReferenceExp("sessionToken") }));
                    if (outSegments.Any())
                        ProcessUniversalPersonalization.Statements.Add(AssignVariable(VariableReferenceExp($"nvc{outSegments.First().Name}"), MethodInvocationExp(VariableReferenceExp("personalization"), "ProcessUniversalPersonalization").AddParameter(VariableReferenceExp("personalizationInput"))));
                    else
                        ProcessUniversalPersonalization.Statements.Add(MethodInvocationExp(VariableReferenceExp("personalization"), "ProcessUniversalPersonalization").AddParameter(VariableReferenceExp("personalizationInput")));

                }
                //todo
                ProcessUniversalPersonalization.Statements.Add(MethodInvocationExp(BaseReferenceExp(), "WriteProfiler").AddParameter(PrimitiveExpression("Completed - ProcessUniversalPersonalization")));

                UPBRTemplate uPBRTemplate = new UPBRTemplate();
                uPBRTemplate.ComponentName = _ecrOptions.Component;
                uPBRTemplate.ServiceName = _service.Name;
                uPBRTemplate.ps = ps;
                universalPersonalizationCodeContent = uPBRTemplate.TransformText();
                upTargetFullFilePath = Path.Combine(_ecrOptions.GenerationPath, _ecrOptions.Platform, _ecrOptions.Customer, _ecrOptions.Project, _ecrOptions.Ecrno, "Updated", _ecrOptions.Component, "Source", "Service", "UP", _service.Name);
                if (!Directory.Exists(upTargetFullFilePath))
                    Directory.CreateDirectory(upTargetFullFilePath);
                upTargetFullFilePath = Path.Combine(upTargetFullFilePath, $"C{_service.Name}_ml_br.cs");
                File.WriteAllText(upTargetFullFilePath, universalPersonalizationCodeContent);

            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("ProcessUniversalPersonalization->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
            return ProcessUniversalPersonalization;
        }

        /// <summary>   
        /// Forms GetBOD method
        /// </summary>
        /// <returns></returns>
        private CodeMemberMethod GetBOD()
        {
            try
            {
                //logger.WriteLogToFile(String.Empty, String.Format("Generating member function \"GetBOD() for the service {0}\"", _service.Name));
                CodeMemberMethod GetBOD;
                String sContext = "GetBOD";

                //Initializing Method object
                GetBOD = new CodeMemberMethod
                {
                    Attributes = MemberAttributes.Private,
                    Name = sContext,
                    ReturnType = new CodeTypeReference(typeof(string))
                };
                GetBOD.Parameters.Add(ParameterDeclarationExp(new CodeTypeReference(typeof(bool)), "allSegments"));

                //Local Variable Declartion
                GetBOD.AddStatement(DeclareVariableAndAssign(typeof(string), "RetVal", true, GetProperty(typeof(String), "Empty")));
                //tech-xxxx line added
                GetBOD.AddStatement(DeclareVariableAndAssign(typeof(StreamReader), "read", true, SnippetExpression("null")));

                CodeTryCatchFinallyStatement tryBlock = AddTryBlock(GetBOD);
                WriteProfiler(sContext, tryBlock, null, null, "MS");

                tryBlock.AddStatement(AssignVariable(FieldReferenceExp(ThisReferenceExp(), "mStream"), ObjectCreateExpression(typeof(MemoryStream))));
                tryBlock.AddStatement(AssignVariable(FieldReferenceExp(ThisReferenceExp(), "writer"), ObjectCreateExpression(typeof(System.Xml.XmlTextWriter), new CodeExpression[] { VariableReferenceExp("mStream"), SnippetExpression("null") })));
                tryBlock.AddStatement(MethodInvocationExp(VariableReferenceExp("writer"), "WriteStartElement").AddParameters(new Object[] { PrimitiveExpression("VW-TD") }));
                tryBlock.AddStatement(MethodInvocationExp(BaseReferenceExp(), "BuildContextSegments"));
                CodeConditionStatement ifCondition = new CodeConditionStatement
                {
                    Condition = ArgumentReferenceExp("allSegments")
                };

                ifCondition.TrueStatements.Add(MethodInvocationExp(ThisReferenceExp(), "BuildOutSegments"));
                tryBlock.AddStatement(ifCondition);
                tryBlock.AddStatement(MethodInvocationExp(BaseReferenceExp(), "BuildErrorSegments"));
                tryBlock.AddStatement(MethodInvocationExp(FieldReferenceExp(ThisReferenceExp(), "writer"), "WriteEndElement"));
                tryBlock.AddStatement(MethodInvocationExp(FieldReferenceExp(ThisReferenceExp(), "writer"), "Flush"));
                tryBlock.AddStatement(SetProperty("mStream", "Position", PrimitiveExpression(0)));
                tryBlock.AddStatement(AssignVariable("read", ObjectCreateExpression(typeof(StreamReader), new CodeExpression[] { FieldReferenceExp(ThisReferenceExp(), "mStream") })));
                //tech-xxxx code commented
                //tryBlock.AddStatement(DeclareVariableAndAssign(typeof(StreamReader), "read", true, ObjectCreateExpression(typeof(StreamReader), new CodeExpression[] { FieldReferenceExp(ThisReferenceExp(), "mStream") })));
                tryBlock.AddStatement(AssignVariable(VariableReferenceExp("RetVal"), MethodInvocationExp(VariableReferenceExp("read"), "ReadToEnd")));

                //tryBlock.AddStatement(MethodInvocationExp(VariableReferenceExp("read"), "Close"));
                //tryBlock.AddStatement(MethodInvocationExp(VariableReferenceExp("mStream"), "Close"));
                //tryBlock.AddStatement(MethodInvocationExp(VariableReferenceExp("writer"), "Close"));
                //tryBlock.AddStatement(AssignVariable(FieldReferenceExp(ThisReferenceExp(), "mStream"), SnippetExpression("null")));

                tryBlock.AddStatement(ReturnExpression(VariableReferenceExp("RetVal")));

                // Catch Block
                CodeCatchClause catchCRVWException = tryBlock.AddCatch("CRVWException", "rvwe");
                ThrowException(catchCRVWException, "rvwe");

                CodeCatchClause catchOtherException = tryBlock.AddCatch("Exception", "e");
                catchOtherException.AddStatement(MethodInvocationExp(BaseReferenceExp(), "WriteProfiler").AddParameters(new CodeExpression[] { MethodInvocationExp(TypeReferenceExp(typeof(string)), "Format").AddParameters(new CodeExpression[] { PrimitiveExpression("General exception in GetBOD - {0}"), GetProperty("e", "Message") }) }));
                catchOtherException.AddStatement(ThrowNewException(ObjectCreateExpression(typeof(Exception), new CodeExpression[] { MethodInvocationExp(TypeReferenceExp(typeof(string)), "Format").AddParameters(new CodeExpression[] { PrimitiveExpression("General exception in GetBOD - {0}"), GetProperty("e", "Message") }) })));

                //TECH-XXXX finally block addition **starts**
                CodeConditionStatement checkReader = IfCondition();
                checkReader.Condition = BinaryOpertorExpression(VariableReferenceExp("read"), CodeBinaryOperatorType.IdentityInequality, SnippetExpression("null"));
                checkReader.AddStatement(MethodInvocationExp(VariableReferenceExp("read"), "Close"));
                tryBlock.FinallyStatements.Add(checkReader);

                CodeConditionStatement checkWriter = IfCondition();
                checkWriter.Condition = BinaryOpertorExpression(VariableReferenceExp("writer"), CodeBinaryOperatorType.IdentityInequality, SnippetExpression("null"));
                checkWriter.AddStatement(MethodInvocationExp(VariableReferenceExp("writer"), "Close"));
                tryBlock.FinallyStatements.Add(checkWriter);

                CodeConditionStatement checkMemoryStream = IfCondition();
                checkMemoryStream.Condition = BinaryOpertorExpression(VariableReferenceExp("mStream"), CodeBinaryOperatorType.IdentityInequality, SnippetExpression("null"));
                checkMemoryStream.AddStatement(MethodInvocationExp(VariableReferenceExp("mStream"), "Close"));
                checkMemoryStream.AddStatement(AssignVariable(VariableReferenceExp("mStream"), SnippetExpression("null")));
                tryBlock.FinallyStatements.Add(checkMemoryStream);
                //TECH-XXXX finally block addition **ends**

                return GetBOD;
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("GetBOD->{0}", !Object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }

        /// <summary>
        /// Generates 'BuildOutSegments' method
        /// </summary>
        /// <returns></returns>
        private CodeMemberMethod BuildOutSegments()
        {
            try
            {
                //logger.WriteLogToFile(String.Empty, String.Format("Generating member function \"BuildOutSegments() for the service {0}\"", _service.Name));
                String sContext = "BuildOutSegments";

                CodeMemberMethod BuildOutSegments;
                BuildOutSegments = new CodeMemberMethod
                {
                    Attributes = MemberAttributes.Private,
                    Name = sContext
                };
                CodeTryCatchFinallyStatement TryBlock = BuildOutSegments.AddTry();
                TryBlock.TryStatements.Add(DeclareVariableAndAssign(typeof(NameValueCollection), "nvcTmp", true, SnippetExpression("null")));
                TryBlock.TryStatements.Add(DeclareVariableAndAssign(typeof(bool), "iEDKESSegExists", false));
                WriteProfiler(sContext, TryBlock, null, null, "MS");



                // For Each Segment with Flow Direction IO and OUT
                foreach (Segment segment in _service.Segments.Where(s => string.Compare(s.Name, CONTEXT_SEGMENT, true) != 0
                                                                            && string.Compare(s.Name, ERR_SEGMENT_NAME, true) != 0
                                                                            && (s.FlowDirection == IO_SEGMENT || s.FlowDirection == OUT_SEGMENT)).OrderBy(s => Convert.ToInt16(s.Sequence))
                                                                            .Distinct())
                {
                    Boolean bMultiInstance = false;
                    String SegmentInstance = segment.Inst;
                    CodeConditionStatement ParentNode;
                    CodeIterationStatement IterateMultiSeg = null;

                    if (SegmentInstance == MULTI_INSTANCE)
                        bMultiInstance = true;

                    CodeConditionStatement ifSegmentIsNotNull = IfCondition();
                    if (segment.Inst.Equals(SINGLE_INSTANCE))
                        ifSegmentIsNotNull.Condition = BinaryOpertorExpression(FieldReferenceExp(ThisReferenceExp(), String.Format("nvc{0}", segment.Name.ToUpperInvariant())), CodeBinaryOperatorType.IdentityInequality, SnippetExpression("null"));
                    else
                        ifSegmentIsNotNull.Condition = BinaryOpertorExpression(FieldReferenceExp(ThisReferenceExp(), String.Format("ht{0}", segment.Name.ToUpperInvariant())), CodeBinaryOperatorType.IdentityInequality, SnippetExpression("null"));

                    ifSegmentIsNotNull.TrueStatements.Add(AssignVariable(VariableReferenceExp("iEDKESSegExists"), PrimitiveExpression(false)));
                    TryBlock.AddStatement(ifSegmentIsNotNull);

                    CodeConditionStatement ifServiceIsIEDK = IfCondition();
                    ifServiceIsIEDK.Condition = FieldReferenceExp(BaseReferenceExp(), "iEDKServiceES");
                    ifServiceIsIEDK.TrueStatements.Add(MethodInvocationExp(BaseReferenceExp(), "IsOutSegmentExists").AddParameters(new CodeExpression[] { PrimitiveExpression(segment.Name.ToLowerInvariant()), SnippetExpression("out iEDKESSegExists") }));
                    ifSegmentIsNotNull.TrueStatements.Add(ifServiceIsIEDK);

                    ifSegmentIsNotNull.TrueStatements.Add(MethodInvocationExp(FieldReferenceExp(ThisReferenceExp(), "writer"), "WriteStartElement").AddParameters(new CodeExpression[] { PrimitiveExpression(segment.Name.ToLowerInvariant()) }));
                    if (bMultiInstance)
                    {
                        ifSegmentIsNotNull.TrueStatements.Add(MethodInvocationExp(FieldReferenceExp(ThisReferenceExp(), "writer"), "WriteAttributeString").AddParameters(new CodeExpression[] { PrimitiveExpression("RecordCount"), SnippetExpression(String.Format("ht{0}.Count.ToString()", segment.Name.ToUpperInvariant())) }));
                    }
                    else
                    {
                        ifSegmentIsNotNull.TrueStatements.Add(MethodInvocationExp(FieldReferenceExp(ThisReferenceExp(), "writer"), "WriteAttributeString").AddParameters(new CodeExpression[] { PrimitiveExpression("RecordCount"), PrimitiveExpression("1") }));
                    }
                    ifSegmentIsNotNull.TrueStatements.Add(MethodInvocationExp(FieldReferenceExp(ThisReferenceExp(), "writer"), "WriteAttributeString").AddParameters(new CodeExpression[] { PrimitiveExpression("seq"), PrimitiveExpression(segment.Sequence) }));

                    if (bMultiInstance)
                    {
                        IterateMultiSeg = ForLoopExpression(
                                                            DeclareVariableAndAssign(typeof(long), "reccount", true, PrimitiveExpression(1)),
                                                            BinaryOpertorExpression(VariableReferenceExp("reccount"), CodeBinaryOperatorType.LessThanOrEqual, GetProperty(String.Format("ht{0}", segment.Name.ToUpperInvariant()), "Count")),
                                                            AssignVariable(VariableReferenceExp("reccount"), BinaryOpertorExpression(VariableReferenceExp("reccount"), CodeBinaryOperatorType.Add, PrimitiveExpression(1))
                                                            ));
                        IterateMultiSeg.Statements.Add(AssignVariable(VariableReferenceExp("nvcTmp"), SnippetExpression(String.Format("(NameValueCollection) ht{0}[reccount]", segment.Name.ToUpperInvariant()))));
                        ifSegmentIsNotNull.TrueStatements.Add(IterateMultiSeg);

                        CodeConditionStatement ifCollectionIsNotNull = new CodeConditionStatement();
                        ifCollectionIsNotNull.Condition = BinaryOpertorExpression(VariableReferenceExp("nvcTmp"), CodeBinaryOperatorType.IdentityInequality, SnippetExpression("null"));
                        IterateMultiSeg.Statements.Add(ifCollectionIsNotNull);
                        ParentNode = ifCollectionIsNotNull;
                    }
                    else
                    {
                        ParentNode = ifSegmentIsNotNull;
                    }

                    ParentNode.TrueStatements.Add(MethodInvocationExp(FieldReferenceExp(ThisReferenceExp(), "writer"), "WriteStartElement").AddParameters(new CodeExpression[] { PrimitiveExpression(String.Format("I{0}", segment.Sequence)) }));

                    foreach (DataItem dataitem in segment.UsedOutDI)
                    {
                        IEnumerable<DataRow> Properties = from r in _service.dtDIPropetyMappings.AsEnumerable()
                                                          where string.Compare(r.Field<string>("segmentname"), segment.Name, true) == 0
                                                          && string.Compare(r.Field<string>("dataitemname"), dataitem.Name, true) == 0
                                                          select r;
                        //_service.dtDIPropetyMappings.Select(String.Format("segmentname='{0}' and dataitemname = '{1}'", segment.Name, dataitem.Name));

                        if (Properties.Any())
                        {
                            CodeConditionStatement ifIncludePropertyDI = IfCondition();
                            ifIncludePropertyDI.Condition = PropertyReferenceExp(BaseReferenceExp(), "IncludePropertyDI");
                            ifIncludePropertyDI.TrueStatements.Add(MethodInvocationExp(FieldReferenceExp(ThisReferenceExp(), "writer"), "WriteAttributeString").AddParameters(new CodeExpression[] { PrimitiveExpression(dataitem.Name.ToLowerInvariant()), MethodInvocationExp(TypeReferenceExp(typeof(Convert)), "ToString").AddParameters(new CodeExpression[] { SnippetExpression(String.Format("{0}[\"{1}\"]", bMultiInstance ? "nvcTmp" : "nvc" + segment.Name.ToUpperInvariant(), dataitem.Name.ToLowerInvariant())) }) }));
                            ParentNode.TrueStatements.Add(ifIncludePropertyDI);
                        }
                        else
                        {
                            ParentNode.TrueStatements.Add(MethodInvocationExp(FieldReferenceExp(ThisReferenceExp(), "writer"), "WriteAttributeString").AddParameters(new CodeExpression[] { PrimitiveExpression(dataitem.Name.ToLowerInvariant()), MethodInvocationExp(TypeReferenceExp(typeof(Convert)), "ToString").AddParameters(new CodeExpression[] { SnippetExpression(String.Format("{0}[\"{1}\"]", bMultiInstance ? "nvcTmp" : "nvc" + segment.Name.ToUpperInvariant(), dataitem.Name.ToLowerInvariant())) }) }));
                        }
                    }

                    // For iEDK Segment Level
                    CodeConditionStatement ifIEDKSegExists = IfCondition();
                    ifIEDKSegExists.Condition = VariableReferenceExp("iEDKESSegExists");
                    ifIEDKSegExists.TrueStatements.Add(MethodInvocationExp(BaseReferenceExp(), "BuildOutSegments").AddParameters(new CodeExpression[] { PrimitiveExpression(segment.Name.ToLowerInvariant()), bMultiInstance ? VariableReferenceExp("nvcTmp") : VariableReferenceExp("nvc" + segment.Name.ToUpper()) }));
                    ParentNode.TrueStatements.Add(ifIEDKSegExists);

                    ParentNode.TrueStatements.Add(MethodInvocationExp(FieldReferenceExp(ThisReferenceExp(), "writer"), "WriteEndElement"));

                    if (bMultiInstance)
                    {
                        IterateMultiSeg.Statements.Add(AssignVariable(VariableReferenceExp("nvcTmp"), SnippetExpression("null")));
                    }
                    ifSegmentIsNotNull.TrueStatements.Add(MethodInvocationExp(FieldReferenceExp(ThisReferenceExp(), "writer"), "WriteEndElement"));
                    //}
                }

                // For iEDK Segment Level
                CodeConditionStatement ifServiceIsIEDK1 = IfCondition();
                ifServiceIsIEDK1.Condition = FieldReferenceExp(BaseReferenceExp(), "iEDKServiceES");
                ifServiceIsIEDK1.TrueStatements.Add(MethodInvocationExp(BaseReferenceExp(), "BuildOutSegments").AddParameters(new CodeExpression[] { GetProperty(typeof(String), "Empty"), SnippetExpression("null") }));
                TryBlock.AddStatement(ifServiceIsIEDK1);

                // Catch Block
                AddCatchClause(TryBlock, sContext);
                return BuildOutSegments;
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("BuildOutSegments->{0}", !Object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }

        /// <summary>
        /// Generates 'GetFieldValue' method
        /// </summary>
        /// <returns></returns>
        private CodeMemberMethod GetFieldValue()
        {
            try
            {
                //logger.WriteLogToFile(String.Empty, String.Format("Generating member function \"GetFieldValue() for the service {0}\"", _service.Name));

                String sContext = "getFieldValue";
                CodeMemberMethod getFieldValue = new CodeMemberMethod
                {
                    Name = sContext,
                    Attributes = MemberAttributes.Public | MemberAttributes.Override,
                    ReturnType = new CodeTypeReference(typeof(int))
                };
                CodeParameterDeclarationExpression pSegmentName = ParameterDeclarationExp(typeof(String), "SegName");
                CodeParameterDeclarationExpression pInstNumber = ParameterDeclarationExp(typeof(long), "InstNumber");
                CodeParameterDeclarationExpression pDataItemName = ParameterDeclarationExp(typeof(String), "DataItem");
                CodeParameterDeclarationExpression pDataItemValue = ParameterDeclarationExp(typeof(String), "DIValue");
                getFieldValue.Parameters.Add(pSegmentName);
                getFieldValue.Parameters.Add(pInstNumber);
                getFieldValue.Parameters.Add(pDataItemName);
                getFieldValue.Parameters.Add(pDataItemValue);

                // Try Block
                CodeTryCatchFinallyStatement tryBlock = getFieldValue.AddTry();

                // Local Variable Declartion
                tryBlock.AddStatement(DeclareVariableAndAssign(typeof(bool), "IsMand", true, PrimitiveExpression(false)));
                tryBlock.AddStatement(AssignVariable(ArgumentReferenceExp("SegName"), MethodInvocationExp(ArgumentReferenceExp("SegName"), "ToLower")));
                tryBlock.AddStatement(AssignVariable(ArgumentReferenceExp(pDataItemName.Name), MethodInvocationExp(MethodInvocationExp(ArgumentReferenceExp(pDataItemName.Name), "ToLower"), "Trim")));

                CodeConditionStatement ifDIValueIsNull = IfCondition();
                ifDIValueIsNull.Condition = BinaryOpertorExpression(VariableReferenceExp(pDataItemValue.Name), CodeBinaryOperatorType.IdentityEquality, SnippetExpression("null"));
                ifDIValueIsNull.TrueStatements.Add(AssignVariable(VariableReferenceExp(pDataItemValue.Name), GetProperty(TypeReferenceExp(typeof(string)), "Empty")));
                tryBlock.AddStatement(ifDIValueIsNull);

                tryBlock.TryStatements.Add(SnippetStatement(String.Format("switch ({0})", "SegName")));
                tryBlock.TryStatements.Add(SnippetStatement("{"));


                IEnumerable<Segment> segments = _service.Segments.Where(s => s.Name != CONTEXT_SEGMENT
                                                                            && s.Name != ERR_SEGMENT_NAME
                                                                            && (s.FlowDirection == IN_SEGMENT || s.FlowDirection == IO_SEGMENT)).OrderBy(s => Convert.ToInt16(s.Sequence))
                                                                            .Distinct();
                // For Each Segment of Flow Attribute IN and IO
                foreach (Segment segment in segments)
                {
                    bool bMultiInstanceSeg = segment.Inst.Equals("1");

                    // For Each Segment DI of Flow Attribute IN and IO
                    //IEnumerable<DataItem> dataitems = segment.DataItems.Where(d => d.FlowDirection == IN_DATAITEM || d.FlowDirection == IO_DATAITEM);


                    tryBlock.TryStatements.Add(SnippetStatement(String.Format("case \"{0}\":", segment.Name.ToLower())));
                    tryBlock.TryStatements.Add(SnippetStatement(String.Format("switch ({0})", pDataItemName.Name)));
                    tryBlock.TryStatements.Add(SnippetStatement("{"));

                    foreach (DataItem dataitem in segment.DataItems.Where(d => d.FlowDirection == FlowAttribute.IN || d.FlowDirection == FlowAttribute.INOUT))
                    {
                        CodeExpression expForDefaultValue = null;

                        if (!string.IsNullOrEmpty(dataitem.DefaultValue))
                            expForDefaultValue = PrimitiveExpression(dataitem.DefaultValue);
                        else
                            expForDefaultValue = GetProperty(TypeReferenceExp(typeof(string)), "Empty");

                        tryBlock.TryStatements.Add(SnippetStatement(String.Format("case \"{0}\":", dataitem.Name.ToLowerInvariant())));
                        tryBlock.TryStatements.Add(AssignVariable(VariableReferenceExp("IsMand"), PrimitiveExpression(dataitem.IsMandatory)));
                        tryBlock.TryStatements.Add(AssignVariable(VariableReferenceExp("defaultValue"), expForDefaultValue));
                        tryBlock.TryStatements.Add(SnippetExpression("break"));
                    }

                    // iEDK Segment Level
                    tryBlock.TryStatements.Add(SnippetStatement("default:"));
                    GetFieldValue_Segment_iEDKBlock(tryBlock, pSegmentName, pInstNumber, pDataItemName, pDataItemValue, bMultiInstanceSeg, segment);
                    tryBlock.TryStatements.Add(ReturnExpression(PrimitiveExpression(0)));
                    tryBlock.TryStatements.Add(SnippetExpression("break"));
                    tryBlock.TryStatements.Add(SnippetStatement("}"));
                    GetFieldValue_CheckForValidate(tryBlock, pSegmentName, pDataItemName);
                    GetFieldValue_RetValue(tryBlock, pSegmentName, pInstNumber, pDataItemName, pDataItemValue, bMultiInstanceSeg, segment); ;

                    tryBlock.TryStatements.Add(ReturnExpression(PrimitiveExpression(0)));
                }

                // iEDK Service Level
                tryBlock.TryStatements.Add(SnippetStatement("default:"));
                GetFieldValue_Service_iEDKBlock(tryBlock, pSegmentName, pInstNumber, pDataItemName, pDataItemValue, (segments.Count() > 0));
                tryBlock.TryStatements.Add(SnippetExpression("break")); //break for segment's default case
                tryBlock.TryStatements.Add(SnippetStatement("}")); // segments switch case ends here

                // Catch Block
                CodeCatchClause catchCRVWException = tryBlock.AddCatch("CRVWException", "rvwe");
                ThrowException(catchCRVWException, "rvwe");

                CodeCatchClause catchOtherException = tryBlock.AddCatch("Exception", "e");
                catchOtherException.AddStatement(MethodInvocationExp(BaseReferenceExp(), "WriteProfiler").AddParameters(new CodeExpression[] { MethodInvocationExp(TypeReferenceExp(typeof(string)), "Format").AddParameters(new CodeExpression[] { PrimitiveExpression("General exception in getFieldValue - {0}"), GetProperty("e", "Message") }) }));
                catchOtherException.AddStatement(MethodInvocationExp(BaseReferenceExp(), "Set_Error_Info").AddParameters(new CodeExpression[] { PrimitiveExpression(0),
                                                                                                                                                FieldReferenceExp(TypeReferenceExp("CUtil"),"FRAMEWORK_ERROR"),
                                                                                                                                                FieldReferenceExp(TypeReferenceExp("CUtil"),"STOP_PROCESSING"),
                                                                                                                                                MethodInvocationExp(TypeReferenceExp(typeof(string)), "Format").AddParameters(new CodeExpression[] { PrimitiveExpression("RVWException in getFieldValue - {0}"), GetProperty("e", "Message") }),
                                                                                                                                                GetProperty(TypeReferenceExp(typeof(string)),"Empty"),
                                                                                                                                                GetProperty(TypeReferenceExp(typeof(string)),"Empty"),
                                                                                                                                                PrimitiveExpression(0),
                                                                                                                                                GetProperty(TypeReferenceExp(typeof(string)),"Empty"),
                                                                                                                                                GetProperty(TypeReferenceExp(typeof(string)),"Empty")
                                                                                                                                                }));
                catchOtherException.AddStatement(ReturnExpression(PrimitiveExpression(1)));

                return getFieldValue;
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("GetFieldValue->{0}", !Object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }

        private void WriteProfiler_ValidateMandatorySegment(String MethodName, CodeTryCatchFinallyStatement tryBlock, CodeCatchClause catchBlock, CodeConditionStatement IfCondition,
            String MessageType, String SegmentName)
        {
            try
            {
                String Message = String.Empty;
                switch (MessageType)
                {
                    case "OT":
                        {
                            switch (MethodName.ToLower())
                            {
                                case "validatemandatorysegment_rps":
                                    Message = String.Format("String.Format(\"RVWException in ValidateMandatorySegment - No Values Supplied for Mandatory Segment " + SegmentName + "\")");
                                    break;
                                case "validatemandatorysegment_err":
                                    Message = String.Format("String.Format(\"RVWException in ValidateMandatorySegment No Values Supplied for Mandatory segment - ErrorDetails\")");
                                    break;
                            }
                        }

                        IfCondition.TrueStatements.Add(MethodInvocationExp(BaseReferenceExp(), "WriteProfiler").AddParameters(new Object[] { SnippetExpression(String.Format(Message)) }));
                        break;
                }
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("WriteProfiler_ValidateMandatorySegment->{0}", !Object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }

        private void Set_Error_Info_ValidateMandatorySegment(String MethodName, CodeTryCatchFinallyStatement tryBlock, CodeCatchClause catchBlock,
            CodeConditionStatement IfCondition, String SegmentName)
        {
            try
            {
                String Message = String.Empty;
                CodeMethodInvokeExpression methodInvocationExp;

                switch (MethodName.ToLower())
                {
                    case "validatemandatorysegment_rps":
                        Message = String.Format("String.Format(\"RVWException in ValidateMandatorySegment No Values Supplied for Mandatory Segment " + SegmentName + "\")");
                        break;
                    case "validatemandatorysegment_err":
                        Message = String.Format("String.Format(\"RVWException in ValidateMandatorySegment No Values Supplied for Mandatory Segment - ErrorDetails\")");
                        break;
                }

                methodInvocationExp = MethodInvocationExp(BaseReferenceExp(), "Set_Error_Info").AddParameters(new CodeExpression[]{PrimitiveExpression(0),
                                                                                                            VariableReferenceExp("FRAMEWORK_ERROR"),
                                                                                                            VariableReferenceExp("STOP_PROCESSING"),
                                                                                                            SnippetExpression(Message),
                                                                                                            GetProperty(typeof(String),"Empty"),
                                                                                                            GetProperty(typeof(String),"Empty"),
                                                                                                            PrimitiveExpression(0),
                                                                                                            GetProperty(typeof(String),"Empty"),
                                                                                                            GetProperty(typeof(String),"Empty")
                                                                                                            });

                switch (MethodName.ToLower())
                {
                    case "validatemandatorysegment_err":
                    case "validatemandatorysegment_rps":
                        IfCondition.TrueStatements.Add(methodInvocationExp);
                        break;
                }
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("Set_Error_Info_ValidateMandatorySegment->{0}", !Object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }
        private void WriteProfiler(String MethodName, CodeTryCatchFinallyStatement tryBlock, CodeCatchClause catchBlock, CodeConditionStatement IfCondition,
String MessageType)
        {
            try
            {
                String Message = String.Empty;
                switch (MessageType)
                {
                    case "FN":
                        Message = String.Format("String.Format(\"Service {0} Ended at  - \" + System.DateTime.Now.ToString())", _service.Name);
                        tryBlock.FinallyStatements.Add(MethodInvocationExp(BaseReferenceExp(), "WriteProfiler").AddParameters(new Object[] { SnippetExpression(String.Format(Message)) }));
                        break;
                    case "MS":
                        {
                            if (MethodName == "ProcessService")
                                Message = String.Format("String.Format(\"Service {0} Started at \" + System.DateTime.Now.ToString())", _service.Name);
                            else
                                Message = String.Format("String.Format(\"Executing {0} Method at \" + System.DateTime.Now.ToString())", MethodName.ToString());

                            tryBlock.TryStatements.Add(MethodInvocationExp(BaseReferenceExp(), "WriteProfiler").AddParameters(new Object[] { SnippetExpression(String.Format(Message)) }));
                        }
                        break;
                    case "EX":
                        Message = String.Format("String.Format(\"General Exception in {0} - \" + e.Message)", MethodName);
                        catchBlock.AddStatement(MethodInvocationExp(BaseReferenceExp(), "WriteProfiler").AddParameters(new Object[] { SnippetExpression(String.Format(Message)) }));
                        break;
                    case "EXEMPTY":
                        catchBlock.AddStatement(MethodInvocationExp(BaseReferenceExp(), "WriteProfiler").AddParameters(new CodeExpression[] { GetProperty("e", "Message") }));
                        break;
                    case "RvwEX":
                        Message = String.Format("String.Format(\"General Exception in {0} - \" + rvwe.Message)", MethodName);
                        catchBlock.AddStatement(MethodInvocationExp(BaseReferenceExp(), "WriteProfiler").AddParameters(new Object[] { SnippetExpression(String.Format(Message)) }));
                        break;
                    case "OT":
                        {
                            switch (MethodName.ToLower())
                            {
                                case "getfieldvalue_checkforvalidate":
                                    Message = String.Format("String.Format(\"RVWException in getFieldValue - No Value is passed for the mandatory DataItem - \" + DataItem + \" for the segment - \" + SegName)");
                                    break;
                                case "getfieldvalue_segment_iedkblock":
                                    Message = String.Format("String.Format(\"RVWException in getFieldValue - DataItem - \" + DataItem + \" for the Segment - \" + SegName + \" is not part of the service \")");
                                    break;
                                case "validatemandatorysegment_rps":
                                    Message = String.Format("String.Format(\"RVWException in ValidateMandatorySegment - No Values Supplied for Mandatory Segment \" + SegName + \")");
                                    break;
                                case "validatemandatorysegment_err":
                                    Message = String.Format("String.Format(\"RVWException in ValidateMandatorySegment No Values Supplied for Mandatory segment - ErrorDetails\")");
                                    break;
                            }

                            IfCondition.TrueStatements.Add(MethodInvocationExp(BaseReferenceExp(), "WriteProfiler").AddParameters(new Object[] { SnippetExpression(String.Format(Message)) }));
                            //IfCondition.FalseStatements.Add(MethodInvocationExp(BaseReferenceExp(), "WriteProfiler").AddParameters(new Object[] { SnippetExpression(String.Format(Message)) }));
                            break;
                        }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("WriteProfiler->{0}", !Object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }

        private void Set_Error_Info(String MethodName, CodeTryCatchFinallyStatement tryBlock, CodeCatchClause catchBlock, CodeConditionStatement IfCondition)
        {
            try
            {
                String Message = String.Empty;
                CodeMethodInvokeExpression methodInvocationExp;

                switch (MethodName.ToLower())
                {
                    case "buildoutsegments":
                        Message = String.Format("String.Format(\"General Exception in BuildOutSegments - \" + e.Message)");
                        break;
                    case "getfieldvalue_checkforvalidate":
                        Message = "String.Format(\"RVWException in getFieldValue - No Value is passed for the Mandatory DataItem - \" + DataItem + \" for the segment - \" + SegName)";
                        break;
                    case "getfieldvalue_service_iedkblock":
                        Message = "String.Format(\"RVWException in getFieldValue - No Such Segment Name \" + SegName + \" is Found in the Service\")";
                        break;
                    case "getsegmentvalue_default":
                        Message = String.Format("String.Format(\"General Exception in GetSegmentValue\")", MethodName);
                        break;
                    case "validatemandatorysegment_rps":
                        Message = String.Format("String.Format(\"RVWException in ValidateMandatorySegment No Values Supplied for Mandatory Segment \" + SegName + \")");
                        break;
                    case "validatemandatorysegment_err":
                        Message = String.Format("String.Format(\"RVWException in ValidateMandatorySegment No Values Supplied for Mandatory Segment - ErrorDetails\")");
                        break;
                    case "processservice":
                        Message = String.Format("String.Format(\"General exception in ProcessService - \" + e.Message)");
                        break;
                    case "fillunmappeddataitems":
                        Message = String.Format("String.Format(\"RVWException in FillUnMappedDataitem - \" + e.Message)");
                        break;
                    case "processsection":
                        Message = "String.Format(\"General Exception during OUT Data Binding of PS - \" + psSeqNo.ToString() + \" BR - - \" + brSeqNo.ToString())";
                        break;
                    case "ps_brexecution_is":
                        Message = "String.Format(\"General Exception at - \" + psSeqNo.ToString() + \" IS - - \" + brSeqNo.ToString())";
                        break;
                        //case "fillplaceholdervalue":
                        //    Message = "RVWException in FillPlaceHolderValue No NameValueCollection is present for the given segment name";
                        //    break;
                }

                methodInvocationExp = MethodInvocationExp(BaseReferenceExp(), "Set_Error_Info").AddParameters(new CodeExpression[]{PrimitiveExpression(0),
																											//FieldReferenceExp(TypeReferenceExp("CUtil"),"FRAMEWORK_ERROR"),
																											//FieldReferenceExp(TypeReferenceExp("CUtil"),"STOP_PROCESSING"),
                                                                                                            VariableReferenceExp("FRAMEWORK_ERROR"),
                                                                                                            VariableReferenceExp("STOP_PROCESSING"),
                                                                                                            SnippetExpression(Message),
                                                                                                            GetProperty(typeof(String),"Empty"),
                                                                                                            GetProperty(typeof(String),"Empty"),
                                                                                                            PrimitiveExpression(0),
                                                                                                            GetProperty(typeof(String),"Empty"),
                                                                                                            GetProperty(typeof(String),"Empty")
                                                                                                            });

                switch (MethodName.ToLower())
                {
                    case "validatemandatorysegment_err":
                    case "validatemandatorysegment_rps":
                    case "getfieldvalue_checkforvalidate":
                        IfCondition.TrueStatements.Add(methodInvocationExp);
                        break;
                    case "processservice":
                    case "getsegmentvalue_default":
                    case "getfieldvalue_service_iedkblock":
                        tryBlock.AddStatement(methodInvocationExp);
                        break;
                    default:
                        catchBlock.AddStatement(methodInvocationExp);
                        break;
                }
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("Set_Error_Info->{0}", !Object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }

        private void GetFieldValue_RetValue(CodeTryCatchFinallyStatement tryBlock, CodeParameterDeclarationExpression pSegmentName,
            CodeParameterDeclarationExpression pInstNumber, CodeParameterDeclarationExpression pDataItemName, CodeParameterDeclarationExpression pDataItemValue,
            Boolean bMultiInstanceSeg, Segment segment)
        {
            try
            {
                if (bMultiInstanceSeg)
                {
                    tryBlock.TryStatements.Add(AssignVariable(FieldReferenceExp(ThisReferenceExp(), "nvcTmp"), SnippetExpression(String.Format("(NameValueCollection) ht{0}[{1}]", segment.Name.ToUpperInvariant(), pInstNumber.Name))));

                    CodeConditionStatement ifnvcTmp = IfCondition();
                    ifnvcTmp.Condition = BinaryOpertorExpression(FieldReferenceExp(ThisReferenceExp(), "nvcTmp"), CodeBinaryOperatorType.IdentityEquality, SnippetExpression("null"));
                    ifnvcTmp.TrueStatements.Add(AssignVariable(VariableReferenceExp("nvcTmp"), ObjectCreateExpression("NameValueCollection")));
                    tryBlock.TryStatements.Add(ifnvcTmp);

                    tryBlock.TryStatements.Add(AssignVariable(ArrayIndexerExpression("nvcTmp", VariableReferenceExp(pDataItemName.Name)), VariableReferenceExp(pDataItemValue.Name)));
                    tryBlock.TryStatements.Add(AssignVariable(ArrayIndexerExpression(String.Format("ht{0}", segment.Name.ToUpperInvariant()), VariableReferenceExp(pInstNumber.Name)), VariableReferenceExp("nvcTmp")));
                    tryBlock.TryStatements.Add(AssignVariable(VariableReferenceExp("nvcTmp"), SnippetExpression("null")));
                }
                else
                {
                    tryBlock.TryStatements.Add(AssignVariable(String.Format("nvc{0}", segment.Name), ArgumentReferenceExp(pDataItemValue.Name), -1, true, "DataItem"));
                }
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("GetFieldValue_RetValue->{0}", !Object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }

        private void GetFieldValue_CheckForValidate(CodeTryCatchFinallyStatement tryBlock, CodeParameterDeclarationExpression pSegmentName,
            CodeParameterDeclarationExpression pDataItemName)
        {
            try
            {
                CodeConditionStatement ifCheckForValidate = IfCondition();

                ifCheckForValidate.Condition = BinaryOpertorExpression(MethodInvocationExp(BaseReferenceExp(), "checkforvalidate").AddParameters(new CodeExpression[] { SnippetExpression("ref DIValue"), SnippetExpression("IsMand"), SnippetExpression("defaultValue") }),
                                                                     CodeBinaryOperatorType.IdentityEquality,
                                                                     PrimitiveExpression(false));

                WriteProfiler("GetFieldValue_CheckForValidate", null, null, ifCheckForValidate, "OT");
                Set_Error_Info("GetFieldValue_CheckForValidate", null, null, ifCheckForValidate);
                ifCheckForValidate.TrueStatements.Add(ReturnExpression(PrimitiveExpression(1)));

                tryBlock.TryStatements.Add(ifCheckForValidate);
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("GetFieldValue_CheckForValidate->{0}", !Object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }

        }

        private void GetFieldValue_Segment_iEDKBlock(CodeTryCatchFinallyStatement tryBlock, CodeParameterDeclarationExpression pSegmentName,
            CodeParameterDeclarationExpression pInstNumber, CodeParameterDeclarationExpression pDataItemName, CodeParameterDeclarationExpression pDataItemValue,
            Boolean bMultiInstanceSeg, Segment segment)
        {
            try
            {
                CodeConditionStatement ifIedkService = IfCondition();
                ifIedkService.Condition = FieldReferenceExp(BaseReferenceExp(), "iEDKServiceES");
                ifIedkService.TrueStatements.Add(ReturnExpression(MethodInvocationExp(BaseReferenceExp(), "getFieldValue").AddParameters(new CodeExpression[] { ArgumentReferenceExp(pSegmentName.Name), ArgumentReferenceExp(pInstNumber.Name), ArgumentReferenceExp(pDataItemName.Name), ArgumentReferenceExp(pDataItemValue.Name) })));
                tryBlock.TryStatements.Add(ifIedkService);

                if (bMultiInstanceSeg)
                {
                    tryBlock.TryStatements.Add(AssignVariable(FieldReferenceExp(ThisReferenceExp(), "nvcTmp"), SnippetExpression(String.Format("(NameValueCollection) ht{0}[{1}]", segment.Name.ToUpperInvariant(), pInstNumber.Name))));

                    CodeConditionStatement ifnvcTmp = IfCondition();
                    ifnvcTmp.Condition = BinaryOpertorExpression(FieldReferenceExp(ThisReferenceExp(), "nvcTmp"), CodeBinaryOperatorType.IdentityEquality, SnippetExpression("null"));
                    ifnvcTmp.TrueStatements.Add(AssignVariable(VariableReferenceExp("nvcTmp"), ObjectCreateExpression("NameValueCollection")));
                    tryBlock.TryStatements.Add(ifnvcTmp);

                    tryBlock.TryStatements.Add(AssignVariable(ArrayIndexerExpression("nvcTmp", VariableReferenceExp(pDataItemName.Name)), VariableReferenceExp(pDataItemValue.Name)));
                    tryBlock.TryStatements.Add(AssignVariable(ArrayIndexerExpression(String.Format("ht{0}", segment.Name.ToUpperInvariant()), VariableReferenceExp(pInstNumber.Name)), VariableReferenceExp("nvcTmp")));
                    tryBlock.TryStatements.Add(AssignVariable(VariableReferenceExp("nvcTmp"), SnippetExpression("null")));
                }
                else
                {
                    ifIedkService.FalseStatements.Add(MethodInvocationExp(BaseReferenceExp(), "WriteProfiler").AddParameters(new CodeExpression[] { MethodInvocationExp(TypeReferenceExp(typeof(string)), "Format").AddParameters(new CodeExpression[] { PrimitiveExpression("RVWException in getFieldValue - DataItem - {0}  is not part of the Service"), VariableReferenceExp("DataItem") }) }));
                    ifIedkService.FalseStatements.Add(AssignVariable(String.Format("nvc{0}", segment.Name), ArgumentReferenceExp(pDataItemValue.Name), -1, true, "DataItem"));
                    //ifIedkService.FalseStatements.Add(ReturnExpression(PrimitiveExpression(0)));
                }
                //ifIedkService.FalseStatements.Add(ReturnExpression(PrimitiveExpression(0)));
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("GetFieldValue_Segment_iEDKBlock->{0}", !Object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }

        private void GetFieldValue_Service_iEDKBlock(CodeTryCatchFinallyStatement tryBlock, CodeParameterDeclarationExpression pSegmentName,
            CodeParameterDeclarationExpression pInstNumber, CodeParameterDeclarationExpression pDataItemName, CodeParameterDeclarationExpression pDataItemValue, bool bSetErrorInfo = true)
        {
            try
            {
                CodeConditionStatement ifIEDKServiceAndSegment = IfCondition();
                ifIEDKServiceAndSegment.Condition = BinaryOpertorExpression(BinaryOpertorExpression(FieldReferenceExp(BaseReferenceExp(), "iEDKServiceES"), CodeBinaryOperatorType.IdentityEquality, PrimitiveExpression(true)),
                                                                            CodeBinaryOperatorType.BooleanAnd,
                                                                            BinaryOpertorExpression(FieldReferenceExp(BaseReferenceExp(), "iEDKInSegment"), CodeBinaryOperatorType.IdentityEquality, PrimitiveExpression(true)));

                CodeConditionStatement CheckSegmentType = IfCondition();
                CheckSegmentType.Condition = BinaryOpertorExpression(MethodInvocationExp(BaseReferenceExp(), "GetSegmentType").AddParameters(new CodeExpression[] { ArgumentReferenceExp(pSegmentName.Name) }),
                                                                     CodeBinaryOperatorType.IdentityInequality,
                                                                     PrimitiveExpression(-1));
                CheckSegmentType.TrueStatements.Add(ReturnExpression(MethodInvocationExp(BaseReferenceExp(), "getFieldValue").AddParameters(new CodeExpression[] { ArgumentReferenceExp(pSegmentName.Name), ArgumentReferenceExp(pInstNumber.Name), ArgumentReferenceExp(pDataItemName.Name), ArgumentReferenceExp(pDataItemValue.Name) })));
                ifIEDKServiceAndSegment.TrueStatements.Add(CheckSegmentType);

                tryBlock.TryStatements.Add(ifIEDKServiceAndSegment);

                if (bSetErrorInfo)
                    Set_Error_Info("GetFieldValue_Service_iEDKBlock", tryBlock, null, null);

                tryBlock.TryStatements.Add(ReturnExpression(PrimitiveExpression(1)));
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("GetFieldValue_Service_iEDKBlock->{0}", !Object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }

        /// <summary>
        /// Generate 'GetSegmentType' method
        /// </summary>
        /// <returns></returns>
        private CodeMemberMethod GetSegmentType()
        {
            //initializing method with some basic values like name,attributes & return type
            try
            {
                //logger.WriteLogToFile(String.Empty, String.Format("Generating member function \"GetSegmentType() for the service {0}\"", _service.Name));

                String sContext = "GetSegmentType";
                CodeMemberMethod GetSegmentType = new CodeMemberMethod
                {
                    Name = sContext,
                    Attributes = MemberAttributes.Public | MemberAttributes.Override,
                    ReturnType = new CodeTypeReference(typeof(int))
                };

                CodeParameterDeclarationExpression pSegmentName = ParameterDeclarationExp(typeof(String), "szSegName");
                GetSegmentType.Parameters.Add(pSegmentName);

                CodeVariableDeclarationStatement vType = DeclareVariableAndAssign(typeof(int), "type", true, PrimitiveExpression(-1));
                GetSegmentType.Statements.Add(vType);
                GetSegmentType.Statements.Add(AssignVariable(ArgumentReferenceExp(pSegmentName.Name),
                                                                MethodInvocationExp(MethodInvocationExp(ArgumentReferenceExp(pSegmentName.Name), "ToLower"), "Trim")
                                                                ));

                IEnumerable<Segment> segments = _service.Segments;

                GetSegmentType.Statements.Add(SnippetStatement(String.Format("switch ({0})", pSegmentName.Name)));
                GetSegmentType.Statements.Add(SnippetStatement("{")); //switch case scope starts here

                foreach (Segment segment in segments)
                {
                    bool IsMultiInstance = segment.Inst.Equals("1");

                    GetSegmentType.Statements.Add(SnippetStatement(String.Format("case \"{0}\":", segment.Name.ToLowerInvariant())));
                    GetSegmentType.Statements.Add(AssignVariable(VariableReferenceExp(vType.Name), IsMultiInstance.Equals(true) ? PrimitiveExpression(1) : PrimitiveExpression(0)));
                    GetSegmentType.Statements.Add(SnippetExpression("break")); //break statement for each segment
                }

                GetSegmentType.Statements.Add(SnippetStatement("default:"));
                GetSegmentType.Statements.Add(ReturnExpression(MethodInvocationExp(BaseReferenceExp(), GetSegmentType.Name).AddParameters(new CodeExpression[] { ArgumentReferenceExp(pSegmentName.Name) })));
                GetSegmentType.Statements.Add(SnippetExpression("break"));//break statement for default case

                GetSegmentType.Statements.Add(SnippetStatement("}"));//switch case scop ends here

                GetSegmentType.Statements.Add(ReturnExpression(VariableReferenceExp(vType.Name)));
                return GetSegmentType;
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("GetSegmentType->{0}", !Object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }

        /// <summary>
        /// Generate 'GetSegmentRecCount' method
        /// </summary>
        /// <returns></returns>
        private CodeMemberMethod GetSegmentRecCount()
        {
            try
            {
                //logger.WriteLogToFile(String.Empty, String.Format("Generating member function \"GetSegmentRecCount() for the service {0}\"", _service.Name));

                String sContext = "GetSegmentRecCount";
                //initializing method with some basic values like name,attributes & return type
                CodeMemberMethod GetSegmentRecCount = new CodeMemberMethod
                {
                    Name = sContext,
                    Attributes = MemberAttributes.Public | MemberAttributes.Override,
                    ReturnType = new CodeTypeReference(typeof(long))
                };

                CodeParameterDeclarationExpression pSegmentName = ParameterDeclarationExp(typeof(String), "szSegName");
                GetSegmentRecCount.Parameters.Add(pSegmentName);

                GetSegmentRecCount.Statements.Add(AssignVariable(ArgumentReferenceExp(pSegmentName.Name),
                                                MethodInvocationExp(MethodInvocationExp(ArgumentReferenceExp(pSegmentName.Name), "ToLower"), "Trim")
                                                ));


                //filtering multi Instance segment alone
                IEnumerable<Segment> segments = _service.Segments.Where(s => s.Inst == "1");
                GetSegmentRecCount.Statements.Add(SnippetStatement(String.Format("switch ({0})", pSegmentName.Name)));
                GetSegmentRecCount.Statements.Add(SnippetStatement("{")); //switch case scope starts here

                foreach (Segment segment in segments)
                {

                    GetSegmentRecCount.Statements.Add(SnippetStatement(String.Format("case \"{0}\":", segment.Name.ToLower())));
                    GetSegmentRecCount.Statements.Add(ReturnExpression(GetProperty(String.Format("ht{0}", segment.Name.ToUpperInvariant()), "Count")));
                    GetSegmentRecCount.Statements.Add(SnippetExpression("break")); //break for segment case statement
                }

                GetSegmentRecCount.Statements.Add(SnippetStatement("default:"));
                GetSegmentRecCount.Statements.Add(ReturnExpression(MethodInvocationExp(BaseReferenceExp(), GetSegmentRecCount.Name).AddParameters(new CodeExpression[] { ArgumentReferenceExp(pSegmentName.Name) })));
                GetSegmentRecCount.Statements.Add(SnippetExpression("break"));
                GetSegmentRecCount.Statements.Add(SnippetStatement("}"));//switch case scop ends here

                return GetSegmentRecCount;
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("GetSegmentRecCount->{0}", !Object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }

        /// <summary>
        /// Generates 'GetMultiSegment' method
        /// </summary>
        /// <returns></returns>
        private CodeMemberMethod GetMultiSegment()
        {
            try
            {
                //logger.WriteLogToFile(String.Empty, String.Format("Generating member function \"GetMultiSegment() for the service {0}\"", _service.Name));

                String sContext = "GetMultiSegment";
                CodeMemberMethod GetMultiSegment = new CodeMemberMethod
                {
                    Name = sContext,
                    Attributes = MemberAttributes.Public | MemberAttributes.Override,
                    ReturnType = new CodeTypeReference(typeof(Hashtable))
                };

                CodeParameterDeclarationExpression pSegName = ParameterDeclarationExp(typeof(String), "szSegName");
                GetMultiSegment.Parameters.Add(pSegName);
                GetMultiSegment.Statements.Add(AssignVariable(ArgumentReferenceExp(pSegName.Name),
                                                    MethodInvocationExp(MethodInvocationExp(ArgumentReferenceExp(pSegName.Name), "ToLower"), "Trim")
                                                    ));

                //filtering multi Instance segment alone
                IEnumerable<Segment> segments = _service.Segments.Where(s => s.Inst == "1");

                GetMultiSegment.Statements.Add(SnippetStatement(String.Format("switch ({0})", pSegName.Name)));
                GetMultiSegment.Statements.Add(SnippetStatement("{")); //switch case scope starts here
                foreach (Segment segment in segments)
                {
                    GetMultiSegment.Statements.Add(SnippetStatement(String.Format("case \"{0}\":", segment.Name.ToLowerInvariant())));
                    GetMultiSegment.Statements.Add(ReturnExpression(FieldReferenceExp(ThisReferenceExp(), String.Format("ht{0}", segment.Name.ToUpperInvariant()))));
                }

                GetMultiSegment.Statements.Add(SnippetStatement("default:"));
                GetMultiSegment.Statements.Add(ReturnExpression(MethodInvocationExp(BaseReferenceExp(), GetMultiSegment.Name).AddParameters(new CodeExpression[] { ArgumentReferenceExp(pSegName.Name) })));
                GetMultiSegment.Statements.Add(SnippetStatement("}"));//switch case scop ends here

                return GetMultiSegment;
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("GetMultiSegment->{0}", !Object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }

        /// <summary>
        /// Generates 'GetSingleSegment' method
        /// </summary>
        /// <returns></returns>
        private CodeMemberMethod GetSingleSegment()
        {
            try
            {
                //logger.WriteLogToFile(String.Empty, String.Format("Generating member function \"GetSingleSegment() for the service {0}\"", _service.Name));

                String sContext = "GetSingleSegment";
                CodeMemberMethod GetSingleSegment = new CodeMemberMethod
                {
                    Name = sContext,
                    Attributes = MemberAttributes.Public | MemberAttributes.Override,
                    ReturnType = new CodeTypeReference(typeof(NameValueCollection))
                };

                CodeParameterDeclarationExpression pSegName = ParameterDeclarationExp(typeof(String), "szSegName");
                GetSingleSegment.Parameters.Add(pSegName);
                GetSingleSegment.Statements.Add(AssignVariable(ArgumentReferenceExp(pSegName.Name),
                                                    MethodInvocationExp(MethodInvocationExp(ArgumentReferenceExp(pSegName.Name), "ToLower"), "Trim")
                                                    ));

                //filtering single Instance segment alone
                IEnumerable<Segment> segments = _service.Segments.Where(s => s.Inst == "0");
                GetSingleSegment.Statements.Add(SnippetStatement(String.Format("switch ({0})", pSegName.Name)));
                GetSingleSegment.Statements.Add(SnippetStatement("{")); //switch case scope starts here

                foreach (Segment segment in segments)
                {
                    GetSingleSegment.Statements.Add(SnippetStatement(String.Format("case \"{0}\":", segment.Name.ToLowerInvariant())));
                    GetSingleSegment.Statements.Add(ReturnExpression(FieldReferenceExp(ThisReferenceExp(), String.Format("nvc{0}", segment.Name.ToUpperInvariant()))));
                }

                GetSingleSegment.Statements.Add(SnippetStatement("default:"));
                GetSingleSegment.Statements.Add(ReturnExpression(MethodInvocationExp(BaseReferenceExp(), GetSingleSegment.Name).AddParameters(new CodeExpression[] { ArgumentReferenceExp(pSegName.Name) })));
                GetSingleSegment.Statements.Add(SnippetStatement("}"));//switch case scope ends here

                return GetSingleSegment;
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("GetSingleSegment->{0}", !Object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }

        /// <summary>
        /// Generates 'GetSegmentValue' method
        /// </summary>
        /// <returns></returns>
        private CodeMemberMethod GetSegmentValue()
        {
            try
            {
                //logger.WriteLogToFile(String.Empty, String.Format("Generating member function \"GetSegmentValue() for the service {0}\"", _service.Name));

                String sContext = "GetSegmentValue";
                CodeMemberMethod GetSegmentValue = new CodeMemberMethod
                {
                    Name = sContext,
                    Attributes = MemberAttributes.Public | MemberAttributes.Override,
                    ReturnType = new CodeTypeReference(typeof(string))
                };
                CodeParameterDeclarationExpression pSegmentName = ParameterDeclarationExp(typeof(String), "szSegName");
                GetSegmentValue.Parameters.Add(pSegmentName);
                CodeParameterDeclarationExpression pInstNumber = ParameterDeclarationExp(typeof(long), "lnInstNumber");
                GetSegmentValue.Parameters.Add(pInstNumber);
                CodeParameterDeclarationExpression pDataItem = ParameterDeclarationExp(typeof(String), "szDataItem");
                GetSegmentValue.Parameters.Add(pDataItem);

                CodeTryCatchFinallyStatement tryBlock = GetSegmentValue.AddTry();
                CodeVariableDeclarationStatement szValue = DeclareVariableAndAssign(typeof(String), "szValue", true, GetProperty(typeof(String), "Empty"));
                tryBlock.AddStatement(szValue);

                tryBlock.AddStatement(AssignVariable(ArgumentReferenceExp(pSegmentName.Name),
                                        MethodInvocationExp(MethodInvocationExp(ArgumentReferenceExp(pSegmentName.Name), "ToLower"), "Trim")
                                        ));
                tryBlock.AddStatement(AssignVariable(ArgumentReferenceExp(pDataItem.Name),
                            MethodInvocationExp(MethodInvocationExp(ArgumentReferenceExp(pDataItem.Name), "ToLower"), "Trim")
                            ));

                tryBlock.AddStatement(SnippetStatement(String.Format("switch ({0})", pSegmentName.Name)));
                tryBlock.AddStatement(SnippetStatement("{")); //switch case scope starts here

                tryBlock.AddStatement(SnippetStatement("case \"fw_context\":"));
                tryBlock.AddStatement(ReturnExpression(ArrayIndexerExpression("nvcFW_CONTEXT", ArgumentReferenceExp(pDataItem.Name))));

                IEnumerable<Segment> segments = _service.Segments;
                foreach (Segment segment in segments)
                {
                    bool IsMultiInstance = segment.Inst.Equals("1");
                    tryBlock.AddStatement(SnippetStatement(String.Format("case \"{0}\":", segment.Name.ToLowerInvariant())));

                    //Single Instance
                    if (!IsMultiInstance)
                    {
                        tryBlock.AddStatement(ReturnExpression(ArrayIndexerExpression(String.Format("nvc{0}", segment.Name.ToUpperInvariant()), ArgumentReferenceExp(pDataItem.Name))));
                    }
                    //Multi Instance
                    else
                    {
                        tryBlock.AddStatement(DeclareVariableAndAssign(typeof(NameValueCollection), String.Format("nvcTmp{0}", segment.Name.ToLowerInvariant()), true, SnippetExpression(String.Format("(NameValueCollection)ht{0}[{1}]", segment.Name.ToUpperInvariant(), pInstNumber.Name))));
                        tryBlock.AddStatement(ReturnExpression(ArrayIndexerExpression(String.Format("nvcTmp{0}", segment.Name.ToLowerInvariant()), ArgumentReferenceExp(pDataItem.Name))));
                    }
                    tryBlock.AddStatement(SnippetExpression("break")); // break for each segment's case
                }

                tryBlock.AddStatement(SnippetStatement("default:"));
                tryBlock.AddStatement(AssignVariable(VariableReferenceExp(szValue.Name), MethodInvocationExp(BaseReferenceExp(), GetSegmentValue.Name).AddParameters(new CodeExpression[] { ArgumentReferenceExp(pSegmentName.Name), ArgumentReferenceExp(pInstNumber.Name), ArgumentReferenceExp(pDataItem.Name) })));

                CodeConditionStatement ifSegValueIsNotNull = IfCondition();
                ifSegValueIsNotNull.Condition = BinaryOpertorExpression(VariableReferenceExp(szValue.Name), CodeBinaryOperatorType.IdentityInequality, SnippetExpression("null"));
                ifSegValueIsNotNull.TrueStatements.Add(ReturnExpression(VariableReferenceExp(szValue.Name)));
                tryBlock.AddStatement(ifSegValueIsNotNull);

                Set_Error_Info("GetSegmentValue_Default", tryBlock, null, null);
                tryBlock.AddStatement(ReturnExpression(GetProperty(typeof(String), "Empty")));
                tryBlock.AddStatement(SnippetExpression("break"));//break for default case
                tryBlock.AddStatement(SnippetStatement("}")); //switch case scope ends here

                CodeCatchClause catchOtherException = tryBlock.AddCatch("Exception", "e");
                //catchOtherException.AddStatement(MethodInvocationExp(BaseReferenceExp(), "WriteProfiler").AddParameters(new CodeExpression[] { MethodInvocationExp(TypeReferenceExp(typeof(string)), "Format").AddParameters(new CodeExpression[] { PrimitiveExpression("General exception in getFieldValue - {0}"), GetProperty("e", "Message") }) }));
                catchOtherException.AddStatement(MethodInvocationExp(BaseReferenceExp(), "Set_Error_Info").AddParameters(new CodeExpression[] { PrimitiveExpression(0),
                                                                                                                                                FieldReferenceExp(TypeReferenceExp("CUtil"),"FRAMEWORK_ERROR"),
                                                                                                                                                FieldReferenceExp(TypeReferenceExp("CUtil"),"STOP_PROCESSING"),
                                                                                                                                                MethodInvocationExp(TypeReferenceExp(typeof(string)), "Format").AddParameters(new CodeExpression[] { PrimitiveExpression("RVWException in getSegmentValue - {0}"), GetProperty("e", "Message") }),
                                                                                                                                                GetProperty(TypeReferenceExp(typeof(string)),"Empty"),
                                                                                                                                                GetProperty(TypeReferenceExp(typeof(string)),"Empty"),
                                                                                                                                                PrimitiveExpression(0),
                                                                                                                                                GetProperty(TypeReferenceExp(typeof(string)),"Empty"),
                                                                                                                                                GetProperty(TypeReferenceExp(typeof(string)),"Empty")
                                                                                                                                                }));
                catchOtherException.AddStatement(ReturnExpression(GetProperty(TypeReferenceExp(typeof(string)), "Empty")));

                return GetSegmentValue;
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("GetSegmentValue->{0}", !Object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }

        /// <summary>
        /// Generates 'ValidateMandatorySegment' method
        /// </summary>
        /// <returns></returns>
        private CodeMemberMethod ValidateMandatorySegment()
        {
            try
            {
                //logger.WriteLogToFile(String.Empty, String.Format("Generating member function \"ValidateMandatorySegment() for the service {0}\"", _service.Name));

                String sContext = "ValidateMandatorySegment";
                CodeMemberMethod ValidateMandatorySegment = new CodeMemberMethod
                {
                    Name = sContext,
                    Attributes = MemberAttributes.Private,
                    ReturnType = new CodeTypeReference(typeof(int))
                };

                if (!_service.HasRollbackProcessSections)
                {
                    //Taking mandatory Multi Segments having in/inout datitems alone
                    foreach (Segment segment in (from segment in _service.Segments
                                                 where segment.Name != CONTEXT_SEGMENT && segment.Name != ERR_SEGMENT_NAME
                                                 && segment.Inst == MULTI_INSTANCE
                                                 && segment.MandatoryFlg == "1"
                                                 && (from di in segment.DataItems
                                                     where di.FlowDirection == FlowAttribute.IN
                                                     || di.FlowDirection == FlowAttribute.INOUT
                                                     select di).Any()
                                                 select segment).Distinct())
                    {

                        CodeConditionStatement CheckCountIsZero = IfCondition();
                        CheckCountIsZero.Condition = BinaryOpertorExpression(GetProperty(String.Format("ht{0}", segment.Name.ToUpperInvariant()), "Count"),
                                                                             CodeBinaryOperatorType.IdentityEquality,
                                                                             PrimitiveExpression(0));
                        WriteProfiler_ValidateMandatorySegment("ValidateMandatorySegment_RPS", null, null, CheckCountIsZero, "OT", segment.Name.ToUpperInvariant());
                        Set_Error_Info_ValidateMandatorySegment("ValidateMandatorySegment_RPS", null, null, CheckCountIsZero, segment.Name.ToUpperInvariant());
                        ValidateMandatorySegment.AddStatement(CheckCountIsZero);
                    }
                }
                else
                {
                    CodeConditionStatement CheckCountIsZero = IfCondition();
                    CheckCountIsZero.Condition = BinaryOpertorExpression(GetProperty("htERRORDETAILS", "Count"),
                                                                         CodeBinaryOperatorType.IdentityEquality,
                                                                         PrimitiveExpression(0));

                    WriteProfiler_ValidateMandatorySegment("ValidateMandatorySegment_Err", null, null, CheckCountIsZero, "OT", String.Empty);
                    Set_Error_Info_ValidateMandatorySegment("ValidateMandatorySegment_Err", null, null, CheckCountIsZero, String.Empty);
                    ValidateMandatorySegment.AddStatement(CheckCountIsZero);
                }

                ValidateMandatorySegment.AddStatement(ReturnExpression(PrimitiveExpression(0)));
                return ValidateMandatorySegment;
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("ValidateMandatorySegment->{0}", !Object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }

        /// <summary>
        /// Generates 'FillUnMappedDataItems' method
        /// </summary>
        /// <returns></returns>
        private CodeMemberMethod FillUnMappedDataItems()
        {
            try
            {
                //logger.WriteLogToFile(String.Empty, String.Format("Generating member function \"FillUnMappedDataItems() for the service {0}\"", _service.Name));
                String sContext = "FillUnMappedDataItems";

                CodeMemberMethod FillUnMappedDataItems = new CodeMemberMethod
                {
                    Name = sContext,
                    Attributes = MemberAttributes.Private,
                    ReturnType = new CodeTypeReference(typeof(int))
                };
                CodeTryCatchFinallyStatement tryBlock = FillUnMappedDataItems.AddTry();



                foreach (IGrouping<String, DataRow> SegmentGroup in (from row in _service.GetUnMappedDataItems(this._ecrOptions).AsEnumerable()
                                                                     where !row.Field<String>("instanceflag").Equals("1")
                                                                     select row).GroupBy(row => row.Field<String>("segmentname")))
                {
                    DataRow SegmentInfo = SegmentGroup.First();

                    String sSegName = SegmentGroup.Key;
                    String sSegInstFlg = Convert.ToString(SegmentInfo["instanceflag"]);

                    //for each dataitem
                    foreach (DataRow DataItem in SegmentGroup)
                    {
                        String sDIName = Convert.ToString(DataItem["dataitemname"]);
                        String sDIDefaultValue = Convert.ToString(DataItem["defaultvalue"]);

                        tryBlock.AddStatement(SnippetExpression(String.Format("nvc{0}[\"{1}\"] = (nvc{0}[\"{1}\"] == null) ? \"{2}\" : nvc{0}[\"{1}\"]", sSegName.ToUpperInvariant(), sDIName.ToLowerInvariant(), sDIDefaultValue)));
                    }
                }


                foreach (IGrouping<String, DataRow> SegmentGroup in (from row in _service.GetUnMappedDataItems(this._ecrOptions).AsEnumerable()
                                                                     where row.Field<String>("instanceflag").Equals("1")
                                                                     select row).GroupBy(row => row.Field<String>("segmentname")))
                {
                    DataRow SegmentInfo = SegmentGroup.First();

                    String sSegName = SegmentGroup.Key.ToUpper();
                    String sSegInstFlg = Convert.ToString(SegmentInfo["instanceflag"]);

                    String sCounterVariable = "InstNumber";
                    CodeIterationStatement ForEachSegmentInstance = ForLoopExpression(DeclareVariableAndAssign(typeof(long), sCounterVariable, true, PrimitiveExpression(1)),
                                                                                        BinaryOpertorExpression(VariableReferenceExp(sCounterVariable),
                                                                                                                CodeBinaryOperatorType.LessThanOrEqual,
                                                                                                                GetProperty(String.Format("ht{0}", sSegName), "Count")),
                                                                                        AssignVariable(VariableReferenceExp(sCounterVariable), BinaryOpertorExpression(VariableReferenceExp(sCounterVariable),
                                                                                                                                                                        CodeBinaryOperatorType.Add,
                                                                                                                                                                        PrimitiveExpression(1)
                                                                                                                                                )));
                    ForEachSegmentInstance.Statements.Add(AssignVariable(FieldReferenceExp(ThisReferenceExp(), "nvcTmp"), SnippetExpression(String.Format("(NameValueCollection) ht{0}[{1}]", sSegName, sCounterVariable))));

                    CodeConditionStatement ifSegmentInstIsNull = IfCondition();
                    ifSegmentInstIsNull.Condition = BinaryOpertorExpression(FieldReferenceExp(ThisReferenceExp(), "nvcTmp"), CodeBinaryOperatorType.IdentityEquality, SnippetExpression("null"));
                    ifSegmentInstIsNull.TrueStatements.Add(AssignVariable(FieldReferenceExp(ThisReferenceExp(), "nvcTmp"), ObjectCreateExpression(typeof(NameValueCollection))));
                    ForEachSegmentInstance.Statements.Add(ifSegmentInstIsNull);

                    //for each dataitem
                    foreach (DataRow DataItem in SegmentGroup)
                    {
                        String sDIName = Convert.ToString(DataItem["dataitemname"]);
                        String sDIDefaultValue = Convert.ToString(DataItem["defaultvalue"]);

                        ForEachSegmentInstance.Statements.Add(AssignVariable(ArrayIndexerExpression("nvcTmp", PrimitiveExpression(sDIName)), SnippetExpression(String.Format("(nvcTmp[\"{0}\"] == null) ? \"{1}\" : nvcTmp[\"{0}\"]", sDIName.ToLower(), sDIDefaultValue.ToLower()))));
                    }

                    ForEachSegmentInstance.Statements.Add(AssignVariable(ArrayIndexerExpression(String.Format("ht{0}", sSegName), VariableReferenceExp(sCounterVariable)), FieldReferenceExp(ThisReferenceExp(), "nvcTmp")));
                    ForEachSegmentInstance.Statements.Add(AssignVariable(FieldReferenceExp(ThisReferenceExp(), "nvcTmp"), SnippetExpression("null")));
                    tryBlock.AddStatement(ForEachSegmentInstance);
                }

                CodeConditionStatement ifIEDKService = IfCondition();
                ifIEDKService.Condition = FieldReferenceExp(BaseReferenceExp(), "iEDKServiceES");
                ifIEDKService.TrueStatements.Add(MethodInvocationExp(BaseReferenceExp(), "FillUnMappedDataItems"));
                tryBlock.AddStatement(ifIEDKService);

                CodeCatchClause catchOtherException = tryBlock.AddCatch("Exception", "e");
                //catchOtherException.AddStatement(MethodInvocationExp(BaseReferenceExp(), "WriteProfiler").AddParameters(new CodeExpression[] { MethodInvocationExp(TypeReferenceExp(typeof(string)), "Format").AddParameters(new CodeExpression[] { PrimitiveExpression("General exception in getFieldValue - {0}"), GetProperty("e", "Message") }) }));
                catchOtherException.AddStatement(MethodInvocationExp(BaseReferenceExp(), "Set_Error_Info").AddParameters(new CodeExpression[] { PrimitiveExpression(0),
                                                                                                                                                GetProperty(VariableReferenceExp("e"),"Source"),
                                                                                                                                                FieldReferenceExp(TypeReferenceExp("CUtil"),"STOP_PROCESSING"),
                                                                                                                                                MethodInvocationExp(TypeReferenceExp(typeof(string)), "Format").AddParameters(new CodeExpression[] { PrimitiveExpression("RVWException in FillUnMappedDataitem - {0}"), GetProperty("e", "Message") }),
                                                                                                                                                GetProperty(TypeReferenceExp(typeof(string)),"Empty"),
                                                                                                                                                GetProperty(TypeReferenceExp(typeof(string)),"Empty"),
                                                                                                                                                PrimitiveExpression(0),
                                                                                                                                                GetProperty(TypeReferenceExp(typeof(string)),"Empty"),
                                                                                                                                                GetProperty(TypeReferenceExp(typeof(string)),"Empty")
                                                                                                                                                }));


                FillUnMappedDataItems.AddStatement(ReturnExpression(PrimitiveExpression(0)));
                return FillUnMappedDataItems;
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("FillUnMappedDataItems->{0}", !Object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }

        #region Method to generate functions for Zipped Service

        /// <summary>
        /// Generates 'ProcessZippedService' Method
        /// </summary>
        /// <returns></returns>
        private CodeMemberMethod ProcessZippedService_Zipped()
        {
            CodeMemberMethod ProcessZippedService = new CodeMemberMethod
            {
                Name = "ProcessZippedService",
                Attributes = MemberAttributes.Public | MemberAttributes.Final,
                ReturnType = new CodeTypeReference(typeof(int))
            };

            try
            {
                //logger.WriteLogToFile(String.Empty, String.Format("Generating member function \"ProcessZippedService() for the service {0}\"", _service.Name));

                #region Add method parameters
                CodeParameterDeclarationExpression pInMTD = ParameterDeclarationExp(typeof(String), "szInMtd");
                ProcessZippedService.Parameters.Add(pInMTD);

                CodeParameterDeclarationExpression pSessionToken = ParameterDeclarationExp(typeof(String), "szSessionToken");
                ProcessZippedService.Parameters.Add(pSessionToken);

                CodeParameterDeclarationExpression pReturnDataSet = ParameterDeclarationExp(typeof(bool), "returnDataSet");
                ProcessZippedService.Parameters.Add(pReturnDataSet);

                CodeParameterDeclarationExpression pOUTMTD = ParameterDeclarationExp(typeof(byte[]), "szOutMtd");
                pOUTMTD.Direction = FieldDirection.Out;
                ProcessZippedService.Parameters.Add(pOUTMTD);
                #endregion

                ProcessZippedService.AddStatement(MethodInvocationExp(BaseReferenceExp(), "WriteProfiler").AddParameters(new CodeExpression[] { MethodInvocationExp(TypeReferenceExp(typeof(String)), "Format").AddParameters(new CodeExpression[] { PrimitiveExpression("Begin Service " + _service.Name + " - ByteArray version - {0}"), SnippetExpression("System.DateTime.Now.ToString()") }) }));

                CodeVariableDeclarationStatement vElapsedTime = DeclareVariableAndAssign("Stopwatch", "elapsedTime", true, ObjectCreateExpression("Stopwatch"));
                ProcessZippedService.AddStatement(vElapsedTime);

                ProcessZippedService.AddStatement(MethodInvocationExp(VariableReferenceExp(vElapsedTime.Name), "Start"));
                ProcessZippedService.AddStatement(AssignVariable(ArgumentReferenceExp(pOUTMTD.Name), SnippetExpression("null")));


                CodeTryCatchFinallyStatement tryBlock = ProcessZippedService.AddTry();
                tryBlock.AddStatement(ReturnExpression(MethodInvocationExp(ThisReferenceExp(), "ProcessServiceSections").AddParameters(new CodeExpression[] { ArgumentReferenceExp(pInMTD.Name), ArgumentReferenceExp(pSessionToken.Name) })));

                CodeCatchClause catchBlock = tryBlock.AddCatch();
                //catchBlock.AddStatement(ReturnExpression(FieldReferenceExp(BaseReferenceExp(), "ATMA_FAILURE")));
                catchBlock.AddStatement(ReturnExpression(VariableReferenceExp("ATMA_FAILURE")));

                #region finally Block
                tryBlock.FinallyStatements.Add(MethodInvocationExp(BaseReferenceExp(), "WriteProfiler").AddParameters(new CodeExpression[] { PrimitiveExpression("Before calling post process") }));

                #region try Block within finally
                CodeTryCatchFinallyStatement innerTry = new CodeTryCatchFinallyStatement();

                CodeConditionStatement ifBlock = IfCondition();
                ifBlock.Condition = BinaryOpertorExpression(FieldReferenceExp(BaseReferenceExp(), "bIsInteg"), CodeBinaryOperatorType.IdentityEquality, PrimitiveExpression(false));
                ifBlock.TrueStatements.Add(AssignVariable(ArgumentReferenceExp(pOUTMTD.Name), MethodInvocationExp(ThisReferenceExp(), "GetBODBytes").AddParameters(new CodeExpression[] { MethodInvocationExp(BaseReferenceExp(), "Service_Post_Process"), ArgumentReferenceExp(pReturnDataSet.Name) })));
                innerTry.TryStatements.Add(ifBlock);

                tryBlock.FinallyStatements.Add(innerTry);

                CodeCatchClause innerCatch = innerTry.AddCatch();
                innerCatch.AddStatement(MethodInvocationExp(BaseReferenceExp(), "WriteProfiler").AddParameters(new CodeExpression[] { GetProperty("e", "Message") }));
                #endregion

                #endregion

                tryBlock.FinallyStatements.Add(MethodInvocationExp(FieldReferenceExp(BaseReferenceExp(), "Out"), "Dispose"));
                tryBlock.FinallyStatements.Add(MethodInvocationExp(VariableReferenceExp(vElapsedTime.Name), "Stop"));
                tryBlock.FinallyStatements.Add(AssignVariable(FieldReferenceExp(ThisReferenceExp(), "serviceElapsedTimeinms"), GetProperty(vElapsedTime.Name, "ElapsedMilliseconds")));
                tryBlock.FinallyStatements.Add(MethodInvocationExp(BaseReferenceExp(), "WriteProfiler").AddParameters(new CodeExpression[] {
                                                                                                                                MethodInvocationExp(TypeReferenceExp(typeof(String)), "Format").AddParameters(new CodeExpression[] { PrimitiveExpression("Service "+_service.Name + " Execution took {0} ms"),
                                                                                                                                                                                                                                     GetProperty(vElapsedTime.Name,"ElapsedMilliseconds")
                                                                                                                                                                                                                                    }) ,
                                                                                                                                }));
                tryBlock.FinallyStatements.Add(MethodInvocationExp(BaseReferenceExp(), "WriteProfiler").AddParameters(new CodeExpression[] { MethodInvocationExp(TypeReferenceExp(typeof(String)), "Format").AddParameters(new CodeExpression[] { PrimitiveExpression("End Service " + _service.Name + " - ByteArray version - {0}"), SnippetExpression("System.DateTime.Now.ToString()") }) }));
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("ProcessZippedService_Zipped->{0}", !Object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
            return ProcessZippedService;
        }

        /// <summary>
        /// Generates 'GetBODBytes' Method
        /// </summary>
        /// <returns></returns>
        private CodeMemberMethod GetBODBytes_Zipped()
        {
            CodeMemberMethod GetBODBytes = new CodeMemberMethod
            {
                Name = "GetBODBytes",
                Attributes = MemberAttributes.Private,
                ReturnType = new CodeTypeReference(typeof(byte[]))
            };

            try
            {
                //logger.WriteLogToFile(String.Empty, String.Format("Generating member function \"GetBODBytes() for the service {0}\"", _service.Name));

                #region Method paramters
                CodeParameterDeclarationExpression pAllSegments = ParameterDeclarationExp(typeof(bool), "allSegments");
                GetBODBytes.Parameters.Add(pAllSegments);
                CodeParameterDeclarationExpression pReturnDataSet = ParameterDeclarationExp(typeof(bool), "returnDataSet");
                GetBODBytes.Parameters.Add(pReturnDataSet);
                #endregion

                #region Local variable Declarations & Initializations
                CodeVariableDeclarationStatement vOutTDInByteArray = DeclareVariableAndAssign(typeof(byte[]), "OutTDInByteArray", true, SnippetExpression("null"));
                GetBODBytes.AddStatement(vOutTDInByteArray);
                #endregion

                #region Try block
                CodeTryCatchFinallyStatement tryBlock = GetBODBytes.AddTry();
                tryBlock.AddStatement(MethodInvocationExp(BaseReferenceExp(), "WriteProfiler").AddParameters(new CodeExpression[] { MethodInvocationExp(TypeReferenceExp(typeof(String)), "Format").AddParameters(new CodeExpression[] { PrimitiveExpression("Executing GetBODBytes Method returnDataSet {0}"), MethodInvocationExp(ArgumentReferenceExp(pReturnDataSet.Name), "ToString") }) }));

                //Declares watch variable
                CodeVariableDeclarationStatement vWatch = DeclareVariableAndAssign("Stopwatch", "watch", true, ObjectCreateExpression("Stopwatch"));
                tryBlock.AddStatement(vWatch);

                //starts stopwatch
                tryBlock.AddStatement(MethodInvocationExp(VariableReferenceExp(vWatch.Name), "Start"));

                #region ifcondition

                //defining if condition
                CodeConditionStatement ifReturnDatasetIsFalse = IfCondition();
                ifReturnDatasetIsFalse.Condition = BinaryOpertorExpression(ArgumentReferenceExp(pReturnDataSet.Name), CodeBinaryOperatorType.IdentityEquality, PrimitiveExpression(false));

                //declare variable for memory stream
                CodeVariableDeclarationStatement vMStream = DeclareVariableAndAssign(typeof(MemoryStream), "mStream", true, SnippetExpression("null"));
                ifReturnDatasetIsFalse.TrueStatements.Add(vMStream);

                //declare variable for GZipStream 
                CodeVariableDeclarationStatement vTinyStream = DeclareVariableAndAssign("GZipStream", "tinyStream", true, SnippetExpression("null"));
                ifReturnDatasetIsFalse.TrueStatements.Add(vTinyStream);

                //assign value for writer
                ifReturnDatasetIsFalse.TrueStatements.Add(AssignVariable(FieldReferenceExp(BaseReferenceExp(), "writer"), SnippetExpression("null")));

                //old code has been restructured to achieve the same result ('using' code to 'dispose by finally')
                #region inner try block
                CodeTryCatchFinallyStatement innerTry = new CodeTryCatchFinallyStatement();
                innerTry.AddStatement(AssignVariable(VariableReferenceExp(vMStream.Name), ObjectCreateExpression(typeof(MemoryStream))));
                innerTry.AddStatement(AssignVariable(VariableReferenceExp(vTinyStream.Name), ObjectCreateExpression("GZipStream", new CodeExpression[] { VariableReferenceExp("mStream"), GetProperty(TypeReferenceExp("CompressionMode"), "Compress") })));
                innerTry.AddStatement(AssignVariable(FieldReferenceExp(BaseReferenceExp(), "writer"), ObjectCreateExpression(typeof(System.Xml.XmlTextWriter), new CodeExpression[] { VariableReferenceExp(vTinyStream.Name), SnippetExpression("null") })));


                CodeConditionStatement ifWriterIsNotNull = IfCondition();
                ifWriterIsNotNull.Condition = BinaryOpertorExpression(GetProperty(BaseReferenceExp(), "writer"), CodeBinaryOperatorType.IdentityInequality, SnippetExpression("null"));

                ifWriterIsNotNull.TrueStatements.Add(MethodInvocationExp(FieldReferenceExp(BaseReferenceExp(), "writer"), "WriteStartElement").AddParameters(new CodeExpression[] { PrimitiveExpression("VW-TD") }));
                ifWriterIsNotNull.TrueStatements.Add(MethodInvocationExp(BaseReferenceExp(), "BuildContextSegments"));

                CodeConditionStatement ifAllSegmentsIsTrue = IfCondition();
                ifAllSegmentsIsTrue.Condition = BinaryOpertorExpression(VariableReferenceExp(pAllSegments.Name), CodeBinaryOperatorType.IdentityEquality, PrimitiveExpression(true));
                ifAllSegmentsIsTrue.TrueStatements.Add(MethodInvocationExp(ThisReferenceExp(), "BuildOutSegments"));
                ifWriterIsNotNull.TrueStatements.Add(ifAllSegmentsIsTrue);

                ifWriterIsNotNull.TrueStatements.Add(MethodInvocationExp(BaseReferenceExp(), "BuildErrorSegments"));
                ifWriterIsNotNull.TrueStatements.Add(MethodInvocationExp(FieldReferenceExp(BaseReferenceExp(), "writer"), "WriteEndElement"));
                ifWriterIsNotNull.TrueStatements.Add(MethodInvocationExp(FieldReferenceExp(BaseReferenceExp(), "writer"), "Flush"));
                ifWriterIsNotNull.TrueStatements.Add(MethodInvocationExp(FieldReferenceExp(BaseReferenceExp(), "writer"), "Close"));
                innerTry.AddStatement(ifWriterIsNotNull);

                innerTry.AddStatement(AssignVariable(VariableReferenceExp(vOutTDInByteArray.Name), MethodInvocationExp(VariableReferenceExp(vMStream.Name), "ToArray")));
                #endregion

                #region inner finally statement
                //dispose memory stream
                CodeConditionStatement ifMemoryStreamIsNotNull = IfCondition();
                ifMemoryStreamIsNotNull.Condition = BinaryOpertorExpression(VariableReferenceExp(vMStream.Name), CodeBinaryOperatorType.IdentityInequality, SnippetExpression("null"));
                ifMemoryStreamIsNotNull.TrueStatements.Add(MethodInvocationExp(VariableReferenceExp(vMStream.Name), "Dispose"));
                innerTry.FinallyStatements.Add(ifMemoryStreamIsNotNull);

                //dispose zip stream 
                CodeConditionStatement ifTinyStreamIsNotNull = IfCondition();
                ifTinyStreamIsNotNull.Condition = BinaryOpertorExpression(VariableReferenceExp(vTinyStream.Name), CodeBinaryOperatorType.IdentityInequality, SnippetExpression("null"));
                ifTinyStreamIsNotNull.TrueStatements.Add(MethodInvocationExp(VariableReferenceExp(vTinyStream.Name), "Dispose"));
                innerTry.FinallyStatements.Add(ifTinyStreamIsNotNull);

                //dispose writer
                //CodeConditionStatement ifWriterIsNotNull = IfCondition();
                //ifWriterIsNotNull.Condition = BinaryOpertorExpression(FieldReferenceExp(BaseReferenceExp(), "writer"), CodeBinaryOperatorType.IdentityInequality, SnippetExpression("null"));
                //ifWriterIsNotNull.TrueStatements.Add(MethodInvocationExp(FieldReferenceExp(BaseReferenceExp(), "writer"), "Dispose"));
                //innerTry.FinallyStatements.Add(ifWriterIsNotNull);
                #endregion Finally statements

                ifReturnDatasetIsFalse.TrueStatements.Add(innerTry);
                tryBlock.AddStatement(ifReturnDatasetIsFalse);
                tryBlock.AddStatement(MethodInvocationExp(BaseReferenceExp(), "WriteProfiler").AddParameters(new CodeExpression[] { MethodInvocationExp(TypeReferenceExp(typeof(String)), "Format").AddParameters(new CodeExpression[] { PrimitiveExpression("Compressed OutTd ByteArray Length : {0}"), MethodInvocationExp(GetProperty(VariableReferenceExp(vOutTDInByteArray.Name), "Length"), "ToString") }) }));
                tryBlock.AddStatement(MethodInvocationExp(VariableReferenceExp(vWatch.Name), "Stop"));
                tryBlock.AddStatement(MethodInvocationExp(BaseReferenceExp(), "WriteProfiler").AddParameters(new CodeExpression[] { MethodInvocationExp(TypeReferenceExp(typeof(String)), "Format").AddParameters(new CodeExpression[] { PrimitiveExpression("Time taken for building zipped outtd : {0}"), GetProperty(VariableReferenceExp(vWatch.Name), "Elapsed") }) }));
                tryBlock.AddStatement(ReturnExpression(VariableReferenceExp(vOutTDInByteArray.Name)));
                #endregion

                #endregion

                #region Catch block
                CodeCatchClause CRVWCatchBlock = tryBlock.AddCatch("CRVWException", "rvwe");
                CRVWCatchBlock.ThrowException("rvwe");

                CodeCatchClause GeneralCatchBlock = tryBlock.AddCatch();
                GeneralCatchBlock.Statements.Add(MethodInvocationExp(BaseReferenceExp(), "WriteProfiler").AddParameters(new CodeExpression[] { MethodInvocationExp(TypeReferenceExp(typeof(String)), "Format").AddParameters(new Object[] { "General exception in GetBOD {0} returnDataset {1}", GetProperty(VariableReferenceExp("e"), "Message"), MethodInvocationExp(ArgumentReferenceExp(pReturnDataSet.Name), "ToString") }) }));
                GeneralCatchBlock.Statements.Add(ThrowNewException(ObjectCreateExpression(typeof(Exception), new CodeExpression[] { MethodInvocationExp(TypeReferenceExp(typeof(String)), "Format").AddParameters(new Object[] { "General exception in GetBOD {0} returnDataSet {1}", MethodInvocationExp(VariableReferenceExp("e"), "ToString"), MethodInvocationExp(ArgumentReferenceExp(pReturnDataSet.Name), "ToString") }) })));
                #endregion
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("GetBODBytes_Zipped->{0}", !Object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }

            return GetBODBytes;
        }

        /// <summary>
        /// Generates 'ProcessService' Method
        /// </summary>
        /// <returns></returns>
        private CodeMemberMethod ProcessService_Zipped()
        {
            CodeMemberMethod ProcessService = new CodeMemberMethod
            {
                Name = "ProcessService",
                Attributes = MemberAttributes.Public | MemberAttributes.Final,
                ReturnType = new CodeTypeReference(typeof(int))
            };

            try
            {
                //logger.WriteLogToFile(String.Empty, String.Format("Generating member function \"ProcessService() for the service {0}\"", _service.Name));

                #region Add method parameters
                CodeParameterDeclarationExpression pInMtd = ParameterDeclarationExp(typeof(String), "szInMtd");
                ProcessService.Parameters.Add(pInMtd);
                CodeParameterDeclarationExpression pSessionToken = ParameterDeclarationExp(typeof(String), "szSessionToken");
                ProcessService.Parameters.Add(pSessionToken);
                CodeParameterDeclarationExpression pOutMtd = ParameterDeclarationExp(typeof(String), "szOutMtd");
                pOutMtd.Direction = FieldDirection.Out;
                ProcessService.Parameters.Add(pOutMtd);
                #endregion

                #region Local Variable Declarations and assignments
                ProcessService.AddStatement(MethodInvocationExp(BaseReferenceExp(), "WriteProfiler").AddParameters(new CodeExpression[] { MethodInvocationExp(TypeReferenceExp(typeof(String)), "Format").AddParameters(new CodeExpression[] { PrimitiveExpression("Begin Service " + _service.Name + " - String version - {0}"), SnippetExpression("System.DateTime.Now.ToString()") }) }));

                CodeVariableDeclarationStatement vElapsedTime = DeclareVariableAndAssign("Stopwatch", "elapsedTime", true, ObjectCreateExpression("Stopwatch"));
                ProcessService.AddStatement(vElapsedTime);

                ProcessService.AddStatement(MethodInvocationExp(VariableReferenceExp(vElapsedTime.Name), "Start"));
                ProcessService.AddStatement(AssignVariable(ArgumentReferenceExp(pOutMtd.Name), SnippetExpression("null")));
                #endregion

                #region Try block
                CodeTryCatchFinallyStatement tryBlock = ProcessService.AddTry();
                tryBlock.AddStatement(ReturnExpression(MethodInvocationExp(ThisReferenceExp(), "ProcessServiceSections").AddParameters(new CodeExpression[] { ArgumentReferenceExp(pInMtd.Name), ArgumentReferenceExp(pSessionToken.Name) })));
                #endregion

                #region Catch block
                CodeCatchClause catchBlock = tryBlock.AddCatch();
                catchBlock.AddStatement(ReturnExpression(VariableReferenceExp("ATMA_FAILURE")));
                #endregion

                #region finally statements
                tryBlock.FinallyStatements.Add(MethodInvocationExp(BaseReferenceExp(), "WriteProfiler").AddParameters(new CodeExpression[] { PrimitiveExpression("Before calling post process") }));

                #region inner try block
                CodeTryCatchFinallyStatement innerTryBlock = new CodeTryCatchFinallyStatement();

                #region condition check
                CodeConditionStatement ifIntegIsFalse = IfCondition();
                ifIntegIsFalse.Condition = BinaryOpertorExpression(VariableReferenceExp("bIsInteg"), CodeBinaryOperatorType.IdentityEquality, PrimitiveExpression(false));
                ifIntegIsFalse.TrueStatements.Add(AssignVariable(ArgumentReferenceExp(pOutMtd.Name), MethodInvocationExp(ThisReferenceExp(), "GetBOD").AddParameter(MethodInvocationExp(BaseReferenceExp(), "Service_Post_Process"))));
                innerTryBlock.AddStatement(ifIntegIsFalse);
                #endregion

                #region inner catch block
                CodeCatchClause innerCatchBlock = innerTryBlock.AddCatch();
                innerCatchBlock.AddStatement(MethodInvocationExp(BaseReferenceExp(), "WriteProfiler").AddParameters(new CodeExpression[] { GetProperty("e", "Message") }));
                #endregion
                #endregion

                tryBlock.FinallyStatements.Add(innerTryBlock);
                tryBlock.FinallyStatements.Add(MethodInvocationExp(FieldReferenceExp(BaseReferenceExp(), "Out"), "Dispose"));
                tryBlock.FinallyStatements.Add(MethodInvocationExp(VariableReferenceExp(vElapsedTime.Name), "Stop"));
                tryBlock.FinallyStatements.Add(AssignVariable(FieldReferenceExp(ThisReferenceExp(), "serviceElapsedTimeinms"), GetProperty(vElapsedTime.Name, "ElapsedMilliseconds")));
                tryBlock.FinallyStatements.Add(MethodInvocationExp(BaseReferenceExp(), "WriteProfiler").AddParameters(new CodeExpression[] { MethodInvocationExp(TypeReferenceExp(typeof(String)), "Format").AddParameters(new CodeExpression[] { PrimitiveExpression("Service " + _service.Name + " Execution took {0} ms"), GetProperty(vElapsedTime.Name, "ElapsedMilliseconds") }) }));
                tryBlock.FinallyStatements.Add(MethodInvocationExp(BaseReferenceExp(), "WriteProfiler").AddParameters(new CodeExpression[] { MethodInvocationExp(TypeReferenceExp(typeof(String)), "Format").AddParameters(new CodeExpression[] { PrimitiveExpression("End Service " + _service.Name + " - String version - {0}"), SnippetExpression("System.DateTime.Now.ToString()") }) }));
                #endregion

            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("ProcessService_Zipped->{0}", !Object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }

            return ProcessService;
        }

        /// <summary>
        /// Generates 'ProcessServiceSections' method
        /// </summary>
        /// <returns></returns>
        private CodeMemberMethod ProcessServiceSections_Zipped()
        {
            CodeMemberMethod ProcessServiceSections = new CodeMemberMethod
            {
                Name = "ProcessServiceSections",
                Attributes = MemberAttributes.Private,
                ReturnType = new CodeTypeReference(typeof(int))
            };

            try
            {
                //logger.WriteLogToFile(String.Empty, String.Format("Generating member function \"ProcessServiceSections() for the service {0}\"", _service.Name));

                #region Add method parameters
                CodeParameterDeclarationExpression pINMtd = ParameterDeclarationExp(typeof(String), "szInMtd");
                ProcessServiceSections.Parameters.Add(pINMtd);
                CodeParameterDeclarationExpression pSessionToken = ParameterDeclarationExp(typeof(String), "szSessionToken");
                ProcessServiceSections.Parameters.Add(pSessionToken);
                #endregion

                ProcessServiceSections.AddStatement(MethodInvocationExp(BaseReferenceExp(), "WriteProfiler").AddParameters(new CodeExpression[] { MethodInvocationExp(TypeReferenceExp(typeof(String)), "Format").AddParameters(new CodeExpression[] { PrimitiveExpression("Service - " + _service.Name + " Started at {0}"), SnippetExpression("System.DateTime.Now.ToString()") }) }));

                #region Try Block
                CodeTryCatchFinallyStatement tryBlock = ProcessServiceSections.AddTry();
                tryBlock.AddStatement(MethodInvocationExp(BaseReferenceExp(), "WriteProfiler").AddParameters(new CodeExpression[] { PrimitiveExpression("Executing Process Service Method") }));

                CodeConditionStatement ifIntegIsFalse1 = IfCondition();
                ifIntegIsFalse1.Condition = BinaryOpertorExpression(FieldReferenceExp(BaseReferenceExp(), "bIsInteg"), CodeBinaryOperatorType.IdentityEquality, PrimitiveExpression(false));
                ifIntegIsFalse1.TrueStatements.Add(MethodInvocationExp(BaseReferenceExp(), "unpackBOD").AddParameters(new CodeExpression[] { ArgumentReferenceExp(pINMtd.Name) }));
                tryBlock.AddStatement(ifIntegIsFalse1);
                tryBlock.AddStatement(MethodInvocationExp(BaseReferenceExp(), "Service_Pre_Process").AddParameters(new CodeExpression[] {   GetProperty("String", "Empty"), ArgumentReferenceExp(pSessionToken.Name),
                                                                                                                                                        SnippetExpression("ref szComponentName") ,
                                                                                                                                                        SnippetExpression("ref szServiceName"),
                                                                                                                                                        SnippetExpression("ref szLangID"),
                                                                                                                                                        SnippetExpression("ref szCompInst"),
                                                                                                                                                        SnippetExpression("ref szOUI"),
                                                                                                                                                        SnippetExpression("ref szSecToken"),
                                                                                                                                                        SnippetExpression("ref szUser"),
                                                                                                                                                        SnippetExpression("ref szConnectionString"),
                                                                                                                                                        SnippetExpression("ref szTxnID"),
                                                                                                                                                        SnippetExpression("ref szRole")
                                                                                                                                                    }));
                tryBlock.AddStatement(MethodInvocationExp(ThisReferenceExp(), "ValidateMandatorySegment"));

                CodeConditionStatement ifIntegIsFalse2 = IfCondition();
                ifIntegIsFalse2.Condition = BinaryOpertorExpression(FieldReferenceExp(BaseReferenceExp(), "bIsInteg"), CodeBinaryOperatorType.IdentityEquality, PrimitiveExpression(false));
                ifIntegIsFalse2.TrueStatements.Add(MethodInvocationExp(ThisReferenceExp(), "FillUnMappedDataItems"));
                tryBlock.AddStatement(ifIntegIsFalse2);

                foreach (DataRow PS in (from processSection in _ecrOptions.generation.ServiceInfo.Tables["ProcessSection"].AsEnumerable()
                                        where String.Equals(processSection["servicename"].ToString(), _service.Name, StringComparison.OrdinalIgnoreCase)
                                        orderby int.Parse(processSection.Field<string>("seqno")) ascending
                                        select processSection))
                {
                    String sPSName = PS["name"].ToString();
                    String sSequenceNO = PS["seqno"].ToString();
                    tryBlock.AddStatement(CommentStatement(sPSName));
                    tryBlock.AddStatement(MethodInvocationExp(ThisReferenceExp(), String.Format("ProcessPS{0}", sSequenceNO)));
                }
                tryBlock.AddStatement(ReturnExpression(VariableReferenceExp("ATMA_SUCCESS")));
                #endregion

                #region Catch CRVWException
                CodeCatchClause catchCRVWException = tryBlock.AddCatch("CRVWException", "rvwe");
                catchCRVWException.AddStatement(MethodInvocationExp(BaseReferenceExp(), "WriteProfiler").AddParameters(new CodeExpression[] { MethodInvocationExp(TypeReferenceExp(typeof(String)), "Format").AddParameters(new CodeExpression[] { PrimitiveExpression("RvwException in ProcessService - {0}"), GetProperty("rvwe", "Message") }) }));
                catchCRVWException.AddStatement(ReturnExpression(VariableReferenceExp("ATMA_FAILURE")));
                #endregion

                #region Catch other exceptions
                CodeCatchClause catchBlock = tryBlock.AddCatch();

                CodeTryCatchFinallyStatement innerTry = new CodeTryCatchFinallyStatement();
                innerTry.AddStatement(MethodInvocationExp(BaseReferenceExp(), "Set_Error_Info").AddParameters(new CodeExpression[] {    PrimitiveExpression(0),
                                                                                                                                        VariableReferenceExp("FRAMEWORK_ERROR"),
                                                                                                                                        VariableReferenceExp("STOP_PROCESSING"),
                                                                                                                                        MethodInvocationExp(TypeReferenceExp(typeof(String)),"Format").AddParameters(new CodeExpression[]{PrimitiveExpression("General exception in ProcessService - {0}"),GetProperty("e","Message")}),
                                                                                                                                        GetProperty(typeof(String),"Empty"),
                                                                                                                                        GetProperty(typeof(String),"Empty"),
                                                                                                                                        PrimitiveExpression(0),
                                                                                                                                        GetProperty(typeof(String),"Empty"),
                                                                                                                                        GetProperty(typeof(String),"Empty")
                                                                                                                                    }));
                CodeCatchClause innerCatch = new CodeCatchClause();
                innerCatch.AddStatement(MethodInvocationExp(BaseReferenceExp(), "WriteProfiler").AddParameters(new CodeExpression[] { MethodInvocationExp(TypeReferenceExp(typeof(String)), "Format").AddParameters(new CodeExpression[] { PrimitiveExpression("General Exception in ProcessService - {0}"), GetProperty("e", "Message") }) }));
                innerTry.CatchClauses.Add(innerCatch);
                catchBlock.AddStatement(innerTry);

                //return expression of catch block
                //catchBlock.AddStatement(ReturnExpression(FieldReferenceExp(BaseReferenceExp(), "ATMA_FAILURE")));
                catchBlock.AddStatement(ReturnExpression(VariableReferenceExp("ATMA_FAILURE")));
                #endregion
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("ProcessServiceSections_Zipped->{0}", !Object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }

            return ProcessServiceSections;
        }
        #endregion


        /// <summary>
        /// Generates 'ProcessService' Method
        /// </summary>
        /// <returns></returns>
        private CodeMemberMethod ProcessService()
        {
            try
            {
                CodeVariableDeclarationStatement vCallRB = null;
                CodeConditionStatement IfbIsIntegUnPackBOD = IfCondition();
                CodeConditionStatement IfbIsIntegFillUnMappedDataItems = IfCondition();
                String sContext = "ProcessService";
                CodeMemberMethod ProcessService = new CodeMemberMethod
                {
                    Name = sContext,
                    ReturnType = new CodeTypeReference(typeof(int)),
                    Attributes = MemberAttributes.Public | MemberAttributes.Final
                };
                CodeParameterDeclarationExpression pINMtd = ParameterDeclarationExp(typeof(String), "szInMtd");
                CodeParameterDeclarationExpression pSessionToken = ParameterDeclarationExp(typeof(String), "szSessionToken");
                CodeParameterDeclarationExpression pOutMtd = ParameterDeclarationExp(typeof(String), "szOutMtd");

                ProcessService.Parameters.Add(pINMtd);
                ProcessService.Parameters.Add(pSessionToken);
                pOutMtd.Direction = FieldDirection.Out;
                ProcessService.Parameters.Add(pOutMtd);
                ProcessService.AddStatement(DeclareVariableAndAssign(typeof(bool), "bServicePostResult", true, true));
                ProcessService.AddStatement(AssignVariable("szOutMtd", GetProperty(typeof(String), "Empty")));

                CodeTryCatchFinallyStatement tryBlock = ProcessService.AddTry();
                WriteProfiler(sContext, tryBlock, null, null, "MS");

                // Has Rollback Process Sections
                if (_service.HasRollbackProcessSections)
                {
                    tryBlock.AddStatement(MethodInvocationExp(BaseReferenceExp(), "WriteProfiler").AddParameters(new CodeExpression[] { MethodInvocationExp(TypeReferenceExp(typeof(String)), "Format").AddParameters(new CodeExpression[] { PrimitiveExpression("Rollback ProcessSection for the service - " + _service.Name + " Started at {0}"), SnippetExpression("System.DateTime.Now.ToString()") }) }));

                    vCallRB = DeclareVariableAndAssign(typeof(bool), "bCallRB", true, PrimitiveExpression(false));
                    tryBlock.AddStatement(vCallRB);

                    tryBlock.AddStatement(AssignVariable(FieldReferenceExp(ThisReferenceExp(), "nErrMax"), GetProperty("htERRORDETAILS", "Count")));
                }

                if ((_service.HasCusBRO && _ecrOptions.InTD) || _service.IsBulkService)
                    tryBlock.AddStatement(AssignVariable(FieldReferenceExp(ThisReferenceExp(), "szInTD"), ArgumentReferenceExp(pINMtd.Name)));

                AddProcessServicebIsInteg(tryBlock, ref IfbIsIntegUnPackBOD, pINMtd, null, "unpackBOD");
                tryBlock.AddStatement(MethodInvocationExp(BaseReferenceExp(), "Service_Pre_Process").AddParameters(new CodeExpression[] {   GetProperty(typeof(String),"Empty"),
                                                                                                                                            ArgumentReferenceExp(pSessionToken.Name),
                                                                                                                                            SnippetExpression("ref szComponentName"),
                                                                                                                                            SnippetExpression("ref szServiceName"),
                                                                                                                                            SnippetExpression("ref szLangID"),
                                                                                                                                            SnippetExpression("ref szCompInst"),
                                                                                                                                            SnippetExpression("ref szOUI"),
                                                                                                                                            SnippetExpression("ref szSecToken"),
                                                                                                                                            SnippetExpression("ref szUser"),
                                                                                                                                            SnippetExpression("ref szConnectionString"),
                                                                                                                                            SnippetExpression("ref szTxnID"),
                                                                                                                                            SnippetExpression("ref szRole")
                                                                                                                                      }));
                if (_service.HasTypeBasedBRO)
                    tryBlock.AddStatement(AssignVariable(FieldReferenceExp(ThisReferenceExp(), "objBRO"),
                                          MethodInvocationExp(TypeReferenceExp(String.Format("{0}BRFactory", Common.InitCaps(_ecrOptions.Component))),
                                                              String.Format("Get{0}BR", Common.InitCaps(_ecrOptions.Component))).AddParameters(new CodeExpression[] { SnippetExpression("Provider") })));

                tryBlock.AddStatement(MethodInvocationExp(ThisReferenceExp(), "ValidateMandatorySegment"));

                AddProcessServicebIsInteg(tryBlock, ref IfbIsIntegFillUnMappedDataItems, pINMtd, null, "FillUnMappedDataItems");

                //Universal Personalization
                foreach (ProcessSection ps in _service.ProcessSections.OrderBy(p => Convert.ToInt16(p.SeqNO)))
                {
                    if (ps.IsUniversalPersonalizedSection != true)
                        tryBlock.AddStatement(MethodInvocationExp(ThisReferenceExp(), String.Format("ProcessPS{0}", ps.SeqNO)));
                    else
                        tryBlock.AddStatement(MethodInvocationExp(ThisReferenceExp(), "ProcessUniversalPersonalization").AddParameter(VariableReferenceExp(pSessionToken.Name)));
                }

                //if any one method under the service has 'method_exec_cont' flag
                if (_service.Implement_New_Method_Of_ParamAddition)
                {
                    CodeConditionStatement ifMethodValidationIsFalse = IfCondition();
                    ifMethodValidationIsFalse.Condition = SnippetExpression("!methodValidationStatus");
                    ifMethodValidationIsFalse.TrueStatements.Add(ThrowNewException(ObjectCreateExpression("CRVWException", new CodeExpression[] { PrimitiveExpression("Multiple Error Message") })));
                    tryBlock.AddStatement(ifMethodValidationIsFalse);
                }


                if (_service.HasRollbackProcessSections)
                    tryBlock.AddStatement(AssignVariable(VariableReferenceExp("bCallRB"), PrimitiveExpression(false)));

                if (!_service.HasRollbackProcessSections && !_service.HasCommitProcessSection)
                    tryBlock.AddStatement(ReturnExpression(VariableReferenceExp("ATMA_SUCCESS")));

                if (_service.HasRollbackProcessSections)
                {
                    CodeConditionStatement CheckErrMax = IfCondition();
                    CheckErrMax.Condition = BinaryOpertorExpression(FieldReferenceExp(ThisReferenceExp(), "nErrMax"), CodeBinaryOperatorType.IdentityEquality, SnippetExpression("(long) htERRORDETAILS.Count"));
                    CheckErrMax.TrueStatements.Add(ReturnExpression(VariableReferenceExp("ATMA_SUCCESS")));
                    CheckErrMax.FalseStatements.Add(ReturnExpression(VariableReferenceExp("ATMA_FAILURE")));
                    tryBlock.AddStatement(CheckErrMax);
                }

                ProcessService_RVWException(sContext, tryBlock, vCallRB);
                ProcessService_Exception(sContext, tryBlock, vCallRB);
                ProcessService_Finally(sContext, tryBlock, vCallRB, pINMtd, pOutMtd);

                return ProcessService;
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("ProcessService->{0}", !Object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }

        private CodeMemberMethod FillPlaceholdervalue()
        {
            try
            {
                CodeMemberMethod FillPlaceHolderValue = null;

                FillPlaceHolderValue = new CodeMemberMethod
                {
                    Name = "FillPlaceHolderValue",
                    Attributes = MemberAttributes.Private
                };

                CodeParameterDeclarationExpression pPlaceHolderData = new CodeParameterDeclarationExpression();
                pPlaceHolderData.Name = "PlaceHolderData";
                pPlaceHolderData.Type = new CodeTypeReference(typeof(Hashtable));
                pPlaceHolderData.Direction = FieldDirection.Ref;

                CodeParameterDeclarationExpression pInstance = new CodeParameterDeclarationExpression();
                pInstance.Name = "lInstance";
                pInstance.Type = new CodeTypeReference(typeof(long));

                FillPlaceHolderValue.Parameters.Add(pPlaceHolderData);
                FillPlaceHolderValue.Parameters.Add(pInstance);

                FillPlaceHolderValue.AddStatement(DeclareVariableAndAssign(typeof(Exception), "ex", true, SnippetExpression("null")));

                CodeTryCatchFinallyStatement tryBlock = AddTryBlock(FillPlaceHolderValue);
                tryBlock.AddStatement(DeclareVariableAndAssign(typeof(Hashtable), "tempdata", true, SnippetExpression("null")));
                tryBlock.AddStatement(DeclareVariableAndAssign(typeof(NameValueCollection), "Localtable", true, SnippetExpression("null")));
                tryBlock.AddStatement(DeclareVariableAndAssign(typeof(int), "count", true, GetProperty(pPlaceHolderData.Name, "Count")));

                CodeIterationStatement forLoop = ForLoopExpression(DeclareVariableAndAssign(typeof(int), "i", true, PrimitiveExpression(1)),
                                                                BinaryOpertorExpression(VariableReferenceExp("i"), CodeBinaryOperatorType.LessThanOrEqual, VariableReferenceExp("count")),
                                                                AssignVariable(VariableReferenceExp("i"), BinaryOpertorExpression(VariableReferenceExp("i"), CodeBinaryOperatorType.Add, PrimitiveExpression(1))));
                forLoop.Statements.Add(AssignVariable(VariableReferenceExp("tempdata"), SnippetExpression("(Hashtable)PlaceHolderData[i]")));
                forLoop.Statements.Add(SnippetStatement("switch(tempdata[\"SegName\"].ToString().ToLower())"));
                forLoop.Statements.Add(SnippetStatement("{"));
                foreach (Segment segment in _service.Segments.Where(s => s.Name != CONTEXT_SEGMENT
                                                                           && s.Name != ERR_SEGMENT_NAME))
                {
                    forLoop.Statements.Add(SnippetStatement(string.Format("case \"{0}\":", segment.Name.ToLower())));
                    forLoop.Statements.Add(AssignVariable("Localtable", SnippetExpression(segment.Inst.Equals(MULTI_INSTANCE) ? "(NameValueCollection)ht" + segment.Name.ToUpper() + "[lInstance]" : "nvc" + segment.Name.ToUpper())));
                    forLoop.Statements.Add(SnippetExpression("break"));
                }

                forLoop.Statements.Add(SnippetStatement(string.Format("case \"{0}\":", CONTEXT_SEGMENT.ToLower())));
                forLoop.Statements.Add(AssignVariable("Localtable", FieldReferenceExp(ThisReferenceExp(), string.Format("nvc{0}", CONTEXT_SEGMENT.ToUpper()))));
                forLoop.Statements.Add(SnippetExpression("break"));

                forLoop.Statements.Add(SnippetStatement("default:"));
                forLoop.Statements.Add(MethodInvocationExp(BaseReferenceExp(), "Set_Error_Info").AddParameters(new CodeExpression[] { PrimitiveExpression(0),
                                                                                                                                    FieldReferenceExp(TypeReferenceExp("CUtil"),"FRAMEWORK_ERROR"),
                                                                                                                                    FieldReferenceExp(TypeReferenceExp("CUtil"),"STOP_PROCESSING"),
                                                                                                                                    PrimitiveExpression("RVWException in FillPlaceHolderValue No NameValueCollection is present for the given segment name"),
                                                                                                                                    GetProperty(typeof(String),"Empty"),
                                                                                                                                    GetProperty(typeof(String),"Empty"),
                                                                                                                                    PrimitiveExpression(0),
                                                                                                                                    GetProperty(typeof(String),"Empty"),
                                                                                                                                    GetProperty(typeof(String),"Empty")
                                                                                                                                    }));
                forLoop.Statements.Add(SnippetExpression("break"));
                forLoop.Statements.Add(SnippetStatement("}"));//end of switch statement

                forLoop.Statements.Add(AssignVariable(ArrayIndexerExpression("tempdata", PrimitiveExpression("DIValue")), ArrayIndexerExpression("Localtable", MethodInvocationExp(ArrayIndexerExpression("tempdata", PrimitiveExpression("DIName")), "ToString"))));
                forLoop.Statements.Add(AssignVariable(ArrayIndexerExpression("PlaceHolderData", VariableReferenceExp("i")), VariableReferenceExp("tempdata")));
                //tryBlock.AddStatement(ReturnExpression());
                tryBlock.AddStatement(forLoop);
                tryBlock.AddStatement(ReturnExpression());

                CodeCatchClause catchCRVWException = tryBlock.AddCatch("CRVWException", "rvwe");
                catchCRVWException.ThrowException("rvwe");

                CodeCatchClause catchOtherException = tryBlock.AddCatch("e");
                catchOtherException.AddStatement(MethodInvocationExp(BaseReferenceExp(), "WriteProfiler").AddParameters(new CodeExpression[] { MethodInvocationExp(TypeReferenceExp(typeof(String)), "Format").AddParameters(new CodeExpression[] { PrimitiveExpression("{0} - {1}"), PrimitiveExpression("General exception in FillPlaceHolderValue"), GetProperty("e", "Message") }) }));
                catchOtherException.AddStatement(MethodInvocationExp(BaseReferenceExp(), "Set_Error_Info").AddParameters(new CodeExpression[] { PrimitiveExpression(0), FieldReferenceExp(TypeReferenceExp("CUtil"), "FRAMEWORK_ERROR"), FieldReferenceExp(TypeReferenceExp("CUtil"), "STOP_PROCESSING"), MethodInvocationExp(TypeReferenceExp(typeof(String)), "Format").AddParameters(new CodeExpression[] { PrimitiveExpression("General exception in FillPlaceHolderValue_{0}"), GetProperty("e", "Message") }), GetProperty(TypeReferenceExp(typeof(String)), "Empty"), GetProperty(TypeReferenceExp(typeof(String)), "Empty"), PrimitiveExpression(0), GetProperty(TypeReferenceExp(typeof(String)), "Empty"), GetProperty(TypeReferenceExp(typeof(String)), "Empty") }));

                return FillPlaceHolderValue;
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("FillPlaceholdervalue->{0}", ex.InnerException != null ? ex.InnerException.Message : ex.Message));
            }
        }

        private CodeMemberMethod Process_MethodError_Info()
        {
            try
            {
                CodeMemberMethod Process_MethodError_Info = null;

                Process_MethodError_Info = new CodeMemberMethod
                {
                    Name = "Process_MethodError_Info",
                    ReturnType = new CodeTypeReference(typeof(bool)),
                    Attributes = MemberAttributes.Private
                };

                CodeParameterDeclarationExpression pErrorDescription = new CodeParameterDeclarationExpression(new CodeTypeReference(typeof(String)), "szErrDesc");
                CodeParameterDeclarationExpression pErrorSource = new CodeParameterDeclarationExpression(new CodeTypeReference(typeof(String)), "szErrSource");
                CodeParameterDeclarationExpression pSPErrorId = new CodeParameterDeclarationExpression(new CodeTypeReference(typeof(long)), "SPErrorID");
                CodeParameterDeclarationExpression pInstance = new CodeParameterDeclarationExpression(new CodeTypeReference(typeof(long)), "lInstance");
                CodeParameterDeclarationExpression pMethodId = new CodeParameterDeclarationExpression(new CodeTypeReference(typeof(long)), "lMethodId");
                CodeParameterDeclarationExpression pPSSeqNo = new CodeParameterDeclarationExpression(new CodeTypeReference(typeof(long)), "lPSSeqNo");
                CodeParameterDeclarationExpression pBRSeqNo = new CodeParameterDeclarationExpression(new CodeTypeReference(typeof(long)), "lBRSeqNo");

                Process_MethodError_Info.Parameters.Add(pErrorDescription);
                Process_MethodError_Info.Parameters.Add(pErrorSource);
                Process_MethodError_Info.Parameters.Add(pSPErrorId);
                Process_MethodError_Info.Parameters.Add(pInstance);
                Process_MethodError_Info.Parameters.Add(pMethodId);
                Process_MethodError_Info.Parameters.Add(pPSSeqNo);
                Process_MethodError_Info.Parameters.Add(pBRSeqNo);

                Process_MethodError_Info.AddStatement(DeclareVariableAndAssign("CErrorHandler", "ehs", true, ObjectCreateExpression("CErrorHandler", new CodeExpression[] { MethodInvocationExp(TypeReferenceExp(typeof(Int32)), "Parse").AddParameter(ArrayIndexerExpression("nvcFW_CONTEXT", PrimitiveExpression("language"))) })));
                Process_MethodError_Info.AddStatement(DeclareVariableAndAssign(typeof(long), "lBRErrorId", true, PrimitiveExpression(0)));
                Process_MethodError_Info.AddStatement(DeclareVariableAndAssign(typeof(long), "lServerity", true, PrimitiveExpression(0)));
                Process_MethodError_Info.AddStatement(DeclareVariableAndAssign(typeof(String), "szErrorMsg", true, GetProperty(typeof(String), "Empty")));
                Process_MethodError_Info.AddStatement(DeclareVariableAndAssign(typeof(String), "szCorrectiveMsg", true, GetProperty(typeof(String), "Empty")));
                Process_MethodError_Info.AddStatement(DeclareVariableAndAssign(typeof(String), "szFocusSegName", true, GetProperty(typeof(String), "Empty")));
                Process_MethodError_Info.AddStatement(DeclareVariableAndAssign(typeof(String), "szFocusDI", true, GetProperty(typeof(String), "Empty")));
                Process_MethodError_Info.AddStatement(DeclareVariableAndAssign(typeof(int), "iStrPos", true, PrimitiveExpression(0)));
                Process_MethodError_Info.AddStatement(DeclareVariableAndAssign(typeof(int), "iEndPos", true, PrimitiveExpression(0)));
                Process_MethodError_Info.AddStatement(DeclareVariableAndAssign(typeof(String), "ErrDesc", true, GetProperty(typeof(String), "Empty")));
                Process_MethodError_Info.AddStatement(DeclareVariableAndAssign(typeof(String), "ErrNo", true, GetProperty(typeof(String), "Empty")));

                Process_MethodError_Info.AddStatement(MethodInvocationExp(BaseReferenceExp(), "WriteProfiler").AddParameter(PrimitiveExpression("Inside Process_MethodError_Info")));
                Process_MethodError_Info.AddStatement(DeclareVariableAndAssign(typeof(Hashtable), "PlaceHolderData", true, ObjectCreateExpression(typeof(Hashtable))));

                CodeTryCatchFinallyStatement tryBlock = AddTryBlock(Process_MethodError_Info);

                CodeConditionStatement ifErrSrcNotEqualToAppError = IfCondition();
                ifErrSrcNotEqualToAppError.Condition = BinaryOpertorExpression(VariableReferenceExp("szErrSource"), CodeBinaryOperatorType.IdentityInequality, FieldReferenceExp(TypeReferenceExp("CUtil"), "APP_ERROR"));
                tryBlock.AddStatement(ifErrSrcNotEqualToAppError);

                ifErrSrcNotEqualToAppError.TrueStatements.Add(MethodInvocationExp(BaseReferenceExp(), "WriteProfiler").AddParameter(MethodInvocationExp(TypeReferenceExp(typeof(string)), "Format").AddParameters(new CodeExpression[] { PrimitiveExpression("Error Message:{0}"), VariableReferenceExp("ErrDesc") })));
                ifErrSrcNotEqualToAppError.TrueStatements.Add(MethodInvocationExp(BaseReferenceExp(), "HandleUnknownError").AddParameters(new CodeExpression[] { VariableReferenceExp("szErrDesc"), SnippetExpression("ref ErrNo"), SnippetExpression("ref ErrDesc") }));

                CodeTryCatchFinallyStatement innerTryBlock = new CodeTryCatchFinallyStatement();
                ifErrSrcNotEqualToAppError.TrueStatements.Add(innerTryBlock);
                innerTryBlock.AddStatement(AssignVariable(VariableReferenceExp("SPErrorID"), MethodInvocationExp(TypeReferenceExp(typeof(Int64)), "Parse").AddParameter(MethodInvocationExp(VariableReferenceExp("ErrNo"), "Trim"))));

                CodeCatchClause innerCatchBlock = new CodeCatchClause();
                innerTryBlock.CatchClauses.Add(innerCatchBlock);
                innerCatchBlock.AddStatement(AssignVariable(VariableReferenceExp("szErrorMsg"), MethodInvocationExp(TypeReferenceExp("ehs"), "GetResourceInfo").AddParameter(PrimitiveExpression("non_num_err"))));
                innerCatchBlock.AddStatement(MethodInvocationExp(BaseReferenceExp(), "Set_Error_Info").AddParameters(new CodeExpression[] { PrimitiveExpression(0), VariableReferenceExp("szErrSource"), FieldReferenceExp(TypeReferenceExp("CUtil"), "STOP_PROCESSING"), VariableReferenceExp("szErrorMsg"), GetProperty(typeof(string), "Empty"), GetProperty(typeof(string), "Empty"), PrimitiveExpression(0), GetProperty(typeof(string), "Empty"), GetProperty(typeof(string), "Empty") }));
                innerCatchBlock.AddStatement(ReturnExpression(PrimitiveExpression(false)));

                CodeConditionStatement ifSPErrIdIsGreaterThanZero = IfCondition();
                ifSPErrIdIsGreaterThanZero.Condition = BinaryOpertorExpression(VariableReferenceExp("SPErrorID"), CodeBinaryOperatorType.GreaterThan, PrimitiveExpression(0));
                ifErrSrcNotEqualToAppError.TrueStatements.Add(ifSPErrIdIsGreaterThanZero);

                CodeTryCatchFinallyStatement innerTryBlock1 = new CodeTryCatchFinallyStatement();
                ifSPErrIdIsGreaterThanZero.TrueStatements.Add(innerTryBlock1);

                CodeConditionStatement checkMethodError = IfCondition();
                checkMethodError.Condition = BinaryOpertorExpression(MethodInvocationExp(BaseReferenceExp(), "Process_MethodError_Info").AddParameters(new CodeExpression[] { VariableReferenceExp("szErrDesc"),
                                                                                                                                                                            VariableReferenceExp("szErrSource"),
                                                                                                                                                                            VariableReferenceExp("SPErrorID"),
                                                                                                                                                                            VariableReferenceExp("lInstance"),
                                                                                                                                                                            VariableReferenceExp("lMethodId"),
                                                                                                                                                                            VariableReferenceExp("lPSSeqNo"),
                                                                                                                                                                            VariableReferenceExp("lBRSeqNo")
                                                                                                                                                                        }), CodeBinaryOperatorType.IdentityEquality, PrimitiveExpression(false));
                innerTryBlock1.AddStatement(checkMethodError);
                checkMethodError.TrueStatements.Add(MethodInvocationExp(TypeReferenceExp("ehs"), string.Format("EHS{0}", _service.Name)).AddParameters(new CodeExpression[] {VariableReferenceExp("lMethodId"),
                                                                                                                                                                                VariableReferenceExp("SPErrorID"),
                                                                                                                                                                                VariableReferenceExp("lInstance"),
                                                                                                                                                                                VariableReferenceExp("lPSSeqNo"),
                                                                                                                                                                                VariableReferenceExp("lBRSeqNo"),
                                                                                                                                                                                SnippetExpression("ref lBRErrorId"),
                                                                                                                                                                                SnippetExpression("ref szErrorMsg"),
                                                                                                                                                                                SnippetExpression("ref szCorrectiveMsg"),
                                                                                                                                                                                SnippetExpression("ref lServerity"),
                                                                                                                                                                                SnippetExpression("ref szFocusSegName"),
                                                                                                                                                                                SnippetExpression("ref szFocusDI"),
                                                                                                                                                                                SnippetExpression("ref PlaceHolderData")}));
                CodeConditionStatement ifPhCountGreaterThanZero = IfCondition();
                ifPhCountGreaterThanZero.Condition = BinaryOpertorExpression(GetProperty(TypeReferenceExp("PlaceHolderData"), "Count"), CodeBinaryOperatorType.GreaterThan, PrimitiveExpression(0));
                checkMethodError.TrueStatements.Add(ifPhCountGreaterThanZero);
                ifPhCountGreaterThanZero.TrueStatements.Add(MethodInvocationExp(ThisReferenceExp(), "FillPlaceHolderValue").AddParameters(new CodeExpression[] { SnippetExpression("ref PlaceHolderData"), VariableReferenceExp("lInstance") }));
                ifPhCountGreaterThanZero.TrueStatements.Add(MethodInvocationExp(TypeReferenceExp("ehs"), "ReplaceErrMsg").AddParameters(new CodeExpression[] { VariableReferenceExp("PlaceHolderData"), SnippetExpression("ref szErrorMsg") }));
                checkMethodError.TrueStatements.Add(MethodInvocationExp(BaseReferenceExp(), "Set_Error_Info").AddParameters(new CodeExpression[] { VariableReferenceExp("lBRErrorId"),
                                                                                                                                                FieldReferenceExp(TypeReferenceExp("CUtil"), "APP_ERROR"),
                                                                                                                                                MethodInvocationExp(TypeReferenceExp("lServerity"),"ToString"),
                                                                                                                                                VariableReferenceExp("szErrorMsg"),
                                                                                                                                                VariableReferenceExp("szFocusDI"),
                                                                                                                                                VariableReferenceExp("szFocusSegName"),
                                                                                                                                                VariableReferenceExp("lInstance"),
                                                                                                                                                VariableReferenceExp("szCorrectiveMsg"),
                                                                                                                                                PrimitiveExpression("0")
                                                                                                                                            }));

                CodeCatchClause catchCRVWException = innerTryBlock1.AddCatch("CRVWException", "rvwe");
                ThrowException(catchCRVWException, "rvwe");

                CodeCatchClause catchOtherException = innerTryBlock1.AddCatch("Exception", "e");
                CodeConditionStatement ifErrDescIsAvailable = IfCondition();

                ifErrDescIsAvailable.Condition = BinaryOpertorExpression(GetProperty("ErrDesc", "Length"), CodeBinaryOperatorType.GreaterThan, PrimitiveExpression(0));
                catchOtherException.AddStatement(ifErrDescIsAvailable);

                ifErrDescIsAvailable.TrueStatements.Add(AssignVariable(VariableReferenceExp("szErrorMsg"), VariableReferenceExp("ErrDesc")));
                ifErrDescIsAvailable.TrueStatements.Add(MethodInvocationExp(BaseReferenceExp(), "Set_Error_Info").AddParameters(new CodeExpression[] { VariableReferenceExp("SPErrorID"),
                                                                                                                                                       VariableReferenceExp("szErrSource"),
                                                                                                                                                        FieldReferenceExp(TypeReferenceExp("CUtil"),"STOP_PROCESSING"),
                                                                                                                                                        VariableReferenceExp("szErrorMsg"),
                                                                                                                                                        GetProperty(TypeReferenceExp(typeof(String)),"Empty"),
                                                                                                                                                        GetProperty(TypeReferenceExp(typeof(String)),"Empty"),
                                                                                                                                                        PrimitiveExpression(0),
                                                                                                                                                        GetProperty(TypeReferenceExp(typeof(String)),"Empty"),
                                                                                                                                                        GetProperty(TypeReferenceExp(typeof(String)),"Empty"),
                                                                                                                                                        }));
                ifErrDescIsAvailable.TrueStatements.Add(ReturnExpression(PrimitiveExpression(true)));

                ifErrDescIsAvailable.FalseStatements.Add(AssignVariable(VariableReferenceExp("szErrorMsg"), MethodInvocationExp(TypeReferenceExp("ehs"), "GetResourceInfo").AddParameter(PrimitiveExpression("err_desc_not_found"))));
                ifErrDescIsAvailable.FalseStatements.Add(MethodInvocationExp(BaseReferenceExp(), "Set_Error_Info").AddParameters(new CodeExpression[] { PrimitiveExpression(0),
                                                                                                                                                        VariableReferenceExp("szErrSource"),
                                                                                                                                                        FieldReferenceExp(TypeReferenceExp("CUtil"), "STOP_PROCESSING"),
                                                                                                                                                        MethodInvocationExp(TypeReferenceExp(typeof(String)),"Format").AddParameters( new CodeExpression[] { PrimitiveExpression("{0} {1}"),VariableReferenceExp("szErrorMsg"),GetProperty("e","Message") }),
                                                                                                                                                        GetProperty(TypeReferenceExp(typeof(String)),"Empty"),
                                                                                                                                                        GetProperty(TypeReferenceExp(typeof(String)),"Empty"),
                                                                                                                                                        PrimitiveExpression(0),
                                                                                                                                                        GetProperty(TypeReferenceExp(typeof(String)),"Empty"),
                                                                                                                                                        GetProperty(TypeReferenceExp(typeof(String)),"Empty")
                                                                                                                                                    }));
                ifErrDescIsAvailable.FalseStatements.Add(ReturnExpression(PrimitiveExpression(false)));

                ifSPErrIdIsGreaterThanZero.FalseStatements.Add(MethodInvocationExp(BaseReferenceExp(), "Set_Error_Info").AddParameters(new CodeExpression[] { VariableReferenceExp("SPErrorID"),
                                                                                                                                                               VariableReferenceExp("szErrSource"),
                                                                                                                                                               FieldReferenceExp(TypeReferenceExp("CUtil"),"STOP_PROCESSING"),
                                                                                                                                                               VariableReferenceExp("szErrDesc"),
                                                                                                                                                               GetProperty(TypeReferenceExp(typeof(String)),"Empty"),
                                                                                                                                                               GetProperty(TypeReferenceExp(typeof(String)),"Empty"),
                                                                                                                                                               PrimitiveExpression(0),
                                                                                                                                                               GetProperty(TypeReferenceExp(typeof(String)),"Empty"),
                                                                                                                                                               GetProperty(TypeReferenceExp(typeof(String)),"Empty")
                                                                                                                                                             }));
                ifSPErrIdIsGreaterThanZero.FalseStatements.Add(ReturnExpression(PrimitiveExpression(false)));

                CodeTryCatchFinallyStatement innerTry2 = new CodeTryCatchFinallyStatement();
                ifErrSrcNotEqualToAppError.FalseStatements.Add(innerTry2);

                CodeConditionStatement checkMethodError1 = IfCondition();
                checkMethodError1.Condition = BinaryOpertorExpression(MethodInvocationExp(BaseReferenceExp(), "Process_MethodError_Info").AddParameters(new CodeExpression[] { VariableReferenceExp("szErrDesc"),
                                                                                                                                                                                VariableReferenceExp("szErrSource"),
                                                                                                                                                                                VariableReferenceExp("SPErrorID"),
                                                                                                                                                                                VariableReferenceExp("lInstance"),
                                                                                                                                                                                VariableReferenceExp("lMethodId"),
                                                                                                                                                                                VariableReferenceExp("lPSSeqNo"),
                                                                                                                                                                                VariableReferenceExp("lBRSeqNo")
                                                                                                                                                                            }), CodeBinaryOperatorType.IdentityEquality, PrimitiveExpression(false));
                checkMethodError1.TrueStatements.Add(MethodInvocationExp(TypeReferenceExp("ehs"), string.Format("EHS{0}", _service.Name)).AddParameters(new CodeExpression[] { VariableReferenceExp("lMethodId"),
                                                                                                                                                                                    VariableReferenceExp("SPErrorID"),
                                                                                                                                                                                    VariableReferenceExp("lInstance"),
                                                                                                                                                                                    VariableReferenceExp("lPSSeqNo"),
                                                                                                                                                                                    VariableReferenceExp("lBRSeqNo"),
                                                                                                                                                                                    SnippetExpression("ref lBRErrorId"),
                                                                                                                                                                                    SnippetExpression("ref szErrorMsg"),
                                                                                                                                                                                    SnippetExpression("ref szCorrectiveMsg"),
                                                                                                                                                                                    SnippetExpression("ref lServerity"),
                                                                                                                                                                                    SnippetExpression("ref szFocusSegName"),
                                                                                                                                                                                    SnippetExpression("ref szFocusDI"),
                                                                                                                                                                                    SnippetExpression("ref PlaceHolderData")}));
                innerTry2.AddStatement(checkMethodError1);
                CodeConditionStatement checkPhCount = IfCondition();
                checkPhCount.Condition = BinaryOpertorExpression(GetProperty("PlaceHolderData", "Count"), CodeBinaryOperatorType.GreaterThan, PrimitiveExpression(0));
                checkMethodError1.TrueStatements.Add(checkPhCount);
                checkPhCount.TrueStatements.Add(MethodInvocationExp(ThisReferenceExp(), "FillPlaceHolderValue").AddParameters(new CodeExpression[] { SnippetExpression("ref PlaceHolderData"), VariableReferenceExp("lInstance") }));
                checkPhCount.TrueStatements.Add(MethodInvocationExp(TypeReferenceExp("ehs"), "ReplaceErrMsg").AddParameters(new CodeExpression[] { VariableReferenceExp("PlaceHolderData"), SnippetExpression("ref szErrorMsg") }));
                checkMethodError1.TrueStatements.Add(MethodInvocationExp(BaseReferenceExp(), "Set_Error_Info").AddParameters(new CodeExpression[] { VariableReferenceExp("lBRErrorId"),
                                                                                                                                                    FieldReferenceExp(TypeReferenceExp("CUtil"),"APP_ERROR"),
                                                                                                                                                    MethodInvocationExp(VariableReferenceExp("lServerity"),"ToString"),
                                                                                                                                                    VariableReferenceExp("szErrorMsg"),
                                                                                                                                                    VariableReferenceExp("szFocusDI"),
                                                                                                                                                    VariableReferenceExp("szFocusSegName"),
                                                                                                                                                    VariableReferenceExp("lInstance"),
                                                                                                                                                    VariableReferenceExp("szCorrectiveMsg"),
                                                                                                                                                    PrimitiveExpression("0")
                                                                                                                                                }));
                CodeCatchClause catchCRVWException1 = innerTry2.AddCatch("CRVWException", "rvwe");
                ThrowException(catchCRVWException1, "rvwe");

                CodeCatchClause catchOtherException1 = innerTry2.AddCatch("Exception", "e");
                catchOtherException1.AddStatement(AssignVariable(VariableReferenceExp("szErrorMsg"), MethodInvocationExp(TypeReferenceExp("ehs"), "GetResourceInfo").AddParameter(PrimitiveExpression("err_desc_not_found"))));
                catchOtherException1.AddStatement(MethodInvocationExp(BaseReferenceExp(), "Set_Error_Info").AddParameters(new CodeExpression[] { PrimitiveExpression(0),
                    VariableReferenceExp("szErrSource"),
                    FieldReferenceExp(TypeReferenceExp("CUtil"), "STOP_PROCESSING"),
                    MethodInvocationExp(TypeReferenceExp(typeof(String)),"Format").AddParameters(new CodeExpression[] { PrimitiveExpression("{0} {1}"), VariableReferenceExp("szErrorMsg"), GetProperty("e", "Message") }),
                    GetProperty(TypeReferenceExp(typeof(String)),"Empty"),
                    GetProperty(TypeReferenceExp(typeof(String)),"Empty"),
                    PrimitiveExpression(0),
                    GetProperty(TypeReferenceExp(typeof(String)),"Empty"),
                    GetProperty(TypeReferenceExp(typeof(String)),"Empty")
                }));
                catchOtherException1.AddStatement(ReturnExpression(PrimitiveExpression(false)));

                tryBlock.AddStatement(ReturnExpression(PrimitiveExpression(true)));

                CodeCatchClause catchCRVWException2 = tryBlock.AddCatch("CRVWException", "rvwe");
                ThrowException(catchCRVWException2, "rvwe");

                CodeCatchClause catchOtherException2 = tryBlock.AddCatch("Exception", "e");
                catchOtherException2.AddStatement(MethodInvocationExp(BaseReferenceExp(), "WriteProfiler").AddParameters(new CodeExpression[] { MethodInvocationExp(TypeReferenceExp(typeof(String)), "Format").AddParameters(new CodeExpression[] { PrimitiveExpression("General Exception in Process_MethodError Info - {0}"), GetProperty("e", "Message") }) }));
                catchOtherException2.AddStatement(MethodInvocationExp(BaseReferenceExp(), "Set_Error_Info").AddParameters(new CodeExpression[] {PrimitiveExpression(0),
                    FieldReferenceExp(TypeReferenceExp("CUtil"), "FRAMEWORK_ERROR"),
                    FieldReferenceExp(TypeReferenceExp("CUtil"), "STOP_PROCESSING"),
                    MethodInvocationExp(TypeReferenceExp(typeof(String)),"Format").AddParameters(new CodeExpression[] { PrimitiveExpression("General Exception in Process_MethodError Info - {0}"),GetProperty("e","Message") }),
                    GetProperty(TypeReferenceExp(typeof(String)),"Empty"),
                    GetProperty(TypeReferenceExp(typeof(String)),"Empty"),
                    PrimitiveExpression(0),
                    GetProperty(TypeReferenceExp(typeof(String)),"Empty"),
                    GetProperty(TypeReferenceExp(typeof(String)),"Empty")
                }));
                catchOtherException2.AddStatement(ReturnExpression(PrimitiveExpression(false)));


                return Process_MethodError_Info;
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("Process_MethodError_Info->{0}", ex.InnerException != null ? ex.InnerException.Message : ex.Message));
            }
        }

        private void ProcessService_Finally(String sContext, CodeTryCatchFinallyStatement tryBlock, CodeVariableDeclarationStatement vCallRB,
            CodeParameterDeclarationExpression pINMtd, CodeParameterDeclarationExpression pOutMtd)
        {
            try
            {
                tryBlock.FinallyStatements.Add(MethodInvocationExp(BaseReferenceExp(), "WriteProfiler").AddParameters(new CodeExpression[] { PrimitiveExpression("Before calling post process") }));

                CodeTryCatchFinallyStatement tryWithinFinally = new CodeTryCatchFinallyStatement();
                tryWithinFinally.AddStatement(AssignVariable(ArgumentReferenceExp(pOutMtd.Name), GetProperty(typeof(String), "Empty")));



                //main service
                if (!_service.HasRollbackProcessSections && !_service.HasCommitProcessSection)
                {
                    CodeConditionStatement IfIsIntegIs = IfCondition();
                    tryWithinFinally.AddStatement(AssignVariable("bServicePostResult", MethodInvocationExp(BaseReferenceExp(), "Service_Post_Process")));
                    AddProcessServicebIsInteg(tryWithinFinally, ref IfIsIntegIs, null, pOutMtd, "GetBOD");

                    //IfIsIntegIs.Condition = BinaryOpertorExpression(VariableReferenceExp("bIsInteg"),CodeBinaryOperatorType.IdentityEquality,PrimitiveExpression(false));
                    //IfIsIntegIs.TrueStatements.Add(AssignVariable(VariableReferenceExp("szOutMtd"),MethodInvocationExp(ThisReferenceExp(), "GetBOD").AddParameter(MethodInvocationExp(ThisReferenceExp(), "Service_Post_Process"))));
                    //tryWithinFinally.AddStatement(IfIsIntegIs);

                    if (_service.HasRollbackProcessSections)
                    {
                        CodeConditionStatement IfbServicePostResult = IfCondition();
                        IfbServicePostResult.Condition = BinaryOpertorExpression(VariableReferenceExp("bServicePostResult"), CodeBinaryOperatorType.IdentityEquality, PrimitiveExpression(false));
                        IfbServicePostResult.TrueStatements.Add(AssignVariable(FieldReferenceExp(BaseReferenceExp(), "bCallRB"), PrimitiveExpression(true)));
                    }
                }
                //service having rollback or commit process sections
                else if (_service.HasRollbackProcessSections)
                {
                    tryWithinFinally.AddStatement(AssignVariable("nvcFW_CONTEXT", MethodInvocationExp(GetProperty(TypeReferenceExp(typeof(DateTime)), "Now"), "ToString"), -1, true, "txntime"));
                    tryWithinFinally.AddStatement(AssignVariable(ArgumentReferenceExp(pOutMtd.Name), MethodInvocationExp(ThisReferenceExp(), "GetBOD").AddParameters(new CodeExpression[] { PrimitiveExpression(false) })));

                    CodeConditionStatement ifCallRBIsTrue = IfCondition();
                    ifCallRBIsTrue.Condition = BinaryOpertorExpression(VariableReferenceExp(vCallRB.Name), CodeBinaryOperatorType.IdentityEquality, PrimitiveExpression(true));
                    ifCallRBIsTrue.TrueStatements.Add(InitializeMemberField("Cmtdstore", "objMTD"));
                    ifCallRBIsTrue.TrueStatements.Add(MethodInvocationExp(TypeReferenceExp("objMTD"), "MTARefreshBOD"));
                    ifCallRBIsTrue.TrueStatements.Add(MethodInvocationExp(TypeReferenceExp("objMTD"), "MTAUnpackBOD").AddParameters(new CodeExpression[] { ArgumentReferenceExp(pINMtd.Name) }));
                    ifCallRBIsTrue.TrueStatements.Add(MethodInvocationExp(TypeReferenceExp("objMTD"), "MTAAppendFragment").AddParameters(new CodeExpression[] { ArgumentReferenceExp(pOutMtd.Name), PrimitiveExpression("fw_context") }));
                    ifCallRBIsTrue.TrueStatements.Add(MethodInvocationExp(TypeReferenceExp("objMTD"), "MTAAppendFragment").AddParameters(new CodeExpression[] { ArgumentReferenceExp(pOutMtd.Name), PrimitiveExpression("errordetails") }));
                    ifCallRBIsTrue.TrueStatements.Add(AssignVariable(FieldReferenceExp(ThisReferenceExp(), "szRBInMtd"), MethodInvocationExp(TypeReferenceExp("objMTD"), "MTAGetBOD").AddParameters(new CodeExpression[] { PrimitiveExpression(true) })));
                    ifCallRBIsTrue.TrueStatements.Add(AssignVariable(TypeReferenceExp("objMTD"), SnippetExpression("null")));
                    ifCallRBIsTrue.TrueStatements.Add(InitializeMemberField(String.Format("{0}_rb", _ecrOptions.Component.ToLowerInvariant()), String.Format("C{0}_rb", _ecrOptions.Component.ToLower())));
                    ifCallRBIsTrue.TrueStatements.Add(MethodInvocationExp(FieldReferenceExp(ThisReferenceExp(), String.Format("{0}_rb", _ecrOptions.Component.ToLowerInvariant())), "ProcessDocument").AddParameters(new CodeExpression[] { FieldReferenceExp(ThisReferenceExp(), "szRBInMtd"), PrimitiveExpression(_service.Name), }));
                    ifCallRBIsTrue.TrueStatements.Add(MethodInvocationExp(TypeReferenceExp((String.Format("((Type){0}_rb.GetType())", _ecrOptions.Component.ToLowerInvariant()))), "InvokeMember").AddParameters(new CodeExpression[] { PrimitiveExpression("Dispose"), GetProperty(typeof(System.Reflection.BindingFlags), "InvokeMethod"), FieldReferenceExp(ThisReferenceExp(), String.Format("{0}_rb", _ecrOptions.Component.ToLowerInvariant())), SnippetExpression("null") }));
                    ifCallRBIsTrue.TrueStatements.Add(AssignVariable(FieldReferenceExp(ThisReferenceExp(), String.Format("{0}_rb", _ecrOptions.Component.ToLower())), SnippetExpression("null")));
                    tryWithinFinally.AddStatement(ifCallRBIsTrue);
                }

                CodeCatchClause catchWithinFinally = tryWithinFinally.AddCatch();
                catchWithinFinally.AddStatement(AssignVariable(ArgumentReferenceExp(pOutMtd.Name), GetProperty(typeof(String), "Empty")));
                WriteProfiler(sContext, null, catchWithinFinally, null, "EXEMPTY");

                tryBlock.FinallyStatements.Add(tryWithinFinally);
                tryBlock.FinallyStatements.Add(MethodInvocationExp(BaseReferenceExp(), "WriteProfiler").AddParameter(PrimitiveExpression("Before exit of finally")));
                WriteProfiler(sContext, tryBlock, null, null, "FN");
                tryBlock.FinallyStatements.Add(MethodInvocationExp(FieldReferenceExp(BaseReferenceExp(), "Out"), "Dispose"));
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("ProcessService_Finally->{0}", !Object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }

        private void ProcessService_Exception(String sContext, CodeTryCatchFinallyStatement tryBlock, CodeVariableDeclarationStatement vCallRB)
        {
            try
            {
                CodeCatchClause CatchException = tryBlock.AddCatch();
                CodeTryCatchFinallyStatement innerTry = new CodeTryCatchFinallyStatement();

                Set_Error_Info(sContext, innerTry, null, null);
                CodeCatchClause innerCatch = new CodeCatchClause();
                WriteProfiler(sContext, null, innerCatch, null, "EX");
                innerTry.CatchClauses.Add(innerCatch);

                if (_service.HasRollbackProcessSections)
                    CatchException.AddStatement(AssignVariable(VariableReferenceExp(vCallRB.Name), PrimitiveExpression(true)));

                CatchException.AddStatement(innerTry);
                CatchException.AddStatement(ReturnExpression(VariableReferenceExp("ATMA_FAILURE")));
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("ProcessService_Exception->{0}", !Object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }

        private void ProcessService_RVWException(String sContext, CodeTryCatchFinallyStatement tryBlock, CodeVariableDeclarationStatement vCallRB)
        {
            try
            {
                CodeCatchClause CatchCRVWException = tryBlock.AddCatch("CRVWException", "rvwe");
                WriteProfiler(sContext, null, CatchCRVWException, null, "RvwEX");

                if (_service.HasRollbackProcessSections)
                    CatchCRVWException.AddStatement(AssignVariable(VariableReferenceExp(vCallRB.Name), PrimitiveExpression(true)));

                //CatchCRVWException.AddStatement(ReturnExpression(FieldReferenceExp(BaseReferenceExp(), "ATMA_FAILURE")));
                CatchCRVWException.AddStatement(ReturnExpression(VariableReferenceExp("ATMA_FAILURE")));
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("ProcessService_RVWException->{0}", !Object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }


        private void AddProcessServicebIsInteg(CodeTryCatchFinallyStatement tryBlock, ref CodeConditionStatement ifCondition, CodeParameterDeclarationExpression pINMtd,
            CodeParameterDeclarationExpression pOutMtd, String CalledMethod)
        {
            try
            {
                ifCondition.Condition = BinaryOpertorExpression(FieldReferenceExp(BaseReferenceExp(), "bIsInteg"), CodeBinaryOperatorType.IdentityEquality, PrimitiveExpression(false));

                switch (CalledMethod)
                {
                    case "unpackBOD":
                        ifCondition.TrueStatements.Add(MethodInvocationExp(BaseReferenceExp(), "unpackBOD").AddParameters(new CodeExpression[] { ArgumentReferenceExp(pINMtd.Name) }));
                        break;
                    case "FillUnMappedDataItems":
                        ifCondition.TrueStatements.Add(MethodInvocationExp(ThisReferenceExp(), "FillUnMappedDataItems"));
                        break;
                    case "GetBOD":
                        ifCondition.TrueStatements.Add(AssignVariable(ArgumentReferenceExp(pOutMtd.Name), MethodInvocationExp(ThisReferenceExp(), "GetBOD").AddParameters(new CodeExpression[] { ArgumentReferenceExp("bServicePostResult") })));
                        break;
                }

                tryBlock.AddStatement(ifCondition);
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("AddProcessServicebIsInteg->{0}", !Object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }

        /// <summary>
        /// Parameter addition for methods with method_exec_cont flag
        /// </summary>
        /// <param name="ps"></param>
        /// <param name="mt"></param>
        /// <returns></returns>
        private CodeStatementCollection PS_AddInParameters_With_MethodExecCont(ProcessSection ps, Method mt)
        {
            CodeStatementCollection statements;
            try
            {
                statements = new CodeStatementCollection();
                IEnumerable<Parameter> inParameters = mt.Parameters.Where(p => p.Seg != null
                                                                    && (p.FlowDirection == FlowAttribute.IN || p.FlowDirection == FlowAttribute.INOUT))
                                                         .OrderBy(p => Convert.ToInt16(p.SequenceNo));
                foreach (Parameter inParameter in inParameters)
                {
                    string sParamName = inParameter.Name.ToLower();
                    string sDBType = string.Empty;
                    int iParamLength = int.Parse(inParameter.Length);
                    string sSegName = inParameter.Seg.Name.ToLower();
                    string sDIName = inParameter.DI.Name.ToLower();

                    switch (inParameter.CategorizedDataType)
                    {
                        case DataType.INT:
                            sDBType = "Int";
                            break;
                        case DataType.STRING:
                            sDBType = "NVarchar";
                            break;
                        case DataType.DOUBLE:
                            sDBType = "Numeric";
                            break;
                        case DataType.VARBINARY:
                            sDBType = "FileBinary";
                            break;
                    }

                    statements.Add(MethodInvocationExp(BaseReferenceExp(), "PrepareMethodParameters").AddParameters(new CodeExpression[] { PrimitiveExpression(inParameter.Name.ToLower()),
                                                                                                                                           FieldReferenceExp(TypeReferenceExp("DBType"),sDBType),
                                                                                                                                           PrimitiveExpression(iParamLength),
                                                                                                                                           PrimitiveExpression(sSegName),
                                                                                                                                           PrimitiveExpression(sDIName)
                                                                                                                                            }));
                }
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("PS_AddInParameters_With_MethodExecCont->{0}", ex.InnerException != null ? ex.InnerException.Message : ex.Message));
            }

            return statements;
        }

        /// <summary>
        /// Method to generate method parameters
        /// </summary>
        /// <param name="ps"></param>
        /// <param name="mt"></param>
        /// <returns></returns>
        private CodeStatementCollection PS_AddInParameters(ProcessSection ps, Method mt)
        {
            CodeStatementCollection statements;
            CodeMethodInvokeExpression methodInvocation;

            statements = new CodeStatementCollection();
            try
            {
                CodeAssignStatement statement;
                Segment loopSegment = null;

                if (!string.IsNullOrEmpty(ps.LoopCausingSegment))
                {
                    loopSegment = _service.Segments.Where(s => s.Name == ps.LoopCausingSegment).First();
                }

                #region  Handcoded/custom br
                if (mt.SystemGenerated == BRTypes.CUSTOM_BR || mt.SystemGenerated == BRTypes.HANDCODED_BR || mt.SystemGenerated == BRTypes.BULK_BR)
                {
                    foreach (Parameter param in mt.Parameters.Where(p => p.Seg != null
                                                                        && string.Compare(p.Seg.Name, CONTEXT_SEGMENT, true) != 0
                                                                        && (p.FlowDirection == FlowAttribute.IN || p.FlowDirection == FlowAttribute.INOUT)).OrderBy(p => Convert.ToInt16(p.SequenceNo)))
                    {
                        switch (param.CategorizedDataType)
                        {
                            case DataType.INT:
                                statements.Add(AssignField(VariableReferenceExp(string.Format("sz{0}{1}", param.Seg.Name.ToLower(), param.DI.Name.ToLower())), SnippetExpression(string.Format("(string) nvc{0}[\"{1}\"]", param.Seg.Inst == "0" ? param.Seg.Name.ToUpper() : "Tmp", param.DI.Name.ToLower()))));
                                CodeConditionStatement chkIntValueForNull = IfCondition();
                                chkIntValueForNull.Condition = BinaryOpertorExpression(MethodInvocationExp(TypeReferenceExp(typeof(string)), "IsNullOrEmpty").AddParameter(VariableReferenceExp(string.Format("sz{0}{1}", param.Seg.Name.ToLower(), param.DI.Name.ToLower()))), CodeBinaryOperatorType.IdentityEquality, PrimitiveExpression(false));
                                chkIntValueForNull.TrueStatements.Add(AssignVariable(VariableReferenceExp(string.Format("l{0}{1}", param.Seg.Name.ToLower(), param.DI.Name.ToLower())), MethodInvocationExp(TypeReferenceExp(typeof(Int64)), "Parse").AddParameter(VariableReferenceExp(string.Format("sz{0}{1}", param.Seg.Name.ToLower(), param.DI.Name.ToLower())))));
                                statements.Add(chkIntValueForNull);
                                break;
                            case DataType.STRING:
                                statements.Add(AssignField(VariableReferenceExp(string.Format("sz{0}{1}", param.Seg.Name.ToLower(), param.DI.Name.ToLower())), SnippetExpression(string.Format("(string) nvc{0}[\"{1}\"]", param.Seg.Inst == "0" ? param.Seg.Name.ToUpper() : "Tmp", param.DI.Name.ToLower()))));
                                break;
                            case DataType.DOUBLE:
                                statements.Add(AssignField(VariableReferenceExp(string.Format("sz{0}{1}", param.Seg.Name.ToLower(), param.DI.Name.ToLower())), SnippetExpression(string.Format("(string) nvc{0}[\"{1}\"]", param.Seg.Inst == "0" ? param.Seg.Name.ToUpper() : "Tmp", param.DI.Name.ToLower()))));
                                CodeConditionStatement chkDoubleValForNull = IfCondition();
                                chkDoubleValForNull.Condition = BinaryOpertorExpression(MethodInvocationExp(TypeReferenceExp(typeof(string)), "IsNullOrEmpty").AddParameter(VariableReferenceExp(string.Format("sz{0}{1}", param.Seg.Name.ToLower(), param.DI.Name.ToLower()))), CodeBinaryOperatorType.IdentityEquality, PrimitiveExpression(false));
                                chkDoubleValForNull.TrueStatements.Add(AssignVariable(VariableReferenceExp(string.Format("dbl{0}{1}", param.Seg.Name.ToLower(), param.DI.Name.ToLower())), MethodInvocationExp(TypeReferenceExp(typeof(Double)), "Parse").AddParameter(VariableReferenceExp(string.Format("sz{0}{1}", param.Seg.Name.ToLower(), param.DI.Name.ToLower())))));
                                statements.Add(chkDoubleValForNull);
                                break;
                        }
                    }
                    statements.Add(CommentStatement("Execute the Method"));
                }
                #endregion

                #region normal method
                else
                {
                    statements.Add(CommentStatement("Execute the Method"));
                    CodeConditionStatement CheckForBRExecution = IfCondition();
                    CheckForBRExecution.Condition = BinaryOpertorExpression(
                                                                            BinaryOpertorExpression(VariableReferenceExp("iEDKServiceES"),
                                                                            CodeBinaryOperatorType.BooleanAnd,
                                                                            BinaryOpertorExpression(VariableReferenceExp("psIndex"), CodeBinaryOperatorType.IdentityInequality, PrimitiveExpression(-1))),
                                                                            CodeBinaryOperatorType.BooleanAnd,
                                                                            BinaryOpertorExpression(VariableReferenceExp("brIndex"), CodeBinaryOperatorType.IdentityInequality, PrimitiveExpression(-1))
                                                                            );
                    statement = AssignVariable("bBRExec", PrimitiveExpression(true));

                    if (ps.ProcessingType == ProcessingType.ALTERNATE)
                        statements.Add(statement);
                    else
                        CheckForBRExecution.TrueStatements.Add(statement);
                    CheckForBRExecution.TrueStatements.Add(MethodInvocationExp(BaseReferenceExp(), "EvaluateBLForBR").AddParameters(new CodeExpression[] { VariableReferenceExp("psIndex"), VariableReferenceExp("brIndex"), VariableReferenceExp("nLoop"), SnippetExpression("out bBRExec") }));

                    statements.Add(CheckForBRExecution);

                    if (ps.ProcessingType == ProcessingType.DEFAULT)
                    {
                        CodeConditionStatement CheckBRExecFlag = IfCondition();
                        CheckBRExecFlag.Condition = BinaryOpertorExpression(VariableReferenceExp("bBRExec"), CodeBinaryOperatorType.IdentityEquality, PrimitiveExpression(false));
                        CheckForBRExecution.TrueStatements.Add(CheckBRExecFlag);
                        CheckBRExecFlag.TrueStatements.Add(SnippetExpression("continue"));
                    }


                    CodeTryCatchFinallyStatement tryBlockForParams = new CodeTryCatchFinallyStatement();
                    CodeCatchClause catchBlockForParams = tryBlockForParams.AddCatch();
                    catchBlockForParams.AddStatement(MethodInvocationExp(BaseReferenceExp(), "Set_Error_Info").AddParameters(new CodeExpression[] {
                    PrimitiveExpression(0),
                    GetProperty("e","Source"),
                    //FieldReferenceExp(TypeReferenceExp("CUtil"),"STOP_PROCESSING"),
                    VariableReferenceExp("STOP_PROCESSING"),
                    MethodInvocationExp( TypeReferenceExp(typeof(string)),"Format").AddParameters(new CodeExpression[]{PrimitiveExpression("General Exception during IN Parameter Binding of PS - "+ps.SeqNO+" BR - "+mt.SeqNO+ " {0}"),GetProperty("e","Message") }),
                    GetProperty("String","Empty"),
                    GetProperty("String","Empty"),
                    PrimitiveExpression(0),
                    GetProperty("String","Empty"),
                    GetProperty("String","Empty")
                }));


                    #region parameter addition
                    if (this._service.Implement_New_Method_Of_ParamAddition)
                    {
                        tryBlockForParams.AddStatement(MethodInvocationExp(BaseReferenceExp(), "GetMethodParameterValue").AddParameter(VariableReferenceExp("nLoop")));
                    }
                    else
                        foreach (Parameter param in mt.Parameters.Where(p => string.Compare(p.MethodSequenceNo, mt.SeqNO, true) == 0).Where(p => p.Seg != null && (p.FlowDirection == FlowAttribute.IN || p.FlowDirection == FlowAttribute.INOUT)))
                        {
                            string sCollectionName = string.Empty;
                            var ContextParam = _fw_context_parameters.AsEnumerable().Where(r => r.Field<string>("parametername").ToLower() == "@ctxt_" + param.DI.Name.ToLower());
                            if (string.Compare(param.Seg.Name, "fw_context", true) == 0 && ContextParam.Any())
                            {
                                DataRow drContextInfo = ContextParam.First();

                                methodInvocation = MethodInvocationExp(BaseReferenceExp(), "Parameters");
                                //methodInvocation.AddParameters(new CodeExpression[] { PrimitiveExpression(drContextInfo["ParameterName"].ToString().ToLower()) });
                                methodInvocation.AddParameters(new CodeExpression[] { PrimitiveExpression("@" + param.Name) });
                                methodInvocation.AddParameter(VariableReferenceExp(drContextInfo["dbtype"].ToString()));
                                methodInvocation.AddParameter(PrimitiveExpression(drContextInfo["dbtype"].ToString().Equals("DBType.Int") ? int.Parse(drContextInfo["length"].ToString()) : int.Parse(param.Length)));
                                //methodInvocation.AddParameter(PrimitiveExpression(drContextInfo["Length"]));
                                methodInvocation.AddParameter(VariableReferenceExp(drContextInfo["parametervalue"].ToString()));
                                tryBlockForParams.AddStatement(methodInvocation);
                            }
                            else
                            {
                                if (param.Seg.Inst == MULTI_INSTANCE)
                                {
                                    sCollectionName = "nvcTmp";
                                }
                                else
                                {
                                    sCollectionName = String.Format("nvc{0}", param.Seg.Name.ToUpperInvariant());
                                }

                                if (loopSegment != null && param.Name.Equals("sysfprowno"))
                                {
                                    if (loopSegment.Process_sel_rows == "sy" || loopSegment.Process_upd_rows == "sy" || loopSegment.Process_sel_upd_rows == "sy")
                                    {
                                        tryBlockForParams.AddStatement(AssignVariable(VariableReferenceExp("sValue"), MethodInvocationExp(VariableReferenceExp("nLoop"), "ToString")));
                                        tryBlockForParams.AddStatement(AssignVariable(ArrayIndexerExpression(sCollectionName, PrimitiveExpression(param.Name)), MethodInvocationExp(VariableReferenceExp("nLoop"), "ToString")));
                                        //tryBlockForParams.AddStatement(CommentStatement("Code Added for Backward Compatibility with DNA. Model Related Issues , old value to be retained"));
                                    }
                                    else
                                    {
                                        tryBlockForParams.AddStatement(AssignVariable(VariableReferenceExp("sValue"), ArrayIndexerExpression(sCollectionName, PrimitiveExpression(param.DI.Name.ToLower()))));
                                    }
                                }
                                else
                                {
                                    tryBlockForParams.AddStatement(AssignVariable(VariableReferenceExp("sValue"), ArrayIndexerExpression(sCollectionName, PrimitiveExpression(param.DI.Name.ToLower()))));
                                }
                                methodInvocation = MethodInvocationExp(BaseReferenceExp(), "Parameters");
                                methodInvocation.AddParameters(new CodeExpression[] { PrimitiveExpression("@" + param.Name.ToLower()) });



                                string paramType = string.Empty;
                                int paramLength = 0;
                                int paramScale = 0;
                                string paramValue = "sValue";
                                CodeExpression valueExpression = null;
                                switch (param.CategorizedDataType)
                                {
                                    case DataType.INT:
                                        paramType = "Int";
                                        paramLength = 32;
                                        paramValue = "s" + param.Seg.Name.ToUpperInvariant() + param.DI.Name.ToLowerInvariant();
                                        if (_ecrOptions.TreatDefaultAsNull)
                                        {
                                            paramValue = "iValue";

                                            CodeConditionStatement ifValueIsNotNull = IfCondition();
                                            ifValueIsNotNull.Condition = SnippetExpression("!String.IsNullOrEmpty(sValue)");

                                            if (param.Seg.Inst == MULTI_INSTANCE)
                                                ifValueIsNotNull.TrueStatements.Add(AssignVariable(VariableReferenceExp(paramValue), MethodInvocationExp(TypeReferenceExp(typeof(Int32)), "Parse").AddParameter(VariableReferenceExp("s" + param.Seg.Name.ToUpperInvariant() + param.DI.Name.ToLowerInvariant()))));
                                            else
                                                ifValueIsNotNull.TrueStatements.Add(AssignVariable(VariableReferenceExp(paramValue), MethodInvocationExp(TypeReferenceExp(typeof(Int32)), "Parse").AddParameter(ArrayIndexerExpression("nvc" + param.Seg.Name.ToUpperInvariant(), PrimitiveExpression(param.DI.Name.ToLowerInvariant())))));

                                            tryBlockForParams.AddStatement(ifValueIsNotNull);

                                            CodeConditionStatement ifValueEqualsDefault = IfCondition();
                                            ifValueEqualsDefault.Condition = BinaryOpertorExpression(MethodInvocationExp(TypeReferenceExp(typeof(String)), "CompareOrdinal").AddParameters(new CodeExpression[] { MethodInvocationExp(VariableReferenceExp(paramValue), "ToString"), PrimitiveExpression("-915") }),
                                                                                                        CodeBinaryOperatorType.IdentityEquality,
                                                                                                        PrimitiveExpression(0));
                                            ifValueEqualsDefault.TrueStatements.Add(AssignVariable(VariableReferenceExp(paramValue), SnippetExpression("null")));
                                            tryBlockForParams.AddStatement(ifValueEqualsDefault);

                                        }
                                        else
                                        {
                                            tryBlockForParams.AddStatement(CommentStatement("Code Added for Backward Compatibility with DNA. Model Related Issues , old value to be retained"));
                                            tryBlockForParams.AddStatement(AssignVariable(VariableReferenceExp(paramValue), SnippetExpression("(Double.TryParse(sValue, out result) == true ? sValue : " + "s" + param.Seg.Name.ToUpperInvariant() + param.DI.Name.ToLowerInvariant() + ")")));
                                        }
                                        valueExpression = VariableReferenceExp(paramValue);
                                        break;
                                    case DataType.STRING:
                                        //if (param.DataType.Equals(DataType.DATE) ||
                                        //    param.DataType.Equals(DataType.TIME) ||
                                        //    param.DataType.Equals(DataType.DATE_TIME) ||
                                        //    param.DataType.Equals(DataType.DATETIME) ||
                                        //    param.DataType.Equals(DataType.TIMESTAMP))
                                        //{
                                        //    paramType = "NVarchar";
                                        //    paramLength = 11;
                                        //}
                                        //else
                                        //{
                                        paramType = "NVarchar";
                                        paramLength = int.Parse(param.Length);
                                        //}
                                        if (_ecrOptions.TreatDefaultAsNull)
                                        {
                                            if (param.DataType.Equals(DataType.DATE) ||
                                            param.DataType.Equals(DataType.TIME) ||
                                            param.DataType.Equals(DataType.DATE_TIME) ||
                                            param.DataType.Equals(DataType.DATETIME) ||
                                            param.DataType.Equals(DataType.TIMESTAMP))
                                            {
                                                paramValue = "dateValue";

                                                //if (param.Seg.Inst == MULTI_INSTANCE)
                                                //    tryBlockForParams.AddStatement(AssignVariable(VariableReferenceExp(paramValue), MethodInvocationExp(TypeReferenceExp("Convert"), "ToDateTime").AddParameter(VariableReferenceExp("s" + param.Seg.Name.ToUpperInvariant() + param.DI.Name.ToLowerInvariant()))));
                                                //else

                                                tryBlockForParams.AddStatement(AssignVariable(VariableReferenceExp("dateValue"), SnippetExpression("null")));

                                                CodeConditionStatement ifValueIsNotNull = IfCondition();
                                                ifValueIsNotNull.Condition = SnippetExpression("!String.IsNullOrEmpty(sValue)");
                                                ifValueIsNotNull.TrueStatements.Add(AssignVariable(VariableReferenceExp(paramValue), MethodInvocationExp(TypeReferenceExp("Convert"), "ToDateTime").AddParameter(VariableReferenceExp("sValue"))));

                                                CodeConditionStatement ifValueEqualsDefault = IfCondition();
                                                //ifValueEqualsDefault.Condition = BinaryOpertorExpression(MethodInvocationExp(TypeReferenceExp(typeof(String)), "Compare").AddParameters(new CodeExpression[] { MethodInvocationExp( FieldReferenceExp( VariableReferenceExp(paramValue),"Value"), "ToString").AddParameter(PrimitiveExpression("MM/dd/yyyy hh:mm:ss tt")), PrimitiveExpression("01/01/1900 12:00:00 AM") }),
                                                //                                                            CodeBinaryOperatorType.IdentityEquality,
                                                //                                                            PrimitiveExpression(0));

                                                ifValueEqualsDefault.Condition = BinaryOpertorExpression(SnippetExpression(string.Format("Convert.ToDateTime({0})", paramValue)),
                                                            CodeBinaryOperatorType.IdentityEquality,
                                                            SnippetExpression("Convert.ToDateTime(\"01/01/1900 12:00:00 AM\")"));

                                                ifValueEqualsDefault.TrueStatements.Add(AssignVariable(VariableReferenceExp(paramValue), SnippetExpression("null")));
                                                ifValueIsNotNull.TrueStatements.Add(ifValueEqualsDefault);

                                                tryBlockForParams.AddStatement(ifValueIsNotNull);

                                                valueExpression = SnippetExpression("dateValue==null?null:dateValue.Value.ToString(\"MM/dd/yyyy hh:mm:ss tt\")");
                                            }
                                            else
                                            {
                                                tryBlockForParams.AddStatement(AssignVariable(VariableReferenceExp(paramValue), MethodInvocationExp(VariableReferenceExp(paramValue), "Trim")));
                                                CodeConditionStatement ifValueEqualsDefault = IfCondition();
                                                ifValueEqualsDefault.Condition = BinaryOpertorExpression(MethodInvocationExp(TypeReferenceExp(typeof(String)), "CompareOrdinal").AddParameters(new CodeExpression[] { MethodInvocationExp(VariableReferenceExp(paramValue), "ToString"), PrimitiveExpression("~#~") }), CodeBinaryOperatorType.IdentityEquality, PrimitiveExpression(0));
                                                ifValueEqualsDefault.TrueStatements.Add(AssignVariable(VariableReferenceExp(paramValue), SnippetExpression("null")));
                                                tryBlockForParams.AddStatement(ifValueEqualsDefault);

                                                valueExpression = VariableReferenceExp(paramValue);
                                            }
                                        }
                                        else
                                        {
                                            valueExpression = VariableReferenceExp(paramValue);
                                        }
                                        break;
                                    case DataType.DOUBLE:
                                        paramType = "Numeric";
                                        paramLength = 28;
                                        paramScale = int.TryParse(param.DecimalLength, out paramScale) ? paramScale : 0;
                                        paramValue = "s" + param.Seg.Name.ToUpperInvariant() + param.DI.Name.ToLowerInvariant();
                                        tryBlockForParams.AddStatement(CommentStatement("`Code Added for Backward Compatibility with DNA. Model Related Issues , old value to be retained"));
                                        tryBlockForParams.AddStatement(AssignVariable(VariableReferenceExp(paramValue), SnippetExpression("(Double.TryParse(sValue, out result) == true ? sValue : " + paramValue + ")")));

                                        if (_ecrOptions.TreatDefaultAsNull)
                                        {
                                            paramType = "Double";

                                            CodeConditionStatement ifValueIsNotNull = IfCondition();
                                            ifValueIsNotNull.Condition = SnippetExpression("!String.IsNullOrEmpty(sValue)");

                                            if (param.Seg.Inst == MULTI_INSTANCE)
                                                ifValueIsNotNull.TrueStatements.Add(AssignVariable(VariableReferenceExp("dValue"), MethodInvocationExp(TypeReferenceExp(typeof(Double)), "Parse").AddParameter(VariableReferenceExp(paramValue))));
                                            else
                                                ifValueIsNotNull.TrueStatements.Add(AssignVariable(VariableReferenceExp("dValue"), MethodInvocationExp(TypeReferenceExp(typeof(Double)), "Parse").AddParameter(ArrayIndexerExpression("nvc" + param.Seg.Name.ToUpperInvariant(), PrimitiveExpression(param.DI.Name.ToLower())))));

                                            tryBlockForParams.AddStatement(ifValueIsNotNull);

                                            CodeConditionStatement ifValueEqualsDefault = IfCondition();
                                            ifValueEqualsDefault.Condition = BinaryOpertorExpression(MethodInvocationExp(TypeReferenceExp(typeof(String)), "CompareOrdinal").AddParameters(new CodeExpression[] { MethodInvocationExp(VariableReferenceExp("dValue"), "ToString"), PrimitiveExpression("-915") }), CodeBinaryOperatorType.IdentityEquality, PrimitiveExpression(0));
                                            ifValueEqualsDefault.TrueStatements.Add(AssignVariable(VariableReferenceExp("dValue"), SnippetExpression("null")));
                                            tryBlockForParams.AddStatement(ifValueEqualsDefault);

                                            paramValue = "dValue";
                                            valueExpression = VariableReferenceExp(paramValue);
                                        }
                                        else
                                        {
                                            valueExpression = VariableReferenceExp(paramValue);
                                        }
                                        break;
                                    case DataType.VARBINARY:
                                        paramType = "FileBinary";
                                        paramLength = 0;
                                        paramValue = "sValue";
                                        methodInvocation.AddParameters(new CodeExpression[] { GetProperty("DBType", paramType), GetProperty("sValue", "Length"), VariableReferenceExp("sValue"), PrimitiveExpression(-1), GetProperty("DIEncodingType", "Base64toBytes") });
                                        valueExpression = VariableReferenceExp(paramValue);
                                        goto skip;
                                }
                                methodInvocation.AddParameter(GetProperty("DBType", paramType));

                                if (paramLength >= 4000)
                                    methodInvocation.AddParameter(SnippetExpression("String.IsNullOrEmpty(sValue) ? 0 : sValue.Length"));
                                else
                                    methodInvocation.AddParameter(PrimitiveExpression(paramLength));

                                if (param.CategorizedDataType == DataType.DOUBLE)
                                    methodInvocation.AddParameter(PrimitiveExpression(paramScale));

                                methodInvocation.AddParameter(valueExpression);

                            skip:
                                //if ((new string[] { DataType.INT, DataType.INTEGER, DataType.DOUBLE, DataType.NUMERIC }.Contains(param.DataType.Trim())) && string.Compare(param.Name, "sysfprowno", true) != 0)
                                //{
                                //    tryBlockForParams.AddStatement(CommentStatement("Code Added for Backward Compatibility with DNA. Model Related Issues , old value to be retained"));
                                //    tryBlockForParams.AddStatement(AssignVariable(VariableReferenceExp("s" + param.Seg.Name.ToUpperInvariant() + param.DI.Name.ToLowerInvariant()), SnippetExpression(string.Format("({0}.TryParse(sValue, out result)==true ? sValue : {1})", (new string[] { DataType.INT, DataType.INTEGER }.Contains(param.DataType.Trim())) ? "Int" : "Double", "s" + param.Seg.Name.ToUpperInvariant() + param.DI.Name.ToLowerInvariant()))));
                                //}

                                tryBlockForParams.AddStatement(methodInvocation);
                            }

                            tryBlockForParams.AddStatement(SnippetStatement("\n"));
                        }
                    #endregion

                    CodeConditionStatement CheckForIEDKParams = IfCondition();
                    CheckForIEDKParams.Condition = BinaryOpertorExpression(BinaryOpertorExpression(VariableReferenceExp("iEDKServiceES"), CodeBinaryOperatorType.BooleanAnd, BinaryOpertorExpression(VariableReferenceExp("psIndex"), CodeBinaryOperatorType.IdentityInequality, PrimitiveExpression(-1))),
                                                                            CodeBinaryOperatorType.BooleanAnd,
                                                                            BinaryOpertorExpression(BinaryOpertorExpression(VariableReferenceExp("brIndex"), CodeBinaryOperatorType.IdentityInequality, PrimitiveExpression(-1)), CodeBinaryOperatorType.BooleanAnd, BinaryOpertorExpression(VariableReferenceExp("cBRInExists"), CodeBinaryOperatorType.IdentityInequality, PrimitiveExpression(-1))));
                    CheckForIEDKParams.TrueStatements.Add(MethodInvocationExp(BaseReferenceExp(), "Parameters").AddParameters(new CodeExpression[] { VariableReferenceExp("psIndex"), VariableReferenceExp("brIndex"), VariableReferenceExp("nLoop") }));
                    tryBlockForParams.AddStatement(CheckForIEDKParams);



                    CodeConditionStatement ifBRExecution = IfCondition();
                    ifBRExecution.Condition = VariableReferenceExp("bBRExec");

                    if (ps.ProcessingType == ProcessingType.ALTERNATE)
                    {
                        ifBRExecution.TrueStatements.Add(MethodInvocationExp(ThisReferenceExp(), "CreateCommand"));
                        ifBRExecution.TrueStatements.Add(tryBlockForParams);
                        statements.Add(ifBRExecution);
                    }
                    else
                    {
                        statements.Add(MethodInvocationExp(ThisReferenceExp(), "CreateCommand"));
                        statements.Add(tryBlockForParams);
                    }
                }
                #endregion
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("PS_AddInParameters-->{0}", !Object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
            return statements;
        }

        //private CodeStatementCollection PS_AddInParametersV1(ProcessSection ps, Method mt)
        //{
        //    CodeStatementCollection statements = null;
        //    CodeStatement statement = null;
        //    try
        //    {
        //        statements = new CodeStatementCollection();
        //        statement = new CodeStatement();

        //        #region SystemGenerated
        //        if (mt.SystemGenerated == BRTypes.SYSTEMGENERATED)
        //        {
        //            statements.Add(CommentStatement("Execute the Method"));
        //            CodeConditionStatement CheckForBRExecution = IfCondition();
        //            CheckForBRExecution.Condition = BinaryOpertorExpression(
        //                                                                    BinaryOpertorExpression(VariableReferenceExp("iEDKServiceES"),
        //                                                                    CodeBinaryOperatorType.BooleanAnd,
        //                                                                    BinaryOpertorExpression(VariableReferenceExp("psIndex"), CodeBinaryOperatorType.IdentityInequality, PrimitiveExpression(-1))),
        //                                                                    CodeBinaryOperatorType.BooleanAnd,
        //                                                                    BinaryOpertorExpression(VariableReferenceExp("brIndex"), CodeBinaryOperatorType.IdentityInequality, PrimitiveExpression(-1))
        //                                                                    );
        //            statement = AssignVariable("bBRExec", PrimitiveExpression(true));

        //            if (ps.ProcessingType == ProcessingType.ALTERNATE)
        //                statements.Add(statement);
        //            else
        //                CheckForBRExecution.TrueStatements.Add(statement);
        //            CheckForBRExecution.TrueStatements.Add(MethodInvocationExp(BaseReferenceExp(), "EvaluateBLForBR").AddParameters(new CodeExpression[] { VariableReferenceExp("psIndex"), VariableReferenceExp("brIndex"), VariableReferenceExp("nLoop"), SnippetExpression("out bBRExec") }));

        //            statements.Add(CheckForBRExecution);

        //            if (ps.ProcessingType == ProcessingType.DEFAULT)
        //            {
        //                CodeConditionStatement CheckBRExecFlag = IfCondition();
        //                CheckBRExecFlag.Condition = BinaryOpertorExpression(VariableReferenceExp("bBRExec"), CodeBinaryOperatorType.IdentityEquality, PrimitiveExpression(false));
        //                CheckForBRExecution.TrueStatements.Add(CheckBRExecFlag);
        //                CheckBRExecFlag.TrueStatements.Add(SnippetExpression("continue"));
        //            }


        //            CodeTryCatchFinallyStatement tryBlockForParams = new CodeTryCatchFinallyStatement();
        //            CodeCatchClause catchBlockForParams = tryBlockForParams.AddCatch();
        //            catchBlockForParams.AddStatement(MethodInvocationExp(BaseReferenceExp(), "Set_Error_Info").AddParameters(new CodeExpression[] {
        //                                            PrimitiveExpression(0),
        //                                            GetProperty("e","Source"),
        //                                            VariableReferenceExp("STOP_PROCESSING"),
        //                                            MethodInvocationExp( TypeReferenceExp(typeof(string)),"Format").AddParameters(new CodeExpression[]{PrimitiveExpression("General Exception during IN Parameter Binding of PS - "+ps.SeqNO+" BR - "+mt.SeqNO+ " {0}"),GetProperty("e","Message") }),
        //                                            GetProperty("String","Empty"),
        //                                            GetProperty("String","Empty"),
        //                                            PrimitiveExpression(0),
        //                                            GetProperty("String","Empty"),
        //                                            GetProperty("String","Empty")
        //                                            }));

        //            #region Parameter addition
        //            #endregion

        //            CodeConditionStatement CheckForIEDKParams = IfCondition();
        //            CheckForIEDKParams.Condition = BinaryOpertorExpression(BinaryOpertorExpression(VariableReferenceExp("iEDKServiceES"), CodeBinaryOperatorType.BooleanAnd, BinaryOpertorExpression(VariableReferenceExp("psIndex"), CodeBinaryOperatorType.IdentityInequality, PrimitiveExpression(-1))),
        //                                                                    CodeBinaryOperatorType.BooleanAnd,
        //                                                                    BinaryOpertorExpression(BinaryOpertorExpression(VariableReferenceExp("brIndex"), CodeBinaryOperatorType.IdentityInequality, PrimitiveExpression(-1)), CodeBinaryOperatorType.BooleanAnd, BinaryOpertorExpression(VariableReferenceExp("cBRInExists"), CodeBinaryOperatorType.IdentityInequality, PrimitiveExpression(-1))));
        //            CheckForIEDKParams.TrueStatements.Add(MethodInvocationExp(BaseReferenceExp(), "Parameters").AddParameters(new CodeExpression[] { VariableReferenceExp("psIndex"), VariableReferenceExp("brIndex"), VariableReferenceExp("nLoop") }));
        //            tryBlockForParams.AddStatement(CheckForIEDKParams);



        //            CodeConditionStatement ifBRExecution = IfCondition();
        //            ifBRExecution.Condition = VariableReferenceExp("bBRExec");

        //            if (ps.ProcessingType == ProcessingType.ALTERNATE)
        //            {
        //                ifBRExecution.TrueStatements.Add(MethodInvocationExp(ThisReferenceExp(), "CreateCommand"));
        //                ifBRExecution.TrueStatements.Add(tryBlockForParams);
        //                statements.Add(ifBRExecution);
        //            }
        //            else
        //            {
        //                statements.Add(MethodInvocationExp(ThisReferenceExp(), "CreateCommand"));
        //                statements.Add(tryBlockForParams);
        //            }
        //        }
        //        #endregion  
        //    }
        //    catch (Exception ex)
        //    {
        //        throw new Exception(String.Format("PS_AddInParametersV1->{0}", ex.InnerException != null ? ex.InnerException.Message : ex.Message));
        //    }
        //    return statements;
        //}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ps"></param>
        /// <param name="mt"></param>
        /// <returns></returns>
        private CodeStatementCollection PS_AddOutParameters(ProcessSection ps, Method mt, IEnumerable<Parameter> outParams, IEnumerable<DataItem> keyDIs)
        {
            CodeStatementCollection statements = null;
            IEnumerable<Segment> multiSegments = outParams.Where(p => p.Seg != null).Where(p => p.Seg.Inst == MULTI_INSTANCE).Select(p => p.Seg).Distinct();
            try
            {
                statements = new CodeStatementCollection();

                if (multiSegments.Any())
                {

                    if (keyDIs.Any())
                    {
                        statements.Add(AssignVariable(VariableReferenceExp("nvcFilterText"), ObjectCreateExpression(typeof(NameValueCollection))));
                        foreach (DataItem di in keyDIs)
                        {
                            string sConvertExpr = string.Empty;

                            statements.Add(AssignVariable(VariableReferenceExp("sValue"), GetProperty(typeof(string), "Empty")));
                            statements.Add(AssignVariable(VariableReferenceExp("sValue"), MethodInvocationExp(ThisReferenceExp(), "GetValue").AddParameter(PrimitiveExpression(di.Name))));
                            switch (di.DataType.ToUpperInvariant())
                            {
                                case DataType.TIME:
                                    sConvertExpr = "HH:mm:ss";
                                    break;
                                case DataType.DATE:
                                    sConvertExpr = "yyyy-MM-dd";
                                    break;
                                case DataType.DATETIME:
                                case DataType.DATE_TIME:
                                    sConvertExpr = "yyyy-MM-dd HH:mm:ss";
                                    break;
                                default:
                                    break;
                            }
                            if (di.DataType.ToUpperInvariant() == DataType.DATE || di.DataType.ToUpperInvariant() == DataType.DATETIME ||
                                di.DataType.ToUpperInvariant() == DataType.DATE_TIME || di.DataType.ToUpperInvariant() == DataType.TIME)
                            {
                                CodeConditionStatement ChecksValue = IfCondition();
                                ChecksValue.Condition = BinaryOpertorExpression(VariableReferenceExp("sValue"), CodeBinaryOperatorType.IdentityInequality, GetProperty(typeof(String), "Empty"));
                                ChecksValue.TrueStatements.Add(AssignVariable("sValue", MethodInvocationExp(BaseReferenceExp(), "DateTimeParse").AddParameters(new CodeExpression[] { VariableReferenceExp("sValue"), PrimitiveExpression(sConvertExpr) })));
                                statements.Add(ChecksValue);
                            }
                            statements.Add(AssignVariable(ArrayIndexerExpression("nvcFilterText", PrimitiveExpression(di.Name)), VariableReferenceExp("sValue")));
                        }
                        statements.Add(AssignVariable(VariableReferenceExp("lInstExists"), MethodInvocationExp(ThisReferenceExp(), "SetRecordPtrEx").AddParameters(new CodeExpression[] { PrimitiveExpression(multiSegments.First().Name.ToLowerInvariant()), VariableReferenceExp("nvcFilterText") })));
                        statements.Add(AssignVariable(VariableReferenceExp("nvcFilterText"), SnippetExpression("null")));

                        CodeConditionStatement checkForInstAvailability = IfCondition();
                        checkForInstAvailability.Condition = BinaryOpertorExpression(VariableReferenceExp("lInstExists"), CodeBinaryOperatorType.IdentityEquality, PrimitiveExpression(0));
                        checkForInstAvailability.TrueStatements.Add(AssignVariable("lInstExists", GetProperty(string.Format("ht{0}", multiSegments.First().Name.ToUpperInvariant()), "Count")));
                        checkForInstAvailability.TrueStatements.Add(AssignVariable(VariableReferenceExp("lInstExists"), BinaryOpertorExpression(VariableReferenceExp("lInstExists"), CodeBinaryOperatorType.Add, PrimitiveExpression(1))));
                        statements.Add(checkForInstAvailability);

                        statements.Add(AssignVariable(VariableReferenceExp("nvcTmp"), SnippetExpression("null")));
                        statements.Add(AssignVariable(VariableReferenceExp("nvcTmp"), SnippetExpression(string.Format("(NameValueCollection)ht{0}[lInstExists]", multiSegments.First().Name.ToUpper()))));

                        CodeConditionStatement checknvcTmpforNull = IfCondition();
                        checknvcTmpforNull.Condition = BinaryOpertorExpression(VariableReferenceExp("nvcTmp"), CodeBinaryOperatorType.IdentityEquality, SnippetExpression("null"));
                        checknvcTmpforNull.TrueStatements.Add(AssignVariable(VariableReferenceExp("nvcTmp"), ObjectCreateExpression(typeof(NameValueCollection))));
                        statements.Add(checknvcTmpforNull);

                    }
                    else
                    {
                        string loopCausingSegment = ps.LoopCausingSegment;
                        string loopingSegment = mt.LoopSegment != null ? mt.LoopSegment.Name : "";


                        if (outParams.Where(p => string.Compare(p.Seg.Name, multiSegments.First().Name, true) == 0)
                                        .All(p => p.FlowDirection == FlowAttribute.OUT) && string.Compare(loopCausingSegment, loopingSegment, true) != 0)
                        {
                            statements.Add(AssignVariable(VariableReferenceExp("nvcTmp"), ObjectCreateExpression("NameValueCollection")));
                        }
                        else
                        {
                            statements.Add(AssignVariable(VariableReferenceExp("nvcTmp"), SnippetExpression(string.Format("(NameValueCollection) ht{0}[nRecCount]", multiSegments.First().Name.ToUpper()))));
                            CodeConditionStatement ifnvcTmpIsNull = IfCondition();
                            ifnvcTmpIsNull.Condition = BinaryOpertorExpression(VariableReferenceExp("nvcTmp"), CodeBinaryOperatorType.IdentityEquality, SnippetExpression("null"));
                            ifnvcTmpIsNull.TrueStatements.Add(AssignVariable(VariableReferenceExp("nvcTmp"), ObjectCreateExpression("NameValueCollection")));
                            statements.Add(ifnvcTmpIsNull);
                        }
                    }

                }

                foreach (Parameter param in outParams.Where(op => op.Name.ToLower().StartsWith("ctxt_") == false).OrderBy(p => p.Name))
                {
                    String ConvertExpr = String.Empty;

                    switch (param.DataType.ToUpperInvariant())
                    {
                        case DataType.TIME:
                            ConvertExpr = "HH:mm:ss";
                            break;
                        case DataType.DATE:
                            ConvertExpr = "yyyy-MM-dd";
                            break;
                        case DataType.DATETIME:
                        case DataType.DATE_TIME:
                            ConvertExpr = "yyyy-MM-dd HH:mm:ss";
                            break;
                        case DataType.VARBINARY:
                            statements.Add(AssignVariable(VariableReferenceExp("sValue"), MethodInvocationExp(ThisReferenceExp(), "GetValue").AddParameters(new CodeExpression[] { PrimitiveExpression(param.Name.ToLowerInvariant()), GetProperty("DBType", "FileBinary"), GetProperty("DIEncodingType", "BytestoBase64") })));
                            goto skip;
                    }
                    statements.Add(AssignVariable(VariableReferenceExp("sValue"), MethodInvocationExp(ThisReferenceExp(), "GetValue").AddParameters(new CodeExpression[] { PrimitiveExpression(param.Name.ToLowerInvariant()) })));

                    if (param.DataType.ToUpperInvariant() == DataType.DATE || param.DataType.ToUpperInvariant() == DataType.DATETIME ||
                        param.DataType.ToUpperInvariant() == DataType.DATE_TIME || param.DataType.ToUpperInvariant() == DataType.TIME
                        )
                    {
                        CodeConditionStatement ChecksValue = IfCondition();
                        ChecksValue.Condition = BinaryOpertorExpression(VariableReferenceExp("sValue"), CodeBinaryOperatorType.IdentityInequality, GetProperty(typeof(String), "Empty"));
                        ChecksValue.TrueStatements.Add(AssignVariable("sValue", MethodInvocationExp(BaseReferenceExp(), "DateTimeParse").AddParameters(new CodeExpression[] { VariableReferenceExp("sValue"), PrimitiveExpression(ConvertExpr) })));
                        statements.Add(ChecksValue);
                    }

                skip:
                    if (param.Seg.Inst == MULTI_INSTANCE)
                        statements.Add(AssignVariable(ArrayIndexerExpression("nvcTmp", PrimitiveExpression(param.DI.Name.ToLowerInvariant())), VariableReferenceExp("sValue")));
                    else
                        statements.Add(AssignVariable(ArrayIndexerExpression("nvc" + param.Seg.Name.ToUpperInvariant(), PrimitiveExpression(param.DI.Name.ToLowerInvariant())), VariableReferenceExp("sValue")));
                }
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("PS_AddOutParameters->{0}", !Object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
            return statements;
        }

        /// <summary>
        /// Method to generate code for executing sp, get raise/non-raise error and get out param values.
        /// </summary>
        /// <param name="ps"></param>
        /// <param name="mt"></param>
        /// <returns></returns>
        private CodeStatementCollection PS_MethodExecution(ProcessSection ps, Method mt)
        {
            CodeStatementCollection statements;
            try
            {
                statements = new CodeStatementCollection();

                #region if control expression is available at mt level
                if (mt.CtrlExp != null)
                {
                    if (mt.CtrlExp.IsValid)
                    {
                        string mtCtrlExpSegName = String.Format("{0}{1}", mt.CtrlExp.Seg.Inst == MULTI_INSTANCE ? "ht" : "nvc", mt.CtrlExp.Seg.Name.ToUpperInvariant());
                        string mtCtrlExpDIName = mt.CtrlExp.DI.Name.ToLowerInvariant();

                        statements.Add(CommentStatement("Evaluate the control expression for this method"));

                        if (mt.CtrlExp.Seg.Inst == MULTI_INSTANCE)
                        {
                            //if (mt.LoopSegment != null)
                            //{
                            statements.Add(AssignVariable(VariableReferenceExp("nvcTmpCrtl"), SnippetExpression("(NameValueCollection)ht" + mt.CtrlExp.Seg.Name.ToUpperInvariant() + "[nLoop]")));
                            //}
                            statements.Add(AssignVariable(VariableReferenceExp("bmtflag"), PrimitiveExpression(true)));
                            CodeConditionStatement CheckToSetbmtflag = IfCondition();
                            CheckToSetbmtflag.Condition = BinaryOpertorExpression(BinaryOpertorExpression(VariableReferenceExp("nvcTmpCrtl"), CodeBinaryOperatorType.IdentityInequality, SnippetExpression("null")), CodeBinaryOperatorType.BooleanAnd, BinaryOpertorExpression(ArrayIndexerExpression("nvcTmpCrtl", PrimitiveExpression(mtCtrlExpDIName)), CodeBinaryOperatorType.IdentityInequality, PrimitiveExpression("0")));
                            CheckToSetbmtflag.TrueStatements.Add(AssignVariable(VariableReferenceExp("bmtflag"), PrimitiveExpression(false)));
                            statements.Add(CheckToSetbmtflag);
                        }
                        else
                        {
                            statements.Add(AssignVariable(VariableReferenceExp("bmtflag"), PrimitiveExpression(true)));
                            CodeConditionStatement CheckToSetbmtflag = IfCondition();
                            CheckToSetbmtflag.Condition = BinaryOpertorExpression(BinaryOpertorExpression(VariableReferenceExp(mtCtrlExpSegName), CodeBinaryOperatorType.IdentityInequality, SnippetExpression("null")), CodeBinaryOperatorType.BooleanAnd, BinaryOpertorExpression(ArrayIndexerExpression(mtCtrlExpSegName, PrimitiveExpression(mtCtrlExpDIName)), CodeBinaryOperatorType.IdentityInequality, PrimitiveExpression("0")));
                            CheckToSetbmtflag.TrueStatements.Add(AssignVariable(VariableReferenceExp("bmtflag"), PrimitiveExpression(false)));
                            statements.Add(CheckToSetbmtflag);
                        }
                        CodeConditionStatement Checkbmtflag = IfCondition();
                        Checkbmtflag.Condition = BinaryOpertorExpression(VariableReferenceExp("bmtflag"), CodeBinaryOperatorType.IdentityEquality, PrimitiveExpression(true));
                        statements.Add(Checkbmtflag);

                        //Code addition starts when we encountered a problem in aviation bulk generation
                        if (!string.IsNullOrEmpty(ps.LoopCausingSegment))
                        {
                            Segment psLoopSegment = _service.Segments.Where(s => s.Name == ps.LoopCausingSegment).First(); //madhan
                            if (ps.ProcessingType == ProcessingType.DEFAULT && (string.Compare(psLoopSegment.Process_sel_rows, "y", true) == 0 || string.Compare(psLoopSegment.Process_upd_rows, "y", true) == 0))
                            {
                                Checkbmtflag.TrueStatements.Add(AssignVariable(FieldReferenceExp(ThisReferenceExp(), "nvcTmp"), SnippetExpression("(NameValueCollection) " + "ht" + ps.LoopCausingSegment.ToUpper() + "[nLoop]")));
                                Checkbmtflag.TrueStatements.Add(AssignVariable(VariableReferenceExp("sValue"), ArrayIndexerExpression("nvcTmp", PrimitiveExpression("ModeFlag"))));
                                CodeConditionStatement checkModeFlagValue = IfCondition();
                                if (psLoopSegment.Process_sel_rows == "y")
                                    checkModeFlagValue.Condition = SnippetExpression("sValue == \"I\" || sValue == \"U\" || sValue == \"S\"");
                                else if (psLoopSegment.Process_upd_rows == "y")
                                    checkModeFlagValue.Condition = BinaryOpertorExpression(VariableReferenceExp("sValue"), CodeBinaryOperatorType.IdentityEquality, PrimitiveExpression("S"));

                                checkModeFlagValue.TrueStatements.Add(new CodeGotoStatement("ht" + ps.LoopCausingSegment.ToUpper()));
                                Checkbmtflag.TrueStatements.Add(checkModeFlagValue);
                            }
                        }
                        //Code addition ends when we encountered a problem in aviation bulk generation


                        if (mt.LoopSegment != null)
                        {
                            if (!new string[] { "sy", "y" }.Contains(mt.LoopSegment.Process_sel_rows.ToLower()) && !new string[] { "sy", "y" }.Contains(mt.LoopSegment.Process_upd_rows.ToLower()) && !new string[] { "sy", "y" }.Contains(mt.LoopSegment.Process_sel_upd_rows.ToLower()))
                                Checkbmtflag.TrueStatements.Add(AssignVariable(VariableReferenceExp("nvcTmp"), SnippetExpression("(NameValueCollection)ht" + mt.LoopSegment.Name.ToUpperInvariant() + "[nLoop]")));
                        }

                        foreach (CodeStatement statement in PS_AddInParameters(ps, mt))
                        {
                            Checkbmtflag.TrueStatements.Add(statement);
                        }

                        foreach (CodeStatement statement in PS_SpExecution(ps, mt))
                        {
                            Checkbmtflag.TrueStatements.Add(statement);
                        }
                    }
                }
                #endregion

                #region if mt doesn't have control expression
                else
                {
                    //if (mt.LoopSegment != null)
                    //{
                    //    statements.Add(AssignVariable(VariableReferenceExp("nvcTmp"), SnippetExpression("(NameValueCollection)ht" + mt.LoopSegment.Name.ToUpperInvariant() + "[nLoop]")));
                    //}
                    foreach (CodeStatement statement in PS_AddInParameters(ps, mt))
                    {
                        statements.Add(statement);
                    }

                    foreach (CodeStatement statement in PS_SpExecution(ps, mt))
                    {
                        statements.Add(statement);
                    }
                }
                #endregion
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("PS_MethodExecution->{0}", !Object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
            return statements;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ps"></param>
        /// <param name="mt"></param>
        /// <returns></returns>
        private CodeStatementCollection PS_SpExecution(ProcessSection ps, Method mt)
        {
            CodeStatementCollection statements;
            try
            {
                statements = new CodeStatementCollection();

                #region Non BRO method
                if (mt.SystemGenerated.Equals(BRTypes.SYSTEMGENERATED))
                {
                    #region Execute sp and get values

                    bool hasOutParam = mt.Parameters.Where(p => p.Seg.Name != CONTEXT_SEGMENT && p.Seg.Name != ERR_SEGMENT_NAME
                                                                && p.FlowDirection != FlowAttribute.IN).Any();

                    if (mt.AccessDatabase)
                    {
                        statements.Add(MethodInvocationExp(BaseReferenceExp(), "WriteProfiler").AddParameters(new CodeExpression[] { MethodInvocationExp(TypeReferenceExp(typeof(String)), "Format").AddParameters(new CodeExpression[] { PrimitiveExpression("Executing-- {0}/{1} of " + mt.Name), VariableReferenceExp("nLoop"), VariableReferenceExp("nMax") }) }));

                        CodeVariableReferenceExpression errorProtocol;
                        if (mt.SPErrorProtocol == "0")
                        {
                            errorProtocol = VariableReferenceExp("SP_ERR_PROTOCOL_OUTPARAM");
                        }
                        else if (mt.SPErrorProtocol == "1")
                        {
                            errorProtocol = VariableReferenceExp("SP_ERR_PROTOCOL_SELECT");
                        }
                        else if (mt.SPErrorProtocol == "2")
                        {
                            errorProtocol = VariableReferenceExp("SP_ERR_PROTOCOL_MRS_OUTPARAM");
                        }
                        else
                        {
                            errorProtocol = VariableReferenceExp("SP_ERR_PROTOCOL_OUTPARAM");
                        }
                        statements.Add(MethodInvocationExp(BaseReferenceExp(), "Execute_SP").AddParameters(new CodeExpression[] { PrimitiveExpression(hasOutParam), PrimitiveExpression(mt.SPName), SnippetExpression("ref szErrorDesc"), SnippetExpression("ref lSPErrorID"), SnippetExpression("ref szErrSrc"), errorProtocol }));
                    }

                    if (ps.ProcessingType == ProcessingType.ALTERNATE)
                    {
                        CodeConditionStatement checkToClearBlExp = IfCondition();
                        checkToClearBlExp.Condition = SnippetExpression("iEDKServiceES && psIndex !=-1 && brIndex !=-1");
                        checkToClearBlExp.TrueStatements.Add(MethodInvocationExp(BaseReferenceExp(), "ClearBLExpression").AddParameters(new CodeExpression[] { VariableReferenceExp("psIndex"), VariableReferenceExp("brIndex"), VariableReferenceExp("nLoop") }));
                        statements.Add(checkToClearBlExp);
                    }

                    statements.Add(CommentStatement("To handle stop error type / Raise errors"));
                    CodeConditionStatement CheckForRaiseError = IfCondition();
                    CheckForRaiseError.Condition = BinaryOpertorExpression(VariableReferenceExp("lSPErrorID"), CodeBinaryOperatorType.IdentityInequality, PrimitiveExpression(0));
                    statements.Add(CheckForRaiseError);

                    CodeConditionStatement CheckForErrSrc = IfCondition();
                    CheckForErrSrc.Condition = BinaryOpertorExpression(MethodInvocationExp(VariableReferenceExp("szErrSrc"), "ToUpper"), CodeBinaryOperatorType.IdentityEquality, VariableReferenceExp("APP_ERROR"));
                    CheckForErrSrc.TrueStatements.Add(MethodInvocationExp(ThisReferenceExp(), "Process_MethodError_Info").AddParameters(new CodeExpression[] { GetProperty("String", "Empty"), VariableReferenceExp("APP_ERROR"), VariableReferenceExp("lSPErrorID"), VariableReferenceExp("nLoop"), PrimitiveExpression(int.Parse(mt.ID)), PrimitiveExpression(int.Parse(ps.SeqNO)), PrimitiveExpression(int.Parse(mt.SeqNO)) }));
                    CheckForErrSrc.FalseStatements.Add(MethodInvocationExp(ThisReferenceExp(), "Process_MethodError_Info").AddParameters(new CodeExpression[] { VariableReferenceExp("szErrorDesc"), VariableReferenceExp("UNKNOWN_ERROR"), VariableReferenceExp("lSPErrorID"), VariableReferenceExp("nLoop"), PrimitiveExpression(int.Parse(mt.ID)), PrimitiveExpression(int.Parse(ps.SeqNO)), PrimitiveExpression(int.Parse(mt.SeqNO)) }));
                    CheckForRaiseError.TrueStatements.Add(CheckForErrSrc);

                    IEnumerable<Parameter> outParams = mt.Parameters.Where(p => p.Seg != null && (p.FlowDirection == FlowAttribute.OUT || p.FlowDirection == FlowAttribute.INOUT));
                    if (outParams.Count() > 0)
                    {
                        foreach (CodeStatement statement in PS_BRExecution_OutBinding(ps, mt, outParams))
                            statements.Add(statement);

                        //for handling non-raise errors
                        statements.Add(CommentStatement("To handle continue error type / Non-Raise errors"));
                        CodeConditionStatement IfSpErridIsZero = IfCondition();
                        IfSpErridIsZero.Condition = BinaryOpertorExpression(VariableReferenceExp("lSPErrorID"), CodeBinaryOperatorType.IdentityEquality, PrimitiveExpression(0));
                        statements.Add(IfSpErridIsZero);

                        CodeConditionStatement GetSpErrorId = IfCondition();
                        GetSpErrorId.Condition = MethodInvocationExp(BaseReferenceExp(), "GetCommandOutParam").AddParameters(new CodeExpression[] { SnippetExpression("ref lSPErrorID"), VariableReferenceExp("SP_ERR_PROTOCOL_OUTPARAM") });
                        IfSpErridIsZero.TrueStatements.Add(GetSpErrorId);

                        CodeConditionStatement IfSpErridIsNotZero = IfCondition();
                        IfSpErridIsNotZero.Condition = BinaryOpertorExpression(VariableReferenceExp("lSPErrorID"), CodeBinaryOperatorType.IdentityInequality, PrimitiveExpression(0));
                        IfSpErridIsNotZero.TrueStatements.Add(MethodInvocationExp(ThisReferenceExp(), "Process_MethodError_Info").AddParameters(new CodeExpression[] { GetProperty("String", "Empty"), VariableReferenceExp("APP_ERROR"), VariableReferenceExp("lSPErrorID"), VariableReferenceExp("nLoop"), PrimitiveExpression(int.Parse(mt.ID)), PrimitiveExpression(int.Parse(ps.SeqNO)), PrimitiveExpression(int.Parse(mt.SeqNO)) }));
                        GetSpErrorId.TrueStatements.Add(IfSpErridIsNotZero);
                    }


                    if (!string.IsNullOrEmpty(ps.LoopCausingSegment))
                    {
                        Segment loopSegment = _service.Segments.Where(s => s.Name == ps.LoopCausingSegment).First();
                        if (loopSegment.Process_sel_rows == "y" || loopSegment.Process_upd_rows == "y")
                        {
                            statements.Add(new CodeLabeledStatement("ht" + ps.LoopCausingSegment.ToUpper()));
                            statements.Add(MethodInvocationExp(BaseReferenceExp(), "WriteProfiler").AddParameter(PrimitiveExpression("Process only selected rows.")));
                        }
                    }
                    #endregion
                }
                #endregion

                #region Custom BRO/Handcoded BRO
                else if (mt.SystemGenerated.Equals(BRTypes.CUSTOM_BR) || mt.SystemGenerated.Equals(BRTypes.HANDCODED_BR))
                {

                    IEnumerable<Parameter> singleSegOutParams = mt.Parameters.Where(p => p.Seg != null && p.Seg.Inst == SINGLE_INSTANCE && p.DI != null && string.Compare(p.Seg.Name, CONTEXT_SEGMENT, true) != 0 && (p.FlowDirection == FlowAttribute.OUT || p.FlowDirection == FlowAttribute.INOUT)).OrderBy(p => p.Name);
                    IEnumerable<Parameter> multiSegOutParams = mt.Parameters.Where(p => p.Seg != null && p.Seg.Inst == MULTI_INSTANCE && p.DI != null && string.Compare(p.Seg.Name, CONTEXT_SEGMENT, true) != 0 && (p.FlowDirection == FlowAttribute.OUT || p.FlowDirection == FlowAttribute.INOUT)).OrderBy(p => p.Name);
                    string sBroObjName = (singleSegOutParams.Any() == false && multiSegOutParams.Any() == false) ? "res" : "spxs";

                    CodeTryCatchFinallyStatement tryForBRO = new CodeTryCatchFinallyStatement();

                    #region BRO calling portion
                    //tryForBRO.AddStatement(CommentStatement("Add BRO invokation here..."));
                    CodeMethodInvokeExpression broMethodInvokation = MethodInvocationExp(FieldReferenceExp(ThisReferenceExp(), "objBRO"), mt.Name.ToLower());
                    if (mt.AccessDatabase)
                    {
                        broMethodInvokation.AddParameter(VariableReferenceExp("szConnectionString"));
                        if (_ecrOptions.InTD)
                            broMethodInvokation.AddParameter(VariableReferenceExp("szInTD"));
                    }
                    //broMethodInvokation.AddParameter(MethodInvocationExp(TypeReferenceExp(typeof(Int32)), "Parse").AddParameter(MethodInvocationExp(VariableReferenceExp("szLangID"), "Trim")));
                    //broMethodInvokation.AddParameter(MethodInvocationExp(TypeReferenceExp(typeof(Int32)), "Parse").AddParameter(MethodInvocationExp(VariableReferenceExp("szOUI"), "Trim")));
                    //broMethodInvokation.AddParameter(VariableReferenceExp("szRole"));
                    //broMethodInvokation.AddParameter(VariableReferenceExp("szServiceName"));
                    //broMethodInvokation.AddParameter(VariableReferenceExp("szUser"));
                    foreach (Parameter param in mt.Parameters.Where(p => p.Seg != null
                    && p.DI != null
                    //&& string.Compare(p.Seg.Name, CONTEXT_SEGMENT, true) != 0
                    && (p.FlowDirection == FlowAttribute.IN || p.FlowDirection == FlowAttribute.INOUT)
                    ).OrderBy(p => int.Parse(p.SequenceNo)))
                    {

                        string sParamType = param.CategorizedDataType;
                        string sParamName = string.Empty;
                        if (string.Compare(param.Seg.Name, "fw_context", true) != 0)
                        {
                            switch (sParamType)
                            {
                                case DataType.INT:
                                    sParamName = string.Concat("l", param.Seg.Name.ToLower(), param.DI.Name.ToLower());
                                    break;
                                case DataType.STRING:
                                case DataType.TIMESTAMP:
                                case DataType.VARBINARY:
                                    sParamName = string.Concat("sz", param.Seg.Name.ToLower(), param.DI.Name.ToLower());
                                    break;
                                case DataType.DOUBLE:
                                    sParamName = string.Concat("dbl", param.Seg.Name.ToLower(), param.DI.Name.ToLower());
                                    break;
                                default:
                                    sParamName = string.Concat("sz", param.Seg.Name.ToLower(), param.DI.Name.ToLower());
                                    break;
                            }
                            broMethodInvokation.AddParameter(VariableReferenceExp(sParamName));
                        }
                        else
                        {
                            var ContextParam = _fw_context_parameters.AsEnumerable().Where(r => r.Field<string>("parametername").ToLower() == "@ctxt_" + param.DI.Name.ToLower());
                            if (ContextParam.Any())
                            {
                                DataRow drContextInfo = ContextParam.First();
                                sParamName = drContextInfo["parametervalue"].ToString();
                                if (param.CategorizedDataType != DataType.STRING)
                                    broMethodInvokation.AddParameter(MethodInvocationExp(TypeReferenceExp(typeof(int)), "Parse").AddParameter(MethodInvocationExp(VariableReferenceExp(sParamName), "Trim")));
                                else
                                    broMethodInvokation.AddParameter(VariableReferenceExp(sParamName));
                            }
                        }
                    }
                    tryForBRO.AddStatement(AssignVariable(VariableReferenceExp(sBroObjName), broMethodInvokation));
                    #endregion

                    CodeConditionStatement ifRetValIsNotZero = IfCondition();
                    ifRetValIsNotZero.Condition = BinaryOpertorExpression(GetProperty(VariableReferenceExp(sBroObjName), "ErrorId"), CodeBinaryOperatorType.IdentityInequality, PrimitiveExpression(0));
                    ifRetValIsNotZero.TrueStatements.Add(MethodInvocationExp(ThisReferenceExp(), "Process_MethodError_Info").AddParameters(new CodeExpression[] { GetProperty(TypeReferenceExp(typeof(string)), "Empty"), FieldReferenceExp(TypeReferenceExp("CUtil"), "APP_ERROR"), GetProperty(TypeReferenceExp(sBroObjName), "ErrorId"), VariableReferenceExp("nLoop"), PrimitiveExpression(int.Parse(mt.ID)), PrimitiveExpression(int.Parse(ps.SeqNO)), PrimitiveExpression(int.Parse(mt.SeqNO)) }));
                    tryForBRO.AddStatement(ifRetValIsNotZero);



                    CodeCatchClause catchCRVWException = tryForBRO.AddCatch("CRVWException", "rvwe");
                    ThrowException(catchCRVWException, "rvwe");

                    CodeCatchClause catchOtherException = AddCatchBlock(tryForBRO, "e");
                    catchOtherException.AddStatement(MethodInvocationExp(ThisReferenceExp(), "Process_MethodError_Info").AddParameters(new CodeExpression[] { GetProperty("e", "Message"), FieldReferenceExp(TypeReferenceExp("CUtil"), "UNKNOWN_ERROR"), PrimitiveExpression(1000), VariableReferenceExp("nLoop"), PrimitiveExpression(int.Parse(mt.ID)), PrimitiveExpression(int.Parse(ps.SeqNO)), PrimitiveExpression(int.Parse(mt.SeqNO)) }));

                    statements.Add(tryForBRO);

                    //IEnumerable<Parameter> singleSegOutParams = mt.Parameters.Where(p => p.Seg != null && p.Seg.Inst == SINGLE_INSTANCE && p.DI != null && string.Compare(p.Seg.Name, CONTEXT_SEGMENT, true) != 0 && (p.FlowDirection == FlowAttribute.OUT || p.FlowDirection == FlowAttribute.INOUT)).OrderBy(p => p.Name);
                    //IEnumerable<Parameter> multiSegOutParams = mt.Parameters.Where(p => p.Seg != null && p.Seg.Inst == MULTI_INSTANCE && p.DI != null && string.Compare(p.Seg.Name, CONTEXT_SEGMENT, true) != 0 && (p.FlowDirection == FlowAttribute.OUT || p.FlowDirection == FlowAttribute.INOUT)).OrderBy(p => p.Name);

                    CodeTryCatchFinallyStatement tryForBROOutBinding = new CodeTryCatchFinallyStatement();

                    #region out binding for header segment
                    foreach (Parameter param in singleSegOutParams.Where(p => string.Compare(p.Seg.Name, CONTEXT_SEGMENT, true) != 0))
                    {
                        string sParamName = param.Name;
                        string sSegName = param.Seg.Name.ToLower();
                        string sDIName = param.DI.Name.ToLower();
                        string sVariableName = string.Concat("sz", sSegName, sDIName);
                        string sCollectionName = string.Concat("nvc", sSegName.ToUpper());

                        tryForBROOutBinding.AddStatement(CommentStatement(sParamName));
                        tryForBROOutBinding.AddStatement(AssignVariable(VariableReferenceExp("sValue"), GetProperty(TypeReferenceExp(typeof(string)), "Empty")));
                        tryForBROOutBinding.AddStatement(AssignVariable(VariableReferenceExp(sVariableName), MethodInvocationExp(GetProperty(VariableReferenceExp("spxs"), param.Name), "ToString")));

                        CodeConditionStatement ifOutValueIsNotNull = IfCondition();
                        ifOutValueIsNotNull.Condition = BinaryOpertorExpression(VariableReferenceExp(sVariableName), CodeBinaryOperatorType.IdentityInequality, SnippetExpression("null"));
                        ifOutValueIsNotNull.TrueStatements.Add(AssignVariable(VariableReferenceExp("sValue"), VariableReferenceExp(sVariableName)));

                        if (param.DataType == DataType.DATE || param.DataType == DataType.TIME || param.DataType == DataType.DATETIME || param.DataType == DataType.DATE_TIME)
                        {
                            CodeConditionStatement ifAssignedValIsNotEmpty = IfCondition();
                            ifAssignedValIsNotEmpty.Condition = BinaryOpertorExpression(VariableReferenceExp("sValue"), CodeBinaryOperatorType.IdentityInequality, GetProperty(TypeReferenceExp(typeof(string)), "Empty"));

                            switch (param.DataType)
                            {
                                case DataType.DATE:
                                    ifAssignedValIsNotEmpty.TrueStatements.Add(AssignVariable(VariableReferenceExp("sValue"), MethodInvocationExp(BaseReferenceExp(), "DateTimeParse").AddParameters(new CodeExpression[] { VariableReferenceExp("sValue"), PrimitiveExpression("yyyy-MM-dd") })));
                                    break;
                                case DataType.TIME:
                                    ifAssignedValIsNotEmpty.TrueStatements.Add(AssignVariable(VariableReferenceExp("sValue"), MethodInvocationExp(BaseReferenceExp(), "DateTimeParse").AddParameters(new CodeExpression[] { VariableReferenceExp("sValue"), PrimitiveExpression("HH:mm:ss") })));
                                    break;
                                case DataType.DATETIME:
                                case DataType.DATE_TIME:
                                    ifAssignedValIsNotEmpty.TrueStatements.Add(AssignVariable(VariableReferenceExp("sValue"), MethodInvocationExp(BaseReferenceExp(), "DateTimeParse").AddParameters(new CodeExpression[] { VariableReferenceExp("sValue"), PrimitiveExpression("yyyy-MM-dd HH:mm:ss") })));
                                    break;
                                default:
                                    break;
                            }


                            ifOutValueIsNotNull.TrueStatements.Add(ifAssignedValIsNotEmpty);
                        }
                        tryForBROOutBinding.AddStatement(ifOutValueIsNotNull);

                        tryForBROOutBinding.AddStatement(AssignVariable(ArrayIndexerExpression(sCollectionName, PrimitiveExpression(param.DI.Name.ToLower())), VariableReferenceExp("sValue")));
                    }
                    #endregion

                    #region out binding for Multi segment                        
                    if (multiSegOutParams.Any())
                    {
                        tryForBROOutBinding.AddStatement(AssignVariable(VariableReferenceExp("nRecCount"), GetProperty(VariableReferenceExp("ht" + multiSegOutParams.First().Seg.Name.ToUpper()), "Count")));

                        CodeConditionStatement ifErrorIdIsZero = IfCondition();
                        ifErrorIdIsZero.Condition = BinaryOpertorExpression(GetProperty(VariableReferenceExp("spxs"), "ErrorId"), CodeBinaryOperatorType.IdentityEquality, PrimitiveExpression(0));

                        ifErrorIdIsZero.TrueStatements.Add(SnippetStatement(string.Format("foreach({0}BRTypes.{1}_RSet spx in spxs.resultSet)", Common.InitCaps(_ecrOptions.Component), Common.InitCaps(mt.Name))));
                        ifErrorIdIsZero.TrueStatements.Add(SnippetStatement("{"));
                        ifErrorIdIsZero.TrueStatements.Add(AssignVariable(VariableReferenceExp("nRecCount"), BinaryOpertorExpression(VariableReferenceExp("nRecCount"), CodeBinaryOperatorType.Add, PrimitiveExpression(1))));
                        ifErrorIdIsZero.TrueStatements.Add(AssignVariable(VariableReferenceExp("nvcTmp"), ObjectCreateExpression(typeof(NameValueCollection))));

                        foreach (Parameter param in multiSegOutParams)
                        {
                            if (param.DataType == DataType.DATE || param.DataType == DataType.TIME || param.DataType == DataType.DATETIME || param.DataType == DataType.DATE_TIME)
                            {
                                ifErrorIdIsZero.TrueStatements.Add(AssignVariable(VariableReferenceExp("sValue"), MethodInvocationExp(GetProperty(VariableReferenceExp("spx"), param.Name), "ToString")));

                                CodeConditionStatement ifAssignedValIsNotEmpty = IfCondition();
                                ifAssignedValIsNotEmpty.Condition = BinaryOpertorExpression(VariableReferenceExp("sValue"), CodeBinaryOperatorType.IdentityInequality, GetProperty(TypeReferenceExp(typeof(string)), "Empty"));

                                switch (param.DataType)
                                {
                                    case DataType.DATE:
                                        ifAssignedValIsNotEmpty.TrueStatements.Add(AssignVariable(VariableReferenceExp("sValue"), MethodInvocationExp(BaseReferenceExp(), "DateTimeParse").AddParameters(new CodeExpression[] { VariableReferenceExp("sValue"), PrimitiveExpression("yyyy-MM-dd") })));
                                        break;
                                    case DataType.TIME:
                                        ifAssignedValIsNotEmpty.TrueStatements.Add(AssignVariable(VariableReferenceExp("sValue"), MethodInvocationExp(BaseReferenceExp(), "DateTimeParse").AddParameters(new CodeExpression[] { VariableReferenceExp("sValue"), PrimitiveExpression("HH:mm:ss") })));
                                        break;
                                    case DataType.DATETIME:
                                    case DataType.DATE_TIME:
                                        ifAssignedValIsNotEmpty.TrueStatements.Add(AssignVariable(VariableReferenceExp("sValue"), MethodInvocationExp(BaseReferenceExp(), "DateTimeParse").AddParameters(new CodeExpression[] { VariableReferenceExp("sValue"), PrimitiveExpression("yyyy-MM-dd HH:mm:ss") })));
                                        break;
                                    default:
                                        break;
                                }
                                ifErrorIdIsZero.TrueStatements.Add(ifAssignedValIsNotEmpty);
                                ifErrorIdIsZero.TrueStatements.Add(AssignVariable(ArrayIndexerExpression("nvcTmp", PrimitiveExpression(param.DI.Name)), VariableReferenceExp("sValue")));
                            }
                            else
                            {
                                ifErrorIdIsZero.TrueStatements.Add(AssignVariable(ArrayIndexerExpression("nvcTmp", PrimitiveExpression(param.DI.Name)), MethodInvocationExp(GetProperty(VariableReferenceExp("spx"), param.Name), "ToString")));
                            }
                        }
                        CodeConditionStatement checkToCallResultSetBinding = IfCondition();
                        checkToCallResultSetBinding.Condition = BinaryOpertorExpression(BinaryOpertorExpression(VariableReferenceExp("iEDKServiceES"), CodeBinaryOperatorType.BooleanAnd, BinaryOpertorExpression(VariableReferenceExp("psIndex"), CodeBinaryOperatorType.IdentityInequality, PrimitiveExpression(-1))), CodeBinaryOperatorType.BooleanAnd, BinaryOpertorExpression(BinaryOpertorExpression(VariableReferenceExp("brIndex"), CodeBinaryOperatorType.IdentityInequality, PrimitiveExpression(-1)), CodeBinaryOperatorType.BooleanAnd, BinaryOpertorExpression(VariableReferenceExp("cBROutExists"), CodeBinaryOperatorType.IdentityInequality, PrimitiveExpression(-1))));
                        checkToCallResultSetBinding.TrueStatements.Add(SnippetExpression("ESResultsetBinding(psIndex, brIndex, nvcTmp)"));
                        ifErrorIdIsZero.TrueStatements.Add(checkToCallResultSetBinding);

                        ifErrorIdIsZero.TrueStatements.Add(AssignVariable(ArrayIndexerExpression("ht" + multiSegOutParams.First().Seg.Name.ToUpper(), VariableReferenceExp("nRecCount")), VariableReferenceExp("nvcTmp")));

                        ifErrorIdIsZero.TrueStatements.Add(SnippetStatement("}"));

                        tryForBROOutBinding.AddStatement(ifErrorIdIsZero);
                    }
                    #endregion

                    CodeCatchClause catchException = tryForBROOutBinding.AddCatch();
                    catchException.AddStatement(MethodInvocationExp(BaseReferenceExp(), "Set_Error_Info").AddParameters(new CodeExpression[] { PrimitiveExpression(0),
                                                                                                                                                   VariableReferenceExp("FRAMEWORK_ERROR"), VariableReferenceExp("STOP_PROCESSING"), PrimitiveExpression("\"General Exception during OUT Data Binding of PS - " + ps.SeqNO + " BR - " + mt.SeqNO + " \"+e.Message"),
                                                                                                                                                    GetProperty(TypeReferenceExp(typeof(string)),"Empty"),
                                                                                                                                                    GetProperty(TypeReferenceExp(typeof(string)),"Empty"),
                                                                                                                                                    PrimitiveExpression(0),
                                                                                                                                                    GetProperty(TypeReferenceExp(typeof(string)),"Empty"),
                                                                                                                                                    GetProperty(TypeReferenceExp(typeof(string)),"Empty")}));

                    if (singleSegOutParams.Any() || multiSegOutParams.Any())
                        statements.Add(tryForBROOutBinding);

                }
                #endregion

                #region Bulk BRO
                else if (mt.SystemGenerated == BRTypes.BULK_BR)
                {
                    CodeTryCatchFinallyStatement tryForBRO = new CodeTryCatchFinallyStatement();

                    #region BRO calling portion
                    //tryForBRO.AddStatement(CommentStatement("Add BRO invokation here..."));
                    CodeMethodInvokeExpression broMethodInvokation = MethodInvocationExp(FieldReferenceExp(ThisReferenceExp(), "objBulk"), mt.Name.ToLower() + "Ex");
                    if (mt.AccessDatabase)
                    {
                        broMethodInvokation.AddParameter(VariableReferenceExp("szConnectionString"));
                        //if (_ecrOptions.InTD)
                        broMethodInvokation.AddParameter(VariableReferenceExp("szInTD"));
                    }
                    foreach (Parameter param in mt.Parameters.Where(p => p.Seg != null && p.DI != null
                    //&& (p.FlowDirection == FlowAttribute.IN || p.FlowDirection == FlowAttribute.INOUT)
                    ).OrderBy(p => int.Parse(p.SequenceNo)))
                    {

                        string sParamType = param.CategorizedDataType;
                        string sParamName = string.Empty;
                        if (string.Compare(param.Seg.Name, "fw_context", true) != 0)
                        {
                            switch (sParamType)
                            {
                                case DataType.INT:
                                    sParamName = string.Concat("l", param.Seg.Name.ToLower(), param.DI.Name.ToLower());
                                    break;
                                case DataType.STRING:
                                case DataType.TIMESTAMP:
                                case DataType.VARBINARY:
                                    sParamName = string.Concat("sz", param.Seg.Name.ToLower(), param.DI.Name.ToLower());
                                    break;
                                case DataType.DOUBLE:
                                    sParamName = string.Concat("dbl", param.Seg.Name.ToLower(), param.DI.Name.ToLower());
                                    break;
                                default:
                                    sParamName = string.Concat("sz", param.Seg.Name.ToLower(), param.DI.Name.ToLower());
                                    break;
                            }

                            if (param.FlowDirection == FlowAttribute.INOUT || param.FlowDirection == FlowAttribute.OUT)
                                broMethodInvokation.AddParameter(SnippetExpression(string.Format("ref {0}", sParamName)));
                            else
                                broMethodInvokation.AddParameter(VariableReferenceExp(sParamName));
                        }
                        else
                        {
                            var ContextParam = _fw_context_parameters.AsEnumerable().Where(r => r.Field<string>("parametername").ToLower() == "@ctxt_" + param.DI.Name.ToLower());
                            if (ContextParam.Any())
                            {
                                DataRow drContextInfo = ContextParam.First();
                                sParamName = drContextInfo["parametervalue"].ToString();
                                if (param.CategorizedDataType != DataType.STRING)
                                    broMethodInvokation.AddParameter(MethodInvocationExp(TypeReferenceExp(typeof(int)), "Parse").AddParameter(MethodInvocationExp(VariableReferenceExp(sParamName), "Trim")));
                                else
                                    broMethodInvokation.AddParameter(VariableReferenceExp(sParamName));
                            }
                        }
                    }
                    tryForBRO.AddStatement(AssignVariable(VariableReferenceExp("lRetVal"), broMethodInvokation));
                    #endregion

                    CodeConditionStatement ifRetValIsNotZero = IfCondition();
                    ifRetValIsNotZero.Condition = BinaryOpertorExpression(VariableReferenceExp("lRetVal"), CodeBinaryOperatorType.IdentityInequality, PrimitiveExpression(0));
                    ifRetValIsNotZero.TrueStatements.Add(MethodInvocationExp(ThisReferenceExp(), "Process_MethodError_Info").AddParameters(new CodeExpression[] { GetProperty(TypeReferenceExp(typeof(string)), "Empty"), FieldReferenceExp(TypeReferenceExp("CUtil"), "APP_ERROR"), VariableReferenceExp("lRetVal"), VariableReferenceExp("nLoop"), PrimitiveExpression(int.Parse(mt.ID)), PrimitiveExpression(int.Parse(ps.SeqNO)), PrimitiveExpression(int.Parse(mt.SeqNO)) }));
                    tryForBRO.AddStatement(ifRetValIsNotZero);



                    CodeCatchClause catchCRVWException = tryForBRO.AddCatch("CRVWException", "rvwe");
                    ThrowException(catchCRVWException, "rvwe");

                    CodeCatchClause catchOtherException = AddCatchBlock(tryForBRO, "e");
                    catchOtherException.AddStatement(MethodInvocationExp(ThisReferenceExp(), "Process_MethodError_Info").AddParameters(new CodeExpression[] { GetProperty("e", "Message"), FieldReferenceExp(TypeReferenceExp("CUtil"), "UNKNOWN_ERROR"), PrimitiveExpression(1000), VariableReferenceExp("nLoop"), PrimitiveExpression(int.Parse(mt.ID)), PrimitiveExpression(int.Parse(ps.SeqNO)), PrimitiveExpression(int.Parse(mt.SeqNO)) }));

                    statements.Add(tryForBRO);

                    IEnumerable<Parameter> singleSegOutParams = mt.Parameters.Where(p => p.Seg != null && p.Seg.Inst == SINGLE_INSTANCE && p.DI != null && (p.FlowDirection == FlowAttribute.OUT || p.FlowDirection == FlowAttribute.INOUT)).OrderBy(p => p.Name);

                    #region out binding for header segment
                    foreach (Parameter param in singleSegOutParams)
                    {
                        string sParamName = param.Name;
                        string sSegName = param.Seg.Name.ToLower();
                        string sDIName = param.DI.Name.ToLower();
                        string sVariableName = string.Concat("sz", sSegName, sDIName);
                        string sCollectionName = string.Concat("nvc", sSegName.ToUpper());

                        switch (param.CategorizedDataType)
                        {
                            case DataType.INT:
                                sParamName = string.Concat("l", param.Seg.Name.ToLower(), param.DI.Name.ToLower());
                                break;
                            case DataType.STRING:
                            case DataType.TIMESTAMP:
                            case DataType.VARBINARY:
                                sParamName = string.Concat("sz", param.Seg.Name.ToLower(), param.DI.Name.ToLower());
                                break;
                            case DataType.DOUBLE:
                                sParamName = string.Concat("dbl", param.Seg.Name.ToLower(), param.DI.Name.ToLower());
                                break;
                            default:
                                sParamName = string.Concat("sz", param.Seg.Name.ToLower(), param.DI.Name.ToLower());
                                break;
                        }

                        statements.Add(CommentStatement(sParamName));
                        statements.Add(AssignVariable(sVariableName, MethodInvocationExp(VariableReferenceExp(sParamName), "ToString")));
                        statements.Add(AssignVariable(VariableReferenceExp("sValue"), GetProperty(TypeReferenceExp(typeof(string)), "Empty")));

                        CodeConditionStatement ifOutValueIsNotNull = IfCondition();
                        ifOutValueIsNotNull.Condition = BinaryOpertorExpression(VariableReferenceExp(sVariableName), CodeBinaryOperatorType.IdentityInequality, SnippetExpression("null"));
                        ifOutValueIsNotNull.TrueStatements.Add(AssignVariable(VariableReferenceExp("sValue"), VariableReferenceExp(sVariableName)));

                        if (param.DataType == DataType.DATE || param.DataType == DataType.TIME || param.DataType == DataType.DATETIME || param.DataType == DataType.DATE_TIME)
                        {
                            CodeConditionStatement ifAssignedValIsNotEmpty = IfCondition();
                            ifAssignedValIsNotEmpty.Condition = BinaryOpertorExpression(VariableReferenceExp("sValue"), CodeBinaryOperatorType.IdentityInequality, GetProperty(TypeReferenceExp(typeof(string)), "Empty"));

                            switch (param.DataType)
                            {
                                case DataType.DATE:
                                    ifAssignedValIsNotEmpty.TrueStatements.Add(AssignVariable(VariableReferenceExp("sValue"), MethodInvocationExp(BaseReferenceExp(), "DateTimeParse").AddParameters(new CodeExpression[] { VariableReferenceExp("sValue"), PrimitiveExpression("yyyy-MM-dd") })));
                                    break;
                                case DataType.TIME:
                                    ifAssignedValIsNotEmpty.TrueStatements.Add(AssignVariable(VariableReferenceExp("sValue"), MethodInvocationExp(BaseReferenceExp(), "DateTimeParse").AddParameters(new CodeExpression[] { VariableReferenceExp("sValue"), PrimitiveExpression("HH:mm:ss") })));
                                    break;
                                case DataType.DATETIME:
                                case DataType.DATE_TIME:
                                    ifAssignedValIsNotEmpty.TrueStatements.Add(AssignVariable(VariableReferenceExp("sValue"), MethodInvocationExp(BaseReferenceExp(), "DateTimeParse").AddParameters(new CodeExpression[] { VariableReferenceExp("sValue"), PrimitiveExpression("yyyy-MM-dd HH:mm:ss") })));
                                    break;
                                default:
                                    ifAssignedValIsNotEmpty.TrueStatements.Add(AssignVariable(VariableReferenceExp("sValue"), VariableReferenceExp(sVariableName)));
                                    break;
                            }


                            ifOutValueIsNotNull.TrueStatements.Add(ifAssignedValIsNotEmpty);
                        }
                        statements.Add(ifOutValueIsNotNull);

                        statements.Add(AssignVariable(ArrayIndexerExpression(sCollectionName, PrimitiveExpression(param.DI.Name.ToLower())), VariableReferenceExp("sValue")));
                    }
                    #endregion

                    statements.Add(SnippetExpression("break"));
                }
                #endregion

                return statements;
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("PS_SpExecution->{0}", !Object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }


        private CodeStatementCollection PS_BRExecution_OutBinding(ProcessSection ps, Method mt, IEnumerable<Parameter> outParams)
        {
            CodeStatementCollection statements = new CodeStatementCollection();

            IEnumerable<DataItem> keyDIs = (from s in _service.Segments.Where(s => s.Inst == MULTI_INSTANCE)
                                            from d in s.DataItems
                                            join p in outParams
                                            on new { segmentName = s.Name, dataitemName = d.Name } equals new { segmentName = p.Seg.Name, dataitemName = p.DI.Name }
                                            select d).Where(d => d.PartOfKey == true);


            try
            {

                IEnumerable<Segment> multiSegments = outParams.Where(p => p.Seg != null).Where(p => p.Seg.Inst == MULTI_INSTANCE).Select(p => p.Seg);

                //added for 2.0.0.1
                bool outBindingHasRecordSet = outParams.Where(p => !string.IsNullOrEmpty(p.RecordSetName)).Any();


                CodeTryCatchFinallyStatement outParamTry = new CodeTryCatchFinallyStatement();
                CodeCatchClause catchException = outParamTry.AddCatch("Exception", "e");
                catchException.AddStatement(MethodInvocationExp(BaseReferenceExp(), "Set_Error_Info").AddParameters(new CodeExpression[] { PrimitiveExpression(0),
                                                                                                                                                        FieldReferenceExp(TypeReferenceExp("CUtil"),"FRAMEWORK_ERROR"),
                                                                                                                                                        FieldReferenceExp(TypeReferenceExp("CUtil"),"STOP_PROCESSING"),
                                                                                                                                                        MethodInvocationExp(TypeReferenceExp(typeof(string)),"Format").AddParameters(new CodeExpression[] { PrimitiveExpression("General Exception during OUT Data Binding of PS - "+ ps.SeqNO+" BR - "+mt.SeqNO),GetProperty("e","Message") }),
                                                                                                                                                        GetProperty(TypeReferenceExp(typeof(string)),"Empty"),
                                                                                                                                                        GetProperty(TypeReferenceExp(typeof(string)),"Empty"),
                                                                                                                                                        PrimitiveExpression(0),
                                                                                                                                                        GetProperty(TypeReferenceExp(typeof(string)),"Empty"),
                                                                                                                                                        GetProperty(TypeReferenceExp(typeof(string)),"Empty")
                                                                                                                                                        }));
                statements.Add(outParamTry);
                //AddCatchClause(outParamTry, "ProcessSection");

                //modified for 2.0.0.1 and reverted
                if (!multiSegments.Any())
                //if (!outBindingHasRecordSet)
                {
                    if (outParams.Any())
                    {
                        outParamTry.AddStatement(AssignVariable(VariableReferenceExp("nRecCount"), PrimitiveExpression(0)));
                        outParamTry.AddStatement(AssignVariable(VariableReferenceExp("nRecCount"), VariableReferenceExp("nLoop")));
                        //switch (mt.ExecutionFlg_r)
                        //{
                        //    case ExecutionFlag.CURRENT:
                        //        outParamTry.AddStatement(CommentStatement("Current instance"));
                        //        outParamTry.AddStatement(AssignVariable(VariableReferenceExp("nRecCount"), VariableReferenceExp("nLoop")));
                        //        break;
                        //    case ExecutionFlag.LOCATE:
                        //        outParamTry.AddStatement(CommentStatement("Locate instance"));
                        //        outParamTry.AddStatement(AssignVariable(VariableReferenceExp("nRecCount"), PrimitiveExpression(0)));
                        //        break;
                        //    case ExecutionFlag.NEW:
                        //        outParamTry.AddStatement(CommentStatement("New instance"));
                        //        if (multiSegments.Any())
                        //            outParamTry.AddStatement(AssignVariable(VariableReferenceExp("nRecCount"), GetProperty(String.Format("ht{0}", multiSegments.First().Name), "Count")));
                        //        break;
                        //}

                        CodeConditionStatement CheckForOutData = IfCondition();
                        CheckForOutData.Condition = BinaryOpertorExpression(MethodInvocationExp(BaseReferenceExp(), "IsDataReader_Accessible"), CodeBinaryOperatorType.BooleanAnd, MethodInvocationExp(BaseReferenceExp(), "Read"));
                        outParamTry.AddStatement(CheckForOutData);


                        CodeConditionStatement CheckForResultSetBinding = IfCondition();
                        CheckForResultSetBinding.Condition = BinaryOpertorExpression(BinaryOpertorExpression(VariableReferenceExp("iEDKServiceES"), CodeBinaryOperatorType.BooleanAnd, BinaryOpertorExpression(VariableReferenceExp("psIndex"), CodeBinaryOperatorType.IdentityInequality, PrimitiveExpression(-1))),
                                                                                    CodeBinaryOperatorType.BooleanAnd,
                                                                                    BinaryOpertorExpression(BinaryOpertorExpression(VariableReferenceExp("brIndex"), CodeBinaryOperatorType.IdentityInequality, PrimitiveExpression(-1)), CodeBinaryOperatorType.BooleanAnd, BinaryOpertorExpression(VariableReferenceExp("cBROutExists"), CodeBinaryOperatorType.IdentityInequality, PrimitiveExpression(-1))));
                        CheckForResultSetBinding.TrueStatements.Add(MethodInvocationExp(BaseReferenceExp(), "ESResultsetBinding").AddParameters(new CodeExpression[] { VariableReferenceExp("psIndex"), VariableReferenceExp("brIndex"), SnippetExpression("null") }));
                        CheckForOutData.TrueStatements.Add(CheckForResultSetBinding);

                        foreach (CodeStatement statement in PS_AddOutParameters(ps, mt, outParams, keyDIs))
                        {
                            CheckForOutData.TrueStatements.Add(statement);
                        }

                        //modified for 2.0.0.1 - whole condition and code and reverted
                        //if (multiSegments.Any())
                        //{
                        //    CheckForOutData.TrueStatements.Add(AssignVariable(ArrayIndexerExpression(String.Format("ht{0}", multiSegments.First().Name), keyDIs.Any() ? VariableReferenceExp("lInstExists") : VariableReferenceExp("nRecCount")), VariableReferenceExp("nvcTmp")));
                        //}
                    }
                }
                //modified for 2.0.0.1 and reverted
                else if (multiSegments.Any())
                //else if (outBindingHasRecordSet)
                {
                    //Multisegment with out param alone
                    if (multiSegments.First().FlowDirection == FlowAttribute.OUT || multiSegments.First().FlowDirection == FlowAttribute.INOUT)
                    //if(multiSegments.First().FlowDirection == FlowAttribute.OUT)
                    //if (outParams.Where(p => p.Seg.Inst == MULTI_INSTANCE).All(p => p.FlowDirection == FlowAttribute.OUT))
                    {

                        //modified for 2.0.0.1 and reverted
                        if (keyDIs.Any())
                            outParamTry.AddStatement(AssignVariable(VariableReferenceExp("nRecCount"), PrimitiveExpression(0)));
                        else
                            outParamTry.AddStatement(AssignVariable(VariableReferenceExp("nRecCount"), GetProperty(String.Format("ht{0}", multiSegments.First().Name), "Count")));
                        //switch (mt.ExecutionFlg_r)
                        //{
                        //    case ExecutionFlag.CURRENT:
                        //        outParamTry.AddStatement(CommentStatement("Current instance"));
                        //        outParamTry.AddStatement(AssignVariable(VariableReferenceExp("nRecCount"), VariableReferenceExp("nLoop")));
                        //        break;
                        //    case ExecutionFlag.LOCATE:
                        //        outParamTry.AddStatement(CommentStatement("Locate instance"));
                        //        outParamTry.AddStatement(AssignVariable(VariableReferenceExp("nRecCount"), PrimitiveExpression(0)));
                        //        break;
                        //    case ExecutionFlag.NEW:
                        //        outParamTry.AddStatement(CommentStatement("New instance"));
                        //        if (multiSegments.Any())
                        //            outParamTry.AddStatement(AssignVariable(VariableReferenceExp("nRecCount"), GetProperty(String.Format("ht{0}", multiSegments.First().Name), "Count")));
                        //        break;
                        //}

                        CodeConditionStatement CheckForOutData = IfCondition();
                        CheckForOutData.Condition = MethodInvocationExp(BaseReferenceExp(), "IsDataReader_Accessible");
                        outParamTry.AddStatement(CheckForOutData);

                        CheckForOutData.TrueStatements.Add(SnippetStatement("while(Read())"));
                        CheckForOutData.TrueStatements.Add(SnippetStatement("{"));

                        if (keyDIs.Any() == false)
                        {
                            CheckForOutData.TrueStatements.Add(AssignVariable(VariableReferenceExp("nRecCount"), BinaryOpertorExpression(VariableReferenceExp("nRecCount"), CodeBinaryOperatorType.Add, PrimitiveExpression(1))));
                        }


                        //foreach (CodeStatement statement in PS_AddOutParameters(ps, mt, outParams, keyDIs))
                        //{
                        //    CheckForOutData.TrueStatements.Add(statement);
                        //}

                        CodeConditionStatement CheckForResultSetBinding = IfCondition();
                        CheckForResultSetBinding.Condition = BinaryOpertorExpression(BinaryOpertorExpression(VariableReferenceExp("iEDKServiceES"), CodeBinaryOperatorType.BooleanAnd, BinaryOpertorExpression(VariableReferenceExp("psIndex"), CodeBinaryOperatorType.IdentityInequality, PrimitiveExpression(-1))),
                                                                                    CodeBinaryOperatorType.BooleanAnd,
                                                                                    BinaryOpertorExpression(BinaryOpertorExpression(VariableReferenceExp("brIndex"), CodeBinaryOperatorType.IdentityInequality, PrimitiveExpression(-1)), CodeBinaryOperatorType.BooleanAnd, BinaryOpertorExpression(VariableReferenceExp("cBROutExists"), CodeBinaryOperatorType.IdentityInequality, PrimitiveExpression(-1))));
                        CheckForResultSetBinding.TrueStatements.Add(MethodInvocationExp(BaseReferenceExp(), "ESResultsetBinding").AddParameters(new CodeExpression[] { VariableReferenceExp("psIndex"), VariableReferenceExp("brIndex"), VariableReferenceExp("nvcTmp") }));
                        CheckForOutData.TrueStatements.Add(CheckForResultSetBinding);

                        foreach (CodeStatement statement in PS_AddOutParameters(ps, mt, outParams, keyDIs))
                        {
                            CheckForOutData.TrueStatements.Add(statement);
                        }

                        CheckForOutData.TrueStatements.Add(AssignVariable(ArrayIndexerExpression(String.Format("ht{0}", multiSegments.First().Name), keyDIs.Any() ? VariableReferenceExp("lInstExists") : VariableReferenceExp("nRecCount")), VariableReferenceExp("nvcTmp")));

                        CheckForOutData.TrueStatements.Add(SnippetStatement("}"));
                    }
                    ////mutlisegment with inout param
                    //else
                    //{
                    //    outParamTry.AddStatement(AssignVariable(VariableReferenceExp("nRecCount"), PrimitiveExpression(0)));
                    //    outParamTry.AddStatement(AssignVariable(VariableReferenceExp("nRecCount"), VariableReferenceExp("nLoop")));
                    //    CodeConditionStatement CheckForOutData = IfCondition();
                    //    CheckForOutData.Condition = BinaryOpertorExpression(MethodInvocationExp(BaseReferenceExp(), "IsDataReader_Accessible"), CodeBinaryOperatorType.BooleanAnd, SnippetExpression("Read()"));

                    //    //foreach (CodeStatement statement in PS_AddOutParameters(ps, mt, outParams, keyDIs))
                    //    //{
                    //    //    CheckForOutData.TrueStatements.Add(statement);
                    //    //}

                    //    CodeConditionStatement CheckForResultSetBinding = IfCondition();
                    //    CheckForResultSetBinding.Condition = BinaryOpertorExpression(BinaryOpertorExpression(VariableReferenceExp("iEDKServiceES"), CodeBinaryOperatorType.BooleanAnd, BinaryOpertorExpression(VariableReferenceExp("psIndex"), CodeBinaryOperatorType.IdentityInequality, PrimitiveExpression(-1))),
                    //                                                                CodeBinaryOperatorType.BooleanAnd,
                    //                                                                BinaryOpertorExpression(BinaryOpertorExpression(VariableReferenceExp("brIndex"), CodeBinaryOperatorType.IdentityInequality, PrimitiveExpression(-1)), CodeBinaryOperatorType.BooleanAnd, BinaryOpertorExpression(VariableReferenceExp("cBROutExists"), CodeBinaryOperatorType.IdentityInequality, PrimitiveExpression(-1))));
                    //    CheckForResultSetBinding.TrueStatements.Add(MethodInvocationExp(BaseReferenceExp(), "ESResultsetBinding").AddParameters(new CodeExpression[] { VariableReferenceExp("psIndex"), VariableReferenceExp("brIndex"), SnippetExpression("null") }));
                    //    CheckForOutData.TrueStatements.Add(CheckForResultSetBinding);

                    //    foreach (CodeStatement statement in PS_AddOutParameters(ps, mt, outParams, keyDIs))
                    //    {
                    //        CheckForOutData.TrueStatements.Add(statement);
                    //    }

                    //    CheckForOutData.TrueStatements.Add(AssignVariable(ArrayIndexerExpression(String.Format("ht{0}", multiSegments.First().Name), VariableReferenceExp("nRecCount")), VariableReferenceExp("nvcTmp")));

                    //    outParamTry.AddStatement(CheckForOutData);
                    //}
                }

                return statements;
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("PS_BRExecution_OutBinding->{0}", !Object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }

        private CodeStatementCollection PS_BRExecution_DeclareVariable(ProcessSection ps, Method method)
        {
            CodeStatementCollection statements;
            statements = new CodeStatementCollection();

            //if (ps.ProcessingType == ProcessingType.ALTERNATE)
            //{
            //    methodLooping.Statements.Add(AssignVariable(VariableReferenceExp(psIndex.Name), PrimitiveExpression(-1)));
            //    methodLooping.Statements.Add(AssignVariable(VariableReferenceExp(brIndex.Name), PrimitiveExpression(-1)));
            //    methodLooping.Statements.Add(AssignVariable(VariableReferenceExp(psSeqNo.Name), PrimitiveExpression(int.Parse(ps.SeqNO))));
            //    methodLooping.Statements.Add(AssignVariable(VariableReferenceExp(brSeqNo.Name), PrimitiveExpression(int.Parse(method.SeqNO))));
            //}
            //else
            //{
            statements.Add(AssignVariable(VariableReferenceExp("psIndex"), PrimitiveExpression(-1)));
            statements.Add(AssignVariable(VariableReferenceExp("brIndex"), PrimitiveExpression(-1)));
            statements.Add(AssignVariable(VariableReferenceExp("psSeqNo"), PrimitiveExpression(int.Parse(ps.SeqNO))));
            statements.Add(AssignVariable(VariableReferenceExp("brSeqNo"), PrimitiveExpression(int.Parse(method.SeqNO))));
            //}

            string mtLoopSegmentName = string.Empty;
            CodeAssignStatement resettingnMax = null;
            CodeAssignStatement assignment = null;
            if (ps.ProcessingType == ProcessingType.DEFAULT)
            {
                if (method.LoopSegment != null)
                {
                    mtLoopSegmentName = method.LoopSegment.Name.ToUpperInvariant();

                    resettingnMax = AssignVariable(VariableReferenceExp("nMax"), PrimitiveExpression(0));
                    assignment = AssignVariable(VariableReferenceExp("nMax"), GetProperty("ht" + mtLoopSegmentName, "Count"));
                }
                else
                {
                    resettingnMax = AssignVariable(VariableReferenceExp("nMax"), PrimitiveExpression(1));
                }

                if (resettingnMax != null)
                    statements.Add(resettingnMax);
                if (assignment != null)
                    statements.Add(assignment);
            }
            else
            {
                if (ps.CtrlExp == null && ps.LoopCausingSegment == null)
                {
                    resettingnMax = AssignVariable(VariableReferenceExp("nMax"), PrimitiveExpression(1));
                    assignment = AssignVariable(VariableReferenceExp("nLoop"), PrimitiveExpression(1));
                    if (resettingnMax != null)
                        statements.Add(resettingnMax);
                    if (assignment != null)
                        statements.Add(assignment);
                }
            }

            return statements;
        }

        private CodeStatementCollection PS_BRExecution_IS(ProcessSection ps, Method mt)
        {
            CodeStatementCollection statements = new CodeStatementCollection();
            try
            {
                CodeConditionStatement Checkbmtflag = IfCondition();
                string integrationObject = "ObjInteg" + mt.SeqNO;

                CodeCommentStatement comment = CommentStatement("Starting to execute the IS - " + mt.SeqNO + "  under the process section - " + ps.SeqNO);

                statements.Add(comment);

                statements.Add(AssignVariable(VariableReferenceExp("psIndex"), PrimitiveExpression(-1)));
                statements.Add(AssignVariable(VariableReferenceExp("brIndex"), PrimitiveExpression(-1)));
                statements.Add(AssignVariable(VariableReferenceExp("psSeqNo"), PrimitiveExpression(int.Parse(ps.SeqNO))));
                statements.Add(AssignVariable(VariableReferenceExp("brSeqNo"), PrimitiveExpression(int.Parse(mt.SeqNO))));

                if (mt.CtrlExp != null)
                {
                    //if (ps.ProcessingType == ProcessingType.ALTERNATE)
                    //{
                    statements.Add(AssignVariable(VariableReferenceExp("bmtflag"), PrimitiveExpression(true)));
                    CodeConditionStatement CheckCtrlExp = IfCondition();

                    if (mt.CtrlExp.Seg.Inst == MULTI_INSTANCE)
                    {
                        statements.Add(AssignVariable(VariableReferenceExp("nvcTmpCrtl"), SnippetExpression(String.Format("(NameValueCollection) ht{0}[nLoop]", mt.CtrlExp.Seg.Name.ToUpperInvariant()))));
                        CheckCtrlExp.Condition = BinaryOpertorExpression(BinaryOpertorExpression(VariableReferenceExp("nvcTmpCrtl"), CodeBinaryOperatorType.IdentityInequality, PrimitiveExpression(null))
                                                                        , CodeBinaryOperatorType.BooleanAnd
                                                                        , BinaryOpertorExpression(ArrayIndexerExpression("nvcTmpCrtl", PrimitiveExpression(mt.CtrlExp.DI.Name.ToLowerInvariant())), CodeBinaryOperatorType.IdentityInequality, PrimitiveExpression("0")));
                    }
                    else
                    {

                        CheckCtrlExp.Condition = BinaryOpertorExpression(BinaryOpertorExpression(VariableReferenceExp("nvc" + mt.CtrlExp.Seg.Name.ToUpperInvariant()), CodeBinaryOperatorType.IdentityInequality, PrimitiveExpression(null))
                                                                        , CodeBinaryOperatorType.BooleanAnd
                                                                        , BinaryOpertorExpression(ArrayIndexerExpression("nvc" + mt.CtrlExp.Seg.Name.ToUpperInvariant(), PrimitiveExpression(mt.CtrlExp.DI.Name.ToLowerInvariant())), CodeBinaryOperatorType.IdentityInequality, PrimitiveExpression("0")));
                    }

                    CheckCtrlExp.TrueStatements.Add(AssignVariable(VariableReferenceExp("bmtflag"), PrimitiveExpression(false)));
                    statements.Add(CheckCtrlExp);
                    //}

                    Checkbmtflag = IfCondition();
                    Checkbmtflag.Condition = BinaryOpertorExpression(VariableReferenceExp("bmtflag"), CodeBinaryOperatorType.IdentityEquality, PrimitiveExpression(true));
                    Checkbmtflag.TrueStatements.Add(CommentStatement("Control Expression evaluated to TRUE"));
                    statements.Add(Checkbmtflag);

                }

                CodeTryCatchFinallyStatement ISTry = new CodeTryCatchFinallyStatement();

                CodeCatchClause catchCRVWException = ISTry.AddCatch("CRVWException", "rvwe");
                ThrowException(catchCRVWException, "rvwe");

                CodeCatchClause catchOtherException = ISTry.AddCatch("Exception", "e");
                catchOtherException.AddStatement(MethodInvocationExp(BaseReferenceExp(), "Set_Error_Info").AddParameters(new CodeExpression[] { PrimitiveExpression(0),
                    FieldReferenceExp(TypeReferenceExp("CUtil"), "FRAMEWORK_ERROR"),
                    FieldReferenceExp(TypeReferenceExp("CUtil"), "STOP_PROCESSING"),
                    MethodInvocationExp(TypeReferenceExp(typeof(string)), "Format").AddParameters(new CodeExpression[] { PrimitiveExpression("General Exception at PS - " + ps.SeqNO + " IS - " + mt.SeqNO + " {0}"), GetProperty("e", "Message") }),
                    GetProperty(TypeReferenceExp(typeof(string)), "Empty"),
                    GetProperty(TypeReferenceExp(typeof(string)), "Empty"),
                    PrimitiveExpression(0),
                    GetProperty(TypeReferenceExp(typeof(string)), "Empty"),
                    GetProperty(TypeReferenceExp(typeof(string)), "Empty")
                }));

                if (mt.CtrlExp != null)
                    Checkbmtflag.TrueStatements.Add(ISTry);
                else
                    statements.Add(ISTry);

                /*
                CodeCatchClause catchCRWExceptionIS = new CodeCatchClause();
                ThrowException(catchCRWExceptionIS, "rvwe");
                ISTry.CatchClauses.Add(catchCRWExceptionIS);

                CodeCatchClause catchOtherExceptionsIS = new CodeCatchClause();
                catchOtherExceptionsIS.AddStatement(MethodInvocationExp(BaseReferenceExp(), "Set_Error_Info").AddParameters(new CodeExpression[] { PrimitiveExpression(0),
                                                                                                                                                            VariableReferenceExp("FRAMEWORK_ERROR"),
                                                                                                                                                            VariableReferenceExp("STOP_PROCESSING"),
                                                                                                                                                            MethodInvocationExp(TypeReferenceExp("String"),"Format").AddParameters(new CodeExpression[]{PrimitiveExpression("General Exception at PS - "+ps.SeqNO+" IS - "+method.SeqNO),GetProperty("e","Message")}),
                                                                                                                                                            GetProperty("String", "Empty"),
                                                                                                                                                            GetProperty("String","Empty"),
                                                                                                                                                            PrimitiveExpression(0),
                                                                                                                                                            GetProperty("String","Empty"),
                                                                                                                                                            GetProperty("String","Empty"),
                                                                                                                                                            }));
                ISTry.CatchClauses.Add(catchOtherExceptionsIS);
                */

                if (ps.ProcessingType == ProcessingType.DEFAULT)
                {
                    CodeConditionStatement checkForIEDKExtension = IfCondition();
                    checkForIEDKExtension.Condition = VariableReferenceExp("iEDKServiceES");
                    checkForIEDKExtension.TrueStatements.Add(AssignVariable(VariableReferenceExp("psIndex"), MethodInvocationExp(BaseReferenceExp(), "IsProcessSectionExists").AddParameter(PrimitiveExpression(ps.Name.ToLower()))));
                    ISTry.AddStatement(checkForIEDKExtension);
                }

                CodeConditionStatement CheckForIBRExecution = IfCondition();
                CheckForIBRExecution.Condition = BinaryOpertorExpression(VariableReferenceExp("psIndex"), CodeBinaryOperatorType.IdentityInequality, PrimitiveExpression(-1));
                if (ps.ProcessingType == ProcessingType.ALTERNATE)
                    CheckForIBRExecution.TrueStatements.Add(MethodInvocationExp(BaseReferenceExp(), "PrepareIBRForExecution").AddParameters(new CodeExpression[] { VariableReferenceExp("psIndex"), VariableReferenceExp("brSeqNo"), PrimitiveExpression(0), VariableReferenceExp("nLoop") }));
                else
                    CheckForIBRExecution.TrueStatements.Add(MethodInvocationExp(BaseReferenceExp(), "PrepareIBRForExecution").AddParameters(new CodeExpression[] { VariableReferenceExp("psIndex"), VariableReferenceExp("brSeqNo"), PrimitiveExpression(0), PrimitiveExpression(0) }));

                ISTry.AddStatement(CheckForIBRExecution);

                CodeVariableDeclarationStatement integObjectCreation = DeclareVariableAndAssign(String.Format("com.ramco.vw.{0}.service.C{1}", mt.IS.ISComponent.ToLower(), mt.IS.ISServiceName.ToLower()), "ObjInteg" + mt.SeqNO, true, ObjectCreateExpression(String.Format("com.ramco.vw.{0}.service.C{1}", mt.IS.ISComponent.ToLower(), mt.IS.ISServiceName.ToLower()), null));
                ISTry.AddStatement(integObjectCreation);

                CodeConditionStatement CheckIntegObjectForNull = IfCondition();
                CheckIntegObjectForNull.Condition = BinaryOpertorExpression(VariableReferenceExp(integrationObject), CodeBinaryOperatorType.IdentityEquality, PrimitiveExpression(null));
                CheckIntegObjectForNull.TrueStatements.Add(MethodInvocationExp(BaseReferenceExp(), "Set_Error_Info").AddParameters(new CodeExpression[]{PrimitiveExpression(0),
                                                                                                                                                                VariableReferenceExp("FRAMEWORK_ERROR"),
                                                                                                                                                                VariableReferenceExp("STOP_PROCESSING"),
                                                                                                                                                                PrimitiveExpression("RVWException at PS - "+ps.SeqNO+" IS - "+mt.SeqNO+" Not able to invoke IntegrationService Object for Execution."),
                                                                                                                                                                GetProperty("String","Empty"),
                                                                                                                                                                GetProperty("String","Empty"),
                                                                                                                                                                PrimitiveExpression(0),
                                                                                                                                                                GetProperty("String","Empty"),
                                                                                                                                                                GetProperty("String","Empty")
                                                                                                                                                                }));

                ISTry.AddStatement(CheckIntegObjectForNull);
                ISTry.AddStatement(SetProperty(integrationObject, "bIsInteg", PrimitiveExpression(true)));

                foreach (CodeStatement statement in PS_IS_INBinding(ps, mt))
                {
                    ISTry.AddStatement(statement);
                }

                ISTry.AddStatement(AssignVariable(VariableReferenceExp("lRetVal"), MethodInvocationExp(TypeReferenceExp(integrationObject), "ProcessService").AddParameters(new CodeExpression[] { GetProperty("String", "Empty"), GetProperty("String", "Empty"), SnippetExpression("out sValue") })));
                ISTry.AddStatement(DeclareVariableAndAssign("NameValueCollection", "TempErrHash", true, SnippetExpression("null")));

                CodeIterationStatement IterateErrorDetails = ForLoopExpression(DeclareVariableAndAssign(typeof(long), "err_Count", true, PrimitiveExpression(1)),
                                                                                BinaryOpertorExpression(VariableReferenceExp("err_Count"), CodeBinaryOperatorType.LessThanOrEqual, GetProperty(GetProperty(integrationObject, "htERRORDETAILS"), "Count")),
                                                                                AssignVariable(VariableReferenceExp("err_Count"), BinaryOpertorExpression(VariableReferenceExp("err_Count"), CodeBinaryOperatorType.Add, PrimitiveExpression(1)))
                                                                                );
                IterateErrorDetails.Statements.Add(AssignVariable(VariableReferenceExp("TempErrHash"), SnippetExpression("(NameValueCollection) " + integrationObject + ".htERRORDETAILS[err_Count]")));
                IterateErrorDetails.Statements.Add(SnippetExpression("htERRORDETAILS[(long)htERRORDETAILS.Count + 1] = TempErrHash"));
                IterateErrorDetails.Statements.Add(AssignVariable(VariableReferenceExp("TempErrHash"), SnippetExpression("null")));
                ISTry.AddStatement(IterateErrorDetails);

                CodeConditionStatement CheckRetValForFailure = IfCondition();
                CheckRetValForFailure.Condition = BinaryOpertorExpression(VariableReferenceExp("lRetVal"), CodeBinaryOperatorType.IdentityEquality, VariableReferenceExp("ATMA_FAILURE"));
                CheckRetValForFailure.TrueStatements.Add(ThrowNewException(ObjectCreateExpression("CRVWException", new CodeExpression[] { PrimitiveExpression("ATMA_FAILURE") })));
                ISTry.AddStatement(CheckRetValForFailure);

                foreach (CodeStatement statement in PS_IS_OutBinding(ps, mt))
                {
                    ISTry.AddStatement(statement);
                }

                CodeConditionStatement CheckPsIndexForBRExecution = IfCondition();
                CheckPsIndexForBRExecution.Condition = BinaryOpertorExpression(VariableReferenceExp("psIndex"), CodeBinaryOperatorType.IdentityInequality, PrimitiveExpression(-1));
                if (ps.ProcessingType == ProcessingType.ALTERNATE)
                    CheckPsIndexForBRExecution.TrueStatements.Add(MethodInvocationExp(BaseReferenceExp(), "PrepareIBRForExecution").AddParameters(new CodeExpression[] { VariableReferenceExp("psIndex"), VariableReferenceExp("brSeqNo"), PrimitiveExpression(1), VariableReferenceExp("nLoop") }));
                else
                    CheckPsIndexForBRExecution.TrueStatements.Add(MethodInvocationExp(BaseReferenceExp(), "PrepareIBRForExecution").AddParameters(new CodeExpression[] { VariableReferenceExp("psIndex"), VariableReferenceExp("brSeqNo"), PrimitiveExpression(1), PrimitiveExpression(0) }));
                ISTry.AddStatement(CheckPsIndexForBRExecution);
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("PS_BRExecution_IS->{0}", !Object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }

            return statements;
        }

        private CodeStatementCollection PS_IS_INBinding(ProcessSection ps, Method mt)
        {
            CodeStatementCollection statements = null;
            try
            {
                statements = new CodeStatementCollection();
                string integrationObject = "ObjInteg" + mt.SeqNO;

                statements.Add(SnippetExpression(String.Format("ObjInteg{0}.nvcFW_CONTEXT[\"component\"] = \"{1}\"", mt.SeqNO, mt.IS.ISComponent.ToLowerInvariant())));
                statements.Add(SnippetExpression(String.Format("ObjInteg{0}.nvcFW_CONTEXT[\"service\"] = \"{1}\"", mt.SeqNO, mt.IS.ISServiceName.ToLowerInvariant())));
                //Take the ou instance mapping
                var vISOuMapping = mt.IS.Mappings.Where(i => i.ISDataItem.Name == "ouinstance");
                if (vISOuMapping.Any())
                {
                    ISMapping ISOuMapping = vISOuMapping.First();
                    if (ISOuMapping.CallerSegment.Name.ToLower() != "fw_context")
                    {
                        if (ISOuMapping.CallerSegment.Inst == MULTI_INSTANCE)
                            statements.Add(AssignVariable(VariableReferenceExp("nvcTmpCrtl"), SnippetExpression(string.Format("(NameValueCollection) ht{0}[nLoop]", ISOuMapping.CallerSegment.Name.ToUpper()))));

                        statements.Add(AssignVariable(VariableReferenceExp("sValue"), SnippetExpression(string.Format("(string) nvc{0}[\"{1}\"]", ISOuMapping.CallerSegment.Inst.Equals(MULTI_INSTANCE) ? "TmpCrtl" : ISOuMapping.CallerSegment.Name.ToUpper(), ISOuMapping.CallerDataItem.Name.ToLower()))));

                    }
                    statements.Add(SnippetExpression(string.Format("ObjInteg{0}.nvcFW_CONTEXT[\"ouinstance\"] = {1}", mt.SeqNO, ISOuMapping.CallerSegment.Name.ToLower().Equals("fw_context") ? "szOUI" : "sValue")));
                }
                statements.Add(SnippetExpression(String.Format("ObjInteg{0}.nvcFW_CONTEXT[\"componentinstance\"] = szCompInst", mt.SeqNO)));
                statements.Add(SnippetExpression(String.Format("ObjInteg{0}.nvcFW_CONTEXT[\"sectoken\"] = szSecToken", mt.SeqNO)));
                statements.Add(SnippetExpression(String.Format("ObjInteg{0}.nvcFW_CONTEXT[\"language\"] = szLangID", mt.SeqNO)));
                statements.Add(SnippetExpression(String.Format("ObjInteg{0}.nvcFW_CONTEXT[\"user\"] = szUser", mt.SeqNO)));
                statements.Add(SnippetExpression(String.Format("ObjInteg{0}.nvcFW_CONTEXT[\"role\"] = szRole", mt.SeqNO)));
                statements.Add(SnippetStatement(Environment.NewLine));

                foreach (Segment callerSegment in mt.IS.Mappings.Where(i => i.ISSegment.Name.ToLower() != "fw_context" && (i.ISDataItem.FlowDirection == FlowAttribute.IN || i.ISDataItem.FlowDirection == FlowAttribute.INOUT)).Select(m => m.CallerSegment).Distinct())
                {
                    if (callerSegment.Inst == MULTI_INSTANCE)
                    {
                        if (ps.ProcessingType != ProcessingType.ALTERNATE) //TECH-XXXX - check added
                            statements.Add(AssignVariable(VariableReferenceExp("nMax"), GetProperty(VariableReferenceExp("ht" + callerSegment.Name.ToUpper()), "Count")));

                        //TECH-XXXX - code modification starts
                        CodeStatementCollection integInParameterAssignments = new CodeStatementCollection
                        {
                            AssignVariable(VariableReferenceExp("nvcISTmpIS"), SnippetExpression(String.Format("(NameValueCollection) {0}{1}[{2}]", "ht", callerSegment.Name.ToUpper(), ps.ProcessingType != ProcessingType.ALTERNATE ? "lISLoop" : "nLoop")))
                        };

                        foreach (ISMapping mapping in mt.IS.Mappings.Where(s => s.CallerSegment.Name == callerSegment.Name && s.ISSegment.Name.ToLower() != "fw_context" && (s.ISDataItem.FlowDirection == FlowAttribute.IN || s.ISDataItem.FlowDirection == FlowAttribute.INOUT)))
                        {
                            integInParameterAssignments.Add(AssignVariable(VariableReferenceExp("sValue"), SnippetExpression(String.Format("(string) nvcISTmpIS[\"{0}\"]", mapping.CallerDataItem.Name.ToLower()))));
                            integInParameterAssignments.Add(MethodInvocationExp(TypeReferenceExp(integrationObject), "getFieldValue").AddParameters(new CodeExpression[] { PrimitiveExpression(mapping.ISSegment.Name.ToUpper()), ps.ProcessingType != ProcessingType.ALTERNATE ? SnippetExpression("lISLoop") : SnippetExpression("(long)1"), PrimitiveExpression(mapping.ISDataItem.Name.ToLower()), VariableReferenceExp("sValue") }));
                            integInParameterAssignments.Add(SnippetStatement(Environment.NewLine));
                        }

                        if (ps.ProcessingType == ProcessingType.ALTERNATE)
                        {
                            foreach (CodeStatement statement in integInParameterAssignments)
                            {
                                statements.Add(statement);
                            }
                        }
                        else
                        {
                            CodeIterationStatement forEachStatement = ForLoopExpression(AssignVariable(VariableReferenceExp("lISLoop"), PrimitiveExpression(1)), BinaryOpertorExpression(VariableReferenceExp("lISLoop"), CodeBinaryOperatorType.LessThanOrEqual, VariableReferenceExp("nMax")), AssignVariable(VariableReferenceExp("lISLoop"), BinaryOpertorExpression(VariableReferenceExp("lISLoop"), CodeBinaryOperatorType.Add, PrimitiveExpression(1))));
                            foreach (CodeStatement statement in integInParameterAssignments)
                            {
                                forEachStatement.Statements.Add(statement);
                            }
                            statements.Add(forEachStatement);
                        }
                        //TECH-XXXX - code modification ends
                    }
                    else
                    {
                        foreach (ISMapping mapping in mt.IS.Mappings.Where(s => s.ISSegment.Name.ToLower() != "fw_context"
                        && s.CallerSegment.Name == callerSegment.Name
                        && (s.ISDataItem.FlowDirection == FlowAttribute.IN || s.ISDataItem.FlowDirection == FlowAttribute.INOUT)))
                        {
                            statements.Add(AssignVariable(VariableReferenceExp("sValue"), SnippetExpression(String.Format("(string) nvc{0}[\"{1}\"]", callerSegment.Name.ToUpper(), mapping.CallerDataItem.Name.ToLower()))));
                            statements.Add(MethodInvocationExp(TypeReferenceExp(integrationObject), "getFieldValue").AddParameters(new CodeExpression[] { PrimitiveExpression(mapping.ISSegment.Name.ToUpper()), SnippetExpression("(long) 1"), PrimitiveExpression(mapping.ISDataItem.Name.ToLower()), VariableReferenceExp("sValue") }));
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("PS_IS_INBinding->{0}", !Object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }

            return statements;
        }

        private CodeStatementCollection PS_IS_OutBinding(ProcessSection ps, Method method)
        {
            CodeStatementCollection statements;

            try
            {
                statements = new CodeStatementCollection();
                IntegrationService IS = method.IS;
                IEnumerable<ISMapping> integ_serv_map = IS.Mappings;
                IEnumerable<IGrouping<string, ISMapping>> groupByISSegments = integ_serv_map.GroupBy(m => m.ISSegment.Name);

                foreach (IGrouping<string, ISMapping> segmentGroup in groupByISSegments)
                {
                    string ISSegName = segmentGroup.Key;
                    string ISSegFlowAtt = string.Empty;
                    IEnumerable<ISMapping> lISMappings = segmentGroup;
                    bool bIsMultiISSeg = lISMappings.First().ISSegment.Inst == MULTI_INSTANCE;
                    bool bIsMultiCallingSeg = lISMappings.First().CallerSegment.Inst == MULTI_INSTANCE;

                    var diExcludingScratch = lISMappings.Where(m => m.ISDataItem.FlowDirection != FlowAttribute.SCRATCH);
                    if (diExcludingScratch.Count() > 0)
                    {
                        if (diExcludingScratch.All(m => m.ISDataItem.FlowDirection == FlowAttribute.IN))
                            ISSegFlowAtt = FlowAttribute.IN;
                        else if (diExcludingScratch.All(m => m.ISDataItem.FlowDirection == FlowAttribute.OUT))
                            ISSegFlowAtt = FlowAttribute.OUT;
                        else if (diExcludingScratch.Any(m => m.ISDataItem.FlowDirection == FlowAttribute.INOUT) || (diExcludingScratch.Any(m => m.ISDataItem.FlowDirection == FlowAttribute.IN) && diExcludingScratch.Any(m => m.ISDataItem.FlowDirection == FlowAttribute.OUT)))
                            ISSegFlowAtt = FlowAttribute.INOUT;
                    }
                    else
                    {
                        ISSegFlowAtt = FlowAttribute.SCRATCH;
                    }

                    if (!(ISSegFlowAtt == FlowAttribute.OUT || ISSegFlowAtt == FlowAttribute.INOUT))
                        continue;


                    foreach (Segment callerSegment in lISMappings.Where(m => m.ISDataItem.FlowDirection == FlowAttribute.OUT || m.ISDataItem.FlowDirection == FlowAttribute.INOUT).Select(m => m.CallerSegment).Distinct())
                    {
                        statements.Add(CommentStatement(string.Format("caller segment - {0}", callerSegment.Name)));

                        IEnumerable<ISMapping> mappingsInTheSegment = lISMappings.Where(m => m.CallerSegment.Name == callerSegment.Name && (m.ISDataItem.FlowDirection == FlowAttribute.OUT || m.ISDataItem.FlowDirection == FlowAttribute.INOUT));
                        IEnumerable<ISMapping> mappingWithKeyDataItemsAlone = lISMappings.Where(m => m.CallerSegment.Name == callerSegment.Name && m.CallerDataItem.PartOfKey == true && (m.ISDataItem.FlowDirection == FlowAttribute.OUT || m.ISDataItem.FlowDirection == FlowAttribute.INOUT));

                        if (bIsMultiISSeg)
                        {
                            statements.Add(AssignVariable(VariableReferenceExp("lISOutRecCount"), GetProperty(FieldReferenceExp(TypeReferenceExp("ObjInteg" + method.SeqNO), "ht" + ISSegName.ToUpper()), "Count")));
                        }
                        else
                        {
                            statements.Add(AssignVariable(VariableReferenceExp("lISOutRecCount"), PrimitiveExpression(1)));
                        }

                        CodeIterationStatement forEachRecord = ForLoopExpression(AssignVariable(VariableReferenceExp("lISLoop"), PrimitiveExpression(1)),
                            BinaryOpertorExpression(VariableReferenceExp("lISLoop"), CodeBinaryOperatorType.LessThanOrEqual, VariableReferenceExp("lISOutRecCount")),
                            AssignVariable(VariableReferenceExp("lISLoop"), BinaryOpertorExpression(VariableReferenceExp("lISLoop"), CodeBinaryOperatorType.Add, PrimitiveExpression(1))));

                        if (callerSegment.Inst == MULTI_INSTANCE)
                        {
                            if (mappingWithKeyDataItemsAlone.Any() == false)
                                forEachRecord.Statements.Add(AssignVariable(VariableReferenceExp("nRecCount"), SnippetExpression(string.Format("ht{1}.Count", method.SeqNO, callerSegment.Name.ToUpper()))));
                            forEachRecord.Statements.Add(AssignVariable(mappingWithKeyDataItemsAlone.Any() ? VariableReferenceExp("nvcFilterText") : VariableReferenceExp("nvcISTmp"), SnippetExpression("null")));
                            forEachRecord.Statements.Add(AssignVariable(mappingWithKeyDataItemsAlone.Any() ? VariableReferenceExp("nvcFilterText") : VariableReferenceExp("nvcISTmp"), ObjectCreateExpression(typeof(NameValueCollection))));
                        }
                        else
                        {
                            forEachRecord.Statements.Add(AssignVariable(VariableReferenceExp("nRecCount"), VariableReferenceExp("nLoop")));
                        }

                        if (callerSegment.Inst == MULTI_INSTANCE)
                        {
                            //Key Dataitems
                            foreach (ISMapping mapping in mappingWithKeyDataItemsAlone)
                            {
                                forEachRecord.Statements.Add(AssignVariable(VariableReferenceExp("sISKeyValue"), GetProperty(TypeReferenceExp(typeof(string)), "Empty")));
                                forEachRecord.Statements.Add(AssignVariable(VariableReferenceExp("nvcTmp"), SnippetExpression(string.Format("(NameValueCollection)ObjInteg{0}.ht{1}[lISLoop]", method.SeqNO, mapping.ISSegment.Name.ToUpper()))));
                                forEachRecord.Statements.Add(AssignVariable(VariableReferenceExp("sISKeyValue"), SnippetExpression(string.Format("(string) nvcTmp[\"{0}\"]", mapping.CallerDataItem.Name.ToLower()))));
                                CodeConditionStatement ifISKeyValueIsNull = IfCondition();
                                ifISKeyValueIsNull.Condition = BinaryOpertorExpression(VariableReferenceExp("sISKeyValue"), CodeBinaryOperatorType.IdentityEquality, SnippetExpression("null"));
                                ifISKeyValueIsNull.TrueStatements.Add(AssignVariable(VariableReferenceExp("sISKeyValue"), GetProperty(TypeReferenceExp(typeof(string)), "Empty")));
                                forEachRecord.Statements.Add(ifISKeyValueIsNull);
                                forEachRecord.Statements.Add(AssignVariable(ArrayIndexerExpression("nvcFilterText", PrimitiveExpression(mapping.CallerDataItem.Name.ToLower())), VariableReferenceExp("sISKeyValue")));
                            }

                            if (mappingWithKeyDataItemsAlone.Any())
                            {
                                forEachRecord.Statements.Add(AssignVariable(VariableReferenceExp("lInstExists"), MethodInvocationExp(ThisReferenceExp(), "SetRecordPtrEx").AddParameters(new CodeExpression[] { PrimitiveExpression(callerSegment.Name.ToLower()), VariableReferenceExp("nvcFilterText") })));
                                CodeConditionStatement ifInstExistsIsZero = IfCondition();
                                ifInstExistsIsZero.Condition = BinaryOpertorExpression(VariableReferenceExp("lInstExists"), CodeBinaryOperatorType.IdentityEquality, PrimitiveExpression(0));
                                ifInstExistsIsZero.TrueStatements.Add(AssignVariable(VariableReferenceExp("lInstExists"), GetProperty("ht" + callerSegment.Name.ToUpper(), "Count")));
                                ifInstExistsIsZero.TrueStatements.Add(AssignVariable(VariableReferenceExp("lInstExists"), BinaryOpertorExpression(VariableReferenceExp("lInstExists"), CodeBinaryOperatorType.Add, PrimitiveExpression(1))));
                                ifInstExistsIsZero.TrueStatements.Add(AssignVariable(VariableReferenceExp("nvcISTmp"), SnippetExpression("null")));
                                ifInstExistsIsZero.TrueStatements.Add(AssignVariable(VariableReferenceExp("nvcISTmp"), ObjectCreateExpression(typeof(NameValueCollection))));
                                ifInstExistsIsZero.FalseStatements.Add(AssignVariable(VariableReferenceExp("nvcISTmp"), SnippetExpression(string.Format("(NameValueCollection)ht{0}[lInstExists]", callerSegment.Name.ToUpper()))));

                                forEachRecord.Statements.Add(ifInstExistsIsZero);
                            }
                        }

                        if (bIsMultiISSeg)
                        {
                            forEachRecord.Statements.Add(AssignVariable(VariableReferenceExp("nvcISTmpIS"), SnippetExpression(string.Format("(NameValueCollection)ObjInteg{0}.ht{1}[lISLoop]", method.SeqNO, lISMappings.First().ISSegment.Name.ToUpper()))));
                        }

                        foreach (ISMapping mapping in mappingsInTheSegment)
                        {
                            string sDIName = mapping.ISDataItem.Name;
                            string sCallingDIName = mapping.CallerDataItem.Name;

                            if (bIsMultiISSeg)
                                forEachRecord.Statements.Add(AssignVariable(VariableReferenceExp("sValue"), SnippetExpression(string.Format("(string)nvc{0}[\"{1}\"]", bIsMultiISSeg ? "ISTmpIS" : ISSegName.ToUpper(), sDIName))));
                            else
                                forEachRecord.Statements.Add(AssignVariable(VariableReferenceExp("sValue"), SnippetExpression(string.Format("(string)ObjInteg{0}.nvc{1}[\"{2}\"]", method.SeqNO, bIsMultiISSeg ? "ISTmpIS" : ISSegName.ToUpper(), sDIName))));

                            if (callerSegment.Inst == MULTI_INSTANCE)
                                forEachRecord.Statements.Add(AssignVariable(ArrayIndexerExpression(string.Format("nvc{0}", "ISTmp"), PrimitiveExpression(sCallingDIName.ToLower())), VariableReferenceExp("sValue")));
                            else
                                forEachRecord.Statements.Add(AssignVariable(ArrayIndexerExpression(string.Format("nvc{0}", mapping.CallerSegment.Name.ToUpper()), PrimitiveExpression(sCallingDIName.ToLower())), VariableReferenceExp("sValue")));
                        }

                        if (callerSegment.Inst == MULTI_INSTANCE)
                            forEachRecord.Statements.Add(AssignVariable(ArrayIndexerExpression(string.Format("ht{0}", callerSegment.Name.ToUpper()), mappingWithKeyDataItemsAlone.Count() > 0 ? SnippetExpression("lInstExists") : SnippetExpression("nRecCount + 1")), VariableReferenceExp("nvcISTmp")));

                        statements.Add(forEachRecord);
                    }
                }

                return statements;
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("PS_IS_OutBinding->{0}", ex.InnerException != null ? ex.InnerException.Message : ex.Message));
            }
        }


        private CodeStatementCollection PS_Alternate_CtrlExp_Check(ProcessSection ps)
        {
            CodeStatementCollection statements;
            CodeVariableDeclarationStatement psIndex, brIndex, psSeqNo, brSeqNo, nMax, BROutExists, BRInExists;

            statements = new CodeStatementCollection();
            psIndex = new CodeVariableDeclarationStatement(typeof(int), "psIndex");
            brIndex = new CodeVariableDeclarationStatement(typeof(int), "brIndex");
            psSeqNo = new CodeVariableDeclarationStatement(typeof(int), "psSeqNo");
            brSeqNo = new CodeVariableDeclarationStatement(typeof(int), "brSeqNo");
            nMax = new CodeVariableDeclarationStatement(typeof(int), "nMax");
            BROutExists = new CodeVariableDeclarationStatement(typeof(int), "cBROutExists");
            BRInExists = new CodeVariableDeclarationStatement(typeof(int), "cBRInExists");
            try
            {
                CodeIterationStatement methodLooping = null;
                IEnumerable<Segment> loopSegmentForAltSection = null;


                loopSegmentForAltSection = ps.Methods.Where(m => m.LoopSegment != null).Select(m => m.LoopSegment);

                if (!string.IsNullOrEmpty(ps.LoopCausingSegment))
                {
                    statements.Add(AssignVariable(VariableReferenceExp("nMax"), PrimitiveExpression(0)));
                    statements.Add(AssignVariable(VariableReferenceExp(nMax.Name), GetProperty("ht" + ps.LoopCausingSegment.ToUpperInvariant(), "Count")));
                }

                methodLooping = ForLoopExpression(AssignVariable(VariableReferenceExp("nLoop"), PrimitiveExpression(1)), BinaryOpertorExpression(VariableReferenceExp("nLoop"), CodeBinaryOperatorType.LessThanOrEqual, VariableReferenceExp("nMax")), AssignVariable(VariableReferenceExp("nLoop"), BinaryOpertorExpression(VariableReferenceExp("nLoop"), CodeBinaryOperatorType.Add, PrimitiveExpression(1))));

                //TECH_XXXX **starts** - uncommented(in alternate process section, nvcTmp initialization is missing)
                if (!string.IsNullOrEmpty(ps.LoopCausingSegment))
                    methodLooping.Statements.Add(AssignVariable(VariableReferenceExp("nvcTmp"), SnippetExpression(string.Format("(NameValueCollection)ht{0}[nLoop]", ps.LoopCausingSegment.ToUpperInvariant()))));
                //TECH_XXXX **ends**

                if (ps.CtrlExp != null)
                {
                    CodeConditionStatement ps_ctrl_exp_check = IfCondition();
                    if (ps.CtrlExp.Seg.Inst == MULTI_INSTANCE)
                    {
                        methodLooping.Statements.Add(AssignVariable(VariableReferenceExp("nvcTmpCrtl"), SnippetExpression("(NameValueCollection) ht" + ps.CtrlExp.Seg.Name.ToUpper() + "[nLoop]")));
                        ps_ctrl_exp_check.Condition = BinaryOpertorExpression(BinaryOpertorExpression(VariableReferenceExp("nvcTmpCrtl"), CodeBinaryOperatorType.IdentityInequality, SnippetExpression("null")), CodeBinaryOperatorType.BooleanAnd, BinaryOpertorExpression(ArrayIndexerExpression("nvcTmpCrtl", PrimitiveExpression(ps.CtrlExp.DI.Name.ToLower())), CodeBinaryOperatorType.IdentityEquality, PrimitiveExpression("0")));
                    }
                    else
                    {
                        ps_ctrl_exp_check.Condition = BinaryOpertorExpression(BinaryOpertorExpression(VariableReferenceExp("nvc" + ps.CtrlExp.Seg.Name.ToUpper()), CodeBinaryOperatorType.IdentityInequality, SnippetExpression("null")), CodeBinaryOperatorType.BooleanAnd, BinaryOpertorExpression(ArrayIndexerExpression("nvc" + ps.CtrlExp.Seg.Name.ToUpper(), PrimitiveExpression(ps.CtrlExp.DI.Name.ToLower())), CodeBinaryOperatorType.IdentityEquality, PrimitiveExpression("0")));
                    }


                    foreach (CodeStatement statement in PS_BRExecution(ps))
                    {
                        if (ps.CtrlExp.Seg.Inst == MULTI_INSTANCE)
                            ps_ctrl_exp_check.TrueStatements.Add(statement);
                        else
                            methodLooping.Statements.Add(statement);
                    }


                    if (ps.CtrlExp.Seg.Inst == MULTI_INSTANCE)
                    {
                        methodLooping.Statements.Add(ps_ctrl_exp_check);
                        statements.Add(methodLooping);
                    }
                    else
                    {
                        ps_ctrl_exp_check.TrueStatements.Add(methodLooping);
                        statements.Add(ps_ctrl_exp_check);
                    }

                }
                else
                {
                    foreach (CodeStatement statement in PS_BRExecution(ps))
                    {
                        methodLooping.Statements.Add(statement);
                    }
                    statements.Add(methodLooping);
                }
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("PS_Alternate_CtrlExp_Check->{0}", !Object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }

            return statements;
        }

        private CodeStatementCollection PS_BR_ApiRequest(Method method, ApiRequest apiRequestInfo = null, string containerJObjectName = "")
        {
            CodeStatementCollection statements = null;
            try
            {
                statements = new CodeStatementCollection();

                string currentJObjectName = string.Empty;

                //root
                if (apiRequestInfo == null)
                {
                    //statements.Add(DeclareVariable("JObject", "jReqObjRoot"));
                    //statements.Add(AssignVariable(VariableReferenceExp("jReqObjRoot"), ObjectCreateExpression("JObject")));

                    apiRequestInfo = method.ApiRequestInfo.Single(ari => string.Compare(ari.ParentNodeID, "root", true) == 0);
                    containerJObjectName = "resObj";
                    currentJObjectName = $"{apiRequestInfo.SchemaName}_req";
                }
                else
                {
                    switch (apiRequestInfo.Type.ToUpper())
                    {
                        case "ARRAY":
                        case "OBJECT":
                            currentJObjectName = $"{apiRequestInfo.SchemaName}_req";
                            break;
                        default:
                            currentJObjectName = $"{apiRequestInfo.ParentSchemaName}_req";
                            break;
                    }
                }

                var childProperties = method.ApiRequestInfo.Where(ari => string.Compare(ari.ParentNodeID, apiRequestInfo.NodeID, true) == 0);
                switch (apiRequestInfo.Type.ToUpper())
                {
                    case "ARRAY":

                        if (apiRequestInfo.Segment == null)
                            apiRequestInfo.Segment = method.ApiRequestInfo.Where(ari => string.Compare(ari.ParentNodeID, apiRequestInfo.NodeID, true) == 0 && ari.Segment != null && ari.Segment.Inst == MULTI_INSTANCE).First().Segment;


                        string segmentFieldName = $"ht{apiRequestInfo.Segment.Name}";
                        statements.Add(SnippetStatement("\n"));
                        statements.Add(DeclareVariable("JArray", currentJObjectName));
                        statements.Add(AssignVariable(VariableReferenceExp(currentJObjectName), ObjectCreateExpression("JArray")));
                        if (string.Compare(apiRequestInfo.ParentNodeID, "root", true) != 0 && !string.IsNullOrEmpty(containerJObjectName))
                            statements.Add(MethodInvocationExp(VariableReferenceExp(containerJObjectName), "Add").AddParameters(new CodeExpression[] { PrimitiveExpression(apiRequestInfo.SchemaName), VariableReferenceExp(currentJObjectName) }));
                        statements.Add(SnippetStatement($"for(long index = 1; index <= {segmentFieldName}.Count; index++)"));
                        statements.Add(SnippetStatement("{"));

                        containerJObjectName = currentJObjectName;
                        statements.Add(AssignVariable(VariableReferenceExp("nvcTmp"), SnippetExpression("null")));

                        statements.Add(AssignVariable("nvcTmp", SnippetExpression($"(NameValueCollection){segmentFieldName}[index]")));

                        if (!string.IsNullOrEmpty(apiRequestInfo.SchemaType) && string.Compare(apiRequestInfo.SchemaType, "string", true) == 0)
                        {
                            statements.Add(MethodInvocationExp(VariableReferenceExp(currentJObjectName), "Add").AddParameter(SnippetExpression($"string.Compare(nvcTmp[\"{apiRequestInfo.DataItem.Name}\"].ToString(),\"~#~\",true)==0?string.Empty:nvcTmp[\"{apiRequestInfo.DataItem.Name}\"].ToString()")));
                        }
                        else
                        {
                            currentJObjectName = $"j{currentJObjectName}_item";
                            statements.Add(DeclareVariable("JObject", currentJObjectName));
                            statements.Add(AssignVariable(VariableReferenceExp($"{currentJObjectName}"), ObjectCreateExpression("JObject")));
                            foreach (var property in childProperties)
                            {
                                foreach (CodeStatement statement in PS_BR_ApiRequest(method, property, currentJObjectName))
                                {
                                    statements.Add(statement);
                                }
                            }
                            statements.Add(MethodInvocationExp(VariableReferenceExp(containerJObjectName), "Add").AddParameter(VariableReferenceExp(currentJObjectName)));
                        }

                        statements.Add(SnippetStatement("}"));
                        if (string.Compare(apiRequestInfo.ParentNodeID, "root", true) == 0)
                        {
                            statements.Add(MethodInvocationExp(ThisReferenceExp(), "LogMessage").AddParameters(new CodeExpression[] { PrimitiveExpression($"{method.SectionName}"), PrimitiveExpression("Request Json formed by service dll is as follows:") }));
                            statements.Add(MethodInvocationExp(ThisReferenceExp(), "LogMessage").AddParameters(new CodeExpression[] { PrimitiveExpression($"{method.SectionName}"), MethodInvocationExp(TypeReferenceExp(typeof(JsonConvert)), "SerializeObject").AddParameter(VariableReferenceExp(containerJObjectName)) }));
                            statements.Add(MethodInvocationExp(ThisReferenceExp(), "LogMessage").AddParameters(new CodeExpression[] { PrimitiveExpression($"{method.SectionName}"), PrimitiveExpression($"{containerJObjectName}.ToString() is :") }));
                            statements.Add(MethodInvocationExp(ThisReferenceExp(), "LogMessage").AddParameters(new CodeExpression[] { PrimitiveExpression($"{method.SectionName}"), MethodInvocationExp(TypeReferenceExp(typeof(JsonConvert)), "SerializeObject").AddParameter(MethodInvocationExp(VariableReferenceExp(containerJObjectName), "ToString")) }));

                            statements.Add(SnippetStatement($"request.AddContent<string>({containerJObjectName}.ToString());"));
                        }
                        //statements.Add(MethodInvocationExp(VariableReferenceExp("jReqObjRoot"), "Add").AddParameters(new CodeExpression[] { PrimitiveExpression("data"), VariableReferenceExp(containerJObjectName) }));
                        //statements.Add();
                        break;
                    case "OBJECT":
                        statements.Add(SnippetStatement("\n"));
                        statements.Add(DeclareVariable("JObject", $"{currentJObjectName}"));
                        statements.Add(AssignVariable(VariableReferenceExp(currentJObjectName), ObjectCreateExpression("JObject")));
                        if (string.Compare(apiRequestInfo.ParentNodeID, "root", true) != 0 && !string.IsNullOrEmpty(containerJObjectName))
                            statements.Add(MethodInvocationExp(VariableReferenceExp(containerJObjectName), "Add").AddParameter(VariableReferenceExp(currentJObjectName)));
                        //statements.Add(AssignVariable(VariableReferenceExp($"{currentJObjectName}"), SnippetExpression($"(JObject) {containerJObjectName}[\"{apiRequestInfo.SchemaName}\"]")));
                        foreach (var property in childProperties)
                        {
                            foreach (CodeStatement statement in PS_BR_ApiRequest(method, property, currentJObjectName))
                            {
                                statements.Add(statement);
                            }
                        }

                        if (string.Compare(apiRequestInfo.ParentNodeID, "root", true) == 0)
                        {
                            statements.Add(MethodInvocationExp(ThisReferenceExp(), "LogMessage").AddParameters(new CodeExpression[] { PrimitiveExpression($"{method.SectionName}"), PrimitiveExpression("Request Json formed by service dll is as follows:") }));
                            statements.Add(MethodInvocationExp(ThisReferenceExp(), "LogMessage").AddParameters(new CodeExpression[] { PrimitiveExpression($"{method.SectionName}"), MethodInvocationExp(TypeReferenceExp(typeof(JsonConvert)), "SerializeObject").AddParameter(VariableReferenceExp(currentJObjectName)) }));
                            statements.Add(MethodInvocationExp(ThisReferenceExp(), "LogMessage").AddParameters(new CodeExpression[] { PrimitiveExpression($"{method.SectionName}"), PrimitiveExpression($"{currentJObjectName}.ToString() is :") }));
                            statements.Add(MethodInvocationExp(ThisReferenceExp(), "LogMessage").AddParameters(new CodeExpression[] { PrimitiveExpression($"{method.SectionName}"), MethodInvocationExp(TypeReferenceExp(typeof(JsonConvert)), "SerializeObject").AddParameter(MethodInvocationExp(VariableReferenceExp(currentJObjectName), "ToString")) }));

                            statements.Add(SnippetStatement($"request.AddContent<string>({currentJObjectName}.ToString());"));
                        }
                        break;
                    case "INTEGER":
                    case "STRING":
                        string sDefaultValue = string.Empty;
                        string sSourceVariable = string.Empty;
                        if (string.Compare(apiRequestInfo.Type, "string", true) == 0)
                            sDefaultValue = "~#~";
                        else if (string.Compare(apiRequestInfo.Type, "integer", true) == 0)
                            sDefaultValue = "-915";
                        sSourceVariable = apiRequestInfo.Segment.Inst == MULTI_INSTANCE ? "nvcTmp" : $"nvc{apiRequestInfo.Segment.Name}";

                        var contentExtractionFromSegment = SnippetExpression($"string.Compare({sSourceVariable}[\"{apiRequestInfo.DataItem.Name}\"].ToString(),\"{sDefaultValue}\",true)==0?string.Empty:{sSourceVariable}[\"{apiRequestInfo.DataItem.Name}\"].ToString()");
                        //var contentExtractionFromSegment = BinaryOpertorExpression(MethodInvocationExp(TypeReferenceExp("string"), "Compare").AddParameters(new CodeExpression[]
                        //{
                        //    ArrayIndexerExpression(apiRequestInfo.Segment.Inst == MULTI_INSTANCE ? "nvcTmp" : $"nvc{apiRequestInfo.Segment.Name}", PrimitiveExpression(apiRequestInfo.DataItem.Name)),
                        //    PrimitiveExpression(sDefaultValue), PrimitiveExpression(true)
                        //}), CodeBinaryOperatorType.IdentityEquality, PrimitiveExpression(0));


                        CodeMethodInvokeExpression conversionExpression = null;
                        switch (apiRequestInfo.DataItem.DataType.ToUpperInvariant())
                        {
                            case DataType.DATE:
                                conversionExpression = MethodInvocationExp(MethodInvocationExp(TypeReferenceExp("Convert"), "ToDateTime").AddParameter(contentExtractionFromSegment), "ToString").AddParameter(PrimitiveExpression("yyyy-MM-dd"));
                                statements.Add(MethodInvocationExp(VariableReferenceExp($"{containerJObjectName}"), "Add").AddParameter(
                                                                            ObjectCreateExpression("JProperty", new CodeExpression[] { PrimitiveExpression(apiRequestInfo.SchemaName), conversionExpression })));
                                statements.Add(conversionExpression);
                                break;
                            case DataType.DATETIME:
                            case DataType.DATE_TIME:
                                conversionExpression = MethodInvocationExp(MethodInvocationExp(TypeReferenceExp("Convert"), "ToDateTime").AddParameter(contentExtractionFromSegment), "ToString").AddParameter(PrimitiveExpression("yyyy-MM-ddTHH:mm:ss"));
                                statements.Add(MethodInvocationExp(VariableReferenceExp($"{containerJObjectName}"), "Add").AddParameter(
                                                                            ObjectCreateExpression("JProperty", new CodeExpression[] { PrimitiveExpression(apiRequestInfo.SchemaName), conversionExpression })));

                                break;
                            default:
                                statements.Add(MethodInvocationExp(VariableReferenceExp($"{containerJObjectName}"), "Add").AddParameter(
                                                                            ObjectCreateExpression("JProperty", new CodeExpression[] { PrimitiveExpression(apiRequestInfo.SchemaName), contentExtractionFromSegment })));
                                break;
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("PS_BR_ApiRequest->{0}", !Object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
            return statements;
        }

        private CodeStatementCollection PS_BR_ApiResponse(Method method, ApiResponse apiResponseInfoProperty = null, ApiResponse apiResponseInfoParent = null, string containerJObjectName = "")
        {
            CodeStatementCollection statements = null;
            try
            {
                statements = new CodeStatementCollection();

                string currentJObjectName = string.Empty;

                //root
                if (apiResponseInfoProperty == null)
                {
                    apiResponseInfoProperty = method.ApiResponseInfo.Single(ari => string.Compare(ari.ParentNodeID, "root", true) == 0);
                    containerJObjectName = "resObj";
                    currentJObjectName = $"{apiResponseInfoProperty.SchemaName}_root";
                }
                else
                {
                    switch (apiResponseInfoProperty.Type.ToUpper())
                    {
                        case "ARRAY":
                        case "OBJECT":
                            currentJObjectName = apiResponseInfoProperty.SchemaName;
                            break;
                        default:
                            currentJObjectName = apiResponseInfoProperty.ParentSchemaName;
                            break;
                    }
                }

                var childProperties = method.ApiResponseInfo.Where(ari => string.Compare(ari.ParentNodeID, apiResponseInfoProperty.NodeID, true) == 0);
                childProperties = childProperties.OrderByDescending(cp => cp.Type);
                switch (apiResponseInfoProperty.Type.ToUpper())
                {
                    case "ARRAY":
                        statements.Add(SnippetStatement("\n"));
                        var multisegments = method.ApiResponseInfo.Where(ari => ari.Segment != null && ari.Segment.Inst == MULTI_INSTANCE);
                        statements.Add(DeclareVariable("JArray", $"{currentJObjectName}"));
                        statements.Add(AssignVariable(VariableReferenceExp($"{currentJObjectName}"), SnippetExpression(string.Format("(JArray) {0}[\"{1}\"]", containerJObjectName, string.Compare(containerJObjectName, "resobj", true) == 0 ? "data" : apiResponseInfoProperty.SchemaName))));

                        //if the parent is array
                        //if (apiResponseInfoParent != null && string.Compare(apiResponseInfoParent.Type, "array", true) == 0)
                        //{
                        //    statements.Add(DeclareVariable(typeof(NameValueCollection), $"nvcTmp{containerJObjectName}Clone"));
                        //    statements.Add(AssignVariable(VariableReferenceExp($"nvcTmp{containerJObjectName}Clone"), VariableReferenceExp($"nvcTmp{containerJObjectName}")));
                        //}


                        statements.Add(SnippetStatement($"foreach(JObject {currentJObjectName}_item in {currentJObjectName})"));
                        currentJObjectName = $"{currentJObjectName}_item";
                        statements.Add(SnippetStatement("{"));

                        //if there is no array in child propeties
                        if (childProperties.Where(ari => string.Compare(ari.Type, "array", true) == 0).Any() == false)
                            statements.Add(AssignVariable(VariableReferenceExp("nRecCount"), BinaryOpertorExpression(VariableReferenceExp("nRecCount"), CodeBinaryOperatorType.Add, PrimitiveExpression(1))));

                        statements.Add(DeclareVariable(typeof(NameValueCollection), $"nvcTmp{currentJObjectName}"));
                        if (apiResponseInfoParent != null && string.Compare(apiResponseInfoParent.Type, "array", true) == 0)
                            statements.Add(AssignVariable(VariableReferenceExp($"nvcTmp{currentJObjectName}"), ObjectCreateExpression(typeof(NameValueCollection), new CodeExpression[] { VariableReferenceExp($"nvcTmp{containerJObjectName}") })));
                        else
                            statements.Add(AssignVariable(VariableReferenceExp($"nvcTmp{currentJObjectName}"), ObjectCreateExpression(typeof(NameValueCollection))));

                        //if the parent is array
                        //if (apiResponseInfoParent != null && string.Compare(apiResponseInfoParent.Type, "array", true) == 0)
                        //{
                        //    statements.Add(AssignVariable(VariableReferenceExp($"nvcTmp{currentJObjectName}"), VariableReferenceExp($"nvcTmp{containerJObjectName}Clone")));
                        //}

                        statements.Add(SnippetStatement("\n"));
                        foreach (var property in childProperties)
                        {
                            foreach (CodeStatement statement in PS_BR_ApiResponse(method, property, apiResponseInfoProperty, currentJObjectName))
                            {
                                statements.Add(statement);
                            }
                        }

                        //if there is no child items
                        if (childProperties.Where(cp => string.Compare(cp.Type, "array", true) == 0).Any() == false)
                            statements.Add(AssignVariable(ArrayIndexerExpression($"ht{multisegments.First().Segment.Name}", VariableReferenceExp("nRecCount")), VariableReferenceExp($"nvcTmp{currentJObjectName}")));
                        statements.Add(SnippetStatement("}"));
                        break;
                    case "OBJECT":
                        statements.Add(SnippetStatement("\n"));
                        statements.Add(DeclareVariable("JObject", $"{currentJObjectName}"));
                        statements.Add(AssignVariable(VariableReferenceExp($"{currentJObjectName}"), SnippetExpression($"(JObject) {containerJObjectName}[\"{(containerJObjectName.Equals("resObj") ? "data" : apiResponseInfoProperty.SchemaName)}\"]")));
                        foreach (var property in childProperties)
                        {
                            foreach (CodeStatement statement in PS_BR_ApiResponse(method, property, apiResponseInfoProperty, currentJObjectName))
                            {
                                statements.Add(statement);
                            }
                        }
                        break;
                    case "INTEGER":
                    case "STRING":
                        if (apiResponseInfoProperty.Segment == null || apiResponseInfoProperty.DataItem == null)
                            throw new Exception($"PS_BR_ApiResponse->Segment or dataitem details unavailable for the property {apiResponseInfoProperty.DisplayName}");

                        if (apiResponseInfoProperty.Segment.Inst == MULTI_INSTANCE)
                            statements.Add(AssignVariable(ArrayIndexerExpression($"nvcTmp{containerJObjectName}", PrimitiveExpression(apiResponseInfoProperty.DataItem.Name)), MethodInvocationExp(ArrayIndexerExpression(containerJObjectName, PrimitiveExpression(apiResponseInfoProperty.SchemaName)), "ToString")));
                        else
                            statements.Add(AssignVariable(ArrayIndexerExpression($"nvc{apiResponseInfoProperty.Segment.Name}", PrimitiveExpression(apiResponseInfoProperty.DataItem.Name)), MethodInvocationExp(ArrayIndexerExpression(containerJObjectName, PrimitiveExpression(apiResponseInfoProperty.SchemaName)), "ToString")));
                        break;
                }
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("PS_BR_ApiResponse->{0}", !Object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
            return statements;
        }

        /// <summary>
        /// Method to generate method execution code
        /// </summary>
        /// <param name="ps"></param>
        /// <returns></returns>
        private CodeStatementCollection PS_BRExecution(ProcessSection ps)
        {
            CodeStatementCollection statements;
            CodeVariableDeclarationStatement psIndex, brIndex, psSeqNo, brSeqNo, nMax, BROutExists, BRInExists;

            statements = new CodeStatementCollection();
            psIndex = new CodeVariableDeclarationStatement(typeof(int), "psIndex");
            brIndex = new CodeVariableDeclarationStatement(typeof(int), "brIndex");
            psSeqNo = new CodeVariableDeclarationStatement(typeof(int), "psSeqNo");
            brSeqNo = new CodeVariableDeclarationStatement(typeof(int), "brSeqNo");
            nMax = new CodeVariableDeclarationStatement(typeof(int), "nMax");
            BROutExists = new CodeVariableDeclarationStatement(typeof(int), "cBROutExists");
            BRInExists = new CodeVariableDeclarationStatement(typeof(int), "cBRInExists");

            try
            {
                CodeIterationStatement methodLooping = null;
                CodeTryCatchFinallyStatement mtTry = null;
                foreach (Method method in ps.Methods.OrderBy(m => Convert.ToInt16(m.SeqNO)))
                {
                    dynamic scope = null;

                    #region is br(i.e., normal method)
                    if (method.IsIntegService == false && method.IsApiConsumerService == false)
                    {
                        CodeCommentStatement comment = CommentStatement("Starting to execute " + ((ps.ProcessingType == ProcessingType.ALTERNATE) ? "alternate" : "default") + " style process section " + ps.SeqNO);
                        CodeCommentStatement comment1 = CommentStatement("Starting to execute the BR - " + method.SeqNO + "  under the process section - " + ps.SeqNO);

                        statements.Add(comment);
                        statements.Add(comment1);

                        foreach (CodeStatement statement in PS_BRExecution_DeclareVariable(ps, method))
                            statements.Add(statement);

                        if (method.SystemGenerated == BRTypes.CUSTOM_BR || method.SystemGenerated == BRTypes.HANDCODED_BR)
                        {
                            //statements.Add(CommentStatement("Creating BRO object"));
                            CodeConditionStatement IfBroObjectIsNull = IfCondition();
                            IfBroObjectIsNull.Condition = BinaryOpertorExpression(FieldReferenceExp(ThisReferenceExp(), "objBRO"), CodeBinaryOperatorType.IdentityEquality, SnippetExpression("null"));
                            IfBroObjectIsNull.TrueStatements.Add(MethodInvocationExp(BaseReferenceExp(), "Set_Error_Info").AddParameters(new CodeExpression[] { PrimitiveExpression(0), SnippetExpression("CUtil.FRAMEWORK_ERROR"), SnippetExpression("CUtil.STOP_PROCESSING"), PrimitiveExpression(string.Format("RVWException at PS - {0} BR - {1} Not able to instantiate the BRO dll in MTS", ps.SeqNO, method.SeqNO)), GetProperty(TypeReferenceExp(typeof(string)), "Empty"), GetProperty(TypeReferenceExp(typeof(string)), "Empty"), PrimitiveExpression(0), GetProperty(TypeReferenceExp(typeof(string)), "Empty"), GetProperty(TypeReferenceExp(typeof(string)), "Empty") }));
                            statements.Add(IfBroObjectIsNull);
                        }
                        else if (method.SystemGenerated == BRTypes.BULK_BR)
                        {
                            CodeConditionStatement IfBroObjectIsNull = IfCondition();
                            IfBroObjectIsNull.Condition = BinaryOpertorExpression(FieldReferenceExp(ThisReferenceExp(), "objBulk"), CodeBinaryOperatorType.IdentityEquality, SnippetExpression("null"));
                            IfBroObjectIsNull.TrueStatements.Add(MethodInvocationExp(BaseReferenceExp(), "Set_Error_Info").AddParameters(new CodeExpression[] { PrimitiveExpression(0), SnippetExpression("CUtil.FRAMEWORK_ERROR"), SnippetExpression("CUtil.STOP_PROCESSING"), PrimitiveExpression(string.Format("RVWException at PS - {0} BR - {1} Not able to instantiate the Bulk dll in MTS", ps.SeqNO, method.SeqNO)), GetProperty(TypeReferenceExp(typeof(string)), "Empty"), GetProperty(TypeReferenceExp(typeof(string)), "Empty"), PrimitiveExpression(0), GetProperty(TypeReferenceExp(typeof(string)), "Empty"), GetProperty(TypeReferenceExp(typeof(string)), "Empty") }));
                            statements.Add(IfBroObjectIsNull);
                        }

                        mtTry = new CodeTryCatchFinallyStatement();
                        //AddCatchClause(mtTry, "PS_BRExecution");
                        CodeCatchClause catchCRVWException = mtTry.AddCatch("CRVWException", "rvwe");
                        ThrowException(catchCRVWException, "rvwe");

                        CodeCatchClause catchOtherException = mtTry.AddCatch("Exception", "e");
                        catchOtherException.AddStatement(MethodInvocationExp(BaseReferenceExp(), "Set_Error_Info").AddParameters(new CodeExpression[] { PrimitiveExpression(0),
                                                                                                                                                        FieldReferenceExp(TypeReferenceExp("CUtil"),"FRAMEWORK_ERROR"),
                                                                                                                                                        FieldReferenceExp(TypeReferenceExp("CUtil"),"STOP_PROCESSING"),
                                                                                                                                                        MethodInvocationExp(TypeReferenceExp(typeof(string)),"Format").AddParameters(new CodeExpression[] { PrimitiveExpression("General Exception at PS - "+ ps.SeqNO +" BR - "+method.SeqNO),GetProperty("e","Message") }),
                                                                                                                                                        GetProperty(TypeReferenceExp(typeof(string)),"Empty"),
                                                                                                                                                        GetProperty(TypeReferenceExp(typeof(string)),"Empty"),
                                                                                                                                                        PrimitiveExpression(0),
                                                                                                                                                        GetProperty(TypeReferenceExp(typeof(string)),"Empty"),
                                                                                                                                                        GetProperty(TypeReferenceExp(typeof(string)),"Empty")
                                                                                                                                                        }));

                        mtTry.FinallyStatements.Add(SnippetExpression("CloseCommand()"));
                        mtTry.FinallyStatements.Add(SnippetExpression("Close()"));

                        statements.Add(mtTry);

                        if (method.SystemGenerated == BRTypes.CUSTOM_BR || method.SystemGenerated == BRTypes.HANDCODED_BR)
                        {
                            IEnumerable<Parameter> singleSegOutParams = method.Parameters.Where(p => p.Seg != null && p.Seg.Inst == SINGLE_INSTANCE && p.DI != null && string.Compare(p.Seg.Name, CONTEXT_SEGMENT, true) != 0 && (p.FlowDirection == FlowAttribute.OUT || p.FlowDirection == FlowAttribute.INOUT)).OrderBy(p => p.Name);
                            IEnumerable<Parameter> multiSegOutParams = method.Parameters.Where(p => p.Seg != null && p.Seg.Inst == MULTI_INSTANCE && p.DI != null && string.Compare(p.Seg.Name, CONTEXT_SEGMENT, true) != 0 && (p.FlowDirection == FlowAttribute.OUT || p.FlowDirection == FlowAttribute.INOUT)).OrderBy(p => p.Name);
                            string sBroObjName = (singleSegOutParams.Any() == false && multiSegOutParams.Any() == false) ? "res" : "spxs";
                            string sBroObjReturnType = (singleSegOutParams.Any() == false && multiSegOutParams.Any() == false) ? "Result" : Common.InitCaps(method.Name);

                            mtTry.AddStatement(DeclareVariableAndAssign(string.Format("{0}BRTypes.{1}", Common.InitCaps(_ecrOptions.Component), sBroObjReturnType), sBroObjName, true, SnippetExpression("null")));
                        }

                        CodeConditionStatement checkForIEDKExtension = IfCondition();
                        checkForIEDKExtension.Condition = VariableReferenceExp("iEDKServiceES");
                        checkForIEDKExtension.TrueStatements.Add(AssignVariable(psIndex.Name, MethodInvocationExp(BaseReferenceExp(), "IsProcessSectionExists").AddParameter(PrimitiveExpression(ps.Name.ToLower()))));
                        mtTry.AddStatement(checkForIEDKExtension);

                        CodeConditionStatement checkPsIndex = IfCondition();
                        checkPsIndex.Condition = BinaryOpertorExpression(VariableReferenceExp(psIndex.Name), CodeBinaryOperatorType.IdentityInequality, PrimitiveExpression(-1));
                        if (ps.ProcessingType == ProcessingType.ALTERNATE)
                            checkPsIndex.TrueStatements.Add(MethodInvocationExp(BaseReferenceExp(), "PrepareIBRForExecution").AddParameters(new CodeExpression[] { VariableReferenceExp("psIndex"), VariableReferenceExp("brSeqNo"), PrimitiveExpression(0), VariableReferenceExp("nLoop") }));
                        else
                            checkPsIndex.TrueStatements.Add(MethodInvocationExp(BaseReferenceExp(), "PrepareIBRForExecution").AddParameters(new CodeExpression[] { VariableReferenceExp("psIndex"), VariableReferenceExp("brSeqNo"), PrimitiveExpression(0), PrimitiveExpression(0) }));
                        mtTry.AddStatement(checkPsIndex);


                        if (method.method_exec_cont)
                        {
                            scope = IfCondition();
                            scope.Condition = BinaryOpertorExpression(FieldReferenceExp(BaseReferenceExp(), "methodValidationStatus"), CodeBinaryOperatorType.IdentityEquality, PrimitiveExpression(true));
                        }
                        else
                            scope = mtTry;

                        ExtensionMethods.AddStatement(scope, DeclareVariableAndAssign(typeof(int), BROutExists.Name, true, PrimitiveExpression(-1)));
                        ExtensionMethods.AddStatement(scope, DeclareVariableAndAssign(typeof(int), BRInExists.Name, true, PrimitiveExpression(-1)));

                        CodeConditionStatement checkForCBRExecution = IfCondition();
                        checkForCBRExecution.Condition = BinaryOpertorExpression(FieldReferenceExp(BaseReferenceExp(), "iEDKServiceES"), CodeBinaryOperatorType.BooleanAnd, BinaryOpertorExpression(VariableReferenceExp(psIndex.Name), CodeBinaryOperatorType.IdentityInequality, PrimitiveExpression(-1)));
                        checkForCBRExecution.TrueStatements.Add(MethodInvocationExp(BaseReferenceExp(), "PrepareCBRForExecution").AddParameters(new CodeExpression[] { VariableReferenceExp(psIndex.Name), VariableReferenceExp(brSeqNo.Name), SnippetExpression("out brIndex"), SnippetExpression("out cBRInExists"), SnippetExpression("out cBROutExists"), SnippetExpression("ref nMax") }));
                        ExtensionMethods.AddStatement(scope, checkForCBRExecution);

                        if (this._service.Implement_New_Method_Of_ParamAddition)
                        {
                            if (method.Parameters.Count > 0)
                            {
                                ExtensionMethods.AddStatement(scope, MethodInvocationExp(FieldReferenceExp(BaseReferenceExp(), "methodParameterDetails"), "Clear"));
                                foreach (CodeStatement statement in PS_AddInParameters_With_MethodExecCont(ps, method))
                                {
                                    ExtensionMethods.AddStatement(scope, statement);
                                }
                            }
                        }

                        //TECH-XXXX 
                        //if (!string.IsNullOrEmpty(ps.LoopCausingSegment) && ps.ProcessingType == ProcessingType.DEFAULT && method.CtrlExp == null)
                        //    mtTry.AddStatement(AssignVariable(FieldReferenceExp(ThisReferenceExp(), "nvcTmp"), SnippetExpression("(NameValueCollection) " + "ht" + ps.LoopCausingSegment.ToUpper() + "[nLoop]")));

                        //for default process section
                        #region Default process section
                        if (ps.ProcessingType == ProcessingType.DEFAULT)
                        {
                            methodLooping = ForLoopExpression(AssignVariable(VariableReferenceExp("nLoop"), PrimitiveExpression(1)), BinaryOpertorExpression(VariableReferenceExp("nLoop"), CodeBinaryOperatorType.LessThanOrEqual, VariableReferenceExp("nMax")), AssignVariable(VariableReferenceExp("nLoop"), BinaryOpertorExpression(VariableReferenceExp("nLoop"), CodeBinaryOperatorType.Add, PrimitiveExpression(1))));

                            //code for PSR,PMR,PSMR
                            if (!string.IsNullOrEmpty(ps.LoopCausingSegment))
                            {
                                Segment loopSegment = _service.Segments.Where(s => s.Name == ps.LoopCausingSegment).First(); //madhan

                                if (loopSegment.Process_sel_rows == "sy" || loopSegment.Process_upd_rows == "sy" || loopSegment.Process_sel_upd_rows == "sy")
                                {
                                    string sRowProcessingType = string.Empty;

                                    if (loopSegment.Process_sel_rows == "sy")
                                        sRowProcessingType = "psr";
                                    else if (loopSegment.Process_upd_rows == "sy")
                                        sRowProcessingType = "pmr";
                                    else if (loopSegment.Process_sel_upd_rows == "sy")
                                        sRowProcessingType = "psmr";

                                    if (method.CtrlExp == null)
                                        methodLooping.Statements.Add(AssignVariable(FieldReferenceExp(ThisReferenceExp(), "nvcTmp"), SnippetExpression("(NameValueCollection) " + "ht" + ps.LoopCausingSegment.ToUpper() + "[nLoop]"))); //TECH-XXXX
                                    methodLooping.Statements.Add(AssignVariable(VariableReferenceExp("modeFlagValue"), ArrayIndexerExpression("nvcTmp", PrimitiveExpression("modeflag"))));
                                    methodLooping.Statements.Add(MethodInvocationExp(ThisReferenceExp(), "EvaluateModeFlagExpression").AddParameters(new CodeExpression[] { PrimitiveExpression(sRowProcessingType), VariableReferenceExp("modeFlagValue"), SnippetExpression("out ExecuteSpFlag") }));

                                    CodeConditionStatement ifExecuteSpFlagIsTrue = IfCondition();
                                    ifExecuteSpFlagIsTrue.Condition = VariableReferenceExp("ExecuteSpFlag");
                                    foreach (CodeStatement statement in PS_MethodExecution(ps, method))
                                    {
                                        ifExecuteSpFlagIsTrue.TrueStatements.Add(statement);
                                    }

                                    methodLooping.Statements.Add(ifExecuteSpFlagIsTrue);
                                }
                                else
                                {
                                    if (loopSegment.Process_sel_rows == "y" || loopSegment.Process_upd_rows == "y")
                                    {
                                        //condition added for a problem we encountered while aviation bulk codegeneration - mainly for default process section & process selected rows
                                        if (method.CtrlExp == null)
                                        {
                                            methodLooping.Statements.Add(AssignVariable(FieldReferenceExp(ThisReferenceExp(), "nvcTmp"), SnippetExpression("(NameValueCollection) " + "ht" + ps.LoopCausingSegment.ToUpper() + "[nLoop]")));
                                            methodLooping.Statements.Add(AssignVariable(VariableReferenceExp("sValue"), ArrayIndexerExpression("nvcTmp", PrimitiveExpression("ModeFlag"))));
                                            CodeConditionStatement checkModeFlagValue = IfCondition();
                                            if (loopSegment.Process_sel_rows == "y")
                                                checkModeFlagValue.Condition = SnippetExpression("sValue == \"I\" || sValue == \"U\" || sValue == \"S\"");
                                            else if (loopSegment.Process_upd_rows == "y")
                                                checkModeFlagValue.Condition = BinaryOpertorExpression(VariableReferenceExp("sValue"), CodeBinaryOperatorType.IdentityEquality, PrimitiveExpression("S"));

                                            checkModeFlagValue.TrueStatements.Add(new CodeGotoStatement("ht" + ps.LoopCausingSegment.ToUpper()));
                                            methodLooping.Statements.Add(checkModeFlagValue);
                                        }
                                        //condition added for a problem we encountered while aviation bulk codegeneration - mainly for default process section & process selected rows
                                    }
                                    else if (method.Parameters.Where(p => (p.FlowDirection == FlowAttribute.IN || p.FlowDirection == FlowAttribute.INOUT) && p.Seg.Inst == MULTI_INSTANCE).Any())
                                        methodLooping.Statements.Add(AssignVariable(FieldReferenceExp(ThisReferenceExp(), "nvcTmp"), SnippetExpression("(NameValueCollection) " + "ht" + (method.LoopSegment != null ? method.LoopSegment.Name : ps.LoopCausingSegment).ToUpper() + "[nLoop]")));
                                    foreach (CodeStatement statement in PS_MethodExecution(ps, method))
                                    {
                                        methodLooping.Statements.Add(statement);
                                    }
                                }
                            }
                            else
                            {
                                foreach (CodeStatement statement in PS_MethodExecution(ps, method))
                                {
                                    methodLooping.Statements.Add(statement);
                                }
                            }

                            ExtensionMethods.AddStatement(scope, methodLooping);
                        }
                        #endregion Default process section


                        //for alternate process section
                        #region Alternate process section
                        else
                        {

                            if (!string.IsNullOrEmpty(ps.LoopCausingSegment))
                            {

                                //code for PSR,PMR,PSMR
                                Segment loopSegment = _service.Segments.Where(s => s.Name == ps.LoopCausingSegment).First();
                                if (loopSegment.Process_sel_rows == "sy" || loopSegment.Process_upd_rows == "sy" || loopSegment.Process_sel_upd_rows == "sy")
                                {
                                    string sRowProcessingType = string.Empty;

                                    if (loopSegment.Process_sel_rows == "sy")
                                        sRowProcessingType = "psr";
                                    if (loopSegment.Process_upd_rows == "sy")
                                        sRowProcessingType = "pmr";
                                    if (loopSegment.Process_sel_upd_rows == "sy")
                                        sRowProcessingType = "psmr";

                                    ExtensionMethods.AddStatement(scope, AssignVariable(FieldReferenceExp(ThisReferenceExp(), "nvcTmp"), SnippetExpression("(NameValueCollection) " + "ht" + ps.LoopCausingSegment.ToUpper() + "[nLoop]")));
                                    ExtensionMethods.AddStatement(scope, AssignVariable(VariableReferenceExp("modeFlagValue"), ArrayIndexerExpression("nvcTmp", PrimitiveExpression("modeflag"))));
                                    ExtensionMethods.AddStatement(scope, MethodInvocationExp(ThisReferenceExp(), "EvaluateModeFlagExpression").AddParameters(new CodeExpression[] { PrimitiveExpression(sRowProcessingType), VariableReferenceExp("modeFlagValue"), SnippetExpression("out ExecuteSpFlag") }));

                                    CodeConditionStatement ifExecuteSpFlagIsTrue = IfCondition();
                                    ifExecuteSpFlagIsTrue.Condition = VariableReferenceExp("ExecuteSpFlag");
                                    foreach (CodeStatement statement in PS_MethodExecution(ps, method))
                                    {
                                        ifExecuteSpFlagIsTrue.TrueStatements.Add(statement);
                                    }

                                    ExtensionMethods.AddStatement(scope, ifExecuteSpFlagIsTrue);
                                }
                                else
                                {
                                    if (loopSegment.Process_sel_rows == "y" || loopSegment.Process_upd_rows == "y")
                                    {
                                        ExtensionMethods.AddStatement(scope, AssignVariable(FieldReferenceExp(ThisReferenceExp(), "nvcTmp"), SnippetExpression("(NameValueCollection) " + "ht" + ps.LoopCausingSegment.ToUpper() + "[nLoop]")));
                                        ExtensionMethods.AddStatement(scope, AssignVariable(VariableReferenceExp("sValue"), ArrayIndexerExpression("nvcTmp", PrimitiveExpression("ModeFlag"))));
                                        CodeConditionStatement checkModeFlagValue = IfCondition();
                                        if (loopSegment.Process_sel_rows == "y")
                                            checkModeFlagValue.Condition = SnippetExpression("sValue == \"I\" || sValue == \"U\" || sValue == \"S\"");
                                        else if (loopSegment.Process_upd_rows == "y")
                                            checkModeFlagValue.Condition = BinaryOpertorExpression(VariableReferenceExp("sValue"), CodeBinaryOperatorType.IdentityEquality, PrimitiveExpression("S"));
                                        checkModeFlagValue.TrueStatements.Add(new CodeGotoStatement("ht" + ps.LoopCausingSegment.ToUpper()));
                                        ExtensionMethods.AddStatement(scope, checkModeFlagValue);
                                    }
                                    foreach (CodeStatement statement in PS_MethodExecution(ps, method))
                                    {
                                        ExtensionMethods.AddStatement(scope, statement);
                                    }
                                }
                            }
                            else
                            {
                                foreach (CodeStatement statement in PS_MethodExecution(ps, method))
                                {
                                    ExtensionMethods.AddStatement(scope, statement);
                                }
                            }
                        }
                        #endregion Alternate process section

                        CodeConditionStatement codeConditionStatement = new CodeConditionStatement
                        {
                            Condition = BinaryOpertorExpression(VariableReferenceExp("psIndex"), CodeBinaryOperatorType.IdentityInequality, PrimitiveExpression(-1))
                        };
                        CodeConditionStatement CheckPsIndex = codeConditionStatement;
                        if (ps.ProcessingType == ProcessingType.ALTERNATE)
                            CheckPsIndex.TrueStatements.Add(MethodInvocationExp(BaseReferenceExp(), "PrepareIBRForExecution").AddParameters(new CodeExpression[] { VariableReferenceExp("psIndex"), VariableReferenceExp("brSeqNo"), PrimitiveExpression(1), VariableReferenceExp("nLoop") }));
                        else
                            CheckPsIndex.TrueStatements.Add(MethodInvocationExp(BaseReferenceExp(), "PrepareIBRForExecution").AddParameters(new CodeExpression[] { VariableReferenceExp("psIndex"), VariableReferenceExp("brSeqNo"), PrimitiveExpression(1), PrimitiveExpression(0) }));

                        if (method.method_exec_cont)
                        {
                            mtTry.TryStatements.Add(scope);
                        }

                        mtTry.AddStatement(CheckPsIndex);
                    }
                    #endregion

                    #region is api consumer method
                    else if (method.IsApiConsumerService == true)
                    {
                        //string sApiMethodName = string.Empty;
                        _service.ConsumesApi = true;

                        //if (string.Compare(method.OperationVerb, "get", true) == 0)
                        //    sApiMethodName = "Get";
                        //else
                        //    sApiMethodName = "Post";

                        CodeCommentStatement comment = CommentStatement("Starting to execute " + ((ps.ProcessingType == ProcessingType.ALTERNATE) ? "alternate" : "default") + " style process section " + ps.SeqNO);
                        CodeCommentStatement comment1 = CommentStatement("Starting to execute the BR - " + method.SeqNO + "  under the process section - " + ps.SeqNO);

                        statements.Add(comment);
                        statements.Add(comment1);

                        foreach (CodeStatement statement in PS_BRExecution_DeclareVariable(ps, method))
                            statements.Add(statement);

                        mtTry = new CodeTryCatchFinallyStatement();
                        mtTry.TryStatements.Add(MethodInvocationExp(ThisReferenceExp(), "LogMessage").AddParameters(new CodeExpression[] { PrimitiveExpression($"{method.SectionName}"), PrimitiveExpression("Initializing ApiSpecMetadataProvider..") }));
                        mtTry.TryStatements.Add(DeclareVariableAndAssign("IApiSpecMetadataProvider", "SpecMetadataProvider", true, ObjectCreateExpression("DefaultSpecMetadataProvider")));

                        mtTry.TryStatements.Add(MethodInvocationExp(ThisReferenceExp(), "LogMessage").AddParameters(new CodeExpression[] { PrimitiveExpression($"{method.SectionName}"), PrimitiveExpression("Initializing client..") }));
                        mtTry.TryStatements.Add(DeclareVariableAndAssign("ICoreClient", "client", true, ObjectCreateExpression("CoreClient", new CodeExpression[] {
                            //TypeReferenceExp("apiSpecMetadata"),
                            VariableReferenceExp("SpecMetadataProvider"),
                            PrimitiveExpression(method.SpecName),
                            GetProperty(TypeReferenceExp(typeof(string)), "Empty"),
                            PrimitiveExpression($"/v{method.SpecVersion}") })));

                        mtTry.TryStatements.Add(MethodInvocationExp(ThisReferenceExp(), "LogMessage").AddParameters(new CodeExpression[] { PrimitiveExpression($"{method.SectionName}"), PrimitiveExpression("Getting Access token..") }));
                        mtTry.TryStatements.Add(DeclareVariableAndAssign("AccessToken", "token", true, MethodInvocationExp(TypeReferenceExp("client"), "RequestClientCredentialsToken").AddParameter(SnippetExpression("new string[]{\"rvw_nonimpersonate\"}"))));

                        mtTry.TryStatements.Add(MethodInvocationExp(ThisReferenceExp(), "LogMessage").AddParameters(new CodeExpression[] { PrimitiveExpression($"{method.SectionName}"), PrimitiveExpression("Intializing request object..") }));
                        mtTry.TryStatements.Add(DeclareVariableAndAssign("ICoreRequest", "request", true, ObjectCreateExpression("CoreRequest", new CodeExpression[] { TypeReferenceExp("token") })));
                        mtTry.TryStatements.Add(AssignVariable(FieldReferenceExp(TypeReferenceExp("request"), "Resource"), PrimitiveExpression(method.RoutePath)));


                        if (method.ApiParameters != null)
                        {
                            #region context parameters
                            if (method.ApiParameters.Where(p => string.Compare(p.ParameterName, "contextrolename", true) == 0
                                                             || string.Compare(p.ParameterName, "contextouid", true) == 0
                                                             || string.Compare(p.ParameterName, "contextlangid", true) == 0).Any())
                                //mtTry.TryStatements.Add(MethodInvocationExp(TypeReferenceExp("request"), "SetContextValue").AddParameters(new CodeExpression[] { SnippetExpression("Convert.ToInt32(szLangID)"), SnippetExpression("Convert.ToInt32(szOUI)"), VariableReferenceExp("szRole") }));
                                mtTry.TryStatements.Add(MethodInvocationExp(TypeReferenceExp("request"), "SetContextValue").AddParameters(new CodeExpression[] { PrimitiveExpression(1), PrimitiveExpression(1), PrimitiveExpression("adminrole") }));

                            method.ApiParameters.RemoveAll(p => string.Compare(p.ParameterName, "contextrolename", true) == 0
                                                                || string.Compare(p.ParameterName, "contextouid", true) == 0
                                                                || string.Compare(p.ParameterName, "contextlangid", true) == 0);
                            #endregion context parameters


                            foreach (var apiParameter in method.ApiParameters)
                            {


                                mtTry.TryStatements.Add(MethodInvocationExp(TypeReferenceExp("request"), apiParameter.methodToUse).AddParameters(new CodeExpression[] {
                                                                                                                                                                PrimitiveExpression(apiParameter.ParameterName),
                                                                                                                                                                ArrayIndexerExpression($"nvc{apiParameter.Segment.Name}", PrimitiveExpression(apiParameter.DataItem.Name))
                                                                                                                                                            }));
                            }
                        }

                        #region Api Request building
                        if (method.ApiRequestInfo != null)
                        {
                            foreach (CodeStatement statement in PS_BR_ApiRequest(method))
                            {
                                mtTry.TryStatements.Add(statement);
                            }
                        }
                        #endregion



                        mtTry.TryStatements.Add(MethodInvocationExp(ThisReferenceExp(), "LogMessage").AddParameters(new CodeExpression[] { PrimitiveExpression($"{method.SectionName}"), PrimitiveExpression("Calling api..") }));
                        mtTry.TryStatements.Add(DeclareVariableAndAssign("ICoreResponse", "response", true, MethodInvocationExp(TypeReferenceExp("client"), $"{method.OperationVerb}Api").AddParameter(TypeReferenceExp("request"))));

                        mtTry.TryStatements.Add(MethodInvocationExp(ThisReferenceExp(), "LogMessage").AddParameters(new CodeExpression[] { PrimitiveExpression($"{method.SectionName}"), MethodInvocationExp(SnippetExpression("string"), "Format").AddParameters(new CodeExpression[] { PrimitiveExpression("Response Statuscode : {0}"), FieldReferenceExp(VariableReferenceExp("response"), "StatusCode") }) }));
                        mtTry.TryStatements.Add(MethodInvocationExp(ThisReferenceExp(), "LogMessage").AddParameters(new CodeExpression[] { PrimitiveExpression("Response.Content"), FieldReferenceExp(VariableReferenceExp("response"), "Content") }));
                        mtTry.TryStatements.Add(DeclareVariableAndAssign("JObject", "resObj", true, PrimitiveExpression(null)));

                        CodeConditionStatement ifResponseStatusIsAvailable = IfCondition();
                        ifResponseStatusIsAvailable.Condition = BinaryOpertorExpression(FieldReferenceExp(TypeReferenceExp("response"), "Status"), CodeBinaryOperatorType.IdentityEquality, PrimitiveExpression(true));

                        CodeConditionStatement ifResponseCodeEqualsOK = IfCondition();
                        ifResponseCodeEqualsOK.Condition = BinaryOpertorExpression(MethodInvocationExp(TypeReferenceExp(typeof(string)), "CompareOrdinal").AddParameters(new CodeExpression[] { FieldReferenceExp(TypeReferenceExp("response"), "StatusCode"), PrimitiveExpression("200") }),
                                                                                    CodeBinaryOperatorType.IdentityEquality,
                                                                                    PrimitiveExpression(0));
                        ifResponseCodeEqualsOK.TrueStatements.Add(AssignVariable(VariableReferenceExp("resObj"), MethodInvocationExp(TypeReferenceExp("response"), "ContentAsJObject")));
                        ifResponseCodeEqualsOK.TrueStatements.Add(MethodInvocationExp(ThisReferenceExp(), "LogMessage").AddParameters(new CodeExpression[] { PrimitiveExpression($"{method.SectionName}"), PrimitiveExpression("Response object value is as follows:") }));
                        ifResponseCodeEqualsOK.TrueStatements.Add(MethodInvocationExp(ThisReferenceExp(), "LogMessage").AddParameters(new CodeExpression[] { PrimitiveExpression($"{method.SectionName}"), MethodInvocationExp(TypeReferenceExp(typeof(JsonConvert)), "SerializeObject").AddParameter(VariableReferenceExp("resObj")) }));
                        ifResponseStatusIsAvailable.TrueStatements.Add(ifResponseCodeEqualsOK);



                        CodeConditionStatement ifResponseCodeEqualsBadRequest = IfCondition();
                        ifResponseCodeEqualsBadRequest.Condition = BinaryOpertorExpression(MethodInvocationExp(TypeReferenceExp(typeof(string)), "CompareOrdinal").AddParameters(new CodeExpression[] { FieldReferenceExp(TypeReferenceExp("response"), "StatusCode"), PrimitiveExpression("400") }),
                                                                                    CodeBinaryOperatorType.IdentityEquality,
                                                                                    PrimitiveExpression(0));


                        ifResponseCodeEqualsBadRequest.TrueStatements.Add(DeclareVariableAndAssign("JToken", "responseContentAsJToken", true, MethodInvocationExp(TypeReferenceExp("JToken"), "Parse").AddParameter(FieldReferenceExp(TypeReferenceExp("response"), "Content"))));

                        CodeConditionStatement ifResponseDataIsJObject = IfCondition();
                        ifResponseDataIsJObject.Condition = SnippetExpression("responseContentAsJToken is JObject");
                        ifResponseDataIsJObject.TrueStatements.Add(CommentStatement("System error when model data is invalid"));
                        ifResponseDataIsJObject.TrueStatements.Add(MethodInvocationExp(BaseReferenceExp(), "Set_Error_Info").AddParameters(new CodeExpression[] {
                            PrimitiveExpression(0),
                            FieldReferenceExp(TypeReferenceExp("CUtil"),"FRAMEWORK_ERROR"),
                            FieldReferenceExp(TypeReferenceExp("CUtil"),"STOP_PROCESSING"),
                            PrimitiveExpression("Invalid data.Kindly check the log for response content to debug further."),
                            GetProperty(TypeReferenceExp(typeof(string)),"Empty"),
                            GetProperty(TypeReferenceExp(typeof(string)),"Empty"),
                            PrimitiveExpression(0),
                            GetProperty(TypeReferenceExp(typeof(string)),"Empty"),
                            GetProperty(TypeReferenceExp(typeof(string)),"Empty")
                            }));
                        ifResponseCodeEqualsBadRequest.TrueStatements.Add(ifResponseDataIsJObject);

                        CodeConditionStatement ifResponseDataIsJArray = IfCondition();
                        ifResponseDataIsJArray.Condition = SnippetExpression("responseContentAsJToken is JArray");
                        ifResponseDataIsJArray.TrueStatements.Add(CommentStatement("Application Error"));
                        ifResponseDataIsJArray.TrueStatements.Add(CommentStatement("eg:[{\"Seqno\":0,\"Id\":0,\"Source\":\"App\",\"Description\":\"Username ADMINUSER already exists\",\"Correctiveaction\":\"Contact Support Team\"}]"));
                        ifResponseDataIsJArray.TrueStatements.Add(MethodInvocationExp(BaseReferenceExp(), "Set_Error_Info").AddParameters(new CodeExpression[] {
                            PrimitiveExpression(0),
                            FieldReferenceExp(TypeReferenceExp("CUtil"),"FRAMEWORK_ERROR"),
                            FieldReferenceExp(TypeReferenceExp("CUtil"),"STOP_PROCESSING"),
                            MethodInvocationExp(TypeReferenceExp(typeof(string)),"Format").AddParameters(new CodeExpression[]{ PrimitiveExpression("General Exception at PS - 1 BR - 1. {0}"),MethodInvocationExp(MethodInvocationExp(VariableReferenceExp("responseContentAsJToken"),"SelectToken").AddParameter(PrimitiveExpression("[0].Description")),"ToString") }),
                            GetProperty(TypeReferenceExp(typeof(string)),"Empty"),
                            GetProperty(TypeReferenceExp(typeof(string)),"Empty"),
                            PrimitiveExpression(0),
                            GetProperty(TypeReferenceExp(typeof(string)),"Empty"),
                            GetProperty(TypeReferenceExp(typeof(string)),"Empty")
                        }));
                        ifResponseDataIsJObject.FalseStatements.Add(ifResponseDataIsJArray);

                        ifResponseStatusIsAvailable.FalseStatements.Add(ifResponseCodeEqualsBadRequest);
                        ifResponseStatusIsAvailable.FalseStatements.Add(ThrowNewException(ObjectCreateExpression("InvalidOperationException", new CodeExpression[] { SnippetExpression("\"Bad Request.Check log to debug further.\"") })));

                        mtTry.TryStatements.Add(ifResponseStatusIsAvailable);


                        #region Api Response building
                        if (method.ApiResponseInfo != null)
                        {
                            foreach (CodeStatement statement in PS_BR_ApiResponse(method))
                            {
                                mtTry.TryStatements.Add(statement);
                            }

                            mtTry.TryStatements.Add(CommentStatement("todo"));
                        }
                        #endregion Api Response building


                        //AddCatchClause(mtTry, "PS_BRExecution");
                        CodeCatchClause catchCRVWException = mtTry.AddCatch("CRVWException", "rvwe");
                        catchCRVWException.Statements.Add(MethodInvocationExp(ThisReferenceExp(), "LogMessage").AddParameters(new CodeExpression[] { PrimitiveExpression("ExceptionMsg"), SnippetExpression("rvwe.InnerException != null ? rvwe.InnerException.Message : rvwe.Message") }));
                        catchCRVWException.Statements.Add(MethodInvocationExp(ThisReferenceExp(), "LogMessage").AddParameters(new CodeExpression[] { PrimitiveExpression("StackTrace"), SnippetExpression("rvwe.StackTrace") }));
                        ThrowException(catchCRVWException, "rvwe");


                        CodeCatchClause catchOtherException = mtTry.AddCatch("Exception", "e");
                        catchOtherException.Statements.Add(DeclareVariableAndAssign(typeof(string), "sMsg", true, SnippetExpression("e.InnerException != null ? e.InnerException.Message : e.Message")));
                        catchOtherException.Statements.Add(MethodInvocationExp(ThisReferenceExp(), "LogMessage").AddParameters(new CodeExpression[] { PrimitiveExpression("ExceptionMsg"), VariableReferenceExp("sMsg") }));
                        catchOtherException.Statements.Add(MethodInvocationExp(ThisReferenceExp(), "LogMessage").AddParameters(new CodeExpression[] { PrimitiveExpression("ExceptionMsg"), FieldReferenceExp(VariableReferenceExp("e"), "StackTrace") }));
                        catchOtherException.AddStatement(MethodInvocationExp(BaseReferenceExp(), "Set_Error_Info").AddParameters(new CodeExpression[] { PrimitiveExpression(0),
                                                                                                                                                        FieldReferenceExp(TypeReferenceExp("CUtil"),"FRAMEWORK_ERROR"),
                                                                                                                                                        FieldReferenceExp(TypeReferenceExp("CUtil"),"STOP_PROCESSING"),
                                                                                                                                                        MethodInvocationExp(TypeReferenceExp(typeof(string)),"Format").AddParameters(new CodeExpression[] { PrimitiveExpression("General Exception at PS - "+ ps.SeqNO +" BR - "+method.SeqNO),GetProperty("e","Message") }),
                                                                                                                                                        GetProperty(TypeReferenceExp(typeof(string)),"Empty"),
                                                                                                                                                        GetProperty(TypeReferenceExp(typeof(string)),"Empty"),
                                                                                                                                                        PrimitiveExpression(0),
                                                                                                                                                        GetProperty(TypeReferenceExp(typeof(string)),"Empty"),
                                                                                                                                                        GetProperty(TypeReferenceExp(typeof(string)),"Empty")
                                                                                                                                                        }));

                        mtTry.FinallyStatements.Add(SnippetExpression("CloseCommand()"));
                        mtTry.FinallyStatements.Add(SnippetExpression("Close()"));

                        statements.Add(mtTry);
                    }
                    #endregion is api consumer method

                    #region is integration service
                    else if (method.IsIntegService == true)
                    {
                        foreach (CodeStatement statement in PS_BRExecution_IS(ps, method))
                        {
                            statements.Add(statement);
                        }
                    }
                    #endregion
                }
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("PS_BRExecution->{0}", !Object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }

            return statements;
        }

        /// <summary>
        /// Generates 'ProcessPS[X]' methods
        /// </summary>
        /// <param name="psName"></param>
        /// <param nxame="psSeqNO"></param>
        /// <returns></returns>
        private CodeMemberMethod ProcessSection(ProcessSection ps)
        {
            bool psHasCtrlExp = false;

            try
            {
                String sContext = String.Format("ProcessPS{0}", ps.SeqNO);
                CodeMemberMethod ProcessSection = new CodeMemberMethod
                {
                    Name = sContext,
                    Attributes = MemberAttributes.Private
                };

                //logger.WriteLogToFile(String.Empty, String.Format("Generating Code for : {0}", ps.Name));

                CodeVariableDeclarationStatement vbpsFlag = DeclareVariable(typeof(bool), "bpsflag");
                CodeVariableDeclarationStatement vbmtflag = DeclareVariableAndAssign(typeof(bool), "bmtflag", true, PrimitiveExpression(false));
                CodeVariableDeclarationStatement vbBRExec = DeclareVariable(typeof(bool), "bBRExec");
                CodeVariableDeclarationStatement vpsIndex = DeclareVariableAndAssign(typeof(int), "psIndex", true, PrimitiveExpression(int.Parse("0")));
                CodeVariableDeclarationStatement vbrIndex = DeclareVariableAndAssign(typeof(int), "brIndex", true, PrimitiveExpression(int.Parse("0")));
                CodeVariableDeclarationStatement vpsSeqNo = DeclareVariableAndAssign(typeof(int), "psSeqNo", true, PrimitiveExpression(int.Parse(ps.SeqNO)));
                CodeVariableDeclarationStatement vbrSeqNo = DeclareVariableAndAssign(typeof(int), "brSeqNo", true, PrimitiveExpression(int.Parse("0")));
                CodeVariableDeclarationStatement vsysfprowno = DeclareVariableAndAssign(typeof(bool), "sysfprowno", true, PrimitiveExpression(false));
                CodeVariableDeclarationStatement vExecuteSpFlag = DeclareVariableAndAssign(typeof(bool), "ExecuteSpFlag", true, PrimitiveExpression(false));
                ProcessSection.AddStatement(vbpsFlag);
                ProcessSection.AddStatement(vbmtflag);
                ProcessSection.AddStatement(vbBRExec);
                ProcessSection.AddStatement(vpsIndex);
                ProcessSection.AddStatement(vbrIndex);
                ProcessSection.AddStatement(vpsSeqNo);
                ProcessSection.AddStatement(vbrSeqNo);
                ProcessSection.AddStatement(vsysfprowno);
                ProcessSection.AddStatement(vExecuteSpFlag);

                CodeTryCatchFinallyStatement tryBlock = ProcessSection.AddTry();
                CodeCatchClause catchCRVWException = tryBlock.AddCatch("CRVWException", "rvwe");
                ThrowException(catchCRVWException, "rvwe");

                CodeCatchClause catchOtherException = tryBlock.AddCatch("Exception", "e");
                catchOtherException.AddStatement(MethodInvocationExp(BaseReferenceExp(), "Set_Error_Info").AddParameters(new CodeExpression[] { PrimitiveExpression(0),
                                                                                                                                                        FieldReferenceExp(TypeReferenceExp("CUtil"),"FRAMEWORK_ERROR"),
                                                                                                                                                        FieldReferenceExp(TypeReferenceExp("CUtil"),"STOP_PROCESSING"),
                                                                                                                                                        MethodInvocationExp(TypeReferenceExp(typeof(string)),"Format").AddParameters(new CodeExpression[] { PrimitiveExpression("PS - "+ps.SeqNO+" {0}"),GetProperty("e","Message") }),
                                                                                                                                                        GetProperty(TypeReferenceExp(typeof(string)),"Empty"),
                                                                                                                                                        GetProperty(TypeReferenceExp(typeof(string)),"Empty"),
                                                                                                                                                        PrimitiveExpression(0),
                                                                                                                                                        GetProperty(TypeReferenceExp(typeof(string)),"Empty"),
                                                                                                                                                        GetProperty(TypeReferenceExp(typeof(string)),"Empty")
                                                                                                                                                        }));
                //generating control expression check code for default process section
                if (ps.ProcessingType == ProcessingType.DEFAULT)
                {
                    if ((ps.CtrlExp == null).Equals(true) ? false : ps.CtrlExp.IsValid)
                    {
                        String strCtrlExpSeg = "nvc" + ps.CtrlExp.Seg.Name.ToUpperInvariant();
                        String strCtrlExpDI = ps.CtrlExp.DI.Name.ToLowerInvariant();
                        psHasCtrlExp = true;

                        tryBlock.AddStatement(AssignVariable(VariableReferenceExp(vbpsFlag.Name), PrimitiveExpression(true)));

                        tryBlock.AddStatement(CommentStatement("Control expression check for default process section"));
                        CodeConditionStatement checkCtrlExp = IfCondition();
                        checkCtrlExp.Condition = BinaryOpertorExpression(BinaryOpertorExpression(FieldReferenceExp(ThisReferenceExp(), strCtrlExpSeg), CodeBinaryOperatorType.IdentityInequality, SnippetExpression("null")),
                                                                        CodeBinaryOperatorType.BooleanAnd,
                                                                        BinaryOpertorExpression(ArrayIndexerExpression(strCtrlExpSeg, PrimitiveExpression(strCtrlExpDI)), CodeBinaryOperatorType.IdentityInequality, PrimitiveExpression("0")));
                        checkCtrlExp.TrueStatements.Add(AssignVariable(VariableReferenceExp("bpsflag"), PrimitiveExpression(false)));
                        tryBlock.AddStatement(checkCtrlExp);
                    }
                }


                if (ps.ProcessingType == ProcessingType.DEFAULT)
                {
                    if (psHasCtrlExp)
                    {
                        CodeConditionStatement Checkbpsflag = IfCondition();
                        Checkbpsflag.Condition = BinaryOpertorExpression(VariableReferenceExp("bpsflag"), CodeBinaryOperatorType.IdentityEquality, PrimitiveExpression(true));
                        tryBlock.AddStatement(Checkbpsflag);

                        foreach (CodeStatement statement in PS_BRExecution(ps))
                        {
                            Checkbpsflag.TrueStatements.Add(statement);
                        }
                    }
                    else
                    {
                        foreach (CodeStatement statement in PS_BRExecution(ps))
                        {
                            tryBlock.AddStatement(statement);
                        }
                    }
                }
                else if (ps.ProcessingType == ProcessingType.ALTERNATE)
                {
                    //with control expression
                    if (ps.CtrlExp != null)
                    {
                        if (ps.CtrlExp.IsValid)
                        {
                            //string strCtrlExpSeg = (ps.CtrlExp.Seg.Inst == MULTI_INSTANCE ? "ht" : "nvc") + ps.CtrlExp.Seg.Name.ToUpperInvariant();
                            //string strCtrlExpDI = ps.CtrlExp.DI.Name.ToLowerInvariant();

                            tryBlock.AddStatement(CommentStatement("Evaluate the PS level control expression"));

                            //if (!string.IsNullOrEmpty(ps.LoopCausingSegment))
                            //{
                            //    tryBlock.AddStatement(AssignVariable(VariableReferenceExp("nMax"), SnippetExpression(String.Format("ht{0}.Count.ToString()", ps.LoopCausingSegment.ToUpper()))));
                            //}

                            foreach (CodeStatement statement in PS_Alternate_CtrlExp_Check(ps))
                            {
                                tryBlock.AddStatement(statement);
                            }
                        }
                    }

                    //without control expression
                    else
                    {
                        if (!string.IsNullOrEmpty(ps.LoopCausingSegment))
                        {
                            tryBlock.AddStatement(AssignVariable(VariableReferenceExp("nMax"), PrimitiveExpression(0)));
                            tryBlock.AddStatement(AssignVariable(VariableReferenceExp("nMax"), GetProperty(FieldReferenceExp(ThisReferenceExp(), "ht" + ps.LoopCausingSegment), "Count")));
                            CodeIterationStatement forLoop = ForLoopExpression(AssignVariable(VariableReferenceExp("nLoop"), PrimitiveExpression(1)),
                                                                                BinaryOpertorExpression(VariableReferenceExp("nLoop"), CodeBinaryOperatorType.LessThanOrEqual, VariableReferenceExp("nMax")),
                                                                                AssignVariable(VariableReferenceExp("nLoop"), BinaryOpertorExpression(VariableReferenceExp("nLoop"), CodeBinaryOperatorType.Add, PrimitiveExpression(1))));

                            //TECH_XXXX **starts**
                            forLoop.Statements.Add(AssignVariable(VariableReferenceExp("nvcTmp"), SnippetExpression(string.Format("(NameValueCollection)ht{0}[nLoop]", ps.LoopCausingSegment.ToUpperInvariant()))));
                            //TECH_XXXX **ends**

                            foreach (CodeStatement statement in PS_BRExecution(ps))
                            {
                                forLoop.Statements.Add(statement);
                            }
                            tryBlock.AddStatement(forLoop);
                        }
                        else
                        {
                            foreach (CodeStatement statement in PS_BRExecution(ps))
                            {
                                tryBlock.AddStatement(statement);
                            }
                        }
                    }
                }

                return ProcessSection;
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("ProcessSection->{0}", !Object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }


        private void ProcessSection_Finally(String sContext, CodeTryCatchFinallyStatement tryBlock, ProcessSection ps)
        {
            try
            {
                tryBlock.AddCatch();
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("ProcessSection_Finally->{0}", !Object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }


        private void AddCatchClause(CodeTryCatchFinallyStatement tryBlock, String MethodName)
        {
            try
            {
                CodeCatchClause catchBlock = AddCatchBlock(tryBlock);
                WriteProfiler(MethodName, tryBlock, catchBlock, null, "EX");

                switch (MethodName.ToLower())
                {
                    case "fillunmappeddataitems":
                    case "buildoutsegments":
                    case "processsection":
                    case "ps_brexecution_is":
                        Set_Error_Info(MethodName, null, catchBlock, null);
                        break;
                    case "getsegmentvalue":
                        catchBlock.AddStatement(ReturnExpression(GetProperty(typeof(String), "Empty")));
                        break;
                    default:
                        break;
                }
                ThrowException(catchBlock);
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("AddCatchClause->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }

        private CodeMemberMethod SetRecordPtrEx(IEnumerable<Segment> segWithKeyDI)
        {
            CodeMemberMethod setRecordPtrEx = null;
            try
            {
                setRecordPtrEx = new CodeMemberMethod
                {
                    Attributes = MemberAttributes.Private,
                    ReturnType = new CodeTypeReference(typeof(long)),
                    Name = "SetRecordPtrEx"
                };
                setRecordPtrEx.Parameters.Add(new CodeParameterDeclarationExpression(typeof(string), "szSegName"));
                setRecordPtrEx.Parameters.Add(new CodeParameterDeclarationExpression(typeof(NameValueCollection), "nvcSearchFields"));

                setRecordPtrEx.AddStatement(DeclareVariable(typeof(long), "i"));
                setRecordPtrEx.AddStatement(DeclareVariable(typeof(NameValueCollection), "nvcTemp"));
                setRecordPtrEx.AddStatement(DeclareVariable(typeof(bool), "bDataMatches"));
                setRecordPtrEx.AddStatement(AssignVariable(VariableReferenceExp("bDataMatches"), PrimitiveExpression(false)));
                setRecordPtrEx.AddStatement(AssignVariable(VariableReferenceExp("i"), PrimitiveExpression(1)));

                setRecordPtrEx.AddStatement(SnippetStatement("switch(szSegName.ToLower())"));
                setRecordPtrEx.AddStatement(SnippetStatement("{"));
                foreach (Segment seg in segWithKeyDI.Distinct())
                {
                    setRecordPtrEx.AddStatement(SnippetStatement(string.Format("case(\"{0}\"):", seg.Name.ToLower())));
                    if (seg.Inst == MULTI_INSTANCE)
                    {
                        setRecordPtrEx.AddStatement(SnippetStatement(string.Format("while (i <= (long)ht{0}.Count)", seg.Name.ToUpper())));
                        setRecordPtrEx.AddStatement(SnippetStatement("{"));

                        setRecordPtrEx.AddStatement(AssignVariable(VariableReferenceExp("nvcTemp"), SnippetExpression(string.Format("(NameValueCollection)ht{0}[i]", seg.Name.ToUpper()))));

                        foreach (DataItem di in seg.DataItems.Where(d => d.PartOfKey == true))
                        {
                            CodeConditionStatement checkForDataMatch = IfCondition();
                            checkForDataMatch.Condition = BinaryOpertorExpression(BinaryOpertorExpression(ArrayIndexerExpression("nvcSearchFields", PrimitiveExpression(di.Name.ToLower())), CodeBinaryOperatorType.IdentityEquality, SnippetExpression("null")), CodeBinaryOperatorType.BooleanOr, BinaryOpertorExpression(ArrayIndexerExpression("nvcTemp", PrimitiveExpression(di.Name.ToLower())), CodeBinaryOperatorType.IdentityEquality, ArrayIndexerExpression("nvcSearchFields", PrimitiveExpression(di.Name.ToLower()))));
                            checkForDataMatch.TrueStatements.Add(AssignVariable(VariableReferenceExp("bDataMatches"), PrimitiveExpression(true)));
                            checkForDataMatch.FalseStatements.Add(AssignVariable(VariableReferenceExp("bDataMatches"), PrimitiveExpression(false)));
                            checkForDataMatch.FalseStatements.Add(SnippetExpression("i++"));
                            checkForDataMatch.FalseStatements.Add(SnippetExpression("continue"));

                            setRecordPtrEx.AddStatement(checkForDataMatch);
                        }

                        setRecordPtrEx.AddStatement(AssignVariable(VariableReferenceExp("nvcTemp"), SnippetExpression("null")));

                        CodeConditionStatement ifDataMatches = IfCondition();
                        ifDataMatches.Condition = BinaryOpertorExpression(VariableReferenceExp("bDataMatches"), CodeBinaryOperatorType.IdentityEquality, PrimitiveExpression(true));
                        ifDataMatches.TrueStatements.Add(ReturnExpression(VariableReferenceExp("i")));
                        setRecordPtrEx.AddStatement(ifDataMatches);

                        setRecordPtrEx.AddStatement(SnippetStatement("}"));
                    }
                    setRecordPtrEx.AddStatement(SnippetExpression("break"));
                }

                setRecordPtrEx.AddStatement(SnippetStatement("default:"));
                setRecordPtrEx.AddStatement(ReturnExpression(PrimitiveExpression(0)));
                setRecordPtrEx.AddStatement(SnippetStatement("}"));

                setRecordPtrEx.AddStatement(ReturnExpression(PrimitiveExpression(0)));

                return setRecordPtrEx;
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("SetRecordPtrEx->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }
        public new bool Generate()
        {
            bool bReturn;
            try
            {
                this.FillServiceObject();

                _logger.WriteLogToFile("GenerateServiceClass->Generate", string.Format("Generating service - {0}", _service.Name));

                GenerateIS generateIS = new GenerateIS(this._ecrOptions);
                generateIS.Generate(ref _service);

                if (this._service.IsSelected == true)
                    base.Generate();

                bReturn = true;
            }
            catch (Exception ex)
            {
                _ecrOptions.ErrorCollection.Add(new Error(ObjectType.Service, this._service.Name, ex.InnerException != null ? ex.InnerException.Message : ex.Message));

                _logger.WriteLogToFile("GenerateCode->Generate", ex.Message, bError: true);

                bReturn = false;
            }
            return bReturn;
        }

        private CodeMemberMethod LogMessage()
        {
            CodeMemberMethod method = new CodeMemberMethod
            {
                Name = "LogMessage",
                Attributes = MemberAttributes.Public
            };
            CodeParameterDeclarationExpression pContext = ParameterDeclarationExp(typeof(string), "sContext");
            CodeParameterDeclarationExpression pMessage = ParameterDeclarationExp(typeof(string), "sMessage");
            method.Parameters.Add(pContext);
            method.Parameters.Add(pMessage);
            method.Statements.Add(DeclareVariableAndAssign(typeof(DefaultTraceListener), "listener", true, ObjectCreateExpression(typeof(DefaultTraceListener))));
            method.Statements.Add(DeclareVariableAndAssign(typeof(string), "sFileName", true, PrimitiveExpression(string.Format(@"c:\temp\{0}.txt", _service.Name))));
            method.Statements.Add(MethodInvocationExp(VariableReferenceExp("listener"), "WriteLine").AddParameter(MethodInvocationExp(SnippetExpression("string"), "Format").AddParameters(new CodeExpression[] { PrimitiveExpression("{0} : {1}"), VariableReferenceExp("sContext"), VariableReferenceExp("sMessage") })));

            CodeConditionStatement fileExistsCheck = IfCondition();
            fileExistsCheck.Condition = BinaryOpertorExpression((MethodInvocationExp(TypeReferenceExp(typeof(File)), "Exists").AddParameter(VariableReferenceExp("sFileName"))), CodeBinaryOperatorType.IdentityEquality, PrimitiveExpression(false));
            fileExistsCheck.TrueStatements.Add(MethodInvocationExp(MethodInvocationExp(TypeReferenceExp(typeof(File)), "Create").AddParameter(VariableReferenceExp("sFileName")), "Close"));
            method.Statements.Add(fileExistsCheck);

            CodeTryCatchFinallyStatement tryClause = new CodeTryCatchFinallyStatement();
            tryClause.TryStatements.Add(DeclareVariableAndAssign(typeof(StreamWriter), "sw", true, ObjectCreateExpression(typeof(StreamWriter), new CodeExpression[] { VariableReferenceExp("sFileName"), PrimitiveExpression(true) })));
            tryClause.TryStatements.Add(MethodInvocationExp(VariableReferenceExp("sw"), "WriteLine").AddParameter(MethodInvocationExp(TypeReferenceExp(typeof(string)), "Format").AddParameters(new CodeExpression[] { PrimitiveExpression("{0} : {1} : {2}"), MethodInvocationExp(FieldReferenceExp(TypeReferenceExp(typeof(DateTime)), "Now"), "ToString"), VariableReferenceExp("sContext"), VariableReferenceExp("sMessage") })));
            tryClause.TryStatements.Add(MethodInvocationExp(VariableReferenceExp("sw"), "Close"));
            CodeCatchClause catchClause = tryClause.AddCatch();
            catchClause.AddStatement(MethodInvocationExp(VariableReferenceExp("listener"), "WriteLine").AddParameter(MethodInvocationExp(TypeReferenceExp(typeof(string)), "Format").AddParameters(new CodeExpression[] { PrimitiveExpression("LogMessage : {0}"), FieldReferenceExp(VariableReferenceExp("e"), "Message") })));
            method.Statements.Add(tryClause);


            return method;
        }

    }
}