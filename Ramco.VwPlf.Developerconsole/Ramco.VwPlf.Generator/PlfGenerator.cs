//-----------------------------------------------------------------------
// <copyright file="IlboSchema.cs" company="Ramco Systems">
//     Copyright (c) . All rights reserved.
// Release id 	: PLF2.0_02398
// Description 	: Coded for desktop generator.
// By			: Karthikeyan V S
// Date 		: 23-Nov-2012
// </copyright>
//-----------------------------------------------------------------------
//  Case ID             : TECH-16278
//  Modified By         : Madhan Sekar M
//  Reason For Change   : Implementing QueryXml Generation for MHUB2
//  Modified On         : 24-Nov-2017
//------------------------------------------------------------------------

namespace Ramco.VwPlf.Generator
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml.Linq;
    using System.Text;
    using System.IO;

    public class PlfGenerator : Common
    {
        public PlfGenerator()
        {
            GlobalVar.ProfilerLog = new Dictionary<int, string>();
            GlobalVar.Key = 0;
        }

        public bool Generator(string sXml)
        {
            return SetCodeGenerationOptions(sXml) ? Generator() : false;
        }

        public bool Generator()
        {
            WriteProfiler("Code Generator started for ECR Number : " + GlobalVar.Ecrno);
            bool bFlag = true;
            try
            {
                if ((new HashTable()).HashTableInsert())
                {

                    bFlag = GlobalVar.ServiceSchema ? (new ServiceSchema()).SchemaHeader().Equals(true) ? true : false : true;
                    bFlag = GlobalVar.ErrorSchema ? (new ErrorSchema()).SchemaHeader().Equals(true) ? bFlag.Equals(true) ? true : false : false : true;
                    bFlag = GlobalVar.ActivityServiceSchema ? (new ActivityServiceSchema()).SchemaHeader().Equals(true) ? bFlag.Equals(true) ? true : false : false : true;
                    bFlag = GlobalVar.IlboSchema ? (new IlboSchema()).SchemaHeader().Equals(true) ? bFlag.Equals(true) ? true : false : false : true;
                    bFlag = GlobalVar.MHub2 ? (new QueryXmlGenerator()).Generate().Equals(true) ? bFlag.Equals(true) ? true : false : false : true; //TECH-16278
                    bFlag = new ServiceSchemaJson().SchemaHeader();
                }
                return bFlag;
            }
            catch (Exception e)
            {
                WriteProfiler(string.Format("Exception raised in Generator class for ECR Number : {0} : {1}", GlobalVar.Ecrno, e.Message));
                return false;
            }
            finally
            {
                CloseConnection();
                WriteProfiler("Code Generator ended for ECR Number : " + GlobalVar.Ecrno);
                WriteLogToFile();
            }
        }

        private bool SetCodeGenerationOptions(string sXml)
        {
            try
            {
                XDocument _XDocument = XDocument.Parse(sXml);
                var _Model = (from _Data in _XDocument.Descendants("model")
                              select new
                              {
                                  Customer = _Data.Attribute("customer").Value.ToString(),
                                  Project = _Data.Attribute("project").Value.ToString(),
                                  Ecrno = _Data.Attribute("ecrno").Value.ToString(),
                                  Comopnent = _Data.Attribute("component").Value.ToString(),
                                  Appliation_rm_type = _Data.Attribute("appliation_rm_type").Value.ToString(),
                                  Generationpath = _Data.Attribute("generationpath").Value.ToString(),
                                  Platform = _Data.Attribute("platform").Value.ToString(),
                                  Connectionstring = _Data.Attribute("connectionstring").Value.ToString(),
                                  //RequestId = _Data.Attribute("requestid").Value.ToString()
                              }).FirstOrDefault();
                GlobalVar.Customer = _Model.Customer;
                GlobalVar.Project = _Model.Project;
                GlobalVar.Ecrno = _Model.Ecrno;
                GlobalVar.Component = _Model.Comopnent;
                GlobalVar.Platform = _Model.Platform;
                GlobalVar.GenerationPath = _Model.Generationpath;
                GlobalVar.ConnectionString = _Model.Connectionstring;
                //if (!string.IsNullOrEmpty(_Model.RequestId))
                //    GlobalVar.GenerationPath = Path.Combine(GlobalVar.GenerationPath, _Model.RequestId);

                var logs = (from _Data in _XDocument.Descendants("logging")
                            select new
                            {
                                Trace = _Data.Attribute("trace").Value.ToString(),
                                Log = _Data.Attribute("log").Value.ToString()
                            }).FirstOrDefault();
                GlobalVar.Trace = logs.Trace.Equals("true");
                GlobalVar.Log = logs.Log.Equals("true");
                CreateDirectoryStructure();
                var _Option = (from _Data in _XDocument.Descendants("xml")
                               select new
                               {
                                   Service = _Data.Attribute("service").Value.ToString(),
                                   Activity = _Data.Attribute("activity").Value.ToString(),
                                   Error = _Data.Attribute("error").Value.ToString(),
                                   Taskservice = _Data.Attribute("taskservice").Value.ToString(),
                                   MHub2 = Convert.ToBoolean(_Data.Attribute("mhub2").Value)
                               }).FirstOrDefault();
                GlobalVar.ServiceSchema = _Option.Service.Equals("true");
                GlobalVar.IlboSchema = _Option.Activity.Equals("true");
                GlobalVar.ErrorSchema = _Option.Error.Equals("true");
                GlobalVar.ActivityServiceSchema = _Option.Taskservice.Equals("true");
                GlobalVar.MHub2 = _Option.MHub2; //TECH-16278

                GlobalVar.Activity = (from _Data in _XDocument.Descendants("activity")
                                      select new ListObject
                                      {
                                          ID = _Data.Attribute("name").Value.ToString(),
                                          Value = _Data.Attribute("value").Value.ToString()
                                      }).ToList<ListObject>();

                //TECH-16278 **starts**
                GlobalVar.Ui = (from _Data in _XDocument.Descendants("htm")
                                select new ListObject
                                {
                                    ID = _Data.Attribute("activityname").Value.ToString(),
                                    Value = _Data.Attribute("uiname").Value.ToString()
                                }).ToList<ListObject>();
                //TECH-16278 **ends**

                IList<ListObject> _Lang = (from _Data in _XDocument.Descendants("language")
                                           select new ListObject
                                           {
                                               ID = _Data.Attribute("id").Value.ToString(),
                                               Value = _Data.Attribute("language").Value.ToString()
                                           }).ToList<ListObject>();
                _XDocument.Save(string.Format(@"{0}\{1}_desktop.xml", GlobalVar.GenerationPath, GlobalVar.Ecrno));
                WriteProfiler("Code Generator SetCodeGenerationOptions ended.");
                return true;
            }
            catch (Exception e)
            {
                WriteProfiler("Exception raised in SetCodeGenerationOptions", e.Message);
                return false;
            }
        }
    }

    public class ListObject
    {
        public String ID { get; set; }
        public String Value { get; set; }
    }
}
