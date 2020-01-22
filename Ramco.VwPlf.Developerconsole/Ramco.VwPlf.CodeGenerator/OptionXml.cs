using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Ramco.VwPlf.CodeGenerator
{

    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, TypeName = "codegeneration")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "", IsNullable = false)]
    public partial class codegeneration
    {

        private Model _model;

        private Language[] _languages;

        private Configxmls _configxmls;

        private Scripts _scripts;

        private Option[] _options;

        private Servicee _service;

        private Activityy[] _activities;

        private Htm[] _htmls;

        /// <remarks/>
        public Model model
        {
            get
            {
                return this._model;
            }
            set
            {
                this._model = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayItemAttribute("language", IsNullable = false)]
        public Language[] languages
        {
            get
            {
                return this._languages;
            }
            set
            {
                this._languages = value;
            }
        }

        /// <remarks/>
        public Configxmls configxmls
        {
            get
            {
                return this._configxmls;
            }
            set
            {
                this._configxmls = value;
            }
        }

        /// <remarks/>
        public Scripts scripts
        {
            get
            {
                return this._scripts;
            }
            set
            {
                this._scripts = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayItemAttribute("option", IsNullable = false)]
        public Option[] options
        {
            get
            {
                return this._options;
            }
            set
            {
                this._options = value;
            }
        }

        /// <remarks/>
        public Servicee service
        {
            get
            {
                return this._service;
            }
            set
            {
                this._service = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayItemAttribute("activity", IsNullable = false)]
        public Activityy[] activities
        {
            get
            {
                return this._activities;
            }
            set
            {
                this._activities = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayItemAttribute("htm", IsNullable = false)]
        public Htm[] htmls
        {
            get
            {
                return this._htmls;
            }
            set
            {
                this._htmls = value;
            }
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class Model
    {

        private string _customer;

        private string _project;

        private string _ecrno;

        private string _process;

        private string _component;

        private string _componentdesc;

        private string _appliation_rm_type;

        private string _previousgenerationpath;

        private string _generationpath;

        private string _platform;

        private string _deploydeliverables;

        private string _requestid;

        private string _codegenclient;

        private string _requeststart_datetime;

        private string _guid;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string customer
        {
            get
            {
                return this._customer;
            }
            set
            {
                this._customer = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string project
        {
            get
            {
                return this._project;
            }
            set
            {
                this._project = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string ecrno
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

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string process
        {
            get
            {
                return this._process;
            }
            set
            {
                this._process = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string component
        {
            get
            {
                return this._component;
            }
            set
            {
                this._component = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string componentdesc
        {
            get
            {
                return this._componentdesc;
            }
            set
            {
                this._componentdesc = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string appliation_rm_type
        {
            get
            {
                return this._appliation_rm_type;
            }
            set
            {
                this._appliation_rm_type = value;
            }
        }

        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string previousgenerationpath
        {
            get
            {
                return this._previousgenerationpath;
            }
            set
            {
                this._previousgenerationpath = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string generationpath
        {
            get
            {
                return this._generationpath;
            }
            set
            {
                this._generationpath = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string platform
        {
            get
            {
                return this._platform;
            }
            set
            {
                this._platform = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string deploydeliverables
        {
            get
            {
                return this._deploydeliverables;
            }
            set
            {
                this._deploydeliverables = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string requestid
        {
            get
            {
                return this._requestid;
            }
            set
            {
                this._requestid = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string codegenclient
        {
            get
            {
                return this._codegenclient;
            }
            set
            {
                this._codegenclient = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string requeststart_datetime
        {
            get
            {
                return this._requeststart_datetime;
            }
            set
            {
                this._requeststart_datetime = value;
            }
        }

        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string guid
        {
            get
            {
                return this._guid;
            }
            set
            {
                this._guid = value;
            }
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(TypeName = "Language")]
    public partial class Language
    {

        private string _id;

        private string _desc;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string id
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

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string desc
        {
            get
            {
                return this._desc;
            }
            set
            {
                this._desc = value;
            }
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class Configxmls
    {

        private string _chart;

        private string _state;

        private string _pivot;

        private string _ddt;

        private string _cexml;

        private string _errorlookup;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string chart
        {
            get
            {
                return this._chart;
            }
            set
            {
                this._chart = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string state
        {
            get
            {
                return this._state;
            }
            set
            {
                this._state = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string pivot
        {
            get
            {
                return this._pivot;
            }
            set
            {
                this._pivot = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string ddt
        {
            get
            {
                return this._ddt;
            }
            set
            {
                this._ddt = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string cexml
        {
            get
            {
                return this._cexml;
            }
            set
            {
                this._cexml = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string errorlookup
        {
            get
            {
                return this._errorlookup;
            }
            set
            {
                this._errorlookup = value;
            }
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class Scripts
    {

        private string _edksscript;

        private string _depscript;

        private long _activityoffset;

        private string _wflowscript;

        private long _wflowoffset;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string edksscript
        {
            get
            {
                return this._edksscript;
            }
            set
            {
                this._edksscript = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string depscript
        {
            get
            {
                return this._depscript;
            }
            set
            {
                this._depscript = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public long activityoffset
        {
            get
            {
                return this._activityoffset;
            }
            set
            {
                this._activityoffset = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string wflowscript
        {
            get
            {
                return this._wflowscript;
            }
            set
            {
                this._wflowscript = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public long wflowoffset
        {
            get
            {
                return this._wflowoffset;
            }
            set
            {
                this._wflowoffset = value;
            }
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class Option
    {

        private string _rtgif;

        private string _selallforgridcheckbox;

        private string _contextmenu;

        private string _allstyle;

        private string _extjs;

        private string _compresshtml;

        private string _compressjs;

        private string _alltaskdata;

        private string _cellspacing;

        private string _generatexml;

        private string _gridfilter;

        private string _labelselect;

        private string _split;

        private string _ellipses;

        private string _deviceconfigpath;

        private string _iphone;

        private string _secondarylinks;

        private string _extjs6;

        private string _mhub2;

        private string _multiplettx;

        private string _reportformat;

        private string _desktopdlv;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string rtgif
        {
            get
            {
                return this._rtgif;
            }
            set
            {
                this._rtgif = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string selallforgridcheckbox
        {
            get
            {
                return this._selallforgridcheckbox;
            }
            set
            {
                this._selallforgridcheckbox = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string contextmenu
        {
            get
            {
                return this._contextmenu;
            }
            set
            {
                this._contextmenu = value;
            }
        }

        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string allstyle
        {
            get
            {
                return this._allstyle;
            }
            set
            {
                this._allstyle = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string extjs
        {
            get
            {
                return this._extjs;
            }
            set
            {
                this._extjs = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string compresshtml
        {
            get
            {
                return this._compresshtml;
            }
            set
            {
                this._compresshtml = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string compressjs
        {
            get
            {
                return this._compressjs;
            }
            set
            {
                this._compressjs = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string alltaskdata
        {
            get
            {
                return this._alltaskdata;
            }
            set
            {
                this._alltaskdata = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string cellspacing
        {
            get
            {
                return this._cellspacing;
            }
            set
            {
                this._cellspacing = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string generatexml
        {
            get
            {
                return this.GeneratexmlField;
            }
            set
            {
                this.GeneratexmlField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string gridfilter
        {
            get
            {
                return this._gridfilter;
            }
            set
            {
                this._gridfilter = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string labelselect
        {
            get
            {
                return this._labelselect;
            }
            set
            {
                this._labelselect = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string split
        {
            get
            {
                return this._split;
            }
            set
            {
                this._split = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string ellipses
        {
            get
            {
                return this._ellipses;
            }
            set
            {
                this._ellipses = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string deviceconfigpath
        {
            get
            {
                return this._deviceconfigpath;
            }
            set
            {
                this._deviceconfigpath = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string iphone
        {
            get
            {
                return this._iphone;
            }
            set
            {
                this._iphone = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string secondarylinks
        {
            get
            {
                return this._secondarylinks;
            }
            set
            {
                this._secondarylinks = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string extjs6
        {
            get
            {
                return this._extjs6;
            }
            set
            {
                this._extjs6 = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string mhub2
        {
            get
            {
                return this._mhub2;
            }
            set
            {
                this._mhub2 = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string multiplettx
        {
            get
            {
                return this._multiplettx;
            }
            set
            {
                this._multiplettx = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string reportformat
        {
            get
            {
                return this._reportformat;
            }
            set
            {
                this._reportformat = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string desktopdlv
        {
            get
            {
                return this._desktopdlv;
            }
            set
            {
                this._desktopdlv = value;
            }
        }

        public string GeneratexmlField
        {
            get
            {
                return _generatexml;
            }

            set
            {
                _generatexml = value;
            }
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(TypeName = "Service")]
    public partial class Servicee
    {

        private string _dll;

        private string _error;

        private string _intd;

        private string _defaultasnull;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string dll
        {
            get
            {
                return this._dll;
            }
            set
            {
                this._dll = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string error
        {
            get
            {
                return this._error;
            }
            set
            {
                this._error = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string intd
        {
            get
            {
                return this._intd;
            }
            set
            {
                this._intd = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string defaultasnull
        {
            get
            {
                return this._defaultasnull;
            }
            set
            {
                this._defaultasnull = value;
            }
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(TypeName = "Activity")]
    public partial class Activityy
    {

        private string _name;

        private string _dll;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string name
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

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string dll
        {
            get
            {
                return this._dll;
            }
            set
            {
                this._dll = value;
            }
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class Htm
    {

        private string _activityname;

        private string _activitydesc;

        private string _uiname;

        private string _uidesc;

        private string _html;

        private string _aspx;

        private string _isglanceui;

        private string _isnativeui;


        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string activityname
        {
            get
            {
                return this._activityname;
            }
            set
            {
                this._activityname = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string activitydesc
        {
            get
            {
                return this._activitydesc;
            }
            set
            {
                this._activitydesc = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string uiname
        {
            get
            {
                return this._uiname;
            }
            set
            {
                this._uiname = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string uidesc
        {
            get
            {
                return this._uidesc;
            }
            set
            {
                this._uidesc = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string html
        {
            get
            {
                return this._html;
            }
            set
            {
                this._html = value;
            }
        }


        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string aspx
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

        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string isglanceui
        {
            get
            {
                return this._isglanceui;
            }
            set
            {
                this._isglanceui = value;
            }
        }

        public string isnativeui
        {
            get
            {
                return this._isnativeui;
            }
            set
            {
                this._isnativeui = value;
            }
        }

        public bool IsReportAspxGenerated { get; set; }
        public string LayoutXmlFile { get; set; }
    }


}
