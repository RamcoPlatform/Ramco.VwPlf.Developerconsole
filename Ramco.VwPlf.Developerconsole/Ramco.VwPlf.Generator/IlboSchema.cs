//-----------------------------------------------------------------------
// <copyright file="IlboSchema.cs" company="Ramco Systems">
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
// Bug id 	    : PLF2.0_04389
// Description  : Inclusion of eventname attribute in subscription link.
// By			: Vidhyalakshmi N R
// Date 		: 18-Apr-2013
//-----------------------------------------------------------------------
// Bug id 	    : TECH-18686
// Description  : event node not coming in model(Activity) xml
// By			: Madhan Sekar M
// Date 		: 09-Feb-2018
//-----------------------------------------------------------------------
// Bug id 	    : TECH-28611
// Description  : event node not coming if an ui have offline tasks alone
// By			: Madhan Sekar M
// Date 		: 27-Nov-2018
//-----------------------------------------------------------------------
// Bug id 	    : TECH-33808
// Description  : btsynonym name for control and view
// By			: Madhan Sekar M
// Date 		: 14-May-2019
//-----------------------------------------------------------------------
// </copyright>
namespace Ramco.VwPlf.Generator
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml.Linq;
    using Ramco.Plf.Global.Interfaces;
    using System.Xml.Serialization;

    /// <summary>
    /// To Generate Ilbo files.
    /// </summary>
    public class IlboSchema : Common
    {
        /// <summary>
        /// Initiate Ilbo generation
        /// </summary>
        /// <returns>Returns a value indicating whether this method executed properly.</returns>
        public bool SchemaHeader()
        {
            WriteProfiler("Activity generation starts.");
            string activityID = string.Empty;
            string activityName = string.Empty;
            string ilboCode = string.Empty;
            try
            {
                if (!GlobalVar.DataCollection.ContainsKey("ilbo"))
                {
                    return true;
                }

                var ilboDetails = from ilbos in GlobalVar.DataCollection["ilbo"]
                                  join act in GlobalVar.Activity.ToList()
                                  on ilbos.Attribute("activityname").Value equals act.ID.ToLower()
                                  select ilbos;

                foreach (XElement ilboDetail in ilboDetails)
                {
                    PlfScreenSchema schema = new PlfScreenSchema();
                    activityID = ilboDetail.Attribute("activityid").Value.ToString();
                    activityName = ilboDetail.Attribute("activityname").Value.ToString();
                    ilboCode = ilboDetail.Attribute("ilbocode").Value.ToString();
                    schema.ActivityName = activityName;
                    schema.ActivityDescription = ilboDetail.Attribute("activitydesc").Value.ToString();
                    schema.ComponentName = GlobalVar.Component.ToLower();
                    schema.UiName = ilboCode;
                    schema.Description = ilboDetail.Attribute("description").Value.ToString();
                    schema.DisplayUri = string.Format("../{0}_{1}.htm", activityName, ilboCode);
                    schema.UiVersion = "1.0.0.0";
                    schema.DataSavingTask = ilboDetail.Attribute("datasavingtask").Value.Equals("1") ? true : false;
                    WriteProfiler(string.Format("Generation starts for Activityid : {0}, Activityname : {1}, Ilbocode : {2}.", activityID, activityName, ilboCode));
                    this.AddControls(schema, activityID, ilboCode);
                    this.AddEvents(schema, activityID, ilboCode);
                    this.AddSubLinks(schema, activityID, ilboCode);
                    this.AddPubLinks(schema, activityID, ilboCode);
                    // new System.Xml.Serialization.XmlSerializer(typeof(PlfScreenSchema)).Serialize(new System.IO.StreamWriter(string.Format(@"{0}ilbo\scrn_{1}_{2}.xml", GlobalVar.ReleasePath, activityName, ilboCode)), schema); // Code commented for bug id: PLF2.0_03462
                    new System.Xml.Serialization.XmlSerializer(typeof(PlfScreenSchema)).Serialize(new System.IO.StreamWriter(string.Format(@"{0}\Model\{3}\scrn_{1}_{2}.xml", GlobalVar.ReleasePath, activityName, ilboCode, GlobalVar.Component)), schema); // Code added for bug id: PLF2.0_03462
                    WriteProfiler(string.Format("Generation ended for Activityid : {0}, Activityname : {1}, Ilbocode : {2}.", activityID, activityName, ilboCode));
                }

                WriteProfiler("Activity generation ends.");
                return true;
            }
            catch (Exception e)
            {
                WriteProfiler("Exception raised in Activity generation. Error", e.Message);
                return false;
            }
        }

        /// <summary>
        /// To assign the controls to Ilbo.
        /// </summary>
        /// <param name="schema">Ilbo object schema.</param>
        /// <param name="activityID">Acivity Namr</param>
        /// <param name="ilboCode">Ilbo Name</param>
        /// <returns>Returns a value indicating whether this method executed properly.</returns>
        private bool AddControls(PlfScreenSchema schema, string activityID, string ilboCode)
        {
            try
            {
                var controlDetails = from controls in GlobalVar.DataCollection["control"]
                                     where controls.Attribute("activityid").Value.Equals(activityID)
                                     && controls.Attribute("ilbocode").Value.Equals(ilboCode)
                                     select controls;
                List<PlfControlSchema> control = new List<PlfControlSchema>();
                control.Add(new PlfControlSchema { ControlName = "rvwrt_cctxt_ou", ControlType = "Edit", Hidden = true, Disabled = false, PageSize = 1, Tabname = string.Empty, SystemControl = false, BTSynonymName = "rvwrt_cctxt_ou" });
                WriteProfiler(string.Format("\t Controlname : rvwrt_cctxt_ou added to Ilbocode : {0}.", ilboCode));
                control.Add(new PlfControlSchema { ControlName = "rvwrt_lctxt_ou", ControlType = "Edit", Hidden = true, Disabled = false, PageSize = 1, Tabname = string.Empty, SystemControl = false, BTSynonymName = "rvwrt_lctxt_ou" });
                WriteProfiler(string.Format("\t Controlname : rvwrt_lctxt_ou added to Ilbocode : {0}.", ilboCode));
                foreach (XElement controlDetail in controlDetails)
                {
                    control.Add(new PlfControlSchema
                    {
                        ControlName = controlDetail.Attribute("controlname").Value.ToString(),
                        ControlType = controlDetail.Attribute("controltype").Value.ToString(),
                        Hidden = controlDetail.Attribute("hidden").Value.ToString().Equals("true"),
                        Disabled = controlDetail.Attribute("disabled").Value.ToString().Equals("true"),
                        PageSize = Convert.ToInt64(controlDetail.Attribute("pagesize").Value.ToString()),
                        Tabname = controlDetail.Attribute("tabname").Value.ToString(),
                        SystemControl = controlDetail.Attribute("systemcontrol").Value.ToString().Equals("true"),
                        BTSynonymName = controlDetail.Attribute("btsynonym")!=null?controlDetail.Attribute("btsynonym").Value.ToString(): string.Empty //TECH-33808
                    });
                    WriteProfiler(string.Format("\t Controlname : {1} added to Ilbocode : {0}.", ilboCode, controlDetail.Attribute("controlname").Value.ToString()));
                }

                schema.Controls = control.ToArray();
                this.AddViewsToControls(schema, activityID, ilboCode);
                return true;
            }
            catch (Exception e)
            {
                WriteProfiler("Exception in adding controls", e.Message);
                return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="schema">Ilbo object schema.</param>
        /// <param name="activityID">Acivity Namr</param>
        /// <param name="ilboCode">Ilbo Name</param>
        /// <returns>Returns a value indicating whether this method executed properly.</returns>
        private bool AddViewsToControls(PlfScreenSchema schema, string activityID, string ilboCode)
        {
            try
            {
                foreach (PlfControlSchema control in schema.Controls)
                {
                    List<PlfViewSchema> viewList = new List<PlfViewSchema>();
                    if (!(control.ControlName.Equals("rvwrt_cctxt_ou") || control.ControlName.Equals("rvwrt_lctxt_ou")))
                    {
                        var viewDetails = from view in GlobalVar.DataCollection["view"]
                                          where view.Attribute("activityid").Value.Equals(activityID)
                                          && view.Attribute("ilbocode").Value.Equals(ilboCode)
                                          && view.Attribute("controlname").Value.Equals(control.ControlName)
                                          select view;
                        foreach (XElement viewDetail in viewDetails)
                        {
                            viewList.Add(new PlfViewSchema
                            {
                                ViewName = viewDetail.Attribute("viewname").Value.ToString(),
                                DataLength = Convert.ToInt32(viewDetail.Attribute("datalength").Value.ToString()),
                                DataType = viewDetail.Attribute("datatype").Value.ToString(),
                                HeaderText = viewDetail.Attribute("headertext").Value.ToString(),
                                HelpText = viewDetail.Attribute("helptext").Value.ToString(),
                                IsHidden = viewDetail.Attribute("ishidden").Value.ToString().Equals("true"),
                                IsDisabled = viewDetail.Attribute("isdisabled").Value.ToString().Equals("true"),
                                IsMultiselect = viewDetail.Attribute("ismultiselect").Value.ToString().Equals("true"),
                                IsReadOnly = viewDetail.Attribute("isreadonly").Value.ToString().Equals("true"),
                                LinkedView = viewDetail.Attribute("linkedview").Value.ToString(),
                                ViewSequence = Convert.ToInt32(viewDetail.Attribute("viewsequence").Value.ToString()),
                                ViewType = viewDetail.Attribute("viewtype").Value.ToString(),
                                ListValues = viewDetail.Attribute("listvalues").Value.ToString(),
                                Default = viewDetail.Attribute("defaultval").Value.ToString(), // Code added for bug id: PLF2.0_03462
                                PrecisionName = viewDetail.Attribute("precisionname").Value.ToString(),
                                BTSynonymName =viewDetail.Attribute("btsynonym")!=null? viewDetail.Attribute("btsynonym").Value.ToString():string.Empty //TECH-33808
                            });
                            WriteProfiler(string.Format("\t\t Viewname : '{2}' added to '{1}' control in Ilbocode : {0}.", ilboCode, control.ControlName, viewDetail.Attribute("viewname").Value.ToString()));
                        }
                    }
                    else
                    {
                        // viewList.Add(new PlfViewSchema { ViewName = control.ControlName, DataLength = 25, DataType = "String", HeaderText = string.Empty, HelpText = string.Empty, IsHidden = false, IsDisabled = false, IsMultiselect = false, IsReadOnly = false, LinkedView = string.Empty, ViewSequence = 1, ViewType = "Edit", ListValues = string.Empty }); // Code commented for bug id: PLF2.0_03462
                        viewList.Add(new PlfViewSchema { ViewName = control.ControlName, DataLength = 25, DataType = "String", HeaderText = string.Empty, HelpText = string.Empty, IsHidden = false, IsDisabled = false, IsMultiselect = false, IsReadOnly = false, LinkedView = string.Empty, ViewSequence = 1, ViewType = "Edit", ListValues = string.Empty, Default = string.Empty,BTSynonymName= control.ControlName }); // Code added for bug id: PLF2.0_03462
                        WriteProfiler(string.Format("\t\t Viewname : '{2}' added to '{1}' control in Ilbocode : {0}.", ilboCode, control.ControlName, control.ControlName));
                    }


                    control.Views = viewList.ToArray();
                }

                return true;
            }
            catch (Exception e)
            {
                WriteProfiler("Exception in adding views to controls", e.Message);
                return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="schema">Ilbo object schema.</param>
        /// <param name="activityID">Acivity Namr</param>
        /// <param name="ilboCode">Ilbo Name</param>
        /// <returns>Returns a value indicating whether this method executed properly.</returns>
        private bool AddEvents(PlfScreenSchema schema, string activityID, string ilboCode)
        {
            try
            {
                //TECH-28611
                IEnumerable<XElement> eventDetails = new List<XElement>();

                //TECH-18686 *starts*
                IEnumerable<XElement> offlineevents = new List<XElement>();

                if (GlobalVar.DataCollection.ContainsKey("offlineevents"))
                {
                    offlineevents = from evt in GlobalVar.DataCollection["offlineevents"]
                                    where evt.Attribute("activityid").Value.Equals(activityID)
                                    && evt.Attribute("ilbocode").Value.Equals(ilboCode)
                                    select evt;
                }

                List<PlfEventSchema> events = new List<PlfEventSchema>();

                foreach (XElement offlineevent in offlineevents)
                {
                    int i;
                    events.Add(new PlfEventSchema
                    {
                        EventName = offlineevent.Attribute("eventname").Value.ToString(),
                        EventType = offlineevent.Attribute("eventtype").Value.ToString(),
                        NavigationKey = ((int.TryParse(offlineevent.Attribute("navigationkey").Value.ToString(), out i) == true) ? i.ToString() : string.Empty),
                        AssociatedService = string.Empty,
                        Mode = offlineevent.Attribute("eventmode").Value.ToString()

                    });
                    WriteProfiler(string.Format("\t Eventname : {1} added to Ilbocode : {0}.", ilboCode, offlineevent.Attribute("eventname").Value.ToString()));
                }
                //TECH-18686 *ends*

                //TECH-28611 condition added
                if (GlobalVar.DataCollection.ContainsKey("event"))
                {
                    eventDetails = from evt in GlobalVar.DataCollection["event"]
                                   where evt.Attribute("activityid").Value.Equals(activityID)
                                   && evt.Attribute("ilbocode").Value.Equals(ilboCode)
                                   select evt;
                }

                foreach (XElement eventDetail in eventDetails)
                {
                    if (offlineevents.Where(oe => string.Compare(eventDetail.Attribute("eventname").Value.ToString(), oe.Attribute("eventname").Value.ToString(), true) == 0).Any() == false)
                    {
                        events.Add(new PlfEventSchema
                        {
                            EventName = eventDetail.Attribute("eventname").Value.ToString(),
                            EventType = eventDetail.Attribute("eventtype").Value.ToString(),
                            NavigationKey = eventDetail.Attribute("navigationkey").Value.ToString(),
                            AssociatedService = eventDetail.Attribute("associatedservice").Value.ToString(),
                            Mode = string.Empty

                        });
                        WriteProfiler(string.Format("\t Eventname : {1} added to Ilbocode : {0}.", ilboCode, eventDetail.Attribute("eventname").Value.ToString()));
                    }
                }

                schema.Events = events.ToArray();
                return true;
            }
            catch (Exception e)
            {
                WriteProfiler("Exception in adding events", e.Message);
                return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="schema">Ilbo object schema.</param>
        /// <param name="activityID">Acivity Namr</param>
        /// <param name="ilboCode">Ilbo Name</param>
        /// <returns>Returns a value indicating whether this method executed properly.</returns>
        private bool AddSubLinks(PlfScreenSchema schema, string activityID, string ilboCode)
        {
            try
            {
                var linkDetails = from link in GlobalVar.DataCollection["link"]
                                  where link.Attribute("activityid").Value.Equals(activityID)
                                  && link.Attribute("ilbocode").Value.Equals(ilboCode)
                                  select link;

                List<PlfSubscriberSchema> links = new List<PlfSubscriberSchema>();
                foreach (XElement linkDetail in linkDetails)
                {
                    links.Add(new PlfSubscriberSchema
                    {
                        NavigationKey = linkDetail.Attribute("linkid").Value.ToString(),
                        TargetComponent = linkDetail.Attribute("targetcomponent").Value.ToString().ToLower(),
                        TargetActivity = linkDetail.Attribute("targetactivity").Value.ToString().ToLower(),
                        TargetScreen = linkDetail.Attribute("targetscreen").Value.ToString().ToLower()
                    });
                    WriteProfiler(string.Format("\t Linkid : {1} added to Ilbocode : {0}.", ilboCode, linkDetail.Attribute("linkid").Value.ToString()));
                }

                schema.Subscribers = links.ToArray();
                this.AddSubToLinks(schema, activityID, ilboCode);
                return true;
            }
            catch (Exception e)
            {
                WriteProfiler("Exception in adding links", e.Message);
                return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="schema">Ilbo object schema.</param>
        /// <param name="activityID">Acivity Namr</param>
        /// <param name="ilboCode">Ilbo Name</param>
        /// <returns>Returns a value indicating whether this method executed properly.</returns>
        private bool AddSubToLinks(PlfScreenSchema schema, string activityID, string ilboCode)
        {
            try
            {
                foreach (PlfSubscriberSchema links in schema.Subscribers)
                {
                    if (string.IsNullOrEmpty(links.NavigationKey).Equals(false))
                    {
                        List<PlfNavigationSchemaItems> navigationItems = new List<PlfNavigationSchemaItems>();
                        var subtolinkDetails = from subtolinks in GlobalVar.DataCollection["subtolink"]
                                               where subtolinks.Attribute("ilbocode").Value.Equals(ilboCode)
                                                  && subtolinks.Attribute("linkid").Value.Equals(links.NavigationKey)
                                               select subtolinks;
                        foreach (XElement subtolinkDetail in subtolinkDetails)
                        {
                            navigationItems.Add(new PlfNavigationSchemaItems
                            {
                                ControlName = subtolinkDetail.Attribute("controlname").Value.ToString(),
                                ViewName = subtolinkDetail.Attribute("viewname").Value.ToString(),
                                Flowdirection = Convert.ToInt32(subtolinkDetail.Attribute("flowdirection").Value.ToString()),
                                IsMultiInstance = subtolinkDetail.Attribute("ismultiinstance").Value.ToString().Equals("true"),
                                ItemName = subtolinkDetail.Attribute("itemname").Value.ToString(),
                                Event = subtolinkDetail.Attribute("event").Value.ToString().ToLower()   //code added for bug id: PLF2.0_04389
                            });
                            WriteProfiler(string.Format("\t\t Subscription link details : {2} added to linkid : {1} in Ilbocode : {0}.", ilboCode, links.NavigationKey, subtolinkDetail.Attribute("controlname").Value.ToString()));
                        }

                        links.NavigationItems = navigationItems.ToArray();
                    }
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
        /// <param name="schema">Ilbo object schema.</param>
        /// <param name="activityID">Acivity Namr</param>
        /// <param name="ilboCode">Ilbo Name</param>
        /// <returns>Returns a value indicating whether this method executed properly.</returns>
        private bool AddPubLinks(PlfScreenSchema schema, string activityID, string ilboCode)
        {
            try
            {
                var pubLinkDetails = from publinks in GlobalVar.DataCollection["publink"]
                                     where publinks.Attribute("activityid").Value.Equals(activityID)
                                          && publinks.Attribute("ilbocode").Value.Equals(ilboCode)
                                     select publinks;

                List<PlfPublisherSchema> links = new List<PlfPublisherSchema>();
                foreach (XElement pubLinkDetail in pubLinkDetails)
                {
                    links.Add(new PlfPublisherSchema { NavigationKey = pubLinkDetail.Attribute("linkid").Value.ToString() });
                    WriteProfiler(string.Format("\t Publish linkid : {1} added to Ilbocode : {0}.", ilboCode, pubLinkDetail.Attribute("linkid").Value.ToString()));
                }

                schema.Publishers = links.ToArray();
                this.AddPubToLinks(schema, activityID, ilboCode);
                return true;
            }
            catch (Exception e)
            {
                WriteProfiler("Exception in adding links", e.Message);
                return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="schema">Ilbo object schema.</param>
        /// <param name="activityID">Acivity Namr</param>
        /// <param name="ilboCode">Ilbo Name</param>
        /// <returns>Returns a value indicating whether this method executed properly.</returns>
        private bool AddPubToLinks(PlfScreenSchema schema, string activityID, string ilboCode)
        {
            try
            {
                foreach (PlfPublisherSchema links in schema.Publishers)
                {
                    if (string.IsNullOrEmpty(links.NavigationKey).Equals(false))
                    {
                        List<PlfNavigationSchemaItems> navigationItems = new List<PlfNavigationSchemaItems>();
                        var pubToLinkDetails = from pubtolinks in GlobalVar.DataCollection["pubtolinks"]
                                               where pubtolinks.Attribute("ilbocode").Value.Equals(ilboCode)
                                                  && pubtolinks.Attribute("linkid").Value.Equals(links.NavigationKey)
                                               select pubtolinks;
                        foreach (XElement pubToLinkDetail in pubToLinkDetails)
                        {
                            navigationItems.Add(new PlfNavigationSchemaItems
                            {
                                ControlName = pubToLinkDetail.Attribute("controlname").Value.ToString(),
                                ViewName = pubToLinkDetail.Attribute("viewname").Value.ToString(),
                                Flowdirection = Convert.ToInt32(pubToLinkDetail.Attribute("flowdirection").Value.ToString()),
                                IsMultiInstance = pubToLinkDetail.Attribute("ismultiinstance").Value.ToString().Equals("true"),
                                ItemName = pubToLinkDetail.Attribute("itemname").Value.ToString()
                            });
                            WriteProfiler(string.Format("\t\t Publish link details : {2} added to linkid : {1} in Ilbocode : {0}.", ilboCode, links.NavigationKey, pubToLinkDetail.Attribute("controlname").Value.ToString()));
                        }

                        links.NavigationItems = navigationItems.ToArray();
                    }
                }

                return true;
            }
            catch (Exception e)
            {
                WriteProfiler("Exception in adding Subcription to Links", e.Message);
                return false;
            }
        }
    }
}
