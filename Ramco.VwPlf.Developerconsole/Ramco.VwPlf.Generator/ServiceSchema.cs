//-----------------------------------------------------------------------
// <copyright file="ServiceSchema.cs" company="Ramco Systems.">
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
// Bug id 	    : PLF2.0_15017
// Description 	: Change in value for ProgID, MethodName, systemgenerated attributes
// By			: Madhan Sekar M
// Date 		: 21-Sep-2015
//-----------------------------------------------------------------------
// </copyright>
//-----------------------------------------------------------------------
namespace Ramco.VwPlf.Generator
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Xml.Linq;
    using Ramco.Plf.Global.Interfaces;

    public class ServiceSchema : Common
    {
        public bool SchemaHeader()
        {
            WriteProfiler("Service schemaHeader start.");
            try
            {
                string serviceName = string.Empty;
                if (!GlobalVar.DataCollection.ContainsKey("service"))
                {
                    return true;
                }

                var serviceDetails = from services in GlobalVar.DataCollection["service"]
                                     select services;
                foreach (XElement serviceDetail in serviceDetails)
                {
                    serviceName = serviceDetail.Attribute("servicename").Value.ToString();
                    RvwService rvwService = new RvwService();
                    rvwService.serviceName = serviceName;
                    rvwService.serviceType = (ServiceType)Enum.Parse(typeof(ServiceType), serviceDetail.Attribute("serviceType").Value.ToString(), true);
                    rvwService.inSegs = serviceDetail.Attribute("inSegs").Value.ToString();
                    rvwService.outSegs = serviceDetail.Attribute("outSegs").Value.ToString();
                    rvwService.ioSegs = serviceDetail.Attribute("ioSegs").Value.ToString();
                    rvwService.scSegs = serviceDetail.Attribute("scSegs").Value.ToString();

                    // Code commented for bug id: PLF2.0_03462 ***start***
                    /*rvwService.inSegCnt = Convert.ToByte(serviceDetail.Attribute("inSegCnt").Value.ToString());
                    rvwService.outSegCnt = Convert.ToByte(serviceDetail.Attribute("outSegCnt").Value.ToString());
                    rvwService.scSegCnt = Convert.ToByte(serviceDetail.Attribute("scSegCnt").Value.ToString());
                    rvwService.psCnt = Convert.ToByte(serviceDetail.Attribute("psCnt").Value.ToString());
                    rvwService.ioSegCnt = Convert.ToByte(serviceDetail.Attribute("ioSegCnt").Value.ToString());*/
                    // Code commented for bug id: PLF2.0_03462 ***end***

                    // Code added for bug id: PLF2.0_03462 ***start***
                    rvwService.inSegCnt = Convert.ToInt32(serviceDetail.Attribute("inSegCnt").Value.ToString());
                    rvwService.outSegCnt = Convert.ToInt32(serviceDetail.Attribute("outSegCnt").Value.ToString());
                    rvwService.scSegCnt = Convert.ToInt32(serviceDetail.Attribute("scSegCnt").Value.ToString());
                    rvwService.psCnt = Convert.ToInt32(serviceDetail.Attribute("psCnt").Value.ToString());
                    rvwService.ioSegCnt = Convert.ToInt32(serviceDetail.Attribute("ioSegCnt").Value.ToString());
                    // Code added for bug id: PLF2.0_03462 ***end***

                    rvwService.dataSegs = serviceDetail.Attribute("dataSegs").Value.ToString();
                    // rvwService.segCnt = Convert.ToByte(serviceDetail.Attribute("segCnt").Value.ToString()); // Code commented for bug id: PLF2.0_03462
                    rvwService.segCnt = Convert.ToInt32(serviceDetail.Attribute("segCnt").Value.ToString()); // Code added for bug id: PLF2.0_03462
                    WriteProfiler("Starting to generate Code for the service-" + serviceName);
                    this.AddProcessSection(rvwService, serviceName);
                    this.AddSegment(rvwService, serviceName);
                    // new System.Xml.Serialization.XmlSerializer(typeof(RvwService)).Serialize(new System.IO.StreamWriter(string.Format(@"{0}\service\svc_{1}.xml", GlobalVar.ReleasePath, serviceName)), rvwService); // Code commented for bug id: PLF2.0_03462
                    new System.Xml.Serialization.XmlSerializer(typeof(RvwService)).Serialize(new System.IO.StreamWriter(string.Format(@"{0}\ServiceXML\{2}\svc_{1}.xml", GlobalVar.ReleasePath, serviceName, GlobalVar.Component)), rvwService); // Code added for bug id: PLF2.0_03462
                    WriteProfiler("Generated Code saved for the service - " + serviceName);
                }

                WriteProfiler("Service schemaHeader end.");
                return true;
            }
            catch (Exception e)
            {
                WriteProfiler("General exception in service schemaheader", e.Message);
                return false;
            }
        }

        private bool AddProcessSection(RvwService rvwService, string serviceName)
        {
            try
            {
                var processSectionDetails = from processsections in GlobalVar.DataCollection["processsection"]
                                            where processsections.Attribute("servicename").Value.Equals(serviceName)
                                            select processsections;
                List<RvwServiceProcessSection> processSection = new List<RvwServiceProcessSection>();
                foreach (XElement processSectionDetail in processSectionDetails)
                {
                    processSection.Add(new RvwServiceProcessSection
                                            {
                                                // seqNo = Convert.ToByte(processSectionDetail.Attribute("seqNo").Value.ToString()), // Code commented for bug id: PLF2.0_03462
                                                seqNo = Convert.ToInt32(processSectionDetail.Attribute("seqNo").Value.ToString()), // Code added for bug id: PLF2.0_03462
                                                controlExpression = processSectionDetail.Attribute("controlExpression").Value.ToString(),
                                                loopingStyle = (PS_LoopingStyle)Enum.Parse(typeof(PS_LoopingStyle), processSectionDetail.Attribute("loopingStyle").Value.ToString(), true),
                                                loopCausingSegment = object.ReferenceEquals(processSectionDetail.Attribute("loopCausingSegment"), null).Equals(true) ? string.Empty : processSectionDetail.Attribute("loopCausingSegment").Value.ToString(),
                                                ceInstDepFlag = (PS_CEInstDepFlag)Enum.Parse(typeof(PS_CEInstDepFlag), processSectionDetail.Attribute("ceInstDepFlag").Value.ToString(), true),
                                                // brsCnt = Convert.ToByte(processSectionDetail.Attribute("brsCnt").Value.ToString()), // Code commented for bug id: PLF2.0_03462
                                                brsCnt = Convert.ToInt32(processSectionDetail.Attribute("brsCnt").Value.ToString()), // Code added for bug id: PLF2.0_03462
                                                sectionName = processSectionDetail.Attribute("sectionName").Value.ToString(),
                                                sectionType = (PS_SectionType)Enum.Parse(typeof(PS_SectionType), processSectionDetail.Attribute("sectionType").Value.ToString(), true)
                                            });
                    WriteProfiler("\t Generating code for Service_Processsection - " + processSectionDetail.Attribute("sectionName").Value.ToString());
                }

                rvwService.ProcessSection = processSection.ToArray();
                this.AddProcesssectionBR(rvwService, serviceName);
                return true;
            }
            catch (Exception e)
            {
                WriteProfiler("Exception in adding Processsection to service", e.Message);
                return false;
            }
        }

        private bool AddProcesssectionBR(RvwService _RvwService, string sServiceName)
        {
            try
            {
                foreach (RvwServiceProcessSection _RvwServiceProcessSection in _RvwService.ProcessSection)
                {
                    var bruleDetails = from brdetails in GlobalVar.DataCollection["brdetails"]
                                       where brdetails.Attribute("servicename").Value.Equals(sServiceName)
                                         && brdetails.Attribute("sectionname").Value.Equals(_RvwServiceProcessSection.sectionName)
                                       select brdetails;
                    List<RvwServiceProcessSectionBR> _RvwServiceProcessSectionBR = new List<RvwServiceProcessSectionBR>();
                    foreach (XElement bruleDetail in bruleDetails)
                    {
                        _RvwServiceProcessSectionBR.Add(new RvwServiceProcessSectionBR
                                                            {
                                                                accessesDatabase = (BR_AccessDB)Enum.Parse(typeof(BR_AccessDB), bruleDetail.Attribute("accessesdatabase").Value.ToString(), true),
                                                                // brlpCnt = Convert.ToByte(bruleDetail.Attribute("brlpcnt").Value.ToString()), // Code commented for bug id: PLF2.0_03462
                                                                brlpCnt = Convert.ToInt32(bruleDetail.Attribute("brlpcnt").Value.ToString()), // Code added for bug id: PLF2.0_03462
                                                                broName = bruleDetail.Attribute("broname").Value.ToString(),
                                                                // brppCnt = Convert.ToByte(bruleDetail.Attribute("brppcnt").Value.ToString()), // Code commented for bug id: PLF2.0_03462
                                                                brppCnt = Convert.ToInt32(bruleDetail.Attribute("brppcnt").Value.ToString()), // Code added for bug id: PLF2.0_03462
                                                                ceInstDepFlag = (PS_CEInstDepFlag)Enum.Parse(typeof(PS_CEInstDepFlag), bruleDetail.Attribute("ceinstdepflag").Value.ToString(), true),
                                                                controlExpression = object.ReferenceEquals(bruleDetail.Attribute("controlexpression"), null).Equals(true) ? string.Empty : bruleDetail.Attribute("controlexpression").Value.ToString(),
                                                                executionFlag = (BR_ExecutionFlag)Enum.Parse(typeof(BR_ExecutionFlag), bruleDetail.Attribute("executionflag").Value.ToString(), true),
                                                                integServiceName = object.ReferenceEquals(bruleDetail.Attribute("integservicename"), null).Equals(true) ? string.Empty : bruleDetail.Attribute("integservicename").Value.ToString(),
                                                                isMethod = (BR_IsMethod)Enum.Parse(typeof(BR_IsMethod), bruleDetail.Attribute("ismethod").Value.ToString(), true),
                                                                loopSegment = bruleDetail.Attribute("loopsegment").Value.ToString(),
                                                                methodID = Convert.ToInt32(bruleDetail.Attribute("methodid").Value.ToString()),

                                                                //PLF2.0_15017 **starts**
                                                                //methodName = bruleDetail.Attribute("methodname").Value.ToString(),
                                                                methodName = bruleDetail.Attribute("systemgenerated").Value.ToString().Equals("0") ? String.Format("{0}Ex", bruleDetail.Attribute("methodname").Value.ToString()) : (bruleDetail.Attribute("systemgenerated").Value.ToString().Equals("3") ? String.Format("{0}Ex", bruleDetail.Attribute("methodname").Value.ToString()) : bruleDetail.Attribute("methodname").Value.ToString()),
                                                                //PLF2.0_15017 **ends**
                                                                
                                                                operationType = bruleDetail.Attribute("operationtype").Value.ToString(),

                                                                //PLF2.0_15017 **starts**
                                                                //progID = (BR_SystemGenerated)Enum.Parse(typeof(BR_SystemGenerated), bruleDetail.Attribute("systemgenerated").Value.ToString().Equals("0") ? "2" : bruleDetail.Attribute("progid").Value.ToString(),
                                                                progID = bruleDetail.Attribute("systemgenerated").Value.ToString().Equals("0") ? String.Format("com.ramco.vw.{0}.cus.C{0}_cus", GlobalVar.Component.ToLower()) : (bruleDetail.Attribute("systemgenerated").Value.ToString().Equals("2") ? String.Format("com.ramco.vw.{0}.br.I{1}BR", GlobalVar.Component.ToLower(), GlobalVar.Component) : bruleDetail.Attribute("progid").Value.ToString()),
                                                                //PLF2.0_15017 **ends**
                                                                
                                                                // seqNo = Convert.ToByte(bruleDetail.Attribute("seqno").Value.ToString()), // Code commented for bug id: PLF2.0_03462
                                                                seqNo = Convert.ToInt32(bruleDetail.Attribute("seqno").Value.ToString()), // Code added for bug id: PLF2.0_03462
                                                                spErrorProtocol = (SPerror_Protocol)Enum.Parse(typeof(SPerror_Protocol), bruleDetail.Attribute("sperrorprotocol").Value.ToString(), true),
                                                                spName = bruleDetail.Attribute("spname").Value.ToString(),

                                                                //PLF2.0_15017 **starts**
                                                                //systemGenerated = (BR_SystemGenerated)Enum.Parse(typeof(BR_SystemGenerated), bruleDetail.Attribute("systemgenerated").Value.ToString())
                                                                systemGenerated = (BR_SystemGenerated)Enum.Parse(typeof(BR_SystemGenerated), bruleDetail.Attribute("systemgenerated").Value.ToString().Equals("0") ? "2" : bruleDetail.Attribute("systemgenerated").Value.ToString())
                                                                //PLF2.0_15017 **ends**
                                                            });
                        WriteProfiler("\t\t Generating code for Service_Processsection_BR - " + bruleDetail.Attribute("methodname").Value.ToString());
                    }

                    _RvwServiceProcessSection.BR = _RvwServiceProcessSectionBR.ToArray();
                    this.AddProcesssectionBRPP(_RvwServiceProcessSection, sServiceName, _RvwServiceProcessSection.sectionName);
                    this.AddProcesssectionBRLP(_RvwServiceProcessSection, sServiceName, _RvwServiceProcessSection.sectionName);
                    this.AddBRIsInSegMap(_RvwServiceProcessSection, sServiceName, _RvwServiceProcessSection.sectionName);
                    this.AddBRIsOutSegMap(_RvwServiceProcessSection, sServiceName, _RvwServiceProcessSection.sectionName);
                }

                return true;
            }
            catch (Exception e)
            {
                WriteProfiler("Exception in adding BR to processsection", e.Message);
                return false;
            }
        }

        private bool AddBRIsInSegMap(RvwServiceProcessSection rvwServiceProcessSection, string serviceName, string sectionName)
        {
            try
            {
                foreach (RvwServiceProcessSectionBR rvwServiceProcessSectionBR in rvwServiceProcessSection.BR)
                {
                    IQueryable<XElement> integsegmapDetails = from data in GlobalVar.DataCollection["integsegmap"]
                                                              where data.Attribute("callerservice").Value.Equals(serviceName)
                                                                && data.Attribute("sectionname").Value.Equals(sectionName)
                                                                && data.Attribute("sequenceno").Value.Equals(rvwServiceProcessSectionBR.seqNo.ToString())
                                                                && data.Attribute("integservice").Value.Equals(rvwServiceProcessSectionBR.integServiceName)
                                                              select data;
                    if (DataExists(integsegmapDetails))
                    {
                        foreach (XElement integsegmapDetail in integsegmapDetails)
                        {
                            rvwServiceProcessSectionBR.IsInSegMap = new RvwServiceProcessSectionBRIsInSegMap()
                                                    {
                                                        callerSegList = integsegmapDetail.Attribute("callerSegList").Value.ToString(),
                                                        isCompName = integsegmapDetail.Attribute("isCompName").Value.ToString(),
                                                        callerOuDi = integsegmapDetail.Attribute("callerOuDi").Value.ToString(),
                                                        callerOuSeg = integsegmapDetail.Attribute("callerOuSeg").Value.ToString(),
                                                        isInSegList = integsegmapDetail.Attribute("isInSegList").Value.ToString()
                                                    };
                            WriteProfiler("\t\t Generating code for Service_Processsection_BR_IS InSegMap - " + rvwServiceProcessSectionBR.integServiceName);
                        }

                        this.AddBRIsInSegMapIsInSeg(rvwServiceProcessSectionBR.IsInSegMap, serviceName, sectionName, rvwServiceProcessSectionBR.seqNo.ToString(), rvwServiceProcessSectionBR.integServiceName);
                    }
                }

                return true;
            }
            catch (Exception e)
            {
                WriteProfiler("Exception in adding method IS IN segment mapping", e.Message);
                return false;
            }
        }

        private bool AddBRIsOutSegMap(RvwServiceProcessSection _RvwServiceProcessSection, string sServiceName, string sSectionName)
        {
            try
            {
                foreach (RvwServiceProcessSectionBR _RvwServiceProcessSectionBR in _RvwServiceProcessSection.BR)
                {
                    var _brisoutsegmapDetails = from data in GlobalVar.DataCollection["brisoutsegmap"]
                                                where data.Attribute("servicename").Value.Equals(sServiceName)
                                                  && data.Attribute("sectionname").Value.Equals(sSectionName)
                                                  && data.Attribute("sequenceno").Value.Equals(_RvwServiceProcessSectionBR.seqNo.ToString())
                                                  && data.Attribute("callerservice").Value.Equals(_RvwServiceProcessSectionBR.integServiceName)
                                                select data;
                    foreach (XElement _XElement in _brisoutsegmapDetails)
                    {
                        _RvwServiceProcessSectionBR.IsOutSegMap = new RvwServiceProcessSectionBRIsOutSegMap() { isSegList = _XElement.Attribute("isSegList").Value.ToString() };
                        WriteProfiler("\t\t Generating code for Service_Processsection_BR_IS OutSegMap - " + _RvwServiceProcessSectionBR.integServiceName);
                    }

                    this.AddBRIsOutSegMapIsOutSeg(_RvwServiceProcessSectionBR.IsOutSegMap, sServiceName, sSectionName, _RvwServiceProcessSectionBR.seqNo.ToString(), _RvwServiceProcessSectionBR.integServiceName);
                }

                return true;
            }
            catch (Exception e)
            {
                WriteProfiler("Exception in adding method IS Out segment mapping", e.Message);
                return false;
            }
        }

        private bool AddBRIsOutSegMapIsOutSeg(RvwServiceProcessSectionBRIsOutSegMap rvwServiceProcessSectionBRIsOutSegMap, string serviceName, string sectionName, string seqNo, string integServiceName)
        {
            try
            {
                var _brisoutsegmapisoutsegDetails = from data in GlobalVar.DataCollection["brisoutsegmapisoutseg"]
                                                    where data.Attribute("callingservicename").Value.Equals(serviceName)
                                                      && data.Attribute("sectionname").Value.Equals(sectionName)
                                                      && data.Attribute("integservicename").Value.Equals(integServiceName)
                                                      && data.Attribute("seqNo").Value.Equals(seqNo)
                                                    select data;
                if (!_brisoutsegmapisoutsegDetails.Count().Equals(0))
                {
                    List<RvwServiceProcessSectionBRIsOutSegMapIsOutSeg> _RvwServiceProcessSectionBRIsOutSegMapIsOutSeg = new List<RvwServiceProcessSectionBRIsOutSegMapIsOutSeg>();
                    foreach (XElement _XElement in _brisoutsegmapisoutsegDetails)
                    {
                        _RvwServiceProcessSectionBRIsOutSegMapIsOutSeg.Add(new RvwServiceProcessSectionBRIsOutSegMapIsOutSeg
                                                                            {
                                                                                isSeg = _XElement.Attribute("isSeg").Value.ToString(),
                                                                                // isSegInst = Convert.ToByte(_XElement.Attribute("instanceflag").Value.ToString()), // Code commented for bug id: PLF2.0_03462
                                                                                isSegInst = Convert.ToInt32(_XElement.Attribute("instanceflag").Value.ToString()), // Code added for bug id: PLF2.0_03462
                                                                                isSegInstSpecified = true,
                                                                                // diCount = Convert.ToByte(_XElement.Attribute("diCount").Value.ToString()), // Code commented for bug id: PLF2.0_03462
                                                                                diCount = Convert.ToInt32(_XElement.Attribute("diCount").Value.ToString()), // Code added for bug id: PLF2.0_03462
                                                                                diCountSpecified = true
                                                                            });
                        WriteProfiler("\t\t\t Generating code for Service_Processsection_BR_ISOutSegMap ISOutSeg - " + _XElement.Attribute("isSeg").Value.ToString());
                    }

                    rvwServiceProcessSectionBRIsOutSegMap.IsOutSeg = _RvwServiceProcessSectionBRIsOutSegMapIsOutSeg.ToArray();
                    this.AddBRIsOutSegMapIsOutSegIsOutDi(rvwServiceProcessSectionBRIsOutSegMap, serviceName, sectionName, seqNo, integServiceName);
                }

                return true;
            }
            catch (Exception e)
            {
                WriteProfiler("Exception in adding method IS Out segment", e.Message);
                return false;
            }
        }

        private bool AddBRIsOutSegMapIsOutSegIsOutDi(RvwServiceProcessSectionBRIsOutSegMap rvwServiceProcessSectionBRIsOutSegMap, string serviceName, string sectionName, string sSeqNo, string integServiceName)
        {
            try
            {
                foreach (RvwServiceProcessSectionBRIsOutSegMapIsOutSeg rvwServiceProcessSectionBRIsOutSegMapIsOutSeg in rvwServiceProcessSectionBRIsOutSegMap.IsOutSeg)
                {
                    byte seqNo = 0;
                    if (GlobalVar.DataCollection.ContainsKey("brisoutsegmapisoutsegisoutdi"))
                    {
                        var brisoutsegmapisoutsegisoutdiDetails = from data in GlobalVar.DataCollection["brisoutsegmapisoutsegisoutdi"]
                                                                  where data.Attribute("servicename").Value.Equals(serviceName)
                                                                     && data.Attribute("sectionname").Value.ToLower().Equals(sectionName)
                                                                     && data.Attribute("segmentname").Value.ToLower().Equals(rvwServiceProcessSectionBRIsOutSegMapIsOutSeg.isSeg)
                                                                     && data.Attribute("integservicename").Value.ToLower().Equals(integServiceName)
                                                                  select data;

                        List<RvwServiceProcessSectionBRIsOutSegMapIsOutSegIsOutDi> rvwServiceProcessSectionBRIsOutSegMapIsOutSegIsOutDi = new List<RvwServiceProcessSectionBRIsOutSegMapIsOutSegIsOutDi>();
                        foreach (XElement brisoutsegmapisoutsegisoutdiDetail in brisoutsegmapisoutsegisoutdiDetails)
                        {
                            rvwServiceProcessSectionBRIsOutSegMapIsOutSegIsOutDi.Add(new RvwServiceProcessSectionBRIsOutSegMapIsOutSegIsOutDi
                                                                                        {
                                                                                            callerDI = brisoutsegmapisoutsegisoutdiDetail.Attribute("callerDI").Value.ToString(),
                                                                                            isDI = brisoutsegmapisoutsegisoutdiDetail.Attribute("isDI").Value.ToString(),
                                                                                            callerSeg = brisoutsegmapisoutsegisoutdiDetail.Attribute("callerSeg").Value.ToString(),
                                                                                            execFlag = (BR_ExecutionFlag)Enum.Parse(typeof(BR_ExecutionFlag), brisoutsegmapisoutsegisoutdiDetail.Attribute("execFlag").Value.ToString()),
                                                                                            execFlagSpecified = true,
                                                                                            seqNo = ++seqNo,
                                                                                            seqNoSpecified = true
                                                                                        });
                            WriteProfiler("\t\t\t Generating code for Service_Processsection_BR_ISOutSegMapISOutSeg Dataitem - " + brisoutsegmapisoutsegisoutdiDetail.Attribute("callerDI").Value.ToString());
                        }

                        rvwServiceProcessSectionBRIsOutSegMapIsOutSeg.IsOutDi = rvwServiceProcessSectionBRIsOutSegMapIsOutSegIsOutDi.ToArray();
                    }
                }

                return true;
            }
            catch (Exception e)
            {
                WriteProfiler("Exception in adding method IS Out segment dataitem mapping: ", e.Message);
                return false;
            }
        }

        private bool AddBRIsInSegMapIsInSeg(RvwServiceProcessSectionBRIsInSegMap rvwServiceProcessSectionBRIsInSegMap, string serviceName, string sectionName, string seqNo, string integServiceName)
        {
            try
            {
                var brisinsegmapisinsegDetails = from data in GlobalVar.DataCollection["brisinsegmapisinseg"]
                                                 where data.Attribute("callingservicename").Value.Equals(serviceName)
                                                  && data.Attribute("sectionname").Value.Equals(sectionName)
                                                  && data.Attribute("integservicename").Value.Equals(integServiceName)
                                                  && data.Attribute("seqNo").Value.Equals(seqNo)
                                                 select data;
                if (!brisinsegmapisinsegDetails.Count().Equals(0))
                {
                    List<RvwServiceProcessSectionBRIsInSegMapIsInSeg> rvwServiceProcessSectionBRIsInSegMapIsInSeg = new List<RvwServiceProcessSectionBRIsInSegMapIsInSeg>();
                    foreach (XElement brisinsegmapisinsegDetail in brisinsegmapisinsegDetails)
                    {
                        rvwServiceProcessSectionBRIsInSegMapIsInSeg.Add(new RvwServiceProcessSectionBRIsInSegMapIsInSeg
                                                                            {
                                                                                callerSeg = brisinsegmapisinsegDetail.Attribute("callerSeg").Value.ToString(),
                                                                                // diCount = Convert.ToByte(brisinsegmapisinsegDetail.Attribute("diCount").Value.ToString()), // Code commented for bug id: PLF2.0_03462
                                                                                diCount = Convert.ToInt32(brisinsegmapisinsegDetail.Attribute("diCount").Value.ToString()), // Code added for bug id: PLF2.0_03462
                                                                                diCountSpecified = true
                                                                            });
                        WriteProfiler("\t\t Generating code for Service_Processsection_BR_ISInSegMap ISInSeg  - " + brisinsegmapisinsegDetail.Attribute("callerSeg").Value.ToString());
                    }

                    rvwServiceProcessSectionBRIsInSegMap.IsInSeg = rvwServiceProcessSectionBRIsInSegMapIsInSeg.ToArray();
                    this.AddBRIsInSegMapIsInSegIsInDi(rvwServiceProcessSectionBRIsInSegMap, serviceName, sectionName, seqNo, integServiceName);
                }

                return true;
            }
            catch (Exception e)
            {
                WriteProfiler("Exception in adding method IS IN segment", e.Message);
                return false;
            }
        }

        private bool AddBRIsInSegMapIsInSegIsInDi(RvwServiceProcessSectionBRIsInSegMap rvwServiceProcessSectionBRIsInSegMap, string serviceName, string sectionName, string sSeqNo, string integServiceName)
        {
            try
            {
                foreach (RvwServiceProcessSectionBRIsInSegMapIsInSeg rvwServiceProcessSectionBRIsInSegMapIsInSeg in rvwServiceProcessSectionBRIsInSegMap.IsInSeg)
                {
                    byte seqNo = 0;
                    var brisinsegmapisinsegisindiDetails = from data in GlobalVar.DataCollection["brisinsegmapisinsegisindi"]
                                                           where data.Attribute("servicename").Value.Equals(serviceName)
                                                              && data.Attribute("sectionname").Value.Equals(sectionName)
                                                              && data.Attribute("segmentname").Value.Equals(rvwServiceProcessSectionBRIsInSegMapIsInSeg.callerSeg)
                                                              && data.Attribute("Integservicename").Value.Equals(integServiceName)
                                                           select data;
                    List<RvwServiceProcessSectionBRIsInSegMapIsInSegIsInDi> rvwServiceProcessSectionBRIsInSegMapIsInSegIsInDi = new List<RvwServiceProcessSectionBRIsInSegMapIsInSegIsInDi>();
                    foreach (XElement brisinsegmapisinsegisindiDetail in brisinsegmapisinsegisindiDetails)
                    {
                        rvwServiceProcessSectionBRIsInSegMapIsInSegIsInDi.Add(new RvwServiceProcessSectionBRIsInSegMapIsInSegIsInDi
                                                                                    {
                                                                                        callerDI = brisinsegmapisinsegisindiDetail.Attribute("callerDI").Value.ToString(),
                                                                                        isDI = brisinsegmapisinsegisindiDetail.Attribute("isDI").Value.ToString(),
                                                                                        isSeg = brisinsegmapisinsegisindiDetail.Attribute("isSeg").Value.ToString(),
                                                                                        isSegInst = (SEG_InstanceFlag)Enum.Parse(typeof(SEG_InstanceFlag), brisinsegmapisinsegisindiDetail.Attribute("isSegInst").Value.ToString()),
                                                                                        seqNo = ++seqNo,
                                                                                        seqNoSpecified = true
                                                                                    });
                        WriteProfiler("\t\t\t Generating code for Service_Processsection_BR_ISInSegMapISInSeg Dataitem - " + brisinsegmapisinsegisindiDetail.Attribute("callerDI").Value.ToString());
                    }

                    rvwServiceProcessSectionBRIsInSegMapIsInSeg.IsInDi = rvwServiceProcessSectionBRIsInSegMapIsInSegIsInDi.ToArray();
                }

                return true;
            }
            catch (Exception e)
            {
                WriteProfiler("Exception in adding method IS In segment dataitem mapping: ", e.Message);
                return false;
            }
        }

        private bool AddProcesssectionBRPP(RvwServiceProcessSection rvwServiceProcessSection, string serviceName, string sectionName)
        {
            try
            {
                foreach (RvwServiceProcessSectionBR rvwServiceProcessSectionBR in rvwServiceProcessSection.BR)
                {
                    var processsectionbrppDetails = from data in GlobalVar.DataCollection["processsectionbrpp"]
                                                    where data.Attribute("servicename").Value.Equals(serviceName)
                                                       && data.Attribute("SectionName").Value.Equals(sectionName)
                                                       && data.Attribute("sequenceno").Value.Equals(rvwServiceProcessSectionBR.seqNo.ToString())
                                                    select data;

                    List<RvwServiceProcessSectionBRPP> rvwServiceProcessSectionBRPP = new List<RvwServiceProcessSectionBRPP>();
                    foreach (XElement processsectionbrppDetail in processsectionbrppDetails)
                    {
                        rvwServiceProcessSectionBRPP.Add(new RvwServiceProcessSectionBRPP
                                                            {
                                                                dataItemName = processsectionbrppDetail.Attribute("dataItemName").Value.ToString(),
                                                                dataSegmentName = processsectionbrppDetail.Attribute("dataSegmentName").Value.ToString(),
                                                                flowDirection = (PP_FlowDirection)Enum.Parse(typeof(PP_FlowDirection), processsectionbrppDetail.Attribute("flowDirection").Value.ToString(), true),
                                                                isCompoundParam = (PP_CompoundParam)Enum.Parse(typeof(PP_CompoundParam), processsectionbrppDetail.Attribute("isCompoundParam").Value.ToString()),
                                                                physicalParameterName = processsectionbrppDetail.Attribute("physicalParameterName").Value.ToString(),
                                                                // seqNo = Convert.ToByte(processsectionbrppDetail.Attribute("seqNo").Value.ToString()), // Code commented for bug id: PLF2.0_03462
                                                                seqNo = Convert.ToInt32(processsectionbrppDetail.Attribute("seqNo").Value.ToString()), // Code added for bug id: PLF2.0_03462
                                                                spParameterType = (PP_SPParameterType)Enum.Parse(typeof(PP_SPParameterType), processsectionbrppDetail.Attribute("spParameterType").Value.ToString(), true)
                                                            });
                        
                        WriteProfiler("\t\t Generating code for Service_Processsection_BR physicalParameterName - " + processsectionbrppDetail.Attribute("physicalParameterName").Value.ToString());
                    }

                    rvwServiceProcessSectionBR.PP = rvwServiceProcessSectionBRPP.ToArray();
                }

                return true;
            }
            catch (Exception e)
            {
                WriteProfiler("Exception in adding Physicalparameter to methods", e.Message);
                return false;
            }
        }

        private bool AddProcesssectionBRLP(RvwServiceProcessSection rvwServiceProcessSection, string serviceName, string sectionName)
        {
            try
            {
                foreach (RvwServiceProcessSectionBR rvwServiceProcessSectionBR in rvwServiceProcessSection.BR)
                {
                    var processsectionbrlpDetails = from data in GlobalVar.DataCollection["processsectionbrlp"]
                                                    where data.Attribute("servicename").Value.Equals(serviceName)
                                                       && data.Attribute("SectionName").Value.Equals(sectionName)
                                                       && data.Attribute("sequenceno").Value.Equals(rvwServiceProcessSectionBR.seqNo.ToString())
                                                    select data;

                    List<RvwServiceProcessSectionBRLP> rvwServiceProcessSectionBRLP = new List<RvwServiceProcessSectionBRLP>();
                    foreach (XElement processsectionbrlpDetail in processsectionbrlpDetails)
                    {
                        rvwServiceProcessSectionBRLP.Add(new RvwServiceProcessSectionBRLP
                                                            {
                                                                dataItemName = processsectionbrlpDetail.Attribute("dataItemName").Value.ToString(),
                                                                dataSegmentName = processsectionbrlpDetail.Attribute("dataSegmentName").Value.ToString(),
                                                                logicalParameterName = processsectionbrlpDetail.Attribute("logicalParameterName").Value.ToString(),
                                                                // seqNo = Convert.ToByte(processsectionbrlpDetail.Attribute("seqNo").Value.ToString()) // Code commented for bug id: PLF2.0_03462
                                                                seqNo = Convert.ToInt32(processsectionbrlpDetail.Attribute("seqNo").Value.ToString()) // Code added for bug id: PLF2.0_03462
                                                            });
                        WriteProfiler("\t\t Generating code for Service_Processsection_BR logicalParameterName - " + processsectionbrlpDetail.Attribute("logicalParameterName").Value.ToString());
                    }

                    rvwServiceProcessSectionBR.LP = rvwServiceProcessSectionBRLP.ToArray();
                }

                return true;
            }
            catch (Exception e)
            {
                WriteProfiler("Exception in adding Logicalparameter to methods", e.Message);
                return false;
            }
        }

        private bool AddSegment(RvwService rvwService, string serviceName)
        {
            try
            {
                var segmentDetails = from data in GlobalVar.DataCollection["segment"]
                                     where data.Attribute("servicename").Value.Equals(serviceName)
                                     select data;

                List<RvwServiceSegment> serviceSegment = new List<RvwServiceSegment>();
                foreach (XElement segmentDetail in segmentDetails)
                {
                    serviceSegment.Add(new RvwServiceSegment
                                            {
                                                segmentName = segmentDetail.Attribute("segmentName").Value.ToString(),
                                                // seqNo = Convert.ToByte(segmentDetail.Attribute("seqNo").Value.ToString()), // Code commented for bug id: PLF2.0_03462
                                                seqNo = Convert.ToInt32(segmentDetail.Attribute("seqNo").Value.ToString()), // Code added for bug id: PLF2.0_03462
                                                instanceFlag = (SEG_InstanceFlag)Enum.Parse(typeof(SEG_InstanceFlag), segmentDetail.Attribute("instanceFlag").Value.ToString()),
                                                flowDirection = (SEG_DI_FlowDirection)Enum.Parse(typeof(SEG_DI_FlowDirection), segmentDetail.Attribute("flowDirection").Value.ToString()),
                                                mandatory = (SEG_DI_Mandatory)Enum.Parse(typeof(SEG_DI_Mandatory), segmentDetail.Attribute("mandatory").Value.ToString()),
                                                // isPartOfHierarchy = Convert.ToByte(segmentDetail.Attribute("isPartOfHierarchy").Value.ToString()), // Code commented for bug id: PLF2.0_03462
                                                isPartOfHierarchy = Convert.ToInt32(segmentDetail.Attribute("isPartOfHierarchy").Value.ToString()), // Code added for bug id: PLF2.0_03462
                                                segKeys = object.ReferenceEquals(segmentDetail.Attribute("segKeys"), null).Equals(true) ? string.Empty : segmentDetail.Attribute("segKeys").Value.ToString(),
                                                // diCnt = Convert.ToByte(segmentDetail.Attribute("diCnt").Value.ToString()), // Code commented for bug id: PLF2.0_03462
                                                diCnt = Convert.ToInt32(segmentDetail.Attribute("diCnt").Value.ToString()), // Code added for bug id: PLF2.0_03462
                                                inDINames = segmentDetail.Attribute("inDINames").Value.ToString(),
                                                ioDINames = segmentDetail.Attribute("ioDINames").Value.ToString(),
                                                outDINames = segmentDetail.Attribute("outDINames").Value.ToString(),
                                                scDINames = segmentDetail.Attribute("scDINames").Value.ToString(),
                                                diNames = segmentDetail.Attribute("diNames").Value.ToString(),

                                                // Code commented for bug id: PLF2.0_03462 ***start***
                                                /*inDICnt = Convert.ToByte(segmentDetail.Attribute("inDICnt").Value.ToString()),
                                                outDICnt = Convert.ToByte(segmentDetail.Attribute("outDICnt").Value.ToString()),
                                                ioDICnt = Convert.ToByte(segmentDetail.Attribute("ioDICnt").Value.ToString()),
                                                scDICnt = Convert.ToByte(segmentDetail.Attribute("scDICnt").Value.ToString())*/
                                                // Code commented for bug id: PLF2.0_03462 ***end***

                                                // Code added for bug id: PLF2.0_03462 ***start***
                                                inDICnt = Convert.ToInt32(segmentDetail.Attribute("inDICnt").Value.ToString()),
                                                outDICnt = Convert.ToInt32(segmentDetail.Attribute("outDICnt").Value.ToString()),
                                                ioDICnt = Convert.ToInt32(segmentDetail.Attribute("ioDICnt").Value.ToString()),
                                                scDICnt = Convert.ToInt32(segmentDetail.Attribute("scDICnt").Value.ToString())
                                                // Code added for bug id: PLF2.0_03462 ***end***
                                            });
                    WriteProfiler("\t\t Generating code for Service_Segment - " + segmentDetail.Attribute("segmentName").Value.ToString());
                }

                rvwService.Segment = serviceSegment.ToArray();
                this.AddSegmentDataItem(rvwService, serviceName);
                return true;
            }
            catch (Exception e)
            {
                WriteProfiler("Exception in adding segment to service", e.Message);
                return false;
            }
        }

        private bool AddSegmentDataItem(RvwService rvwService, string serviceName)
        {
            try
            {
                foreach (RvwServiceSegment serviceSegment in rvwService.Segment)
                {
                    var segmentdataItemDetails = from data in GlobalVar.DataCollection["segmentdataItem"]
                                                 where data.Attribute("servicename").Value.Equals(serviceName)
                                                  && data.Attribute("segmentName").Value.Equals(serviceSegment.segmentName)
                                                 select data;

                    List<RvwServiceSegmentDataItem> serviceSegmentDataItem = new List<RvwServiceSegmentDataItem>();
                    foreach (XElement segmentdataItemDetail in segmentdataItemDetails)
                    {
                        serviceSegmentDataItem.Add(
                                                        new RvwServiceSegmentDataItem
                                                        {
                                                            dataItemName = segmentdataItemDetail.Attribute("dataItemName").Value.ToString(),
                                                            dataType = (DI_DataType)Enum.Parse(typeof(DI_DataType), segmentdataItemDetail.Attribute("dataType").Value.ToString()),
                                                            dataLength = Convert.ToInt32(segmentdataItemDetail.Attribute("dataLength").Value.ToString()),
                                                            isPartOfKey = (DI_IsPartofKey)Enum.Parse(typeof(DI_IsPartofKey), segmentdataItemDetail.Attribute("isPartOfKey").Value.ToString()),
                                                            flowAttribute = (SEG_DI_FlowDirection)Enum.Parse(typeof(SEG_DI_FlowDirection), segmentdataItemDetail.Attribute("flowAttribute").Value.ToString()),
                                                            mandatoryFlag = (SEG_DI_Mandatory)Enum.Parse(typeof(SEG_DI_Mandatory), segmentdataItemDetail.Attribute("mandatoryFlag").Value.ToString()),
                                                            defaultValue = segmentdataItemDetail.Attribute("defaultValue").Value.ToString(),
                                                            bt_Type = (DI_BT_TYPE)Enum.Parse(typeof(DI_BT_TYPE), segmentdataItemDetail.Attribute("bt_Type").Value.ToString()),
                                                            isModel = (IS_MODEL_TYPE)Enum.Parse(typeof(IS_MODEL_TYPE), segmentdataItemDetail.Attribute("isModel").Value.ToString())
                                                        });
                        WriteProfiler("\t\t Generating code for Service_Segment Dataitem- " + segmentdataItemDetail.Attribute("dataItemName").Value.ToString());
                    }

                    serviceSegment.DataItem = serviceSegmentDataItem.ToArray();
                }

                return true;
            }
            catch (Exception e)
            {
                WriteProfiler("Exception in adding dataitem to segment", e.Message);
                return false;
            }
        }
    }
}