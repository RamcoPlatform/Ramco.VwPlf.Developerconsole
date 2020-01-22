/***************************************************************************************************
 * Case Id          :   TECH-9466,TECH-9438
 * Modified Date    :   10 May 2017
 * Modified By      :   Madhan Sekar M
 * Case Description :   Namespace format changed, Directory structure changed.
 ***************************************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.CodeDom;
using System.Reflection;
using Ramco.VwPlf.DataAccess;
using System.IO;
using System.CodeDom.Compiler;

namespace Ramco.VwPlf.CodeGenerator.Callout
{

    //class Program
    //{
    //    static void Main(string[] args)
    //    {
    //        try
    //        {
    //            if (args.Count() != 8)
    //            {
    //                Console.WriteLine("Invalid Input. Try Again");
    //                Console.WriteLine("customer");
    //                Console.WriteLine("project");
    //                Console.WriteLine("process");
    //                Console.WriteLine("component");
    //                Console.WriteLine("ecr");
    //                Console.WriteLine("generationpath");
    //                Console.WriteLine("Log-FullFilePath");
    //                Console.WriteLine("connectionString");
    //            }
    //            else
    //            {
    //                CalloutWrapper calloutWrapper = new CalloutWrapper(args[0], args[1], args[2], args[3], args[4], args[5], @args[6], args[7]);

    //                if (calloutWrapper.Generate())
    //                    Console.WriteLine("Generation Completed successfully.");
    //                else
    //                    Console.WriteLine("Generation UnSuccessful.Check Logfile for Errors.");
    //            }
    //        }
    //        catch (Exception e)
    //        {
    //            Console.WriteLine(e);
    //        }
    //    }
    //}

    public class CalloutWrapper
    {
        String sConnectionString;
        IEnumerable<DataStoreCallout> dataStoreCallout;
        Logger logger;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="customer"></param>
        /// <param name="project"></param>
        /// <param name="process"></param>
        /// <param name="component"></param>
        /// <param name="ecr"></param>
        /// <param name="generationPath"></param>
        /// <param name="logPath"></param>
        /// <param name="connectionString"></param>
        public CalloutWrapper(string customer, string project, string process, string component, string ecr, string generationPath, string logPath, string connectionString)
        {
            string sLogFilePath = string.Empty;
            sConnectionString = string.Empty;
            dataStoreCallout = null;
            logger = new Logger();

            GlobalVar.Customer = customer;
            GlobalVar.Project = project;
            GlobalVar.Process = process;
            GlobalVar.Ecrno = ecr;
            GlobalVar.Component = component;
            GlobalVar.GenerationPath = generationPath;
            GlobalVar.SourcePath = Path.Combine(GlobalVar.GenerationPath, "dotnet", GlobalVar.Customer, GlobalVar.Project, GlobalVar.Ecrno, "Updated", GlobalVar.Component, "Source", "ILBO", string.Format("Ramco.VW.TaskCallout.{0}", GlobalVar.Component));
            GlobalVar.ReleasePath = Path.Combine(GlobalVar.GenerationPath, "dotnet", GlobalVar.Customer, GlobalVar.Project, GlobalVar.Ecrno, "Updated", GlobalVar.Component, "Release", "ILBO");
            GlobalVar.LogPath = logPath;

            sLogFilePath = Path.Combine(GlobalVar.GenerationPath, "dotnet", GlobalVar.Customer, GlobalVar.Project, GlobalVar.Ecrno, $"{GlobalVar.Ecrno}.txt");
            logger.WriteLogToFile(sLogFilePath, $"LogPath:{GlobalVar.LogPath}");
            

            if (!String.IsNullOrEmpty(connectionString))
            {
                string[] conStringProperties = connectionString.Split(';');
                foreach (string property in conStringProperties)
                {
                    if (!String.IsNullOrEmpty(sConnectionString))
                    {
                        if (!property.ToLower().Contains("provider"))
                            sConnectionString = sConnectionString + ";" + property;
                    }
                    else
                    {
                        if (!property.ToLower().Contains("provider"))
                            sConnectionString = property;
                    }
                }
            }
            logger.WriteLogToFile(sLogFilePath, $"ConnectionString:{sConnectionString}");
        }

        public void DownloadCallout()
        {
            StringBuilder sb = new StringBuilder();
            DBManager dbManager = new DBManager(@sConnectionString);

            DataTable dtCalloutinfo = new DataTable();
            DataTable dtCalloutSegmentInfo = new DataTable();
            DataTable dtCalloutDataItemInfo = new DataTable();
            DataTable dtBtInfo = new DataTable();
            try
            {
                dbManager.Open();
                dbManager.BeginTransaction();

                sb.Clear();
                sb.AppendLine("create table #de_fw_des_task_callout(customername varchar(60),projectname varchar(60),ecrno varchar(60),process_name varchar(60),component_name varchar(60),activity_name varchar(60),ui_name varchar(60),task_name varchar(60),calloutname varchar(60))");
                sb.AppendLine("create table #de_fw_des_task_callout_segement(customername varchar(60),projectname varchar(60),ecrno varchar(60),process_name varchar(60),component_name varchar(60),activity_name varchar(60),ui_name varchar(60),task_name varchar(60),calloutname varchar(60),segmentname varchar(60),instanceflag varchar(60),flowattribute varchar(60),sequence varchar(60))");
                sb.AppendLine("create table #de_fw_des_task_callout_dataitem(customername varchar(60),projectname varchar(60),ecrno varchar(60),process_name varchar(60),component_name varchar(60),activity_name varchar(60),ui_name varchar(60),task_name varchar(60),calloutname varchar(60),segmentname varchar(60),dataitemname varchar(60),flowattribute varchar(60))");
                sb.AppendLine("create table #fw_des_service_dataitem(dataitemname varchar(60) collate SQL_Latin1_General_CP1_CI_AS, defaultvalue varchar(60) collate SQL_Latin1_General_CP1_CI_AS, flowattribute int,ispartofkey int,mandatoryflag int, segmentname varchar(60) collate SQL_Latin1_General_CP1_CI_AS, servicename varchar(60) collate SQL_Latin1_General_CP1_CI_AS, itk_dataitem varchar(2) collate SQL_Latin1_General_CP1_CI_AS, UpdUser varchar(60) collate SQL_Latin1_General_CP1_CI_AS,  UpdTime datetime, controlid varchar(40)collate SQL_Latin1_General_CP1_CI_AS )");
                sb.AppendLine("create table #fw_req_bterm(btdesc varchar(250) collate SQL_Latin1_General_CP1_CI_AS,btname varchar(60) collate SQL_Latin1_General_CP1_CI_AS, datatype varchar(20) collate SQL_Latin1_General_CP1_CI_AS,isbterm    int,length int, maxvalue  varchar(250) collate SQL_Latin1_General_CP1_CI_AS,minvalue   varchar(250) collate SQL_Latin1_General_CP1_CI_AS,precisiontype varchar(20) collate SQL_Latin1_General_CP1_CI_AS , UpdUser varchar(60) collate SQL_Latin1_General_CP1_CI_AS, UpdTime datetime)");
                sb.AppendLine("create table #fw_req_bterm_synonym(btname varchar(60) collate SQL_Latin1_General_CP1_CI_AS,btsynonym varchar(60) collate SQL_Latin1_General_CP1_CI_AS , UpdUser varchar(60) collate SQL_Latin1_General_CP1_CI_AS, UpdTime datetime)");
                dbManager.ExecuteNonQuery(System.Data.CommandType.Text, sb.ToString(),null);

                sb.Clear();
                sb.AppendLine("insert into #de_fw_des_task_callout(customername,projectname,ecrno,process_name,component_name,activity_name,ui_name,task_name,calloutname)");
                sb.AppendLine("select distinct customername,projectname,ecrno,process_name,component_name,activity_name,ui_name,task_name,calloutname from de_fw_des_publish_task_callout_vw_fn(@customer_name,@project_name,@ecr_no)");
                var parameters = dbManager.CreateParameters(3);
                dbManager.AddParamters(parameters,0, "@customer_name", GlobalVar.Customer);
                dbManager.AddParamters(parameters,1, "@project_name", GlobalVar.Project);
                dbManager.AddParamters(parameters,2, "@ecr_no", GlobalVar.Ecrno);
                dbManager.ExecuteNonQuery(System.Data.CommandType.Text, sb.ToString(),parameters);

                sb.Clear();
                sb.AppendLine("insert into #de_fw_des_task_callout_segement(customername,projectname,ecrno,process_name,component_name,activity_name,ui_name,task_name,calloutname,segmentname,instanceflag,flowattribute,sequence)");
                sb.AppendLine("select distinct customername,projectname,ecrno,process_name,component_name,activity_name,ui_name,task_name,calloutname,segmentname,instanceflag,segmentflowattribute,segmentsequence from de_fw_des_publish_task_callout_segement_vw_fn(@customer_name,@project_name,@ecr_no)");
                parameters = dbManager.CreateParameters(3);
                dbManager.AddParamters(parameters,0, "@customer_name", GlobalVar.Customer);
                dbManager.AddParamters(parameters,1, "@project_name", GlobalVar.Project);
                dbManager.AddParamters(parameters,2, "@ecr_no", GlobalVar.Ecrno);
                dbManager.ExecuteNonQuery(System.Data.CommandType.Text, sb.ToString(), parameters);

                sb.Clear();
                sb.AppendLine("insert into #de_fw_des_task_callout_dataitem(customername,projectname,ecrno,process_name,component_name,activity_name,ui_name,task_name,calloutname,segmentname,dataitemname,flowattribute)");
                sb.Append("select distinct customername,projectname,ecrno,process_name,component_name,activity_name,ui_name,task_name,calloutname,segmentname,dataitemname,flowattribute from de_fw_des_publish_task_callout_dataitem_vw_fn(@customer_name,@project_name,@ecr_no)");
                parameters = dbManager.CreateParameters(3);
                dbManager.AddParamters(parameters,0, "@customer_name", GlobalVar.Customer);
                dbManager.AddParamters(parameters,1, "@project_name", GlobalVar.Project);
                dbManager.AddParamters(parameters,2, "@ecr_no", GlobalVar.Ecrno);
                dbManager.ExecuteNonQuery(System.Data.CommandType.Text, sb.ToString(), parameters);


                sb.Clear();
                sb.AppendLine("insert into #fw_des_service_dataitem ");
                sb.AppendLine("select  dataitemname ,defaultvalue , flowattribute ,ispartofkey ,mandatoryflag , segmentname , servicename, itk_dataitem, 'dbo', getdate(), '' From de_fw_des_publish_service_dataitem_vw_fn (@customername, @projectname, @ecrno) where   customername  = @customername  and   Projectname   = @projectname  and   ecrno     = @ecrno");
                parameters = dbManager.CreateParameters(5);
                dbManager.AddParamters(parameters,0, "@customername", GlobalVar.Customer);
                dbManager.AddParamters(parameters,1, "@projectname", GlobalVar.Project);
                dbManager.AddParamters(parameters,2, "@componentname", GlobalVar.Component);
                dbManager.AddParamters(parameters,3, "@ecrno", GlobalVar.Ecrno);
                dbManager.AddParamters(parameters,4, "@bTabs", 1);
                dbManager.ExecuteNonQuery(System.Data.CommandType.Text, sb.ToString(), parameters);


                sb.Clear();
                sb.AppendLine("insert into #fw_req_bterm (btdesc, btname, datatype, isbterm, length, maxvalue, minvalue, precisiontype , UpdUser, Updtime) ");
                sb.AppendLine("select btdesc, btname, datatype, isbterm, length, maxvalue, minvalue, precisiontype , host_name(), getdate() from    de_fw_req_publish_bterm_vw_fn (@customername, @projectname, @ecrno) where customername = @customername  and projectname = @projectname and ecrno = @ecrno");
                parameters = dbManager.CreateParameters(5);
                dbManager.AddParamters(parameters,0, "@customername", GlobalVar.Customer);
                dbManager.AddParamters(parameters,1, "@projectname", GlobalVar.Project);
                dbManager.AddParamters(parameters,2, "@componentname", GlobalVar.Component);
                dbManager.AddParamters(parameters,3, "@ecrno", GlobalVar.Ecrno);
                dbManager.AddParamters(parameters,4, "@bTabs", 1);
                dbManager.ExecuteNonQuery(System.Data.CommandType.Text, sb.ToString(), parameters);


                sb.Clear();
                sb.AppendLine("insert into #fw_req_bterm_synonym (btname, btsynonym, UpdUser, Updtime)");
                sb.AppendLine("select btname, btsynonym, host_name(), getdate() from de_fw_req_publish_bterm_synonym_vw_fn (@customername, @projectname, @ecrno) where customername = @customername  and projectname = @projectname and ecrno = @ecrno");
                parameters = dbManager.CreateParameters(5);
                dbManager.AddParamters(parameters,0, "@customername", GlobalVar.Customer);
                dbManager.AddParamters(parameters,1, "@projectname", GlobalVar.Project);
                dbManager.AddParamters(parameters,2, "@componentname", GlobalVar.Component);
                dbManager.AddParamters(parameters,3, "@ecrno", GlobalVar.Ecrno);
                dbManager.AddParamters(parameters,4, "@bTabs", 1);
                dbManager.ExecuteNonQuery(System.Data.CommandType.Text, sb.ToString(), parameters);

                sb.Clear();
                sb.AppendLine("SELECT Distinct ");
                sb.AppendLine("ISNULL(cast(di.DataItemName as nvarchar), \'\') as name,");
                sb.AppendLine("ISNULL(cast(bt.DataType as nvarchar), \'\') as type");
                sb.AppendLine("FROM    #fw_des_service_dataitem di (Nolock),  ");
                sb.AppendLine("#fw_req_bterm bt   (Nolock),  ");
                sb.AppendLine("#fw_req_bterm_synonym sy(Nolock) ");
                sb.AppendLine("Where sy.BTSynonym = di.DataItemName");
                sb.AppendLine("and  sy.btname = bt.btname");
                sb.AppendLine("order by name  ");
                GlobalVar.BtInfo = dtBtInfo = dbManager.ExecuteDataTable(CommandType.Text, sb.ToString(),null);

                dtCalloutinfo = dbManager.ExecuteDataTable(CommandType.Text, "select * from #de_fw_des_task_callout",null);
                dtCalloutSegmentInfo = dbManager.ExecuteDataTable(CommandType.Text, "select * from #de_fw_des_task_callout_segement", null);
                dtCalloutDataItemInfo = dbManager.ExecuteDataTable(CommandType.Text, "select * from #de_fw_des_task_callout_dataitem", null);

                if (dtCalloutinfo.Rows.Count > 0)
                {
                    dataStoreCallout = (from c in dtCalloutinfo.AsEnumerable()
                                        select new DataStoreCallout
                                        {
                                            CustomerName = c.Field<String>("customername"),
                                            ProjectName = c.Field<String>("projectname"),
                                            ProcessName = c.Field<String>("process_name"),
                                            EcrNO = c.Field<String>("ecrno"),
                                            ComponentName = c.Field<String>("component_name").ToLower(),
                                            ActivityName = c.Field<String>("activity_name"),
                                            Ui = c.Field<String>("ui_name"),
                                            CalloutName = c.Field<String>("calloutname").ToLower(),
                                            Task = c.Field<String>("task_name"),

                                            CalloutSegments = (from s in dtCalloutSegmentInfo.AsEnumerable()
                                                               where c.Field<String>("calloutname") == s.Field<String>("calloutname")
                                                               select new CalloutSegment
                                                               {
                                                                   Name = s.Field<String>("segmentname").ToLower(),
                                                                   Activity = s.Field<String>("activity_name"),
                                                                   Ui = s.Field<String>("ui_name"),
                                                                   TaskName = s.Field<String>("task_name"),
                                                                   CalloutName = s.Field<String>("calloutname"),
                                                                   Inst = s.Field<String>("instanceflag"),
                                                                   FlowAttribute = s.Field<String>("flowattribute"),
                                                                   Sequence = s.Field<String>("sequence"),
                                                                   CalloutDataitems = (from d in dtCalloutDataItemInfo.AsEnumerable()
                                                                                       where s.Field<String>("calloutname") == d.Field<String>("calloutname")
                                                                                             && s.Field<String>("segmentname") == d.Field<String>("segmentname")
                                                                                       select new CalloutDataItem
                                                                                       {
                                                                                           Name = d.Field<String>("dataitemname").ToLower(),
                                                                                           SegmentName = d.Field<String>("segmentname").ToLower(),
                                                                                           CalloutName = d.Field<String>("calloutname"),
                                                                                           Activity = d.Field<String>("activity_name"),
                                                                                           Ui = d.Field<String>("ui_name"),
                                                                                           FlowDirection = d.Field<String>("flowattribute"),
                                                                                           TaskName = d.Field<string>("task_name")
                                                                                       }).Distinct()
                                                               }).Distinct()
                                        }).Distinct();
                }
                else
                {
                    dataStoreCallout = new List<DataStoreCallout>();
                }


            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("DownloadCallout->{0}", !Object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
            finally
            {
                dbManager.RollbackTransaction();
                dbManager.Close();
            }
        }

        public bool Generate()
        {
            this.DownloadCallout();
            if (dataStoreCallout.Count() > 0)
            {
                foreach (DataStoreCallout dsc in dataStoreCallout)
                {
                    GenerateDataStore dataStore = new GenerateDataStore(dsc);
                    dataStore.Generate();

                    GenerateCallout callout = new GenerateCallout(dsc);
                    callout.Generate();

                    //if (!this.Compile(Path.Combine(GlobalVar.SourcePath, dsc.CalloutName), GlobalVar.ReleasePath, dsc.CalloutName))
                    //    return false;
                }
            }

            return true;
        }

        private bool Compile(string sSourceDir, string sTargetDir, string sAssemblyName)
        {
            string sCompilationString = string.Empty;
            string currentDirectory = Environment.CurrentDirectory;
            try
            {
                string[] files = Directory.GetFiles(@sSourceDir, "*.cs", SearchOption.TopDirectoryOnly);

                CodeDomProvider cdp = CodeDomProvider.CreateProvider("CSharp", new Dictionary<string, string>() { { "CompilerVersion", "v3.5" } });
                ICodeCompiler compiler = cdp.CreateCompiler();

                List<string> referenceDlls = new List<string>();
                System.Diagnostics.DefaultTraceListener trace = new System.Diagnostics.DefaultTraceListener();
                referenceDlls.Add("System.AddIn.Contract.dll");
                referenceDlls.Add("System.Configuration.dll");
                referenceDlls.Add("System.dll");
                referenceDlls.Add("System.Runtime.Serialization.dll");
                referenceDlls.Add("System.ServiceModel.dll");
                referenceDlls.Add("System.ServiceModel.Web.dll");
                referenceDlls.Add("System.Web.dll");
                referenceDlls.Add("System.Web.Extensions.dll");
                referenceDlls.Add("System.Core.dll");
                referenceDlls.Add("System.Xml.dll");
                referenceDlls.Add("System.Core.dll");
                referenceDlls.Add(Path.Combine(currentDirectory, "Ramco.VW.RT.Web.Core.dll"));
                referenceDlls.Add(Path.Combine(currentDirectory, "Ramco.VW.RT.Web.TaskCallout.dll"));

                Common.CreateDirectory(sTargetDir, false);
                CompilerParameters parameters = new CompilerParameters(referenceDlls.ToArray(), Path.Combine(sTargetDir, sAssemblyName + ".dll"), true);
                parameters.GenerateExecutable = false;

                CompilerResults results = compiler.CompileAssemblyFromFileBatch(parameters, files);

                if (results.Errors.Count == 0)
                {
                    string sSrcFile, sTargFile;
                    sSrcFile = Path.Combine(sSourceDir, sAssemblyName + ".dll");
                    sTargFile = Path.Combine(sTargetDir, sAssemblyName + ".dll");
                    if (File.Exists(sTargFile))
                    {
                        return true;
                    }
                    return false;
                }
                else
                {
                    foreach (CompilerError error in results.Errors)
                    {
                        logger.WriteLogToFile(GlobalVar.LogPath, error.ToString());
                    }

                    return false;
                }

            }
            catch
            {
                return false;
            }
        }

    }

    internal class GenerateDataStore : CodeDomHelper
    {
        DataStoreCallout dataStoreCallout;
        CodeUnit DataStore;
        Logger logger;

        public GenerateDataStore(DataStoreCallout _dataStoreCallout)
        {
            logger = new Logger();
            DataStore = new CodeUnit();
            dataStoreCallout = _dataStoreCallout;
            DataStore.CompileUnit = new System.CodeDom.CodeCompileUnit();
            DataStore.NameSpace = new System.CodeDom.CodeNamespace();
            DataStore.UserDefinedTypes = new List<System.CodeDom.CodeTypeDeclaration>();
        }

        public void Generate()
        {
            logger.WriteLogToTraceListener("CreateNamespace", "Creating Datastore Namespace..");
            CreateNamespace(String.Format("Ramco.VW.TaskCallout.{0}.{1}datastore", dataStoreCallout.ComponentName.ToLower(), dataStoreCallout.CalloutName));

            logger.WriteLogToTraceListener("ImportNamespace", "Creating Datastore Namespace..");
            ImportNamespace();

            foreach (string segmentname in dataStoreCallout.CalloutSegments.Select(s => s.Name).Distinct())
            {
                CalloutSegment calloutSegment = dataStoreCallout.CalloutSegments.Where(s => s.Name == segmentname).First();

                logger.WriteLogToTraceListener("CreateClass", "Creating Datastore class..");
                CreateClass(calloutSegment);
            }

            logger.WriteLogToTraceListener("StichCsFile", "Stitching class components together..");
            DataStore.StitchCSFile();

            logger.WriteLogToTraceListener("WriteCSFile", "Writing class to a physical file..");
            DataStore.WriteCSFile("CalloutDataStore", Path.Combine(GlobalVar.SourcePath, dataStoreCallout.CalloutName));
        }

        private void CreateNamespace(string sDataStoreNamespace)
        {
            try
            {
                DataStore.NameSpace.Name = sDataStoreNamespace;
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("CreateNamespace->{0}", !Object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }

        private void ImportNamespace()
        {
            DataStore.ReferencedNamespace.Add("System");
            DataStore.ReferencedNamespace.Add("Ramco.VW.RT.Web.TaskCallout");
            DataStore.ReferencedNamespace.Add("Ramco.VW.RT.Web.TaskCallout.Helper");
        }

        private void CreateClass(CalloutSegment segment)
        {
            CodeTypeDeclaration newUserDefinedType = new CodeTypeDeclaration
            {
                Name = segment.Inst.Equals("1") ? ("c" + segment.Name) : ("c_" + segment.Name),
                IsClass = true,
                Attributes = MemberAttributes.Public,
                TypeAttributes = TypeAttributes.Sealed
            };
            newUserDefinedType.BaseTypes.Add(new CodeTypeReference { BaseType = "IVWTaskCalloutData" });
            DataStore.UserDefinedTypes.Add(newUserDefinedType);

            CreateMemberFields(newUserDefinedType, segment.CalloutDataitems);
            CreateMemberFunctions(newUserDefinedType, segment.CalloutDataitems);
        }

        private void CreateMemberFields(CodeTypeDeclaration UserDefinedType, IEnumerable<CalloutDataItem> dataitems)
        {
            foreach (string sDataItem in dataitems.Select(d => d.Name).Distinct())
            {
                var datatype = typeof(string);
                try
                {
                    datatype = Common.CategorizeDIType((from bt in GlobalVar.BtInfo.AsEnumerable()
                                                        where bt.Field<String>("name").ToString() == sDataItem
                                                        select bt.Field<String>("type")).First());
                }
                catch
                {
                    datatype = typeof(string);
                }

                if (datatype == typeof(int))
                    DeclareMemberField(MemberAttributes.Public, UserDefinedType, sDataItem, datatype, true, InitExpression: PrimitiveExpression(0));
                else
                    DeclareMemberField(MemberAttributes.Public, UserDefinedType, sDataItem, datatype, false);

            }
            UserDefinedType.Members.Add(new CodeMemberField("Int32?", "tempVar"));
        }

        private void CreateMemberFunctions(CodeTypeDeclaration UserDefinedType, IEnumerable<CalloutDataItem> calloutDataItems)
        {
            UserDefinedType.Members.Add(GetDIValue(calloutDataItems));
            UserDefinedType.Members.Add(SetDIValue(calloutDataItems));
            UserDefinedType.Members.Add(Clear(calloutDataItems));
        }

        private CodeMemberMethod GetDIValue(IEnumerable<CalloutDataItem> calloutDataItems)
        {
            CodeMemberMethod GetDIValue;
            GetDIValue = new CodeMemberMethod
            {
                Attributes = MemberAttributes.Public | MemberAttributes.Final,
                Name = "GetDIValue",
                ReturnType = new CodeTypeReference(typeof(string))
            };

            CodeParameterDeclarationExpression parameter1 = new CodeParameterDeclarationExpression(typeof(string), "diName");
            GetDIValue.Parameters.Add(parameter1);

            CodeVariableDeclarationStatement Variable1 = DeclareVariableAndAssign("String", "retVal", true, GetProperty(typeof(string), "Empty"));
            GetDIValue.Statements.Add(Variable1);

            GetDIValue.Statements.Add(SnippetStatement("switch (" + parameter1.Name + ".ToLowerInvariant())"));
            GetDIValue.Statements.Add(SnippetStatement("{"));
            foreach (string dataitem in calloutDataItems.Select(d => d.Name).Distinct())
            {
                Type datatype;
                try
                {
                    datatype = Common.CategorizeDIType((from bt in GlobalVar.BtInfo.AsEnumerable()
                                                        where bt.Field<String>("name").ToString() == dataitem
                                                        select bt.Field<String>("type")).First());
                }
                catch
                {
                    datatype = typeof(string);
                }

                GetDIValue.Statements.Add(SnippetStatement("case \"" + dataitem + "\":"));
                if (datatype == typeof(string))
                    GetDIValue.Statements.Add(ReturnExpression(VariableReferenceExp(dataitem)));
                else
                {
                    GetDIValue.Statements.Add(AssignVariable(VariableReferenceExp(Variable1.Name), MethodInvocationExp(FieldReferenceExp(ThisReferenceExp(), dataitem), "ToString")));
                    GetDIValue.Statements.Add(SnippetExpression("break"));
                }
            }
            GetDIValue.Statements.Add(SnippetStatement("}//ENDSWITCH"));
            GetDIValue.Statements.Add(ReturnExpression(VariableReferenceExp(Variable1.Name)));
            return GetDIValue;
        }

        private CodeMemberMethod SetDIValue(IEnumerable<CalloutDataItem> calloutDataItems)
        {
            CodeMemberMethod SetDIValue;
            SetDIValue = new CodeMemberMethod
            {
                Attributes = MemberAttributes.Public | MemberAttributes.Final,
                Name = "SetDIValue"
            };
            CodeParameterDeclarationExpression parameter1 = new CodeParameterDeclarationExpression(typeof(string), "diName");
            CodeParameterDeclarationExpression parameter2 = new CodeParameterDeclarationExpression(typeof(string), "diValue");
            SetDIValue.Parameters.Add(parameter1);
            SetDIValue.Parameters.Add(parameter2);
            SetDIValue.Statements.Add(SnippetStatement("switch (" + parameter1.Name + ".ToLowerInvariant())"));
            SetDIValue.Statements.Add(SnippetStatement("{"));
            foreach (string dataitem in calloutDataItems.Select(d => d.Name).Distinct())
            {
                Type dataType = typeof(string);
                try
                {
                    dataType = Common.CategorizeDIType((from bt in GlobalVar.BtInfo.AsEnumerable()
                                                        where bt.Field<String>("name").ToString() == dataitem
                                                        select bt.Field<String>("type")).First());
                }
                catch
                { }

                SetDIValue.Statements.Add(SnippetStatement("case \"" + dataitem + "\":"));
                if (dataType.Equals(typeof(Int32)))
                {
                    SetDIValue.Statements.Add(AssignField(VariableReferenceExp("tempVar"), MethodInvocationExp(TypeReferenceExp("Utility"), "VWConvertToInt").AddParameters(new CodeExpression[] { VariableReferenceExp("diValue") })));
                    SetDIValue.Statements.Add(AssignField(VariableReferenceExp(dataitem), SnippetExpression(String.Format("{0} ?? {1}", "tempVar", dataitem))));
                }
                else
                {
                    SetDIValue.Statements.Add(AssignField(FieldReferenceExp(ThisReferenceExp(), dataitem), ArgumentReferenceExp(parameter2.Name)));
                }

                SetDIValue.Statements.Add(SnippetExpression("break"));
            }
            SetDIValue.Statements.Add(SnippetStatement("}//ENDSWITCH"));
            return SetDIValue;
        }

        private CodeMemberMethod Clear(IEnumerable<CalloutDataItem> calloutDataItems)
        {
            CodeMemberMethod Clear;
            Clear = new CodeMemberMethod
            {
                Attributes = MemberAttributes.Public | MemberAttributes.Final,
                Name = "Clear",
            };

            return Clear;
        }
    }
}

