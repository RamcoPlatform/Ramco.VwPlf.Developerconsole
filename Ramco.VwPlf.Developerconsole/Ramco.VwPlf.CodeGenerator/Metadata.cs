using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.Data;
//using System.Data.SqlClient;
//using System.Reflection;
//using System.Text;
using System.Linq;
using System.Collections;


namespace Ramco.VwPlf.CodeGenerator
{
    [XmlRoot("Generation")]
    public class Generation
    {
        public Generation()
        {
            htms = new List<Htm>();
            activities = new List<Activity>();
            services = new List<Service>();
            resources = new List<Resource>();
        }

        [XmlAttribute("StartTime")]
        public string StartTime { get; set; }

        [XmlAttribute("EndTime")]
        public string EndTime { get; set; }

        [XmlAttribute("ServiceDllSourcePath")]
        public string ServicedllSrcPath { get; set; }

        [XmlAttribute("ServiceDllReleasePath")]
        public string ServicedllRelPath { get; set; }

        public string Template0SrcPath { get; set; }
        public string Template0RelPath { get; set; }

        public string Template1SrcPath { get; set; }
        public string Template1RelPath { get; set; }

        public string TemplateLtmSrcPath { get; set; }
        public string TemplateLtmRelPath { get; set; }

        public string TemplateLtmcrSrcPath { get; set; }
        public string TemplateLtmcrRelPath { get; set; }

        public string TemplateCrSrcPath { get; set; }
        public string TemplateCrRelPath { get; set; }

        public string TemplateRbSrcPath { get; set; }
        public string TemplateRbRelPath { get; set; }
        public string BRDll { get; set; }
        public string BulkDll { get; set; }

        [XmlAttribute("ServiceCompilationStatus")]
        public GenerationStatus ServiceCompilationStatus { get; set; }

        [XmlAttribute("ResourceDllReleasePath")]
        public string ResourcedllRelPath { get; set; }

        [XmlAttribute("ResourceCompilationStatus")]
        public GenerationStatus ResourceCompilationStatus { get; set; }

        //[XmlAttribute("ErrorHandlerSrcPath")]
        //public string ErrHdlrSrcPath { get; set; }

        //[XmlAttribute("ErrorHandlerRelPath")]
        //public string ErrHdlrRelPath { get; set; }

        [XmlArray("Activities")]
        [XmlArrayItem("Activity")]
        public List<Activity> activities { get; set; }

        public List<Htm> htms { get; set; }

        [XmlArray("Services")]
        [XmlArrayItem("Service")]
        public List<Service> services { get; set; }

        [XmlArray("Resources")]
        [XmlArrayItem("Resource")]
        public List<Resource> resources { get; set; }

        [XmlElement("ErrorHandler")]
        public ErrorHandler errorHandler { get; set; }

        //[XmlIgnore]
        //private DataSet dsServiceInfo;

        [XmlIgnore]
        public DataSet ServiceInfo { get; set; }
        /// <summary>
        /// 0-Activities 1-ILBO  2-TabPage 3-Control 4-Enumeration 5-ControlProperty
        /// 6-TaskService 7-ReportInfo 8-EzeeView 9-Tree 10-TreeInfo 11-Chart
        /// 12-Publication 13-Traversal 14-Subscription
        /// </summary>
        [XmlIgnore]
        public DataSet ActivityInfo { get; set; }

        [XmlIgnore]
        public DataSet dsResourceInfo { get; set; }

    }

    //New
    public class Activity
    {
        public Activity(string sName)
        {
            this.Name = sName;
            this._ILBOs = new List<ILBO>();
            this._AdditionalReferenceDlls = new List<string>();
        }

        public Activity()
        {
            this._ILBOs = new List<ILBO>();
            this._AdditionalReferenceDlls = new List<string>();
        }

        public int Id { get; set; }
        public string Name { get; set; }
        public bool HasBaseCallout { get; set; }
        public bool IsBaseCallout { get; set; }
        public string Desc { get; set; }
        public string SourceDirectory { get; set; }
        public string TargetDirectory { get; set; }

        private List<ILBO> _ILBOs;
        public List<string> _AdditionalReferenceDlls;

        public List<ILBO> ILBOs
        {
            get
            {
                return this._ILBOs;
            }
            set
            {
                this._ILBOs = value;
            }
        }
        public List<string> AdditionalReferenceDlls
        {
            get
            {
                return this._AdditionalReferenceDlls;
            }
            set
            {
                this._AdditionalReferenceDlls = value;
            }
        }

        public void FillData(ECRLevelOptions ecrOptions)
        {
            try
            {
                DataRow drActivity = null;
                try
                {
                    drActivity = (from row in ecrOptions.generation.ActivityInfo.Tables["Activities"].AsEnumerable()
                                  where string.Compare(row["activityname"].ToString(), Name, true) == 0
                                  select row).First();
                }
                catch
                {
                    //Invalid Activity
                    return;
                }

                Id = int.Parse(drActivity["activityid"].ToString());
                Name = Convert.ToString(drActivity["activityname"]);
                Desc = Convert.ToString(drActivity["activitydesc"]);
                SourceDirectory = System.IO.Path.Combine(ecrOptions.GenerationPath, ecrOptions.Platform, ecrOptions.Customer, ecrOptions.Project, ecrOptions.Ecrno, "Updated", ecrOptions.Component, "Source", "ILBO", Name);
                TargetDirectory = System.IO.Path.Combine(ecrOptions.GenerationPath, ecrOptions.Platform, ecrOptions.Customer, ecrOptions.Project, ecrOptions.Ecrno, "Updated", ecrOptions.Component, "Release", "ILBO");

                drActivity.GetChildRows("R_Activity_Ilbo").ToList<DataRow>().ForEach(drIlbo =>
                {
                    ILBO ilbo = new ILBO();
                    ilbo.FillData(drIlbo, ecrOptions);
                    ILBOs.Add(ilbo);
                });

                if (this.ILBOs.Where(ui => ui.HasBaseCallout == true).Any())
                {
                    this.HasBaseCallout = true;
                    this.AdditionalReferenceDlls.Add(System.IO.Path.Combine(ecrOptions.GenerationPath, ecrOptions.Platform, ecrOptions.Customer, ecrOptions.Project, ecrOptions.Ecrno, "Updated", ecrOptions.Component, "Release", "ILBO", string.Format("{0}__.dll", Name)));
                }
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Activity.FillData()->{0}", ex.InnerException != null ? ex.InnerException.Message : ex.Message));
            }
        }
    }
    public class ILBO
    {
        public string Code { get; set; }
        public string Desc { get; set; }
        public string Type { get; set; }
        public bool HasStateCtrl { get; set; }
        public bool HasLegacyState { get; set; }
        public bool HasRTState { get; set; }
        public bool HasTree { get; set; }
        public bool HasChart { get; set; }
        public bool HasRichControl { get; set; }
        public bool HasPivot { get; set; }
        public bool HasContextDataItem { get; set; }
        public bool HasDataDrivenTask { get; set; }
        public bool HasControlExtensions { get; set; }
        public bool HasMessageLookup { get; set; }
        public bool HasMessageLookupPub { get; set; }
        public bool HasDynamicLink { get; set; }
        public bool HasBaseCallout { get; set; }
        public bool HasTaskCallout { get; set; }
        public bool HasPreTaskCallout { get; set; }
        public bool HasPostTaskCallout { get; set; }
        public bool HaseZeeView { get; set; }
        public bool HasDynamicILBOTitle { get; set; }
        public bool HasDataSavingTask { get; set; }
        public bool HasIlboPublished { get; set; }
        public bool HasStackedLinks { get; set; }
        public bool HasVwQlik { get; set; }
        public bool HasSensitiveData { get; set; }
        public bool HasUniversalPersonalization { get; set; }


        private List<Page> _TabPages;
        private List<PublicationInfo> _Publication;
        private List<TaskService> _TaskServiceList;
        private List<Tree> _Trees;
        private List<Chart> _Charts;
        private List<Control> _Controls;
        private List<Zoom> _ZoomControls;


        public ILBO()
        {
            this._TabPages = new List<Page>();
            this._Publication = new List<PublicationInfo>();
            this._TaskServiceList = new List<TaskService>();
            this._Trees = new List<Tree>();
            this._Charts = new List<Chart>();
            this._Controls = new List<Control>();
            this._ZoomControls = new List<Zoom>();
        }

        public List<Page> TabPages
        {
            get
            {
                return this._TabPages;
            }
            set
            {
                this._TabPages = value;
            }
        }
        public List<PublicationInfo> Publication
        {
            get
            {
                return this._Publication;
            }
            set
            {
                this._Publication = value;
            }
        }
        public List<TaskService> TaskServiceList
        {
            get
            {
                return this._TaskServiceList;
            }
            set
            {
                this._TaskServiceList = value;
            }
        }

        public List<Tree> Trees
        {
            get
            {
                return this._Trees;
            }
            set
            {
                this._Trees = value;
            }
        }

        public List<Chart> Charts
        {
            get
            {
                return this._Charts;
            }
            set
            {
                this._Charts = value;
            }
        }

        public List<Control> Controls
        {
            get
            {
                return this._Controls;
            }
            set
            {
                this._Controls = value;
            }
        }

        public List<Zoom> ZoomControls
        {
            get
            {
                return this._ZoomControls;
            }
            set
            {
                this._ZoomControls = value;
            }
        }



        public void FillData(DataRow dr, ECRLevelOptions ecrOptions)
        {
            try
            {
                Code = Convert.ToString(dr["ilbocode"]);
                Type = Convert.ToString(dr["ilbotype"]);
                Desc = Convert.ToString(dr["ilbodesc"]);
                HasLegacyState = !string.IsNullOrEmpty(Convert.ToString(dr["haslegacystate"]));
                HasRTState = !string.IsNullOrEmpty(Convert.ToString(dr["hasrtstate"]));
                HasTree = !string.IsNullOrEmpty(Convert.ToString(dr["hastree"]));
                HasChart = !string.IsNullOrEmpty(Convert.ToString(dr["haschart"]));
                HasRichControl = !string.IsNullOrEmpty(Convert.ToString(dr["hasrichcontrol"]));
                HasContextDataItem = !string.IsNullOrEmpty(Convert.ToString(dr["hascontextdataitem"]));
                HasDataDrivenTask = !string.IsNullOrEmpty(Convert.ToString(dr["hasdatadriventask"]));
                HaseZeeView = !string.IsNullOrEmpty(Convert.ToString(dr["hasezeeview"]));
                HasDynamicILBOTitle = !string.IsNullOrEmpty(Convert.ToString(dr["hasdynamicilbotitle"]));
                HasDataSavingTask = !string.IsNullOrEmpty(Convert.ToString(dr["hasdatasavingtask"]));
                HasPreTaskCallout = !string.IsNullOrEmpty(Convert.ToString(dr["haspretaskcallout"]));
                HasPostTaskCallout = !string.IsNullOrEmpty(Convert.ToString(dr["hasposttaskcallout"]));
                HasBaseCallout = !string.IsNullOrEmpty(Convert.ToString(dr["hasbasecallout"]));
                HasPivot = !string.IsNullOrEmpty(Convert.ToString(dr["haspivot"]));
                HasMessageLookup = !string.IsNullOrEmpty(Convert.ToString(dr["hasmessagelookup"]));
                HasMessageLookupPub = !string.IsNullOrEmpty(Convert.ToString(dr["hasmessagelookuppub"]));
                HasIlboPublished = !string.IsNullOrEmpty(Convert.ToString(dr["hasilbopublished"]));
                HasControlExtensions = !string.IsNullOrEmpty(Convert.ToString(dr["hascontrolextension"]));
                HasDynamicLink = !string.IsNullOrEmpty(Convert.ToString(dr["hasdynamiclink"]));
                HasVwQlik = !string.IsNullOrEmpty(Convert.ToString(dr["hasqlik"]));
                HasSensitiveData = string.Compare(Convert.ToString(dr["hassensitivedata"]), "y", true) == 0;
                //HasUniversalPersonalization = string.Compare(dr.Field<string>("hasuniversalpersonalization"), "y", true) == 0;

                ecrOptions.CurrentIlbo = Code;

                //populate page
                dr.GetChildRows("R_Ilbo_Page").ToList<DataRow>().ForEach(drTabPage =>
                {
                    Page page = new Page();
                    page.FillData(drTabPage, ecrOptions);
                    _TabPages.Add(page);
                });

                this.HasStateCtrl = (from p in TabPages
                                     from c in p.Controls
                                     where string.Compare(c.Id, "hdnhdnrt_stcontrol", true) == 0
                                     select c).Any();

                //populating publication info
                dr.GetChildRows("R_Ilbo_Publication").ToList<DataRow>().ForEach(drSubscription =>
                {
                    PublicationInfo publicationInfo = new PublicationInfo();
                    publicationInfo.FillData(drSubscription);
                    Publication.Add(publicationInfo);
                });

                //populating task service list under the ilbo
                dr.GetChildRows("R_Ilbo_TaskService").ToList<DataRow>().ForEach(drTask =>
                {
                    TaskService task = new TaskService();
                    task.FillData(drTask, ecrOptions);
                    TaskServiceList.Add(task);
                });

                dr.GetChildRows("R_Ilbo_Tree").ToList<DataRow>().ForEach(drTree =>
                {
                    Tree tree = new Tree();
                    tree.FillData(drTree);
                    Trees.Add(tree);
                });

                dr.GetChildRows("R_Ilbo_Chart").ToList<DataRow>().ForEach(drChart =>
                {
                    Chart chart = new Chart();
                    chart.FillData(drChart);
                    Charts.Add(chart);
                });

                //for populating control
                dr.GetChildRows("R_Ilbo_Control").GroupBy(row => row.Field<string>("controlid")).ToList<IGrouping<string, DataRow>>().ForEach(drControlGrp =>
                {
                    IEnumerable<DataRow> drControlGrpTmp = drControlGrp.OrderBy(r => r.Field<string>("type"));

                    Control control = new Control();
                    control.FillData(drControlGrpTmp.First(), ecrOptions);
                    Controls.Add(control);
                });

                dr.GetChildRows("R_Ilbo_ParentZoomControl").ToList<DataRow>().ForEach(drZoomControl =>
                {
                    Zoom zoomControl = new Zoom();
                    zoomControl.FillData(drZoomControl);
                    ZoomControls.Add(zoomControl);
                });

            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("ILBO.FillData()->{0}", ex.InnerException != null ? ex.InnerException.Message : ex.Message));
            }
        }
    }
    public class Page
    {
        public string Name { get; set; }
        public string Sequence { get; set; }

        private List<Control> _Controls;
        private List<Tree> _Trees;
        private List<Chart> _Charts;

        public Page()
        {
            this._Controls = new List<Control>();
            this._Trees = new List<Tree>();
            this._Charts = new List<Chart>();
        }

        public List<Control> Controls
        {
            get
            {
                return this._Controls;
            }
            set
            {
                this._Controls = value;
            }
        }
        public List<Tree> Trees
        {
            get
            {
                return this._Trees;
            }
            set
            {
                this._Trees = value;
            }
        }
        public List<Chart> Charts
        {
            get
            {
                return this._Charts;
            }
            set
            {
                this._Charts = value;
            }
        }

        public void FillData(DataRow dr, ECRLevelOptions ecrOptions)
        {
            try
            {
                Name = Convert.ToString(dr["tabname"]);
                Sequence = Convert.ToString(dr["sequence"]);

                //for populating control
                dr.GetChildRows("R_Page_Control").GroupBy(row => row.Field<string>("controlid")).ToList<IGrouping<string, DataRow>>().ForEach(drControlGrp =>
                {
                    IEnumerable<DataRow> drControlGrpTmp = drControlGrp.OrderBy(r => r.Field<string>("type"));
                    Control control = new Control();
                    control.FillData(drControlGrpTmp.First(), ecrOptions);
                    Controls.Add(control);
                });

                //for populating tree
                dr.GetChildRows("R_Page_Tree").ToList<DataRow>().ForEach(drTree =>
                {
                    Tree tree = new Tree();
                    tree.FillData(drTree);
                    Trees.Add(tree);
                });

                //for poulating chart
                dr.GetChildRows("R_Page_Chart").ToList<DataRow>().ForEach(drChart =>
                {
                    Chart chart = new Chart();
                    chart.FillData(drChart);
                    Charts.Add(chart);
                });
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Page.FillData()->{0}", ex.InnerException != null ? ex.InnerException.Message : ex.Message));
            }
        }
    }
    public class Tree
    {
        public string Name { get; set; }
        public string Page { get; set; }
        public string Clear_Flag { get; set; }
        public List<TreeInfo> Info { get; set; }

        public Tree()
        {
            this.Info = new List<TreeInfo>();
        }

        public void FillData(DataRow dr)
        {
            try
            {
                Name = Convert.ToString(dr["treecontrol"]);
                Page = Convert.ToString(dr["tabname"]);
                Clear_Flag = Convert.ToString(dr["clear_flag"]);

                dr.GetChildRows("R_Tree_TreeInfo").ToList<DataRow>().ForEach(drTreeInfo =>
                {
                    TreeInfo treeInfo = new TreeInfo();
                    treeInfo.FillData(drTreeInfo);
                    Info.Add(treeInfo);
                });
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Tree.FillData()->{0}", ex.InnerException != null ? ex.InnerException.Message : ex.Message));
            }
        }
    }
    public class TreeInfo
    {
        public string NodeType { get; set; }
        public string NodeDesc { get; set; }
        public string OpenImage { get; set; }
        public string NotExpandedImage { get; set; }
        public string ExpandableImage { get; set; }
        public string ExpandedImage { get; set; }
        public string CloseImage { get; set; }
        public string CheckImage { get; set; }
        public string UnCheckImage { get; set; }
        public string PartialCheckImage { get; set; }
        public void FillData(DataRow dr)
        {
            try
            {
                NodeType = Convert.ToString(dr["node_type"]);
                NodeDesc = Convert.ToString(dr["node_description"]);
                OpenImage = Convert.ToString(dr["vwt_openimage"]);
                NotExpandedImage = Convert.ToString(dr["vwt_notexpandedimage"]);
                ExpandableImage = Convert.ToString(dr["vwt_expandableimage"]);
                ExpandedImage = Convert.ToString(dr["vwt_expandedimage"]);
                CloseImage = Convert.ToString(dr["vwt_closeimage"]);
                CheckImage = Convert.ToString(dr["vwt_checkimage"]);
                UnCheckImage = Convert.ToString(dr["vwt_uncheckimage"]);
                PartialCheckImage = Convert.ToString(dr["chkbox_parial_chkimg"]);
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("TreeInfo.FillData()->{0}", ex.InnerException != null ? ex.InnerException.Message : ex.Message));
            }
        }
    }
    public class Chart
    {
        public string Name { get; set; }
        public string TabName { get; set; }
        public void FillData(DataRow dr)
        {
            try
            {
                Name = Convert.ToString(dr["chartcontrol"]);
                TabName = Convert.ToString(dr["tabname"]);
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Chart.FillData()->{0}", ex.InnerException != null ? ex.InnerException.Message : ex.Message));
            }
        }
    }
    public class Zoom
    {
        public string IlboCode { get; set; }
        public string ParentIlboCode { get; set; }
        public string TaskName { get; set; }
        public string ControlId { get; set; }
        private List<ZoomMapping> _Mappings;

        public List<ZoomMapping> Mappings
        {
            get
            {
                return this._Mappings;
            }
            set
            {
                this._Mappings = value;
            }
        }

        public Zoom()
        {
            this._Mappings = new List<ZoomMapping>();
        }
        public void FillData(DataRow dr)
        {
            try
            {
                IlboCode = Convert.ToString(dr["childilbocode"]);
                ParentIlboCode = Convert.ToString(dr["parentilbocode"]);
                TaskName = Convert.ToString(dr["taskname"]);
                ControlId = Convert.ToString(dr["controlid"]);

                dr.GetChildRows("R_Zoom_Mapping").ToList<DataRow>().ForEach(drZoomMapping =>
                {
                    ZoomMapping zoomMapping = new ZoomMapping();
                    zoomMapping.FillData(drZoomMapping);
                    Mappings.Add(zoomMapping);
                });
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Zoom.FillData()->{0}", ex.InnerException != null ? ex.InnerException.Message : ex.Message));
            }
        }
    }
    public class Control
    {
        public string Id { get; set; }
        public string BtSynonym { get; set; }
        public string ViewName { get; set; }
        public string PageName { get; set; }
        public string Type { get; set; }
        public string DisplayFlag { get; set; }
        public string DataType { get; set; }
        public string SectionType { get; set; }
        public bool IsLayoutControl { get; set; }
        public int VisibleRowCount { get; set; }
        public bool ListEditRequired { get; set; }
        public bool IsRichControl { get; set; }
        public string Precision { get; set; }

        private List<View> _Views;
        private List<Property> _Properties;
        private Zoom _Zoom;
        private List<Enumeration> _Enumerations;
        private List<SyncView> _SyncViews;
        //private ZoomView _ZoomControl;

        public List<View> Views
        {
            get
            {
                return this._Views;
            }
            set
            {
                this._Views = value;
            }
        }
        public List<Property> Properties
        {
            get
            {
                return this._Properties;
            }
            set
            {
                this._Properties = value;
            }
        }
        public Zoom Zoom
        {
            get
            {
                return this._Zoom;
            }
            set
            {
                this._Zoom = value;
            }
        }

        public List<Enumeration> Enumerations
        {
            get
            {
                return this._Enumerations;
            }
            set
            {
                this._Enumerations = value;
            }
        }
        public List<SyncView> SyncViews
        {
            get
            {
                return this._SyncViews;
            }
            set
            {
                this._SyncViews = value;
            }
        }

        public Control()
        {
            this._Properties = new List<Property>();
            this._Views = new List<View>();
            this._Enumerations = new List<Enumeration>();
            this._SyncViews = new List<SyncView>();
        }
        public void FillData(DataRow dr, ECRLevelOptions ecrOptions)
        {
            try
            {
                Id = Convert.ToString(dr["controlid"]);

                //if (string.Compare("mlcalender_control", Id, true) == 0)
                //{
                //    Id = Id;
                //}

                BtSynonym = Convert.ToString(dr["btsynonym"]);
                ViewName = Convert.ToString(dr["viewname"]);
                PageName = Convert.ToString(dr["tabname"]);
                Type = Convert.ToString(dr["controltype"]);
                DisplayFlag = Convert.ToString(dr["displayflag"]);
                DataType = Convert.ToString(dr["datatype"]);
                IsLayoutControl = Convert.ToString(dr["layoutcontrol"]).ToLower().Equals("y");
                ListEditRequired = !string.IsNullOrEmpty(Convert.ToString(dr["listedit_req"]));
                Precision = Convert.ToString(dr["precision"]);
                SectionType = string.Empty;

                string sCurrentIlbo = Convert.ToString(dr["ilbocode"]);

                IEnumerable<DataRow> drViews = from drView in ecrOptions.generation.ActivityInfo.Tables["Control"].AsEnumerable()
                                               where string.Compare(Convert.ToString(drView["ilbocode"]), sCurrentIlbo, true) == 0
                                               && string.Compare(Convert.ToString(drView["controlid"]), Id, true) == 0
                                               && string.Compare(Convert.ToString(drView["type"]), "view", true) == 0
                                               select drView;

                drViews.ToList<DataRow>().ForEach(drView =>
                {
                    View view = new View();
                    view.FillData(drView);
                    Views.Add(view);
                });

                #region control properties
                IEnumerable<DataRow> drControlProperties = null;

                //for grid control
                if (drViews.Count() > 0)
                {
                    drControlProperties = ecrOptions.generation.ActivityInfo.Tables["ControlProperty"].AsEnumerable()
                        .Where(row => string.Compare(row.Field<string>("ilbocode"), sCurrentIlbo, true) == 0
                                        && string.Compare(row.Field<string>("controlid"), Id, true) == 0
                                        && string.Compare(row.Field<string>("viewname"), Id, true) == 0);
                }

                //for header control
                else
                {
                    drControlProperties = dr.GetChildRows("R_Control_Property");
                }

                drControlProperties.ToList<DataRow>().ForEach(drProperty =>
                {
                    Property property = new Property();
                    property.FillData(drProperty);
                    Properties.Add(property);
                });
                #endregion

                #region Rich Control
                IEnumerable<DataRow> drRichControls = dr.GetChildRows("R_Control_RichControl");
                if (drRichControls.Any())
                {
                    IsRichControl = true;
                    SectionType = Convert.ToString(drRichControls.First()["sectiontype"]);
                }
                #endregion  

                #region Zoom mapping
                IEnumerable<DataRow> drZoom = dr.GetChildRows("R_Control_Zoom");
                if (drZoom.Any())
                {
                    this.Zoom = new Zoom();
                    this.Zoom.FillData(drZoom.First());
                }
                #endregion

                #region Enumerations
                dr.GetChildRows("R_Control_Enum").ToList<DataRow>().ForEach(drEnum =>
                {
                    Enumeration enumeration = new Enumeration();
                    enumeration.FillData(drEnum);
                    Enumerations.Add(enumeration);
                });
                #endregion

                dr.GetChildRows("R_Control_SyncView").ToList<DataRow>().ForEach(drSyncView =>
                {
                    SyncView syncView = new SyncView();
                    syncView.FillData(drSyncView);
                    SyncViews.Add(syncView);
                });

            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Control.FillData()->{0}", ex.InnerException != null ? ex.InnerException.Message : ex.Message));
            }
        }
    }
    public class View
    {
        public string Name { get; set; }
        public string ControlId { get; set; }
        public string BtSynonym { get; set; }
        public string Type { get; set; }
        public string DisplayFlag { get; set; }
        public string DataType { get; set; }
        public string Length { get; set; }
        public string Precision { get; set; }
        public bool ListEditRequired { get; set; }
        public string ListEditAssociatedCtrlId { get; set; }
        public bool IsControl { get; set; }
        public bool IsView { get; set; }


        private List<Property> _Properties;
        private List<Enumeration> _Enumerations;
        private PivotField _PivotField;

        public List<Property> Properties
        {
            get
            {
                return this._Properties;
            }
            set
            {
                this._Properties = value;
            }
        }
        public List<Enumeration> Enumerations
        {
            get
            {
                return this._Enumerations;
            }
            set
            {
                this._Enumerations = value;
            }
        }
        public PivotField PivotField
        {
            get
            {
                return _PivotField;
            }
            set
            {
                this._PivotField = value;
            }
        }

        public View()
        {
            this._Properties = new List<Property>();
            this._Enumerations = new List<Enumeration>();
            this._PivotField = null;
        }
        public void FillData(DataRow dr)
        {
            try
            {
                Name = Convert.ToString(dr["viewname"]);
                ControlId = Convert.ToString(dr["controlid"]);
                BtSynonym = Convert.ToString(dr["btsynonym"]);
                Type = Convert.ToString(dr["controltype"]);
                DisplayFlag = Convert.ToString(dr["displayflag"]);
                DataType = Convert.ToString(dr["datatype"]);
                Length = Convert.ToString(dr["length"]);
                Precision = Convert.ToString(dr["precision"]);
                ListEditRequired = !string.IsNullOrEmpty(Convert.ToString(dr["listedit_req"]));
                IsControl = Name.Equals(ControlId);
                IsView = !(Name.Equals(ControlId));
                ListEditAssociatedCtrlId = Convert.ToString(dr["associatedcontrol"]);

                dr.GetChildRows("R_Control_Property").ToList<DataRow>().ForEach(drProperty =>
                {
                    Property property = new Property();
                    property.FillData(drProperty);
                    Properties.Add(property);
                });

                dr.GetChildRows("R_Control_Enum").ToList<DataRow>().ForEach(drEnum =>
                {
                    Enumeration enumeration = new Enumeration();
                    enumeration.FillData(drEnum);
                    Enumerations.Add(enumeration);
                });

                if (Type.Equals("rspivotgrid") && dr.GetChildRows("R_Control_PivotField").Any())
                {
                    PivotField = new PivotField();
                    PivotField.FillData(dr.GetChildRows("R_Control_PivotField").First());
                }

            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Control.FillData()->{0}", ex.InnerException != null ? ex.InnerException.Message : ex.Message));
            }
        }
    }
    public class ZoomMapping
    {
        public string ParentViewName { get; set; }
        public string ChildControlId { get; set; }
        public string ChildViewName { get; set; }
        public string ChildDisplayFlag { get; set; }
        public string ChildControlType { get; set; }

        public void FillData(DataRow dr)
        {
            try
            {
                ParentViewName = Convert.ToString(dr["parentviewname"]);
                ChildControlId = Convert.ToString(dr["childcontrolid"]);
                ChildViewName = Convert.ToString(dr["childviewname"]);
                ChildDisplayFlag = Convert.ToString(dr["childdisplayflag"]);
                ChildControlType = Convert.ToString(dr["childcontroltype"]);
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("ZoomMapping.FillData()->{0}", ex.InnerException != null ? ex.InnerException.Message : ex.Message));
            }
        }
    }
    public class PivotField
    {
        public string RowLabel { get; set; }
        public int RowLabelSequence { get; set; }
        public string ColumnLabel { get; set; }
        public int ColumnLabelSequence { get; set; }
        public string FieldValue { get; set; }
        public int ValueSequence { get; set; }
        public string ValueFunction { get; set; }

        public void FillData(DataRow dr)
        {
            try
            {
                RowLabel = Convert.ToString(dr["rowlabel"]);
                RowLabelSequence = int.Parse(Convert.ToString(dr["rowlabelseq"]));
                ColumnLabel = Convert.ToString(dr["columnlabel"]);
                ColumnLabelSequence = int.Parse(Convert.ToString(dr["columnlabelseq"]));
                FieldValue = Convert.ToString(dr["fieldvalue"]);
                ValueSequence = int.Parse(Convert.ToString(dr["valueseq"]));
                ValueFunction = Convert.ToString(dr["valuefunction"]);
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("PivotField.FillData()->{0}", ex.InnerException != null ? ex.InnerException.Message : ex.Message));
            }
        }
    }
    public class Enumeration
    {
        public int SequenceNo { get; set; }
        public string OptionCode { get; set; }
        public string OptionDesc { get; set; }
        public int LangId { get; set; }
        public void FillData(DataRow dr)
        {
            try
            {
                SequenceNo = int.Parse(Convert.ToString(dr["sequenceno"]));
                OptionCode = Convert.ToString(dr["optioncode"]);
                OptionDesc = Convert.ToString(dr["optiondesc"]);
                LangId = int.Parse(Convert.ToString(dr["languageid"]));
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Enumeration.FillData()->{0}", ex.InnerException != null ? ex.InnerException.Message : ex.Message));
            }
        }
    }

    public class Property
    {
        public string Name { get; set; }
        public string Value { get; set; }

        public void FillData(DataRow dr)
        {
            try
            {
                Name = Convert.ToString(dr["propertyname"]);
                Value = Convert.ToString(dr["propertyvalue"]);
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Property.FillData()->{0}", ex.InnerException != null ? ex.InnerException.Message : ex.Message));
            }
        }
    }

    public class SubscriberInfo
    {
        public int LinkId { get; set; }
        public string ControlId { get; set; }
        public string DataItemName { get; set; }
        public string ViewName { get; set; }
        public bool RetrieveMultiple { get; set; }
        public string Flow { get; set; }

        public SubscriberInfo()
        { }
        public void FillData(DataRow drSubscription)
        {
            try
            {
                LinkId = int.Parse(Convert.ToString(drSubscription["linkid"]));
                ControlId = Convert.ToString(drSubscription["controlid"]);
                DataItemName = Convert.ToString(drSubscription["dataitemname"]);
                ViewName = Convert.ToString(drSubscription["viewname"]);
                Flow = Convert.ToString(drSubscription["flowattribute"]);
                RetrieveMultiple = Convert.ToString(drSubscription["retrievemultiple"]).Equals("1");
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("SubscriberInfo.FillData()->{0}", ex.InnerException != null ? ex.InnerException.Message : ex.Message));
            }
        }
    }
    public class PublicationInfo
    {
        public int LinkId { get; set; }
        public string ControlId { get; set; }
        public string ViewName { get; set; }
        public string DataItemName { get; set; }
        public string ControlType { get; set; }
        public string Flow { get; set; }

        public PublicationInfo()
        {

        }

        public void FillData(DataRow dr)
        {
            try
            {
                LinkId = int.Parse(Convert.ToString(dr["linkid"]));
                ControlId = Convert.ToString(dr["controlid"]);
                ViewName = Convert.ToString(dr["viewname"]);
                DataItemName = Convert.ToString(dr["dataitemname"]);
                Flow = Convert.ToString(dr["flowtype"]);
                ControlType = Convert.ToString(dr["controltype"]);
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("PublicationInfo.FillData()->{0}", ex.InnerException != null ? ex.InnerException.Message : ex.Message));
            }
        }
    }
    public class Traversal
    {
        public string TargetComponent { get; set; }
        public string ChildActivity { get; set; }
        public string ChildIlbo { get; set; }
        public int ChildActivityType { get; set; }
        public string PostTask { get; set; }


        public void FillData(DataRow drTraversal)
        {
            try
            {
                TargetComponent = Convert.ToString(drTraversal["componentname"]);
                ChildActivity = Convert.ToString(drTraversal["childactivity"]);
                ChildIlbo = Convert.ToString(drTraversal["childilbo"]);
                ChildActivityType = int.Parse(Convert.ToString(drTraversal["childactivitytype"]));
                PostTask = Convert.ToString(drTraversal["posttask"]);
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Traversal.FillData()->{0}", ex.InnerException != null ? ex.InnerException.Message : ex.Message));
            }
        }
    }
    public class TaskData
    {
        public string TabName { get; set; }
        public string Filling { get; set; }
        private Control _Control;

        public Control Control
        {
            get
            {
                return this._Control;
            }
            set
            {
                this._Control = value;
            }
        }

        public void FillData(DataRow drTaskData, ECRLevelOptions ecrOptions)
        {
            try
            {
                TabName = Convert.ToString(drTaskData["tabname"]);
                Filling = Convert.ToString(drTaskData["filling"]);

                string sCurrentIlbo = Convert.ToString(drTaskData["ilbocode"]);

                IEnumerable<DataRow> drControls = from row in ecrOptions.generation.ActivityInfo.Tables["Control"].AsEnumerable()
                                                  where string.Compare(row.Field<string>("ilbocode"), sCurrentIlbo, true) == 0
                                                  && string.Compare(row.Field<string>("tabname"), TabName, true) == 0
                                                  && string.Compare(row.Field<string>("controlid"), Convert.ToString(drTaskData["controlid"]), true) == 0
                                                  select row;


                Control control = new Control();
                try
                {
                    control.FillData(drControls.First(), ecrOptions);
                }
                catch
                {
                    throw new Exception(string.Format("DataUnavailbale for the Ilbo - {0}, tabname - {1}, controlid - {2} in control details.\n", sCurrentIlbo, TabName, Convert.ToString(drTaskData["controlid"])));
                };
                this.Control = control;
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("TaskData.FillData()->{0}", ex.InnerException != null ? ex.InnerException.Message : ex.Message));
            }
        }
    }
    public class ReportData
    {
        public int ProcessingType { get; set; }
        public string ContextName { get; set; }
        public string Type { get; set; }
        public string PageName { get; set; }

        public void FillData(DataRow dr)
        {
            try
            {
                ProcessingType = int.Parse(Convert.ToString(dr["processingtype"]));
                ContextName = Convert.ToString(dr["contextname"]);
                Type = Convert.ToString(dr["reporttype"]);
                PageName = Convert.ToString(dr["tabname"]);
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("ReportInfo.FillData()=>{0}", ex.InnerException != null ? ex.InnerException.Message : ex.Message));
            }
        }
    }
    public class TaskService
    {

        public string Name { get; set; }
        public string Type { get; set; }
        public string Desc { get; set; }
        public string ServiceName { get; set; }
        public int ServiceType { get; set; }
        public string ComponentName { get; set; }
        public string SessionVariable { get; set; }
        public string ResultantAsp { get; set; }
        public bool IsDataSavingTask { get; set; }
        public bool IsZipped { get; set; }
        public bool IsCached { get; set; }
        public string ClearKeyPattern { get; set; }
        public string SetKeyPattern { get; set; }
        public bool HasChart { get; set; }
        public bool HasTree { get; set; }
        public bool HasRichControls { get; set; }
        public bool HasTaskCallout { get; set; }
        public string PrimaryControlSynonym { get; set; }
        public bool HasUniversalPersonalization { get; set; }

        private List<SubscriberInfo> _Subscriptions;
        private Traversal _Traversal;
        private List<Tree> _Trees;
        private List<Chart> _Charts;
        private List<TaskData> _TaskInfos;
        private List<Segment> _Segments;
        private ReportData _ReportInfo;
        private EzeeView _EzeeView;
        private List<Control> _RichControls;
        private TaskCallout _TaskCallout;

        public List<SubscriberInfo> Subscriptions
        {
            get
            {
                return this._Subscriptions;
            }
            set
            {
                this._Subscriptions = value;
            }
        }
        public Traversal Traversal
        {
            get
            {
                return this._Traversal;
            }
            set
            {
                this._Traversal = value;
            }
        }
        public List<Tree> Trees
        {
            get
            {
                return _Trees;
            }

            set
            {
                _Trees = value;
            }
        }
        public List<Chart> Charts
        {
            get
            {
                return this._Charts;
            }
            set
            {
                this._Charts = value;
            }
        }
        public List<TaskData> TaskInfos
        {
            get
            {
                return this._TaskInfos;
            }
            set
            {
                this._TaskInfos = value;
            }
        }
        public List<Segment> Segments
        {
            get
            {
                return this._Segments;
            }
            set
            {
                this._Segments = value;
            }
        }
        public EzeeView EzeeView
        {
            get
            {
                return this._EzeeView;
            }
            set
            {
                this._EzeeView = value;
            }
        }
        public ReportData ReportInfo
        {
            get
            {
                return this._ReportInfo;
            }
            set
            {
                this._ReportInfo = value;
            }
        }
        public List<Control> RichControls
        {
            get
            {
                return this._RichControls;
            }
            set
            {
                this._RichControls = value;
            }
        }
        public TaskCallout TaskCallout
        {
            get
            {
                return this._TaskCallout;
            }
            set
            {
                this._TaskCallout = value;
            }
        }

        public TaskService()
        {
            this._Trees = new List<Tree>();
            this._Charts = new List<Chart>();
            this._TaskInfos = new List<TaskData>();
            this._Segments = new List<Segment>();
            this._RichControls = new List<Control>();
            this._Subscriptions = new List<SubscriberInfo>();
            this._TaskCallout = null;
        }
        public void FillData(DataRow dr, ECRLevelOptions ecrOptions)
        {
            try
            {
                Name = Convert.ToString(dr["taskname"]);
                Type = Convert.ToString(dr["tasktype"]);
                Desc = Convert.ToString(dr["taskdesc"]);
                ServiceName = Convert.ToString(dr["servicename"]);
                ServiceType = string.IsNullOrEmpty(Convert.ToString(dr["servicetype"])) ? 0 : int.Parse(Convert.ToString(dr["servicetype"]));
                ComponentName = Convert.ToString(dr["servicecomponentname"]);
                SessionVariable = Convert.ToString(dr["sessionvariable"]);
                ResultantAsp = Convert.ToString(dr["resultantasp"]);
                IsDataSavingTask = Convert.ToString(dr["datasavingtask"]).Equals("1");
                IsZipped = Convert.ToString(dr["iszipped"]).Equals("1");
                IsCached = Convert.ToString(dr["iscached"]).Equals("1");
                ClearKeyPattern = Convert.ToString(dr["clearkey_pattern"]);
                SetKeyPattern = Convert.ToString(dr["setkey_pattern"]);
                PrimaryControlSynonym = Convert.ToString(dr["primarycontrol"]);
                //HasUniversalPersonalization = string.Compare(dr.Field<string>("IsUniversalPersonalization"), "y", true) == 0;

                //populate tree control
                dr.GetChildRows("R_Task_Tree").ToList<DataRow>().ForEach(drTree =>
                {
                    Tree tree = new Tree();
                    tree.FillData(drTree);
                    Trees.Add(tree);
                });


                //populate chart control
                dr.GetChildRows("R_Task_Chart").ToList<DataRow>().ForEach(drChart =>
                {
                    Chart chart = new Chart();
                    chart.FillData(drChart);
                    Charts.Add(chart);
                });

                //populate traversal info
                IEnumerable<DataRow> drTraversals = dr.GetChildRows("R_Task_Traversal");
                if (drTraversals.Any())
                {
                    this.Traversal = new Traversal();
                    this.Traversal.FillData(drTraversals.First());
                }

                //populate subscriberInfo
                dr.GetChildRows("R_Task_Subscription").ToList<DataRow>().ForEach(drSubscription =>
                {
                    SubscriberInfo subscriberInfo = new SubscriberInfo();
                    subscriberInfo.FillData(drSubscription);
                    Subscriptions.Add(subscriberInfo);
                });

                //populate taskdata
                dr.GetChildRows("R_Task_Taskdata").ToList<DataRow>().ForEach(drTaskData =>
                {
                    TaskData taskdata = new TaskData();
                    taskdata.FillData(drTaskData, ecrOptions);
                    TaskInfos.Add(taskdata);
                });

                //populate report info
                IEnumerable<DataRow> drReportData = dr.GetChildRows("R_Task_Report");
                if (drReportData.Any())
                {
                    this.ReportInfo = new ReportData();
                    ReportInfo.FillData(drReportData.First());
                }

                //populate ezeeview
                IEnumerable<DataRow> drEzeeViews = dr.GetChildRows("R_Task_Ezeeview");
                if (drEzeeViews.Any())
                {
                    this.EzeeView = new EzeeView();
                    EzeeView.FillData(drEzeeViews.First(), ecrOptions);
                }

                //populate segments
                dr.GetChildRows("R_TaskService_Segment").ToList<DataRow>().ForEach(drSegment =>
                {
                    Segment segment = new Segment();
                    segment.FillData(drSegment, ecrOptions);
                    this.Segments.Add(segment);
                });

                //Taskcallout
                if (dr.GetChildRows("R_Task_Callout").Any())
                {
                    TaskCallout taskCallout = new TaskCallout();
                    taskCallout.FillData(dr.GetChildRows("R_Task_Callout"));
                    this.TaskCallout = taskCallout;
                    this.HasTaskCallout = true;
                }

                if (this.Charts.Any())
                    this.HasChart = true;

                if (this.Trees.Any())
                    this.HasTree = true;

                //adding rich controls                
                (from s in this.Segments
                 from d in s.DataItems.Where(di => di.Control != null)
                 where d.Control.SectionType != string.Empty
                 group d.Control by d.Control.Id into g
                 select g).ToList<IGrouping<string, Control>>().ForEach(rc => { this.RichControls.Add(rc.First()); });
                if (this.RichControls.Any())
                    this.HasRichControls = true;

            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Task.FillData()->{0}", ex.InnerException != null ? ex.InnerException.Message : ex.Message));
            }
        }
    }

    public class TaskCallout
    {
        private CalloutInfo _PreCallout = null;
        private CalloutInfo _PostCallout = null;

        public CalloutInfo PreCallout
        {
            get
            {
                return this._PreCallout;
            }
            set
            {
                this._PreCallout = value;
            }
        }
        public CalloutInfo PostCallout
        {
            get
            {
                return this._PostCallout;
            }
            set
            {
                this._PostCallout = value;
            }
        }

        public string Name { get; set; }



        public TaskCallout()
        {

        }

        public void FillData(IEnumerable<DataRow> drs)
        {
            try
            {
                this.Name = Convert.ToString(drs.First()["calloutname"]);
                foreach (DataRow dr in drs)
                {
                    string sMode = Convert.ToString(dr["calloutmode"]);
                    if (string.Compare(sMode, "pre", true) == 0)
                    {
                        CalloutInfo calloutInfo = new CalloutInfo();
                        calloutInfo.FillData(dr);
                        PreCallout = calloutInfo;
                    }
                    else if (string.Compare(sMode, "post", true) == 0)
                    {
                        CalloutInfo calloutInfo = new CalloutInfo();
                        calloutInfo.FillData(dr);
                        PostCallout = calloutInfo;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("TaskCallout.FillData()->{0}", ex.InnerException != null ? ex.InnerException.Message : ex.Message));
            }
        }
    }

    public class CalloutInfo
    {
        private List<TaskCalloutSegment> _Segments = null;
        public List<TaskCalloutSegment> Segments
        {
            get
            {
                return this._Segments;
            }
            set
            {
                this._Segments = value;
            }
        }
        public CalloutInfo()
        {
            this._Segments = new List<TaskCalloutSegment>();
        }

        public void FillData(DataRow dr)
        {
            #region fill segments
            dr.GetChildRows("R_Callout_Segments").ToList<DataRow>().ForEach(drSegment =>
            {
                TaskCalloutSegment segment = new TaskCalloutSegment();
                segment.FillData(drSegment);
                this._Segments.Add(segment);
            });
            #endregion
        }
    }


    public class TaskCalloutSegment
    {
        public string Name { get; set; }
        public int Instance { get; set; }
        public int Sequence { get; set; }
        public Boolean Filling { get; set; }
        public string FlowDirection { get; set; }
        private List<TaskCalloutDataItem> _DataItems = null;

        public List<TaskCalloutDataItem> DataItems
        {
            get
            {
                return this._DataItems;
            }
            set
            {
                this._DataItems = value;
            }
        }

        public TaskCalloutSegment()
        {
            this._DataItems = new List<TaskCalloutDataItem>();
        }

        public void FillData(DataRow dr)
        {
            try
            {
                this.Name = Convert.ToString(dr["segmentname"]);
                this.Instance = int.Parse(Convert.ToString(dr["instanceflag"]));
                this.Sequence = int.Parse(Convert.ToString(dr["segmentsequence"]));
                this.FlowDirection = Convert.ToString(dr["segmentflowattribute"]);
                this.Filling = Convert.ToString(dr["combofilling"]).Equals("true");

                #region fill dataitems
                dr.GetChildRows("R_CalloutSegment_Dataitems").ToList<DataRow>().ForEach(drDataitem =>
                {
                    TaskCalloutDataItem dataitem = new TaskCalloutDataItem();
                    dataitem.FillData(drDataitem);
                    this._DataItems.Add(dataitem);
                });
                #endregion
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("TaskCalloutSegment.FillData()->{0}", ex.InnerException != null ? ex.InnerException.Message : ex.Message));
            }
        }
    }

    public class TaskCalloutDataItem
    {
        public string Name { get; set; }
        public string FlowDirection { get; set; }
        public string ControlId { get; set; }
        public string ViewName { get; set; }
        public void FillData(DataRow dr)
        {
            try
            {
                this.Name = Convert.ToString(dr["dataitemname"]);
                this.FlowDirection = Convert.ToString(dr["flowattribute"]);
                this.ControlId = Convert.ToString(dr["controlid"]);
                this.ViewName = Convert.ToString(dr["viewname"]);
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("TaskCalloutDataItem.FillData()->{0}", ex.InnerException != null ? ex.InnerException.Message : ex.Message));
            }
        }
    }

    public class SyncView
    {
        public string FilterView { get; set; }
        public string ListView { get; set; }

        public void FillData(DataRow dr)
        {
            FilterView = Convert.ToString(dr["filter_viewname"]);
            ListView = Convert.ToString(dr["list_viewname"]);
        }
    }

    public class EzeeView
    {
        public string TargetSpName { get; set; }
        private List<EzeeViewParam> _Params;

        public List<EzeeViewParam> Params
        {
            get
            {
                return this._Params;
            }
            set
            {
                this._Params = value;
            }
        }

        public EzeeView()
        {
            this._Params = new List<EzeeViewParam>();
        }

        public void FillData(DataRow dr, ECRLevelOptions ecrOptions)
        {
            try
            {
                TargetSpName = Convert.ToString(dr["target_spname"]);

                IEnumerable<DataRow> drEzeeViewParams = ecrOptions.generation.ActivityInfo.Tables["EzeeView"].AsEnumerable()
                                                                                                            .Where(row => string.Compare(row.Field<string>("ui_name"), Convert.ToString(dr["ui_name"]), true) == 0
                                                                                                                       && string.Compare(row.Field<string>("taskname"), Convert.ToString(dr["taskname"]), true) == 0
                                                                                                                       && string.Compare(row.Field<string>("target_spname"), Convert.ToString(dr["target_spname"]), true) == 0);
                drEzeeViewParams.ToList<DataRow>().ForEach(drEzeeViewParam =>
                {
                    EzeeViewParam ezeeViewParam = new EzeeViewParam();
                    ezeeViewParam.FillData(drEzeeViewParam);
                    Params.Add(ezeeViewParam);
                });
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("EzeeView.FillData()->{0}", ex.InnerException != null ? ex.InnerException.Message : ex.Message));
            }
        }
    }

    public class EzeeViewParam
    {
        public string Name { get; set; }
        public string MappedControl { get; set; }
        public string ControlId { get; set; }
        public string ViewName { get; set; }
        public string DataType { get; set; }
        public string Length { get; set; }

        public void FillData(DataRow dr)
        {
            try
            {
                Name = Convert.ToString(dr["parametername"]);
                MappedControl = Convert.ToString(dr["mapped_control"]);
                ControlId = Convert.ToString(dr["controlid"]);
                ViewName = Convert.ToString(dr["viewname"]);
                DataType = Convert.ToString(dr["datatype"]);
                Length = Convert.ToString(dr["length"]);
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("EzeeViewParam.FillData()->{0}", ex.InnerException != null ? ex.InnerException.Message : ex.Message));
            }
        }

    }

    public class Service
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public Boolean IsSelected { get; set; }

        public Boolean IsZipped { get; set; }
        public Boolean IsCached { get; set; }
        public Boolean IsIntegService { get; set; }
        public Boolean IsBulkService { get; set; }
        public Boolean HasCusBRO { get; set; }
        public Boolean HasTypeBasedBRO { get; set; }
        public Boolean HasRollbackProcessSections { get; set; }
        public Boolean HasCommitProcessSection { get; set; }
        //public Boolean HasValidationMessage { get; set; }
        public Boolean HasISinSameComponent { get; set; }
        public Boolean Implement_New_Method_Of_ParamAddition { get; set; }
        public Boolean ConsumesApi { get; set; }
        public bool HasUniversalPersonalization { get; set; }

        public GenerationStatus Status { get; set; }

        public string StartTime { get; set; }
        public string EndTime { get; set; }

        [XmlArray("Segments")]
        [XmlArrayItem("Segment")]
        public List<Segment> Segments { get; set; }

        [XmlArray("ProcessSections")]
        [XmlArrayItem("ProcessSection")]
        public List<ProcessSection> ProcessSections { get; set; }

        [XmlIgnore]
        public DataTable dtDIPropetyMappings { get; set; }
        [XmlIgnore]
        public DataTable dtUnmappedDataItems { get; set; }

        /// <summary>
        /// Full filepath of the source file eg:/source/test.cs.
        /// </summary>
        public string SourcePath { get; set; }

        /// <summary>
        /// Fulle filepath of the release file eg:/source/test.netmodule.
        /// </summary>
        public string ReleasePath { get; set; }

        /// <summary>
        /// Returns datatable with details about unmapped dataitems for the service.
        /// </summary>
        /// <returns></returns>
        public DataTable GetUnMappedDataItems(ECRLevelOptions ecrOptions)
        {
            try
            {
                dtUnmappedDataItems = new DataTable();

                if (ecrOptions.generation.ServiceInfo.Tables["UnusedDI"] != null)
                {
                    var tmp = ecrOptions.generation.ServiceInfo.Tables["UnusedDI"].Select(string.Format("servicename = '{0}'", this.Name));
                    dtUnmappedDataItems = tmp.Any() ? tmp.CopyToDataTable() : new DataTable();
                }

                return dtUnmappedDataItems;
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("GetUnMappedDataItems-->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }

        public Service()
        {
            Segments = new List<Segment>();
            ProcessSections = new List<ProcessSection>();
            dtDIPropetyMappings = new DataTable();
        }

    }

    public class Resource
    {
        public string ErrSrc_FullFilepath { get; set; }
        public string PhSrc_FullFilepath { get; set; }
        public string ErrTarg_FullFilepath { get; set; }
        public string PhTarg_FullFilepath { get; set; }
        public GenerationStatus Status { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
    }

    public class ErrorHandler
    {
        public string SourcePath { get; set; }
        public string ReleasePath { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public GenerationStatus Status { get; set; }
    }

    #region 1=>

    public class ProcessSection
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string SeqNO { get; set; }
        public string ProcessingType { get; set; }
        public string Expression { get; set; }
        public ControlExpression CtrlExp { get; set; }
        public bool IsUniversalPersonalizedSection { get; set; }

        /// <summary>
        /// value will be multi instance in/inout segment . Applicable for Alternate Process Section alone.
        /// value assigned from ProcessSection_WithCtrlExpression().
        /// </summary>
        public string LoopCausingSegment { get; set; }


        [XmlIgnore]
        /// <summary>
        ///Methods details are available as relational data.. 
        /// </summary>
        public DataRow[] RelationalData { get; set; }

        [XmlArray("Methods")]
        [XmlArrayItem("Method")]
        /// <summary>
        /// 
        /// </summary>
        public List<Method> Methods { get; set; }

        public ProcessSection()
        {
            Methods = new List<Method>();
        }

    }

    internal struct PSType
    {
        public const string MAIN = "0";
        public const string COMMIT = "1";
        public const string ROLLBACK = "2";
    }

    public class Method : IEquatable<Method>
    {
        public string Name { get; set; }
        public string ID { get; set; }
        public string SeqNO { get; set; }
        public string SectionName { get; set; }
        public string PSSeqNO { get; set; }
        public Boolean IsIntegService { get; set; }
        public Boolean IsApiConsumerService { get; set; }
        public string SPName { get; set; }  
        public string SPErrorProtocol { get; set; }
        public Boolean AccessDatabase { get; set; }
        public string SystemGenerated { get; set; }
        public Boolean method_exec_cont { get; set; }
        //public string ExecutionFlg { get; set; }

        ///re-determined execution flag
        public string ExecutionFlg_r { get; set; }
        
        //public Boolean ConnectivityFlg { get; set; }
        public Boolean OperationType { get; set; }
        public ControlExpression CtrlExp { get; set; }

        /// <summary>
        /// Spec ID
        /// </summary>
        public string SpecID { get; set; }

        /// <summary>
        /// Spec Name
        /// </summary>
        public string SpecName { get; set; }

        /// <summary>
        /// Spec Version
        /// </summary>
        public string SpecVersion { get; set; }

        /// <summary>
        /// Route path
        /// </summary>
        public string RoutePath { get; set; }

        /// <summary>
        /// Operation ID
        /// </summary>
        public string OperationID { get; set; }

        /// <summary>
        /// Http verb
        /// </summary>
        public string OperationVerb { get; set; }

        /// <summary>
        /// value will be multi instance in/inout segment.
        /// value assigned from ProcessSection().
        /// </summary>
        public Segment LoopSegment { get; set; }

        [XmlIgnore]
        public ISDumpInfo ISDump { get; set; }
        [XmlIgnore]
        public DataTable ISMappings { get; set; }
        [XmlIgnore]
        public DataTable RelationData_Params { get; set; }

        public DataTable RelationData_ApiRequest { get; set; }
        public DataTable RelationData_ApiResponse { get; set; }
        public DataTable RelationData_ApiParameter { get; set; }

        public List<ApiRequest> ApiRequestInfo { get; set; }
        public List<ApiResponse> ApiResponseInfo { get; set; }
        public List<ApiParameter> ApiParameters { get; set; }

        public IntegrationService IS { get; set; }

        [XmlArray("Parameters")]
        [XmlArrayItem("Parameter")]
        public List<Parameter> Parameters { get; set; }

        public void Parameter()
        {
            Parameters = new List<Parameter>();
        }

        public bool Equals(Method other)
        {
            if (Name == other.Name)
                return true;
            return false;
        }

        public override int GetHashCode()
        {
            int hashName = Name == null ? 0 : Name.GetHashCode();

            return hashName;
        }
    }

    public class Parameter
    {
        public string Name { get; set; }
        public string MethodID { get; set; }
        public string MethodSequenceNo { get; set; }
        public string FlowDirection { get; set; }
        public string DataType { get; set; }
        public string CategorizedDataType { get; set; }
        public string Length { get; set; }
        public string DecimalLength { get; set; }
        public string RecordSetName { get; set; }
        public string SequenceNo { get; set; }
        public DataItem DI { get; set; }
        public Segment Seg { get; set; }
    }

    internal struct SpErrorProtocol
    {
        public const string OUTPARAM_PROTOCOL = "0";
        public const string SELECT_PROTOCOL = "1";
        public const string OUTPARAM_MULTIRS_PROTOCOL = "2";
    }

    #endregion

    #region 2=>

    public class Segment : IEquatable<Segment>
    {
        public string Name { get; set; }
        public string Inst { get; set; }
        public string FlowDirection { get; set; }
        public string Sequence { get; set; }
        public string MandatoryFlg { get; set; }
        public string ParentSegName { get; set; }
        public string Process_sel_rows { get; set; }
        public string Process_upd_rows { get; set; }
        public string Process_sel_upd_rows { get; set; }

        #region for activity
        public bool ComboFilling { get; set; }
        #endregion

        private List<DataItem> _DataItems;

        public List<DataItem> DataItems
        {
            get
            {
                return this._DataItems;
            }
            set
            {
                this._DataItems = value;
            }

        }
        public List<DataItem> UsedOutDI { get; set; }

        public Segment()
        {
            this._DataItems = new List<DataItem>();
        }

        public void FillData(DataRow dr, ECRLevelOptions ecrOptions)
        {
            try
            {
                Name = Convert.ToString(dr["segmentname"]);
                Sequence = Convert.ToString(dr["segmentsequence"]);
                Inst = Convert.ToString(dr["segmentinstance"]);
                FlowDirection = Convert.ToString(dr["flowdirection"]);
                ComboFilling = Convert.ToString(dr["combofilling"]).Equals("true");

                //populate dataitems
                dr.GetChildRows("R_Segment_DataItem").Distinct().ToList<DataRow>().ForEach(drDataitem =>
                {
                    DataItem dataitem = new DataItem();
                    dataitem.FillData(drDataitem, ecrOptions);
                    this.DataItems.Add(dataitem);
                });

            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Segment.FillData()->{0}", ex.InnerException != null ? ex.InnerException.Message : ex.Message));
            }
        }

        public bool Equals(Segment other)
        {
            if (Name == other.Name)
                return true;
            return false;
        }

        public override int GetHashCode()
        {
            int hashName = Name == null ? 0 : Name.GetHashCode();

            return hashName;
        }
    }

    public class DataItem : IEquatable<DataItem>
    {
        public string Name { get; set; }
        public string ControlId { get; set; }
        public string ViewName { get; set; }
        public string FlowDirection { get; set; }
        public Boolean PartOfKey { get; set; }
        public string DataType { get; set; }
        public Boolean IsMandatory { get; set; }
        public string DefaultValue { get; set; }

        #region for activity
        public Control Control { get; set; }
        public View View { get; set; }
        public string PropertyType { get; set; }
        public string PropertyName { get; set; }
        public string DiType { get; set; }
        #endregion

        public bool Equals(DataItem other)
        {
            if (Name == other.Name)
                return true;
            return false;
        }

        public override int GetHashCode()
        {
            int hashName = Name == null ? 0 : Name.GetHashCode();

            return hashName;
        }

        public void FillData(DataRow dr, ECRLevelOptions ecrOptions)
        {
            try
            {
                Name = Convert.ToString(dr["dataitemname"]);
                FlowDirection = Convert.ToString(dr["flowattribute"]);
                PropertyType = Convert.ToString(dr["propertytype"]);
                PropertyName = Convert.ToString(dr["propertyname"]);
                DiType = Convert.ToString(dr["ditype"]);
                ControlId = Convert.ToString(dr["controlid"]);
                ViewName = Convert.ToString(dr["viewname"]);

                string sCurrentIlbo = Convert.ToString(dr["ilbocode"]);

                IEnumerable<DataRow> drControls = from row in ecrOptions.generation.ActivityInfo.Tables["Control"].AsEnumerable()
                                                  where string.Compare(row.Field<string>("ilbocode"), sCurrentIlbo, true) == 0
                                                  && string.Compare(row.Field<string>("controlid"), Convert.ToString(dr["controlid"]), true) == 0
                                                  select row;

                IEnumerable<DataRow> drViews = from row in ecrOptions.generation.ActivityInfo.Tables["Control"].AsEnumerable()
                                               where string.Compare(row.Field<string>("ilbocode"), sCurrentIlbo, true) == 0
                                               && string.Compare(row.Field<string>("controlid"), Convert.ToString(dr["controlid"]), true) == 0
                                               && string.Compare(row.Field<string>("viewname"), Convert.ToString(dr["viewname"]), true) == 0
                                               select row;

                if (drControls.Any())
                {
                    DataRow drControl = null;
                    drControl = drControls.First();

                    this.Control = new Control();
                    this.Control.FillData(drControl, ecrOptions);
                }

                if (drViews.Any())
                {
                    this.View = new View();
                    this.View.FillData(drViews.First());
                }

            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("DataItem.FillData()->{0}", ex.InnerException != null ? ex.InnerException.Message : ex.Message));
            }
        }
    }

    #endregion

    public class IntegrationService
    {
        public IntegrationService()
        {
            Mappings = new List<ISMapping>();
        }

        //IS related
        public string ISComponent { get; set; }
        public string ISServiceName { get; set; }
        public string ISServiceType { get; set; }

        //0-Batch,1-Regular
        public string ISProcessingType { get; set; }
        public Boolean ISIntegService { get; set; } //for nested IS service

        //Caller
        public string CallerComponent { get; set; }
        public string CallerService { get; set; }

        [XmlArray("ISMappings")]
        [XmlArrayItem("ISMapping")]
        //mapping informations
        public List<ISMapping> Mappings { get; set; }
    }

    public class ISMapping
    {
        //IS
        public string ISSection { get; set; }
        public Segment ISSegment { get; set; }
        public DataItem ISDataItem { get; set; }

        //Caller
        public string CallerSection { get; set; }
        public Segment CallerSegment { get; set; }
        public DataItem CallerDataItem { get; set; }
    }


    /// <summary>
    /// 
    /// </summary>
    public class ControlExpression
    {
        //field
        public string _Expression;

        //property
        public string Expression
        {
            get
            {
                return _Expression;
            }
            set
            {
                if (!value.Equals("???") && !string.IsNullOrEmpty(value))
                {
                    if (value[0].Equals("."))
                        throw new Exception(string.Format("{0} ControlExpression for the BR not in proper format", Convert.ToString(value)));

                    _Expression = value;
                    IsValid = true;
                }
                else
                {
                    IsValid = false;
                }
            }
        }
        public bool IsValid { get; set; }
        public Segment Seg { get; set; }
        public DataItem DI { get; set; }
    }

    /// <summary>
    /// Input class ISDump Template
    /// </summary>
    public class ISDumpInfo
    {
        public string ComponentName { get; set; }
        public string ServiceName { get; set; }

        [XmlArray("Segments")]
        [XmlArrayItem("Segment")]
        public IEnumerable<string> Segments { get; set; }
        public string SourcePath { get; set; }
        public string ReleasePath { get; set; }
        public string AssemblyDescr { get; set; }
    }

    /// <summary>
    /// Status of the Compilation
    /// </summary>
    public enum GenerationStatus
    {
        NotStarted,
        InProgress,
        SourceCodeGenerated,
        Success,
        Failure,
        Copied
    }

    /// <summary>
    /// 
    /// </summary>
    public enum ObjectType
    {
        Activity,
        Service,
        Resource,
        ErrorHandler,
        Htm,
        ThirdParty,
        Script,
        IS,
        BR,
        Bulk,
        Report,
        Ext2,
        MHUB,
        Extjs6,
        MHUB2,
        TaskCallout,
        LayoutXml,
        ChartXml,
        StateXml,
        RuleStateXml,
        DesktopXml,
        PivotXml,
        ControlExtensionXml,
        ControlExtensionDataXml,
        DataDrivenTaskXml,
        ErrorLookupXml,
        Glance,
        UniversalPersonalization,
        MDCFTemplates
    }

    /// <summary>
    /// 
    /// </summary>
    internal struct FlowAttribute
    {
        public const string IN = "0";
        public const string OUT = "1";
        public const string INOUT = "2";
        public const string SCRATCH = "3";
    }


    /// <summary>
    /// DataType
    /// </summary>
    internal struct DataType
    {
        public const string LONG = "LONG";
        public const string INT = "INT";
        public const string INTEGER = "INTEGER";
        public const string STRING = "string";
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

    /// <summary>
    /// Section Processing type
    /// </summary>
    internal struct ProcessingType
    {
        public const string DEFAULT = "0";
        public const string ALTERNATE = "1";
    }

    /// <summary>
    ///BR execution flag
    /// </summary>
    internal struct ExecutionFlag
    {
        public const string CURRENT = "1";
        public const string NEW = "2";
        public const string LOCATE = "3";
    }

    /// <summary>
    /// BR Types based on Systemgenerated values
    /// </summary>
    internal struct BRTypes
    {
        public const string CUSTOM_BR = "0";
        public const string SYSTEMGENERATED = "1";
        public const string HANDCODED_BR = "2";
        public const string BULK_BR = "3";
    }

    /// <summary>
    /// Class to hold Error Info
    /// </summary>
    public class ErrorCollection
    {
        public ObjectType ObjectType;
        public string ObjectName;
        public string Compilationstring;
        public List<string> ErrorDesc;

        public ErrorCollection()
        {
            ErrorDesc = new List<string>();
        }

        public ErrorCollection(ObjectType type)
        {
            ObjectType = type;
            ErrorDesc = new List<string>();
        }

        public ErrorCollection(ObjectType type, string name, string cmdstring)
        {
            ObjectType = type;
            ObjectName = name;
            Compilationstring = cmdstring;
            ErrorDesc = new List<string>();
        }

        public void AddDescription(string desc)
        {
            ErrorDesc.Add(desc);
        }
    }

    /// <summary>   
    /// 
    /// </summary>
    internal class keyValuePairs
    {
        public string key { get; set; }
        public string value { get; set; }
    }

    internal enum ServiceType
    {
        Fetch,
        Update,
        FetchAndUpdate
    }


    public class ApiRequest
    {
        public string ParentSchemaName { get; set; }
        public string SchemaName { get; set; }
        public string SchemaCategory { get; set; }
        public Segment Segment { get; set; }
        public DataItem DataItem { get; set; }
        public string NodeID { get; set; }
        public string ParentNodeID { get; set; }
        public string Identifier { get; set; }
        public string Type { get; set; }
        public string DisplayName { get; set; }
        public string SchemaType { get; set; }
    }

    public class ApiResponse
    {
        public string MediaType { get; set; }
        public string ResponseCode { get; set; }
        public string ParentSchemaName { get; set; }
        public string SchemaName { get; set; }
        public string SchemaCategory { get; set; }
        public Segment Segment { get; set; }
        public DataItem DataItem { get; set; }
        public string NodeID { get; set; }
        public string ParentNodeID { get; set; }
        public string Identifier { get; set; }
        public string Type { get; set; }
        public string DisplayName { get; set; }
        public string SchemaType { get; set; }
    }

    public class ApiParameter
    {
        public string ParameterName { get; set; }
        public Segment Segment { get; set; }
        public DataItem DataItem { get; set; }

        //contains name of the method to use from corerequest.
        public string methodToUse { get; set; }

        private string _In;
        public string In
        {
            get
            {
                return this._In;
            }
            set
            {
                this._In = value;
                switch (value.ToLower())
                {
                    case "header":
                        methodToUse = "AddHeader";
                        break;
                    case "path":
                    case "pathoperation":
                        methodToUse = "AddPathParameter";
                        break;
                    case "query":
                        methodToUse = "AddQueryParameter";
                        break;
                }
            }
        }
    }
}
