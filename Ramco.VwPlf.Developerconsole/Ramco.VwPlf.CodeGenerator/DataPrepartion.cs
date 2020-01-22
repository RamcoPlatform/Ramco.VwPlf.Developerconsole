using System;
using System.Text;
using System.Data;
using System.Collections.Generic;
using Ramco.VwPlf.DataAccess;
using System.Linq;
using System.Xml.Linq;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace Ramco.VwPlf.CodeGenerator
{
    public class DataPrepartion
    {
        public DBManager _dbManager;
        private Logger _logger;
        private ECRLevelOptions _ecrOptions;
        private Guid _guid;

        public DataPrepartion(Guid guid, DBManager dbManager, ref ECRLevelOptions ecrOptions)
        {
            this._guid = guid;
            this._ecrOptions = ecrOptions;
            this._dbManager = dbManager;
            this._logger = new Logger(System.IO.Path.Combine(_ecrOptions.GenerationPath, _ecrOptions.Platform, _ecrOptions.Customer, _ecrOptions.Project, _ecrOptions.Ecrno, string.Format("{0}.txt", _ecrOptions.Ecrno)));
        }

        /// <summary>
        /// Prepares temp tables for codegeneration
        /// </summary>
        /// <returns></returns>
        public bool PrepareHashTables()
        {
            try
            {
                _logger.WriteLogToFile("PrepareHashTables", "Preparing Hashtables necessary for code generation...");
                if (_dbManager.Open())
                {
                    _dbManager.BeginTransaction();
                    CreateHashTables();
                    PopulateHashTables();
                    CreateIndexes();
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.WriteLogToFile("PrepareHashTable", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message, bError: true);
                _logger.WriteLogToFile("PrepareHashTable", string.Format("ERROR While preparing hashtables.Rollback Transaction..."), bError: true);
                throw ex;
            }
        }

        /// <summary>
        /// Create necessary temp tables for codegeneration
        /// </summary>
        private void CreateHashTables()
        {
            try
            {
                _logger.WriteLogToFile("CreateTables", string.Format("Creating Hashtables..."));

                StringBuilder sb = new StringBuilder();
                sb.AppendLine("create table #fw_req_activity (activitydesc varchar(60) collate SQL_Latin1_General_CP1_CI_AS,activityid int,activityname varchar(60) collate SQL_Latin1_General_CP1_CI_AS,activityposition int, activitysequence int,activitytype int, iswfenabled int,ComponentName varchar(60) collate SQL_Latin1_General_CP1_CI_AS, UpdUser varchar(60) collate SQL_Latin1_General_CP1_CI_AS, UpdTime datetime)");
                sb.AppendLine("create table #fw_req_activity_ilbo (activityid int,ilbocode varchar(60) collate SQL_Latin1_General_CP1_CI_AS,activityname varchar(60) collate SQL_Latin1_General_CP1_CI_AS, UpdUser varchar(60) collate SQL_Latin1_General_CP1_CI_AS, UpdTime datetime) ");
                sb.AppendLine("create table #fw_req_ilbo (aspofilepath varchar(255) collate SQL_Latin1_General_CP1_CI_AS,description varchar(255) collate SQL_Latin1_General_CP1_CI_AS, ilbocode varchar(60) collate SQL_Latin1_General_CP1_CI_AS,ilbotype int,progid varchar(60) collate SQL_Latin1_General_CP1_CI_AS, statusflag int, UpdUser varchar(60) collate SQL_Latin1_General_CP1_CI_AS, UpdTime datetime, HasLegacyState varchar(2),HasRTState varchar(2), HasTree varchar(2), HasChart varchar(2), HasRichControl varchar(2), HasPivot varchar(2), HasContextDataItem varchar(2),HasDataSavingTask varchar(2) ,HasDataDrivenTask varchar(2), HasControlExtension varchar(2),hasmessagelookup varchar(2), HasMessageLookupPub varchar(2), HasDynamicLink varchar(2), HasBaseCallout varchar(2), HasTaskCallout varchar(2),HasPreTaskCallout varchar(2),HasPostTaskCallout varchar(2), HaseZeeView varchar(2), HasDynamicILBOTitle varchar(2),HasIlboPublished varchar(2),hasqlik varchar(2),sensitive varchar(1),HasUniversalPersonalization varchar(2))");
                sb.AppendLine("create table #fw_req_activity_task (ActivityId varchar(60) collate SQL_Latin1_General_CP1_CI_AS, TaskName varchar(60) collate SQL_Latin1_General_CP1_CI_AS, InvocationType tinyint, TaskSequence int, UpdUser varchar(60) collate SQL_Latin1_General_CP1_CI_AS, UpdTime datetime)");
                sb.AppendLine("create table #fw_req_ilbo_local_info (ilbocode varchar(60) collate SQL_Latin1_General_CP1_CI_AS, Langid tinyint, Description varchar(255) collate SQL_Latin1_General_CP1_CI_AS, HelpIndex int, UpdUser varchar(60) collate SQL_Latin1_General_CP1_CI_AS, UpdTime datetime)");
                sb.AppendLine("create table #fw_req_ilbo_tabs (ILBOCode varchar(60) collate SQL_Latin1_General_CP1_CI_AS, TabName varchar(60) collate SQL_Latin1_General_CP1_CI_AS, BTSynonym varchar(60) collate SQL_Latin1_General_CP1_CI_AS, UpdUser varchar(60) collate SQL_Latin1_General_CP1_CI_AS, UpdTime datetime)");
                sb.AppendLine("create table #fw_req_lang_bterm_synonym (BTSynonym varchar(60) collate SQL_Latin1_General_CP1_CI_AS, Langid tinyint, ForeignName varchar(120) collate SQL_Latin1_General_CP1_CI_AS, LongPLText nvarchar(240) collate SQL_Latin1_General_CP1_CI_AS, ShortPLText nvarchar(200) collate SQL_Latin1_General_CP1_CI_AS, ShortDesc nvarchar(510) collate SQL_Latin1_General_CP1_CI_AS, LongDesc varchar(510) collate SQL_Latin1_General_CP1_CI_AS, UpdUser varchar(60) collate SQL_Latin1_General_CP1_CI_AS, UpdTime datetime)");
                sb.AppendLine("create table #fw_req_precision (PrecisionType varchar(60) collate SQL_Latin1_General_CP1_CI_AS, TotalLength int, DecimalLength int, UpdUser varchar(60) collate SQL_Latin1_General_CP1_CI_AS, UpdTime datetime)");
                sb.AppendLine("create table #fw_req_ilbo_control_property  (controlid varchar(60) collate SQL_Latin1_General_CP1_CI_AS,ilbocode varchar(255) collate SQL_Latin1_General_CP1_CI_AS, propertyname varchar(60) collate SQL_Latin1_General_CP1_CI_AS,type varchar(60) collate SQL_Latin1_General_CP1_CI_AS, value varchar(255) collate SQL_Latin1_General_CP1_CI_AS,viewname varchar(60) collate SQL_Latin1_General_CP1_CI_AS , UpdUser varchar(60) collate SQL_Latin1_General_CP1_CI_AS, UpdTime datetime)");
                sb.AppendLine("create table #fw_req_ilbo_control (controlid varchar(60) collate SQL_Latin1_General_CP1_CI_AS, ilbocode varchar(255) collate SQL_Latin1_General_CP1_CI_AS,tabname varchar(60) collate SQL_Latin1_General_CP1_CI_AS,type varchar(60) collate SQL_Latin1_General_CP1_CI_AS, listedit varchar(5) collate SQL_Latin1_General_CP1_CI_AS, UpdUser varchar(60) collate SQL_Latin1_General_CP1_CI_AS, UpdTime datetime)");
                sb.AppendLine("create table #fw_req_ilbo_layout_control (ilbocode varchar(255) collate SQL_Latin1_General_CP1_CI_AS, tabname varchar(60) collate SQL_Latin1_General_CP1_CI_AS, controlid varchar(60) collate SQL_Latin1_General_CP1_CI_AS, type varchar(60) collate SQL_Latin1_General_CP1_CI_AS)");
                sb.AppendLine("create table #fw_req_ilbo_view (btsynonym varchar(60) collate SQL_Latin1_General_CP1_CI_AS,controlid  varchar(60) collate SQL_Latin1_General_CP1_CI_AS,displayflag varchar(5) collate SQL_Latin1_General_CP1_CI_AS, displaylength int,ilbocode varchar(200) collate SQL_Latin1_General_CP1_CI_AS, viewname varchar(60) collate SQL_Latin1_General_CP1_CI_AS , UpdUser varchar(60) collate SQL_Latin1_General_CP1_CI_AS, UpdTime datetime,  le_pop varchar(5) collate SQL_Latin1_General_CP1_CI_AS, seq_no int, isItkCtrl char(1) collate SQL_Latin1_General_CP1_CI_AS)");
                sb.AppendLine("create table #fw_req_ilbo_task_control (btsynonym varchar(60) collate SQL_Latin1_General_CP1_CI_AS,activityid varchar(60)  collate SQL_Latin1_General_CP1_CI_AS,ilbocode varchar(60)  collate SQL_Latin1_General_CP1_CI_AS,");
                sb.AppendLine("taskname  varchar(100) collate SQL_Latin1_General_CP1_CI_AS,tasktype  varchar(20) collate SQL_Latin1_General_CP1_CI_AS )");
                sb.AppendLine("create table #fw_req_bterm(btdesc varchar(250) collate SQL_Latin1_General_CP1_CI_AS,btname varchar(60) collate SQL_Latin1_General_CP1_CI_AS, datatype varchar(20) collate SQL_Latin1_General_CP1_CI_AS,isbterm    int,length int, maxvalue  varchar(250) collate SQL_Latin1_General_CP1_CI_AS,minvalue   varchar(250) collate SQL_Latin1_General_CP1_CI_AS,precisiontype varchar(20) collate SQL_Latin1_General_CP1_CI_AS , UpdUser varchar(60) collate SQL_Latin1_General_CP1_CI_AS, UpdTime datetime)");
                sb.AppendLine("create table #fw_req_bterm_synonym(btname varchar(60) collate SQL_Latin1_General_CP1_CI_AS,btsynonym varchar(60) collate SQL_Latin1_General_CP1_CI_AS , UpdUser varchar(60) collate SQL_Latin1_General_CP1_CI_AS, UpdTime datetime)");
                sb.AppendLine("create table #fw_req_process_component(componentdesc varchar(255) collate SQL_Latin1_General_CP1_CI_AS,parentprocess varchar(255) collate SQL_Latin1_General_CP1_CI_AS, componentname varchar(60) collate SQL_Latin1_General_CP1_CI_AS, sequenceno int , UpdUser varchar(60) collate SQL_Latin1_General_CP1_CI_AS, UpdTime datetime)");
                sb.AppendLine("create table #fw_req_ilbo_tab_properties(ilbocode varchar(200) collate SQL_Latin1_General_CP1_CI_AS, propertyname  varchar(64) collate SQL_Latin1_General_CP1_CI_AS,tabname varchar(60) collate SQL_Latin1_General_CP1_CI_AS,value  varchar(250) collate SQL_Latin1_General_CP1_CI_AS , UpdUser varchar(60) collate SQL_Latin1_General_CP1_CI_AS, UpdTime datetime) ");
                sb.AppendLine("create table #fw_req_activity_ilbo_task(activityid int, datasavingtask varchar(100) collate SQL_Latin1_General_CP1_CI_AS, ilbocode varchar(200) collate SQL_Latin1_General_CP1_CI_AS, linktype  int, taskname varchar(100) collate SQL_Latin1_General_CP1_CI_AS, activityname varchar(60) collate SQL_Latin1_General_CP1_CI_AS, Taskconfirmation   int , usageid varchar(60) collate SQL_Latin1_General_CP1_CI_AS,ddt_control_id varchar(60) collate SQL_Latin1_General_CP1_CI_AS,ddt_view_name varchar(60) collate SQL_Latin1_General_CP1_CI_AS, UpdUser varchar(60) collate SQL_Latin1_General_CP1_CI_AS, UpdTime datetime, Tasktype varchar(60) collate SQL_Latin1_General_CP1_CI_AS) ");
                sb.AppendLine("create table #fw_req_task(taskdesc varchar(260)  collate SQL_Latin1_General_CP1_CI_AS,taskname  varchar(100) collate SQL_Latin1_General_CP1_CI_AS,tasktype  varchar(20) collate SQL_Latin1_General_CP1_CI_AS , UpdUser varchar(60) collate SQL_Latin1_General_CP1_CI_AS, UpdTime datetime)");
                sb.AppendLine("create table #fw_req_bterm_enumerated_option(btname   varchar(60) collate SQL_Latin1_General_CP1_CI_AS,langid int, optioncode  varchar(30) collate SQL_Latin1_General_CP1_CI_AS, optiondesc  nvarchar(160) collate SQL_Latin1_General_CP1_CI_AS, sequenceno int, UpdUser varchar(60) collate SQL_Latin1_General_CP1_CI_AS, UpdTime datetime)");
                sb.AppendLine("create table #fw_req_ilbo_link_publish(activityid int,description varchar(512) collate SQL_Latin1_General_CP1_CI_AS, ilbocode varchar(200) collate SQL_Latin1_General_CP1_CI_AS, linkid int, taskname varchar(100) collate SQL_Latin1_General_CP1_CI_AS , UpdUser varchar(60) collate SQL_Latin1_General_CP1_CI_AS, UpdTime datetime)");
                sb.AppendLine("create table #fw_req_ilbo_data_publish(controlid varchar(60) collate SQL_Latin1_General_CP1_CI_AS, controlvariablename varchar(60) collate SQL_Latin1_General_CP1_CI_AS, dataitemname varchar(60) collate SQL_Latin1_General_CP1_CI_AS, flowtype int, havemultiple int, ilbocode  varchar(200) collate SQL_Latin1_General_CP1_CI_AS,iscontrol int, linkid  int, mandatoryflag  int, viewname   varchar(60) collate SQL_Latin1_General_CP1_CI_AS , UpdUser varchar(60) collate SQL_Latin1_General_CP1_CI_AS, UpdTime datetime)");
                sb.AppendLine("create table #fw_req_ilbo_data_use(childilbocode varchar(200) collate SQL_Latin1_General_CP1_CI_AS, controlid  varchar(60) collate SQL_Latin1_General_CP1_CI_AS, controlvariablename varchar(60) collate SQL_Latin1_General_CP1_CI_AS, dataitemname varchar(60) collate SQL_Latin1_General_CP1_CI_AS, flowtype int, iscontrol int, linkid  int, parentilbocode  varchar(200) collate SQL_Latin1_General_CP1_CI_AS, primarydata  varchar(5) collate SQL_Latin1_General_CP1_CI_AS, retrievemultiple int, taskname varchar(100) collate SQL_Latin1_General_CP1_CI_AS, viewname varchar(60) collate SQL_Latin1_General_CP1_CI_AS, UpdUser varchar(60) collate SQL_Latin1_General_CP1_CI_AS, UpdTime datetime)");
                sb.AppendLine("create table #fw_req_ilbo_linkuse (childilbocode  varchar(200) collate SQL_Latin1_General_CP1_CI_AS, childorder int, linkid int, parentilbocode  varchar(200) collate SQL_Latin1_General_CP1_CI_AS, taskname  varchar(100) collate SQL_Latin1_General_CP1_CI_AS, posttask varchar(255) collate SQL_Latin1_General_CP1_CI_AS, post_linktask varchar(255) collate SQL_Latin1_General_CP1_CI_AS, UpdUser varchar(60) collate SQL_Latin1_General_CP1_CI_AS, UpdTime datetime)");
                sb.AppendLine("create table #fw_des_ilbo_services (ilbocode  varchar(200) collate SQL_Latin1_General_CP1_CI_AS, isprepopulate tinyint, servicename    varchar(64) collate SQL_Latin1_General_CP1_CI_AS , UpdUser varchar(60) collate SQL_Latin1_General_CP1_CI_AS, UpdTime datetime, IsUniversalPersonalization  varchar(5) collate SQL_Latin1_General_CP1_CI_AS) "); //Universal Personalizzation
                sb.AppendLine("create table #fw_asp_codegen_tmp(ilbocode varchar(60) collate SQL_Latin1_General_CP1_CI_AS, IdentityId    varchar(120) collate SQL_Latin1_General_CP1_CI_AS,   SerialNo int, Position varchar(120) collate SQL_Latin1_General_CP1_CI_AS, ControlID  varchar(64) collate SQL_Latin1_General_CP1_CI_AS, Type varchar(120) collate SQL_Latin1_General_CP1_CI_AS, LineNum    int, AttributeName varchar(120) collate SQL_Latin1_General_CP1_CI_AS, InitVal varchar(120) collate SQL_Latin1_General_CP1_CI_AS, Script varchar(4000) collate SQL_Latin1_General_CP1_CI_AS )");
                sb.AppendLine("create table #fw_req_system_parameters (paramname varchar(30) collate SQL_Latin1_General_CP1_CI_AS, paramvalue varchar(80) collate SQL_Latin1_General_CP1_CI_AS,  timestamp int, createdby varchar(60) collate SQL_Latin1_General_CP1_CI_AS, createddate datetime, modifiedby varchar(60) collate SQL_Latin1_General_CP1_CI_AS, modifieddate datetime, paramdesc varchar(255) collate SQL_Latin1_General_CP1_CI_AS)");
                sb.AppendLine("create table #fw_des_service_dataitem(dataitemname varchar(60) collate SQL_Latin1_General_CP1_CI_AS, defaultvalue varchar(60) collate SQL_Latin1_General_CP1_CI_AS, flowattribute int,ispartofkey int,mandatoryflag int, segmentname varchar(60) collate SQL_Latin1_General_CP1_CI_AS, servicename varchar(60) collate SQL_Latin1_General_CP1_CI_AS, itk_dataitem varchar(2) collate SQL_Latin1_General_CP1_CI_AS, UpdUser varchar(60) collate SQL_Latin1_General_CP1_CI_AS,  UpdTime datetime, controlid varchar(40)collate SQL_Latin1_General_CP1_CI_AS )");
                sb.AppendLine("create table #fw_des_be_placeholder(errorid int  ,methodid int  ,parametername varchar(60) collate SQL_Latin1_General_CP1_CI_AS ,placeholdername varchar(60) collate SQL_Latin1_General_CP1_CI_AS ) ");
                sb.AppendLine("create table #fw_des_br_logical_parameter ( methodid int  ,logicalparametername varchar(60) collate SQL_Latin1_General_CP1_CI_AS ,logicalparamseqno int,recordsetname varchar(60) collate SQL_Latin1_General_CP1_CI_AS ,rssequenceno smallint,flowdirection smallint, btname varchar(60) collate SQL_Latin1_General_CP1_CI_AS ,spparametertype varchar(20) collate SQL_Latin1_General_CP1_CI_AS, controlid varchar(40)collate SQL_Latin1_General_CP1_CI_AS )");//smallint to int
                sb.AppendLine("create table #fw_des_brerror (errorid int  ,methodid int  ,sperrorcode varchar(12) collate SQL_Latin1_General_CP1_CI_AS)");
                sb.AppendLine("create table #fw_des_bro (broname varchar(100) collate SQL_Latin1_General_CP1_CI_AS  , brodescription varchar(256) collate SQL_Latin1_General_CP1_CI_AS , componentname varchar(60) collate SQL_Latin1_General_CP1_CI_AS  ,broscope tinyint, brotype tinyint, clsid varchar(40) collate SQL_Latin1_General_CP1_CI_AS ,clsname varchar(40) collate SQL_Latin1_General_CP1_CI_AS ,dllname varchar(510) collate SQL_Latin1_General_CP1_CI_AS,progid varchar(510) collate SQL_Latin1_General_CP1_CI_AS ,systemgenerated tinyint)");
                sb.AppendLine("create table #fw_des_businessrule(accessesdatabase tinyint,bocode varchar(100) collate SQL_Latin1_General_CP1_CI_AS , broname varchar(100) collate SQL_Latin1_General_CP1_CI_AS , dispid varchar(510) collate SQL_Latin1_General_CP1_CI_AS ,isintegbr tinyint, methodid int  , methodname varchar(510) collate SQL_Latin1_General_CP1_CI_AS , operationtype tinyint,statusflag tinyint,systemgenerated tinyint,method_exec_cont varchar(5)) ");//TECH-XXXX
                sb.AppendLine("create table #fw_des_context(correctiveaction varchar(4000) collate SQL_Latin1_General_CP1_CI_AS,errorcontext varchar(510) collate SQL_Latin1_General_CP1_CI_AS ,errorid int  ,severityid tinyint)");
                sb.AppendLine("create table #fw_des_di_parameter(dataitemname varchar(60) collate SQL_Latin1_General_CP1_CI_AS ,methodid int  , parametername varchar(60) collate SQL_Latin1_General_CP1_CI_AS , sectionname varchar(60) collate SQL_Latin1_General_CP1_CI_AS , segmentname varchar(60) collate SQL_Latin1_General_CP1_CI_AS , sequenceno int ,servicename varchar(64) collate SQL_Latin1_General_CP1_CI_AS, old_seq_no int, old_seq_no_ins int, le_pop varchar(5) collate SQL_Latin1_General_CP1_CI_AS ) ");
                sb.AppendLine("create table #fw_des_di_placeholder(dataitemname varchar(60) collate SQL_Latin1_General_CP1_CI_AS ,errorid  int  , methodid int  , placeholdername varchar(60) collate SQL_Latin1_General_CP1_CI_AS , sectionname varchar(60) collate SQL_Latin1_General_CP1_CI_AS , segmentname varchar(60) collate SQL_Latin1_General_CP1_CI_AS , sequenceno int ,servicename varchar(64)  collate SQL_Latin1_General_CP1_CI_AS, old_seq_no int, old_seq_no_ins int) ");
                sb.AppendLine("create table #fw_des_error (componentname varchar(60) collate SQL_Latin1_General_CP1_CI_AS  ,defaultcorrectiveaction varchar(4000) collate SQL_Latin1_General_CP1_CI_AS,defaultseverity tinyint, detaileddesc varchar(2000) collate SQL_Latin1_General_CP1_CI_AS , displaytype varchar(30) collate SQL_Latin1_General_CP1_CI_AS,errorid  int  , errormessage nvarchar(1020) collate SQL_Latin1_General_CP1_CI_AS , errorsource tinyint,reqerror varchar(40) collate SQL_Latin1_General_CP1_CI_AS)");
                sb.AppendLine("create table #fw_des_integ_serv_map (callingdataitem varchar(60) collate SQL_Latin1_General_CP1_CI_AS , callingsegment varchar(60) collate SQL_Latin1_General_CP1_CI_AS , callingservicename varchar(64) collate SQL_Latin1_General_CP1_CI_AS  , integdataitem varchar(60) collate SQL_Latin1_General_CP1_CI_AS , integsegment varchar(60) collate SQL_Latin1_General_CP1_CI_AS  , integservicename varchar(64) collate SQL_Latin1_General_CP1_CI_AS  , sectionname varchar(60) collate SQL_Latin1_General_CP1_CI_AS  , sequenceno int, old_seq_no int, old_seq_no_ins int)");
                sb.AppendLine("create table #fw_des_processsection( controlexpression varchar(256) collate SQL_Latin1_General_CP1_CI_AS,processingtype tinyint,sectionname varchar(60) collate SQL_Latin1_General_CP1_CI_AS , sectiontype tinyint , sequenceno int , servicename varchar(64) collate SQL_Latin1_General_CP1_CI_AS, old_seq_no int, old_seq_no_ins int, IsUniversalPersonalization varchar(5) collate SQL_Latin1_General_CP1_CI_AS)"); //Universal Personalization
                //sb.AppendLine("create table #fw_des_processsection_br_is (connectivityflag tinyint, controlexpression varchar(256) collate SQL_Latin1_General_CP1_CI_AS,executionflag tinyint, integservicename varchar(64) collate SQL_Latin1_General_CP1_CI_AS , isbr tinyint, methodid int  , sectionname varchar(60) collate SQL_Latin1_General_CP1_CI_AS , sequenceno int , servicename varchar(64) collate SQL_Latin1_General_CP1_CI_AS, old_seq_no int, old_seq_no_ins int, Method_Ext char(2), methodid_ref int, methodname_ref varchar(64) collate SQL_Latin1_General_CP1_CI_AS, sequenceno_ref int, sectionname_ref varchar(64) collate SQL_Latin1_General_CP1_CI_AS, ps_sequenceno_ref int,loopcausingsegment varchar(60) collate SQL_Latin1_General_CP1_CI_AS,SpecID int,SpecName varchar(300),SpecVersion int,Path varchar(500),OperationID varchar(100),OperationVerb varchar(10) )");
                sb.AppendLine("create table #fw_des_processsection_br_is (connectivityflag tinyint, controlexpression varchar(256) collate SQL_Latin1_General_CP1_CI_AS,executionflag tinyint, integservicename varchar(64) collate SQL_Latin1_General_CP1_CI_AS , isbr tinyint, methodid int  , sectionname varchar(60) collate SQL_Latin1_General_CP1_CI_AS , sequenceno int , servicename varchar(64) collate SQL_Latin1_General_CP1_CI_AS, old_seq_no int, old_seq_no_ins int, Method_Ext char(2), methodid_ref int, methodname_ref varchar(64) collate SQL_Latin1_General_CP1_CI_AS, sequenceno_ref int, sectionname_ref varchar(64) collate SQL_Latin1_General_CP1_CI_AS, ps_sequenceno_ref int,loopcausingsegment varchar(60) collate SQL_Latin1_General_CP1_CI_AS,SpecID int,SpecName varchar(300),SpecVersion int,Path varchar(500),OperationID varchar(100),OperationVerb varchar(10), IsUniversalPersonlizedSection varchar(5) collate SQL_Latin1_General_CP1_CI_AS )"); //Universal personlization 
                sb.AppendLine("create table #fw_des_processsection_br_is_err (connectivityflag tinyint, controlexpression varchar(256) collate SQL_Latin1_General_CP1_CI_AS,executionflag tinyint, integservicename varchar(64) collate SQL_Latin1_General_CP1_CI_AS , isbr tinyint, methodid int, sectionname varchar(60) collate SQL_Latin1_General_CP1_CI_AS not null, sequenceno int not null, servicename varchar(64) collate SQL_Latin1_General_CP1_CI_AS not null)");
                sb.AppendLine("create table #fw_des_sp (methodid int  ,sperrorprotocol tinyint, spname varchar(60) collate SQL_Latin1_General_CP1_CI_AS)");
                sb.AppendLine("create table #fw_des_bo ( BOCode varchar(64) collate SQL_Latin1_General_CP1_CI_AS  , ComponentName varchar(64) collate SQL_Latin1_General_CP1_CI_AS , BODesc varchar(255) collate SQL_Latin1_General_CP1_CI_AS , StatusFlag int ) ");
                sb.AppendLine("create table #fw_des_svco( SVCOName varchar(60) collate SQL_Latin1_General_CP1_CI_AS , SVCODescription varchar(255) collate SQL_Latin1_General_CP1_CI_AS , ComponentName varchar(60) collate SQL_Latin1_General_CP1_CI_AS , SVCOScope int ,SVCOType int ,DLLName varchar(60) collate SQL_Latin1_General_CP1_CI_AS ,ProgID varchar(255) collate SQL_Latin1_General_CP1_CI_AS ) ");
                sb.AppendLine("create table #fw_des_error_placeholder (ErrorID int, PlaceholderName varchar(60) collate SQL_Latin1_General_CP1_CI_AS)");
                sb.AppendLine("create table #fw_des_reqbr_desbr ( ReqBRName varchar(60) collate SQL_Latin1_General_CP1_CI_AS, MethodID int )");
                sb.AppendLine("create table #fw_req_task_rule (TaskName varchar(60) collate SQL_Latin1_General_CP1_CI_AS, BRSequence int, BRName varchar(60) collate SQL_Latin1_General_CP1_CI_AS, InvocationType tinyint) ");
                sb.AppendLine("create table #fw_des_ilbo_placeholder (ilbocode varchar(60) collate SQL_Latin1_General_CP1_CI_AS, ControlID varchar(64) collate SQL_Latin1_General_CP1_CI_AS, EventName varchar(60) collate SQL_Latin1_General_CP1_CI_AS, PlaceholderName varchar(60) collate SQL_Latin1_General_CP1_CI_AS, IsControl tinyint, ControlName varchar(60) collate SQL_Latin1_General_CP1_CI_AS, ViewName varchar(60) collate SQL_Latin1_General_CP1_CI_AS, VariableName varchar(60) collate SQL_Latin1_General_CP1_CI_AS, ErrorID varchar(40) collate SQL_Latin1_General_CP1_CI_AS, CtrlEvent_ViewName varchar(60) collate SQL_Latin1_General_CP1_CI_AS) ");
                sb.AppendLine("create table #fw_des_ilbo_ctrl_event  (ILBOCode varchar(60) collate SQL_Latin1_General_CP1_CI_AS, ControlID varchar(60) collate SQL_Latin1_General_CP1_CI_AS, EventName varchar(60) collate SQL_Latin1_General_CP1_CI_AS,  TaskName varchar(60) collate SQL_Latin1_General_CP1_CI_AS, MethodName varchar(60) collate SQL_Latin1_General_CP1_CI_AS, CreateStubFlag tinyint, ViewName varchar(60) collate SQL_Latin1_General_CP1_CI_AS)");
                sb.AppendLine("create table #fw_req_businessrule (BRName varchar(60) collate SQL_Latin1_General_CP1_CI_AS, BRType varchar(20) collate SQL_Latin1_General_CP1_CI_AS, BRDesc varchar(510) collate SQL_Latin1_General_CP1_CI_AS) ");
                sb.AppendLine("create table #fw_req_br_error ( BRName varchar(60) collate SQL_Latin1_General_CP1_CI_AS, ErrorCode varchar(60) collate SQL_Latin1_General_CP1_CI_AS)");
                sb.AppendLine("CREATE TABLE #fw_req_ILBO_Transpose ( ComponentName varchar(60) collate SQL_Latin1_General_CP1_CI_AS NOT NULL, ActivityName varchar(60) collate SQL_Latin1_General_CP1_CI_AS NOT NULL, [ILBOCode] varchar(200) collate SQL_Latin1_General_CP1_CI_AS NOT NULL, [ControlID] varchar(64) collate SQL_Latin1_General_CP1_CI_AS NOT NULL, [ZoomName] varchar(13) collate SQL_Latin1_General_CP1_CI_AS NOT NULL, [Version] varchar(7) collate SQL_Latin1_General_CP1_CI_AS NOT NULL ,UpdUser varchar(60) collate SQL_Latin1_General_CP1_CI_AS, UpdTime datetime)");
                sb.AppendLine("create table #fw_req_ilbo_task_rpt ( ilbocode varchar(60) collate SQL_Latin1_General_CP1_CI_AS,taskname varchar(60) collate SQL_Latin1_General_CP1_CI_AS,PageName varchar(60) collate SQL_Latin1_General_CP1_CI_AS,ProcessingType varchar(60) collate SQL_Latin1_General_CP1_CI_AS,ContextName varchar(4000) collate SQL_Latin1_General_CP1_CI_AS, ReportType varchar(60) collate SQL_Latin1_General_CP1_CI_AS)");
                sb.AppendLine("Create table #fw_des_chart_service_segment (component_name varchar(60) collate SQL_Latin1_General_CP1_CI_AS, chart_id varchar(60) collate SQL_Latin1_General_CP1_CI_AS, chart_section varchar(60) collate SQL_Latin1_General_CP1_CI_AS, servicename varchar(60) collate SQL_Latin1_General_CP1_CI_AS, segmentname varchar(60) collate SQL_Latin1_General_CP1_CI_AS, instanceflag int) ");
                sb.AppendLine("Create table #fw_task_service_map (activity_name varchar(60) collate SQL_Latin1_General_CP1_CI_AS ,service_name varchar(60) collate SQL_Latin1_General_CP1_CI_AS, task_name varchar(60) collate SQL_Latin1_General_CP1_CI_AS,ui_name varchar(60) collate SQL_Latin1_General_CP1_CI_AS )");
                sb.AppendLine("create table #fw_des_service ( isintegser tinyint, processingtype tinyint, servicename varchar(60) collate SQL_Latin1_General_CP1_CI_AS, servicetype int,statusflag int, svconame varchar(60) collate SQL_Latin1_General_CP1_CI_AS, componentname varchar(60) collate SQL_Latin1_General_CP1_CI_AS, isCached varchar(5) collate SQL_Latin1_General_CP1_CI_AS, isZipped varchar(5) collate SQL_Latin1_General_CP1_CI_AS, SetKey_Pattern varchar(255) collate SQL_Latin1_General_CP1_CI_AS, ClearKey_Pattern varchar(255) collate SQL_Latin1_General_CP1_CI_AS, UpdUser varchar(60) collate SQL_Latin1_General_CP1_CI_AS, UpdTime datetime, HasUniversalPersonalization varchar(5) collate SQL_Latin1_General_CP1_CI_AS)"); //Universal Personalization
                sb.AppendLine("create table #fw_des_service_segment (bocode varchar(60) collate SQL_Latin1_General_CP1_CI_AS, bosegmentname varchar(60) collate SQL_Latin1_General_CP1_CI_AS, instanceflag int, mandatoryflag int, parentsegmentname varchar(60) collate SQL_Latin1_General_CP1_CI_AS, segmentname varchar(60) collate SQL_Latin1_General_CP1_CI_AS, servicename varchar(60) collate SQL_Latin1_General_CP1_CI_AS, SegmentSequence int, UpdUser varchar(60) collate SQL_Latin1_General_CP1_CI_AS, UpdTime datetime, process_selrows varchar(2) collate SQL_Latin1_General_CP1_CI_AS, process_updrows varchar(2) collate SQL_Latin1_General_CP1_CI_AS,process_selupdrows varchar(2) collate SQL_Latin1_General_CP1_CI_AS,flowattribute int)");
                sb.AppendLine("create table #fw_des_ilbo_service_view_datamap (activityid    int, controlid  varchar(60) collate SQL_Latin1_General_CP1_CI_AS, dataitemname   varchar(60) collate SQL_Latin1_General_CP1_CI_AS, ilbocode  varchar(200) collate SQL_Latin1_General_CP1_CI_AS, iscontrol tinyint, segmentname varchar(60) collate SQL_Latin1_General_CP1_CI_AS, servicename varchar(64) collate SQL_Latin1_General_CP1_CI_AS, taskname varchar(100) collate SQL_Latin1_General_CP1_CI_AS, variablename varchar(60) collate SQL_Latin1_General_CP1_CI_AS, viewname varchar(60) collate SQL_Latin1_General_CP1_CI_AS, page_bt_synonym varchar(60) collate SQL_Latin1_General_CP1_CI_AS, UpdUser varchar(60) collate SQL_Latin1_General_CP1_CI_AS, UpdTime datetime)");
                sb.AppendLine("create table #fw_des_task_segment_attribs (activityid int, ilbocode varchar(200) collate SQL_Latin1_General_CP1_CI_AS, taskname  varchar(100) collate SQL_Latin1_General_CP1_CI_AS,servicename  varchar(64) collate SQL_Latin1_General_CP1_CI_AS, segmentname  varchar(60) collate SQL_Latin1_General_CP1_CI_AS,combofill  int, displayflag varchar(5) collate SQL_Latin1_General_CP1_CI_AS, UpdUser varchar(60) collate SQL_Latin1_General_CP1_CI_AS, UpdTime datetime) ");
                sb.AppendLine("create table #fw_req_task_local_info (langid tinyint, taskname  varchar(100) collate SQL_Latin1_General_CP1_CI_AS, description  varchar(512) collate SQL_Latin1_General_CP1_CI_AS, helpindex int, UpdUser varchar(60) collate SQL_Latin1_General_CP1_CI_AS, UpdTime datetime) ");
                sb.AppendLine("create table #fw_req_ilbo_link_local_info (LinkID int, ilbocode varchar(60) collate SQL_Latin1_General_CP1_CI_AS, Langid tinyint, Description varchar(255) collate SQL_Latin1_General_CP1_CI_AS, HelpIndex int, UpdUser varchar(60) collate SQL_Latin1_General_CP1_CI_AS, UpdTime datetime) ");
                sb.AppendLine("create table #fw_req_activity_ilbo_task_extension_map (componentname  varchar(60) collate SQL_Latin1_General_CP1_CI_AS, activityid int, ilbocode varchar(60) collate SQL_Latin1_General_CP1_CI_AS, taskname varchar(60) collate SQL_Latin1_General_CP1_CI_AS, resultantaspname varchar(512) collate SQL_Latin1_General_CP1_CI_AS, sessionvariable varchar(60) collate SQL_Latin1_General_CP1_CI_AS , UpdUser varchar(60) collate SQL_Latin1_General_CP1_CI_AS, UpdTime datetime) ");
                sb.AppendLine("create table #LP_RVWObjects_tmp(ObjectType NVARCHAR(60) collate SQL_Latin1_General_CP1_CI_AS, ObjectCode NVARCHAR(200) collate SQL_Latin1_General_CP1_CI_AS )");
                sb.AppendLine("create table #LP_ILBO_Tmp(ILBOCode NVARCHAR(300) collate SQL_Latin1_General_CP1_CI_AS,processFlag  NVARCHAR(300) collate SQL_Latin1_General_CP1_CI_AS, publishflag  NVARCHAR(300) collate SQL_Latin1_General_CP1_CI_AS)");
                sb.AppendLine("create table #LP_Service_Tmp( ServiceName NVARCHAR(100) collate SQL_Latin1_General_CP1_CI_AS, ServiceLevel INT, ServiceFlag INT)");
                sb.AppendLine("create table #service_tmp (ServiceName  NVARCHAR(100) collate SQL_Latin1_General_CP1_CI_AS, processflag NVARCHAR(100) collate SQL_Latin1_General_CP1_CI_AS, publishflag NVARCHAR(100) collate SQL_Latin1_General_CP1_CI_AS)");
                sb.AppendLine("create table #LP_PublishErr_Tmp( ObjectType NVARCHAR(100) collate SQL_Latin1_General_CP1_CI_AS,  ObjectCode NVARCHAR(200) collate SQL_Latin1_General_CP1_CI_AS, ErrorID INT)");
                sb.AppendLine("create table #fw_des_focus_control (ErrorID int, ErrorContext varchar(60) collate SQL_Latin1_General_CP1_CI_AS, ControlID varchar(60) collate SQL_Latin1_General_CP1_CI_AS , SegmentName varchar(60) collate SQL_Latin1_General_CP1_CI_AS, FocusDataItem varchar(60) collate SQL_Latin1_General_CP1_CI_AS, UpdUser varchar(60)  collate SQL_Latin1_General_CP1_CI_AS, UpdTime datetime, methodid int, method_name varchar(510) collate SQL_Latin1_General_CP1_CI_AS)");
                sb.AppendLine("create table #meta_severity (SeverityID tinyint, SeverityDesc varchar(255) collate SQL_Latin1_General_CP1_CI_AS, DefButton varchar(100) collate SQL_Latin1_General_CP1_CI_AS, ErrorSource varchar(120) collate SQL_Latin1_General_CP1_CI_AS, ButtonText varchar(100) collate SQL_Latin1_General_CP1_CI_AS) ");
                sb.AppendLine("create table #fw_req_activity_PLBO(ActivityId varchar(100) collate SQL_Latin1_General_CP1_CI_AS,PLBOCode varchar(100) collate SQL_Latin1_General_CP1_CI_AS) ");
                sb.AppendLine("create table #fw_req_plbo (PLBOCode varchar(100) collate SQL_Latin1_General_CP1_CI_AS, Description varchar(510) collate SQL_Latin1_General_CP1_CI_AS, PLBOType tinyint, ProgID varchar(256) collate SQL_Latin1_General_CP1_CI_AS, StatusFlag tinyint) ");
                sb.AppendLine("create table #fw_des_ilbo_focus_control (ErrorID int, ErrorContext varchar(510) collate SQL_Latin1_General_CP1_CI_AS, ControlID varchar(60) collate SQL_Latin1_General_CP1_CI_AS, SegmentName varchar(60) collate SQL_Latin1_General_CP1_CI_AS,  FocusDataItem varchar(60) collate SQL_Latin1_General_CP1_CI_AS) ");
                sb.AppendLine("create table #fw_des_service_view_datamap (PLBOCode varchar(100) collate SQL_Latin1_General_CP1_CI_AS, ServiceName varchar(64) collate SQL_Latin1_General_CP1_CI_AS, TaskName varchar(100) collate SQL_Latin1_General_CP1_CI_AS, SegmentName varchar(60) collate SQL_Latin1_General_CP1_CI_AS, DataItemName varchar(60) collate SQL_Latin1_General_CP1_CI_AS, IsControl tinyint, ControlID varchar(60) collate SQL_Latin1_General_CP1_CI_AS, ViewName varchar(60) collate SQL_Latin1_General_CP1_CI_AS, VariableName varchar(60) collate SQL_Latin1_General_CP1_CI_AS)");
                sb.AppendLine("create table #fw_des_ilerror (ILBOCode varchar(100) collate SQL_Latin1_General_CP1_CI_AS, ControlID varchar(60) collate SQL_Latin1_General_CP1_CI_AS, EventName varchar(100) collate SQL_Latin1_General_CP1_CI_AS, LocalErrorNo int, ErrorID int, ViewName varchar(60) collate SQL_Latin1_General_CP1_CI_AS) ");
                sb.AppendLine("create table #fw_des_err_det_local_info (ErrorID int, ErrorMessage nvarchar(1020) collate SQL_Latin1_General_CP1_CI_AS, DetailedDesc varchar(4000) collate SQL_Latin1_General_CP1_CI_AS, LangId tinyint)");
                sb.AppendLine("create table #fw_des_corr_action_local_info (ErrorID varchar(60) collate SQL_Latin1_General_CP1_CI_AS, ErrorContext varchar(510) collate SQL_Latin1_General_CP1_CI_AS, CorrectiveAction varchar(4000) collate SQL_Latin1_General_CP1_CI_AS, LangId tinyint) ");
                //sb.AppendLine("create table #de_log_ext_ctrl_met(le_customer_name varchar(60) collate sql_latin1_general_cp1_ci_as,le_project_name varchar(60) collate sql_latin1_general_cp1_ci_as,le_control varchar(60) collate sql_latin1_general_cp1_ci_as,le_ctrl_clf  varchar(5) collate sql_latin1_general_cp1_ci_as,le_data_type  varchar(60) collate sql_latin1_general_cp1_ci_as,le_data_length int,le_btname varchar(60) collate sql_latin1_general_cp1_ci_as,le_precisiontype varchar(60) collate sql_latin1_general_cp1_ci_as,le_decimallength int, col_seq int, le_control_type varchar(60) collate sql_latin1_general_cp1_ci_as)");
                sb.AppendLine("create table #es_comp_ctrl_type_mst(customer_name varchar(60) collate sql_latin1_general_cp1_ci_as,project_name varchar(60) collate sql_latin1_general_cp1_ci_as,req_no varchar(60) collate sql_latin1_general_cp1_ci_as,process_name varchar(60) collate sql_latin1_general_cp1_ci_as,component_name varchar(60) collate sql_latin1_general_cp1_ci_as,ctrl_type_name varchar(60) collate sql_latin1_general_cp1_ci_as,ctrl_type_descr varchar(255) collate sql_latin1_general_cp1_ci_as,base_ctrl_type varchar(60) collate sql_latin1_general_cp1_ci_as, mandatory_flag varchar(5) collate sql_latin1_general_cp1_ci_as,visisble_flag varchar(5) collate sql_latin1_general_cp1_ci_as,editable_flag varchar(5) collate sql_latin1_general_cp1_ci_as,caption_req varchar(5) collate sql_latin1_general_cp1_ci_as,select_flag  varchar(5) collate sql_latin1_general_cp1_ci_as,zoom_req varchar(5) collate sql_latin1_general_cp1_ci_as,insert_req varchar(5) collate sql_latin1_general_cp1_ci_as,delete_req varchar(5) collate sql_latin1_general_cp1_ci_as,help_req varchar(5) collate sql_latin1_general_cp1_ci_as, event_handling_req  varchar(5) collate sql_latin1_general_cp1_ci_as,ellipses_req varchar(5) collate sql_latin1_general_cp1_ci_as,comp_ctrl_type_sysid varchar(40) collate sql_latin1_general_cp1_ci_as,timestamp int,createdby varchar(30) collate sql_latin1_general_cp1_ci_as,createddate datetime,modifiedby varchar(30) collate sql_latin1_general_cp1_ci_as,modifieddate datetime,visisble_rows varchar(5) collate sql_latin1_general_cp1_ci_as,ctrl_type_doc varchar(4000) collate sql_latin1_general_cp1_ci_as,caption_alignment varchar(8) collate sql_latin1_general_cp1_ci_as, caption_wrap varchar(5) collate sql_latin1_general_cp1_ci_as,caption_position varchar(8) collate sql_latin1_general_cp1_ci_as,ctrl_position varchar(8) collate sql_latin1_general_cp1_ci_as,label_class varchar(60) collate sql_latin1_general_cp1_ci_as,ctrl_class varchar(60) collate sql_latin1_general_cp1_ci_as,password_char varchar(5) collate sql_latin1_general_cp1_ci_as,tskimg_class varchar(60) collate sql_latin1_general_cp1_ci_as,hlpimg_class varchar(60) collate sql_latin1_general_cp1_ci_as,disponlycmb_req varchar(5) collate sql_latin1_general_cp1_ci_as, html_txt_area varchar(5) collate sql_latin1_general_cp1_ci_as, report_req varchar(5) collate sql_latin1_general_cp1_ci_as,auto_tab_stop varchar(5) collate sql_latin1_general_cp1_ci_as,spin_required varchar(5) collate sql_latin1_general_cp1_ci_as,spin_up_image varchar(60) collate sql_latin1_general_cp1_ci_as,spin_down_image varchar(60) collate sql_latin1_general_cp1_ci_as, Extjs_Ctrl_type varchar(60) collate sql_latin1_general_cp1_ci_as)");
                //sb.AppendLine("create table #de_published_service_logic_extn_dtl(component_name varchar(60)collate sql_latin1_general_cp1_ci_as, servicename varchar(60)collate sql_latin1_general_cp1_ci_as, section_name varchar(60)collate sql_latin1_general_cp1_ci_as, methodid varchar(60)collate sql_latin1_general_cp1_ci_as, methodname varchar(60)collate sql_latin1_general_cp1_ci_as, br_sequence int, rdbms_type varchar(60)collate sql_latin1_general_cp1_ci_as, ext_as varchar(60)collate sql_latin1_general_cp1_ci_as, ext_before varchar(5)collate sql_latin1_general_cp1_ci_as, ext_separate_ps varchar(5)collate sql_latin1_general_cp1_ci_as, ext_object_name varchar(60)collate sql_latin1_general_cp1_ci_as, extend_flag varchar(5)collate sql_latin1_general_cp1_ci_as, status_flag varchar(5)collate sql_latin1_general_cp1_ci_as, methodtype varchar(5)collate sql_latin1_general_cp1_ci_as )");
                sb.AppendLine("Create Table #fw_req_language (customername  varchar(60) collate SQL_Latin1_General_CP1_CI_AS, projectname  varchar(60) collate SQL_Latin1_General_CP1_CI_AS, ecrno  varchar(60) collate SQL_Latin1_General_CP1_CI_AS, langid int, langdesc varchar(225) collate SQL_Latin1_General_CP1_CI_AS)");
                sb.AppendLine("create table #fw_des_plbo_placeholder (PLBOCode varchar(60) collate SQL_Latin1_General_CP1_CI_AS, ControlID varchar(60) collate SQL_Latin1_General_CP1_CI_AS, EventName varchar(60) collate SQL_Latin1_General_CP1_CI_AS, PlaceholderName varchar(60) collate SQL_Latin1_General_CP1_CI_AS, ControlName varchar(60) collate SQL_Latin1_General_CP1_CI_AS, ViewName varchar(60) collate SQL_Latin1_General_CP1_CI_AS, ErrorID int, CtrlEvent_ViewName varchar(60) collate SQL_Latin1_General_CP1_CI_AS) ");
                sb.AppendLine("create table #fw_req_control_property (PLBOCode varchar(60) collate SQL_Latin1_General_CP1_CI_AS, ControlID varchar(60) collate SQL_Latin1_General_CP1_CI_AS, ViewName varchar(60) collate SQL_Latin1_General_CP1_CI_AS, PropertyName varchar(60) collate SQL_Latin1_General_CP1_CI_AS, Type varchar(20) collate SQL_Latin1_General_CP1_CI_AS, Value varchar(510) collate SQL_Latin1_General_CP1_CI_AS) ");
                sb.AppendLine("create table #fw_des_plerror (PLBOCode varchar(60) collate SQL_Latin1_General_CP1_CI_AS, ControlID varchar(60) collate SQL_Latin1_General_CP1_CI_AS, EventName varchar(60) collate SQL_Latin1_General_CP1_CI_AS, LocalErrorNo int, ErrorID int , ViewName varchar(60) collate SQL_Latin1_General_CP1_CI_AS)");
                sb.AppendLine("create table #fw_req_activity_local_info ( activityid int ,langid int ,activitydesc varchar(255) collate SQL_Latin1_General_CP1_CI_AS ,helpindex int ,tooltiptext varchar(255) collate SQL_Latin1_General_CP1_CI_AS)");
                sb.AppendLine("create table #fw_req_ilboctrl_initval (ilbocode varchar(60) collate SQL_Latin1_General_CP1_CI_AS, ControlID varchar(64) collate SQL_Latin1_General_CP1_CI_AS, ViewName varchar(60) collate SQL_Latin1_General_CP1_CI_AS , sequenceNo int, InitialValue varchar(510) collate SQL_Latin1_General_CP1_CI_AS, UpdUser varchar(64) collate SQL_Latin1_General_CP1_CI_AS, UpdTime datetime) ");
                sb.AppendLine("create table #fw_service_validation_message (service_name varchar(60) collate SQL_Latin1_General_CP1_CI_AS, languageid int, segmentname varchar(60) collate SQL_Latin1_General_CP1_CI_AS, dataitemname varchar(60) collate SQL_Latin1_General_CP1_CI_AS, validation_code varchar(60) collate SQL_Latin1_General_CP1_CI_AS, message_code int, message_doc varchar(255) collate SQL_Latin1_General_CP1_CI_AS, value1 varchar(255) collate SQL_Latin1_General_CP1_CI_AS, value2 varchar(255) collate SQL_Latin1_General_CP1_CI_AS, order_of_sequence int)");
                sb.AppendLine("create table #de_published_ui_control(component_name varchar(60) collate SQL_Latin1_General_CP1_CI_AS,activity_name varchar(60) collate SQL_Latin1_General_CP1_CI_AS,control_bt_synonym varchar(60) collate SQL_Latin1_General_CP1_CI_AS,control_doc varchar(400) collate SQL_Latin1_General_CP1_CI_AS,control_id varchar(60) collate SQL_Latin1_General_CP1_CI_AS,control_prefix varchar(6) collate SQL_Latin1_General_CP1_CI_AS,control_type varchar(60) collate SQL_Latin1_General_CP1_CI_AS,data_column_width int,horder int,label_column_width int,order_seq int,page_bt_synonym varchar(60) collate SQL_Latin1_General_CP1_CI_AS,proto_tooltip varchar(255) collate SQL_Latin1_General_CP1_CI_AS,sample_data varchar(4000) collate SQL_Latin1_General_CP1_CI_AS,section_bt_synonym varchar(60) collate SQL_Latin1_General_CP1_CI_AS,timestamp int,ui_control_sysid varchar(40) collate SQL_Latin1_General_CP1_CI_AS,ui_name varchar(60) collate SQL_Latin1_General_CP1_CI_AS,ui_section_sysid varchar(40) collate SQL_Latin1_General_CP1_CI_AS,view_name varchar(60) collate SQL_Latin1_General_CP1_CI_AS,visisble_length numeric(5),vorder int,label_column_scalemode varchar(6) collate SQL_Latin1_General_CP1_CI_AS,data_column_scalemode varchar(6) collate SQL_Latin1_General_CP1_CI_AS,tab_seq int,help_tabstop varchar(5) collate SQL_Latin1_General_CP1_CI_AS,LabelClass varchar(60) collate SQL_Latin1_General_CP1_CI_AS,ControlClass varchar(60) collate SQL_Latin1_General_CP1_CI_AS,LabelImageClass varchar(60) collate SQL_Latin1_General_CP1_CI_AS,ControlImageClass varchar(60) collate SQL_Latin1_General_CP1_CI_AS,label_control_id varchar(60) collate SQL_Latin1_General_CP1_CI_AS,req_no varchar(60) collate SQL_Latin1_General_CP1_CI_AS)");
                sb.AppendLine("create table #de_published_ui_grid(activity_name varchar(60) collate SQL_Latin1_General_CP1_CI_AS, column_bt_synonym varchar(60) collate SQL_Latin1_General_CP1_CI_AS, column_no int, column_prefix varchar(6) collate SQL_Latin1_General_CP1_CI_AS, column_type varchar(60) collate SQL_Latin1_General_CP1_CI_AS, col_doc varchar(4000) collate SQL_Latin1_General_CP1_CI_AS, component_name varchar(60) collate SQL_Latin1_General_CP1_CI_AS, control_bt_synonym varchar(60) collate SQL_Latin1_General_CP1_CI_AS, control_id varchar(60) collate SQL_Latin1_General_CP1_CI_AS, grid_sysid varchar(40) collate SQL_Latin1_General_CP1_CI_AS, page_bt_synonym varchar(60) collate SQL_Latin1_General_CP1_CI_AS, proto_tooltip varchar(255) collate SQL_Latin1_General_CP1_CI_AS, sample_data varchar(4000) collate SQL_Latin1_General_CP1_CI_AS, section_bt_synonym varchar(255) collate SQL_Latin1_General_CP1_CI_AS, timestamp int, ui_control_sysid varchar(40) collate SQL_Latin1_General_CP1_CI_AS, ui_name varchar(60) collate SQL_Latin1_General_CP1_CI_AS, view_name varchar(60) collate SQL_Latin1_General_CP1_CI_AS, visible_length numeric(5), req_no varchar(60) collate SQL_Latin1_General_CP1_CI_AS)");
                sb.AppendLine("create table #de_published_ui_page(activity_name varchar(60) collate SQL_Latin1_General_CP1_CI_AS, component_name varchar(60) collate SQL_Latin1_General_CP1_CI_AS, horder int, page_bt_synonym varchar(30) collate SQL_Latin1_General_CP1_CI_AS, page_doc varchar(4000) collate SQL_Latin1_General_CP1_CI_AS,  page_prefix varchar(6) collate SQL_Latin1_General_CP1_CI_AS, timestamp int, ui_name varchar(60) collate SQL_Latin1_General_CP1_CI_AS, ui_page_sysid varchar(40) collate SQL_Latin1_General_CP1_CI_AS, ui_sysid varchar(40) collate SQL_Latin1_General_CP1_CI_AS, vorder int, req_no varchar(60) collate SQL_Latin1_General_CP1_CI_AS)");
                //sb.AppendLine("create table #fw_exrep_task_temp_map (activity_name varchar(60) collate SQL_Latin1_General_CP1_CI_AS, ui_name varchar(60) collate SQL_Latin1_General_CP1_CI_AS, page_name varchar(60) collate SQL_Latin1_General_CP1_CI_AS, task_name varchar(60) collate SQL_Latin1_General_CP1_CI_AS, template_id varchar(60) collate SQL_Latin1_General_CP1_CI_AS, control_name varchar(60) collate SQL_Latin1_General_CP1_CI_AS, control_page_name varchar(60) collate SQL_Latin1_General_CP1_CI_AS, control_id varchar(60) collate SQL_Latin1_General_CP1_CI_AS, view_name varchar(60) collate SQL_Latin1_General_CP1_CI_AS)");
                sb.AppendLine("create table #de_listedit_view_datamap(activity_name varchar(60) collate SQL_Latin1_General_CP1_CI_AS, ilbocode varchar(60) collate SQL_Latin1_General_CP1_CI_AS, controlid varchar(60) collate SQL_Latin1_General_CP1_CI_AS, viewname varchar(60) collate SQL_Latin1_General_CP1_CI_AS, listedit varchar(60) collate SQL_Latin1_General_CP1_CI_AS, instance int)");
                sb.AppendLine("create table #de_ezeereport_task_control(component_name varchar(60) collate SQL_Latin1_General_CP1_CI_AS, activity_name varchar(60) collate SQL_Latin1_General_CP1_CI_AS, ui_name varchar(60) collate SQL_Latin1_General_CP1_CI_AS, page_name varchar(60) collate SQL_Latin1_General_CP1_CI_AS, control_name varchar(60) collate SQL_Latin1_General_CP1_CI_AS, control_id varchar(60) collate SQL_Latin1_General_CP1_CI_AS, task_name varchar(60) collate SQL_Latin1_General_CP1_CI_AS) ");
                sb.AppendLine("create table #published_ui(activity_name varchar(60) collate SQL_Latin1_General_CP1_CI_AS, base_activity_name varchar(60) collate SQL_Latin1_General_CP1_CI_AS, base_component_name varchar(60) collate SQL_Latin1_General_CP1_CI_AS, base_ui_name varchar(60) collate SQL_Latin1_General_CP1_CI_AS, caption_alignment varchar(60) collate SQL_Latin1_General_CP1_CI_AS, current_req_no varchar(60) collate SQL_Latin1_General_CP1_CI_AS, tab_height int, trail_bar varchar(5) collate SQL_Latin1_General_CP1_CI_AS, ui_descr varchar(255) collate SQL_Latin1_General_CP1_CI_AS, ui_doc varchar(4000) collate SQL_Latin1_General_CP1_CI_AS, ui_format varchar(5) collate SQL_Latin1_General_CP1_CI_AS, ui_name varchar(60) collate SQL_Latin1_General_CP1_CI_AS, ui_sysid varchar(40) collate SQL_Latin1_General_CP1_CI_AS, ui_type varchar(60) collate SQL_Latin1_General_CP1_CI_AS, grid_type varchar(20) collate SQL_Latin1_General_CP1_CI_AS, state_processing varchar(20) collate SQL_Latin1_General_CP1_CI_AS, req_no varchar(60) collate SQL_Latin1_General_CP1_CI_AS, callout_type varchar(20) collate SQL_Latin1_General_CP1_CI_AS, taskpane_req varchar(5) collate SQL_Latin1_General_CP1_CI_AS)");
                sb.AppendLine("create table #fw_des_publish_ilbo_service_view_attributemap (activityid  int, controlid  varchar(60) collate SQL_Latin1_General_CP1_CI_AS, dataitemname   varchar(60) collate SQL_Latin1_General_CP1_CI_AS, ilbocode  varchar(200) collate SQL_Latin1_General_CP1_CI_AS, segmentname varchar(60) collate SQL_Latin1_General_CP1_CI_AS, servicename varchar(64) collate SQL_Latin1_General_CP1_CI_AS, taskname varchar(100) collate SQL_Latin1_General_CP1_CI_AS, viewname varchar(60) collate SQL_Latin1_General_CP1_CI_AS, PropertyType varchar(60) collate SQL_Latin1_General_CP1_CI_AS, PropertyName varchar(60) collate SQL_Latin1_General_CP1_CI_AS, Page_bt_Synonym varchar(60) collate SQL_Latin1_General_CP1_CI_AS)");
                sb.AppendLine("create table #fw_req_activity_ilbo_task_tabs(activityid int,  ilbocode varchar(200) collate SQL_Latin1_General_CP1_CI_AS, taskname varchar(100) collate SQL_Latin1_General_CP1_CI_AS, tabname varchar(60) collate SQL_Latin1_General_CP1_CI_AS) ");
                sb.AppendLine("create table #fw_des_service_dtl(servicename varchar(60) collate sql_latin1_general_cp1_ci_as, servicetype int, insegs varchar(8000) collate sql_latin1_general_cp1_ci_as, outsegs varchar(8000) collate sql_latin1_general_cp1_ci_as, iosegs varchar(8000) collate sql_latin1_general_cp1_ci_as, scsegs varchar(8000) collate sql_latin1_general_cp1_ci_as, insegcnt int, outsegcnt int, scsegcnt int, pscnt int, iosegcnt int, datasegs varchar(8000) collate sql_latin1_general_cp1_ci_as, segcnt int, Junk_outscsegs varchar(200) collate sql_latin1_general_cp1_ci_as, junk_inscsegs int)");
                sb.AppendLine("create table #fw_des_service_segment_dtl(servicename varchar(60) collate sql_latin1_general_cp1_ci_as, segmentname varchar(60) collate sql_latin1_general_cp1_ci_as, instanceflag int, parentsegment varchar(60) collate sql_latin1_general_cp1_ci_as, flowdirection int, mandatoryflag int, ispartofhierarchy int, isscratchdipartofsegment int, segmentsequence int, segkeys int, dicnt int, indinames  varchar(8000) collate sql_latin1_general_cp1_ci_as, iodinames varchar(8000) collate sql_latin1_general_cp1_ci_as, outdinames varchar(8000) collate sql_latin1_general_cp1_ci_as, scdinames varchar(8000) collate sql_latin1_general_cp1_ci_as, dinames varchar(8000) collate sql_latin1_general_cp1_ci_as, indicnt int, outdicnt int, iodicnt int, scdicnt int)");
                sb.AppendLine("create table #fw_des_integ_outsegmap_dtl(servicename varchar(60) collate sql_latin1_general_cp1_ci_as, callerservice  varchar(60) collate sql_latin1_general_cp1_ci_as, sectionname varchar(60) collate sql_latin1_general_cp1_ci_as, sequenceno int, isCompName varchar(60) collate sql_latin1_general_cp1_ci_as, isSegList varchar(8000) collate sql_latin1_general_cp1_ci_as)");
                sb.AppendLine("create table #fw_des_integ_segmap_dtl( callerservice varchar(60) collate sql_latin1_general_cp1_ci_as, integservice varchar(60) collate sql_latin1_general_cp1_ci_as, sectionname varchar(60) collate sql_latin1_general_cp1_ci_as, sequenceno int, callerSegList varchar(8000) collate sql_latin1_general_cp1_ci_as,isCompName varchar(60) collate sql_latin1_general_cp1_ci_as, callerOuSeg varchar(60) collate sql_latin1_general_cp1_ci_as, callerOuDi varchar(8000) collate sql_latin1_general_cp1_ci_as, isInSegList varchar(8000) collate sql_latin1_general_cp1_ci_as)");
                sb.AppendLine("create table #fw_des_processsection_dtl(servicename varchar(60) collate sql_latin1_general_cp1_ci_as, sectionname varchar(60) collate sql_latin1_general_cp1_ci_as,sectiontype tinyint, seqNo tinyint, controlexpression varchar(256) collate sql_latin1_general_cp1_ci_as, loopingStyle tinyint, loopcausingsegment varchar(60) collate sql_latin1_general_cp1_ci_as,loopinstanceflag tinyint, ceInstDepFlag tinyint, brsCnt int)");
                sb.AppendLine("create table #fw_des_service_dataitem_dtl (servicename varchar(60) collate sql_latin1_general_cp1_ci_as, segmentname varchar(60) collate sql_latin1_general_cp1_ci_as, dataitemname varchar(60) collate sql_latin1_general_cp1_ci_as, datatype varchar(20) collate sql_latin1_general_cp1_ci_as, datalength int, ispartofkey int, flowattribute int, mandatoryflag int, defaultvalue varchar(510) collate sql_latin1_general_cp1_ci_as, BT_Type varchar(20) collate sql_latin1_general_cp1_ci_as, isModel char(2) collate sql_latin1_general_cp1_ci_as )");
                sb.AppendLine("create table #fw_des_parameter_cnt(servicename varchar(60) collate sql_latin1_general_cp1_ci_as, segmentname varchar(60) collate sql_latin1_general_cp1_ci_as, dataitemname varchar(60) collate sql_latin1_general_cp1_ci_as, br_totcnt int, br_incnt int)");
                sb.AppendLine("create table #fw_ezeeview_sp(component_name varchar(60) collate sql_latin1_general_cp1_ci_as, activity_name varchar(60) collate sql_latin1_general_cp1_ci_as,ui_name varchar(60) collate sql_latin1_general_cp1_ci_as, taskname varchar(60) collate sql_latin1_general_cp1_ci_as, page_bt_synonym varchar(60) collate sql_latin1_general_cp1_ci_as, Link_ControlName varchar(60) collate sql_latin1_general_cp1_ci_as, Target_SPName varchar(60) collate sql_latin1_general_cp1_ci_as, Link_Caption varchar(60) collate sql_latin1_general_cp1_ci_as, Linked_Component varchar(60) collate sql_latin1_general_cp1_ci_as, Linked_Activity varchar(60) collate sql_latin1_general_cp1_ci_as, Linked_ui varchar(60) collate sql_latin1_general_cp1_ci_as) ");
                sb.AppendLine("create table #fw_ezeeview_spparamlist(component_name varchar(60) collate sql_latin1_general_cp1_ci_as, activity_name varchar(60) collate sql_latin1_general_cp1_ci_as, ui_name varchar(60) collate sql_latin1_general_cp1_ci_as, page_bt_synonym varchar(60) collate sql_latin1_general_cp1_ci_as, Link_ControlName varchar(60) collate sql_latin1_general_cp1_ci_as, Target_SPName varchar(60) collate sql_latin1_general_cp1_ci_as, ParameterName varchar(60) collate sql_latin1_general_cp1_ci_as, Mapped_Control varchar(60) collate sql_latin1_general_cp1_ci_as, Link_Caption varchar(60) collate sql_latin1_general_cp1_ci_as, taskname varchar(60) collate sql_latin1_general_cp1_ci_as, controlid varchar(60) collate sql_latin1_general_cp1_ci_as, viewname varchar(60) collate sql_latin1_general_cp1_ci_as) ");
                sb.AppendLine("create table #fw_des_caller_integ_serv_map (componentname varchar(60) collate SQL_Latin1_General_CP1_CI_AS, callingservicename varchar(60) collate SQL_Latin1_General_CP1_CI_AS, callingsegment varchar(60) collate SQL_Latin1_General_CP1_CI_AS, callingdataitem varchar(60) collate SQL_Latin1_General_CP1_CI_AS, integservicename varchar(60) collate SQL_Latin1_General_CP1_CI_AS, integsegment varchar(60) collate SQL_Latin1_General_CP1_CI_AS, integdataitem varchar(60) collate SQL_Latin1_General_CP1_CI_AS)");
                sb.AppendLine("create table #fw_extjs_link_grid_map (taskname varchar(100) collate SQL_Latin1_General_CP1_CI_AS, ui_name varchar(60) collate SQL_Latin1_General_CP1_CI_AS, subscribed_control_id varchar(60) collate SQL_Latin1_General_CP1_CI_AS)");
                sb.AppendLine("create table #fw_extjs_control_dtl (activityname varchar(60) collate SQL_Latin1_General_CP1_CI_AS, uiname varchar(60) collate SQL_Latin1_General_CP1_CI_AS, taskname varchar(60) collate SQL_Latin1_General_CP1_CI_AS, servicename varchar(60) collate SQL_Latin1_General_CP1_CI_AS, segmentname varchar(60) collate SQL_Latin1_General_CP1_CI_AS, sectiontype varchar(60) collate SQL_Latin1_General_CP1_CI_AS, controlid varchar(60) collate SQL_Latin1_General_CP1_CI_AS)");
                sb.AppendLine("create table #de_published_action(activity_name varchar(60) collate SQL_Latin1_General_CP1_CI_AS, component_name varchar(60) collate SQL_Latin1_General_CP1_CI_AS, page_bt_synonym varchar(60) collate SQL_Latin1_General_CP1_CI_AS, primary_control_bts varchar(60) collate SQL_Latin1_General_CP1_CI_AS, task_descr varchar(255) collate SQL_Latin1_General_CP1_CI_AS, task_name varchar(60) collate SQL_Latin1_General_CP1_CI_AS, task_pattern varchar(60) collate SQL_Latin1_General_CP1_CI_AS, task_seq int, task_type varchar(60) collate SQL_Latin1_General_CP1_CI_AS, ui_name varchar(60) collate SQL_Latin1_General_CP1_CI_AS, task_confirm_msg varchar(255) collate SQL_Latin1_General_CP1_CI_AS, task_status_msg varchar(255) collate SQL_Latin1_General_CP1_CI_AS, usageid varchar(60) collate SQL_Latin1_General_CP1_CI_AS, ddt_page_bt_synonym varchar(60) collate SQL_Latin1_General_CP1_CI_AS, ddt_control_bt_synonym varchar(60) collate SQL_Latin1_General_CP1_CI_AS, ddt_control_id varchar(60) collate SQL_Latin1_General_CP1_CI_AS, ddt_view_name varchar(60) collate SQL_Latin1_General_CP1_CI_AS, task_process_msg varchar(255) collate SQL_Latin1_General_CP1_CI_AS) ");
                sb.AppendLine("create table #fw_req_ilbo_pivot_fields(component_name varchar(60) collate SQL_Latin1_General_CP1_CI_AS, activity_name varchar(60) collate SQL_Latin1_General_CP1_CI_AS, ilbocode varchar(60) collate SQL_Latin1_General_CP1_CI_AS, PageName varchar(60) collate SQL_Latin1_General_CP1_CI_AS, controlid varchar(60) collate SQL_Latin1_General_CP1_CI_AS, viewname varchar(60) collate SQL_Latin1_General_CP1_CI_AS, rowlabel varchar(5) collate SQL_Latin1_General_CP1_CI_AS, columnlabel varchar(5) collate SQL_Latin1_General_CP1_CI_AS, fieldValue varchar(5) collate SQL_Latin1_General_CP1_CI_AS, rowlabelseq int,");
                sb.AppendLine("columnlabelseq int, valueseq int, ValueFunction varchar(60) collate SQL_Latin1_General_CP1_CI_AS)");
                sb.AppendLine("create table #de_published_ui_control_association_map(component_name varchar(60) collate SQL_Latin1_General_CP1_CI_AS, activity_name varchar(60) collate SQL_Latin1_General_CP1_CI_AS, ilbocode varchar(60) collate SQL_Latin1_General_CP1_CI_AS, PageName varchar(60) collate SQL_Latin1_General_CP1_CI_AS, section_bt_synonym  varchar(60) collate SQL_Latin1_General_CP1_CI_AS, controlid varchar(60) collate SQL_Latin1_General_CP1_CI_AS, viewname varchar(60) collate SQL_Latin1_General_CP1_CI_AS, PropertyName varchar(60) collate SQL_Latin1_General_CP1_CI_AS, PropertyControl varchar(60) collate SQL_Latin1_General_CP1_CI_AS, PropertyViewName varchar(60) collate SQL_Latin1_General_CP1_CI_AS, TaskName varchar(60) collate SQL_Latin1_General_CP1_CI_AS)");
                sb.AppendLine("create table #de_published_subscription_dataitem(component_name varchar(60) collate SQL_Latin1_General_CP1_CI_AS, activity_name varchar(60) collate SQL_Latin1_General_CP1_CI_AS, ilbocode varchar(60) collate SQL_Latin1_General_CP1_CI_AS, PageName varchar(60) collate SQL_Latin1_General_CP1_CI_AS, TaskName varchar(60) collate SQL_Latin1_General_CP1_CI_AS, subscribed_bt_synonym varchar(60) collate SQL_Latin1_General_CP1_CI_AS, controlid varchar(60) collate SQL_Latin1_General_CP1_CI_AS, viewname varchar(60) collate SQL_Latin1_General_CP1_CI_AS, Qlik_dataitem varchar(60) collate SQL_Latin1_General_CP1_CI_AS)");

                sb.AppendLine(" create table #de_fw_des_publish_task_callout(component_name varchar(60) collate SQL_Latin1_General_CP1_CI_AS, activity_name varchar(60) collate SQL_Latin1_General_CP1_CI_AS, ui_name varchar(60) collate SQL_Latin1_General_CP1_CI_AS, task_name varchar(60) collate SQL_Latin1_General_CP1_CI_AS,  CalloutName varchar(60) collate SQL_Latin1_General_CP1_CI_AS, CalloutMode varchar(60) collate SQL_Latin1_General_CP1_CI_AS)");
                sb.AppendLine(" create table #de_fw_des_publish_task_callout_segement(component_name varchar(60) collate SQL_Latin1_General_CP1_CI_AS, activity_name varchar(60) collate SQL_Latin1_General_CP1_CI_AS, ui_name varchar(60) collate SQL_Latin1_General_CP1_CI_AS, task_name varchar(60) collate SQL_Latin1_General_CP1_CI_AS,  CalloutName varchar(60) collate SQL_Latin1_General_CP1_CI_AS, SegmentName varchar(60) collate SQL_Latin1_General_CP1_CI_AS, InstanceFlag int, SegmentSequence int, SegmentFlowAttribute varchar(10) collate SQL_Latin1_General_CP1_CI_AS) ");
                sb.AppendLine(" create table #de_fw_des_publish_task_callout_dataitem(component_name varchar(60) collate SQL_Latin1_General_CP1_CI_AS, activity_name varchar(60) collate SQL_Latin1_General_CP1_CI_AS, ui_name varchar(60) collate SQL_Latin1_General_CP1_CI_AS, task_name varchar(60) collate SQL_Latin1_General_CP1_CI_AS,  CalloutName varchar(60) collate SQL_Latin1_General_CP1_CI_AS, SegmentName varchar(60) collate SQL_Latin1_General_CP1_CI_AS, DataItemName varchar(60) collate SQL_Latin1_General_CP1_CI_AS, FlowAttribute int, ControlID  varchar(60) collate SQL_Latin1_General_CP1_CI_AS, ViewName varchar(60) collate SQL_Latin1_General_CP1_CI_AS) ");

                sb.AppendLine("CREATE TABLE #fw_des_publish_api_request_serv_map	(	ServiceName	VARCHAR(60) collate SQL_Latin1_General_CP1_CI_AS,SectionName	VARCHAR(60) collate SQL_Latin1_General_CP1_CI_AS,SequenceNo	int,SpecID	INT,SpecName VARCHAR(300) collate SQL_Latin1_General_CP1_CI_AS,Version	 int, Path	 VARCHAR(500) collate SQL_Latin1_General_CP1_CI_AS,OperationVerb	 VARCHAR(5) collate SQL_Latin1_General_CP1_CI_AS, MediaType VARCHAR(60) collate SQL_Latin1_General_CP1_CI_AS,ParentSchemaName VARCHAR(100) collate SQL_Latin1_General_CP1_CI_AS,SchemaName VARCHAR(100) collate SQL_Latin1_General_CP1_CI_AS,SchemaCategory	 VARCHAR(5) collate SQL_Latin1_General_CP1_CI_AS, SegmentName VARCHAR(60) collate SQL_Latin1_General_CP1_CI_AS, DataItemName	 VARCHAR(60) collate SQL_Latin1_General_CP1_CI_AS,NodeID	 NVARCHAR(MAX) collate SQL_Latin1_General_CP1_CI_AS,	ParentNodeID	 NVARCHAR(MAX) collate SQL_Latin1_General_CP1_CI_AS,Identifier	VARCHAR(20) collate SQL_Latin1_General_CP1_CI_AS, Type	VARCHAR(20) collate SQL_Latin1_General_CP1_CI_AS, DisplayName	 NVARCHAR(MAX) collate SQL_Latin1_General_CP1_CI_AS, SchemaType VARCHAR(100) collate SQL_Latin1_General_CP1_CI_AS)");
                sb.AppendLine("CREATE TABLE #fw_des_publish_api_response_serv_map	(	ServiceName  VARCHAR(60) collate SQL_Latin1_General_CP1_CI_AS, SectionName  VARCHAR(60) collate SQL_Latin1_General_CP1_CI_AS, SequenceNo  int, SpecID 	 INT, SpecName  VARCHAR(300) collate SQL_Latin1_General_CP1_CI_AS, Version 	 int, Path 	 VARCHAR(500) collate SQL_Latin1_General_CP1_CI_AS, OperationVerb	 VARCHAR(5) collate SQL_Latin1_General_CP1_CI_AS, MediaType  VARCHAR(60) collate SQL_Latin1_General_CP1_CI_AS, ResponseCode	 VARCHAR(60) collate SQL_Latin1_General_CP1_CI_AS, ParentSchemaName VARCHAR(100) collate SQL_Latin1_General_CP1_CI_AS, SchemaName  VARCHAR(100) collate SQL_Latin1_General_CP1_CI_AS, SchemaCategory	 VARCHAR(5) collate SQL_Latin1_General_CP1_CI_AS, SegmentName  VARCHAR(60) collate SQL_Latin1_General_CP1_CI_AS, DataItemName	 VARCHAR(60) collate SQL_Latin1_General_CP1_CI_AS, NodeID 	 NVARCHAR(MAX) collate SQL_Latin1_General_CP1_CI_AS, ParentNodeID	 NVARCHAR(MAX) collate SQL_Latin1_General_CP1_CI_AS, Identifier	VARCHAR(20) collate SQL_Latin1_General_CP1_CI_AS,Type	VARCHAR(20) collate SQL_Latin1_General_CP1_CI_AS,DisplayName	 NVARCHAR(MAX) collate SQL_Latin1_General_CP1_CI_AS, SchemaType VARCHAR(100) collate SQL_Latin1_General_CP1_CI_AS)");
                sb.AppendLine("CREATE TABLE #fw_des_publish_api_pathparameter_serv_map (ServiceName VARCHAR(60) collate SQL_Latin1_General_CP1_CI_AS, SectionName VARCHAR(60) collate SQL_Latin1_General_CP1_CI_AS, SequenceNo int, SpecID 	INT, SpecName VARCHAR(300) collate SQL_Latin1_General_CP1_CI_AS, Version 	int, Path 	VARCHAR(500) collate SQL_Latin1_General_CP1_CI_AS,  ParameterName	VARCHAR(60) collate SQL_Latin1_General_CP1_CI_AS, SegmentName VARCHAR(60) collate SQL_Latin1_General_CP1_CI_AS, DataItemName	VARCHAR(60) collate SQL_Latin1_General_CP1_CI_AS, [In] VARCHAR(50) collate SQL_Latin1_General_CP1_CI_AS)");
                sb.AppendLine("CREATE TABLE #fw_des_publish_api_pathoperationparameter_serv_map (ServiceName VARCHAR(60), SectionName VARCHAR(60), SequenceNo int, SpecID INT, SpecName VARCHAR(300) collate SQL_Latin1_General_CP1_CI_AS, Version      int, Path  VARCHAR(500) collate SQL_Latin1_General_CP1_CI_AS,  OperationVerb      VARCHAR(60), ParameterName VARCHAR(60), SegmentName VARCHAR(60), DataItemName  VARCHAR(60), [In] VARCHAR(50) collate SQL_Latin1_General_CP1_CI_AS)");
                _dbManager.ExecuteNonQuery(System.Data.CommandType.Text, sb.ToString(), null);
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("CreateTables->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }

        private void PopulateHashTables()
        {
            try
            {
                _logger.WriteLogToFile("PopulateHashTables", "Populating hash tables..");
                var parameters = _dbManager.CreateParameters(4);
                _dbManager.AddParamters(parameters, 0, "@customername", _ecrOptions.Customer);
                _dbManager.AddParamters(parameters, 1, "@projectname", _ecrOptions.Project);
                _dbManager.AddParamters(parameters, 2, "@componentname", _ecrOptions.Component);
                _dbManager.AddParamters(parameters, 3, "@ecrno", _ecrOptions.Ecrno);
                _dbManager.ExecuteNonQuery(CommandType.StoredProcedure, "vw_netgen_populate_temptable_sp", parameters);
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("PopulateHashTables->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }

        /// <summary>
        /// Creates Index for codegen temp tables
        /// </summary>
        private void CreateIndexes()
        {
            try
            {
                _logger.WriteLogToFile("CreateIndexes", "Creating Indexes...");
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("create clustered index fw_req_activity_unqidx on  #fw_req_activity(componentname, activityid)");
                sb.AppendLine("create clustered index fw_req_activity_ilbo_unqidx on  #fw_req_activity_ilbo(activityid, ilbocode)");
                sb.AppendLine("create clustered index fw_req_ilbo_unqidx on  #fw_req_ilbo(ilbocode)");
                sb.AppendLine("create clustered index fw_req_ilbo_control_property_unqidx on  #fw_req_ilbo_control_property(controlid, ilbocode, propertyname, viewname)");
                sb.AppendLine("create clustered index fw_req_ilbo_control_unqidx on  #fw_req_ilbo_control(ilbocode, controlid)");
                sb.AppendLine("create clustered index fw_req_ilbo_view_unqidx on  #fw_req_ilbo_view(controlid, ilbocode, viewname)");
                sb.AppendLine("create clustered index fw_req_ilbo_task_unqidx on #fw_req_ilbo_task_control(activityid,ilbocode, taskname,btsynonym )");
                sb.AppendLine("create clustered index fw_req_bterm_unqidx on  #fw_req_bterm(btname)");
                sb.AppendLine("create clustered index fw_req_bterm_synonym_unqidx on  #fw_req_bterm_synonym(btsynonym)");
                sb.AppendLine("create clustered index fw_req_process_component_unqidx on  #fw_req_process_component(  parentprocess)");
                sb.AppendLine("create clustered index fw_req_ilbo_tab_properties_unqidx on  #fw_req_ilbo_tab_properties(ilbocode, propertyname, tabname)");
                sb.AppendLine("create clustered index fw_req_activity_ilbo_task_unqidx on  #fw_req_activity_ilbo_task(activityid, ilbocode, taskname)");
                sb.AppendLine("create clustered index fw_req_task_unqidx on  #fw_req_task(taskname)");
                sb.AppendLine("create clustered index fw_req_bterm_enumerated_option_unqidx on  #fw_req_bterm_enumerated_option(btname, langid, sequenceno)");
                sb.AppendLine("create clustered index fw_req_ilbo_link_publish_unqidx on  #fw_req_ilbo_link_publish(ilbocode, linkid)");
                sb.AppendLine("create clustered index fw_req_ilbo_data_publish_unqidx on  #fw_req_ilbo_data_publish(dataitemname, ilbocode, linkid)");
                sb.AppendLine("create clustered index fw_req_ilbo_linkuse_unqidx on  #fw_req_ilbo_linkuse(parentilbocode, taskname)");
                sb.AppendLine("create clustered index fw_req_ilbo_data_use_unqidx on  #fw_req_ilbo_data_use(controlid, dataitemname, linkid, parentilbocode, taskname, viewname)");
                sb.AppendLine("create clustered index fw_des_ilbo_services_unqidx on  #fw_des_ilbo_services(ilbocode, servicename)");
                sb.AppendLine("create clustered index fw_des_service_dataitem_unqidx on  #fw_des_service_dataitem(servicename, segmentname, dataitemname)");
                sb.AppendLine("create clustered index fw_des_service_unqidx on  #fw_des_service( componentname, servicename)");
                sb.AppendLine("create clustered index fw_des_service_segment_unqidx on  #fw_des_service_segment(servicename, segmentname)");
                sb.AppendLine("create clustered index fw_des_ilbo_service_view_datamap_unqidx on  #fw_des_ilbo_service_view_datamap(ilbocode, servicename, activityid, taskname, segmentname, dataitemname)");
                sb.AppendLine("create clustered index fw_des_task_segment_attribs_unqidx on #fw_des_task_segment_attribs(activityid, ilbocode, taskname, servicename, segmentname)");
                sb.AppendLine("create clustered index fw_req_task_local_info_unqidx on  #fw_req_task_local_info(langid, taskname)");
                sb.AppendLine("create clustered index fw_req_ilbo_layout_control_unqidx on  #fw_req_ilbo_layout_control(ilbocode, tabname, controlid)");
                sb.AppendLine("create unique clustered index fw_des_br_logical_parameter_indx on #fw_des_br_logical_parameter (methodid, logicalparametername) ");
                sb.AppendLine("CREATE clustered INDEX fw_des_brerror_Indx on #fw_des_brerror (errorid ,methodid ,sperrorcode)");
                sb.AppendLine("CREATE clustered INDEX  fw_des_bro_indx on #fw_des_bro (broname , componentname )");
                sb.AppendLine("CREATE clustered INDEX  fw_des_businessrule_indx on #fw_des_businessrule (methodid)");
                sb.AppendLine("CREATE clustered INDEX  fw_des_context_indx on #fw_des_context ( errorid , errorcontext )");
                sb.AppendLine("CREATE clustered INDEX  fw_des_di_parameter_indx on #fw_des_di_parameter (servicename, sectionname, sequenceno, parametername )");
                sb.AppendLine("CREATE nonCLUSTERED INDEX  fw_des_di_parameter_indx1        on #fw_des_di_parameter (servicename, sectionname, MethodID, parametername ) ");
                sb.AppendLine("CREATE clustered INDEX  fw_des_di_placeholder_indx on #fw_des_di_placeholder(servicename, sectionname, sequenceno, methodid, placeholdername, errorid) ");
                sb.AppendLine("CREATE clustered INDEX  fw_des_error_indx on #fw_des_error (errorid)");
                sb.AppendLine("CREATE clustered INDEX  fw_des_integ_serv_map_indx on #fw_des_integ_serv_map(callingservicename, sectionname, sequenceno, integservicename, integsegment, integdataitem)");
                sb.AppendLine("CREATE clustered INDEX  fw_des_processsection_indx on #fw_des_processsection(servicename, sectionname) ");
                sb.AppendLine("CREATE clustered INDEX  fw_des_processsection_br_is_indx on #fw_des_processsection_br_is (servicename, sectionname, sequenceno, methodid)");
                sb.AppendLine("CREATE clustered INDEX  fw_des_sp_indx on #fw_des_sp ( methodid ) ");
                sb.AppendLine("CREATE clustered INDEX  fw_des_bo_indx on #fw_des_bo ( componentname, bocode ) ");
                sb.AppendLine("CREATE clustered INDEX  fw_des_error_placeholder_indx on #fw_des_error_placeholder (ErrorID, PlaceholderName) ");
                sb.AppendLine("CREATE clustered INDEX  fw_des_reqbr_desbr_indx on #fw_des_reqbr_desbr (ReqBRName, MethodID) ");
                sb.AppendLine("CREATE clustered INDEX  fw_req_task_rule_indx on #fw_req_task_rule (TaskName, BRSequence, BRName) ");
                sb.AppendLine("CREATE clustered INDEX  fw_des_ilbo_placeholder_indx on #fw_des_ilbo_placeholder (ilbocode, ControlID, EventName, PlaceholderName, ErrorID, CtrlEvent_ViewName) ");
                sb.AppendLine("CREATE clustered INDEX  fw_des_ilbo_ctrl_event_indx on #fw_des_ilbo_ctrl_event (ILBOCode, ControlID, taskname) ");
                sb.AppendLine("CREATE clustered INDEX  fw_req_businessrule_indx on #fw_req_businessrule (BRName) ");
                sb.AppendLine("CREATE clustered INDEX  fw_req_br_error_indx on #fw_req_br_error (BRName, ErrorCode) ");
                sb.AppendLine("CREATE clustered INDEX  fw_des_err_det_local_info_indx on #fw_des_err_det_local_info (errorid, langid) ");
                //sb.AppendLine("CREATE clustered INDEX  fw_exrep_task_temp_map_indx on #fw_exrep_task_temp_map (activity_name, ui_name, page_name, task_name) ");
                //sb.AppendLine("create clustered index  de_log_ext_ctrl_met_idx on #de_log_ext_ctrl_met(le_customer_name, le_project_name, col_seq)");
                sb.AppendLine("create clustered index fw_des_ilbo_service_view_attributemap_unqidx on  #fw_des_publish_ilbo_service_view_attributemap(ilbocode, servicename, activityid, taskname, segmentname, dataitemname)");
                sb.AppendLine("create index #fw_des_integ_segmap_dtl_idx on #fw_des_integ_segmap_dtl(callerservice, sequenceno, integservice)");
                sb.AppendLine("create index #fw_des_service_segment_dtl_idx on #fw_des_service_segment_dtl(servicename, segmentsequence)");
                sb.AppendLine("create clustered index fw_req_ilbo_pivot_fields_unqidx on  #fw_req_ilbo_pivot_fields(component_name, activity_name, ilbocode, PageName, controlid)");
                sb.AppendLine("create clustered index de_published_ui_control_association_map_unqidx on  #de_published_ui_control_association_map(component_name, activity_name, ilbocode, taskname, propertyname, propertycontrol, propertyviewname)");
                sb.AppendLine("create clustered index de_published_subscription_dataitem_unqidx on #de_published_subscription_dataitem(component_name, activity_name, ilbocode, taskname, subscribed_bt_synonym, qlik_dataitem)");
                _dbManager.ExecuteNonQuery(System.Data.CommandType.Text, sb.ToString(), null);
            }
            catch (Exception ex)
            {
                _logger.WriteLogToFile("CreateIndexes", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message);
                throw ex;
            }
        }


        private void ConstructServiceDatasetSchema(ref DataSet dsService)
        {
            dsService.DataSetName = "Services";

            //getting service
            if (dsService.Tables["Service"] == null)
            {
                DataTable dtService = new DataTable("Service");
                dtService.Columns.Add("name", typeof(string));
                dtService.Columns.Add("type", typeof(string));
                dtService.Columns.Add("isintegser", typeof(string));
                dtService.Columns.Add("iszipped", typeof(string));
                dtService.Columns.Add("iscached", typeof(string));
                dtService.Columns.Add("componentname", typeof(string));
                dtService.Columns.Add("isselected", typeof(string));
                dsService.Tables.Add(dtService);
            }

            //getting segment
            if (dsService.Tables["Segment"] == null)
            {
                DataTable dtSegment = new DataTable("Segment");
                dtSegment.Columns.Add("name", typeof(string));
                dtSegment.Columns.Add("sequence", typeof(string));
                dtSegment.Columns.Add("instanceflag", typeof(string));
                dtSegment.Columns.Add("mandatoryflag", typeof(string));
                dtSegment.Columns.Add("servicename", typeof(string));
                dtSegment.Columns.Add("process_selrows", typeof(string));
                dtSegment.Columns.Add("process_updrows", typeof(string));
                dtSegment.Columns.Add("process_selupdrows", typeof(string));
                dtSegment.Columns.Add("flowattribute", typeof(string));
                dsService.Tables.Add(dtSegment);
            }

            //getting dataitem
            if (dsService.Tables["DataItem"] == null)
            {
                DataTable dtDataItem = new DataTable("DataItem");
                dtDataItem.Columns.Add("name", typeof(string));
                dtDataItem.Columns.Add("type", typeof(string));
                dtDataItem.Columns.Add("datalength", typeof(string));
                dtDataItem.Columns.Add("ispartofkey", typeof(string));
                dtDataItem.Columns.Add("flowattribute", typeof(string));
                dtDataItem.Columns.Add("mandatoryflag", typeof(string));
                dtDataItem.Columns.Add("defaultvalue", typeof(string));
                dtDataItem.Columns.Add("servicename", typeof(string));
                dtDataItem.Columns.Add("segmentname", typeof(string));
                dsService.Tables.Add(dtDataItem);
            }

            //getting outdataitem
            if (dsService.Tables["OutDataItem"] == null)
            {
                DataTable dtOutDataItem = new DataTable("OutDataItem");
                dtOutDataItem.Columns.Add("name", typeof(string));
                dtOutDataItem.Columns.Add("type", typeof(string));
                dtOutDataItem.Columns.Add("datalength", typeof(string));
                dtOutDataItem.Columns.Add("ispartofkey", typeof(string));
                dtOutDataItem.Columns.Add("flowattribute", typeof(string));
                dtOutDataItem.Columns.Add("mandatoryflag", typeof(string));
                dtOutDataItem.Columns.Add("defaultvalue", typeof(string));
                dtOutDataItem.Columns.Add("servicename", typeof(string));
                dtOutDataItem.Columns.Add("segmentname", typeof(string));
                dsService.Tables.Add(dtOutDataItem);
            }

            //getting processsection
            if (dsService.Tables["ProcessSection"] == null)
            {
                DataTable dtProcessSection = new DataTable("ProcessSection");
                dtProcessSection.Columns.Add("name", typeof(string));
                dtProcessSection.Columns.Add("type", typeof(string));
                dtProcessSection.Columns.Add("seqno", typeof(string));
                dtProcessSection.Columns.Add("controlexpression", typeof(string));
                dtProcessSection.Columns.Add("loopingstyle", typeof(string));
                dtProcessSection.Columns.Add("servicename", typeof(string));
                dsService.Tables.Add(dtProcessSection);
            }

            //getting method
            if (dsService.Tables["Method"] == null)
            {
                DataTable dtMethod = new DataTable("Method");
                dtMethod.Columns.Add("id", typeof(string));
                dtMethod.Columns.Add("name", typeof(string));
                dtMethod.Columns.Add("seqno", typeof(string));
                dtMethod.Columns.Add("ismethod", typeof(string));
                dtMethod.Columns.Add("spname", typeof(string));
                dtMethod.Columns.Add("servicename", typeof(string));
                dtMethod.Columns.Add("integservicename", typeof(string));
                dtMethod.Columns.Add("controlexpression", typeof(string));
                dtMethod.Columns.Add("sectionname", typeof(string));
                dtMethod.Columns.Add("accessesdatabase", typeof(string));
                dtMethod.Columns.Add("operationtype", typeof(string));
                dtMethod.Columns.Add("systemgenerated", typeof(string));
                dtMethod.Columns.Add("sperrorprotocol", typeof(string));
                dtMethod.Columns.Add("loopcausingsegment", typeof(string));
                dtMethod.Columns.Add("method_exec_cont", typeof(string));
                dtMethod.Columns.Add("specid", typeof(string));
                dtMethod.Columns.Add("specname", typeof(string));
                dtMethod.Columns.Add("specversion", typeof(string));
                dtMethod.Columns.Add("path", typeof(string));
                dtMethod.Columns.Add("operationid", typeof(string));
                dtMethod.Columns.Add("operationverb", typeof(string));
                dsService.Tables.Add(dtMethod);
            }

            //getting ismappingdetails
            if (dsService.Tables["ISMappingDetails"] == null)
            {
                DataTable dtISMappingDetails = new DataTable("ISMappingDetails");
                dtISMappingDetails.Columns.Add("issegname", typeof(string));
                dtISMappingDetails.Columns.Add("isdiname", typeof(string));
                dtISMappingDetails.Columns.Add("callersegname", typeof(string));
                dtISMappingDetails.Columns.Add("callerdiname", typeof(string));
                dtISMappingDetails.Columns.Add("integservicename", typeof(string));
                dtISMappingDetails.Columns.Add("flowattribute", typeof(string));
                dtISMappingDetails.Columns.Add("defaultvalue", typeof(string));
                dtISMappingDetails.Columns.Add("ispartofkey", typeof(string));
                dtISMappingDetails.Columns.Add("callingservicename", typeof(string));
                dtISMappingDetails.Columns.Add("servicename", typeof(string));
                dtISMappingDetails.Columns.Add("icomponentname", typeof(string));
                dtISMappingDetails.Columns.Add("sectionname", typeof(string));
                dtISMappingDetails.Columns.Add("seqno", typeof(string));
                dtISMappingDetails.Columns.Add("callsegmentinst", typeof(string));
                dtISMappingDetails.Columns.Add("calldiflow", typeof(string));
                dtISMappingDetails.Columns.Add("issegmentinst", typeof(string));
                dtISMappingDetails.Columns.Add("iser_pr_type", typeof(string));
                dtISMappingDetails.Columns.Add("is_servicetype", typeof(string));
                dtISMappingDetails.Columns.Add("is_isintegser", typeof(string));
                dsService.Tables.Add(dtISMappingDetails);
            }

            //getting physical parameters
            if (dsService.Tables["PhysicalParameters"] == null)
            {
                DataTable dtPhysicalParameters = new DataTable("PhysicalParameters");
                dtPhysicalParameters.Columns.Add("physicalparametername", typeof(string));
                dtPhysicalParameters.Columns.Add("recordsetname", typeof(string));
                dtPhysicalParameters.Columns.Add("seqno", typeof(string));
                dtPhysicalParameters.Columns.Add("flowdirection", typeof(string));
                dtPhysicalParameters.Columns.Add("datasegmentname", typeof(string));
                dtPhysicalParameters.Columns.Add("dataitemname", typeof(string));
                dtPhysicalParameters.Columns.Add("methodid", typeof(string));
                dtPhysicalParameters.Columns.Add("sequenceno", typeof(string));
                dtPhysicalParameters.Columns.Add("servicename", typeof(string));
                dtPhysicalParameters.Columns.Add("sectionname", typeof(string));
                dtPhysicalParameters.Columns.Add("brparamtype", typeof(string));
                dtPhysicalParameters.Columns.Add("length", typeof(string));
                dtPhysicalParameters.Columns.Add("paramtype", typeof(string));
                dsService.Tables.Add(dtPhysicalParameters);
            }

            ////getting logical parameters
            //if (dsService.Tables["LogicalParameters"] == null)
            //{
            //    DataTable dtLogicalParameters = new DataTable("LogicalParameters");
            //    dtLogicalParameters.Columns.Add("logicalparametername", typeof(string));
            //    dtLogicalParameters.Columns.Add("recordsetname", typeof(string));
            //    dtLogicalParameters.Columns.Add("seqno", typeof(string));
            //    dtLogicalParameters.Columns.Add("datasegmentname", typeof(string));
            //    dtLogicalParameters.Columns.Add("dataitemname", typeof(string));
            //    dtLogicalParameters.Columns.Add("methodid", typeof(string));
            //    dtLogicalParameters.Columns.Add("sequenceno", typeof(string));
            //    dtLogicalParameters.Columns.Add("servicename", typeof(string));
            //    dtLogicalParameters.Columns.Add("sectionname", typeof(string));
            //    dtLogicalParameters.Columns.Add("methodid", typeof(string));
            //    dtLogicalParameters.Columns.Add("paramtype", typeof(string));
            //    dsService.Tables.Add(dtLogicalParameters);
            //}

            //geting di property mapping
            if (dsService.Tables["DIPropertyMapping"] == null)
            {
                DataTable dtDIPropertyMapping = new DataTable("DIPropertyMapping");
                dtDIPropertyMapping.Columns.Add("servicename", typeof(string));
                dtDIPropertyMapping.Columns.Add("segmentname", typeof(string));
                dtDIPropertyMapping.Columns.Add("dataitemname", typeof(string));
                dsService.Tables.Add(dtDIPropertyMapping);
            }

            //getting unused dataitems
            if (dsService.Tables["UnusedDI"] == null)
            {
                DataTable dtUnusedDI = new DataTable("UnusedDI");
                dtUnusedDI.Columns.Add("servicename", typeof(string));
                dtUnusedDI.Columns.Add("segmentname", typeof(string));
                dtUnusedDI.Columns.Add("instanceflag", typeof(string));
                dtUnusedDI.Columns.Add("dataitemname", typeof(string));
                dtUnusedDI.Columns.Add("defaultvalue", typeof(string));
                dsService.Tables.Add(dtUnusedDI);
            }
        }
        private void ConstructErrorDatasetSchema(ref DataSet dsError)
        {

        }

        private void AddDataRelationToServiceDataset(ref DataSet dsService)
        {
            string sParentTable = string.Empty;
            string sChildTable = string.Empty;

            try
            {
                //adding relations
                //service-segment
                sParentTable = "Service";
                sChildTable = "Segment";
                DataRelation drServiceSegment = new DataRelation("R_service_segment", dsService.Tables["Service"].Columns["name"],
                                          dsService.Tables["Segment"].Columns["servicename"], false);
                //drServiceSegment.Nested = true;
                dsService.Relations.Add(drServiceSegment);


                //segment-dataitem
                sParentTable = "Segment";
                sChildTable = "DataItem";
                DataRelation drSegmentDataitem = new DataRelation("R_segment_dataitem", new DataColumn[] {
                                            dsService.Tables["Segment"].Columns["servicename"],
                                            dsService.Tables["Segment"].Columns["name"] },
                                         new DataColumn[] {
                                             dsService.Tables["DataItem"].Columns["servicename"],
                                             dsService.Tables["DataItem"].Columns["segmentname"] }, false);
                //drSegmentDataitem.Nested = true;
                dsService.Relations.Add(drSegmentDataitem);


                //segment-out_dataitem
                sParentTable = "Segment";
                sChildTable = "OutDataItem";
                DataRelation drSegmentOutDataitem = new DataRelation("R_segment_out_dataitem", new DataColumn[] {
                                            dsService.Tables["Segment"].Columns["servicename"],
                                            dsService.Tables["Segment"].Columns["name"] },
                                         new DataColumn[] {
                                             dsService.Tables["OutDataItem"].Columns["servicename"],
                                             dsService.Tables["OutDataItem"].Columns["segmentname"] }, false);
                //drSegmentOutDataitem.Nested = true;
                dsService.Relations.Add(drSegmentOutDataitem);



                //service-process section
                sParentTable = "Service";
                sChildTable = "ProcessSecion";
                DataRelation drServiceProcessSection = new DataRelation("R_service_ps", dsService.Tables["Service"].Columns["name"],
                                         dsService.Tables["ProcessSection"].Columns["servicename"], false);
                //drServiceProcessSection.Nested = true;
                dsService.Relations.Add(drServiceProcessSection);


                //process section - BR(Method)
                sParentTable = "ProcessSection";
                sChildTable = "Method";
                DataRelation drProcessSectionMethod = new DataRelation("R_ps_method", new DataColumn[] {
                                            dsService.Tables["ProcessSection"].Columns["servicename"],
                                            dsService.Tables["ProcessSection"].Columns["name"] },
                                         new DataColumn[] {
                                            dsService.Tables["Method"].Columns["servicename"],
                                          dsService.Tables["Method"].Columns["sectionname"] }, false);
                //drProcessSectionMethod.Nested = true;
                dsService.Relations.Add(drProcessSectionMethod); //cannot create constraints in Method as it has Integration service with methodid & methodname value as empty.


                //Method(BR) - Integration service
                sParentTable = "Method";
                sChildTable = "ISMappingDetails";
                DataRelation drMethodISMapping = new DataRelation("R_method_ismapping", new DataColumn[] {
                                             dsService.Tables["Method"].Columns["servicename"]
                                             ,dsService.Tables["Method"].Columns["sectionname"]
                                             ,dsService.Tables["Method"].Columns["seqno"]
                                                            },
                                         new DataColumn[] {
                                             dsService.Tables["ISMappingDetails"].Columns["CallingServiceName"]
                                             ,dsService.Tables["ISMappingDetails"].Columns["sectionname"]
                                             ,dsService.Tables["ISMappingDetails"].Columns["seqno"]
                                                            }, false);
                //drMethodISMapping.Nested = true;
                dsService.Relations.Add(drMethodISMapping);

                //BR(Method) - Physical parameters
                sParentTable = "Method";
                sChildTable = "PhysicalParameters";
                DataRelation drMethodPhysicalParms = new DataRelation("R_method_params", new DataColumn[] {
                                                dsService.Tables["Method"].Columns["servicename"],
                                                dsService.Tables["Method"].Columns["id"],
                                                dsService.Tables["Method"].Columns["sectionname"],
                                                dsService.Tables["Method"].Columns["seqno"]
                                                },
                                            new DataColumn[] {
                                                dsService.Tables["PhysicalParameters"].Columns["servicename"],
                                                    dsService.Tables["PhysicalParameters"].Columns["methodid"],
                                                    dsService.Tables["PhysicalParameters"].Columns["sectionname"],
                                                    dsService.Tables["PhysicalParameters"].Columns["sequenceno"]
                                                }, false);
                //drMethodPhysicalParms.Nested = true;
                dsService.Relations.Add(drMethodPhysicalParms); //cannot create constraints in Method as it has Integration service with methodid & methodname value as empty.												

                ////PhysicalParameter - LogicalParameter
                //sParentTable = "PhysicalParameters";
                //sChildTable = "LogicalParameters";
                //DataRelation drPhysicalLogicalParms = new DataRelation("R_physical_logical_params",
                //    new DataColumn[]
                //    {
                //        dsService.Tables["PhysicalParameters"].Columns["servicename"],
                //        dsService.Tables["PhysicalParameters"].Columns["methodid"],
                //        dsService.Tables["PhysicalParameters"].Columns["sectionname"],
                //        dsService.Tables["PhysicalParameters"].Columns["sequenceno"],
                //        dsService.Tables["PhysicalParameters"].Columns["recordsetname"]
                //    },
                //    new DataColumn[]
                //    {
                //        dsService.Tables["LogicalParameters"].Columns["servicename"],
                //        dsService.Tables["LogicalParameters"].Columns["methodid"],
                //        dsService.Tables["LogicalParameters"].Columns["sectionname"],
                //        dsService.Tables["LogicalParameters"].Columns["sequenceno"],
                //        dsService.Tables["LogicalParameters"].Columns["recordsetname"]
                //    });

                ////BR(Method) - Api Request
                //sParentTable = "Method";
                //sChildTable = "ApiRequest";
                //DataRelation drMethodApiRequest = new DataRelation("R_method_apirequest",
                //                                                    new DataColumn[]
                //                                                    {
                //                                                        dsService.Tables[sParentTable].Columns["servicename"],
                //                                                        dsService.Tables[sParentTable].Columns["sectionname"],
                //                                                        dsService.Tables[sParentTable].Columns["seqno"],
                //                                                        dsService.Tables[sParentTable].Columns["specid"],
                //                                                        dsService.Tables[sParentTable].Columns["specname"],
                //                                                        dsService.Tables[sParentTable].Columns["specversion"],
                //                                                        dsService.Tables[sParentTable].Columns["path"],
                //                                                        dsService.Tables[sParentTable].Columns["operationverb"]
                //                                                    },
                //                                                    new DataColumn[]
                //                                                    {
                //                                                        dsService.Tables[sChildTable].Columns["servicename"],
                //                                                        dsService.Tables[sChildTable].Columns["sectionname"],
                //                                                        dsService.Tables[sChildTable].Columns["sequenceno"],
                //                                                        dsService.Tables[sChildTable].Columns["specid"],
                //                                                        dsService.Tables[sChildTable].Columns["specname"],
                //                                                        dsService.Tables[sChildTable].Columns["version"],
                //                                                        dsService.Tables[sChildTable].Columns["path"],
                //                                                        dsService.Tables[sChildTable].Columns["operationverb"]
                //                                                    }, false);
                //dsService.Relations.Add(drMethodApiRequest);


                ////BR(Method) - Api Response
                //sParentTable = "Method";
                //sChildTable = "ApiResponse";
                //DataRelation drMethodApiResponse = new DataRelation("R_method_apiresponse",
                //                                                    new DataColumn[]
                //                                                    {
                //                                                        dsService.Tables[sParentTable].Columns["servicename"],
                //                                                        dsService.Tables[sParentTable].Columns["sectionname"],
                //                                                        dsService.Tables[sParentTable].Columns["seqno"],
                //                                                        dsService.Tables[sParentTable].Columns["specid"],
                //                                                        dsService.Tables[sParentTable].Columns["specname"],
                //                                                        dsService.Tables[sParentTable].Columns["specversion"],
                //                                                        dsService.Tables[sParentTable].Columns["path"],
                //                                                        dsService.Tables[sParentTable].Columns["operationverb"]
                //                                                    },
                //                                                    new DataColumn[]
                //                                                    {
                //                                                        dsService.Tables[sChildTable].Columns["servicename"],
                //                                                        dsService.Tables[sChildTable].Columns["sectionname"],
                //                                                        dsService.Tables[sChildTable].Columns["sequenceno"],
                //                                                        dsService.Tables[sChildTable].Columns["specid"],
                //                                                        dsService.Tables[sChildTable].Columns["specname"],
                //                                                        dsService.Tables[sChildTable].Columns["version"],
                //                                                        dsService.Tables[sChildTable].Columns["path"],
                //                                                        dsService.Tables[sChildTable].Columns["operationverb"]
                //                                                    }, false);
                //dsService.Relations.Add(drMethodApiResponse);


                //////BR(Method) - Api Parameter
                //sParentTable = "Method";
                //sChildTable = "ApiParameter";
                //DataRelation drMethodApiParameter = new DataRelation("R_method_apiparameter",
                //                                   new DataColumn[]
                //                                                    {
                //                                                        dsService.Tables[sParentTable].Columns["servicename"],
                //                                                        dsService.Tables[sParentTable].Columns["sectionname"],
                //                                                        dsService.Tables[sParentTable].Columns["seqno"],
                //                                                        dsService.Tables[sParentTable].Columns["specid"],
                //                                                        dsService.Tables[sParentTable].Columns["specname"],
                //                                                        dsService.Tables[sParentTable].Columns["specversion"],
                //                                                        dsService.Tables[sParentTable].Columns["path"]
                //                                                    },
                //                                                    new DataColumn[]
                //                                                    {
                //                                                        dsService.Tables[sChildTable].Columns["servicename"],
                //                                                        dsService.Tables[sChildTable].Columns["sectionname"],
                //                                                        dsService.Tables[sChildTable].Columns["sequenceno"],
                //                                                        dsService.Tables[sChildTable].Columns["specid"],
                //                                                        dsService.Tables[sChildTable].Columns["specname"],
                //                                                        dsService.Tables[sChildTable].Columns["version"],
                //                                                        dsService.Tables[sChildTable].Columns["path"]
                //                                                    }, false);
                //dsService.Relations.Add(drMethodApiParameter);


                //////BR(Method) - Api Operational parameter
                //sParentTable = "Method";
                //sChildTable = "ApiOperationalParameter";
                //DataRelation drMethodOperationalParameter = new DataRelation("R_method_apioperationalparameter",
                //                                   new DataColumn[]
                //                                                    {
                //                                                        dsService.Tables[sParentTable].Columns["servicename"],
                //                                                        dsService.Tables[sParentTable].Columns["sectionname"],
                //                                                        dsService.Tables[sParentTable].Columns["seqno"],
                //                                                        dsService.Tables[sParentTable].Columns["specid"],
                //                                                        dsService.Tables[sParentTable].Columns["specname"],
                //                                                        dsService.Tables[sParentTable].Columns["specversion"],
                //                                                        dsService.Tables[sParentTable].Columns["path"]
                //                                                    },
                //                                                    new DataColumn[]
                //                                                    {
                //                                                        dsService.Tables[sChildTable].Columns["servicename"],
                //                                                        dsService.Tables[sChildTable].Columns["sectionname"],
                //                                                        dsService.Tables[sChildTable].Columns["sequenceno"],
                //                                                        dsService.Tables[sChildTable].Columns["specid"],
                //                                                        dsService.Tables[sChildTable].Columns["specname"],
                //                                                        dsService.Tables[sChildTable].Columns["version"],
                //                                                        dsService.Tables[sChildTable].Columns["path"]
                //                                                    }, false);
                //dsService.Relations.Add(drMethodOperationalParameter);
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("argument cannot be null"))
                {
                    _logger.WriteLogToFile("DownloadService", "Null Data not Allowed.Null handling should be done in the sp 'vw_netgen_serviceschema_sp_test'");
                }
                if (ex.Message.Contains("constraint cannot be enabled as not all values have corresponding parent values"))
                {
                    _logger.WriteLogToFile("DownloadService", string.Format("Foreign key violation between {0}-{1} tables.", sParentTable, sChildTable));
                }
            }
        }

        private bool WriteModelInfoAsXml(String xml, ObjectType objectType)
        {
            bool bSuccessFlg = false;
            XDocument xdoc;
            string dir, fullFilePath;
            try
            {
                dir = Path.Combine(this._ecrOptions.GenerationPath, "ModelInfo");
                xdoc = XDocument.Parse(xml);

                if (objectType == ObjectType.Service)
                {
                    dir = Path.Combine(dir, "Service");
                    Directory.CreateDirectory(dir);

                    IEnumerable<XElement> services = xdoc.Descendants("Service").ToList();
                    foreach (XElement service in services)
                    {
                        string serviceName = service.Attribute("name").Value;
                        fullFilePath = Path.Combine(dir, serviceName + ".xml");
                        File.WriteAllText(fullFilePath, service.ToString());
                    }
                }
                else if (objectType == ObjectType.Activity)
                {
                    dir = Path.Combine(dir, "Activity");
                    Directory.CreateDirectory(dir);

                    fullFilePath = Path.Combine(dir, "activity.xml");
                    File.WriteAllText(fullFilePath, xml);
                }
                else if (objectType == ObjectType.ErrorHandler)
                {
                    dir = Path.Combine(dir, "Error");
                    Directory.CreateDirectory(dir);

                    fullFilePath = Path.Combine(dir, "Error.xml");
                    File.WriteAllText(fullFilePath, xml);
                }
                return bSuccessFlg;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// Downloads Service Information for the component
        /// </summary>
        public DataSet DownloadService(UserInputs userInputs)
        {
            string sParentTable = string.Empty, sChildTable = string.Empty;
            DataSet dsService = null;
            try
            {
                if (userInputs.DataMode.Equals(DataMode.Model))
                {
                    _logger.WriteLogToFile("DownloadService", "Downloading service information...");

                    dsService = new DataSet("Services");
                    dsService.Namespace = "Services";

                    var parameters = this._dbManager.CreateParameters(2);
                    this._dbManager.AddParamters(parameters, 0, "@Guid", this._guid);
                    this._dbManager.AddParamters(parameters, 1, "@EcrNo", this._ecrOptions.Ecrno);
                    dsService = _dbManager.ExecuteDataSet(CommandType.StoredProcedure, "vw_netgen_serviceschema_sp_test", parameters);

                    //Naming for datatables
                    dsService.Tables[0].TableName = "Service";
                    dsService.Tables[1].TableName = "Segment";
                    dsService.Tables[2].TableName = "DataItem";
                    dsService.Tables[3].TableName = "OutDataItem";
                    dsService.Tables[4].TableName = "ProcessSection";
                    dsService.Tables[5].TableName = "Method";
                    dsService.Tables[6].TableName = "ISMappingDetails";
                    dsService.Tables[7].TableName = "PhysicalParameters";
                    //dsService.Tables[8].TableName = "LogicalParameters";
                    dsService.Tables[8].TableName = "DIPropertyMapping";
                    dsService.Tables[9].TableName = "UnusedDI";
                    //dsService.Tables[11].TableName = "ApiRequest";
                    //dsService.Tables[12].TableName = "ApiResponse";
                    //dsService.Tables[13].TableName = "ApiParameter";
                    //dsService.Tables[14].TableName = "ApiOperationalParameter";

                    //adding relations
                    AddDataRelationToServiceDataset(ref dsService);

                    SetColumnMapping(ref dsService);
                    WriteModelInfoAsXml(dsService.GetXml(), ObjectType.Service);
                }

                else if (userInputs.DataMode.Equals(DataMode.Xml))
                {
                    string dir = Path.Combine(this._ecrOptions.GenerationPath, "ModelInfo", "Service");
                    if (!Directory.Exists(dir))
                        throw new Exception($"Directory unavailable for reading servicexml from {dir}");

                    Ramco.VwPlf.CodeGenerator.DeserializingSchema.Services services = new Ramco.VwPlf.CodeGenerator.DeserializingSchema.Services();
                    foreach (string serviceXmlFile in Directory.GetFiles(dir, "*.xml", SearchOption.TopDirectoryOnly))
                    {
                        XDocument xdoc = XDocument.Load(serviceXmlFile);
                        services.Add((Ramco.VwPlf.CodeGenerator.DeserializingSchema.Service)Common.DeserializeFromXML(typeof(Ramco.VwPlf.CodeGenerator.DeserializingSchema.Service), serviceXmlFile));
                    }

                    StringBuilder sb;
                    Common.SerializeToXML(services, out sb);
                    dsService = new DataSet("Services");
                    dsService.ReadXml(new StringReader(sb.ToString()));

                    ConstructServiceDatasetSchema(ref dsService);
                    AddDataRelationToServiceDataset(ref dsService);
                }

                return dsService;
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("DownloadService->{0}", ex.InnerException != null ? ex.InnerException.Message : ex.Message));
            }
        }

        public void DownloadActivity(UserInputs userInputs)
        {
            DataSet ds = null;
            string sParentTable = string.Empty;
            string sChildTable = string.Empty;
            try
            {
                if (userInputs.DataMode == DataMode.Model)
                {
                    _logger.WriteLogToFile("DownloadActivity", "Downloading activity info for ecr :" + _ecrOptions.Ecrno);
                    var parameters = _dbManager.CreateParameters(5);
                    _dbManager.AddParamters(parameters, 0, "@customer_name", _ecrOptions.Customer);
                    _dbManager.AddParamters(parameters, 1, "@project_name", _ecrOptions.Project);
                    _dbManager.AddParamters(parameters, 2, "@ecrno", _ecrOptions.Ecrno);
                    _dbManager.AddParamters(parameters, 3, "@guid", _guid);
                    _dbManager.AddParamters(parameters, 4, "@component_name", _ecrOptions.Component);
                    ds = _dbManager.ExecuteDataSet(CommandType.StoredProcedure, "vw_netgen_activityschema_sp", parameters);
                }
                else if (userInputs.DataMode == DataMode.Xml)
                {
                    string sActivityXmlFfp = Path.Combine(_ecrOptions.GenerationPath, "ModelInfo", "Activity", "activity.xml");
                    if (!Directory.Exists(Path.GetDirectoryName(sActivityXmlFfp)) || !File.Exists(sActivityXmlFfp))
                        throw new Exception("Directory/File unavailable for reading activity metadata");

                    ds = new DataSet();
                    ds.ReadXml(sActivityXmlFfp);
                }
                ds.DataSetName = "ActivitySchema";
                ds.Tables[0].TableName = "Activities";
                ds.Tables[1].TableName = "ILBO";
                ds.Tables[2].TableName = "TabPage";
                ds.Tables[3].TableName = "Control";
                ds.Tables[4].TableName = "Enumeration";
                ds.Tables[5].TableName = "ControlProperty";
                ds.Tables[6].TableName = "TaskService";
                ds.Tables[7].TableName = "ReportInfo";
                ds.Tables[8].TableName = "EzeeView";
                ds.Tables[9].TableName = "Tree";
                ds.Tables[10].TableName = "TreeInfo";
                ds.Tables[11].TableName = "Chart";
                ds.Tables[12].TableName = "Publication";
                ds.Tables[13].TableName = "Traversal";
                ds.Tables[14].TableName = "Subscription";
                ds.Tables[15].TableName = "Segment";
                ds.Tables[16].TableName = "Dataitem";
                ds.Tables[17].TableName = "Taskdata";
                ds.Tables[18].TableName = "PivotFields";
                ds.Tables[19].TableName = "RichControls"; //doubt
                ds.Tables[20].TableName = "Zoom";
                ds.Tables[21].TableName = "ZoomMapping";
                ds.Tables[22].TableName = "Callout";
                ds.Tables[23].TableName = "CalloutSegments";
                ds.Tables[24].TableName = "CalloutDataItems";
                ds.Tables[25].TableName = "SyncView";


                sParentTable = "Activities";
                sChildTable = "ILBO";
                DataRelation DR_Activity_Ilbo = new DataRelation("R_Activity_Ilbo"
                    , new DataColumn[] { ds.Tables["Activities"].Columns["activityname"] }
                    , new DataColumn[] { ds.Tables["ILBO"].Columns["activityname"] }, false);
                //DR_Activity_Ilbo.Nested = true;
                ds.Relations.Add(DR_Activity_Ilbo);

                sParentTable = "ILBO";
                sChildTable = "TabPage";
                DataRelation DR_Ilbo_Page = new DataRelation("R_Ilbo_Page"
                    , new DataColumn[] { ds.Tables["ILBO"].Columns["ilbocode"] }
                    , new DataColumn[] { ds.Tables["TabPage"].Columns["ilbocode"] }, false);
                //DR_Ilbo_Page.Nested = true;
                ds.Relations.Add(DR_Ilbo_Page);


                sParentTable = "ILBO";
                sChildTable = "Control";
                DataRelation DR_Ilbo_Control = new DataRelation("R_Ilbo_Control"
                    , new DataColumn[] { ds.Tables["ILBO"].Columns["ilbocode"] }
                    , new DataColumn[] { ds.Tables["Control"].Columns["ilbocode"] }, false);
                //DR_Ilbo_Control.Nested = true;
                ds.Relations.Add(DR_Ilbo_Control);

                sParentTable = "TabPage";
                sChildTable = "Control";
                DataRelation DR_Page_Control = new DataRelation("R_Page_Control"
                    , new DataColumn[] { ds.Tables["TabPage"].Columns["ilbocode"], ds.Tables["TabPage"].Columns["tabname"] }
                    , new DataColumn[] { ds.Tables["Control"].Columns["ilbocode"], ds.Tables["Control"].Columns["tabname"] }, false);
                //DR_Page_Control.Nested = true;
                ds.Relations.Add(DR_Page_Control);

                sParentTable = "Control";
                sChildTable = "Enumeration";
                DataRelation DR_Control_Enum = new DataRelation("R_Control_Enum"
                    , new DataColumn[] { ds.Tables["Control"].Columns["ilbocode"], ds.Tables["Control"].Columns["controlid"], ds.Tables["Control"].Columns["viewname"] }
                    , new DataColumn[] { ds.Tables["Enumeration"].Columns["ilbocode"], ds.Tables["Enumeration"].Columns["controlid"], ds.Tables["Enumeration"].Columns["viewname"] }, false);
                //DR_Control_Enum.Nested = true;
                ds.Relations.Add(DR_Control_Enum);

                sParentTable = "Control";
                sChildTable = "ControlProperty";
                DataRelation DR_Control_Property = new DataRelation("R_Control_Property"
                    , new DataColumn[] { ds.Tables["Control"].Columns["ilbocode"], ds.Tables["Control"].Columns["controlid"], ds.Tables["Control"].Columns["viewname"] }
                    , new DataColumn[] { ds.Tables["ControlProperty"].Columns["ilbocode"], ds.Tables["ControlProperty"].Columns["controlid"], ds.Tables["ControlProperty"].Columns["viewname"] }, false);
                //DR_Control_Property.Nested = true;
                ds.Relations.Add(DR_Control_Property);

                sParentTable = "Control";
                sChildTable = "PivotFields";
                DataRelation DR_Control_PivotField = new DataRelation("R_Control_PivotField"
                    , new DataColumn[] { ds.Tables["Control"].Columns["ilbocode"], ds.Tables["Control"].Columns["controlid"], ds.Tables["Control"].Columns["viewname"] }
                    , new DataColumn[] { ds.Tables["PivotFields"].Columns["ilbocode"], ds.Tables["PivotFields"].Columns["controlid"], ds.Tables["PivotFields"].Columns["viewname"] }, false);
                //DR_Control_PivotField.Nested = true;
                ds.Relations.Add(DR_Control_PivotField);

                sParentTable = "Control";
                sChildTable = "SyncView";
                DataRelation DR_Control_SyncView = new DataRelation("R_Control_SyncView"
                    , new DataColumn[] { ds.Tables["Control"].Columns["ilbocode"], ds.Tables["Control"].Columns["controlid"] }
                    , new DataColumn[] { ds.Tables["SyncView"].Columns["ui_name"], ds.Tables["SyncView"].Columns["filter_controlid"] }, false);
                //DR_Control_SyncView.Nested = true;
                ds.Relations.Add(DR_Control_SyncView);

                sParentTable = "ILBO";
                sChildTable = "Zoom";
                DataRelation DR_Ilbo_ParentZoomControl = new DataRelation("R_Ilbo_ParentZoomControl"
                    , new DataColumn[] { ds.Tables["ILBO"].Columns["ilbocode"] }
                    , new DataColumn[] { ds.Tables["Zoom"].Columns["childilbocode"] }, false);
                //DR_Ilbo_ParentZoomControl.Nested = true;
                ds.Relations.Add(DR_Ilbo_ParentZoomControl);

                sParentTable = "Control";
                sChildTable = "Zoom";
                DataRelation DR_Control_Zoom = new DataRelation("R_Control_Zoom"
                    , new DataColumn[] { ds.Tables["Control"].Columns["ilbocode"], ds.Tables["Control"].Columns["controlid"] }
                    , new DataColumn[] { ds.Tables["Zoom"].Columns["parentilbocode"], ds.Tables["Zoom"].Columns["controlid"] }, false);
                //DR_Control_Zoom.Nested = true;
                ds.Relations.Add(DR_Control_Zoom);

                sParentTable = "Zoom";
                sChildTable = "ZoomMapping";
                DataRelation DR_Zoom_Mapping = new DataRelation("R_Zoom_Mapping"
                    , new DataColumn[] { ds.Tables["Zoom"].Columns["parentilbocode"], ds.Tables["Zoom"].Columns["controlid"] }
                    , new DataColumn[] { ds.Tables["ZoomMapping"].Columns["parentilbocode"], ds.Tables["ZoomMapping"].Columns["parentcontrolid"] }, false);

                //DR_Zoom_Mapping.Nested = true;
                ds.Relations.Add(DR_Zoom_Mapping);

                sParentTable = "Control";
                sChildTable = "RichControls";
                DataRelation DR_Control_RichControl = new DataRelation("R_Control_RichControl"
                    , new DataColumn[] { ds.Tables["Control"].Columns["ilbocode"], ds.Tables["Control"].Columns["controlid"] }
                    , new DataColumn[] { ds.Tables["RichControls"].Columns["uiname"], ds.Tables["RichControls"].Columns["controlid"] }, false);
                //DR_Control_RichControl.Nested = true;
                ds.Relations.Add(DR_Control_RichControl);

                sParentTable = "ILBO";
                sChildTable = "TaskService";
                DataRelation DR_Ilbo_TaskService = new DataRelation("R_Ilbo_TaskService"
                    , new DataColumn[] { ds.Tables["ILBO"].Columns["ilbocode"] }
                    , new DataColumn[] { ds.Tables["TaskService"].Columns["ilbocode"] }, false);
                //DR_Ilbo_TaskService.Nested = true;
                ds.Relations.Add(DR_Ilbo_TaskService);

                sParentTable = "ILBO";
                sChildTable = "ReportInfo";
                DataRelation DR_Ilbo_Report = new DataRelation("R_Ilbo_Report"
                    , new DataColumn[] { ds.Tables["ILBO"].Columns["ilbocode"] }
                    , new DataColumn[] { ds.Tables["ReportInfo"].Columns["ilbocode"] }, false);
                //DR_Ilbo_Report.Nested = true;
                ds.Relations.Add(DR_Ilbo_Report);

                sParentTable = "ILBO";
                sChildTable = "EzeeView";
                DataRelation DR_Ilbo_Ezeeview = new DataRelation("R_Ilbo_Ezeeview"
                    , new DataColumn[] { ds.Tables["ILBO"].Columns["ilbocode"] }
                    , new DataColumn[] { ds.Tables["EzeeView"].Columns["ui_name"] }, false);
                //DR_Ilbo_Ezeeview.Nested = true;
                ds.Relations.Add(DR_Ilbo_Ezeeview);

                sParentTable = "TaskService";
                sChildTable = "EzeeView";
                DataRelation DR_Task_Ezeeview = new DataRelation("R_Task_Ezeeview"
                    , new DataColumn[] { ds.Tables["TaskService"].Columns["ilbocode"], ds.Tables["TaskService"].Columns["taskname"] },
                    new DataColumn[] { ds.Tables["EzeeView"].Columns["ui_name"], ds.Tables["EzeeView"].Columns["taskname"] }, false);
                //DR_Task_Ezeeview.Nested = true;
                ds.Relations.Add(DR_Task_Ezeeview);

                sParentTable = "TaskService";
                sChildTable = "ReportInfo";
                DataRelation DR_Task_Report = new DataRelation("R_Task_Report"
                    , new DataColumn[] { ds.Tables["TaskService"].Columns["ilbocode"], ds.Tables["TaskService"].Columns["taskname"] }
                    , new DataColumn[] { ds.Tables["ReportInfo"].Columns["ilbocode"], ds.Tables["ReportInfo"].Columns["taskname"] }, false);
                //DR_Task_Report.Nested = true;
                ds.Relations.Add(DR_Task_Report);

                sParentTable = "ILBO";
                sChildTable = "Tree";
                DataRelation DR_Ilbo_Tree = new DataRelation("R_Ilbo_Tree"
                    , new DataColumn[] { ds.Tables["ILBO"].Columns["ilbocode"] }
                    , new DataColumn[] { ds.Tables["Tree"].Columns["ui_name"] }, false);
                //DR_Ilbo_Tree.Nested = true;
                ds.Relations.Add(DR_Ilbo_Tree);

                sParentTable = "TabPage";
                sChildTable = "Tree";
                DataRelation DR_Page_Tree = new DataRelation("R_Page_Tree"
                    , new DataColumn[] { ds.Tables["TabPage"].Columns["ilbocode"], ds.Tables["TabPage"].Columns["tabname"] }
                    , new DataColumn[] { ds.Tables["Tree"].Columns["ui_name"], ds.Tables["Tree"].Columns["tabname"] }, false);
                //DR_Page_Tree.Nested = true;
                ds.Relations.Add(DR_Page_Tree);

                sParentTable = "TaskService";
                sChildTable = "Tree";
                DataRelation DR_Task_Tree = new DataRelation("R_Task_Tree"
                    , new DataColumn[] { ds.Tables["TaskService"].Columns["ilbocode"], ds.Tables["TaskService"].Columns["taskname"] }
                    , new DataColumn[] { ds.Tables["Tree"].Columns["ui_name"], ds.Tables["Tree"].Columns["taskname"] }, false);
                //DR_Task_Tree.Nested = true;
                ds.Relations.Add(DR_Task_Tree);

                sParentTable = "Tree";
                sChildTable = "TreeInfo";
                DataRelation DR_Tree_TreeInfo = new DataRelation("R_Tree_TreeInfo"
                    , new DataColumn[] { ds.Tables["Tree"].Columns["ui_name"], ds.Tables["Tree"].Columns["treecontrol"] }
                    , new DataColumn[] { ds.Tables["TreeInfo"].Columns["ui_name"], ds.Tables["TreeInfo"].Columns["section_bt_synonym"] }, false);
                //DR_Tree_TreeInfo.Nested = true;
                ds.Relations.Add(DR_Tree_TreeInfo);

                sParentTable = "ILBO";
                sChildTable = "Chart";
                DataRelation DR_Ilbo_Chart = new DataRelation("R_Ilbo_Chart"
                    , new DataColumn[] { ds.Tables["ILBO"].Columns["ilbocode"] }
                    , new DataColumn[] { ds.Tables["Chart"].Columns["ui_name"] }, false);
                //DR_Ilbo_Chart.Nested = true;
                ds.Relations.Add(DR_Ilbo_Chart);

                sParentTable = "TabPage";
                sChildTable = "Chart";
                DataRelation DR_Page_Chart = new DataRelation("R_Page_Chart"
                    , new DataColumn[] { ds.Tables["TabPage"].Columns["ilbocode"], ds.Tables["TabPage"].Columns["tabname"] }
                    , new DataColumn[] { ds.Tables["Chart"].Columns["ui_name"], ds.Tables["Chart"].Columns["tabname"] }, false);
                //DR_Page_Chart.Nested = true;
                ds.Relations.Add(DR_Page_Chart);

                sParentTable = "TaskService";
                sChildTable = "Chart";
                DataRelation DR_Task_Chart = new DataRelation("R_Task_Chart",
                    new DataColumn[] {
                                        ds.Tables["TaskService"].Columns["ilbocode"],
                                        ds.Tables["TaskService"].Columns["taskname"] },
                    new DataColumn[] {  ds.Tables["Chart"].Columns["ui_name"],
                                        ds.Tables["Chart"].Columns["taskname"] }, false);
                //DR_Task_Chart.Nested = true;
                ds.Relations.Add(DR_Task_Chart);

                sParentTable = "ILBO";
                sChildTable = "Publication";
                DataRelation DR_Ilbo_Publication = new DataRelation("R_Ilbo_Publication"
                    , new DataColumn[] { ds.Tables["ILBO"].Columns["ilbocode"] }
                    , new DataColumn[] { ds.Tables["Publication"].Columns["ilbocode"] }, false);
                //DR_Ilbo_Publication.Nested = true;
                ds.Relations.Add(DR_Ilbo_Publication);

                sParentTable = "TaskService";
                sChildTable = "Traversal";
                DataRelation DR_Task_Traversal = new DataRelation("R_Task_Traversal"
                    , new DataColumn[] { ds.Tables["TaskService"].Columns["ilbocode"], ds.Tables["TaskService"].Columns["taskname"] }
                    , new DataColumn[] { ds.Tables["Traversal"].Columns["ilbocode"], ds.Tables["Traversal"].Columns["taskname"] }, false);
                //DR_Task_Traversal.Nested = true;
                ds.Relations.Add(DR_Task_Traversal);

                sParentTable = "TaskService";
                sChildTable = "Subscription";
                DataRelation DR_Task_Subscription = new DataRelation("R_Task_Subscription"
                    , new DataColumn[] { ds.Tables["TaskService"].Columns["ilbocode"], ds.Tables["TaskService"].Columns["taskname"] }
                    , new DataColumn[] { ds.Tables["Subscription"].Columns["ilbocode"], ds.Tables["Subscription"].Columns["taskname"] }, false);
                //DR_Task_Subscription.Nested = true;
                ds.Relations.Add(DR_Task_Subscription);

                sParentTable = "TaskService";
                sChildTable = "Segment";
                DataRelation DR_TaskService_Segment = new DataRelation("R_TaskService_Segment"
                    , new DataColumn[] { ds.Tables["TaskService"].Columns["ilbocode"], ds.Tables["TaskService"].Columns["servicename"] }
                    , new DataColumn[] { ds.Tables["Segment"].Columns["ilbocode"], ds.Tables["Segment"].Columns["servicename"] }, false);
                //DR_TaskService_Segment.Nested = true;
                ds.Relations.Add(DR_TaskService_Segment);

                sParentTable = "segment";
                sChildTable = "Dataitem";
                DataRelation DR_Segment_DataItem = new DataRelation("R_Segment_DataItem"
                    , new DataColumn[] { ds.Tables["segment"].Columns["ilbocode"], ds.Tables["Segment"].Columns["servicename"], ds.Tables["Segment"].Columns["segmentname"] }
                    , new DataColumn[] { ds.Tables["Dataitem"].Columns["ilbocode"], ds.Tables["Dataitem"].Columns["servicename"], ds.Tables["Dataitem"].Columns["segmentname"] }, false);
                //DR_Segment_DataItem.Nested = true;
                ds.Relations.Add(DR_Segment_DataItem);

                sParentTable = "TaskService";
                sChildTable = "Taskdata";
                DataRelation DR_Task_Taskdata = new DataRelation("R_Task_Taskdata",
                    new DataColumn[] { ds.Tables["TaskService"].Columns["ilbocode"], ds.Tables["TaskService"].Columns["taskname"] },
                    new DataColumn[] { ds.Tables["Taskdata"].Columns["ilbocode"], ds.Tables["Taskdata"].Columns["taskname"] }, false);
                //DR_Task_Taskdata.Nested = true;
                ds.Relations.Add(DR_Task_Taskdata);

                sParentTable = "TaskService";
                sChildTable = "Callout";
                DataRelation DR_Task_Callout = new DataRelation("R_Task_Callout"
                    , new DataColumn[] { ds.Tables["TaskService"].Columns["ilbocode"], ds.Tables["TaskService"].Columns["taskname"] }
                    , new DataColumn[] { ds.Tables["Callout"].Columns["uiname"], ds.Tables["Callout"].Columns["taskname"] }, false);
                //DR_Task_Callout.Nested = true;
                ds.Relations.Add(DR_Task_Callout);

                sParentTable = "Callout";
                sChildTable = "CalloutSegments";
                DataRelation DR_Callout_Segments = new DataRelation("R_Callout_Segments"
                    , new DataColumn[] { ds.Tables["Callout"].Columns["uiname"], ds.Tables["Callout"].Columns["taskname"], ds.Tables["Callout"].Columns["calloutname"] }
                    , new DataColumn[] { ds.Tables["CalloutSegments"].Columns["uiname"], ds.Tables["CalloutSegments"].Columns["taskname"], ds.Tables["CalloutSegments"].Columns["calloutname"] }, false);
                //DR_Callout_Segments.Nested = true;
                ds.Relations.Add(DR_Callout_Segments);

                sParentTable = "CalloutSegments";
                sChildTable = "CalloutDataItems";
                DataRelation DR_CalloutSegment_Dataitems = new DataRelation("R_CalloutSegment_Dataitems"
                    , new DataColumn[] { ds.Tables["CalloutSegments"].Columns["uiname"], ds.Tables["CalloutSegments"].Columns["taskname"], ds.Tables["CalloutSegments"].Columns["calloutname"], ds.Tables["CalloutSegments"].Columns["segmentname"] }
                    , new DataColumn[] { ds.Tables["CalloutDataItems"].Columns["uiname"], ds.Tables["CalloutDataItems"].Columns["taskname"], ds.Tables["CalloutDataItems"].Columns["calloutname"], ds.Tables["CalloutDataItems"].Columns["segmentname"] }, false);
                //DR_CalloutSegment_Dataitems.Nested = true;
                ds.Relations.Add(DR_CalloutSegment_Dataitems);

                SetColumnMapping(ref ds);
                _ecrOptions.generation.ActivityInfo = ds;

                if (userInputs.DataMode == DataMode.Model)
                    WriteModelInfoAsXml(_ecrOptions.generation.ActivityInfo.GetXml(), ObjectType.Activity);

                this.PopulateActivityMetadata(this._ecrOptions);

            }
            catch (Exception ex)
            {
                throw new Exception($"DownloadActivity->{(ex.InnerException != null ? ex.InnerException.Message : ex.Message)} while adding relationship between {sParentTable},{sChildTable}");
            }
        }

        private void SetColumnMapping(ref DataSet ds)
        {
            foreach (DataTable dt in ds.Tables)
            {
                foreach (DataColumn dc in dt.Columns)
                {
                    dc.ColumnMapping = MappingType.Attribute;
                }
            }
        }

        public void DownloadResourceInfo(UserInputs userInput)
        {
            _logger.WriteLogToFile("DownloadResourceInfo", "Downloading Error Info from model...");

            DataSet ds;

            try
            {
                ds = null;
                if (userInput.DataMode == DataMode.Model)
                {
                    var parameters = _dbManager.CreateParameters(2);
                    _dbManager.AddParamters(parameters, 0, "@componentname", _ecrOptions.Component);
                    _dbManager.AddParamters(parameters, 1, "@langid", "1");
                    ds = _dbManager.ExecuteDataSet(CommandType.StoredProcedure, "vw_netgen_errorschema_sp", parameters);
                    ds.Tables[0].TableName = "Service";
                    ds.Tables[1].TableName = "Error";
                    ds.Tables[2].TableName = "Placeholder";
                }
                else if (userInput.DataMode == DataMode.Xml)
                {
                    string sErrorInfoXmlFfp = Path.Combine(_ecrOptions.GenerationPath, "ModelInfo", "Error", "Error.xml");
                    if (!File.Exists(sErrorInfoXmlFfp))
                        throw new Exception("Error information xml missing.");

                    ds = new DataSet();
                    ds.ReadXml(sErrorInfoXmlFfp);
                }

                ds.Relations.Add(new DataRelation("error_placeholder"
                    , new DataColumn[] { ds.Tables["Error"].Columns["servicename"], ds.Tables["Error"].Columns["methodid"], ds.Tables["Error"].Columns["sperrorcode"] }
                    , new DataColumn[] { ds.Tables["Placeholder"].Columns["servicename"], ds.Tables["Placeholder"].Columns["methodid"], ds.Tables["Placeholder"].Columns["sperrorcode"] }, true));
                SetColumnMapping(ref ds);
                _ecrOptions.generation.dsResourceInfo = ds;
                _ecrOptions.generation.dsResourceInfo.DataSetName = "ErrorDetails";



                if (userInput.DataMode == DataMode.Model)
                    WriteModelInfoAsXml(_ecrOptions.generation.dsResourceInfo.GetXml(), ObjectType.ErrorHandler);
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("DownloadResourceInfo->{0}", ex.InnerException != null ? ex.InnerException.Message : ex.Message));
            }
        }

        /// <summary>
        /// Drops temp tables
        /// </summary>
        private void DropTables()
        {
            try
            {
                _logger.WriteLogToFile("DropTables", "Dropping Hastables..");

                StringBuilder sb = new StringBuilder();
                sb.AppendLine(string.Format("IF OBJECT_ID('tempdb..#{0}') IS NOT NULL DROP TABLE #{0}", "fw_req_activity"));
                sb.AppendLine(string.Format("IF OBJECT_ID('tempdb..#{0}') IS NOT NULL DROP TABLE #{0}", "fw_req_activity_ilbo"));
                sb.AppendLine(string.Format("IF OBJECT_ID('tempdb..#{0}') IS NOT NULL DROP TABLE #{0}", "fw_req_ilbo"));
                sb.AppendLine(string.Format("IF OBJECT_ID('tempdb..#{0}') IS NOT NULL DROP TABLE #{0}", "fw_req_ilbo_control_property"));
                sb.AppendLine(string.Format("IF OBJECT_ID('tempdb..#{0}') IS NOT NULL DROP TABLE #{0}", "fw_req_ilbo_control"));
                sb.AppendLine(string.Format("IF OBJECT_ID('tempdb..#{0}') IS NOT NULL DROP TABLE #{0}", "fw_req_ilbo_layout_control"));
                sb.AppendLine(string.Format("IF OBJECT_ID('tempdb..#{0}') IS NOT NULL DROP TABLE #{0}", "fw_req_ilbo_view"));
                sb.AppendLine(string.Format("IF OBJECT_ID('tempdb..#{0}') IS NOT NULL DROP TABLE #{0}", "fw_req_bterm"));
                sb.AppendLine(string.Format("IF OBJECT_ID('tempdb..#{0}') IS NOT NULL DROP TABLE #{0}", "fw_req_bterm_synonym"));
                sb.AppendLine(string.Format("IF OBJECT_ID('tempdb..#{0}') IS NOT NULL DROP TABLE #{0}", "fw_req_process_component"));
                sb.AppendLine(string.Format("IF OBJECT_ID('tempdb..#{0}') IS NOT NULL DROP TABLE #{0}", "fw_req_ilbo_tab_properties"));
                sb.AppendLine(string.Format("IF OBJECT_ID('tempdb..#{0}') IS NOT NULL DROP TABLE #{0}", "fw_req_activity_ilbo_task"));
                sb.AppendLine(string.Format("IF OBJECT_ID('tempdb..#{0}') IS NOT NULL DROP TABLE #{0}", "fw_req_task"));
                sb.AppendLine(string.Format("IF OBJECT_ID('tempdb..#{0}') IS NOT NULL DROP TABLE #{0}", "fw_req_bterm_enumerated_option"));
                sb.AppendLine(string.Format("IF OBJECT_ID('tempdb..#{0}') IS NOT NULL DROP TABLE #{0}", "fw_req_ilbo_link_publish"));
                sb.AppendLine(string.Format("IF OBJECT_ID('tempdb..#{0}') IS NOT NULL DROP TABLE #{0}", "fw_req_ilbo_data_publish"));
                sb.AppendLine(string.Format("IF OBJECT_ID('tempdb..#{0}') IS NOT NULL DROP TABLE #{0}", "fw_req_ilbo_data_use"));
                sb.AppendLine(string.Format("IF OBJECT_ID('tempdb..#{0}') IS NOT NULL DROP TABLE #{0}", "fw_req_ilbo_linkuse"));
                sb.AppendLine(string.Format("IF OBJECT_ID('tempdb..#{0}') IS NOT NULL DROP TABLE #{0}", "fw_des_ilbo_services"));
                sb.AppendLine(string.Format("IF OBJECT_ID('tempdb..#{0}') IS NOT NULL DROP TABLE #{0}", "fw_des_service_dataitem"));
                sb.AppendLine(string.Format("IF OBJECT_ID('tempdb..#{0}') IS NOT NULL DROP TABLE #{0}", "fw_asp_codegen_tmp"));
                sb.AppendLine(string.Format("IF OBJECT_ID('tempdb..#{0}') IS NOT NULL DROP TABLE #{0}", "fw_des_service"));
                sb.AppendLine(string.Format("IF OBJECT_ID('tempdb..#{0}') IS NOT NULL DROP TABLE #{0}", "fw_des_service_segment"));
                sb.AppendLine(string.Format("IF OBJECT_ID('tempdb..#{0}') IS NOT NULL DROP TABLE #{0}", "fw_des_ilbo_service_view_datamap"));
                sb.AppendLine(string.Format("IF OBJECT_ID('tempdb..#{0}') IS NOT NULL DROP TABLE #{0}", "fw_des_task_segment_attribs"));
                sb.AppendLine(string.Format("IF OBJECT_ID('tempdb..#{0}') IS NOT NULL DROP TABLE #{0}", "fw_req_task_local_info"));
                sb.AppendLine(string.Format("IF OBJECT_ID('tempdb..#{0}') IS NOT NULL DROP TABLE #{0}", "fw_req_ilbo_link_local_info"));
                sb.AppendLine(string.Format("IF OBJECT_ID('tempdb..#{0}') IS NOT NULL DROP TABLE #{0}", "fw_req_activity_ilbo_task_extension_map"));
                sb.AppendLine(string.Format("IF OBJECT_ID('tempdb..#{0}') IS NOT NULL DROP TABLE #{0}", "fw_req_activity_task"));
                sb.AppendLine(string.Format("IF OBJECT_ID('tempdb..#{0}') IS NOT NULL DROP TABLE #{0}", "fw_req_ilbo_local_info"));
                sb.AppendLine(string.Format("IF OBJECT_ID('tempdb..#{0}') IS NOT NULL DROP TABLE #{0}", "fw_req_ilbo_tabs"));
                sb.AppendLine(string.Format("IF OBJECT_ID('tempdb..#{0}') IS NOT NULL DROP TABLE #{0}", "fw_req_lang_bterm_synonym"));
                sb.AppendLine(string.Format("IF OBJECT_ID('tempdb..#{0}') IS NOT NULL DROP TABLE #{0}", "fw_req_precision"));
                sb.AppendLine(string.Format("IF OBJECT_ID('tempdb..#{0}') IS NOT NULL DROP TABLE #{0}", "fw_req_system_parameters"));
                sb.AppendLine(string.Format("IF OBJECT_ID('tempdb..#{0}') IS NOT NULL DROP TABLE #{0}", "fw_des_be_placeholder"));
                sb.AppendLine(string.Format("IF OBJECT_ID('tempdb..#{0}') IS NOT NULL DROP TABLE #{0}", "fw_des_br_logical_parameter"));
                sb.AppendLine(string.Format("IF OBJECT_ID('tempdb..#{0}') IS NOT NULL DROP TABLE #{0}", "fw_des_brerror"));
                sb.AppendLine(string.Format("IF OBJECT_ID('tempdb..#{0}') IS NOT NULL DROP TABLE #{0}", "fw_des_bro"));
                sb.AppendLine(string.Format("IF OBJECT_ID('tempdb..#{0}') IS NOT NULL DROP TABLE #{0}", "fw_des_businessrule"));
                sb.AppendLine(string.Format("IF OBJECT_ID('tempdb..#{0}') IS NOT NULL DROP TABLE #{0}", "fw_des_context"));
                sb.AppendLine(string.Format("IF OBJECT_ID('tempdb..#{0}') IS NOT NULL DROP TABLE #{0}", "fw_des_di_parameter"));
                sb.AppendLine(string.Format("IF OBJECT_ID('tempdb..#{0}') IS NOT NULL DROP TABLE #{0}", "fw_des_di_placeholder"));
                sb.AppendLine(string.Format("IF OBJECT_ID('tempdb..#{0}') IS NOT NULL DROP TABLE #{0}", "fw_des_error"));
                sb.AppendLine(string.Format("IF OBJECT_ID('tempdb..#{0}') IS NOT NULL DROP TABLE #{0}", "fw_des_integ_serv_map"));
                sb.AppendLine(string.Format("IF OBJECT_ID('tempdb..#{0}') IS NOT NULL DROP TABLE #{0}", "fw_des_processsection"));
                sb.AppendLine(string.Format("IF OBJECT_ID('tempdb..#{0}') IS NOT NULL DROP TABLE #{0}", "fw_des_processsection_br_is"));
                sb.AppendLine(string.Format("IF OBJECT_ID('tempdb..#{0}') IS NOT NULL DROP TABLE #{0}", "fw_des_processsection_br_is_err"));
                sb.AppendLine(string.Format("IF OBJECT_ID('tempdb..#{0}') IS NOT NULL DROP TABLE #{0}", "fw_des_sp"));
                sb.AppendLine(string.Format("IF OBJECT_ID('tempdb..#{0}') IS NOT NULL DROP TABLE #{0}", "fw_des_bo"));
                sb.AppendLine(string.Format("IF OBJECT_ID('tempdb..#{0}') IS NOT NULL DROP TABLE #{0}", "fw_des_svco"));
                sb.AppendLine(string.Format("IF OBJECT_ID('tempdb..#{0}') IS NOT NULL DROP TABLE #{0}", "fw_des_error_placeholder"));
                sb.AppendLine(string.Format("IF OBJECT_ID('tempdb..#{0}') IS NOT NULL DROP TABLE #{0}", "fw_des_reqbr_desbr"));
                sb.AppendLine(string.Format("IF OBJECT_ID('tempdb..#{0}') IS NOT NULL DROP TABLE #{0}", "fw_req_task_rule"));
                sb.AppendLine(string.Format("IF OBJECT_ID('tempdb..#{0}') IS NOT NULL DROP TABLE #{0}", "fw_des_ilbo_placeholder"));
                sb.AppendLine(string.Format("IF OBJECT_ID('tempdb..#{0}') IS NOT NULL DROP TABLE #{0}", "fw_des_ilbo_ctrl_event"));
                sb.AppendLine(string.Format("IF OBJECT_ID('tempdb..#{0}') IS NOT NULL DROP TABLE #{0}", "fw_req_businessrule"));
                sb.AppendLine(string.Format("IF OBJECT_ID('tempdb..#{0}') IS NOT NULL DROP TABLE #{0}", "fw_req_br_error"));
                sb.AppendLine(string.Format("IF OBJECT_ID('tempdb..#{0}') IS NOT NULL DROP TABLE #{0}", "LP_ILBO_Tmp"));
                sb.AppendLine(string.Format("IF OBJECT_ID('tempdb..#{0}') IS NOT NULL DROP TABLE #{0}", "LP_Service_Tmp"));
                sb.AppendLine(string.Format("IF OBJECT_ID('tempdb..#{0}') IS NOT NULL DROP TABLE #{0}", "service_tmp"));
                sb.AppendLine(string.Format("IF OBJECT_ID('tempdb..#{0}') IS NOT NULL DROP TABLE #{0}", "LP_PublishErr_Tmp"));
                sb.AppendLine(string.Format("IF OBJECT_ID('tempdb..#{0}') IS NOT NULL DROP TABLE #{0}", "LP_RVWObjects_tmp"));
                sb.AppendLine(string.Format("IF OBJECT_ID('tempdb..#{0}') IS NOT NULL DROP TABLE #{0}", "fw_req_ILBO_Transpose"));
                sb.AppendLine(string.Format("IF OBJECT_ID('tempdb..#{0}') IS NOT NULL DROP TABLE #{0}", "fw_req_ilbo_task_rpt"));
                sb.AppendLine(string.Format("IF OBJECT_ID('tempdb..#{0}') IS NOT NULL DROP TABLE #{0}", "fw_des_chart_service_segment"));
                sb.AppendLine(string.Format("IF OBJECT_ID('tempdb..#{0}') IS NOT NULL DROP TABLE #{0}", "fw_task_service_map"));
                sb.AppendLine(string.Format("IF OBJECT_ID('tempdb..#{0}') IS NOT NULL DROP TABLE #{0}", "fw_des_focus_control"));
                sb.AppendLine(string.Format("IF OBJECT_ID('tempdb..#{0}') IS NOT NULL DROP TABLE #{0}", "meta_severity"));
                sb.AppendLine(string.Format("IF OBJECT_ID('tempdb..#{0}') IS NOT NULL DROP TABLE #{0}", "fw_req_activity_PLBO"));
                sb.AppendLine(string.Format("IF OBJECT_ID('tempdb..#{0}') IS NOT NULL DROP TABLE #{0}", "fw_req_plbo"));
                sb.AppendLine(string.Format("IF OBJECT_ID('tempdb..#{0}') IS NOT NULL DROP TABLE #{0}", "fw_des_ilbo_focus_control"));
                sb.AppendLine(string.Format("IF OBJECT_ID('tempdb..#{0}') IS NOT NULL DROP TABLE #{0}", "fw_des_service_view_datamap"));
                sb.AppendLine(string.Format("IF OBJECT_ID('tempdb..#{0}') IS NOT NULL DROP TABLE #{0}", "fw_des_ilerror"));
                sb.AppendLine(string.Format("IF OBJECT_ID('tempdb..#{0}') IS NOT NULL DROP TABLE #{0}", "fw_des_err_det_local_info"));
                sb.AppendLine(string.Format("IF OBJECT_ID('tempdb..#{0}') IS NOT NULL DROP TABLE #{0}", "fw_des_corr_action_local_info"));
                sb.AppendLine(string.Format("IF OBJECT_ID('tempdb..#{0}') IS NOT NULL DROP TABLE #{0}", "fw_req_language"));
                sb.AppendLine(string.Format("IF OBJECT_ID('tempdb..#{0}') IS NOT NULL DROP TABLE #{0}", "fw_des_plbo_placeholder"));
                sb.AppendLine(string.Format("IF OBJECT_ID('tempdb..#{0}') IS NOT NULL DROP TABLE #{0}", "fw_req_control_property"));
                sb.AppendLine(string.Format("IF OBJECT_ID('tempdb..#{0}') IS NOT NULL DROP TABLE #{0}", "fw_des_plerror"));
                sb.AppendLine(string.Format("IF OBJECT_ID('tempdb..#{0}') IS NOT NULL DROP TABLE #{0}", "fw_req_activity_local_info"));
                sb.AppendLine(string.Format("IF OBJECT_ID('tempdb..#{0}') IS NOT NULL DROP TABLE #{0}", "fw_req_ilboctrl_initval"));
                sb.AppendLine(string.Format("IF OBJECT_ID('tempdb..#{0}') IS NOT NULL DROP TABLE #{0}", "TmpTable"));
                sb.AppendLine(string.Format("IF OBJECT_ID('tempdb..#{0}') IS NOT NULL DROP TABLE #{0}", "PHTable"));
                sb.AppendLine(string.Format("IF OBJECT_ID('tempdb..#{0}') IS NOT NULL DROP TABLE #{0}", "fw_des_service_dataitem_Applog"));
                sb.AppendLine(string.Format("IF OBJECT_ID('tempdb..#{0}') IS NOT NULL DROP TABLE #{0}", "fw_service_validation_message"));
                sb.AppendLine(string.Format("IF OBJECT_ID('tempdb..#{0}') IS NOT NULL DROP TABLE #{0}", "de_published_service_logic_extn_dtl"));
                sb.AppendLine(string.Format("IF OBJECT_ID('tempdb..#{0}') IS NOT NULL DROP TABLE #{0}", "es_comp_ctrl_type_mst"));
                //sb.AppendLine(string.Format("IF OBJECT_ID('tempdb..#{0}') IS NOT NULL DROP TABLE #{0}", "de_log_ext_ctrl_met"));
                sb.AppendLine(string.Format("IF OBJECT_ID('tempdb..#{0}') IS NOT NULL DROP TABLE #{0}", "de_published_ui_control"));
                sb.AppendLine(string.Format("IF OBJECT_ID('tempdb..#{0}') IS NOT NULL DROP TABLE #{0}", "de_published_ui_grid"));
                sb.AppendLine(string.Format("IF OBJECT_ID('tempdb..#{0}') IS NOT NULL DROP TABLE #{0}", "de_published_ui_page"));
                //sb.AppendLine(string.Format("IF OBJECT_ID('tempdb..#{0}') IS NOT NULL DROP TABLE #{0}", "fw_exrep_task_temp_map"));
                sb.AppendLine(string.Format("IF OBJECT_ID('tempdb..#{0}') IS NOT NULL DROP TABLE #{0}", "fw_des_service_dtl"));
                sb.AppendLine(string.Format("IF OBJECT_ID('tempdb..#{0}') IS NOT NULL DROP TABLE #{0}", "fw_des_service_segment_dtl"));
                sb.AppendLine(string.Format("IF OBJECT_ID('tempdb..#{0}') IS NOT NULL DROP TABLE #{0}", "fw_des_integ_outsegmap_dtl"));
                sb.AppendLine(string.Format("IF OBJECT_ID('tempdb..#{0}') IS NOT NULL DROP TABLE #{0}", "fw_des_integ_segmap_dtl"));
                sb.AppendLine(string.Format("IF OBJECT_ID('tempdb..#{0}') IS NOT NULL DROP TABLE #{0}", "fw_des_processsection_dtl"));
                sb.AppendLine(string.Format("IF OBJECT_ID('tempdb..#{0}') IS NOT NULL DROP TABLE #{0}", "fw_des_caller_integ_serv_map"));
                sb.AppendLine(string.Format("IF OBJECT_ID('tempdb..#{0}') IS NOT NULL DROP TABLE #{0}", "fw_ezeeview_spparamlist"));
                sb.AppendLine(string.Format("IF OBJECT_ID('tempdb..#{0}') IS NOT NULL DROP TABLE #{0}", "fw_ezeeview_sp"));
                sb.AppendLine(string.Format("IF OBJECT_ID('tempdb..#{0}') IS NOT NULL DROP TABLE #{0}", "fw_des_service_dataitem_dtl"));
                sb.AppendLine(string.Format("IF OBJECT_ID('tempdb..#{0}') IS NOT NULL DROP TABLE #{0}", "fw_des_parameter_cnt"));
                sb.AppendLine(string.Format("IF OBJECT_ID('tempdb..#{0}') IS NOT NULL DROP TABLE #{0}", "fw_extjs_control_dtl"));
                sb.AppendLine(string.Format("IF OBJECT_ID('tempdb..#{0}') IS NOT NULL DROP TABLE #{0}", "fw_extjs_link_grid_map"));
                sb.AppendLine(string.Format("IF OBJECT_ID('tempdb..#{0}') IS NOT NULL DROP TABLE #{0}", "fw_des_integ_serv_map_dtl"));
                sb.AppendLine(string.Format("IF OBJECT_ID('tempdb..#{0}') IS NOT NULL DROP TABLE #{0}", "de_listedit_view_datamap"));
                sb.AppendLine(string.Format("IF OBJECT_ID('tempdb..#{0}') IS NOT NULL DROP TABLE #{0}", "de_ezeereport_task_control"));
                sb.AppendLine(string.Format("IF OBJECT_ID('tempdb..#{0}') IS NOT NULL DROP TABLE #{0}", "published_ui"));
                sb.AppendLine(string.Format("IF OBJECT_ID('tempdb..#{0}') IS NOT NULL DROP TABLE #{0}", "de_published_action"));
                sb.AppendLine(string.Format("IF OBJECT_ID('tempdb..#{0}') IS NOT NULL DROP TABLE #{0}", "fw_des_publish_ilbo_service_view_attributemap"));
                sb.AppendLine(string.Format("IF OBJECT_ID('tempdb..#{0}') IS NOT NULL DROP TABLE #{0}", "fw_req_activity_ilbo_task_tabs"));

                sb.AppendLine(string.Format("IF OBJECT_ID('tempdb..#{0}') IS NOT NULL DROP TABLE #{0}", "de_fw_des_publish_task_callout"));
                sb.AppendLine(string.Format("IF OBJECT_ID('tempdb..#{0}') IS NOT NULL DROP TABLE #{0}", "de_fw_des_publish_task_callout_segement"));
                sb.AppendLine(string.Format("IF OBJECT_ID('tempdb..#{0}') IS NOT NULL DROP TABLE #{0}", "de_fw_des_publish_task_callout_dataitem"));
                _dbManager.ExecuteNonQuery(System.Data.CommandType.Text, sb.ToString(), null);
            }
            catch (Exception ex)
            {
                _logger.WriteLogToFile("DropTables", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message);
                throw ex;
            }
        }

        /// <summary>
        /// Sets data related options necessary for generating activity and its ilbo.
        /// </summary>
        /// <returns></returns>
        private bool PopulateActivityMetadata(ECRLevelOptions ecrOptions)
        {
            Boolean bStatus;
            DataTable dtUIList;

            dtUIList = new DataTable();

            try
            {
                //get & set information needed for a activity (skip basecallout activity)
                foreach (Activity activity in _ecrOptions.generation.activities)
                {
                    activity.FillData(ecrOptions);
                }

                //removing invalid activities
                _ecrOptions.generation.activities.Where(a => a.ILBOs.Count == 0).ToList<Activity>().ForEach(a =>
                {
                    ecrOptions.ErrorCollection.Add(new Error(ObjectType.Activity, a.Name, "Invalid Activity."));
                    this._logger.WriteLogToFile("PopulateActivityMetadata", string.Format("WARNING!. Invalid Activity : {0}", a.Name), bError: true);
                });
                _ecrOptions.generation.activities.RemoveAll(a => a.ILBOs.Count == 0);

                //adding activity with basecallout to the collection
                List<Activity> baseCallouts = new List<Activity>();
                foreach (Activity activity in _ecrOptions.generation.activities.Where(a => a.ILBOs.Where(i => i.HasBaseCallout == true).Any()))
                {
                    Activity calloutActivity = new Activity();
                    calloutActivity.Name = activity.Name + "__";
                    calloutActivity.IsBaseCallout = true; //flag to identify that its a callout dll.
                    calloutActivity.SourceDirectory = System.IO.Path.Combine(_ecrOptions.GenerationPath, _ecrOptions.Platform, _ecrOptions.Customer, _ecrOptions.Project, _ecrOptions.Ecrno, "Updated", _ecrOptions.Component, "Source", "ILBO", string.Format("{0}", calloutActivity.Name));
                    calloutActivity.TargetDirectory = System.IO.Path.Combine(_ecrOptions.GenerationPath, _ecrOptions.Platform, _ecrOptions.Customer, _ecrOptions.Project, _ecrOptions.Ecrno, "Updated", _ecrOptions.Component, "Release", "ILBO");
                    calloutActivity.ILBOs = activity.ILBOs;
                    baseCallouts.Add(calloutActivity);
                }

                foreach (Activity baseCallout in baseCallouts)
                {
                    _ecrOptions.generation.activities.Add(baseCallout);
                }

                bStatus = true;
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("PopulateActivityMetadata->{0}", !(object.Equals(ex.InnerException, null)) ? ex.InnerException.Message : ex.Message));
            }

            return bStatus;
        }
    }
}
