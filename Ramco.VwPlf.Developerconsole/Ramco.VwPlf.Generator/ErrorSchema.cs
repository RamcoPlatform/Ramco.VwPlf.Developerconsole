//-----------------------------------------------------------------------
// <copyright file="ErrorSchema.cs" company="Ramco Systems">
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
// Bug id 	    : PLF2.0_03803
// Description 	: For Error Place Holders ^ Control Caption should be part
//                of Error Message
// By			: Vidhyalakshmi N R
// Date 		: 09-Apr-2013
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
    using System.Text.RegularExpressions;   // Code added for bug id: PLF2.0_03803

    /// <summary>
    /// To Generate Error files.
    /// </summary>
    public class ErrorSchema : Common
    {
        /// <summary>
        /// Initiate Error generation
        /// </summary>
        /// <returns>Returns a value indicating whether this method executed properly.</returns>
        public bool SchemaHeader()
        {
            try
            {
                string serviceName = string.Empty;
                if (!GlobalVar.DataCollection.ContainsKey("error"))
                {
                    return true;
                }

                var errorDetails = from errors in GlobalVar.DataCollection["error"]
                                   select errors;

                foreach (XElement errorDetail in errorDetails)
                {
                    EhsInfo ehsInfo = new EhsInfo();
                    serviceName = errorDetail.Attribute("servicename").Value.ToString();
                    ehsInfo.MethodCnt = Convert.ToByte(errorDetail.Attribute("count").Value.ToString());
                    var methodDetails = from methods in GlobalVar.DataCollection["errormethod"]
                                        where methods.Attribute("servicename").Value.Equals(serviceName)
                                        select methods;
                    List<EhsInfoMethod> ehsInfoMethod = new List<EhsInfoMethod>();
                    WriteProfiler("Generating Error code for the service" + serviceName);
                    foreach (XElement methodDetail in methodDetails)
                    {
                        ehsInfoMethod.Add(new EhsInfoMethod
                        {
                            id = Convert.ToInt32(methodDetail.Attribute("methodid").Value.ToString()),
                            name = methodDetail.Attribute("methodname").Value.ToString()
                        });
                    }

                    ehsInfo.Method = ehsInfoMethod.ToArray();
                    this.AddMethodToSp(ehsInfo, serviceName, "1");
                    // new System.Xml.Serialization.XmlSerializer(typeof(EhsInfo)).Serialize(new System.IO.StreamWriter(string.Format(@"{0}\service\1\ehs_{1}.xml", GlobalVar.ReleasePath, serviceName.ToLower())), ehsInfo); // Code commented for bug id: PLF2.0_03462
                    new System.Xml.Serialization.XmlSerializer(typeof(EhsInfo)).Serialize(new System.IO.StreamWriter(string.Format(@"{0}\ServiceXML\{2}\1\ehs_{1}.xml", GlobalVar.ReleasePath, serviceName.ToLower(), GlobalVar.Component)), ehsInfo); // Code added for bug id: PLF2.0_03462
                }

                return true;
            }
            catch (Exception e)
            {
                WriteProfiler("General exception in Error schemaheader", e.Message);
                return false;
            }
        }

        /// <summary>
        /// To assign the Sp to method.
        /// </summary>
        /// <param name="ehsInfo">Error information object.</param>
        /// <param name="service">Service Name</param>
        /// <param name="langId">Language Id</param>
        /// <returns>Returns a value indicating whether this method executed properly.</returns>
        private bool AddMethodToSp(EhsInfo ehsInfo, string service, string langId)
        {
            try
            {
                foreach (EhsInfoMethod method in ehsInfo.Method)
                {
                    var messageDetails = from messages in GlobalVar.DataCollection["errormsg"]
                                         where messages.Attribute("servicename").Value.Equals(service)
                                          && messages.Attribute("methodid").Value.Equals(method.id.ToString())
                                          && messages.Attribute("langid").Value.Equals(langId)
                                         select messages;
                    List<EhsInfoMethodSP> ehsInfoMethodSP = new List<EhsInfoMethodSP>();
                    foreach (XElement messageDetail in messageDetails)
                    {
                        ehsInfoMethodSP.Add(new EhsInfoMethodSP
                                            {
                                                errorID = Convert.ToInt32(messageDetail.Attribute("errorid").Value.ToString()),
                                                brErrorID = Convert.ToInt32(messageDetail.Attribute("brerrorid").Value.ToString()),
                                                errorMsg = messageDetail.Attribute("errormsg").Value.ToString(),
                                                focusSegName = messageDetail.Attribute("focussegname").Value.ToString(),
                                                focusDIName = messageDetail.Attribute("focusdiname").Value.ToString(),
                                                correctiveMsg = messageDetail.Attribute("correctivemsg").Value.ToString(),
                                                severity = this.GetSeverity(messageDetail.Attribute("severity").Value.ToString())
                                            });
                    }

                    method.SP = ehsInfoMethodSP.ToArray();
                    this.AddPlaceHolderToErrors(method, service, method.id.ToString(), langId);
                }

                return true;
            }
            catch (Exception e)
            {
                WriteProfiler("Exception in adding error id to methods", e.Message);
                return false;
            }
        }

        /// <summary>
        /// To assign placeholder to errors.
        /// </summary>
        /// <param name="method">Method Name</param>
        /// <param name="service">Service Name</param>
        /// <param name="methodID">Method Id</param>
        /// <param name="langId">Language Id</param>
        /// <returns>Returns a value indicating whether this method executed properly.</returns>
        private bool AddPlaceHolderToErrors(EhsInfoMethod method, string service, string methodID, string langId)
        {
            try
            {
                foreach (EhsInfoMethodSP ehsInfoMethodSP in method.SP)
                {
                    int seqNo = 0;
                    var pholderdetails = from messagesph in GlobalVar.DataCollection["errormsgph"]
                                         where messagesph.Attribute("servicename").Value.Equals(service)
                                          && messagesph.Attribute("methodid").Value.Equals(method.id.ToString())
                                          && messagesph.Attribute("errorid").Value.Equals(ehsInfoMethodSP.errorID.ToString())
                                         select messagesph;
                    List<EhsInfoMethodSPPH> ehsInfoMethodSPPH = new List<EhsInfoMethodSPPH>();
                    foreach (XElement pholderdetail in pholderdetails)
                    {
                        ehsInfoMethodSPPH.Add(new EhsInfoMethodSPPH
                                                    {
                                                        segName = pholderdetail.Attribute("segname").Value.ToString(),
                                                        seqNo = seqNo,
                                                        diName = pholderdetail.Attribute("diname").Value.ToString(),
                                                        value = pholderdetail.Attribute("shortpltext").Value.ToString(),
                                                    });
                        // Code commented for bug id: PLF2.0_03803 ***start***
                        //ehsInfoMethodSP.errorMsg = ehsInfoMethodSP.errorMsg.Replace("<" + pholderdetail.Attribute("placeholdername").Value.ToString() + ">", "{" + seqNo.ToString() + "}");
                        //ehsInfoMethodSP.errorMsg = ehsInfoMethodSP.errorMsg.Replace("^" + pholderdetail.Attribute("placeholdername").Value.ToString() + "!", pholderdetail.Attribute("shortpltext").Value.ToString());
                        // Code commented for bug id: PLF2.0_03803 ***end***
                        // Code added for bug id: PLF2.0_03803 ***start***
                        ehsInfoMethodSP.errorMsg = Regex.Replace(ehsInfoMethodSP.errorMsg, "<" + pholderdetail.Attribute("placeholdername").Value.ToString() + ">", "{" + seqNo.ToString() + "}", RegexOptions.IgnoreCase);
                        ehsInfoMethodSP.errorMsg = Regex.Replace(ehsInfoMethodSP.errorMsg, Regex.Escape("^" + pholderdetail.Attribute("placeholdername").Value.ToString() + "!"), pholderdetail.Attribute("shortpltext").Value.ToString(), RegexOptions.IgnoreCase);
                        // Code added for bug id: PLF2.0_03803 ***end***

                        seqNo++;
                    }

                    ehsInfoMethodSP.PH = ehsInfoMethodSPPH.ToArray();
                }

                return true;
            }
            catch (Exception e)
            {
                WriteProfiler("Exception in adding placeholder to error id", e.Message);
                return false;
            }
        }

        /// <summary>
        /// To get the error severity.
        /// </summary>
        /// <param name="severity">severity number.</param>
        /// <returns>returns the error severity.</returns>
        private SeverityType GetSeverity(string severity)
        {
            return severity.Equals("5") ? SeverityType.Stop : SeverityType.Continue;
        }
    }
}
