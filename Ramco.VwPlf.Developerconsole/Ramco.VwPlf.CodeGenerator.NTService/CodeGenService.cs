using System;
using System.Reflection;
using System.ServiceProcess;
using System.IO;
using System.Timers;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using System.Diagnostics;
using System.Collections.Generic;
using Ramco.VwPlf.CodeGenerator.NTService.Custom;
using System.Configuration;

namespace Ramco.VwPlf.CodeGenerator.NTService
{
    public partial class CodeGenService : ServiceBase
    {
        //private string _sModelEncryptedConnString = string.Empty;
        private string _sModelDecryptedConnString = string.Empty;

        private string _sMailerEncryptedConString = string.Empty;
        private string _sMailerDecryptedConString = string.Empty;

        private ILogger _trace = null;
        private Timer _serviceTimer = null;
        private int _iRunningInstance = 0;
        private int _iMaxAllowedInstance = 0;
        private string _sBaseCodeGenpath = string.Empty;
        private string _sSharepath = string.Empty;

        private void AuthenticateAndGetConnectionString()
        {
            this._trace.Write("Authenticating and getting connectionstring starts..");

            string sModelUrl = string.Empty;
            string sModelUserName = string.Empty;
            string sModelPassword = string.Empty;
            LoginHelpers loginHelper = new LoginHelpers();

            ModelConfiguration modelConfiguration = (ModelConfiguration)ConfigurationManager.GetSection("modelConfiguration");
            var enumerator = modelConfiguration.Models.GetEnumerator();
            enumerator.MoveNext();
            var modelInfo = (ModelInfo)enumerator.Current;

            sModelUrl = modelInfo.Url;
            sModelUserName = modelInfo.UserName;
            sModelPassword = modelInfo.Password;


            //Authenticating
            if (!loginHelper.AuthenticateUser(sModelUrl, sModelUserName, sModelPassword, "", "", ""))
            {
                throw new Exception("Authentication Failed!.");
            }
            //getting depdb connectionstring
            loginHelper.GetDepDBConnectionString(sModelUrl, "admin", "1", "0");

            //getting rm connectionstring
            this._sModelDecryptedConnString = loginHelper.GetRMConnectionString(sModelUrl, "preview", "0", "0");

            this._trace.Write("Authenticating and getting connectionstring ends..");
        }

        /// <summary>
        /// 
        /// </summary>
        public CodeGenService()
        {
            InitializeComponent();
            this._serviceTimer = new Timer(1 * 10000);

            this._iRunningInstance = 0;
            this._iMaxAllowedInstance = int.Parse(System.Configuration.ConfigurationManager.AppSettings["maxinstance"].ToString());
            //this._sModelEncryptedConnString = System.Configuration.ConfigurationManager.ConnectionStrings["constr"].ConnectionString;
            this._sMailerEncryptedConString = System.Configuration.ConfigurationManager.ConnectionStrings["mailerconstr"].ConnectionString;

            this._trace = new FileLogger(Path.Combine(Environment.CurrentDirectory, "Ramco.VwPlf.CodeGenerator.NTService", ".log"));
            this._sBaseCodeGenpath = System.Configuration.ConfigurationManager.AppSettings["codegenBasePath"].ToString();
            this._sSharepath = System.Configuration.ConfigurationManager.AppSettings["sharePath"].ToString();
            //this._sModelDecryptedConnString = EncryptionHelper.Decrypt(this._sModelEncryptedConnString);
            this._sMailerDecryptedConString = EncryptionHelper.Decrypt(this._sMailerEncryptedConString);

            this.AuthenticateAndGetConnectionString();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CheckPendingAndInitializeCodegen(object sender, ElapsedEventArgs e)
        {
            string sCurrentScope = "CheckPendingAndInitializeCodegen";
            this._trace.Write("Inside CheckPendingAndInitializeCodegen");
            try
            {
                if (this._iRunningInstance < this._iMaxAllowedInstance)
                {
                    string sQry = "vw_netgen_get_pending_ecrlist";
                    DataTable dtPendingList = new DataTable();
                    this._trace.Write("Before opening connection");
                    using (SqlConnection sqlcon = new SqlConnection(this._sModelDecryptedConnString))
                    {
                        this._trace.Write("After opening connection");
                        using (SqlCommand sqlcom = new SqlCommand(sQry, sqlcon))
                        {
                            sqlcom.CommandType = CommandType.StoredProcedure;

                            using (SqlDataAdapter sqlda = new SqlDataAdapter(sqlcom))
                            {
                                sqlda.SelectCommand.CommandTimeout = 30;

                                this._trace.Write("getting pending ecrs..");
                                sqlcon.Open();
                                sqlda.Fill(dtPendingList);
                                sqlcon.Close();
                            }
                        }
                    }

                    if (dtPendingList.Rows.Count != 0)
                    {
                        string sRequestId = string.Empty;
                        try
                        {
                            ++this._iRunningInstance;

                            Guid guid = new Guid(Convert.ToString(dtPendingList.Rows[0]["Guid"]));
                            sRequestId = Convert.ToString(dtPendingList.Rows[0]["Request_ID"]);

                            UpdateStatus("SCHEDULE", sRequestId, "inprogress", "START");
                            UpdateStatus("STATUS", sRequestId, "running", "START");

                            this._trace.Write("started processing guid: " + dtPendingList.Rows[0]["Guid"]);

                            this._trace.Write("Guid:" + dtPendingList.Rows[0]["Guid"].ToString());

                            try
                            {

                                //loading assembly
                                Assembly assembly;
                                ///= Assembly.LoadFile(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Ramco.VwPlf.CodeGenerator.dll"));
                                assembly = Assembly.Load(string.Format("Ramco.VwPlf.CodeGenerator,Version={0},Culture=neutral,PublicKeyToken=null", GetConsoleVersion()));

                                //get the class u want to instantiate
                                Type type = assembly.GetType("Ramco.VwPlf.CodeGenerator.CodegenWrapper");

                                //instantiate the class
                                object classInstance = Activator.CreateInstance(type, new Guid(dtPendingList.Rows[0]["Guid"].ToString()), this._sModelDecryptedConnString);

                                //call the method to start generation
                                MethodInfo method = type.GetMethod("StartGeneration");

                                dynamic result = method.Invoke(classInstance, new object[] { new UserInputs { DataMode = DataMode.Model } });

                                //result is nothing but the error count
                                if (result.Count == 0)
                                {
                                    this._trace.Write("Success");
                                    UpdateStatus("STATUS", sRequestId, "success", "END");
                                }
                                else
                                {
                                    this._trace.Write("Failed");
                                    List<String> errLst = (List<String>)result;
                                    StringBuilder sb = new StringBuilder();
                                    foreach (string s in errLst)
                                    {
                                        sb.AppendLine(s);
                                    }
                                    UpdateStatus("STATUS", sRequestId, "failure", "END");
                                }
                            }
                            catch (Exception ex)
                            {
                                this._trace.Write("Exception : " + ex.Message);
                            }
                        }
                        catch (Exception ex1)
                        {
                            _trace.Write(string.Format("{0}->{1}", sCurrentScope, ex1.InnerException != null ? ex1.InnerException.Message : ex1.Message));
                        }
                        finally
                        {
                            --this._iRunningInstance;
                            UpdateStatus("SCHEDULE", sRequestId, "completed", "END");
                        }
                    }
                    else
                    {
                        this._trace.Write("No pending Ecrs...");
                    }

                }
                else
                {
                    this._trace.Write(string.Format("Exceeding Maximum instance!{0}. Exiting code..", this._iMaxAllowedInstance));
                }
            }
            catch (Exception ex)
            {
                _trace.Write(string.Format("{0}->{1}", sCurrentScope, ex.InnerException != null ? ex.InnerException.Message : ex.Message));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private string GetConsoleVersion()
        {
            string sDeveloperConsoleVersion = "1.0.0.0";

            this._trace.Write("Getting developerconsole_dotnet version...");
            using (SqlConnection conn = new SqlConnection(this._sModelDecryptedConnString))
            {
                using (SqlCommand cmd = new SqlCommand("select version from engg_developerconsole_version where componentname = 'developerconsole_dotnet'", conn))
                {
                    cmd.CommandType = CommandType.Text;
                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        DataTable dt = new DataTable();
                        conn.Open();
                        da.Fill(dt);
                        conn.Close();

                        if (dt.Rows.Count > 0)
                        {
                            sDeveloperConsoleVersion = Convert.ToString(dt.Rows[0][0]);
                        }
                    }
                }
            }

            return sDeveloperConsoleVersion;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private bool ResetStatus()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(this._sModelDecryptedConnString))
                {
                    using (SqlCommand cmd = new SqlCommand("vw_netgen_reset_codegen_status", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        this._trace.Write("Resetting codegen status..");

                        cmd.ExecuteNonQuery();
                    }
                }
                return true;
            }
            catch 
            {
                this._trace.Write("Error while resetting codegen status..");
                return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sTableKeyword"></param>
        /// <param name="sRequestId"></param>
        /// <param name="sStatus"></param>
        /// <param name="sStartOrEnd"></param>
        private DataTable UpdateStatus(string sTableKeyword, string sRequestId, string sStatus, string sStartOrEnd)
        {
            DataTable dt = null;
            string sCurrentScope = string.Empty;
            try
            {
                sCurrentScope = "UpdateStatus";
                this._trace.Write(string.Format("{0}->{1}", sCurrentScope, "Updating status.."));
                using (SqlConnection conn = new SqlConnection(this._sModelDecryptedConnString))
                {
                    using (SqlCommand cmd = new SqlCommand("vw_netgen_update_codegen_status", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.Add(new SqlParameter("@RequestID", sRequestId));
                        cmd.Parameters.Add(new SqlParameter("@Status", sStatus));
                        cmd.Parameters.Add(new SqlParameter("@CodegenPath", this._sBaseCodeGenpath));
                        cmd.Parameters.Add(new SqlParameter("@SharePath", Path.Combine(this._sSharepath, sRequestId)));

                        this._trace.Write(string.Format("CodegenClient : {0}", Environment.MachineName));

                        cmd.Parameters.Add(new SqlParameter("@CodegenClient", Environment.MachineName));
                        cmd.Parameters.Add(new SqlParameter("@TableKeyword", sTableKeyword));
                        cmd.Parameters.Add(new SqlParameter("@StartOrEnd", sStartOrEnd));
                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            dt = new DataTable();
                            conn.Open();
                            da.Fill(dt);
                            conn.Close();
                        }
                    }
                }

                if (string.Compare(sStartOrEnd, "end", true) == 0)
                {

                    if (dt.Rows.Count > 0)
                    {
                        this._trace.Write("Updating codegen status for mailing..");
                        DataRow dr = dt.Rows[0];
                        string guid = dr.Field<string>("Guid");
                        string requestid = dr.Field<string>("Request_ID");
                        string customer = dr.Field<string>("CustomerName");
                        string project = dr.Field<string>("ProjectName");
                        string component = dr.Field<string>("ComponentName");
                        string ecrno = dr.Field<string>("Ecrno");
                        string codegenclient = dr.Field<string>("CodeGenClient");
                        string codegenpath = dr.Field<string>("CodegenPath");
                        string sharepath = dr.Field<string>("sharePath");
                        string request_starttime = dr.Field<string>("Requeststart_datetime");
                        string requested_user = dr.Field<string>("Requested_user");
                        string remarks = dr.Field<string>("Remarks");
                        string codegenStarttime = dr.Field<string>("CodeGenStartTime");
                        string codegenEndtime = dr.Field<string>("CodeGenEndTime");
                        string codegenStatus = dr.Field<string>("CodeGenStatus");
                        string mailTo = dr.Field<string>("MailTo");
                        string mailCC = dr.Field<string>("MailCC");
                        string mailBCC = dr.Field<string>("MailBCC");
                        string mailStatus = dr.Field<string>("MailStatus");
                        string requestStatus = dr.Field<string>("RequestStatus");
                        string ecrStatus = dr.Field<string>("EcrStatus");
                        string machineName = Environment.MachineName;
                        using (SqlConnection conn = new SqlConnection(_sMailerDecryptedConString))
                        {
                            using (SqlCommand cmd = new SqlCommand("vw_netgen_insert_mailer_record", conn))
                            {
                                cmd.CommandType = CommandType.StoredProcedure;
                                cmd.Parameters.Add(new SqlParameter("@Guid", guid));
                                cmd.Parameters.Add(new SqlParameter("@Request_ID", requestid));
                                cmd.Parameters.Add(new SqlParameter("@CustomerName", customer));
                                cmd.Parameters.Add(new SqlParameter("@ProjectName", project));
                                cmd.Parameters.Add(new SqlParameter("@ComponentName", component));
                                cmd.Parameters.Add(new SqlParameter("@Ecrno", ecrno));
                                cmd.Parameters.Add(new SqlParameter("@CodeGenClient", codegenclient));
                                cmd.Parameters.Add(new SqlParameter("@CodegenPath", codegenpath));
                                cmd.Parameters.Add(new SqlParameter("@sharePath", sharepath));
                                cmd.Parameters.Add(new SqlParameter("@Requeststart_datetime", request_starttime));
                                cmd.Parameters.Add(new SqlParameter("@Requested_user", requested_user));
                                cmd.Parameters.Add(new SqlParameter("@Remarks", remarks));
                                cmd.Parameters.Add(new SqlParameter("@CodeGenStartTime", codegenStarttime));
                                cmd.Parameters.Add(new SqlParameter("@CodeGenEndTime", codegenEndtime));
                                cmd.Parameters.Add(new SqlParameter("@CodeGenStatus", codegenStatus));
                                cmd.Parameters.Add(new SqlParameter("@MailTo", mailTo));
                                cmd.Parameters.Add(new SqlParameter("@MailCC", mailCC));
                                cmd.Parameters.Add(new SqlParameter("@MailBCC", mailBCC));
                                cmd.Parameters.Add(new SqlParameter("@MailStatus", mailStatus));
                                cmd.Parameters.Add(new SqlParameter("@RequestStatus", requestStatus));
                                cmd.Parameters.Add(new SqlParameter("@EcrStatus", ecrStatus));
                                cmd.Parameters.Add(new SqlParameter("@ModelServer", "bpdtpfr"));
                                cmd.Parameters.Add(new SqlParameter("@ModelDatabase", "rvw20appdb"));
                                conn.Open();
                                cmd.ExecuteNonQuery();
                                conn.Close();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                this._trace.Write(string.Format("{0}->{1}", sCurrentScope, ex.InnerException != null ? ex.InnerException.Message : ex.Message));
            }

            return dt;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        protected override void OnStart(string[] args)
        {
            string sCurrentScope = "OnStart";
            try
            {
                this._trace.Write("Trying to start the Service..");

                //this._iRunningInstance = 0;
                //this._iMaxAllowedInstance = int.Parse(System.Configuration.ConfigurationManager.AppSettings["maxinstance"].ToString());
                //this._sEncryptedConnString = System.Configuration.ConfigurationManager.ConnectionStrings["constr"].ConnectionString;
                //this._trace = new FileLogger(Path.Combine(Environment.CurrentDirectory, "Ramco.VwPlf.CodeGenerator.NTService", ".log"));
                //this._sBaseCodeGenpath = System.Configuration.ConfigurationManager.AppSettings["codegenBasePath"].ToString();
                //this._sSharepath = System.Configuration.ConfigurationManager.AppSettings["sharePath"].ToString();
                //this._sDecryptedConnString = EncryptionHelper.Decrypt(this._sEncryptedConnString);

                this._serviceTimer.Elapsed += CheckPendingAndInitializeCodegen;
                this._serviceTimer.Enabled = true;
                this._serviceTimer.Start();
                base.OnStart(args);

                this._trace.Write("Service started successfully..");
            }
            catch (Exception ex)
            {
                this._trace.Write(string.Format("{0}->{1}", sCurrentScope, ex.InnerException != null ? ex.InnerException.Message : ex.Message));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        protected override void OnStop()
        {
            string sCurrentScope = "OnStop";
            try
            {
                this.ResetStatus();

                this._serviceTimer.Enabled = false;
                this._serviceTimer.Stop();
                base.OnStop();

                this._trace.Write("Service stopped successfully..");
            }
            catch (Exception ex)
            {
                _trace.Write(string.Format("{0}->{1}", sCurrentScope, ex.InnerException != null ? ex.InnerException.Message : ex.Message));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        protected override void OnContinue()
        {
            string sCurrentScope = "OnContinue";
            try
            {
                this._serviceTimer.Enabled = true;
                base.OnContinue();

                this._trace.Write("Service continued..");
            }
            catch (Exception ex)
            {
                _trace.Write(string.Format("{0}->{1}", sCurrentScope, ex.InnerException != null ? ex.InnerException.Message : ex.Message));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        protected override void OnPause()
        {
            string sCurrentScope = "OnPause";
            try
            {
                this._serviceTimer.Enabled = false;
                base.OnPause();

                this._trace.Write("Service paused..");
            }
            catch (Exception ex)
            {
                _trace.Write(string.Format("{0}->{1}", sCurrentScope, ex.InnerException != null ? ex.InnerException.Message : ex.Message));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        protected override void OnShutdown()
        {
            string sCurrentScope = "OnShutdown";
            try
            {
                base.OnShutdown();
                this._trace.Write("Service shutdown successful..");
            }
            catch (Exception ex)
            {
                _trace.Write(string.Format("{0}->{1}", sCurrentScope, ex.InnerException != null ? ex.InnerException.Message : ex.Message));
            }
        }
    }
}
