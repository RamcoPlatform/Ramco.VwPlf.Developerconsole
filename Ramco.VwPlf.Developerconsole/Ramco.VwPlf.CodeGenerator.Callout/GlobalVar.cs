using System;
using System.Collections.Generic;
using Ramco.VwPlf.DataAccess;
using System.Data;

namespace Ramco.VwPlf.CodeGenerator.Callout
{
    internal class GlobalVar
    {
        //variable for storing Option xml values
        public static String Customer { get; set; }
        public static String Project { get; set; }
        public static String Ecrno { get; set; }
        public static String Process { get; set; }
        public static String Component { get; set; }
        public static String GenerationPath { get; set; }
        public static String SourcePath { get; set; }
        public static String ReleasePath { get; set; }
        public static String LogPath { get; set; }
        public static String Platform { get; set; }
        public static DataTable BtInfo { get; set; }
    }
}
