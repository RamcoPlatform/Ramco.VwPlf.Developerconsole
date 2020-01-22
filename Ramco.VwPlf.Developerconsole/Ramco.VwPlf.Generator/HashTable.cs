//-----------------------------------------------------------------------
// <copyright file="HashTable.cs" company="Ramco Systems">
//     Copyright (c) . All rights reserved.
// Release id 	: PLF2.0_02398
// Description 	: Coded for desktop generator.
// By			: Karthikeyan V S
// Date 		: 23-Nov-2012
//-----------------------------------------------------------------------
// Bug id 	    : PLF2.0_03462
// Description 	: Desktop code generation changes for adding default 
//                enumerated value in schema.
// By			: Vidhyalakshmi N R
// Date 		: 14-Feb-2013
//-----------------------------------------------------------------------
// Bug id 	    : PLF2.0_03791
// Description  : Task name duplication while generating screen xml
// By			: Vidhyalakshmi N R
// Date 		: 11-Mar-2013
//-----------------------------------------------------------------------
// Bug id 	    : PLF2.0_03803
// Description  : For Error Place Holders ^ Control Caption should be part 
//			      of Error Message
// By			: Karthikeyan V S
// Date 		: 09-Apr-2013
//-----------------------------------------------------------------------
// Bug id 	    : PLF2.0_04389
// Description  : Inclusion of eventname attribute in subscription link.
// By			: Vidhyalakshmi N R
// Date 		: 18-Apr-2013
//-----------------------------------------------------------------------
// Bug id 	    : PLF2.0_04462
// Description  : Desktop code generation fix for modeflag.
// By			: Vidhyalakshmi N R
// Date 		: 26-Apr-2013
//-----------------------------------------------------------------------
// Bug id 	    : PLF2.0_06579
// Description  : Publish / Subscribe Issue. Join for Dataitem in the update statement for  
//                fw_req_ilbo_data_use added
// By			: Ramachandran T
// Date 		: 11-Nov-2013
//-----------------------------------------------------------------------
// Bug id 	    : PLF2.0_15017
// Description  : lowercase for parameter name is removed
// By			: Madhan Sekar M
// Date 		: 21-Sep-2015
//-----------------------------------------------------------------------
// Bug id 	    : TECH-18686
// Description  : query problem while populating error shema
// By			: Madhan Sekar M
// Date 		: 09-Feb-2018
//-----------------------------------------------------------------------
// Bug id 	    : TECH-33808
// Description  : btsynonym name for control and view
// By			: Madhan Sekar M
// Date 		: 14-May-2019
//-----------------------------------------------------------------------
// Bug id 	    : TECH-36179
// Description  : modeflag di node duplication in svcui xml 
// By			: Madhan Sekar M
// Date 		: 22-Jul-2019
//-----------------------------------------------------------------------
// </copyright>
//-----------------------------------------------------------------------
namespace Ramco.VwPlf.Generator
{
    using System;
    using System.Text;
    
    /// <summary>
    /// 
    /// </summary>
    public class HashTable : Common
    {
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public bool HashTableInsert()
        {
            try
            {
                if (OpenConnection())
                {
                    this.CreateTables();
                    this.InsertTables();
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception e)
            {
                WriteProfiler("Exception raised in HashTableInsert.", e.Message);
                return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private bool PublishECRUpdates()
        {
            try
            {
                WriteProfiler("De_il_PublishRVWObjects_upd Sp Start.");
                ExecuteNonQueryResult(string.Format("exec De_il_PublishRVWObjects_upd '{0}'", GlobalVar.Component));
                WriteProfiler("De_il_PublishRVWObjects_upd Sp End.");
                WriteProfiler("de_il_fw_des_iu_save_service_ecr_update Sp Start.");
                ExecuteNonQueryResult(string.Format("exec de_il_fw_des_iu_save_service_ecr_update '{0}', '1', 'PUBLISH', 1", GlobalVar.Component));
                WriteProfiler("de_il_fw_des_iu_save_service_ecr_update Sp End.");
                return true;
            }
            catch (Exception e)
            {
                WriteProfiler("Exception raised in PublishECRUpdates.", e.Message);
                return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private bool InsertITK()
        {
            try
            {
                string sFg = "n";
                string sQL = "n";
                if (GlobalVar.Snippet) sFg = "y";
                if (GlobalVar.QuickLink) sQL = "y";
                WriteProfiler("ITK engg_dc_logcl_extn_ins sp execution start.");
                ExecuteNonQueryResult(string.Format("exec engg_dc_logcl_extn_ins '{0}', '{1}', '{2}', '{3}', '{4}'", GlobalVar.Customer, GlobalVar.Project, GlobalVar.Ecrno, sFg, sQL));
                WriteProfiler("ITK engg_dc_logcl_extn_ins sp execution end.");
                return true;
            }
            catch (Exception exSql)
            {
                WriteProfiler("Exception raised while inserting Logical Extension data for EcrNo : " + GlobalVar.Ecrno + " " + exSql.Message);
                return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private void CreateTables()
        {
            try
            {
                WriteProfiler("Creating Hashtables started.");
                StringBuilder sbQuery = new StringBuilder();
                sbQuery.AppendLine("create table #fw_req_activity (activitydesc varchar(60),activityid int,activityname varchar(60),activityposition int, activitysequence int,activitytype int,");
                sbQuery.AppendLine("iswfenabled int,ComponentName varchar(60), UpdUser varchar(60), UpdTime datetime)");
                sbQuery.AppendLine("create table #fw_req_activity_ilbo (activityid int,ilbocode varchar(60),activityname varchar(60), UpdUser varchar(60), UpdTime datetime)");
                sbQuery.AppendLine("create table #fw_req_ilbo (aspofilepath varchar(255),description varchar(255),");
                sbQuery.AppendLine("ilbocode varchar(60),ilbotype int,progid varchar(60), statusflag int, UpdUser varchar(60), UpdTime datetime)");
                sbQuery.AppendLine("create table #fw_req_activity_task (ActivityId varchar(60), TaskName varchar(60), InvocationType tinyint, TaskSequence int, UpdUser varchar(60), UpdTime datetime)");
                sbQuery.AppendLine("create table #fw_req_ilbo_local_info (ilbocode varchar(60), Langid tinyint, Description varchar(255), HelpIndex int, UpdUser varchar(60), UpdTime datetime)");
                sbQuery.AppendLine("create table #fw_req_ilbo_tabs (ILBOCode varchar(60), TabName varchar(60), BTSynonym varchar(60), UpdUser varchar(60), UpdTime datetime)");
                sbQuery.AppendLine("create table #fw_req_lang_bterm_synonym (BTSynonym varchar(60), Langid tinyint, ForeignName varchar(120), LongPLText nvarchar(240), ShortPLText nvarchar(200), ShortDesc nvarchar(510),");
                sbQuery.AppendLine("LongDesc varchar(510), UpdUser varchar(60), UpdTime datetime)");
                sbQuery.AppendLine("create table #fw_req_precision (PrecisionType varchar(60), TotalLength int, DecimalLength int, UpdUser varchar(60), UpdTime datetime)");
                sbQuery.AppendLine("create table #fw_req_ilbo_control_property (controlid varchar(60),ilbocode varchar(255), propertyname varchar(60),type varchar(60),");
                sbQuery.AppendLine("value varchar(255),viewname varchar(60), UpdUser varchar(60), UpdTime datetime)");
                sbQuery.AppendLine("create table #fw_req_ilbo_control (btsynonym varchar(255),controlid varchar(60), ilbocode varchar(255),tabname varchar(60),type varchar(60), listedit varchar(5), UpdUser varchar(60), UpdTime datetime)");
                sbQuery.AppendLine("create table #fw_req_ilbo_view (btsynonym varchar(60),controlid varchar(60),displayflag varchar(5), displaylength int,ilbocode varchar(200),");
                sbQuery.AppendLine("viewname varchar(60), UpdUser varchar(60), UpdTime datetime, le_pop varchar(5), seq_no int, isItkCtrl char(1))");
                sbQuery.AppendLine("create table #fw_req_bterm(btdesc varchar(250),btname varchar(60), datatype varchar(20),isbterm int,length int,");
                sbQuery.AppendLine("maxvalue varchar(250),minvalue varchar(250),precisiontype varchar(20), UpdUser varchar(60), UpdTime datetime)");
                sbQuery.AppendLine("create table #fw_req_bterm_synonym(btname varchar(60),btsynonym varchar(60), UpdUser varchar(60), UpdTime datetime)");
                sbQuery.AppendLine("create table #fw_req_process_component(componentdesc varchar(255),parentprocess varchar(255), componentname varchar(60),");
                sbQuery.AppendLine("sequenceno int, UpdUser varchar(60), UpdTime datetime)");
                sbQuery.AppendLine("create table #fw_req_ilbo_tab_properties(ilbocode varchar(200), propertyname varchar(64),tabname varchar(60),value varchar(250), UpdUser varchar(60), UpdTime datetime)");
                sbQuery.AppendLine("create table #fw_req_activity_ilbo_task(activityid int, datasavingtask varchar(100), ilbocode varchar(200), linktype int,");
                sbQuery.AppendLine("taskname varchar(100), activityname varchar(60), Taskconfirmation int, usageid varchar(60),ddt_control_id varchar(60),ddt_view_name varchar(60), UpdUser varchar(60), UpdTime datetime,Autoupload varchar(5))"); //TECH-16278
                sbQuery.AppendLine("create table #fw_req_task(taskdesc varchar(260),taskname varchar(100),tasktype varchar(20), UpdUser varchar(60), UpdTime datetime)");
                sbQuery.AppendLine("create table #fw_req_bterm_enumerated_option(btname varchar(60),langid int, optioncode varchar(30), optiondesc nvarchar(160), sequenceno int, UpdUser varchar(60), UpdTime datetime)");
                sbQuery.AppendLine("create table #fw_req_ilbo_link_publish(activityid int,description varchar(512), ilbocode varchar(200), linkid int,");
                sbQuery.AppendLine("taskname varchar(100), UpdUser varchar(60), UpdTime datetime)");
                sbQuery.AppendLine("create table #fw_req_ilbo_data_publish(controlid varchar(60), controlvariablename varchar(60), dataitemname varchar(60), flowtype int, havemultiple int,");
                sbQuery.AppendLine("ilbocode varchar(200),iscontrol int, linkid int, mandatoryflag int, viewname varchar(60), UpdUser varchar(60), UpdTime datetime)");
                sbQuery.AppendLine("create table #fw_req_ilbo_data_use(childilbocode varchar(200), controlid varchar(60), controlvariablename varchar(60), dataitemname varchar(60),");
                sbQuery.AppendLine("flowtype int, iscontrol int, linkid int, parentilbocode varchar(200), primarydata varchar(5), retrievemultiple int, taskname varchar(100), viewname varchar(60), UpdUser varchar(60), UpdTime datetime)");
                sbQuery.AppendLine("create table #fw_req_ilbo_linkuse (childilbocode varchar(200), childorder int, linkid int, parentilbocode varchar(200),");
                sbQuery.AppendLine("taskname varchar(100), posttask varchar(255), UpdUser varchar(60), UpdTime datetime)");
                sbQuery.AppendLine("create table #fw_des_ilbo_services (ilbocode varchar(200), isprepopulate tinyint, servicename varchar(64), UpdUser varchar(60), UpdTime datetime)");
                sbQuery.AppendLine("create table #fw_asp_codegen_tmp(ilbocode varchar(60), IdentityId varchar(120), SerialNo int, Position varchar(120), ControlID varchar(64),");
                sbQuery.AppendLine("Type varchar(120), LineNum int, AttributeName varchar(120), InitVal varchar(120), Script varchar(4000) )");
                sbQuery.AppendLine("create table #fw_req_system_parameters (paramname varchar(30), paramvalue varchar(80), timestamp int,");
                sbQuery.AppendLine("createdby varchar(60), createddate datetime, modifiedby varchar(60), modifieddate datetime, paramdesc varchar(255))");
                sbQuery.AppendLine("create table #fw_des_service_dataitem(dataitemname varchar(60), defaultvalue varchar(60), flowattribute int,ispartofkey int,mandatoryflag int,");
                sbQuery.AppendLine("segmentname varchar(60), servicename varchar(60), itk_dataitem varchar(2), UpdUser varchar(60), UpdTime datetime, controlid varchar(40) )");
                sbQuery.AppendLine("create table #fw_des_be_placeholder(errorid int, methodid int, parametername varchar(60),placeholdername varchar(60))");
                sbQuery.AppendLine("create table #fw_des_br_logical_parameter ( methodid int, logicalparametername varchar(60),logicalparamseqno smallint,recordsetname varchar(60),rssequenceno smallint,flowdirection smallint,");
                sbQuery.AppendLine("btname varchar(60),spparametertype varchar(20), controlid varchar(40))");
                sbQuery.AppendLine("create table #fw_des_brerror (errorid int, methodid int, sperrorcode varchar(12))");
                sbQuery.AppendLine("create table #fw_des_bro (broname varchar(100), brodescription varchar(256), componentname varchar(60), broscope tinyint, brotype tinyint,");
                sbQuery.AppendLine("clsid varchar(40),clsname varchar(40),dllname varchar(510),progid varchar(510),systemgenerated tinyint)");
                sbQuery.AppendLine("create table #fw_des_businessrule(accessesdatabase tinyint,bocode varchar(100), broname varchar(100), dispid varchar(510),isintegbr tinyint, methodid int, ");
                sbQuery.AppendLine("methodname varchar(510), operationtype tinyint,statusflag tinyint,systemgenerated tinyint)");
                sbQuery.AppendLine("create table #fw_des_context(correctiveaction varchar(4000),errorcontext varchar(510),errorid int, severityid tinyint)");
                sbQuery.AppendLine("create table #fw_des_di_parameter(dataitemname varchar(60),methodid int, parametername varchar(60), sectionname varchar(60),");
                sbQuery.AppendLine("segmentname varchar(60), sequenceno int, servicename varchar(64), old_seq_no int, old_seq_no_ins int, le_pop varchar(5))");
                sbQuery.AppendLine("create table #fw_des_di_placeholder(dataitemname varchar(60),errorid  int, methodid int, placeholdername varchar(60),");
                sbQuery.AppendLine("sectionname varchar(60), segmentname varchar(60), sequenceno int, servicename varchar(64) , old_seq_no int, old_seq_no_ins int)");
                sbQuery.AppendLine("create table #fw_des_error (componentname varchar(60), defaultcorrectiveaction varchar(4000),defaultseverity tinyint, detaileddesc varchar(2000), displaytype varchar(30),errorid  int, ");
                sbQuery.AppendLine("errormessage nvarchar(1020), errorsource tinyint,reqerror varchar(40))");
                sbQuery.AppendLine("create table #fw_des_integ_serv_map (callingdataitem varchar(60), callingsegment varchar(60), callingservicename varchar(64), integdataitem varchar(60),");
                sbQuery.AppendLine("integsegment varchar(60), integservicename varchar(64), sectionname varchar(60), sequenceno int, old_seq_no int, old_seq_no_ins int)");
                sbQuery.AppendLine("create table #fw_des_processsection( controlexpression varchar(256),processingtype tinyint,sectionname varchar(60), sectiontype tinyint, sequenceno int, servicename varchar(64), old_seq_no int, old_seq_no_ins int)");

                sbQuery.AppendLine("create table #fw_des_processsection_br_is (connectivityflag tinyint, controlexpression varchar(256),executionflag tinyint, integservicename varchar(64) , isbr tinyint, methodid int  ,");
                sbQuery.AppendLine("sectionname varchar(60) , sequenceno int , servicename varchar(64), old_seq_no int, old_seq_no_ins int, Method_Ext char(2), methodid_ref int, methodname_ref varchar(64), sequenceno_ref int, sectionname_ref varchar(64), ps_sequenceno_ref int )");
                sbQuery.AppendLine("create table #fw_des_processsection_br_is_err (connectivityflag tinyint, controlexpression varchar(256),executionflag tinyint, integservicename varchar(64) , isbr tinyint, methodid int,");
                sbQuery.AppendLine("sectionname varchar(60) not null, sequenceno int not null, servicename varchar(64) not null)");
                sbQuery.AppendLine("create table #fw_des_sp (methodid int  ,sperrorprotocol tinyint, spname varchar(60))");
                sbQuery.AppendLine("create table #fw_des_bo ( BOCode varchar(64)  , ComponentName varchar(64) , BODesc varchar(255) , StatusFlag int ) ");
                sbQuery.AppendLine("create table #fw_des_svco( SVCOName varchar(60) , SVCODescription varchar(255) , ComponentName varchar(60) , SVCOScope int ,SVCOType int ,DLLName varchar(60) ,ProgID varchar(255) ) ");
                sbQuery.AppendLine("create table #fw_des_error_placeholder (ErrorID int, PlaceholderName varchar(60))");
                sbQuery.AppendLine("create table #fw_des_reqbr_desbr ( ReqBRName varchar(60), MethodID int )");
                sbQuery.AppendLine("create table #fw_req_task_rule (TaskName varchar(60), BRSequence int, BRName varchar(60), InvocationType tinyint) ");
                sbQuery.AppendLine("create table #fw_des_ilbo_placeholder (ilbocode varchar(60), ControlID varchar(64), EventName varchar(60), PlaceholderName varchar(60),");
                sbQuery.AppendLine("IsControl tinyint, ControlName varchar(60), ViewName varchar(60), VariableName varchar(60), ErrorID varchar(40), CtrlEvent_ViewName varchar(60)) ");
                sbQuery.AppendLine("create table #fw_des_ilbo_ctrl_event  (ILBOCode varchar(60), ControlID varchar(60), EventName varchar(60),  TaskName varchar(60),");
                sbQuery.AppendLine("MethodName varchar(60), CreateStubFlag tinyint, ViewName varchar(60))");
                sbQuery.AppendLine("create table #fw_req_businessrule (BRName varchar(60), BRType varchar(20), BRDesc varchar(510)) ");
                sbQuery.AppendLine("create table #fw_req_br_error ( BRName varchar(60), ErrorCode varchar(60))");
                sbQuery.AppendLine("create table #fw_req_ILBO_Transpose ( ComponentName varchar(60) NOT NULL, ActivityName varchar(60) NOT NULL, [ILBOCode] varchar(200) NOT NULL, [ControlID] varchar(64) NOT NULL, [ZoomName] varchar(13) NOT NULL, [Version] varchar(7) NOT NULL ,UpdUser varchar(60), UpdTime datetime)");
                sbQuery.AppendLine("create table #fw_req_ilbo_task_rpt ( ilbocode varchar(60),taskname varchar(60),PageName varchar(60),ProcessingType varchar(60),ContextName varchar(4000),");
                sbQuery.AppendLine("ReportType varchar(60))");
                sbQuery.AppendLine("Create table #fw_des_chart_service_segment (component_name varchar(60), chart_id varchar(60), chart_section varchar(60), servicename varchar(60), segmentname varchar(60), instanceflag int)");
                sbQuery.AppendLine("Create table #fw_task_service_map (activity_name varchar(60) ,service_name varchar(60), task_name varchar(60),ui_name varchar(60) )");
                sbQuery.AppendLine("create table #fw_des_service ( isintegser tinyint, processingtype tinyint, servicename varchar(60), servicetype int,statusflag int,");
                sbQuery.AppendLine("svconame varchar(60), componentname varchar(60), UpdUser varchar(60), UpdTime datetime)");
                sbQuery.AppendLine("create table #fw_des_service_segment (bocode varchar(60), bosegmentname varchar(60), instanceflag int, mandatoryflag int, parentsegmentname varchar(60),");
                sbQuery.AppendLine("segmentname varchar(60), servicename varchar(60), SegmentSequence int, UpdUser varchar(60), UpdTime datetime, process_selrows varchar(2))");
                sbQuery.AppendLine("create table #fw_des_ilbo_service_view_datamap (activityid  int, controlid  varchar(60), dataitemname varchar(60), ilbocode  varchar(200), iscontrol tinyint,");
                sbQuery.AppendLine("segmentname varchar(60), servicename varchar(64), taskname varchar(100), variablename varchar(60), viewname varchar(60), btsynonym varchar(255) ,UpdUser varchar(60), UpdTime datetime)"); //TECH-33808
                sbQuery.AppendLine("create table #fw_des_task_segment_attribs (activityid int, ilbocode varchar(200), taskname  varchar(100),servicename  varchar(64), segmentname  varchar(60),combofill  int, displayflag varchar(5), UpdUser varchar(60), UpdTime datetime)");
                sbQuery.AppendLine("create table #fw_req_task_local_info (langid tinyint, taskname  varchar(100), description  varchar(512), helpindex int, UpdUser varchar(60), UpdTime datetime)");
                sbQuery.AppendLine("create table #fw_req_ilbo_link_local_info (LinkID int, ilbocode varchar(60), Langid tinyint, Description varchar(255), HelpIndex int, UpdUser varchar(60), UpdTime datetime)");
                sbQuery.AppendLine("create table #fw_req_activity_ilbo_task_extension_map (componentname  varchar(60), activityid int, ilbocode varchar(60), taskname varchar(60), resultantaspname varchar(512), sessionvariable varchar(60) , UpdUser varchar(60), UpdTime datetime)");

                sbQuery.AppendLine("create table #LP_RVWObjects_tmp(ObjectType NVARCHAR(60), ObjectCode NVARCHAR(200) )");
                sbQuery.AppendLine("create table #LP_ILBO_Tmp(ILBOCode NVARCHAR(300),processFlag  NVARCHAR(300), publishflag  NVARCHAR(300))");
                sbQuery.AppendLine("create table #LP_Service_Tmp( ServiceName NVARCHAR(100), ServiceLevel INT, ServiceFlag INT)");
                sbQuery.AppendLine("create table #service_tmp (ServiceName  NVARCHAR(100), processflag NVARCHAR(100), publishflag NVARCHAR(100))");
                sbQuery.AppendLine("create table #LP_PublishErr_Tmp( ObjectType NVARCHAR(100),  ObjectCode NVARCHAR(200), ErrorID INT)");

                sbQuery.AppendLine("create table #fw_des_focus_control (ErrorID int, ErrorContext varchar(60), ControlID varchar(60),  SegmentName varchar(60), FocusDataItem varchar(60), UpdUser varchar(60) , UpdTime datetime)");
                sbQuery.AppendLine("create table #meta_severity (SeverityID tinyint, SeverityDesc varchar(255), DefButton varchar(100), ErrorSource varchar(120), ButtonText varchar(100) )");
                sbQuery.AppendLine("create table #fw_req_activity_PLBO(ActivityId varchar(100), PLBOCode varchar(100) )");
                sbQuery.AppendLine("create table #fw_req_plbo (PLBOCode varchar(100), Description varchar(510), PLBOType tinyint, ProgID varchar(256), StatusFlag tinyint)");
                sbQuery.AppendLine("create table #fw_des_ilbo_focus_control (ErrorID int, ErrorContext varchar(510), ControlID varchar(60), SegmentName varchar(60),  FocusDataItem varchar(60) )");
                sbQuery.AppendLine("create table #fw_des_service_view_datamap (PLBOCode varchar(100), ServiceName varchar(64), TaskName varchar(100), SegmentName varchar(60), DataItemName varchar(60), ");
                sbQuery.AppendLine("IsControl tinyint, ControlID varchar(60), ViewName varchar(60), VariableName varchar(60) )");
                sbQuery.AppendLine("create table #fw_des_ilerror (ILBOCode varchar(100), ControlID varchar(60), EventName varchar(100), LocalErrorNo int, ErrorID int, ViewName varchar(60))");
                sbQuery.AppendLine("create table #fw_des_err_det_local_info (ErrorID int, ErrorMessage nvarchar(1020), DetailedDesc varchar(4000), LangId tinyint)");
                sbQuery.AppendLine("create table #fw_des_corr_action_local_info (ErrorID varchar(60), ErrorContext varchar(510), CorrectiveAction varchar(4000), LangId tinyint)");
                sbQuery.AppendLine("create table #de_log_ext_ctrl_met(le_customer_name varchar(60), le_project_name varchar(60), le_control varchar(60), le_ctrl_clf  varchar(5), le_data_type  varchar(60), le_data_length int, le_btname varchar(60), le_precisiontype varchar(60), le_decimallength int, col_seq int, le_control_type varchar(60) )");
                sbQuery.AppendLine("create table #es_comp_ctrl_type_mst(customer_name varchar(60), project_name varchar(60), req_no varchar(60), process_name varchar(60), component_name varchar(60), ctrl_type_name varchar(60), ctrl_type_descr varchar(255), base_ctrl_type varchar(60), ");
                sbQuery.AppendLine("mandatory_flag varchar(5), visisble_flag varchar(5), editable_flag varchar(5), caption_req varchar(5), select_flag  varchar(5), zoom_req varchar(5), insert_req varchar(5), delete_req varchar(5), help_req varchar(5), ");
                sbQuery.AppendLine("event_handling_req  varchar(5), ellipses_req varchar(5), comp_ctrl_type_sysid varchar(40), timestamp int, createdby varchar(30), createddate datetime, modifiedby varchar(30), modifieddate datetime, visisble_rows varchar(5), ctrl_type_doc varchar(4000), caption_alignment varchar(8), ");
                sbQuery.AppendLine("caption_wrap varchar(5), caption_position varchar(8), ctrl_position varchar(8), label_class varchar(60), ctrl_class varchar(60), password_char varchar(5), tskimg_class varchar(60), hlpimg_class varchar(60), disponlycmb_req varchar(5), ");
                sbQuery.AppendLine("html_txt_area varchar(5), report_req varchar(5), auto_tab_stop varchar(5), spin_required varchar(5), spin_up_image varchar(60), spin_down_image varchar(60), Extjs_Ctrl_type varchar(60) )");

                sbQuery.AppendLine("create table #de_published_service_logic_extn_dtl(component_name varchar(60), servicename varchar(60), section_name varchar(60), methodid varchar(60), methodname varchar(60), br_sequence int, rdbms_type varchar(60), ext_as varchar(60), ext_before varchar(5), ");
                sbQuery.AppendLine("ext_separate_ps varchar(5), ext_object_name varchar(60), extend_flag varchar(5), status_flag varchar(5), methodtype varchar(5) )");
                sbQuery.AppendLine("Create Table #fw_req_language (customername  varchar(60), projectname  varchar(60), ecrno  varchar(60), langid int, langdesc varchar(225) )");
                sbQuery.AppendLine("create table #fw_des_plbo_placeholder (PLBOCode varchar(60), ControlID varchar(60), EventName varchar(60), PlaceholderName varchar(60), ControlName varchar(60), ViewName varchar(60), ErrorID int, CtrlEvent_ViewName varchar(60) )");
                sbQuery.AppendLine("create table #fw_req_control_property (PLBOCode varchar(60), ControlID varchar(60), ViewName varchar(60), PropertyName varchar(60), Type varchar(20), Value varchar(510) )");
                sbQuery.AppendLine("create table #fw_des_plerror (PLBOCode varchar(60), ControlID varchar(60), EventName varchar(60), LocalErrorNo int, ErrorID int, ViewName varchar(60) )");
                sbQuery.AppendLine("create table #fw_req_activity_local_info ( activityid int, langid int, activitydesc varchar(255) , helpindex int, tooltiptext varchar(255) )");
                sbQuery.AppendLine("create table #fw_req_ilboctrl_initval (ilbocode varchar(60), ControlID varchar(64), ViewName varchar(60),  sequenceNo int, InitialValue varchar(510), UpdUser varchar(64), UpdTime datetime)");
                sbQuery.AppendLine("create table #fw_service_validation_message (service_name varchar(60), languageid int, segmentname varchar(60), dataitemname varchar(60), validation_code varchar(60), message_code int, message_doc varchar(255), value1 varchar(255), value2 varchar(255), order_of_sequence int)");
                sbQuery.AppendLine("create table #de_published_ui_control(component_name varchar(60), activity_name varchar(60), control_bt_synonym varchar(60), control_doc varchar(400), control_id varchar(60), control_prefix varchar(6), control_type varchar(60), ");
                sbQuery.AppendLine("data_column_width int, horder int, label_column_width int, order_seq int, page_bt_synonym varchar(60), proto_tooltip varchar(255), sample_data varchar(4000), section_bt_synonym varchar(60), timestamp int, ui_control_sysid varchar(40), ui_name varchar(60), ");
                sbQuery.AppendLine("ui_section_sysid varchar(40), view_name varchar(60), visisble_length numeric(5), vorder int, label_column_scalemode varchar(6), data_column_scalemode varchar(6), tab_seq int, help_tabstop varchar(5), LabelClass varchar(60), ControlClass varchar(60), ");
                sbQuery.AppendLine("LabelImageClass varchar(60), ControlImageClass varchar(60), label_control_id varchar(60), req_no varchar(60) )");
                sbQuery.AppendLine("create table #de_published_ui_grid(activity_name varchar(60), column_bt_synonym varchar(60), column_no int, column_prefix varchar(6), column_type varchar(60), ");
                sbQuery.AppendLine("col_doc varchar(4000), component_name varchar(60), control_bt_synonym varchar(60), control_id varchar(60), grid_sysid varchar(40), ");
                sbQuery.AppendLine("page_bt_synonym varchar(60), proto_tooltip varchar(60), sample_data varchar(4000), section_bt_synonym varchar(255), timestamp int, ");
                sbQuery.AppendLine("ui_control_sysid varchar(40), ui_name varchar(60), view_name varchar(60), visible_length numeric(5), req_no varchar(60) )");
                sbQuery.AppendLine("create table #de_published_ui_page(activity_name varchar(60), component_name varchar(60), horder int, page_bt_synonym varchar(30), page_doc varchar(4000), ");
                sbQuery.AppendLine("page_prefix varchar(6), timestamp int, ui_name varchar(60), ui_page_sysid varchar(40), ui_sysid varchar(40), vorder int, req_no varchar(60) )");
                sbQuery.AppendLine("create table #fw_exrep_task_temp_map (activity_name varchar(60), ui_name varchar(60), page_name varchar(60), task_name varchar(60), template_id varchar(60), control_name varchar(60), control_page_name varchar(60), control_id varchar(60), view_name varchar(60) )");
                sbQuery.AppendLine("create table #de_listedit_view_datamap(activity_name varchar(60), ilbocode varchar(60), controlid varchar(60), viewname varchar(60), listedit varchar(60), instance int)");
                sbQuery.AppendLine("create table #de_ezeereport_task_control(component_name varchar(60), activity_name varchar(60), ui_name varchar(60), page_name varchar(60), control_name varchar(60), control_id varchar(60), task_name varchar(60) )");

                sbQuery.AppendLine("create table #fw_des_service_dtl(servicename varchar(60), servicetype int, insegs varchar(8000), outsegs varchar(8000), iosegs varchar(8000), scsegs varchar(8000), ");
                sbQuery.AppendLine("insegcnt int, outsegcnt int, scsegcnt int, pscnt int, iosegcnt int, datasegs varchar(8000), segcnt int, Junk_outscsegs varchar(200), junk_inscsegs int)");
                sbQuery.AppendLine("create table #fw_des_service_segment_dtl(servicename varchar(60), segmentname varchar(60), instanceflag int, parentsegment varchar(60), flowdirection int, ");
                sbQuery.AppendLine("mandatoryflag int, ispartofhierarchy int, isscratchdipartofsegment int, segmentsequence int, segkeys int, dicnt int, indinames  varchar(8000), ");
                sbQuery.AppendLine("iodinames varchar(8000), outdinames varchar(8000), scdinames varchar(8000), dinames varchar(8000), indicnt int, outdicnt int, iodicnt int, scdicnt int)");
                sbQuery.AppendLine("create table #fw_des_integ_outsegmap_dtl(servicename varchar(60), callerservice  varchar(60), sectionname varchar(60), sequenceno int, isCompName varchar(60), isSegList varchar(8000) )");
                sbQuery.AppendLine("create table #fw_des_integ_segmap_dtl( callerservice varchar(60), integservice varchar(60), sectionname varchar(60), sequenceno int, callerSegList varchar(8000), ");
                sbQuery.AppendLine("isCompName varchar(60), callerOuSeg varchar(60), callerOuDi varchar(8000), isInSegList varchar(8000) )");
                sbQuery.AppendLine("create table #fw_des_processsection_dtl(servicename varchar(60), sectionname varchar(60), ");
                sbQuery.AppendLine("sectiontype tinyint, seqNo tinyint, controlexpression varchar(256), loopingStyle tinyint, loopcausingsegment varchar(60),");
                sbQuery.AppendLine("loopinstanceflag tinyint, ceInstDepFlag tinyint, brsCnt int)");
                sbQuery.AppendLine("create table #fw_des_service_dataitem_dtl (servicename varchar(60), segmentname varchar(60), dataitemname varchar(60), ");
                sbQuery.AppendLine("datatype varchar(20), datalength int, ispartofkey int, flowattribute int, mandatoryflag int, defaultvalue varchar(510), BT_Type varchar(20), isModel char(2))");
                sbQuery.AppendLine("create table #fw_des_parameter_cnt(servicename varchar(60), segmentname varchar(60), dataitemname varchar(60), br_totcnt int, br_incnt int)");
                sbQuery.AppendLine("create table #fw_ezeeview_sp(component_name varchar(60), activity_name varchar(60), ui_name varchar(60), taskname varchar(60), ");
                sbQuery.AppendLine("page_bt_synonym varchar(60), Link_ControlName varchar(60), Target_SPName varchar(60), ");
                sbQuery.AppendLine("Link_Caption varchar(60), Linked_Component varchar(60), Linked_Activity varchar(60), Linked_ui varchar(60) )");
                sbQuery.AppendLine("create table #fw_ezeeview_spparamlist(component_name varchar(60), activity_name varchar(60), ui_name varchar(60), ");
                sbQuery.AppendLine("page_bt_synonym varchar(60), Link_ControlName varchar(60), Target_SPName varchar(60), ParameterName varchar(60), ");
                sbQuery.AppendLine("Mapped_Control varchar(60), Link_Caption varchar(60), taskname varchar(60), controlid varchar(60), viewname varchar(60) )");
                sbQuery.AppendLine("create table #fw_des_caller_integ_serv_map (componentname varchar(60), callingservicename varchar(60), callingsegment varchar(60), callingdataitem varchar(60), integservicename varchar(60), ");
                sbQuery.AppendLine("integsegment varchar(60), integdataitem varchar(60) )");
                sbQuery.AppendLine("create table #fw_extjs_link_grid_map (taskname varchar(100), ui_name varchar(60), subscribed_control_id varchar(60) )");
                sbQuery.AppendLine("create table #fw_extjs_control_dtl (activityname varchar(60), uiname varchar(60), taskname varchar(60), servicename varchar(60), segmentname varchar(60), sectiontype varchar(60), controlid varchar(60) )");
                sbQuery.AppendLine("create table #fw_req_control_dtls (activityid int, ilbocode varchar(60), controlname varchar(60), controltype varchar(40),btsynonym varchar(255), hidden varchar(20), disabled varchar(20), pagesize varchar(20), tabname varchar(60), systemcontrol varchar(20))");
                //sbQuery.AppendLine("create table #fw_req_control_view_dtls(activityid int, ilbocode varchar(60), controlname varchar(60), viewname varchar(60), datalength int, datatype varchar(60), headertext varchar(256), helptext varchar(256), ishidden varchar(20), isdisabled varchar(20), ismultiselect varchar(20), isreadonly varchar(20), linkedview varchar(60), viewsequence int, viewtype varchar(60), listvalues varchar(8000), precisionname varchar(20))"); // Code commented for bug id: PLF2.0_03462
                sbQuery.AppendLine("create table #fw_req_control_view_dtls(activityid int, ilbocode varchar(60), controlname varchar(60), viewname varchar(60),btsynonym varchar(255), datalength int, datatype varchar(60), headertext varchar(256), helptext varchar(256), ishidden varchar(20), isdisabled varchar(20), ismultiselect varchar(20), isreadonly varchar(20), linkedview varchar(60), viewsequence int, viewtype varchar(60), listvalues varchar(8000), precisionname varchar(20), defaultval varchar(255))"); // Code added for bug id: PLF2.0_03462
                sbQuery.AppendLine("create table #fw_req_task_dtls(activityid int, ilbocode varchar(60), eventname varchar(60), eventtype varchar(60), navigationkey varchar(60), associatedservice varchar(60), eventmode varchar(7)) "); //TECH-16278
                sbQuery.AppendLine("create table #fw_req_link_subscription_dtls (linkid varchar(60), ilbocode varchar(60), controlname varchar(60), viewname varchar(60), ismultiinstance int, flowdirection int, itemname varchar(60), event varchar(60))");   //code modified for bug id: PLF2.0_04389
                sbQuery.AppendLine("create table #fw_req_pub_link_sub_dtls (linkid varchar(60), ilbocode varchar(60), controlname varchar(60), viewname varchar(60), ismultiinstance int, flowdirection int, itemname varchar(60))");
                sbQuery.AppendLine("create table #fw_req_link_dtls (activityid varchar(60), linkid varchar(60), ilbocode varchar(60), targetcomponent varchar(60), targetactivity varchar(60), targetscreen varchar(60))");
                sbQuery.AppendLine("create table #fw_req_pub_link_dtls (activityid varchar(60), linkid varchar(60), ilbocode varchar(60), targetcomponent varchar(60), targetactivity varchar(60), targetscreen varchar(60))");
                sbQuery.AppendLine("create table #fw_des_webservice_dtl (activityid int, activityname varchar(60), ilbocode varchar(60), servicename varchar(60))");
                sbQuery.AppendLine("create table #fw_des_webservice_segment_dtl (servicename varchar(60), segmentname varchar(60), seq int, flowdirection int, multiline varchar(8), filling varchar(8),");
                sbQuery.AppendLine("indicnt int, outdicnt int, iodicnt int, scdicnt int, dicnt int, instanceflag int, control varchar(64), ilbocode varchar(64), activityid int)");
                sbQuery.AppendLine("create table #fw_des_webservice_dataitem_dtl (servicename varchar(60), segmentname varchar(60), dataitemname varchar(60), control varchar(64), ");
                sbQuery.AppendLine("viewname varchar(64), btsynonym varchar(255) ,flowattribute tinyint, ilbocode varchar(64), activityid int)"); //TECH-33808
                sbQuery.AppendLine("create table #de_ui_control_dtl(activity_name varchar(60), ui_name varchar(60), page_bt_synonym varchar(60), section_bt_synonym varchar(60), control_bt_synonym varchar(60), column_bt_synonym varchar(60), base_ctrl_type varchar(60), ctrl_type varchar(60), ctrl_id varchar(60), view_name varchar(60))");
                sbQuery.AppendLine("create table #fw_chart_task_map (activity_name varchar(60), ui_name varchar(60), page_name varchar(60), section_name varchar(60), taskname varchar(60), servicename varchar(60), tasktype varchar(60) )");
                sbQuery.AppendLine("create table #fw_des_chart_service_dataitem (chart_id varchar(60), chart_section varchar(60), chart_attribute varchar(60), servicename varchar(60), segmentname varchar(60), dataitemname varchar(60), ispartofkey int, mandatoryflag int, flowattribute  varchar(60), defaultvalue  varchar(60))");
                sbQuery.AppendLine("create table #fw_des_parameter_bterm_dtls (servicename varchar(60), segmentname varchar(60), dataitemname varchar(60), flowattribute int, datatype varchar(20), instanceflag int, ispartofkey int)");
                sbQuery.AppendLine("create table #fw_des_br_pp_count (servicename varchar(60), sectionname varchar(60),methodid int, ppcnt int)");
                sbQuery.AppendLine("create table #fw_des_br_lp_count (servicename varchar(60), sectionname varchar(60), methodid int, lpcnt int)");
                sbQuery.AppendLine("create table #fw_des_service_tree_map (componentname varchar(60), activity_name varchar(60), ui_name varchar(60), page_name varchar(60), section_name varchar(60), servicename varchar(64), taskname varchar(100), control_bt_synonym varchar(60),clear_tree_before_population varchar(3))");
                sbQuery.AppendLine("create table #fw_des_tree_service_dataitem(componentname varchar(60), activity_name varchar(60), ui_name varchar(60), page_name varchar(60), section_name varchar(60), servicename varchar(60), taskname varchar(60), segmentname varchar(60), dataitemname varchar(60), control_bt_synonym varchar(60),clear_tree_before_population varchar(3))");

                ExecuteNonQueryResult(sbQuery.ToString());
                WriteProfiler("Creating Hashtables end.");
            }
            catch (Exception ex)
            {
                WriteProfiler("Exception raised while creating Hashtable for EcrNo : " + GlobalVar.Ecrno, ex.Message);
            }
        }

        private void InsertTables()
        {
            try
            {
                WriteProfiler("Inserting data in Hashtables start.");
                StringBuilder sbQuery = new StringBuilder();
                string sCondition = string.Empty;
                string s_Condition = string.Empty;

                sCondition = string.Format("where customername = '{0}' and projectname = '{1}' and ecrno = '{2}'", GlobalVar.Customer, GlobalVar.Project, GlobalVar.Ecrno);
                s_Condition = string.Format("where customer_name = '{0}' and project_name = '{1}' and ecrno = '{2}'", GlobalVar.Customer, GlobalVar.Project, GlobalVar.Ecrno);
                sbQuery.AppendLine("insert into #fw_req_activity (activitydesc, activityid, activityname, activityposition, activitysequence, activitytype, componentname, iswfenabled, UpdUser, Updtime)");
                sbQuery.AppendLine("select activitydesc, activityid, activityname, activityposition, activitysequence, activitytype, componentname, iswfenabled, host_name(), getdate() from de_fw_req_publish_activity_vw (nolock)");
                sbQuery.AppendLine(sCondition);
                sbQuery.AppendLine("insert into #fw_req_activity_ilbo (activityid, ilbocode, activityname, UpdUser, Updtime)");
                sbQuery.AppendLine("select activityid, ilbocode, activityname, host_name(), getdate() from  de_fw_req_publish_activity_ilbo_vw (nolock)");
                sbQuery.AppendLine(sCondition);
                sbQuery.AppendLine("insert into #fw_req_ilbo (aspofilepath, description, ilbocode, ilbotype, progid, statusflag, UpdUser, Updtime)");
                sbQuery.AppendLine("select aspofilepath, description, ilbocode, ilbotype, progid, statusflag, host_name(), getdate() from de_fw_req_publish_ilbo_vw (nolock)");
                sbQuery.AppendLine(sCondition);

                sbQuery.AppendLine("insert into #fw_req_ilbo_control_property (controlid, ilbocode, propertyname, type, value, viewname, UpdUser, Updtime)");
                sbQuery.AppendLine("select lower(controlid), ilbocode, propertyname, type, value, viewname, host_name(), getdate() from de_fw_req_publish_ilbo_control_property_vw (nolock)");
                sbQuery.AppendLine(sCondition);

                sbQuery.AppendLine("insert into #fw_req_ilbo_view (btsynonym, controlid, displayflag, displaylength, ilbocode, viewname, isItkCtrl, UpdUser, Updtime )");
                sbQuery.AppendLine("select btsynonym, lower(controlid), displayflag, displaylength, ilbocode, viewname, 'n', host_name(), getdate() from    de_fw_req_publish_ilbo_view_vw (nolock)");
                sbQuery.AppendLine(sCondition);
                sbQuery.AppendLine("insert into #fw_req_bterm (btdesc, btname, datatype, isbterm, length, maxvalue, minvalue, precisiontype, UpdUser, Updtime)");
                sbQuery.AppendLine("select btdesc, btname, datatype, isbterm, length, maxvalue, minvalue, precisiontype, host_name(), getdate() from de_fw_req_publish_bterm_vw (nolock)");
                sbQuery.AppendLine(sCondition);
                sbQuery.AppendLine("insert into #fw_req_bterm_synonym (btname, btsynonym, UpdUser, Updtime)");
                sbQuery.AppendLine("select btname, btsynonym, host_name(), getdate() from de_fw_req_publish_bterm_synonym_vw (nolock)");
                sbQuery.AppendLine(sCondition);
                sbQuery.AppendLine("insert into #fw_req_process_component (componentdesc, componentname, parentprocess, sequenceno, UpdUser, Updtime)");
                sbQuery.AppendLine("select componentdesc, componentname, parentprocess, sequenceno, host_name(), getdate() from de_fw_req_publish_process_component_vw (nolock)");
                sbQuery.AppendLine(sCondition);
                sbQuery.AppendLine("insert into #fw_req_ilbo_tab_properties (ilbocode, propertyname, tabname, value, UpdUser, Updtime)");
                sbQuery.AppendLine("select ilbocode, propertyname, tabname, value, host_name(), getdate() from de_fw_req_publish_ilbo_tab_properties_vw (nolock)");
                sbQuery.AppendLine(sCondition);
                sbQuery.AppendLine("insert into #fw_req_activity_ilbo_task (activityid, datasavingtask, ilbocode, linktype, taskname, activityname, Taskconfirmation, usageid, ddt_control_id, ddt_view_name, UpdUser, Updtime,Autoupload)"); //TECH-16278
                sbQuery.AppendLine("select activityid, datasavingtask, ilbocode, linktype, ltrim(rtrim(taskname)), activityname, Taskconfirmation, usageid, ddt_control_id, ddt_view_name, host_name(), getdate(),Autoupload  from de_fw_req_publish_activity_ilbo_task_vw (nolock)"); //TECH-16278
                sbQuery.AppendLine(sCondition);
                sbQuery.AppendLine("insert into #fw_req_task (taskdesc, taskname, tasktype, UpdUser, Updtime)");
                sbQuery.AppendLine("select taskdesc, ltrim(rtrim(taskname)), tasktype, host_name(), getdate() from de_fw_req_publish_task_vw (nolock)");
                sbQuery.AppendLine(sCondition);
                sbQuery.AppendLine("insert into #fw_req_bterm_enumerated_option (btname, langid, optioncode, optiondesc, sequenceno, UpdUser, Updtime)");
                sbQuery.AppendLine("select btname, langid, optioncode, optiondesc, sequenceno, host_name(), getdate() from de_fw_req_publish_bterm_enumerated_option_vw (nolock)");
                sbQuery.AppendLine(sCondition);
                sbQuery.AppendLine("insert into #fw_req_ilbo_link_publish (activityid, description, ilbocode, linkid, taskname, UpdUser, Updtime)");
                sbQuery.AppendLine("select activityid, description, ilbocode, linkid, ltrim(rtrim(taskname)), host_name(), getdate() from de_fw_req_publish_ilbo_link_publish_vw (nolock)");
                sbQuery.AppendLine(sCondition);
                sbQuery.AppendLine("insert into #fw_req_ilbo_data_publish (controlid, controlvariablename, dataitemname, flowtype, havemultiple, ilbocode, iscontrol, linkid, mandatoryflag, viewname, UpdUser, Updtime)");
                sbQuery.AppendLine("select lower(controlid), controlvariablename, dataitemname, flowtype, havemultiple, ilbocode, iscontrol, linkid, mandatoryflag, viewname, host_name(), getdate() from de_fw_req_publish_ilbo_data_publish_vw (nolock)");
                sbQuery.AppendLine(sCondition);
                sbQuery.AppendLine("insert into #fw_req_ilbo_data_use (childilbocode, controlid, controlvariablename, dataitemname, flowtype, iscontrol, linkid, parentilbocode, primarydata, retrievemultiple, taskname, viewname, UpdUser, Updtime)");
                sbQuery.AppendLine("select childilbocode, lower(controlid), controlvariablename, dataitemname, flowtype, iscontrol, linkid, parentilbocode, primarydata, retrievemultiple, ltrim(rtrim(taskname)), viewname, host_name(), getdate() from de_fw_req_publish_ilbo_data_use_vw (nolock)");
                sbQuery.AppendLine(sCondition);
                sbQuery.AppendLine("insert into #fw_req_ilbo_linkuse (childilbocode, childorder, linkid, parentilbocode, taskname, posttask, UpdUser, Updtime)");
                sbQuery.AppendLine("select childilbocode, childorder, linkid, parentilbocode, ltrim(rtrim(taskname)), post_task, host_name(), getdate() from de_fw_req_publish_ilbo_linkuse_vw (nolock)");
                sbQuery.AppendLine(sCondition);
                sbQuery.AppendLine("insert into #fw_des_ilbo_services (ilbocode, isprepopulate, servicename, UpdUser, Updtime)");
                sbQuery.AppendLine("select ilbocode, isprepopulate, servicename, host_name(), getdate() from de_fw_des_publish_ilbo_services_vw (nolock)");
                sbQuery.AppendLine(sCondition);
                sbQuery.AppendLine("insert into #fw_des_service_dataitem select dataitemname, defaultvalue, flowattribute, ispartofkey, mandatoryflag, segmentname, servicename, itk_dataitem, 'dbo', getdate(), '' From de_fw_des_publish_service_dataitem_vw (nolock)");
                sbQuery.AppendLine(sCondition);

                sbQuery.AppendLine("insert into #fw_des_service  ( isintegser, componentname, processingtype, servicename, servicetype, statusflag, svconame)");
                sbQuery.AppendLine("select isintegser, componentname, processingtype, servicename, servicetype, 1, svconame  from  de_fw_des_publish_service_vw (nolock)");
                sbQuery.AppendLine(sCondition);
                sbQuery.AppendLine("insert into #fw_des_service_segment ( bocode, bosegmentname, instanceflag, mandatoryflag, parentsegmentname, segmentname, SegmentSequence, servicename, process_selrows)");
                sbQuery.AppendLine("select bocode, bosegmentname, instanceflag, mandatoryflag, parentsegmentname, segmentname, SegmentSequence, servicename, process_selrows from de_fw_des_publish_service_segment_vw (nolock)");
                sbQuery.AppendLine(sCondition);
                sbQuery.AppendLine("insert into #fw_des_ilbo_service_view_datamap (activityid, controlid, dataitemname, ilbocode, iscontrol, segmentname, servicename, taskname, variablename, viewname, btsynonym,UpdUser, Updtime)");
                sbQuery.AppendLine("select activityid, lower(controlid), dataitemname, ilbocode, iscontrol, segmentname, servicename, ltrim(rtrim(taskname)), variablename, viewname, btsynonym ,host_name(), getdate() from de_fw_des_publish_ilbo_service_view_datamap_vw (nolock)"); //TECH-33808
                sbQuery.AppendLine(sCondition);
                sbQuery.AppendLine("insert into #fw_des_ilbo_service_view_datamap (activityid, controlid, dataitemname, ilbocode, iscontrol, segmentname, servicename, taskname, variablename, viewname, UpdUser, Updtime)");
                sbQuery.AppendLine("select distinct b.activityid, '', '', b.ilbocode, '', '', service_name, ltrim(rtrim(task_name)) TaskName, '', '', '', '' from de_published_task_service_map a(nolock), #fw_req_activity_ilbo b (nolock) where a.customer_name = '");
                sbQuery.AppendLine(GlobalVar.Customer);
                sbQuery.AppendLine("' and a.project_name = '");
                sbQuery.AppendLine(GlobalVar.Project);
                sbQuery.AppendLine("' and a.ecrno = '");
                sbQuery.AppendLine(GlobalVar.Ecrno);
                sbQuery.AppendLine("' and a.activity_name collate database_default = b.activityname and task_name collate database_default not in (select distinct taskname from #fw_des_ilbo_service_view_datamap (nolock))");
                sbQuery.AppendLine("insert into #fw_des_task_segment_attribs (activityid, ilbocode, taskname, servicename, segmentname, combofill, UpdUser, Updtime)");
                sbQuery.AppendLine("select activityid, ilbocode, ltrim(rtrim(taskname)), servicename, segmentname, combofill, host_name(), getdate() from de_fw_des_publish_task_segment_attribs_vw (nolock)");
                sbQuery.AppendLine(sCondition);
                sbQuery.AppendLine("insert into #fw_des_task_segment_attribs (activityid, ilbocode, taskname, servicename, segmentname, combofill, UpdUser, Updtime)");
                sbQuery.AppendLine("select activityid, ilbocode, ltrim(rtrim(taskname)), servicename, segmentname, combofill, host_name(), getdate() from de_fw_des_publish_task_segment_attribs_vw (nolock)");
                sbQuery.AppendLine(sCondition);
                sbQuery.AppendLine("insert into #fw_req_task_local_info (langid, taskname, description, helpindex, UpdUser, Updtime)");
                sbQuery.AppendLine("select langid, ltrim(rtrim(taskname)), description, helpindex, host_name(), getdate() from de_fw_req_Publish_task_local_info_vw (nolock)");
                sbQuery.AppendLine(sCondition);
                sbQuery.AppendLine("insert into #fw_req_ilbo_link_local_info (LinkID, ilbocode, Langid, Description, HelpIndex, UpdUser, UpdTime)");
                sbQuery.AppendLine("select LinkID, ilbocode, Langid, Description, HelpIndex, host_name(), getdate() from  de_fw_req_publish_ilbo_link_local_info_vw (nolock)");
                sbQuery.AppendLine(sCondition);
                sbQuery.AppendLine("insert into #fw_req_activity_ilbo_task_extension_map (componentname, activityid, ilbocode, taskname, resultantaspname, sessionvariable, UpdUser, Updtime)");
                sbQuery.AppendLine("select componentname, activityid, ilbocode, ltrim(rtrim(taskname)), resultantaspname, sessionvariable, host_name(), getdate() from de_fw_req_publish_activity_ilbo_task_extension_map (nolock)");
                sbQuery.AppendLine(sCondition);
                sbQuery.AppendLine("insert into #fw_req_activity_task (ActivityId, TaskName, InvocationType, TaskSequence, UpdUser, UpdTime)");
                sbQuery.AppendLine("select ActivityId, ltrim(rtrim(TaskName)), InvocationType, TaskSequence, host_name(), getdate() from de_fw_req_publish_activity_task_vw (nolock)");
                sbQuery.AppendLine(sCondition);
                sbQuery.AppendLine("insert into #fw_req_ilbo_local_info (ilbocode, Langid, Description, HelpIndex, UpdUser, UpdTime)");
                sbQuery.AppendLine("select ilbocode, Langid, Description, HelpIndex, host_name(), getdate() from de_fw_req_publish_ilbo_local_info_vw");
                sbQuery.AppendLine(sCondition);

                if (GlobalVar.Tabs)
                {
                    sbQuery.AppendLine("insert into #fw_req_ilbo_tabs (ILBOCode, TabName, BTSynonym, UpdUser, UpdTime)");
                    sbQuery.AppendLine("select ILBOCode, Null, BTSynonym, host_name(), getdate() from  de_fw_req_publish_ilbo_tabs_vw (nolock)");
                    sbQuery.AppendLine(sCondition);
                    sbQuery.AppendLine("insert into #fw_req_ilbo_control (controlid, ilbocode, tabname, type, listedit,btsynonym, UpdUser, Updtime)");
                    sbQuery.AppendLine("select lower(controlid), ilbocode, Null, type, listedit,btsynonym, host_name(), getdate() from de_fw_req_publish_ilbo_control_vw (nolock)");
                    sbQuery.AppendLine(sCondition);
                }
                else
                {
                    sbQuery.AppendLine("insert into #fw_req_ilbo_tabs (ILBOCode, TabName, BTSynonym, UpdUser, UpdTime)");
                    sbQuery.AppendLine("select ILBOCode, TabName, BTSynonym, host_name(), getdate() from  de_fw_req_publish_ilbo_tabs_vw (nolock)");
                    sbQuery.AppendLine(sCondition);
                    sbQuery.AppendLine("insert into #fw_req_ilbo_control (controlid, ilbocode, tabname, type, listedit,btsynonym, UpdUser, Updtime)");
                    sbQuery.AppendLine("select lower(controlid), ilbocode, tabname, type, listedit,btsynonym ,host_name(), getdate() from de_fw_req_publish_ilbo_control_vw (nolock)");
                    sbQuery.AppendLine(sCondition);
                }

                sbQuery.AppendLine("insert into #fw_req_lang_bterm_synonym (BTSynonym, Langid, ForeignName, LongPLText, ShortPLText, ShortDesc, LongDesc, UpdUser, UpdTime)");
                sbQuery.AppendLine("select BTSynonym, Langid, ForeignName, LongPLText, ShortPLText, ShortDesc, LongDesc, host_name(), getdate() from de_fw_req_publish_lang_bterm_synonym_vw (nolock)");
                sbQuery.AppendLine(sCondition);
                sbQuery.AppendLine("insert into #fw_req_precision (PrecisionType, TotalLength, DecimalLength, UpdUser, UpdTime)");
                sbQuery.AppendLine("select PrecisionType, TotalLength, DecimalLength, host_name(), getdate() from de_fw_req_publish_precision_vw (nolock)");
                sbQuery.AppendLine(sCondition);

                sbQuery.AppendLine("insert into #fw_req_system_parameters (paramname, paramvalue, timestamp, createdby, createddate, modifiedby, modifieddate, paramdesc)");
                sbQuery.AppendLine("select paramname, paramvalue, timestamp, createdby, createddate, modifiedby, modifieddate, paramdesc from de_fw_req_system_parameters (nolock) where customer_name = '");
                sbQuery.AppendLine(GlobalVar.Customer);
                sbQuery.AppendLine("' and project_name = '");
                sbQuery.AppendLine(GlobalVar.Project);
                sbQuery.AppendLine("'");
                sbQuery.AppendLine("Insert into #fw_des_be_placeholder ( errorid, methodid, parametername, placeholdername )");
                sbQuery.AppendLine("select errorid, methodid, parametername, placeholdername  from de_fw_des_publish_be_placeholder_vw (nolock)");
                sbQuery.AppendLine(sCondition);
                sbQuery.AppendLine("insert into #fw_des_br_logical_parameter ( btname,  flowdirection, logicalparametername, logicalparamseqno, methodid, recordsetname, rssequenceno, spparametertype )");
                sbQuery.AppendLine("select btname, flowdirection, logicalparametername, logicalparamseqno, methodid, recordsetname, rssequenceno, spparametertype from de_fw_des_publish_br_logical_parameter_vw (nolock)");
                sbQuery.AppendLine(sCondition);
                sbQuery.AppendLine("insert into #fw_des_brerror(errorid, methodid, sperrorcode )");
                sbQuery.AppendLine("select errorid, methodid, sperrorcode from de_fw_des_publish_brerror_vw (nolock)");
                sbQuery.AppendLine(sCondition);

                sbQuery.AppendLine("insert into #fw_des_bro ( brodescription, broname, broscope, brotype, componentname, clsid, clsname, dllname, progid, systemgenerated )");
                sbQuery.AppendLine("select brodescription, broname, broscope, brotype, componentname, clsid, clsname, dllname, progid, systemgenerated from  de_fw_des_publish_bro_vw (nolock)");
                sbQuery.AppendLine(sCondition);
                sbQuery.AppendLine("insert into #fw_des_businessrule ( accessesdatabase, bocode, broname, dispid, isintegbr, methodid, methodname, operationtype, statusflag, systemgenerated )");
                sbQuery.AppendLine("select accessesdatabase, bocode, broname, dispid, isintegbr, methodid, methodname, operationtype, statusflag, systemgenerated from  de_fw_des_publish_businessrule_vw (nolock)");
                sbQuery.AppendLine(sCondition);

                sbQuery.AppendLine("insert into #fw_des_context(correctiveaction, errorcontext, errorid, severityid )");
                sbQuery.AppendLine("select correctiveaction, errorcontext, errorid, severityid from de_fw_des_publish_context_vw (nolock)");
                sbQuery.AppendLine(sCondition);
                sbQuery.AppendLine("insert into #fw_des_di_parameter(dataitemname, methodid, parametername, sectionname, segmentname, sequenceno, servicename )");
                sbQuery.AppendLine("select dataitemname,  methodid, parametername, sectionname, segmentname, sequenceno, servicename from de_fw_des_publish_di_parameter_vw (nolock)");
                sbQuery.AppendLine(sCondition);

                sbQuery.AppendLine("insert into #fw_des_di_placeholder(dataitemname,  errorid, methodid, placeholdername, sectionname, segmentname, sequenceno, servicename )");
                sbQuery.AppendLine("select dataitemname,  errorid, methodid, placeholdername, sectionname, segmentname, sequenceno, servicename from de_fw_des_publish_di_placeholder_vw (nolock)");
                sbQuery.AppendLine(sCondition);
                sbQuery.AppendLine("insert into #fw_des_error(defaultcorrectiveaction, defaultseverity,  detaileddesc, displaytype,   errorid, errormessage, errorsource, reqerror )");
                sbQuery.AppendLine("select defaultcorrectiveaction, defaultseverity,  detaileddesc, displaytype,   errorid, errormessage, errorsource, reqerror from de_fw_des_publish_error_vw (nolock)");
                sbQuery.AppendLine(sCondition);

                sbQuery.AppendLine("insert into #fw_des_integ_serv_map(callingdataitem, callingsegment, callingservicename, integdataitem, integsegment, integservicename, sectionname, sequenceno )");
                sbQuery.AppendLine("select callingdataitem, callingsegment, callingservicename, integdataitem, integsegment, integservicename, sectionname, sequenceno from de_fw_des_publish_integ_serv_map_vw (nolock)");
                sbQuery.AppendLine(sCondition);
                sbQuery.AppendLine("insert into #fw_des_processsection(controlexpression, processingtype, sectionname, sectiontype, sequenceno, servicename )");
                sbQuery.AppendLine("select case controlexpression when '' then NUll else controlexpression end, processingtype,  sectionname, sectiontype, sequenceno, servicename from de_fw_des_publish_processsection_vw (nolock)");
                sbQuery.AppendLine(sCondition);

                sbQuery.AppendLine("insert into #fw_des_processsection_br_is ( connectivityflag, controlexpression, executionflag, integservicename, isbr,  methodid, sectionname, sequenceno, servicename, Method_Ext, methodid_ref, methodname_ref, sequenceno_ref, sectionname_ref, ps_sequenceno_ref)");
                sbQuery.AppendLine("select connectivityflag, controlexpression, executionflag, integservicename, isbr, methodid, sectionname, sequenceno, servicename, '' as Method_Ext, '' as methodid_ref, '' as methodname_ref, '' as sequenceno_ref, '' as sectionname_ref, '' as ps_sequenceno_ref from de_fw_des_publish_processsection_br_is_vw (nolock)");
                sbQuery.AppendLine(sCondition);

                sbQuery.AppendLine("insert into #fw_des_processsection_br_is_err ( connectivityflag, controlexpression, executionflag, integservicename, isbr,  methodid, sectionname, sequenceno, servicename  )");
                sbQuery.AppendLine("select connectivityflag, controlexpression, executionflag, integservicename, isbr,  methodid, sectionname, sequenceno, servicename  from de_fw_des_publish_processsection_br_is_err_vw (nolock)");
                sbQuery.AppendLine(sCondition);

                sbQuery.AppendLine("insert into #fw_des_sp ( methodid, sperrorprotocol, spname )");
                sbQuery.AppendLine("select methodid, sperrorprotocol, spname from de_fw_des_publish_sp_vw (nolock)");
                sbQuery.AppendLine(sCondition);

                sbQuery.AppendLine("Insert into #fw_des_bo(BOCode, ComponentName, BODesc, StatusFlag)");
                sbQuery.AppendLine("select BOCode, ComponentName, BODesc, StatusFlag from de_fw_des_publish_bo_vw (nolock)");
                sbQuery.AppendLine(sCondition);
                sbQuery.AppendLine("insert into #fw_des_svco (SVCOName, SVCODescription, ComponentName, SVCOScope, SVCOType, DLLName, ProgID)");
                sbQuery.AppendLine("select SVCOName, SVCODescription, ComponentName, SVCOScope, SVCOType, DLLName, ProgID from de_fw_des_publish_svco_vw (nolock)");
                sbQuery.AppendLine(sCondition);

                sbQuery.AppendLine("insert into #fw_des_error_placeholder (ErrorID, PlaceholderName)");
                sbQuery.AppendLine("select ErrorID, PlaceholderName from  de_fw_des_publish_ERROR_PLACEHOLDER_vw (nolock)");
                sbQuery.AppendLine(sCondition);
                sbQuery.AppendLine("insert into #fw_des_reqbr_desbr (ReqBRName, MethodID)");
                sbQuery.AppendLine("select ReqBRName, MethodID from  de_fw_des_publish_REQBR_DESBR_vw (nolock)");
                sbQuery.AppendLine(sCondition);

                sbQuery.AppendLine("insert into #fw_req_TASK_RULE (TaskName, BRSequence, BRName, InvocationType)");
                sbQuery.AppendLine("select ltrim(rtrim(TaskName)), BRSequence, BRName, InvocationType from  de_fw_req_publish_task_rule_vw (nolock)");
                sbQuery.AppendLine(sCondition);
                sbQuery.AppendLine("insert into #fw_des_ilbo_placeholder (ilbocode, ControlID, EventName, PlaceholderName, IsControl, ControlName, ViewName, VariableName, ErrorID, CtrlEvent_ViewName)");
                sbQuery.AppendLine("select ilbocode, lower(ControlID), EventName, PlaceholderName, IsControl, ControlName, ViewName, VariableName, ErrorID, CtrlEvent_ViewName from  de_fw_des_publish_ilbo_placeholder_vw (nolock)");
                sbQuery.AppendLine(sCondition);
                sbQuery.AppendLine("insert into #fw_req_ilboctrl_initval (ilbocode, ControlID, ViewName, sequenceNo, InitialValue, UpdUser, UpdTime)");
                sbQuery.AppendLine("select ilbocode, lower(ControlID), ViewName, sequenceNo, InitialValue, host_name(), getdate() from de_fw_req_publish_ilboctrl_initval (nolock)");
                sbQuery.AppendLine(sCondition);

                sbQuery.AppendLine("insert into #fw_des_ilbo_ctrl_event (ILBOCode, ControlID, EventName, TaskName, MethodName, CreateStubFlag, ViewName)");
                sbQuery.AppendLine("select ILBOCode, lower(ControlID), EventName, ltrim(rtrim(TaskName)), MethodName, CreateStubFlag, ViewName from  de_fw_des_publish_ilbo_ctrl_event_vw (nolock)");
                sbQuery.AppendLine(sCondition);
                sbQuery.AppendLine("insert into #fw_req_businessrule (BRName, BRType, BRDesc)");
                sbQuery.AppendLine("select BRName, BRType, BRDesc from  de_fw_req_publish_businessrule_vw (nolock)");
                sbQuery.AppendLine(sCondition);
                sbQuery.AppendLine("insert into #fw_req_BR_ERROR (BRName, ErrorCode)");
                sbQuery.AppendLine("select BRName, ErrorCode from  de_fw_req_publish_BR_ERROR_vw (nolock)");
                sbQuery.AppendLine(sCondition);
                sbQuery.AppendLine("Insert into #fw_req_language (customername, projectname, ecrno, langid, langdesc)");
                sbQuery.AppendLine("select customername, projectname, ecrno, langid, langdesc from de_fw_req_publish_language_vw (nolock)");
                sbQuery.AppendLine(sCondition);

                sbQuery.AppendLine("insert #fw_req_activity_local_info (activityid, langid, activitydesc, helpindex, tooltiptext)");
                sbQuery.AppendLine("select activityid, langid, activitydesc, helpindex, tooltiptext from de_fw_req_publish_activity_local_info_vw (nolock)");
                sbQuery.AppendLine(sCondition);

                sbQuery.AppendLine("insert into #fw_req_ilbo_task_rpt (ilbocode, taskname, PageName, ProcessingType, ContextName, ReportType)");
                sbQuery.AppendLine("select ilbocode, taskname, PageName, ProcessingType, ContextName, ReportType from  de_fw_req_publish_ilbo_task_rpt_vw (nolock)");
                sbQuery.AppendLine(sCondition);

                sbQuery.AppendLine("if exists (select 'x' from sysobjects (nolock) where name = 'avs_message_lng_extn_vw') begin insert into #avs_message_lng_extn (customer_name, project_name, process_name, component_name, activity_name, ui_name, page_name, service_name, Segment_name, dataitemname, validation_code, message_code, languageid, message_doc)");
                sbQuery.AppendLine("select customer_name, project_name, process_name, component_name, activity_name, ui_name, page_name, service_name, Segment_name, dataitemname, validation_code, message_code, languageid, message_doc  from avs_message_lng_extn_vw (nolock) where customer_name = '");
                sbQuery.AppendLine(GlobalVar.Customer);
                sbQuery.AppendLine("' and project_name = '");
                sbQuery.AppendLine(GlobalVar.Project);
                sbQuery.AppendLine("' and Component_name = '");
                sbQuery.AppendLine(GlobalVar.Component);
                sbQuery.AppendLine("' end ");

                sbQuery.AppendLine("insert into #fw_des_chart_service_segment (component_name, chart_id, chart_section, servicename, segmentname, instanceflag)");
                sbQuery.AppendLine("select component_name, chart_id, chart_section, servicename, segmentname, instanceflag from de_fw_des_publish_chart_service_segment_vw (nolock)");
                sbQuery.AppendLine(s_Condition);

                sbQuery.AppendLine("insert into #fw_des_service_tree_map (componentname, activity_name, ui_name, page_name, section_name, servicename, taskname, control_bt_synonym, clear_tree_before_population)");
                sbQuery.AppendLine("select componentname, activity_name, ui_name, page_name, section_name, servicename, taskname, control_bt_synonym, clear_tree_before_population from de_published_service_tree_map_vw (nolock)");
                sbQuery.AppendLine(sCondition);

                sbQuery.AppendLine("insert into #fw_task_service_map (activity_name, service_name, task_name, ui_name )");
                sbQuery.AppendLine("select activity_name, service_name, task_name, ui_name from de_published_task_service_map (nolock)");
                sbQuery.AppendLine(s_Condition);

                sbQuery.AppendLine("insert into #fw_des_focus_control (errorcontext, errorid, controlid, segmentname, focusdataitem)");
                sbQuery.AppendLine("select errorcontext, errorid, lower(controlid), segmentname, focusdataitem from de_fw_des_publish_focus_control_vw (nolock)");
                sbQuery.AppendLine(s_Condition);

                sbQuery.AppendLine("insert into #fw_des_ilerror(ILBOCode, ControlID, EventName, LocalErrorNo, ErrorID, ViewName)");
                sbQuery.AppendLine("select ILBOCode, lower(ControlID), EventName, LocalErrorNo, ErrorID, ViewName from de_fw_des_publish_ilerror_vw  ");
                sbQuery.AppendLine(sCondition);
                sbQuery.AppendLine("insert into #fw_des_err_det_local_info (ErrorID, ErrorMessage, DetailedDesc, LangId )");
                sbQuery.AppendLine("select ErrorID, ErrorMessage, DetailedDesc, LangId  from de_fw_des_publish_err_det_local_info_vw");
                sbQuery.AppendLine(sCondition);
                sbQuery.AppendLine("insert into #fw_des_corr_action_local_info (ErrorID, ErrorContext, CorrectiveAction, LangId)");
                sbQuery.AppendLine("select ErrorID, ErrorContext, CorrectiveAction, LangId from de_fw_des_publish_corr_action_local_info_vw");
                sbQuery.AppendLine(sCondition);

                sbQuery.AppendLine("insert into #fw_service_validation_message (service_name, languageid, segmentname, dataitemname, validation_code, message_code, message_doc, value1, value2, order_of_sequence)");
                sbQuery.AppendLine("select service_name, languageid, segmentname, dataitemname, validation_code, message_code, message_doc, value1, value2, order_of_sequence from avs_published_service_validation_message_vw (nolock)");
                sbQuery.AppendLine(s_Condition);

                sbQuery.AppendLine("insert into #de_log_ext_ctrl_met (le_customer_name, le_project_name, le_control, le_ctrl_clf, le_data_type, le_data_length, le_btname, le_precisiontype, le_decimallength, col_seq, le_control_type)");
                sbQuery.AppendLine("select le_customer_name, le_project_name, le_control, le_ctrl_clf, le_data_type, le_data_length, le_btname, le_precisiontype, le_decimallength, col_seq, le_control_type from de_log_ext_ctrl_met_vw (nolock) where le_customer_name = '");
                sbQuery.AppendLine(GlobalVar.Customer);
                sbQuery.AppendLine("' and le_project_name = '");
                sbQuery.AppendLine(GlobalVar.Project);
                sbQuery.AppendLine("'");

                sbQuery.AppendLine("insert into #es_comp_ctrl_type_mst (customer_name, project_name, req_no, process_name, component_name, ctrl_type_name, ctrl_type_descr, base_ctrl_type, mandatory_flag, visisble_flag, editable_flag, caption_req, select_flag, zoom_req, insert_req, delete_req, help_req, event_handling_req, ellipses_req, comp_ctrl_type_sysid, timestamp, createdby, createddate, modifiedby, modifieddate, visisble_rows, ctrl_type_doc, caption_alignment, caption_wrap, caption_position, ctrl_position, label_class, ctrl_class, ");
                sbQuery.AppendLine("password_char, tskimg_class, hlpimg_class, disponlycmb_req, html_txt_area, report_req, auto_tab_stop, spin_required, spin_up_image, spin_down_image, Extjs_Ctrl_type )");
                sbQuery.AppendLine("select customer_name, project_name, req_no, process_name, component_name, ctrl_type_name, ctrl_type_descr, base_ctrl_type, mandatory_flag, visisble_flag, editable_flag, caption_req, select_flag, zoom_req, insert_req, delete_req, help_req, event_handling_req, ellipses_req, comp_ctrl_type_sysid, timestamp, createdby, createddate, modifiedby, modifieddate, visisble_rows, ctrl_type_doc, caption_alignment, caption_wrap, caption_position, ctrl_position, label_class, ctrl_class, password_char, tskimg_class, ");
                sbQuery.AppendLine("hlpimg_class, disponlycmb_req, html_txt_area, report_req, auto_tab_stop, spin_required, spin_up_image, spin_down_image, Extjs_Ctrl_type  from  es_comp_ctrl_type_mst_vw (nolock) where customer_name = '");
                sbQuery.AppendLine(GlobalVar.Customer);
                sbQuery.AppendLine("' and project_name = '");
                sbQuery.AppendLine(GlobalVar.Project);
                sbQuery.AppendLine("' and component_name ='");
                sbQuery.AppendLine(GlobalVar.Component);
                sbQuery.AppendLine("'");

                sbQuery.AppendLine("insert into #de_published_service_logic_extn_dtl (component_name, servicename, section_name, methodid, methodname, br_sequence, rdbms_type, ext_as, ext_before, ext_separate_ps, ext_object_name, extend_flag, status_flag, methodtype)");
                sbQuery.AppendLine("select component_name, servicename, section_name, methodid, methodname, br_sequence, rdbms_type, ext_as, ext_before, ext_separate_ps, ext_object_name, extend_flag, status_flag, methodtype from  de_published_service_logic_extn_dtl (nolock) where customer_name = '");
                sbQuery.AppendLine(GlobalVar.Customer);
                sbQuery.AppendLine("' and project_name = '");
                sbQuery.AppendLine(GlobalVar.Project);
                sbQuery.AppendLine("' and ecr_no = '");
                sbQuery.AppendLine(GlobalVar.Ecrno);
                sbQuery.AppendLine("'");

                sbQuery.AppendLine("insert into #de_published_ui_control(activity_name, component_name, control_bt_synonym, control_doc, control_id, control_prefix, control_type, data_column_width, horder, label_column_width, order_seq, page_bt_synonym, proto_tooltip, sample_data, section_bt_synonym, timestamp, ui_control_sysid, ");
                sbQuery.AppendLine("ui_name, ui_section_sysid, view_name, visisble_length, vorder, label_column_scalemode, data_column_scalemode, tab_seq, help_tabstop, LabelClass, ControlClass, LabelImageClass, ControlImageClass, label_control_id, req_no)");
                sbQuery.AppendLine("select activity_name, component_name, control_bt_synonym, control_doc, control_id, control_prefix, control_type, data_column_width, horder, label_column_width, order_seq, page_bt_synonym, proto_tooltip, sample_data, section_bt_synonym, timestamp, ui_control_sysid, ");
                sbQuery.AppendLine("ui_name, ui_section_sysid, view_name, visisble_length, vorder, label_column_scalemode, data_column_scalemode, tab_seq, help_tabstop, LabelClass, ControlClass, LabelImageClass, ControlImageClass, label_control_id, req_no from de_published_ui_control_vw (nolock)");
                sbQuery.AppendLine(s_Condition);

                sbQuery.AppendLine("insert into #de_published_ui_grid(activity_name, column_bt_synonym, column_no, column_prefix, column_type, col_doc, component_name, control_bt_synonym, control_id, grid_sysid, page_bt_synonym, proto_tooltip, sample_data, section_bt_synonym, timestamp, ui_control_sysid, ui_name, view_name, visible_length, req_no)");
                sbQuery.AppendLine("select ctivity_name, column_bt_synonym, column_no, column_prefix, column_type, col_doc, component_name, control_bt_synonym, control_id, grid_sysid, page_bt_synonym, proto_tooltip, sample_data, section_bt_synonym, timestamp, ui_control_sysid, ui_name, view_name, visible_length, req_no");
                sbQuery.AppendLine("from de_published_ui_grid_vw (nolock)");
                sbQuery.AppendLine(s_Condition);

                sbQuery.AppendLine("insert into  #de_published_ui_page( activity_name, component_name, horder, page_bt_synonym, page_doc, page_prefix, timestamp, ui_name, ui_page_sysid, ui_sysid, vorder, req_no)");
                sbQuery.AppendLine("select activity_name, component_name, horder, page_bt_synonym, page_doc, page_prefix, timestamp, ui_name, ui_page_sysid, ui_sysid, vorder, req_no from de_published_ui_page_vw (nolock)");
                sbQuery.AppendLine(s_Condition);

                sbQuery.AppendLine("insert into #fw_exrep_task_temp_map (activity_name, ui_name, page_name, task_name, template_id, control_name, control_page_name, control_id, view_name)");
                sbQuery.AppendLine("select activity_name, ui_name, page_name, task_name, template_id, control_name, control_page_name, control_id, view_name from de_published_exrep_task_temp_map_vw (nolock)");
                sbQuery.AppendLine(s_Condition);

                sbQuery.AppendLine("insert into #fw_ezeeview_sp(component_name, activity_name, ui_name, taskname, page_bt_synonym, Link_ControlName, Target_SPName, Link_Caption, Linked_Component, Linked_Activity, Linked_ui)");
                sbQuery.AppendLine("select component_name, activity_name, ui_name, task_name, page_bt_synonym, Link_ControlName, Target_SPName, Link_Caption, Linked_Component, Linked_Activity, Linked_ui from de_published_ezeeview_sp_vw (nolock)");
                sbQuery.AppendLine(s_Condition);

                sbQuery.AppendLine("insert into  #fw_ezeeview_spparamlist(component_name, activity_name, ui_name, page_bt_synonym, Link_ControlName, Target_SPName, ParameterName, Mapped_Control, Link_Caption, taskname, controlid, viewname)");
                sbQuery.AppendLine("select component_name, activity_name, ui_name, page_bt_synonym, Link_ControlName, Target_SPName, ParameterName, Mapped_Control, Link_Caption, task_name, control_id, view_name from de_published_ezeeview_spparamlist_vw (nolock)");
                sbQuery.AppendLine(s_Condition);

                sbQuery.AppendLine("insert into #fw_des_caller_integ_serv_map (componentname, callingservicename, callingsegment, callingdataitem, integservicename, integsegment, integdataitem)");
                sbQuery.AppendLine("select componentname, callingservicename, callingsegment, callingdataitem, integservicename, integsegment, integdataitem");
                sbQuery.AppendLine("from de_fw_des_publish_caller_integ_serv_map_vw");
                sbQuery.AppendLine("where componentname ='");
                sbQuery.AppendLine(GlobalVar.Component);
                sbQuery.AppendLine("'");

                sbQuery.AppendLine("select issegname=integ.integsegment, isdiname=integ.integdataitem, callersegname=integ.callingsegment, callerdiname =integ.callingdataitem, integ.integservicename, sdi.flowattribute, sdi.defaultvalue, sdi.ispartofkey,");
                sbQuery.AppendLine("callingservicename=integ.callingservicename, sdi.servicename, icomponentname=ser.componentname, integ.sectionname, integ.sequenceno, callsegmentinst=sdi.flowattribute, calldiflow=sdi.flowattribute, issegmentinst=seg.instanceflag, iser_pr_type=ser.processingtype, ");
                sbQuery.AppendLine("is_servicetype=ser.servicetype, is_isintegser= ser.isintegser into #fw_des_integ_serv_map_dtl from #fw_des_integ_serv_map integ (nolock) left outer join #fw_des_service_dataitem sdi(nolock) on (sdi.servicename = integ.integservicename and sdi.segmentname = integ.integsegment and sdi.dataitemname = integ.integdataitem)");
                sbQuery.AppendLine("left outer join #fw_des_service ser(nolock) on (integ.integservicename = ser.servicename) left outer join #fw_des_service_segment seg(nolock) on (seg.servicename = integ.integservicename and seg.segmentname = integ.integsegment)");

                sbQuery.AppendLine("insert into #fw_extjs_link_grid_map (taskname, ui_name, subscribed_control_id)");
                sbQuery.AppendLine("select taskname, ui_name, subscribed_control_id from de_published_extjs_link_grid_map_vw");
                sbQuery.AppendLine(sCondition);

                sbQuery.AppendLine("insert into #fw_extjs_control_dtl (activityname, uiname, taskname, servicename, segmentname, sectiontype, controlid)");
                sbQuery.AppendLine("select activity_name, ui_name, task_name, service_name, segment_name, section_type, control_id from de_published_extjs_control_dtl_vw (nolock)");
                sbQuery.AppendLine(s_Condition);

                sbQuery.AppendLine("insert into #fw_des_chart_service_dataitem ( chart_id, chart_section, chart_attribute, servicename, segmentname, dataitemname, ispartofkey, mandatoryflag, flowattribute, defaultvalue ) select chart_id, chart_section, chart_attribute, servicename, segmentname, dataitemname, ispartofkey, mandatoryflag, flowattribute, defaultvalue from de_fw_des_publish_chart_service_dataitem_vw (nolock) ");
                sbQuery.AppendLine(s_Condition);

                sbQuery.AppendLine("insert into #fw_chart_task_map ( activity_name, ui_name, page_name, section_name, taskname, servicename, tasktype )");
                sbQuery.AppendLine("select activity_name, ui_name, page_name, section_name, taskname, servicename, tasktype from de_published_chart_task_map_vw (nolock) ");
                sbQuery.AppendLine(sCondition);

                sbQuery.AppendLine("insert #fw_des_tree_service_dataitem(componentname, activity_name, ui_name, page_name, section_name, servicename, taskname, segmentname, dataitemname, control_bt_synonym, clear_tree_before_population) ");
                sbQuery.AppendLine("select a.componentname, a.activity_name, a.ui_name, a.page_name, a.section_name, a.servicename, a.taskname, b.segmentname, c.dataitemname, a.control_bt_synonym, a.clear_tree_before_population ");
                sbQuery.AppendLine("from #fw_des_service_tree_map a (nolock) join #fw_des_service_segment b (nolock) on (b.servicename = a.servicename and b.segmentname = 'tree_data_segment') join #fw_des_service_dataitem c(nolock) on (c.servicename = b.servicename and c.segmentname = b.segmentname ) ");
                sbQuery.AppendLine("order by a.componentname, a.activity_name, a.ui_name, a.page_name, a.section_name, a.servicename, a.taskname, b.segmentname, c.dataitemname ");
                ExecuteNonQueryResult(sbQuery.ToString());

                if (GlobalVar.Snippet || GlobalVar.QuickLink)
                {
                    if (this.InsertITK().Equals(false))
                    {
                        WriteProfiler("Error while inserting ITK controls.");
                    }
                }

                this.CreateIndex();
                this.PublishECRUpdates();

                if (GlobalVar.IlboSchema)
                {
                    sbQuery = new StringBuilder();
                    sbQuery.AppendLine("insert into #fw_req_control_dtls (activityid, ilbocode, ControlName, ControlType,btsynonym, Hidden, Disabled, PageSize, Tabname, SystemControl)");
                    sbQuery.AppendLine("select activityid, lower(a.ilbocode) as 'ilbocode', lower(controlid) as 'ControlName', Type,b.btsynonym, 'false', 'false', '1', isnull(tabname, ''), 'false' from #fw_req_activity_ilbo a(nolock) join #fw_req_ilbo_control b(nolock) on (b.ilbocode = a.ilbocode) order by activityid, ilbocode, ControlName");
                    sbQuery.AppendLine("insert into #fw_req_control_dtls (activityid, ilbocode, ControlName, ControlType, Hidden, Disabled, PageSize, Tabname, SystemControl)");
                    sbQuery.AppendLine("select c.activityid, lower(b.ui_name) as 'ilbocode', case chart_section when 'chart_config_segment' then chart_id + '_config_grd' when 'chart_data_segment' then chart_id + '_data_grd' ");
                    sbQuery.AppendLine("when 'chart_series_segment' then chart_id + '_series_grd' end as 'ControlName', 'RSGrid', 'true', 'false', '12', case isnull(page_name, '') when '[mainscreen]' then '' end, 'false'");
                    sbQuery.AppendLine("from #fw_des_chart_service_segment a(nolock) join #fw_chart_task_map b(nolock) on (b.section_name = a.chart_id and b.servicename = a.servicename) join #fw_req_activity_ilbo c(nolock) on ( c.ilbocode = b.ui_name ) order by c.activityid, ilbocode, ControlName ");
                    sbQuery.AppendLine("insert into #fw_req_control_dtls (activityid, ilbocode, ControlName, ControlType, Hidden, Disabled, PageSize, Tabname, SystemControl)");
                    sbQuery.AppendLine("select distinct c.activityid, lower(b.ui_name) as 'ilbocode', lower(section_name) + '_data_grd' as 'ControlName', 'RSGrid', 'true', 'false', '12', case isnull(page_name, '') when '[mainscreen]' then '' end, 'false' from #fw_des_service_tree_map b(nolock) join #fw_req_activity_ilbo c(nolock) on ( c.ilbocode = b.ui_name ) order by c.activityid, ilbocode, ControlName ");

                    sbQuery.AppendLine("update #fw_req_control_dtls set disabled = 'true' from #fw_req_control_dtls a(nolock) join #fw_req_ilbo_control_property b (nolock) on (b.ilbocode = a.ilbocode and b.controlid = a.controlname) where propertyname = 'Enabled' and value = 'false'");
                    sbQuery.AppendLine("update #fw_req_control_dtls set hidden = 'true' from #fw_req_control_dtls a(nolock) join #fw_req_ilbo_control_property b (nolock) on (b.ilbocode = a.ilbocode and b.controlid = a.controlname) where propertyname = 'visible' and value = 'false'");
                    sbQuery.AppendLine("update #fw_req_control_dtls set controltype = case controltype when 'RSCheck' then 'CheckBox' when 'RSComboCtrl' then 'Combo' when 'RSEdit' then 'Edit' when 'RSEditCtrl' then 'Edit' when 'RSLIST' then 'List' when 'RSListEdit' then 'ListEdit' else controltype end");

                    sbQuery.AppendLine("insert into #fw_req_control_view_dtls (activityid, ilbocode, controlname, viewname,btsynonym, datalength, datatype, headertext, helptext, ishidden, isdisabled, ismultiselect, isreadonly, linkedview, viewsequence, viewtype, listvalues, precisionname)");
                    sbQuery.AppendLine("select activityid, lower(a.ilbocode) as 'ilbocode', lower(b.controlid) as 'controlid', lower(b.viewname) as 'viewname',b.btsynonym, length, lower(ltrim(rtrim(datatype))) as 'datatype', btdesc, btdesc, case displayflag when 'T' then 'false' else 'true' end as 'displayflag', 'false', 'false', '', '', -1, '', '', ltrim(rtrim(isnull(lower(precisiontype), ''))) from #fw_req_activity_ilbo a(nolock) join #fw_req_ilbo_view b(nolock) on (b.ilbocode = a.ilbocode) join #fw_req_bterm_synonym c (nolock) on (c.btsynonym = b.btsynonym) join #fw_req_bterm d(nolock) on (c.btname = d.btname) ");
                    sbQuery.AppendLine("insert into #fw_req_control_view_dtls (activityid, ilbocode, controlname, viewname, datalength, datatype, headertext, helptext, ishidden, isdisabled, ismultiselect, isreadonly, linkedview, viewsequence, viewtype, listvalues, precisionname)");
                    sbQuery.AppendLine("select c.activityid, lower(b.ui_name) as 'ilbocode', case a.chart_section when 'chart_config_segment' then a.chart_id + '_config_grd' when 'chart_data_segment' then a.chart_id + '_data_grd' when 'chart_series_segment' then a.chart_id + '_series_grd' end as 'controlid', '$$#viewname#$$', '2000', 'char', lower(dataitemname), lower(dataitemname), 'false', 'false', 'false', '', '', -1, 'edit', '', '' from #fw_des_chart_service_segment a(nolock) join #fw_chart_task_map b(nolock) on (b.section_name = a.chart_id and b.servicename = a.servicename) ");
                    sbQuery.AppendLine("join #fw_req_activity_ilbo c(nolock) on ( c.ilbocode = b.ui_name ) join #fw_des_chart_service_dataitem d (nolock) on ( d.chart_id = a.chart_id and d.chart_section = a.chart_section and d.servicename = a.servicename and d.segmentname = a.segmentname ) order by c.activityid, ilbocode, controlid");
                    sbQuery.AppendLine("insert into #fw_req_control_view_dtls (activityid, ilbocode, controlname, viewname, datalength, datatype, headertext, helptext, ishidden, isdisabled, ismultiselect, isreadonly, linkedview, viewsequence, viewtype, listvalues, precisionname)");
                    sbQuery.AppendLine("select distinct b.activityid, lower(a.ui_name) as 'ilbocode', lower(a.section_name) + '_data_grd' as 'controlid', '$$#viewname#$$', '2000', 'char', a.dataitemname, a.dataitemname, 'false', 'false', 'false', '', '', -1, 'edit', '', '' from #fw_des_tree_service_dataitem a(nolock) join #fw_req_activity_ilbo b(nolock) on ( b.ilbocode = a.ui_name ) join #fw_req_ilbo_view c (nolock) on (c.ilbocode = b.ilbocode and c.btsynonym = a.control_bt_synonym) order by b.activityid, ilbocode, controlid");
                    //sbQuery.AppendLine("select distinct b.activityid, lower(a.ui_name) as 'ilbocode', lower(a.section_name) + '_data_grd' as 'controlid', '$$#viewname#$$', '2000', 'char', e.btdesc, e.btdesc, 'false', 'false', 'false', '', '', -1, 'edit', '', '' from #fw_des_service_tree_map a(nolock) join #fw_req_activity_ilbo b(nolock) on ( b.ilbocode = a.ui_name ) join #fw_req_ilbo_view c (nolock) on (c.ilbocode = b.ilbocode and c.btsynonym = a.control_bt_synonym) join #fw_req_bterm_synonym d (nolock) on (d.btsynonym = c.btsynonym) join #fw_req_bterm e(nolock) on (e.btname = d.btname)order by b.activityid, ilbocode, controlid");

                    sbQuery.AppendLine("update #fw_req_control_view_dtls set viewname = (select count(*) from #fw_req_control_view_dtls b(nolock) where b.activityid = a.activityid and b.ilbocode = a.ilbocode and b.controlname = a.controlname and b.headertext <= a.headertext )from #fw_req_control_view_dtls a (nolock) where viewname = '$$#viewname#$$'");
                    sbQuery.AppendLine("update #fw_req_control_view_dtls set linkedview = lower(value) from #fw_req_control_view_dtls a(nolock) join #fw_req_ilbo_control_property b(nolock) on (b.ilbocode = a.ilbocode and b.controlid = a.controlname and b.viewname = a.viewname) where b.propertyname in ('linkedcomboview', 'linkedcheckview') and linkedview = ''");
                    sbQuery.AppendLine("update #fw_req_control_view_dtls set linkedview = lower(b.viewname) from #fw_req_control_view_dtls a(nolock) join #fw_req_ilbo_control_property b(nolock) on (b.ilbocode = a.ilbocode and b.controlid = a.controlname and b.value = a.viewname) where b.propertyname in ('linkedcomboview', 'linkedcheckview') and linkedview = ''");
                    sbQuery.AppendLine("update #fw_req_control_dtls set pagesize = value from #fw_req_control_dtls a(nolock) join #fw_req_ilbo_control_property b (nolock) on (b.ilbocode = a.ilbocode and b.controlid = a.controlname) where propertyname = 'VisibleRows'");
                    sbQuery.AppendLine("update #fw_req_control_view_dtls set viewsequence = column_no from #fw_req_control_view_dtls a (nolock) join #fw_req_activity_ilbo b (nolock) on (b.activityid = a.activityid and b.ilbocode = a.ilbocode) join #de_published_ui_grid c(nolock) on (c.activity_name = b.activityname and c.ui_name = b.ilbocode and c.control_id = a.controlname and c.view_name = a.viewname)");
                    sbQuery.AppendLine("update a set a.linkedview = lower(b.viewname) from #fw_req_control_view_dtls a(nolock) join #fw_req_control_view_dtls b(nolock) on (b.activityid = a.activityid and b.ilbocode = a.ilbocode and b.controlname = a.controlname) where a.ishidden = 'true' and a.viewsequence = -1 and b.ishidden = 'false' and a.linkedview = '' and b.viewsequence = -1");
                    sbQuery.AppendLine("update a set a.linkedview = lower(b.viewname) from #fw_req_control_view_dtls a(nolock) join #fw_req_control_view_dtls b(nolock) on (b.activityid = a.activityid and b.ilbocode = a.ilbocode and b.controlname = a.controlname) where a.ishidden = 'false' and a.viewsequence = -1 and b.ishidden = 'true' and a.linkedview = '' and b.viewsequence = -1");
                    sbQuery.AppendLine("update #fw_req_control_view_dtls set viewsequence = 0 where viewsequence = -1");
                    sbQuery.AppendLine("update #fw_req_control_view_dtls set viewtype = column_type from #fw_req_control_view_dtls a(nolock) join #fw_req_activity b(nolock) on (b.activityid = a.activityid) join #de_published_ui_grid c(nolock) on (c.activity_name = b.activityname and c.ui_name = a.ilbocode and c.control_id = a.controlname and c.view_name = a.viewname)");
                    sbQuery.AppendLine("insert into #de_ui_control_dtl (activity_name, ui_name, page_bt_synonym, section_bt_synonym, control_bt_synonym, column_bt_synonym, base_ctrl_type, ctrl_type, ctrl_id, view_name)");
                    sbQuery.AppendLine(string.Format("select activity_name, ui_name, page_bt_synonym, section_bt_synonym, control_bt_synonym, column_bt_synonym, base_ctrl_type, ctrl_type, ctrl_id, view_name from de_ui_control_dtl_vw (nolock) where customer_name = '{0}' and project_name = '{1}' and component_name = '{2}'", GlobalVar.Customer, GlobalVar.Project, GlobalVar.Component));
                    sbQuery.AppendLine("update #fw_req_control_view_dtls set viewtype = lower(base_ctrl_type) from #fw_req_control_view_dtls a(nolock) join #fw_req_activity b(nolock) on (b.activityid = a.activityid) join #de_ui_control_dtl c(nolock) on (c.activity_name = b.activityname and c.ui_name = a.ilbocode and c.ctrl_id = a.controlname and c.view_name = a.viewname)");
                    sbQuery.AppendLine("update #fw_req_control_view_dtls set viewtype = 'Edit' where viewtype = '' and ishidden = 'true'");
                    sbQuery.AppendLine("update #fw_req_control_view_dtls set defaultval = '0' where viewtype = 'RSCheck' or viewtype = 'CheckBox'");	//code added for bug id : PLF2.0_03803
                    sbQuery.AppendLine("insert into #fw_req_task_dtls (activityid, ilbocode, eventname, eventtype, navigationkey, associatedservice,eventmode)"); //TECH-16278
                    sbQuery.AppendLine("select distinct b.activityid, lower(a.ilbocode), taskname = lower(c.taskname), ltrim(rtrim(d.tasktype)), '', servicename = lower(a.servicename),eventmode = (case isnull(Autoupload,'')  when 'Y' then 'offline' else 'online' end) from #fw_des_ilbo_services a(nolock) join #fw_req_activity_ilbo b (nolock) on (b.ilbocode = a.ilbocode) join #fw_des_ilbo_service_view_datamap  c (nolock) on (b.ilbocode = c.ilbocode and a.servicename = c.servicename and b.activityid = c.activityid) join #fw_req_task d (nolock) on (c.taskname = d.taskname) join #fw_req_activity_ilbo_task e (nolock) on (c.ilbocode = e.ilbocode and b.activityid = e.activityid and c.taskname = e.taskname) "); //code modified for bug id : PLF2.0_03791
                    sbQuery.AppendLine("insert into #fw_req_task_dtls (activityid, ilbocode, eventname, eventtype, navigationkey, associatedservice)");  //code added for bug id : PLF2.0_03791
                    sbQuery.AppendLine("select distinct b.activityid, lower(a.ilbocode), taskname = lower(c.taskname), ltrim(rtrim(c.tasktype)), '', '' from #fw_req_activity_ilbo a (nolock) join #fw_req_activity_ilbo_task b (nolock) on (b.ilbocode = a.ilbocode and b.activityid = a.activityid) join #fw_req_task c (nolock) on (c.taskname = b.taskname) where lower(c.tasktype) in ('help', 'link', 'zoom') and c.taskname not in (select eventname from #fw_req_task_dtls(nolock))"); //code modified for bug id : PLF2.0_03791
                    sbQuery.AppendLine("update #fw_req_task_dtls set navigationkey = linkid from #fw_req_task_dtls a (nolock) join #fw_req_activity_ilbo_task b (nolock) on (b.activityid = a.activityid and b.ilbocode = a.ilbocode and b.taskname = a.eventname) join #fw_req_ilbo_linkuse  c(nolock) on (c.parentilbocode = b.ilbocode and c.taskname = b.taskname)");
                    sbQuery.AppendLine("insert #fw_req_link_dtls (activityid, linkid, ilbocode, targetcomponent, targetactivity, targetscreen)");
                    sbQuery.AppendLine("select distinct d.activityid, linkid, a.parentilbocode, lower(c.componentname) as 'TargetComponent', lower(c.activityname) as 'TargetActivity', lower(a.childilbocode) as 'TargetScreen' from #fw_req_ilbo_data_use a(nolock) join #fw_req_activity_ilbo d(nolock) on (d.ilbocode = a.parentilbocode) join #fw_req_activity_ilbo b(nolock) on (b.ilbocode = a.childilbocode) join #fw_req_activity c(nolock) on (c.activityid = b.activityid and c.activityname = b.activityname)");
                    sbQuery.AppendLine("insert #fw_req_pub_link_dtls (activityid, linkid, ilbocode, targetcomponent, targetactivity, targetscreen)");
                    sbQuery.AppendLine("select distinct ai.activityid, linkid = ilp.linkid, ilp.ilbocode, '', '', '' from #fw_req_ilbo_data_publish ilp (nolock) join #fw_req_activity_ilbo ai (nolock) on (ai.ilbocode = ilp.ilbocode) join #fw_req_ilbo_control c (nolock) on (ilp.ilbocode = c.ilbocode and ilp.controlid = c.controlid) union -- and ilp.flowtype in (1, 2)");
                    sbQuery.AppendLine("select ai.activityid, linkid = ilp.linkid, ilp.ilbocode, '', '', '' from #fw_req_ilbo_data_publish ilp (nolock) join #fw_req_activity_ilbo ai (nolock) on (ilp.ilbocode = ai.ilbocode and ilp.iscontrol = 0 and ilp.flowtype in (1, 2)) order by 2");
                    sbQuery.AppendLine("update #fw_req_task_dtls set navigationkey = eventname from #fw_req_task_dtls a (nolock) where eventtype ='Link' and isnull(navigationkey, '') = ''");
                    sbQuery.AppendLine("update #fw_req_ilbo_data_use set flowtype=1 from #fw_req_ilbo_data_use a(nolock) join #fw_req_ilbo_data_publish b(nolock) on b.ilbocode = a.childilbocode and b.linkid = a.linkid and a.dataitemname = b.dataitemname where b.flowtype = 0 and a.flowtype = 2");	//code added for bug id : PLF2.0_03803 //PLF2.0_06579
                    //code modified for bug id: PLF2.0_04389 ***start***
                    sbQuery.AppendLine("insert into #fw_req_link_subscription_dtls (linkid, ilbocode, controlname, viewname, ismultiinstance, flowdirection, itemname, event)");
                    sbQuery.AppendLine("select linkid, lower(parentilbocode), controlid  = lower(isnull(controlid,controlvariablename)), viewname  = lower(isnull(viewname,controlvariablename)), retrievemultiple= retrievemultiple, flowtype as 'flowattribute', dataitemname, taskname from #fw_req_ilbo_data_use (nolock)");
                    //code modified for bug id: PLF2.0_04389 ***end***
                    sbQuery.AppendLine("insert into #fw_req_pub_link_sub_dtls (linkid, ilbocode, controlname, viewname, ismultiinstance, flowdirection, itemname)");
                    sbQuery.AppendLine("select linkid = ilp.linkid, ilp.ilbocode, controlid = lower(ilp.controlid), viewname = lower(ilp.viewname), retrievemultiple= havemultiple, flowtype as 'flowattribute', dataitemname = lower(ilp.dataitemname) from #fw_req_ilbo_data_publish ilp(nolock) join #fw_req_activity_ilbo ai(nolock) on (ai.ilbocode = ilp.ilbocode) order by ilp.linkid");
                    ExecuteNonQueryResult(sbQuery.ToString());

                    sbQuery = new StringBuilder();
                    sbQuery.AppendLine("declare @ilbocode varchar(60), @controlid varchar(60), @viewname varchar(60), @btname varchar(60), @optiondesc varchar(8000), @optioncode varchar(8000)");
                    sbQuery.AppendLine("declare @value varchar(255), @optiondescvalue varchar(255)"); // Code added for bug id: PLF2.0_03462
                    sbQuery.AppendLine("declare cur_Enum cursor for");
                    sbQuery.AppendLine("select distinct ilbocode, controlid, a.viewname, b.btname from #fw_req_ilbo_view a(nolock) join #fw_req_bterm_synonym b(nolock) on (b.btsynonym = a.btsynonym) join #fw_req_bterm_enumerated_option c(nolock) on (c.btname = b.btname) where langid = 1 order by ilbocode, controlid --, viewname");
                    sbQuery.AppendLine("open cur_Enum");
                    sbQuery.AppendLine("fetch next from cur_Enum into @ilbocode, @controlid, @viewname, @btname");
                    sbQuery.AppendLine("while @@fetch_status = 0");
                    sbQuery.AppendLine("begin");
                    sbQuery.AppendLine("select @optiondesc = '', @optioncode = ''");
                    sbQuery.AppendLine("select @optiondesc = @optiondesc + optiondesc + ',' , @optioncode = @optioncode + optioncode + ',' from #fw_req_bterm_enumerated_option (nolock) where langid = 1 and btname = @btname order by sequenceno");
                    sbQuery.AppendLine("select @optiondesc = left(@optiondesc, len(@optiondesc) -1), @optioncode = left(@optioncode, len(@optioncode) -1)");

                    // Code commented for bug id: PLF2.0_03462 ***start***
                    /*sbQuery.AppendLine("update #fw_req_control_view_dtls set listvalues = @optiondesc where ilbocode = @ilbocode and controlname = @controlid and viewname = @viewname and ishidden = 'false'");
                    sbQuery.AppendLine("update #fw_req_control_view_dtls set listvalues = @optioncode where ilbocode = @ilbocode and controlname = @controlid and viewname = @viewname and ishidden = 'true'");*/
					// Code commented for bug id: PLF2.0_03462 ***end***

                    // Code added for bug id: PLF2.0_03462 ***start***
                    sbQuery.AppendLine("select @value = value from #fw_req_ilbo_control_property where ilbocode = @ilbocode and controlid = @controlid and viewname = @viewname and propertyname = 'default value'");
                    sbQuery.AppendLine("select @optiondescvalue = optiondesc from #fw_req_bterm_enumerated_option (nolock) where langid = 1 and btname = @btname and optioncode = @value order by sequenceno");
                    sbQuery.AppendLine("update #fw_req_control_view_dtls set listvalues = @optiondesc, defaultval = @optiondescvalue where ilbocode = @ilbocode and controlname = @controlid and viewname = @viewname and ishidden = 'false'");
                    sbQuery.AppendLine("update #fw_req_control_view_dtls set listvalues = @optioncode, defaultval = @value where ilbocode = @ilbocode and controlname = @controlid and viewname = @viewname and ishidden = 'true'");
                    // Code added for bug id: PLF2.0_03462 ***end***

                    sbQuery.AppendLine("fetch next from cur_Enum into @ilbocode, @controlid, @viewname, @btname");
                    sbQuery.AppendLine("end");
                    sbQuery.AppendLine("close cur_Enum");
                    sbQuery.AppendLine("deallocate cur_Enum");
                    ExecuteNonQueryResult(sbQuery.ToString());

                    sbQuery = new StringBuilder();
                    sbQuery.AppendLine("select a.activityid, lower(a.activityname) 'activityname', a.activitydesc, lower(b.ilbocode) 'ilbocode', c.description, 0 'datasavingtask' into #fw_req_activity_details from #fw_req_activity a(nolock) join #fw_req_activity_ilbo b(nolock) on (b.activityid = a.activityid) join #fw_req_ilbo c(nolock) on (c.ilbocode = b.ilbocode) order by a.activityid, a.activityname, b.ilbocode");
                    sbQuery.AppendLine("update #fw_req_activity_details set datasavingtask = 1 from #fw_req_activity_details a (nolock) join #fw_req_activity_ilbo_task b(nolock) on (b.activityid = a.activityid and b.ilbocode = a.ilbocode and b.datasavingtask = 1)");
                    ExecuteNonQueryResult(sbQuery.ToString());

                    GlobalVar.Query = "select a.activityid, a.activityname, a.activitydesc, a.ilbocode, a.description, a.datasavingtask from #fw_req_activity_details a(nolock) order by a.activityid, a.activityname, a.ilbocode for xml raw, root('root')";
                    BindToLinq("ilbo");
                    GlobalVar.Query = "select activityid, lower(ilbocode) 'ilbocode', controlname, controltype, hidden, disabled, pagesize, tabname, systemcontrol,lower(btsynonym) as 'btsynonym' from #fw_req_control_dtls control(nolock) for xml raw, root('root')";//TECH-33808
                    BindToLinq("control");
                    // GlobalVar.Query = string.Format("select activityid, lower(ilbocode) 'ilbocode', controlname, viewname, datalength, datatype, headertext, helptext, ishidden, isdisabled, ismultiselect, isreadonly, linkedview, viewsequence, viewtype, listvalues, precisionname from #fw_req_control_view_dtls (nolock) order by ilbocode, controlname, viewsequence for xml raw, root('root')"); // Code commented for bug id: PLF2.0_03462
                    GlobalVar.Query = string.Format("select activityid, lower(ilbocode) 'ilbocode', controlname, viewname, datalength, datatype, headertext, helptext, ishidden, isdisabled, ismultiselect, isreadonly, linkedview, viewsequence, viewtype, listvalues, precisionname, Isnull(defaultval,'') defaultval,lower(btsynonym) as 'btsynonym' from #fw_req_control_view_dtls (nolock) order by ilbocode, controlname, viewsequence for xml raw, root('root')"); // Code added for bug id: PLF2.0_03462,TECH-33808
                    BindToLinq("view");
                    GlobalVar.Query = string.Format("select a.activityid, lower(a.ilbocode) 'ilbocode', eventname, eventtype, navigationkey, associatedservice,eventmode from #fw_req_task_dtls a (nolock)  join #fw_req_activity_ilbo_task b (nolock) on (a.ilbocode collate DATABASE_DEFAULT = b.ilbocode and a.eventname collate DATABASE_DEFAULT = b.taskname) where isnull(b.Autoupload,'N') <> 'Y' order by a.activityid, a.ilbocode, eventname for xml raw, root('root')"); //TECH-16278
                    BindToLinq("event");
                    GlobalVar.Query = string.Format("SELECT DISTINCT activityid = a.activityid,ilbocode = Lower( a.ilbocode ), eventname = Lower( a.taskname ), eventtype = Ltrim( Rtrim( b.tasktype ) ), isnull(navigationkey,'') 'navigationkey', eventmode = 'offline' FROM  #fw_req_activity_ilbo_task a(nolock) inner JOIN #fw_req_task b (nolock)  ON ( b.taskname collate DATABASE_DEFAULT = a.taskname) left Join #fw_req_task_dtls c (nolock) ON (a.ilbocode collate DATABASE_DEFAULT = c.ilbocode and a.taskname collate DATABASE_DEFAULT = c.eventname)  where a.Autoupload = 'Y' for xml raw, root('root')"); //TECH-16278
                    BindToLinq("offlineevents");
                    GlobalVar.Query = string.Format("select distinct activityid, linkid, lower(ilbocode) 'ilbocode', targetcomponent, targetactivity, targetscreen from #fw_req_link_dtls (nolock) for xml raw, root('root')");
                    BindToLinq("link");
                    GlobalVar.Query = string.Format("select distinct lower(ilbocode) 'ilbocode', linkid, controlname, viewname, flowdirection, ismultiinstance, itemname, event from #fw_req_link_subscription_dtls (nolock) order by controlname, viewname for xml raw, root('root')");    //code modified for bug id: PLF2.0_04389
                    BindToLinq("subtolink");
                    GlobalVar.Query = string.Format("select activityid, linkid, lower(ilbocode) as 'ilbocode', targetcomponent, targetactivity, targetscreen from #fw_req_pub_link_dtls (nolock) for xml raw, root('root')");
                    BindToLinq("publink");
                    GlobalVar.Query = string.Format("select distinct lower(ilbocode) as 'ilbocode', linkid, controlname, viewname, flowdirection, ismultiinstance, itemname from #fw_req_pub_link_sub_dtls (nolock) order by controlname, viewname for xml raw, root('root')");
                    BindToLinq("pubtolinks");
                }

                if (GlobalVar.ServiceSchema)
                {
                    sbQuery = new StringBuilder();
                    sbQuery.AppendLine("set concat_null_yields_null off");
                    sbQuery.AppendLine("declare @indi varchar(8000), @outdi varchar(8000), @iodi varchar(8000), @scdi varchar(8000), @servicename varchar(60), @segmentname varchar(60), @segseqno int");
                    sbQuery.AppendLine("select @segseqno = 0");
                    sbQuery.AppendLine("if not exists (select 'x' from #fw_req_bterm_synonym (nolock) where btsynonym = 'ctxt_user') begin insert into #fw_req_bterm_synonym (btname, btsynonym) values ('ctxt_user','ctxt_user') end");
                    sbQuery.AppendLine("if not exists (select 'x' from #fw_req_bterm_synonym (nolock) where btsynonym = 'user') begin insert into #fw_req_bterm_synonym (btname, btsynonym) values ('user','user') end");
                    sbQuery.AppendLine("if not exists (select 'x' from #fw_req_bterm_synonym (nolock) where btsynonym = 'Ctxt_Language') begin insert into #fw_req_bterm_synonym (btname, btsynonym) values ('Ctxt_Language','Ctxt_Language') end");
                    sbQuery.AppendLine("if not exists (select 'x' from #fw_req_bterm_synonym (nolock) where btsynonym = 'Language') begin insert into #fw_req_bterm_synonym (btname, btsynonym) values ('Language','Language') end");
                    sbQuery.AppendLine("if not exists (select 'x' from #fw_req_bterm_synonym (nolock) where btsynonym = 'Ctxt_OUInstance') begin insert into #fw_req_bterm_synonym (btname, btsynonym) values ('Ctxt_OUInstance','Ctxt_OUInstance') end");
                    sbQuery.AppendLine("if not exists (select 'x' from #fw_req_bterm_synonym (nolock) where btsynonym = 'OUInstance') begin insert into #fw_req_bterm_synonym (btname, btsynonym) values ('OUInstance','OUInstance') end");
                    sbQuery.AppendLine("if not exists (select 'x' from #fw_req_bterm_synonym (nolock) where btsynonym = 'Ctxt_Role') begin insert into #fw_req_bterm_synonym (btname, btsynonym) values ('Ctxt_Role','Ctxt_Role') end");
                    sbQuery.AppendLine("if not exists (select 'x' from #fw_req_bterm_synonym (nolock) where btsynonym = 'Role') begin insert into #fw_req_bterm_synonym (btname, btsynonym) values ('Role','Role') end");
                    sbQuery.AppendLine("if not exists (select 'x' from #fw_req_bterm_synonym (nolock) where btsynonym = 'Ctxt_Service') begin insert into #fw_req_bterm_synonym (btname, btsynonym) values ('Ctxt_Service','Ctxt_Service') end");
                    sbQuery.AppendLine("if not exists (select 'x' from #fw_req_bterm_synonym (nolock) where btsynonym = 'Service') begin insert into #fw_req_bterm_synonym (btname, btsynonym) values ('Service','Service') end");
                    sbQuery.AppendLine("insert #fw_des_service_dataitem_dtl (servicename, segmentname, dataitemname, datatype, datalength, ispartofkey, flowattribute, mandatoryflag, defaultvalue, BT_Type, isModel)");
                    sbQuery.AppendLine("select distinct lower(di.servicename) as 'servicename', lower(di.segmentname) as segmentname, di.dataitemname, case when bt.datatype = 'LONG' or bt.datatype='INT' or bt.datatype='INTEGER' then '0'");
                    sbQuery.AppendLine("when bt.datatype = 'STRING' or bt.datatype='DATETIME' or bt.datatype='CHAR' or bt.datatype = 'NVARCHAR' or bt.datatype='DATE' or bt.datatype='TIME' or bt.datatype = 'ENUMERATED' ");
                    sbQuery.AppendLine("or bt.datatype='DATE-TIME' then '2' when bt.datatype = 'DOUBLE' or bt.datatype='NUMERIC' then '1' end as datatype, datalength = bt.length, di.ispartofkey, di.flowattribute, di.mandatoryflag,");
                    sbQuery.AppendLine("di.defaultvalue, bt.datatype 'BT_Type', '' from #fw_des_service_dataitem di (nolock), #fw_req_bterm bt (nolock), #fw_req_bterm_synonym sy (nolock) where sy.btsynonym = di.dataitemname and sy.btname = bt.btname");
                    sbQuery.AppendLine("union select distinct lower(di.servicename) as 'servicename', lower(di.segmentname) as segmentname, di.dataitemname, case when bt.datatype = 'LONG' or bt.datatype='INT' or bt.datatype='INTEGER' then '0'");
                    sbQuery.AppendLine("when bt.datatype = 'STRING' or bt.datatype='DATETIME' or bt.datatype='CHAR' or bt.datatype = 'NVARCHAR' or bt.datatype='DATE' or bt.datatype='TIME' or bt.datatype = 'ENUMERATED' or bt.datatype='DATE-TIME' then '2'");
                    sbQuery.AppendLine("when bt.datatype = 'DOUBLE' or bt.datatype='NUMERIC' then '1'end  as datatype, datalength = bt.length, 0, 1, 1, '', bt.datatype 'BT_Type', '' from #fw_des_di_parameter di (nolock) join #fw_des_br_logical_parameter br(nolock) on (br.methodid = di.methodid and br.logicalparametername = di.parametername)");
                    sbQuery.AppendLine("join #fw_req_bterm bt (nolock) on (bt.btname = br.btname) join  #fw_req_bterm_synonym sy (nolock) on (sy.btname = bt.btname) where di.segmentname = 'fw_context' order by servicename, segmentname, di.dataitemname");
                    sbQuery.AppendLine("insert into #fw_des_service_segment_dtl(servicename, segmentname, instanceflag, parentsegment, flowdirection, mandatoryflag, ispartofhierarchy, isscratchdipartofsegment, segmentsequence)");
                    sbQuery.AppendLine("select distinct lower(a.servicename) as 'servicename', lower(a.segmentname), instanceflag, parentsegmentname, 0, 0, instanceflag, 0, segmentsequence from #fw_des_service_segment a (nolock) join #fw_des_di_parameter b(nolock) on (b.servicename = a.servicename and b.segmentname = a.segmentname)");
                    sbQuery.AppendLine("union select distinct lower(a.servicename) as 'servicename', lower(a.segmentname), instanceflag, parentsegmentname, 0, 0, instanceflag, 0, segmentsequence from #fw_des_service_segment a (nolock) join #fw_des_ilbo_service_view_datamap b(nolock) on (b.servicename = a.servicename and b.segmentname = a.segmentname)");
                    sbQuery.AppendLine("union select distinct lower(a.servicename) as 'servicename', lower(a.segmentname), instanceflag, parentsegmentname, 0, 0, instanceflag, 0, segmentsequence from #fw_des_service_segment a (nolock) join #fw_des_integ_serv_map_dtl b (nolock) on (b.callingservicename = a.servicename and (b.CallerSegName = a.segmentname or b.ISSegName = a.segmentname))");
                    sbQuery.AppendLine("union select distinct lower(servicename) as 'servicename', segmentName, 0, '', 2, 1, 1, '', '4' from #fw_des_di_parameter (nolock) where segmentname = 'fw_context' order by segmentsequence");

                    sbQuery.AppendLine("update #fw_des_service_dataitem_dtl set isModel = '0' Where flowattribute = 3");
                    sbQuery.AppendLine("update #fw_des_service_dataitem_dtl set defaultvalue = '' where bt_type = 'char' and flowattribute = 3");
                    sbQuery.AppendLine("update #fw_des_service_dataitem_dtl set flowattribute = 3, isModel = '1' from #fw_des_service_dataitem_dtl a (nolock) join #fw_des_service_segment c (nolock) on (c.servicename = a.servicename and c.segmentname = a.segmentname) join #fw_des_service d (nolock) on (d.servicename = c.servicename) where a.flowattribute = 0 and d.isintegser = 0");
                    sbQuery.AppendLine("and not exists (select 'x' from #fw_des_ilbo_service_view_datamap b(nolock) where b.servicename = a.servicename and b.segmentname = a.segmentname and b.dataitemname = a.dataitemname)");
                    sbQuery.AppendLine("update #fw_des_service_dataitem_dtl set isModel = '2' Where isnull(isModel, '') = ''");

                    sbQuery.AppendLine("select distinct a.servicename, a.segmentname, a.dataitemname, a.methodid, a.parametername, b.flowattribute into #fw_des_dataitem_parameter from #fw_des_di_parameter a (nolock) join #fw_des_service_dataitem b (nolock) on (b.servicename = a.servicename and b.segmentname = a.segmentname and b.dataitemname = a.dataitemname) ");
                    sbQuery.AppendLine("select distinct a.servicename, a.segmentname, a.dataitemname, count(*) as incnt into #fw_des_dataitem_parameter_cnt from #fw_des_dataitem_parameter a(nolock) join #fw_des_br_logical_parameter c (nolock) on (c.methodid = a.methodid and c.logicalparametername = a.parametername) where a.flowattribute = 2 and c.flowdirection = 0 group by a.servicename, a.segmentname, a.dataitemname");
                    sbQuery.AppendLine("insert into #fw_des_parameter_cnt (servicename, segmentname, dataitemname, br_totcnt) select distinct a.servicename, a.segmentname, a.dataitemname, count(*) as br_totcnt from #fw_des_dataitem_parameter a (nolock) join #fw_des_br_logical_parameter c(nolock) on (c.methodid = a.methodid and c.logicalparametername = a.parametername) where a.flowattribute = 2 group by a.servicename, a.segmentname, a.dataitemname");
                    sbQuery.AppendLine("update #fw_des_parameter_cnt set br_incnt = incnt from #fw_des_parameter_cnt brcnt(nolock) join (select distinct a.servicename, a.segmentname, a.dataitemname, count(*) as incnt from #fw_des_dataitem_parameter a (nolock) ");
                    sbQuery.AppendLine("join #fw_des_br_logical_parameter c (nolock) on (c.methodid = a.methodid and c.logicalparametername = a.parametername) where a.flowattribute = 2 and c.flowdirection = 0");
                    sbQuery.AppendLine("group by a.servicename, a.segmentname, a.dataitemname)brincnt on (brcnt.servicename = brincnt.servicename and brcnt.segmentname = brincnt.segmentname and brcnt.dataitemname = brincnt.dataitemname)");
                    sbQuery.AppendLine("update #fw_des_service_dataitem_dtl set flowattribute = 0 from #fw_des_service_dataitem_dtl a (nolock) join #fw_des_parameter_cnt b (nolock) on (b.servicename = a.servicename and b.segmentname = a.segmentname and ");
                    sbQuery.AppendLine("b.dataitemname = a.dataitemname and b.br_totcnt = b.br_incnt and not exists (select 'x' from #fw_des_caller_integ_serv_map c (nolock) where c.integservicename= b.servicename and c.integsegment = b.segmentname and c.integdataitem = b.dataitemname))");
                    
                    sbQuery.AppendLine("update #fw_des_service_segment_dtl set IsPartOfHierarchy = case when parentsegment is null then 1 when segmentname = parentsegment then 0 else 0 end");
                    sbQuery.AppendLine("update #fw_des_service_segment_dtl set mandatoryflag = 1 from #fw_des_service_segment_dtl a (nolock) join #fw_des_service_segment b (nolock) on (b.servicename = a.servicename and b.segmentname = a.segmentname) where a.flowDirection in (0, 2) and a.segmentname not in ('fw_context', 'errordetails') and a.instanceflag = 1 and b.mandatoryflag = 1");
                    sbQuery.AppendLine("update #fw_des_service_segment_dtl set indicnt = isnull(incnt,0), outdicnt = isnull(outcnt,0), iodicnt = isnull(iocnt,0), scdicnt = isnull(sccnt,0), dicnt = isnull(incnt,0) + isnull(outcnt, 0) + isnull(iocnt, 0) + isnull(sccnt, 0), flowdirection = case when isnull(iocnt, 0) > 0 or (isnull(incnt, 0) > 0 and isnull(outcnt, 0) > 0) then 2 when isnull(incnt, 0) > 0 then 0");
                    sbQuery.AppendLine("when isnull(outcnt, 0) > 0 then 1 else 3 end, IsScratchDIPartOfSegment = case when isnull(sccnt, 0) > 0 then 555 end from #fw_des_service_segment_dtl a(nolock) join (select servicename, segmentname, isnull(count(case when flowattribute = 0 then dataitemname end), 0) as incnt, isnull(count(case when flowattribute = 1 then dataitemname end), 0) as outcnt,");
                    sbQuery.AppendLine("isnull(count(case when flowattribute = 2 then dataitemname end), 0) as iocnt, isnull(count(case when flowattribute = 3 then dataitemname end), 0) as sccnt from #fw_des_service_dataitem_dtl (nolock) group by servicename, segmentname) cnt on (cnt.servicename collate database_default = a.servicename and cnt.segmentname collate database_default = a.segmentname)");
                    sbQuery.AppendLine("update #fw_des_service_segment_dtl set flowdirection = 3 from #fw_des_service_segment_dtl where scDICnt = diCnt");
                    sbQuery.AppendLine("declare cur_diname cursor for select distinct servicename, segmentname from #fw_des_service_segment_dtl b (nolock) order by servicename");
                    sbQuery.AppendLine("open cur_diname fetch next from cur_diname into @servicename, @segmentname");
                    sbQuery.AppendLine("while @@fetch_status = 0");
                    sbQuery.AppendLine("begin");
                    sbQuery.AppendLine("select @indi = '', @outdi = '', @iodi = '', @scdi = ''");
                    sbQuery.AppendLine("select @indi = @indi + case when flowattribute = 0 then isnull( cast(dataitemname as varchar(8000)) + ';', cast(dataitemname as varchar(8000))) end,");
                    sbQuery.AppendLine("@outdi = @outdi + case when flowattribute = 1 then isnull( cast(dataitemname as varchar(8000)) + ';', cast(dataitemname as varchar(8000))) end,");
                    sbQuery.AppendLine("@iodi = @iodi + case when flowattribute = 2 then isnull( cast(dataitemname as varchar(8000)) + ';', cast(dataitemname as varchar(8000))) end,");
                    sbQuery.AppendLine("@scdi = @scdi + case when flowattribute = 3 then isnull( cast(dataitemname as varchar(8000)) + ';', cast(dataitemname as varchar(8000))) end");
                    sbQuery.AppendLine("from #fw_des_service_dataitem_dtl (nolock) where servicename = @servicename and segmentname = @segmentname group by servicename, segmentname, dataitemname, flowattribute");
                    sbQuery.AppendLine("update #fw_des_service_segment_dtl set indinames = @indi, outdinames = @outdi, iodinames = @iodi, scdinames = @scdi, dinames = @indi+@outdi+@iodi+@scdi where servicename = @servicename and segmentname = @segmentname");
                    sbQuery.AppendLine("fetch next from cur_diname into @servicename, @segmentname");
                    sbQuery.AppendLine("end");
                    sbQuery.AppendLine("close cur_diname");
                    sbQuery.AppendLine("deallocate cur_diname");
                    sbQuery.AppendLine("set concat_null_yields_null on");
                    ExecuteNonQueryResult(sbQuery.ToString());

                    sbQuery = new StringBuilder();
                    sbQuery.AppendLine("set concat_null_yields_null off");
                    sbQuery.AppendLine("declare @inseg varchar(8000), @outseg varchar(8000), @ioseg varchar(8000), @scseg varchar(8000), @datasegs varchar(8000), @servicename varchar(60)");
                    sbQuery.AppendLine("insert #fw_des_service_dtl (servicename, servicetype, insegcnt, outsegcnt, iosegcnt, scsegcnt) select distinct lower(ser.servicename), ser.servicetype, count(case when isnull(inseg, '') <> '' then inseg end) as incnt, count(case when isnull(outseg, '') <> '' then outseg end) as outcnt, ");
                    sbQuery.AppendLine("count(case when isnull(ioseg, '') <> '' then ioseg end) as iocnt, count(case when isnull(scseg, '') <> '' then scseg end) as sccnt from #fw_des_service ser (nolock) join ");
                    sbQuery.AppendLine("(select distinct lower(a.servicename) as servicename,a.servicetype, isnull(case when flowattribute = 0 then isnull(b.segmentname, '') end,'') as inseg, isnull(case when flowattribute = 1 then isnull(b.segmentname, '') end,'') as outseg, isnull(case when flowattribute = 2 then isnull(b.segmentname, '') end,'') as ioseg,");
                    sbQuery.AppendLine("isnull(case when flowattribute = 3 then isnull(b.segmentname, '') end,'') as scseg from #fw_des_service a (nolock) join #fw_des_service_segment_dtl b (nolock) on (b.servicename = a.servicename) join #fw_des_service_dataitem_dtl c (nolock) on (c.servicename = b.servicename and c.segmentname = b.segmentname)");
                    sbQuery.AppendLine("where a.componentname = '" + GlobalVar.Component + "' and b.segmentname <> 'fw_context' group by a.servicename,a.servicetype,b.segmentname,c.flowattribute)cnt on (cnt.servicename = ser.servicename) group by ser.servicename, ser.servicetype");
                    sbQuery.AppendLine("declare cur_segment cursor for");
                    sbQuery.AppendLine("select distinct servicename from #fw_des_service_segment_dtl (nolock) where segmentname <> 'fw_context' order by servicename");
                    sbQuery.AppendLine("open cur_segment");
                    sbQuery.AppendLine("fetch next from cur_segment into @servicename");
                    sbQuery.AppendLine("while @@fetch_status = 0");
                    sbQuery.AppendLine("begin");
                    sbQuery.AppendLine("select @inseg = '', @outseg = '', @ioseg = '', @scseg = ''");
                    sbQuery.AppendLine("select @inseg = @inseg + case when flowattribute = 0 then isnull( cast(a.segmentname as varchar(8000)) + ';' , cast(a.segmentname as varchar(8000))) end , @outseg  = @outseg + case when flowattribute = 1 then isnull( cast(a.segmentname as varchar(8000)) + ';',");
                    sbQuery.AppendLine("cast(a.segmentname as varchar(8000))) end, @ioseg  = @ioseg + case when flowattribute = 2 then isnull( cast(a.segmentname as varchar(8000)) + ';' , cast(a.segmentname as varchar(8000))) end, @scseg  = @scseg + case when flowattribute = 3 then");
                    sbQuery.AppendLine("isnull( cast(a.segmentname as varchar(8000)) + ';', cast(a.segmentname as varchar(8000))) end from #fw_des_service_dataitem_dtl a (nolock) join #fw_des_service_segment_dtl b(nolock) on (b.servicename = a.servicename and b.segmentname = a.segmentname) where a.servicename = @servicename and a.segmentname <> 'fw_context' group by a.servicename, a.segmentname, a.flowattribute");
                    sbQuery.AppendLine("select @datasegs = ''");
                    sbQuery.AppendLine("select @datasegs = @datasegs + isnull(cast(segmentname as varchar(8000)) + ';' , cast(segmentname as varchar(8000))) from #fw_des_service_segment_dtl (nolock) where servicename = @servicename and segmentname <> 'fw_context'");
                    sbQuery.AppendLine("update #fw_des_service_dtl set insegs = @inseg, outsegs = @outseg, iosegs = @ioseg, scsegs = @scseg, datasegs = @datasegs where servicename = @servicename");
                    sbQuery.AppendLine("fetch next from cur_segment into @servicename");
                    sbQuery.AppendLine("end");
                    sbQuery.AppendLine("close cur_segment");
                    sbQuery.AppendLine("deallocate cur_segment");
                    sbQuery.AppendLine("update #fw_des_service_dtl set pscnt = pscn from #fw_des_service a (nolock), (select servicename, count(*)  as pscn from #fw_des_processsection (nolock) group by servicename)cnt where #fw_des_service_dtl.servicename collate database_default = cnt.servicename");
                    sbQuery.AppendLine("update #fw_des_service_dtl set segcnt = segcn from #fw_des_service a(nolock), (select servicename, count(*) as segcn from #fw_des_service_segment_dtl (nolock) where segmentname <> 'fw_context' group by servicename)cnt where #fw_des_service_dtl.servicename collate database_default = cnt.servicename");
                    sbQuery.AppendLine("set concat_null_yields_null on");
                    ExecuteNonQueryResult(sbQuery.ToString());

                    sbQuery = new StringBuilder();
                    sbQuery.AppendLine("insert into #fw_des_processsection_dtl (servicename, sectionname, sectiontype, seqNo, controlexpression, loopingStyle, loopinstanceflag) select lower(a.servicename) as servicename, lower(a.sectionname) as sectionname, sectiontype, a.sequenceno as 'seqNo', isnull(a.controlexpression,'') as controlexpression, processingtype,");
                    sbQuery.AppendLine("loopinstanceflag = case when processingtype <> 1 then '' else processingtype end from #fw_des_processsection a(nolock) order by servicename");
                    sbQuery.AppendLine("update #fw_des_processsection_dtl set brsCnt = brcn from #fw_des_service a(nolock), (select servicename, sectionname, count(isnull(methodid,'')) as brcn from #fw_des_processsection_br_is (nolock) group by servicename, sectionname)cnt");
                    sbQuery.AppendLine("where #fw_des_processsection_dtl.servicename collate database_default = cnt.servicename and #fw_des_processsection_dtl.sectionname collate database_default = cnt.sectionname");
                    sbQuery.AppendLine("update #fw_des_processsection_dtl set loopCausingSegment = lower(c.segmentname) from #fw_des_processsection_dtl a (nolock), #fw_des_br_logical_parameter b (nolock), #fw_des_di_parameter c (nolock), #fw_des_service_segment d (nolock) where a.loopingstyle = 1 and b.flowdirection = 0");
                    sbQuery.AppendLine("and c.methodid = b.methodid and c.parametername = b.logicalparametername and c.sectionname = a.sectionname and d.servicename = a.servicename and d.segmentname = c.segmentname and d.instanceflag = 1");
                    sbQuery.AppendLine("update #fw_des_processsection_dtl set ceInstDepFlag = '0'");
                    sbQuery.AppendLine("update #fw_des_processsection_dtl set ceInstDepFlag = 1 where loopingstyle = 1 and isnull(loopCausingSegment, '') = ''");
                    sbQuery.AppendLine("update #fw_des_processsection_dtl set ceInstDepFlag = 1 where loopingstyle = 0 and isnull(loopCausingSegment, '') <> ''");
                    ExecuteNonQueryResult(sbQuery.ToString());

                    sbQuery = new StringBuilder();
                    sbQuery.AppendLine("select ps.servicename, ps.sequenceno, ps.isbr, lower(br.methodname) as methodname, br.methodid, ps.integservicename, ps.controlexpression, ps.connectivityflag, ps.executionflag, ps.sectionname, br.broname, br.dispid, br.accessesdatabase, br.operationtype, br.systemgenerated,lower(sp.spname) as spname,");
                    sbQuery.AppendLine("case when isnull(sp.sperrorprotocol,'') = '' then 0 else sp.sperrorprotocol end as 'sperrorprotocol' into #tmptable from #fw_des_processsection_br_is ps (nolock) left outer join #fw_des_businessrule br (nolock) on (ps.methodid = br.methodid and ps.isbr = 1) left outer join #fw_des_sp sp (nolock) on (ps.methodid = sp.methodid)");
                    sbQuery.AppendLine("select distinct tmp.servicename, seqno = tmp.sequenceno, ismethod = tmp.isbr, progid = bro.progid, bro.clsid, tmp.methodname, tmp.dispid, tmp.integservicename, tmp.controlexpression, tmp.connectivityflag, tmp.executionflag, loopflag = tmp.sequenceno, loopsegment = tmp.methodname, tmp.methodid, tmp.sectionname, tmp.broname,");
                    sbQuery.AppendLine("tmp.accessesdatabase, ceinstdepflag = '', recordsetfetch = '', tmp.spname, tmp.operationtype, tmp.systemgenerated, tmp.sperrorprotocol, brppcnt = 0, brlpcnt = 0 into #fw_des_br_details_dtl from #tmptable tmp (nolock) left outer join #fw_des_bro bro (nolock) on(tmp.broname = bro.broname) order by tmp.sequenceno");

                    sbQuery.AppendLine("insert #fw_des_br_pp_count(servicename,sectionname,methodid,ppcnt)");
                    sbQuery.AppendLine("select dip.servicename, dip.sectionname, dip.methodid, count(dip.dataitemname) as ppcnt from #fw_des_br_logical_parameter lp (nolock) join #fw_des_di_parameter dip (nolock) on (dip.methodid = lp.methodid and dip.parametername = lp.logicalparametername) group by dip.servicename, dip.sectionname, dip.methodid");
                    sbQuery.AppendLine("update a set brppcnt = ppcnt from #fw_des_br_details_dtl a(nolock) join (select servicename, sectionname, methodid, ppcnt from #fw_des_br_pp_count (nolock)) cnt on (a.servicename = cnt.servicename and a.sectionname = cnt.sectionname and a.methodid = cnt.methodid)");
                    sbQuery.AppendLine("insert #fw_des_br_lp_count (servicename,sectionname,methodid,lpcnt)");
                    sbQuery.AppendLine("select dip.servicename, dip.sectionname, dip.methodid, count(dip.dataitemname) as lpcnt from #fw_des_br_logical_parameter lp (nolock) join #fw_des_di_parameter dip (nolock) on (lp.flowdirection = 1 and dip.methodid = lp.methodid and dip.parametername = lp.logicalparametername) join #fw_des_service_segment seg (nolock) on (seg.servicename = dip.servicename and seg.segmentname = dip.segmentname and instanceflag = 1) group by dip.servicename, dip.sectionname, dip.methodid");
                    sbQuery.AppendLine("update a set brlpcnt = lpcnt from #fw_des_br_details_dtl a(nolock) join (select servicename, sectionname, methodid, lpcnt from #fw_des_br_lp_count (nolock))cnt on (a.servicename = cnt.servicename and a.sectionname = cnt.sectionname and a.methodid = cnt.methodid)");

                    sbQuery.AppendLine("update #fw_des_br_details_dtl set loopsegment = ''");
                    sbQuery.AppendLine("select distinct c.servicename, b.methodid, c.segmentname into #fw_loopsegmentdtls from #fw_des_br_logical_parameter b(nolock) join #fw_des_di_parameter c(nolock) on (b.logicalparametername = c.parametername and c.methodid = b.methodid and b.flowdirection in (0, 2)) join #fw_des_service_segment d (nolock) on (d.servicename = c.servicename and d.segmentname = c.segmentname and d.instanceflag = 1)");
                    sbQuery.AppendLine("join #fw_des_service_dataitem e (nolock) on (e.servicename = d.servicename and e.segmentname = d.segmentname and e.dataitemname = c.dataitemname and e.flowattribute in (0, 2))");	//code added for bug id : PLF2.0_03803
                    sbQuery.AppendLine("update #fw_des_br_details_dtl set loopsegment = lower(b.segmentname) from #fw_des_br_details_dtl a (nolock) join #fw_loopsegmentdtls b(nolock) on (b.servicename = a.servicename and b.methodid = a.methodid) ");

                    sbQuery.AppendLine("update #fw_des_br_details_dtl set ceInstDepFlag = '0'");
                    sbQuery.AppendLine("update #fw_des_br_details_dtl set ceInstDepFlag = 1 from #fw_des_br_details_dtl a (nolock) join #fw_des_processsection b(nolock) on (b.servicename = a.servicename and b.sectionname = a.sectionname and b.processingtype = 0 and isnull(loopsegment, '') = '')");
                    sbQuery.AppendLine("update #fw_des_br_details_dtl set ceInstDepFlag = 0 from #fw_des_br_details_dtl a (nolock) join #fw_des_processsection b(nolock) on (b.servicename = a.servicename and b.sectionname = a.sectionname and b.processingtype = 0 and isnull(loopsegment, '') <> '')");
                    sbQuery.AppendLine("update #fw_des_br_details_dtl set executionflag = 1");
                    sbQuery.AppendLine("update #fw_des_br_details_dtl set executionflag = 2 from #fw_des_br_details_dtl a (nolock) join #fw_des_service_segment b (nolock) on (b.servicename = a.servicename and b.instanceflag = 1) join #fw_des_di_parameter c(nolock) ");
                    sbQuery.AppendLine("on (c.servicename = b.servicename and c.segmentname = b.segmentname and c.methodid = a.methodid) join #fw_des_br_logical_parameter d(nolock) on (d.methodid = c.methodid and d.logicalparametername = c.parametername and d.btname <> 'rowno' and d.flowdirection = 1 and isnull(d.recordsetname, '') <> '')");
                    sbQuery.AppendLine("update #fw_des_br_details_dtl set executionflag = 3 from #fw_des_br_details_dtl a (nolock) join #fw_des_service_segment b (nolock) on (b.servicename = a.servicename) join #fw_des_di_parameter c (nolock) on (c.servicename = b.servicename and c.segmentname = b.segmentname and c.methodid = a.methodid) ");
                    sbQuery.AppendLine("join #fw_des_br_logical_parameter d (nolock) on (d.methodid = c.methodid and d.logicalparametername = c.parametername and d.btname <> 'rowno' and d.flowdirection = 1 and isnull(d.recordsetname, '') <> '') join #fw_des_service_dataitem e (nolock) on (e.servicename = c.servicename and e.segmentname = c.segmentname and e.dataitemname = c.dataitemname and e.ispartofkey = 1)");
                    ExecuteNonQueryResult(sbQuery.ToString());

                    sbQuery = new StringBuilder();
                    sbQuery.AppendLine("select distinct a.servicename as 'servicename', a.sectionname as 'sectionname', a.sequenceno as 'seqNo', a.isbr as 'isMethod', 0 as 'ceInstDepFlag', '' as 'progID', '' as 'methodName', lower(a.integservicename) as 'integServiceName', a.controlExpression as 'controlExpression',");
                    sbQuery.AppendLine("'' as 'broName', '' as 'spName', '' as 'operationType' into #fw_des_integ_service_dtl from #fw_des_processsection_br_is a(nolock) join #fw_des_integ_serv_map b (nolock) on (a.isbr = 0 and b.callingservicename = a.servicename and b.integservicename = a.integservicename and b.callingsegment <> 'fw_context') ");
                    sbQuery.AppendLine("join #fw_des_service_segment c (nolock) on (c.servicename = b.callingservicename and c.segmentname = b.callingsegment) order by servicename, sectionname");
                    sbQuery.AppendLine("update #fw_des_integ_service_dtl set ceInstDepFlag = 0 from #fw_des_processsection_dtl a(nolock) join #fw_des_integ_service_dtl b(nolock) on(b.servicename = a.servicename and b.sectionname = a.sectionname and a.loopingstyle = 1)");
                    sbQuery.AppendLine("update #fw_des_integ_service_dtl set ceInstDepFlag = 1 from #fw_des_processsection_dtl a(nolock) join #fw_des_integ_service_dtl b(nolock) on(b.servicename = a.servicename and b.sectionname = a.sectionname and a.loopingstyle = 0)");
                    sbQuery.AppendLine("set concat_null_yields_null off");
                    sbQuery.AppendLine("declare @callerDI varchar(8000), @CompName varchar(60), @seg varchar(8000), @callerOuSeg varchar(60), @servicename varchar(60), @segmentname varchar(60), @inseg varchar(8000), @callingservicename varchar(60), @integservice varchar(60), @segseqno int, @dataseqno int");
                    sbQuery.AppendLine("select @segseqno = 0, @dataseqno = 0");
                    sbQuery.AppendLine("insert #fw_des_integ_segmap_dtl(callerservice, integservice, sectionname, sequenceno, isCompName) select distinct a.callingservicename, lower(a.integservicename), c.sectionname, c.sequenceno, lower(b.componentname)from #fw_des_integ_serv_map a(nolock) join #fw_des_service b(nolock) on (b.servicename = a.integservicename) join #fw_des_processsection_br_is c(nolock) on (c.integservicename = a.integservicename and isbr = 0)");
                    sbQuery.AppendLine("select distinct callingservicename, integservicename, callersegname as 'callingsegment', issegname as 'integsegment' into #fw_des_integ_seglist from #fw_des_integ_serv_map_dtl where issegname <> 'fw_context' and CallDIFlow in (0, 2) order by callingservicename, integservicename, callersegname, issegname");
                    sbQuery.AppendLine("declare cur_CompName cursor for ");
                    sbQuery.AppendLine("select isCompName, callerservice, integservice from #fw_des_integ_segmap_dtl b(nolock)");
                    sbQuery.AppendLine("open cur_CompName");
                    sbQuery.AppendLine("fetch next from cur_CompName into @CompName, @callingservicename, @integservice");
                    sbQuery.AppendLine("while @@fetch_status = 0");
                    sbQuery.AppendLine("begin");
                    sbQuery.AppendLine("select @seg = '', @inseg = '', @callerDI = '', @callerOuSeg = ''");
                    sbQuery.AppendLine("select @seg = @seg + isnull(cast(callingsegment as varchar(8000)) + ';' , cast(callingsegment as varchar(8000))) from #fw_des_integ_seglist a(nolock) where a.callingservicename = @callingservicename and a.integservicename = @integservice group by a.callingsegment");
                    sbQuery.AppendLine("select @inseg = @inseg + isnull( cast(integsegment as varchar(8000)) + ';' , cast(integsegment as varchar(8000))) from #fw_des_integ_seglist a(nolock) where a.callingservicename = @callingservicename and a.integservicename = @integservice group by a.integsegment");
                    sbQuery.AppendLine("select @callerOuSeg = callingsegment, @callerDI = callingdataitem from #fw_des_integ_serv_map a(nolock) where a.integsegment = 'fw_context' and a.integdataitem = 'OUInstance' and a.callingservicename = @callingservicename and a.integservicename = @integservice");
                    sbQuery.AppendLine("update #fw_des_integ_segmap_dtl set callerSegList = lower(@seg), isInSegList = lower(@inseg), callerOuSeg = lower(@callerOuSeg), callerOuDi = lower(@callerDI) where isCompName = @CompName and callerservice = @callingservicename and integservice = @integservice");
                    sbQuery.AppendLine("fetch next from cur_CompName into @CompName, @callingservicename, @integservice");
                    sbQuery.AppendLine("end");
                    sbQuery.AppendLine("close cur_CompName");
                    sbQuery.AppendLine("deallocate cur_CompName");
                    sbQuery.AppendLine("select callingservicename, sectionname, integservicename, sequenceno as 'seqNo', 0 as 'sequenceno', lower(CallerSegName) as 'callerSeg', count(CallerDIName) as 'diCount' into #fw_des_integ_isinseg from #fw_des_integ_serv_map_dtl (nolock) where ISSegName <> 'fw_context' and CallerSegName = 'fw_context' and CallDIFlow in (0, 2) group by callingservicename, sectionname, integservicename, CallerSegName, sequenceno union");
                    sbQuery.AppendLine("select callingservicename, sectionname, integservicename, sequenceno as 'seqNo', 0 as 'sequenceno', lower(CallerSegName) as 'callerSeg', count(CallerDIName) as 'diCount' from #fw_des_integ_serv_map_dtl (nolock) where ISSegName <> 'fw_context' and CallDIFlow in (0, 2) group by callingservicename, sectionname, integservicename, CallerSegName, sequenceno");
                    sbQuery.AppendLine("update #fw_des_integ_isinseg set sequenceno =(select count(*) from #fw_des_integ_isinseg b (nolock) where b.callingservicename = a.callingservicename and b.SectionName = a.SectionName and b.integservicename = a.integservicename and b.callerseg <= a.callerseg) from #fw_des_integ_isinseg a(nolock)");
                    sbQuery.AppendLine("select callingservicename as 'servicename', sectionname, CallerSegName as 'segmentname', callingservicename, Integservicename, 0 as 'seqNo', lower(ISSegName) as 'isSeg', ISSegmentInst as 'isSegInst', lower(CallerDIName) as 'callerDI',");
                    sbQuery.AppendLine("lower(ISDIName) as 'isDI' into #fw_des_integ_isindi from #fw_des_integ_serv_map_dtl (nolock) where ISSegName <> 'fw_context' and CallerSegName = 'fw_context' and CallDIFlow in (0, 2) union ");
                    sbQuery.AppendLine("select callingservicename as 'servicename', sectionname, CallerSegName as 'segmentname', callingservicename, Integservicename, 0 as 'seqNo', lower(ISSegName) as 'isSeg', ISSegmentInst as 'isSegInst', lower(CallerDIName) as 'callerDI', lower(ISDIName) as 'isDI' from #fw_des_integ_serv_map_dtl (nolock) where ISSegName <> 'fw_context' and CallDIFlow in (0, 2) order by callingservicename, sectionname, segmentname, seqNo");
                    sbQuery.AppendLine("update #fw_des_integ_isindi set seqNo =(select distinct count(*) from #fw_des_integ_isindi b (nolock) where b.callingservicename = a.callingservicename and b.SectionName = a.SectionName and b.segmentname = a.segmentname and b.integservicename = a.integservicename and b.isseg = a.isseg and b.callerdi <= a.callerdi) from #fw_des_integ_isindi a (nolock)");
                    sbQuery.AppendLine("create index #fw_des_integ_isindi_idx on #fw_des_integ_isindi(servicename, seqNo)");
                    sbQuery.AppendLine("set concat_null_yields_null on");
                    ExecuteNonQueryResult(sbQuery.ToString());

                    sbQuery = new StringBuilder();
                    sbQuery.AppendLine("set concat_null_yields_null off");
                    sbQuery.AppendLine("declare @indi varchar(8000), @CompName varchar(60), @seg varchar(8000), @callerOuSeg varchar(60), @servicename varchar(60), @segmentname varchar(60), @inseg varchar(8000), @callerservice varchar(60), @seqno int");
                    sbQuery.AppendLine("select @seqno = 0");
                    sbQuery.AppendLine("insert #fw_des_integ_outsegmap_dtl(servicename, callerservice, sectionname, sequenceno, isCompName)select distinct callingservicename, lower(a.integservicename), c.sectionname, c.sequenceno, lower(b.componentname) from #fw_des_integ_serv_map a(nolock) join #fw_des_service b(nolock) on (b.servicename = a.callingservicename) join #fw_des_processsection_br_is c(nolock) on (c.servicename = b.servicename and c.integservicename = a.integservicename and c.isbr = 0) order by callingservicename, c.sectionname, c.sequenceno");
                    sbQuery.AppendLine("select distinct callingservicename, integservicename, callersegname as 'callingsegment', issegname as 'integsegment' into #fw_des_integ_outseglist from #fw_des_integ_serv_map_dtl (nolock) where callersegname <> 'fw_context' and issegname <> 'fw_context' and flowattribute not in (0, 3) order by callingservicename, integservicename, callersegname, issegname");
                    sbQuery.AppendLine("declare cur_CompName cursor for");
                    sbQuery.AppendLine("select isCompName, callerservice from #fw_des_integ_outsegmap_dtl b(nolock)");
                    sbQuery.AppendLine("open cur_CompName");
                    sbQuery.AppendLine("fetch next from cur_CompName into @CompName, @callerservice");
                    sbQuery.AppendLine("while @@fetch_status = 0");
                    sbQuery.AppendLine("begin");
                    sbQuery.AppendLine("select @seg = ''");
                    sbQuery.AppendLine("select @seg = @seg + isnull(cast(integsegment as varchar(8000)) + ';', cast(integsegment as varchar(8000))) from #fw_des_integ_outseglist a(nolock) where a.integservicename = @callerservice group by a.integsegment");
                    sbQuery.AppendLine("update #fw_des_integ_outsegmap_dtl set isSegList = @seg where callerservice = @callerservice");
                    sbQuery.AppendLine("fetch next from cur_CompName into @CompName, @callerservice");
                    sbQuery.AppendLine("end");
                    sbQuery.AppendLine("close cur_CompName");
                    sbQuery.AppendLine("deallocate cur_CompName");
                    sbQuery.AppendLine("select distinct callingservicename, sectionname, lower(integservicename) as 'integservicename', sequenceno as 'seqNo', 0 as 'sequenceno', lower(issegname) as 'isSeg', count(CallerDIName) as 'diCount', ISSegmentInst as 'instanceflag' into #fw_des_integ_isoutseg from #fw_des_integ_serv_map_dtl (nolock) where ISSegName <> 'fw_context' and flowattribute not in (0, 3) group by callingservicename, sectionname, integservicename, sequenceno, issegname, ISSegmentInst");
                    sbQuery.AppendLine("update #fw_des_integ_isoutseg set sequenceno =(select count(*) from #fw_des_integ_isoutseg b(nolock) where b.callingservicename = a.callingservicename and b.SectionName = a.SectionName and b.integservicename = a.integservicename) from #fw_des_integ_isoutseg a (nolock)");
                    sbQuery.AppendLine("select distinct callingservicename as 'servicename', sectionname, issegname as 'segmentname', callingservicename, integservicename, 0 as 'seqNo', lower(CallerSegName) as 'callerSeg', case when ISSegmentInst = 1 then 2 else 1 end as 'execFlag', lower(CallerDIName) as 'callerDI', lower(ISDIName) as 'isDI' into #fw_des_integ_isoutdi from #fw_des_integ_serv_map_dtl (nolock) where ISSegName <> 'fw_context' and CallerSegName <> 'fw_context' and flowattribute not in (0, 3) order by callingservicename, sectionname, callerSeg, seqNo");
                    sbQuery.AppendLine("update #fw_des_integ_isoutdi set seqNo = (select distinct count(*) from #fw_des_integ_isoutdi b(nolock) where b.callingservicename = a.callingservicename and b.SectionName = a.SectionName and b.segmentname = a.segmentname and b.integservicename = a.integservicename and b.callerdi <= a.callerdi) from #fw_des_integ_isoutdi a(nolock)");
                    sbQuery.AppendLine("set concat_null_yields_null on");
                    sbQuery.AppendLine("create index #fw_des_integ_isoutseg_idx on #fw_des_integ_isoutseg(integservicename, sequenceno)");
                    sbQuery.AppendLine("create index #fw_des_integ_isoutdi_idx on #fw_des_integ_isoutdi(integservicename, seqNo)");
                    ExecuteNonQueryResult(sbQuery.ToString());

                    sbQuery = new StringBuilder();
                    //PLF2.0_15017 **starts**                    
                    //sbQuery.AppendLine("select distinct diparam.servicename, physicalparametername = lower(lparam1.logicalparametername), lparam1.recordsetname, seqno = lparam1.logicalparamseqno, iscompoundparam = 1, lparam1.flowdirection, datasegmentname = lower(diparam.segmentname), dataitemname = diparam.dataitemname,");
                    sbQuery.AppendLine("select distinct diparam.servicename, physicalparametername = lparam1.logicalparametername, lparam1.recordsetname, seqno = lparam1.logicalparamseqno, iscompoundparam = 1, lparam1.flowdirection, datasegmentname = lower(diparam.segmentname), dataitemname = diparam.dataitemname,");
                    //PLF2.0_15017 **ends**
                    sbQuery.AppendLine("lparam1.methodid, diparam.sequenceno, diparam.sectionname, diflow = lparam1.methodid, ditype = lparam1.methodid, seginst = lparam1.methodid, brparamtype = bterm.datatype, bterm.length, ispartofkey = lparam1.methodid, actualditype = lparam1.logicalparametername,");
                    sbQuery.AppendLine("case replace(replace(replace(lparam1.spparametertype, CHAR(10), ''), CHAR(13), ''), CHAR(9), '') when 'DATE-TIME' then 'DATE' when 'DATETIME' then 'DATE' when 'TIME' then 'DATE' when 'REAL' then 'FLOAT' else replace(replace(replace(lparam1.spparametertype, CHAR(10), ''), CHAR(13), ''), CHAR(9), '') end spParameterType into #fw_des_physical_parameter_dtl from #fw_des_di_parameter diparam (nolock)");
                    sbQuery.AppendLine("join #fw_des_br_logical_parameter lparam1(nolock) on (lparam1.methodid = diparam.methodid and lparam1.logicalparametername = diparam.parametername) join #fw_req_bterm bterm(nolock) on (bterm.btname = lparam1.btname) order by lparam1.logicalparamseqno");
                    sbQuery.AppendLine("insert #fw_des_parameter_bterm_dtls (servicename,segmentname,dataitemname,flowattribute,datatype,instanceflag,ispartofkey)");
                    sbQuery.AppendLine("select distinct sdi.servicename, serseg.segmentname, ltrim(rtrim(sdi.dataitemname)) as dataitemname, sdi.flowattribute, datatype = bt.datatype, serseg.instanceflag, sdi.ispartofkey from #fw_des_service_dataitem sdi (nolock) join #fw_des_service_segment serseg(nolock) on (serseg.servicename = sdi.servicename and serseg.segmentname = sdi.segmentname), #fw_req_bterm bt(nolock) where exists (select 'x' from #fw_req_bterm_synonym s (nolock) where s.btsynonym = sdi.dataitemname and s.btname = bt.btname)");
                    ExecuteNonQueryResult(sbQuery.ToString());

                    sbQuery = new StringBuilder();
                    sbQuery.AppendLine("update #fw_des_physical_parameter_dtl set ispartofkey = b.ispartofkey, diflow = b.flowattribute, seginst = b.instanceflag, ditype = case when datatype = 'LONG' or datatype = 'INT' or datatype = 'INTEGER' then 3 ");
                    sbQuery.AppendLine("when datatype = 'STRING' or datatype = 'DATETIME' or datatype = 'CHAR' or datatype = 'NVARCHAR' or datatype = 'DATE' or datatype = 'TIME' or datatype = 'ENUMERATED' or datatype = 'DATE-TIME' then 8 ");
                    sbQuery.AppendLine("when datatype = 'DOUBLE' or datatype = 'NUMERIC' then 5 end from #fw_des_physical_parameter_dtl a(nolock) join #fw_des_parameter_bterm_dtls b(nolock) on (b.servicename = a.servicename and b.segmentname = a.datasegmentname)");
                    ExecuteNonQueryResult(sbQuery.ToString());

                    sbQuery = new StringBuilder();
                    sbQuery.AppendLine("update #fw_des_physical_parameter_dtl set ispartofkey = 0, diflow = 2, seginst = 1 where datasegmentname = 'errordetails'");
                    sbQuery.AppendLine("update #fw_des_physical_parameter_dtl set ispartofkey = 0, diflow = 2, seginst = case when datasegmentname = 'fw_context' then 0 end, DIType = case when DataItemName = 'Language' or DataItemName = 'ErrorNo' then 3 else 8 end where datasegmentname in ('fw_context','errordetails')");

                    //PLF2.0_15017 **starts**
                    //sbQuery.AppendLine("select distinct diparam.servicename, physicalparametername = lower(lparam1.logicalparametername), lparam1.recordsetname, seqno = lparam1.logicalparamseqno, iscompoundparam = 0, lparam1.flowdirection, datasegmentname = lower(diparam.segmentname), dataitemname = diparam.dataitemname,");
                    sbQuery.AppendLine("select distinct diparam.servicename, physicalparametername = lparam1.logicalparametername, lparam1.recordsetname, seqno = lparam1.logicalparamseqno, iscompoundparam = 0, lparam1.flowdirection, datasegmentname = lower(diparam.segmentname), dataitemname = diparam.dataitemname,");
                    //PLF2.0_15017 **ends**
                    sbQuery.AppendLine("lparam1.methodid, diparam.sequenceno, diparam.sectionname, diflow = lparam1.methodid, ditype = lparam1.methodid, seginst = lparam1.methodid, brparamtype = bterm.datatype, bterm.length, ispartofkey = lparam1.methodid, actualditype = lparam1.logicalparametername,");
                    sbQuery.AppendLine("upper(lparam1.spparametertype) as spparametertype into #fw_des_logical_parameter_dtl from #fw_des_di_parameter diparam (nolock) join #fw_des_br_logical_parameter lparam1(nolock) on (lparam1.methodid = diparam.methodid and diparam.parametername = lparam1.logicalparametername)");
                    sbQuery.AppendLine("join #fw_req_bterm bterm(nolock) on (bterm.btname = lparam1.btname) order by lparam1.logicalparamseqno");
                    ExecuteNonQueryResult(sbQuery.ToString());

                    sbQuery = new StringBuilder();
                    sbQuery.AppendLine("update #fw_des_logical_parameter_dtl set ispartofkey = b.ispartofkey, diflow = b.flowattribute, seginst = b.instanceflag, ditype = case when datatype = 'LONG' or datatype = 'INT' or datatype = 'INTEGER' then 3 when datatype = 'STRING' or datatype = 'DATETIME' or datatype = 'CHAR' ");
                    sbQuery.AppendLine("or datatype = 'NVARCHAR' or datatype = 'DATE' or datatype = 'TIME' or datatype = 'ENUMERATED' or datatype = 'DATE-TIME' then 8 when datatype = 'DOUBLE' or datatype = 'NUMERIC' then 5 end");
                    sbQuery.AppendLine("from #fw_des_logical_parameter_dtl a(nolock) join #fw_des_parameter_bterm_dtls b(nolock) on (b.servicename = a.servicename and b.segmentname = a.datasegmentname)");
                    ExecuteNonQueryResult(sbQuery.ToString());

                    sbQuery = new StringBuilder();
                    sbQuery.AppendLine("update #fw_des_logical_parameter_dtl set ispartofkey = 0, diflow = 2, seginst = 1 where datasegmentname = 'errordetails'");
                    sbQuery.AppendLine("update #fw_des_logical_parameter_dtl set ispartofkey = 0, diflow = 2, seginst = case when datasegmentname = 'fw_context' then 0 end, DIType = case when DataItemName = 'Language' or DataItemName = 'ErrorNo' then 3 else 8 end where datasegmentname in('fw_context','errordetails')");
                    sbQuery.AppendLine("update #fw_des_logical_parameter_dtl set seqno = (select count(*) from #fw_des_logical_parameter_dtl b (nolock) where b.servicename = a.servicename and b.SectionName = a.SectionName and b.methodid = a.methodid and b.physicalparametername <= a.physicalparametername and b.flowdirection in (1, 2)) from #fw_des_logical_parameter_dtl a (nolock)");
                    sbQuery.AppendLine("update #fw_des_service_dataitem_dtl set BT_Type = 'DATETIME1' where BT_Type = 'DATE-TIME'");
                    ExecuteNonQueryResult(sbQuery.ToString());

                    GlobalVar.Query = "select lower(servicename) as 'servicename', serviceType, lower(inSegs) as inSegs, lower(outSegs) as outSegs, lower(ioSegs) as ioSegs, lower(scSegs) as scSegs, inSegCnt, outSegCnt, scSegCnt, psCnt, ioSegCnt, lower(dataSegs) as dataSegs, segCnt from #fw_des_service_dtl (nolock) order by serviceName for xml raw, root('root')";
                    BindToLinq("service");
                    GlobalVar.Query = "select lower(servicename) as 'servicename', sectionType, seqNo, lower(controlExpression) as controlExpression, loopingStyle, loopCausingSegment, ceInstDepFlag, brsCnt, sectionName from #fw_des_processsection_dtl (nolock) order by servicename, seqNo for xml raw, root('root')";
                    BindToLinq("processsection");
                    GlobalVar.Query = "select distinct lower(servicename) as 'servicename', lower(sectionname) as'sectionname', seqno, ismethod, isnull(progid, '') 'progid', isnull(methodname, '') 'methodname', lower(integservicename) as 'integservicename', ceinstdepflag, lower(controlexpression) as controlexpression, executionflag, loopsegment, isnull(methodid, 0) 'methodid', isnull(accessesdatabase, '1') 'accessesdatabase', isnull(systemgenerated, '1') 'systemgenerated', sperrorprotocol, isnull(broname, '') 'broname', isnull(spname, '') 'spname', isnull(operationtype, '') 'operationtype', brppcnt, brlpcnt from #fw_des_br_details_dtl (nolock) order by servicename, sectionname, seqno for xml raw, root('root')";
                    BindToLinq("brdetails");
                    GlobalVar.Query = "select lower(callerservice) as 'callerservice', integservice, lower(sectionname) as 'sectionname', sequenceno, callerSegList, isCompName, callerOuSeg, callerOuDi, isInSegList from #fw_des_integ_segmap_dtl (nolock) order by callerservice, sectionname, sequenceno for xml raw, root('root')";
                    BindToLinq("integsegmap");
                    GlobalVar.Query = "select lower(servicename) as 'servicename', lower(callerservice) as 'callerservice', lower(sectionname) as 'sectionname', sequenceno, lower(isSegList) as 'isSegList' from #fw_des_integ_outsegmap_dtl (nolock) order by servicename, callerservice, sectionname, sequenceno for xml raw, root('root')";
                    BindToLinq("brisoutsegmap");
                    GlobalVar.Query = "select lower(callingservicename) as 'callingservicename', lower(sectionname) as 'sectionname', lower(integservicename) as 'integservicename', seqNo, sequenceno, isSeg, diCount, instanceflag from #fw_des_integ_isoutseg (nolock) for xml raw, root('root')";
                    BindToLinq("brisoutsegmapisoutseg");
                    GlobalVar.Query = "select lower(servicename) as 'servicename', sectionname, segmentname, callingservicename, integservicename, seqNo, callerSeg, execFlag, callerDI, isDI from #fw_des_integ_isoutdi (nolock) for xml raw, root('root')";
                    BindToLinq("brisoutsegmapisoutsegisoutdi");
                    GlobalVar.Query = "select lower(callingservicename) as 'callingservicename', lower(sectionname) as 'sectionname', lower(integservicename) as 'integservicename', seqNo, sequenceno, callerSeg, diCount from #fw_des_integ_isinseg (nolock) for xml raw, root('root')";
                    BindToLinq("brisinsegmapisinseg");
                    GlobalVar.Query = "select lower(servicename) as 'servicename', lower(sectionname) as 'sectionname', lower(segmentname) as 'segmentname', callingservicename, lower(Integservicename) as 'Integservicename', seqNo, isSeg, isSegInst, callerDI, isDI from #fw_des_integ_isindi(nolock) for xml raw, root('root')";
                    BindToLinq("brisinsegmapisinsegisindi");
                    GlobalVar.Query = "select lower(servicename) as 'servicename', lower(SectionName) as 'SectionName', sequenceno, physicalParameterName, seqNo, isCompoundParam, flowDirection, dataSegmentName, lower(dataItemName) as dataItemName, spParameterType from #fw_des_physical_parameter_dtl (nolock) where (flowdirection in (0,1,2,3) or seginst <> 1) union select distinct lower(servicename) as 'servicename', lower(SectionName) as 'SectionName', sequenceno, 'RSET101' as physicalParameterName, 0 as seqNo, 0 as isCompoundParam, flowDirection, dataSegmentName, '' as dataItemName, 'CHAR' from #fw_des_physical_parameter_dtl (nolock) where flowdirection = 1 and seginst = 1 order by servicename, sectionname, sequenceno, seqNo for xml raw, root('root')";
                    BindToLinq("processsectionbrpp");
                    GlobalVar.Query = "select lower(servicename) as 'servicename', lower(SectionName) as 'SectionName', sequenceno, physicalParameterName as logicalParameterName, seqNo, lower(dataSegmentName) as 'dataSegmentName', lower(dataItemName) as 'dataItemName' from #fw_des_logical_parameter_dtl (nolock) where flowdirection = 1 and seginst = 1 order by servicename, sectionname, sequenceno, seqNo for xml raw, root('root')";
                    BindToLinq("processsectionbrlp");
                    //GlobalVar.Query = "select lower(servicename) as 'servicename', lower(segmentName) as segmentName, segmentsequence as seqNo, instanceFlag, flowDirection, mandatoryflag as mandatory, isPartOfHierarchy, isnull(segKeys, '') segKeys, isnull(diCnt, 0) diCnt, lower(isnull(inDINames, '')) as inDINames, lower(isnull(ioDINames, '')) as ioDINames, lower(isnull(outDINames, '')) as outDINames, lower(isnull(scDINames, '')) as scDINames, lower(isnull(diNames, '')) as diNames, isnull(inDICnt, 0) as inDICnt, isnull(outDICnt, 0) as outDICnt, isnull(ioDICnt, 0) as ioDICnt, isnull(scDICnt, 0) as scDICnt from #fw_des_service_segment_dtl (nolock) order by servicename, SegmentSequence for xml raw, root('root')";
                    GlobalVar.Query = "select lower(servicename) as 'servicename', lower(segmentName) as segmentName, segmentsequence as seqNo, instanceFlag, flowDirection, mandatoryflag as mandatory, isPartOfHierarchy, segKeys, diCnt, lower(inDINames) as inDINames, lower(ioDINames) as ioDINames, lower(outDINames) as outDINames, lower(scDINames) as scDINames, lower(diNames) as diNames, inDICnt, outDICnt, ioDICnt, scDICnt from #fw_des_service_segment_dtl (nolock) order by servicename, SegmentSequence for xml raw, root('root')";
                    BindToLinq("segment");
                    GlobalVar.Query = "select lower(servicename) as 'servicename', lower(segmentName) as segmentName, lower(dataItemName) as dataItemName, dataType, dataLength, isPartOfKey, flowAttribute, mandatoryFlag, isnull(defaultValue, '') as defaultValue, upper(LTRIM(RTRIM(BT_Type))) as bt_Type, isModel from #fw_des_service_dataitem_dtl (nolock) order by servicename, segmentname, dataitemname for xml raw, root('root')";
                    BindToLinq("segmentdataItem");
                }

                if (GlobalVar.ActivityServiceSchema)
                {
                    sbQuery = new StringBuilder();
                    sbQuery.AppendLine("insert into #fw_des_webservice_dtl (activityid, activityname, ilbocode, servicename) select distinct a.activityid as 'activityid', lower(activityname) as 'activityname', lower(ilbocode) as 'ilbocode', lower(b.servicename) as 'servicename' from #fw_req_activity a(nolock) join #fw_des_ilbo_service_view_datamap b(nolock) on (b.activityid = a.activityid) join #fw_des_service c (nolock) on (c.servicename = b.servicename) order by activityid, activityname, ilbocode, servicename"); ////where c.isintegser = 0 
                    sbQuery.AppendLine("insert into #fw_des_webservice_dataitem_dtl(ilbocode, servicename, segmentname, dataitemname, control, viewname,btsynonym, flowattribute, activityid ) select a.ilbocode, b.servicename, b.segmentname, b.dataitemname, b.controlid, viewname,a.btsynonym, flowattribute, activityid from #fw_req_ilbo_control a (nolock) join #fw_des_ilbo_service_view_datamap b (nolock) on (b.ilbocode = a.ilbocode and b.controlid = a.controlid) join #fw_des_service_dataitem c(nolock) on (c.servicename = b.servicename and c.segmentname = b.segmentname and c.dataitemname = b.dataitemname and b.dataitemname <> 'modeflag') union"); //code modified for bug id: PLF2.0_04462,TECH-33808
                    sbQuery.AppendLine("select distinct b.ilbocode, a.servicename, a.segmentname, a.dataitemname, controlid, 'modeflag', b.btsynonym, 3 ,activityid from #fw_des_di_parameter a(nolock) join #fw_des_ilbo_service_view_datamap b(nolock) on (a.dataitemname = 'modeflag' and b.servicename = a.servicename and b.segmentname = a.segmentname and b.dataitemname = a.dataitemname) order by a.ilbocode, b.servicename, b.segmentname, b.dataitemname, b.controlid");//TECH-33808 //TECH-36179 - modeflag issue
                    sbQuery.AppendLine("insert into #fw_des_webservice_dataitem_dtl(ilbocode, servicename, segmentname, dataitemname, control, viewname, flowattribute, activityid )");
                    sbQuery.AppendLine("select c.ilbocode as 'ilbocode', lower(a.servicename) as 'servicename', lower(a.segmentname) as 'segmentname', lower(d.dataitemname) as 'dataitemname', case a.chart_section when 'chart_config_segment' then a.chart_id + '_config_grd' when 'chart_data_segment' then a.chart_id + '_data_grd' when 'chart_series_segment' then a.chart_id + '_series_grd' end as 'controlid', '$$#viewname#$$', 1, c.activityid ");
                    sbQuery.AppendLine("from #fw_des_chart_service_segment a(nolock) join #fw_chart_task_map b(nolock) on (b.section_name = a.chart_id and b.servicename = a.servicename) join #fw_req_activity_ilbo c(nolock) on ( c.ilbocode = b.ui_name ) join #fw_des_chart_service_dataitem d (nolock) on ( d.chart_id = a.chart_id and d.chart_section = a.chart_section and d.servicename = a.servicename and d.segmentname = a.segmentname ) order by ilbocode, servicename, segmentname, dataitemname, controlid");
                    sbQuery.AppendLine("insert into #fw_des_webservice_dataitem_dtl(ilbocode, servicename, segmentname, dataitemname, control, viewname, flowattribute, activityid )");
                    sbQuery.AppendLine("select c.ilbocode as 'ilbocode', lower(a.servicename) as 'servicename', lower(b.segmentname) as 'segmentname', lower(d.dataitemname) as 'dataitemname', lower(a.section_name) + '_data_grd' as 'controlid', '$$#viewname#$$', 1, c.activityid ");
                    sbQuery.AppendLine("from #fw_des_service_tree_map a(nolock) join #fw_des_service_segment b(nolock) on (b.servicename = a.servicename and b.segmentname = 'tree_data_segment') join #fw_des_service_dataitem d (nolock) on (d.servicename = b.servicename and d.segmentname = 'tree_data_segment') join #fw_req_activity_ilbo c(nolock) on ( c.ilbocode = a.ui_name ) order by servicename, segmentname");
                    sbQuery.AppendLine("update #fw_des_webservice_dataitem_dtl set viewname = (select count(*) from #fw_des_webservice_dataitem_dtl b(nolock) where b.activityid = a.activityid and b.ilbocode = a.ilbocode and b.control = a.control and b.servicename = a.servicename and b.segmentname = a.segmentname and b.dataitemname <= a.dataitemname )from #fw_des_webservice_dataitem_dtl a (nolock) where viewname = '$$#viewname#$$'");
                    sbQuery.AppendLine("insert into #fw_des_webservice_segment_dtl(servicename, segmentname, seq, flowdirection, multiline, filling, indicnt, outdicnt, iodicnt, scdicnt, dicnt, instanceflag, activityid, ilbocode, control)");
                    sbQuery.AppendLine("select distinct b.servicename as 'servicename', lower(b.segmentname) as 'segmentname', segmentsequence, 0, 'false', 'false', 0, 0, 0, 0, 0, instanceflag, b.activityid, b.ilbocode, a.controlid from #fw_req_ilbo_control a (nolock) join #fw_des_ilbo_service_view_datamap b(nolock) on (b.ilbocode = a.ilbocode and b.controlid = a.controlid) join #fw_des_service_segment c (nolock) on (c.servicename = b.servicename and c.segmentname = b.segmentname) order by servicename, segmentname");
                    ExecuteNonQueryResult(sbQuery.ToString());

                    sbQuery = new StringBuilder();
                    sbQuery.AppendLine("insert into #fw_des_webservice_segment_dtl(servicename, segmentname, seq, flowdirection, multiline, filling, indicnt, outdicnt, iodicnt, scdicnt, dicnt, instanceflag, activityid, ilbocode, control)");
                    sbQuery.AppendLine("select lower(a.servicename) as 'servicename', lower(a.segmentname) as 'segmentname', 2, 1, 'true', 'false', 0, 0, 0, 0, 0, instanceflag, c.activityid, c.ilbocode, case a.chart_section when 'chart_config_segment' then a.chart_id + '_config_grd' when 'chart_data_segment' then a.chart_id + '_data_grd' when 'chart_series_segment' then a.chart_id + '_series_grd' end as 'controlid' ");
                    sbQuery.AppendLine("from #fw_des_chart_service_segment a(nolock) join #fw_chart_task_map b(nolock) on (b.section_name = a.chart_id and b.servicename = a.servicename) join #fw_req_activity_ilbo c(nolock) on ( c.ilbocode = b.ui_name ) order by servicename, segmentname");
                    sbQuery.AppendLine("insert into #fw_des_webservice_segment_dtl(servicename, segmentname, seq, flowdirection, multiline, filling, indicnt, outdicnt, iodicnt, scdicnt, dicnt, instanceflag, activityid, ilbocode, control)");
                    sbQuery.AppendLine("select lower(a.servicename) as 'servicename', lower(b.segmentname) as 'segmentname', 2, 1, 'true', 'false', 0, 0, 0, 0, 0, instanceflag, c.activityid, c.ilbocode, lower(a.section_name) + '_data_grd' as 'controlid' ");
                    sbQuery.AppendLine("from #fw_des_service_tree_map a(nolock) join #fw_des_service_segment b(nolock) on (b.servicename = a.servicename and b.segmentname = 'tree_data_segment') join #fw_req_activity_ilbo c(nolock) on ( c.ilbocode = a.ui_name ) order by servicename, segmentname");
                    sbQuery.AppendLine("update #fw_des_webservice_segment_dtl set multiline = 'true' where instanceflag = 1");
                    sbQuery.AppendLine("update #fw_des_webservice_segment_dtl set indicnt = isnull(incnt,0), outdicnt = isnull(outcnt,0), iodicnt = isnull(iocnt,0), scdicnt = isnull(sccnt,0), dicnt = isnull(incnt,0) + isnull(outcnt, 0) + isnull(iocnt, 0) + isnull(sccnt, 0), flowdirection = case  when  isnull(iocnt, 0) > 0 or (isnull(incnt, 0) > 0 and isnull(outcnt, 0) > 0) then 2 when isnull(incnt, 0) > 0 then 0 when isnull(outcnt, 0) > 0 then 1 else 3 end");
                    sbQuery.AppendLine("from #fw_des_webservice_segment_dtl a(nolock) join (select servicename, segmentname, isnull(count(case when flowattribute = 0 then dataitemname end), 0) as incnt, isnull(count(case when flowattribute = 1 then dataitemname end), 0) as outcnt, isnull(count(case when flowattribute = 2 then dataitemname end), 0) as iocnt, isnull(count(case when flowattribute = 3 then dataitemname end), 0) as sccnt from #fw_des_webservice_dataitem_dtl (nolock) group by servicename, segmentname) cnt on (cnt.servicename = a.servicename and cnt.segmentname = a.segmentname)");
                    ExecuteNonQueryResult(sbQuery.ToString());

                    sbQuery = new StringBuilder();
                    sbQuery.AppendLine("update #fw_des_webservice_segment_dtl set flowdirection = 3 from #fw_des_webservice_segment_dtl where scDICnt = diCnt");
                    sbQuery.AppendLine("update #fw_des_webservice_segment_dtl set filling = 'true' from #fw_des_webservice_segment_dtl a (nolock) join #fw_des_task_segment_attribs b (nolock) on (b.servicename = a.servicename and b.segmentname = a.segmentname) join #fw_req_task c (nolock) on (c.taskname = b.taskname) where c.tasktype = 'initialize'");
                    sbQuery.AppendLine("update #fw_des_webservice_segment_dtl set filling = 'true' from #fw_des_webservice_segment_dtl a (nolock) join #fw_des_ilbo_service_view_datamap b (nolock) on (b.activityid = a.activityid and b.ilbocode = a.ilbocode and b.servicename = a.servicename and b.segmentname = a.segmentname) join #fw_des_task_segment_attribs c (nolock) on (c.activityid = b.activityid and c.ilbocode = b.ilbocode and c.servicename = b.servicename and c.segmentname = b.segmentname and c.taskname = b.taskname) where c.combofill = 1");
                    sbQuery.AppendLine("update #fw_des_webservice_segment_dtl set control = '' from #fw_des_webservice_segment_dtl a (nolock) join #fw_des_ilbo_service_view_datamap b (nolock) on (b.servicename = a.servicename and b.segmentname = a.segmentname) where a.multiline = 'false'");
                    ExecuteNonQueryResult(sbQuery.ToString());

                    GlobalVar.Query = "select distinct activityid, activityname, ilbocode from #fw_des_webservice_dtl (nolock) order by activityid, activityname, ilbocode for xml raw, root('root')";
                    BindToLinq("activity");
                    GlobalVar.Query = "select activityid, activityname, Ltrim(Rtrim(ilbocode)) 'ilbocode', lower(LTrim(Rtrim(servicename))) 'servicename' from #fw_des_webservice_dtl (nolock) order by activityid, activityname, ilbocode, servicename for xml raw, root('root')";   //code modified for bug id : PLF2.0_03791
                    BindToLinq("actservices");
                    GlobalVar.Query = "select distinct activityid, lower(Ltrim(Rtrim(ilbocode))) ilbocode, lower(Ltrim(Rtrim(servicename))) 'servicename', lower(Ltrim(Rtrim(segmentname))) 'segmentname', seq, flowdirection 'flowdirection', multiline, filling, isnull(lower(control), '') 'control' from #fw_des_webservice_segment_dtl (nolock) where flowdirection <> 3 order by activityid, ilbocode, servicename, segmentname, control for xml raw, root('root')";    //code modified for bug id : PLF2.0_03791
                    BindToLinq("actsersegment");
                    GlobalVar.Query = "select distinct activityid, lower(Ltrim(Rtrim(ilbocode))) ilbocode, lower(Ltrim(Rtrim(servicename))) 'servicename', lower(Ltrim(Rtrim(segmentname))) 'segmentname', lower(Ltrim(Rtrim(dataitemname))) 'dataitemname', lower(Ltrim(Rtrim(control))) 'control', lower(Ltrim(Rtrim(viewname))) as 'view', flowattribute 'flow', lower(btsynonym) as 'btsynonym' from #fw_des_webservice_dataitem_dtl (nolock) order by activityid, ilbocode, servicename, segmentname, dataitemname for xml raw, root('root')";    //code modified for bug id : PLF2.0_03791,TECH-33808
                    BindToLinq("actserdataitem");
                }

                if (GlobalVar.ErrorSchema)
                {
                    sbQuery = new StringBuilder();
                    sbQuery.AppendLine("select distinct ltrim(rtrim(ps.methodid)) methodid, ltrim(rtrim(ser.servicename)) ServiceName, ltrim(rtrim(bre.SPErrorCode)) errorID, ltrim(rtrim(bre.ErrorID)) brErrorID, ltrim(rtrim(isnull(isnull(eli.errormessage, err.errormessage), ''))) errorMsg, ltrim(rtrim(isnull(isnull(corr.correctiveaction, err.defaultcorrectiveaction),''))) correctiveMsg,");
                    sbQuery.AppendLine("ltrim(rtrim(isnull(ctxt.SeverityID ,err.defaultseverity))) severity, ltrim(rtrim(isnull(fctrl.SegmentName,''))) focusSegName, ltrim(rtrim(isnull(fctrl.FocusDataItem,''))) focusDIName, isnull(eli.langid, '1') langid into #fw_des_methodsp_dtls from #fw_des_service ser (nolock) join #fw_des_processsection_br_is ps (nolock) on (ser.servicename = ps.servicename and ps.isbr = 1 ) join #fw_des_brerror bre (nolock) on (ps.methodid = bre.methodid) ");
                    sbQuery.AppendLine("join #fw_des_error err(nolock) on (bre.errorid = err.errorid) left outer join #fw_des_context ctxt (nolock) on (err.errorid = ctxt.errorid and ser.servicename = ctxt.errorcontext ) left outer join #fw_des_corr_action_local_info corr (nolock) on (ser.servicename = corr.errorcontext and err.errorid = corr.errorid and corr.langid = 1) left outer join #fw_des_err_det_local_info eli (nolock)");
                    sbQuery.AppendLine("on (err.errorid = eli.errorid) left outer join #fw_des_focus_control fctrl (nolock) on (err.errorid = fctrl.errorid and ser.servicename = fctrl.errorContext) order by 'methodid','errorMsg','ServiceName'");//TECH-18686
                    sbQuery.AppendLine("select ser.servicename, bre.Methodid as 'methodid', bre.Sperrorcode as 'errorID', diph.Placeholdername as 'Placeholdername', diph.SequenceNo as 'seqNo', lower(diph.segmentname) as 'segName', diph.dataitemname as 'diName', isnull(DIPL.ShortPLText,'') as 'ShortPLText', dipl.langid into #fw_des_method_placeholder_dtls from #fw_des_error_placeholder ph(nolock) join #fw_des_di_placeholder diph (nolock) on (diph.Placeholdername = ph.Placeholdername) ");
                    sbQuery.AppendLine("join #fw_des_brerror bre (nolock) on (ph.Errorid = bre.Errorid and diph.Errorid = bre.Errorid and diph.Methodid = bre.Methodid) left outer join #fw_des_service_segment seg (nolock) on (seg.ServiceName = diph.servicename and seg.SegmentName = diph.segmentname) join #fw_des_processsection ps (nolock) on (diph.sectionname = ps.sectionname and diph.ServiceName = ps.ServiceName) ");
                    sbQuery.AppendLine("join #fw_des_service ser (nolock) on (diph.ServiceName = ser.ServiceName and ser.StatusFlag = 1) left outer join #fw_req_lang_bterm_synonym DIPL (nolock) on (diph.dataitemname = dipl.BTSynonym and dipl.langid = 1) union");
                    sbQuery.AppendLine("select ser.servicename as 'servicename', bre.Methodid as 'id', bre.Sperrorcode as 'errorID', ph.Placeholdername as 'Placeholdername', di.SequenceNo as 'seqNo', lower(di.segmentname) as 'segName', di.dataitemname as 'diName', isnull(DIPL.ShortPLText,'') as 'ShortPLText', dipl.langid from #fw_des_error_placeholder ph(nolock) join #fw_des_be_placeholder beph (nolock) on (beph.Placeholdername = ph.Placeholdername)"); //TECH-18686
                    sbQuery.AppendLine("join #fw_des_di_parameter di (nolock) on (beph.Methodid = di.methodid and beph.ParameterName = di.ParameterName) join #fw_des_brerror bre(nolock) on (ph.Errorid = bre.Errorid and beph.Methodid = bre.Methodid and beph.Errorid = bre.Errorid) left outer join #fw_des_service_segment seg(nolock) on (seg.ServiceName = di.servicename and seg.SegmentName = di.segmentname)");
                    sbQuery.AppendLine("join #fw_des_processsection ps(nolock) on (di.sectionname = ps.sectionname and di.ServiceName = ps.ServiceName) join #fw_des_service ser(nolock) on (ser.ServiceName = di.ServiceName and ser.StatusFlag = 1) left outer join #fw_req_lang_bterm_synonym dipl(nolock) on (DIPL.BTSynonym = di.dataitemname and dipl.langid = 1) order by bre.methodid, bre.sperrorcode, ser.servicename "); //TECH-18686
                    ExecuteNonQueryResult(sbQuery.ToString());

                    GlobalVar.Query = "select distinct lower(pb.servicename) as 'servicename', count(methodid) 'count' from #fw_des_processsection_br_is pb (nolock) join #fw_des_service ser (nolock) on (ser.servicename = pb.servicename and ser.statusflag = 1 and isbr = 1) group by pb.servicename for xml raw, root('root')";
                    BindToLinq("error");
                    GlobalVar.Query = string.Format("select distinct lower(pb.servicename) as 'servicename', pb.Methodid as 'methodid', lower(bus.methodname) as 'methodname' from #fw_des_processsection_br_is pb(nolock) join #fw_des_service ser (nolock) on (ser.servicename = pb.servicename and ser.statusflag = 1 and isbr = 1) join #fw_des_businessrule bus(nolock)on (pb.methodid = bus.methodid) order by 'servicename', 'methodid', 'methodname' for xml raw, root('root')"); //TECH-18686
                    BindToLinq("errormethod");
                    GlobalVar.Query = string.Format("select methodid, lower(servicename) as 'servicename', langid, errorid, brerrorid, errormsg, correctivemsg, severity, focussegname, focusdiname from #fw_des_methodsp_dtls (nolock) for xml raw, root('root')");
                    BindToLinq("errormsg");
                    GlobalVar.Query = string.Format("select lower(servicename) as 'servicename', methodid, errorid, placeholdername, seqno, segname, diname, shortpltext from #fw_des_method_placeholder_dtls(nolock) for xml raw, root('root')");
                    BindToLinq("errormsgph");
                }
                WriteProfiler("Inserting data in Hashtables end.");
            }
            catch (Exception ex)
            {
                WriteProfiler(string.Format("Exception raised in inserting data in Hashtables for EcrNo : {0} : Error : {1}", GlobalVar.Ecrno, ex.Message));
            }
        }

        private void CreateIndex()
        {
            try
            {
                WriteProfiler("Creating index for Hashtables start.");
                StringBuilder sbQuery = new StringBuilder();
                sbQuery.AppendLine("create clustered index fw_req_activity_unqidx on #fw_req_activity(componentname, activityid)");
                sbQuery.AppendLine("create clustered index fw_req_activity_ilbo_unqidx on #fw_req_activity_ilbo(activityid, ilbocode)");
                sbQuery.AppendLine("create clustered index fw_req_ilbo_unqidx on #fw_req_ilbo(ilbocode)");
                sbQuery.AppendLine("create clustered index fw_req_ilbo_control_property_unqidx on #fw_req_ilbo_control_property(controlid, ilbocode, propertyname, viewname)");
                sbQuery.AppendLine("create clustered index fw_req_ilbo_control_unqidx on #fw_req_ilbo_control(ilbocode, controlid)");
                sbQuery.AppendLine("create clustered index fw_req_ilbo_view_unqidx on #fw_req_ilbo_view(controlid, ilbocode, viewname)");
                sbQuery.AppendLine("create clustered index fw_req_bterm_unqidx on #fw_req_bterm(btname)");
                sbQuery.AppendLine("create clustered index fw_req_bterm_synonym_unqidx on #fw_req_bterm_synonym(btsynonym)");
                sbQuery.AppendLine("create clustered index fw_req_process_component_unqidx on #fw_req_process_component(parentprocess)");
                sbQuery.AppendLine("create clustered index fw_req_ilbo_tab_properties_unqidx on #fw_req_ilbo_tab_properties(ilbocode, propertyname, tabname)");
                sbQuery.AppendLine("create clustered index fw_req_activity_ilbo_task_unqidx on #fw_req_activity_ilbo_task(activityid, ilbocode, taskname)");
                sbQuery.AppendLine("create clustered index fw_req_task_unqidx on #fw_req_task(taskname)");
                sbQuery.AppendLine("create clustered index fw_req_bterm_enumerated_option_unqidx on #fw_req_bterm_enumerated_option(btname, langid, sequenceno)");
                sbQuery.AppendLine("create clustered index fw_req_ilbo_link_publish_unqidx on #fw_req_ilbo_link_publish(ilbocode, linkid)");
                sbQuery.AppendLine("create clustered index fw_req_ilbo_data_publish_unqidx on #fw_req_ilbo_data_publish(dataitemname, ilbocode, linkid)");
                sbQuery.AppendLine("create clustered index fw_req_ilbo_linkuse_unqidx on #fw_req_ilbo_linkuse(parentilbocode, taskname)");
                sbQuery.AppendLine("create clustered index fw_req_ilbo_data_use_unqidx on #fw_req_ilbo_data_use(controlid, dataitemname, linkid, parentilbocode, taskname, viewname)");
                sbQuery.AppendLine("create clustered index fw_des_ilbo_services_unqidx on #fw_des_ilbo_services(ilbocode, servicename)");
                sbQuery.AppendLine("create clustered index fw_des_service_dataitem_unqidx on #fw_des_service_dataitem(servicename, segmentname, dataitemname)");
                sbQuery.AppendLine("create clustered index fw_des_service_unqidx on #fw_des_service(componentname, servicename)");
                sbQuery.AppendLine("create clustered index fw_des_service_segment_unqidx on #fw_des_service_segment(servicename, segmentname)");
                sbQuery.AppendLine("create clustered index fw_des_ilbo_service_view_datamap_unqidx on #fw_des_ilbo_service_view_datamap(ilbocode, servicename, activityid, taskname, segmentname, dataitemname)");
                sbQuery.AppendLine("create clustered index fw_des_task_segment_attribs_unqidx on #fw_des_task_segment_attribs(activityid, ilbocode, taskname, servicename, segmentname)");
                sbQuery.AppendLine("create clustered index fw_req_task_local_info_unqidx on #fw_req_task_local_info(langid, taskname)");
                sbQuery.AppendLine("create unique clustered index fw_des_br_logical_parameter_indx on #fw_des_br_logical_parameter (methodid, logicalparametername)");
                sbQuery.AppendLine("create clustered index fw_des_brerror_Indx on #fw_des_brerror (errorid, methodid, sperrorcode)");
                sbQuery.AppendLine("create clustered index fw_des_bro_indx on #fw_des_bro (broname, componentname )");
                sbQuery.AppendLine("create clustered index fw_des_businessrule_indx on #fw_des_businessrule (methodid)");
                sbQuery.AppendLine("create clustered index fw_des_context_indx on #fw_des_context (errorid, errorcontext )");
                sbQuery.AppendLine("create clustered index fw_des_di_parameter_indx on #fw_des_di_parameter (servicename, sectionname, sequenceno, parametername )");
                sbQuery.AppendLine("create nonclustered index fw_des_di_parameter_indx1 on #fw_des_di_parameter (servicename, sectionname, MethodID, parametername )");
                sbQuery.AppendLine("create clustered index fw_des_di_placeholder_indx on #fw_des_di_placeholder(servicename, sectionname, sequenceno, methodid, placeholdername, errorid)");
                sbQuery.AppendLine("create clustered index fw_des_error_indx on #fw_des_error (errorid)");
                sbQuery.AppendLine("create clustered index fw_des_integ_serv_map_indx on #fw_des_integ_serv_map(callingservicename, sectionname, sequenceno, integservicename, integsegment, integdataitem)");
                sbQuery.AppendLine("create clustered index fw_des_processsection_indx on #fw_des_processsection(servicename, sectionname)");
                sbQuery.AppendLine("create clustered index fw_des_processsection_br_is_indx on #fw_des_processsection_br_is (servicename, sectionname, sequenceno, methodid)");
                sbQuery.AppendLine("create clustered index fw_des_sp_indx on #fw_des_sp (methodid )");
                sbQuery.AppendLine("create clustered index fw_des_bo_indx on #fw_des_bo (componentname, bocode )");
                sbQuery.AppendLine("create clustered index fw_des_error_placeholder_indx on #fw_des_error_placeholder (ErrorID, PlaceholderName)");
                sbQuery.AppendLine("create clustered index fw_des_reqbr_desbr_indx on #fw_des_reqbr_desbr (ReqBRName, MethodID)");
                sbQuery.AppendLine("create clustered index fw_req_task_rule_indx on #fw_req_task_rule (TaskName, BRSequence, BRName)");
                sbQuery.AppendLine("create clustered index fw_des_ilbo_placeholder_indx on #fw_des_ilbo_placeholder (ilbocode, ControlID, EventName, PlaceholderName, ErrorID, CtrlEvent_ViewName)");
                sbQuery.AppendLine("create clustered index fw_des_ilbo_ctrl_event_indx on #fw_des_ilbo_ctrl_event (ILBOCode, ControlID, taskname)");
                sbQuery.AppendLine("create clustered index fw_req_businessrule_indx on #fw_req_businessrule (BRName)");
                sbQuery.AppendLine("create clustered index fw_req_br_error_indx on #fw_req_br_error (BRName, ErrorCode)");
                sbQuery.AppendLine("create clustered index fw_des_err_det_local_info_indx on #fw_des_err_det_local_info (errorid, langid)");
                sbQuery.AppendLine("create clustered index fw_exrep_task_temp_map_indx on #fw_exrep_task_temp_map (activity_name, ui_name, page_name, task_name)");
                sbQuery.AppendLine("create clustered index de_log_ext_ctrl_met_idx on #de_log_ext_ctrl_met(le_customer_name, le_project_name, col_seq)");
                sbQuery.AppendLine("create index #fw_des_integ_segmap_dtl_idx on #fw_des_integ_segmap_dtl(callerservice, sequenceno, integservice)");
                sbQuery.AppendLine("create index #fw_des_service_segment_dtl_idx on #fw_des_service_segment_dtl(servicename, segmentsequence)");

                ExecuteNonQueryResult(sbQuery.ToString());
                WriteProfiler("Creating index for Hashtables end.");
            }
            catch (Exception ex) { WriteProfiler("Exception raised while creating index in Hashtables for EcrNo : " + GlobalVar.Ecrno, ex.Message); }
        }

        private void DropTables()
        {
            try
            {
                WriteProfiler("Drop Hashtables start.");
                StringBuilder sbQuery = new StringBuilder();
                sbQuery.AppendLine("Drop Table #fw_req_activity");
                sbQuery.AppendLine("Drop Table #fw_req_activity_ilbo");
                sbQuery.AppendLine("Drop Table #fw_req_ilbo");
                sbQuery.AppendLine("Drop Table #fw_req_ilbo_control_property");
                sbQuery.AppendLine("Drop Table #fw_req_ilbo_control ");
                sbQuery.AppendLine("Drop Table #fw_req_ilbo_view ");
                sbQuery.AppendLine("Drop Table #fw_req_bterm");
                sbQuery.AppendLine("Drop Table #fw_req_bterm_synonym");
                sbQuery.AppendLine("Drop Table #fw_req_process_component");
                sbQuery.AppendLine("Drop Table #fw_req_ilbo_tab_properties");
                sbQuery.AppendLine("Drop Table #fw_req_activity_ilbo_task");
                sbQuery.AppendLine("Drop Table #fw_req_task");
                sbQuery.AppendLine("Drop Table #fw_req_bterm_enumerated_option");
                sbQuery.AppendLine("Drop Table #fw_req_ilbo_link_publish");
                sbQuery.AppendLine("Drop Table #fw_req_ilbo_data_publish");
                sbQuery.AppendLine("Drop Table #fw_req_ilbo_data_use");
                sbQuery.AppendLine("Drop Table #fw_req_ilbo_linkuse");
                sbQuery.AppendLine("Drop Table #fw_des_ilbo_services");
                sbQuery.AppendLine("Drop Table #fw_des_service_dataitem");
                sbQuery.AppendLine("Drop Table #fw_asp_codegen_tmp");
                sbQuery.AppendLine("Drop Table #fw_des_service ");
                sbQuery.AppendLine("Drop Table #fw_des_service_segment");
                sbQuery.AppendLine("Drop Table #fw_des_ilbo_service_view_datamap");
                sbQuery.AppendLine("Drop Table #fw_des_task_segment_attribs");
                sbQuery.AppendLine("Drop Table #fw_req_task_local_info");
                sbQuery.AppendLine("Drop Table #fw_req_ilbo_link_local_info");
                sbQuery.AppendLine("Drop Table #fw_req_activity_ilbo_task_extension_map");
                sbQuery.AppendLine("Drop Table #fw_req_activity_task");
                sbQuery.AppendLine("Drop Table #fw_req_ilbo_local_info");
                sbQuery.AppendLine("Drop Table #fw_req_ilbo_tabs");
                sbQuery.AppendLine("Drop Table #fw_req_lang_bterm_synonym");
                sbQuery.AppendLine("Drop Table #fw_req_precision");
                sbQuery.AppendLine("Drop Table #fw_req_system_parameters");
                sbQuery.AppendLine("Drop Table #fw_des_be_placeholder");
                sbQuery.AppendLine("Drop Table #fw_des_br_logical_parameter");
                sbQuery.AppendLine("Drop Table #fw_des_brerror");
                sbQuery.AppendLine("Drop Table #fw_des_bro");
                sbQuery.AppendLine("Drop Table #fw_des_businessrule");
                sbQuery.AppendLine("Drop Table #fw_des_context");
                sbQuery.AppendLine("Drop Table #fw_des_di_parameter");
                sbQuery.AppendLine("Drop Table #fw_des_di_placeholder");
                sbQuery.AppendLine("Drop Table #fw_des_error");
                sbQuery.AppendLine("Drop Table #fw_des_integ_serv_map");
                sbQuery.AppendLine("Drop Table #fw_des_processsection");
                sbQuery.AppendLine("Drop Table #fw_des_processsection_br_is");
                sbQuery.AppendLine("Drop Table #fw_des_processsection_br_is_err");
                sbQuery.AppendLine("Drop Table #fw_des_sp");
                sbQuery.AppendLine("Drop Table #fw_des_bo");
                sbQuery.AppendLine("Drop Table #fw_des_svco");
                sbQuery.AppendLine("Drop Table #fw_des_error_placeholder");
                sbQuery.AppendLine("Drop Table #fw_des_reqbr_desbr");
                sbQuery.AppendLine("Drop Table #fw_req_task_rule");
                sbQuery.AppendLine("Drop Table #fw_des_ilbo_placeholder");
                sbQuery.AppendLine("Drop Table #fw_des_ilbo_ctrl_event");
                sbQuery.AppendLine("Drop Table #fw_req_businessrule");
                sbQuery.AppendLine("Drop Table #fw_req_br_error");
                sbQuery.AppendLine("Drop Table #LP_ILBO_Tmp");
                sbQuery.AppendLine("Drop Table #LP_Service_Tmp");
                sbQuery.AppendLine("Drop Table #service_tmp");
                sbQuery.AppendLine("Drop Table #LP_PublishErr_Tmp");
                sbQuery.AppendLine("Drop Table #lp_rvwobjects_tmp");
                sbQuery.AppendLine("Drop Table #fw_req_ILBO_Transpose");
                sbQuery.AppendLine("Drop Table #fw_req_ilbo_task_rpt");
                sbQuery.AppendLine("Drop Table #fw_des_chart_service_segment");
                sbQuery.AppendLine("Drop Table #fw_task_service_map");
                sbQuery.AppendLine("Drop Table #fw_des_focus_control");
                sbQuery.AppendLine("Drop Table #meta_severity");
                sbQuery.AppendLine("Drop Table #fw_req_activity_PLBO");
                sbQuery.AppendLine("Drop Table #fw_req_plbo");
                sbQuery.AppendLine("Drop Table #fw_des_ilbo_focus_control");
                sbQuery.AppendLine("Drop Table #fw_des_service_view_datamap");
                sbQuery.AppendLine("Drop Table #fw_des_ilerror");
                sbQuery.AppendLine("Drop Table #fw_des_err_det_local_info");
                sbQuery.AppendLine("Drop Table #fw_des_corr_action_local_info");
                sbQuery.AppendLine("Drop Table #fw_req_language");
                sbQuery.AppendLine("Drop Table #fw_des_plbo_placeholder");
                sbQuery.AppendLine("Drop Table #fw_req_control_property");
                sbQuery.AppendLine("Drop Table #fw_des_plerror");
                sbQuery.AppendLine("Drop Table #fw_req_activity_local_info");
                sbQuery.AppendLine("Drop Table #fw_req_ilboctrl_initval");
                sbQuery.AppendLine("Drop Table #tmptable");
                sbQuery.AppendLine("Drop Table #phtable");
                sbQuery.AppendLine("Drop Table #fw_des_service_dataitem_Applog");
                sbQuery.AppendLine("Drop Table #fw_service_validation_message");
                sbQuery.AppendLine("drop table #de_published_service_logic_extn_dtl");
                sbQuery.AppendLine("drop table #es_comp_ctrl_type_mst");
                sbQuery.AppendLine("drop table #de_log_ext_ctrl_met");
                sbQuery.AppendLine("drop table #de_published_ui_control");
                sbQuery.AppendLine("drop table #de_published_ui_grid");
                sbQuery.AppendLine("drop table #de_published_ui_page");
                sbQuery.AppendLine("drop table #fw_exrep_task_temp_map");
                sbQuery.AppendLine("drop table #fw_des_service_dtl");
                sbQuery.AppendLine("drop table #fw_des_service_segment_dtl");
                sbQuery.AppendLine("drop table #fw_des_integ_outsegmap_dtl");
                sbQuery.AppendLine("drop table #fw_des_integ_segmap_dtl");
                sbQuery.AppendLine("drop table #fw_des_processsection_dtl");
                sbQuery.AppendLine("drop table #fw_des_caller_integ_serv_map");
                sbQuery.AppendLine("drop table #fw_ezeeview_spparamlist");
                sbQuery.AppendLine("drop table #fw_ezeeview_sp");
                sbQuery.AppendLine("drop table #fw_des_service_dataitem_dtl");
                sbQuery.AppendLine("drop table #fw_des_parameter_cnt");
                sbQuery.AppendLine("drop table #fw_extjs_control_dtl");
                sbQuery.AppendLine("drop table #fw_extjs_link_grid_map");
                sbQuery.AppendLine("drop table #fw_des_integ_serv_map_dtl");
                sbQuery.AppendLine("drop table #de_listedit_view_datamap");
                sbQuery.AppendLine("drop table #de_ezeereport_task_control");
                sbQuery.AppendLine("drop table #fw_req_control_dtls");
                sbQuery.AppendLine("drop table #fw_req_control_view_dtls");
                sbQuery.AppendLine("drop table #fw_req_task_dtls");
                sbQuery.AppendLine("drop table #fw_req_link_subscription_dtls");
                sbQuery.AppendLine("drop table #fw_req_pub_link_sub_dtls");
                sbQuery.AppendLine("drop table #fw_req_link_dtls");
                sbQuery.AppendLine("drop table #fw_req_pub_link_dtls");
                sbQuery.AppendLine("drop table #fw_des_webservice_dtl");
                sbQuery.AppendLine("drop table #fw_des_webservice_segment_dtl");
                sbQuery.AppendLine("drop table #fw_des_webservice_dataitem_dtl");
                sbQuery.AppendLine("drop table #fw_des_parameter_bterm_dtls");
                sbQuery.AppendLine("drop table #fw_des_br_pp_count");
                sbQuery.AppendLine("drop table #fw_des_br_lp_count");
                sbQuery.AppendLine("drop table #fw_des_dataitem_parameter");
                sbQuery.AppendLine("drop table #fw_des_dataitem_parameter_cnt");
                sbQuery.AppendLine("drop table #fw_des_tree_service_dataitem");
                ExecuteNonQueryResult(sbQuery.ToString());
                WriteProfiler("Drop Hashtables end.");
            }
            catch 
            {
            }
            
            //(Exception ex) { WriteProfiler("Exception raised while dropping Hashtables for EcrNo : " + GlobalVar.Ecrno + " " + ex.Message); }
        }
    }
}
