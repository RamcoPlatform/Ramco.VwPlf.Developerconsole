using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ramco.VwPlf.Generator
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Xml.Linq;
    using Ramco.Plf.Global.Interfaces;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using System.IO;

    public class ServiceSchemaJson : Common
    {
        /*
        public enum SpParameterType { Char, Integer, SpParameterTypeInteger };
        public enum DataType { Char, Integer };
        public enum PP_CompoundParam
        {
            ATMA_DI_COMPOUND_PARAM = 0,
            ATMA_DI_NOT_COMPOUND_PARAM = 1
        }

        public enum DI_DATA_TYPE
        {
            DATE = 0,
            TIME = 1,
            DATETIME = 2,
            DATETIME1 = 3,
            OTHERS = 4,
            STRING = 5,
            NUMERIC = 6,
            NVARCHAR = 7,
            INTEGER = 8,
            ENUMERATED = 9,
            CHAR = 10,
            VARCHAR = 11
        }

        public enum PP_FlowDirection
        {
            ATMA_IN_PARAM = 0,
            ATMA_OUT_PARAM = 1,
            ATMA_IO_PARAM = 2
        }
        */

        public bool SchemaHeader()
        {
            WriteProfiler("Service schemaHeader start.");

            try
            {
                if (!GlobalVar.DataCollection.ContainsKey("service"))
                {
                    return true;
                }

                var serviceDetails = from services in GlobalVar.DataCollection["service"]
                                     select services;

                foreach (XElement serviceDetail in serviceDetails)
                {
                    string serviceName = serviceDetail.Attribute("servicename").Value.ToString();
                    WriteProfiler("Starting to generate JSON for the service-" + serviceName);

                    ServiceJsonSchema serviceJsonSchema = new ServiceJsonSchema();
                    serviceJsonSchema.ServiceName = serviceName.ToLower();
                    serviceJsonSchema.Version = 1;
                    serviceJsonSchema.ComponentName = GlobalVar.Component.ToLower();
                    serviceJsonSchema.TransactionType = Convert.ToInt32(serviceDetail.Attribute("serviceType").Value.ToString());

                    this.AddProcessSection(serviceJsonSchema, serviceName);
                    this.AddServiceSegment(serviceJsonSchema, serviceName);

                    string fileName = string.Format(@"{0}\ServiceXML\{2}\{1}.json", GlobalVar.ReleasePath, serviceName, GlobalVar.Component);
                    string service = JsonConvert.SerializeObject(serviceJsonSchema, Formatting.Indented);
                    File.WriteAllText(fileName, service);

                    WriteProfiler("Generated JSON saved for the service - " + serviceName);
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

        private bool AddServiceSegment(ServiceJsonSchema serviceJsonSchema, string serviceName)
        {
            try
            {
                var segmentDetails = from data in GlobalVar.DataCollection["segment"]
                                     where data.Attribute("servicename").Value.Equals(serviceName)
                                     select data;

                List<SegmentJSON> serviceSegments = new List<SegmentJSON>();
                foreach (XElement segmentDetail in segmentDetails)
                {
                    String segmentName = segmentDetail.Attribute("segmentName").Value.ToString();

                    SegmentJSON serviceSegment = new SegmentJSON
                    {
                        SegmentName = segmentDetail.Attribute("segmentName").Value.ToString().ToLower(),
                        InstanceFlag = Convert.ToInt32(segmentDetail.Attribute("instanceFlag").Value.ToString()),
                        MandatoryFlag = Convert.ToInt32(segmentDetail.Attribute("mandatory").Value.ToString())
                    };


                    var segmentdataItemDetails = from data in GlobalVar.DataCollection["segmentdataItem"]
                                                 where data.Attribute("servicename").Value.Equals(serviceName)
                                                  && data.Attribute("segmentName").Value.Equals(segmentName)
                                                 select data;

                    List<DIJSON> serviceSegmentDataItems = new List<DIJSON>();
                    foreach (XElement segmentdataItemDetail in segmentdataItemDetails)
                    {
                        DIJSON serviceSegmentDataItem = new DIJSON
                        {
                            DataItemName = segmentdataItemDetail.Attribute("dataItemName").Value.ToString().ToLower(),
                            DefaultValue = segmentdataItemDetail.Attribute("defaultValue").Value.ToString(),
                            IsPartOfKey = Convert.ToUInt32(segmentdataItemDetail.Attribute("isPartOfKey").Value.ToString()),
                            MandatoryFlag = Convert.ToUInt32(segmentdataItemDetail.Attribute("mandatoryFlag").Value.ToString()),
                            FlowAttribute = Convert.ToUInt32(segmentdataItemDetail.Attribute("flowAttribute").Value.ToString()),
                            DataType = segmentdataItemDetail.Attribute("bt_Type").Value.ToString()
                        };
                        serviceSegmentDataItems.Add(serviceSegmentDataItem);
                    }

                    serviceSegment.DI = serviceSegmentDataItems.ToArray();
                    serviceSegments.Add(serviceSegment);
                }

                serviceJsonSchema.Segment = serviceSegments.ToArray();
                return true;
            }
            catch (Exception e)
            {
                WriteProfiler("AddServiceSegment::General exception in service segment", e.Message);
                return false;
            }
        }

        private bool AddProcessSection(ServiceJsonSchema serviceJsonSchema, string serviceName)
        {
            try
            {
                // Process Section
                var processSectionDetails = from processsections in GlobalVar.DataCollection["processsection"]
                                            where processsections.Attribute("servicename").Value.Equals(serviceName)
                                            select processsections;
                List<ProcessSection> processSections = new List<ProcessSection>();
                foreach (XElement processSectionDetail in processSectionDetails)
                {
                    string sectionName = processSectionDetail.Attribute("sectionName").Value.ToString();

                    ProcessSection processSection = new ProcessSection
                    {
                        Version = 1,
                        SequenceNo = Convert.ToInt32(processSectionDetail.Attribute("seqNo").Value.ToString()),
                        SectionName = processSectionDetail.Attribute("sectionName").Value.ToString().ToLower(),
                        ProcessingType = Convert.ToInt32(processSectionDetail.Attribute("sectionType").Value.ToString())
                    };                    
                    
                    // Business Rule
                    var bruleDetails = from brdetails in GlobalVar.DataCollection["brdetails"]
                                       where brdetails.Attribute("servicename").Value.Equals(serviceName)
                                         && brdetails.Attribute("sectionname").Value.Equals(sectionName)
                                       select brdetails;

                    List<BusinessRule> businessRules = new List<BusinessRule>();
                    foreach (XElement bruleDetail in bruleDetails)
                    {
                        Int32 BRSeqNo = Convert.ToInt32(bruleDetail.Attribute("seqno").Value.ToString());

                        BusinessRule businessRule = new BusinessRule
                        {
                            SequenceNo = Convert.ToInt32(bruleDetail.Attribute("seqno").Value.ToString()),
                            MethodName = bruleDetail.Attribute("systemgenerated").Value.ToString().Equals("0") ? String.Format("{0}Ex", bruleDetail.Attribute("methodname").Value.ToString()) : (bruleDetail.Attribute("systemgenerated").Value.ToString().Equals("3") ? String.Format("{0}Ex", bruleDetail.Attribute("methodname").Value.ToString()) : bruleDetail.Attribute("methodname").Value.ToString().ToLower()),
                            MethodVersionNo = 1,
                            StmtId = "null",
                            IntegComponentName = String.Empty,
                            IntegServiceName = object.ReferenceEquals(bruleDetail.Attribute("integservicename"), null).Equals(true) ? string.Empty : bruleDetail.Attribute("integservicename").Value.ToString().ToLower(),
                            IntegServiceVersionNo = 1,
                            Description = bruleDetail.Attribute("systemgenerated").Value.ToString().Equals("0") ? String.Format("{0}Ex", bruleDetail.Attribute("methodname").Value.ToString()) : (bruleDetail.Attribute("systemgenerated").Value.ToString().Equals("3") ? String.Format("{0}Ex", bruleDetail.Attribute("methodname").Value.ToString().ToLower()) : bruleDetail.Attribute("methodname").Value.ToString().ToLower()),
                            OperationType = Convert.ToInt32(bruleDetail.Attribute("operationtype").Value.ToString()),
                            SPErrorProtocol = (SPerror_Protocol)Enum.Parse(typeof(SPerror_Protocol), bruleDetail.Attribute("sperrorprotocol").Value.ToString(), true),
                            BROName = bruleDetail.Attribute("systemgenerated").Value.ToString().Equals("0") ? String.Format("{0}Ex", bruleDetail.Attribute("methodname").Value.ToString()) : (bruleDetail.Attribute("systemgenerated").Value.ToString().Equals("3") ? String.Format("{0}Ex", bruleDetail.Attribute("methodname").Value.ToString().ToLower()) : bruleDetail.Attribute("methodname").Value.ToString().ToLower()),
                            BROAccessDB = 1,
                            SPName = bruleDetail.Attribute("spname").Value.ToString().ToLower()
                        };

                        // Business Rule Parameters
                        var bruleParameters = from data in GlobalVar.DataCollection["processsectionbrpp"]
                                              where data.Attribute("servicename").Value.Equals(serviceName)
                                                 && data.Attribute("SectionName").Value.Equals(sectionName)
                                                 && data.Attribute("sequenceno").Value.Equals(BRSeqNo.ToString())
                                              select data;
                        
                        List<BRParameter> businessRuleParameters = new List<BRParameter>();
                        foreach (XElement bruleParameter in bruleParameters)
                        {
                            string ParameterName = bruleParameter.Attribute("physicalParameterName").Value.ToString().ToLower();
                            string DataSegmentName = bruleParameter.Attribute("dataSegmentName").Value.ToString().ToLower();
                            string DataItemName = bruleParameter.Attribute("dataItemName").Value.ToString().ToLower();
                            string RecordSetName = string.Empty;
                            Int32 RecordSetSeqNo = 0;

                            var businessRuleParametersLP = (from data in GlobalVar.DataCollection["processsectionbrlp"]
                                                            where data.Attribute("servicename").Value.Equals(serviceName)
                                                               && data.Attribute("SectionName").Value.Equals(sectionName)
                                                               && data.Attribute("sequenceno").Value.Equals(BRSeqNo.ToString())
                                                               && data.Attribute("dataSegmentName").Value.ToLower().Equals(DataSegmentName.ToString())
                                                               && data.Attribute("dataItemName").Value.ToLower().Equals(DataItemName.ToString())
                                                               && data.Attribute("logicalParameterName").Value.ToLower().Equals(ParameterName.ToString())
                                                            select data).FirstOrDefault();
                            
                            if (businessRuleParametersLP != null)
                            {
                                RecordSetSeqNo = Convert.ToInt32(businessRuleParametersLP.Attribute("seqNo").Value.ToString());
                            }

                            BRParameter businessRuleParameter = new BRParameter
                            {
                                ParameterName = bruleParameter.Attribute("physicalParameterName").Value.ToString().ToLower(),
                                SeqNo = Convert.ToInt32(bruleParameter.Attribute("seqNo").Value.ToString()),
                                IsCompoundParam = Convert.ToInt32(bruleParameter.Attribute("isCompoundParam").Value.ToString()),
                                FlowDirection = Convert.ToInt32(bruleParameter.Attribute("flowDirection").Value.ToString()),
                                DataSegmentName = bruleParameter.Attribute("dataSegmentName").Value.ToString().ToLower(),
                                DataItemName = bruleParameter.Attribute("dataItemName").Value.ToString().ToLower(),
                                SpParameterType = (PP_SPParameterType)Enum.Parse(typeof(PP_SPParameterType), bruleParameter.Attribute("spParameterType").Value.ToString(), true),
                                RecordSetName = (RecordSetSeqNo > 0) ? "RSET101" : string.Empty,
                                RecordSetSeqNo  = RecordSetSeqNo
                            };
                            businessRuleParameters.Add(businessRuleParameter);
                            WriteProfiler("\t\t Generating code for Service_Processsection_BR JSON physicalParameterName - " + bruleParameter.Attribute("physicalParameterName").Value.ToString());
                        }

                        businessRule.BRParameter = businessRuleParameters.ToArray();
                        businessRules.Add(businessRule);
                    }

                    processSection.BusinessRule = businessRules.ToArray();
                    processSections.Add(processSection);


                    WriteProfiler("\t Generating JSON for Service_Processsection - " + processSectionDetail.Attribute("sectionName").Value.ToString());
                }

                serviceJsonSchema.ProcessSection = processSections.ToArray();
                return true;
            }
            catch (Exception e)
            {
                WriteProfiler("Exception in adding Processsection to service", e.Message);
                return false;
            }
        }
    }

    public partial class ServiceJsonSchema
    {
        [JsonProperty("ServiceName")]
        public string ServiceName { get; set; }

        [JsonProperty("Version")]
        public long Version { get; set; }

        [JsonProperty("ComponentName")]
        public string ComponentName { get; set; }

        [JsonProperty("TransactionType")]
        public long TransactionType { get; set; }

        [JsonProperty("ProcessSection")]
        public ProcessSection[] ProcessSection { get; set; }

        [JsonProperty("Segment")]
        public SegmentJSON[] Segment { get; set; }
    }

    public partial class ProcessSection
    {
        [JsonProperty("Version")]
        public long Version { get; set; }

        [JsonProperty("SequenceNo")]
        public long SequenceNo { get; set; }

        [JsonProperty("SectionName")]
        public string SectionName { get; set; }

        [JsonProperty("ProcessingType")]
        public long ProcessingType { get; set; }

        [JsonProperty("BusinessRule")]
        public BusinessRule[] BusinessRule { get; set; }
    }

    public partial class BusinessRule
    {
        [JsonProperty("SequenceNo")]
        //[JsonConverter(typeof(ParseStringConverter))]
        public long SequenceNo { get; set; }

        [JsonProperty("MethodName")]
        public string MethodName { get; set; }

        [JsonProperty("MethodVersionNo")]
        public object MethodVersionNo { get; set; }

        [JsonProperty("StmtID")]
        public object StmtId { get; set; }

        [JsonProperty("IntegComponentName")]
        public object IntegComponentName { get; set; }

        [JsonProperty("IntegServiceName")]
        public object IntegServiceName { get; set; }

        [JsonProperty("IntegServiceVersionNo")]
        public object IntegServiceVersionNo { get; set; }

        [JsonProperty("Description")]
        public string Description { get; set; }

        [JsonProperty("OperationType")]
        //[JsonConverter(typeof(ParseStringConverter))]
        public long OperationType { get; set; }

        [JsonProperty("SPErrorProtocol")]
        //[JsonConverter(typeof(ParseStringConverter))]
        public SPerror_Protocol SPErrorProtocol { get; set; }

        [JsonProperty("BROName")]
        public object BROName { get; set; }

        [JsonProperty("BROAccessDB")]
        //[JsonConverter(typeof(ParseStringConverter))]
        public long BROAccessDB { get; set; }

        [JsonProperty("SPName")]
        public string SPName { get; set; }

        [JsonProperty("BRParameter")]
        public BRParameter[] BRParameter { get; set; }
    }

    public partial class BRParameter
    {
        [JsonProperty("ParameterName")]
        public string ParameterName { get; set; }

        [JsonProperty("seqNo")]
        public long SeqNo { get; set; }

        [JsonProperty("isCompoundParam")]
        //[JsonConverter(typeof(ParseStringConverter))]
        public long IsCompoundParam { get; set; }

        [JsonProperty("flowDirection")]
        //[JsonConverter(typeof(ParseStringConverter))]
        public long FlowDirection { get; set; }

        [JsonProperty("dataSegmentName")]
        public string DataSegmentName { get; set; }

        [JsonProperty("dataItemName")]
        public string DataItemName { get; set; }

        [JsonProperty("spParameterType")]
        public PP_SPParameterType SpParameterType { get; set; }

        [JsonProperty("RecordSetName")]
        public string RecordSetName { get; set; }

        [JsonProperty("RecordSetSeqNo")]
        public long RecordSetSeqNo { get; set; }
    }

    
    public partial class SegmentJSON
    {
        [JsonProperty("SegmentName")]
        public string SegmentName { get; set; }

        [JsonProperty("InstanceFlag")]
        //[JsonConverter(typeof(ParseStringConverter))]
        public long InstanceFlag { get; set; }

        [JsonProperty("MandatoryFlag")]
        //[JsonConverter(typeof(ParseStringConverter))]
        public long MandatoryFlag { get; set; }

        [JsonProperty("DI")]
        public DIJSON[] DI { get; set; }
    }

    public partial class DIJSON
    {
        [JsonProperty("DataItemName")]
        public string DataItemName { get; set; }

        [JsonProperty("DefaultValue")]
        public string DefaultValue { get; set; }

        [JsonProperty("IsPartOfKey")]
        //[JsonConverter(typeof(ParseStringConverter))]
        public long IsPartOfKey { get; set; }

        [JsonProperty("MandatoryFlag")]
        //[JsonConverter(typeof(ParseStringConverter))]
        public long MandatoryFlag { get; set; }

        [JsonProperty("FlowAttribute")]
        public long FlowAttribute { get; set; }

        [JsonProperty("DataType")]
        public string DataType { get; set; }
    }    
}
