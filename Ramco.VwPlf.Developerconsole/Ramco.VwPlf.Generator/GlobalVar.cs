//-----------------------------------------------------------------------
// <copyright file="GlobalVar.cs" company="Ramco Systems">
//     Copyright (c) . All rights reserved.
// Release id 	: PLF2.0_02398
// Description 	: Coded for desktop generator.
// By			: Karthikeyan V S
// Date 		: 23-Nov-2012
// </copyright>
//-----------------------------------------------------------------------
namespace Ramco.VwPlf.Generator
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Xml.Linq;

    public static class GlobalVar
    {
        public static System.Data.SqlClient.SqlConnection oConnection = null;
        public static System.Data.DataView ControlDetails = null;
        public static Dictionary<string, IQueryable<XElement>> DataCollection = new Dictionary<string, IQueryable<XElement>>();
        public static Dictionary<int, string> ProfilerLog = null;
        public static IList<ListObject> Activity = null;
        public static IList<ListObject> Ui = null; //TECH-16278
        public static int Key = 0;

        public static bool IlboSchema { get; set; }

        public static bool ActivityServiceSchema { get; set; }
        
        public static bool ServiceSchema { get; set; }

        public static bool ServiceSchemaJson { get; set; }

        public static bool ErrorSchema { get; set; }

        public static bool UISchema { get; set; }

        public static bool Snippet { get; set; }

        /// <summary>
        /// Gets or sets QuickLink
        /// </summary>
        public static bool QuickLink { get; set; }

        /// <summary>
        /// Gets or sets Tabs
        /// </summary>        
        public static bool Tabs { get; set; }

        /// <summary>
        /// Gets or sets Trace
        /// </summary>        
        public static bool Trace { get; set; }

        /// <summary>
        /// Gets or sets Log
        /// </summary>        
        public static bool Log { get; set; }

        /// <summary>
        /// Gets or sets Platform
        /// </summary>        
        public static string Platform { get; set; }

        /// <summary>
        /// Gets or sets Customer
        /// </summary>        
        public static string Customer { get; set; }

        /// <summary>
        /// Gets or sets Project
        /// </summary>        
        public static string Project { get; set; }

        /// <summary>
        /// Gets or sets Ecrno
        /// </summary>        
        public static string Ecrno { get; set; }

        /// <summary>
        /// Gets or sets Component
        /// </summary>        
        public static string Component { get; set; }

        /// <summary>
        /// Gets or sets ConnectionString
        /// </summary>        
        public static string ConnectionString { get; set; }

        /// <summary>
        /// Gets or sets ReleasePath
        /// </summary>        
        public static string ReleasePath { get; set; }

        /// <summary>
        /// Gets or sets GenerationPath
        /// </summary>        
        public static string GenerationPath { get; set; }

        /// <summary>
        /// Gets or sets LogFilePath
        /// </summary>        
        public static string LogFilePath { get; set; }

        /// <summary>
        /// Gets or sets Query
        /// </summary>        
        public static string Query { get; set; }

        /// <summary>
        /// Gets or sets MHub2 option
        /// </summary>
        public static bool MHub2 { get; set; }
    }
}
