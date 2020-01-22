using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ramco.VwPlf.CodeGenerator.DeserializingSchema
{
    

    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    [System.Xml.Serialization.XmlRoot("Services")]
    public class Services
    {

        private List<Service> serviceField;

        [System.Xml.Serialization.XmlElement("Service")]
        public List<Service> Service
        {
            get
            {
                return this.serviceField;
            }
            set
            {
                this.serviceField = value;
            }
        }

        public Services()
        {
            serviceField = new List<Service>();
        }

        public void Add(Service item)
        {
            this.serviceField.Add(item);
        }
    }

    // NOTE: Generated code may require at least .NET Framework 4.5 or .NET Core/Standard 2.0.
    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "", IsNullable = false)]
    public partial class Service
    {

        private ServiceSegment[] segmentField;

        private ServiceProcessSection[] processSectionField;

        private string nameField;

        private byte typeField;

        private byte isintegserField;

        private byte iszippedField;

        private byte iscachedField;

        private string componentnameField;

        private byte isselectedField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("Segment")]
        public ServiceSegment[] Segment
        {
            get
            {
                return this.segmentField;
            }
            set
            {
                this.segmentField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("ProcessSection")]
        public ServiceProcessSection[] ProcessSection
        {
            get
            {
                return this.processSectionField;
            }
            set
            {
                this.processSectionField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string name
        {
            get
            {
                return this.nameField;
            }
            set
            {
                this.nameField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public byte type
        {
            get
            {
                return this.typeField;
            }
            set
            {
                this.typeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public byte isintegser
        {
            get
            {
                return this.isintegserField;
            }
            set
            {
                this.isintegserField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public byte iszipped
        {
            get
            {
                return this.iszippedField;
            }
            set
            {
                this.iszippedField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public byte iscached
        {
            get
            {
                return this.iscachedField;
            }
            set
            {
                this.iscachedField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string componentname
        {
            get
            {
                return this.componentnameField;
            }
            set
            {
                this.componentnameField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public byte isselected
        {
            get
            {
                return this.isselectedField;
            }
            set
            {
                this.isselectedField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class ServiceSegment
    {

        private ServiceSegmentDataItem[] dataItemField;

        private ServiceSegmentOutDataItem[] outDataItemField;

        private string nameField;

        private byte sequenceField;

        private byte instanceflagField;

        private byte mandatoryflagField;

        private string servicenameField;

        private string process_selrowsField;

        private string process_updrowsField;

        private string process_selupdrowsField;

        private byte flowattributeField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("DataItem")]
        public ServiceSegmentDataItem[] DataItem
        {
            get
            {
                return this.dataItemField;
            }
            set
            {
                this.dataItemField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("OutDataItem")]
        public ServiceSegmentOutDataItem[] OutDataItem
        {
            get
            {
                return this.outDataItemField;
            }
            set
            {
                this.outDataItemField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string name
        {
            get
            {
                return this.nameField;
            }
            set
            {
                this.nameField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public byte sequence
        {
            get
            {
                return this.sequenceField;
            }
            set
            {
                this.sequenceField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public byte instanceflag
        {
            get
            {
                return this.instanceflagField;
            }
            set
            {
                this.instanceflagField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public byte mandatoryflag
        {
            get
            {
                return this.mandatoryflagField;
            }
            set
            {
                this.mandatoryflagField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string servicename
        {
            get
            {
                return this.servicenameField;
            }
            set
            {
                this.servicenameField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string process_selrows
        {
            get
            {
                return this.process_selrowsField;
            }
            set
            {
                this.process_selrowsField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string process_updrows
        {
            get
            {
                return this.process_updrowsField;
            }
            set
            {
                this.process_updrowsField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string process_selupdrows
        {
            get
            {
                return this.process_selupdrowsField;
            }
            set
            {
                this.process_selupdrowsField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public byte flowattribute
        {
            get
            {
                return this.flowattributeField;
            }
            set
            {
                this.flowattributeField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class ServiceSegmentDataItem
    {

        private string nameField;

        private string typeField;

        private ushort datalengthField;

        private byte ispartofkeyField;

        private byte flowattributeField;

        private byte mandatoryflagField;

        private string defaultvalueField;

        private string servicenameField;

        private string segmentnameField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string name
        {
            get
            {
                return this.nameField;
            }
            set
            {
                this.nameField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string type
        {
            get
            {
                return this.typeField;
            }
            set
            {
                this.typeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public ushort datalength
        {
            get
            {
                return this.datalengthField;
            }
            set
            {
                this.datalengthField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public byte ispartofkey
        {
            get
            {
                return this.ispartofkeyField;
            }
            set
            {
                this.ispartofkeyField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public byte flowattribute
        {
            get
            {
                return this.flowattributeField;
            }
            set
            {
                this.flowattributeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public byte mandatoryflag
        {
            get
            {
                return this.mandatoryflagField;
            }
            set
            {
                this.mandatoryflagField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string defaultvalue
        {
            get
            {
                return this.defaultvalueField;
            }
            set
            {
                this.defaultvalueField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string servicename
        {
            get
            {
                return this.servicenameField;
            }
            set
            {
                this.servicenameField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string segmentname
        {
            get
            {
                return this.segmentnameField;
            }
            set
            {
                this.segmentnameField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class ServiceSegmentOutDataItem
    {

        private string nameField;

        private string typeField;

        private ushort datalengthField;

        private byte ispartofkeyField;

        private byte flowattributeField;

        private byte mandatoryflagField;

        private string defaultvalueField;

        private string servicenameField;

        private string segmentnameField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string name
        {
            get
            {
                return this.nameField;
            }
            set
            {
                this.nameField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string type
        {
            get
            {
                return this.typeField;
            }
            set
            {
                this.typeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public ushort datalength
        {
            get
            {
                return this.datalengthField;
            }
            set
            {
                this.datalengthField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public byte ispartofkey
        {
            get
            {
                return this.ispartofkeyField;
            }
            set
            {
                this.ispartofkeyField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public byte flowattribute
        {
            get
            {
                return this.flowattributeField;
            }
            set
            {
                this.flowattributeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public byte mandatoryflag
        {
            get
            {
                return this.mandatoryflagField;
            }
            set
            {
                this.mandatoryflagField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string defaultvalue
        {
            get
            {
                return this.defaultvalueField;
            }
            set
            {
                this.defaultvalueField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string servicename
        {
            get
            {
                return this.servicenameField;
            }
            set
            {
                this.servicenameField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string segmentname
        {
            get
            {
                return this.segmentnameField;
            }
            set
            {
                this.segmentnameField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class ServiceProcessSection
    {

        private ServiceProcessSectionMethod[] methodField;

        private string nameField;

        private byte typeField;

        private byte seqnoField;

        private string controlexpressionField;

        private byte loopingstyleField;

        private string servicenameField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("Method")]
        public ServiceProcessSectionMethod[] Method
        {
            get
            {
                return this.methodField;
            }
            set
            {
                this.methodField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string name
        {
            get
            {
                return this.nameField;
            }
            set
            {
                this.nameField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public byte type
        {
            get
            {
                return this.typeField;
            }
            set
            {
                this.typeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public byte seqno
        {
            get
            {
                return this.seqnoField;
            }
            set
            {
                this.seqnoField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string controlexpression
        {
            get
            {
                return this.controlexpressionField;
            }
            set
            {
                this.controlexpressionField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public byte loopingstyle
        {
            get
            {
                return this.loopingstyleField;
            }
            set
            {
                this.loopingstyleField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string servicename
        {
            get
            {
                return this.servicenameField;
            }
            set
            {
                this.servicenameField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class ServiceProcessSectionMethod
    {

        private ServiceProcessSectionMethodISMappingDetails[] iSMappingDetailsField;

        private ServiceProcessSectionMethodPhysicalParameters[] physicalParametersField;

        private uint idField;

        private string nameField;

        private byte seqnoField;

        private byte ismethodField;

        private string spnameField;

        private string servicenameField;

        private string integservicenameField;

        private string controlexpressionField;

        private string sectionnameField;

        private string accessesdatabaseField;

        private string operationtypeField;

        private string systemgeneratedField;

        private string sperrorprotocolField;

        private string loopcausingsegmentField;

        private string method_exec_contField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("ISMappingDetails")]
        public ServiceProcessSectionMethodISMappingDetails[] ISMappingDetails
        {
            get
            {
                return this.iSMappingDetailsField;
            }
            set
            {
                this.iSMappingDetailsField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("PhysicalParameters")]
        public ServiceProcessSectionMethodPhysicalParameters[] PhysicalParameters
        {
            get
            {
                return this.physicalParametersField;
            }
            set
            {
                this.physicalParametersField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public uint id
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
        public string name
        {
            get
            {
                return this.nameField;
            }
            set
            {
                this.nameField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public byte seqno
        {
            get
            {
                return this.seqnoField;
            }
            set
            {
                this.seqnoField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public byte ismethod
        {
            get
            {
                return this.ismethodField;
            }
            set
            {
                this.ismethodField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string spname
        {
            get
            {
                return this.spnameField;
            }
            set
            {
                this.spnameField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string servicename
        {
            get
            {
                return this.servicenameField;
            }
            set
            {
                this.servicenameField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string integservicename
        {
            get
            {
                return this.integservicenameField;
            }
            set
            {
                this.integservicenameField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string controlexpression
        {
            get
            {
                return this.controlexpressionField;
            }
            set
            {
                this.controlexpressionField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string sectionname
        {
            get
            {
                return this.sectionnameField;
            }
            set
            {
                this.sectionnameField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string accessesdatabase
        {
            get
            {
                return this.accessesdatabaseField;
            }
            set
            {
                this.accessesdatabaseField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string operationtype
        {
            get
            {
                return this.operationtypeField;
            }
            set
            {
                this.operationtypeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string systemgenerated
        {
            get
            {
                return this.systemgeneratedField;
            }
            set
            {
                this.systemgeneratedField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string sperrorprotocol
        {
            get
            {
                return this.sperrorprotocolField;
            }
            set
            {
                this.sperrorprotocolField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string loopcausingsegment
        {
            get
            {
                return this.loopcausingsegmentField;
            }
            set
            {
                this.loopcausingsegmentField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string method_exec_cont
        {
            get
            {
                return this.method_exec_contField;
            }
            set
            {
                this.method_exec_contField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class ServiceProcessSectionMethodISMappingDetails
    {

        private string issegnameField;

        private string isdinameField;

        private string callersegnameField;

        private string callerdinameField;

        private string integservicenameField;

        private byte flowattributeField;

        private string defaultvalueField;

        private string ispartofkeyField;

        private string callingservicenameField;

        private string servicenameField;

        private string icomponentnameField;

        private string sectionnameField;

        private byte seqnoField;

        private byte callsegmentinstField;

        private byte calldiflowField;

        private byte issegmentinstField;

        private byte iser_pr_typeField;

        private byte is_servicetypeField;

        private byte is_isintegserField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string issegname
        {
            get
            {
                return this.issegnameField;
            }
            set
            {
                this.issegnameField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string isdiname
        {
            get
            {
                return this.isdinameField;
            }
            set
            {
                this.isdinameField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string callersegname
        {
            get
            {
                return this.callersegnameField;
            }
            set
            {
                this.callersegnameField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string callerdiname
        {
            get
            {
                return this.callerdinameField;
            }
            set
            {
                this.callerdinameField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string integservicename
        {
            get
            {
                return this.integservicenameField;
            }
            set
            {
                this.integservicenameField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public byte flowattribute
        {
            get
            {
                return this.flowattributeField;
            }
            set
            {
                this.flowattributeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string defaultvalue
        {
            get
            {
                return this.defaultvalueField;
            }
            set
            {
                this.defaultvalueField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string ispartofkey
        {
            get
            {
                return this.ispartofkeyField;
            }
            set
            {
                this.ispartofkeyField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string callingservicename
        {
            get
            {
                return this.callingservicenameField;
            }
            set
            {
                this.callingservicenameField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string servicename
        {
            get
            {
                return this.servicenameField;
            }
            set
            {
                this.servicenameField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string icomponentname
        {
            get
            {
                return this.icomponentnameField;
            }
            set
            {
                this.icomponentnameField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string sectionname
        {
            get
            {
                return this.sectionnameField;
            }
            set
            {
                this.sectionnameField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public byte seqno
        {
            get
            {
                return this.seqnoField;
            }
            set
            {
                this.seqnoField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public byte callsegmentinst
        {
            get
            {
                return this.callsegmentinstField;
            }
            set
            {
                this.callsegmentinstField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public byte calldiflow
        {
            get
            {
                return this.calldiflowField;
            }
            set
            {
                this.calldiflowField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public byte issegmentinst
        {
            get
            {
                return this.issegmentinstField;
            }
            set
            {
                this.issegmentinstField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public byte iser_pr_type
        {
            get
            {
                return this.iser_pr_typeField;
            }
            set
            {
                this.iser_pr_typeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public byte is_servicetype
        {
            get
            {
                return this.is_servicetypeField;
            }
            set
            {
                this.is_servicetypeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public byte is_isintegser
        {
            get
            {
                return this.is_isintegserField;
            }
            set
            {
                this.is_isintegserField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class ServiceProcessSectionMethodPhysicalParameters
    {

        private string physicalparameternameField;

        private string recordsetnameField;

        private byte seqnoField;

        private byte flowdirectionField;

        private string datasegmentnameField;

        private string dataitemnameField;

        private uint methodidField;

        private byte sequencenoField;

        private string servicenameField;

        private string sectionnameField;

        private string brparamtypeField;

        private ushort lengthField;

        private string paramtypeField;

        private string decimallengthField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string physicalparametername
        {
            get
            {
                return this.physicalparameternameField;
            }
            set
            {
                this.physicalparameternameField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string recordsetname
        {
            get
            {
                return this.recordsetnameField;
            }
            set
            {
                this.recordsetnameField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public byte seqno
        {
            get
            {
                return this.seqnoField;
            }
            set
            {
                this.seqnoField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public byte flowdirection
        {
            get
            {
                return this.flowdirectionField;
            }
            set
            {
                this.flowdirectionField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string datasegmentname
        {
            get
            {
                return this.datasegmentnameField;
            }
            set
            {
                this.datasegmentnameField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string dataitemname
        {
            get
            {
                return this.dataitemnameField;
            }
            set
            {
                this.dataitemnameField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public uint methodid
        {
            get
            {
                return this.methodidField;
            }
            set
            {
                this.methodidField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public byte sequenceno
        {
            get
            {
                return this.sequencenoField;
            }
            set
            {
                this.sequencenoField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string servicename
        {
            get
            {
                return this.servicenameField;
            }
            set
            {
                this.servicenameField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string sectionname
        {
            get
            {
                return this.sectionnameField;
            }
            set
            {
                this.sectionnameField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string brparamtype
        {
            get
            {
                return this.brparamtypeField;
            }
            set
            {
                this.brparamtypeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public ushort length
        {
            get
            {
                return this.lengthField;
            }
            set
            {
                this.lengthField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string paramtype
        {
            get
            {
                return this.paramtypeField;
            }
            set
            {
                this.paramtypeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string decimallength
        {
            get
            {
                return this.decimallengthField;
            }
            set
            {
                this.decimallengthField = value;
            }
        }
    }


}
