using System;
using System.Windows;

using System.Data.SqlClient;
using System.Data;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
using Ramco.VwPlf.CodeGenerator;
using Ramco.VwPlf.CodeGenerator.NTService;
using System.Text.RegularExpressions;
using System.Configuration;
using Ramco.VwPlf.Developerconsole.Custom;

namespace DeveloperconsoleWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Guid _guid;
        ModelViewData _modelViewData;
        CancellationTokenSource _cancellationTokenSource;
        delegate void sampleDelegate(Hashtable parameters);

        string customerName = string.Empty;
        string projectName = string.Empty;


        public MainWindow()
        {
            InitializeComponent();
            this._modelViewData = new ModelViewData();
            this.DataContext = _modelViewData;
        }

        //reference - http://blog.stephencleary.com/2010/06/reporting-progress-from-tasks.html
        private void StartBackgroundTask(sampleDelegate functionToCall, Hashtable parameters)
        {
            this._cancellationTokenSource = new CancellationTokenSource();
            var _cancellationToken = this._cancellationTokenSource.Token;
            var _progressReporter = new ProgressReporter();

            var task = Task.Factory.StartNew(() =>
           {
               _cancellationToken.ThrowIfCancellationRequested();

               _progressReporter.ReportProgressAsync(() =>
               {
                   this.rbiProgress.BusyContent = this._modelViewData.StatusUpdate.Status;
                   this.rbiProgress.IsBusy = true;
               });

               //Thread.Sleep(5000);
               functionToCall(parameters);
           });

            _progressReporter.RegisterContinuation(task, () =>
            {
                this.rbiProgress.BusyContent = "Completed..";
                this.rbiProgress.IsBusy = false;
                if (task.Exception != null)
                {
                    foreach (var exception in task.Exception.InnerExceptions)
                    {
                        this.lstUnhandledExceptions.Items.Add(exception.InnerException != null ? exception.InnerException.Message : exception.Message);
                    }
                }
                else if (task.IsCanceled)
                {
                    this.lstTracker.Items.Add("Background task cancelled.");
                }
                else
                {
                    this.lstTracker.Items.Add("Background task result : " + task.Status.ToString());
                }
            });
        }

        private DataTable FillData(string sQuery, CommandType commandType, Hashtable parameters)
        {
            DataTable dtResult = new DataTable();

            using (SqlConnection connection = new SqlConnection(App.connectionString))
            {
                using (SqlCommand command = new SqlCommand(sQuery, connection))
                {
                    command.CommandType = commandType;

                    var enumerator = parameters.GetEnumerator();
                    while (enumerator.MoveNext())
                    {
                        command.Parameters.Add(new SqlParameter(enumerator.Key.ToString(), enumerator.Value.ToString()));
                    }

                    using (SqlDataAdapter adapter = new SqlDataAdapter(command))
                    {
                        connection.Open();
                        adapter.Fill(dtResult);
                        connection.Close();
                    }
                }
            }

            return dtResult;
        }

        private void ExecuteNonQuery(string sQuery, CommandType commandType, Hashtable parameters)
        {
            using (SqlConnection connection = new SqlConnection(App.connectionString))
            {
                using (SqlCommand command = new SqlCommand(sQuery, connection))
                {
                    command.CommandType = commandType;

                    var enumerator = parameters.GetEnumerator();
                    while (enumerator.MoveNext())
                    {
                        command.Parameters.Add(new SqlParameter(enumerator.Key.ToString(), enumerator.Value.ToString()));
                    }

                    connection.Open();
                    command.ExecuteNonQuery();
                    connection.Close();
                }
            }
        }

        private void GetDBList(Hashtable parameters)
        {
            DataTable dt = new DataTable();

            string sServerName = parameters["ServerName"].ToString();
            string sUserName = parameters["UserName"].ToString();
            string sPassword = parameters["Password"].ToString();

            using (SqlConnection _sqlconnection = new SqlConnection(string.Format("Data Source={0};Initial Catalog={1};User ID={2};Password={3}", sServerName, "master", sUserName, sPassword)))
            {
                using (SqlCommand _sqlCommand = new SqlCommand("select distinct name from sys.databases where database_id > 4", _sqlconnection))
                {
                    using (SqlDataAdapter _sqlDataAdapter = new SqlDataAdapter(_sqlCommand))
                    {
                        _sqlconnection.Open();
                        _sqlDataAdapter.Fill(dt);
                        _sqlconnection.Close();

                        this._modelViewData.DbList = dt;
                    }
                }
            }
        }

        private void GetComponentList(Hashtable parameters)
        {
            string sCustomerName = this.customerName;
            string sProjectName = this.projectName;
            string sComponentName = parameters["ComponentName"].ToString();
            string sEcrno = parameters["EcrNo"].ToString();
            bool bLatestDocument = Boolean.Parse(parameters["LatestDocument"].ToString());

            Hashtable htParams = new Hashtable();
            htParams.Add("@engg_customer_name_in", sCustomerName);
            htParams.Add("@engg_project_name_in", sProjectName);
            htParams.Add("@ecrno_in", sEcrno);
            htParams.Add("@component_name_in", sComponentName);
            htParams.Add("@flag", bLatestDocument == true ? 1 : 0);
            htParams.Add("@report_flag", 0);
            htParams.Add("@IsPublished", 1);
            htParams.Add("@ActivityName", string.Empty);

            DataTable dtEcrList = FillData("engg_devcon_depscript_ecrcomponent_test", CommandType.StoredProcedure, htParams);
            this._modelViewData.UserSelectedEcrs = (from DataRow drEcr in dtEcrList.AsEnumerable()
                                                    select new UserSelectedEcr
                                                    {
                                                        ActivityOffset = int.Parse(drEcr["ActivityOffset"].ToString()),
                                                        Aspx = false,
                                                        Selected = false,
                                                        ComponentDescription = drEcr["ComponentDescription"].ToString(),
                                                        ComponentName = drEcr["ComponentName"].ToString(),
                                                        Description = drEcr["ECRDescription"].ToString(),
                                                        EcrNo = drEcr["ECRNo"].ToString(),
                                                        NeedActivity = false,
                                                        NeedDepScript = false,
                                                        NeedRTHtml = false,
                                                        NeedService = false,
                                                        NeedWorkflowscript = false,
                                                        WorkflowOffset = int.Parse(drEcr["WorkflowOffset"].ToString()),
                                                    }).ToList<UserSelectedEcr>();

        }

        private void GetActivityList(Hashtable parameters)
        {
            Hashtable paramAndValue = new Hashtable();
            paramAndValue.Add("@Guid", this._guid);
            paramAndValue.Add("@CustomerName", this.customerName);
            paramAndValue.Add("@ProjectName", this.projectName);
            paramAndValue.Add("@flag", 0);
            paramAndValue.Add("@report_flag", 0);
            paramAndValue.Add("@IsPublished", 1);
            paramAndValue.Add("@ActivityName", string.Empty);

            DataTable dtActivityList = FillData("engg_devcon_gendeliv_fetchactivity", CommandType.StoredProcedure, paramAndValue);
            this._modelViewData.UserSelectedActivities = (from dr in dtActivityList.AsEnumerable()
                                                          select new UserSelectedActivity
                                                          {
                                                              Selected = int.Parse(dr["ActivityChk"].ToString()) == 1 ? true : false,
                                                              ComponentName = dr["ComponentName"].ToString(),
                                                              Description = dr["ActivityDescription"].ToString(),
                                                              Ecrno = dr["EcrNo"].ToString(),
                                                              Name = dr["ActivityName"].ToString(),
                                                              NeedActivity = int.Parse(dr["ActivityChk"].ToString()) == 1 ? true : false,
                                                              NeedAspx = int.Parse(dr["AspxXml"].ToString()) == 1 ? true : false,
                                                              NeedHtml = int.Parse(dr["RtHtmlChk"].ToString()) == 1 ? true : false,
                                                              NeedReport = int.Parse(dr["ReportChk"].ToString()) == 1 ? true : false
                                                          }).ToList<UserSelectedActivity>();
        }

        private void GetUIList(Hashtable parameters)
        {
            Hashtable paramAndValue = new Hashtable();
            paramAndValue.Add("@Guid", this._guid);
            paramAndValue.Add("@CustomerName", this.customerName);
            paramAndValue.Add("@ProjectName", this.projectName);
            paramAndValue.Add("@flag", 1);
            paramAndValue.Add("@report_flag", 0);
            paramAndValue.Add("@IsPublished", 1);
            paramAndValue.Add("@ActivityName", string.Empty);
            DataTable dtUiList = FillData("engg_devcon_gendeliv_fetchui", CommandType.StoredProcedure, paramAndValue);
            this._modelViewData.UserSelectedIlbos = (from dr in dtUiList.AsEnumerable()
                                                     select new UserSelectedIlbo
                                                     {
                                                         ActivityName = dr["ActivityName"].ToString(),
                                                         ComponentName = dr["ComponentName"].ToString(),
                                                         Description = dr["UIDescription"].ToString(),
                                                         Ecrno = dr["EcrNo"].ToString(),
                                                         Name = dr["UIName"].ToString(),
                                                         TabHeight = int.Parse(dr["TabHeight"].ToString()),
                                                         NeedAspx = int.Parse(dr["AspxXml"].ToString()) == 1 ? true : false,
                                                         NeedReport = int.Parse(dr["ReportChk"].ToString()) == 1 ? true : false,
                                                         NeedHtml = int.Parse(dr["RtHtmlChk"].ToString()) == 1 ? true : false,
                                                         Selected = int.Parse(dr["RtHtmlChk"].ToString()) == 1 ? true : false
                                                     }).ToList<UserSelectedIlbo>();
        }

        private void GetServiceList(Hashtable parameters)
        {
            Hashtable paramAndValue = new Hashtable();
            paramAndValue.Add("@Guid", this._guid);
            DataTable dtServiceList = FillData("select * from engg_gen_deliverables_service where guid = @Guid", CommandType.Text, paramAndValue);
            this._modelViewData.UserSelectedServices = (from dr in dtServiceList.AsEnumerable()
                                                        select new UserSelectedService
                                                        {
                                                            Selected = int.Parse(dr["IsSelected"].ToString()) == 1 ? true : false,
                                                            Name = dr["ServiceName"].ToString(),
                                                            ComponentName = dr["ComponentName"].ToString(),
                                                            Ecrno = dr["EcrNo"].ToString()
                                                        }).ToList<UserSelectedService>();


        }

        private void GetLangList(Hashtable parameters)
        {
            DataTable dt = FillData("Select  distinct quick_code_value as quick_code,quick_code as quick_code_value from ep_language_met(nolock) order by quick_code", CommandType.Text, parameters);
            IEnumerable<LanguageInfo> langList = (from dr in dt.AsEnumerable()
                                                  select new LanguageInfo
                                                  {
                                                      Id = int.Parse(dr["quick_code_value"].ToString()),
                                                      Name = dr["quick_code"].ToString(),
                                                      Checked = false
                                                  });
            this._modelViewData.LanguageList = new ObservableCollection<LanguageInfo>(langList);
        }

        private void SaveUiList(Hashtable parameters)
        {
            foreach (UserSelectedIlbo userSelectedIlbo in this._modelViewData.UserSelectedIlbos.Where(ui => ui.Selected == true))
            {
                parameters = new Hashtable();
                parameters.Add("@Guid", this._guid);
                parameters.Add("@CustomerName", this.customerName);
                parameters.Add("@ProjectName", this.projectName);
                parameters.Add("@EcrNo", userSelectedIlbo.Ecrno);
                parameters.Add("@ComponentName", userSelectedIlbo.ComponentName);
                parameters.Add("@ActivityName", userSelectedIlbo.ActivityName);
                parameters.Add("@UIName", userSelectedIlbo.Name);
                parameters.Add("@UIDescr", userSelectedIlbo.Description);
                parameters.Add("@tabheight", userSelectedIlbo.TabHeight);
                parameters.Add("@ReportChk", userSelectedIlbo.NeedReport == true ? 1 : 0);
                parameters.Add("@CalloutJSChk", 0);
                parameters.Add("@AspxXml", userSelectedIlbo.NeedAspx == true ? 1 : 0);
                parameters.Add("@CalloutWeb", 0);
                parameters.Add("@RTHTMLFlag", userSelectedIlbo.NeedHtml == true ? 1 : 0);
                parameters.Add("@UiSelect", userSelectedIlbo.Selected == true ? 1 : 0);
                ExecuteNonQuery("engg_devcon_gendeliv_uisave", CommandType.StoredProcedure, parameters);
            }
        }

        private void SaveComponentList(Hashtable parameters)
        {
            string sCustomerName = this.customerName;
            string sProjectName = this.projectName;
            foreach (UserSelectedEcr component in this._modelViewData.UserSelectedEcrs.Where(co => co.Selected == true))
            {
                parameters = new Hashtable();
                parameters.Add("@Guid", this._guid);
                parameters.Add("@CustomerName", sCustomerName);
                parameters.Add("@ProjectName", sProjectName);
                parameters.Add("@EcrNo", component.EcrNo);
                parameters.Add("@EcrDescr", component.Description);
                parameters.Add("@ComponentName", component.ComponentName);
                parameters.Add("@ComponentDescr", component.ComponentDescription);
                parameters.Add("@ActivityOffset", component.ActivityOffset);
                parameters.Add("@ActivityFlag", 0);
                parameters.Add("@RTHTMLFlag", 0);
                //parameters.Add("@ActivityFlag", component.NeedActivity ? 1 : 0);
                //parameters.Add("@RTHTMLFlag", component.NeedRTHtml ? 1 : 0);
                parameters.Add("@ReportChk", component.Aspx ? 1 : 0);
                parameters.Add("@WScriptFlag", component.NeedWorkflowscript ? 1 : 0);
                parameters.Add("@WorkflowOffset", component.WorkflowOffset);
                parameters.Add("@AspxXml", component.Aspx ? 1 : 0);
                parameters.Add("@CalloutWeb", 0);
                parameters.Add("@CalloutJS", 0);
                parameters.Add("@ServiceChk", component.NeedService ? 1 : 0);
                parameters.Add("@CalloutApp", 0);
                parameters.Add("@AllAct", 0);
                parameters.Add("@IsPub", 0);
                parameters.Add("@SerXml", 0);
                parameters.Add("@ErrorXml", 0);
                parameters.Add("@depscript", component.NeedDepScript ? 1 : 0);
                parameters.Add("@webxml", 0);
                parameters.Add("@error", 1);

                ExecuteNonQuery("engg_devcon_gendeliv_componentsave", CommandType.StoredProcedure, parameters);
            }
        }

        private void SaveActivityList(Hashtable parameters)
        {
            foreach (UserSelectedActivity userSelectedActivity in this._modelViewData.UserSelectedActivities.Where(a => a.Selected == true))
            {
                parameters = new Hashtable();
                parameters.Add("@Guid", this._guid);
                parameters.Add("@CustomerName", this.customerName);
                parameters.Add("@ProjectName", this.projectName);
                parameters.Add("@EcrNo", userSelectedActivity.Ecrno);
                parameters.Add("@ComponentName", userSelectedActivity.ComponentName);
                parameters.Add("@ActivityName", userSelectedActivity.Name);
                parameters.Add("@ActivityDescr", userSelectedActivity.Description);
                parameters.Add("@RTHTMLFlag", userSelectedActivity.NeedHtml == true ? 1 : 0);
                parameters.Add("@ReportFlag", userSelectedActivity.NeedReport == true ? 1 : 0);
                parameters.Add("@AspxXml", userSelectedActivity.NeedAspx == true ? 1 : 0);
                parameters.Add("@CalloutWeb", 0);
                parameters.Add("@Calloutjs", 0);
                parameters.Add("@ActivityFlag", userSelectedActivity.NeedActivity == true ? 1 : 0);
                parameters.Add("@ActSelect", userSelectedActivity.NeedActivity == true ? 1 : 0);
                parameters.Add("@UiSelect", userSelectedActivity.NeedHtml == true ? 1 : 0);

                ExecuteNonQuery("engg_devcon_gendeliv_activitysave", CommandType.StoredProcedure, parameters);
            }
        }


        private void SaveServiceList(Hashtable parameters)
        {
            foreach (UserSelectedService userSelectedService in this._modelViewData.UserSelectedServices.Where(a => a.Selected == true))
            {
                parameters = new Hashtable();
                parameters.Add("@Guid", this._guid);
                parameters.Add("@ComponentName", userSelectedService.ComponentName);
                parameters.Add("@EcrNo", userSelectedService.Ecrno);
                parameters.Add("@ServiceName", userSelectedService.Name);
                parameters.Add("@IsSelected", userSelectedService.Selected == true ? 1 : 0);

                ExecuteNonQuery("engg_devcon_gendeliv_servicesave", CommandType.StoredProcedure, parameters);
            }
        }
        private void btnRefreshDbList_Click(Object sender, RoutedEventArgs e)
        {
            this._modelViewData.StatusUpdate.Status = "Fetching database list..!";

            Hashtable parameters = new Hashtable();
            parameters.Add("ServerName", txtServer.Text);
            parameters.Add("UserName", txtUserName.Text);
            parameters.Add("Password", txtPassword.Password);

            sampleDelegate _handler = GetDBList;
            StartBackgroundTask(_handler, parameters);
        }

        private void GetCustomerAndProjList()
        {
            DataTable dt;
            dt = FillData("select distinct customer_name,project_name from de_ui_ico(nolock) ", CommandType.Text, new Hashtable());
            this._modelViewData.CustProjMappings = (from dr in dt.AsEnumerable()
                                                    select new CustomerProjectMapping
                                                    {
                                                        Customer = new Customer { Name = dr["customer_name"].ToString() },
                                                        Project = new Project { Name = dr["project_name"].ToString() }
                                                    }
                                               ).ToList<CustomerProjectMapping>();


            this._modelViewData.CustomerList = this._modelViewData.CustProjMappings.Select(m => m.Customer).Distinct(new CustomerComparer()).ToList();
            this._modelViewData.ProjectList = this._modelViewData.CustProjMappings.Where(m => string.Compare(m.Customer.Name, this._modelViewData.CustomerList.First().Name, true) == 0).Select(m => m.Project).Distinct(new ProjectComparer()).ToList();
        }

        private void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            this._modelViewData.Login = new Login
            {
                ServerName = txtServer.Text,
                UserName = txtUserName.Text,
                Password = txtPassword.Password,
                Database = cmbDatabase.SelectedValue.ToString()
            };

            App.connectionString = this._modelViewData.Login.FormConnectionString();

            this._modelViewData.ServerName = this._modelViewData.Login.ServerName;
            this._modelViewData.UserName = this._modelViewData.Login.UserName;
            this._modelViewData.Password = this._modelViewData.Login.Password;
            this._modelViewData.Database = this._modelViewData.Login.Database;

            GetCustomerAndProjList();
            cmbCustomer.SelectedIndex = 0;
            StartBackgroundTask(new sampleDelegate(GetLangList), new Hashtable());
        }

        private void AuthenticateAndGetConnectionString(Hashtable parameters)
        {
            string sModelUrl = parameters["url"].ToString();
            string sModelUserName = parameters["username"].ToString();
            string sModelPassword = parameters["password"].ToString();

            //ModelConfiguration modelConfiguration = (ModelConfiguration)ConfigurationManager.GetSection("modelConfiguration");
            //var enumerator = modelConfiguration.Models.GetEnumerator();
            //while (enumerator.MoveNext())
            //{
            //    var modelInfo = (ModelInfo)enumerator.Current;
            //    sModelUrl = modelInfo.Url;
            //    sModelUserName = modelInfo.UserName;
            //    sModelPassword = modelInfo.Password;
            //}


            if (!this._modelViewData.Login.AuthenticateUser(sModelUrl, sModelUserName, sModelPassword, "", "", ""))
            {
                throw new Exception("Authentication Failed!.");
            }
            this._modelViewData.Login.GetDepDBConnectionString(sModelUrl, "admin", "1", "0");
            App.connectionString = this._modelViewData.Login.GetRMConnectionString(sModelUrl, "preview", "0", "0");
            SqlConnectionStringBuilder sqlCon = new SqlConnectionStringBuilder(App.connectionString);

            this._modelViewData.ServerName = sqlCon.DataSource;
            this._modelViewData.UserName = sqlCon.UserID;
            this._modelViewData.Password = sqlCon.Password;
            this._modelViewData.Database = sqlCon.InitialCatalog;
        }

        private void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            this._guid = Guid.NewGuid();
            this.customerName = cmbCustomer.SelectedValue.ToString();
            this.projectName = cmbProject.SelectedValue.ToString();

            this._modelViewData.UserSelectedEcrs = null;
            this._modelViewData.UserSelectedActivities = null;
            this._modelViewData.UserSelectedIlbos = null;
            this._modelViewData.UserSelectedServices = null;


            Hashtable parameters = new Hashtable();
            parameters.Add("ComponentName", txtComponent.Text);
            parameters.Add("EcrNo", txtEcrNo.Text);
            parameters.Add("LatestDocument", chkLatestDoc.IsChecked);

            //this.rbiProgress.BusyContent = "Loading Component List...";
            this._modelViewData.StatusUpdate.Status = "Loading component list...!";
            sampleDelegate _handler = new sampleDelegate(GetComponentList);
            StartBackgroundTask(_handler, parameters);

            tbcDeliverables.SelectedIndex = 0;

        }

        private void btnSaveComponentList_Click(object sender, RoutedEventArgs e)
        {
            //this.rbiProgress.BusyContent = "Saving Component List..";
            this._modelViewData.StatusUpdate.Status = "Saving component list...!";

            Hashtable parameters = new Hashtable();

            sampleDelegate _handler = new sampleDelegate(SaveComponentList);
            StartBackgroundTask(_handler, parameters);
        }

        private void btnLoadActivityList_Click(object sender, RoutedEventArgs e)
        {
            //this.rbiProgress.BusyContent = "Loading Activity List..";
            this._modelViewData.StatusUpdate.Status = "Loading activity list...!";

            Hashtable parameters = new Hashtable();

            sampleDelegate _handler = new sampleDelegate(GetActivityList);
            StartBackgroundTask(_handler, parameters);
        }

        private void btnSaveActivityList_Click(object sender, RoutedEventArgs e)
        {
            //this.rbiProgress.BusyContent = "Saving Activity List..";
            this._modelViewData.StatusUpdate.Status = "Saving activity list...!";

            Hashtable parameters = new Hashtable();

            sampleDelegate _handler = new sampleDelegate(SaveActivityList);
            StartBackgroundTask(_handler, parameters);
        }

        private void btnLoadUiList_Click(object sender, RoutedEventArgs e)
        {
            //this.rbiProgress.BusyContent = "Loading Ui List..";
            this._modelViewData.StatusUpdate.Status = "Loading Ilbo list...!";

            Hashtable parameters = new Hashtable();

            sampleDelegate _handler = new sampleDelegate(GetUIList);
            StartBackgroundTask(_handler, parameters);
        }

        private void btnSaveUiList_Click(object sender, RoutedEventArgs e)
        {
            //this.rbiProgress.BusyContent = "Saving Ui List..";
            this._modelViewData.StatusUpdate.Status = "Saving Ilbo list...!";

            Hashtable parameters = new Hashtable();

            sampleDelegate _handler = new sampleDelegate(SaveUiList);
            StartBackgroundTask(_handler, parameters);
        }


        private void btnLoadServiceList_Click(object sender, RoutedEventArgs e)
        {
            this._modelViewData.StatusUpdate.Status = "Loading Service list...!";
            Hashtable parameters = new Hashtable();

            sampleDelegate _handler = new sampleDelegate(GetServiceList);
            StartBackgroundTask(_handler, parameters);
        }

        private void btnSaveServiceList_Click(object sender, RoutedEventArgs e)
        {
            this._modelViewData.StatusUpdate.Status = "Saving Service list...!";
            Hashtable parameters = new Hashtable();

            sampleDelegate _handler = new sampleDelegate(SaveServiceList);
            StartBackgroundTask(_handler, parameters);
        }

        private void chkSelectAllECR_Checked(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.CheckBox chkBox = (System.Windows.Controls.CheckBox)sender;

            if (_modelViewData != null)
            {
                if (_modelViewData.UserSelectedEcrs != null)
                {
                    if (_modelViewData.UserSelectedEcrs.Count > 0)
                    {
                        foreach (UserSelectedEcr userSelectedEcr in _modelViewData.UserSelectedEcrs)
                        {
                            userSelectedEcr.Selected = (bool)chkBox.IsChecked;
                        }
                    }
                }
            }
        }

        private async Task SaveLanguage(Hashtable paramAndValue)
        {
            foreach (LanguageInfo lang in this._modelViewData.LanguageList.Where(l => l.Checked == true))
            {
                paramAndValue = new Hashtable();

                paramAndValue.Add("@guid", this._guid);
                paramAndValue.Add("@languageid", lang.Id);
                paramAndValue.Add("@language_desc", lang.Name);

                ExecuteNonQuery("engg_devcon_codegen_options_lang_sp", CommandType.StoredProcedure, paramAndValue);
            }
        }

        private async Task SaveOptions(Hashtable parameters)
        {
            Hashtable paramAndValue = new Hashtable();
            paramAndValue.Add("@guid", this._guid);
            paramAndValue.Add("@rtgif", "N");
            paramAndValue.Add("@fpgrid", "N");
            paramAndValue.Add("@sectioncollapse", "N");
            paramAndValue.Add("@displaysepcification", "N");
            paramAndValue.Add("@fillerrow", "N");
            paramAndValue.Add("@gridalternaterowcolor", "N");
            paramAndValue.Add("@glowcorners", "N");
            paramAndValue.Add("@niftybuttons", "N");
            paramAndValue.Add("@smoothbuttons", "N");
            paramAndValue.Add("@statejs", "N");
            paramAndValue.Add("@blockerrors", "N");
            paramAndValue.Add("@linkmode", "N");
            paramAndValue.Add("@scrolltitle", "N");
            paramAndValue.Add("@tooltip", "N");
            paramAndValue.Add("@wizardhtml", "N");
            paramAndValue.Add("@helpmode", "N");
            paramAndValue.Add("@impdeliverables", "N");
            paramAndValue.Add("@quicklinks", "N");
            paramAndValue.Add("@richui", "N");
            paramAndValue.Add("@widgetui", "N");
            paramAndValue.Add("@selallforgridcheckbox", "N");
            paramAndValue.Add("@contextmenu", "N");
            paramAndValue.Add("@extjs", chkExtjs2.IsChecked == true ? "Y" : "N");
            paramAndValue.Add("@accesskey", "N");
            paramAndValue.Add("@richtree", "N");
            paramAndValue.Add("@richchart", "N");
            paramAndValue.Add("@compresshtml", "N");
            paramAndValue.Add("@compressjs", "N");
            paramAndValue.Add("@allstyle", chkAllStyle.IsChecked == true ? "Y" : "N");
            paramAndValue.Add("@alltaskdata", "N");
            paramAndValue.Add("@cellspacing", "N");
            paramAndValue.Add("@applicationcss", "N");
            paramAndValue.Add("@comments", "N");
            paramAndValue.Add("@inplacetrialbar", "N");
            paramAndValue.Add("@captionalignment", "");
            paramAndValue.Add("@uiformat", "");
            paramAndValue.Add("@trialbar", "");
            paramAndValue.Add("@smartspan", "N");
            paramAndValue.Add("@stylesheet", "");
            paramAndValue.Add("@chart", chkChart.IsChecked == true ? "Y" : "N");
            paramAndValue.Add("@state", chkState.IsChecked == true ? "Y" : "N");
            paramAndValue.Add("@pivot", "N");
            paramAndValue.Add("@ddt", chkddt.IsChecked == true ? "Y" : "N");
            paramAndValue.Add("@cvs", "N");
            paramAndValue.Add("@excelreport", "N");
            paramAndValue.Add("@logicalextn", "N");
            paramAndValue.Add("@errorxml", "N");
            paramAndValue.Add("@instconfig", "N");
            paramAndValue.Add("@imptoolkitdel", "N");
            paramAndValue.Add("@spstub", "N");
            paramAndValue.Add("@refdocs", "N");
            paramAndValue.Add("@quicklink", "N");
            paramAndValue.Add("@datascript", "N");
            paramAndValue.Add("@edksscript", chkEdkScript.IsChecked == true ? "Y" : "N");
            paramAndValue.Add("@controlextn", chkCtrlExtn.IsChecked == true ? "Y" : "N");
            paramAndValue.Add("@seperrordll", "Y");
            paramAndValue.Add("@customurl", "N");
            paramAndValue.Add("@datadriventask", "N");
            paramAndValue.Add("@custombr", "N");
            paramAndValue.Add("@app", "N");
            paramAndValue.Add("@sys", "N");
            paramAndValue.Add("@grc", "N");
            paramAndValue.Add("@deploydeliverables", "N");
            paramAndValue.Add("@alllanguage", "N");
            paramAndValue.Add("@platform", "DotNet");
            paramAndValue.Add("@appliation_rm_type", "SQL Server");
            paramAndValue.Add("@generationpath", parameters["GenerationPath"].ToString());
            paramAndValue.Add("@multittx", "N");
            paramAndValue.Add("@repprintdate", "N");
            paramAndValue.Add("@iEDK", "N");
            paramAndValue.Add("@ucd", "N");
            paramAndValue.Add("@ezreport", "N");
            paramAndValue.Add("@CEXml", "N");
            paramAndValue.Add("@InTD", "N");
            paramAndValue.Add("@onlyxml", "N");
            paramAndValue.Add("@reportaspx", chkReportAspx.IsChecked == true ? "Y" : "N");
            paramAndValue.Add("@webasync", "N");
            paramAndValue.Add("@errorlookup", "N");
            paramAndValue.Add("@taskpane", "N");
            paramAndValue.Add("@suffixcolon", "N");
            paramAndValue.Add("@gridfilter", "N");
            paramAndValue.Add("@ezlookup", "N");
            paramAndValue.Add("@labelselect", "N");
            paramAndValue.Add("@ReleaseVersion", "");
            paramAndValue.Add("@inlinetab", "");
            paramAndValue.Add("@split", "");
            paramAndValue.Add("@ellipses", "");
            paramAndValue.Add("@reportxml", "");
            paramAndValue.Add("@generatedatejs", "");
            paramAndValue.Add("@typebro", "");
            paramAndValue.Add("@iPad5", "");
            paramAndValue.Add("@desktopdlv", chkDesktop.IsChecked == true ? "Y" : "N");
            paramAndValue.Add("@DeviceConfigPath", "");
            paramAndValue.Add("@iPhone", "");
            paramAndValue.Add("@ellipsesleft", "");
            paramAndValue.Add("@ezeewizard", "");
            paramAndValue.Add("@layoutcontrols", "Y");
            paramAndValue.Add("@rtstate", chkState.IsChecked == true ? "Y" : "N");
            paramAndValue.Add("@SecondaryLink", "");
            paramAndValue.Add("@depscript_with_actname", "N");
            paramAndValue.Add("@extjs6", chkExtjs6.IsChecked == true ? "Y" : "N");
            paramAndValue.Add("@defaultasnull", "N");
            paramAndValue.Add("@mhub2", chkMhub2.IsChecked == true ? "Y" : "N");
            paramAndValue.Add("@customer", parameters["CustomerName"]);
            paramAndValue.Add("@project", parameters["ProjectName"]);
            paramAndValue.Add("@ecrno", string.Empty);
            paramAndValue.Add("@component", string.Empty);
            paramAndValue.Add("@codegenclient", Environment.MachineName);
            paramAndValue.Add("@status", string.Empty);
            paramAndValue.Add("@previousgenerationpath", parameters["PreviousGenerationPath"]);
            ExecuteNonQuery("vw_netgen_codegen_options_save", CommandType.StoredProcedure, paramAndValue);
        }

        private void StartGeneration(IProgress<string> progress, UserInputs userInputs)
        {
            CodegenWrapper wrapper = new CodegenWrapper(this._guid, App.connectionString, progress);

            var errors = wrapper.StartGeneration(userInputs);
            if (errors.Count() == 0)
                MessageBox.Show("Generation completed successfully...", "DeveloperConsole");
            else
                MessageBox.Show("Generation completed With error", "Warning!");
        }


        private void ClearTrackers()
        {
            lstTracker.Items.Clear();
            lstUnhandledExceptions.Items.Clear();
        }

        private void UpdateStatus(string sMessage)
        {
            this._modelViewData.StatusUpdate.Status = sMessage;
        }

        private async void btnGenerateFromXml_ClickAsync(object sender, RoutedEventArgs e)
        {
            ClearTrackers();
            UpdateStatus("Generation in progress...Please wait!");

            var progress = new Progress<string>(s =>
            {
                lblStatus.Text = s;
                lstTracker.Items.Add(s);
                lstTracker.SelectedIndex = lstTracker.Items.Count - 1;
                lstTracker.ScrollIntoView(lstTracker.SelectedItem);
            });

            UserInputs userInputs = new UserInputs();
            userInputs.DataMode = DataMode.Xml;
            userInputs.OptionXmlPath = this.txtOptionXmlPath.Text;

            await Task.Run(() => StartGeneration(progress, userInputs));

            lblStatus.Text = "Codegeneration completed..";
            lstTracker.Items.Add("Codegeneration completed..");
        }

        private async void btnGenerateFromModel_ClickAsync(object sender, RoutedEventArgs e)
        {

            ClearTrackers();
            UpdateStatus("Generation in progress...Please wait!");

            Hashtable hashTable = new Hashtable();
            hashTable.Add("CustomerName", cmbCustomer.SelectedValue.ToString());
            hashTable.Add("ProjectName", cmbProject.SelectedValue.ToString());
            hashTable.Add("GenerationPath", txtTargDir.Text);
            hashTable.Add("PreviousGenerationPath", txtPreviousDir.Text);

            Task task_SaveLanguage = SaveLanguage(hashTable);
            Task task_SaveOptions = SaveOptions(hashTable);

            // The Progress<T> constructor captures our UI context,
            //  so the lambda will be run on the UI thread.
            var progress = new Progress<string>(s =>
            {
                lblStatus.Text = s;
                lstTracker.Items.Add(s);
                lstTracker.SelectedIndex = lstTracker.Items.Count - 1;
                lstTracker.ScrollIntoView(lstTracker.SelectedItem);
                //this.rbiProgress.BusyContent = s;
                //this.rbiProgress.IsBusy = true;
            });


            await task_SaveOptions;

            await Task.Run(() => StartGeneration(progress, new UserInputs
            {
                DataMode = DataMode.Model,
                OptionXmlPath = string.Empty
            }));

            lblStatus.Text = "Codegeneration completed..";
            lstTracker.Items.Add("Codegeneration completed..");

            //rbiProgress.BusyContent = "Completed..";
            //rbiProgress.IsBusy = false;
        }

        private void cmbCustomer_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            this._modelViewData.ProjectList = this._modelViewData.CustProjMappings.Where(m => string.Compare(m.Customer.Name, cmbCustomer.SelectedValue.ToString()) == 0).Select(m => m.Project).Distinct().ToList();
            cmbProject.SelectedIndex = 0;
        }

        private void btnAuthenticateAndLogin_Click(object sender, RoutedEventArgs e)
        {
            this._modelViewData.StatusUpdate.Status = "Authenticating and Getting Connectionstring.";
            Hashtable parameters = new Hashtable();
            parameters.Add("url", txtModelURL.Text);
            parameters.Add("username", txtModelUserName.Text);
            parameters.Add("password", txtModelPassword.Password);

            StartBackgroundTask(new sampleDelegate(AuthenticateAndGetConnectionString), parameters);
        }
    }

    /// <summary> 
    /// A class used by Tasks to report progress or completion updates back to the UI. 
    /// </summary> 
    public sealed class ProgressReporter
    {
        /// <summary> 
        /// The underlying scheduler for the UI's synchronization context. 
        /// </summary> 
        private readonly TaskScheduler scheduler;

        /// <summary> 
        /// Initializes a new instance of the <see cref="ProgressReporter"/> class.
        /// This should be run on a UI thread. 
        /// </summary> 
        public ProgressReporter()
        {
            this.scheduler = TaskScheduler.FromCurrentSynchronizationContext();
        }

        /// <summary> 
        /// Gets the task scheduler which executes tasks on the UI thread. 
        /// </summary> 
        public TaskScheduler Scheduler
        {
            get { return this.scheduler; }
        }

        /// <summary> 
        /// Reports the progress to the UI thread. This method should be called from the task.
        /// Note that the progress update is asynchronous with respect to the reporting Task.
        /// For a synchronous progress update, wait on the returned <see cref="Task"/>. 
        /// </summary> 
        /// <param name="action">The action to perform in the context of the UI thread.
        /// Note that this action is run asynchronously on the UI thread.</param> 
        /// <returns>The task queued to the UI thread.</returns> 
        public Task ReportProgressAsync(Action action)
        {
            return Task.Factory.StartNew(action, CancellationToken.None, TaskCreationOptions.None, this.scheduler);
        }

        /// <summary> 
        /// Reports the progress to the UI thread, and waits for the UI thread to process
        /// the update before returning. This method should be called from the task. 
        /// </summary> 
        /// <param name="action">The action to perform in the context of the UI thread.</param> 
        public void ReportProgress(Action action)
        {
            this.ReportProgressAsync(action).Wait();
        }

        /// <summary> 
        /// Registers a UI thread handler for when the specified task finishes execution,
        /// whether it finishes with success, failiure, or cancellation. 
        /// </summary> 
        /// <param name="task">The task to monitor for completion.</param> 
        /// <param name="action">The action to take when the task has completed, in the context of the UI thread.</param> 
        /// <returns>The continuation created to handle completion. This is normally ignored.</returns> 
        public Task RegisterContinuation(Task task, Action action)
        {
            return task.ContinueWith(_ => action(), CancellationToken.None, TaskContinuationOptions.None, this.scheduler);
        }

        /// <summary> 
        /// Registers a UI thread handler for when the specified task finishes execution,
        /// whether it finishes with success, failiure, or cancellation. 
        /// </summary> 
        /// <typeparam name="TResult">The type of the task result.</typeparam> 
        /// <param name="task">The task to monitor for completion.</param> 
        /// <param name="action">The action to take when the task has completed, in the context of the UI thread.</param> 
        /// <returns>The continuation created to handle completion. This is normally ignored.</returns> 
        public Task RegisterContinuation<TResult>(Task<TResult> task, Action action)
        {
            return task.ContinueWith(_ => action(), CancellationToken.None, TaskContinuationOptions.None, this.scheduler);
        }

        /// <summary> 
        /// Registers a UI thread handler for when the specified task successfully finishes execution. 
        /// </summary> 
        /// <param name="task">The task to monitor for successful completion.</param> 
        /// <param name="action">The action to take when the task has successfully completed, in the context of the UI thread.</param> 
        /// <returns>The continuation created to handle successful completion. This is normally ignored.</returns> 
        public Task RegisterSucceededHandler(Task task, Action action)
        {
            return task.ContinueWith(_ => action(), CancellationToken.None, TaskContinuationOptions.OnlyOnRanToCompletion, this.scheduler);
        }

        /// <summary> 
        /// Registers a UI thread handler for when the specified task successfully finishes execution
        /// and returns a result. 
        /// </summary> 
        /// <typeparam name="TResult">The type of the task result.</typeparam> 
        /// <param name="task">The task to monitor for successful completion.</param> 
        /// <param name="action">The action to take when the task has successfully completed, in the context of the UI thread.
        /// The argument to the action is the return value of the task.</param> 
        /// <returns>The continuation created to handle successful completion. This is normally ignored.</returns> 
        public Task RegisterSucceededHandler<TResult>(Task<TResult> task, Action<TResult> action)
        {
            return task.ContinueWith(t => action(t.Result), CancellationToken.None, TaskContinuationOptions.OnlyOnRanToCompletion, this.Scheduler);
        }

        /// <summary> 
        /// Registers a UI thread handler for when the specified task becomes faulted. 
        /// </summary> 
        /// <param name="task">The task to monitor for faulting.</param> 
        /// <param name="action">The action to take when the task has faulted, in the context of the UI thread.</param> 
        /// <returns>The continuation created to handle faulting. This is normally ignored.</returns> 
        public Task RegisterFaultedHandler(Task task, Action<Exception> action)
        {
            return task.ContinueWith(t => action(t.Exception), CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted, this.Scheduler);
        }

        /// <summary> 
        /// Registers a UI thread handler for when the specified task becomes faulted. 
        /// </summary> 
        /// <typeparam name="TResult">The type of the task result.</typeparam> 
        /// <param name="task">The task to monitor for faulting.</param> 
        /// <param name="action">The action to take when the task has faulted, in the context of the UI thread.</param> 
        /// <returns>The continuation created to handle faulting. This is normally ignored.</returns> 
        public Task RegisterFaultedHandler<TResult>(Task<TResult> task, Action<Exception> action)
        {
            return task.ContinueWith(t => action(t.Exception), CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted, this.Scheduler);
        }

        /// <summary> 
        /// Registers a UI thread handler for when the specified task is cancelled. 
        /// </summary> 
        /// <param name="task">The task to monitor for cancellation.</param> 
        /// <param name="action">The action to take when the task is cancelled, in the context of the UI thread.</param> 
        /// <returns>The continuation created to handle cancellation. This is normally ignored.</returns> 
        public Task RegisterCancelledHandler(Task task, Action action)
        {
            return task.ContinueWith(_ => action(), CancellationToken.None, TaskContinuationOptions.OnlyOnCanceled, this.Scheduler);
        }

        /// <summary> 
        /// Registers a UI thread handler for when the specified task is cancelled. 
        /// </summary> 
        /// <typeparam name="TResult">The type of the task result.</typeparam> 
        /// <param name="task">The task to monitor for cancellation.</param> 
        /// <param name="action">The action to take when the task is cancelled, in the context of the UI thread.</param> 
        /// <returns>The continuation created to handle cancellation. This is normally ignored.</returns> 
        public Task RegisterCancelledHandler<TResult>(Task<TResult> task, Action action)
        {
            return task.ContinueWith(_ => action(), CancellationToken.None, TaskContinuationOptions.OnlyOnCanceled, this.Scheduler);
        }
    }

    public class StatusUpdate
    {
        private string _status;

        //public event PropertyChangedEventHandler PropertyChanged;

        public string Status
        {
            get
            {
                return this._status;
            }
            set
            {
                this._status = value;
                //Notify("Status");
            }
        }

        //private void Notify(string sPropertyName)
        //{
        //    if (PropertyChanged != null)
        //    {
        //        PropertyChanged(this, new PropertyChangedEventArgs(sPropertyName));
        //    }
        //}
    }

    #region Temporary
    /// <summary>
    /// 
    /// </summary>
    public class Login
    {
        public string ServerName { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string Database { get; set; }

        public string FormConnectionString()
        {
            return string.Format("MultipleActiveResultSets=true;data source={0};database={1};User ID={2};Password={3}", ServerName, Database, UserName, Password);
        }

        public bool AuthenticateUser(string modelURL, string userName, string password, string ou, string role, string langid)
        {
            LoginHelpers loginHelper = new LoginHelpers();
            return loginHelper.AuthenticateUser(modelURL, userName, password, ou, role, langid);
        }

        public string GetDepDBConnectionString(string modelURL, string component, string ou, string componentInst)
        {
            LoginHelpers loginHelper = new LoginHelpers();
            return loginHelper.GetDepDBConnectionString(modelURL, component, ou, componentInst);
        }

        public string GetRMConnectionString(string modelURL, string component, string ou, string componentInst)
        {
            LoginHelpers loginHelper = new LoginHelpers();
            return loginHelper.GetRMConnectionString(modelURL, component, ou, componentInst);
        }
    }
    /// <summary>
    /// 
    /// </summary>
    public class ModelViewData : INotifyPropertyChanged
    {
        public List<CustomerProjectMapping> CustProjMappings = null;
        private List<Customer> _customerList = null;
        private List<Project> _projectList = null;
        private DataTable _dbList = null;
        private string _serverNameValue = "UnKnown";
        private string _userNameValue = "UnKnown";
        private string _databaseValue = "UnKnown";
        private string _passwordValue = "UnKnown";
        private bool _isAllComponentSelected;
        private bool _isAllActivitySelected;
        private bool _isAllUiSelected;
        private List<UserSelectedEcr> _userSelectedEcrs = null;
        private List<UserSelectedActivity> _userSelectedActivities = null;
        private List<UserSelectedIlbo> _userSelectedHtms = null;
        private List<UserSelectedService> _userSelectedServices = null;
        private Login _login = null;
        private ObservableCollection<LanguageInfo> _languageList = null;
        private StatusUpdate _statusUpdate = null;

        public ModelViewData()
        {
            _customerList = new List<Customer>();
            _projectList = new List<Project>();
            CustProjMappings = new List<CustomerProjectMapping>();
            _login = new Login();
            _dbList = new DataTable();
            _userSelectedEcrs = new List<UserSelectedEcr>();
            _userSelectedActivities = new List<UserSelectedActivity>();
            _userSelectedHtms = new List<UserSelectedIlbo>();
            _userSelectedServices = new List<UserSelectedService>();
            _languageList = new ObservableCollection<LanguageInfo>();
            _statusUpdate = new StatusUpdate();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void Notify(string sPropertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(sPropertyName));
            }
        }

        public string ServerName
        {
            get
            {
                return _serverNameValue;
            }
            set
            {
                if (_serverNameValue != value)
                {
                    _serverNameValue = value;
                    Notify("ServerName");
                }

            }
        }

        public string UserName
        {
            get
            {
                return _userNameValue;
            }
            set
            {
                if (_userNameValue != value)
                {
                    _userNameValue = value;
                    Notify("UserName");
                }
            }
        }

        public string Database
        {
            get
            {
                return _databaseValue;
            }
            set
            {
                if (_databaseValue != value)
                {
                    _databaseValue = value;
                    Notify("Database");
                }
            }
        }

        public string Password
        {
            get
            {
                return _passwordValue;
            }
            set
            {
                _passwordValue = value;
            }
        }

        public List<Customer> CustomerList
        {
            get
            {
                return this._customerList;
            }
            set
            {
                this._customerList = value;
                Notify("CustomerList");
            }
        }

        public List<Project> ProjectList
        {
            get
            {
                return this._projectList;
            }
            set
            {
                this._projectList = value;
                Notify("ProjectList");
            }
        }

        public bool IsAllComponentSelected
        {
            get
            {
                return true;
            }
            set
            {
                this._isAllComponentSelected = value;
                Notify("IsAllComponentSelected");
            }
        }

        public bool IsAllActivitySelected
        {
            get
            {
                return this._isAllActivitySelected;
            }
            set
            {
                this._isAllActivitySelected = value;
                Notify("IsAllActivitySelected");
            }
        }

        public bool IsAllUiSelected
        {
            get
            {
                return this._isAllUiSelected;
            }
            set
            {
                this._isAllUiSelected = value;
                Notify("IsAllUiSelected");
            }
        }
        public StatusUpdate StatusUpdate
        {
            get
            {
                return this._statusUpdate;
            }
            set
            {
                this._statusUpdate = value;
                Notify("StatusUpdate");
            }
        }
        public DataTable DbList
        {
            get
            {
                return this._dbList;
            }
            set
            {
                this._dbList = value;
                Notify("DbList");
            }
        }
        public Login Login
        {
            get
            {
                return this._login;
            }
            set
            {
                this._login = value;
            }
        }

        public List<UserSelectedEcr> UserSelectedEcrs
        {
            get
            {
                return this._userSelectedEcrs;
            }
            set
            {
                this._userSelectedEcrs = value;
                Notify("UserSelectedEcrs");
            }
        }

        public List<UserSelectedActivity> UserSelectedActivities
        {
            get
            {
                return this._userSelectedActivities;
            }
            set
            {
                this._userSelectedActivities = value;
                Notify("UserSelectedActivities");
            }
        }

        public List<UserSelectedIlbo> UserSelectedIlbos
        {
            get
            {
                return this._userSelectedHtms;
            }
            set
            {
                this._userSelectedHtms = value;
                Notify("UserSelectedIlbos");
            }
        }

        public List<UserSelectedService> UserSelectedServices
        {
            get
            {
                return this._userSelectedServices;
            }
            set
            {
                this._userSelectedServices = value;
                Notify("UserSelectedServices");
            }
        }

        public ObservableCollection<LanguageInfo> LanguageList
        {
            get
            {
                return this._languageList;
            }
            set
            {
                this._languageList = value;
                Notify("LanguageList");
            }
        }
    }
    /// <summary>
    /// 
    /// </summary>
    public class UserSelectedEcr
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void Notify(string sPropertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(sPropertyName));
            }
        }

        private bool _selected;
        public bool Selected
        {
            get
            {
                return this._selected;
            }
            set
            {
                if (this._selected != value)
                {
                    this._selected = value;
                    Notify("Selected");
                }
            }
        }

        private string _componentName;
        public string ComponentName
        {
            get
            {
                return _componentName;
            }
            set
            {
                this._componentName = value;
            }
        }

        private string _ecrno;
        public string EcrNo
        {
            get
            {
                return this._ecrno;
            }
            set
            {
                this._ecrno = value;
            }
        }

        private string _Description;
        public string Description
        {
            get
            {
                return this._Description;
            }
            set
            {
                this._Description = value;
            }
        }

        private string _componentDescription;
        public string ComponentDescription
        {
            get
            {
                return this._componentDescription;
            }
            set
            {
                this._componentDescription = value;
            }
        }

        private bool _needDepscript;
        public bool NeedDepScript
        {
            get
            {
                return this._needDepscript;
            }
            set
            {
                this._needDepscript = value;
            }
        }

        private Int64 _activityOffset;
        public Int64 ActivityOffset
        {
            get
            {
                return this._activityOffset;
            }
            set
            {
                this._activityOffset = value;
            }
        }

        private bool _needWorkflowscript;
        public bool NeedWorkflowscript
        {
            get
            {
                return this._needWorkflowscript;
            }
            set
            {
                this._needWorkflowscript = value;
            }
        }

        private Int64 _workflowOffset;
        public Int64 WorkflowOffset
        {
            get
            {
                return this._workflowOffset;
            }
            set
            {
                this._workflowOffset = value;
            }
        }

        private bool _needService;
        public bool NeedService
        {
            get
            {
                return this._needService;
            }
            set
            {
                this._needService = value;
            }
        }

        private bool _needActivity;
        public bool NeedActivity
        {
            get
            {
                return this._needActivity;
            }
            set
            {
                this._needActivity = value;
            }
        }

        private bool _needRTHtml;
        public bool NeedRTHtml
        {
            get
            {
                return this._needRTHtml;
            }
            set
            {
                this._needRTHtml = value;
            }
        }

        private bool _aspx;
        public bool Aspx
        {
            get
            {
                return this._aspx;
            }
            set
            {
                this._aspx = value;
            }
        }
    }
    /// <summary>
    /// 
    /// </summary>
    public class UserSelectedActivity
    {
        private bool _selected;
        private string _ecrno;
        private string _componentName;
        private string _name;
        private string _description;
        private bool _needActivity;
        private bool _needHtml;
        private bool _needReport;
        private bool _needAspx;

        public bool Selected
        {
            get
            {
                return this._selected;
            }
            set
            {
                this._selected = value;
            }
        }
        public string Ecrno
        {
            get
            {
                return this._ecrno;
            }
            set
            {
                this._ecrno = value;
            }
        }
        public string ComponentName
        {
            get
            {
                return this._componentName;
            }
            set
            {
                this._componentName = value;
            }
        }
        public string Name
        {
            get
            {
                return this._name;
            }
            set
            {
                this._name = value;
            }
        }
        public string Description
        {
            get
            {
                return this._description;
            }
            set
            {
                this._description = value;
            }
        }
        public bool NeedActivity
        {
            get
            {
                return this._needActivity;
            }
            set
            {
                this._needActivity = value;
            }
        }
        public bool NeedHtml
        {
            get
            {
                return this._needHtml;
            }
            set
            {
                this._needHtml = value;
            }
        }
        public bool NeedReport
        {
            get
            {
                return this._needReport;
            }
            set
            {
                this._needReport = value;
            }
        }
        public bool NeedAspx
        {
            get
            {
                return this._needAspx;
            }
            set
            {
                this._needAspx = value;
            }
        }
    }
    /// <summary>
    /// 
    /// </summary>
    public class UserSelectedIlbo
    {
        private bool _selected;
        private string _ecrno;
        private string _componentName;
        private string _activityName;
        private string _name;
        private string _description;
        private int _tabHeight;
        private bool _needHtml;
        private bool _needReport;
        private bool _needAspx;

        public bool Selected
        {
            get
            {
                return this._selected;
            }
            set
            {
                this._selected = value;
            }
        }
        public string Ecrno
        {
            get
            {
                return this._ecrno;
            }
            set
            {
                this._ecrno = value;
            }
        }
        public string ComponentName
        {
            get
            {
                return this._componentName;
            }
            set
            {
                this._componentName = value;
            }
        }
        public string ActivityName
        {
            get
            {
                return this._activityName;
            }
            set
            {
                this._activityName = value;
            }
        }
        public string Name
        {
            get
            {
                return this._name;
            }
            set
            {
                this._name = value;
            }
        }
        public string Description
        {
            get
            {
                return this._description;
            }
            set
            {
                this._description = value;
            }
        }
        public int TabHeight
        {
            get
            {
                return this._tabHeight;
            }
            set
            {
                this._tabHeight = value;
            }
        }
        public bool NeedHtml
        {
            get
            {
                return this._needHtml;
            }
            set
            {
                this._needHtml = value;
            }
        }
        public bool NeedReport
        {
            get
            {
                return this._needReport;
            }
            set
            {
                this._needReport = value;
            }
        }
        public bool NeedAspx
        {
            get
            {
                return this._needAspx;
            }
            set
            {
                this._needAspx = value;
            }
        }
    }
    public class UserSelectedService
    {
        private bool _selected;
        private string _name;
        private string _ecrno;
        private string _componentName;

        public bool Selected
        {
            get
            {
                return this._selected;
            }
            set
            {
                this._selected = value;
            }
        }
        public string Name
        {
            get
            {
                return this._name;
            }
            set
            {
                this._name = value;
            }
        }
        public string Ecrno
        {
            get
            {
                return this._ecrno;
            }
            set
            {
                this._ecrno = value;
            }
        }
        public string ComponentName
        {
            get
            {
                return this._componentName;
            }
            set
            {
                this._componentName = value;
            }
        }
    }
    /// <summary>
    /// 
    /// </summary>
    public class LanguageInfo
    {
        private int _id;
        private string _name;
        private bool _checked;

        public int Id
        {
            get
            {
                return this._id;
            }
            set
            {
                this._id = value;
            }
        }

        public string Name
        {
            get
            {
                return this._name;
            }
            set
            {
                this._name = value;
            }
        }

        public bool Checked
        {
            get
            {
                return this._checked;
            }
            set
            {
                this._checked = value;
            }
        }
    }

    public class Customer
    {
        private string _name;

        public string Name
        {
            get
            {
                return this._name;
            }
            set
            {
                this._name = value;
            }
        }
    }
    public class CustomerComparer : IEqualityComparer<Customer>
    {
        public bool Equals(Customer x, Customer y)
        {
            return (string.Compare(x.Name, y.Name, true) == 0);
        }

        public int GetHashCode(Customer obj)
        {
            return obj.Name.GetHashCode();
        }
    }

    public class Project : IEquatable<Project>
    {
        private string _name;
        public string Name
        {
            get
            {
                return this._name;
            }
            set
            {
                this._name = value;
            }
        }

        public bool Equals(Project other)
        {
            if (Name == other.Name)
                return true;
            else
                return false;
        }
    }
    public class ProjectComparer : IEqualityComparer<Project>
    {
        public bool Equals(Project x, Project y)
        {
            return (string.Compare(x.Name, y.Name, true) == 0);
        }

        public int GetHashCode(Project obj)
        {
            return obj.Name.GetHashCode();
        }
    }

    public class CustomerProjectMapping
    {
        public Customer Customer { get; set; }
        public Project Project { get; set; }
    }
    #endregion
}
