using System;
using System.Collections.Generic;
using System.Linq;
using System.CodeDom;
using System.Data;
using System.IO;

namespace Ramco.VwPlf.CodeGenerator.WebLayer
{
    internal class ChartClassGenerator : AbstractCSFileGenerator
    {

        private Activity _activity = null;
        private ILBO _ilbo = null;
        private ECRLevelOptions _ecrOptions = null;

        /// <summary>
        /// Constructor Method
        /// </summary>
        /// <param name="sActivityName">Name of the Activity which will be the Namespace of the Chart class.</param>
        /// <param name="sIlboCode">ILBO Code which will be given as class name.</param>
        public ChartClassGenerator(Activity activity, ILBO ilbo, ref ECRLevelOptions ecrOptions) : base(ecrOptions)
        {
            this._activity = activity;
            this._ilbo = ilbo;
            this._ecrOptions = ecrOptions;
            base._targetFileName = string.Format("{0}_ch", ilbo.Code);
            base._targetDir = Path.Combine(_ecrOptions.GenerationPath, _ecrOptions.Platform, _ecrOptions.Customer, _ecrOptions.Project, _ecrOptions.Ecrno, "Updated", _ecrOptions.Component, "Source", "ILBO", activity.Name);
        }

        public override void CreateNamespace()
        {
            try
            {
                base._csFile.NameSpace.Name = _activity.Name;
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("CreateNameSpace->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }
        public override void ImportNamespace()
        {
            _csFile.ReferencedNamespace.Add("System");
            _csFile.ReferencedNamespace.Add("System.Xml");
            _csFile.ReferencedNamespace.Add("System.Collections");
            _csFile.ReferencedNamespace.Add("System.Collections.Generic");
            _csFile.ReferencedNamespace.Add("System.Diagnostics");
            _csFile.ReferencedNamespace.Add("Plf.Ui.Ramco.Utility");
        }
        public override void AddCustomAttributes()
        {
            //throw new NotImplementedException();
        }
        public override void CreateClasses()
        {
            try
            {
                CodeTypeDeclaration chartClass = new CodeTypeDeclaration
                {
                    Name = string.Format("{0}_ch", _ilbo.Code),
                    IsClass = true
                };

                AddMemberFields(ref chartClass);
                AddMemberFunctions(ref chartClass);

                _csFile.UserDefinedTypes.Add(chartClass);
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("CreateClasses->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }
        public override void AddMemberFields(ref CodeTypeDeclaration classObj)
        {
            try
            {
                DeclareMemberField(MemberAttributes.Private, classObj, "oChartControl", "Chartcontrol", true);
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("AddMemberFields->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }
        public override void AddMemberFunctions(ref CodeTypeDeclaration classObj)
        {
            try
            {
                classObj.Members.Add(ProcessChart());
                classObj.Members.Add(PopulateChartContext());
                classObj.Members.Add(Update_ScreenData_Chart());
                classObj.Members.Add(ResetChartControls());

            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("AddMemberFunctions->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }

        private CodeMemberMethod ProcessChart()
        {
            CodeMemberMethod ProcessChart = null;
            CodeMethodInvokeExpression MethodInvokation = null;

            try
            {
                ProcessChart = new CodeMemberMethod
                {
                    Name = "ProcessChart",
                    Attributes = (MemberAttributes.Public | MemberAttributes.Final),
                    ReturnType = new CodeTypeReference(typeof(bool))
                };

                //add method parameters
                CodeParameterDeclarationExpression pContexItemChart = new CodeParameterDeclarationExpression
                {
                    Name = "cContextItemsChart",
                    Type = new CodeTypeReference(typeof(Dictionary<string, Object>)),
                    Direction = FieldDirection.Ref
                };
                CodeParameterDeclarationExpression pOutMTD = new CodeParameterDeclarationExpression
                {
                    Name = "sOutMTD",
                    Type = new CodeTypeReference(typeof(string))
                };
                CodeParameterDeclarationExpression pServiceName = new CodeParameterDeclarationExpression
                {
                    Name = "sServiceName",
                    Type = new CodeTypeReference(typeof(string))
                };
                ProcessChart.Parameters.Add(pContexItemChart);
                ProcessChart.Parameters.Add(pOutMTD);
                ProcessChart.Parameters.Add(pServiceName);

                //try block
                CodeTryCatchFinallyStatement tryblock = AddTryBlock(ProcessChart);
                tryblock.TryStatements.Add(new CodeVariableDeclarationStatement("System.String[]", "sChartControl", SnippetExpression("null")));

                if (_ilbo.Charts.Any())
                {
                    tryblock.TryStatements.Add(SnippetStatement("switch (sServiceName)"));
                    tryblock.TryStatements.Add(SnippetStatement("{"));

                    _ilbo.TaskServiceList.Where(ts => ts.Charts.Any()).GroupBy(ts => ts.ServiceName);

                    IEnumerable<TaskService> chartInvolvedServices = (from ts in _ilbo.TaskServiceList
                                                                      where ts.Charts.Any()
                                                                      orderby ts.ServiceName
                                                                      group ts by ts.ServiceName into sg
                                                                      select sg.First());

                    // for each service
                    foreach (TaskService taskService in chartInvolvedServices)
                    {
                        tryblock.TryStatements.Add(SnippetStatement(string.Format("case \"{0}\":", taskService.ServiceName)));

                        IEnumerable<IGrouping<string, Chart>> chartGrps = taskService.Charts.GroupBy(c => c.Name);
                        tryblock.TryStatements.Add(AssignVariable("sChartControl", new CodeArrayCreateExpression(typeof(string), chartGrps.Count())));

                        int index = 0;
                        foreach (IGrouping<string, Chart> chartGrp in chartGrps)
                        {
                            tryblock.TryStatements.Add(AssignVariable("sChartControl", PrimitiveExpression(chartGrp.Key), index, true));
                            index++;
                        }
                        tryblock.TryStatements.Add(SnippetExpression("break"));
                    }
                    tryblock.TryStatements.Add(SnippetStatement("}"));

                    //
                    MethodInvokation = MethodInvocationExp(ThisReferenceExp(), "PopulateChartContext");
                    AddParameters(MethodInvokation, new Object[] { SnippetExpression("ref cContextItemsChart"), SnippetExpression("sOutMTD"), SnippetExpression("sChartControl"), SnippetExpression("sServiceName") });
                    tryblock.TryStatements.Add(MethodInvokation);

                    //
                    MethodInvokation = MethodInvocationExp(TypeReferenceExp("Trace"), "WriteLineIf");
                    AddParameters(MethodInvokation, new Object[] { PrimitiveExpression(true), PrimitiveExpression(string.Format("ProcessChart : Processing Chart Completed at  : ")), SnippetExpression("System.Convert.ToString(System.DateTime.Now)") });
                    tryblock.TryStatements.Add(MethodInvokation);

                }

                //catch block
                CodeCatchClause catchblock = AddCatchBlock(tryblock);
                MethodInvokation = MethodInvocationExp(TypeReferenceExp("Trace"), "WriteLineIf");
                AddParameters(MethodInvokation, new Object[] { PrimitiveExpression(true), PrimitiveExpression(" ProcessChart : "), SnippetExpression("string.Format( \"Error : {0}\",e.Message)") });
                catchblock.Statements.Add(MethodInvokation);

                ProcessChart.Statements.Add(ReturnExpression(PrimitiveExpression(false)));

            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("ProcessChart->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
            return ProcessChart;
        }

        private CodeMemberMethod PopulateChartContext()
        {
            CodeMemberMethod PopulateChartContext = null;
            CodeMethodInvokeExpression MethodInvokation = null;
            try
            {
                PopulateChartContext = new CodeMemberMethod
                {
                    Name = "PopulateChartContext",
                    Attributes = MemberAttributes.Private,
                    ReturnType = new CodeTypeReference(typeof(Object))
                };

                //method parameters
                CodeParameterDeclarationExpression pContextItemChart = new CodeParameterDeclarationExpression
                {
                    Name = "cContextItemsChart",
                    Type = new CodeTypeReference(typeof(Dictionary<string, Object>)),
                    Direction = FieldDirection.Ref
                };
                PopulateChartContext.Parameters.Add(pContextItemChart);
                PopulateChartContext.Parameters.Add(new CodeParameterDeclarationExpression(typeof(string), "sOutMTD"));
                PopulateChartContext.Parameters.Add(new CodeParameterDeclarationExpression(typeof(string[]), "sChartControl"));
                PopulateChartContext.Parameters.Add(new CodeParameterDeclarationExpression(typeof(string), "sServiceName"));

                //method return statement
                MethodInvokation = MethodInvocationExp(TypeReferenceExp("oChartControl"), "PopulateChartContext");
                AddParameters(MethodInvokation, new Object[] { SnippetExpression("ref cContextItemsChart"), SnippetExpression("sOutMTD"), SnippetExpression("sServiceName"), SnippetExpression("sChartControl"), PrimitiveExpression("ExtJs") });
                PopulateChartContext.Statements.Add(ReturnExpression(MethodInvokation));
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("PopulateChartContext->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }

            return PopulateChartContext;
        }

        private CodeMemberMethod Update_ScreenData_Chart()
        {
            CodeMemberMethod Update_ScreenData_Chart = null;
            CodeMethodInvokeExpression MethodInvokation = null;
            try
            {
                Update_ScreenData_Chart = new CodeMemberMethod
                {
                    Name = "Update_ScreenData_Chart",
                    Attributes = (MemberAttributes.Public | MemberAttributes.Final),
                    ReturnType = new CodeTypeReference(typeof(bool))
                };

                //method parameters
                Update_ScreenData_Chart.Parameters.Add(new CodeParameterDeclarationExpression(typeof(Dictionary<string, Object>), "cContextItemsChart"));
                Update_ScreenData_Chart.Parameters.Add(new CodeParameterDeclarationExpression(typeof(string), "sTabName"));
                CodeParameterDeclarationExpression pNodeScreenInfo = new CodeParameterDeclarationExpression
                {
                    Name = "nodeScreenInfo",
                    Type = new CodeTypeReference(typeof(System.Xml.XmlNode)),
                    Direction = FieldDirection.Ref
                };
                Update_ScreenData_Chart.Parameters.Add(pNodeScreenInfo);
                Update_ScreenData_Chart.Statements.Add(new CodeVariableDeclarationStatement("System.String[]", "sChartControl", SnippetExpression("null")));

                //try block
                CodeTryCatchFinallyStatement tryblock = AddTryBlock(Update_ScreenData_Chart);

                IEnumerable<Page> pageWithChart = from p in _ilbo.TabPages
                                                  where p.Charts.Any()
                                                  select p;

                if (pageWithChart.Any())
                {
                    tryblock.TryStatements.Add(SnippetStatement("switch (sTabName)"));
                    tryblock.TryStatements.Add(SnippetStatement("{"));

                    // for each service
                    foreach (Page page in pageWithChart)
                    {
                        tryblock.TryStatements.Add(SnippetStatement(string.Format("case \"{0}\":", page.Name.Equals("mainpage") ? string.Empty : page.Name)));

                        IEnumerable<IGrouping<string, Chart>> chartGrps = page.Charts.GroupBy(c => c.Name);
                        tryblock.TryStatements.Add(AssignVariable("sChartControl", new CodeArrayCreateExpression(typeof(string), chartGrps.Count())));
                        int index = 0;
                        foreach (IGrouping<string, Chart> chartGrp in chartGrps)
                        {
                            tryblock.TryStatements.Add(AssignVariable("sChartControl", PrimitiveExpression(chartGrp.Key), index, true));
                            index++;
                        }
                        tryblock.TryStatements.Add(SnippetExpression("break"));
                    }
                    tryblock.TryStatements.Add(SnippetStatement("}"));

                    //
                    MethodInvokation = MethodInvocationExp(TypeReferenceExp("oChartControl"), "Update_ScreenData_Chart");
                    AddParameters(MethodInvokation, new Object[] { ArgumentReferenceExp("cContextItemsChart"), ArgumentReferenceExp("sTabName"), SnippetExpression("ref nodeScreenInfo"), VariableReferenceExp("sChartControl") });
                    tryblock.TryStatements.Add(MethodInvokation);

                    //
                    MethodInvokation = MethodInvocationExp(TypeReferenceExp("Trace"), "WriteLineIf");
                    AddParameters(MethodInvokation, new Object[] { PrimitiveExpression(true), PrimitiveExpression("Update_ScreenData_Chart"), PrimitiveExpression("Updating Chart Content for Response XML Completed") });
                    tryblock.TryStatements.Add(MethodInvokation);

                }

                tryblock.TryStatements.Add(ReturnExpression(PrimitiveExpression(true)));

                //catch block
                CodeCatchClause catchblock = AddCatchBlock(tryblock);
                MethodInvokation = MethodInvocationExp(TypeReferenceExp("Trace"), "WriteLineIf");
                AddParameters(MethodInvokation, new Object[] { PrimitiveExpression(true), PrimitiveExpression("Update_ScreenData_Chart"), PrimitiveExpression("string.Format(\" Error : {0}\",e.Message)") });
                catchblock.Statements.Add(MethodInvokation);

                //method return expression
                Update_ScreenData_Chart.Statements.Add(ReturnExpression(PrimitiveExpression(false)));
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Update_SceenData_Chart->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
            return Update_ScreenData_Chart;
        }
        private CodeMemberMethod ResetChartControls()
        {
            CodeMemberMethod ResetChartControls = null;
            try
            {
                ResetChartControls = new CodeMemberMethod
                {
                    Name = "ResetChartControls",
                    Attributes = MemberAttributes.Public | MemberAttributes.Final,
                    ReturnType = new CodeTypeReference(typeof(bool)),

                };

                CodeParameterDeclarationExpression parameter = new CodeParameterDeclarationExpression();
                parameter.Name = "cContextItemsChart";
                parameter.Type = new CodeTypeReference(typeof(Dictionary<String, Object>));
                parameter.Direction = FieldDirection.Ref;

                ResetChartControls.Parameters.Add(parameter);

                CodeTryCatchFinallyStatement tryBlock = new CodeTryCatchFinallyStatement();
                foreach (string sChartName in _ilbo.Charts.Select(c=>c.Name).Distinct())
                {
                    CodeConditionStatement ifKeyExists = IfCondition();
                    ifKeyExists.Condition = BinaryOpertorExpression(MethodInvocationExp(MethodInvocationExp(VariableReferenceExp("cContextItemsChart"), "ContainsKey").AddParameter(PrimitiveExpression(string.Format("ICT_ILBO_CHART_{0}", sChartName))), "ToString"), CodeBinaryOperatorType.IdentityEquality, PrimitiveExpression("True"));
                    ifKeyExists.TrueStatements.Add(MethodInvocationExp(VariableReferenceExp("cContextItemsChart"), "Remove").AddParameter(PrimitiveExpression(string.Format("ICT_ILBO_CHART_{0}", sChartName))));
                    tryBlock.AddStatement(ifKeyExists);
                }
                tryBlock.AddStatement(ReturnExpression(PrimitiveExpression(true)));

                CodeCatchClause catchBlock = AddCatchBlock(tryBlock, "e");
                catchBlock.AddStatement(MethodInvocationExp(TypeReferenceExp("Trace"), "WriteLineIf").AddParameters(new CodeExpression[] { PrimitiveExpression(false), PrimitiveExpression("ResetChartControls"), MethodInvocationExp(GetProperty("e", "Message"), "ToString") }));

                ResetChartControls.AddStatement(tryBlock);

                ResetChartControls.AddStatement(ReturnExpression(PrimitiveExpression(false)));

                return ResetChartControls;
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("ResetChartControls->{0}", ex.InnerException != null ? ex.InnerException.Message : ex.Message));
            }
        }
    }
}
