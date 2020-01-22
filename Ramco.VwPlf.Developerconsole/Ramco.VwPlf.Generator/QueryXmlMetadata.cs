//------------------------------------------------------------------------
//  Case ID             : TECH-16278
//  Created By          : Madhan Sekar M
//  Reason For Change   : Implementing QueryXml Generation for MHUB2
//  Modified On         : 24-Nov-2017
//------------------------------------------------------------------------
//  Case ID             : TECH-30103
//  Created By          : Madhan Sekar M
//  Reason For Change   : Code for SystemTask Node
//  Modified On         : 07-Jan-2019
//------------------------------------------------------------------------
//	Case ID				: TECH-33808
//	Created By			: Madhan Sekar M
//	Reason For Change	: Multi Query printing header control on its node
//	Modified On			: 09-May-2019
//------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Data;
using System.Data.SqlClient;
using System.Xml.Serialization;
using System.Xml;

namespace Ramco.VwPlf.Generator
{

    public class QueryXmlMetadata
    {
        private String _activityName, _uiName, _connectionString;
        private Schema _schema;
        public Schema schema
        {
            get
            {
                return this._schema;
            }
        }
        public QueryXmlMetadata(string sConnectionString, string sActivityName, string sUiName)
        {
            this._schema = null;
            this._connectionString = sConnectionString;
            this._activityName = sActivityName.ToLower();
            this._uiName = sUiName.ToLower();
            this._schema = Populate();
        }
        private Schema Populate()
        {
            DataTable dtMsgCtrlMap = null;
            DataTable dtQuery = null;
            DataTable dtQueryParam = null;
            DataTable dtTaskQueryMap = null;
            DataTable dtMsgPh = null;

            //TECH-30103
            DataTable dtSystemTasks = null;

            Dictionary<string, string> paramValueDictionary = new Dictionary<string, string>();
            paramValueDictionary.Add("@customer_name", GlobalVar.Customer);
            paramValueDictionary.Add("@project_name", GlobalVar.Project);
            paramValueDictionary.Add("@ecr_no", GlobalVar.Ecrno);

            this.fillDataTable(ref dtMsgCtrlMap, "select * from de_published_offtask_message_vw_fn(@customer_name,@project_name,@ecr_no)", CommandType.Text, paramValueDictionary);
            this.fillDataTable(ref dtQuery, "select * from de_published_offtask_query_vw_fn(@customer_name,@project_name,@ecr_no)", CommandType.Text, paramValueDictionary);
            this.fillDataTable(ref dtQueryParam, "select * from de_published_offtask_queryparam_vw_fn(@customer_name,@project_name,@ecr_no)", CommandType.Text, paramValueDictionary);
            this.fillDataTable(ref dtTaskQueryMap, "select * from de_published_offtask_tskqrymap_vw_fn(@customer_name,@project_name,@ecr_no)", CommandType.Text, paramValueDictionary);
            this.fillDataTable(ref dtMsgPh, "select * from de_published_offtask_message_ph_vw_fn(@customer_name,@project_name,@ecr_no)", CommandType.Text, paramValueDictionary);

            //TECH-30103 - For SystemTask Nodes
            this.fillDataTable(ref dtSystemTasks, "select * from de_published_offtask_systask_vw_fn(@customer_name,@project_name,@ecr_no)", CommandType.Text, paramValueDictionary);

            Schema schema = new Schema();

            try
            {
                #region schema.Queries
                List<SchemaQuery> queries = new List<SchemaQuery>();
                dtQuery.AsEnumerable().Where(r => string.Compare(r.Field<string>("ui_name"), _uiName, true) == 0)
                                        .ToList()
                                        .ForEach(Q =>
                                        {
                                            SchemaQuery query = new SchemaQuery
                                            {
                                                Name = Q.Field<string>("query_name"),
                                                QueryType = Q.Field<string>("query_type"),
                                                Instance = Q.Field<string>("query_instance"),
                                                Text = new System.Xml.XmlDocument().CreateCDataSection(Q.Field<string>("query_text"))
                                            };
                                            List<SchemaQueryParameter> parameters = new List<SchemaQueryParameter>();
                                            dtQueryParam.AsEnumerable().Where(r => string.Compare(r.Field<string>("ui_name"), _uiName, true) == 0
                                                                                && string.Compare(r.Field<string>("query_name"), query.Name, true) == 0)
                                                                       .ToList()
                                                                       .ForEach(P =>
                                                                       {
                                                                           SchemaQueryParameter parameter = new SchemaQueryParameter
                                                                           {
                                                                               Name = P.Field<string>("parameter_name"),
                                                                               ControlID = P.Field<string>("Control_ID"),
                                                                               ViewName = P.Field<string>("View_Name"),
                                                                               FlowDirection = P.Field<string>("flow_direction"),
                                                                               DataType = P.Field<string>("Datatype"),
                                                                               BaseControlType = P.Field<string>("base_ctrl_type"),
                                                                               BTSynonym = P.Field<string>("Control/Column BT Synonym")
                                                                           };
                                                                           parameters.Add(parameter);
                                                                       });
                                            query.Parameters = parameters.ToArray();

                                            //QueryInstance - multi
                                            if (string.Compare(query.Instance, "multi", true) == 0)
                                            {
                                                // Grid with Flow Direction Out
                                                var filteredParams = parameters.Where(p => string.Compare(p.ControlID, p.ViewName, true) != 0 && p.FlowDirection == "1" && p.ControlID.ToUpper().StartsWith("ML"));
                                                if (filteredParams.Count() == 0)
                                                {
                                                    // Combo with Flow Direction Out
                                                    filteredParams = parameters.Where(p => (p.BaseControlType == "combo" && p.FlowDirection == "1"));

                                                    // Grid with Flow Direction IN
                                                    if (filteredParams.Count() == 0)
                                                    {
                                                        filteredParams = parameters.Where(p => string.Compare(p.ControlID, p.ViewName, true) != 0 && p.ControlID.ToUpper().StartsWith("ML"));
                                                    }
                                                }
                                                if (filteredParams.Any())
                                                {
                                                    query.ControlID = filteredParams.First().ControlID;
                                                }
                                            }

                                            queries.Add(query);
                                        });
                schema.Queries = queries.ToArray();
                #endregion

                #region schema.Tasks
                List<SchemaTask> tasks = new List<SchemaTask>();
                dtTaskQueryMap.AsEnumerable().Where(r => string.Compare(r.Field<string>("ui_name"), _uiName, true) == 0)
                                            .GroupBy(r => r.Field<string>("task_name"))
                                            .ToList<IGrouping<string, DataRow>>()
                                            .ForEach(T =>
                                            {
                                                SchemaTask task = new SchemaTask
                                                {
                                                    Name = T.Key
                                                };
                                                List<SchemaTaskSeq> sequences = new List<SchemaTaskSeq>();
                                                T.AsEnumerable()
                                                 .OrderBy(r => r.Field<int>("query_seq"))
                                                 .ToList<DataRow>()
                                                                .ForEach(s =>
                                                                {
                                                                    sequences.Add(new SchemaTaskSeq
                                                                    {
                                                                        No = Convert.ToString(s.Field<int>("query_seq")),
                                                                        QueryName = s.Field<string>("query_name")
                                                                    });
                                                                });
                                                task.Seqs = sequences.ToArray();
                                                tasks.Add(task);
                                            });
                schema.Tasks = tasks.ToArray();
                #endregion

                #region schema.Messages
                List<SchemaMessage> messages = new List<SchemaMessage>();
                dtMsgCtrlMap.AsEnumerable().Where(r => string.Compare(r.Field<string>("ui_name"), _uiName, true) == 0)
                    .GroupBy(r => r.Field<string>("message_id"))
                    .ToList<IGrouping<string, DataRow>>()
                    .ForEach(M =>
                    {
                        SchemaMessage message = new SchemaMessage
                        {
                            ID = M.Key,
                            Severity = M.First().Field<string>("severity")
                        };

                        List<SchemaMessageLanguage> languages = new List<SchemaMessageLanguage>();
                        M.AsEnumerable()
                            .GroupBy(r => Convert.ToString(r.Field<int>("language_id")))
                            .ToList<IGrouping<string, DataRow>>()
                            .ForEach(L =>
                            {
                                languages.Add(new SchemaMessageLanguage
                                {
                                    ID = L.Key,
                                    Message = L.First().Field<string>("message_desc")
                                });
                            });

                        List<SchemaMessagePH> placeholders = new List<SchemaMessagePH>();
                        dtMsgPh.AsEnumerable()
                            .Where(r => string.Compare(M.First().Field<string>("activity_name"), r.Field<string>("activity_name"), true) == 0
                                        && string.Compare(M.First().Field<string>("ui_name"), r.Field<string>("ui_name"), true) == 0
                                        && string.Compare(M.Key, r.Field<string>("message_id"), true) == 0)
                            .OrderBy(r => r.Field<int>("placeholder_seq"))
                            .GroupBy(r => r.Field<int>("placeholder_seq"))
                            .ToList<IGrouping<int, DataRow>>()
                            .ForEach(P =>
                            {
                                placeholders.Add(new SchemaMessagePH
                                {
                                    Seq = P.Key.ToString(),
                                    ControlID = P.First().Field<string>("Control_ID"),
                                    ViewName = P.First().Field<string>("View_Name")
                                });
                            });

                        message.Languages = languages.ToArray();
                        message.PHS = placeholders.ToArray();
                        messages.Add(message);
                    });
                schema.Messages = messages.ToArray();
                #endregion

				//TECH-30103 - For SystemTask Nodes
                #region schema.SystemTasks
                if (dtSystemTasks.Rows.Count > 0)
                {

                    List<SchemaSystemTasksTask> systemTasks = new List<SchemaSystemTasksTask>();
                    dtSystemTasks.AsEnumerable().Where(r => string.Compare(r.Field<string>("ui_name"), _uiName, true) == 0
                                                            && string.Compare(r.Field<string>("Layer"), "parent", true) == 0)
                        .GroupBy(r => r.Field<string>("task_name"))
                        .ToList<IGrouping<string, DataRow>>()
                        .ForEach(T =>
                        {
                            SchemaSystemTasksTask systemTask = new SchemaSystemTasksTask
                            {
                                Name = T.Key,
                                TaskType = T.First().Field<string>("TaskType")
                            };

                            IEnumerable<DataRow> drSystems = dtSystemTasks.AsEnumerable().Where(r => string.Compare(r.Field<string>("ui_name"), _uiName, true) == 0
                                                                                                    && string.Compare(r.Field<string>("Layer"), "child", true) == 0
                                                                                                    && r.Field<string>("task_name").ToLower().Contains(systemTask.Name) == true);

                            if (drSystems.Count() > 0)
                            {
                                List<SchemaSystemTasksTaskSystem> systems = new List<SchemaSystemTasksTaskSystem>();
                                foreach (DataRow drSystem in drSystems)
                                {
                                    SchemaSystemTasksTaskSystem system = new SchemaSystemTasksTaskSystem
                                    {
                                        SysTaskName = drSystem.Field<string>("task_name"),
                                        SysTaskType = drSystem.Field<string>("TaskType")
                                    };
                                    systems.Add(system);
                                }
                                systemTask.System = systems.ToArray();
                            }

                            systemTasks.Add(systemTask);
                        });
                    schema.SystemTasks = new SchemaSystemTasks { Task = systemTasks.ToArray() };
                }
                #endregion schema.SystemTasks
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return schema;
        }

        private void fillDataTable(ref DataTable dt, string sQuery, CommandType commandType, Dictionary<string, string> ht)
        {
            using (SqlConnection sqlCon = new SqlConnection(_connectionString))
            {
                using (SqlCommand sqlCmd = new SqlCommand(sQuery, sqlCon))
                {
                    sqlCmd.CommandType = commandType;
                    sqlCmd.CommandTimeout = 0;

                    var enumerator = ht.GetEnumerator();
                    while (enumerator.MoveNext())
                    {
                        KeyValuePair<string, string> param = enumerator.Current;
                        sqlCmd.Parameters.Add(new SqlParameter(param.Key, param.Value));
                    }

                    using (SqlDataAdapter sqlDA = new SqlDataAdapter(sqlCmd))
                    {
                        dt = new DataTable();
                        sqlCon.Open();
                        sqlDA.Fill(dt);
                        sqlCon.Close();
                    }
                }
            }
        }

        public void saveAsXml()
        {
            string sDir = System.IO.Path.Combine(GlobalVar.GenerationPath, GlobalVar.Platform, GlobalVar.Customer, GlobalVar.Project, GlobalVar.Ecrno, "Updated",
                 GlobalVar.Component, "Release", "mHUB2", "Deliverables", GlobalVar.Component, "Queries");

            using (System.IO.MemoryStream memoryStream = new System.IO.MemoryStream())
            {
                XmlSerializer xmlSeraializer = new XmlSerializer(typeof(Schema));
                xmlSeraializer.Serialize(memoryStream, _schema);

                //check whether the ui has atleast one query
                if (memoryStream.Length > 185)
                {
                    if (!System.IO.Directory.Exists(sDir))
                        System.IO.Directory.CreateDirectory(sDir);

                    XmlWriter xmlWriter = XmlWriter.Create(System.IO.Path.Combine(sDir, string.Format("{0}_{1}.xml", _activityName, _uiName)), new XmlWriterSettings { Indent = true });
                    xmlSeraializer.Serialize(xmlWriter, _schema);
                }
            }
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "", IsNullable = false)]
    public partial class Schema
    {

        private SchemaQuery[] queriesField;

        private SchemaTask[] tasksField;

        private SchemaMessage[] messagesField;

		//Added against TECH-30103
        private SchemaSystemTasks systemTasksField;

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayItemAttribute("Query", IsNullable = false)]
        public SchemaQuery[] Queries
        {
            get
            {
                return this.queriesField;
            }
            set
            {
                this.queriesField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayItemAttribute("Task", IsNullable = false)]
        public SchemaTask[] Tasks
        {
            get
            {
                return this.tasksField;
            }
            set
            {
                this.tasksField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayItemAttribute("Message", IsNullable = false)]
        public SchemaMessage[] Messages
        {
            get
            {
                return this.messagesField;
            }
            set
            {
                this.messagesField = value;
            }
        }


		//Added against TECH-30103
        [System.Xml.Serialization.XmlElement()]
        public SchemaSystemTasks SystemTasks
        {
            get
            {
                return this.systemTasksField;
            }
            set
            {
                this.systemTasksField = value;
            }
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class SchemaQuery
    {

        private SchemaQueryParameter[] parametersField;

        private string textField;

        private string nameField;

        private string queryTypeField;

        private string instanceField;

        private string controlIDField;

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayItemAttribute("Parameter", IsNullable = false)]
        public SchemaQueryParameter[] Parameters
        {
            get
            {
                return this.parametersField;
            }
            set
            {
                this.parametersField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElement("QueryText")]
        public System.Xml.XmlCDataSection Text
        {
            get
            {
                return new System.Xml.XmlDocument().CreateCDataSection(this.textField);
            }
            set
            {
                this.textField = value.Value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string Name
        {
            get
            {
                return this.nameField;
            }
            set
            {
                if (!string.IsNullOrEmpty(value))
                    value = value.ToLower();

                this.nameField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string QueryType
        {
            get
            {
                return this.queryTypeField;
            }
            set
            {
                this.queryTypeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string Instance
        {
            get
            {
                return this.instanceField;
            }
            set
            {
                this.instanceField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string ControlID
        {
            get
            {
                return this.controlIDField;
            }
            set
            {
                if (!string.IsNullOrEmpty(value))
                    value = value.ToLower();

                this.controlIDField = value;
            }
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class SchemaQueryParameter
    {

        private string nameField;

        private string controlIDField;

        private string viewNameField;

        private string flowDirectionField;

        private string dataTypeField;
        private string baseControlType;
        private string btSynonymField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string Name
        {
            get
            {
                return this.nameField;
            }
            set
            {
                if (!string.IsNullOrEmpty(value))
                    value = value.ToLower();

                this.nameField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string ControlID
        {
            get
            {
                return this.controlIDField;
            }
            set
            {
                if (!string.IsNullOrEmpty(value))
                    value = value.ToLower();
                this.controlIDField = value;
            }
        }

        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string BaseControlType
        {
            get
            {
                return this.baseControlType;
            }
            set
            {
                if (!string.IsNullOrEmpty(value))
                    value = value.ToLower();
                this.baseControlType = value;

            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string ViewName
        {
            get
            {
                return this.viewNameField;
            }
            set
            {
                if (!string.IsNullOrEmpty(value))
                    value = value.ToLower();

                this.viewNameField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string FlowDirection
        {
            get
            {
                return this.flowDirectionField;
            }
            set
            {
                this.flowDirectionField = value;
            }
        }

        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string DataType
        {
            get
            {
                return this.dataTypeField;
            }
            set
            {
                this.dataTypeField = value;
            }
        }

        [System.Xml.Serialization.XmlAttribute()]
        public string BTSynonym
        {
            get
            {
                return this.btSynonymField;
            }
            set
            {
                this.btSynonymField = value;
            }
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class SchemaTask
    {

        private SchemaTaskSeq[] seqsField;

        private string nameField;

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayItemAttribute("Seq", IsNullable = false)]
        public SchemaTaskSeq[] Seqs
        {
            get
            {
                return this.seqsField;
            }
            set
            {
                this.seqsField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string Name
        {
            get
            {
                return this.nameField;
            }
            set
            {
                if (!string.IsNullOrEmpty(value))
                    value = value.ToLower();

                this.nameField = value;
            }
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class SchemaTaskSeq
    {

        private string noField;

        private string queryNameField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string No
        {
            get
            {
                return this.noField;
            }
            set
            {
                this.noField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string QueryName
        {
            get
            {
                return this.queryNameField;
            }
            set
            {
                if (!string.IsNullOrEmpty(value))
                    value = value.ToLower();
                this.queryNameField = value;
            }
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class SchemaMessage
    {

        private SchemaMessageLanguage[] languagesField;

        private SchemaMessagePH[] pHSField;

        private string idField;

        private string severityField;

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayItemAttribute("Language", IsNullable = false)]
        public SchemaMessageLanguage[] Languages
        {
            get
            {
                return this.languagesField;
            }
            set
            {
                this.languagesField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayItemAttribute("PH", IsNullable = false)]
        public SchemaMessagePH[] PHS
        {
            get
            {
                return this.pHSField;
            }
            set
            {
                this.pHSField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string ID
        {
            get
            {
                return this.idField;
            }
            set
            {
                this.idField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string Severity
        {
            get
            {
                return this.severityField;
            }
            set
            {
                this.severityField = value;
            }
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class SchemaMessageLanguage
    {

        private string idField;

        private string messageField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string ID
        {
            get
            {
                return this.idField;
            }
            set
            {
                this.idField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string Message
        {
            get
            {
                return this.messageField;
            }
            set
            {
                this.messageField = value;
            }
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class SchemaMessagePH
    {

        private string seqField;

        private string controlIDField;

        private string viewNameField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string Seq
        {
            get
            {
                return this.seqField;
            }
            set
            {
                this.seqField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string ControlID
        {
            get
            {
                return this.controlIDField;
            }
            set
            {
                if (!string.IsNullOrEmpty(value))
                    value = value.ToLower();
                this.controlIDField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string ViewName
        {
            get
            {
                return this.viewNameField;
            }
            set
            {
                if (!string.IsNullOrEmpty(value))
                    value = value.ToLower();
                this.viewNameField = value;
            }
        }
    }

	
    /// class added against TECH-30103
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class SchemaSystemTasks
    {
        private SchemaSystemTasksTask[] tasksField;

        [System.Xml.Serialization.XmlElement()]
        public SchemaSystemTasksTask[] Task
        {
            get
            {
                return this.tasksField;
            }
            set
            {
                this.tasksField = value;
            }
        }
    }

    /// class added against TECH-30103
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class SchemaSystemTasksTask
    {
        private string nameField;

        private string taskTypeField;

        private SchemaSystemTasksTaskSystem[] systemField;

        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string Name
        {
            get
            {
                return this.nameField;
            }
            set
            {
                this.nameField = value.ToLower();
            }
        }

        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string TaskType
        {
            get
            {
                return this.taskTypeField;
            }
            set
            {
                this.taskTypeField = value.ToLower();
            }
        }

        [System.Xml.Serialization.XmlElement()]
        public SchemaSystemTasksTaskSystem[] System
        {
            get
            {
                return this.systemField;
            }
            set
            {
                this.systemField = value;
            }
        }

    }

    /// class added against TECH-30103
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class SchemaSystemTasksTaskSystem
    {
        private string sysTaskNameField;

        private string sysTaskTypeField;

        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string SysTaskName
        {
            get
            {
                return this.sysTaskNameField;
            }
            set
            {
                this.sysTaskNameField = value.ToLower();
            }
        }

        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string SysTaskType
        {
            get
            {
                return this.sysTaskTypeField;
            }
            set
            {
                this.sysTaskTypeField = value.ToLower();
            }
        }
    }
}
