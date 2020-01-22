using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ramco.VwPlf.CodeGenerator.Callout
{
    internal struct DataType
    {
        public const string LONG = "LONG";
        public const string INT = "INT";
        public const string INTEGER = "INTEGER";
        public const string STRING = "STRING";
        public const string DATETIME = "DATETIME";
        public const string CHAR = "CHAR";
        public const string NVARCHAR = "NVARCHAR";
        public const string DATE = "DATE";
        public const string TIME = "TIME";
        public const string ENUMERATED = "ENUMERATED";
        public const string DATE_TIME = "DATE-TIME";
        public const string DOUBLE = "DOUBLE";
        public const string NUMERIC = "NUMERIC";
        public const string VARBINARY = "VARBINARY";
        public const string FILEBINARY = "FILEBINARY";
        public const string TIMESTAMP = "TIMESTAMP";
    }

    public class Segment
    {
        public String Name { get; set; }
        public String Inst { get; set; }
        public String FlowAttribute { get; set; }
        public String Sequence { get; set; }
        public String MandatoryFlg { get; set; }
        public String ParentSegName { get; set; }
        public String Process_sel_rows { get; set; }
        public String Process_upd_rows { get; set; }
        public String Process_sel_upd_rows { get; set; }
        public List<DataItem> DataItems { get; set; }
    }


    public class DataItem
    {
        public String Name { get; set; }
        public String FlowDirection { get; set; }
        public Boolean PartOfKey { get; set; }
        public String DataType { get; set; }
        public Boolean IsMandatory { get; set; }
        public String DefaultValue { get; set; }
    }
    public class DataStoreCallout
    {
        public string CustomerName { get; set; }
        public string ProjectName { get; set; }
        public string ProcessName { get; set; }
        public string EcrNO { get; set; }
        public string ComponentName { get; set; }
        public string ActivityName { get; set; }
        public string Ui { get; set; }
        public string Task { get; set; }
        public string CalloutName { get; set; }
        public IEnumerable<CalloutSegment> CalloutSegments { get; set; }
    }


    public class CalloutSegment : Segment
    {
        public String CalloutName { get; set; }
        public string Activity { get; set; }
        public string Ui { get; set; }
        public String TaskName { get; set; }
        public IEnumerable<CalloutDataItem> CalloutDataitems { get; set; }

    }

    public class CalloutDataItem : DataItem
    {
        public String Activity { get; set; }
        public String Ui { get; set; }
        public String CalloutName { get; set; }
        public String SegmentName { get; set; }
        public String TaskName { get; set; }
    }
}
