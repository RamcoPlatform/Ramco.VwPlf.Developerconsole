//-----------------------------------------------------------------------
// <copyright file="ActivityServiceSchema.cs" company="Ramco Systems">
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
// </copyright>
//-----------------------------------------------------------------------

namespace Ramco.VwPlf.Generator
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml.Linq;
    using System.Text;
    using Ramco.Plf.Global.Interfaces;

    /// <summary>
    /// 
    /// </summary>
    public class ActivityServiceSchema : Common
    {
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public bool SchemaHeader()
        {
            WriteProfiler("Activity Service generation starts.");
            string activityID = string.Empty;
            string activityName = string.Empty;
            string ilboCode = string.Empty;

            try
            {
                if (!GlobalVar.DataCollection.ContainsKey("activity"))
                {
                    return true;
                }

                var activityDetails = from activitys in GlobalVar.DataCollection["activity"]
                                      join act in GlobalVar.Activity.ToList()
                                      on activitys.Attribute("activityname").Value equals act.ID.ToLower()
                                      select activitys;

                foreach (XElement activityDetail in activityDetails)
                {
                    PlfScreenToServiceSchema _PlfScreenToServiceSchema = new PlfScreenToServiceSchema();
                    activityID = activityDetail.Attribute("activityid").Value.ToString();
                    activityName = activityDetail.Attribute("activityname").Value.ToString();
                    ilboCode = activityDetail.Attribute("ilbocode").Value.ToString();
                    WriteProfiler(string.Format("Generation starts for Activityid : {0}, Activityname : {1}, Ilbocode : {2}.", activityID, activityName, ilboCode));
                    var actserviceDetails = from service in GlobalVar.DataCollection["actservices"]
                                             where service.Attribute("activityid").Value.Equals(activityID)
                                               && service.Attribute("ilbocode").Value.Equals(ilboCode)
                                             select service;
                    List<Service> _Service = new List<Service>();
                    foreach (XElement actserviceDetail in actserviceDetails)
                        _Service.Add(new Service { Name = actserviceDetail.Attribute("servicename").Value.ToString() });
                    _PlfScreenToServiceSchema.Services = _Service.ToArray();
                    this.AddSegments(_PlfScreenToServiceSchema, activityID, ilboCode);

                    // new System.Xml.Serialization.XmlSerializer(typeof(PlfScreenToServiceSchema)).Serialize(new System.IO.StreamWriter(string.Format(@"{0}ilbo\svcui_{1}_{2}.xml", GlobalVar.ReleasePath, activityName, ilboCode)), _PlfScreenToServiceSchema);  // Code commented for bug id: PLF2.0_03462
                    new System.Xml.Serialization.XmlSerializer(typeof(PlfScreenToServiceSchema)).Serialize(new System.IO.StreamWriter(string.Format(@"{0}\Model\{3}\svcui_{1}_{2}.xml", GlobalVar.ReleasePath, activityName, ilboCode, GlobalVar.Component)), _PlfScreenToServiceSchema); // Code added for bug id: PLF2.0_03462
                    WriteProfiler(string.Format("Generation end for Activityid : {0}, Activityname : {1}, Ilbocode : {2}.", activityID, activityName, ilboCode));
                }
                WriteProfiler("Activity Service generation ends.");
                return true;
            }
            catch (Exception e)
            {
                WriteProfiler("Exception raised in Activity Service generation.", e.Message);
                return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_PlfScreenToServiceSchema"></param>
        /// <param name="sActivityID"></param>
        /// <param name="sIlboCode"></param>
        /// <returns></returns>
        private bool AddSegments(PlfScreenToServiceSchema _PlfScreenToServiceSchema, string sActivityID, string sIlboCode)
        {
            try
            {
                foreach (Service _Service in _PlfScreenToServiceSchema.Services)
                {
                    var _actsersegmentDetails = from service in GlobalVar.DataCollection["actsersegment"]
                                                where service.Attribute("activityid").Value.Equals(sActivityID)
                                                   && service.Attribute("ilbocode").Value.Equals(sIlboCode)
                                                   && service.Attribute("servicename").Value.Equals(_Service.Name)
                                                select service;
                    List<Segment> _Segment = new List<Segment>();
                    _Segment.Add(new Segment { Name = "fw_context", Sequence = "1", FlowDirection = 1, IsMultiline = false, IsFilling = false, ControlName = string.Empty });
                    foreach (XElement _XElement in _actsersegmentDetails)
                    {
                        _Segment.Add(new Segment
                                    {
                                        Name = _XElement.Attribute("segmentname").Value.ToString(),
                                        Sequence = _XElement.Attribute("seq").Value.ToString(),
                                        FlowDirection = Convert.ToInt32(_XElement.Attribute("flowdirection").Value.ToString()),
                                        IsMultiline = _XElement.Attribute("multiline").Value.ToString().Equals("true"),
                                        IsFilling = _XElement.Attribute("filling").Value.ToString().Equals("true"),
                                        ControlName = _XElement.Attribute("control").Value.ToString()
                                    });
                        WriteProfiler(string.Format("\t Segmentname : {1} added to servicename : {2} Ilbocode : {0}.", sIlboCode, _XElement.Attribute("segmentname").Value.ToString(), _Service.Name));
                    }
                    _Service.Segments = _Segment.ToArray();
                    this.AddDataitem(_Service, sActivityID, sIlboCode, _Service.Name);
                }
                return true;
            }
            catch (Exception e)
            {
                WriteProfiler("Exception in adding Subcription to Links", e.Message);
                return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_Service"></param>
        /// <param name="sActivityID"></param>
        /// <param name="sIlboCode"></param>
        /// <param name="sServiceName"></param>
        /// <returns></returns>
        private bool AddDataitem(Service _Service, string sActivityID, string sIlboCode, string sServiceName)
        {
            try
            {
                foreach (Segment _Segment in _Service.Segments)
                {
                    var _actserdataitemDetails = from service in GlobalVar.DataCollection["actserdataitem"]
                                                 where service.Attribute("activityid").Value.Equals(sActivityID)
                                                    && service.Attribute("ilbocode").Value.Equals(sIlboCode)
                                                    && service.Attribute("servicename").Value.Equals(sServiceName)
                                                    && service.Attribute("segmentname").Value.Equals(_Segment.Name)
                                                 select service;
                    List<Field> _Field = new List<Field>();
                    if (_Segment.Name.Equals("fw_context"))
                    {
                        _Field.Add(new Field { Name = "language", Control = "rvwrt_lctxt_ou", View = "rvwrt_lctxt_ou", FlowDirection = 1 ,BTSynonym= "rvwrt_lctxt_ou" });
                        _Field.Add(new Field { Name = "ouinstance", Control = "rvwrt_cctxt_ou", View = "rvwrt_cctxt_ou", FlowDirection = 1,BTSynonym= "rvwrt_cctxt_ou" });
                    }
                    else
                    {
                        foreach (XElement _XElement in _actserdataitemDetails)
                        {
                            _Field.Add(new Field
                                        {
                                            Name = _XElement.Attribute("dataitemname").Value.ToString(),
                                            Control = _XElement.Attribute("control").Value.ToString(),
                                            View = _XElement.Attribute("view").Value.ToString(),
                                            FlowDirection = Convert.ToInt32(_XElement.Attribute("flow").Value.ToString()),
                                            BTSynonym = _XElement.Attribute("btsynonym")!=null?_XElement.Attribute("btsynonym").Value.ToString():string.Empty
                                        });
                            WriteProfiler(string.Format("\t\t Dataitemname : {3} added to segmentname : {1} servicename : {2} Ilbocode : {0}.", sIlboCode, _XElement.Attribute("segmentname").Value.ToString(), _Service.Name, _XElement.Attribute("dataitemname").Value.ToString()));
                        }
                    }
                    _Segment.Fields = _Field.ToArray();
                }
                return true;
            }
            catch (Exception e)
            {
                WriteProfiler("Exception in adding web layer segment dataitem", e.Message);
                return false;
            }
        }
    }
}
