using System;
using System.Collections.Generic;
using System.Linq;
using System.CodeDom;
using System.Data;
using System.IO;

namespace Ramco.VwPlf.CodeGenerator.WebLayer
{
    internal class TreeClassGenerator : AbstractCSFileGenerator
    {
        private Activity _activity = null;
        private ILBO _ilbo = null;
        private ECRLevelOptions _ecrOptions = null;

        public TreeClassGenerator(Activity activity, ILBO ilbo,ref ECRLevelOptions ecrOptions) : base(ecrOptions)
        {
            this._activity = activity;
            this._ilbo = ilbo;
            this._ecrOptions = ecrOptions;
            base._objectType = ObjectType.Activity;
            base._targetFileName = string.Format("{0}_tr", ilbo.Code);
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
                throw new Exception(string.Format("GenerateTreeClass.CreateNamespace()->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }
        public override void ImportNamespace()
        {
            base._csFile.ReferencedNamespace.Add("System");
            base._csFile.ReferencedNamespace.Add("System.Xml");
            base._csFile.ReferencedNamespace.Add("System.Collections");
            base._csFile.ReferencedNamespace.Add("System.Collections.Generic");
            base._csFile.ReferencedNamespace.Add("System.Diagnostics");
            base._csFile.ReferencedNamespace.Add("Plf.Ui.Ramco.Utility");
        }
        public override void AddCustomAttributes()
        {
            //throw new NotImplementedException();
        }
        public override void CreateClasses()
        {
            try
            {
                CodeTypeDeclaration treeCls = new CodeTypeDeclaration
                {
                    Name = string.Format("{0}_tr", _ilbo.Code),
                    IsClass = true
                };
                AddMemberFields(ref treeCls);
                AddMemberFunctions(ref treeCls);

                base._csFile.UserDefinedTypes.Add(treeCls);
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("GenerateTreeClass.CreateClasses()->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }
        public override void AddMemberFields(ref CodeTypeDeclaration classObj)
        {
            try
            {
                DeclareMemberField(MemberAttributes.Private, classObj, "oDefaultNTConfig", "Dictionary<string,Object>", true);
                DeclareMemberField(MemberAttributes.Private, classObj, "oTreeControl", "Treecontrol", true);
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("GenerateTreeClass.AddMemberFields()->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }
        public override void AddMemberFunctions(ref CodeTypeDeclaration classObj)
        {
            try
            {
                classObj.Members.Add(ProcessTree());
                classObj.Members.Add(Set_DefaultNodeConfig_Tree());
                classObj.Members.Add(PopulateTreeContext());
                classObj.Members.Add(Update_TaskData_Tree());
                classObj.Members.Add(GetTaskData_Tree());
                classObj.Members.Add(UpdateScreenData_Tree());
                classObj.Members.Add(GetScreenData_Tree());
                classObj.Members.Add(ResetTreeControls());
                classObj.Members.Add(Clear());
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("GenerateTreeClass.AddMemberFunctions()->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private CodeMemberMethod ProcessTree()
        {
            CodeMemberMethod ProcessTree = null;
            CodeMethodInvokeExpression MethodInvokation = null;

            try
            {
                ProcessTree = new CodeMemberMethod
                {
                    Name = "ProcessTree",
                    Attributes = MemberAttributes.Public | MemberAttributes.Final,
                    ReturnType = new CodeTypeReference(typeof(bool))
                };

                //add method parameters
                CodeParameterDeclarationExpression pContexItemTree = new CodeParameterDeclarationExpression
                {
                    Name = "cContextItemsTree",
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
                ProcessTree.Parameters.Add(pContexItemTree);
                ProcessTree.Parameters.Add(pOutMTD);
                ProcessTree.Parameters.Add(pServiceName);

                //try block
                CodeTryCatchFinallyStatement tryblock = AddTryBlock(ProcessTree);

                CodeVariableDeclarationStatement vTreeControl = new CodeVariableDeclarationStatement("System.String[]", "sTreeControl", SnippetExpression("null"));
                CodeVariableDeclarationStatement vClearTreeControls = new CodeVariableDeclarationStatement("System.String[]", "sClearTreeControls", SnippetExpression("null"));

                tryblock.TryStatements.Add(vTreeControl);
                tryblock.TryStatements.Add(vClearTreeControls);

                //sp = "engg_devcon_bulkgen_srvtreedtl";
                //GlobalVar.var parameters = dbManager.CreateParameters(5);
                //GlobalVar.dbManager.AddParamters(parameters,0, "@CustomerName", GlobalVar.Customer);
                //GlobalVar.dbManager.AddParamters(parameters,1, "@ProjectName", GlobalVar.Project);
                //GlobalVar.dbManager.AddParamters(parameters,2, "@EcrNo", GlobalVar.Ecrno);
                //GlobalVar.dbManager.AddParamters(parameters,3, "@ActivityName", NamespaceName);
                //GlobalVar.dbManager.AddParamters(parameters,4, "@UIName", IlboCode);
                //DataTable dtTreeInfo = GlobalVar.dbManager.ExecuteDataTable(CommandType.StoredProcedure, sp);

                var serviceTreeInfo = (from task in _ilbo.TaskServiceList
                                       where task.Trees.Count > 0
                                       group task by task.ServiceName into g
                                       select new
                                       {
                                           serviceName = g.First().ServiceName,
                                           trees = g.First().Trees
                                       }).OrderBy(t => t.serviceName).Distinct();

                if (serviceTreeInfo.Any())
                {
                    tryblock.TryStatements.Add(SnippetStatement("switch (sServiceName.ToLower())"));
                    tryblock.TryStatements.Add(SnippetStatement("{"));

                    // for each service
                    foreach (var info in serviceTreeInfo)
                    {

                        tryblock.TryStatements.Add(SnippetStatement(string.Format("case \"{0}\":", info.serviceName)));

                        IEnumerable<IGrouping<string, Tree>> treeGroups = info.trees.GroupBy(t => t.Name);
                        tryblock.TryStatements.Add(AssignVariable("sTreeControl", new CodeArrayCreateExpression(typeof(string), treeGroups.Count())));
                        tryblock.TryStatements.Add(AssignVariable("sClearTreeControls", new CodeArrayCreateExpression(typeof(string), treeGroups.Count())));

                        int index = 0;
                        foreach (IGrouping<string, Tree> treeGrp in treeGroups)
                        {
                            string sControlName = Convert.ToString(treeGrp.Key);
                            tryblock.TryStatements.Add(AssignVariable("sClearTreeControls", PrimitiveExpression(sControlName), index, true));
                            tryblock.TryStatements.Add(AssignVariable("sTreeControl", PrimitiveExpression(sControlName), index, true));
                            index++;
                        }

                        tryblock.TryStatements.Add(SnippetExpression("break"));
                    }
                    tryblock.TryStatements.Add(SnippetStatement("}"));

                    tryblock.TryStatements.Add(MethodInvocationExp(ThisReferenceExp(), "Set_DefaultNodeConfig_Tree"));

                    //
                    MethodInvokation = MethodInvocationExp(ThisReferenceExp(), "PopulateTreeContext");
                    AddParameters(MethodInvokation, new Object[] { SnippetExpression("ref cContextItemsTree"), ArgumentReferenceExp(pOutMTD.Name), VariableReferenceExp(vTreeControl.Name), VariableReferenceExp(vClearTreeControls.Name), FieldReferenceExp(ThisReferenceExp(), "oDefaultNTConfig") });
                    tryblock.TryStatements.Add(MethodInvokation);

                }

                //catch block
                CodeCatchClause catchblock = AddCatchBlock(tryblock);
                MethodInvokation = MethodInvocationExp(TypeReferenceExp("Trace"), "WriteLineIf");
                AddParameters(MethodInvokation, new Object[] { PrimitiveExpression(false), PrimitiveExpression(" ProcessTree : "), SnippetExpression("string.Format( \"Error : {0}\",e.Message)") });
                catchblock.Statements.Add(MethodInvokation);

                catchblock.Statements.Add(ReturnExpression(PrimitiveExpression(false)));
                ProcessTree.Statements.Add(ReturnExpression(PrimitiveExpression(true)));

            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("GenerateTreeClass.ProcessTree()->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
            return ProcessTree;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private CodeMemberMethod Set_DefaultNodeConfig_Tree()
        {
            CodeMemberMethod Set_DefaultNodeConfig_Tree = null;
            CodeMethodInvokeExpression MethodInvokation = null;
            try
            {
                Set_DefaultNodeConfig_Tree = new CodeMemberMethod
                {
                    Name = "Set_DefaultNodeConfig_Tree",
                    Attributes = MemberAttributes.Public | MemberAttributes.Final,
                    ReturnType = new CodeTypeReference(typeof(Object))
                };

                //try block
                CodeTryCatchFinallyStatement tryblock = AddTryBlock(Set_DefaultNodeConfig_Tree);

                IEnumerable<IGrouping<string, Tree>> treeGroups = _ilbo.Trees.GroupBy(t => t.Name);

                foreach (IGrouping<string, Tree> treeGrp in treeGroups)
                {
                    Tree tree = treeGrp.First();

                    foreach (TreeInfo treeinfo in tree.Info)
                    {
                        tryblock.AddStatement(MethodInvocationExp(TypeReferenceExp("oDefaultNTConfig"), "Add").AddParameters(new Object[] { Convert.ToString(treeinfo.OpenImage), string.Format("{0}~~{1}~~{2}", tree.Name, treeinfo.NodeType, "vwt_openimage") }));
                        tryblock.AddStatement(MethodInvocationExp(TypeReferenceExp("oDefaultNTConfig"), "Add").AddParameters(new Object[] { Convert.ToString(treeinfo.NotExpandedImage), string.Format("{0}~~{1}~~{2}", tree.Name, treeinfo.NodeType, "vwt_notexpandedimage") }));
                        tryblock.AddStatement(MethodInvocationExp(TypeReferenceExp("oDefaultNTConfig"), "Add").AddParameters(new Object[] { Convert.ToString(treeinfo.ExpandableImage), string.Format("{0}~~{1}~~{2}", tree.Name, treeinfo.NodeType, "vwt_expandableimage") }));
                        tryblock.AddStatement(MethodInvocationExp(TypeReferenceExp("oDefaultNTConfig"), "Add").AddParameters(new Object[] { Convert.ToString(treeinfo.ExpandedImage), string.Format("{0}~~{1}~~{2}", tree.Name, treeinfo.NodeType, "vwt_expandedimage") }));
                        tryblock.AddStatement(MethodInvocationExp(TypeReferenceExp("oDefaultNTConfig"), "Add").AddParameters(new Object[] { Convert.ToString(treeinfo.CloseImage), string.Format("{0}~~{1}~~{2}", tree.Name, treeinfo.NodeType, "vwt_closeimage") }));
                        tryblock.AddStatement(MethodInvocationExp(TypeReferenceExp("oDefaultNTConfig"), "Add").AddParameters(new Object[] { Convert.ToString(treeinfo.CheckImage), string.Format("{0}~~{1}~~{2}", tree.Name, treeinfo.NodeType, "vwt_checkimage") }));
                        tryblock.AddStatement(MethodInvocationExp(TypeReferenceExp("oDefaultNTConfig"), "Add").AddParameters(new Object[] { Convert.ToString(treeinfo.UnCheckImage), string.Format("{0}~~{1}~~{2}", tree.Name, treeinfo.NodeType, "vwt_uncheckimage") }));
                        tryblock.AddStatement(MethodInvocationExp(TypeReferenceExp("oDefaultNTConfig"), "Add").AddParameters(new Object[] { Convert.ToString(treeinfo.PartialCheckImage), string.Format("{0}~~{1}~~{2}", tree.Name, treeinfo.NodeType, "chkbox_parial_chkimg") }));
                    }
                }

                tryblock.TryStatements.Add(ReturnExpression(SnippetExpression("null")));

                //catch block
                CodeCatchClause catchblock = AddCatchBlock(tryblock);

                MethodInvokation = MethodInvocationExp(TypeReferenceExp("Trace"), "WriteLineIf");
                AddParameters(MethodInvokation, new Object[] { PrimitiveExpression(false), PrimitiveExpression("Set_DefaultNodeConfig_Tree"), SnippetExpression("e.Message.ToString()") });
                catchblock.Statements.Add(MethodInvokation);

                catchblock.Statements.Add(ReturnExpression(SnippetExpression("null")));
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("GenerateTreeClass.Set_DefaultNodeConfig_Tree()->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
            return Set_DefaultNodeConfig_Tree;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private CodeMemberMethod PopulateTreeContext()
        {
            CodeMemberMethod PopulateTreeContext = null;
            CodeMethodInvokeExpression MethodInvokation = null;
            try
            {
                PopulateTreeContext = new CodeMemberMethod
                {
                    Attributes = MemberAttributes.Public | MemberAttributes.Final,
                    Name = "PopulateTreeContext",
                    ReturnType = new CodeTypeReference(typeof(Object))
                };

                //method parameters
                PopulateTreeContext.Parameters.Add(new CodeParameterDeclarationExpression { Name = "cContextItemsTree", Type = new CodeTypeReference(typeof(Dictionary<string, Object>)), Direction = FieldDirection.Ref });
                PopulateTreeContext.Parameters.Add(new CodeParameterDeclarationExpression(typeof(string), "sOutMTD"));
                PopulateTreeContext.Parameters.Add(new CodeParameterDeclarationExpression(typeof(string[]), "sTreeControl"));
                PopulateTreeContext.Parameters.Add(new CodeParameterDeclarationExpression(typeof(string[]), "sClearTreeControls"));
                PopulateTreeContext.Parameters.Add(new CodeParameterDeclarationExpression(typeof(Dictionary<string, Object>), "oDefaultNTConfig"));

                //method summary
                AddMethodSummary(PopulateTreeContext, "For Populating Tree Content Into ILBO Context Variable");

                //method return statement
                MethodInvokation = MethodInvocationExp(TypeReferenceExp("oTreeControl"), "PopulateTreeContext");
                AddParameters(MethodInvokation, new Object[] { SnippetExpression("ref cContextItemsTree"), ArgumentReferenceExp("sOutMTD"), ArgumentReferenceExp("sTreeControl"), ArgumentReferenceExp("sClearTreeControls"), ArgumentReferenceExp("oDefaultNTConfig"), PrimitiveExpression("ExtJs") });
                PopulateTreeContext.Statements.Add(ReturnExpression(MethodInvokation));
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("GenerateTreeClass.PopulateTreeContext()->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
            return PopulateTreeContext;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private CodeMemberMethod Update_TaskData_Tree()
        {
            CodeMemberMethod Update_TaskData_Tree = null;
            try
            {
                Update_TaskData_Tree = new CodeMemberMethod
                {
                    Attributes = MemberAttributes.Public | MemberAttributes.Final,
                    Name = "Update_TaskData_Tree",
                    ReturnType = new CodeTypeReference(typeof(bool))
                };

                //method parameters
                CodeParameterDeclarationExpression pContextItems = new CodeParameterDeclarationExpression(typeof(Dictionary<string, Object>), "cContextItemsTree");
                pContextItems.Direction = FieldDirection.Ref;
                Update_TaskData_Tree.Parameters.Add(pContextItems);

                Update_TaskData_Tree.Parameters.Add(new CodeParameterDeclarationExpression(typeof(string), "sTabName"));
                Update_TaskData_Tree.Parameters.Add(new CodeParameterDeclarationExpression(typeof(string), "sTaskName"));

                CodeParameterDeclarationExpression pScreenInfoNode = new CodeParameterDeclarationExpression
                {
                    Name = "pScreenInfoNode",
                    Type = new CodeTypeReference(typeof(System.Xml.XmlNode)),
                    Direction = FieldDirection.Ref
                };
                Update_TaskData_Tree.Parameters.Add(pScreenInfoNode);

                Update_TaskData_Tree.Statements.Add(ReturnExpression(PrimitiveExpression(false)));
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("GenerateTreeClass.Update_TaskData_Tree()->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
            return Update_TaskData_Tree;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private CodeMemberMethod GetTaskData_Tree()
        {
            CodeMemberMethod GetTaskData_Tree = null;
            CodeMethodInvokeExpression MethodInvokation = null;
            try
            {
                GetTaskData_Tree = new CodeMemberMethod
                {
                    Attributes = MemberAttributes.Public | MemberAttributes.Final,
                    Name = "GetTaskData_Tree",
                    ReturnType = new CodeTypeReference(typeof(bool))
                };

                //method parameters
                GetTaskData_Tree.Parameters.Add(new CodeParameterDeclarationExpression { Direction = FieldDirection.Ref, Type = new CodeTypeReference(typeof(Dictionary<string, Object>)), Name = "cContextItemsTree" });
                GetTaskData_Tree.Parameters.Add(new CodeParameterDeclarationExpression(typeof(string), "sTabName"));
                GetTaskData_Tree.Parameters.Add(new CodeParameterDeclarationExpression(typeof(string), "sTaskName"));
                CodeParameterDeclarationExpression pScreenInfoNode = new CodeParameterDeclarationExpression
                {
                    Direction = FieldDirection.Ref,
                    Type = new CodeTypeReference(typeof(System.Xml.XmlNode)),
                    Name = "pScreenInfoNode"
                };
                GetTaskData_Tree.Parameters.Add(pScreenInfoNode);

                //local variable declarations
                GetTaskData_Tree.Statements.Add(new CodeVariableDeclarationStatement("System.String[]", "sTreeControl", SnippetExpression("null")));

                //try block
                CodeTryCatchFinallyStatement tryblock = AddTryBlock(GetTaskData_Tree);

                IEnumerable<TaskService> taskTreeDtl = (from task in _ilbo.TaskServiceList
                                                        where task.Trees.Count > 0
                                                        select task);

                if (taskTreeDtl.Count() > 0)
                {
                    tryblock.TryStatements.Add(SnippetStatement("switch(sTaskName.ToLower())"));
                    tryblock.TryStatements.Add(SnippetStatement("{"));//open braces for switch statement


                    foreach (TaskService task in taskTreeDtl)
                    {
                        tryblock.TryStatements.Add(SnippetStatement(string.Format("case \"{0}\":", task.Name)));

                        IEnumerable<IGrouping<string, Tree>> treeGroups = task.Trees.GroupBy(t => t.Name);
                        tryblock.TryStatements.Add(AssignVariable("sTreeControl", new CodeArrayCreateExpression(typeof(string), treeGroups.Count())));

                        int index = 0;
                        foreach (IGrouping<string, Tree> treeGrp in treeGroups)
                        {
                            tryblock.TryStatements.Add(AssignVariable("sTreeControl", PrimitiveExpression(treeGrp.Key), index, true));
                            index++;
                        }
                        tryblock.TryStatements.Add(SnippetExpression("break"));
                    }

                    tryblock.TryStatements.Add(SnippetStatement("}//ENDSWITCH")); //close braces for switch statement

                    MethodInvokation = MethodInvocationExp(TypeReferenceExp("oTreeControl"), "GetTaskData_Tree");
                    AddParameters(MethodInvokation, new Object[] { SnippetExpression("ref cContextItemsTree"), ArgumentReferenceExp("sTabName"), ArgumentReferenceExp("sTaskName"), SnippetExpression("ref pScreenInfoNode"), SnippetExpression("sTreeControl") });
                    tryblock.TryStatements.Add(ReturnExpression(MethodInvokation));
                }

                //catch block
                CodeCatchClause catchblock = AddCatchBlock(tryblock);
                MethodInvokation = MethodInvocationExp(TypeReferenceExp("Trace"), "WriteLineIf");
                AddParameters(MethodInvokation, new Object[] { PrimitiveExpression(false), PrimitiveExpression("GetTaskData_Tree"), SnippetExpression("e.Message.ToString()") });
                catchblock.Statements.Add(MethodInvokation);

                GetTaskData_Tree.Statements.Add(ReturnExpression(PrimitiveExpression(false)));
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("GenerateTreeClass.GetTaskData_Tree()->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
            return GetTaskData_Tree;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private CodeMemberMethod UpdateScreenData_Tree()
        {
            CodeMemberMethod UpdateScreenData_Tree = null;
            CodeMethodInvokeExpression MethodInvokation = null;
            string sp = string.Empty;
            try
            {
                UpdateScreenData_Tree = new CodeMemberMethod
                {
                    Name = "UpdateScreenData_Tree",
                    Attributes = MemberAttributes.Public | MemberAttributes.Final,
                    ReturnType = new CodeTypeReference(typeof(bool))
                };

                CodeParameterDeclarationExpression pcContextItemsTree = new CodeParameterDeclarationExpression
                {
                    Name = "cContextItemsTree",
                    Type = new CodeTypeReference(typeof(Dictionary<string, Object>)),
                    Direction = FieldDirection.Ref
                };
                UpdateScreenData_Tree.Parameters.Add(pcContextItemsTree);
                UpdateScreenData_Tree.Parameters.Add(new CodeParameterDeclarationExpression(typeof(string), "sTabName"));

                CodeParameterDeclarationExpression pScreenInfoNode = new CodeParameterDeclarationExpression
                {
                    Name = "pScreenInfoNode",
                    Type = new CodeTypeReference(typeof(System.Xml.XmlNode)),
                    Direction = FieldDirection.Ref
                };
                UpdateScreenData_Tree.Parameters.Add(pScreenInfoNode);

                //local variable declarations
                UpdateScreenData_Tree.Statements.Add(new CodeVariableDeclarationStatement("System.String[]", "sTreeControl", SnippetExpression("null")));

                //try block
                CodeTryCatchFinallyStatement TryBlock = AddTryBlock(UpdateScreenData_Tree);

                IEnumerable<Page> pageContainsTree = from page in _ilbo.TabPages
                                                     where page.Trees.Count() > 0
                                                     select page;
                if (pageContainsTree.Any())
                {
                    TryBlock.TryStatements.Add(SnippetStatement("switch(sTabName.ToLower())"));
                    TryBlock.TryStatements.Add(SnippetStatement("{"));

                    foreach (Page page in pageContainsTree)
                    {
                        string sPageName = page.Name.Equals("mainpage") ? "" : page.Name;

                        TryBlock.TryStatements.Add(SnippetStatement(string.Format("case \"{0}\":", sPageName)));

                        IEnumerable<IGrouping<string, Tree>> treeGroups = page.Trees.GroupBy(t => t.Name);
                        TryBlock.TryStatements.Add(AssignVariable("sTreeControl", new CodeArrayCreateExpression(typeof(string), treeGroups.Count()))); //based on control count

                        int index = 0;
                        foreach (IGrouping<string, Tree> treeGrp in treeGroups)
                        {
                            TryBlock.TryStatements.Add(AssignVariable("sTreeControl", PrimitiveExpression(treeGrp.Key), index, true));
                            index++;
                        }

                        TryBlock.TryStatements.Add(SnippetExpression("break"));
                    }

                    TryBlock.TryStatements.Add(SnippetStatement("}//ENDSWITCH"));

                    MethodInvokation = MethodInvocationExp(TypeReferenceExp("oTreeControl"), "UpdateScreenData_Tree");
                    AddParameters(MethodInvokation, new Object[] { SnippetExpression("ref cContextItemsTree"), ArgumentReferenceExp("sTabName"), SnippetExpression("ref pScreenInfoNode"), VariableReferenceExp("sTreeControl") });
                    TryBlock.TryStatements.Add(ReturnExpression(MethodInvokation));
                }

                //catch block
                CodeCatchClause CatchBlock = AddCatchBlock(TryBlock);
                MethodInvokation = MethodInvocationExp(TypeReferenceExp("Trace"), "WriteLineIf");
                AddParameters(MethodInvokation, new Object[] { PrimitiveExpression(false), PrimitiveExpression("UpdateScreenData_Tree"), SnippetExpression("e.Message.ToString()") });
                CatchBlock.Statements.Add(MethodInvokation);

                UpdateScreenData_Tree.Statements.Add(ReturnExpression(PrimitiveExpression(false)));

            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("GenerateTreeClass.UpdateScreenData_Tree()->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
            return UpdateScreenData_Tree;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private CodeMemberMethod GetScreenData_Tree()
        {
            CodeMemberMethod GetScreenData_Tree = null;
            CodeMethodInvokeExpression MethodInvokation = null;
            string sp = string.Empty;
            try
            {
                GetScreenData_Tree = new CodeMemberMethod
                {
                    Name = "GetScreenData_Tree",
                    Attributes = MemberAttributes.Public | MemberAttributes.Final,
                    ReturnType = new CodeTypeReference(typeof(bool))
                };

                CodeParameterDeclarationExpression pcContextItemsTree = new CodeParameterDeclarationExpression
                {
                    Name = "cContextItemsTree",
                    Type = new CodeTypeReference(typeof(Dictionary<string, Object>)),
                    Direction = FieldDirection.Ref
                };
                GetScreenData_Tree.Parameters.Add(pcContextItemsTree);
                GetScreenData_Tree.Parameters.Add(new CodeParameterDeclarationExpression(typeof(string), "sTabName"));

                CodeParameterDeclarationExpression pScreenInfoNode = new CodeParameterDeclarationExpression
                {
                    Name = "pScreenInfoNode",
                    Type = new CodeTypeReference(typeof(System.Xml.XmlNode)),
                    Direction = FieldDirection.Ref
                };
                GetScreenData_Tree.Parameters.Add(pScreenInfoNode);

                //local variable declarations
                GetScreenData_Tree.Statements.Add(new CodeVariableDeclarationStatement("System.String[]", "sTreeControl", SnippetExpression("null")));

                //try block
                CodeTryCatchFinallyStatement TryBlock = AddTryBlock(GetScreenData_Tree);

                IEnumerable<Page> pageContainsTree = from page in _ilbo.TabPages
                                                     where page.Trees.Count() > 0
                                                     select page;

                if (pageContainsTree.Any())
                {
                    TryBlock.TryStatements.Add(SnippetStatement("switch(sTabName.ToLower())"));
                    TryBlock.TryStatements.Add(SnippetStatement("{"));

                    foreach (Page page in pageContainsTree)
                    {
                        string sPageName = page.Name.Equals("mainpage") ? "" : page.Name;
                        TryBlock.TryStatements.Add(SnippetStatement(string.Format("case \"{0}\":", sPageName)));

                        IEnumerable<IGrouping<string, Tree>> treeGroups = page.Trees.GroupBy(t => t.Name);
                        TryBlock.TryStatements.Add(AssignVariable("sTreeControl", new CodeArrayCreateExpression(typeof(string), treeGroups.Count()))); //based on control count

                        int index = 0;
                        foreach (IGrouping<string, Tree> treeGrp in treeGroups)
                        {
                            TryBlock.TryStatements.Add(AssignVariable("sTreeControl", PrimitiveExpression(treeGrp.Key), index, true));
                            index++;
                        }

                        TryBlock.TryStatements.Add(SnippetExpression("break"));
                    }

                    TryBlock.TryStatements.Add(SnippetStatement("}//ENDSWITCH"));//close braces for switch case

                    MethodInvokation = MethodInvocationExp(TypeReferenceExp("oTreeControl"), "GetScreenData_Tree");
                    AddParameters(MethodInvokation, new Object[] { SnippetExpression("ref cContextItemsTree"), ArgumentReferenceExp("sTabName"), SnippetExpression("ref pScreenInfoNode"), VariableReferenceExp("sTreeControl") });
                    TryBlock.TryStatements.Add(ReturnExpression(MethodInvokation));
                }

                //catch block
                CodeCatchClause CatchBlock = AddCatchBlock(TryBlock);
                MethodInvokation = MethodInvocationExp(TypeReferenceExp("Trace"), "WriteLineIf");
                AddParameters(MethodInvokation, new Object[] { PrimitiveExpression(false), PrimitiveExpression("GetScreenData_Tree"), SnippetExpression("e.Message.ToString()") });
                CatchBlock.Statements.Add(MethodInvokation);

                GetScreenData_Tree.Statements.Add(ReturnExpression(PrimitiveExpression(false)));
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("GenerateTreeClass.GetScreenData_Tree()->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
            return GetScreenData_Tree;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private CodeMemberMethod ResetTreeControls()
        {
            CodeMemberMethod ResetTreeControls = null;
            CodeMethodInvokeExpression MethodInvokation = null;

            try
            {

                ResetTreeControls = new CodeMemberMethod
                {
                    Name = "ResetTreeControls",
                    Attributes = MemberAttributes.Public | MemberAttributes.Final,
                    ReturnType = new CodeTypeReference(typeof(bool))
                };

                CodeParameterDeclarationExpression pcContextItemsTree = new CodeParameterDeclarationExpression
                {
                    Name = "cContextItemsTree",
                    Type = new CodeTypeReference(typeof(Dictionary<string, Object>)),
                    Direction = FieldDirection.Ref
                };
                ResetTreeControls.Parameters.Add(pcContextItemsTree);

                //local variable declarations
                ResetTreeControls.Statements.Add(new CodeVariableDeclarationStatement("System.String[]", "sTreeControl", SnippetExpression("null")));

                //try block
                CodeTryCatchFinallyStatement TryBlock = AddTryBlock(ResetTreeControls);

                IEnumerable<IGrouping<string, Tree>> treeGroups = _ilbo.Trees.GroupBy(t => t.Name);
                TryBlock.TryStatements.Add(AssignVariable("sTreeControl", new CodeArrayCreateExpression(typeof(string), treeGroups.Count()))); //based on control count

                int index = 0;
                foreach (IGrouping<string, Tree> treeGrp in treeGroups)
                {
                    string sTreeControl = Convert.ToString(treeGrp.Key);
                    string sPageName = Convert.ToString(treeGrp.First().Page);
                    TryBlock.TryStatements.Add(AssignVariable("sTreeControl", PrimitiveExpression(sTreeControl), index, true));
                    index++;
                }


                MethodInvokation = MethodInvocationExp(TypeReferenceExp("oTreeControl"), "ResetTreeControls");
                AddParameters(MethodInvokation, new Object[] { SnippetExpression("ref cContextItemsTree"), VariableReferenceExp("sTreeControl") });
                TryBlock.TryStatements.Add(ReturnExpression(MethodInvokation));

                //catch block
                CodeCatchClause CatchBlock = AddCatchBlock(TryBlock);
                MethodInvokation = MethodInvocationExp(TypeReferenceExp("Trace"), "WriteLineIf");
                AddParameters(MethodInvokation, new Object[] { PrimitiveExpression(false), PrimitiveExpression("ResetTreeControls"), SnippetExpression("e.Message.ToString()") });
                CatchBlock.Statements.Add(MethodInvokation);

                ResetTreeControls.Statements.Add(ReturnExpression(PrimitiveExpression(false)));
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("GenerateTreeClass.ResetTreeControls()->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
            return ResetTreeControls;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private CodeMemberMethod Clear()
        {
            CodeMemberMethod Clear = null;
            try
            {
                Clear = new CodeMemberMethod
                {
                    Name = "Clear",
                    Attributes = MemberAttributes.Public | MemberAttributes.Final
                };

                Clear.Statements.Add(MethodInvocationExp(TypeReferenceExp("oTreeControl"), "Clear"));
                Clear.Statements.Add(MethodInvocationExp(TypeReferenceExp("oDefaultNTConfig"), "Clear"));
                Clear.Statements.Add(AssignVariable("oTreeControl", SnippetExpression("null")));
                Clear.Statements.Add(AssignVariable("oDefaultNTConfig", SnippetExpression("null")));
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("GenerateTreeClass.Clear()->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
            return Clear;
        }
    }
}
