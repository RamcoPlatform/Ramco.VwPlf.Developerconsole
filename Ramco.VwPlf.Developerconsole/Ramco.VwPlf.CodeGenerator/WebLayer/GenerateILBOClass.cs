using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.CodeDom;
using System.Data;
using System.Reflection;
using System.Xml;
using System.IO;

namespace Ramco.VwPlf.CodeGenerator.WebLayer
{
    /// <summary>
    /// Class contain functions to generate ilbo class
    /// </summary>
    internal class GenerateILBOClass : AbstractCSFileGenerator
    {
        /// <summary>
        /// object which holds ilbo information.
        /// </summary>
        private Activity _activity;
        private ILBO _ilbo;
        private ECRLevelOptions _ecrOptions;

        //constant values
        private const String CHARTCONTROL = "m_conChart";
        private const String TREECONTROL = "m_conTree";

        //repeatedly used values across methods
        IEnumerable<Control> _ctrlsInILBO;
        IEnumerable<Control> _richControlsInILBO;

        Dictionary<string, string> _dictContextDataitem;


        /// <summary>
        /// Default constructor to initialize member field
        /// </summary>
        public GenerateILBOClass(Activity activity, ILBO ilbo, ref ECRLevelOptions ecrOptions) : base(ecrOptions)
        {
            this._activity = activity;
            this._ilbo = ilbo;
            this._ecrOptions = ecrOptions;
            base._objectType = ObjectType.Activity;
            base._targetDir = Path.Combine(_ecrOptions.GenerationPath, _ecrOptions.Platform, _ecrOptions.Customer, _ecrOptions.Project, _ecrOptions.Ecrno, "Updated", _ecrOptions.Component, "Source", "ILBO", activity.Name);
            base._targetFileName = ilbo.Code;


            _ctrlsInILBO = _ilbo.Controls.Where(c => c.Type != "rslistedit" && (c.Type != "rspivotgrid" || c.Type != "pivot" || c.Type != "report list"));

            _richControlsInILBO = from c in _ctrlsInILBO.Where(c => c.SectionType != string.Empty)
                                  select c;

            //populate context dataitem
            _dictContextDataitem = new Dictionary<string, string>();
            _dictContextDataitem.Add("rvwrt_cctxt_pcomponent", "rvwrt_cctxt_pcomponent");
            _dictContextDataitem.Add("rvwrt_cctxt_component", "rvwrt_cctxt_component");
            _dictContextDataitem.Add("rvwrt_cctxt_pactivity", "rvwrt_cctxt_pactivity");
            _dictContextDataitem.Add("rvwrt_cctxt_activity", "rvwrt_cctxt_activity");
            _dictContextDataitem.Add("rvwrt_cctxt_pilbo", "rvwrt_cctxt_pilbo");
            _dictContextDataitem.Add("rvwrt_cctxt_ilbo", "rvwrt_cctxt_ilbo");

            _dictContextDataitem.Add("rvwrt_lctxt_ou", "rvwrt_lctxt_ou");
            _dictContextDataitem.Add("rvwrt_cctxt_ou", "rvwrt_cctxt_ou");

            _ilbo.HasStateCtrl = _ilbo.Controls.Where(c => c.Id == "hdnhdnrt_stcontrol").Any();
        }

        public override void CreateNamespace()
        {
            base._csFile.NameSpace.Name = this._activity.Name;
        }
        public override void ImportNamespace()
        {
            base._csFile.ReferencedNamespace.Add("System");
            base._csFile.ReferencedNamespace.Add("System.Web");
            base._csFile.ReferencedNamespace.Add("System.Xml");
            base._csFile.ReferencedNamespace.Add("System.Collections");
            if (this._ilbo.Type != "9")
                base._csFile.ReferencedNamespace.Add("System.Collections.Generic");
            base._csFile.ReferencedNamespace.Add("System.Diagnostics");

            base._csFile.ReferencedNamespace.Add("Ramco.VW.RT.Web.Core");
            base._csFile.ReferencedNamespace.Add("Ramco.VW.RT.Web.Controls");

            if (this._ilbo.Type != "9")
            {
                base._csFile.ReferencedNamespace.Add("Ramco.VW.RT.Web.Core.Controls.LayoutControls");
                base._csFile.ReferencedNamespace.Add("Ramco.VW.RT.AsyncResult");
                base._csFile.ReferencedNamespace.Add("Ramco.VW.RT.State");
                base._csFile.ReferencedNamespace.Add("Plf.Ui.Ramco.Utility");
                base._csFile.ReferencedNamespace.Add("System.Reflection");
            }
            //if (this._ilbo.HasBaseCallout)
            //{
            //    base._csFile.ReferencedNamespace.Add("Plf.Itk.Ramco.Callout");
            //    base._csFile.ReferencedNamespace.Add("Plf.Ramco.WebCallout.Interface");
            //}
        }
        public override void CreateClasses()
        {
            try
            {
                //attribute for the ilbo class.
                CodeAttributeDeclarationCollection AttributeCollection = new CodeAttributeDeclarationCollection();
                AttributeCollection.Add(new CodeAttributeDeclaration("Serializable"));

                CodeTypeDeclaration ilboClass = new CodeTypeDeclaration
                {
                    Name = _ilbo.Code,
                    IsClass = true,
                    CustomAttributes = AttributeCollection
                };

                //summary for the method
                CreateClassSummary(ilboClass, String.Format("defines all the methods of {0} class", _ilbo.Code));

                //setting access specifier
                ilboClass.Attributes = (_ilbo.Type != "9") ? MemberAttributes.Assembly : MemberAttributes.FamilyOrAssembly;

                //additional attribute - sealed class
                if (_ilbo.Type != "9")
                    ilboClass.TypeAttributes = TypeAttributes.Sealed;

                //specifying parent class
                ilboClass.BaseTypes.Add(new CodeTypeReference { BaseType = _ilbo.Type.Equals("9") ? "IILBO" : "CILBO" });

                AddMemberFields(ref ilboClass);
                AddMemberFunctions(ref ilboClass);

                base._csFile.UserDefinedTypes.Add(ilboClass);
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("CreateClass->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }
        public override void AddCustomAttributes()
        {
            //throw new NotImplementedException();
        }

        ///// <summary>
        ///// 
        ///// </summary>
        public override void AddMemberFunctions(ref CodeTypeDeclaration classObj)
        {
            try
            {
                //add method to class
                //_logger.WriteLogToFile("CreateConstructor", "Creating constructor..", bLogTiming: true);
                classObj.Members.Add(CreateConstructor());

                //_logger.WriteLogToFile("Clear", "creating function Clear..", bLogTiming: true);
                if (_ilbo.Type != "9")
                    classObj.Members.Add(Clear());

                //_logger.WriteLogToFile("InitializeControls", "Creating function 'Initializecontrols'..", bLogTiming: true);
                classObj.Members.Add(InitializeControls());

                //_logger.WriteLogToFile("ResetControls", "Creating function 'ResetControls'..", bLogTiming: true);
                classObj.Members.Add(ResetControls());

                //_logger.WriteLogToFile("AddViewInfo", "Creating function 'AddViewInfo'..", bLogTiming: true);
                classObj.Members.Add(AddViewInfo());

                //_logger.WriteLogToFile("GetControlX", "Creating function 'GetControlX'..", bLogTiming: true);
                classObj.Members.Add(GetControlX());

                //_logger.WriteLogToFile("GetControl", "Creating function 'GetContro'..", bLogTiming: true);
                classObj.Members.Add(GetControl());

                if (_ilbo.HasVwQlik)
                {
                    //_logger.WriteLogToFile("GetControlValueEx", "Creating function 'GetControlValueEx'..", bLogTiming: true);
                    classObj.Members.Add(GetControlValueEx());

                    //_logger.WriteLogToFile("GetQlikSubValue", "Creating function 'GetQlikSubValue'..", bLogTiming: true);
                    classObj.Members.Add(GetQlikSubValue());
                }

                //_logger.WriteLogToFile("GetDataItem", "Creating function 'GetDataItem'..", bLogTiming: true);
                classObj.Members.Add(GetDataItem());

                //_logger.WriteLogToFile("GetDataItemInstances", "Creating function 'GetDataItemInstances'..", bLogTiming: true);
                classObj.Members.Add(GetDataItemInstances());

                //_logger.WriteLogToFile("SetDataItem", "Creating function 'SetDataItem'..", bLogTiming: true);
                classObj.Members.Add(SetDataItem());

                //_logger.WriteLogToFile("GetVariable", "Creating function 'GetVariable'..", bLogTiming: true);
                classObj.Members.Add(GetVariable());

                //_logger.WriteLogToFile("PerformTask", "Creating function 'PerformTask'..", bLogTiming: true);
                classObj.Members.Add(PerformTask());

                if (!_ilbo.Type.Equals("9"))
                {
                    //_logger.WriteLogToFile("BeginPerformTask", "Creating function 'BeginPerformTask'..", bLogTiming: true);
                    classObj.Members.Add(BeginPerformTask());

                    //_logger.WriteLogToFile("EndPerformTask", "Creating function 'EndPerformTask'..", bLogTiming: true);
                    classObj.Members.Add(EndPerformTask());
                }
                else
                {
                    //_logger.WriteLogToFile("UpdateScreenData", "Creating function 'UpdateScreenData'..", bLogTiming: true);
                    classObj.Members.Add(UpdateScreenData());
                }

                //_logger.WriteLogToFile("PreProcess1", "Creating function 'PreProcess1'..", bLogTiming: true);
                classObj.Members.Add(BeginPreProcess1("PreProcess1"));

                //_logger.WriteLogToFile("PreProcess2", "Creating function 'PreProcess2'..", bLogTiming: true);
                classObj.Members.Add(PreProcess2());

                if (!_ilbo.Type.Equals("9"))
                {
                    //_logger.WriteLogToFile("BeginPreProcess2", "Creating function 'BeginPreProcess2'..", bLogTiming: true);
                    classObj.Members.Add(BeginPreProcess2());

                    //_logger.WriteLogToFile("EndPreProcess2", "Creating function 'EndPreProcess2'..", bLogTiming: true);
                    classObj.Members.Add(EndPreProcess2());
                }

                //_logger.WriteLogToFile("PreProcess3", "Creating fucntion 'PreProcess3'..", bLogTiming: true);
                classObj.Members.Add(PreProcess3());

                if (!_ilbo.Type.Equals("9"))
                {
                    //_logger.WriteLogToFile("BeginPreProcess3", "Creating function 'BeginPreProcess3'..", bLogTiming: true);
                    classObj.Members.Add(BeginPreProcess3());

                    //_logger.WriteLogToFile("EndPreProcess3", "Creating function 'EndPreProcess3'..", bLogTiming: true);
                    classObj.Members.Add(EndPreProcess3());
                }

                if (!_ilbo.HasContextDataItem)
                {
                    //_logger.WriteLogToFile("GetContextValue", "Creating function 'GetContextValue'..", bLogTiming: true);
                    classObj.Members.Add(GetContextValue());

                    //_logger.WriteLogToFile("SetContextValue", "Creating function 'SetContextValue'..", bLogTiming: true);
                    classObj.Members.Add(SetContextValue());
                }

                //_logger.WriteLogToFile("GetTaskData", "Creating function 'GetTaskData'..", bLogTiming: true);
                classObj.Members.Add(GetTaskData());

                //_logger.WriteLogToFile("AddDirtyTab", "Creating function 'AddDirtyTab'..", bLogTiming: true);
                classObj.Members.Add(AddDirtyTab());

                //_logger.WriteLogToFile("ObsoleteGetTaskData", "Creating function 'ObsoleteGetTaskData'..", bLogTiming: true);
                classObj.Members.Add(ObsoleteGetTaskData());

                //_logger.WriteLogToFile("GetDisplayURL", "Creating function 'GetDisplayURL'..", bLogTiming: true);
                classObj.Members.Add(GetDisplayURL());

                //_logger.WriteLogToFile("ExecuteService", "Creating function 'ExecuteService'..", bLogTiming: true);
                classObj.Members.Add(ExecuteService());

                if (!_ilbo.Type.Equals("9"))
                {
                    //_logger.WriteLogToFile("BeginExecuteService", "Creating function 'BeginExecuteService'..", bLogTiming: true);
                    classObj.Members.Add(BeginExecuteService());

                    //_logger.WriteLogToFile("EndExecuteService", "Creating function 'EndExecuteService'..", bLogTiming: true);
                    classObj.Members.Add(EndExecuteService());

                    //_logger.WriteLogToFile("InitializeTabControls", "Creating function 'InitializeTabControls'..", bLogTiming: true);
                    classObj.Members.Add(InitializeTabControls());

                    //_logger.WriteLogToFile("InitializeLayoutControls", "Creating function 'InitializeLayoutControls'..", bLogTiming: true);
                    classObj.Members.Add(InitializeLayoutControls());

                    //_logger.WriteLogToFile("ResetTabControls", "Creating function 'ResetTabControls'..", bLogTiming: true);
                    classObj.Members.Add(ResetTabControls());

                    //_logger.WriteLogToFile("ResetLayoutControls", "Creating function 'ResetLayoutControls'..", bLogTiming: true);
                    classObj.Members.Add(ResetLayoutControls());

                    //_logger.WriteLogToFile("GetTabControl", "Creating function 'GetTabControl'..", bLogTiming: true);
                    classObj.Members.Add(GetTabControl());

                    //_logger.WriteLogToFile("GetLayoutControl", "Creating function 'GetLayoutControl'..", bLogTiming: true);
                    classObj.Members.Add(GetLayoutControl());

                    //_logger.WriteLogToFile("UpdateScreenData", "Creating function 'UpdateScreenData'..", bLogTiming: true);
                    classObj.Members.Add(UpdateScreenData());
                }

                //_logger.WriteLogToFile("GetScreenData", "Creating function 'GetScreenData'..", bLogTiming: true);
                if (_ilbo.Type == "9")
                    classObj.Members.Add(GetScreenData_Zoom());
                else
                    classObj.Members.Add(GetScreenData());

                if (_ilbo.Type != "9")
                    if (_ilbo.HasPreTaskCallout || _ilbo.HasPostTaskCallout)
                        classObj.Members.Add(PerformTaskCallout());

                //_logger.WriteLogToFile("FillMessageObject", "Creating function 'FillMessageObject'..", bLogTiming: true);
                classObj.Members.Add(AddFillMessageObject());
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("CreateMemberFunctions->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }



        /// <summary>
        /// Creates member fields for the ilbo class
        /// </summary>
        public override void AddMemberFields(ref CodeTypeDeclaration ilboCls)
        {

            try
            {
                //Default members
                if (_ilbo.Type != "9")
                    DeclareMemberField(MemberAttributes.Private, ilboCls, "htContextItems", "Dictionary<String, Object>", true);
                else
                    DeclareMemberField(MemberAttributes.Private, ilboCls, "htContextItems", typeof(System.Collections.Hashtable), true);


                int rowindex = 0;

                // For Each Controls
                foreach (Control ctrl in _ctrlsInILBO)
                {
                    String sMemberVariableType = String.Empty;
                    String sMemberVariableId = ctrl.IsLayoutControl ? string.Format("_{0}", ctrl.Id) : string.Format("m_con{0}", ctrl.Id);

                    switch (ctrl.Type.ToUpper())
                    {
                        case "RSEDITCTRL":
                            if (ctrl.ListEditRequired)
                                sMemberVariableType = "ListEdit";
                            else
                                sMemberVariableType = "Edit";
                            break;
                        case "RSCOMBOCTRL":
                            sMemberVariableType = "Combo";
                            break;
                        case "RSCHECK":
                            sMemberVariableType = "Check";
                            break;
                        case "RSGROUP":
                            sMemberVariableType = "Group";
                            break;
                        case "RSLIST":
                            sMemberVariableType = "Edit";
                            break;
                        case "RSSTACKEDLINKS":
                            sMemberVariableType = "StackedLinks";
                            break;
                        case "RSPIVOTGRID":
                            sMemberVariableType = "PivotGrid";
                            break;
                        case "RSASSORTED":
                            sMemberVariableType = "Assorted";
                            break;
                        case "RSGRID":
                            sMemberVariableType = "Multiline";
                            break;
                        case "RSTREEGRID":
                            sMemberVariableType = "TreeGrid";
                            break;
                        case "RSSLIDER":
                            sMemberVariableType = "Slider";
                            break;
                        case "RSLISTVIEW":
                            sMemberVariableType = "ListView";
                            break;
                        default:
                            sMemberVariableType = Common.InitCaps(ctrl.Type);
                            break;
                    }
                    DeclareMemberField(MemberAttributes.Private, ilboCls, sMemberVariableId, sMemberVariableType, true);
                    rowindex++;
                }

                // For Extended Controls
                AddMemberFields_Ext(ref ilboCls);

            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("AddMemberFields->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }

        private void AddMemberFields_Ext(ref CodeTypeDeclaration _ilboCls)
        {
            try
            {
                List<String> ExtControlList = new List<String>();

                #region declarations based on options
                if (_ilbo.HasBaseCallout && !_ilbo.Type.Equals("9"))
                    DeclareMemberField(MemberAttributes.Private, _ilboCls, "objActBaseEx", String.Format("{0}__.{1}", _activity.Name, _ilbo.Code), true);

                if (_ilbo.HasTree && !_ilbo.Type.Equals("9"))
                    DeclareMemberField(MemberAttributes.Private, _ilboCls, TREECONTROL, String.Format("{0}_tr", _ilbo.Code), true);

                if (_ilbo.HasChart && !_ilbo.Type.Equals("9"))
                    DeclareMemberField(MemberAttributes.Private, _ilboCls, CHARTCONTROL, String.Format("{0}_ch", _ilbo.Code), true);

                if (_ilbo.HasRichControl && !_ilbo.Type.Equals("9"))
                    DeclareMemberField(MemberAttributes.Private, _ilboCls, "oPlfRichControls", "PlfRichControls", true);

                if (_ilbo.HasDataDrivenTask && !_ilbo.Type.Equals("9"))
                    DeclareMemberField(MemberAttributes.Private, _ilboCls, "oDDTask", "DataDrivenTask", true);

                if (_ilbo.HasDynamicLink && !_ilbo.Type.Equals("9"))
                {
                    DeclareMemberField(MemberAttributes.Private, _ilboCls, "oDlink", "DynamicLink", true);
                    DeclareMemberField(MemberAttributes.Private, _ilboCls, "bLink", typeof(bool), true, null, SnippetExpression("false"));
                }

                if (_ilbo.HasMessageLookup && !_ilbo.Type.Equals("9"))
                {
                    DeclareMemberField(MemberAttributes.Private, _ilboCls, "oErrorlookup", "ErrorLookup", true);
                    DeclareMemberField(MemberAttributes.Private, _ilboCls, "bMsgLkup", typeof(bool), true, null, SnippetExpression("false"));
                }

                if (_ilbo.HasControlExtensions && !_ilbo.Type.Equals("9"))
                {
                    DeclareMemberField(MemberAttributes.Private, _ilboCls, "oCE", "ControlExtensions", true);
                    DeclareMemberField(MemberAttributes.Private, _ilboCls, "bCE", typeof(bool), true, null, SnippetExpression("false"));
                }


                if (_ilbo.HasLegacyState && _ilbo.HasStateCtrl)
                {
                    DeclareMemberField(MemberAttributes.Private, _ilboCls, "ctrlState", "ControlState", true);
                }


                if (_ilbo.HaseZeeView && !_ilbo.Type.Equals("9"))
                {
                    ExtControlList.Add("ezvw_spname_glv_publish");
                    ExtControlList.Add("ezvw_spparam_glv_publish");
                }

                if (!_ilbo.HasContextDataItem)
                {
                    ExtControlList.Add("rvwrt_cctxt_component");
                    ExtControlList.Add("rvwrt_cctxt_ou");
                    ExtControlList.Add("rvwrt_lctxt_ou");
                }

                if (_ilbo.HasControlExtensions && !_ilbo.Type.Equals("9"))
                {
                    ExtControlList.Add("plfrt_glv_ctrlpublish");
                }

                if (_ilbo.HasDynamicLink && !_ilbo.Type.Equals("9"))
                {
                    ExtControlList.Add("itk_select_link");
                }

                foreach (String ExtControl in ExtControlList)
                {
                    String sControlType = "Edit";
                    if (ExtControl == "itk_select_link")
                        sControlType = "Combo";

                    DeclareMemberField(MemberAttributes.Private, _ilboCls, String.Format("m_con{0}", ExtControl), sControlType, true);
                }

                #endregion
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("AddMemberFields_Ext->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }

        /// <summary>
        /// Creates Constructor Method
        /// </summary>
        private CodeMemberMethod CreateConstructor()
        {
            CodeConstructor constructorMethod;
            StringBuilder sbQuery = new StringBuilder();
            IEnumerable<Page> TabsOtherThanMainpage = _ilbo.TabPages.Where(p => p.Name != "mainpage");
            try
            {
                String sContext = "Constructor";
                //Creating Method
                constructorMethod = new CodeConstructor
                {
                    Name = _ilbo.Code,
                    Attributes = MemberAttributes.Public
                };

                // Method Summary
                AddMethodSummary(constructorMethod, "Calls the AddViewInfo and InitializeControls");

                // Initializating Runtime State
                if (_ilbo.HasRTState && _ilbo.HasStateCtrl)
                    constructorMethod.Statements.Add(AssignVariable("base.AppStateControl", "hdnhdnrt_stcontrol"));

                // Preprocess Initization
                if (_ilbo.Type != "9" && (_ilbo.Publication.Count > 0 || _ilbo.HasControlExtensions))
                    constructorMethod.Statements.Add(AssignVariable("IsPreProcess1", true));
                if (_ilbo.TaskServiceList.Any(t => t.Type == "init" || t.Type == "initialize"))
                    constructorMethod.Statements.Add(AssignVariable("IsPreProcess2", true));
                if (_ilbo.TaskServiceList.Any(t => t.Type == "fetch"))
                    constructorMethod.Statements.Add(AssignVariable("IsPreProcess3", true));

                #region try block
                CodeTryCatchFinallyStatement tryBlock = AddTryBlock(constructorMethod);
                tryBlock.AddStatement(AddTraceInfo(String.Format("{0}()", constructorMethod.Name)));

                // Base Callout Start
                Generate_CallOut_MethodStart(tryBlock, sContext);

                // Setting Context
                tryBlock.AddStatement(SetContextValue("ICT_COMPONENTNAME", _ecrOptions.Component));
                tryBlock.AddStatement(SetContextValue("ICT_COMPONENTDESCRIPTION", _ecrOptions.ComponentDesc));
                tryBlock.AddStatement(SetContextValue("ICT_ACTIVITYNAME", _activity.Name));
                tryBlock.AddStatement(SetContextValue("ICT_ACTIVITYDESCRIPTION", _activity.Desc));
                tryBlock.AddStatement(SetContextValue("ICT_ILBOCODE", _ilbo.Code));
                tryBlock.AddStatement(SetContextValue("ICT_ILBODESCRIPTION", _ilbo.Desc));

                if (TabsOtherThanMainpage.Any())
                {
                    tryBlock.AddStatement(SetContextValue("ICT_ACTIVE_TAB", TabsOtherThanMainpage.Where(t => t.Sequence == "1").First().Name));
                }

                //sensitive data
                if (_ilbo.HasSensitiveData)
                {
                    tryBlock.AddStatement(SetContextValue("ICT_SENSITIVE_CONTROL_AVAILABLE", "true"));
                }

                //Universal Personlization
                if (_ilbo.HasUniversalPersonalization)
                {
                    tryBlock.AddStatement(SetContextValue("ICT_UNIVERSALPERSONALIZATION_AVAILABLE", "true"));
                }

                if (!_ilbo.Type.Equals("9"))
                {
                    tryBlock.AddStatement(MethodInvocationExp(BaseReferenceExp(), "LoadILBODefinition").AddParameters(new String[] { _ecrOptions.Component, _activity.Name, _ilbo.Code }));
                    tryBlock.AddStatement(MethodInvocationExp(ThisReferenceExp(), "InitializeTabControls"));
                    tryBlock.AddStatement(MethodInvocationExp(ThisReferenceExp(), "InitializeLayoutControls"));
                }
                tryBlock.AddStatement(MethodInvocationExp(ThisReferenceExp(), "AddViewInfo"));
                tryBlock.AddStatement(MethodInvocationExp(ThisReferenceExp(), "InitializeControls"));

                // For Extended Controls
                CreateConstructor_Ext(tryBlock, constructorMethod);

                // Base Callout Constructor End
                Generate_CallOut_MethodEnd(tryBlock, sContext);
                #endregion

                CodeCatchClause catchException = tryBlock.AddCatch();
                catchException.AddStatement(MethodInvocationExp(TypeReferenceExp("Trace"), "WriteLineIf").AddParameters(new CodeExpression[] { GetProperty(GetProperty("SessionManager", "m_ILActTraceSwitch"), "TraceError"), PrimitiveExpression(constructorMethod.Name + "()"), PrimitiveExpression(_ilbo.Code) }));
                catchException.AddStatement(new CodeThrowExceptionStatement(ObjectCreateExpression(typeof(Exception), new CodeExpression[] { GetProperty("e", "Message"), VariableReferenceExp("e") })));
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("CreateConstructor->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
            return constructorMethod;
        }

        private void CreateConstructor_Ext(CodeTryCatchFinallyStatement tryBlock, CodeConstructor constructorMethod)
        {
            try
            {
                CodeMethodInvokeExpression newMethodInvocation;
                StringBuilder sbQuery = new StringBuilder();

                // Tree Initialzation
                if (_ilbo.HasTree)
                    tryBlock.AddStatement(MethodInvocationExp(TypeReferenceExp(TREECONTROL), "Set_DefaultNodeConfig_Tree"));


                // Dynamic Link
                if (_ilbo.HasDynamicLink && !_ilbo.Type.Equals("9"))
                {
                    tryBlock.AddStatement(InitializeMemberField(constructorMethod, "DynamicLink", "oDlink"));
                    newMethodInvocation = MethodInvocationExp(TypeReferenceExp("oDlink"), "InitializeLinkData");
                    AddParameters(newMethodInvocation, new String[] { _ecrOptions.Component, _activity.Name, _ilbo.Code });
                    tryBlock.AddStatement(AssignVariable("bLink", newMethodInvocation));
                }

                // Message Lookup
                if (_ilbo.HasMessageLookup && !_ilbo.Type.Equals("9"))
                {
                    tryBlock.AddStatement(InitializeMemberField(constructorMethod, "ErrorLookup", "oErrorlookup"));
                    newMethodInvocation = MethodInvocationExp(TypeReferenceExp("oErrorlookup"), "InitializeLinkData");
                    AddParameters(newMethodInvocation, new String[] { _ecrOptions.Component, _activity.Name, _ilbo.Code });
                    tryBlock.AddStatement(AssignVariable("bMsgLkup", newMethodInvocation));
                }

                // Control Extension
                if (_ilbo.HasControlExtensions && !_ilbo.Type.Equals("9"))
                {
                    tryBlock.AddStatement(InitializeMemberField(constructorMethod, "ControlExtensions", "oCE"));
                    newMethodInvocation = MethodInvocationExp(TypeReferenceExp("oCE"), "InitializeCETable");
                    AddParameters(newMethodInvocation, new Object[] { _ecrOptions.Component, _activity.Name, _ilbo.Code, SnippetExpression("String.Empty") });
                    tryBlock.AddStatement(AssignVariable("bCE", newMethodInvocation));
                }
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("CreateConstructor_Ext->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="sControlID"></param>
        /// <param name="sIdentityType"></param>
        private CodeMethodInvokeExpression SetIdentity(String sControlID, String sIdentityType)
        {
            try
            {
                CodeMethodInvokeExpression newMethodInvocation;
                newMethodInvocation = MethodInvocationExp(TypeReferenceExp(String.Format("m_con{0}", sControlID)), "SetIdentity");
                AddParameters(newMethodInvocation, new String[] { sControlID });
                AddFieldRefParameter(newMethodInvocation, "ControlType", sIdentityType);
                return newMethodInvocation;
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("SetIdentity->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="sControlID"></param>
        /// <param name="sTabName"></param>
        private CodeMethodInvokeExpression AddControl(String sControlID, String sTabName)
        {
            try
            {
                CodeMethodInvokeExpression newMethodInvocation;
                newMethodInvocation = MethodInvocationExp(TypeReferenceExp(String.Format("_{0}", sTabName)), "AddControl");
                AddParameters(newMethodInvocation, new String[] { sControlID });
                return newMethodInvocation;
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("AddControl->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }


        /// <summary>
        /// Add Trace
        /// </summary>
        /// <param name="catchBlock"></param>
        /// <param name="sFunctionName"></param>
        /// <returns></returns>
        private CodeMethodInvokeExpression AddTraceInfo(Object sMessage)
        {
            try
            {
                CodeMethodInvokeExpression newMethodInvocation;
                newMethodInvocation = MethodInvocationExp(TypeReferenceExp("Trace"), "WriteLineIf");
                AddFieldRefParameter(newMethodInvocation, "SessionManager.m_ILActTraceSwitch", "TraceInfo");

                if (sMessage is CodeSnippetExpression)
                    AddParameters(newMethodInvocation, new Object[] { (CodeSnippetExpression)sMessage, _ilbo.Code });
                else
                    AddParameters(newMethodInvocation, new Object[] { (String)sMessage, _ilbo.Code });
                return newMethodInvocation;
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("AddTraceInfo->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="sMessage"></param>
        /// <param name="sExceptionObjectName"></param>
        /// <returns></returns>
        private CodeMethodInvokeExpression AddTraceError(Object sMessage, String sExceptionObjectName = "e")
        {
            CodeMethodInvokeExpression newMethodInvocation;
            newMethodInvocation = MethodInvocationExp(TypeReferenceExp("Trace"), "WriteLineIf");
            AddFieldRefParameter(newMethodInvocation, "SessionManager.m_ILActTraceSwitch", "TraceError");
            AddFieldRefParameter(newMethodInvocation, sExceptionObjectName, "Message");
            if (sMessage is CodeSnippetExpression)
                AddParameters(newMethodInvocation, new Object[] { (CodeSnippetExpression)sMessage });
            else
                AddParameters(newMethodInvocation, new Object[] { (String)sMessage });
            return newMethodInvocation;
        }


        /// <summary>
        /// Adds invocation of FillMessageObject 
        /// </summary>
        /// <param name="catchBlock"></param>
        /// <param name="sFunctionName"></param>
        /// <returns></returns>
        private CodeMethodInvokeExpression FillMessageObject(String sFunctionName, Object sMessage)
        {
            try
            {
                CodeMethodInvokeExpression newMethodInvokation;
                newMethodInvokation = MethodInvocationExp(ThisReferenceExp(), "FillMessageObject");

                if (sMessage is CodeSnippetExpression)
                    AddParameters(newMethodInvokation, new Object[] { (CodeSnippetExpression)sMessage });
                else
                    AddParameters(newMethodInvokation, new Object[] { (String)sMessage });

                switch (sFunctionName.ToLower())
                {
                    case "constructor":
                        AddParameters(newMethodInvokation, new Object[] { "ILBO0000" });
                        break;
                    case "initializecontrols":
                        AddParameters(newMethodInvokation, new Object[] { "ILBO0001" });
                        break;
                    case "resetcontrols":
                        AddParameters(newMethodInvokation, new Object[] { "ILBO0002" });
                        break;
                    case "addviewinfo":
                        AddParameters(newMethodInvokation, new Object[] { "ILBO0003" });
                        break;
                    case "getcontrolx":
                        AddParameters(newMethodInvokation, new Object[] { "ILBO0004" });
                        break;
                    case "getcontrol":
                        AddParameters(newMethodInvokation, new Object[] { "ILBO0005" });
                        break;
                    case "getdataitem":
                        AddParameters(newMethodInvokation, new Object[] { "ILBO0006" });
                        break;
                    case "getdataiteminstances":
                        AddParameters(newMethodInvokation, new Object[] { "ILBO0007" });
                        break;
                    case "setdataitem":
                        AddParameters(newMethodInvokation, new Object[] { "ILBO0008" });
                        break;
                    case "getvariable":
                        AddParameters(newMethodInvokation, new Object[] { "ILBO0009" });
                        break;
                    case "performtask":
                        AddParameters(newMethodInvokation, new Object[] { "ILBO0010" });
                        break;
                    case "beginperformtask":
                        AddParameters(newMethodInvokation, new Object[] { "ILBO0011" });
                        break;
                    case "endperformtask":
                        AddParameters(newMethodInvokation, new Object[] { "ILBO0012" });
                        break;
                    case "preprocess1":
                        AddParameters(newMethodInvokation, new Object[] { "ILBO0013" });
                        break;
                    case "preprocess2":
                        AddParameters(newMethodInvokation, new Object[] { "ILBO0014 " });
                        break;
                    case "beginpreprocess2":
                        AddParameters(newMethodInvokation, new Object[] { "ILBO0015" });
                        break;
                    case "endpreprocess2":
                        AddParameters(newMethodInvokation, new Object[] { "ILBO0016" });
                        break;
                    case "preprocess3":
                        AddParameters(newMethodInvokation, new Object[] { "ILBO0017" });
                        break;
                    case "beginpreprocess3":
                        AddParameters(newMethodInvokation, new Object[] { "ILBO0018" });
                        break;
                    case "endpreprocess3":
                        AddParameters(newMethodInvokation, new Object[] { "ILBO0019" });
                        break;
                    case "getcontextvalue":
                        AddParameters(newMethodInvokation, new Object[] { "ILBO0020" });
                        break;
                    case "setcontextvalue":
                        AddParameters(newMethodInvokation, new Object[] { "ILBO0021" });
                        break;
                    case "gettaskdata":
                        AddParameters(newMethodInvokation, new Object[] { "ILBO0022" });
                        break;
                    case "adddirtytab":
                        AddParameters(newMethodInvokation, new Object[] { "ILBO0023" });
                        break;
                    case "obsoletegettaskdata":
                        AddParameters(newMethodInvokation, new Object[] { "ILBO0024" });
                        break;
                    case "getdisplayurl":
                        AddParameters(newMethodInvokation, new Object[] { "ILBO0025" });
                        break;
                    case "executeservice":
                        AddParameters(newMethodInvokation, new Object[] { "ILBO0026" });
                        break;
                    case "beginexecuteservice":
                        AddParameters(newMethodInvokation, new Object[] { "ILBO0027" });
                        break;
                    case "endexecuteservice":
                        AddParameters(newMethodInvokation, new Object[] { "ILBO0028" });
                        break;
                    case "initializetabcontrols":
                        AddParameters(newMethodInvokation, new Object[] { "ILBO0029" });
                        break;
                    case "initializelayoutcontrols":
                        AddParameters(newMethodInvokation, new Object[] { "ILBO0030" });
                        break;
                    case "resettabcontrols":
                        AddParameters(newMethodInvokation, new Object[] { "ILBO0031" });
                        break;
                    case "resetlayoutcontrols":
                        AddParameters(newMethodInvokation, new Object[] { "ILBO0032" });
                        break;
                    case "gettabcontrol":
                        AddParameters(newMethodInvokation, new Object[] { "ILBO0033" });
                        break;
                    case "getlayoutcontrol":
                        AddParameters(newMethodInvokation, new Object[] { "ILBO0034" });
                        break;
                    case "updatescreendata":
                        AddParameters(newMethodInvokation, new Object[] { "ILBO0035" });
                        break;
                    case "getscreendata":
                        AddParameters(newMethodInvokation, new Object[] { "ILBO0036" });
                        break;
                    case "getcontrolvalueex":
                        AddParameters(newMethodInvokation, new object[] { "ILBO0037" });
                        break;
                    case "getqliksubvalue":
                        AddParameters(newMethodInvokation, new object[] { "ILBO0038" });
                        break;
                    default:
                        break;
                }
                AddFieldRefParameter(newMethodInvokation, "e", "Message");
                return newMethodInvokation;
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("FillMessageObject->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="initializeControlsMethod"></param>
        /// <param name="controlInfo"></param>
        private void Initialize_EditCtrl(CodeTryCatchFinallyStatement tryBlock, Control control)
        {
            try
            {
                String sIsEnabled = "true";
                String sIdentityType = String.Empty;

                IEnumerable<Property> enableProperty = control.Properties.Where(cp => cp.Name == "enabled");
                if (enableProperty.Any())
                    sIsEnabled = enableProperty.First().Value;

                if (sIsEnabled.ToLower().Equals("false"))
                    sIdentityType = "RSDisplayOnly";
                else
                    sIdentityType = "RSEdit";

                AddExpressionToTryBlock(tryBlock, SetIdentity(control.Id, sIdentityType));
                if (!_ilbo.Type.Equals("9"))
                    AddExpressionToTryBlock(tryBlock, AddControl(control.Id, control.PageName));
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("Initialize_EditCtrl->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="initializeControlsMethod"></param>
        /// <param name="controlInfo"></param>
        private void Initialize_ComboCtrl(CodeTryCatchFinallyStatement tryBlock, Control control)
        {
            try
            {
                String sIdentityType = "RSCombo";
                CodeMethodInvokeExpression newMethodInvocation;
                bool bSetControlValue = false;

                AddExpressionToTryBlock(tryBlock, SetIdentity(control.Id, sIdentityType));
                if (!_ilbo.Type.Equals("9"))
                    AddExpressionToTryBlock(tryBlock, AddControl(control.Id, control.PageName));

                foreach (Enumeration enumeration in control.Enumerations.Where(e => e.LangId == 1))
                {
                    if (!enumeration.OptionCode.ToUpper().Equals("NOTENUMERATED"))
                    {
                        newMethodInvocation = MethodInvocationExp(TypeReferenceExp(String.Format("m_con{0}", control.Id)), "AddListItem");
                        AddParameters(newMethodInvocation, new Object[] { control.Id, enumeration.SequenceNo, control.DisplayFlag.Equals("t") ? enumeration.OptionDesc : enumeration.OptionCode });
                        AddExpressionToTryBlock(tryBlock, newMethodInvocation);
                    }
                }


                //looping through enumerated control info
                foreach (View view in control.Views)
                {
                    foreach (Enumeration enumeration in view.Enumerations.Where(e => e.LangId == 1))
                    {
                        if (!enumeration.OptionCode.ToUpper().Equals("NOTENUMERATED"))
                        {
                            string sDefaultValue = string.Empty;
                            newMethodInvocation = MethodInvocationExp(TypeReferenceExp(String.Format("m_con{0}", control.Id)), "AddListItem");
                            AddParameters(newMethodInvocation, new Object[] { view.Name, enumeration.SequenceNo, view.DisplayFlag.Equals("t") ? enumeration.OptionDesc : enumeration.OptionCode });
                            AddExpressionToTryBlock(tryBlock, newMethodInvocation);

                            //set default value for combobox
                            IEnumerable<Property> defaultProperty = control.Properties.Where(p => p.Name == "default value");
                            if (defaultProperty.Any())
                                sDefaultValue = defaultProperty.First().Value;

                            if (!String.IsNullOrEmpty(sDefaultValue) && !bSetControlValue)
                            {
                                newMethodInvocation = MethodInvocationExp(TypeReferenceExp(String.Format("m_con{0}", control.Id)), "SetControlValue");
                                AddParameters(newMethodInvocation, new Object[] { view.Name, sDefaultValue, Convert.ToInt64("0") });
                                AddExpressionToTryBlock(tryBlock, newMethodInvocation);
                                bSetControlValue = true;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("Initialize_ComboCtrl->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="initializeControlsMethod"></param>
        /// <param name="controlInfo"></param>
        private void Initialize_TextArea(CodeTryCatchFinallyStatement tryBlock, Control control)
        {
            try
            {
                String sIdentityType = "RSTextArea";

                AddExpressionToTryBlock(tryBlock, SetIdentity(control.Id, sIdentityType));
                if (!_ilbo.Type.Equals("9"))
                    AddExpressionToTryBlock(tryBlock, AddControl(control.Id, control.PageName));
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("Initialize_TextArea->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="initializeControlsMethod"></param>
        /// <param name="controlInfo"></param>
        private void Initialize_GridCtrl(CodeTryCatchFinallyStatement tryBlock, Control control)
        {
            String sIdentityType = "RSGrid";
            StringBuilder sbQuery = new StringBuilder();
            CodeMethodInvokeExpression newMethodInvocation;
            try
            {
                AddExpressionToTryBlock(tryBlock, SetIdentity(control.Id, sIdentityType));
                if (!_ilbo.Type.Equals("9"))
                    AddExpressionToTryBlock(tryBlock, AddControl(control.Id, control.PageName));

                if (control.Zoom != null)
                {
                    newMethodInvocation = MethodInvocationExp(TypeReferenceExp(String.Format("m_con{0}", control.Id)), "SetZoomTask");
                    AddParameters(newMethodInvocation, new String[] { control.Zoom.TaskName });
                    AddExpressionToTryBlock(tryBlock, newMethodInvocation);
                }

                //loop through enumerated control
                foreach (View view in control.Views)
                {
                    //foreach (Enumeration enumeration in view.Enumerations)
                    //{
                    string sColumnType = string.Empty;
                    string sDefaultValue = string.Empty;

                    IEnumerable<Property> columnTypeProperty = view.Properties.Where(p => p.Name == "columntype");
                    if (columnTypeProperty.Any())
                        sColumnType = columnTypeProperty.First().Value.ToLower();

                    IEnumerable<Property> defaultProperty = view.Properties.Where(p => p.Name == "default value");
                    if (defaultProperty.Any())
                        sDefaultValue = defaultProperty.First().Value;

                    if (view.Enumerations.Any())
                    {
                        string sLinkedView = string.Empty;
                        IEnumerable<Property> linkedCheckView = null;
                        IEnumerable<View> linkedComboView = null;
                        IEnumerable<Property> defaultValueProperty = null;

                        foreach (Enumeration enumeration in view.Enumerations)
                        {
                            if (!enumeration.OptionCode.Equals("NOTENUMERATED"))
                            {
                                if (sColumnType.Equals("combobox") || sColumnType.Equals("checkbox"))
                                {
                                    newMethodInvocation = MethodInvocationExp(TypeReferenceExp(String.Format("m_con{0}", control.Id)), "AddListItem");
                                    AddParameters(newMethodInvocation, new Object[] { view.Name, enumeration.SequenceNo, view.DisplayFlag.Equals("t") ? enumeration.OptionDesc : enumeration.OptionCode });
                                    AddExpressionToTryBlock(tryBlock, newMethodInvocation);
                                }
                            }
                        }


                        if (view.DisplayFlag.Equals("t"))
                        {
                            if (sColumnType.Equals("checkbox"))
                            {
                                linkedCheckView = view.Properties.Where(p => p.Name == "linkedcheckview");
                                if (linkedCheckView.Any())
                                    sLinkedView = linkedCheckView.First().Value;
                            }
                        }


                        if (sColumnType.Equals("combobox") && view.DisplayFlag.Equals("t"))
                        {
                            if (view.DataType == "enumerated")
                            {
                                linkedComboView = (from v in control.Views
                                                   from p in v.Properties
                                                   where p.Name == "linkedcomboview"
                                                   && p.Value == view.Name
                                                   select v);
                                if (linkedComboView.Any())
                                    sLinkedView = linkedComboView.First().Name;
                            }

                            defaultValueProperty = view.Properties.Where(p => p.Name == "default value");
                            if (defaultValueProperty.Any())
                                sDefaultValue = defaultValueProperty.First().Value;

                            tryBlock.AddStatement(Generate_SetContextValue(TypeReferenceExp(String.Format("m_con{0}", control.Id)), "SetContextValue", "CCT_DEFAULT_VALUE_" + (!String.IsNullOrEmpty(sLinkedView) ? sLinkedView : view.Name), sDefaultValue));
                        }
                        else if (sColumnType.Equals("checkbox"))
                        {
                            string sONState = string.Empty;
                            string sOFFState = string.Empty;

                            IEnumerable<Property> ONStateProperty = view.Properties.Where(p => p.Name == "on state value");
                            IEnumerable<Property> OFFStateProperty = view.Properties.Where(p => p.Name == "off state value");


                            sONState = ONStateProperty.Any() ? ONStateProperty.First().Value : "1";
                            sOFFState = OFFStateProperty.Any() ? OFFStateProperty.First().Value : "0";
                            sDefaultValue = String.IsNullOrEmpty(sDefaultValue) ? "0" : sDefaultValue;

                            tryBlock.AddStatement(Generate_SetContextValue(TypeReferenceExp(String.Format("m_con{0}", control.Id)), "SetContextValue", String.Format("{0}_{1}", "CCT_ON_STATE_VALUE", view.Name), sONState));
                            tryBlock.AddStatement(Generate_SetContextValue(TypeReferenceExp(String.Format("m_con{0}", control.Id)), "SetContextValue", String.Format("{0}_{1}", "CCT_OFF_STATE_VALUE", view.Name), sOFFState));
                            tryBlock.AddStatement(Generate_SetContextValue(TypeReferenceExp(String.Format("m_con{0}", control.Id)), "SetControlValue", String.Format("{0}_{1}", "CCT_DEFAULT_VALUE", (!String.IsNullOrEmpty(sLinkedView) ? sLinkedView : view.Name)), sDefaultValue));
                        }
                    }

                    //For Non Enumerated Columns
                    else
                    {
                        if (sColumnType.Equals("checkbox"))
                        {
                            tryBlock.AddStatement(Generate_SetContextValue(TypeReferenceExp(String.Format("m_con{0}", control.Id)), "SetContextValue", String.Format("{0}_{1}", "CCT_ON_STATE_VALUE", view.Name), "1"));
                            tryBlock.AddStatement(Generate_SetContextValue(TypeReferenceExp(String.Format("m_con{0}", control.Id)), "SetContextValue", String.Format("{0}_{1}", "CCT_OFF_STATE_VALUE", view.Name), "0"));
                        }
                    }
                    //}
                }

            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("Initialize_GridCtrl->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="initializeControlsMethod"></param>
        /// <param name="controlInfo"></param>
        private void Initialize_GroupCtrl(CodeTryCatchFinallyStatement tryBlock, Control control)
        {
            String sIdentityType = "RSGroup";
            CodeMethodInvokeExpression newMethodInvocation;

            tryBlock.AddStatement(SetIdentity(control.Id, sIdentityType));
            if (!_ilbo.Type.Equals("9"))
                tryBlock.AddStatement(AddControl(control.Id, control.PageName));

            foreach (View view in control.Views)
            {
                string sDefaultValue = string.Empty;
                foreach (Enumeration enumeration in view.Enumerations.Where(e => e.LangId == 1))
                {
                    if (!enumeration.OptionCode.ToUpper().Equals("NOTENUMERATED"))
                    {
                        newMethodInvocation = MethodInvocationExp(TypeReferenceExp(String.Format("m_con{0}", control.Id)), "AddListItem");
                        AddParameters(newMethodInvocation, new Object[] { view.Name, enumeration.SequenceNo, enumeration.OptionCode });
                        tryBlock.AddStatement(newMethodInvocation);

                        newMethodInvocation = MethodInvocationExp(TypeReferenceExp(String.Format("m_con{0}", control.Id)), "AddListItem");
                        AddParameters(newMethodInvocation, new Object[] { control.Id, enumeration.SequenceNo, enumeration.OptionDesc });
                        tryBlock.AddStatement(newMethodInvocation);
                    }
                }

                IEnumerable<Property> defaultProperty = control.Properties.Where(p => p.Name == "default value");
                if (defaultProperty.Any())
                    sDefaultValue = defaultProperty.First().Value;


                //adding default value
                if (!String.IsNullOrEmpty(sDefaultValue))
                {
                    newMethodInvocation = MethodInvocationExp(TypeReferenceExp(String.Format("m_con{0}", control.Id)), "SetControlValue");
                    AddParameters(newMethodInvocation, new Object[] { view.Name, sDefaultValue, Convert.ToInt64("0") });
                    tryBlock.AddStatement(newMethodInvocation);
                }


                //tryBlock.AddStatement(MethodInvocationExp(VariableReferenceExp(string.Format("m_con{0}", control.Id)), "SetControlValue").AddParameters(new CodeExpression[] { PrimitiveExpression(view.Name), PrimitiveExpression("NULL"), PrimitiveExpression(0) }));
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="initializeControlsMethod"></param>
        /// <param name="controlInfo"></param>
        private void Initialize_Checkbox(CodeTryCatchFinallyStatement tryBlock, Control control)
        {
            String sIdentityType = "RSCheck";
            CodeMethodInvokeExpression newMethodInvocation;
            bool bSetContextValueFlg = false;

            AddExpressionToTryBlock(tryBlock, SetIdentity(control.Id, sIdentityType));
            if (!_ilbo.Type.Equals("9"))
                AddExpressionToTryBlock(tryBlock, AddControl(control.Id, control.PageName));

            foreach (View view in control.Views)
            {
                //loop through enum control info
                foreach (Enumeration enumeration in view.Enumerations)
                {
                    String sONState = String.Empty;
                    String sOFFState = String.Empty;
                    String sDefaultValue = String.Empty;
                    if (!enumeration.OptionCode.Equals("NOTENUMERATED"))
                    {
                        newMethodInvocation = MethodInvocationExp(TypeReferenceExp(String.Format("m_con{0}", control.Id)), "AddListItem");
                        AddParameters(newMethodInvocation, new Object[] { view.Name, enumeration.SequenceNo, view.DisplayFlag.Equals("t") ? enumeration.OptionDesc : enumeration.OptionCode });
                        AddExpressionToTryBlock(tryBlock, newMethodInvocation);

                        //setting values for onstate,offstate and defaultvalue
                        IEnumerable<Property> ONStateProperty = control.Properties.Where(p => p.Name == "on state value");
                        IEnumerable<Property> OFFStateProperty = control.Properties.Where(p => p.Name == "off state value");
                        sONState = ONStateProperty.Any() ? ONStateProperty.First().Value : "1";
                        sOFFState = OFFStateProperty.Any() ? OFFStateProperty.First().Value : "1";
                        sDefaultValue = "0";

                        if (!bSetContextValueFlg)
                        {
                            tryBlock.AddStatement(Generate_SetContextValue(TypeReferenceExp(String.Format("m_con{0}", control.Id)), "SetControlValue", "CCT_DEFAULT_VALUE", sDefaultValue));
                        }

                    }
                }
            }
            tryBlock.AddStatement(Generate_SetContextValue(TypeReferenceExp(String.Format("m_con{0}", control.Id)), "SetContextValue", "CCT_ON_STATE_VALUE", "1"));
            tryBlock.AddStatement(Generate_SetContextValue(TypeReferenceExp(String.Format("m_con{0}", control.Id)), "SetContextValue", "CCT_OFF_STATE_VALUE", "0"));
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="initializeControlsMethod"></param>
        /// <param name="controlInfo"></param>
        private void Initialize_StackLinks(CodeTryCatchFinallyStatement tryBlock, Control control)
        {
            tryBlock.AddStatement(SetIdentity(control.Id, "RSStackedLinks"));
            if (!_ilbo.Type.Equals("9"))
                tryBlock.AddStatement(AddControl(control.Id, control.PageName));
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="tryBlock"></param>
        /// <param name="controlInfo"></param>
        private void Initialize_PivotGrid(CodeTryCatchFinallyStatement tryBlock, Control control)
        {

            tryBlock.AddStatement(SetIdentity(control.Id, "RSPivotGrid"));
            if (!_ilbo.Type.Equals("9"))
                tryBlock.AddStatement(AddControl(control.Id, control.PageName));



            //for row labels
            tryBlock.AddStatement(MethodInvocationExp(FieldReferenceExp(ThisReferenceExp(), String.Format("m_con{0}", control.Id))
                                                , "AddRows").AddParameter(SnippetExpression("new string[]{\"" + String.Join("\",\"", control.Views.Where(v => v.PivotField != null && v.PivotField.RowLabel.ToLower() == "y").OrderBy(v => v.PivotField.RowLabelSequence).Select(v => v.Name)) + "\"}")));
            //for column labels
            tryBlock.AddStatement(MethodInvocationExp(FieldReferenceExp(ThisReferenceExp(), String.Format("m_con{0}", control.Id))
                                    , "AddColumns").AddParameter(SnippetExpression("new string[]{\"" + String.Join("\",\"", control.Views.Where(v => v.PivotField != null && v.PivotField.ColumnLabel.ToLower() == "y").OrderBy(v => v.PivotField.ColumnLabelSequence)
                                                                                                                                                                                                 .Select(v => v.Name)) + "\"}")));
            //for values
            foreach (View view in control.Views.Where(v => v.PivotField != null && v.PivotField.FieldValue.ToLower() == "y").OrderBy(v => v.PivotField.ValueSequence))
            {
                tryBlock.AddStatement(MethodInvocationExp(FieldReferenceExp(ThisReferenceExp(), String.Format("m_con{0}", control.Id)), "AddFacts").AddParameters(new CodeExpression[] { PrimitiveExpression(view.Name), GetProperty("VWAggregatorType", view.PivotField.ValueFunction) }));
            }


        }

        private void Initialize_Assorted(CodeTryCatchFinallyStatement tryBlock, Control control)
        {
            tryBlock.AddStatement(SetIdentity(control.Id, "RSAssorted"));
            if (!_ilbo.Type.Equals("9"))
                tryBlock.AddStatement(AddControl(control.Id, control.PageName));
        }

        private void Initialize_Slider(CodeTryCatchFinallyStatement tryBlock, Control control)
        {
            tryBlock.AddStatement(SetIdentity(control.Id, "RSSlider"));
            if (!_ilbo.Type.Equals("9"))
                tryBlock.AddStatement(AddControl(control.Id, control.PageName));
        }

        private void Initialize_ListView(CodeTryCatchFinallyStatement tryBlock, Control control)
        {
            tryBlock.AddStatement(SetIdentity(control.Id, "RSListView"));
            if (!_ilbo.Type.Equals("9"))
                tryBlock.AddStatement(AddControl(control.Id, control.PageName));
        }


        /// <summary>
        /// Creates 'InitializeControls' method
        /// </summary>
        /// <returns></returns>
        private CodeMemberMethod InitializeControls()
        {
            String sContext = "InitializeControls";

            String sp = String.Empty;
            String sQuery = String.Empty;
            String sFilterExpression = String.Empty;

            CodeMemberMethod InitializeControls = null;
            try
            {

                InitializeControls = new CodeMemberMethod
                {
                    Name = sContext,
                    Attributes = MemberAttributes.Private | MemberAttributes.New
                };

                // Method summary
                AddMethodSummary(InitializeControls, "Initializes the controls of Enumerated datatype.");

                #region try block
                CodeTryCatchFinallyStatement tryBlock = AddTryBlock(InitializeControls);
                tryBlock.AddStatement(AddTraceInfo(String.Format("{0}()", sContext)));

                // Base Callout Start
                Generate_CallOut_MethodStart(tryBlock, sContext);

                // For Each Controls
                //foreach (Page page in _ilbo.TabPages.OrderBy(t => t.Name))
                //{
                foreach (Control ctrl in _ctrlsInILBO.Where(c => c.IsLayoutControl == false))
                {
                    switch (ctrl.Type)
                    {
                        case "rseditctrl":
                            Initialize_EditCtrl(tryBlock, ctrl);
                            break;

                        case "rscomboctrl":
                            Initialize_ComboCtrl(tryBlock, ctrl);
                            break;

                        case "rslist":
                            Initialize_TextArea(tryBlock, ctrl);
                            break;

                        case "rsgrid":
                            Initialize_GridCtrl(tryBlock, ctrl);
                            break;

                        case "rsgroup":
                            Initialize_GroupCtrl(tryBlock, ctrl);
                            break;

                        case "rscheck":
                            Initialize_Checkbox(tryBlock, ctrl);
                            break;

                        case "rsstackedlinks":
                            Initialize_StackLinks(tryBlock, ctrl);
                            if (_ilbo.Type != "9")
                            {
                                _ilbo.HasStackedLinks = true;
                            }
                            break;

                        case "rspivotgrid":
                            Initialize_PivotGrid(tryBlock, ctrl);
                            break;

                        case "rsassorted":
                            Initialize_Assorted(tryBlock, ctrl);
                            break;
                        case "rstreegrid":
                            Initialize_TreeGrid(tryBlock, ctrl);
                            break;
                        case "rsslider":
                            Initialize_Slider(tryBlock, ctrl);
                            break;
                        case "rslistview":
                            Initialize_ListView(tryBlock, ctrl);
                            break;
                        default:
                            break;
                    }
                }
                //}


                // For Extended Controls
                InitializeControls_Ext(tryBlock);

                // For iEDK
                Generate_iEDKMethod(tryBlock, sContext);

                // Base Callout End
                Generate_CallOut_MethodEnd(tryBlock, sContext);
                #endregion

                AddCatchClause(tryBlock, sContext);
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("InitializeControls->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
            return InitializeControls;
        }

        private void Initialize_TreeGrid(CodeTryCatchFinallyStatement tryBlock, Control ctrl)
        {
            try
            {
                CodeMethodInvokeExpression MethodInvokation = null;

                MethodInvokation = MethodInvocationExp(VariableReferenceExp(string.Format("m_con{0}", ctrl.Id)), "SetIdentity");
                MethodInvokation.AddParameters(new CodeExpression[] { PrimitiveExpression(ctrl.Id), GetProperty("ControlType", "RSTreeGrid") });
                tryBlock.AddStatement(MethodInvokation);

                tryBlock.AddStatement(MethodInvocationExp(VariableReferenceExp(string.Format("_{0}", ctrl.PageName)), "AddControl").AddParameter(PrimitiveExpression(ctrl.Id)));
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("Initialize_TreeGrid->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }

        private void InitializeControls_Ext(CodeTryCatchFinallyStatement tryBlock)
        {
            try
            {
                List<String> ExtControlList = new List<String>();
                CodeMethodInvokeExpression methodInvokation;
                String sQuery = String.Empty;

                // For Dynamic Link
                if (_ilbo.HasDynamicLink && !_ilbo.Type.Equals("9"))
                {
                    ExtControlList.Add("itk_select_link");
                }

                // For eZee View
                if (_ilbo.HaseZeeView && !_ilbo.Type.Equals("9"))
                {
                    ExtControlList.Add("ezvw_spname_glv_publish");
                    ExtControlList.Add("ezvw_spparam_glv_publish");
                }

                // For Control Extension
                if (_ilbo.HasControlExtensions && !_ilbo.Type.Equals("9"))
                {
                    ExtControlList.Add("plfrt_glv_ctrlpublish");
                }

                foreach (String ExtControl in ExtControlList)
                {
                    String ControlType = "RSEdit";
                    methodInvokation = MethodInvocationExp(TypeReferenceExp("m_con" + ExtControl), "SetIdentity");
                    AddParameters(methodInvokation, new Object[] { ExtControl });
                    if (ExtControl == "itk_select_link")
                        ControlType = "RSCombo";

                    AddFieldRefParameter(methodInvokation, "ControlType", ControlType);
                    AddExpressionToTryBlock(tryBlock, methodInvokation);
                }

                // For PLFRich Controls
                if (_ilbo.HasRichControl)
                {
                    foreach (Control control in _richControlsInILBO)
                    {
                        methodInvokation = MethodInvocationExp(TypeReferenceExp("oPlfRichControls"), "PopulateRichControls");
                        AddParameters(methodInvokation, new Object[] { VariableReferenceExp("htContextItems"), VariableReferenceExp(string.Format("m_con{0}", control.Id)), PrimitiveExpression(control.Id), PrimitiveExpression(control.SectionType) });
                        tryBlock.AddStatement(methodInvokation);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("InitializeControls_Ext->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }

        /// <summary>
        /// Creates 'AddViewInfo' method
        /// </summary>
        /// <returns></returns>
        private CodeMemberMethod AddViewInfo()
        {
            CodeMemberMethod AddViewInfo;
            String sContext = "AddViewInfo";

            try
            {
                AddViewInfo = new CodeMemberMethod
                {
                    Name = sContext,
                    Attributes = MemberAttributes.Private | MemberAttributes.New
                };

                // Method Summary
                AddMethodSummary(AddViewInfo, "adds view information to the controls.");

                #region tryBlock
                CodeTryCatchFinallyStatement tryBlock = AddTryBlock(AddViewInfo);
                tryBlock.AddStatement(AddTraceInfo(String.Format("{0}()", sContext)));

                // Base Callout Start
                Generate_CallOut_MethodStart(tryBlock, sContext);

                // Context Info
                tryBlock.AddStatement(AddISManager());
                tryBlock.AddStatement(DeclareVariableAndAssign("IContext", "ISMContext", true, MethodInvocationExp(TypeReferenceExp("ISManager"), "GetContextObject")));

                // For Each ILBO Control
                AddViewInfo_ILBOControl(tryBlock);

                //// For Pivot Control
                //#region for pivot
                //foreach (CodeStatement statement in AddViewInfo_PivotConfig())
                //{
                //    tryBlock.AddStatement(statement);
                //}
                //#endregion

                // For List Edit
                AddViewInfo_ListEdit(tryBlock);

                // For Global Variables
                //AddViewInfo_GlobalVariables(tryBlock);

                // For Extended Controls
                AddViewInfo_Ext(tryBlock);

                //Syncview
                foreach (Control ctrl in _ctrlsInILBO.Where(c => c.IsLayoutControl == false && c.SyncViews.Any()))
                {
                    foreach (SyncView syncView in ctrl.SyncViews)
                    {
                        tryBlock.AddStatement(MethodInvocationExp(VariableReferenceExp("m_con" + ctrl.Id), "SetSynchronizeView").AddParameters(new CodeExpression[] { PrimitiveExpression(syncView.FilterView), PrimitiveExpression(syncView.ListView) }));
                    }
                }

                // For iEDK
                Generate_iEDKMethod(tryBlock, AddViewInfo.Name);

                // Base Callout End
                Generate_CallOut_MethodEnd(tryBlock, sContext);
                #endregion

                AddCatchClause(tryBlock, sContext);
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("AddViewInfo->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
            return AddViewInfo;
        }

        private CodeCatchClause AddCatchClause(CodeTryCatchFinallyStatement tryBlock, String MethodName)
        {
            try
            {
                CodeCatchClause catchBlock = AddCatchBlock(tryBlock);

                switch (MethodName.ToLower())
                {
                    case "adddirtytab":
                        catchBlock.Statements.Add(FillMessageObject(MethodName, SnippetExpression("String.Format(\"" + _ilbo.Code + " : AddDirtyTab(TabName = \\\"{0}\\\")\", sTabName)")));
                        break;
                    case "getdisplayurl":
                        if (_ilbo.HasBaseCallout)
                            catchBlock.AddStatement(MethodInvocationExp(VariableReferenceExp("objActBaseEx"), "GetDisplayURL_ErrHdlr").AddParameters(new CodeExpression[] { SnippetExpression("ref sTabName"), SnippetExpression("ref  e") }));
                        catchBlock.Statements.Add(FillMessageObject(MethodName, SnippetExpression("String.Format(\"" + _ilbo.Code + " : GetDisplayURL(sTabName = \\\"{0}\\\")\", sTabName)")));
                        break;
                    case "getcontrol":
                        catchBlock.Statements.Add(FillMessageObject(MethodName, SnippetExpression("String.Format(\"" + _ilbo.Code + " : GetControl(sControlID = \\\"{0}\\\")\", sControlID)")));
                        break;
                    case "getcontrolx":
                        if (_ilbo.HasBaseCallout)
                            catchBlock.AddStatement(MethodInvocationExp(VariableReferenceExp("objActBaseEx"), "GetControl_ErrHdlr").AddParameters(new CodeExpression[] { SnippetExpression("ref htContextItems"), SnippetExpression("ref sControlID"), SnippetExpression("ref e") }));
                        catchBlock.Statements.Add(FillMessageObject(MethodName, SnippetExpression("String.Format(\"" + _ilbo.Code + " : GetControlX(sControlID = \\\"{0}\\\")\", sControlID)")));
                        break;
                    case "getdataitem":
                        if (_ilbo.HasBaseCallout)
                            catchBlock.AddStatement(MethodInvocationExp(VariableReferenceExp("objActBaseEx"), "GetDataItem_ErrHdlr").AddParameters(new CodeExpression[] { SnippetExpression("ref  sLinkID"), SnippetExpression("ref  sDataItemName"), SnippetExpression("ref  nInstance"), SnippetExpression("ref  htContextItems"), SnippetExpression("ref  e") }));
                        catchBlock.Statements.Add(FillMessageObject(MethodName, SnippetExpression("String.Format(\"" + _ilbo.Code + " : GetDataItem(sLinkID = \\\"{0}\\\", sDataItemName = \\\"{1}\\\"), nInstance = \\\"{2}\\\")\", sLinkID, sDataItemName, nInstance.ToString())")));
                        break;
                    case "getdataiteminstances":
                        if (_ilbo.HasBaseCallout)
                            catchBlock.AddStatement(MethodInvocationExp(VariableReferenceExp("objActBaseEx"), "GetDataItemInstances_ErrHdlr").AddParameters(new CodeExpression[] { SnippetExpression("ref  sLinkID"), SnippetExpression("ref  sDataItemName"), SnippetExpression("ref  htContextItems"), SnippetExpression("ref  e") }));
                        catchBlock.Statements.Add(FillMessageObject(MethodName, SnippetExpression("String.Format(\"" + _ilbo.Code + " : GetDataItemInstances(sLinkID = \\\"{0}\\\", sDataItemName = \\\"{1}\\\")\", sLinkID, sDataItemName)")));
                        break;
                    case "setdataitem":
                        if (_ilbo.HasBaseCallout)
                            catchBlock.AddStatement(MethodInvocationExp(VariableReferenceExp("objActBaseEx"), "SetDataItem_ErrHdlr").AddParameters(new CodeExpression[] { SnippetExpression("ref sLinkID"), SnippetExpression("ref  sDataItemName"), SnippetExpression("ref  nInstance"), SnippetExpression("ref sValue"), SnippetExpression("ref  htContextItems"), SnippetExpression("ref  e") }));
                        catchBlock.Statements.Add(FillMessageObject(MethodName, SnippetExpression("String.Format(\"" + _ilbo.Code + " : SetDataItem(sLinkID = \\\"{0}\\\", sDataItemName = \\\"{1}\\\", nInstance = \\\"{2}\\\", sValue = \\\"{3}\\\")\", sLinkID, sDataItemName, nInstance.ToString(), sValue.ToString())")));
                        break;
                    case "getvariable":
                        catchBlock.Statements.Add(FillMessageObject(MethodName, SnippetExpression("String.Format(\"" + _ilbo.Code + " : GetVariable(sVariable = \\\"{0}\\\")\", sVariable)")));
                        break;
                    case "performtask":
                        if (_ilbo.TaskServiceList.Where(t => t.Traversal != null && !string.IsNullOrEmpty(t.Traversal.PostTask)).Any())
                            catchBlock.AddStatement(MethodInvocationExp(ThisReferenceExp(), "SetContextValue").AddParameters(new CodeExpression[] { PrimitiveExpression("ICT_POSTLINKTASK"), GetProperty(TypeReferenceExp(typeof(string)), "Empty") }));
                        if (_ilbo.HasBaseCallout)
                            catchBlock.AddStatement(MethodInvocationExp(VariableReferenceExp("objActBaseEx"), "PerformTask_ErrHdlr").AddParameters(new CodeExpression[] { VariableReferenceExp("sControlID"), VariableReferenceExp("sEventName"), VariableReferenceExp("sEventDetails"), SnippetExpression("out sTargetURL"), SnippetExpression("ref htContextItems"), SnippetExpression("ref e") }));
                        catchBlock.Statements.Add(FillMessageObject(MethodName, SnippetExpression("String.Format(\"" + _ilbo.Code + " : PerformTask(sControlID = \\\"{0}\\\", sEventName = \\\"{1}\\\", sEventDetails = \\\"{2}\\\")\", sControlID, sEventName, sEventDetails)")));
                        break;
                    case "beginperformtask":
                        if (_ilbo.HasBaseCallout)
                            catchBlock.AddStatement(MethodInvocationExp(VariableReferenceExp("objActBaseEx"), "BeginPerformTask_ErrHdlr").AddParameters(new CodeExpression[] { VariableReferenceExp("cb"), VariableReferenceExp("reqState"), SnippetExpression("ref htContextItems"), SnippetExpression("ref e") }));
                        catchBlock.Statements.Add(FillMessageObject(MethodName, SnippetExpression("String.Format(\"" + _ilbo.Code + " : BeginPerformTask(sControlID = \\\"{0}\\\", sEventName = \\\"{1}\\\", sEventDetails = \\\"{2}\\\")\", reqState.Control, reqState.EventName, reqState.EventDetails)")));
                        break;
                    case "endperformtask":
                        if (_ilbo.TaskServiceList.Where(t => t.Traversal != null && !string.IsNullOrEmpty(t.Traversal.PostTask)).Any())
                            catchBlock.AddStatement(MethodInvocationExp(ThisReferenceExp(), "SetContextValue").AddParameters(new CodeExpression[] { PrimitiveExpression("ICT_POSTLINKTASK"), GetProperty(TypeReferenceExp(typeof(string)), "Empty") }));
                        if (_ilbo.HasBaseCallout)
                            catchBlock.AddStatement(MethodInvocationExp(VariableReferenceExp("objActBaseEx"), "EndPerformTask_ErrHdlr").AddParameters(new CodeExpression[] { VariableReferenceExp("ar"), SnippetExpression("ref htContextItems"), SnippetExpression("ref e") }));
                        catchBlock.Statements.Add(FillMessageObject(MethodName, SnippetExpression("String.Format(\"" + _ilbo.Code + " : EndPerformTask(sControlID = \\\"{0}\\\", sEventName = \\\"{1}\\\", sEventDetails = \\\"{2}\\\")\", reqState.Control, reqState.EventName, reqState.EventDetails)")));
                        break;
                    case "updatescreendata":
                        catchBlock.Statements.Add(FillMessageObject(MethodName, SnippetExpression("String.Format(\"" + _ilbo.Code + " : UpdateScreenData(sTabName = \\\"{0}\\\", nodeScreenInfo = \\\"{1}\\\")\", sTabName, nodeScreenInfo.OuterXml)")));
                        break;
                    case "getscreendata":
                        catchBlock.Statements.Add(FillMessageObject(MethodName, SnippetExpression("String.Format(\"" + _ilbo.Code + " : GetScreenData(sTabName = \\\"{0}\\\", nodeScreenInfo = \\\"{1}\\\")\", sTabName, nodeScreenInfo.OuterXml)")));
                        break;
                    case "getcontextvalue":
                        if (_ilbo.HasBaseCallout)
                            catchBlock.Statements.Add(MethodInvocationExp(VariableReferenceExp("objActBaseEx"), "GetContextValue_ErrHdlr").AddParameters(new CodeExpression[] { SnippetExpression("ref sContextName"), SnippetExpression("ref htContextItems"), SnippetExpression("ref e") }));
                        catchBlock.Statements.Add(FillMessageObject(MethodName, SnippetExpression("String.Format(\"" + _ilbo.Code + " : GetContextValue(sContextName = \\\"{0}\\\")\", sContextName)")));
                        break;
                    case "setcontextvalue":
                        if (_ilbo.HasBaseCallout)
                            catchBlock.AddStatement(MethodInvocationExp(VariableReferenceExp("objActBaseEx"), "SetContextValue_ErrHdlr").AddParameters(new CodeExpression[] { SnippetExpression("ref sContextName"), SnippetExpression("ref sContextValue"), SnippetExpression("ref htContextItems"), SnippetExpression("ref e") }));
                        catchBlock.Statements.Add(FillMessageObject(MethodName, SnippetExpression("String.Format(\"" + _ilbo.Code + " : SetContextValue(sContextName = \\\"{0}\\\", sContextValue = \\\"{1}\\\")\", sContextName, sContextValue)")));
                        break;
                    case "gettaskdata":
                        if (_ilbo.HasBaseCallout)
                            catchBlock.Statements.Add(MethodInvocationExp(VariableReferenceExp("objActBaseEx"), "GetTaskData_ErrHdlr").AddParameters(new CodeExpression[] { SnippetExpression("ref sTabName"), SnippetExpression("ref  sTaskName"), SnippetExpression("ref   nodeScreenInfo"), SnippetExpression("ref  e") }));
                        catchBlock.Statements.Add(FillMessageObject(MethodName, SnippetExpression("String.Format(\"" + _ilbo.Code + " : GetTaskData(sTabName = \\\"{0}\\\", sTaskName = \\\"{1}\\\", nodeScreenInfo = \\\"{2}\\\")\", sTabName, sTaskName, nodeScreenInfo.OuterXml)")));
                        break;
                    case "obsoletegettaskdata":
                        catchBlock.Statements.Add(FillMessageObject(MethodName, SnippetExpression("String.Format(\"" + _ilbo.Code + " : ObsoleteGetTaskData(sTabName = \\\"{0}\\\", sTaskName = \\\"{1}\\\", nodeScreenInfo = \\\"{2}\\\")\", sTabName, sTaskName, nodeScreenInfo.OuterXml)")));
                        break;
                    case "executeservice":
                        if (_ilbo.HasBaseCallout)
                            catchBlock.Statements.Add(MethodInvocationExp(VariableReferenceExp("objActBaseEx"), "ExecuteService_ErrHdlr").AddParameters(new CodeExpression[] { SnippetExpression("ref sServiceName"), SnippetExpression("ref  e") }));
                        catchBlock.Statements.Add(FillMessageObject(MethodName, SnippetExpression("String.Format(\"" + _ilbo.Code + " : ExecuteService(sServiceName = \\\"{0}\\\")\", sServiceName)")));
                        break;
                    case "beginexecuteservice":
                        if (_ilbo.HasBaseCallout)
                            catchBlock.Statements.Add(MethodInvocationExp(VariableReferenceExp("objActBaseEx"), "BeginExecuteService_ErrHdlr").AddParameters(new CodeExpression[] { VariableReferenceExp("cb"), VariableReferenceExp("reqState"), SnippetExpression("ref e") }));
                        catchBlock.Statements.Add(FillMessageObject(MethodName, SnippetExpression("String.Format(\"" + _ilbo.Code + " : BeginExecuteService(sServiceName = \\\"{0}\\\")\", reqState.ServiceName)")));
                        break;
                    case "endexecuteservice":
                        catchBlock.Statements.Add(FillMessageObject(MethodName, SnippetExpression("String.Format(\"" + _ilbo.Code + " : EndExecuteService(sServiceName = \\\"{0}\\\")\", reqState.ServiceName)")));
                        break;
                    case "gettabcontrol":
                        catchBlock.Statements.Add(FillMessageObject(MethodName, SnippetExpression("String.Format(\"" + _ilbo.Code + " : GetTabControl(sTabName= \\\"{0}\\\")\", sTabName)")));
                        break;
                    case "getlayoutcontrol":
                        catchBlock.Statements.Add(FillMessageObject(MethodName, SnippetExpression("String.Format(\"" + _ilbo.Code + " : GetLayoutControl(sLayoutControlName= \\\"{0}\\\")\", sLayoutControlName)")));
                        break;
                    case "preprocess2":
                    case "beginpreprocess2":
                    case "endpreprocess2":
                        if (_ilbo.HasBaseCallout)
                            catchBlock.Statements.Add(MethodInvocationExp(VariableReferenceExp("objActBaseEx"), "PreProcess2_ErrHdlr").AddParameters(new CodeExpression[] { SnippetExpression("ref htContextItems"), SnippetExpression("ref e") }));
                        catchBlock.Statements.Add(FillMessageObject(MethodName, String.Format("{0} : {1}()", _ilbo.Code, MethodName)));
                        break;
                    case "preprocess3":
                    case "beginpreprocess3":
                    case "endpreprocess3":
                        if (_ilbo.HasBaseCallout)
                            catchBlock.Statements.Add(MethodInvocationExp(VariableReferenceExp("objActBaseEx"), "PreProcess3_ErrHdlr").AddParameters(new CodeExpression[] { SnippetExpression("ref htContextItems"), SnippetExpression("ref e") }));
                        catchBlock.Statements.Add(FillMessageObject(MethodName, String.Format("{0} : {1}()", _ilbo.Code, MethodName)));
                        break;
                    case "initializecontrols":
                        if (_ilbo.HasBaseCallout)
                            catchBlock.Statements.Add(MethodInvocationExp(VariableReferenceExp("objActBaseEx"), "InitializeControls_ErrHdlr").AddParameters(new CodeExpression[] { SnippetExpression("ref htContextItems"), SnippetExpression("ref e") }));
                        catchBlock.Statements.Add(FillMessageObject(MethodName, String.Format("{0} : {1}()", _ilbo.Code, MethodName)));
                        break;
                    case "resetcontrols":
                        if (_ilbo.HasBaseCallout)
                            catchBlock.Statements.Add(MethodInvocationExp(VariableReferenceExp("objActBaseEx"), "ResetControls_ErrHdlr").AddParameters(new CodeExpression[] { SnippetExpression("ref htContextItems"), SnippetExpression("ref e") }));
                        catchBlock.Statements.Add(FillMessageObject(MethodName, String.Format("{0} : {1}()", _ilbo.Code, MethodName)));
                        break;
                    case "preprocess1":
                        if (_ilbo.HasBaseCallout)
                            catchBlock.Statements.Add(MethodInvocationExp(VariableReferenceExp("objActBaseEx"), "PreProcess1_ErrHdlr").AddParameters(new CodeExpression[] { SnippetExpression("ref htContextItems"), SnippetExpression("ref  e") }));
                        catchBlock.Statements.Add(FillMessageObject(MethodName, String.Format("{0} : {1}()", _ilbo.Code, MethodName)));
                        break;
                    default:
                        catchBlock.Statements.Add(FillMessageObject(MethodName, String.Format("{0} : {1}()", _ilbo.Code, MethodName)));
                        break;
                }

                ThrowException(catchBlock);
                switch (MethodName.ToLower())
                {
                    case "performtask":
                    case "endperformtask":
                        catchBlock.Statements.Add(ReturnExpression(PrimitiveExpression(false)));
                        break;
                }

                return catchBlock;
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("AddCatchClause->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }

        private void AddViewInfo_ILBOControl(CodeTryCatchFinallyStatement tryBlock)
        {
            try
            {
                #region for controls other than listedit,stackedlinks & pivot
                foreach (Control ctrl in _ctrlsInILBO.Where(c => c.IsLayoutControl == false))
                {

                    //if (ctrl.Type == "rspivotgrid" || ctrl.Type == "pivot")
                    //    return;

                    string sMLVisibleRowCount = string.Empty;
                    bool bMultiline = false;
                    IEnumerable<Property> VisibleRowProperty = ctrl.Properties.Where(p => p.Name == "visiblerows");

                    if (VisibleRowProperty.Any())
                    {
                        sMLVisibleRowCount = VisibleRowProperty.First().Value;
                        bMultiline = true;
                    }

                    if (ctrl.Type.Equals("rsgrid") && !String.IsNullOrEmpty(sMLVisibleRowCount))
                    {
                        SetPageCount(tryBlock, ctrl.Id, int.Parse(sMLVisibleRowCount));
                        SetVisibleRowCount(tryBlock, ctrl.Id, int.Parse(sMLVisibleRowCount));
                    }

                    if (!bMultiline && ctrl.Type != "rsassorted" && ctrl.Type != "rsstackedlinks" && ctrl.Type != "rsslider")
                        AddView(tryBlock, ctrl.Id, ctrl.ViewName, ctrl.DisplayFlag.Equals("t") ? true : false, ctrl.DataType, "", ctrl.DisplayFlag.Equals("t") ? ctrl.Precision : string.Empty);

                    if (ctrl.Views.Count > 0)
                    {
                        foreach (View view in ctrl.Views)
                        {
                            String sColumnType = String.Empty;
                            String sHelpview = String.Empty;
                            String sVisibleRows = String.Empty;
                            String sLinkedComboView = String.Empty;
                            String sLinkedCheckView = String.Empty;
                            String sEnabled = String.Empty;


                            IEnumerable<Property> EnabledProperty = view.Properties.Where(p => p.Name == "enabled");
                            IEnumerable<Property> ColumnTypeProperty = view.Properties.Where(p => p.Name == "columntype");
                            if (EnabledProperty.Any())
                                sEnabled = EnabledProperty.First().Value.ToLower();
                            if (ColumnTypeProperty.Any())
                                sColumnType = ColumnTypeProperty.First().Value.ToLower();

                            // For Help
                            if (view.DisplayFlag.Equals("t"))
                            {
                                IEnumerable<Property> MouseOverCtrlProperty = view.Properties.Where(p => p.Name == "enabled");
                                if (MouseOverCtrlProperty.Any())
                                    sHelpview = MouseOverCtrlProperty.First().Value;
                            }

                            // Writing AddView
                            AddView(tryBlock, ctrl.Id, view.Name, view.DisplayFlag.Equals("t") ? true : false, view.DataType, sHelpview, view.DisplayFlag.Equals("t") ? view.Precision : string.Empty);

                            // For Grid Control
                            if (ctrl.Type.Equals("rsgrid") || ctrl.Type.Equals("rspivotgrid"))
                            {
                                if (sColumnType.Equals("combobox") || sColumnType.Equals("checkbox"))
                                {
                                    IEnumerable<Property> LinkedComboView = view.Properties.Where(p => p.Name == "linkedcomboview");
                                    IEnumerable<Property> LinkedCheckView = view.Properties.Where(p => p.Name == "linkedcheckview");
                                    if (LinkedComboView.Any())
                                        sLinkedComboView = LinkedComboView.First().Value;
                                    if (LinkedCheckView.Any())
                                        sLinkedCheckView = LinkedCheckView.First().Value;

                                    if (sColumnType.Equals("combobox"))
                                    {
                                        if (view.DataType != "enumerated")
                                            SetColumnProperty(tryBlock, ctrl.Id, (!String.IsNullOrEmpty(sLinkedComboView) ? sLinkedComboView : view.Name), sColumnType, (!String.IsNullOrEmpty(sLinkedComboView) ? view.Name : String.Empty));
                                        else
                                        {
                                            var tmp = (from v in ctrl.Views
                                                       from p in v.Properties
                                                       where p.Name == "linkedcomboview"
                                                       && p.Value == view.Name
                                                       select v);
                                            if (tmp.Any())
                                            {
                                                sLinkedComboView = tmp.First().Name;
                                                SetColumnProperty(tryBlock, ctrl.Id, view.Name, sColumnType, sLinkedComboView);
                                            }
                                        }

                                    }
                                    else if (sColumnType.Equals("checkbox"))
                                    {
                                        //if (!String.IsNullOrEmpty(sLinkedCheckView))
                                        SetColumnProperty(tryBlock, ctrl.Id, (!String.IsNullOrEmpty(sLinkedCheckView) ? sLinkedComboView : view.Name), sColumnType, (!String.IsNullOrEmpty(sLinkedCheckView) ? view.Name : String.Empty));
                                    }
                                }
                                else if (sColumnType.Equals("editbox") || sColumnType.Equals("displayonly"))
                                {
                                    if (sEnabled != "" && sEnabled.ToLower() == "false")
                                        sColumnType = "displayonly";

                                    SetColumnProperty(tryBlock, ctrl.Id, view.Name, sColumnType, String.Empty);
                                }
                                else
                                    SetColumnProperty(tryBlock, ctrl.Id, view.Name, String.Empty, String.Empty);
                            }
                            // For Assorted Control
                            else if (ctrl.Type.Equals("rsassorted"))
                            {
                                SetViewAttributes(tryBlock, ctrl.Id, view.Name, GetProperty("ViewType", sColumnType), String.Empty);
                            }

                        }
                    }
                    //else
                    //{
                    //AddView(tryBlock, ctrl.Id, ctrl.Id, ctrl.DisplayFlag.Equals("t") ? true : false, ctrl.DataType, "", "");
                    //}

                    //foreach (SyncView syncView in ctrl.SyncViews)
                    //{
                    //    tryBlock.AddStatement(MethodInvocationExp(VariableReferenceExp("m_con" + ctrl.Id), "SetSynchronizeView").AddParameters(new CodeExpression[] { PrimitiveExpression(syncView.FilterView), PrimitiveExpression(syncView.ListView) }));
                    //}

                }
                #endregion
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("AddViewInfo_ILBOControl->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }

        private void AddViewInfo_ListEdit(CodeTryCatchFinallyStatement tryBlock)
        {
            try
            {
                // For List Edit Controls
                IEnumerable<IGrouping<String, Control>> listEditCtrlGrps = from c in _ilbo.Controls
                                                                           where c.Type == "rslistedit"
                                                                           group c by c.Id into g
                                                                           select g;

                foreach (IGrouping<String, Control> listEditCtrlGrp in listEditCtrlGrps)
                {
                    Control ctrl = listEditCtrlGrp.First();
                    foreach (View view in ctrl.Views)
                    {
                        IEnumerable<Control> AssociatedControl = _ctrlsInILBO.Where(c => c.Id == view.ListEditAssociatedCtrlId);
                        if (AssociatedControl.Any())
                        {
                            AddView(tryBlock, view.ListEditAssociatedCtrlId, view.Name, false, view.DataType, String.Empty, String.Empty);
                            if (string.Compare(AssociatedControl.First().Type,"rsgrid",true)==0)//to check whether it is multiline
                            {
                                SetColumnProperty(tryBlock, view.ListEditAssociatedCtrlId, view.Name, "listeditbox", String.Empty);
                            }
                    }
                    }

                }
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("AddViewInfo_ListEdit->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }

        //private void AddViewInfo_GlobalVariables(CodeTryCatchFinallyStatement tryBlock)
        //{
        //    try
        //    {
        //        // For Global Variables
        //        String spName = "de_il_GetGlobalVar_SP";
        //        GlobalVar.var parameters = dbManager.CreateParameters(2);
        //        GlobalVar.dbManager.AddParamters(parameters,0, "@ILBOCode", ilbo.Name);
        //        GlobalVar.dbManager.AddParamters(parameters,1, "@EcrNo", GlobalVar.Ecrno);
        //        DataTable dt = GlobalVar.dbManager.ExecuteDataTable(CommandType.StoredProcedure, spName);
        //        foreach (DataRow drGlobalVariable in dt.Rows)
        //        {
        //            String sControlId = Convert.ToString(drGlobalVariable["ControlID"]);
        //            String sControlType = Convert.ToString(drGlobalVariable["ControlType"]);
        //            String sDataType = Convert.ToString(drGlobalVariable["DataType"]);
        //            AddView(tryBlock, sControlId, "", false, sDataType, String.Empty, String.Empty);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        throw new Exception(String.Format("AddViewInfo_GlobalVariables->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
        //    }
        //}


        private void AddViewInfo_Ext(CodeTryCatchFinallyStatement tryBlock)
        {
            try
            {
                // For eZee View
                if (_ilbo.HaseZeeView && !_ilbo.Type.Equals("9"))
                {
                    AddView(tryBlock, "ezvw_spname_glv_publish", "ezvw_spname_glv_publish", false, "char", String.Empty, String.Empty);
                    AddView(tryBlock, "ezvw_spparam_glv_publish", "ezvw_spparam_glv_publish", false, "char", String.Empty, String.Empty);
                }

                // For Control Extension
                if (_ilbo.HasControlExtensions && !_ilbo.Type.Equals("9"))
                {
                    AddView(tryBlock, "plfrt_glv_ctrlpublish", "plfrt_glv_ctrlpublish", false, "char", String.Empty, String.Empty);
                }

                // For Context Data Items
                if (!_ilbo.HasContextDataItem)
                {
                    AddView(tryBlock, "rvwrt_lctxt_ou", "rvwrt_lctxt_ou", false, "char", String.Empty, String.Empty);
                    AddView(tryBlock, "rvwrt_cctxt_ou", "rvwrt_cctxt_ou", false, "char", String.Empty, String.Empty);
                    AddView(tryBlock, "rvwrt_cctxt_component", "rvwrt_cctxt_component", false, "char", String.Empty, String.Empty);
                }

                // For PLFRich Controls
                #region for extjs controls
                if (_ilbo.HasRichControl)
                {
                    foreach (Control richCtrl in this._richControlsInILBO)
                    {

                        tryBlock.AddStatement(CodeDomHelper.DeclareVariableAndAssign("Hashtable", String.Format("m_con{0}_vw", richCtrl.Id), true, new CodeObjectCreateExpression("Hashtable")));

                        int index = 1;
                        foreach (View view in richCtrl.Views)
                        {
                            if (richCtrl.Type.Equals("pivot"))
                                tryBlock.AddStatement(CodeDomHelper.AssignVariable(String.Format("m_con{0}_vw", richCtrl.Id), SnippetExpression(String.Format("{0}~{1}~{2}~{3}", view.Name, view.DataType, view.Length, view.Precision)), index, true));
                            else
                                tryBlock.AddStatement(CodeDomHelper.AssignVariable(ArrayIndexerExpression(String.Format("m_con{0}_vw", richCtrl.Id), PrimitiveExpression(index.ToString())), PrimitiveExpression(view.Name)));
                            index++;
                        }
                        tryBlock.AddStatement(ControlSetContextValue("vwinfo", VariableReferenceExp(String.Format("m_con{0}_vw", richCtrl.Id)), String.Format("m_con{0}", richCtrl.Id)));
                    }
                }
                #endregion
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("AddViewInfo_Ext->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="sControlId"></param>
        /// <returns></returns>
        private CodeStatementCollection AddViewInfo_PivotConfig()
        {

            CodeStatementCollection statements = new CodeStatementCollection();

            foreach (Control pivotCtrl in _ctrlsInILBO.Where(c => c.Type == "rspivotgrid"))
            {

                //for row labels
                statements.Add(MethodInvocationExp(FieldReferenceExp(ThisReferenceExp(), String.Format("m_con{0}", pivotCtrl.Id))
                                                    , "AddRows").AddParameters(new CodeExpression[] { ObjectCreateExpression(typeof(String[]),
                                                                                                                        new CodeExpression[] { SnippetExpression(String.Join("\",\"", pivotCtrl.Views.Where(v=>v.PivotField!=null && v.PivotField.RowLabel.ToLower() =="y")
                                                                                                                                                                                                .Select(v=>v.Name))) }) }));
                //for column labels
                statements.Add(MethodInvocationExp(FieldReferenceExp(ThisReferenceExp(), String.Format("m_con{0}", pivotCtrl.Id))
                                        , "AddColumns").AddParameters(new CodeExpression[] { ObjectCreateExpression(typeof(String[]),
                                                                                                                        new CodeExpression[] { SnippetExpression(String.Join("\",\"", pivotCtrl.Views.Where(v=>v.PivotField!=null && v.PivotField.ColumnLabel.ToLower()=="y")
                                                                                                                                                                                                 .Select(v => v.Name))) }) }));
                //for values
                foreach (View view in pivotCtrl.Views.Where(v => v.PivotField != null && v.PivotField.FieldValue.ToLower() == "y"))
                {
                    statements.Add(MethodInvocationExp(FieldReferenceExp(ThisReferenceExp(), String.Format("m_con{0}", pivotCtrl.Id)), "AddFacts").AddParameters(new CodeExpression[] { PrimitiveExpression(view.Name), GetProperty("VWAggregatorType", view.PivotField.ValueFunction) }));
                }
            }
            return statements;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="sControlId"></param>
        /// <param name="sViewName"></param>
        /// <param name="bVisible"></param>
        /// <param name="sDataType"></param>
        /// <param name="sHelpView"></param>
        /// <param name="precisionType"></param>
        /// <returns></returns>
        private CodeMethodInvokeExpression AddView(CodeTryCatchFinallyStatement tryBlock, String sControlId, String sViewName, bool bVisible, String sDataType, String sHelpView, String precisionType)
        {
            CodeMethodInvokeExpression methodInvocation = new CodeMethodInvokeExpression();
            methodInvocation.Method.TargetObject = TypeReferenceExp(String.Format("m_con{0}", sControlId));
            methodInvocation.Method.MethodName = "AddView";

            methodInvocation.Parameters.Add(CodeDomHelper.CreatePrimitiveExpression(!String.IsNullOrEmpty(sViewName) ? sViewName : sControlId));
            methodInvocation.Parameters.Add(CodeDomHelper.CreatePrimitiveExpression(bVisible));
            methodInvocation.Parameters.Add(CodeDomHelper.CreatePrimitiveExpression(sDataType));

            if (!String.IsNullOrEmpty(sHelpView))
                methodInvocation.Parameters.Add(CodeDomHelper.CreatePrimitiveExpression(sHelpView));
            else
                methodInvocation.Parameters.Add(GetProperty("String", "Empty"));

            if (!String.IsNullOrEmpty(precisionType))
                methodInvocation.Parameters.Add(CodeDomHelper.CreateSnippetExpression((String.Format("(string)ISMContext.GetContextValue(\"SCT_{0}_PRECISION\")", precisionType.ToUpper()))));
            else
                methodInvocation.Parameters.Add(GetProperty("String", "Empty"));

            tryBlock.AddStatement(methodInvocation);

            return methodInvocation;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="sControlId"></param>
        /// <param name="iVisibleRows"></param>
        /// <returns></returns>
        private CodeMethodInvokeExpression SetPageCount(CodeTryCatchFinallyStatement tryBlock, String sControlId, int iVisibleRows)
        {
            CodeMethodInvokeExpression methodInvocation = new CodeMethodInvokeExpression();
            methodInvocation.Method.TargetObject = TypeReferenceExp(String.Format("m_con{0}", sControlId));
            methodInvocation.Method.MethodName = "SetPageRowCount";
            methodInvocation.Parameters.Add(new CodeBinaryOperatorExpression(PrimitiveExpression(iVisibleRows), CodeBinaryOperatorType.Multiply, PrimitiveExpression(3)));
            tryBlock.AddStatement(methodInvocation);
            return methodInvocation;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="sControlId"></param>
        /// <param name="iVisibleRows"></param>
        /// <returns></returns>
        private CodeMethodInvokeExpression SetVisibleRowCount(CodeTryCatchFinallyStatement tryBlock, String sControlId, int iVisibleRows)
        {
            CodeMethodInvokeExpression methodInvocation = new CodeMethodInvokeExpression();
            methodInvocation.Method.TargetObject = TypeReferenceExp(String.Format("m_con{0}", sControlId));
            methodInvocation.Method.MethodName = "SetVisibleRowCount";
            methodInvocation.Parameters.Add(CodeDomHelper.CreatePrimitiveExpression(iVisibleRows));
            tryBlock.AddStatement(methodInvocation);
            return methodInvocation;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="sControlId"></param>
        /// <param name="sViewName"></param>
        /// <param name="sPropertyName"></param>
        /// <param name="sPropertyValue"></param>
        /// <returns></returns>
        private CodeMethodInvokeExpression SetColumnProperty(CodeTryCatchFinallyStatement tryBlock, String sControlId, String sViewName, String sPropertyName, String sPropertyValue)
        {
            CodeMethodInvokeExpression methodInvocation = new CodeMethodInvokeExpression();
            methodInvocation.Method.TargetObject = TypeReferenceExp(String.Format("m_con{0}", sControlId));
            methodInvocation.Method.MethodName = "SetColumnProperties";
            methodInvocation.Parameters.Add(CodeDomHelper.CreatePrimitiveExpression(sViewName));
            if (!String.IsNullOrEmpty(sPropertyName))
                methodInvocation.Parameters.Add(CodeDomHelper.CreatePrimitiveExpression(sPropertyName));
            else
                methodInvocation.Parameters.Add(GetProperty("String", "Empty"));

            if (!String.IsNullOrEmpty(sPropertyValue))
                methodInvocation.Parameters.Add(CodeDomHelper.CreatePrimitiveExpression(sPropertyValue));
            else
                methodInvocation.Parameters.Add(GetProperty("String", "Empty"));

            tryBlock.AddStatement(methodInvocation);
            return methodInvocation;
        }

        private CodeMethodInvokeExpression SetViewAttributes(CodeTryCatchFinallyStatement tryBlock, String sControlId, String sViewName, CodeExpression ViewType, String sPropertyValue)
        {
            CodeMethodInvokeExpression methodInvocation = new CodeMethodInvokeExpression();
            methodInvocation.Method.TargetObject = TypeReferenceExp(String.Format("m_con{0}", sControlId));
            methodInvocation.Method.MethodName = "SetViewAttributes";
            methodInvocation.Parameters.Add(CodeDomHelper.CreatePrimitiveExpression(sViewName));

            methodInvocation.Parameters.Add(ViewType);
            if (!String.IsNullOrEmpty(sPropertyValue))
                methodInvocation.Parameters.Add(CodeDomHelper.CreatePrimitiveExpression(sPropertyValue));
            else
                methodInvocation.Parameters.Add(GetProperty(typeof(String), "Empty"));
            tryBlock.AddStatement(methodInvocation);
            return methodInvocation;
        }


        ///// <summary>
        ///// 
        ///// </summary>
        ///// <param name="sActivityId"></param>
        ///// <param name="ilbo.Name"></param>
        ///// <param name="sControlId"></param>
        ///// <param name="sViewName"></param>
        ///// <returns></returns>
        //private String GetPrecision(String sActivityId, String sIlboCode, String sControlId, String sViewName)
        //{
        //    String sPrecisionType = String.Empty;
        //    StringBuilder sbQuery = new StringBuilder();
        //    int iViewName;
        //    IEnumerable<String> precisionTypes;

        //    if (int.TryParse(sViewName, out iViewName))
        //    {
        //        precisionTypes = from r in ilbo.dtPrecision.AsEnumerable()
        //                         where String.Compare(r.Field<String>("type"), "view", true) == 0
        //                         && String.Compare(r.Field<String>("controlid"), sControlId, true) == 0
        //                         && String.Compare(r.Field<String>("viewname"), sViewName, true) == 0
        //                         select r.Field<String>("precisiontype");

        //        //GlobalVar.var parameters = dbManager.CreateParameters(4);
        //        //GlobalVar.dbManager.AddParamters(parameters,0, "@ilbocode", sIlboCode);
        //        //GlobalVar.dbManager.AddParamters(parameters,1, "@controlid", sControlId);
        //        //GlobalVar.dbManager.AddParamters(parameters,2, "@viewname", sViewName);
        //        //GlobalVar.dbManager.AddParamters(parameters,3, "@activityid", sActivityId);

        //        //sbQuery.AppendLine("select isnull(bt.precisiontype,'') precisiontype From #fw_req_ilbo_view v (nolock), #fw_req_activity_ilbo a (nolock), #fw_req_bterm bt (nolock), #fw_req_bterm_synonym syn (nolock)");
        //        //sbQuery.AppendLine("Where v.ilbocode    = @ilbocode");
        //        //sbQuery.AppendLine("and	v.controlid		= @controlid");
        //        //sbQuery.AppendLine("and	v.displayflag	= 't'");
        //        //sbQuery.AppendLine("and	v.viewname		= @viewname");
        //        //sbQuery.AppendLine("and	a.activityid	= @activityid");
        //        //sbQuery.AppendLine("and	a.ilbocode		= v.ilbocode");
        //        //sbQuery.AppendLine("and	v.btsynonym		= syn.btsynonym");
        //        //sbQuery.AppendLine("and	syn.btname		= bt.btname");
        //    }
        //    else
        //    {
        //        precisionTypes = from r in ilbo.dtPrecision.AsEnumerable()
        //                         where String.Compare(r.Field<String>("type"), "control", true) == 0
        //                         && String.Compare(r.Field<String>("controlid"), sControlId, true) == 0
        //                         select r.Field<String>("precisiontype");
        //        //GlobalVar.var parameters = dbManager.CreateParameters(3);
        //        //GlobalVar.dbManager.AddParamters(parameters,0, "@ilbocode", sIlboCode);
        //        //GlobalVar.dbManager.AddParamters(parameters,1, "@controlid", sControlId);
        //        //GlobalVar.dbManager.AddParamters(parameters,2, "@activityid", sActivityId);

        //        //sbQuery.AppendLine("select isnull(bt.precisiontype,'') precisiontype From #fw_req_ilbo_view v (nolock), #fw_req_activity_ilbo a (nolock), #fw_req_bterm bt (nolock), #fw_req_bterm_synonym syn (nolock)");
        //        //sbQuery.AppendLine("Where v.ilbocode    = @ilbocode");
        //        //sbQuery.AppendLine("and	v.controlid		= @controlid");
        //        //sbQuery.AppendLine("and	v.displayflag	= 't'");
        //        //sbQuery.AppendLine("and	a.activityid	= @activityid");
        //        //sbQuery.AppendLine("and	a.ilbocode		= v.ilbocode");
        //        //sbQuery.AppendLine("and	v.btsynonym		= syn.btsynonym");
        //        //sbQuery.AppendLine("and	syn.btname		= bt.btname");
        //    }

        //    //sPrecisionType = (String)GlobalVar.dbManager.ExecuteScalar(CommandType.Text, sbQuery.ToString());
        //    //return sPrecisionType.Trim();
        //    sPrecisionType = precisionTypes.Count() > 0 ? precisionTypes.First().Trim() : String.Empty;
        //    return sPrecisionType;
        //}

        private void Generate_CallOut_MethodStart(CodeTryCatchFinallyStatement tryBlock, String sMethodName, CodeMemberMethod memberMethod = null)
        {
            if (_ilbo.Type.Equals("9") || !_ilbo.HasBaseCallout)
                return;


            CodeMethodInvokeExpression MethodInvocation;

            if (sMethodName.ToLower().Equals("getcontrolx"))
                sMethodName = "GetControl";

            MethodInvocation = MethodInvocationExp(TypeReferenceExp("objActBaseEx"), String.Format("{0}_Start", sMethodName));
            switch (sMethodName.ToLower())
            {
                case "constructor":
                    AddParameter(MethodInvocation, "ref htContextItems");
                    break;
                case "resetcontrols":
                case "initializecontrols":
                case "addviewinfo":
                    AddParameter(MethodInvocation, "ref htContextItems");
                    break;
                case "getvariable":
                    MethodInvocation = MethodInvocationExp(TypeReferenceExp("objActBaseEx"), "GetVariable");
                    MethodInvocation.AddParameters(new CodeExpression[] { SnippetExpression("ref sVariable"), SnippetExpression("ref  htContextItems") });
                    break;
                case "getdisplayurl":
                    MethodInvocation.AddParameter(SnippetExpression("ref sTabName"));
                    break;
                case "getcontrol":
                    AddParameters(MethodInvocation, new Object[] { SnippetExpression("ref htContextItems"), SnippetExpression("ref sControlID") });
                    break;
                case "getdataitem":
                    AddParameters(MethodInvocation, new Object[] { SnippetExpression("ref sLinkID"), SnippetExpression("ref sDataItemName"), SnippetExpression("ref nInstance"), SnippetExpression("ref htContextItems") });
                    break;
                case "setdataitem":
                    AddParameters(MethodInvocation, new Object[] { SnippetExpression("ref sLinkID"), SnippetExpression("ref sDataItemName"), SnippetExpression("ref nInstance"), SnippetExpression("ref sValue"), SnippetExpression("ref htContextItems") });
                    break;
                case "getdataiteminstances":
                    AddParameters(MethodInvocation, new Object[] { SnippetExpression("ref sLinkID"), SnippetExpression("ref sDataItemName"), SnippetExpression("ref htContextItems") });
                    break;
                case "performtask":
                    MethodInvocation.AddParameters(new CodeExpression[] { SnippetExpression("sControlID"), SnippetExpression("sEventName"), SnippetExpression("sEventDetails"), SnippetExpression("out sTargetURL"), SnippetExpression("ref htContextItems") });
                    break;
                case "beginperformtask":
                    MethodInvocation.AddParameters(new CodeExpression[] { SnippetExpression("cb"), SnippetExpression("reqState"), SnippetExpression("ref htContextItems") });
                    break;
                case "preprocess1":
                case "preprocess2":
                case "preprocess3":
                case "beginpreprocess2":
                case "endpreprocess2":
                case "beginpreprocess3":
                case "endpreprocess3":
                    if (sMethodName.ToLower().Equals("beginpreprocess2") || sMethodName.ToLower().Equals("endpreprocess2"))
                    {
                        MethodInvocation = MethodInvocationExp(TypeReferenceExp("objActBaseEx"), String.Format("{0}_Start", "PreProcess2"));
                    }
                    else if (sMethodName.ToLower().Equals("beginpreprocess3") || sMethodName.ToLower().Equals("endpreprocess3"))
                    {
                        MethodInvocation = MethodInvocationExp(TypeReferenceExp("objActBaseEx"), String.Format("{0}_Start", "PreProcess3"));
                    }
                    MethodInvocation.AddParameters(new CodeExpression[] { SnippetExpression("ref htContextItems") });
                    break;
                case "getscreendata":
                    MethodInvocation.AddParameters(new CodeExpression[] { SnippetExpression("ref sTabName"), SnippetExpression("ref  nodeScreenInfo"), SnippetExpression("ref  htContextItems") });
                    break;
                case "updatescreendata":
                    MethodInvocation.AddParameters(new CodeExpression[] { SnippetExpression("ref sTabName"), SnippetExpression("ref  nodeScreenInfo"), SnippetExpression("ref  htContextItems") });
                    break;
                case "beginexecuteservice":
                    MethodInvocation.AddParameters(new CodeExpression[] { VariableReferenceExp("cb"), VariableReferenceExp("reqState") });
                    break;
                case "executeservice":
                    MethodInvocation.AddParameter(SnippetExpression("ref sServiceName"));
                    break;
                case "gettaskdata":
                    MethodInvocation.AddParameters(new CodeExpression[] { SnippetExpression("ref sTabName"), SnippetExpression("ref sTaskName"), SnippetExpression("ref nodeScreenInfo") });
                    break;
                default:
                    break;
            }

            if (memberMethod != null)
                memberMethod.AddStatement(MethodInvocation);
            else
                tryBlock.AddStatement(MethodInvocation);
        }

        private void Generate_CallOut_MethodEnd(CodeTryCatchFinallyStatement tryBlock, String sMethodName)
        {
            if (_ilbo.Type.Equals("9") || _ilbo.HasBaseCallout == false)
                return;

            CodeMethodInvokeExpression MethodInvocation;
            MethodInvocation = MethodInvocationExp(TypeReferenceExp("objActBaseEx"), String.Format("{0}_End", sMethodName));
            switch (sMethodName.ToLower())
            {
                case "constructor":
                    AddParameter(MethodInvocation, "ref htContextItems");
                    break;
                case "resetcontrols":
                case "initializecontrols":
                    AddParameter(MethodInvocation, "ref htContextItems");
                    break;
                case "addviewinfo":
                    AddParameters(MethodInvocation, new CodeExpression[] { SnippetExpression("ref htContextItems"), SnippetExpression("ref ISManager"), SnippetExpression("ref ISMContext") });
                    break;
                case "setdataitem":
                    AddParameters(MethodInvocation, new Object[] { SnippetExpression("ref sLinkID"), SnippetExpression("ref sDataItemName"), SnippetExpression("ref  nInstance"), SnippetExpression("ref  sValue"), SnippetExpression("ref htContextItems") });
                    break;
                case "getdataitem":
                    AddParameters(MethodInvocation, new Object[] { SnippetExpression("ref sLinkID"), SnippetExpression("ref sDataItemName"), SnippetExpression("ref nInstance"), SnippetExpression("ref htContextItems") });
                    break;
                case "getdataiteminstances":
                    AddParameters(MethodInvocation, new Object[] { SnippetExpression("ref sLinkID"), SnippetExpression("ref sDataItemName"), SnippetExpression("ref htContextItems") });
                    break;
                case "getscreendata":
                    AddParameters(MethodInvocation, new Object[] { SnippetExpression("ref sTabName"), ArgumentReferenceExp("ref nodeScreenInfo"), SnippetExpression("ref  htContextItems") });
                    break;
                case "gettaskdata":
                    AddParameters(MethodInvocation, new Object[] { SnippetExpression("ref sTabName"), SnippetExpression("ref sTaskName"), SnippetExpression("ref nodeScreenInfo") });
                    break;
                case "beginperformtask":
                    MethodInvocation.AddParameters(new CodeExpression[] { SnippetExpression("cb"), SnippetExpression("reqState"), SnippetExpression("ref htContextItems") });
                    break;
                case "preprocess1":
                case "preprocess2":
                case "preprocess3":
                case "endpreprocess2":
                case "endpreprocess3":
                    if (sMethodName.ToLower().Equals("endpreprocess2"))
                    {
                        MethodInvocation = MethodInvocationExp(TypeReferenceExp("objActBaseEx"), String.Format("{0}_End", "PreProcess2"));
                    }
                    else if (sMethodName.ToLower().Equals("endpreprocess3"))
                    {
                        MethodInvocation = MethodInvocationExp(TypeReferenceExp("objActBaseEx"), String.Format("{0}_End", "PreProcess3"));
                    }
                    MethodInvocation.AddParameters(new CodeExpression[] { SnippetExpression("ref htContextItems") });
                    break;
                case "endexecuteservice":
                    MethodInvocation.AddParameters(new CodeExpression[] { SnippetExpression("ref ar") });
                    break;
                case "executeservice":
                    MethodInvocation.AddParameter(SnippetExpression("ref sServiceName"));
                    break;
                case "getdisplayurl":
                    MethodInvocation.AddParameter(SnippetExpression("ref sTabName"));
                    break;
                default:
                    break;
            }
            tryBlock.AddStatement(MethodInvocation);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tryBlock"></param>
        /// <param name="sMethodName"></param>
        private void Generate_iEDKMethod(CodeTryCatchFinallyStatement tryBlock, String sMethodName, CodeConditionStatement IfStatement = null)
        {
            if (_ilbo.Type.Equals("9"))
                return;

            CodeStatementCollection statements = new CodeStatementCollection();
            CodeConditionStatement ifCondition = new CodeConditionStatement();
            CodeMethodInvokeExpression newMethodInvokation = new CodeMethodInvokeExpression();
            ifCondition = IfCondition();

            // Condition Statement
            switch (sMethodName.ToLower())
            {
                case "setdataitem":
                case "setdataitem_default":
                case "getdataiteminstances":
                case "getdataiteminstances_default":
                case "getdataitem":
                case "getdataitem_default":
                    ifCondition.Condition = BinaryOpertorExpression(SnippetExpression("iEDKEIExists"),
                                                                    CodeBinaryOperatorType.BooleanAnd,
                                                                    SnippetExpression("!bFlag"));
                    break;
                case "preprocess2":
                case "endpreprocess2":
                    if (_ilbo.TaskServiceList.Where(t => t.Type == "init" || t.Type == "initialize").Any())
                    {
                        ifCondition.Condition = BinaryOpertorExpression(SnippetExpression("bReturn"),
                                                                    CodeBinaryOperatorType.BooleanAnd,
                                                                    SnippetExpression("iEDKEIExists"));
                    }
                    else
                    {
                        ifCondition.Condition = VariableReferenceExp("iEDKEIExists");
                    }
                    break;
                case "preprocess3":
                case "endpreprocess3":

                    if (_ilbo.TaskServiceList.Where(t => t.Type == "fetch").Any())
                    {
                        ifCondition.Condition = BinaryOpertorExpression(SnippetExpression("bReturn"),
                                                                    CodeBinaryOperatorType.BooleanAnd,
                                                                    SnippetExpression("iEDKEIExists"));
                    }
                    else
                    {
                        ifCondition.Condition = VariableReferenceExp("iEDKEIExists");
                    }
                    break;
                default:
                    ifCondition.Condition = VariableReferenceExp("iEDKEIExists");
                    break;
            }

            // True Statement
            newMethodInvokation = MethodInvocationExp(BaseReferenceExp(), sMethodName.Replace("_Final", "").Replace("_Default", ""));
            switch (sMethodName.ToLower())
            {
                case "performtask":
                    AddParameters(newMethodInvokation, new Object[] { SnippetExpression("sControlID"), SnippetExpression("sEventName"), SnippetExpression("sEventDetails"), SnippetExpression("out sTargetURL") });
                    ifCondition.TrueStatements.Add(ReturnExpression(newMethodInvokation));
                    break;
                case "getcontrolx":
                    AddParameters(newMethodInvokation, new Object[] { VariableReferenceExp("sControlID") });
                    ifCondition.TrueStatements.Add(ReturnExpression(newMethodInvokation));
                    break;
                case "getdataitem_default":
                case "getdataitem":
                    AddParameters(newMethodInvokation, new Object[] { VariableReferenceExp("sLinkID"), VariableReferenceExp("sDataItemName"), VariableReferenceExp("nInstance") });
                    ifCondition.TrueStatements.Add(ReturnExpression(newMethodInvokation));
                    break;
                case "setdataitem":
                    AddParameters(newMethodInvokation, new Object[] { VariableReferenceExp("sLinkID"), VariableReferenceExp("sDataItemName"), VariableReferenceExp("nInstance"), ArgumentReferenceExp("sValue") });
                    ifCondition.TrueStatements.Add(newMethodInvokation);
                    break;
                case "getdataiteminstances":
                case "getdataiteminstances_default":
                    AddParameters(newMethodInvokation, new Object[] { VariableReferenceExp("sLinkID"), VariableReferenceExp("sDataItemName") });
                    ifCondition.TrueStatements.Add(ReturnExpression(newMethodInvokation));
                    break;
                case "getvariable":
                    AddParameters(newMethodInvokation, new Object[] { ArgumentReferenceExp("sVariable") });
                    ifCondition.TrueStatements.Add(ReturnExpression(newMethodInvokation));
                    break;
                case "preprocess1":
                    ifCondition.TrueStatements.Add(ReturnExpression(newMethodInvokation));
                    break;
                case "preprocess2":
                    ifCondition.TrueStatements.Add(AssignVariable("bReturn", newMethodInvokation));
                    if (_ilbo.TaskServiceList.Where(t => t.Type == "init" || t.Type == "initialize").Any() == false)
                        ifCondition.TrueStatements.Add(ReturnExpression(VariableReferenceExp("bReturn")));
                    break;
                case "preprocess3":
                    ifCondition.TrueStatements.Add(AssignVariable("bReturn", newMethodInvokation));
                    break;
                case "beginperformtask":
                case "beginpreprocess2":
                case "beginpreprocess3":
                case "beginperformtask_final":
                    AddParameters(newMethodInvokation, new Object[] { ArgumentReferenceExp("cb"), ArgumentReferenceExp("reqState") });
                    ifCondition.TrueStatements.Add(ReturnExpression(newMethodInvokation));
                    break;
                case "endperformtask":
                    AddParameters(newMethodInvokation, new Object[] { ArgumentReferenceExp("ar") });
                    ifCondition.TrueStatements.Add(ReturnExpression(newMethodInvokation));
                    break;
                case "endpreprocess2":
                    AddParameters(newMethodInvokation, new Object[] { ArgumentReferenceExp("ar") });
                    ifCondition.TrueStatements.Add(AssignVariable("bReturn", newMethodInvokation));
                    if (_ilbo.TaskServiceList.Where(t => t.Type == "init" || t.Type == "initialize").Any() == false)
                    {
                        ifCondition.TrueStatements.Add(ReturnExpression(VariableReferenceExp("bReturn")));
                    }
                    break;
                case "endpreprocess3":
                    AddParameters(newMethodInvokation, new Object[] { ArgumentReferenceExp("ar") });
                    ifCondition.TrueStatements.Add(AssignVariable("bReturn", newMethodInvokation));
                    break;
                case "endexecuteservice":
                    AddParameters(newMethodInvokation, new Object[] { ArgumentReferenceExp("ar") });
                    ifCondition.TrueStatements.Add(ReturnExpression(newMethodInvokation));
                    break;
                case "getdisplayurl":
                    AddParameters(newMethodInvokation, new Object[] { ArgumentReferenceExp("sTabName") });
                    ifCondition.TrueStatements.Add(ReturnExpression(newMethodInvokation));
                    break;
                case "executeservice":
                    AddParameters(newMethodInvokation, new Object[] { SnippetExpression("ISExecutor"), SnippetExpression("sServiceName") });
                    ifCondition.TrueStatements.Add(AssignVariable("bExecFlag", newMethodInvokation));
                    break;
                case "executeservice_final":
                    AddParameters(newMethodInvokation, new Object[] { SnippetExpression("null"), SnippetExpression("sServiceName") });
                    ifCondition.TrueStatements.Add(ReturnExpression(newMethodInvokation));
                    break;
                case "beginexecuteservice":
                    AddParameters(newMethodInvokation, new Object[] { ArgumentReferenceExp("cb"), ArgumentReferenceExp("reqState"), VariableReferenceExp("ISExecutor") });
                    ifCondition.TrueStatements.Add(ReturnExpression(newMethodInvokation));
                    break;
                case "beginexecuteservice_final":
                    AddParameters(newMethodInvokation, new Object[] { ArgumentReferenceExp("cb"), ArgumentReferenceExp("reqState"), PrimitiveExpression(null) });
                    ifCondition.TrueStatements.Add(ReturnExpression(newMethodInvokation));
                    break;
                default:
                    ifCondition.TrueStatements.Add(newMethodInvokation);
                    break;
            }

            // False Statement            
            switch (sMethodName.ToLower())
            {
                case "getcontrolx":
                    ifCondition.FalseStatements.Add(ReturnExpression(PrimitiveExpression(null)));
                    break;
                case "getdataiteminstances":
                case "getdataitem":
                    ifCondition.FalseStatements.Add(ReturnExpression(VariableReferenceExp("sRetData")));
                    break;
                case "getvariable":
                    ifCondition.FalseStatements.Add(ReturnExpression(SnippetExpression("null")));
                    break;
                case "preprocess1":
                    ifCondition.FalseStatements.Add(ReturnExpression(PrimitiveExpression(true)));
                    break;
                case "beginperformtask":
                    newMethodInvokation = MethodInvocationExp(TypeReferenceExp("IHandler"), "BeginLaunchScreenObject");
                    AddParameters(newMethodInvokation, new Object[] { ArgumentReferenceExp("cb"), ArgumentReferenceExp("reqState") });
                    ifCondition.FalseStatements.Add(ReturnExpression(newMethodInvokation));
                    break;
                case "getdisplayurl":
                    ifCondition.FalseStatements.Add(CodeDomHelper.ThrowNewException("Invalid TabName"));
                    break;
                case "executeservice":
                    newMethodInvokation = MethodInvocationExp(TypeReferenceExp("ISExecutor"), "ExecuteService");
                    ifCondition.FalseStatements.Add(AssignVariable("bExecFlag", newMethodInvokation));
                    break;
                default:
                    break;
            }

            if (sMethodName.ToLower().Equals("getdisplayurl"))
            {
                IfStatement.FalseStatements.Add(ifCondition);
            }
            else
            {
                tryBlock.AddStatement(ifCondition);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tryBlock"></param>
        /// <param name="sMethodName"></param>
        private CodeStatementCollection Generate_iEDKMethod(String sMethodName)
        {
            if (_ilbo.Type.Equals("9"))
                return new CodeStatementCollection();

            CodeStatementCollection statements = new CodeStatementCollection();
            CodeConditionStatement ifCondition = new CodeConditionStatement();
            CodeMethodInvokeExpression newMethodInvokation = new CodeMethodInvokeExpression();

            // Condition Statement
            switch (sMethodName.ToLower())
            {
                case "setdataitem":
                case "setdataitem_default":
                case "getdataiteminstances":
                case "getdataiteminstances_default":
                case "getdataitem":
                case "getdataitem_default":
                    ifCondition.Condition = BinaryOpertorExpression(SnippetExpression("iEDKEIExists"),
                                                                    CodeBinaryOperatorType.BooleanAnd,
                                                                    SnippetExpression("!bFlag"));
                    break;
                case "preprocess2":
                case "endpreprocess2":
                    if (_ilbo.TaskServiceList.Where(t => t.Type == "init" || t.Type == "initialize").Any())
                    {
                        ifCondition.Condition = BinaryOpertorExpression(SnippetExpression("bReturn"),
                                                                    CodeBinaryOperatorType.BooleanAnd,
                                                                    SnippetExpression("iEDKEIExists"));
                    }
                    else
                    {
                        ifCondition.Condition = VariableReferenceExp("iEDKEIExists");
                    }
                    break;
                case "preprocess3":
                case "endpreprocess3":

                    if (_ilbo.TaskServiceList.Where(t => t.Type == "fetch").Any())
                    {
                        ifCondition.Condition = BinaryOpertorExpression(SnippetExpression("bReturn"),
                                                                    CodeBinaryOperatorType.BooleanAnd,
                                                                    SnippetExpression("iEDKEIExists"));
                    }
                    else
                    {
                        ifCondition.Condition = VariableReferenceExp("iEDKEIExists");
                    }
                    break;
                default:
                    ifCondition.Condition = VariableReferenceExp("iEDKEIExists");
                    break;
            }

            // True Statement
            newMethodInvokation = MethodInvocationExp(BaseReferenceExp(), sMethodName.Replace("_Final", "").Replace("_Default", ""));
            switch (sMethodName.ToLower())
            {
                case "performtask":
                    AddParameters(newMethodInvokation, new Object[] { SnippetExpression("sControlID"), SnippetExpression("sEventName"), SnippetExpression("sEventDetails"), SnippetExpression("out sTargetURL") });
                    ifCondition.TrueStatements.Add(ReturnExpression(newMethodInvokation));
                    break;
                case "getcontrolx":
                    AddParameters(newMethodInvokation, new Object[] { VariableReferenceExp("sControlID") });
                    ifCondition.TrueStatements.Add(ReturnExpression(newMethodInvokation));
                    break;
                case "getdataitem_default":
                case "getdataitem":
                    AddParameters(newMethodInvokation, new Object[] { VariableReferenceExp("sLinkID"), VariableReferenceExp("sDataItemName"), VariableReferenceExp("nInstance") });
                    ifCondition.TrueStatements.Add(ReturnExpression(newMethodInvokation));
                    break;
                case "setdataitem":
                    AddParameters(newMethodInvokation, new Object[] { VariableReferenceExp("sLinkID"), VariableReferenceExp("sDataItemName"), VariableReferenceExp("nInstance"), ArgumentReferenceExp("sValue") });
                    ifCondition.TrueStatements.Add(newMethodInvokation);
                    break;
                case "getdataiteminstances":
                case "getdataiteminstances_default":
                    AddParameters(newMethodInvokation, new Object[] { VariableReferenceExp("sLinkID"), VariableReferenceExp("sDataItemName") });
                    ifCondition.TrueStatements.Add(ReturnExpression(newMethodInvokation));
                    break;
                case "getvariable":
                    AddParameters(newMethodInvokation, new Object[] { ArgumentReferenceExp("sVariable") });
                    ifCondition.TrueStatements.Add(ReturnExpression(newMethodInvokation));
                    break;
                case "preprocess1":
                    ifCondition.TrueStatements.Add(ReturnExpression(newMethodInvokation));
                    break;
                case "preprocess2":
                    ifCondition.TrueStatements.Add(AssignVariable("bReturn", newMethodInvokation));
                    if (_ilbo.TaskServiceList.Where(t => t.Type == "init" || t.Type == "initialize").Any() == false)
                        ifCondition.TrueStatements.Add(ReturnExpression(VariableReferenceExp("bReturn")));
                    break;
                case "preprocess3":
                    ifCondition.TrueStatements.Add(AssignVariable("bReturn", newMethodInvokation));
                    break;
                case "beginperformtask":
                case "beginpreprocess2":
                case "beginpreprocess3":
                case "beginperformtask_final":
                    AddParameters(newMethodInvokation, new Object[] { ArgumentReferenceExp("cb"), ArgumentReferenceExp("reqState") });
                    ifCondition.TrueStatements.Add(ReturnExpression(newMethodInvokation));
                    break;
                case "endperformtask":
                    AddParameters(newMethodInvokation, new Object[] { ArgumentReferenceExp("ar") });
                    ifCondition.TrueStatements.Add(ReturnExpression(newMethodInvokation));
                    break;
                case "endpreprocess2":
                    AddParameters(newMethodInvokation, new Object[] { ArgumentReferenceExp("ar") });
                    ifCondition.TrueStatements.Add(AssignVariable("bReturn", newMethodInvokation));
                    if (_ilbo.TaskServiceList.Where(t => t.Type == "init" || t.Type == "initialize").Any() == false)
                    {
                        ifCondition.TrueStatements.Add(ReturnExpression(VariableReferenceExp("bReturn")));
                    }
                    break;
                case "endpreprocess3":
                    AddParameters(newMethodInvokation, new Object[] { ArgumentReferenceExp("ar") });
                    ifCondition.TrueStatements.Add(AssignVariable("bReturn", newMethodInvokation));
                    break;
                case "endexecuteservice":
                    AddParameters(newMethodInvokation, new Object[] { ArgumentReferenceExp("ar") });
                    ifCondition.TrueStatements.Add(ReturnExpression(newMethodInvokation));
                    break;
                case "getdisplayurl":
                    AddParameters(newMethodInvokation, new Object[] { ArgumentReferenceExp("sTabName") });
                    ifCondition.TrueStatements.Add(ReturnExpression(newMethodInvokation));
                    break;
                case "executeservice":
                    AddParameters(newMethodInvokation, new Object[] { SnippetExpression("ISExecutor"), SnippetExpression("sServiceName") });
                    ifCondition.TrueStatements.Add(AssignVariable("bExecFlag", newMethodInvokation));
                    break;
                case "executeservice_final":
                    AddParameters(newMethodInvokation, new Object[] { SnippetExpression("null"), SnippetExpression("sServiceName") });
                    ifCondition.TrueStatements.Add(ReturnExpression(newMethodInvokation));
                    break;
                case "beginexecuteservice":
                    AddParameters(newMethodInvokation, new Object[] { ArgumentReferenceExp("cb"), ArgumentReferenceExp("reqState"), VariableReferenceExp("ISExecutor") });
                    ifCondition.TrueStatements.Add(ReturnExpression(newMethodInvokation));
                    break;
                case "beginexecuteservice_final":
                    AddParameters(newMethodInvokation, new Object[] { ArgumentReferenceExp("cb"), ArgumentReferenceExp("reqState"), PrimitiveExpression(null) });
                    ifCondition.TrueStatements.Add(ReturnExpression(newMethodInvokation));
                    break;
                default:
                    ifCondition.TrueStatements.Add(newMethodInvokation);
                    break;
            }

            // False Statement            
            switch (sMethodName.ToLower())
            {
                case "getcontrolx":
                    ifCondition.FalseStatements.Add(ReturnExpression(PrimitiveExpression(null)));
                    break;
                case "getdataiteminstances":
                case "getdataitem":
                    ifCondition.FalseStatements.Add(ReturnExpression(VariableReferenceExp("sRetData")));
                    break;
                case "getvariable":
                    ifCondition.FalseStatements.Add(ReturnExpression(SnippetExpression("null")));
                    break;
                case "preprocess1":
                    ifCondition.FalseStatements.Add(ReturnExpression(PrimitiveExpression(true)));
                    break;
                case "beginperformtask":
                    newMethodInvokation = MethodInvocationExp(TypeReferenceExp("IHandler"), "BeginLaunchScreenObject");
                    AddParameters(newMethodInvokation, new Object[] { ArgumentReferenceExp("cb"), ArgumentReferenceExp("reqState") });
                    ifCondition.FalseStatements.Add(ReturnExpression(newMethodInvokation));
                    break;
                case "getdisplayurl":
                    ifCondition.FalseStatements.Add(CodeDomHelper.ThrowNewException("Invalid TabName"));
                    break;
                case "executeservice":
                    newMethodInvokation = MethodInvocationExp(TypeReferenceExp("ISExecutor"), "ExecuteService");
                    ifCondition.FalseStatements.Add(AssignVariable("bExecFlag", newMethodInvokation));
                    break;
                default:
                    break;
            }

            statements.Add(ifCondition);
            return statements;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private CodeMemberMethod ResetControls()
        {
            try
            {
                String sContext = "ResetControls";
                CodeMemberMethod ResetControls = new CodeMemberMethod
                {
                    Name = sContext,
                    Attributes = _ilbo.Type.Equals("9") ? (MemberAttributes.Public | MemberAttributes.Final) : (MemberAttributes.Public | MemberAttributes.Override)
                };

                // Method Summary
                AddMethodSummary(ResetControls, "Resets all the controls of the ILBO");

                CodeTryCatchFinallyStatement tryBlock = AddTryBlock(ResetControls);
                tryBlock.AddStatement(AddTraceInfo(String.Format("{0}()", sContext)));

                // Base Callout Starts
                Generate_CallOut_MethodStart(tryBlock, sContext);

                // For ILBO Control
                foreach (Control ctrl in _ctrlsInILBO.Where(c => c.IsLayoutControl == false))
                {
                    tryBlock.AddStatement(CodeDomHelper.MethodInvocationExp(TypeReferenceExp(String.Format("m_con{0}", ctrl.Id)), "ClearAll"));
                }

                // For Extended Controls
                ResetControls_Ext(tryBlock);

                // For Layout Controls
                if (!_ilbo.Type.Equals("9"))
                {
                    tryBlock.AddStatement(CodeDomHelper.MethodInvocationExp(ThisReferenceExp(), "ResetLayoutControls"));
                    tryBlock.AddStatement(CodeDomHelper.MethodInvocationExp(ThisReferenceExp(), "ResetTabControls"));
                }

                // For iEDK
                Generate_iEDKMethod(tryBlock, sContext);

                // Initialize Controls
                tryBlock.AddStatement(CodeDomHelper.MethodInvocationExp(ThisReferenceExp(), "InitializeControls"));

                // Base Callout End
                Generate_CallOut_MethodEnd(tryBlock, sContext);

                // Catch Block
                AddCatchClause(tryBlock, sContext);

                return ResetControls;
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("ResetControls->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }

        private void ResetControls_Ext(CodeTryCatchFinallyStatement tryBlock)
        {
            try
            {
                List<String> ExtControlList = new List<String>();

                // For Tree Controls
                if (_ilbo.HasTree)
                {
                    tryBlock.AddStatement(MethodInvocationExp(FieldReferenceExp(ThisReferenceExp(), "m_conTree"), "ResetTreeControls").AddParameters(new CodeExpression[] { SnippetExpression("ref htContextItems") }));
                }

                // For Chart Controls
                if (_ilbo.HasChart)
                {
                    tryBlock.AddStatement(MethodInvocationExp(FieldReferenceExp(ThisReferenceExp(), "m_conChart"), "ResetChartControls").AddParameter(SnippetExpression("ref htContextItems")));
                }

                // For Dyamic Links
                if (_ilbo.HasDynamicLink && !_ilbo.Type.Equals("9"))
                {
                    ExtControlList.Add("itk_select_link");
                }

                // For eZee View Controls
                if (_ilbo.HaseZeeView && !_ilbo.Type.Equals("9"))
                {
                    ExtControlList.Add("ezvw_spname_glv_publish");
                    ExtControlList.Add("ezvw_spparam_glv_publish");
                }

                // For Control Extension
                if (_ilbo.HasControlExtensions && !_ilbo.Type.Equals("9"))
                {
                    ExtControlList.Add("plfrt_glv_ctrlpublish");
                }

                foreach (String ExtControl in ExtControlList)
                {
                    tryBlock.AddStatement(CodeDomHelper.MethodInvocationExp(TypeReferenceExp(String.Format("m_con{0}", ExtControl)), "ClearAll"));
                }

                // For PLFRichControl
                if (_ilbo.HasRichControl)
                {
                    foreach (Control control in this._richControlsInILBO)
                    {
                        if (control.Type.Equals("rspivotgrid"))
                        {
                            tryBlock.AddStatement(MethodInvocationExp(FieldReferenceExp(ThisReferenceExp(), String.Format("m_con{0}",control.Id)), "SetContextValue").AddParameters(new Object[] { "data", GetProperty(typeof(String), "Empty") }));
                            tryBlock.AddStatement(MethodInvocationExp(FieldReferenceExp(ThisReferenceExp(), String.Format("m_con{0}", control.Id)), "SetContextValue").AddParameters(new Object[] { "orgdata", GetProperty(typeof(String), "Empty") }));
                            tryBlock.AddStatement(MethodInvocationExp(FieldReferenceExp(ThisReferenceExp(), String.Format("m_con{0}", control.Id)), "SetContextValue").AddParameters(new Object[] { "distvalue", GetProperty(typeof(String), "Empty") }));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("ResetControls_Ext->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private CodeMemberMethod GetControlX()
        {
            try
            {
                String sContext = "GetControlX";
                CodeMemberMethod GetControlX = new CodeMemberMethod
                {
                    Name = sContext,
                    Attributes = _ilbo.Type.Equals("9") ? (MemberAttributes.Public | MemberAttributes.Final) : (MemberAttributes.Public | MemberAttributes.Override),
                    ReturnType = new CodeTypeReference("IControl")
                };
                GetControlX.Parameters.Add(ParameterDeclarationExp(typeof(String), "sControlID"));

                // Method Summary
                AddMethodSummary(GetControlX, "creates/gets the Object Handle of the Control");

                // Try Block
                CodeTryCatchFinallyStatement tryBlock = AddTryBlock(GetControlX);
                tryBlock.AddStatement(AddTraceInfo(SnippetExpression("String.Format(\"" + sContext + "(sControlID = \\\"{0}\\\")\", sControlID)")));

                Generate_CallOut_MethodStart(tryBlock, sContext);

                tryBlock.AddStatement(SnippetStatement("switch(sControlID.ToLower())"));
                tryBlock.AddStatement(SnippetStatement("{"));

                foreach (Control ctrl in this._ctrlsInILBO.Where(c => c.IsLayoutControl == false))
                {
                    tryBlock.AddStatement(SnippetStatement(String.Format("case \"{0}\":", ctrl.Id)));
                    tryBlock.AddStatement(ReturnExpression(FieldReferenceExp(ThisReferenceExp(), String.Format("m_con{0}", ctrl.Id))));
                }

                // For Extended Controls
                GetControlX_Ext(tryBlock);

                // Default Case
                tryBlock.AddStatement(SnippetStatement("default:"));

                if (_ilbo.Type.Equals("9"))
                {
                    tryBlock.AddStatement(ReturnExpression(PrimitiveExpression(null)));
                }
                else
                {
                    if (!_ilbo.HasContextDataItem)
                        Generate_iEDKMethod(tryBlock, sContext);
                    else
                        tryBlock.AddStatement(ReturnExpression(MethodInvocationExp(BaseReferenceExp(), sContext).AddParameter(VariableReferenceExp("sControlID"))));
                }

                // Switch Case Close
                tryBlock.AddStatement(SnippetStatement("}"));

                // Base Callout End
                //Generate_CallOut_MethodEnd(tryBlock, sContext);

                // Catch Block
                CodeCatchClause catchBlock = AddCatchClause(tryBlock, sContext);

                return GetControlX;
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("GetControlX->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }

        private void GetControlX_Ext(CodeTryCatchFinallyStatement tryBlock)
        {
            try
            {
                List<String> ExtControlList = new List<String>();

                if (!_ilbo.HasContextDataItem)
                {
                    ExtControlList.Add("rvwrt_cctxt_component");
                    ExtControlList.Add("rvwrt_cctxt_ou");
                    ExtControlList.Add("rvwrt_lctxt_ou");
                }

                // For eZee View
                if (_ilbo.HaseZeeView && !_ilbo.Type.Equals("9"))
                {
                    ExtControlList.Add("ezvw_spname_glv_publish");
                    ExtControlList.Add("ezvw_spparam_glv_publish");
                }

                // For Control Extension
                if (_ilbo.HasControlExtensions && !_ilbo.Type.Equals("9"))
                {
                    ExtControlList.Add("plfrt_glv_ctrlpublish");
                }

                foreach (String ExtControl in ExtControlList)
                {
                    tryBlock.AddStatement(SnippetStatement(String.Format("case \"{0}\":", ExtControl)));
                    tryBlock.AddStatement(ReturnExpression(FieldReferenceExp(ThisReferenceExp(), String.Format("m_con{0}", ExtControl))));
                }
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("GetControlX_Ext->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private CodeMemberMethod GetControlValueEx()
        {
            try
            {
                CodeMemberMethod GetControlValueEx;
                GetControlValueEx = new CodeMemberMethod
                {
                    Name = "GetControlValueEx",
                    Attributes = MemberAttributes.Private,
                    ReturnType = new CodeTypeReference(typeof(string))
                };
                GetControlValueEx.Parameters.Add(new CodeParameterDeclarationExpression(typeof(String), "ControlID"));
                GetControlValueEx.Parameters.Add(new CodeParameterDeclarationExpression(typeof(String), "ViewName"));

                // Method Summary
                AddMethodSummary(GetControlValueEx, "Get the Value for ControlID and View Name");

                // Paramters
                GetControlValueEx.AddStatement(DeclareVariableAndAssign(typeof(String), "ReturnValue", true, GetProperty(typeof(String), "Empty")));

                CodeTryCatchFinallyStatement tryBlock = AddTryBlock(GetControlValueEx);
                tryBlock.AddStatement(DeclareVariableAndAssign("IControl", "oControl", true, MethodInvocationExp(ThisReferenceExp(), "GetControl").AddParameters(new Object[] { ArgumentReferenceExp("ControlID") })));

                CodeConditionStatement ControlTypeCheck = new CodeConditionStatement();
                ControlTypeCheck.Condition = SnippetExpression("oControl is IGridControl");
                ControlTypeCheck.TrueStatements.Add(DeclareVariableAndAssign(typeof(long), "nthSelectedRow", true, SnippetExpression("((IGridControl)oControl).GetNthSelectedRow(1)")));
                ControlTypeCheck.TrueStatements.Add(AssignVariable(VariableReferenceExp("ReturnValue"), MethodInvocationExp(TypeReferenceExp("oControl"), "GetControlValue").AddParameters(new CodeExpression[] { ArgumentReferenceExp("ViewName"), VariableReferenceExp("nthSelectedRow") })));
                ControlTypeCheck.FalseStatements.Add(AssignVariable(VariableReferenceExp("ReturnValue"), MethodInvocationExp(TypeReferenceExp("oControl"), "GetControlValue").AddParameters(new CodeExpression[] { ArgumentReferenceExp("ViewName") })));
                tryBlock.AddStatement(ControlTypeCheck);

                CodeCatchClause catchBlock = AddCatchBlock(tryBlock);
                catchBlock.Statements.Add(FillMessageObject(GetControlValueEx.Name, SnippetExpression("String.Format(\"" + _ilbo.Code + " : GetControlValueEx(controlID = \\\"{0}\\\")\",ControlID)")));
                ThrowException(catchBlock);

                GetControlValueEx.AddStatement(ReturnExpression(VariableReferenceExp("ReturnValue")));

                return GetControlValueEx;
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("GetControlValueEx()->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
                throw ex;
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private CodeMemberMethod GetQlikSubValue()
        {
            try
            {
                CodeMemberMethod GetQlikSubValue = new CodeMemberMethod
                {
                    Name = "GetQlikSubValue",
                    Attributes = MemberAttributes.Private,
                    ReturnType = new CodeTypeReference(typeof(string))
                };
                GetQlikSubValue.Parameters.Add(new CodeParameterDeclarationExpression(typeof(String), "ControlID"));
                GetQlikSubValue.Parameters.Add(new CodeParameterDeclarationExpression(typeof(String), "ViewName"));

                // Method Summary
                AddMethodSummary(GetQlikSubValue, "Get the Value for Qlik Link Subscription");
                GetQlikSubValue.AddStatement(DeclareVariableAndAssign(typeof(String), "ReturnValue", true, GetProperty(typeof(String), "Empty")));

                // Try Block
                CodeTryCatchFinallyStatement tryBlock = GetQlikSubValue.AddTry();
                tryBlock.AddStatement(DeclareVariableAndAssign("IControl", "oControl", true, MethodInvocationExp(ThisReferenceExp(), "GetControl").AddParameters(new Object[] { ArgumentReferenceExp("ControlID") })));
                CodeConditionStatement ControlTypeCheck = new CodeConditionStatement();
                ControlTypeCheck.Condition = SnippetExpression("oControl is IGridControl");
                CodeIterationStatement LoopThroughColumn = ForLoopExpression(DeclareVariableAndAssign(typeof(Int64), "lCount", true, PrimitiveExpression(1)),
                                                                                BinaryOpertorExpression(VariableReferenceExp("lCount"), CodeBinaryOperatorType.LessThanOrEqual, SnippetExpression("((IGridControl)oControl).GetNumSelectedRows()")),
                                                                                AssignVariable(VariableReferenceExp("lCount"), BinaryOpertorExpression(VariableReferenceExp("lCount"), CodeBinaryOperatorType.Add, PrimitiveExpression(1))));
                LoopThroughColumn.Statements.Add(DeclareVariableAndAssign(typeof(Int64), "instance", true, SnippetExpression("((IGridControl)oControl).GetNthSelectedRow(lCount)")));
                LoopThroughColumn.Statements.Add(AssignVariable(VariableReferenceExp("ReturnValue"),
                                                    BinaryOpertorExpression(VariableReferenceExp("ReturnValue"), CodeBinaryOperatorType.Add, MethodInvocationExp(TypeReferenceExp("oControl"), "GetControlValue").AddParameters(new Object[] { ArgumentReferenceExp("ViewName") }))));
                ControlTypeCheck.TrueStatements.Add(LoopThroughColumn);
                ControlTypeCheck.FalseStatements.Add(AssignVariable(VariableReferenceExp("ReturnValue"), MethodInvocationExp(TypeReferenceExp("oControl"), "GetControlValue").AddParameters(new CodeExpression[] { ArgumentReferenceExp("ViewName") })));
                tryBlock.TryStatements.Add(ControlTypeCheck);

                // Catch Block
                CodeCatchClause catchBlock = AddCatchBlock(tryBlock);
                catchBlock.Statements.Add(FillMessageObject(GetQlikSubValue.Name, SnippetExpression("String.Format(\"" + _ilbo.Code + " : GetQlikSubValue(controlID = \\\"{0}\\\")\",ControlID)")));
                ThrowException(catchBlock);

                GetQlikSubValue.AddStatement(ReturnExpression(VariableReferenceExp("ReturnValue")));

                return GetQlikSubValue;
            }
            catch (Exception ex)
            {

                throw new Exception(String.Format("GetQlikSubValue()->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
                throw ex;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private CodeMemberMethod GetControl()
        {
            try
            {
                String sContext = "GetControl";
                CodeMethodInvokeExpression methodInvocation;
                CodeMemberMethod GetControl = new CodeMemberMethod
                {
                    Name = sContext,
                    Attributes = _ilbo.Type.Equals("9") ? (MemberAttributes.Public | MemberAttributes.Final) : (MemberAttributes.Public | MemberAttributes.Override),
                    ReturnType = new CodeTypeReference("IControl")
                };
                GetControl.Parameters.Add(ParameterDeclarationExp(typeof(String), "sControlID"));

                // Method summary
                AddMethodSummary(GetControl, "Gets the Context Value");

                // Try Block
                CodeTryCatchFinallyStatement tryBlock = AddTryBlock(GetControl);
                tryBlock.AddStatement(AddTraceInfo(SnippetExpression("String.Format(\"" + sContext + "(sControlID = \\\"{0}\\\")\", sControlID)")));

                // Base Callout Start
                //Generate_CallOut_MethodStart(tryBlock, sContext);

                methodInvocation = MethodInvocationExp(ThisReferenceExp(), "GetControlX");
                methodInvocation.Parameters.Add(ArgumentReferenceExp("sControlID"));
                tryBlock.AddStatement(CodeDomHelper.DeclareVariableAndAssign("IControl", "control", true, methodInvocation));

                CodeConditionStatement compareControl = new CodeConditionStatement(new CodeBinaryOperatorExpression(VariableReferenceExp("control"), CodeBinaryOperatorType.IdentityInequality, SnippetExpression("null")));
                compareControl.TrueStatements.Add(ReturnExpression(VariableReferenceExp("control")));
                compareControl.FalseStatements.Add(CodeDomHelper.ThrowNewException("String.Format(\"Invalid ControlID - {0}\", sControlID)"));
                tryBlock.AddStatement(compareControl);

                // Base Callout End
                //Generate_CallOut_MethodEnd(tryBlock, sContext);

                // Catch Block
                AddCatchClause(tryBlock, sContext);

                return GetControl;
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("GetControl->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }


        }


        /// <summary>
        /// Generate 'GetDataItem' Method
        /// </summary>
        /// <returns></returns>
        private CodeMemberMethod GetDataItem()
        {
            try
            {
                String sContext = "GetDataItem";
                Boolean bLinkExists = false;
                CodeMethodInvokeExpression newMethodInvocation;

                CodeMemberMethod GetDataItem = new CodeMemberMethod
                {
                    Name = sContext,
                    Attributes = _ilbo.Type.Equals("9") ? (MemberAttributes.Public | MemberAttributes.Final) : (MemberAttributes.Public | MemberAttributes.Override),
                    ReturnType = new CodeTypeReference(typeof(System.String))
                };
                GetDataItem.Parameters.Add(ParameterDeclarationExp(new CodeTypeReference(typeof(System.String)), "sLinkID"));
                GetDataItem.Parameters.Add(ParameterDeclarationExp(new CodeTypeReference(typeof(System.String)), "sDataItemName"));
                GetDataItem.Parameters.Add(ParameterDeclarationExp(new CodeTypeReference(typeof(long)), "nInstance"));

                // Method Summary
                AddMethodSummary(GetDataItem, "gets the dataItem");

                // Local Variable Declaration
                GetDataItem.Statements.Add(CodeDomHelper.DeclareVariableAndAssign(typeof(String), "sRetData", true, SnippetExpression("String.Empty")));
                GetDataItem.Statements.Add(CodeDomHelper.DeclareVariableAndAssign(typeof(bool), "bFlag", true, SnippetExpression("false")));


                // Get the links,dataitem,control,view published by ilbo
                IEnumerable<IGrouping<int, PublicationInfo>> links = _ilbo.Publication.Where(p => p.Flow == FlowAttribute.OUT || p.Flow == FlowAttribute.INOUT
                                                                                        && !_dictContextDataitem.ContainsKey(p.ControlId))
                                                                              .GroupBy(p => p.LinkId);

                // Iterate Through Each Links
                if (links.Count() > 0)
                {
                    bLinkExists = true;
                    GetDataItem.Statements.Add(CodeDomHelper.DeclareVariableAndAssign(typeof(long), "lSelectedRowIndex", true, SnippetExpression("0")));
                }

                // Try Block
                CodeTryCatchFinallyStatement tryBlock = AddTryBlock(GetDataItem);
                tryBlock.AddStatement(AddTraceInfo(SnippetExpression("String.Format(\"" + sContext + "(sLinkID = \\\"{0}\\\", sDataItemName = \\\"{1}\\\", nInstance = \\\"{2}\\\")\", sLinkID, sDataItemName, nInstance.ToString())")));

                // Base Callout Start
                Generate_CallOut_MethodStart(tryBlock, sContext);

                if (bLinkExists)
                {
                    tryBlock.AddStatement(SnippetStatement("switch(sLinkID)"));
                    tryBlock.AddStatement(SnippetStatement("{"));
                }

                foreach (IGrouping<int, PublicationInfo> link in links)
                {
                    tryBlock.AddStatement(SnippetStatement(String.Format("case \"{0}\":", link.Key)));
                    var enumerator = link.GetEnumerator();
                    while (enumerator.MoveNext())
                    {
                        PublicationInfo pub = enumerator.Current;

                        CodeConditionStatement compareDataitem = new CodeConditionStatement(new CodeBinaryOperatorExpression(TypeReferenceExp("sDataItemName"), CodeBinaryOperatorType.IdentityEquality, PrimitiveExpression(pub.DataItemName)));
                        CodeMethodReturnStatement returnStatement;

                        //For Grid Control
                        if (pub.ControlType.Equals("rsgrid"))
                        {
                            newMethodInvocation = MethodInvocationExp(TypeReferenceExp(String.Format("m_con{0}", pub.ControlId)), "GetNthSelectedRow");
                            AddParameter(newMethodInvocation, "nInstance");
                            CodeAssignStatement selectedRowIndex = AssignVariable("lSelectedRowIndex", newMethodInvocation);
                            compareDataitem.TrueStatements.Add(selectedRowIndex);

                            CodeMethodInvokeExpression getControlValue = MethodInvocationExp(TypeReferenceExp(String.Format("m_con{0}", pub.ControlId)), "GetControlValue");
                            AddParameters(getControlValue, new Object[] { pub.ViewName, VariableReferenceExp("lSelectedRowIndex") });
                            returnStatement = ReturnExpression(getControlValue);
                        }
                        // Other Than Grid Controls
                        else
                        {
                            CodeMethodInvokeExpression getControlValue = MethodInvocationExp(TypeReferenceExp(String.Format("m_con{0}", pub.ControlId)), "GetControlValue");
                            AddParameters(getControlValue, new String[] { pub.ViewName });
                            returnStatement = ReturnExpression(getControlValue);
                        }

                        compareDataitem.TrueStatements.Add(returnStatement);
                        tryBlock.AddStatement(compareDataitem);
                    }

                    tryBlock.AddStatement(SnippetExpression("break"));
                }

                // Default Case
                if (bLinkExists)
                {
                    tryBlock.AddStatement(SnippetStatement("default:"));
                    tryBlock.AddStatement(SnippetExpression("break"));
                    tryBlock.AddStatement(SnippetStatement("}"));
                    // For Extended Controls
                    GetExtendedLinkMethods(tryBlock, "GetDataItem");

                    Generate_iEDKMethod(tryBlock, String.Format("{0}{1}{2}", sContext, "_", "Default"));

                    CodeConditionStatement bFlagCondition = new CodeConditionStatement(SnippetExpression("!bFlag"));
                    tryBlock.AddStatement(bFlagCondition);
                    bFlagCondition.TrueStatements.Add(AddTraceInfo("Invalid Link ID."));
                    //bFlagCondition.TrueStatements.Add(CodeDomHelper.ThrowNewException("Invalid Link ID"));

                    tryBlock.AddStatement(ReturnExpression(VariableReferenceExp("sRetData")));
                    //tryBlock.AddStatement(SnippetExpression("break"));//commented for link change - iedkpart moved out of the default case

                    // Switch Case Closing                    
                }

                // Base Callout End
                //Generate_CallOut_MethodEnd(tryBlock, sContext);

                // For iEDK
                if (!bLinkExists)
                    Generate_iEDKMethod(tryBlock, sContext);
                //else
                //tryBlock.AddStatement(ReturnExpression(SnippetExpression("String.Empty"))); //commented for link change - iedkpart moved out of the default case



                // Catch Block
                AddCatchClause(tryBlock, sContext);

                return GetDataItem;
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("GetDataItem->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }

        private void GetExtendedLinkMethods_Cmn(CodeTryCatchFinallyStatement tryBlock, String InvkCondition, String InvkObject, String InvkMethod)
        {
            try
            {


                CodeConditionStatement ifCondition = new CodeConditionStatement
                {
                    Condition = BinaryOpertorExpression(FieldReferenceExp(ThisReferenceExp(), InvkCondition),
                                                                    CodeBinaryOperatorType.BooleanAnd,
                                                                    SnippetExpression("!bFlag"))
                };


                if (InvkCondition.Equals("bCE"))
                {
                    ifCondition.Condition = FieldReferenceExp(ThisReferenceExp(), InvkCondition);
                }

                CodeMethodInvokeExpression newMethodInvokation = MethodInvocationExp(TypeReferenceExp(InvkObject), InvkMethod);
                AddParameter(newMethodInvokation, "(IILBO) this");
                AddParameter(newMethodInvokation, String.Format("\"{0}\"", _activity.Name));
                AddParameter(newMethodInvokation, String.Format("\"{0}\"", _ilbo.Code));
                AddParameter(newMethodInvokation, String.Format("{0}", "sLinkID"));
                AddParameter(newMethodInvokation, String.Format("{0}", "sDataItemName"));
                if (InvkMethod != "GetDynamicLinkDataItemIntstances" && InvkMethod != "GetErrorlookupDataItemIntstances" && InvkMethod != "GetCEDataItemIntstances")
                {
                    AddParameter(newMethodInvokation, String.Format("{0}", "nInstance"));
                }

                if (InvkMethod == "SetErrorlookupDataItem" || InvkMethod == "SetCEDataItem")
                {
                    AddParameter(newMethodInvokation, String.Format("{0}", "sValue"));
                }
                AddParameter(newMethodInvokation, String.Format("{0}", "out bFlag"));


                if (InvkMethod != "SetDynamicLinkDataItem" && InvkMethod != "SetCEDataItem" && InvkMethod != "SetErrorlookupDataItem")
                    ifCondition.TrueStatements.Add(AssignVariable(VariableReferenceExp("sRetData"), newMethodInvokation));
                else
                    ifCondition.TrueStatements.Add(newMethodInvokation);

                tryBlock.AddStatement(ifCondition);
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("GetExtendedLinkMethods_Cmn->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }

        private void GetExtendedLinkMethods(CodeTryCatchFinallyStatement tryBlock, String TriggeringMethod)
        {
            try
            {
                String CalledMethodName = String.Empty;

                // For Control Extensions
                if (_ilbo.HasControlExtensions)
                {
                    if (TriggeringMethod == "GetDataItem")
                        CalledMethodName = "GetCEDataItem";
                    else if (TriggeringMethod == "GetDataItemInstances")
                        CalledMethodName = "GetCEDataItemIntstances";
                    else if (TriggeringMethod == "SetDataItem")
                        CalledMethodName = "SetCEDataItem";

                    GetExtendedLinkMethods_Cmn(tryBlock, "bCE", "oCE", CalledMethodName);
                }

                // For Dynamic Links
                if (_ilbo.HasDynamicLink)
                {
                    if (TriggeringMethod == "GetDataItem")
                        CalledMethodName = "GetDynamicLinkDataItem";
                    else if (TriggeringMethod == "GetDataItemInstances")
                        CalledMethodName = "GetDynamicLinkDataItemIntstances";
                    else if (TriggeringMethod == "SetDataItem")
                        CalledMethodName = "SetDynamicLinkDataItem";

                    GetExtendedLinkMethods_Cmn(tryBlock, "bLink", "oDlink", CalledMethodName);
                }

                // For Message Lookup
                if (_ilbo.HasMessageLookup)
                {
                    if (TriggeringMethod == "GetDataItem")
                        CalledMethodName = "GetErrorlookupDataItem";
                    else if (TriggeringMethod == "GetDataItemInstances")
                        CalledMethodName = "GetErrorlookupDataItemIntstances";
                    else if (TriggeringMethod == "SetDataItem")
                        CalledMethodName = "SetErrorlookupDataItem";


                    GetExtendedLinkMethods_Cmn(tryBlock, "bMsgLkup", "oErrorlookup", CalledMethodName);
                }
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("GetExtendedLinkMethods->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }

        ///// <summary>
        ///// Generates 'GetDataItemInstances' Method
        ///// </summary>
        ///// <returns></returns>
        private CodeMemberMethod GetDataItemInstances()
        {
            try
            {
                String sContext = "GetDataItemInstances";
                String spName = String.Empty;
                Boolean bLinkExists = false;
                DataTable dtPublication = new DataTable();
                CodeMemberMethod GetDataItemInstances = new CodeMemberMethod
                {
                    Name = sContext,
                    Attributes = _ilbo.Type.Equals("9") ? (MemberAttributes.Public | MemberAttributes.Final) : (MemberAttributes.Public | MemberAttributes.Override),
                    ReturnType = new CodeTypeReference(typeof(long))
                };
                GetDataItemInstances.Parameters.Add(ParameterDeclarationExp(new CodeTypeReference(typeof(System.String)), "sLinkID"));
                GetDataItemInstances.Parameters.Add(ParameterDeclarationExp(new CodeTypeReference(typeof(System.String)), "sDataItemName"));

                // Method Summary
                AddMethodSummary(GetDataItemInstances, "gets the dataItem Instances.");

                // Local Variable Declaration
                GetDataItemInstances.Statements.Add(CodeDomHelper.DeclareVariableAndAssign(typeof(long), "sRetData", true, 0));
                GetDataItemInstances.Statements.Add(CodeDomHelper.DeclareVariableAndAssign(typeof(bool), "bFlag", true, false));

                // Try Block
                CodeTryCatchFinallyStatement tryBlock = AddTryBlock(GetDataItemInstances);
                tryBlock.AddStatement(AddTraceInfo(SnippetExpression("String.Format(\"" + sContext + "(sLinkID = \\\"{0}\\\", sDataItemName = \\\"{1}\\\")\", sLinkID, sDataItemName)")));

                // Base Callout Start
                Generate_CallOut_MethodStart(tryBlock, sContext);

                IEnumerable<IGrouping<int, PublicationInfo>> links = _ilbo.Publication.Where(p => p.Flow == FlowAttribute.OUT || p.Flow == FlowAttribute.INOUT
                                                                      && !_dictContextDataitem.ContainsKey(p.ControlId))
                                                             .GroupBy(p => p.LinkId);

                if (links.Count() > 0)
                    bLinkExists = true;

                if (bLinkExists)
                {
                    tryBlock.AddStatement(SnippetStatement("switch(sLinkID)"));
                    tryBlock.AddStatement(SnippetStatement("{"));
                }

                foreach (IGrouping<int, PublicationInfo> link in links)
                {
                    String sLinkID = Convert.ToString(link.Key);

                    tryBlock.AddStatement(SnippetStatement(String.Format("case \"{0}\":", sLinkID)));
                    var enumerator = link.GetEnumerator();
                    while (enumerator.MoveNext())
                    {
                        PublicationInfo pub = enumerator.Current;

                        CodeConditionStatement compareDataitem = new CodeConditionStatement(new CodeBinaryOperatorExpression(TypeReferenceExp("sDataItemName"), CodeBinaryOperatorType.IdentityEquality, PrimitiveExpression(pub.DataItemName)));
                        CodeMethodReturnStatement returnStatement;
                        //for grid control
                        if (pub.ControlType.ToLower().Equals("rsgrid"))
                        {
                            returnStatement = ReturnExpression(CodeDomHelper.MethodInvocationExp(TypeReferenceExp(String.Format("m_con{0}", pub.ControlId)), "GetNumSelectedRows"));
                        }
                        //other than grid controls
                        else
                        {
                            returnStatement = ReturnExpression(CodeDomHelper.MethodInvocationExp(TypeReferenceExp(String.Format("m_con{0}", pub.ControlId)), "GetNumInstances"));
                        }
                        compareDataitem.TrueStatements.Add(returnStatement);
                        AddExpressionToTryBlock(tryBlock, compareDataitem);
                    }

                    tryBlock.AddStatement(SnippetExpression("break"));
                }

                if (bLinkExists)
                {
                    // Default Case
                    tryBlock.AddStatement(SnippetStatement("default:"));
                    tryBlock.AddStatement(SnippetExpression("break"));
                    // Switch Case Closing
                    tryBlock.AddStatement(SnippetStatement("}"));

                    // For Extended Controls
                    GetExtendedLinkMethods(tryBlock, "GetDataItemInstances");

                    Generate_iEDKMethod(tryBlock, String.Format("{0}{1}{2}", sContext, "_", "Default"));

                    CodeConditionStatement bFlagCondition = new CodeConditionStatement(SnippetExpression("!bFlag"));
                    tryBlock.AddStatement(bFlagCondition);
                    bFlagCondition.TrueStatements.Add(AddTraceInfo("Invalid Link ID."));
                    //bFlagCondition.TrueStatements.Add(CodeDomHelper.ThrowNewException("Invalid Link ID"));
                    tryBlock.AddStatement(ReturnExpression(VariableReferenceExp("sRetData")));
                    //tryBlock.AddStatement(SnippetExpression("break"));//commented for link change - iedkpart moved out of the default case
                }


                // Base Callout End
                Generate_CallOut_MethodEnd(tryBlock, sContext);

                // For iEDK
                if (!bLinkExists)
                    Generate_iEDKMethod(tryBlock, sContext);
                //else
                //    tryBlock.AddStatement(ReturnExpression(VariableReferenceExp("0")));//commented for link change - iedkpart moved out of the default case

                // Catch Block
                AddCatchClause(tryBlock, sContext);

                return GetDataItemInstances;
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("GetDataItemInstances->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }

        ///// <summary>
        ///// Generates SetDataItem Method
        ///// </summary>
        ///// <returns></returns>
        private CodeMemberMethod SetDataItem()
        {
            try
            {
                String sContext = "SetDataItem";
                DataTable dtPublication = new DataTable();
                CodeMethodInvokeExpression methodInvocation;

                CodeMemberMethod SetDataItem = new CodeMemberMethod
                {
                    Name = sContext,
                    Attributes = _ilbo.Type.Equals("9") ? (MemberAttributes.Public | MemberAttributes.Final) : (MemberAttributes.Public | MemberAttributes.Override)
                };
                SetDataItem.Parameters.Add(ParameterDeclarationExp(new CodeTypeReference(typeof(System.String)), "sLinkID"));
                SetDataItem.Parameters.Add(ParameterDeclarationExp(new CodeTypeReference(typeof(System.String)), "sDataItemName"));
                SetDataItem.Parameters.Add(ParameterDeclarationExp(new CodeTypeReference(typeof(long)), "nInstance"));
                SetDataItem.Parameters.Add(ParameterDeclarationExp(new CodeTypeReference(typeof(System.String)), "sValue"));

                // Method Summary
                AddMethodSummary(SetDataItem, "sets the DataItem");

                // Try Block
                CodeTryCatchFinallyStatement tryBlock = AddTryBlock(SetDataItem);
                tryBlock.AddStatement(AddTraceInfo(SnippetExpression("String.Format(\"" + sContext + "(sLinkID = \\\"{0}\\\", sDataItemName = \\\"{1}\\\",nInstance = \\\"{2}\\\", sValue = \\\"{3}\\\")\", sLinkID, sDataItemName,nInstance,sValue)")));

                //Local Variable Declaration
                tryBlock.AddStatement(CodeDomHelper.DeclareVariableAndAssign(typeof(bool), "bFlag", true, false));

                Generate_CallOut_MethodStart(tryBlock, sContext);

                tryBlock.AddStatement(SnippetStatement("switch(sLinkID)"));
                tryBlock.AddStatement(SnippetStatement("{"));

                if (!_ilbo.Type.Equals("9"))
                {
                    tryBlock.AddStatement(SnippetStatement("case \"ezeeview\":"));
                    tryBlock.AddStatement(ReturnExpression());
                }

                IEnumerable<IGrouping<int, PublicationInfo>> links = _ilbo.Publication.Where(p => p.Flow == FlowAttribute.IN || p.Flow == FlowAttribute.INOUT
                                                                                        //&& !dictContextDataitem.ContainsKey(p.ControlId)
                                                                                        && (p.ControlType != "rsstackedlinks"))
                                                                              .GroupBy(p => p.LinkId);
                if (links.Count() == 0)
                    goto DefaultBlock;

                // Looping for Each Link ID
                foreach (IGrouping<int, PublicationInfo> link in links)
                {
                    String sLinkID = Convert.ToString(link.Key);

                    tryBlock.AddStatement(SnippetStatement(String.Format("case \"{0}\":", sLinkID)));
                    var enumerator = link.GetEnumerator();
                    while (enumerator.MoveNext())
                    {
                        PublicationInfo pub = enumerator.Current;
                        CodeConditionStatement compareDataItem = new CodeConditionStatement(new CodeBinaryOperatorExpression(TypeReferenceExp("sDataItemName"), CodeBinaryOperatorType.IdentityEquality, PrimitiveExpression(pub.DataItemName)));

                        if (pub.ControlType.ToLower().Equals("rsgrid"))
                        {
                            methodInvocation = MethodInvocationExp(TypeReferenceExp(String.Format("m_con{0}", pub.ControlId)), "SetPagedControlValue");
                            AddParameters(methodInvocation, new Object[] { "modeflag", GetProperty(typeof(String), "Empty"), ArgumentReferenceExp("nInstance") });
                            compareDataItem.TrueStatements.Add(methodInvocation);

                            methodInvocation = MethodInvocationExp(TypeReferenceExp(String.Format("m_con{0}", pub.ControlId)), "SetControlValue");
                            AddParameters(methodInvocation, new Object[] { pub.ViewName, ArgumentReferenceExp("sValue"), ArgumentReferenceExp("nInstance") });
                        }
                        else if (_ilbo.HasContextDataItem && pub.ControlId.Equals("rvwrt_lctxt_ou"))
                        {
                            methodInvocation = MethodInvocationExp(MethodInvocationExp(BaseReferenceExp(), "GetControlX").AddParameters(new Object[] { PrimitiveExpression(pub.ControlId) }), "SetControlValue");
                            methodInvocation.AddParameters(new CodeExpression[] { PrimitiveExpression(pub.ControlId), ArgumentReferenceExp("sValue"), PrimitiveExpression(0) });
                        }
                        else
                        {
                            methodInvocation = MethodInvocationExp(TypeReferenceExp(String.Format("m_con{0}", pub.ControlId)), "SetControlValue");
                            AddParameters(methodInvocation, new Object[] { pub.ViewName, ArgumentReferenceExp("sValue"), Convert.ToInt64("0") });
                        }
                        compareDataItem.TrueStatements.Add(methodInvocation);
                        compareDataItem.TrueStatements.Add(ReturnExpression());
                        tryBlock.AddStatement(compareDataItem);
                    }
                    tryBlock.AddStatement(SnippetExpression("break"));
                }
            DefaultBlock:
                // Default Block
                tryBlock.AddStatement(SnippetStatement("default:"));
                tryBlock.AddStatement(SnippetExpression("break"));

                // Switch Case Closing
                tryBlock.AddStatement(SnippetStatement("}"));

                // For Extended Controls
                GetExtendedLinkMethods(tryBlock, "SetDataItem");

                // For iEDK
                Generate_iEDKMethod(tryBlock, sContext);

                // Base Callout End
                Generate_CallOut_MethodEnd(tryBlock, sContext);

                // Catch Block
                AddCatchClause(tryBlock, sContext);
                return SetDataItem;
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("SetDataItem->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }


        ///// <summary>
        ///// 
        ///// </summary>
        ///// <returns></returns>
        private CodeMemberMethod GetVariable()
        {
            try
            {
                String sContext = "GetVariable";
                CodeMemberMethod GetVariable = new CodeMemberMethod
                {
                    Name = sContext,
                    Attributes = _ilbo.Type.Equals("9") ? (MemberAttributes.Public | MemberAttributes.Final) : (MemberAttributes.Public | MemberAttributes.Override),
                    ReturnType = new CodeTypeReference("IGlobalVariable")
                };
                GetVariable.Parameters.Add(ParameterDeclarationExp(typeof(String), "sVariable"));

                // Method Summary
                AddMethodSummary(GetVariable, "Gets the GlobalVariable handle");

                // Try Block
                CodeTryCatchFinallyStatement tryBlock = AddTryBlock(GetVariable);
                tryBlock.AddStatement(AddTraceInfo(SnippetExpression("String.Format(\"" + sContext + "(sVariable = \\\"{0}\\\")\", sVariable)")));

                // Base Callout
                Generate_CallOut_MethodStart(tryBlock, sContext);

                // For iEDK
                if (!_ilbo.Type.Equals("9"))
                    Generate_iEDKMethod(tryBlock, sContext);
                else
                    tryBlock.AddStatement(ReturnExpression(SnippetExpression("null")));

                // Catch Block
                AddCatchClause(tryBlock, sContext);

                return GetVariable;
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("GetVariable->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }

        private void PerformTask_Cmn(CodeTryCatchFinallyStatement tryBlock, String MethodName)
        {
            try
            {
                // Base Callout Start
                Generate_CallOut_MethodStart(tryBlock, MethodName);

                // Local Variable Declarations
                tryBlock.AddStatement(DeclareVariableAndAssign(typeof(long), "lSubscriptionID", true, 0));
                tryBlock.AddStatement(DeclareVariableAndAssign(typeof(bool), "bServiceResult", true, false));
                if (_ilbo.HasStackedLinks)
                {
                    tryBlock.AddStatement(DeclareVariable("IAsyncResult", "retAsynResult"));
                }

                // For eZeeView
                if (_ilbo.HaseZeeView)
                {
                    tryBlock.AddStatement(CodeDomHelper.DeclareVariableAndAssign(typeof(String), "sEzVwSPName", true, SnippetExpression("String.Empty")));
                    tryBlock.AddStatement(CodeDomHelper.DeclareVariableAndAssign(typeof(String), "sEzVwParamters", true, SnippetExpression("String.Empty")));
                }

                // For Control Extension
                if (_ilbo.HasControlExtensions)
                {
                    CodeConditionStatement checkControlExtension = new CodeConditionStatement
                    {
                        Condition = VariableReferenceExp("bCE")
                    };
                    checkControlExtension.TrueStatements.Add(Generate_SetContextValue(ThisReferenceExp(), "SetContextValue", "ICT_CONTROLEXT_LINKID", String.Empty));
                    tryBlock.AddStatement(checkControlExtension);
                }

                // For Dynamic Links
                if (_ilbo.HasDynamicLink)
                {
                    CodeConditionStatement checkDynamicLink = new CodeConditionStatement
                    {
                        Condition = VariableReferenceExp("bLink")
                    };
                    checkDynamicLink.TrueStatements.Add(Generate_SetContextValue(ThisReferenceExp(), "SetContextValue", "ICT_DYNAMIC_EVENTNAME", String.Empty));
                    tryBlock.AddStatement(checkDynamicLink);
                }

                tryBlock.AddStatement(AddISManager());
                tryBlock.AddStatement(DeclareVariableAndAssign("IDataBroker", "IDBroker", true, MethodInvocationExp(TypeReferenceExp("ISManager"), "GetDataBroker")));
                tryBlock.AddStatement(DeclareVariableAndAssign("IScreenObjectLauncher", "IHandler", true, MethodInvocationExp(TypeReferenceExp("ISManager"), "GetScreenObjectLauncher")));
                tryBlock.AddStatement(DeclareVariableAndAssign("IMessage", "IMsg", true, MethodInvocationExp(TypeReferenceExp("ISManager"), "GetMessageObject")));
                tryBlock.AddStatement(DeclareVariableAndAssign("IContext", "ISMContext", true, MethodInvocationExp(TypeReferenceExp("ISManager"), "GetContextObject")));
                tryBlock.AddStatement(DeclareVariableAndAssign("IReportManager", "IRptManager", true, MethodInvocationExp(TypeReferenceExp("ISManager"), "GetReportManager")));
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("PerformTask_Cmn->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private CodeMemberMethod PerformTask()
        {
            try
            {
                String sContext = "PerformTask";

                CodeMemberMethod PerformTask = new CodeMemberMethod
                {
                    Name = sContext,
                    Attributes = _ilbo.Type.Equals("9") ? (MemberAttributes.Public | MemberAttributes.Final) : (MemberAttributes.Public | MemberAttributes.Override),
                    ReturnType = new CodeTypeReference(typeof(bool))
                };
                PerformTask.Parameters.Add(ParameterDeclarationExp(new CodeTypeReference(typeof(System.String)), "sControlID"));
                PerformTask.Parameters.Add(ParameterDeclarationExp(new CodeTypeReference(typeof(System.String)), "sEventName"));
                PerformTask.Parameters.Add(ParameterDeclarationExp(new CodeTypeReference(typeof(System.String)), "sEventDetails"));
                PerformTask.Parameters.Add(new CodeParameterDeclarationExpression
                {
                    Type = new CodeTypeReference(typeof(System.String)),
                    Name = "sTargetURL",
                    Direction = FieldDirection.Out
                });


                // Method Summary
                AddMethodSummary(PerformTask, "Executes User defined tasks");

                //
                if (_ilbo.HasMessageLookup)
                {
                    PerformTask.AddStatement(DeclareVariableAndAssign(typeof(string), "sTargetActivity", true, GetProperty(TypeReferenceExp(typeof(String)), "Empty")));
                    PerformTask.AddStatement(DeclareVariableAndAssign(typeof(string), "sTargetILBO", true, GetProperty(TypeReferenceExp(typeof(String)), "Empty")));
                }

                #region try block
                CodeTryCatchFinallyStatement tryBlock = AddTryBlock(PerformTask);
                tryBlock.AddStatement(AddTraceInfo(SnippetExpression("String.Format(\"" + sContext + "(sControlID = \\\"{0}\\\", sEventName = \\\"{1}\\\", sEventDetails = \\\"{2}\\\")\", sControlID, sEventName, sEventDetails)")));

                // Common Code
                PerformTask_Cmn(tryBlock, "PerformTask");

                tryBlock.AddStatement(AssignVariable("sTargetURL", GetProperty(typeof(String), "Empty")));

                if (_ilbo.HaseZeeView && !_ilbo.Type.Equals("9"))
                {
                    tryBlock.AddStatement(AssignVariable("sEzVwSPName", GetProperty(typeof(String), "Empty")));
                    tryBlock.AddStatement(AssignVariable("sEzVwParamters", GetProperty(typeof(String), "Empty")));
                }

                // Switch Case Start
                tryBlock.AddStatement(SnippetStatement("switch(sEventName.ToLower())"));
                tryBlock.AddStatement(SnippetStatement("{"));

                // For Extended Links
                BeginPerformTask_ExtendedLinks(tryBlock, "PerformTask");

                // For Disposal Task
                BeginPerformTask_DisposalTasks(tryBlock, "PerformTask");

                // For Trans, UI and Submit Task
                BeginPerformTask_TransTasks(tryBlock, "PerformTask");

                // For Help, Link and zoom
                BeginPerformTask_HelpLinkZoomTasks(tryBlock, "PerformTask");

                // For Report Tasks
                BeginPerformTask_ReportTasks(tryBlock, "PerformTask");

                // Default Block
                tryBlock.AddStatement(SnippetStatement("default:"));

                // For iEDK
                Generate_iEDKMethod(tryBlock, sContext);
                tryBlock.AddStatement(SnippetExpression("break"));

                // Switch Case Closing
                tryBlock.AddStatement(SnippetStatement("}")); //scope ends for task switch

                if (_ilbo.HasBaseCallout)
                    tryBlock.AddStatement(MethodInvocationExp(VariableReferenceExp("objActBaseEx"), "PerformTask_End").AddParameters(new CodeExpression[] { VariableReferenceExp("sControlID"), VariableReferenceExp("sEventName"), VariableReferenceExp("sEventDetails"), SnippetExpression("out sTargetURL"), SnippetExpression("ref htContextItems"), SnippetExpression("ref ISMContext"), SnippetExpression("ref ISManager"), SnippetExpression("ref IMsg"), SnippetExpression("ref IHandler"), SnippetExpression("ref IDBroker"), SnippetExpression("ref bServiceResult") }));

                tryBlock.AddStatement(ReturnExpression(PrimitiveExpression(true)));
                #endregion

                // Catch Block
                AddCatchClause(tryBlock, sContext);

                return PerformTask;
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("PerformTask->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }


        /// <summary>
        /// Forms BeginPerformTask Method
        /// </summary>
        /// <returns></returns>
        private CodeMemberMethod BeginPerformTask()
        {
            try
            {
                String sContext = "BeginPerformTask";
                CodeMethodInvokeExpression methodInvokation;
                CodeMemberMethod BeginPerformTask = new CodeMemberMethod
                {
                    Name = sContext,
                    Attributes = MemberAttributes.Public | MemberAttributes.Override,
                    ReturnType = new CodeTypeReference("IAsyncResult")
                };
                BeginPerformTask.Parameters.Add(ParameterDeclarationExp(new CodeTypeReference("AsyncCallback"), "cb"));
                BeginPerformTask.Parameters.Add(ParameterDeclarationExp(new CodeTypeReference("VWRequestState"), "reqState"));

                // Method Summary
                AddMethodSummary(BeginPerformTask, "Executes User defined tasks");

                if (_ilbo.HasMessageLookup)
                {
                    BeginPerformTask.AddStatement(DeclareVariableAndAssign(typeof(string), "sTargetActivity", true, GetProperty(TypeReferenceExp(typeof(String)), "Empty")));
                    BeginPerformTask.AddStatement(DeclareVariableAndAssign(typeof(string), "sTargetILBO", true, GetProperty(TypeReferenceExp(typeof(String)), "Empty")));
                }

                // Try Block
                #region try block
                CodeTryCatchFinallyStatement tryBlock = AddTryBlock(BeginPerformTask);
                tryBlock.AddStatement(AddTraceInfo(SnippetExpression("String.Format(\"" + sContext + "(sControlID = \\\"{0}\\\", sEventName = \\\"{1}\\\", sEventDetails = \\\"{2}\\\")\", reqState.Control, reqState.EventName, reqState.EventDetails)")));

                // Common Code
                PerformTask_Cmn(tryBlock, "BeginPerformTask");

                // Pre Task Execution
                methodInvokation = MethodInvocationExp(BaseReferenceExp(), "PreTaskExecution");
                AddParameters(methodInvokation, new Object[] { ArgumentReferenceExp("reqState") });
                tryBlock.AddStatement(DeclareVariableAndAssign(typeof(Boolean), "bExecFlag", true, methodInvokation));

                if (_ilbo.HasPreTaskCallout)
                {
                    CodeConditionStatement checkExecutionFlg = IfCondition();
                    checkExecutionFlg.Condition = VariableReferenceExp("bExecFlag");
                    checkExecutionFlg.TrueStatements.Add(AssignVariable(VariableReferenceExp("bExecFlag"), MethodInvocationExp(ThisReferenceExp(), "PerformTaskCallout").AddParameters(new CodeExpression[] { GetProperty("reqState", "TaskName"), GetProperty("TaskCalloutMode", "PreTask") })));
                    tryBlock.AddStatement(checkExecutionFlg);
                }

                CodeConditionStatement checkExecutionFlg1 = new CodeConditionStatement
                {
                    Condition = new CodeBinaryOperatorExpression(VariableReferenceExp("bExecFlag"),
                                                                CodeBinaryOperatorType.IdentityEquality,
                                                                PrimitiveExpression(false))
                };
                methodInvokation = MethodInvocationExp(TypeReferenceExp("ISManager"), "SetAsyncException");
                AddParameters(methodInvokation, new Object[] { SnippetExpression("null") });
                checkExecutionFlg1.TrueStatements.Add(ReturnExpression(methodInvokation));
                tryBlock.AddStatement(checkExecutionFlg1);
                #endregion

                // Data Population
                //BeginPerformTask_DataPopulatuion();

                // Switch Case Start
                tryBlock.AddStatement(SnippetStatement("switch(reqState.TaskName.ToLower())"));
                tryBlock.AddStatement(SnippetStatement("{"));

                // For Extended Links
                BeginPerformTask_ExtendedLinks(tryBlock, "BeginPerformTask");

                // For Trans, UI and Submit Task
                BeginPerformTask_TransTasks(tryBlock, "BeginPerformTask");

                // For Link, Help and Zoom Tasks
                BeginPerformTask_HelpLinkZoomTasks(tryBlock, "BeginPerformTask");

                // For Report Tasks
                BeginPerformTask_ReportTasks(tryBlock, "BeginPerformTask");

                // Default Block
                #region default:
                tryBlock.AddStatement(SnippetStatement("default:"));

                //Generate_iEDKMethod(tryBlock, sContext);
                Generate_iEDKMethod(tryBlock, String.Format("{0}_{1}", sContext, "Final"));
                tryBlock.AddStatement(SnippetExpression("break"));
                #endregion

                // Switch Case Closing
                tryBlock.AddStatement(SnippetStatement("}")); //scope ends for task switch

                // Base Callout End
                Generate_CallOut_MethodEnd(tryBlock, sContext);

                methodInvokation = MethodInvocationExp(TypeReferenceExp("ISManager"), "SetAsyncCompleted");
                AddParameters(methodInvokation, new Object[] { new CodeObjectCreateExpression { CreateType = new CodeTypeReference("VWResponseState") } });
                tryBlock.AddStatement(ReturnExpression(methodInvokation));

                AddCatchClause(tryBlock, sContext);

                return BeginPerformTask;
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("BeginPerformTask->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }

        //private void BeginPerformTask_DataPopulation()
        //{
        //    try
        //    {
        //        ilbo.dsTaskList = new DataSet();

        //        //Populating Submit/Trans tasks
        //        String spName = "de_il_GetTransSubmitTasks_SP";
        //        GlobalVar.var parameters = dbManager.CreateParameters(3);
        //        GlobalVar.dbManager.AddParamters(parameters,0, "@ActID", ilbo.ActivityId);
        //        GlobalVar.dbManager.AddParamters(parameters,1, "@ILBOCode", ilbo.Name);
        //        GlobalVar.dbManager.AddParamters(parameters,2, "@EcrNo", GlobalVar.Ecrno);
        //        DataTable dt = GlobalVar.dbManager.ExecuteDataTable(CommandType.StoredProcedure, spName);
        //        dt.TableName = "TransSubmitTasks";
        //        ilbo.dsTaskList.Tables.Add(dt);

        //        //Populating Help/Link/Zoom tasks
        //        spName = "de_il_GetHelpLinkZoomTasks_SP";
        //        GlobalVar.var parameters = dbManager.CreateParameters(3);
        //        GlobalVar.dbManager.AddParamters(parameters,0, "@ActID", ilbo.ActivityId);
        //        GlobalVar.dbManager.AddParamters(parameters,1, "@ILBOCode", ilbo.Name);
        //        GlobalVar.dbManager.AddParamters(parameters,2, "@EcrNo", GlobalVar.Ecrno);
        //        dt = GlobalVar.dbManager.ExecuteDataTable(CommandType.StoredProcedure, spName);
        //        dt.TableName = "HelpLinkZoomTasks";
        //        ilbo.dsTaskList.Tables.Add(dt);

        //        //Populating Report tasks
        //        spName = "de_IL_GetReportTasks_SP";
        //        GlobalVar.var parameters = dbManager.CreateParameters(3);
        //        GlobalVar.dbManager.AddParamters(parameters,0, "@ActID", ilbo.ActivityId);
        //        GlobalVar.dbManager.AddParamters(parameters,1, "@ILBOCode", ilbo.Name);
        //        GlobalVar.dbManager.AddParamters(parameters,2, "@EcrNo", GlobalVar.Ecrno);
        //        dt = GlobalVar.dbManager.ExecuteDataTable(CommandType.StoredProcedure, spName);
        //        dt.TableName = "ReportTasks";
        //        ilbo.dsTaskList.Tables.Add(dt);

        //        //Populating Disposal Task
        //        spName = "de_IL_GetDisposalTasks_SP";
        //        GlobalVar.var parameters = dbManager.CreateParameters(3);
        //        GlobalVar.dbManager.AddParamters(parameters,0, "@ActID", ilbo.ActivityId);
        //        GlobalVar.dbManager.AddParamters(parameters,1, "@ILBOCode", ilbo.Name);
        //        GlobalVar.dbManager.AddParamters(parameters,2, "@EcrNo", GlobalVar.Ecrno);
        //        dt = GlobalVar.dbManager.ExecuteDataTable(CommandType.StoredProcedure, spName);
        //        dt.TableName = "DisposalTasks";
        //        ilbo.dsTaskList.Tables.Add(dt);
        //    }
        //    catch (Exception ex)
        //    {
        //        throw new Exception(String.Format("BeginPerformTask_DataPopulatuion->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
        //    }
        //}

        private void BeginPerformTask_DisposalTasks(CodeTryCatchFinallyStatement tryBlock, String CallingMethod)
        {
            CodeMethodInvokeExpression methodInvokation;
            StringBuilder sbQuery = new StringBuilder();

            try
            {
                #region Disposal Tasks
                foreach (TaskService task in _ilbo.TaskServiceList.Where(t => t.Type == "disposal"))
                {
                    tryBlock.AddStatement(SnippetStatement(String.Format("case \"{0}\":", task.Name)));
                    tryBlock.AddStatement(ISMSetContextValue("SCT_LASTTASK_TYPE", task.Type));

                    methodInvokation = MethodInvocationExp(ThisReferenceExp(), "ExecuteService");
                    AddParameters(methodInvokation, new object[] { task.ServiceName });

                    tryBlock.AddStatement(AssignVariable("bServiceResult", methodInvokation));
                    CodeConditionStatement checkServiceResult = new CodeConditionStatement
                    {
                        Condition = new CodeBinaryOperatorExpression(VariableReferenceExp("bServiceResult"),
                                                                    CodeBinaryOperatorType.IdentityEquality,
                                                                    PrimitiveExpression(false))
                    };
                    checkServiceResult.TrueStatements.Add(ReturnExpression(PrimitiveExpression(false)));
                    checkServiceResult.FalseStatements.Add(ReturnExpression(PrimitiveExpression(true)));
                    tryBlock.AddStatement(checkServiceResult);
                }
                #endregion Disposal Tasks
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("BeginPerformTask_DisposalTasks->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }

        private void BeginPerformTask_TransTasks(CodeTryCatchFinallyStatement tryBlock, String CallingMethod)
        {
            CodeMethodInvokeExpression methodInvokation;

            try
            {
                #region Trans/Submit Tasks
                foreach (TaskService task in _ilbo.TaskServiceList.Where(t => t.Type.Equals("trans") || t.Type.Equals("ui")))
                //foreach (TaskService task in _ilbo.TaskServiceList.Where(t => t.Type.Equals("trans") || t.Type.Equals("ui")).OrderBy(t => t.Name))
                {
                    tryBlock.AddStatement(SnippetStatement(String.Format("case \"{0}\":", task.Name)));
                    tryBlock.AddStatement(ISMSetContextValue("SCT_LASTTASK_TYPE", task.Type));

                    if (CallingMethod == "BeginPerformTask")
                    {
                        if (_ilbo.HasBaseCallout)
                        {
                            tryBlock.AddStatement(MethodInvocationExp(VariableReferenceExp("objActBaseEx"), "BeginPerformTask_After_Context").AddParameters(new CodeExpression[] { VariableReferenceExp("cb"), VariableReferenceExp("reqState"), SnippetExpression("ref htContextItems"), SnippetExpression("ref ISMContext"), SnippetExpression("ref ISManager"), SnippetExpression("ref IMsg"), SnippetExpression("ref IHandler"), SnippetExpression("ref IDBroker") }));
                            if (!string.IsNullOrEmpty(task.ServiceName))
                                tryBlock.AddStatement(MethodInvocationExp(VariableReferenceExp("objActBaseEx"), "BeginPerformTask_Before_ExecuteService").AddParameters(new CodeExpression[] { VariableReferenceExp("cb"), VariableReferenceExp("reqState"), SnippetExpression("ref htContextItems"), SnippetExpression("ref ISMContext"), SnippetExpression("ref ISManager"), SnippetExpression("ref IMsg"), SnippetExpression("ref IHandler"), SnippetExpression("ref IDBroker") }));
                        }

                        tryBlock.AddStatement(SetProperty("reqState", "ServiceName", PrimitiveExpression(task.ServiceName)));
                        tryBlock.AddStatement(SetProperty("reqState", "TransactionScope", (task.ServiceType > 0) ? PrimitiveExpression(0) : PrimitiveExpression(2)));

                        methodInvokation = MethodInvocationExp(ThisReferenceExp(), "BeginExecuteService");
                        AddParameters(methodInvokation, new Object[] { ArgumentReferenceExp("cb"), ArgumentReferenceExp("reqState") });
                        tryBlock.AddStatement(ReturnExpression(methodInvokation));

                    }
                    else if (CallingMethod == "PerformTask")
                    {
                        if (_ilbo.HasBaseCallout)
                        {
                            tryBlock.AddStatement(MethodInvocationExp(VariableReferenceExp("objActBaseEx"), "PerformTask_After_Context").AddParameters(new CodeExpression[] { VariableReferenceExp("sControlID"), VariableReferenceExp("sEventName"), VariableReferenceExp("sEventDetails"), SnippetExpression("out sTargetURL"), SnippetExpression("ref htContextItems"), SnippetExpression("ref ISMContext"), SnippetExpression("ref ISManager"), SnippetExpression("ref IMsg"), SnippetExpression("ref IHandler"), SnippetExpression("ref IDBroker") }));
                            if (!string.IsNullOrEmpty(task.ServiceName))
                                tryBlock.AddStatement(MethodInvocationExp(VariableReferenceExp("objActBaseEx"), "PerformTask_Before_ExecuteService").AddParameters(new CodeExpression[] { VariableReferenceExp("sControlID"), VariableReferenceExp("sEventName"), VariableReferenceExp("sEventDetails"), SnippetExpression("out sTargetURL"), SnippetExpression("ref htContextItems"), SnippetExpression("ref ISMContext"), SnippetExpression("ref ISManager"), SnippetExpression("ref IMsg"), SnippetExpression("ref IHandler"), SnippetExpression("ref IDBroker") }));
                        }

                        methodInvokation = MethodInvocationExp(ThisReferenceExp(), "ExecuteService");
                        AddParameters(methodInvokation, new object[] { task.ServiceName });
                        tryBlock.AddStatement(AssignVariable("bServiceResult", methodInvokation));

                        if (_ilbo.HasBaseCallout && !string.IsNullOrEmpty(task.ServiceName))
                            tryBlock.AddStatement(MethodInvocationExp(VariableReferenceExp("objActBaseEx"), "PerformTask_After_ExecuteService").AddParameters(new CodeExpression[] { VariableReferenceExp("sControlID"), VariableReferenceExp("sEventName"), VariableReferenceExp("sEventDetails"), SnippetExpression("out sTargetURL"), SnippetExpression("ref htContextItems"), SnippetExpression("ref ISMContext"), SnippetExpression("ref ISManager"), SnippetExpression("ref IMsg"), SnippetExpression("ref IHandler"), SnippetExpression("ref IDBroker"), SnippetExpression("ref bServiceResult") }));

                        CodeConditionStatement checkServiceResult = new CodeConditionStatement
                        {
                            Condition = new CodeBinaryOperatorExpression(VariableReferenceExp("bServiceResult"),
                                                                        CodeBinaryOperatorType.IdentityEquality,
                                                                        PrimitiveExpression(false))
                        };
                        checkServiceResult.TrueStatements.Add(ReturnExpression(PrimitiveExpression(false)));
                        if (task.IsDataSavingTask)
                        {
                            checkServiceResult.FalseStatements.Add(ISMSetContextValue("SCT_SETPAGE_STATUS", "1"));
                        }
                        checkServiceResult.FalseStatements.Add(ReturnExpression(PrimitiveExpression(true)));
                        tryBlock.AddStatement(checkServiceResult);
                    }

                }
                #endregion Trans/Submit Tasks
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("BeginPerformTask_TransTasks->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }

        private void BeginPerformTask_ReportTasks(CodeTryCatchFinallyStatement tryBlock, String CallingMethod)
        {
            CodeMethodInvokeExpression methodInvokation;

            try
            {
                #region Report Tasks
                foreach (TaskService task in _ilbo.TaskServiceList.Where(t => t.Type == "report"))
                {
                    if (task.ReportInfo != null)
                    {

                        tryBlock.AddStatement(SnippetStatement(String.Format("case \"{0}\":", task.Name)));
                        tryBlock.AddStatement(Generate_SetContextValue(TypeReferenceExp("ISMContext"), "SetContextValue", "SCT_LASTTASK_TYPE", "REPORT"));
                        tryBlock.AddStatement(Generate_SetContextValue(ThisReferenceExp(), "SetContextValue", "ICT_REPORT_CONTEXT", task.ReportInfo.ContextName));
                        tryBlock.AddStatement(Generate_SetContextValue(ThisReferenceExp(), "SetContextValue", "ICT_REPORT_TYPE", task.ReportInfo.Type));

                        if (CallingMethod == "BeginPerformTask")
                        {
                            tryBlock.AddStatement(SetProperty("reqState", "ServiceName", PrimitiveExpression(task.ServiceName)));
                            tryBlock.AddStatement(SetProperty("reqState", "TransactionScope", task.ServiceType > 0 ? PrimitiveExpression(0) : PrimitiveExpression(2)));

                            methodInvokation = MethodInvocationExp(ThisReferenceExp(), "BeginExecuteService");
                            AddParameters(methodInvokation, new Object[] { ArgumentReferenceExp("cb"), ArgumentReferenceExp("reqState") });
                            tryBlock.AddStatement(ReturnExpression(methodInvokation));
                        }
                        else if (CallingMethod == "PerformTask")
                        {
                            methodInvokation = MethodInvocationExp(ThisReferenceExp(), "ExecuteService");
                            AddParameters(methodInvokation, new object[] { task.ServiceName });
                            tryBlock.AddStatement(AssignVariable("bServiceResult", methodInvokation));

                            CodeConditionStatement checkServiceResult = new CodeConditionStatement
                            {
                                Condition = new CodeBinaryOperatorExpression(VariableReferenceExp("bServiceResult"),
                                                                            CodeBinaryOperatorType.IdentityEquality,
                                                                            PrimitiveExpression(false))
                            };
                            checkServiceResult.TrueStatements.Add(ReturnExpression(PrimitiveExpression(false)));
                            tryBlock.AddStatement(checkServiceResult);

                            methodInvokation = MethodInvocationExp(TypeReferenceExp("IRptManager"), "SetRPTContextValue");
                            AddParameters(methodInvokation, new Object[] { task.ReportInfo.ContextName.ToLower(), SnippetExpression("ISManager.GetServiceExecutor().GetLastOutMTD()") });
                            checkServiceResult.FalseStatements.Add(methodInvokation);

                            checkServiceResult.FalseStatements.Add(AssignVariable("sTargetURL", PrimitiveExpression(String.Format("{0}.aspx", task.ReportInfo.PageName))));
                            checkServiceResult.FalseStatements.Add(ReturnExpression(PrimitiveExpression(true)));
                        }
                    }
                }
                #endregion Report Tasks
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("BeginPerformTask_ReportTasks->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }

        private CodeMethodInvokeExpression Generate_SetContextValue(CodeExpression codeExpression, String MethodName, String ParameterName, String ParameterValue)
        {
            try
            {
                CodeMethodInvokeExpression methodInvokation = MethodInvocationExp(codeExpression, MethodName);

                if (!String.IsNullOrEmpty(ParameterValue))
                    AddParameters(methodInvokation, new Object[] { ParameterName, CodeDomHelper.CreatePrimitiveExpression(ParameterValue) });
                else
                    AddParameters(methodInvokation, new Object[] { ParameterName, GetProperty(typeof(String), "Empty") });

                return methodInvokation;
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("Generate_SetContextValue->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }

        private void BeginPerformTask_HelpLinkZoomTasks(CodeTryCatchFinallyStatement tryBlock, String CallingMethod)
        {
            CodeMethodInvokeExpression methodInvokation;
            StringBuilder sbQuery = new StringBuilder();

            try
            {
                #region Help/Link/Zoom Tasks
                foreach (TaskService task in _ilbo.TaskServiceList.Where(t => t.Type == "help" || t.Type == "link" || t.Type == "zoom"))
                //foreach (TaskService task in _ilbo.TaskServiceList.Where(t => t.Type == "help" || t.Type == "link" || t.Type == "zoom").OrderBy(t => t.Name))
                {
                    tryBlock.AddStatement(SnippetStatement(String.Format("case \"{0}\":", task.Name)));
                    tryBlock.AddStatement(ISMSetContextValue("SCT_LASTTASK_TYPE", task.Type.ToUpper()));

                    #region  For Getting Child ILBO
                    if (CallingMethod.Equals("PerformTask") && task.Traversal != null)
                    {
                        WritePostTask("PerformTask", task, tryBlock);
                    }
                    #endregion

                    #region  Getting Associated Service for Help/Link Task
                    if (!String.IsNullOrEmpty(task.ServiceName))
                    {
                        //bIsServiceAvail = true;
                        if (CallingMethod == "BeginPerformTask")
                        {
                            if (_ilbo.HasBaseCallout)
                            {
                                if (!string.IsNullOrEmpty(task.ServiceName))
                                    tryBlock.AddStatement(MethodInvocationExp(VariableReferenceExp("objActBaseEx"), "BeginPerformTask_Before_ExecuteService").AddParameters(new CodeExpression[] { VariableReferenceExp("cb"), VariableReferenceExp("reqState"), SnippetExpression("ref htContextItems"), SnippetExpression("ref ISMContext"), SnippetExpression("ref ISManager"), SnippetExpression("ref IMsg"), SnippetExpression("ref IHandler"), SnippetExpression("ref IDBroker") }));
                            }

                            if (task.Traversal == null)
                            {
                                tryBlock.AddStatement(SetProperty("reqState", "ServiceName", PrimitiveExpression(task.ServiceName)));
                                tryBlock.AddStatement(SetProperty("reqState", "TransactionScope", task.ServiceType > 0 ? PrimitiveExpression(0) : PrimitiveExpression(2)));

                                methodInvokation = MethodInvocationExp(ThisReferenceExp(), "BeginExecuteService");
                                AddParameters(methodInvokation, new Object[] { ArgumentReferenceExp("cb"), ArgumentReferenceExp("reqState") });
                                tryBlock.AddStatement(ReturnExpression(methodInvokation));
                            }
                            else
                            {
                                methodInvokation = MethodInvocationExp(ThisReferenceExp(), "ExecuteService");
                                AddParameters(methodInvokation, new object[] { task.ServiceName });
                                tryBlock.AddStatement(AssignVariable("bServiceResult", methodInvokation));

                                CodeConditionStatement checkServiceResult = new CodeConditionStatement
                                {
                                    Condition = new CodeBinaryOperatorExpression(VariableReferenceExp("bServiceResult"),
                                                                                CodeBinaryOperatorType.IdentityEquality,
                                                                                PrimitiveExpression(false))
                                };
                                tryBlock.AddStatement(checkServiceResult);
                                methodInvokation = MethodInvocationExp(TypeReferenceExp("ISManager"), "SetAsyncException");
                                AddParameters(methodInvokation, new Object[] { SnippetExpression("new Exception(\"Service failed\")") });
                                checkServiceResult.TrueStatements.Add(ReturnExpression(methodInvokation));
                            }
                        }
                        else if (CallingMethod == "PerformTask")
                        {
                            if (_ilbo.HasBaseCallout && !String.IsNullOrEmpty(task.ServiceName))
                                tryBlock.AddStatement(MethodInvocationExp(VariableReferenceExp("objActBaseEx"), "PerformTask_Before_ExecuteService").AddParameters(new CodeExpression[] { VariableReferenceExp("sControlID"),
                                                                                                                                                                                            VariableReferenceExp("sEventName"),
                                                                                                                                                                                            VariableReferenceExp("sEventDetails"),
                                                                                                                                                                                            SnippetExpression("out sTargetURL"),
                                                                                                                                                                                            SnippetExpression("ref htContextItems"),
                                                                                                                                                                                            SnippetExpression("ref ISMContext"),
                                                                                                                                                                                            SnippetExpression("ref ISManager"),
                                                                                                                                                                                            SnippetExpression("ref IMsg"),
                                                                                                                                                                                            SnippetExpression("ref IHandler"),
                                                                                                                                                                                            SnippetExpression("ref IDBroker") }));

                            methodInvokation = MethodInvocationExp(ThisReferenceExp(), "ExecuteService");
                            AddParameters(methodInvokation, new object[] { task.ServiceName });
                            tryBlock.AddStatement(AssignVariable("bServiceResult", methodInvokation));

                            if (_ilbo.HasBaseCallout && !String.IsNullOrEmpty(task.ServiceName))
                                tryBlock.AddStatement(MethodInvocationExp(VariableReferenceExp("objActBaseEx"), "PerformTask_After_ExecuteService").AddParameters(new CodeExpression[] { VariableReferenceExp("sControlID"),
                                                                                                                                                                                        VariableReferenceExp("sEventName"),
                                                                                                                                                                                        VariableReferenceExp("sEventDetails"),
                                                                                                                                                                                        SnippetExpression("out sTargetURL"),
                                                                                                                                                                                        SnippetExpression("ref htContextItems"),
                                                                                                                                                                                        SnippetExpression("ref ISMContext"),
                                                                                                                                                                                        SnippetExpression("ref ISManager"),
                                                                                                                                                                                        SnippetExpression("ref IMsg"),
                                                                                                                                                                                        SnippetExpression("ref IHandler"),
                                                                                                                                                                                        SnippetExpression("ref IDBroker"),
                                                                                                                                                                                        SnippetExpression("ref bServiceResult")
                                                                                                                                                                                        }));

                            CodeConditionStatement checkServiceResult = new CodeConditionStatement
                            {
                                Condition = new CodeBinaryOperatorExpression(VariableReferenceExp("bServiceResult"),
                                                                            CodeBinaryOperatorType.IdentityEquality,
                                                                            PrimitiveExpression(false))
                            };
                            checkServiceResult.TrueStatements.Add(ReturnExpression(PrimitiveExpression(false)));
                            if (task.IsDataSavingTask)
                            {
                                tryBlock.AddStatement(ISMSetContextValue("SCT_SETPAGE_STATUS", "1"));
                            }
                            tryBlock.AddStatement(checkServiceResult);
                        }
                    }
                    #endregion
                    #region  For Stacked Link
                    //if (this.ctrlsInILBO.Where(c=>c.BtSynonym == task.PrimaryControlSynonym && c.Type == "rsstackedlinks").Any())
                    if ((from c in _ctrlsInILBO.Where(c => c.Type == "rsstackedlinks")
                         from v in c.Views
                         where v.BtSynonym == task.PrimaryControlSynonym
                         select v).Any())
                    {
                        if (CallingMethod == "BeginPerformTask")
                        {
                            methodInvokation = MethodInvocationExp(BaseReferenceExp(), "BeginStackedLinkLaunch");
                            AddParameters(methodInvokation, new Object[] { ArgumentReferenceExp("cb"), ArgumentReferenceExp("reqState") });
                            tryBlock.AddStatement(AssignVariable(VariableReferenceExp("retAsynResult"), methodInvokation));

                            CodeConditionStatement checkAsyncResult = IfCondition();
                            checkAsyncResult.Condition = BinaryOpertorExpression(VariableReferenceExp("retAsynResult"), CodeBinaryOperatorType.IdentityInequality, SnippetExpression("null"));
                            checkAsyncResult.TrueStatements.Add(ReturnExpression(VariableReferenceExp("retAsynResult")));

                            tryBlock.AddStatement(checkAsyncResult);

                            tryBlock.AddStatement(SnippetExpression("break"));
                            continue;
                        }
                        else if (CallingMethod == "PerformTask")
                        {
                            methodInvokation = MethodInvocationExp(BaseReferenceExp(), "StackedLinkLaunch");
                            AddParameters(methodInvokation, new Object[] { SnippetExpression("sControlID"), SnippetExpression("out sTargetURL") });
                            tryBlock.AddStatement(methodInvokation);
                            tryBlock.AddStatement(SnippetExpression("break"));
                            continue;
                        }
                    }
                    #endregion

                    #region For eZee View
                    if (task.EzeeView != null)
                    {
                        BeginPerformTask_eZeeView(tryBlock, task.Name, task.ServiceName, task.EzeeView, CallingMethod);
                        continue;
                    }
                    #endregion

                    if (task.Traversal != null)
                        BeginPerformTask_ChildActivityILBO(tryBlock, task, CallingMethod);

                    if (CallingMethod == "PerformTask")
                    {

                        //with service
                        if (!string.IsNullOrEmpty(task.ServiceName))
                            tryBlock.AddStatement(ReturnExpression(VariableReferenceExp("bServiceResult")));
                        //without service
                        else
                            tryBlock.AddStatement(ReturnExpression(PrimitiveExpression(true)));
                    }

                    if (task.Traversal == null && task.ServiceName == "")
                        tryBlock.AddStatement(SnippetExpression("break"));
                }
                #endregion Help/Link/Zoom Tasks
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("BeginPerformTask_HelpLinkZoom->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }

        private void BeginPerformTask_ChildActivityILBO(CodeTryCatchFinallyStatement tryBlock, TaskService task, String CallingMethod)
        {
            CodeMethodInvokeExpression methodInvokation;
            String sPostHelpTask = String.Empty;
            String sPostLinkTask = String.Empty;

            try
            {
                #region getting child activity & ilbo values
                String sLinkID = String.Empty;

                if (task.Subscriptions.Any())
                {
                    String sFlowAttribute = string.Empty;

                    methodInvokation = MethodInvocationExp(TypeReferenceExp("IDBroker"), "SubscribeLink");
                    AddParameters(methodInvokation, new Object[] { _activity.Name, _ilbo.Code, task.Traversal.ChildActivity, task.Traversal.ChildIlbo, task.Subscriptions.First().LinkId.ToString() });
                    tryBlock.AddStatement(AssignVariable("lSubscriptionID", methodInvokation));
                    foreach (SubscriberInfo subscription in task.Subscriptions)
                    {
                        switch (subscription.Flow)
                        {
                            case "0":
                                sFlowAttribute = "FlowAttribute.flowIn";
                                break;
                            case "1":
                                sFlowAttribute = "FlowAttribute.flowOut";
                                break;
                            default:
                                sFlowAttribute = "FlowAttribute.flowInOut";
                                break;
                        }

                        methodInvokation = MethodInvocationExp(TypeReferenceExp("IDBroker"), "SubscribeData");
                        AddParameters(methodInvokation, new Object[] { VariableReferenceExp("lSubscriptionID"), subscription.DataItemName, SnippetExpression(sFlowAttribute),
                                                                                            subscription.RetrieveMultiple, _ilbo.Code, subscription.ControlId,
                                                                                            subscription.ViewName});
                        tryBlock.AddStatement(methodInvokation);
                    }

                }

                if (CallingMethod == "BeginPerformTask")
                {
                    methodInvokation = MethodInvocationExp(TypeReferenceExp("reqState"), "SetTraversal");
                    AddParameters(methodInvokation, new Object[] { task.Traversal.ChildActivity, task.Traversal.ChildIlbo, _activity.Name, _ilbo.Code });
                    tryBlock.AddStatement(methodInvokation);
                }
                else if (CallingMethod == "PerformTask")
                {
                    methodInvokation = MethodInvocationExp(TypeReferenceExp("IHandler"), "LaunchScreenObject");
                    AddParameters(methodInvokation, new Object[] { task.Traversal.ChildActivity, task.Traversal.ChildIlbo, _activity.Name, _ilbo.Code, SnippetExpression("sControlID") });
                    tryBlock.AddStatement(AssignVariable("sTargetURL", methodInvokation));
                    if (task.Subscriptions == null)
                        tryBlock.AddStatement(ReturnExpression(PrimitiveExpression(true)));
                }

                // For iEDK
                if (CallingMethod.ToLower().Equals("beginperformtask"))
                    Generate_iEDKMethod(tryBlock, CallingMethod);

                #endregion link/help/zoom with child screen
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("BeginPerformTask_ChildActivityILBO->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }

        private void BeginPerformTask_eZeeView(CodeTryCatchFinallyStatement tryBlock, String sTaskName, String sAssociatedService, EzeeView ezeeView, String CallingMethod)
        {
            CodeMethodInvokeExpression methodInvokation;
            String sChildActivity = "spexecutor";
            String sChildILBO = "spexecutor";
            //String spName = "de_il_ezeeview_sp";

            try
            {
                // For ezeeView Link
                #region ezeeview link

                methodInvokation = MethodInvocationExp(TypeReferenceExp("IDBroker"), "SubscribeLink");
                AddParameters(methodInvokation, new Object[] { _activity.Name, _ilbo.Code, sChildActivity, sChildILBO, "ezeeview" });
                tryBlock.AddStatement(AssignVariable("lSubscriptionID", methodInvokation));
                tryBlock.AddStatement(AssignVariable("sEzVwSPName", ezeeView.TargetSpName));

                // eZee View SP Parameters
                if (ezeeView.Params.Any())
                {
                    StringBuilder sbEzParams = new StringBuilder();
                    CodeExpressionCollection expressions = new CodeExpressionCollection();
                    foreach (EzeeViewParam param in ezeeView.Params)
                    {
                        String sFlowDirection = "FlowAttribute.flowIn";

                        methodInvokation = MethodInvocationExp(TypeReferenceExp("IDBroker"), "SubscribeData");
                        AddParameters(methodInvokation, new Object[] { VariableReferenceExp("lSubscriptionID"),
                                                                            param.Name,
                                                                            SnippetExpression(sFlowDirection),
                                                                            false,
                                                                            _ilbo.Code,
                                                                            param.ControlId,
                                                                            param.ViewName
                                                                            });
                        expressions.Add(methodInvokation);
                        sbEzParams.Append(String.Format("{0}#{1}#{2}#{3}~~", param.Name, param.ViewName, param.DataType, param.Length));
                    }

                    tryBlock.AddStatement(AssignVariable("sEzVwParamters", sbEzParams.ToString().Substring(0, sbEzParams.Length - 2)));

                    methodInvokation = MethodInvocationExp(TypeReferenceExp("m_conezvw_spname_glv_publish"), "SetControlValue");
                    AddParameters(methodInvokation, new Object[] { "ezvw_spname_glv_publish", VariableReferenceExp("sEzVwSPName") });
                    tryBlock.AddStatement(methodInvokation);

                    methodInvokation = MethodInvocationExp(TypeReferenceExp("m_conezvw_spparam_glv_publish"), "SetControlValue");
                    AddParameters(methodInvokation, new Object[] { "ezvw_spparam_glv_publish", VariableReferenceExp("sEzVwParamters") });
                    tryBlock.AddStatement(methodInvokation);

                    methodInvokation = MethodInvocationExp(TypeReferenceExp("IDBroker"), "SubscribeData");
                    AddParameters(methodInvokation, new Object[] { VariableReferenceExp("lSubscriptionID"), "spname", SnippetExpression("FlowAttribute.flowIn"), false, _ilbo.Code, "ezvw_spname_glv_publish", "ezvw_spname_glv_publish" });
                    tryBlock.AddStatement(methodInvokation);

                    methodInvokation = MethodInvocationExp(TypeReferenceExp("IDBroker"), "SubscribeData");
                    AddParameters(methodInvokation, new Object[] { SnippetExpression("lSubscriptionID"), "spparam", SnippetExpression("FlowAttribute.flowIn"), false, _ilbo.Code, "ezvw_spparam_glv_publish", "ezvw_spparam_glv_publish" });
                    tryBlock.AddStatement(methodInvokation);

                    foreach (CodeExpression expression in expressions)
                        tryBlock.AddStatement(expression);

                    // For Traversal
                    if (CallingMethod.ToLower() == "beginperformtask")
                    {
                        methodInvokation = MethodInvocationExp(TypeReferenceExp("reqState"), "SetTraversal");
                        AddParameters(methodInvokation, new Object[] { "spexecutor", "spexecutor", _activity.Name, _ilbo.Code });
                        tryBlock.AddStatement(methodInvokation);

                        Generate_iEDKMethod(tryBlock, CallingMethod);
                    }
                    else if (CallingMethod.ToLower() == "performtask")
                    {
                        methodInvokation = MethodInvocationExp(TypeReferenceExp("IHandler"), "LaunchScreenObject");
                        AddParameters(methodInvokation, new Object[] { "spexecutor", "spexecutor", _activity.Name, _ilbo.Code, SnippetExpression("sControlID") });
                        tryBlock.AddStatement(AssignVariable("sTargetURL", methodInvokation));
                        tryBlock.AddStatement(ReturnExpression(PrimitiveExpression(true)));

                        //tryBlock.AddStatement(SnippetExpression("break"));
                    }
                }
                #endregion ezeeView
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("BeginPerformTask_eZeeView->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }

        private void BeginPerformTask_ExtendedLinks(CodeTryCatchFinallyStatement tryBlock, String CallingMethod)
        {
            CodeMethodInvokeExpression methodInvokation;

            try
            {
                #region MessageLookup
                if (_ilbo.HasMessageLookup)
                {
                    tryBlock.AddStatement(SnippetStatement(String.Format("case \"{0}\":", "tskstderror")));
                    CodeConditionStatement checkMessageLookup = new CodeConditionStatement
                    {
                        Condition = VariableReferenceExp("bMsgLkup")
                    };
                    tryBlock.AddStatement(checkMessageLookup);

                    methodInvokation = MethodInvocationExp(ThisReferenceExp(), "SetContextValue");
                    AddParameters(methodInvokation, new Object[] { "ICT_ERRORLOOKUP_EVENTNAME", CallingMethod.ToLower().Equals("performtask") ? SnippetExpression("sEventDetails") : SnippetExpression("reqState.EventDetails") });
                    checkMessageLookup.TrueStatements.Add(methodInvokation);

                    if (CallingMethod == "BeginPerformTask")
                    {
                        methodInvokation = MethodInvocationExp(TypeReferenceExp("oErrorlookup"), "ExecuteErrorlookupTask");
                        AddParameters(methodInvokation, new object[] { _activity.Name, _ilbo.Code, SnippetExpression("String.Empty"), SnippetExpression("reqState.EventDetails"), SnippetExpression("ref sTargetActivity"), SnippetExpression("ref sTargetILBO") });
                        checkMessageLookup.TrueStatements.Add(methodInvokation);

                        methodInvokation = MethodInvocationExp(TypeReferenceExp("reqState"), "SetTraversal");
                        AddParameters(methodInvokation, new object[] { SnippetExpression("sTargetActivity"), SnippetExpression("sTargetILBO"), _activity.Name, _ilbo.Code });
                        checkMessageLookup.TrueStatements.Add(methodInvokation);

                        methodInvokation = MethodInvocationExp(TypeReferenceExp("IHandler"), "BeginLaunchScreenObject");
                        AddParameters(methodInvokation, new Object[] { ArgumentReferenceExp("cb"), ArgumentReferenceExp("reqState") });
                        checkMessageLookup.TrueStatements.Add(ReturnExpression(methodInvokation));
                    }
                    else if (CallingMethod == "PerformTask")
                    {
                        methodInvokation = MethodInvocationExp(TypeReferenceExp("oErrorlookup"), "ExecuteErrorlookupTask");
                        AddParameters(methodInvokation, new object[] { _activity.Name, _ilbo.Code, SnippetExpression("String.Empty"), SnippetExpression("sEventDetails"), SnippetExpression("sEventDetails"), SnippetExpression("ref sTargetURL") });
                        checkMessageLookup.TrueStatements.Add(methodInvokation);
                        tryBlock.AddStatement(ReturnExpression(PrimitiveExpression(true)));
                    }
                    tryBlock.AddStatement(SnippetExpression("break"));
                }
                #endregion

                #region control extension
                if (_ilbo.HasControlExtensions)
                {
                    tryBlock.AddStatement(SnippetStatement(String.Format("case \"{0}\":", "tskstdclink")));

                    CodeConditionStatement checkControlExtension = new CodeConditionStatement
                    {
                        Condition = VariableReferenceExp("bCE")
                    };
                    tryBlock.AddStatement(checkControlExtension);

                    if (CallingMethod == "BeginPerformTask")
                    {
                        methodInvokation = MethodInvocationExp(TypeReferenceExp("oCE"), "BeginExecuteCETask");
                        AddParameters(methodInvokation, new Object[] { SnippetExpression("(CILBO)this"), _activity.Name, _ilbo.Code, SnippetExpression("reqState.Control"), SnippetExpression("reqState.EventDetails"), SnippetExpression("ref reqState") });
                        checkControlExtension.TrueStatements.Add(methodInvokation);
                        methodInvokation = MethodInvocationExp(TypeReferenceExp("IHandler"), "BeginLaunchScreenObject");
                        AddParameters(methodInvokation, new Object[] { SnippetExpression("cb"), SnippetExpression("reqState") });

                        tryBlock.AddStatement(ReturnExpression(methodInvokation));
                    }
                    else if (CallingMethod == "PerformTask")
                    {
                        methodInvokation = MethodInvocationExp(TypeReferenceExp("oCE"), "ExecuteCETask");
                        AddParameters(methodInvokation, new Object[] { SnippetExpression("(IILBO)this"), _activity.Name, _ilbo.Code, SnippetExpression("sControlID"), SnippetExpression("sEventDetails"), SnippetExpression("ref sTargetURL") });
                        checkControlExtension.TrueStatements.Add(ReturnExpression(methodInvokation));
                    }
                    tryBlock.AddStatement(SnippetExpression("break"));
                }
                #endregion

                #region Dynamic Link
                if (_ilbo.HasDynamicLink)
                {
                    tryBlock.AddStatement(SnippetStatement(String.Format("case \"{0}\":", "tskstdlink")));

                    if (CallingMethod == "BeginPerformTask")
                    {
                        methodInvokation = MethodInvocationExp(TypeReferenceExp("ISManager"), "SetAsyncCompleted");
                        AddParameters(methodInvokation, new Object[] { new CodeObjectCreateExpression { CreateType = new CodeTypeReference("VWResponseState") } });
                        tryBlock.AddStatement(ReturnExpression(methodInvokation));
                    }
                    else if (CallingMethod == "PerformTask")
                    {
                        CodeConditionStatement checkDynamicLink = new CodeConditionStatement
                        {
                            Condition = VariableReferenceExp("bLink")
                        };
                        tryBlock.AddStatement(checkDynamicLink);

                        methodInvokation = MethodInvocationExp(TypeReferenceExp("oDlink"), "ExecuteDynamicLinkTask");
                        AddParameters(methodInvokation, new Object[] { _activity.Name, _ilbo.Code, SnippetExpression("sControlID"), SnippetExpression("m_conitk_select_link.GetControlValue(\"itk_select_link_hv\")"), SnippetExpression("String.Empty"), SnippetExpression("ref sTargetURL") });
                        checkDynamicLink.TrueStatements.Add(ReturnExpression(methodInvokation));
                    }
                    tryBlock.AddStatement(SnippetExpression("break"));
                }
                #endregion

                //if (_ilbo.HasMessageLookup || _ilbo.HasControlExtensions || _ilbo.HasDynamicLink)
                //    tryBlock.AddStatement(SnippetExpression("break"));
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("BeginPerformTask_ExtendedLinks->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }

        private void EndPerformTask_ExtendedLinks(CodeTryCatchFinallyStatement tryBlock)
        {
            CodeMethodInvokeExpression methodInvokation;
            try
            {
                if (_ilbo.HasMessageLookup)
                {
                    tryBlock.AddStatement(SnippetStatement(String.Format("case \"{0}\":", "tskstderror")));

                    CodeConditionStatement checkMessageLookup = new CodeConditionStatement
                    {
                        Condition = VariableReferenceExp("bMsgLkup")
                    };
                    methodInvokation = MethodInvocationExp(TypeReferenceExp("IHandler"), "EndLaunchScreenObject");
                    AddParameters(methodInvokation, new Object[] { ArgumentReferenceExp("ar") });
                    checkMessageLookup.TrueStatements.Add(SetProperty("resState", "TargetURL", methodInvokation));
                    tryBlock.AddStatement(checkMessageLookup);
                    tryBlock.AddStatement(ReturnExpression(PrimitiveExpression(true)));
                }

                if (_ilbo.HasControlExtensions)
                {
                    tryBlock.AddStatement(SnippetStatement(String.Format("case \"{0}\":", "tskstdclink")));

                    CodeConditionStatement checkControlExtension = new CodeConditionStatement
                    {
                        Condition = VariableReferenceExp("bCE")
                    };
                    methodInvokation = MethodInvocationExp(TypeReferenceExp("IHandler"), "EndLaunchScreenObject");
                    AddParameters(methodInvokation, new Object[] { ArgumentReferenceExp("ar") });
                    checkControlExtension.TrueStatements.Add(SetProperty("resState", "TargetURL", methodInvokation));
                    tryBlock.AddStatement(checkControlExtension);
                    tryBlock.AddStatement(ReturnExpression(PrimitiveExpression(true)));
                }

                if (_ilbo.HasDynamicLink)
                {
                    tryBlock.AddStatement(SnippetStatement(String.Format("case \"{0}\":", "tskstdlink")));

                    CodeConditionStatement checkDynamicLink = new CodeConditionStatement
                    {
                        Condition = VariableReferenceExp("bLink")
                    };
                    checkDynamicLink.TrueStatements.Add(SetContextValue("ICT_DYNAMIC_EVENTNAME", SnippetExpression("m_conitk_select_link.GetControlValue(\"itk_select_link_hv\")")));
                    methodInvokation = MethodInvocationExp(TypeReferenceExp("oDlink"), "ExecuteDynamicLinkTask");
                    AddParameters(methodInvokation, new Object[] { _activity.Name, _ilbo.Code, SnippetExpression("reqState.Control"), SnippetExpression("m_conitk_select_link.GetControlValue(\"itk_select_link_hv\")"), SnippetExpression("String.Empty"), SnippetExpression("ref sTargetURL") });
                    checkDynamicLink.TrueStatements.Add(methodInvokation);
                    checkDynamicLink.TrueStatements.Add(AssignVariable("resState.TargetURL", SnippetExpression("sTargetURL")));
                    tryBlock.AddStatement(checkDynamicLink);
                    tryBlock.AddStatement(ReturnExpression(PrimitiveExpression(true)));
                }
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("EndPerformTask_ExtendedLinks->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }

        private void EndPerformTask_TransTasks(CodeTryCatchFinallyStatement tryBlock)
        {
            CodeMethodInvokeExpression methodInvokation;

            try
            {
                #region trans/submit tasks
                foreach (TaskService task in _ilbo.TaskServiceList.Where(t => t.Type == "trans" || t.Type == "ui"))
                //foreach (TaskService task in _ilbo.TaskServiceList.Where(t => t.Type == "trans" || t.Type == "ui").OrderBy(t => t.Name))
                {
                    tryBlock.AddStatement(SnippetStatement(String.Format("case \"{0}\":", task.Name)));

                    methodInvokation = MethodInvocationExp(ThisReferenceExp(), "EndExecuteService");
                    AddParameters(methodInvokation, new Object[] { ArgumentReferenceExp("ar") });
                    tryBlock.AddStatement(AssignVariable("bServiceResult", methodInvokation));

                    if (_ilbo.HasBaseCallout && !string.IsNullOrEmpty(task.ServiceName))
                        tryBlock.AddStatement(MethodInvocationExp(VariableReferenceExp("objActBaseEx"), "EndPerformTask_After_ExecuteService").AddParameters(new CodeExpression[] { VariableReferenceExp("ar"), SnippetExpression("ref htContextItems"), SnippetExpression("ref ISMContext"), SnippetExpression("ref ISManager"), SnippetExpression("ref IMsg"), SnippetExpression("ref IHandler"), SnippetExpression("ref IDBroker"), SnippetExpression("ref bServiceResult") }));

                    tryBlock.AddStatement(SnippetExpression("break"));
                }
                #endregion trans/submit tasks
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("EndPerformTask_TransTasks->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }

        private void EndPerformTask_ChildActivityILBO(CodeTryCatchFinallyStatement tryBlock, TaskService task)
        {
            String spName = String.Empty;
            String sChildActivity = String.Empty;
            String sChildILBO = String.Empty;
            String sPostHelpTask = String.Empty;
            String sPostLinkTask = String.Empty;
            StringBuilder sbQuery = new StringBuilder();

            try
            {
                #region get child activity & ilbo
                if (task.Traversal != null)
                {
                    if (!String.IsNullOrEmpty(sChildActivity) && !String.IsNullOrEmpty(sChildILBO))
                    {
                        WritePostTask("EndPerformTask", task, tryBlock);
                    }

                    CodeMethodInvokeExpression methodInvokation = MethodInvocationExp(TypeReferenceExp("IHandler"), "EndLaunchScreenObject");
                    AddParameters(methodInvokation, new Object[] { ArgumentReferenceExp("ar") });
                    tryBlock.AddStatement(SetProperty("resState", "TargetURL", methodInvokation));
                }
                else if (!string.IsNullOrEmpty(task.ServiceName))
                {
                    CodeMethodInvokeExpression methodInvokation = MethodInvocationExp(ThisReferenceExp(), "EndExecuteService");
                    AddParameters(methodInvokation, new Object[] { ArgumentReferenceExp("ar") });
                    tryBlock.AddStatement(AssignVariable("bServiceResult", methodInvokation));
                }
                #endregion
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("EndPerformTask_ChildActivityILBO->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }

        //private void EndPerformTask_eZeeView(CodeTryCatchFinallyStatement tryBlock, String sTaskName, String sAssociatedService, DataTable dtEzeeView)
        //{
        //    CodeMethodInvokeExpression methodInvokation;
        //    String spName;

        //    try
        //    {
        //        #region ezeeview link
        //        String sChildActivity = "spexecutor";
        //        String sChildILBO = "spexecutor";
        //        StringBuilder sbEzParams = new StringBuilder();
        //        CodeExpressionCollection expressions = new CodeExpressionCollection();

        //        foreach (DataRow drEzView in dtEzeeView.Rows)
        //        {
        //            String sSPName = Convert.ToString(drEzView["Target_SPName"]);
        //            methodInvokation = MethodInvocationExp(TypeReferenceExp("IDBroker"), "SubscribeLink");
        //            AddParameters(methodInvokation, new Object[] { ilbo.ActivityName, ilbo.Name, sChildActivity, sChildILBO, "ezeeview" });
        //            tryBlock.AddStatement(AssignVariable("lSubscriptionID", methodInvokation));

        //            tryBlock.AddStatement(AssignVariable("sEzVwSPName", sSPName));

        //            // Get SP Parameters
        //            spName = "de_il_ezeeviewparam_sp";
        //            GlobalVar.var parameters = dbManager.CreateParameters(5);
        //            GlobalVar.dbManager.AddParamters(parameters,0, "@activityname", ilbo.ActivityName);
        //            GlobalVar.dbManager.AddParamters(parameters,1, "@uiname", ilbo.Name);
        //            GlobalVar.dbManager.AddParamters(parameters,2, "@targetspname", sSPName);
        //            GlobalVar.dbManager.AddParamters(parameters,3, "@ecrno", GlobalVar.Ecrno);
        //            GlobalVar.dbManager.AddParamters(parameters,4, "@taskname", sTaskName);
        //            DataTable dtEzParams = GlobalVar.dbManager.ExecuteDataTable(CommandType.StoredProcedure, spName);
        //            if (dtEzParams.Rows.Count > 0)
        //            {
        //                foreach (DataRow drEzParam in dtEzParams.Rows)
        //                {
        //                    String sParamName = Convert.ToString(drEzParam["ParameterName"]).ToLower();
        //                    String sDataType = Convert.ToString(drEzParam["dataType"]).ToLower();
        //                    String sLength = Convert.ToString(drEzParam["length"]).ToLower();
        //                    String sControlId = Convert.ToString(drEzParam["controlid"]).ToLower();
        //                    String sViewName = Convert.ToString(drEzParam["viewname"]).ToLower();
        //                    String sFlowDirection = "FlowAttribute.flowIn";

        //                    methodInvokation = MethodInvocationExp(TypeReferenceExp("IDBroker"), "SubscribeData");
        //                    AddParameters(methodInvokation, new Object[] { VariableReferenceExp("lSubscriptionID"),sParamName,
        //                                                                    SnippetExpression(sFlowDirection),false,
        //                                                                    false,ilbo.Name,sControlId,
        //                                                                    sViewName
        //                                                                    });
        //                    expressions.Add(methodInvokation);
        //                    sbEzParams.Append(String.Format("{0}#{1}#{2}#{3}~~", sParamName, sViewName, sDataType, sLength));
        //                }
        //            }
        //        }

        //        if (sbEzParams.Length > 0)
        //            tryBlock.AddStatement(AssignVariable("sEzVwParamters", sbEzParams.ToString().Substring(0, sbEzParams.Length - 2)));

        //        methodInvokation = MethodInvocationExp(TypeReferenceExp("m_conezvw_spname_glv_publish"), "SetControlValue");
        //        AddParameters(methodInvokation, new Object[] { "ezvw_spname_glv_publish", VariableReferenceExp("sEzVwSPName") });
        //        tryBlock.AddStatement(methodInvokation);

        //        methodInvokation = MethodInvocationExp(TypeReferenceExp("m_conezvw_spparam_glv_publish"), "SetControlValue");
        //        AddParameters(methodInvokation, new Object[] { "ezvw_spparam_glv_publish", VariableReferenceExp("sEzVwParamters") });
        //        tryBlock.AddStatement(methodInvokation);

        //        methodInvokation = MethodInvocationExp(TypeReferenceExp("IDBroker"), "SubscribeData");
        //        AddParameters(methodInvokation, new Object[] { VariableReferenceExp("lSubscriptionID"), "spname",
        //                                                            SnippetExpression("FlowAttribute.flowIn"), false,
        //                                                            ilbo.Name, "ezvw_spname_glv_publish",
        //                                                            "ezvw_spname_glv_publish" });
        //        tryBlock.AddStatement(methodInvokation);

        //        methodInvokation = MethodInvocationExp(TypeReferenceExp("IDBroker"), "SubscribeData");
        //        AddParameters(methodInvokation, new Object[] { SnippetExpression("lSubscriptionID"), "spparam",
        //                                                            SnippetExpression("FlowAttribute.flowIn"), false,
        //                                                            ilbo.Name, "ezvw_spparam_glv_publish",
        //                                                            "ezvw_spparam_glv_publish" });
        //        tryBlock.AddStatement(methodInvokation);

        //        foreach (CodeExpression expression in expressions)
        //            tryBlock.AddStatement(expression);

        //        sChildActivity = sChildILBO = String.Empty;
        //        #endregion ezeeview link
        //    }
        //    catch (Exception ex)
        //    {
        //        throw new Exception(String.Format("EndPerformTask_eZeeView->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
        //    }
        //}

        private void EndPerformTask_HelpLinkZoomTasks(CodeTryCatchFinallyStatement tryBlock)
        {
            CodeMethodInvokeExpression methodInvokation;
            StringBuilder sbQuery = new StringBuilder();

            try
            {
                #region help/link/zoom tasks
                foreach (TaskService task in _ilbo.TaskServiceList.Where(t => t.Type == "help" || t.Type == "link" || t.Type == "zoom"))
                //foreach (TaskService task in _ilbo.TaskServiceList.Where(t => t.Type == "help" || t.Type == "link" || t.Type == "zoom").OrderBy(t => t.Name))
                {
                    tryBlock.AddStatement(SnippetStatement(String.Format("case \"{0}\":", task.Name)));

                    if (task.Traversal != null)
                    {
                        WritePostTask("EndPerformTask", task, tryBlock);
                    }

                    // For Stacked Link
                    if ((from c in _ctrlsInILBO.Where(c => c.Type == "rsstackedlinks")
                         from v in c.Views
                         where v.BtSynonym == task.PrimaryControlSynonym
                         select v).Any())
                    {
                        methodInvokation = MethodInvocationExp(BaseReferenceExp(), "EndStackedLinkLaunch");
                        AddParameters(methodInvokation, new Object[] { ArgumentReferenceExp("ar") });
                        tryBlock.AddStatement(AssignVariable(VariableReferenceExp("bServiceResult"), methodInvokation));
                        tryBlock.AddStatement(SnippetExpression("break"));
                        continue;
                    }

                    // For eZee View
                    if (task.EzeeView != null)
                    {
                        methodInvokation = MethodInvocationExp(TypeReferenceExp("IHandler"), "EndLaunchScreenObject");
                        AddParameters(methodInvokation, new Object[] { ArgumentReferenceExp("ar") });
                        tryBlock.AddStatement(SetProperty("resState", "TargetURL", methodInvokation));
                        tryBlock.AddStatement(SnippetExpression("break"));
                        continue;
                    }

                    // For Getting Child Activity & ILBO
                    EndPerformTask_ChildActivityILBO(tryBlock, task);

                    if (_ilbo.HasBaseCallout && !string.IsNullOrEmpty(task.ServiceName))
                        tryBlock.AddStatement(MethodInvocationExp(VariableReferenceExp("objActBaseEx"), "EndPerformTask_After_ExecuteService").AddParameters(new CodeExpression[] { VariableReferenceExp("ar"), SnippetExpression("ref htContextItems"), SnippetExpression("ref ISMContext"), SnippetExpression("ref ISManager"), SnippetExpression("ref IMsg"), SnippetExpression("ref IHandler"), SnippetExpression("ref IDBroker"), SnippetExpression("ref bServiceResult") }));

                    tryBlock.AddStatement(SnippetExpression("break"));
                }
                #endregion help/link/zoom tasks
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("EndPerformTask_HelpLinkZoomTasks->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }

        private void EndPerformTask_ReportTasks(CodeTryCatchFinallyStatement tryBlock)
        {
            try
            {
                CodeMethodInvokeExpression methodInvokation;

                #region report tasks
                foreach (TaskService task in _ilbo.TaskServiceList.Where(t => t.Type == "report"))
                {
                    tryBlock.AddStatement(SnippetStatement(String.Format("case \"{0}\":", task.Name)));
                    if (task.ReportInfo != null)
                    {
                        methodInvokation = MethodInvocationExp(ThisReferenceExp(), "EndExecuteService");
                        AddParameters(methodInvokation, new Object[] { ArgumentReferenceExp("ar") });
                        tryBlock.AddStatement(AssignVariable("bServiceResult", methodInvokation));

                        CodeConditionStatement checkServiceResult = new CodeConditionStatement
                        {
                            Condition = BinaryOpertorExpression(VariableReferenceExp("bServiceResult"), CodeBinaryOperatorType.IdentityEquality, PrimitiveExpression(false))
                        };
                        checkServiceResult.TrueStatements.Add(ReturnExpression(PrimitiveExpression(false)));
                        checkServiceResult.FalseStatements.Add(SetProperty("resState", "TargetURL", PrimitiveExpression(String.Format("{0}.aspx", task.ReportInfo.PageName))));

                        #region if processingtype is 0
                        if (task.ReportInfo.ProcessingType.Equals(0))
                        {
                            if (task.IsZipped)
                            {
                                CodeConditionStatement checkZippedService = new CodeConditionStatement
                                {
                                    Condition = new CodeBinaryOperatorExpression(new CodeBinaryOperatorExpression(GetProperty("resState", "OutMTD"),
                                                                                                                        CodeBinaryOperatorType.IdentityEquality,
                                                                                                                        SnippetExpression("null")
                                                                                                                    ),
                                                                                    CodeBinaryOperatorType.BooleanAnd,
                                                                                    new CodeBinaryOperatorExpression(GetProperty("reqState", "ZippedService"),
                                                                                                                        CodeBinaryOperatorType.IdentityEquality,
                                                                                                                        PrimitiveExpression(true)
                                                                                                                    )
                                                                                    )
                                };

                                methodInvokation = MethodInvocationExp(TypeReferenceExp("IRptManager"), "SetRPTContextValue");
                                AddParameters(methodInvokation, new Object[] { task.ReportInfo.ContextName.ToLower(), GetProperty("resState", "OutMTDInBytes") });
                                checkZippedService.TrueStatements.Add(methodInvokation);

                                methodInvokation = MethodInvocationExp(TypeReferenceExp("IRptManager"), "SetRPTContextValue");
                                AddParameters(methodInvokation, new Object[] { task.ReportInfo.ContextName.ToLower(), GetProperty("resState", "OutMTD") });
                                checkZippedService.FalseStatements.Add(methodInvokation);
                                checkServiceResult.FalseStatements.Add(checkZippedService);
                            }
                            else
                            {
                                methodInvokation = MethodInvocationExp(TypeReferenceExp("IRptManager"), "SetRPTContextValue");
                                AddParameters(methodInvokation, new Object[] { task.ReportInfo.ContextName.ToLower(), SnippetExpression("resState.OutMTD") });
                                checkServiceResult.FalseStatements.Add(methodInvokation);
                            }
                        }
                        #endregion if processing type is 0

                        checkServiceResult.FalseStatements.Add(ReturnExpression(PrimitiveExpression(true)));
                        tryBlock.AddStatement(checkServiceResult);
                    }

                    CodeConditionStatement CheckForPostTaskExecution = new CodeConditionStatement
                    {
                        Condition = new CodeBinaryOperatorExpression(VariableReferenceExp("bServiceResult"),
                                                                        CodeBinaryOperatorType.IdentityEquality,
                                                                        PrimitiveExpression(true))
                    };

                    methodInvokation = MethodInvocationExp(BaseReferenceExp(), "PostTaskExecution");
                    AddParameters(methodInvokation, new Object[] { GetProperty("reqState", "TaskName") });
                    CheckForPostTaskExecution.TrueStatements.Add(AssignVariable("bServiceResult", methodInvokation));

                    tryBlock.AddStatement(SnippetExpression("break"));
                }
                #endregion report tasks
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("EndPerformTask_ReportTasks->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private CodeMemberMethod EndPerformTask()
        {
            try
            {
                String sContext = "EndPerformTask";
                CodeMethodInvokeExpression methodInvokation;
                StringBuilder sbQuery = new StringBuilder();
                CodeMemberMethod EndPerformTask = new CodeMemberMethod
                {
                    Name = sContext,
                    Attributes = MemberAttributes.Public | MemberAttributes.Override,
                    ReturnType = new CodeTypeReference(typeof(bool))
                };
                EndPerformTask.Parameters.Add(ParameterDeclarationExp(new CodeTypeReference("IAsyncResult"), "ar"));

                // Local Variable Declaration
                EndPerformTask.AddStatement(DeclareVariableAndAssign("VWAsyncResult", "result", true, SnippetExpression("ar as VWAsyncResult")));
                EndPerformTask.AddStatement(DeclareVariableAndAssign("VWRequestState", "reqState", true, SnippetExpression("result.AsyncState as VWRequestState")));
                EndPerformTask.AddStatement(DeclareVariableAndAssign("VWResponseState", "resState", true, SnippetExpression("result.ResponseState")));

                // Method Summary
                AddMethodSummary(EndPerformTask, "Executes User defined tasks");

                // Try Block
                #region try block
                CodeTryCatchFinallyStatement tryBlock = AddTryBlock(EndPerformTask);
                tryBlock.AddStatement(AddTraceInfo(SnippetExpression("String.Format(\"" + sContext + "(sControlID = \\\"{0}\\\", sEventName = \\\"{1}\\\", sEventDetails = \\\"{2}\\\")\", reqState.Control, reqState.EventName, reqState.EventDetails)")));


                tryBlock.AddStatement(AddISManager());
                tryBlock.AddStatement(DeclareVariableAndAssign("IDataBroker", "IDBroker", true, MethodInvocationExp(TypeReferenceExp("ISManager"), "GetDataBroker")));
                tryBlock.AddStatement(DeclareVariableAndAssign("IScreenObjectLauncher", "IHandler", true, MethodInvocationExp(TypeReferenceExp("ISManager"), "GetScreenObjectLauncher")));
                tryBlock.AddStatement(DeclareVariableAndAssign("IMessage", "IMsg", true, MethodInvocationExp(TypeReferenceExp("ISManager"), "GetMessageObject")));
                tryBlock.AddStatement(DeclareVariableAndAssign("IContext", "ISMContext", true, MethodInvocationExp(TypeReferenceExp("ISManager"), "GetContextObject")));
                tryBlock.AddStatement(DeclareVariableAndAssign("IReportManager", "IRptManager", true, MethodInvocationExp(TypeReferenceExp("ISManager"), "GetReportManager")));


                tryBlock.AddStatement(DeclareVariableAndAssign(typeof(long), "lSubscriptionID", true, 0));
                tryBlock.AddStatement(DeclareVariableAndAssign(typeof(bool), "bServiceResult", true, true));
                tryBlock.AddStatement(CodeDomHelper.DeclareVariableAndAssign(typeof(String), "sTargetURL", true, GetProperty(typeof(String), "Empty")));

                // For Control Extension
                if (_ilbo.HasControlExtensions)
                {
                    CodeConditionStatement checkControlExtension = new CodeConditionStatement
                    {
                        Condition = VariableReferenceExp("bCE")
                    };
                    checkControlExtension.TrueStatements.Add(Generate_SetContextValue(ThisReferenceExp(), "SetContextValue", "ICT_CONTROLEXT_LINKID", String.Empty));
                    tryBlock.AddStatement(checkControlExtension);
                }

                // Switch Case Starting
                tryBlock.AddStatement(SnippetStatement("switch(reqState.TaskName.ToLower())"));
                tryBlock.AddStatement(SnippetStatement("{"));

                // For Extended Links
                EndPerformTask_ExtendedLinks(tryBlock);

                // For Submit, UI and Trans Task
                EndPerformTask_TransTasks(tryBlock);

                // For Link, Help and Zoom Task
                EndPerformTask_HelpLinkZoomTasks(tryBlock);

                // For Report Task
                EndPerformTask_ReportTasks(tryBlock);

                // For Default Block
                tryBlock.AddStatement(SnippetStatement("default:"));

                // For iEDK
                Generate_iEDKMethod(tryBlock, sContext);
                tryBlock.AddStatement(SnippetExpression("break"));

                // Switch Case Closing
                tryBlock.AddStatement(SnippetStatement("}"));

                if (_ilbo.HasBaseCallout)
                    tryBlock.AddStatement(MethodInvocationExp(VariableReferenceExp("objActBaseEx"), "EndPerformTask_End").AddParameters(new CodeExpression[] { VariableReferenceExp("ar"), SnippetExpression("ref htContextItems"), SnippetExpression("ref ISMContext"), SnippetExpression("ref ISManager"), SnippetExpression("ref IMsg"), SnippetExpression("ref IHandler"), SnippetExpression("ref IDBroker"), SnippetExpression("ref bServiceResult") }));

                // Post Task Execution
                CodeConditionStatement CheckbServiceResult = new CodeConditionStatement
                {
                    Condition = VariableReferenceExp("bServiceResult")
                };
                methodInvokation = MethodInvocationExp(BaseReferenceExp(), "PostTaskExecution");
                AddParameters(methodInvokation, new Object[] { GetProperty("reqState", "TaskName") });
                CheckbServiceResult.TrueStatements.Add(AssignVariable("bServiceResult", methodInvokation));
                tryBlock.AddStatement(CheckbServiceResult);

                if (_ilbo.HasPostTaskCallout)
                    tryBlock.AddStatement(AssignVariable(VariableReferenceExp("bServiceResult"), MethodInvocationExp(ThisReferenceExp(), "PerformTaskCallout").AddParameters(new CodeExpression[] { GetProperty("reqState", "TaskName"), GetProperty("TaskCalloutMode", "PostTask") })));

                tryBlock.AddStatement(ReturnExpression(VariableReferenceExp("bServiceResult")));
                #endregion try block

                // Catch Block
                AddCatchClause(tryBlock, sContext);

                return EndPerformTask;
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("EndPerformTask->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }


        ///// <summary>
        ///// Forms method 'PreProcess2'
        ///// </summary>
        ///// <returns></returns>
        private CodeMemberMethod
            PreProcess2()
        {
            try
            {
                String sContext = "PreProcess2";
                CodeMethodInvokeExpression methodInvokation;
                CodeMemberMethod PreProcess2 = new CodeMemberMethod
                {
                    Name = sContext,
                    Attributes = _ilbo.Type.Equals("9") ? (MemberAttributes.Public | MemberAttributes.Final) : (MemberAttributes.Public | MemberAttributes.Override),
                    ReturnType = new CodeTypeReference(typeof(bool))
                };

                // Method Summary
                AddMethodSummary(PreProcess2, "Initiates Init tasks");

                // Local Variable Declaration
                PreProcess2.Statements.Add(DeclareVariableAndAssign(typeof(bool), "bReturn", true, false));

                // Try Block
                CodeTryCatchFinallyStatement tryBlock = new CodeTryCatchFinallyStatement();
                PreProcess2.Statements.Add(tryBlock);
                tryBlock.AddStatement(AddTraceInfo("PreProcess2"));

                // Base Callout Start
                Generate_CallOut_MethodStart(tryBlock, sContext);

                tryBlock.AddStatement(AddISManager());
                tryBlock.AddStatement(DeclareVariableAndAssign("IDataBroker", "IDBroker", true, MethodInvocationExp(TypeReferenceExp("ISManager"), "GetDataBroker")));

                // For Init Task
                var InitTasks = _ilbo.TaskServiceList.Where(t => t.Type == "initialize" || t.Type == "init");
                if (InitTasks.Any())
                {
                    methodInvokation = MethodInvocationExp(ThisReferenceExp(), "ExecuteService");
                    AddParameters(methodInvokation, new Object[] { InitTasks.First().ServiceName });
                    tryBlock.AddStatement(AssignVariable("bReturn", methodInvokation));
                }
                else
                {
                    tryBlock.AddStatement(new CodeCommentStatement("No Initialization Tasks are defined for the ILBO."));
                }

                if (_ilbo.Publication.Count > 0)
                {
                    methodInvokation = MethodInvocationExp(TypeReferenceExp("IDBroker"), "Transfer");
                    AddParameters(methodInvokation, new Object[] { GetProperty("TransferDirection", "transferIn"), _activity.Name, _ilbo.Code });
                    tryBlock.AddStatement(methodInvokation);
                }

                // For iEDK
                if (_ilbo.Type.Equals("9"))
                {
                    tryBlock.AddStatement(ReturnExpression(PrimitiveExpression(true)));
                }
                else
                {
                    // Base Callout End
                    Generate_CallOut_MethodEnd(tryBlock, sContext);

                    Generate_iEDKMethod(tryBlock, sContext);

                    // Return Statement
                    if (InitTasks.Any())
                        tryBlock.AddStatement(ReturnExpression(VariableReferenceExp("bReturn")));
                    else
                        tryBlock.AddStatement(ReturnExpression(PrimitiveExpression(true)));
                }

                // Catch Block
                AddCatchClause(tryBlock, sContext);

                return PreProcess2;
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("PreProcess2->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }


        ///// <summary>
        ///// Forms method 'PreProcess3'
        ///// </summary>
        ///// <returns></returns>
        private CodeMemberMethod PreProcess3()
        {
            try
            {
                String sContext = "PreProcess3";
                CodeMemberMethod PreProcess3 = new CodeMemberMethod
                {
                    Name = sContext,
                    Attributes = _ilbo.Type.Equals("9") ? (MemberAttributes.Public | MemberAttributes.Final) : (MemberAttributes.Public | MemberAttributes.Override),
                    ReturnType = new CodeTypeReference(typeof(bool))
                };

                CodeMethodInvokeExpression methodInvokation;

                //method comments
                AddMethodSummary(PreProcess3, "Initiates Fetch tasks");

                //local variable declarations
                PreProcess3.Statements.Add(DeclareVariableAndAssign(typeof(bool), "bReturn", true, false));

                //add try block
                CodeTryCatchFinallyStatement tryBlock = AddTryBlock(PreProcess3);
                tryBlock.AddStatement(AddTraceInfo("PreProcess3()"));

                // Base Callout
                Generate_CallOut_MethodStart(tryBlock, sContext);

                IEnumerable<TaskService> FetchTasks = _ilbo.TaskServiceList.Where(t => t.Type == "fetch");
                if (FetchTasks.Any())
                {
                    TaskService FetchTask = FetchTasks.First();

                    methodInvokation = MethodInvocationExp(ThisReferenceExp(), "ExecuteService");
                    AddParameters(methodInvokation, new Object[] { FetchTask.ServiceName });
                    tryBlock.AddStatement(AssignVariable("bReturn", methodInvokation));
                }
                else
                {
                    tryBlock.AddStatement(new CodeCommentStatement("No Fetch Tasks are defined for the ILBO."));
                }

                // Dynamic ILBO Title
                if (_ilbo.HasDynamicILBOTitle)
                {
                    methodInvokation = MethodInvocationExp(ThisReferenceExp(), "SetContextValue");
                    AddParameters(methodInvokation, new Object[] { "ICT_ILBODESCRIPTION", new CodeMethodInvokeExpression(TypeReferenceExp("m_contxtvw_rt_ilbo_title"), "GetControlValue", PrimitiveExpression("txtvw_rt_ilbo_title")) });
                    tryBlock.AddStatement(methodInvokation);
                }

                if (_ilbo.HasBaseCallout)
                {
                    tryBlock.AddStatement(MethodInvocationExp(VariableReferenceExp("objActBaseEx"), "PreProcess3_After_ExecuteService").AddParameter(SnippetExpression("ref htContextItems")));
                }

                //if (FetchTasks.Any() == false)
                //{
                //    CodeConditionStatement IfCondition = new CodeConditionStatement
                //    {
                //        Condition = new CodeBinaryOperatorExpression(VariableReferenceExp("bReturn"),
                //                                                    CodeBinaryOperatorType.IdentityEquality,
                //                                                    PrimitiveExpression(false))
                //    };
                //    tryBlock.AddStatement(IfCondition);
                //    IfCondition.TrueStatements.Add(ReturnExpression(VariableReferenceExp("bReturn")));
                //}

                // Base Callout
                //Generate_CallOut_MethodEnd(tryBlock, sContext);

                // For iEDK
                if (FetchTasks.Any() == false)
                {
                    tryBlock.AddStatement(AssignVariable(VariableReferenceExp("bReturn"), PrimitiveExpression(true)));
                }
                Generate_iEDKMethod(tryBlock, sContext);

                // Return Statement
                tryBlock.AddStatement(ReturnExpression(VariableReferenceExp("bReturn")));

                // Catch Block
                AddCatchClause(tryBlock, sContext);

                return PreProcess3;
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("PreProcess3->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }

        private void BeginPreProcess1_Zoom(CodeTryCatchFinallyStatement tryBlock, string sMethodName)
        {
            CodeMethodInvokeExpression methodInvokation;
            CodeIterationStatement ForLoop = new CodeIterationStatement();

            try
            {
                IEnumerable<Zoom> zoomControls = _ilbo.ZoomControls;

                // For Each Zoom ILBO
                if (zoomControls.Any())
                {
                    // For Zoom ILBO
                    tryBlock.AddStatement(DeclareVariableAndAssign(typeof(String), "sParentActivity", true, SnippetExpression("(String)GetContextValue(\"ICT_PARENTACTIVITY\")")));
                    tryBlock.AddStatement(DeclareVariableAndAssign(typeof(String), "sParentILBO", true, SnippetExpression("(String)GetContextValue(\"ICT_PARENTILBO\")")));
                    tryBlock.AddStatement(DeclareVariableAndAssign(typeof(String), "sParentControlID", true, SnippetExpression("(String)GetContextValue(\"ICT_PARENTCONTROL\")")));
                    tryBlock.AddStatement(DeclareVariableAndAssign(typeof(String), "sValue", true, SnippetExpression("String.Empty")));
                    tryBlock.AddStatement(DeclareVariableAndAssign("Multiline", "oMultilineControl", false));
                    tryBlock.AddStatement(DeclareVariableAndAssign(typeof(long), "lListItems", false));
                    tryBlock.AddStatement(DeclareVariableAndAssign(typeof(long), "lLoop", false));

                    // Null Check for Parent Control ID
                    CodeConditionStatement checkParentControlId = new CodeConditionStatement
                    {
                        Condition = new CodeBinaryOperatorExpression(VariableReferenceExp("sParentControlID"), CodeBinaryOperatorType.IdentityEquality, SnippetExpression("null"))
                    };
                    checkParentControlId.TrueStatements.Add(AssignVariable("sParentControlID", SnippetExpression("(String)GetContextValue(\"ICT_GRID_CONTROL\")")));
                    tryBlock.AddStatement(checkParentControlId);

                    //if (!sMethodName.ToLower().Equals("preprocess1"))
                    //{
                    methodInvokation = MethodInvocationExp(TypeReferenceExp("IobjBroker"), "GetScreenObject");
                    AddParameters(methodInvokation, new Object[] { VariableReferenceExp("sParentActivity"), VariableReferenceExp("sParentILBO") });
                    tryBlock.AddStatement(DeclareVariableAndAssign("IILBO", "IilboHandle", true, methodInvokation));
                    //}
                }


                // For Each Zoom Task
                foreach (Zoom zoomControl in zoomControls)
                {
                    Control parentControl = (from act in _ecrOptions.generation.activities.Where(a => a.Name == _activity.Name)
                                             from ui in act.ILBOs.Where(i => i.Code == zoomControl.ParentIlboCode)
                                             from page in ui.TabPages
                                             from ctrl in page.Controls.Where(c => c.Id == zoomControl.ControlId)
                                             select ctrl).First();
                    foreach (ZoomMapping zoomMapping in zoomControl.Mappings)
                    {
                        string sOptionCode = string.Empty;

                        View parentView = (from view in parentControl.Views
                                           where view.Name == zoomMapping.ParentViewName
                                           select view).First();
                        if (parentView.Enumerations.Any())
                        {
                            sOptionCode = parentView.Enumerations.First().OptionCode;
                        }

                        //if (sOptionCode.Equals("NOTENUMERATED"))
                        //{
                        //sVisibleView = ilbo.GetControlProperty(sParentILBO, sParentControlID, false, "Linkedcomboview");
                        tryBlock.AddStatement(AssignVariable("oMultilineControl", SnippetExpression("(Multiline)IilboHandle.GetControl(sParentControlID)")));

                        methodInvokation = MethodInvocationExp(TypeReferenceExp("oMultilineControl"), "GetNumberOfListItems");
                        AddParameters(methodInvokation, new Object[] { zoomMapping.ChildViewName.Substring(1) });
                        //AddParameters(methodInvokation, new Object[] { (zoomMapping.ChildDisplayFlag.Equals("t") && !String.IsNullOrEmpty(zoomMapping.ChildViewName)) ? zoomMapping.ChildViewName : zoomMapping.ParentViewName });
                        tryBlock.AddStatement(AssignVariable("lListItems", methodInvokation));

                        //for loop                            
                        ForLoop = ForLoopExpression(AssignVariable("lLoop", PrimitiveExpression(1)),
                                                                            BinaryOpertorExpression(VariableReferenceExp("lLoop"), CodeBinaryOperatorType.LessThanOrEqual, VariableReferenceExp("lListItems")),
                                                                            AssignVariable("lLoop", BinaryOpertorExpression(VariableReferenceExp("lLoop"), CodeBinaryOperatorType.Add, PrimitiveExpression(1))));

                        methodInvokation = MethodInvocationExp(TypeReferenceExp("oMultilineControl"), "GetListItemValue");
                        AddParameters(methodInvokation, new Object[] { zoomMapping.ChildViewName.Substring(1), SnippetExpression("lLoop") });
                        //AddParameters(methodInvokation, new Object[] { (zoomMapping.ChildDisplayFlag.Equals("t") && !String.IsNullOrEmpty(zoomMapping.ChildViewName)) ? zoomMapping.ChildViewName : zoomMapping.ParentViewName, SnippetExpression("lLoop") });
                        ForLoop.Statements.Add(AssignVariable("sValue", methodInvokation));

                        tryBlock.AddStatement(ForLoop);

                        //}

                        methodInvokation = MethodInvocationExp(TypeReferenceExp(String.Format("m_con{0}", zoomMapping.ChildControlId)), "AddListItem");
                        AddParameters(methodInvokation, new Object[] { zoomMapping.ChildViewName, SnippetExpression("lLoop"), SnippetExpression("sValue") });
                        ForLoop.Statements.Add(methodInvokation);
                    }
                }
            }

            catch (Exception ex)
            {
                throw new Exception(String.Format("BeginPreProcess1_Zoom->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }

        /// <summary>
        /// Forms method 'BeginPreProcess1'
        /// </summary>
        /// <returns></returns>
        private CodeMemberMethod BeginPreProcess1(String MethodName)
        {
            try
            {
                CodeMethodInvokeExpression methodInvokation;
                CodeMemberMethod BeginPreProcess1 = new CodeMemberMethod
                {
                    Name = MethodName,
                    Attributes = _ilbo.Type.Equals("9") ? (MemberAttributes.Public | MemberAttributes.Final) : (MemberAttributes.Public | MemberAttributes.Override),
                    ReturnType = new CodeTypeReference(typeof(bool))
                };

                // Method Summary
                AddMethodSummary(BeginPreProcess1, "initializes Scripts for Data Transfer");
                // Try Block
                #region try block
                CodeTryCatchFinallyStatement tryBlock = AddTryBlock(BeginPreProcess1);
                tryBlock.AddStatement(AddTraceInfo(MethodName));

                // Base Callout Start
                Generate_CallOut_MethodStart(tryBlock, MethodName);

                if (_ilbo.Publication.Any() || _ilbo.HaseZeeView || _ilbo.Type.Equals("9"))
                {
                    tryBlock.AddStatement(AddISManager());
                    tryBlock.AddStatement(DeclareVariableAndAssign("IDataBroker", "IDBroker", true, MethodInvocationExp(TypeReferenceExp("ISManager"), "GetDataBroker")));
                    //if (!MethodName.ToLower().Equals("preprocess1"))
                    tryBlock.AddStatement(DeclareVariableAndAssign("IObjectBroker", "IobjBroker", true, MethodInvocationExp(TypeReferenceExp("ISManager"), "GetObjectBroker")));
                }

                // For Zoom ILBO
                if (_ilbo.Type.Equals("9"))
                {
                    BeginPreProcess1_Zoom(tryBlock, MethodName);

                    methodInvokation = MethodInvocationExp(TypeReferenceExp("IDBroker"), "Transfer");
                    AddParameters(methodInvokation, new Object[] { GetProperty("TransferDirection", "transferIn"), _activity.Name, _ilbo.Code });
                    tryBlock.AddStatement(methodInvokation);

                    tryBlock.AddStatement(ReturnExpression(PrimitiveExpression(true)));
                }
                else
                {
                    if (_ilbo.Publication.Any() || _ilbo.HaseZeeView)
                    {
                        methodInvokation = MethodInvocationExp(TypeReferenceExp("IDBroker"), "Transfer");
                        AddParameters(methodInvokation, new Object[] { GetProperty("TransferDirection", "transferIn"), _activity.Name, _ilbo.Code });
                        tryBlock.AddStatement(methodInvokation);
                    }

                    // Base Callout End
                    Generate_CallOut_MethodEnd(tryBlock, MethodName);

                    // For iEDK
                    Generate_iEDKMethod(tryBlock, MethodName);
                }
                #endregion try block

                // Catch Block
                AddCatchClause(tryBlock, MethodName);
                return BeginPreProcess1;
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("BeginPreProcess1->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }


        ///// <summary>
        ///// 
        ///// </summary>
        ///// <returns></returns>
        private CodeMemberMethod BeginPreProcess2()
        {
            try
            {
                String sContext = "BeginPreProcess2";
                CodeMethodInvokeExpression methodInvokation;
                CodeMemberMethod BeginPreProcess2 = new CodeMemberMethod
                {
                    Name = sContext,
                    Attributes = MemberAttributes.Public | MemberAttributes.Override,
                    ReturnType = new CodeTypeReference("IAsyncResult")
                };
                BeginPreProcess2.Parameters.Add(ParameterDeclarationExp(new CodeTypeReference("AsyncCallback"), "cb"));
                BeginPreProcess2.Parameters.Add(ParameterDeclarationExp(new CodeTypeReference("VWRequestState"), "reqState"));

                //Method Summary
                AddMethodSummary(BeginPreProcess2, "Initiates Init tasks");

                // Try Block
                CodeTryCatchFinallyStatement tryBlock = AddTryBlock(BeginPreProcess2);
                tryBlock.AddStatement(AddTraceInfo("BeginPreProcess2()"));

                // Base Callout Start
                Generate_CallOut_MethodStart(tryBlock, sContext);

                IEnumerable<TaskService> InitTasks = _ilbo.TaskServiceList.Where(t => t.Type == "initialize" || t.Type == "init");
                if (InitTasks.Any())
                {
                    TaskService InitTask = InitTasks.First();

                    Int32 iServiceType = Convert.ToInt32(object.Equals(InitTask, null) ? 0 : InitTask.ServiceType);

                    tryBlock.AddStatement(SetProperty("reqState", "ServiceName", PrimitiveExpression(InitTask.ServiceName)));
                    tryBlock.AddStatement(SetProperty("reqState", "TransactionScope", iServiceType > 0 ? PrimitiveExpression(0) : PrimitiveExpression(2)));

                    methodInvokation = MethodInvocationExp(ThisReferenceExp(), "BeginExecuteService");
                    AddParameters(methodInvokation, new Object[] { ArgumentReferenceExp("cb"), ArgumentReferenceExp("reqState") });
                    tryBlock.AddStatement(ReturnExpression(methodInvokation));
                }
                else
                {
                    tryBlock.AddStatement(new CodeCommentStatement("No Initialize Tasks are defined for the ILBO."));

                    // For iEDK
                    Generate_iEDKMethod(tryBlock, sContext);

                    tryBlock.AddStatement(ThrowNewException("No Initialization Tasks are defined for the ILBO."));
                }

                // Catch Block
                AddCatchClause(tryBlock, sContext);


                return BeginPreProcess2;
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("BeginPreProcess2->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }


        ///// <summary>
        ///// 
        ///// </summary>
        ///// <returns></returns>
        private CodeMemberMethod BeginPreProcess3()
        {
            try
            {
                String sContext = "BeginPreProcess3";
                CodeMethodInvokeExpression methodInvokation;
                CodeMemberMethod BeginPreProcess3 = new CodeMemberMethod
                {
                    Name = sContext,
                    Attributes = MemberAttributes.Public | MemberAttributes.Override,
                    ReturnType = new CodeTypeReference("IAsyncResult")
                };

                //Method Summary
                AddMethodSummary(BeginPreProcess3, "Initiates Fetch tasks");

                //Method Parameters
                BeginPreProcess3.Parameters.Add(ParameterDeclarationExp(new CodeTypeReference("AsyncCallback"), "cb"));
                BeginPreProcess3.Parameters.Add(ParameterDeclarationExp(new CodeTypeReference("VWRequestState"), "reqState"));

                // Try Block
                CodeTryCatchFinallyStatement tryBlock = AddTryBlock(BeginPreProcess3);
                tryBlock.AddStatement(AddTraceInfo("BeginPreProcess3()"));

                // Base Call Out Start
                Generate_CallOut_MethodStart(tryBlock, sContext);

                IEnumerable<TaskService> FetchTasks = _ilbo.TaskServiceList.Where(t => t.Type.ToLower() == "fetch");
                if (FetchTasks.Any())
                {
                    TaskService FetchTask = FetchTasks.First();

                    Int32 iServiceType = Convert.ToInt32(object.Equals(FetchTask, null) ? 0 : FetchTask.ServiceType);

                    tryBlock.AddStatement(SetProperty("reqState", "ServiceName", PrimitiveExpression(FetchTask.ServiceName)));
                    tryBlock.AddStatement(SetProperty("reqState", "TransactionScope", iServiceType > 0 ? PrimitiveExpression(0) : PrimitiveExpression(iServiceType)));

                    methodInvokation = MethodInvocationExp(ThisReferenceExp(), "BeginExecuteService");
                    AddParameters(methodInvokation, new Object[] { ArgumentReferenceExp("cb"), ArgumentReferenceExp("reqState") });
                    tryBlock.AddStatement(ReturnExpression(methodInvokation));
                }
                else
                {
                    tryBlock.AddStatement(new CodeCommentStatement("No Fetch Tasks are defined for the ILBO."));

                    // For iEDK
                    Generate_iEDKMethod(tryBlock, sContext);

                    tryBlock.AddStatement(ThrowNewException("No Fetch Tasks are defined for the ILBO."));
                }

                // Base Callout End
                //Generate_CallOut_MethodEnd(tryBlock, sContext);

                // Catch Block
                AddCatchClause(tryBlock, sContext);

                return BeginPreProcess3;
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("BeginPreProcess3->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }


        ///// <summary>
        ///// Generates 'EndPreProcess2' function
        ///// </summary>
        ///// <returns></returns>
        private CodeMemberMethod EndPreProcess2()
        {
            try
            {
                String sContext = "EndPreProcess2";
                CodeMethodInvokeExpression methodInvocation;
                CodeMemberMethod EndPreProcess2 = new CodeMemberMethod
                {
                    Name = sContext,
                    Attributes = MemberAttributes.Public | MemberAttributes.Override,
                    ReturnType = new CodeTypeReference(typeof(bool))
                };
                EndPreProcess2.Parameters.Add(ParameterDeclarationExp(new CodeTypeReference("IAsyncResult"), "ar"));

                // Method Summary
                AddMethodSummary(EndPreProcess2, "Initiates Init tasks");

                // Local Variable Declaration
                EndPreProcess2.Statements.Add(CodeDomHelper.DeclareVariableAndAssign(typeof(bool), "bReturn", true, false));

                // Try Block
                CodeTryCatchFinallyStatement tryBlock = AddTryBlock(EndPreProcess2);
                tryBlock.AddStatement(AddTraceInfo(String.Format("{0}()", EndPreProcess2.Name)));

                // Base Callout Start
                Generate_CallOut_MethodStart(tryBlock, sContext);

                tryBlock.AddStatement(AddISManager());
                tryBlock.AddStatement(DeclareVariableAndAssign("IDataBroker", "IDBroker", true, MethodInvocationExp(TypeReferenceExp("ISManager"), "GetDataBroker")));

                // For Init Task
                IEnumerable<TaskService> InitTasks = _ilbo.TaskServiceList.Where(t => t.Type.ToLower() == "initialize");
                if (InitTasks.Any())
                {
                    methodInvocation = MethodInvocationExp(ThisReferenceExp(), "EndExecuteService");
                    AddParameters(methodInvocation, new Object[] { ArgumentReferenceExp("ar") });
                    tryBlock.AddStatement(AssignVariable("bReturn", methodInvocation));
                }

                //
                //if (_ilbo.HasBaseCallout)
                //{
                //    tryBlock.AddStatement(MethodInvocationExp(VariableReferenceExp("objActBaseEx"), "PreProcess2_After_ExecuteService").AddParameter(SnippetExpression("ref htContextItems")));
                //}

                Generate_iEDKMethod(tryBlock, sContext);

                // For Published Links
                if (_ilbo.Publication.Any())
                {
                    methodInvocation = MethodInvocationExp(TypeReferenceExp("IDBroker"), "Transfer");
                    AddParameters(methodInvocation, new Object[] { GetProperty("TransferDirection", "transferIn"), _activity.Name, _ilbo.Code });
                    tryBlock.AddStatement(methodInvocation);
                }

                // Base Callout End
                Generate_CallOut_MethodEnd(tryBlock, sContext);

                // Return Statement
                if (InitTasks.Any())
                    tryBlock.AddStatement(ReturnExpression(VariableReferenceExp("bReturn")));
                else
                    tryBlock.AddStatement(ReturnExpression(PrimitiveExpression(true)));

                // Catch Block
                AddCatchClause(tryBlock, sContext);

                return EndPreProcess2;
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("EndPreProcess2->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }


        ///// <summary>
        ///// Generates EndPreProcess3 function
        ///// </summary>
        ///// <returns></returns>
        private CodeMemberMethod EndPreProcess3()
        {
            try
            {
                String sContext = "EndPreProcess3";
                CodeMethodInvokeExpression methodInvocation;
                CodeMemberMethod EndPreProcess3 = new CodeMemberMethod
                {
                    Name = sContext,
                    Attributes = MemberAttributes.Public | MemberAttributes.Override,
                    ReturnType = new CodeTypeReference(typeof(bool))
                };
                EndPreProcess3.Parameters.Add(ParameterDeclarationExp(new CodeTypeReference("IAsyncResult"), "ar"));

                // Method Summary
                AddMethodSummary(EndPreProcess3, "Initiates Fetch tasks");

                // Local Variable Declaration
                EndPreProcess3.Statements.Add(CodeDomHelper.DeclareVariableAndAssign(typeof(bool), "bReturn", true, false));

                // Try Block
                CodeTryCatchFinallyStatement tryBlock = AddTryBlock(EndPreProcess3);
                tryBlock.AddStatement(AddTraceInfo(String.Format("{0}()", EndPreProcess3.Name)));

                // Base Callout Start
                Generate_CallOut_MethodStart(tryBlock, sContext);

                // For Fetch Task
                IEnumerable<TaskService> FetchTasks = _ilbo.TaskServiceList.Where(t => t.Type.ToLower() == "fetch");
                if (FetchTasks.Any())
                {
                    TaskService FetchTask = FetchTasks.First();

                    methodInvocation = MethodInvocationExp(ThisReferenceExp(), "EndExecuteService");
                    AddParameters(methodInvocation, new Object[] { ArgumentReferenceExp("ar") });
                    tryBlock.AddStatement(AssignVariable("bReturn", methodInvocation));

                    if (_ilbo.HasBaseCallout)
                        tryBlock.AddStatement(MethodInvocationExp(VariableReferenceExp("objActBaseEx"), "PreProcess3_After_ExecuteService").AddParameter(SnippetExpression("ref htContextItems")));
                }
                else
                {
                    tryBlock.AddStatement(new CodeCommentStatement("No Fetch Tasks are defined for the ILBO."));
                    tryBlock.AddStatement(AssignVariable(VariableReferenceExp("bReturn"), PrimitiveExpression(true)));
                    Generate_iEDKMethod(tryBlock, sContext);
                }

                // For Dynamic ILBO Title
                if (_ilbo.HasDynamicILBOTitle)
                {
                    methodInvocation = new CodeMethodInvokeExpression(ThisReferenceExp(), "SetContextValue");
                    AddParameters(methodInvocation, new Object[] { "ICT_ILBODESCRIPTION", new CodeMethodInvokeExpression(TypeReferenceExp("m_contxtvw_rt_ilbo_title"), "GetControlValue", PrimitiveExpression("txtvw_rt_ilbo_title")) });
                    tryBlock.AddStatement(methodInvocation);
                }

                // Base Callout End
                //Generate_CallOut_MethodEnd(tryBlock, sContext);

                // Return Statement
                tryBlock.AddStatement(ReturnExpression(VariableReferenceExp("bReturn")));

                // Catch Block
                AddCatchClause(tryBlock, sContext);

                return EndPreProcess3;
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("EndPreProcess3->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }


        ///// <summary>
        ///// 
        ///// </summary>
        ///// <returns></returns>
        private CodeMemberMethod GetContextValue()
        {
            String sContext = "GetContextValue";
            CodeMemberMethod GetContextValue = new CodeMemberMethod
            {
                Name = sContext,
                Attributes = _ilbo.Type.Equals("9") ? (MemberAttributes.Public | MemberAttributes.Final) : (MemberAttributes.Public | MemberAttributes.Override),
                ReturnType = new CodeTypeReference(typeof(Object))
            };

            try
            {

                //method summary
                AddMethodSummary(GetContextValue, "Gets the Context Value");

                //method parameters
                GetContextValue.Parameters.Add(ParameterDeclarationExp(new CodeTypeReference(typeof(String)), "sContextName"));

                //try block
                CodeTryCatchFinallyStatement tryBlock = AddTryBlock(GetContextValue);
                tryBlock.AddStatement(AddTraceInfo(SnippetExpression("String.Format(\"" + sContext + "(sContextName = \\\"{0}\\\")\", sContextName)")));

                if (_ilbo.HasBaseCallout)
                    tryBlock.AddStatement(MethodInvocationExp(VariableReferenceExp("objActBaseEx"), "GetContextValue_Start").AddParameters(new CodeExpression[] { SnippetExpression("ref sContextName"), SnippetExpression("ref  htContextItems") }));

                CodeConditionStatement OuterConditionalStatement = new CodeConditionStatement();
                CodeBinaryOperatorExpression condition1 = new CodeBinaryOperatorExpression(ArgumentReferenceExp("sContextName"), CodeBinaryOperatorType.IdentityInequality, PrimitiveExpression("ICT_PARENTOU"));
                CodeBinaryOperatorExpression condition2 = new CodeBinaryOperatorExpression(ArgumentReferenceExp("sContextName"), CodeBinaryOperatorType.IdentityInequality, PrimitiveExpression("ICT_PARENTCOMPONENT"));
                CodeBinaryOperatorExpression condition3 = new CodeBinaryOperatorExpression(ArgumentReferenceExp("sContextName"), CodeBinaryOperatorType.IdentityInequality, PrimitiveExpression("ICT_LAUNCHINGOU"));
                CodeBinaryOperatorExpression overAllCondition = new CodeBinaryOperatorExpression(condition1, CodeBinaryOperatorType.BooleanAnd, condition2);
                overAllCondition = new CodeBinaryOperatorExpression(overAllCondition, CodeBinaryOperatorType.BooleanAnd, condition3);
                OuterConditionalStatement.Condition = overAllCondition;
                tryBlock.AddStatement(OuterConditionalStatement);

                if (!_ilbo.Type.Equals("9"))
                {
                    CodeConditionStatement InnerConditionalStatement = new CodeConditionStatement();
                    InnerConditionalStatement.Condition = new CodeMethodInvokeExpression(TypeReferenceExp("htContextItems"), "ContainsKey", ArgumentReferenceExp("sContextName"));
                    InnerConditionalStatement.TrueStatements.Add(ReturnExpression(SnippetExpression("htContextItems[sContextName]")));
                    InnerConditionalStatement.FalseStatements.Add(ReturnExpression(SnippetExpression("null")));
                    OuterConditionalStatement.TrueStatements.Add(InnerConditionalStatement);
                }
                else
                {
                    OuterConditionalStatement.TrueStatements.Add(ReturnExpression(SnippetExpression("htContextItems[sContextName]")));
                }

                CodeConditionStatement conditionalStatement = new CodeConditionStatement(new CodeBinaryOperatorExpression(ArgumentReferenceExp("sContextName"), CodeBinaryOperatorType.IdentityEquality, PrimitiveExpression("ICT_PARENTCOMPONENT")));
                conditionalStatement.TrueStatements.Add(ReturnExpression(new CodeMethodInvokeExpression(TypeReferenceExp("m_conrvwrt_cctxt_component"), "GetControlValue", PrimitiveExpression("rvwrt_cctxt_component"))));
                tryBlock.AddStatement(conditionalStatement);

                conditionalStatement = new CodeConditionStatement(new CodeBinaryOperatorExpression(ArgumentReferenceExp("sContextName"), CodeBinaryOperatorType.IdentityEquality, PrimitiveExpression("ICT_PARENTOU")));
                conditionalStatement.TrueStatements.Add(ReturnExpression(new CodeMethodInvokeExpression(TypeReferenceExp("m_conrvwrt_cctxt_ou"), "GetControlValue", PrimitiveExpression("rvwrt_cctxt_ou"))));
                tryBlock.AddStatement(conditionalStatement);

                conditionalStatement = new CodeConditionStatement(new CodeBinaryOperatorExpression(ArgumentReferenceExp("sContextName"), CodeBinaryOperatorType.IdentityEquality, PrimitiveExpression("ICT_LAUNCHINGOU")));
                conditionalStatement.TrueStatements.Add(ReturnExpression(new CodeMethodInvokeExpression(TypeReferenceExp("m_conrvwrt_lctxt_ou"), "GetControlValue", PrimitiveExpression("rvwrt_lctxt_ou"))));
                tryBlock.AddStatement(conditionalStatement);

                if (_ilbo.HasBaseCallout)
                    tryBlock.AddStatement(MethodInvocationExp(VariableReferenceExp("objActBaseEx"), "GetContextValue_End").AddParameters(new CodeExpression[] { SnippetExpression("ref sContextName"), SnippetExpression("ref htContextItems") }));

                tryBlock.AddStatement(ReturnExpression(SnippetExpression("null")));

                // Catch Block
                AddCatchClause(tryBlock, sContext);
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("GetContextValue->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }

            return GetContextValue;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private CodeMemberMethod SetContextValue()
        {
            String sContext = "SetContextValue";
            CodeMemberMethod SetContextValue = new CodeMemberMethod
            {
                Name = sContext,
                Attributes = _ilbo.Type.Equals("9") ? (MemberAttributes.Public | MemberAttributes.Final) : (MemberAttributes.Public | MemberAttributes.Override),
            };

            try
            {
                //method summary
                AddMethodSummary(SetContextValue, "Adds/Sets the Context Value to the collection based on the contextname");

                //method parameters
                SetContextValue.Parameters.Add(ParameterDeclarationExp(new CodeTypeReference(typeof(String)), "sContextName"));
                SetContextValue.Parameters.Add(ParameterDeclarationExp(new CodeTypeReference(typeof(Object)), "sContextValue"));

                //try block
                CodeTryCatchFinallyStatement tryBlock = AddTryBlock(SetContextValue);
                tryBlock.AddStatement(AddTraceInfo(SnippetExpression("String.Format(\"" + sContext + "(sContextName = \\\"{0}\\\", sContextValue = \\\"{1}\\\")\", sContextName, sContextValue)")));

                if (_ilbo.HasBaseCallout)
                    tryBlock.AddStatement(MethodInvocationExp(VariableReferenceExp("objActBaseEx"), "SetContextValue_Start").AddParameters(new CodeExpression[] { SnippetExpression("ref sContextName"), SnippetExpression("ref sContextValue"), SnippetExpression("ref htContextItems") }));

                CodeConditionStatement ConditionalStatement = new CodeConditionStatement();
                CodeBinaryOperatorExpression condition1 = new CodeBinaryOperatorExpression(ArgumentReferenceExp("sContextName"), CodeBinaryOperatorType.IdentityInequality, PrimitiveExpression("ICT_PARENTOU"));
                CodeBinaryOperatorExpression condition2 = new CodeBinaryOperatorExpression(ArgumentReferenceExp("sContextName"), CodeBinaryOperatorType.IdentityInequality, PrimitiveExpression("ICT_PARENTCOMPONENT"));
                CodeBinaryOperatorExpression condition3 = new CodeBinaryOperatorExpression(ArgumentReferenceExp("sContextName"), CodeBinaryOperatorType.IdentityInequality, PrimitiveExpression("ICT_LAUNCHINGOU"));
                CodeBinaryOperatorExpression overAllCondition = new CodeBinaryOperatorExpression(condition1, CodeBinaryOperatorType.BooleanAnd, condition2);
                overAllCondition = new CodeBinaryOperatorExpression(overAllCondition, CodeBinaryOperatorType.BooleanAnd, condition3);
                ConditionalStatement.Condition = overAllCondition;
                ConditionalStatement.TrueStatements.Add(CodeDomHelper.AssignVariable("htContextItems", ArgumentReferenceExp("sContextValue"), -1, true, "sContextName"));
                tryBlock.AddStatement(ConditionalStatement);

                CodeConditionStatement conditionalStatement = new CodeConditionStatement(new CodeBinaryOperatorExpression(ArgumentReferenceExp("sContextName"), CodeBinaryOperatorType.IdentityEquality, PrimitiveExpression("ICT_PARENTCOMPONENT")));
                conditionalStatement.TrueStatements.Add(new CodeMethodInvokeExpression(TypeReferenceExp("m_conrvwrt_cctxt_component"), "SetControlValue", PrimitiveExpression("rvwrt_cctxt_component"), SnippetExpression("(string)sContextValue")));
                conditionalStatement.TrueStatements.Add(ReturnExpression());
                tryBlock.AddStatement(conditionalStatement);

                conditionalStatement = new CodeConditionStatement(new CodeBinaryOperatorExpression(ArgumentReferenceExp("sContextName"), CodeBinaryOperatorType.IdentityEquality, PrimitiveExpression("ICT_PARENTOU")));
                conditionalStatement.TrueStatements.Add(new CodeMethodInvokeExpression(TypeReferenceExp("m_conrvwrt_cctxt_ou"), "SetControlValue", PrimitiveExpression("rvwrt_cctxt_ou"), SnippetExpression("(string)sContextValue")));
                conditionalStatement.TrueStatements.Add(ReturnExpression());
                tryBlock.AddStatement(conditionalStatement);

                conditionalStatement = new CodeConditionStatement(new CodeBinaryOperatorExpression(ArgumentReferenceExp("sContextName"), CodeBinaryOperatorType.IdentityEquality, PrimitiveExpression("ICT_LAUNCHINGOU")));
                conditionalStatement.TrueStatements.Add(new CodeMethodInvokeExpression(TypeReferenceExp("m_conrvwrt_lctxt_ou"), "SetControlValue", PrimitiveExpression("rvwrt_lctxt_ou"), SnippetExpression("(string)sContextValue")));
                conditionalStatement.TrueStatements.Add(ReturnExpression());
                tryBlock.AddStatement(conditionalStatement);

                if (_ilbo.HasBaseCallout)
                    tryBlock.AddStatement(MethodInvocationExp(VariableReferenceExp("objActBaseEx"), "SetContextValue_End").AddParameters(new CodeExpression[] { SnippetExpression("ref sContextName"), SnippetExpression("ref sContextValue"), SnippetExpression("ref htContextItems") }));

                // Catch Block
                AddCatchClause(tryBlock, sContext);
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("SetContextValue->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }

            return SetContextValue;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private CodeMemberMethod GetTaskData()
        {
            String sContext = "GetTaskData";
            try
            {
                CodeMethodInvokeExpression methodInvokation;
                CodeMemberMethod GetTaskData = new CodeMemberMethod
                {
                    Name = sContext,
                    Attributes = _ilbo.Type.Equals("9") ? (MemberAttributes.Public | MemberAttributes.Final) : (MemberAttributes.Public | MemberAttributes.Override),
                };
                GetTaskData.Parameters.Add(ParameterDeclarationExp(typeof(String), "sTabName"));
                GetTaskData.Parameters.Add(ParameterDeclarationExp(typeof(String), "sTaskName"));
                GetTaskData.Parameters.Add(ParameterDeclarationExp(typeof(System.Xml.XmlNode), "nodeScreenInfo"));

                // Method Summary
                AddMethodSummary(GetTaskData, "Gets the Task specific data");

                // Local Variable Declaration
                GetTaskData.Statements.Add(DeclareVariableAndAssign(typeof(String), "sOutMTD", true, GetProperty(typeof(String), "Empty")));
                if (_ilbo.HasLegacyState && !_ilbo.HasRTState)
                {
                    GetTaskData.Statements.Add(DeclareVariable(typeof(bool), "bProcessState"));
                }

                // Try Block
                CodeTryCatchFinallyStatement tryBlock = AddTryBlock(GetTaskData);
                tryBlock.AddStatement(AddTraceInfo(SnippetExpression("String.Format(\"" + sContext + "(sTabName = \\\"{0}\\\", sTaskName = \\\"{1}\\\", nodeScreenInfo = \\\"{2}\\\")\", sTabName, sTaskName, nodeScreenInfo.OuterXml)")));

                // Base Callout Start
                Generate_CallOut_MethodStart(tryBlock, sContext);

                // Check for InLine Tab
                methodInvokation = MethodInvocationExp(TypeReferenceExp(typeof(String)), "CompareOrdinal");
                AddParameters(methodInvokation, new Object[] { SnippetExpression("GetContextValue(\"ICT_INLINE_TAB\") as String"), "1" });
                CodeConditionStatement checkInlineTab = new CodeConditionStatement
                {
                    Condition = new CodeBinaryOperatorExpression(methodInvokation, CodeBinaryOperatorType.IdentityEquality, PrimitiveExpression(0))
                };
                checkInlineTab.TrueStatements.Add(AddISManager());
                checkInlineTab.TrueStatements.Add(AddISMContext());
                checkInlineTab.TrueStatements.Add(AddISExecutor());
                checkInlineTab.TrueStatements.Add(DeclareVariableAndAssign(typeof(System.Xml.XmlDocument), "xmlDom", true, PropertyReferenceExp(TypeReferenceExp("nodeScreenInfo"), "OwnerDocument")));
                checkInlineTab.TrueStatements.Add(DeclareVariable(typeof(System.Xml.XmlElement), "eltContextName"));
                checkInlineTab.TrueStatements.Add(DeclareVariableAndAssign(typeof(System.Xml.XmlElement), "eltIlboInfo", true, SnippetExpression("null")));
                checkInlineTab.TrueStatements.Add(DeclareVariableAndAssign(typeof(System.Xml.XmlElement), "eltDTabs", true, SnippetExpression("null")));


                // Control Information
                methodInvokation = MethodInvocationExp(BaseReferenceExp(), "GetControlInfoElement");
                AddParameters(methodInvokation, new Object[] { VariableReferenceExp("xmlDom"), ArgumentReferenceExp("nodeScreenInfo") });
                if (_ilbo.Type == "9")
                    checkInlineTab.TrueStatements.Add(DeclareVariableAndAssign(typeof(System.Xml.XmlElement), "eltControlInfo", true, SnippetExpression("null")));
                else
                    checkInlineTab.TrueStatements.Add(DeclareVariableAndAssign(typeof(System.Xml.XmlElement), "eltControlInfo", true, methodInvokation));

                if (_ilbo.Type != "9")
                {
                    // Layout Control Information
                    methodInvokation = MethodInvocationExp(BaseReferenceExp(), "GetLayoutControlInfoElement");
                    AddParameters(methodInvokation, new Object[] { VariableReferenceExp("xmlDom"), ArgumentReferenceExp("nodeScreenInfo") });
                    checkInlineTab.TrueStatements.Add(DeclareVariableAndAssign(typeof(System.Xml.XmlElement), "eltLayoutControlInfo", true, methodInvokation));
                }

                if (_ilbo.Type == "9")
                {
                    checkInlineTab.TrueStatements.Add(CommentStatement("Form control information"));
                    methodInvokation = MethodInvocationExp(TypeReferenceExp("xmlDom"), "SelectSingleNode");
                    AddParameters(methodInvokation, new Object[] { "trpi/scri/ci" });
                    CodeConditionStatement checkCtrlNode = IfCondition();
                    checkInlineTab.TrueStatements.Add(checkCtrlNode);
                    checkCtrlNode.Condition = new CodeBinaryOperatorExpression(methodInvokation, CodeBinaryOperatorType.IdentityEquality, SnippetExpression("null"));
                    checkCtrlNode.TrueStatements.Add(AssignVariable(VariableReferenceExp("eltControlInfo"), MethodInvocationExp(VariableReferenceExp("xmlDom"), "CreateElement").AddParameter(PrimitiveExpression("ci"))));
                    checkCtrlNode.TrueStatements.Add(MethodInvocationExp(VariableReferenceExp("nodeScreenInfo"), "AppendChild").AddParameter(VariableReferenceExp("eltControlInfo")));
                    checkCtrlNode.FalseStatements.Add(AssignVariable(VariableReferenceExp("eltControlInfo"), SnippetExpression("xmlDom.SelectSingleNode(\"trpi/scri/ci\") as XmlElement")));
                }

                // ILBO Node
                methodInvokation = MethodInvocationExp(TypeReferenceExp("xmlDom"), "SelectSingleNode");
                AddParameters(methodInvokation, new Object[] { "trpi/scri/ii" });
                CodeConditionStatement checkILBONode = new CodeConditionStatement
                {
                    Condition = new CodeBinaryOperatorExpression(methodInvokation, CodeBinaryOperatorType.IdentityEquality, SnippetExpression("null"))
                };
                methodInvokation = MethodInvocationExp(TypeReferenceExp("xmlDom"), "CreateElement");
                AddParameters(methodInvokation, new Object[] { "ii" });
                checkILBONode.TrueStatements.Add(AssignVariable("eltIlboInfo", methodInvokation));
                methodInvokation = MethodInvocationExp(TypeReferenceExp("nodeScreenInfo"), "AppendChild");
                AddParameters(methodInvokation, new Object[] { SnippetExpression("eltIlboInfo") });
                checkILBONode.TrueStatements.Add(methodInvokation);
                checkILBONode.FalseStatements.Add(AssignVariable("eltIlboInfo", SnippetExpression("xmlDom.SelectSingleNode(\"trpi/scri/ii\") as XmlElement")));
                checkInlineTab.TrueStatements.Add(checkILBONode);

                // For Legacy State
                if (_ilbo.HasLegacyState && _ilbo.HasRTState == false)
                {
                    methodInvokation = MethodInvocationExp(VariableReferenceExp("ctrlState"), "UpdateTaskDataState");
                    AddParameters(methodInvokation, new Object[] { _ilbo.Code, VariableReferenceExp("sTabName"), VariableReferenceExp("sTaskName"), PrimitiveExpression("USER"), VariableReferenceExp("ref nodeScreenInfo"), VariableReferenceExp("htContextItems") });
                    checkInlineTab.TrueStatements.Add(AssignVariable(VariableReferenceExp("bProcessState"), methodInvokation));
                }

                if (_ilbo.TaskServiceList.Any())
                {
                    checkInlineTab.TrueStatements.Add(SnippetStatement("switch(sTaskName.ToLower())"));
                    checkInlineTab.TrueStatements.Add(SnippetStatement("{"));
                    foreach (IGrouping<string, TaskService> taskGrp in _ilbo.TaskServiceList.Where(ts => (ts.Type != "init" && ts.Type != "initialize" && ts.Type != "fetch") && ts.ReportInfo == null).OrderBy(ts => ts.Name).GroupBy(ts => ts.Name))
                    //foreach (IGrouping<string, TaskService> taskGrp in _ilbo.TaskServiceList.Where(ts => ts.ReportInfo == null).OrderBy(ts => ts.ServiceName).GroupBy(ts => ts.Name))
                    {
                        TaskService task = taskGrp.First();
                        checkInlineTab.TrueStatements.Add(SnippetStatement(String.Format("case \"{0}\":", task.Name)));

                        #region mainscreen controls
                        foreach (TaskData taskdata in task.TaskInfos.Where(ti => ti.TabName == "mainpage"))
                        {
                            checkInlineTab.TrueStatements.Add(AddRenderAsXML(taskdata.Control, taskdata.Control.IsLayoutControl, taskdata.Filling.Equals("1")));
                        }

                        foreach (CodeMethodInvokeExpression expression in GetTaskData_PlfRichControls(task, "mainpage"))
                            checkInlineTab.TrueStatements.Add(expression);
                        #endregion

                        #region other than mainscreen controls

                        //grouping tabs under the task
                        var TabGroups = task.TaskInfos.Where(ti => ti.TabName != "mainpage").GroupBy(ti => ti.TabName);
                        if (TabGroups.Count() > 0)
                        {

                            checkInlineTab.TrueStatements.Add(SnippetStatement("switch(sTabName.ToLower())"));
                            checkInlineTab.TrueStatements.Add(SnippetStatement("{"));

                            foreach (IGrouping<String, TaskData> TabGroup in TabGroups)
                            {
                                checkInlineTab.TrueStatements.Add(SnippetStatement(String.Format("case \"{0}\":", TabGroup.Key)));
                                foreach (TaskData taskinfo in TabGroup)
                                {
                                    checkInlineTab.TrueStatements.Add(AddRenderAsXML(taskinfo.Control, taskinfo.Control.IsLayoutControl, taskinfo.Filling.Equals("1")));
                                }

                                foreach (CodeMethodInvokeExpression expression in GetTaskData_PlfRichControls(task, TabGroup.Key))
                                {
                                    checkInlineTab.TrueStatements.Add(expression);
                                }

                                foreach (CodeMethodInvokeExpression expression in GetTaskData_AddDirtyTab(task, TabGroup.Key, false))
                                {
                                    checkInlineTab.TrueStatements.Add(expression);
                                }

                                checkInlineTab.TrueStatements.Add(SnippetExpression("break")); //break for each tab
                            }

                            // Default Case
                            checkInlineTab.TrueStatements.Add(SnippetStatement("default :"));
                            foreach (CodeMethodInvokeExpression expression in GetTaskData_AddDirtyTab(task, String.Empty, true))
                            {
                                checkInlineTab.TrueStatements.Add(expression);
                            }
                            checkInlineTab.TrueStatements.Add(SnippetExpression("break"));

                            checkInlineTab.TrueStatements.Add(SnippetStatement("}"));//closing tab switch
                        }
                        #endregion

                        checkInlineTab.TrueStatements.Add(SnippetExpression("break"));//break for each task
                    }


                    // For Report Task
                    GetTaskData_ReportTask(tryBlock, checkInlineTab);

                    // Switch Case Closing
                    checkInlineTab.TrueStatements.Add(SnippetStatement("}"));//closing task switch
                }

                // For Extended Tasks
                GetTaskData_ExtendedLinkMethods(checkInlineTab);

                // Base Call Out End
                if (_ilbo.HasBaseCallout)
                {
                    checkInlineTab.TrueStatements.Add(MethodInvocationExp(VariableReferenceExp("objActBaseEx"), "GetTaskData_End").AddParameters(new CodeExpression[] { SnippetExpression("ref sTabName"), SnippetExpression("ref sTaskName"), SnippetExpression("ref nodeScreenInfo") }));
                }

                if (_ilbo.Type != "9")
                {
                    methodInvokation = MethodInvocationExp(BaseReferenceExp(), "GetTaskData");
                    AddParameters(methodInvokation, new Object[] { ArgumentReferenceExp("sTabName"), ArgumentReferenceExp("sTaskName"), ArgumentReferenceExp("nodeScreenInfo") });
                    checkInlineTab.TrueStatements.Add(methodInvokation);
                }

                methodInvokation = MethodInvocationExp(ThisReferenceExp(), "ObsoleteGetTaskData");
                AddParameters(methodInvokation, new Object[] { ArgumentReferenceExp("sTabName"), ArgumentReferenceExp("sTaskName"), ArgumentReferenceExp("nodeScreenInfo") });
                checkInlineTab.FalseStatements.Add(methodInvokation);
                tryBlock.AddStatement(checkInlineTab);

                // Catch Block
                AddCatchClause(tryBlock, sContext);

                return GetTaskData;
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("GetTaskData->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }

        private void GetTaskData_ExtendedLinkMethods_Cmn(CodeConditionStatement checkInlineTab, String InvkCondition, String InvkObject)
        {
            try
            {
                CodeConditionStatement ifCondition = new CodeConditionStatement
                {
                    Condition = FieldReferenceExp(ThisReferenceExp(), InvkCondition)
                };

                CodeMethodInvokeExpression newMethodInvokation = MethodInvocationExp(TypeReferenceExp(InvkObject), "GetTaskData");
                AddParameter(newMethodInvokation, "(IILBO) this");
                AddParameter(newMethodInvokation, "sTabName");
                AddParameter(newMethodInvokation, "sTaskName");
                AddParameter(newMethodInvokation, "eltControlInfo");

                ifCondition.TrueStatements.Add(newMethodInvokation);
                checkInlineTab.TrueStatements.Add(ifCondition);
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("GetTaskData_ExtendedLinkMethods_Cmn->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }


        private void GetTaskData_ExtendedLinkMethods(CodeConditionStatement checkInlineTab)
        {
            try
            {
                String CalledMethodName = String.Empty;

                // For Control Extensions
                if (_ilbo.HasControlExtensions)
                {
                    GetTaskData_ExtendedLinkMethods_Cmn(checkInlineTab, "bCE", "oCE");
                }

                // For Dynamic Links
                if (_ilbo.HasDynamicLink)
                {
                    GetTaskData_ExtendedLinkMethods_Cmn(checkInlineTab, "bLink", "oDlink");
                }

                // For Message Lookup
                if (_ilbo.HasMessageLookup)
                {
                    GetTaskData_ExtendedLinkMethods_Cmn(checkInlineTab, "bMsgLkup", "oErrorlookup");
                }
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("GetTaskData_ExtendedLinkMethods->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private CodeMemberMethod AddDirtyTab()
        {
            try
            {
                String sContext = "AddDirtyTab";
                CodeMemberMethod AddDirtyTab = new CodeMemberMethod
                {
                    Name = "AddDirtyTab",
                    Attributes = MemberAttributes.Private
                };
                AddDirtyTab.Parameters.Add(ParameterDeclarationExp(new CodeTypeReference("XmlDocument"), "xmlDom"));
                AddDirtyTab.Parameters.Add(ParameterDeclarationExp(new CodeTypeReference("XmlElement"), "eltDTabs"));
                AddDirtyTab.Parameters.Add(ParameterDeclarationExp(typeof(String), "sTabName"));

                // Method Summary
                AddMethodSummary(AddDirtyTab, "Add DirtyTab");

                #region tryBlock
                //CodeTryCatchFinallyStatement tryBlock = AddTryBlock(AddDirtyTab);

                AddDirtyTab.AddStatement(AddTraceInfo(SnippetExpression("String.Format(\"" + sContext + "(sTabName = \\\"{0}\\\")\", sTabName)")));

                AddDirtyTab.AddStatement(DeclareVariableAndAssign(typeof(System.Xml.XmlElement), "eltIlboInfo", true, SnippetExpression("xmlDom.SelectSingleNode(\"trpi/scri/ii\") as XmlElement")));

                // For eltDTabs
                AddDirtyTab.AddStatement(AssignVariable("eltDTabs", SnippetExpression("xmlDom.SelectSingleNode(\"//dtabs\") as XmlElement")));
                CodeConditionStatement checkeltDTabs = new CodeConditionStatement
                {
                    Condition = new CodeBinaryOperatorExpression(VariableReferenceExp("eltDTabs"), CodeBinaryOperatorType.IdentityEquality, SnippetExpression("null"))
                };
                AddDirtyTab.AddStatement(checkeltDTabs);
                CodeMethodInvokeExpression methodInvokation = MethodInvocationExp(ArgumentReferenceExp("xmlDom"), "CreateElement");
                AddParameters(methodInvokation, new Object[] { "dtabs" });
                checkeltDTabs.TrueStatements.Add(AssignVariable("eltDTabs", methodInvokation));

                methodInvokation = MethodInvocationExp(TypeReferenceExp("eltIlboInfo"), "AppendChild");
                AddParameters(methodInvokation, new Object[] { SnippetExpression("eltDTabs") });
                checkeltDTabs.TrueStatements.Add(methodInvokation);

                // For eltDTab
                AddDirtyTab.AddStatement(DeclareVariableAndAssign(typeof(System.Xml.XmlElement), "eltDTab", true, SnippetExpression("xmlDom.SelectSingleNode(\"//dtabs/t[@n='\" + sTabName + \"']\") as XmlElement")));
                CodeConditionStatement checkeltDTab = new CodeConditionStatement
                {
                    Condition = new CodeBinaryOperatorExpression(VariableReferenceExp("eltDTab"), CodeBinaryOperatorType.IdentityEquality, SnippetExpression("null"))
                };
                AddDirtyTab.AddStatement(checkeltDTab);
                methodInvokation = MethodInvocationExp(ArgumentReferenceExp("xmlDom"), "CreateElement");
                AddParameters(methodInvokation, new Object[] { "t" });
                checkeltDTab.TrueStatements.Add(AssignVariable("eltDTab", methodInvokation));

                methodInvokation = MethodInvocationExp(TypeReferenceExp("eltDTab"), "SetAttribute");
                AddParameters(methodInvokation, new Object[] { "n", SnippetExpression("sTabName") });
                checkeltDTab.TrueStatements.Add(methodInvokation);

                methodInvokation = MethodInvocationExp(TypeReferenceExp("eltDTabs"), "AppendChild");
                AddParameters(methodInvokation, new Object[] { SnippetExpression("eltDTab") });
                checkeltDTab.TrueStatements.Add(methodInvokation);
                #endregion

                // Catch Block
                //AddCatchClause(tryBlock, sContext);

                return AddDirtyTab;
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("AddDirtyTab->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }

        //private void ObsoleteGetTaskData_Link(CodeTryCatchFinallyStatement tryBlock, String sTabName, DataTable dtTaskControls)
        //{
        //    try
        //    {
        //        // For Link
        //        foreach (DataRow dtLinkWithoutChild in ilbo.dtLinkWithoutChilds.Rows)
        //        {
        //            String sTaskName = Convert.ToString(dtLinkWithoutChild["taskname"]);

        //            tryBlock.AddStatement(SnippetStatement(String.Format("case \"{0}\":", sTaskName)));

        //            StringBuilder sbQuery = new StringBuilder();
        //            sbQuery.AppendLine("select distinct a.ilbocode, a.taskname, a.controlid, isnull(b.tabname, '') 'tabname', max(c.instanceflag) 'Filling', b.type ");
        //            sbQuery.AppendLine(" from #fw_des_ilbo_service_view_datamap a(nolock), #fw_req_ilbo_control b (nolock), #fw_des_service_segment c(nolock), ");
        //            sbQuery.AppendLine(" #fw_des_service_dataitem d(nolock) where a.activityid = @ActID and a.ilbocode = @ILBOCode and a.taskname = @TaskName ");
        //            sbQuery.AppendLine(" and d.flowattribute in (1,2) and b.ilbocode = a.ilbocode and b.controlid = a.controlid  and c.servicename = a.servicename ");
        //            sbQuery.AppendLine(" and c.segmentname = a.segmentname and d.servicename = c.servicename and d.segmentname = c.segmentname and d.dataitemname= a.dataitemname ");
        //            sbQuery.AppendLine(" group by a.ilbocode, a.taskname, tabname, a.controlid, b.type order by a.ilbocode, a.taskname, tabname, a.controlid, b.type");
        //            GlobalVar.var parameters = dbManager.CreateParameters(3);
        //            GlobalVar.dbManager.AddParamters(parameters,0, "@ActID", ilbo.ActivityId);
        //            GlobalVar.dbManager.AddParamters(parameters,1, "@ILBOCode", ilbo.Name);
        //            GlobalVar.dbManager.AddParamters(parameters,2, "@TaskName", sTaskName);
        //            DataTable dtLinkCtrls = GlobalVar.dbManager.ExecuteDataTable(CommandType.Text, sbQuery.ToString());

        //            foreach (DataRow dtLinkCtrl in dtLinkCtrls.Rows)
        //            {
        //                String sControlID = Convert.ToString(dtLinkCtrl["controlid"]);
        //                String sControlType = Convert.ToString(dtLinkCtrl["type"]);
        //                String sFilling = Convert.ToString(dtLinkCtrl["Filling"]);
        //                bool IsLayoutControl = sControlType.Equals("2");

        //                tryBlock.AddStatement(AddRenderAsXML(sControlID, IsLayoutControl, true));
        //            }

        //            tryBlock.AddStatement(SnippetExpression("break"));
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        throw new Exception(String.Format("ObsoleteGetTaskData_Link->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
        //    }
        //}

        //private void ObsoleteGetTaskData_HelpLink(CodeTryCatchFinallyStatement tryBlock, String sTabName, DataTable dtTaskControls)
        //{
        //    try
        //    {
        //        // For Help
        //        String spName = "de_IL_GetHelpLinkTasksCntDtl_SP";
        //        GlobalVar.var parameters = dbManager.CreateParameters(4);
        //        GlobalVar.dbManager.AddParamters(parameters,0, "@ActID", ilbo.ActivityId);
        //        GlobalVar.dbManager.AddParamters(parameters,1, "@ILBOCode", ilbo.Name);
        //        GlobalVar.dbManager.AddParamters(parameters,2, "@TabName", sTabName);
        //        GlobalVar.dbManager.AddParamters(parameters,3, "@EcrNo", GlobalVar.Ecrno);
        //        DataTable dtTaskGroups = GlobalVar.dbManager.ExecuteDataTable(CommandType.StoredProcedure, spName);
        //        IEnumerable<IGrouping<String, DataRow>> TaskGroups = dtTaskGroups.Select().AsEnumerable().OrderBy(row => row.Field<String>("taskname")).GroupBy(row => row.Field<String>("taskname"));

        //        foreach (IGrouping<String, DataRow> taskGroup in TaskGroups)
        //        {
        //            String sTaskName = taskGroup.Key.ToLower();
        //            tryBlock.AddStatement(SnippetStatement(String.Format("case \"{0}\":", sTaskName)));

        //            var dtTabTaskControlInfo = dtTaskGroups.Select(String.Format("taskname='{0}'", sTaskName));
        //            DataTable dtTabTaskControls = dtTabTaskControlInfo.Any() ? dtTabTaskControlInfo.CopyToDataTable() : dtTaskControls.Clone();
        //            foreach (DataRow dr in dtTabTaskControls.Rows)
        //            {
        //                String sControlID = Convert.ToString(dr["controlid"]);
        //                String sControlType = Convert.ToString(dr["type"]);
        //                bool IsLayoutControl = sControlType.Equals("2");

        //                tryBlock.AddStatement(AddRenderAsXML(sControlID, IsLayoutControl, true));
        //            }
        //            tryBlock.AddStatement(SnippetExpression("break"));
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        throw new Exception(String.Format("ObsoleteGetTaskData_HelpLink->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
        //    }
        //}

        //private void ObsoleteGetTaskData_SubmitTransUI(CodeTryCatchFinallyStatement tryBlock, String sTabName, DataTable dtTaskControls)
        //{
        //    try
        //    {
        //        // For Submit, Trans & UI within Tab Page
        //        String spName = "de_il_GetTaskControlFillingForTabs";
        //        GlobalVar.var parameters = dbManager.CreateParameters(4);
        //        GlobalVar.dbManager.AddParamters(parameters,0, "@ActID", ilbo.ActivityId);
        //        GlobalVar.dbManager.AddParamters(parameters,1, "@ILBOCode", ilbo.Name);
        //        GlobalVar.dbManager.AddParamters(parameters,2, "@TabName", sTabName);
        //        GlobalVar.dbManager.AddParamters(parameters,3, "@EcrNo", GlobalVar.Ecrno);
        //        DataTable dtTaskGroups = GlobalVar.dbManager.ExecuteDataTable(CommandType.StoredProcedure, spName);
        //        IEnumerable<IGrouping<String, DataRow>> TaskGroups = dtTaskGroups.Select().AsEnumerable().OrderBy(row => row.Field<String>("taskname")).GroupBy(row => row.Field<String>("taskname"));

        //        foreach (IGrouping<String, DataRow> taskGroup in TaskGroups)
        //        {
        //            String sTaskName = taskGroup.Key.ToLower();
        //            tryBlock.AddStatement(SnippetStatement(String.Format("case \"{0}\":", sTaskName)));

        //            var dtTabTaskControlInfo = dtTaskGroups.Select(String.Format("taskname='{0}'", sTaskName));
        //            DataTable dtTabTaskControls = dtTabTaskControlInfo.Any() ? dtTabTaskControlInfo.CopyToDataTable() : dtTaskControls.Clone();
        //            foreach (DataRow dr in dtTabTaskControls.Rows)
        //            {
        //                String sControlID = Convert.ToString(dr["controlid"]);
        //                String sControlType = Convert.ToString(dr["type"]);
        //                String sFilling = Convert.ToString(dr["Filling"]);
        //                bool IsLayoutControl = sControlType.Equals("2");

        //                tryBlock.AddStatement(AddRenderAsXML(sControlID, IsLayoutControl, sFilling.Equals("1")));
        //            }
        //            tryBlock.AddStatement(SnippetExpression("break"));
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        throw new Exception(String.Format("ObsoleteGetTaskData_SubmitTransUI->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
        //    }
        //}

        /// <summary>
        /// Generates ObsoleteGetTaskData Method
        /// </summary>
        /// <returns></returns>
        private CodeMemberMethod ObsoleteGetTaskData()
        {
            try
            {
                String sContext = "ObsoleteGetTaskData";
                //CodeMethodInvokeExpression methodInvokation;
                CodeMemberMethod ObsoleteGetTaskData = new CodeMemberMethod
                {
                    Name = sContext
                };
                ObsoleteGetTaskData.Parameters.Add(ParameterDeclarationExp(new CodeTypeReference(typeof(System.String)), "sTabName"));
                ObsoleteGetTaskData.Parameters.Add(ParameterDeclarationExp(new CodeTypeReference(typeof(System.String)), "sTaskName"));
                ObsoleteGetTaskData.Parameters.Add(ParameterDeclarationExp(new CodeTypeReference("XmlNode"), "nodeScreenInfo"));

                // Method Summary
                AddMethodSummary(ObsoleteGetTaskData, "Gets the Task specific data");

                //#region try block
                CodeTryCatchFinallyStatement tryBlock = AddTryBlock(ObsoleteGetTaskData);
                tryBlock.AddStatement(AddTraceInfo(SnippetExpression("String.Format(\"" + sContext + "(sTabName = \\\"{0}\\\", sTaskName = \\\"{1}\\\", nodeScreenInfo = \\\"{2}\\\")\", sTabName, sTaskName, nodeScreenInfo.OuterXml)")));
                //tryBlock.AddStatement((AddISManager()));
                //tryBlock.AddStatement((AddISMContext()));
                //tryBlock.AddStatement(DeclareVariableAndAssign(typeof(System.Xml.XmlDocument), "xmlDom", true, PropertyReferenceExp(TypeReferenceExp("nodeScreenInfo"), "OwnerDocument")));

                //// Control Info
                //tryBlock.AddStatement(CodeDomHelper.DeclareVariableAndAssign(typeof(XmlElement), "eltControlInfo", true, SnippetExpression("xmlDom.SelectSingleNode(\"trpi/scri/ci\") as XmlElement")));
                //CodeConditionStatement chkControlInfo = new CodeConditionStatement(new CodeBinaryOperatorExpression(VariableReferenceExp("eltControlInfo"), CodeBinaryOperatorType.IdentityEquality, SnippetExpression("null")));
                //tryBlock.AddStatement(chkControlInfo);
                //chkControlInfo.TrueStatements.Add(AssignVariable("eltControlInfo", new CodeMethodInvokeExpression(TypeReferenceExp("xmlDom"), "CreateElement").AddParameters(new Object[] { PrimitiveExpression("ci") })));
                //chkControlInfo.TrueStatements.Add(new CodeMethodInvokeExpression(TypeReferenceExp("nodeScreenInfo"), "AppendChild").AddParameters(new Object[] { VariableReferenceExp("eltControlInfo") }));

                //// For Legacy State
                //if (ilbo.HasStateCtrl)
                //{
                //    methodInvokation = MethodInvocationExp(TypeReferenceExp("ctrlState"), "UpdateTaskDataState");
                //    AddParameters(methodInvokation, new Object[] { ilbo.Name, SnippetExpression("sTabName"), SnippetExpression("sTaskName"), PrimitiveExpression("USER"), SnippetExpression("ref nodeScreenInfo"), SnippetExpression("htContextItems") });
                //    tryBlock.AddStatement(DeclareVariableAndAssign(typeof(bool), "bProcessState",true, methodInvokation));
                //}

                //String spName = "vw_netgen_gettaskdata_controls_sp";
                //GlobalVar.var parameters = dbManager.CreateParameters(2);
                //GlobalVar.dbManager.AddParamters(parameters,0, "@activity_id", ilbo.ActivityId);
                //GlobalVar.dbManager.AddParamters(parameters,1, "@ilbocode", ilbo.Name);
                //DataTable dtTaskControls = GlobalVar.dbManager.ExecuteDataTable(CommandType.StoredProcedure, spName);

                //IEnumerable<IGrouping<String, DataRow>> TabGroups = dtTaskControls.Select().OrderBy(row => row.Field<String>("tabname")).ThenBy(row => row.Field<String>("taskname")).GroupBy(row => row.Field<String>("tabname")) ;
                //// For Each Tab Pages
                //tryBlock.AddStatement(SnippetStatement("switch(sTabName.ToLower())"));
                //tryBlock.AddStatement(SnippetStatement("{"));
                //foreach (IGrouping<String, DataRow> TabGroup in TabGroups)
                //{
                //    Object CodeBlock = tryBlock;
                //    String sTabName = TabGroup.Key;
                //    if (sTabName.Equals(String.Empty))
                //    {
                //        tryBlock.AddStatement(SnippetStatement("default:"));
                //        tryBlock.AddStatement(SnippetStatement("if(sTabName == String.Empty)"));
                //        tryBlock.AddStatement(SnippetStatement("{"));
                //    }
                //    else
                //    {
                //        tryBlock.AddStatement(SnippetStatement(String.Format("case \"{0}\":", sTabName)));
                //    }

                //    tryBlock.AddStatement(SnippetStatement("switch(sTaskName.ToLower())"));
                //    tryBlock.AddStatement(SnippetStatement("{"));

                //    // For Link Without Child
                //    ObsoleteGetTaskData_Link(tryBlock, sTabName, dtTaskControls);

                //    // For Submit, Trans & UI Task
                //    ObsoleteGetTaskData_SubmitTransUI(tryBlock, sTabName, dtTaskControls);

                //    // For Help, Link
                //    ObsoleteGetTaskData_HelpLink(tryBlock, sTabName, dtTaskControls);

                //    tryBlock.AddStatement(SnippetStatement("}"));

                //    if (sTabName.Equals(String.Empty)) 
                //        tryBlock.AddStatement(SnippetStatement("}"));

                //    tryBlock.AddStatement(SnippetExpression("break"));
                //}

                //tryBlock.AddStatement(SnippetStatement("}"));
                //tryBlock.AddStatement(MethodInvocationExp(BaseReferenceExp(), "GetTaskData").AddParameters(new object[] { SnippetExpression("sTabName"), SnippetExpression("sTaskName"), SnippetExpression("nodeScreenInfo") }));
                //#endregion

                // Catch Block
                AddCatchClause(tryBlock, sContext);

                return ObsoleteGetTaskData;
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("ObsoleteGetTaskData->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private CodeMemberMethod GetDisplayURL()
        {
            try
            {
                String sContext = "GetDisplayURL";
                String sLaunchILBO = String.Empty;
                CodeMemberMethod GetDisplayURL = new CodeMemberMethod
                {
                    Name = sContext,
                    Attributes = _ilbo.Type.Equals("9") ? (MemberAttributes.Public | MemberAttributes.Final) : (MemberAttributes.Public | MemberAttributes.Override),
                    ReturnType = new CodeTypeReference(typeof(String))
                };
                GetDisplayURL.Parameters.Add(ParameterDeclarationExp(typeof(String), "sTabName"));

                // Method Summary
                AddMethodSummary(GetDisplayURL, "Gets the display URL based on the tab name");

                // Try Block
                CodeTryCatchFinallyStatement tryBlock = AddTryBlock(GetDisplayURL);
                tryBlock.AddStatement(AddTraceInfo(SnippetExpression("String.Format(\"" + sContext + "(sTabName = \\\"{0}\\\")\", sTabName)")));

                // Base Callout Start
                Generate_CallOut_MethodStart(tryBlock, sContext);

                // Switch Case Starting
                tryBlock.AddStatement(SnippetStatement("switch(sTabName.ToLower())"));
                tryBlock.AddStatement(SnippetStatement("{"));

                sLaunchILBO = GetLaunchILBO("mainpage");
                tryBlock.AddStatement(SnippetStatement("default:"));
                CodeConditionStatement bTabCheck = new CodeConditionStatement(new CodeBinaryOperatorExpression(VariableReferenceExp("sTabName"), CodeBinaryOperatorType.IdentityEquality, GetProperty(typeof(String), "Empty")));
                tryBlock.AddStatement(bTabCheck);
                bTabCheck.TrueStatements.Add(ReturnExpression(PrimitiveExpression(sLaunchILBO)));

                IEnumerable<Page> pages = _ilbo.TabPages.Where(t => t.Name != "mainpage");
                foreach (Page page in pages)
                {
                    sLaunchILBO = GetLaunchILBO(page.Name);
                    tryBlock.AddStatement(SnippetStatement(String.Format("case \"{0}\":", page.Name)));
                    tryBlock.AddStatement(ReturnExpression(PrimitiveExpression(sLaunchILBO)));
                }

                if (_ilbo.Type.Equals("9"))
                {
                    bTabCheck.FalseStatements.Add(CodeDomHelper.ThrowNewException("Invalid TabName"));
                }
                else
                {
                    Generate_iEDKMethod(tryBlock, sContext, bTabCheck);
                }

                // Switch Case Closing
                tryBlock.AddStatement(SnippetStatement("}"));

                // Base Callout End
                Generate_CallOut_MethodEnd(tryBlock, sContext);

                // Catch Block
                AddCatchClause(tryBlock, sContext);

                // Return Statement
                GetDisplayURL.Statements.Add(ReturnExpression(PrimitiveExpression(String.Empty)));
                return GetDisplayURL;
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("GetDisplayURL->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }


        /// <summary>
        /// Generates ExecuteService Method.
        /// </summary>
        /// <returns></returns>
        private CodeMemberMethod ExecuteService()
        {
            try
            {
                String sContext = "ExecuteService";
                CodeMemberMethod ExecuteService = new CodeMemberMethod
                {
                    Name = sContext,
                    Attributes = _ilbo.Type.Equals("9") ? (MemberAttributes.Family | MemberAttributes.Final) : (MemberAttributes.Family | MemberAttributes.Override),
                    ReturnType = new CodeTypeReference(typeof(bool))
                };

                // Method Summary
                AddMethodSummary(ExecuteService, "executes services for Init, Fetch, Trans and Submit tasks");
                ExecuteService.Parameters.Add(ParameterDeclarationExp(new CodeTypeReference(typeof(System.String)), "sServiceName"));

                CodeTryCatchFinallyStatement tryBlock = AddTryBlock(ExecuteService);
                tryBlock.AddStatement(AddTraceInfo(SnippetExpression("String.Format(\"" + sContext + "(sServiceName = \\\"{0}\\\")\", sServiceName)")));

                ExecuteService_Cmn(tryBlock, sContext);

                if (_ilbo.TaskServiceList.Any())
                {
                    // For iEDK
                    Generate_iEDKMethod(tryBlock, String.Format("{0}{1}{2}", sContext, "_", "Final"));
                    tryBlock.AddStatement(CodeDomHelper.ThrowNewException("Invalid Service"));
                }
                else
                {
                    tryBlock.AddStatement(ReturnExpression(PrimitiveExpression(true)));
                }

                // Catch Block
                AddCatchClause(tryBlock, sContext);

                return ExecuteService;
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("ExecuteService->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }


        private CodeExpressionCollection SetServiceProperty(Segment segment)
        {
            CodeExpressionCollection expressions = new CodeExpressionCollection();


            foreach (DataItem dataitem in segment.DataItems.Where(d => d.DiType == "property"))
            {
                CodeMethodInvokeExpression MethodInvokation;
                String sMethodName = String.Empty;

                CodePropertyReferenceExpression PropertyType = new CodePropertyReferenceExpression();
                PropertyType.TargetObject = TypeReferenceExp("DIPropertyType");

                CodePropertyReferenceExpression PropertyName = PropertyReferenceExp(TypeReferenceExp("PropertyNames"), dataitem.PropertyName);

                CodePropertyReferenceExpression FlowAttribute = new CodePropertyReferenceExpression();
                FlowAttribute.TargetObject = TypeReferenceExp("FlowAttribute");

                #region flowattribute
                switch (dataitem.FlowDirection)
                {
                    case "0":
                        FlowAttribute.PropertyName = "flowIn";
                        break;
                    case "1":
                        FlowAttribute.PropertyName = "flowOut";
                        break;
                    default:
                        FlowAttribute.PropertyName = "flowInOut";
                        break;
                }
                #endregion flowattribute

                #region methodname & propertytype
                if (dataitem.Control.IsLayoutControl)
                {
                    if (string.Compare(dataitem.PropertyType, "tab", true) == 0)
                    {
                        sMethodName = "SetServiceTabPropertySource";
                        PropertyType.PropertyName = "Control";
                    }
                    else
                    {
                        sMethodName = "SetServiceLayoutPropertySource";
                        PropertyType.PropertyName = dataitem.PropertyType;
                    }
                }
                else
                {
                    sMethodName = "SetServicePropertySource";
                    PropertyType.PropertyName = dataitem.PropertyType;
                }
                #endregion methodname

                MethodInvokation = MethodInvocationExp(TypeReferenceExp("ISExecutor"), sMethodName);
                AddParameters(MethodInvokation, new Object[] { dataitem.Name, FlowAttribute, dataitem.Control.Id });

                if (dataitem.Control.IsLayoutControl == false)
                    AddParameters(MethodInvokation, new Object[] { dataitem.View != null ? dataitem.View.Name : dataitem.Control.Id });
                AddParameters(MethodInvokation, new Object[] { PropertyType, PropertyName });

                expressions.Add(MethodInvokation);
            }

            return expressions;
        }


        //private String isChartTask(String sServiceName)
        //{
        //    try
        //    {
        //        StringBuilder sbQuery = new StringBuilder();
        //        sbQuery.AppendLine("select 'x' from de_published_chart_task_map_vw (nolock) where customername = @customername and projectname = @projectname and ecrno = @ecrno and servicename = @servicename");
        //        GlobalVar.var parameters = dbManager.CreateParameters(4);
        //        GlobalVar.dbManager.AddParamters(parameters,0, "@customername", GlobalVar.Customer);
        //        GlobalVar.dbManager.AddParamters(parameters,1, "@projectname", GlobalVar.Project);
        //        GlobalVar.dbManager.AddParamters(parameters,2, "@ecrno", GlobalVar.Ecrno);
        //        GlobalVar.dbManager.AddParamters(parameters,3, "@servicename", sServiceName);
        //        return (String)GlobalVar.dbManager.ExecuteScalar(CommandType.Text, sbQuery.ToString());
        //    }
        //    catch (Exception ex)
        //    {
        //        throw new Exception(String.Format("isChartTask->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
        //    }
        //}

        //private String isTreeTask(String sServiceName)
        //{
        //    try
        //    {
        //        StringBuilder sbQuery = new StringBuilder();
        //        sbQuery.AppendLine("select segmentname from #fw_des_service_segment(nolock) where segmentname ='tree_data_segment' and servicename= @servicename");
        //        GlobalVar.var parameters = dbManager.CreateParameters(1);
        //        GlobalVar.dbManager.AddParamters(parameters,0, "@servicename", sServiceName);
        //        return (String)GlobalVar.dbManager.ExecuteScalar(CommandType.Text, sbQuery.ToString());
        //    }
        //    catch (Exception ex)
        //    {
        //        throw new Exception(String.Format("isTreeTask->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
        //    }
        //}

        //private String isRichControlTask(String sServiceName)
        //{
        //    try
        //    {
        //        StringBuilder sbQuery = new StringBuilder();
        //        sbQuery.AppendLine("select sectiontype, lower(controlid) as controlid from #fw_extjs_control_dtl where uiname = @ilbocode and servicename = @servicename");
        //        GlobalVar.var parameters = dbManager.CreateParameters(2);
        //        GlobalVar.dbManager.AddParamters(parameters,0, "@ilbocode", ilbo.Name);
        //        GlobalVar.dbManager.AddParamters(parameters,1, "@servicename", sServiceName);
        //        return (String)GlobalVar.dbManager.ExecuteScalar(CommandType.Text, sbQuery.ToString());
        //    }
        //    catch (Exception ex)
        //    {
        //        throw new Exception(String.Format("isTreeTask->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
        //    }
        //}

        //private String GetSegmentDIFlowDirection(String sServiceName, String sSegmentName)
        //{
        //    String sSegFlowDir = String.Empty;
        //    try
        //    {
        //        String spName = "de_il_GetSegFlowDir_SP";
        //        GlobalVar.var parameters = dbManager.CreateParameters(3);
        //        GlobalVar.dbManager.AddParamters(parameters,0, "@ServiceName", sServiceName);
        //        GlobalVar.dbManager.AddParamters(parameters,1, "@SegmentName", sSegmentName);
        //        GlobalVar.dbManager.AddParamters(parameters,2, "@EcrNo", GlobalVar.Ecrno);
        //        String spReturnValue = (String)GlobalVar.dbManager.ExecuteScalar(CommandType.StoredProcedure, spName);
        //        sSegFlowDir = (String.IsNullOrEmpty(spReturnValue) || spReturnValue.ToLower().Equals("null")) ? String.Empty : spReturnValue.ToString();
        //    }
        //    catch (Exception ex)
        //    {
        //        throw new Exception(String.Format("GetSegmentDIFlowDirection->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
        //    }
        //    return sSegFlowDir;
        //}

        //private bool GetSegmentFlowDirection(String sServiceName, String sSegmentName, String sTaskName, String sTaskType, bool bIsMultiInstance)
        //{
        //    try
        //    {
        //        String spName = String.Empty;
        //        StringBuilder sbQuery = new StringBuilder();
        //        String spReturnValue = String.Empty;
        //        bool bSegFilling = false;

        //        if (bIsMultiInstance && (sTaskType.Equals("ui") || sTaskType.Equals("trans") || sTaskType.Equals("link")))
        //        {
        //            spName = "de_il_GetSegFilling_SP";
        //            GlobalVar.var parameters = dbManager.CreateParameters(5);
        //            GlobalVar.dbManager.AddParamters(parameters,0, "@ActID", ilbo.ActivityId);
        //            GlobalVar.dbManager.AddParamters(parameters,1, "@ServiceName", sServiceName);
        //            GlobalVar.dbManager.AddParamters(parameters,2, "@SegmentName", sSegmentName);
        //            GlobalVar.dbManager.AddParamters(parameters,3, "@TaskName", sTaskName);
        //            GlobalVar.dbManager.AddParamters(parameters,4, "@Ecrno", GlobalVar.Ecrno);

        //            spReturnValue = (String)GlobalVar.dbManager.ExecuteScalar(CommandType.StoredProcedure, spName);
        //            bSegFilling = (String.IsNullOrEmpty(spReturnValue) || spReturnValue.ToLower().Equals("null")) ? false : (spReturnValue.ToString().Equals(Boolean.TrueString) ? true : false);
        //        }
        //        else if (sTaskType.Equals("fetch"))
        //        {
        //            sbQuery.Clear();
        //            GlobalVar.var parameters = dbManager.CreateParameters(4);
        //            GlobalVar.dbManager.AddParamters(parameters,0, "@activityid", ilbo.ActivityId);
        //            GlobalVar.dbManager.AddParamters(parameters,1, "@ilbocode", ilbo.Name);
        //            GlobalVar.dbManager.AddParamters(parameters,2, "@servicename", sServiceName);
        //            GlobalVar.dbManager.AddParamters(parameters,3, "@segmentname", sSegmentName);
        //            sbQuery.AppendLine("select distinct a.ilbocode, a.servicename, a.segmentname, b.controlid, a.combofill from");
        //            sbQuery.AppendLine("#fw_des_task_segment_attribs a(nolock),#fw_des_ilbo_service_view_datamap b(nolock),");
        //            sbQuery.AppendLine("#fw_req_ilbo_control c(nolock) where a.activityid = @activityid and");
        //            sbQuery.AppendLine("a.ilbocode = @ilbocode and a.servicename = @servicename and");
        //            sbQuery.AppendLine("a.segmentname = @segmentname and b.activityid = a.activityid and  b.ilbocode= a.ilbocode");
        //            sbQuery.AppendLine("and b.servicename = a.servicename and b.segmentname = a.segmentname and c.ilbocode = b.ilbocode");
        //            sbQuery.AppendLine("and c.controlid = b.controlid and c.type = 'rslistedit'");
        //            DataTable dtFilling = GlobalVar.dbManager.ExecuteDataTable(CommandType.Text, sbQuery.ToString());

        //            //bSegFilling = (dtFilling.Rows.Count > 0) ? (dtFilling.Rows[0]["combofill"].ToString().ToLower().Equals(Boolean.TrueString.ToLower()) ? true : false) : false;
        //            bSegFilling = (dtFilling.Rows.Count > 0) ? (dtFilling.Rows[0]["combofill"].ToString().Equals("1") ? true : false) : false;
        //        }
        //        else if (sTaskType.Equals("initialize") && bIsMultiInstance)
        //        {
        //            bSegFilling = true;
        //        }
        //        return bSegFilling;
        //    }
        //    catch (Exception ex)
        //    {
        //        throw new Exception(String.Format("GetSegmentFlowDirection->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
        //    }
        //}

        private void ExecuteService_Cmn(CodeTryCatchFinallyStatement tryBlock, String CallingMethod)
        {
            try
            {
                StringBuilder sbQuery = new StringBuilder();
                CodeMethodInvokeExpression methodInvokation;

                // Local Variable Declaration
                tryBlock.AddStatement(DeclareVariableAndAssign(typeof(String), "sOutMTD", true, SnippetExpression("null")));
                tryBlock.AddStatement(DeclareVariableAndAssign(typeof(String), "sTaskName", true, SnippetExpression("String.Empty")));
                tryBlock.AddStatement(DeclareVariableAndAssign(typeof(bool), "bExecFlag", true, SnippetExpression("true")));

                if (_ilbo.HasLegacyState && _ilbo.HasRTState == false)
                {
                    tryBlock.AddStatement(DeclareVariableAndAssign(typeof(bool), "bProcessState", true, SnippetExpression("false")));
                }

                if (_ilbo.TaskServiceList.Where(ts => ts.IsCached == true).Any())
                {
                    tryBlock.AddStatement(DeclareVariableAndAssign(typeof(String), "setKeyPattern", true, PropertyReferenceExp(TypeReferenceExp(typeof(String)), "Empty")));
                    tryBlock.AddStatement(DeclareVariableAndAssign(typeof(String), "clearKeyPattern", true, PropertyReferenceExp(TypeReferenceExp(typeof(String)), "Empty")));
                }

                if (_ilbo.HasRichControl)
                    tryBlock.AddStatement(DeclareVariableAndAssign(typeof(System.Collections.Hashtable), "htExtJSControls", true, new CodeObjectCreateExpression(typeof(System.Collections.Hashtable))));

                // Base Callout Start
                Generate_CallOut_MethodStart(tryBlock, CallingMethod);

                tryBlock.AddStatement(AddISManager());
                tryBlock.AddStatement(AddISExecutor());

                // Task - Service List
                if (_ilbo.TaskServiceList.Any() == false)
                    return;

                // PreServiceProcess
                if (CallingMethod == "BeginExecuteService")
                {
                    methodInvokation = MethodInvocationExp(BaseReferenceExp(), "PreServiceProcess");
                    AddParameters(methodInvokation, new Object[] { GetProperty("reqState", "ServiceName"), GetProperty("reqState", "ServiceName") });
                    tryBlock.AddStatement(AssignVariable("bExecFlag", methodInvokation));
                    tryBlock.AddStatement(SwitchStatement("reqState.ServiceName.ToLower()"));
                }
                else if (CallingMethod == "ExecuteService")
                {
                    tryBlock.AddStatement(SwitchStatement("sServiceName.ToLower()"));
                }

                tryBlock.AddStatement(StartSwitch());

                //For Each Service
                foreach (IGrouping<string, TaskService> serviceGrp in _ilbo.TaskServiceList.Where(ts => ts.ServiceName != String.Empty).OrderBy(ts => ts.ServiceName).GroupBy(ts => ts.ServiceName))
                //foreach (IGrouping<string, TaskService> serviceGrp in _ilbo.TaskServiceList.Where(ts => ts.ServiceName != String.Empty).OrderBy(ts => ts.ServiceName).GroupBy(ts => ts.ServiceName))
                {
                    TaskService taskService = serviceGrp.First();

                    tryBlock.AddStatement(SnippetStatement(String.Format("case \"{0}\":", taskService.ServiceName)));

                    CodeStatementCollection statements = new CodeStatementCollection();

                    statements.Add(AssignVariable("sTaskName", taskService.Name));

                    //Universal Personalization
                    if (string.Compare(CallingMethod, "beginexecuteservice", true) == 0)
                    {
                        if (taskService.HasUniversalPersonalization)
                        {
                            CodeConditionStatement checkForUniversalPersonalization = IfCondition();
                            checkForUniversalPersonalization.Condition = SnippetExpression("string.IsNullOrEmpty(GetContextValue(\"ICT_PARENTILBO\") as string) && (GetContextValue(\"ICT_ENABLE_UNIVERSALPERSONALIZATION\") as string == \"true\")");
                            checkForUniversalPersonalization.TrueStatements.Add(AssignVariable(FieldReferenceExp(VariableReferenceExp("reqState"), "EnableUniversalPersonalization"), PrimitiveExpression(true)));
                            statements.Add(checkForUniversalPersonalization);
                        }
                    }

                    //ISExecutor.SetExecutionContext
                    methodInvokation = MethodInvocationExp(TypeReferenceExp("ISExecutor"), "SetExecutionContext");
                    AddParameters(methodInvokation, new Object[] { taskService.ComponentName, taskService.ServiceName, _activity.Name, _ilbo.Code });
                    statements.Add(methodInvokation);

                    // Is Zipped
                    if (taskService.IsZipped && CallingMethod == "BeginExecuteService")
                    {
                        methodInvokation = MethodInvocationExp(TypeReferenceExp("ISExecutor"), "SetServiceAttributes");
                        AddParameters(methodInvokation, new Object[] { taskService.IsZipped, false, taskService.IsCached });
                        statements.Add(methodInvokation);
                    }

                    // Is Cached
                    if (taskService.IsCached)
                    {
                        statements.Add(AssignVariable("setKeyPattern", taskService.SetKeyPattern));
                        methodInvokation = MethodInvocationExp(ThisReferenceExp(), "SetAddCacheKey"); //doubt - todo
                        AddParameters(methodInvokation, new Object[] { VariableReferenceExp("setKeyPattern"), VariableReferenceExp("ISExecutor"), ArgumentReferenceExp("reqState.ServiceName") });
                        statements.Add(methodInvokation);
                    }

                    // For Clear Key Pattern
                    if (!String.IsNullOrEmpty(taskService.ClearKeyPattern))
                    {
                        statements.Add(AssignVariable("clearKeyPattern", taskService.ClearKeyPattern));
                        methodInvokation = MethodInvocationExp(ThisReferenceExp(), "SetClearCacheKey"); //doubt - todo
                        AddParameters(methodInvokation, new Object[] { VariableReferenceExp("clearKeyPattern"), VariableReferenceExp("ISExecutor") });
                        statements.Add(methodInvokation);
                    }

                    #region for each segment
                    foreach (Segment segment in taskService.Segments)
                    {
                        CodePropertyReferenceExpression SegFlowDirection;

                        SegFlowDirection = new CodePropertyReferenceExpression();
                        SegFlowDirection.TargetObject = TypeReferenceExp("FlowAttribute");
                        switch (segment.FlowDirection.ToLower())
                        {
                            case "0":
                                SegFlowDirection.PropertyName = "flowIn";
                                break;
                            case "1":
                                SegFlowDirection.PropertyName = "flowOut";
                                break;
                            default:
                                SegFlowDirection.PropertyName = "flowInOut";
                                break;
                        }

                        // Set Segment Context
                        if (segment.DataItems.Where(d => d.Control != null).Any())
                        {
                            methodInvokation = MethodInvocationExp(TypeReferenceExp("ISExecutor"), "SetSegmentContext");
                            AddParameters(methodInvokation, new Object[]
                            {
                                segment.Name,
                                segment.Sequence,
                                segment.Inst.Equals("1"),
                                SegFlowDirection,
                                (segment.Inst == "1" && segment.DataItems.Where(d=>d.Control!=null).Select(d => d.Control).Where(c => c.ListEditRequired.Equals(true)).Any()) ? true : segment.ComboFilling
                            });
                            statements.Add(methodInvokation);
                        }

                        //[SetServicePropertySource]/[SetServiceTabPropertySource]/[SetServiceLayoutPropertySource]
                        foreach (CodeExpression expression in SetServiceProperty(segment))
                        {
                            statements.Add(expression);
                        }

                        //For SetServiceDataSource
                        foreach (DataItem dataitem in segment.DataItems.Where(d => d.Control != null && d.DiType == "data"))
                        {

                            CodePropertyReferenceExpression DIFlowDirection = new CodePropertyReferenceExpression();
                            DIFlowDirection.TargetObject = TypeReferenceExp("FlowAttribute");

                            switch (dataitem.FlowDirection)
                            {
                                case "0":
                                    DIFlowDirection.PropertyName = "flowIn";
                                    break;
                                case "1":
                                    DIFlowDirection.PropertyName = "flowOut";
                                    break;
                                default:
                                    DIFlowDirection.PropertyName = "flowInOut";
                                    break;
                            }

                            //if (dataitem.Control.Type.Equals("rslistedit"))
                            //    sControlID = GetListEditControlID(sControlID).ToLower();

                            methodInvokation = MethodInvocationExp(TypeReferenceExp("ISExecutor"), "SetServiceDataSource");
                            if (dataitem.Name.ToLower().Equals("modeflag"))
                            {
                                AddParameters(methodInvokation, new Object[] { dataitem.Name, DIFlowDirection, _ilbo.Code, dataitem.Control.Id, dataitem.Name });
                            }
                            else
                            {
                                AddParameters(methodInvokation, new Object[] { dataitem.Name, DIFlowDirection, _ilbo.Code, dataitem.Control.Id, dataitem.ViewName });
                            }

                            if (dataitem.View != null)
                            {
                                if (dataitem.View.DataType.Equals("numeric"))
                                    AddParameters(methodInvokation, new Object[] { dataitem.View.Precision.ToUpper() });
                                else
                                    AddParameters(methodInvokation, new Object[] { GetProperty("String", "Empty") });
                            }
                            else if (!string.IsNullOrEmpty(dataitem.Control.Precision) && dataitem.Control.DataType.Equals("numeric"))
                            {
                                AddParameters(methodInvokation, new Object[] { dataitem.Control.Precision });
                            }
                            else
                            {
                                AddParameters(methodInvokation, new Object[] { GetProperty("String", "Empty") });
                            }

                            statements.Add(methodInvokation);
                        }
                        #endregion for each dataitem

                        if (CallingMethod.ToLower() == "beginexecuteservice")
                            // For Context DataItems
                            foreach (string dataitemName in segment.DataItems.Where(d => _dictContextDataitem.ContainsKey(d.Name) == true).Select(d => d.Name).Distinct())
                            {
                                methodInvokation = MethodInvocationExp(VariableReferenceExp("ISExecutor"), "SetServiceDataSource");
                                methodInvokation.AddParameters(new CodeExpression[] { PrimitiveExpression(dataitemName), GetProperty("FlowAttribute", "flowIn"), PrimitiveExpression(_ilbo.Code), PrimitiveExpression(String.Format("_con{0}", dataitemName)), PrimitiveExpression(dataitemName), GetProperty("String", "Empty") });
                                statements.Add(methodInvokation);
                            }
                    }

                    // For iEDK
                    if (taskService.Type != "disposal")
                        foreach (CodeStatement statement in Generate_iEDKMethod(CallingMethod))
                        { statements.Add(statement); }

                    // Return Statement
                    if (CallingMethod == "BeginExecuteService")
                    {
                        methodInvokation = MethodInvocationExp(TypeReferenceExp("ISExecutor"), "BeginExecuteService");
                        AddParameters(methodInvokation, new Object[] { ArgumentReferenceExp("cb"), ArgumentReferenceExp("reqState") });
                        statements.Add(ReturnExpression(methodInvokation));
                    }
                    else if (CallingMethod == "ExecuteService")
                    {
                        methodInvokation = MethodInvocationExp(TypeReferenceExp("ISExecutor"), "ExecuteService");

                        //statements.Add(AssignVariable(VariableReferenceExp("bExecFlag"), methodInvokation));

                        if (taskService.Charts.Any() || taskService.Trees.Any() || taskService.HasRichControls)
                        {
                            statements.Add(AssignVariable(VariableReferenceExp("sOutMTD"), MethodInvocationExp(TypeReferenceExp("ISExecutor"), "GetLastOutMTD")));
                            foreach (CodeStatement st in ExecuteService_Ext(taskService, CallingMethod))
                            {
                                statements.Add(st);
                            }
                        }

                        if (taskService.Type != "disposal")
                        {
                            // For Legacy State
                            if (_ilbo.HasRTState == false && _ilbo.HasLegacyState == true)
                            {
                                CodeConditionStatement checkState = new CodeConditionStatement
                                {
                                    Condition = VariableReferenceExp("bExecFlag")
                                };
                                methodInvokation = MethodInvocationExp(TypeReferenceExp("ctrlState"), "ProcessState");
                                AddParameters(methodInvokation, new Object[] { _ecrOptions.Component, _activity.Name, _ilbo.Code, SnippetExpression("sOutMTD"), SnippetExpression("sServiceName") });

                                if (taskService.Type.Equals("init") || taskService.Type.Equals("initialize") || taskService.Type.Equals("fetch"))
                                    methodInvokation.AddParameter(PrimitiveExpression("tskstdscreeninit"));
                                else
                                    methodInvokation.AddParameter(SnippetExpression("sTaskName"));

                                methodInvokation.AddParameter(SnippetExpression("m_conhdnhdnrt_stcontrol.GetControlValue(\"hdnhdnrt_stcontrol\")"));

                                statements.Add(checkState);
                                checkState.TrueStatements.Add(AssignVariable("bProcessState", methodInvokation));
                            }
                        }

                        if (_ilbo.HasBaseCallout)
                            statements.Add(MethodInvocationExp(VariableReferenceExp("objActBaseEx"), "ExecuteService_End").AddParameter(SnippetExpression("ref sServiceName")));

                        statements.Add(ReturnExpression(VariableReferenceExp("bExecFlag")));
                    }

                    //statements.Add(SnippetExpression("break"));


                    if (taskService.Type.Equals("disposal") && CallingMethod.ToLower().Equals("executeservice"))
                    {
                        CodeTryCatchFinallyStatement innerTry = new CodeTryCatchFinallyStatement();

                        CodeConditionStatement ifIsExecutorIsNull = IfCondition();
                        ifIsExecutorIsNull.Condition = BinaryOpertorExpression(VariableReferenceExp("ISExecutor"), CodeBinaryOperatorType.IdentityEquality, SnippetExpression("null"));
                        ifIsExecutorIsNull.TrueStatements.Add(MethodInvocationExp(VariableReferenceExp("ISManager"), "InitializeServiceExecutor"));
                        ifIsExecutorIsNull.TrueStatements.Add(AssignVariable(VariableReferenceExp("ISExecutor"), MethodInvocationExp(VariableReferenceExp("ISManager"), "GetServiceExecutor")));
                        innerTry.AddStatement(ifIsExecutorIsNull);

                        foreach (CodeStatement statement in statements)
                            innerTry.AddStatement(statement);

                        CodeConditionStatement ifIsExecutorIsNotNull = IfCondition();
                        ifIsExecutorIsNotNull.Condition = BinaryOpertorExpression(VariableReferenceExp("ISExecutor"), CodeBinaryOperatorType.IdentityInequality, SnippetExpression("null"));
                        ifIsExecutorIsNotNull.TrueStatements.Add(MethodInvocationExp(VariableReferenceExp("ISManager"), "DisposeServiceExecutor"));
                        innerTry.FinallyStatements.Add(ifIsExecutorIsNotNull);

                        tryBlock.AddStatement(innerTry);
                    }
                    else
                    {
                        foreach (CodeStatement statement in statements)
                            tryBlock.AddStatement(statement);
                    }

                }
                // Switch Case Closing
                tryBlock.AddStatement(EndSwitch());
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("BeginExecuteService_Cmn->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }



        /// <summary>
        /// Forms BeginExecuteService method
        /// </summary>
        /// <returns></returns>
        private CodeMemberMethod BeginExecuteService()
        {
            try
            {
                String sContext = "BeginExecuteService";
                StringBuilder sbQuery = new StringBuilder();

                CodeMemberMethod BeginExecuteService = new CodeMemberMethod
                {
                    Name = sContext,
                    Attributes = MemberAttributes.Family | MemberAttributes.Override,
                    ReturnType = new CodeTypeReference("IAsyncResult")
                };
                BeginExecuteService.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference("AsyncCallback"), "cb"));
                BeginExecuteService.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference("VWRequestState"), "reqState"));

                CodeTryCatchFinallyStatement tryBlock = AddTryBlock(BeginExecuteService);
                tryBlock.AddStatement(AddTraceInfo(SnippetExpression("String.Format(\"" + sContext + "(ServiceName = \\\"{0}\\\")\", reqState.ServiceName)")));

                // Method Summary
                AddMethodSummary(BeginExecuteService, "executes services for Init, Fetch, Trans and Submit tasks");


                if (!_ilbo.TaskServiceList.Any())
                {
                    tryBlock.AddStatement(CodeDomHelper.ThrowNewException("No service available for this ILBO"));
                }
                else
                {
                    ExecuteService_Cmn(tryBlock, "BeginExecuteService");

                    // For iEDK
                    Generate_iEDKMethod(tryBlock, String.Format("{0}{1}{2}", sContext, "_", "Final"));
                    tryBlock.AddStatement(CodeDomHelper.ThrowNewException("Invalid Service"));
                }

                // Catch Block
                AddCatchClause(tryBlock, sContext);
                return BeginExecuteService;
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("BeginExecuteService->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }

        private CodeStatementCollection ExecuteService_Ext(TaskService taskService, string sCallingMethod)
        {
            String sContext = "ExecuteService_Ext";
            CodeStatementCollection statements = new CodeStatementCollection();
            try
            {
                CodeConditionStatement checkExecutionFlg;
                CodeMethodInvokeExpression methodInvokation;

                //tree
                if (taskService.HasTree)
                {
                    checkExecutionFlg = new CodeConditionStatement
                    {
                        Condition = VariableReferenceExp("bExecFlag")
                    };
                    methodInvokation = MethodInvocationExp(TypeReferenceExp(TREECONTROL), "ProcessTree");
                    AddParameters(methodInvokation, new Object[] { SnippetExpression("ref htContextItems"), VariableReferenceExp("sOutMTD") });

                    if (sCallingMethod == "ExecuteService")
                        methodInvokation.AddParameter(VariableReferenceExp("sServiceName"));
                    else
                        methodInvokation.AddParameter(GetProperty("reqState", "ServiceName"));

                    checkExecutionFlg.TrueStatements.Add(methodInvokation);
                    statements.Add(checkExecutionFlg);

                    //foreach (Control control in taskService.RichControls)
                    //{
                    //    tryBlock.AddStatement(MethodInvocationExp(VariableReferenceExp("oPlfRichControls"), "PopulateRichControls").AddParameters(new CodeExpression[] { VariableReferenceExp("htContextItems"), VariableReferenceExp(string.Format("m_con{0}", control.Id)), PrimitiveExpression(control.Id), PrimitiveExpression(control.SectionType) }));
                    //}
                }

                //chart
                if (taskService.HasChart)
                {
                    checkExecutionFlg = new CodeConditionStatement
                    {
                        Condition = new CodeBinaryOperatorExpression(VariableReferenceExp("bExecFlag"), CodeBinaryOperatorType.IdentityEquality, PrimitiveExpression(true))
                    };
                    methodInvokation = MethodInvocationExp(TypeReferenceExp(CHARTCONTROL), "ProcessChart");
                    AddParameters(methodInvokation, new Object[] { SnippetExpression("ref htContextItems"), VariableReferenceExp("sOutMTD") });

                    if (sCallingMethod == "ExecuteService")
                        methodInvokation.AddParameter(VariableReferenceExp("sServiceName"));
                    else
                        methodInvokation.AddParameter(GetProperty("reqState", "ServiceName"));

                    checkExecutionFlg.TrueStatements.Add(methodInvokation);
                    statements.Add(checkExecutionFlg);
                }

                //plf rich controls
                if (taskService.HasRichControls)
                {
                    foreach (Control control in taskService.RichControls)
                    {
                        //String sSectionType = Convert.ToString(drRichControl["sectiontype"]);
                        //String sControlID = Convert.ToString(drRichControl["controlid"]);
                        methodInvokation = MethodInvocationExp(TypeReferenceExp("oPlfRichControls"), "PopulateRichControls");
                        AddParameters(methodInvokation, new Object[] { VariableReferenceExp("htContextItems"), SnippetExpression(String.Format("m_con{0}", control.Id)), control.Id, control.SectionType });
                        statements.Add(methodInvokation);
                    }
                }
                return statements;
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("{0}->{0}", sContext, !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }

        /// <summary>
        /// Forms EndExecuteService method
        /// </summary>
        /// <returns></returns>
        private CodeMemberMethod EndExecuteService()
        {
            String sContext = "EndExecuteService";
            CodeMemberMethod EndExecuteService = new CodeMemberMethod
            {
                Name = sContext,
                Attributes = MemberAttributes.Family | MemberAttributes.Override,
                ReturnType = new CodeTypeReference(typeof(bool))
            };

            try
            {
                String sQuery = String.Empty;
                StringBuilder sbQuery = new StringBuilder();
                CodeMethodInvokeExpression methodInvokation;

                //method parameters
                EndExecuteService.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference("IAsyncResult"), "ar"));

                //method summary
                AddMethodSummary(EndExecuteService, "executes services for Init, Fetch, Trans and Submit tasks");

                //local variable declarations
                if (_ilbo.HasChart || _ilbo.HasTree || _ilbo.HasRichControl || (_ilbo.HasLegacyState && !_ilbo.HasRTState))
                    EndExecuteService.Statements.Add(DeclareVariableAndAssign(typeof(String), "sOutMTD", true, SnippetExpression("null")));

                if (_ilbo.HasLegacyState && !_ilbo.HasRTState)
                    EndExecuteService.AddStatement(DeclareVariable(typeof(bool), "bProcessState"));

                EndExecuteService.Statements.Add(DeclareVariableAndAssign(typeof(bool), "bExecFlag", true, true));
                EndExecuteService.Statements.Add(DeclareVariableAndAssign("VWRequestState", "reqState", true, SnippetExpression("ar.AsyncState as VWRequestState")));
                if (_ilbo.HasRichControl)
                    EndExecuteService.Statements.Add(DeclareVariableAndAssign(typeof(System.Collections.Hashtable), "htExtJSControls", true, new CodeObjectCreateExpression(typeof(System.Collections.Hashtable))));

                #region try block
                CodeTryCatchFinallyStatement tryBlock = AddTryBlock(EndExecuteService);
                tryBlock.AddStatement(AddTraceInfo(SnippetExpression("String.Format(\"" + sContext + "(ServiceName = \\\"{0}\\\")\", reqState.ServiceName)")));

                //local variable declarations and initialization
                tryBlock.AddStatement(AddISManager());
                tryBlock.AddStatement(AddISExecutor());

                tryBlock.AddStatement(SwitchStatement("reqState.ServiceName.ToLower()"));
                tryBlock.AddStatement(StartSwitch());

                IEnumerable<IGrouping<String, TaskService>> ServiceGrp = _ilbo.TaskServiceList.Where(s => s.ServiceName != string.Empty).OrderBy(ts => ts.ServiceName).GroupBy(s => s.ServiceName);
                //IEnumerable<IGrouping<String, TaskService>> ServiceGrp = _ilbo.TaskServiceList.Where(s => s.ServiceName != string.Empty).OrderBy(ts => ts.ServiceName).GroupBy(s => s.ServiceName);

                #region for each service
                foreach (IGrouping<String, TaskService> taskService in ServiceGrp)
                {
                    TaskService service = taskService.First();
                    //bool bServiceHasRichControl = dr["hasRichControl"].Equals(DBNull.Value) ? false : Convert.ToBoolean(dr["hasRichControl"]);

                    tryBlock.AddStatement(CaseStatement(service.ServiceName.ToLower()));

                    //assign value for execution flag
                    methodInvokation = MethodInvocationExp(TypeReferenceExp("ISExecutor"), "EndExecuteService");
                    AddParameters(methodInvokation, new Object[] { ArgumentReferenceExp("ar") });
                    tryBlock.AddStatement(AssignVariable("bExecFlag", methodInvokation));

                    //process tree/chart based on execution flag value                    
                    if (service.HasChart || service.HasTree || service.HasRichControls)
                    {
                        tryBlock.AddStatement(AssignVariable(VariableReferenceExp("sOutMTD"), MethodInvocationExp(TypeReferenceExp("ISExecutor"), "GetLastOutMTD")));
                        foreach (CodeStatement st in ExecuteService_Ext(service, "EndExecuteService"))
                        {
                            tryBlock.AddStatement(st);
                        }
                    }
                    if (_ilbo.HasLegacyState && _ilbo.HasRTState == false)
                    {
                        CodeConditionStatement ifExecutionFlgIsTrue = IfCondition();
                        ifExecutionFlgIsTrue.Condition = VariableReferenceExp("bExecFlag");
                        methodInvokation = MethodInvocationExp(VariableReferenceExp("ctrlState"), "ProcessState").AddParameters(new CodeExpression[] { PrimitiveExpression(_ecrOptions.Component.ToLower()), PrimitiveExpression(_activity.Name), PrimitiveExpression(_ilbo.Code), VariableReferenceExp("sOutMTD"), GetProperty("reqState", "ServiceName") });

                        if (service.Type == "init" || service.Type == "initialize" || service.Type == "fetch")
                            methodInvokation.AddParameter(PrimitiveExpression("tskstdscreeninit"));
                        else
                            methodInvokation.AddParameter(GetProperty("reqState", "TaskName"));

                        methodInvokation.AddParameters(new CodeExpression[] { MethodInvocationExp(VariableReferenceExp("m_conhdnhdnrt_stcontrol"), "GetControlValue").AddParameter(PrimitiveExpression("hdnhdnrt_stcontrol")) });
                        ifExecutionFlgIsTrue.TrueStatements.Add(AssignVariable(VariableReferenceExp("bProcessState"), methodInvokation));
                        tryBlock.AddStatement(ifExecutionFlgIsTrue);
                    }
                    tryBlock.AddStatement(SnippetExpression("break")); //close braces for the if/elseif condition for each service
                }
                #endregion for each service

                tryBlock.AddStatement(SnippetStatement("default:"));

                // For iEDK
                Generate_iEDKMethod(tryBlock, sContext);

                tryBlock.AddStatement(SnippetExpression("break"));

                tryBlock.AddStatement(SnippetStatement("}"));

                //post service process
                CodeConditionStatement checkForPostServiceProcess = new CodeConditionStatement(VariableReferenceExp("bExecFlag"));
                methodInvokation = MethodInvocationExp(BaseReferenceExp(), "PostServiceProcess");
                AddParameters(methodInvokation, new Object[] { GetProperty("reqState", "TaskName"), GetProperty("reqState", "ServiceName") });
                checkForPostServiceProcess.TrueStatements.Add(AssignVariable("bExecFlag", methodInvokation));
                tryBlock.AddStatement(checkForPostServiceProcess);

                // Base Callout End
                Generate_CallOut_MethodEnd(tryBlock, sContext);

                //for legacy state


                //return expression under try block
                tryBlock.AddStatement(ReturnExpression(VariableReferenceExp("bExecFlag")));


                #endregion try block

                // Catch Block
                AddCatchClause(tryBlock, sContext);
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("EndExecuteService->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }

            return EndExecuteService;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private CodeMemberMethod InitializeTabControls()
        {
            String sContext = "InitializeTabControls";
            CodeMemberMethod InitializeTabControls = new CodeMemberMethod
            {
                Name = sContext,
                Attributes = MemberAttributes.Private | MemberAttributes.New
            };
            try
            {
                //Method summary
                AddMethodSummary(InitializeTabControls, "Initializes tab control");

                CodeTryCatchFinallyStatement tryBlock = AddTryBlock(InitializeTabControls);
                tryBlock.AddStatement(AddTraceInfo(String.Format("{0}()", sContext)));


                //setidentity, addtabcontrol for each tab
                foreach (Page page in _ilbo.TabPages)
                {
                    String sTabName = page.Name;

                    CodeMethodInvokeExpression methodInvokation;
                    methodInvokation = MethodInvocationExp(TypeReferenceExp(String.Format("_{0}", sTabName)), "SetIdentity");
                    AddParameters(methodInvokation, new Object[] { sTabName });
                    tryBlock.AddStatement(methodInvokation);

                    methodInvokation = MethodInvocationExp(BaseReferenceExp(), "AddTabControl");
                    AddParameter(methodInvokation, String.Format("_{0}", sTabName));
                    tryBlock.AddStatement(methodInvokation);
                }

                // For iEDK
                Generate_iEDKMethod(tryBlock, sContext);

                // Catch Block
                AddCatchClause(tryBlock, sContext);

            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("InitializeTabControls->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }

            return InitializeTabControls;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private CodeMemberMethod InitializeLayoutControls()
        {
            try
            {
                String sContext = "InitializeLayoutControls";
                CodeMethodInvokeExpression methodInvokation;

                CodeMemberMethod InitializeLayoutControls = new CodeMemberMethod
                {
                    Name = sContext,
                    Attributes = MemberAttributes.Private | MemberAttributes.New
                };

                // Method Summary
                AddMethodSummary(InitializeLayoutControls, "Initializes Layout Controls");

                CodeTryCatchFinallyStatement tryBlock = AddTryBlock(InitializeLayoutControls);
                tryBlock.AddStatement(AddTraceInfo(String.Format("{0}()", sContext)));

                // Seting Identity & Adding to Layoutcontrol For Each Layout Control
                //foreach (Page page in _ilbo.TabPages)
                //{
                foreach (Control ctrl in _ctrlsInILBO.Where(c => c.IsLayoutControl == true && c.Type != "tab").OrderBy(c => c.Type).ThenBy(c => c.Id))
                //foreach (Control ctrl in ctrlsInILBO.Where(c => c.IsLayoutControl == true && c.Type != "tab").OrderBy(c => c.Type).ThenBy(c => c.Id))
                {
                    String sControlID = ctrl.Id;
                    String sTabName = ctrl.PageName;

                    // Setting Identity
                    methodInvokation = MethodInvocationExp(TypeReferenceExp(String.Format("_{0}", sControlID)), "SetIdentity");
                    AddParameters(methodInvokation, new Object[] { sControlID });
                    tryBlock.AddStatement(methodInvokation);

                    // Adding to Layoutcontrol
                    methodInvokation = MethodInvocationExp(TypeReferenceExp(String.Format("_{0}", sTabName)), "AddLayoutControl");
                    AddParameters(methodInvokation, new Object[] { sControlID });
                    tryBlock.AddStatement(methodInvokation);
                }
                //}

                // For iEDK
                Generate_iEDKMethod(tryBlock, sContext);

                // Catch Block
                AddCatchClause(tryBlock, sContext);

                return InitializeLayoutControls;
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("InitializeLayoutControls->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }

        }

        private CodeMemberMethod Clear()
        {
            CodeMemberMethod clear = new CodeMemberMethod
            {
                Name = "Clear",
                Attributes = MemberAttributes.Public | MemberAttributes.Final
            };
            try
            {
                // For clearing Tree Controls
                if (_ilbo.HasTree)
                {
                    CodeConditionStatement ifTreeCtrlIsNotNull = IfCondition();
                    ifTreeCtrlIsNotNull.Condition = BinaryOpertorExpression(FieldReferenceExp(ThisReferenceExp(), "m_conTree"), CodeBinaryOperatorType.IdentityInequality, SnippetExpression("null"));
                    ifTreeCtrlIsNotNull.TrueStatements.Add(MethodInvocationExp(FieldReferenceExp(ThisReferenceExp(), "m_conTree"), "Clear"));
                    ifTreeCtrlIsNotNull.TrueStatements.Add(AssignField(FieldReferenceExp(ThisReferenceExp(), "m_conTree"), SnippetExpression("null")));
                    clear.AddStatement(ifTreeCtrlIsNotNull);
                }

                if (_ilbo.HasLegacyState && !_ilbo.HasRTState)
                {
                    CodeConditionStatement ifStateCtrlIsNotNull = IfCondition();
                    ifStateCtrlIsNotNull.Condition = BinaryOpertorExpression(FieldReferenceExp(ThisReferenceExp(), "ctrlState"), CodeBinaryOperatorType.IdentityInequality, SnippetExpression("null"));
                    ifStateCtrlIsNotNull.TrueStatements.Add(MethodInvocationExp(FieldReferenceExp(ThisReferenceExp(), "ctrlState"), "Clear"));
                    ifStateCtrlIsNotNull.TrueStatements.Add(AssignField(FieldReferenceExp(ThisReferenceExp(), "ctrlState"), SnippetExpression("null")));
                    clear.AddStatement(ifStateCtrlIsNotNull);
                }

                if (_ilbo.HasMessageLookup)
                {
                    CodeConditionStatement ifMessageLookUp = new CodeConditionStatement();
                    ifMessageLookUp.Condition = VariableReferenceExp("bMsgLkup");

                    CodeConditionStatement ifErrorLookUpIsNotNull = new CodeConditionStatement();
                    ifErrorLookUpIsNotNull.Condition = BinaryOpertorExpression(VariableReferenceExp("oErrorlookup"), CodeBinaryOperatorType.IdentityInequality, SnippetExpression("null"));
                    ifErrorLookUpIsNotNull.TrueStatements.Add(MethodInvocationExp(VariableReferenceExp("oErrorlookup"), "Clear"));
                    ifErrorLookUpIsNotNull.TrueStatements.Add(AssignVariable(VariableReferenceExp("oErrorlookup"), SnippetExpression("null")));
                    ifMessageLookUp.TrueStatements.Add(ifErrorLookUpIsNotNull);

                    clear.AddStatement(ifMessageLookUp);
                }

                return clear;
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("Clear->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private CodeMemberMethod ResetTabControls()
        {
            try
            {
                String sContext = "ResetTabControls";
                CodeMemberMethod ResetTabControls = new CodeMemberMethod
                {
                    Name = sContext,
                    Attributes = MemberAttributes.Public | MemberAttributes.Override
                };

                // Method Summary
                AddMethodSummary(ResetTabControls, "Resets Tab Controls");

                CodeTryCatchFinallyStatement tryBlock = AddTryBlock(ResetTabControls);
                tryBlock.AddStatement(AddTraceInfo(String.Format("{0}()", sContext)));

                // Resetting for each TAB
                foreach (Page page in _ilbo.TabPages)
                {
                    String sTabName = page.Name;
                    CodeMethodInvokeExpression methodInvokation;

                    methodInvokation = MethodInvocationExp(TypeReferenceExp(String.Format("_{0}", sTabName)), "Clear");
                    tryBlock.AddStatement(methodInvokation);
                }

                // For iEDK
                Generate_iEDKMethod(tryBlock, sContext);

                // Catch Block
                AddCatchClause(tryBlock, sContext);

                return ResetTabControls;
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("ResetTabControls->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private CodeMemberMethod ResetLayoutControls()
        {
            try
            {
                String sContext = "ResetLayoutControls";
                CodeMemberMethod ResetLayoutControls = new CodeMemberMethod
                {
                    Name = sContext,
                    Attributes = MemberAttributes.Public | MemberAttributes.Override
                };

                // Method Summary
                AddMethodSummary(ResetLayoutControls, "Resets Layout Controls");

                // Try Block
                CodeTryCatchFinallyStatement tryBlock = AddTryBlock(ResetLayoutControls);
                tryBlock.AddStatement(AddTraceInfo(String.Format("{0}()", sContext)));

                //For Each Layout Control
                //foreach (Page page in _ilbo.TabPages)
                //{
                foreach (Control ctrl in _ctrlsInILBO.Where(c => c.IsLayoutControl == true && c.Type != "tab").OrderBy(c => c.Type).ThenBy(c => c.Id))
                {
                    String sControlID = ctrl.Id;
                    String sTabName = ctrl.PageName;

                    CodeMethodInvokeExpression methodInvokation = MethodInvocationExp(TypeReferenceExp(String.Format("_{0}", sControlID)), "Clear");
                    tryBlock.AddStatement(methodInvokation);
                }
                //}

                // For iEDK
                Generate_iEDKMethod(tryBlock, sContext);

                // Catch Block
                AddCatchClause(tryBlock, sContext);

                return ResetLayoutControls;
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("ResetLayoutControls->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private CodeMemberMethod GetTabControl()
        {
            try
            {
                String sContext = "GetTabControl";
                CodeMethodInvokeExpression methodInvocation;
                CodeMemberMethod GetTabControl = new CodeMemberMethod
                {
                    Name = sContext,
                    Attributes = MemberAttributes.Public | MemberAttributes.Override,
                    ReturnType = new CodeTypeReference("ITabControl")
                };
                GetTabControl.Parameters.Add(ParameterDeclarationExp(typeof(String), "sTabName"));

                // Method Summary
                AddMethodSummary(GetTabControl, "");

                // Try Block
                CodeTryCatchFinallyStatement tryBlock = AddTryBlock(GetTabControl);
                tryBlock.AddStatement(AddTraceInfo(SnippetExpression("String.Format(\"" + sContext + "(sTabName = \\\"{0}\\\")\", sTabName)")));

                // Switch Case Starting
                tryBlock.AddStatement(SnippetStatement("switch (sTabName.ToLower())"));
                tryBlock.AddStatement(SnippetStatement("{"));

                foreach (Page page in _ilbo.TabPages)
                {
                    String sControlID = page.Name;
                    tryBlock.AddStatement(SnippetStatement(String.Format("case \"{0}\":", sControlID)));

                    if (String.IsNullOrEmpty(sControlID) || sControlID.Equals("mainpage"))
                        tryBlock.AddStatement(SnippetStatement(String.Format("case \"\":")));

                    tryBlock.AddStatement(ReturnExpression(FieldReferenceExp(ThisReferenceExp(), String.Format("_{0}", sControlID))));
                }

                // Default Block
                tryBlock.AddStatement(SnippetStatement("default:"));
                methodInvocation = MethodInvocationExp(BaseReferenceExp(), "GetTabControl");
                methodInvocation.Parameters.Add(ArgumentReferenceExp("sTabName"));
                tryBlock.AddStatement(ReturnExpression(methodInvocation));

                // Switch Case Starting
                tryBlock.AddStatement(SnippetStatement("}"));

                // Catch Block
                AddCatchClause(tryBlock, sContext);

                return GetTabControl;
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("GetTabControl->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private CodeMemberMethod GetLayoutControl()
        {
            try
            {
                String sContext = "GetLayoutControl";
                CodeMethodInvokeExpression methodInvokation;
                CodeMemberMethod GetLayoutControl = new CodeMemberMethod
                {
                    Name = sContext,
                    Attributes = MemberAttributes.Public | MemberAttributes.Override,
                    ReturnType = new CodeTypeReference("ILayoutControl")
                };
                GetLayoutControl.Parameters.Add(ParameterDeclarationExp(typeof(String), "sLayoutControlName"));

                // Method Summary
                AddMethodSummary(GetLayoutControl, "Get Layout Controls");

                // Try Block
                CodeTryCatchFinallyStatement tryBlock = AddTryBlock(GetLayoutControl);
                tryBlock.AddStatement(AddTraceInfo(SnippetExpression("String.Format(\"" + sContext + "(sLayoutControlName = \\\"{0}\\\")\", sLayoutControlName)")));

                tryBlock.AddStatement(SnippetStatement("switch(sLayoutControlName)"));
                tryBlock.AddStatement(SnippetStatement("{"));

                //String sTabName = page.Name;
                foreach (string sControlID in _ctrlsInILBO.Where(c => c.IsLayoutControl == true && c.Type != "tab").OrderBy(c => c.Type).ThenBy(c => c.Id).Select(c => c.Id).Distinct())
                {
                    tryBlock.AddStatement(SnippetStatement(String.Format("case \"{0}\":", sControlID)));
                    tryBlock.AddStatement(ReturnExpression(new CodeMethodReferenceExpression(ThisReferenceExp(), String.Format("_{0}", sControlID))));
                }

                // Default Block
                tryBlock.AddStatement(SnippetStatement("default:"));
                methodInvokation = MethodInvocationExp(BaseReferenceExp(), "GetLayoutControl");
                methodInvokation.Parameters.Add(ArgumentReferenceExp("sLayoutControlName"));
                tryBlock.AddStatement(ReturnExpression(methodInvokation));

                // Switch Case Closing
                tryBlock.AddStatement(SnippetStatement("}")); //close braces for switch case

                // Catch Block
                AddCatchClause(tryBlock, sContext);

                return GetLayoutControl;
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("GetLayoutControl->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }


        private void UpdateScreenData_Zoom(CodeTryCatchFinallyStatement tryBlock)
        {
            try
            {
                tryBlock.AddStatement(SnippetStatement("switch(sTabName.ToLower())"));
                tryBlock.AddStatement(SnippetStatement("{"));

                tryBlock.AddStatement(SnippetStatement("default:"));

                CodeConditionStatement chkTabName = new CodeConditionStatement(new CodeBinaryOperatorExpression(VariableReferenceExp("sTabName"), CodeBinaryOperatorType.IdentityEquality, SnippetExpression("String.Empty")));
                chkTabName.TrueStatements.Add(SetContextValue("ICT_FOCUSCONTROL", SnippetExpression("nodeScreenInfo.Attributes[\"fc\"].Value")));
                chkTabName.TrueStatements.Add(SetContextValue("ICT_ILBOHSP", SnippetExpression("nodeScreenInfo.Attributes[\"ihsp\"].Value")));
                chkTabName.TrueStatements.Add(SetContextValue("ICT_ILBOVSP", SnippetExpression("nodeScreenInfo.Attributes[\"ivsp\"].Value")));
                chkTabName.FalseStatements.Add(CodeDomHelper.ThrowNewException("Invalid TabName"));
                tryBlock.AddStatement(chkTabName);

                tryBlock.AddStatement(SnippetExpression("break"));
                tryBlock.AddStatement(SnippetStatement("}"));
                tryBlock.AddStatement(DeclareVariableAndAssign(typeof(System.Xml.XmlNode), "nodeControlInfo", true, SnippetExpression("nodeScreenInfo.ChildNodes[0]")));

                CodeIterationStatement LoopThroughControl = ForLoopExpression(DeclareVariableAndAssign(typeof(int), "iControlCount", true, PrimitiveExpression(1)),
                                                                                BinaryOpertorExpression(VariableReferenceExp("iControlCount"), CodeBinaryOperatorType.LessThanOrEqual, SnippetExpression("nodeControlInfo.ChildNodes.Count")),
                                                                                AssignVariable(VariableReferenceExp("iControlCount"), BinaryOpertorExpression(VariableReferenceExp("iControlCount"), CodeBinaryOperatorType.Add, PrimitiveExpression(1))));

                LoopThroughControl.Statements.Add(DeclareVariableAndAssign(typeof(System.Xml.XmlNode), "nodeControl", true, SnippetExpression("nodeControlInfo.ChildNodes[iControlCount]")));
                tryBlock.AddStatement(LoopThroughControl);
                LoopThroughControl.Statements.Add(SnippetStatement("switch(nodeControl.Attributes[\"n\"].Value)"));
                LoopThroughControl.Statements.Add(SnippetStatement("{"));

                //DataTable dtControlInfo = ilbo.GetControlInfo();
                //IEnumerable<IGrouping<String, DataRow>> recordGroupsByControlID =
                //    dtControlInfo.AsEnumerable().Where(row => row.Field<String>("ControlType") != "rslistedit").OrderBy(row => row.Field<String>("ControlID")).GroupBy(row => row.Field<String>("ControlID"));

                foreach (Control ctrl in this._ctrlsInILBO)
                {
                    LoopThroughControl.Statements.Add(SnippetStatement(String.Format("case \"{0}\":", ctrl.Id)));
                    CodeMethodInvokeExpression methodInvocationExp = MethodInvocationExp(ThisReferenceExp(), String.Format("m_con{0}.UpdateControlData", ctrl.Id));
                    AddParameters(methodInvocationExp, new Object[] { ArgumentReferenceExp("nodeControl") });
                    LoopThroughControl.Statements.Add(methodInvocationExp);
                    LoopThroughControl.Statements.Add(SnippetExpression("break"));
                }

                LoopThroughControl.Statements.Add(SnippetStatement("default:"));
                LoopThroughControl.Statements.Add(CodeDomHelper.ThrowNewException("Invalid ControlID"));
                LoopThroughControl.Statements.Add(SnippetStatement("}"));
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("UpdateScreenData_Zoom->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }

        private void UpdateScreenData_Ext(CodeTryCatchFinallyStatement tryBlock)
        {
            try
            {
                CodeMethodInvokeExpression methodInvokation;

                IEnumerable<Control> treeGrids = from c in _ilbo.Controls
                                                 where c.SectionType.ToLower() == "tree grid"
                                                 select c;
                // For PLFRichControls
                if (_ilbo.HasRichControl)
                {
                    // For Tree Grid
                    if (treeGrids.Any())
                    {
                        tryBlock.AddStatement(CodeDomHelper.DeclareVariableAndAssign(typeof(String[]), "sTreeGridControl", true, null));
                        tryBlock.AddStatement(CodeDomHelper.AssignVariable("sTreeGridControl", new CodeArrayCreateExpression(typeof(String), treeGrids.Count())));

                        int index = 0;
                        foreach (Control treeGrid in treeGrids)
                        {
                            tryBlock.AddStatement(CodeDomHelper.AssignVariable("sTreeGridControl", treeGrid.Id, index, true, null));
                            index++;
                        }

                        methodInvokation = MethodInvocationExp(TypeReferenceExp("oPlfRichControls"), "UpdateScreenData_TreeGrid");
                        AddParameters(methodInvokation, new Object[] { SnippetExpression("ref htContextItems"), ArgumentReferenceExp("sTabName"), SnippetExpression("ref nodeScreenInfo"), VariableReferenceExp("sTreeGridControl") });
                        tryBlock.AddStatement(methodInvokation);
                    }
                }

                // For Tree Control
                if (_ilbo.HasTree)
                {
                    methodInvokation = MethodInvocationExp(TypeReferenceExp(TREECONTROL), "UpdateScreenData_Tree");
                    AddParameters(methodInvokation, new Object[] { SnippetExpression("ref htContextItems"), ArgumentReferenceExp("sTabName"), SnippetExpression("ref nodeScreenInfo") });
                    tryBlock.AddStatement(methodInvokation);
                }
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("UpdateScreenData_Ext->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private CodeMemberMethod UpdateScreenData()
        {
            try
            {
                String sContext = "UpdateScreenData";
                CodeMemberMethod UpdateScreenData = new CodeMemberMethod
                {
                    Name = sContext,
                    Attributes = _ilbo.Type.Equals("9") ? (MemberAttributes.Public | MemberAttributes.Final) : (MemberAttributes.Public | MemberAttributes.Override),
                };
                UpdateScreenData.Parameters.Add(ParameterDeclarationExp(typeof(String), "sTabName"));
                UpdateScreenData.Parameters.Add(ParameterDeclarationExp(new CodeTypeReference("XmlNode"), "nodeScreenInfo"));

                // Method Summary
                AddMethodSummary(UpdateScreenData, "Updates the screen data");

                // Try Block
                CodeTryCatchFinallyStatement tryBlock = AddTryBlock(UpdateScreenData);
                tryBlock.AddStatement(AddTraceInfo(SnippetExpression("String.Format(\"" + sContext + "(sTabName = \\\"{0}\\\", nodeScreenInfo = \\\"{1}\\\")\", sTabName, nodeScreenInfo.OuterXml)")));


                // Base Callout Start
                Generate_CallOut_MethodStart(tryBlock, sContext);

                // For Zoom ILBOs
                if (_ilbo.Type.Equals("9"))
                {
                    UpdateScreenData_Zoom(tryBlock);
                }
                else
                {
                    // For Extended Controls
                    UpdateScreenData_Ext(tryBlock);

                    tryBlock.AddStatement(MethodInvocationExp(BaseReferenceExp(), "UpdateScreenData").AddParameters(new object[] { VariableReferenceExp("sTabName"), VariableReferenceExp("nodeScreenInfo") }));
                }
                tryBlock.AddStatement(ReturnExpression());

                // Base Callout End
                //Generate_CallOut_MethodEnd(tryBlock, sContext);

                // Catch Block
                AddCatchClause(tryBlock, sContext);

                return UpdateScreenData;
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("UpdateScreenData->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }

        private void GetScreenData_TabPages(CodeTryCatchFinallyStatement tryBlock)
        {
            try
            {
                if (_ilbo.HasTree == true || _ilbo.HasChart == true || _ilbo.HasRichControl == true || _ilbo.Trees.Any() || _ilbo.HasDataDrivenTask)
                {
                    // Switch Case Starting
                    tryBlock.AddStatement(SnippetStatement("switch(sTabName.ToLower())"));
                    tryBlock.AddStatement(SnippetStatement("{"));

                    foreach (Page tabPage in _ilbo.TabPages)
                    {
                        var codeForILBOControls = GetScreenData_ILBOControls(tabPage.Name);
                        var codeForExtendedLinks = GetScreenData_ExtendedLinks();

                        if (tabPage.Name != "mainpage")
                        {
                            if ((tabPage.Trees.Any() == false && tabPage.Charts.Any() == false && (_ilbo.HasRichControl && _ilbo.Trees.Any())))
                                continue;
                        }

                        tryBlock.AddStatement(SnippetStatement(String.Format("case \"{0}\":", tabPage.Name == "mainpage" ? "" : tabPage.Name)));

                        // For ILBO Controls(chkIlboInfo)
                        if (_ilbo.Type.Equals("9"))
                            foreach (CodeExpression expression in codeForILBOControls)
                                tryBlock.AddStatement(expression);

                        // For Extended Task
                        if (tabPage.Name.Equals("mainpage"))
                            foreach (CodeStatement statement in codeForExtendedLinks)
                                tryBlock.AddStatement(statement);

                        // For Tree
                        if (tabPage.Trees.Any())
                            tryBlock.AddStatement(new CodeMethodInvokeExpression(TypeReferenceExp(TREECONTROL), "GetScreenData_Tree").AddParameters(new Object[] { SnippetExpression("ref htContextItems"), VariableReferenceExp("sTabName"), SnippetExpression("ref nodeScreenInfo") }));

                        // For Chart
                        if (tabPage.Charts.Any())
                            tryBlock.AddStatement(new CodeMethodInvokeExpression(TypeReferenceExp(CHARTCONTROL), "Update_ScreenData_Chart").AddParameters(
                                                                                                                                                            new Object[] { SnippetExpression("htContextItems"),
                                                                                                                                                            (string.IsNullOrEmpty(tabPage.Name) || string.Compare(tabPage.Name, "mainpage", true) == 0) ? SnippetExpression("string.Empty") : SnippetExpression("sTabName"),
                                                                                                                                                            SnippetExpression("ref nodeScreenInfo") }));

                        tryBlock.AddStatement(SnippetExpression("break"));
                    }
                    // Switch Case Closing
                    tryBlock.AddStatement(SnippetStatement("}"));

                    // For Tree Grid
                    IEnumerable<Control> treeGrids = _richControlsInILBO.Where(c => c.SectionType == "tree grid");
                    if (treeGrids.Any())
                    {
                        tryBlock.AddStatement(AssignVariable("sTreeGridControl", new CodeArrayCreateExpression(typeof(String), treeGrids.Count())));

                        int index = 0;
                        foreach (Control treeGrid in treeGrids)
                        {
                            tryBlock.AddStatement(AssignVariable("sTreeGridControl", treeGrid.Id, index, true));
                            index++;
                        }
                        tryBlock.AddStatement(new CodeMethodInvokeExpression(TypeReferenceExp("oPlfRichControls"), "GetScreenData_TreeGrid").AddParameters(new object[] { SnippetExpression("ref htContextItems"), SnippetExpression("sTabName"), SnippetExpression("ref nodeScreenInfo"), SnippetExpression("sTreeGridControl") }));
                    }
                }
                tryBlock.AddStatement(new CodeMethodInvokeExpression(BaseReferenceExp(), "GetScreenData").AddParameters(new object[] { VariableReferenceExp("sTabName"), VariableReferenceExp("nodeScreenInfo") }));
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("GetScreenData_TabPages->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }


        private CodeStatementCollection GetScreenData_ExtendedLinks()
        {
            CodeStatementCollection statements = new CodeStatementCollection();
            try
            {
                // For Dynamic Links
                if (_ilbo.HasDynamicLink)
                {
                    CodeConditionStatement chkDLink = new CodeConditionStatement(new CodeBinaryOperatorExpression(VariableReferenceExp("oDlink"), CodeBinaryOperatorType.IdentityInequality, SnippetExpression("null")));
                    statements.Add(chkDLink);
                    chkDLink.TrueStatements.Add(DeclareVariableAndAssign("Hashtable", "_hashTasklist", true, new CodeMethodInvokeExpression(TypeReferenceExp("oDlink"), "GetIlboTasks").AddParameters(new object[] { _ecrOptions.Component, _activity.Name, _ilbo.Code, SnippetExpression("ISMContext.GetContextValue(\"SCT_LOGINLANGID\").ToString()"), SnippetExpression("String.Empty") })));

                    CodeConditionStatement checkHashList = new CodeConditionStatement
                    {
                        Condition = new CodeBinaryOperatorExpression(
                                                new CodeBinaryOperatorExpression(VariableReferenceExp("_hashTasklist"), CodeBinaryOperatorType.IdentityInequality, SnippetExpression("null")),
                                                CodeBinaryOperatorType.BooleanAnd,
                                                new CodeBinaryOperatorExpression(GetProperty("_hashTasklist", "Count"), CodeBinaryOperatorType.GreaterThan, PrimitiveExpression(0)))
                    };
                    chkDLink.TrueStatements.Add(checkHashList);
                    checkHashList.TrueStatements.Add(DeclareVariableAndAssign(typeof(String[]), "arrKeys", true, SnippetExpression("new String[_hashTasklist.Count + 1]")));
                    checkHashList.TrueStatements.Add(new CodeMethodInvokeExpression(TypeReferenceExp("_hashTasklist.Keys"), "CopyTo").AddParameters(new object[] { VariableReferenceExp("arrKeys"), SnippetExpression("1") }));
                    checkHashList.TrueStatements.Add(DeclareVariableAndAssign(typeof(int), "_nCount", true, SnippetExpression("arrKeys.Length")));

                    CodeIterationStatement IterateHash = ForLoopExpression(
                                                                                DeclareVariableAndAssign(typeof(int), "i", true, PrimitiveExpression(1)),
                                                                                BinaryOpertorExpression(VariableReferenceExp("i"), CodeBinaryOperatorType.LessThan, SnippetExpression("_nCount")),
                                                                                AssignVariable(VariableReferenceExp("i"), BinaryOpertorExpression(VariableReferenceExp("i"), CodeBinaryOperatorType.Add, PrimitiveExpression(1))
                                                                                ));
                    checkHashList.TrueStatements.Add(IterateHash);
                    IterateHash.Statements.Add(new CodeMethodInvokeExpression(TypeReferenceExp(String.Format("m_con{0}", "itk_select_link")), "AddListItem").AddParameters(new Object[] { PrimitiveExpression("itk_select_link_hv"), VariableReferenceExp("i"), SnippetExpression("arrKeys[i].ToString()") }));
                    IterateHash.Statements.Add(new CodeMethodInvokeExpression(TypeReferenceExp(String.Format("m_con{0}", "itk_select_link")), "AddListItem").AddParameters(new Object[] { PrimitiveExpression("itk_select_link"), VariableReferenceExp("i"), SnippetExpression("_hashTasklist[arrKeys[i]].ToString()") }));
                    chkDLink.TrueStatements.Add(DeclareVariableAndAssign(typeof(XmlNode), "oDlinkData", true, new CodeMethodInvokeExpression(TypeReferenceExp("oDlink"), "GetIlboTaskData").AddParameters(new object[] { _ecrOptions.Component, _activity.Name, _ilbo.Code, SnippetExpression("ISMContext.GetContextValue(\"SCT_LOGINLANGID\").ToString()"), SnippetExpression("String.Empty") })));

                    CodeConditionStatement oDLinkData = new CodeConditionStatement(new CodeBinaryOperatorExpression(VariableReferenceExp("oDlinkData"), CodeBinaryOperatorType.IdentityInequality, SnippetExpression("null")));
                    chkDLink.TrueStatements.Add(oDLinkData);
                    oDLinkData.TrueStatements.Add(DeclareVariableAndAssign(typeof(System.Xml.XmlElement), "eltDlink", true, new CodeMethodInvokeExpression(TypeReferenceExp("nodeScreenInfo.OwnerDocument"), "CreateElement").AddParameters(new object[] { PrimitiveExpression("dlink") })));
                    oDLinkData.TrueStatements.Add(AssignVariable("eltDlink.InnerXml", SnippetExpression("oDlinkData.OuterXml")));
                    oDLinkData.TrueStatements.Add(new CodeMethodInvokeExpression(TypeReferenceExp("nodeScreenInfo"), "AppendChild").AddParameters(new object[] { VariableReferenceExp("eltDlink") }));
                }

                // For Control Extensions
                if (_ilbo.HasControlExtensions)
                {
                    CodeConditionStatement chkCE = new CodeConditionStatement(new CodeBinaryOperatorExpression(VariableReferenceExp("oCE"), CodeBinaryOperatorType.IdentityInequality, SnippetExpression("null")));
                    statements.Add(chkCE);

                    chkCE.TrueStatements.Add(DeclareVariableAndAssign(typeof(string), "CEContent", true, GetProperty(TypeReferenceExp(typeof(string)), "Empty")));
                    chkCE.TrueStatements.Add(AssignVariable(VariableReferenceExp("CEContent"), MethodInvocationExp(VariableReferenceExp("oCE"), "GetCEIconDetails").AddParameters(new CodeExpression[] { PrimitiveExpression("1"), PrimitiveExpression(_ecrOptions.Component), PrimitiveExpression(_activity.Name), PrimitiveExpression(_ilbo.Code), GetProperty(TypeReferenceExp(typeof(string)), "Empty") })));

                    CodeConditionStatement chkCEContent = IfCondition();
                    chkCEContent.Condition = SnippetExpression("!String.IsNullOrEmpty(CEContent)");
                    chkCE.TrueStatements.Add(chkCEContent);

                    //chkCEContent.TrueStatements.Add(AssignVariable(VariableReferenceExp("CEContent"), new CodeMethodInvokeExpression(TypeReferenceExp("oCE"), "GetCEIconDetails").AddParameters(new object[] { PrimitiveExpression("1"), GlobalVar.Component, _activity.Name, _ilbo.Code, SnippetExpression("String.Empty") })));
                    chkCEContent.TrueStatements.Add(DeclareVariableAndAssign(typeof(XmlElement), "eltCE", true, SnippetExpression("(XmlElement)nodeScreenInfo.SelectSingleNode(\"controlextensions\")")));

                    CodeConditionStatement checkForCE = new CodeConditionStatement
                    {
                        Condition = new CodeBinaryOperatorExpression(VariableReferenceExp("eltCE"), CodeBinaryOperatorType.IdentityEquality, PrimitiveExpression(null))
                    };
                    chkCEContent.TrueStatements.Add(checkForCE);
                    checkForCE.TrueStatements.Add(AssignVariable(VariableReferenceExp("eltCE"), new CodeMethodInvokeExpression(TypeReferenceExp("nodeScreenInfo.OwnerDocument"), "CreateElement").AddParameters(new object[] { PrimitiveExpression("controlextensions") })));
                    chkCEContent.TrueStatements.Add(AssignVariable("eltCE.InnerXml", VariableReferenceExp("CEContent")));
                    chkCEContent.TrueStatements.Add(new CodeMethodInvokeExpression(TypeReferenceExp("nodeScreenInfo"), "AppendChild").AddParameters(new object[] { SnippetExpression("eltCE") }));
                }

                // For Data Driven Task
                if (_ilbo.HasDataDrivenTask)
                {
                    CodeConditionStatement chkDDT = new CodeConditionStatement(new CodeBinaryOperatorExpression(VariableReferenceExp("oDDTask"), CodeBinaryOperatorType.IdentityInequality, SnippetExpression("null")));
                    statements.Add(chkDDT);

                    chkDDT.TrueStatements.Add(DeclareVariableAndAssign(typeof(String), "DDTContent", true, new CodeMethodInvokeExpression(TypeReferenceExp("oDDTask"), "GetDDTDetails").AddParameters(new object[] { PrimitiveExpression("1"), _ecrOptions.Component, _activity.Name, _ilbo.Code })));

                    CodeConditionStatement ifDDTContentHasValue = IfCondition();
                    ifDDTContentHasValue.Condition = SnippetExpression("!String.IsNullOrEmpty(DDTContent)");


                    ifDDTContentHasValue.TrueStatements.Add(DeclareVariableAndAssign(typeof(XmlElement), "eltDDT", true, SnippetExpression("(XmlElement) nodeScreenInfo.SelectSingleNode(\"ddt\")")));

                    CodeConditionStatement checkForCE = new CodeConditionStatement
                    {
                        Condition = new CodeBinaryOperatorExpression(VariableReferenceExp("eltDDT"), CodeBinaryOperatorType.IdentityEquality, PrimitiveExpression(null))
                    };
                    ifDDTContentHasValue.TrueStatements.Add(checkForCE);
                    checkForCE.TrueStatements.Add(AssignVariable("eltDDT", new CodeMethodInvokeExpression(TypeReferenceExp("nodeScreenInfo.OwnerDocument"), "CreateElement").AddParameters(new object[] { PrimitiveExpression("ddt") })));
                    ifDDTContentHasValue.TrueStatements.Add(AssignVariable("eltDDT.InnerXml", VariableReferenceExp("DDTContent")));
                    ifDDTContentHasValue.TrueStatements.Add(new CodeMethodInvokeExpression(TypeReferenceExp("nodeScreenInfo"), "AppendChild").AddParameters(new object[] { SnippetExpression("eltDDT") }));
                    chkDDT.TrueStatements.Add(ifDDTContentHasValue);
                }
                return statements;
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("GetScreenData_ExtendedLinks->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }

        private CodeExpressionCollection GetScreenData_ILBOControls(String sTabName)
        {
            CodeExpressionCollection expressions = new CodeExpressionCollection();

            try
            {
                foreach (Control control in (from ts in _ilbo.TaskServiceList
                                             from ti in ts.TaskInfos
                                             where ti.TabName == sTabName
                                             select ti.Control).Distinct())
                {
                    expressions.Add(AddRenderAsXML(control, false, true));
                }
                return expressions;
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("GetScreenData_ILBOControls->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }

        private void GetScreenData_DisabledTaskInfo(CodeConditionStatement chkIlboInfo)
        {
            try
            {
                IEnumerable<TaskService> DisabledTasks = _ilbo.TaskServiceList.Where(t => t.Type == "link" && t.Traversal != null).Where(ts => ts.Traversal.ChildActivity != _activity.Name && ts.Traversal.ChildActivityType == 1).OrderBy(ts => ts.Name).ThenBy(ts => ts.Traversal.ChildActivity);

                chkIlboInfo.TrueStatements.Add(DeclareVariableAndAssign(typeof(System.Xml.XmlElement), "eltDsTaskInfo", true, new CodeMethodInvokeExpression(TypeReferenceExp("nodeScreenInfo.OwnerDocument"), "CreateElement").AddParameters(new object[] { PrimitiveExpression("dti") })));
                chkIlboInfo.TrueStatements.Add(new CodeMethodInvokeExpression(TypeReferenceExp("nodeScreenInfo"), "AppendChild").AddParameters(new Object[] { VariableReferenceExp("eltDsTaskInfo") }));
                foreach (TaskService DisabledTask in DisabledTasks)
                {
                    CodeMethodInvokeExpression methodInvokation = MethodInvocationExp(TypeReferenceExp("IAuthorizedInfo"), "IsNavigationAuthorized");
                    methodInvokation.AddParameters(new String[] { _activity.Name, DisabledTask.Traversal.ChildActivity });

                    CodeConditionStatement bFlagCondition = new CodeConditionStatement();
                    bFlagCondition.Condition = BinaryOpertorExpression(methodInvokation, CodeBinaryOperatorType.IdentityEquality, PrimitiveExpression(false));
                    bFlagCondition.TrueStatements.Add(AssignVariable("eltTask", new CodeMethodInvokeExpression(TypeReferenceExp("xmlDom"), "CreateElement").AddParameters(new Object[] { PrimitiveExpression("t") })));
                    bFlagCondition.TrueStatements.Add(new CodeMethodInvokeExpression(TypeReferenceExp("eltTask"), "SetAttribute").AddParameters(new Object[] { PrimitiveExpression("n"), Convert.ToString(DisabledTask.Name) }));
                    bFlagCondition.TrueStatements.Add(new CodeMethodInvokeExpression(TypeReferenceExp("eltDsTaskInfo"), "AppendChild").AddParameters(new Object[] { SnippetExpression("eltTask") }));

                    chkIlboInfo.TrueStatements.Add(bFlagCondition);
                }
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("GetScreenData_DisabledTaskInfo->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }


        private void GetScreenData_LayoutControls(CodeConditionStatement chkIlboInfo)
        {
            try
            {
                if (this._ctrlsInILBO.Where(c => c.IsLayoutControl == true).Any())
                {

                    CodeConditionStatement CheckForMainpage = new CodeConditionStatement
                    {
                        Condition = new CodeBinaryOperatorExpression(SnippetExpression("sTabName"), CodeBinaryOperatorType.IdentityEquality, SnippetExpression("String.Empty"))
                    };
                    chkIlboInfo.TrueStatements.Add(CheckForMainpage);
                    CheckForMainpage.TrueStatements.Add(new CodeMethodInvokeExpression(TypeReferenceExp("eltIlboInfo"), "SetAttribute").AddParameters(new Object[] { "at", SnippetExpression("(String)GetContextValue(\"ICT_ACTIVE_TAB\")") }));

                    CodeConditionStatement Check_SCTLastTaskType = new CodeConditionStatement
                    {
                        Condition = new CodeBinaryOperatorExpression(
                                      new CodeBinaryOperatorExpression(
                                                                        SnippetExpression("(String)ISMContext.GetContextValue(\"SCT_LASTTASK_TYPE\")"),
                                                                        CodeBinaryOperatorType.IdentityEquality,
                                                                        PrimitiveExpression("HELP")
                                                                       ),
                                     CodeBinaryOperatorType.BooleanAnd,
                                      new CodeBinaryOperatorExpression(
                                                                        SnippetExpression("GetContextValue(\"ICT_PARENTILBO\")"),
                                                                        CodeBinaryOperatorType.IdentityInequality,
                                                                        SnippetExpression("null")
                                                                       )
                                                                    )
                    };

                    CodeConditionStatement Check_HelpTaskTypeIsNull = new CodeConditionStatement
                    {
                        Condition = new CodeBinaryOperatorExpression(SnippetExpression("ISMContext.GetContextValue(\"ICT_HELPTASK_TYPE\")"),
                                                                    CodeBinaryOperatorType.IdentityEquality,
                                                                    SnippetExpression("null"))
                    };
                    Check_HelpTaskTypeIsNull.TrueStatements.Add(new CodeMethodInvokeExpression(ThisReferenceExp(), "SetContextValue").AddParameters(new Object[] { "ICT_HELPTASK_TYPE", new CodeMethodInvokeExpression(TypeReferenceExp("ISMContext"), "GetContextValue", PrimitiveExpression("SCT_LASTTASK_TYPE")) }));
                    Check_SCTLastTaskType.TrueStatements.Add(Check_HelpTaskTypeIsNull);
                    CheckForMainpage.TrueStatements.Add(Check_SCTLastTaskType);

                    CodeConditionStatement Check_ICTHelpTaskType = new CodeConditionStatement
                    {
                        Condition = new CodeBinaryOperatorExpression(SnippetExpression("(String)GetContextValue(\"ICT_HELPTASK_TYPE\")"),
                                                                    CodeBinaryOperatorType.IdentityEquality,
                                                                    PrimitiveExpression("HELP"))
                    };
                    Check_ICTHelpTaskType.TrueStatements.Add(new CodeMethodInvokeExpression(TypeReferenceExp("eltIlboInfo"), "SetAttribute").AddParameters(new Object[] { "ttype", "HELP" }));
                    Check_ICTHelpTaskType.TrueStatements.Add(new CodeMethodInvokeExpression(TypeReferenceExp("ISMContext"), "SetContextValue").AddParameters(new Object[] { "SCT_LASTTASK_TYPE", "" }));
                    Check_ICTHelpTaskType.FalseStatements.Add(new CodeMethodInvokeExpression(ThisReferenceExp(), "SetContextValue").AddParameters(new Object[] { "ICT_HELPTASK_TYPE", "NOTHELP" }));
                    Check_ICTHelpTaskType.FalseStatements.Add(new CodeMethodInvokeExpression(TypeReferenceExp("eltIlboInfo"), "SetAttribute").AddParameters(new Object[] { "ttype", "NOTHELP" }));
                    CheckForMainpage.TrueStatements.Add(Check_ICTHelpTaskType);
                }
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("GetScreenData_LayoutControls->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }

        //private void GetScreenData_Zoom(CodeTryCatchFinallyStatement tryBlock)
        //{
        //    CodeMethodInvokeExpression methodInvokation;

        //    try
        //    {
        //        tryBlock.AddStatement(DeclareVariableAndAssign(typeof(String), "sParentActivity", true, SnippetExpression("(String)GetContextValue(\"ICT_PARENTACTIVITY\")")));
        //        tryBlock.AddStatement(DeclareVariableAndAssign(typeof(String), "sParentILBO", true, SnippetExpression("(String)GetContextValue(\"ICT_PARENTILBO\")")));

        //        CodeConditionStatement compareWithNull = new CodeConditionStatement
        //        {
        //            Condition = new CodeBinaryOperatorExpression
        //            {
        //                Left = MethodInvocationExp(ThisReferenceExp(), "GetContextValue").AddParameters(new object[] { SnippetExpression("\"ICT_GRID_CONTROL\"") }),
        //                Operator = CodeBinaryOperatorType.IdentityEquality,
        //                Right = SnippetExpression("null")
        //            }
        //        };
        //        compareWithNull.TrueStatements.Add(DeclareVariableAndAssign(typeof(String), "sParentControlID", true, SnippetExpression("(String)GetContextValue(\"ICT_PARENTCONTROL\")")));
        //        methodInvokation = MethodInvocationExp(ThisReferenceExp(), "SetContextValue");
        //        AddParameters(methodInvokation, new Object[] { "ICT_GRID_CONTROL", SnippetExpression("sParentControlID") });
        //        compareWithNull.TrueStatements.Add(methodInvokation);
        //        tryBlock.AddStatement(compareWithNull);

        //        methodInvokation = MethodInvocationExp(TypeReferenceExp("IobjBroker"), "GetScreenObject");
        //        AddParameters(methodInvokation, new Object[] { SnippetExpression("sParentActivity"), SnippetExpression("sParentILBO") });
        //        tryBlock.AddStatement(DeclareVariableAndAssign("IILBO", "IilboHandle", true, methodInvokation));

        //        methodInvokation = MethodInvocationExp(TypeReferenceExp("(Multiline)IilboHandle"), "GetControl");
        //        AddParameters(methodInvokation, new Object[] { SnippetExpression("(String)GetContextValue(\"ICT_GRID_CONTROL\")") });
        //        tryBlock.AddStatement(DeclareVariableAndAssign("Multiline", "oMultilineControl", true, methodInvokation));

        //        methodInvokation = MethodInvocationExp(TypeReferenceExp("oMultilineControl"), "GetNumInstances");
        //        tryBlock.AddStatement(DeclareVariableAndAssign(typeof(long), "lTotalRows", true, methodInvokation));

        //        CodeConditionStatement CheckTab = new CodeConditionStatement(new CodeBinaryOperatorExpression(VariableReferenceExp("sTabName"), CodeBinaryOperatorType.IdentityEquality, GetProperty(typeof(String), "Empty")));
        //        CheckTab.TrueStatements.Add(new CodeMethodInvokeExpression(TypeReferenceExp("eltIlboInfo"), "SetAttribute").AddParameters(new String[] { "at", "(String)GetContextValue(\"ICT_ACTIVE_TAB\")" }));
        //        CodeConditionStatement Check_SCTLastTaskType = new CodeConditionStatement
        //        {
        //            Condition = new CodeBinaryOperatorExpression(
        //                          new CodeBinaryOperatorExpression(
        //                                                            SnippetExpression("((String)ISMContext.GetContextValue(\"SCT_LASTTASK_TYPE\"))"),
        //                                                            CodeBinaryOperatorType.IdentityEquality,
        //                                                            PrimitiveExpression("HELP")
        //                                                           ),
        //                         CodeBinaryOperatorType.BooleanAnd,
        //                          new CodeBinaryOperatorExpression(
        //                                                            SnippetExpression("GetContextValue(\"ICT_PARENTILBO\")"),
        //                                                            CodeBinaryOperatorType.IdentityInequality,
        //                                                            SnippetExpression("null")
        //                                                           )
        //                                                        )
        //        };
        //        CheckTab.TrueStatements.Add(Check_SCTLastTaskType);

        //        CodeConditionStatement Check_ICTHelpTaskType = new CodeConditionStatement();
        //        Check_ICTHelpTaskType.Condition = BinaryOpertorExpression(SnippetExpression("ISMContext.GetContextValue(\"ICT_HELPTASK_TYPE\")"),
        //                                                                    CodeBinaryOperatorType.IdentityEquality,
        //                                                                    PrimitiveExpression(null));
        //        Check_SCTLastTaskType.TrueStatements.Add(Check_ICTHelpTaskType);
        //        Check_ICTHelpTaskType.TrueStatements.Add(SetContextValue("ICT_HELPTASK_TYPE", SnippetExpression("ISMContext.GetContextValue(\"SCT_LASTTASK_TYPE\")")));

        //        CodeConditionStatement Check_ICTHelpType = new CodeConditionStatement();
        //        Check_ICTHelpType.Condition = BinaryOpertorExpression(SnippetExpression("ISMContext.GetContextValue(\"ICT_HELPTASK_TYPE\")"),
        //                                                                    CodeBinaryOperatorType.IdentityEquality,
        //                                                                    SnippetExpression("\"HELP\""));
        //        CheckTab.TrueStatements.Add(Check_ICTHelpType);
        //        Check_ICTHelpType.TrueStatements.Add(new CodeMethodInvokeExpression(TypeReferenceExp("eltIlboInfo"), "SetAttribute").AddParameters(new String[] { "ttype", "HELP" }));
        //        Check_ICTHelpType.TrueStatements.Add(new CodeMethodInvokeExpression(TypeReferenceExp("ISMContext"), "SetContextValue").AddParameters(new String[] { "SCT_LASTTASK_TYPE", String.Empty }));
        //        Check_ICTHelpType.FalseStatements.Add(SetContextValue("ICT_HELPTASK_TYPE", SnippetExpression("\"NOTHELP\"")));
        //        Check_ICTHelpType.FalseStatements.Add(new CodeMethodInvokeExpression(TypeReferenceExp("eltIlboInfo"), "SetAttribute").AddParameters(new String[] { "ttype", "NOTHELP" }));

        //        CheckTab.TrueStatements.Add(new CodeMethodInvokeExpression(TypeReferenceExp("eltIlboInfo"), "SetAttribute").AddParameters(new Object[] { SnippetExpression("\"cr\""), SnippetExpression("(String)(oMultilineControl.GetContextValue(\"CCT_CURRENTROW\"))") }));
        //        CheckTab.TrueStatements.Add(new CodeMethodInvokeExpression(TypeReferenceExp("eltIlboInfo"), "SetAttribute").AddParameters(new Object[] { SnippetExpression("\"tr\""), SnippetExpression("lTotalRows.ToString()") }));

        //        tryBlock.AddStatement(CheckTab);
        //    }
        //    catch (Exception ex)
        //    {
        //        throw new Exception(String.Format("GetScreenData_Zoom->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
        //    }
        //}

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private CodeMemberMethod GetScreenData()
        {
            try
            {
                String sContext = "GetScreenData";
                CodeMethodInvokeExpression methodInvokation;
                CodeMemberMethod GetScreenData = new CodeMemberMethod
                {
                    Name = sContext,
                    Attributes = MemberAttributes.Public | MemberAttributes.Override
                };
                GetScreenData.Parameters.Add(ParameterDeclarationExp(typeof(String), "sTabName"));
                GetScreenData.Parameters.Add(ParameterDeclarationExp(new CodeTypeReference("XmlNode"), "nodeScreenInfo"));

                //for legacy state
                if (_ilbo.HasStateCtrl && _ilbo.HasRTState == false)
                    GetScreenData.Statements.Add(DeclareVariable(typeof(bool), "bProcessState"));

                // For Rich Controls
                if (_ilbo.HasRichControl)
                    GetScreenData.Statements.Add(DeclareVariableAndAssign(typeof(String[]), "sTreeGridControl", true, SnippetExpression("null")));

                // Method Summary
                AddMethodSummary(GetScreenData, "Gets the screen data");

                // Try Block
                CodeTryCatchFinallyStatement tryBlock = AddTryBlock(GetScreenData);
                tryBlock.AddStatement(AddTraceInfo(SnippetExpression("String.Format(\"" + sContext + "(sTabName = \\\"{0}\\\", nodeScreenInfo = \\\"{1}\\\")\", sTabName, nodeScreenInfo.OuterXml)")));

                // Base Callout Start
                Generate_CallOut_MethodStart(tryBlock, sContext);

                // Local Variable Declaration
                tryBlock.AddStatement(AddISManager());
                tryBlock.AddStatement(AddISMContext());
                tryBlock.AddStatement(DeclareVariableAndAssign("IAuthorizedActivitiesAndOULists", "IAuthorizedInfo", true, MethodInvocationExp(TypeReferenceExp("ISManager"), "GetAuthorizedInfoObject")));

                if (_ilbo.Type == "9")
                    tryBlock.AddStatement(DeclareVariableAndAssign("IObjectBroker", "IobjBroker", true, MethodInvocationExp(TypeReferenceExp("ISManager"), "GetObjectBroker")));

                tryBlock.AddStatement(DeclareVariableAndAssign("XmlDocument", "xmlDom", true, GetProperty("nodeScreenInfo", "OwnerDocument")));
                tryBlock.AddStatement(DeclareVariableAndAssign(typeof(System.Xml.XmlElement), "eltTask"));
                tryBlock.AddStatement(DeclareVariableAndAssign(typeof(System.Xml.XmlElement), "eltControlInfo"));
                tryBlock.AddStatement(DeclareVariableAndAssign(typeof(System.Xml.XmlElement), "eltLayoutControlInfo"));

                // ILBO Info
                tryBlock.AddStatement(CodeDomHelper.DeclareVariableAndAssign(typeof(XmlElement), "eltIlboInfo", true, SnippetExpression("xmlDom.SelectSingleNode(\"trpi/scri/ii\") as XmlElement")));
                CodeConditionStatement chkIlboInfo = new CodeConditionStatement(new CodeBinaryOperatorExpression(VariableReferenceExp("eltIlboInfo"), CodeBinaryOperatorType.IdentityEquality, SnippetExpression("null")));
                tryBlock.AddStatement(chkIlboInfo);
                chkIlboInfo.TrueStatements.Add(AssignVariable("eltIlboInfo", new CodeMethodInvokeExpression(TypeReferenceExp("xmlDom"), "CreateElement").AddParameters(new Object[] { PrimitiveExpression("ii") })));
                chkIlboInfo.TrueStatements.Add(new CodeMethodInvokeExpression(TypeReferenceExp("nodeScreenInfo"), "AppendChild").AddParameters(new Object[] { VariableReferenceExp("eltIlboInfo") }));

                //IEnumerable<TaskService> DataSavingTasks = _ilbo.TaskServiceList.Where(ts => ts.IsDataSavingTask);
                //if (DataSavingTasks.Any())
                //{
                chkIlboInfo.TrueStatements.Add(new CodeMethodInvokeExpression(TypeReferenceExp("eltIlboInfo"), "SetAttribute").AddParameters(new Object[] { "dst", _ilbo.HasDataSavingTask ? PrimitiveExpression("1") : PrimitiveExpression("0") }));
                chkIlboInfo.TrueStatements.Add(Generate_SetContextValue(TypeReferenceExp("ISMContext"), "SetContextValue", "SCT_SETPAGE_STATUS", "0"));
                //}

                /*
                // Control Info
                tryBlock.AddStatement(CodeDomHelper.DeclareVariableAndAssign(typeof(XmlElement), "eltControlInfo", true, SnippetExpression("xmlDom.SelectSingleNode(\"trpi/scri/ci\") as XmlElement")));
                CodeConditionStatement chkControlInfo = new CodeConditionStatement(new CodeBinaryOperatorExpression(VariableReferenceExp("eltControlInfo"), CodeBinaryOperatorType.IdentityEquality, SnippetExpression("null")));
                tryBlock.AddStatement(chkControlInfo);
                chkControlInfo.TrueStatements.Add(AssignVariable("eltControlInfo", new CodeMethodInvokeExpression(TypeReferenceExp("xmlDom"), "CreateElement").AddParameters(new Object[] { PrimitiveExpression("ci") })));
                chkControlInfo.TrueStatements.Add(new CodeMethodInvokeExpression(TypeReferenceExp("nodeScreenInfo"), "AppendChild").AddParameters(new Object[] { VariableReferenceExp("eltControlInfo") }));
                */


                chkIlboInfo.FalseStatements.Add(CodeDomHelper.AssignVariable("eltIlboInfo", SnippetExpression("xmlDom.SelectSingleNode(\"trpi/scri/ii\") as XmlElement")));
                chkIlboInfo.FalseStatements.Add(CodeDomHelper.AssignVariable("eltControlInfo", SnippetExpression("xmlDom.SelectSingleNode(\"trpi/scri/ci\") as XmlElement")));

                // For Zoom iLBO
                //if (_ilbo.Type == "9")
                //{
                //    GetScreenData_Zoom(tryBlock);
                //}

                // For Layout Controls
                GetScreenData_LayoutControls(chkIlboInfo);

                // For Disabled Task Info 
                GetScreenData_DisabledTaskInfo(chkIlboInfo);

                // Control Information
                methodInvokation = MethodInvocationExp(BaseReferenceExp(), "GetControlInfoElement");
                AddParameters(methodInvokation, new Object[] { VariableReferenceExp("xmlDom"), ArgumentReferenceExp("nodeScreenInfo") });
                chkIlboInfo.TrueStatements.Add(AssignVariable("eltControlInfo", methodInvokation));

                if (_ilbo.Type != "9")
                {
                    // Layout Control Information
                    methodInvokation = MethodInvocationExp(BaseReferenceExp(), "GetLayoutControlInfoElement");
                    AddParameters(methodInvokation, new Object[] { VariableReferenceExp("xmlDom"), ArgumentReferenceExp("nodeScreenInfo") });
                    chkIlboInfo.TrueStatements.Add(AssignVariable("eltLayoutControlInfo", methodInvokation));
                }

                //For Legacy State
                if (_ilbo.HasLegacyState && _ilbo.HasRTState == false)
                {
                    methodInvokation = MethodInvocationExp(VariableReferenceExp("ctrlState"), "UpdateTaskDataState");
                    AddParameters(methodInvokation, new Object[] { _ilbo.Code, VariableReferenceExp("sTabName"), GetProperty(TypeReferenceExp(typeof(string)), "Empty"), PrimitiveExpression("USER"), VariableReferenceExp("ref nodeScreenInfo"), VariableReferenceExp("htContextItems") });
                    tryBlock.AddStatement(AssignVariable(VariableReferenceExp("bProcessState"), methodInvokation));
                }

                // For Tab Pages
                GetScreenData_TabPages(tryBlock);

                // Base Callout End
                Generate_CallOut_MethodEnd(tryBlock, sContext);

                // Catch Block
                AddCatchClause(tryBlock, sContext);

                return GetScreenData;
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("GetScreenData->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }

        }

        private CodeMemberMethod GetScreenData_Zoom()
        {
            CodeMemberMethod GetScreenData = new CodeMemberMethod
            {
                Name = "GetScreenData",
                Attributes = MemberAttributes.Public | MemberAttributes.Final
            };

            CodeParameterDeclarationExpression pTabName = new CodeParameterDeclarationExpression(typeof(string), "sTabName");
            CodeParameterDeclarationExpression pNodeScreenInfo = new CodeParameterDeclarationExpression(typeof(XmlNode), "nodeScreenInfo");

            GetScreenData.Parameters.Add(pTabName);
            GetScreenData.Parameters.Add(pNodeScreenInfo);

            CodeTryCatchFinallyStatement tryBlock = new CodeTryCatchFinallyStatement();
            GetScreenData.AddStatement(tryBlock);

            tryBlock.AddStatement(AddTraceInfo(SnippetExpression("String.Format(\"" + "GetScreenData" + "(sTabName = \\\"{0}\\\", nodeScreenInfo = \\\"{1}\\\")\", sTabName, nodeScreenInfo.OuterXml)")));

            #region declarations
            tryBlock.AddStatement(DeclareVariableAndAssign(typeof(XmlDocument), "xmlDom", true, GetProperty("nodeScreenInfo", "OwnerDocument")));
            tryBlock.AddStatement(DeclareVariable(typeof(XmlElement), "eltIlboInfo"));
            tryBlock.AddStatement(DeclareVariable(typeof(XmlElement), "eltDsTaskInfo"));
            tryBlock.AddStatement(DeclareVariable(typeof(XmlElement), "eltControlInfo"));
            tryBlock.AddStatement(DeclareVariable(typeof(XmlElement), "eltTask"));
            tryBlock.AddStatement(DeclareVariable("IContext", "ISMContext"));
            tryBlock.AddStatement(DeclareVariable("IAuthorizedActivitiesAndOULists", "IAuthorizedInfo"));
            tryBlock.AddStatement(DeclareVariable("IObjectBroker", "IobjBroker"));
            tryBlock.AddStatement(DeclareVariable("IILBO", "IilboHandle"));
            tryBlock.AddStatement(DeclareVariable("Multiline", "oMultilineControl"));
            tryBlock.AddStatement(DeclareVariable(typeof(string), "sParentActivity"));
            tryBlock.AddStatement(DeclareVariable(typeof(string), "sParentILBO"));
            tryBlock.AddStatement(DeclareVariable(typeof(string), "sParentControlID"));
            tryBlock.AddStatement(DeclareVariable(typeof(long), "lTotalRows"));
            #endregion

            #region initialization
            tryBlock.AddStatement(DeclareVariableAndAssign("ISessionManager", "ISManager", true, SnippetExpression("(ISessionManager)System.Web.HttpContext.Current.Session[\"SessionManager\"]")));
            tryBlock.AddStatement(AssignVariable(VariableReferenceExp("ISMContext"), MethodInvocationExp(VariableReferenceExp("ISManager"), "GetContextObject")));
            tryBlock.AddStatement(AssignVariable(VariableReferenceExp("IAuthorizedInfo"), MethodInvocationExp(VariableReferenceExp("ISManager"), "GetAuthorizedInfoObject")));
            #endregion  

            CodeConditionStatement checkIlboInfo = IfCondition();
            checkIlboInfo.Condition = BinaryOpertorExpression(MethodInvocationExp(VariableReferenceExp("xmlDom"), "SelectSingleNode").AddParameter(PrimitiveExpression("trpi/scri/ii")), CodeBinaryOperatorType.IdentityEquality, SnippetExpression("null"));
            checkIlboInfo.TrueStatements.Add(AssignVariable(VariableReferenceExp("IobjBroker"), MethodInvocationExp(VariableReferenceExp("ISManager"), "GetObjectBroker")));
            checkIlboInfo.TrueStatements.Add(AssignVariable(VariableReferenceExp("sParentActivity"), SnippetExpression("(string)GetContextValue(\"ICT_PARENTACTIVITY\")")));
            checkIlboInfo.TrueStatements.Add(AssignVariable(VariableReferenceExp("sParentILBO"), SnippetExpression("(string)GetContextValue(\"ICT_PARENTILBO\")")));

            CodeConditionStatement checkGridCtrl = IfCondition();
            checkGridCtrl.Condition = BinaryOpertorExpression(MethodInvocationExp(ThisReferenceExp(), "GetContextValue").AddParameter(PrimitiveExpression("ICT_GRID_CONTROL")), CodeBinaryOperatorType.IdentityEquality, SnippetExpression("null"));
            checkGridCtrl.TrueStatements.Add(AssignVariable(VariableReferenceExp("sParentControlID"), SnippetExpression("(string)GetContextValue(\"ICT_PARENTCONTROL\")")));
            checkGridCtrl.TrueStatements.Add(MethodInvocationExp(ThisReferenceExp(), "SetContextValue").AddParameters(new CodeExpression[] { PrimitiveExpression("ICT_GRID_CONTROL"), VariableReferenceExp("sParentControlID") }));
            checkIlboInfo.TrueStatements.Add(checkGridCtrl);

            checkIlboInfo.TrueStatements.Add(AssignVariable(VariableReferenceExp("IilboHandle"), MethodInvocationExp(VariableReferenceExp("IobjBroker"), "GetScreenObject").AddParameters(new CodeExpression[] { VariableReferenceExp("sParentActivity"), VariableReferenceExp("sParentILBO") })));
            checkIlboInfo.TrueStatements.Add(AssignVariable(VariableReferenceExp("oMultilineControl"), SnippetExpression("(Multiline)IilboHandle.GetControl((string)GetContextValue(\"ICT_GRID_CONTROL\"))")));
            checkIlboInfo.TrueStatements.Add(AssignVariable(VariableReferenceExp("lTotalRows"), MethodInvocationExp(VariableReferenceExp("oMultilineControl"), "GetNumInstances")));

            checkIlboInfo.TrueStatements.Add(CommentStatement("Form ILBO Information"));
            checkIlboInfo.TrueStatements.Add(AssignVariable(VariableReferenceExp("eltIlboInfo"), MethodInvocationExp(VariableReferenceExp("xmlDom"), "CreateElement").AddParameter(PrimitiveExpression("ii"))));
            checkIlboInfo.TrueStatements.Add(MethodInvocationExp(VariableReferenceExp("nodeScreenInfo"), "AppendChild").AddParameter(VariableReferenceExp("eltIlboInfo")));
            checkIlboInfo.TrueStatements.Add(MethodInvocationExp(VariableReferenceExp("eltIlboInfo"), "SetAttribute").AddParameters(new CodeExpression[] { PrimitiveExpression("dst"), PrimitiveExpression("0") }));
            checkIlboInfo.TrueStatements.Add(MethodInvocationExp(VariableReferenceExp("ISMContext"), "SetContextValue").AddParameters(new CodeExpression[] { PrimitiveExpression("SCT_SETPAGE_STATUS"), PrimitiveExpression("0") }));

            CodeConditionStatement checkTabName = IfCondition();
            checkTabName.Condition = BinaryOpertorExpression(VariableReferenceExp("sTabName"), CodeBinaryOperatorType.IdentityEquality, GetProperty(TypeReferenceExp(typeof(string)), "Empty"));
            checkTabName.TrueStatements.Add(MethodInvocationExp(VariableReferenceExp("eltIlboInfo"), "SetAttribute").AddParameters(new CodeExpression[] { PrimitiveExpression("at"), SnippetExpression("(string)GetContextValue(\"ICT_ACTIVE_TAB\")") }));

            CodeConditionStatement ifParentIlboIsNotNull = IfCondition();
            ifParentIlboIsNotNull.Condition = BinaryOpertorExpression(BinaryOpertorExpression(SnippetExpression("(string)ISMContext.GetContextValue(\"SCT_LASTTASK_TYPE\")"), CodeBinaryOperatorType.IdentityEquality, PrimitiveExpression("HELP")), CodeBinaryOperatorType.BooleanAnd, BinaryOpertorExpression(MethodInvocationExp(ThisReferenceExp(), "GetContextValue").AddParameter(PrimitiveExpression("ICT_PARENTILBO")), CodeBinaryOperatorType.IdentityInequality, SnippetExpression("null")));
            CodeConditionStatement ifTaskTypeIsNull = IfCondition();
            ifTaskTypeIsNull.Condition = BinaryOpertorExpression(MethodInvocationExp(VariableReferenceExp("ISMContext"), "GetContextValue").AddParameter(PrimitiveExpression("ICT_HELPTASK_TYPE")), CodeBinaryOperatorType.IdentityEquality, SnippetExpression("null"));
            ifTaskTypeIsNull.TrueStatements.Add(MethodInvocationExp(ThisReferenceExp(), "SetContextValue").AddParameters(new CodeExpression[] { PrimitiveExpression("ICT_HELPTASK_TYPE"), MethodInvocationExp(VariableReferenceExp("ISMContext"), "GetContextValue").AddParameter(PrimitiveExpression("SCT_LASTTASK_TYPE")) }));
            ifParentIlboIsNotNull.TrueStatements.Add(ifTaskTypeIsNull);
            checkTabName.TrueStatements.Add(ifParentIlboIsNotNull);

            CodeConditionStatement ifTaskTypeIsHelp = IfCondition();
            ifTaskTypeIsHelp.Condition = BinaryOpertorExpression(SnippetExpression("(string)GetContextValue(\"ICT_HELPTASK_TYPE\")"), CodeBinaryOperatorType.IdentityEquality, PrimitiveExpression("HELP"));
            ifTaskTypeIsHelp.TrueStatements.Add(MethodInvocationExp(VariableReferenceExp("eltIlboInfo"), "SetAttribute").AddParameters(new CodeExpression[] { PrimitiveExpression("ttype"), PrimitiveExpression("HELP") }));
            ifTaskTypeIsHelp.TrueStatements.Add(MethodInvocationExp(VariableReferenceExp("ISMContext"), "SetContextValue").AddParameters(new CodeExpression[] { PrimitiveExpression("SCT_LASTTASK_TYPE"), GetProperty(TypeReferenceExp(typeof(string)), "Empty") }));
            ifTaskTypeIsHelp.FalseStatements.Add(MethodInvocationExp(ThisReferenceExp(), "SetContextValue").AddParameters(new CodeExpression[] { PrimitiveExpression("ICT_HELPTASK_TYPE"), PrimitiveExpression("NOTHELP") }));
            ifTaskTypeIsHelp.FalseStatements.Add(MethodInvocationExp(VariableReferenceExp("eltIlboInfo"), "SetAttribute").AddParameters(new CodeExpression[] { PrimitiveExpression("ttype"), PrimitiveExpression("NOTHELP") }));
            checkTabName.TrueStatements.Add(ifTaskTypeIsHelp);

            checkTabName.TrueStatements.Add(MethodInvocationExp(VariableReferenceExp("eltIlboInfo"), "SetAttribute").AddParameters(new CodeExpression[] { PrimitiveExpression("cr"), SnippetExpression("(string)(oMultilineControl.GetContextValue(\"CCT_CURRENTROW\"))") }));
            checkTabName.TrueStatements.Add(MethodInvocationExp(VariableReferenceExp("eltIlboInfo"), "SetAttribute").AddParameters(new CodeExpression[] { PrimitiveExpression("tr"), MethodInvocationExp(VariableReferenceExp("lTotalRows"), "ToString") }));
            checkIlboInfo.TrueStatements.Add(checkTabName);

            checkIlboInfo.TrueStatements.Add(CommentStatement("Form disabled task info"));
            checkIlboInfo.TrueStatements.Add(AssignVariable(VariableReferenceExp("eltDsTaskInfo"), MethodInvocationExp(VariableReferenceExp("xmlDom"), "CreateElement").AddParameter(PrimitiveExpression("dti"))));
            checkIlboInfo.TrueStatements.Add(MethodInvocationExp(VariableReferenceExp("nodeScreenInfo"), "AppendChild").AddParameter(VariableReferenceExp("eltDsTaskInfo")));

            foreach (TaskService linkTask in _ilbo.TaskServiceList.Where(ts => ts.Type == "link"))
            {
                CodeConditionStatement checkNavigation = IfCondition();
                checkNavigation.Condition = BinaryOpertorExpression(MethodInvocationExp(VariableReferenceExp("IAuthorizedInfo"), "IsNavigationAuthorized").AddParameters(new CodeExpression[] { PrimitiveExpression(_activity.Name), PrimitiveExpression(linkTask.Traversal.ChildActivity) }), CodeBinaryOperatorType.IdentityInequality, PrimitiveExpression(true));
                checkNavigation.TrueStatements.Add(AssignVariable(VariableReferenceExp("eltTask"), MethodInvocationExp(VariableReferenceExp("xmlDom"), "CreateElement").AddParameter(PrimitiveExpression("t"))));
                checkNavigation.TrueStatements.Add(MethodInvocationExp(VariableReferenceExp("eltTask"), "SetAttribute").AddParameters(new CodeExpression[] { PrimitiveExpression("n"), PrimitiveExpression(linkTask.Name) }));
                checkNavigation.TrueStatements.Add(MethodInvocationExp(VariableReferenceExp("eltDsTaskInfo"), "AppendChild").AddParameter(VariableReferenceExp("eltTask")));
                checkIlboInfo.TrueStatements.Add(checkNavigation);
            }

            checkIlboInfo.TrueStatements.Add(CommentStatement("Form control information"));
            CodeConditionStatement checkControlInfo = IfCondition();
            checkControlInfo.Condition = BinaryOpertorExpression(MethodInvocationExp(VariableReferenceExp("xmlDom"), "SelectSingleNode").AddParameter(PrimitiveExpression("trpi/scri/ci")), CodeBinaryOperatorType.IdentityEquality, SnippetExpression("null"));
            checkControlInfo.TrueStatements.Add(AssignVariable(VariableReferenceExp("eltControlInfo"), MethodInvocationExp(VariableReferenceExp("xmlDom"), "CreateElement").AddParameter(PrimitiveExpression("ci"))));
            checkControlInfo.TrueStatements.Add(MethodInvocationExp(VariableReferenceExp("nodeScreenInfo"), "AppendChild").AddParameter(VariableReferenceExp("eltControlInfo")));
            checkControlInfo.FalseStatements.Add(AssignVariable(VariableReferenceExp("eltControlInfo"), SnippetExpression("xmlDom.SelectSingleNode(\"trpi/scri/ci\") as XmlElement")));
            checkIlboInfo.TrueStatements.Add(checkControlInfo);

            checkIlboInfo.FalseStatements.Add(AssignVariable(VariableReferenceExp("eltIlboInfo"), SnippetExpression("xmlDom.SelectSingleNode(\"trpi/scri/ii\") as XmlElement")));
            checkIlboInfo.FalseStatements.Add(AssignVariable(VariableReferenceExp("eltControlInfo"), SnippetExpression("xmlDom.SelectSingleNode(\"trpi/scri/ci\") as XmlElement")));

            tryBlock.AddStatement(checkIlboInfo);

            tryBlock.AddStatement(SnippetStatement("switch(sTabName.ToLower())"));
            tryBlock.AddStatement(SnippetStatement("{"));
            tryBlock.AddStatement(SnippetStatement("default:"));
            CodeConditionStatement ifTabNameIsEmpty = IfCondition();
            ifTabNameIsEmpty.Condition = BinaryOpertorExpression(VariableReferenceExp("sTabName"), CodeBinaryOperatorType.IdentityEquality, GetProperty(TypeReferenceExp(typeof(string)), "Empty"));
            ifTabNameIsEmpty.TrueStatements.Add(MethodInvocationExp(VariableReferenceExp("eltIlboInfo"), "SetAttribute").AddParameters(new CodeExpression[] { PrimitiveExpression("fc"), SnippetExpression("(string)GetContextValue(\"ICT_FOCUSCONTROL\")") }));
            ifTabNameIsEmpty.TrueStatements.Add(MethodInvocationExp(VariableReferenceExp("eltIlboInfo"), "SetAttribute").AddParameters(new CodeExpression[] { PrimitiveExpression("ihsp"), SnippetExpression("(string)GetContextValue(\"ICT_ILBOHSP\")") }));
            ifTabNameIsEmpty.TrueStatements.Add(MethodInvocationExp(ThisReferenceExp(), "SetContextValue").AddParameters(new CodeExpression[] { PrimitiveExpression("ICT_ILBOHSP"), PrimitiveExpression("0") }));
            ifTabNameIsEmpty.TrueStatements.Add(MethodInvocationExp(VariableReferenceExp("eltIlboInfo"), "SetAttribute").AddParameters(new CodeExpression[] { PrimitiveExpression("ivsp"), SnippetExpression("(string)GetContextValue(\"ICT_ILBOVSP\")") }));
            ifTabNameIsEmpty.TrueStatements.Add(MethodInvocationExp(ThisReferenceExp(), "SetContextValue").AddParameters(new CodeExpression[] { PrimitiveExpression("ICT_ILBOVSP"), PrimitiveExpression("0") }));
            foreach (Control control in _ilbo.Controls)
            {
                ifTabNameIsEmpty.TrueStatements.Add(MethodInvocationExp(VariableReferenceExp(string.Format("m_con{0}", control.Id)), "RenderAsXML").AddParameters(new CodeExpression[] { GetProperty("RenderType", "renderComplete"), VariableReferenceExp("eltControlInfo") }));
            }

            CodeTryCatchFinallyStatement innerTry = new CodeTryCatchFinallyStatement();
            CodeCatchClause innerCatch = innerTry.AddCatch("stEx");
            innerCatch.AddStatement(MethodInvocationExp(VariableReferenceExp("Trace"), "WriteLineIf").AddParameters(new CodeExpression[] { GetProperty(GetProperty("SessionManager", "m_ILActTraceSwitch"), "TraceError"), GetProperty("stEx", "Message"), PrimitiveExpression(string.Format("{0} : GetScreenData", _ilbo.Code)) }));

            ifTabNameIsEmpty.TrueStatements.Add(innerTry);

            ifTabNameIsEmpty.FalseStatements.Add(ThrowNewException("Invalid TabName"));

            tryBlock.AddStatement(ifTabNameIsEmpty);
            tryBlock.AddStatement(SnippetExpression("break"));
            tryBlock.AddStatement(SnippetStatement("}"));

            CodeCatchClause catchClause = tryBlock.AddCatch("e");
            CodeMethodInvokeExpression methodInvokation = MethodInvocationExp(ThisReferenceExp(), "FillMessageObject").AddParameters(new CodeExpression[] { SnippetExpression("string.Format(\"zsta_whcap_ : GetScreenData(sTabName = {0}, nodeScreenInfo = {1}\",sTabName,nodeScreenInfo.OuterXml)"),
                                                                                                                                                            PrimitiveExpression("ILBO0016"),
                                                                                                                                                            GetProperty("e","Message")});
            catchClause.AddStatement(methodInvokation);
            catchClause.AddStatement(new CodeThrowExceptionStatement(ObjectCreateExpression(typeof(Exception), new CodeExpression[] { GetProperty("e", "Message"), VariableReferenceExp("e") })));

            return GetScreenData;
        }

        private CodeMemberMethod PerformTaskCallout()
        {
            CodeMemberMethod PerformTaskCallout = new CodeMemberMethod
            {
                Name = "PerformTaskCallout",
                Attributes = MemberAttributes.Public | MemberAttributes.Override,
                ReturnType = new CodeTypeReference(typeof(Boolean))
            };
            try
            {
                #region function parameters
                CodeParameterDeclarationExpression pTaskName = new CodeParameterDeclarationExpression(typeof(string), "taskName");
                CodeParameterDeclarationExpression pMode = new CodeParameterDeclarationExpression("TaskCalloutMode", "mode");
                PerformTaskCallout.Parameters.Add(pTaskName);
                PerformTaskCallout.Parameters.Add(pMode);
                #endregion

                CodeTryCatchFinallyStatement tryBlock = new CodeTryCatchFinallyStatement();
                PerformTaskCallout.AddStatement(tryBlock);

                #region variable declaration
                tryBlock.AddStatement(DeclareVariableAndAssign(typeof(Boolean), "result", true, PrimitiveExpression(true)));
                tryBlock.AddStatement(DeclareVariableAndAssign("ISessionManager", "ISManager", true, SnippetExpression("(ISessionManager)System.Web.HttpContext.Current.Session[\"SessionManager\"]")));
                tryBlock.AddStatement(DeclareVariableAndAssign("ITaskCalloutExecutor", "ICExecutor", true, MethodInvocationExp(VariableReferenceExp("ISManager"), "GetTaskCalloutExecutor")));
                #endregion

                #region pre and post callout
                CodeConditionStatement ifCalloutModeIsPreCallout = IfCondition();
                ifCalloutModeIsPreCallout.Condition = BinaryOpertorExpression(VariableReferenceExp("mode"), CodeBinaryOperatorType.IdentityEquality, GetProperty("TaskCalloutMode", "PreTask"));

                #region pre
                IEnumerable<TaskService> PreCalloutTasks = _ilbo.TaskServiceList.Where(ts => ts.TaskCallout != null && ts.TaskCallout.PreCallout != null);
                if (PreCalloutTasks.Any())
                {
                    ifCalloutModeIsPreCallout.TrueStatements.Add(SnippetStatement("switch (taskName)"));
                    ifCalloutModeIsPreCallout.TrueStatements.Add(SnippetStatement("{"));
                    foreach (TaskService task in PreCalloutTasks)
                    {
                        ifCalloutModeIsPreCallout.TrueStatements.Add(SnippetStatement(string.Format("case \"{0}\":", task.Name)));
                        ifCalloutModeIsPreCallout.TrueStatements.Add(MethodInvocationExp(VariableReferenceExp("ICExecutor"), "SetCalloutContext").AddParameters(new CodeExpression[] { PrimitiveExpression(_ecrOptions.Component), PrimitiveExpression(_activity.Name), PrimitiveExpression(_ilbo.Code), PrimitiveExpression(task.TaskCallout.Name) }));
                        foreach (TaskCalloutSegment segment in task.TaskCallout.PreCallout.Segments)
                        {
                            string sSegFlow = string.Empty;

                            if (segment.FlowDirection == "0")
                                sSegFlow = "flowIn";
                            else if (segment.FlowDirection == "1")
                                sSegFlow = "flowOut";
                            else if (segment.FlowDirection == "2")
                                sSegFlow = "flowInOut";

                            ifCalloutModeIsPreCallout.TrueStatements.Add(MethodInvocationExp(VariableReferenceExp("ICExecutor"), "SetCalloutSegment").AddParameters(new CodeExpression[] { PrimitiveExpression(segment.Name), PrimitiveExpression(Convert.ToString(segment.Sequence)), PrimitiveExpression(segment.Instance.Equals(1)), GetProperty("FlowAttribute", sSegFlow), PrimitiveExpression(segment.Filling) }));

                            foreach (TaskCalloutDataItem dataitem in segment.DataItems)
                            {
                                string sDiFlow = string.Empty;

                                if (dataitem.FlowDirection == "0")
                                    sDiFlow = "flowIn";
                                else if (dataitem.FlowDirection == "1")
                                    sDiFlow = "flowOut";
                                else if (dataitem.FlowDirection == "2")
                                    sDiFlow = "flowInOut";
                                ifCalloutModeIsPreCallout.TrueStatements.Add(MethodInvocationExp(VariableReferenceExp("ICExecutor"), "SetCalloutDataSource").AddParameters(new CodeExpression[] { PrimitiveExpression(dataitem.Name), GetProperty("FlowAttribute", sDiFlow), PrimitiveExpression(dataitem.ControlId), PrimitiveExpression(dataitem.ViewName), GetProperty(TypeReferenceExp(typeof(string)), "Empty") }));
                            }
                        }
                        ifCalloutModeIsPreCallout.TrueStatements.Add(AssignVariable(VariableReferenceExp("result"), MethodInvocationExp(VariableReferenceExp("ICExecutor"), "ExecuteCalloutService")));
                        ifCalloutModeIsPreCallout.TrueStatements.Add(SnippetExpression("break"));
                    }
                    ifCalloutModeIsPreCallout.TrueStatements.Add(SnippetStatement("}"));
                }
                #endregion

                #region post
                CodeConditionStatement ifCalloutModeIsPostCallout = IfCondition();
                ifCalloutModeIsPostCallout.Condition = BinaryOpertorExpression(VariableReferenceExp("mode"), CodeBinaryOperatorType.IdentityEquality, GetProperty("TaskCalloutMode", "PostTask"));
                ifCalloutModeIsPreCallout.FalseStatements.Add(ifCalloutModeIsPostCallout);

                IEnumerable<TaskService> PostCalloutTasks = _ilbo.TaskServiceList.Where(ts => ts.TaskCallout != null && ts.TaskCallout.PostCallout != null);
                if (PostCalloutTasks.Any())
                {
                    ifCalloutModeIsPostCallout.TrueStatements.Add(SnippetStatement("switch (taskName)"));
                    ifCalloutModeIsPostCallout.TrueStatements.Add(SnippetStatement("{"));
                    foreach (TaskService task in PostCalloutTasks)
                    {
                        ifCalloutModeIsPostCallout.TrueStatements.Add(SnippetStatement(string.Format("case \"{0}\":", task.Name)));
                        ifCalloutModeIsPostCallout.TrueStatements.Add(MethodInvocationExp(VariableReferenceExp("ICExecutor"), "SetCalloutContext").AddParameters(new CodeExpression[] { PrimitiveExpression(_ecrOptions.Component), PrimitiveExpression(_activity.Name), PrimitiveExpression(_ilbo.Code), PrimitiveExpression(task.TaskCallout.Name) }));
                        foreach (TaskCalloutSegment segment in task.TaskCallout.PostCallout.Segments)
                        {
                            string sSegFlow = string.Empty;

                            if (segment.FlowDirection == "0")
                                sSegFlow = "flowIn";
                            else if (segment.FlowDirection == "1")
                                sSegFlow = "flowOut";
                            else if (segment.FlowDirection == "2")
                                sSegFlow = "flowInOut";

                            ifCalloutModeIsPostCallout.TrueStatements.Add(MethodInvocationExp(VariableReferenceExp("ICExecutor"), "SetCalloutSegment").AddParameters(new CodeExpression[] { PrimitiveExpression(segment.Name), PrimitiveExpression(Convert.ToString(segment.Sequence)), PrimitiveExpression(segment.Instance.Equals(1)), GetProperty("FlowAttribute", sSegFlow), PrimitiveExpression(segment.Filling) }));
                            foreach (TaskCalloutDataItem dataitem in segment.DataItems)
                            {
                                string sDiFlow = string.Empty;

                                if (dataitem.FlowDirection == "0")
                                    sDiFlow = "flowIn";
                                else if (dataitem.FlowDirection == "1")
                                    sDiFlow = "flowOut";
                                else if (dataitem.FlowDirection == "2")
                                    sDiFlow = "flowInOut";
                                ifCalloutModeIsPostCallout.TrueStatements.Add(MethodInvocationExp(VariableReferenceExp("ICExecutor"), "SetCalloutDataSource").AddParameters(new CodeExpression[] { PrimitiveExpression(dataitem.Name), GetProperty("FlowAttribute", sDiFlow), PrimitiveExpression(dataitem.ControlId), PrimitiveExpression(dataitem.ViewName), GetProperty(TypeReferenceExp(typeof(string)), "Empty") }));
                            }
                        }
                        ifCalloutModeIsPostCallout.TrueStatements.Add(AssignVariable(VariableReferenceExp("result"), MethodInvocationExp(VariableReferenceExp("ICExecutor"), "ExecuteCalloutService")));
                        ifCalloutModeIsPostCallout.TrueStatements.Add(SnippetExpression("break"));
                    }
                    ifCalloutModeIsPostCallout.TrueStatements.Add(SnippetStatement("}"));
                }


                #endregion


                tryBlock.AddStatement(ifCalloutModeIsPreCallout);

                tryBlock.AddStatement(ReturnExpression(VariableReferenceExp("result")));

                CodeCatchClause catchBlock = AddCatchBlock(tryBlock);
                catchBlock.AddStatement(MethodInvocationExp(ThisReferenceExp(), "FillMessageObject").AddParameters(new CodeExpression[] { SnippetExpression(string.Format("\"{0} : PerformTaskCallout(taskName = \\\"\" + taskName + \"\\\")\"", _ilbo.Code)), PrimitiveExpression("ILBO0104"), GetProperty("e", "Message") }));
                catchBlock.AddStatement(new CodeThrowExceptionStatement());
                #endregion  

                return PerformTaskCallout;
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("PerformTaskCallout->{0}", ex.InnerException != null ? ex.InnerException.Message : ex.Message));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private CodeMemberMethod AddFillMessageObject()
        {
            CodeMemberMethod FillMessageObject = new CodeMemberMethod
            {
                Name = "FillMessageObject",
                Attributes = MemberAttributes.Private
            };

            try
            {
                //method summary
                AddMethodSummary(FillMessageObject, "Fills the Message object when an error occurs");
                FillMessageObject.Parameters.Add(ParameterDeclarationExp(typeof(String), "sMethod"));
                FillMessageObject.Parameters.Add(ParameterDeclarationExp(typeof(String), "sErrNumber"));
                FillMessageObject.Parameters.Add(ParameterDeclarationExp(typeof(String), "sErrMessage"));

                //tryBlock
                CodeTryCatchFinallyStatement tryBlock = AddTryBlock(FillMessageObject);

                if (_ilbo.HasBaseCallout)
                    tryBlock.AddStatement(MethodInvocationExp(VariableReferenceExp("objActBaseEx"), "FillMessageObject").AddParameters(new CodeExpression[] { VariableReferenceExp("sMethod"), VariableReferenceExp("sErrNumber"), VariableReferenceExp("sErrMessage") }));

                tryBlock.AddStatement(AddTraceInfo(SnippetExpression("String.Format(\"" + _ilbo.Code + " : FillMessageObject(sMethod = \\\"{0}\\\",sErrNumber = \\\"{0}\\\",sErrMessage = \\\"{0}\\\")\", sMethod, sErrNumber, sErrMessage)")));
                tryBlock.AddStatement(AddISManager());
                tryBlock.AddStatement(CodeDomHelper.DeclareVariableAndAssign("IMessage", "Imsg", true, MethodInvocationExp(VariableReferenceExp("ISManager"), "GetMessageObject")));
                CodeMethodInvokeExpression methodInvocation = MethodInvocationExp(VariableReferenceExp("Imsg"), "AddMessage");
                AddParameters(methodInvocation, new Object[] { ArgumentReferenceExp("sErrNumber"), ArgumentReferenceExp("sErrMessage"), ArgumentReferenceExp("sMethod"), GetProperty(typeof(String), "Empty"), "5" });
                tryBlock.AddStatement(methodInvocation);

                //catchBlock
                CodeCatchClause catchBlock = AddCatchBlock(tryBlock);
                catchBlock.Statements.Add(AddTraceError(SnippetExpression("String.Format(\"" + _ilbo.Code + " : FillMessageObject(sMethod = \\\"{0}\\\",sErrNumber = \\\"{0}\\\",sErrMessage = \\\"{0}\\\")\",sMethod,sErrNumber,sErrMessage)")));
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("AddFillMessageObject->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }

            return FillMessageObject;
        }


        ///// <summary>
        ///// 
        ///// </summary>
        //private void ImportNamespace()
        //{
        //    ILBOCs.ReferencedNamespace.Add("System");
        //    ILBOCs.ReferencedNamespace.Add("System.Web");
        //    ILBOCs.ReferencedNamespace.Add("System.Xml");
        //    ILBOCs.ReferencedNamespace.Add("System.Collections");
        //    ILBOCs.ReferencedNamespace.Add("System.Collections.Generic");
        //    ILBOCs.ReferencedNamespace.Add("System.Diagnostics");

        //    ILBOCs.ReferencedNamespace.Add("Ramco.VW.RT.Web.Core");
        //    ILBOCs.ReferencedNamespace.Add("Ramco.VW.RT.Web.Controls");
        //    ILBOCs.ReferencedNamespace.Add("Ramco.VW.RT.Web.Core.Controls.LayoutControls");
        //    ILBOCs.ReferencedNamespace.Add("Ramco.VW.RT.AsyncResult");
        //    ILBOCs.ReferencedNamespace.Add("Ramco.VW.RT.State");

        //    ILBOCs.ReferencedNamespace.Add("Plf.Ui.Ramco.Utility");
        //    ILBOCs.ReferencedNamespace.Add("System.Reflection");

        //    if (ilbo.HasBaseCallout)
        //    {
        //        ILBOCs.ReferencedNamespace.Add("Plf.Itk.Ramco.Callout");
        //        ILBOCs.ReferencedNamespace.Add("Plf.Ramco.WebCallout.Interface");
        //    }
        //}


        ///// <summary>
        /////
        ///// </summary>
        ///// <returns></returns>
        //private CodeUnit GetProduct()
        //{
        //    return ILBOCs;
        //}


        ///// <summary>
        /////  Entrypoint method for generating ilbo class file.
        ///// </summary>
        //internal void Generate()
        //{
        //    logger.WriteLogToFile("Generate", String.Format("creating ilbo - {0}", ilbo.Name), bLogTiming: true);
        //    GenerateILBOCls();
        //}


        /// <summary>
        /// return ISManager declaration & inline initialization statement
        /// </summary>
        /// <returns></returns>
        private CodeStatement AddISManager()
        {
            return DeclareVariableAndAssign("ISessionManager", "ISManager", true, SnippetExpression("(ISessionManager)System.Web.HttpContext.Current.Session[\"SessionManager\"]"));
        }


        /// <summary>
        /// returns ISMContext declaration & inline initialization statement
        /// </summary>
        /// <returns></returns>
        private CodeStatement AddISMContext()
        {
            return DeclareVariableAndAssign("IContext", "ISMContext", true, MethodInvocationExp(TypeReferenceExp("ISManager"), "GetContextObject"));
        }


        /// <summary>
        /// return ISExecutor declaration statement
        /// </summary>
        /// <returns></returns>
        private CodeStatement AddISExecutor()
        {
            //return DeclareVariable("IServiceExecutor", "ISExecutor");
            return DeclareVariableAndAssign("IServiceExecutor", "ISExecutor", true, MethodInvocationExp(TypeReferenceExp("ISManager"), "GetServiceExecutor"));
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="sContextName"></param>
        /// <param name="sContextValue"></param>
        /// <returns></returns>
        private CodeExpression SetContextValue(Object ContextName, Object ContextValue)
        {
            CodeMethodInvokeExpression newMethodInvocation = MethodInvocationExp(ThisReferenceExp(), "SetContextValue");
            AddParameters(newMethodInvocation, new Object[] { ContextName, ContextValue });
            return newMethodInvocation;
        }

        private CodeExpression ControlSetContextValue(Object ContextName, Object ContextValue, String ControlName)
        {
            CodeMethodInvokeExpression newMethodInvocation = new CodeMethodInvokeExpression();
            newMethodInvocation.Method.TargetObject = TypeReferenceExp(ControlName);
            newMethodInvocation.Method.MethodName = "SetContextValue";
            AddParameters(newMethodInvocation, new Object[] { ContextName, ContextValue });
            return newMethodInvocation;
        }


        /// <summary>
        /// SetContextValue for BeginPerformTask method
        /// </summary>
        /// <param name="sTaskType"></param>
        /// <returns></returns>
        private CodeExpression ISMSetContextValue(String ContextName, String ContextValue)
        {
            CodeMethodInvokeExpression methodInvocation;

            methodInvocation = MethodInvocationExp(TypeReferenceExp("ISMContext"), "SetContextValue");
            AddParameters(methodInvocation, new Object[] { ContextName, ContextValue.ToUpper() });

            return methodInvocation;
        }


        /// <summary>
        /// Forms MethodInvokeExpression for RenderAsXML for controls
        /// </summary>
        /// <param name="sControlId">controlid</param>
        /// <param name="bIsLayoutControl">whether the control is layout control or not</param>
        /// <param name="bFilling">filling enabled or not</param>
        /// <returns>CodeMethodInvokeExpression</returns>
        private CodeMethodInvokeExpression AddRenderAsXML(Control control, bool bIsLayoutControl, bool bFilling)
        {
            CodePropertyReferenceExpression RenderType;
            CodeVariableReferenceExpression ControlInfoNode;
            string sControlId = string.Empty;

            sControlId = bIsLayoutControl ? String.Format("_{0}", control.Id) : String.Format("m_con{0}", control.Id);
            RenderType = PropertyReferenceExp(TypeReferenceExp("RenderType"), bFilling ? "renderComplete" : "renderModified");

            if (control.Type == "rspivotgrid")
                RenderType = PropertyReferenceExp(TypeReferenceExp("RenderType"), "renderPivotData");
            //RenderType = PropertyReferenceExp(TypeReferenceExp("RenderType"), bFilling ? "renderComplete" : "renderModified");

            ControlInfoNode = VariableReferenceExp(bIsLayoutControl ? "eltLayoutControlInfo" : "eltControlInfo");

            CodeMethodInvokeExpression methodInvokation = MethodInvocationExp(TypeReferenceExp(sControlId), "RenderAsXML");
            AddParameters(methodInvokation, new Object[] { RenderType, ControlInfoNode });

            return methodInvokation;
        }

        private void GetTaskData_ReportTask(CodeTryCatchFinallyStatement tryBlock, CodeConditionStatement checkInlineTab)
        {
            CodeMethodInvokeExpression methodInvokation;
            try
            {
                IEnumerable<TaskService> reportTasks = from ts in _ilbo.TaskServiceList
                                                       where ts.ReportInfo != null
                                                       && ts.ReportInfo.ProcessingType == 1
                                                       select ts;

                //report tasks
                foreach (TaskService task in reportTasks.OrderBy(rt => rt.Name))
                {
                    checkInlineTab.TrueStatements.Add(SnippetStatement(String.Format("case \"{0}\":", task.Name)));

                    //foreach (TaskData taskData in reportTask.TaskInfos)
                    //{
                    methodInvokation = MethodInvocationExp(TypeReferenceExp("xmlDom"), "CreateElement");
                    AddParameters(methodInvokation, new Object[] { task.ReportInfo.ContextName });
                    checkInlineTab.TrueStatements.Add(AssignVariable("eltContextName", methodInvokation));

                    checkInlineTab.TrueStatements.Add(AssignVariable("sOutMTD", MethodInvocationExp(TypeReferenceExp("ISExecutor"), "GetLastOutMTD")));
                    checkInlineTab.TrueStatements.Add(SetProperty("eltContextName", "InnerText", VariableReferenceExp("sOutMTD")));

                    methodInvokation = MethodInvocationExp(TypeReferenceExp("nodeScreenInfo"), "AppendChild");
                    AddParameters(methodInvokation, new Object[] { VariableReferenceExp("eltContextName") });
                    checkInlineTab.TrueStatements.Add(methodInvokation);
                    //}

                    checkInlineTab.TrueStatements.Add(SnippetExpression("break"));
                }
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("GetTaskData_ReportTask->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }


        /// <summary>
        /// Returns expressions of PLFRichcontrols(tree,chart) for GetTaskData method
        /// </summary>
        /// <param name="sTaskName">TaskName</param>
        /// <param name="sTabName">TabName</param>
        /// <param name="sPostTask">PostTask</param>
        /// <returns></returns>
        private CodeExpressionCollection GetTaskData_PlfRichControls(TaskService task, String sTabName)
        {
            string postTask = string.Empty;
            CodeExpressionCollection expressions = new CodeExpressionCollection();

            if (task.Traversal != null)
                postTask = task.Traversal.PostTask;

            try
            {
                //tree
                foreach (Tree tree in task.Trees)
                {
                    if (tree.Page == sTabName)
                    {
                        CodeMethodInvokeExpression methodInvokation = MethodInvocationExp(TypeReferenceExp(TREECONTROL), "GetTaskData_Tree");
                        AddParameters(methodInvokation, new Object[] { SnippetExpression("ref htContextItems"), (String.IsNullOrEmpty(sTabName) || sTabName == "mainpage") ? SnippetExpression("String.Empty") : SnippetExpression("sTabName") });

                        if (!String.IsNullOrEmpty(postTask))
                            AddParameters(methodInvokation, new Object[] { postTask });
                        else
                            AddParameters(methodInvokation, new Object[] { SnippetExpression("sTaskName") });

                        AddParameters(methodInvokation, new Object[] { SnippetExpression("ref nodeScreenInfo") });

                        expressions.Add(methodInvokation);
                    }

                }

                //chart
                //foreach (Chart chart in task.Charts)
                //{
                if (task.Charts.Where(c => c.TabName == sTabName).Any())
                {
                    CodeMethodInvokeExpression methodInvokation = MethodInvocationExp(TypeReferenceExp(CHARTCONTROL), "Update_ScreenData_Chart");
                    AddParameters(methodInvokation, new Object[] { FieldReferenceExp(ThisReferenceExp(), "htContextItems"), (String.IsNullOrEmpty(sTabName) || String.Compare(sTabName, "mainpage", true) == 0) ? SnippetExpression("String.Empty") : SnippetExpression("sTabName"), SnippetExpression("ref nodeScreenInfo") });
                    expressions.Add(methodInvokation);
                }
                //}

            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("GetTaskData_PlfRichControls->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }

            return expressions;
        }


        /// <summary>
        /// Returns DirtyTab calling portion for GetTaskData method
        /// </summary>
        /// <param name="sTaskName">TaskName</param>
        /// <param name="sTabName">TabName</param>
        /// <returns>CodeExpressionCollection</returns>
        private CodeExpressionCollection GetTaskData_AddDirtyTab(TaskService task, String sTabName, bool bIsDefault)
        {
            CodeExpressionCollection expressions = new CodeExpressionCollection();
            CodeMethodInvokeExpression methodInvokation;
            try
            {
                IEnumerable<string> dirtyTabs;

                if (bIsDefault)
                {
                    dirtyTabs = (from ti in task.TaskInfos
                                 where ti.TabName != "mainpage"
                                 && ti.TabName != ""
                                 select ti.TabName).Distinct();
                }
                else
                {
                    dirtyTabs = (from ti in task.TaskInfos
                                 where ti.TabName != "mainpage"
                                 && ti.TabName != ""
                                 && ti.TabName != sTabName
                                 select ti.TabName).Distinct();
                }

                foreach (string pageName in dirtyTabs)
                {
                    methodInvokation = MethodInvocationExp(ThisReferenceExp(), "AddDirtyTab");
                    AddParameters(methodInvokation, new Object[] { VariableReferenceExp("xmlDom"), VariableReferenceExp("eltDTabs"), pageName });
                    expressions.Add(methodInvokation);
                }
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("GetTaskData_AddDirtyTab->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }

            return expressions;
        }

        //private void GetServiceAttributes(String sTaskName, String sServiceName, String sSegmentName, String sDataItemName, ref String sPropertyName, ref String sPropertyType)
        //{
        //    StringBuilder sbQuery = new StringBuilder();

        //    try
        //    {
        //        sbQuery.AppendLine("select PropertyType, PropertyName  from #fw_des_publish_ilbo_service_view_attributemap (nolock) ");
        //        sbQuery.AppendLine(" Where ActivityId = @ActivityID");
        //        sbQuery.AppendLine(" and ILBOCode = @ILBOCode");
        //        sbQuery.AppendLine(" and TaskName = @TaskName");
        //        sbQuery.AppendLine(" and ServiceName = @ServiceName");
        //        sbQuery.AppendLine(" and SegmentName = @SegmentName");
        //        sbQuery.AppendLine(" and DataItemName = @DataItemName");

        //        GlobalVar.var parameters = dbManager.CreateParameters(6);
        //        GlobalVar.dbManager.AddParamters(parameters,0, "@ActivityID", ilbo.ActivityId);
        //        GlobalVar.dbManager.AddParamters(parameters,1, "@ILBOCode", ilbo.Name);
        //        GlobalVar.dbManager.AddParamters(parameters,2, "@TaskName", sTaskName);
        //        GlobalVar.dbManager.AddParamters(parameters,3, "@ServiceName", sServiceName);
        //        GlobalVar.dbManager.AddParamters(parameters,4, "@SegmentName", sSegmentName);
        //        GlobalVar.dbManager.AddParamters(parameters,5, "@DataItemName", sDataItemName);
        //        DataTable dtProperty = GlobalVar.dbManager.ExecuteDataTable(CommandType.Text, sbQuery.ToString());

        //        if (dtProperty.Rows.Count > 0)
        //        {
        //            sPropertyName = dtProperty.Rows[0]["PropertyName"].ToString();
        //            sPropertyType = dtProperty.Rows[0]["PropertyType"].ToString();
        //        }

        //    }
        //    catch (Exception ex)
        //    {
        //        throw new Exception(String.Format("GetServiceAttributes->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
        //    }
        //}


        //private String GetListEditControlID(String sControlID)
        //{
        //    String sRetControlID = String.Empty;
        //    StringBuilder sbQuery = new StringBuilder();

        //    try
        //    {
        //        sbQuery.AppendLine("select ControlID, Instance from #de_listedit_view_datamap(nolock) where activity_name= @ActivityName");
        //        sbQuery.AppendLine(" and ilbocode = @ILBOCode and listedit = @ControlID");
        //        GlobalVar.var parameters = dbManager.CreateParameters(3);
        //        GlobalVar.dbManager.AddParamters(parameters,0, "@ActivityName", ilbo.ActivityName);
        //        GlobalVar.dbManager.AddParamters(parameters,1, "@ILBOCode", ilbo.Name);
        //        GlobalVar.dbManager.AddParamters(parameters,2, "@ControlID", sControlID);
        //        object result = GlobalVar.dbManager.ExecuteScalar(CommandType.Text, sbQuery.ToString());

        //        if (!object.Equals(result, null))
        //        {
        //            sRetControlID = (String)result;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        throw new Exception(String.Format("GetListEditControlID->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
        //    }

        //    return sRetControlID;
        //}

        private void WritePostTask(String CallingMethod, TaskService task, CodeTryCatchFinallyStatement tryBlock)
        {
            try
            {
                CodeMethodInvokeExpression methodInvokation;

                //if (CallingMethod == "PerformTask")
                //{
                // For Post Help Task
                #region getting posttask for help task
                if (task.Type.Equals("help"))
                {
                    if (!String.IsNullOrEmpty(task.Traversal.PostTask))
                    {
                        methodInvokation = MethodInvocationExp(ThisReferenceExp(), "SetContextValue");
                        AddParameters(methodInvokation, new Object[] { "ICT_POSTHELPTASK", task.Traversal.PostTask });
                        tryBlock.AddStatement(methodInvokation);
                    }
                }
                #endregion getting posttask for help task

                // For Post Link Task
                #region getting posttask for link task
                if (task.Type.Equals("link"))
                {
                    if (!String.IsNullOrEmpty(task.Traversal.PostTask))
                    {
                        methodInvokation = MethodInvocationExp(ThisReferenceExp(), "SetContextValue");
                        AddParameters(methodInvokation, new Object[] { "ICT_POSTLINKTASK", task.Traversal.PostTask });
                        tryBlock.AddStatement(methodInvokation);
                    }
                }
                #endregion getting posttask for link task
                //}
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("WritePostTask->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }


        //private String GetPostTask(String sTaskName, String sTaskType)
        //{
        //    StringBuilder sbQuery = new StringBuilder();
        //    String sPostTask = String.Empty;

        //    try
        //    {
        //        if (sTaskType.Equals("help"))
        //        {
        //            sbQuery.AppendLine("SELECT 'PostTask' = LOWER(PostTask) FROM #fw_req_ilbo_linkuse (nolock) WHERE taskname = @taskname AND parentilbocode = @ilbocode AND PostTask IS NOT NULL");
        //        }
        //        else
        //        {
        //            sbQuery.AppendLine("SELECT 'PostLinkTask' = LOWER(Post_linkTask) FROM #fw_req_ilbo_linkuse (nolock) WHERE taskname = @taskname AND parentilbocode = @ilbocode AND isnull(Post_linkTask, '') <> ''");
        //        }

        //        GlobalVar.var parameters = dbManager.CreateParameters(2);
        //        GlobalVar.dbManager.AddParamters(parameters,0, "@taskname", sTaskName);
        //        GlobalVar.dbManager.AddParamters(parameters,1, "@ilbocode", ilbo.Name);
        //        Object spReturnValue = GlobalVar.dbManager.ExecuteScalar(CommandType.Text, sbQuery.ToString());
        //        sPostTask = object.Equals(spReturnValue, null) ? String.Empty : (String)spReturnValue;
        //    }
        //    catch (Exception ex)
        //    {
        //        throw new Exception(String.Format("GetPostTask->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
        //    }

        //    return sPostTask;
        //}

        private String GetLaunchILBO(String sTabName)
        {
            try
            {
                String sLaunchILBO = String.Empty;

                if (sTabName == "mainpage")
                {
                    if (_ilbo.Code.Contains("zoom_"))
                        sLaunchILBO = String.Format("Zoom_{0}_{1}.htm", _activity.Name, _ilbo.Code);
                    else
                        sLaunchILBO = String.Format("{0}_{1}.htm", _activity.Name, _ilbo.Code);
                }
                else
                {
                    if (_ilbo.Code.Contains("zoom_"))
                        sLaunchILBO = String.Format("Zoom_{0}_{1}.htm", _activity.Name, _ilbo.Code);
                    else
                        sLaunchILBO = String.Format("{0}_{1}_{2}.htm", _activity.Name, _ilbo.Code, sTabName);
                }

                return sLaunchILBO;
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("GetLaunchILBO->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }

        public new bool Generate()
        {
            try
            {
                _logger.WriteLogToFile("GenerateILBOClass.Generate()", string.Format("generating ilbo - {0}", _ilbo.Code));

                base.Generate();

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("GenerateILBOClass.Generate->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }
    }
}
