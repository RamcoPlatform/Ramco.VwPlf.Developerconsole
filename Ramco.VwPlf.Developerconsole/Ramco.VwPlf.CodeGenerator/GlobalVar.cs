using System;
using Ramco.VwPlf.DataAccess;
using System.Collections.Generic;
using System.Collections;
using System.Diagnostics;

namespace Ramco.VwPlf.CodeGenerator
{
        public class ECRLevelOptions
    {
        public ECRLevelOptions()
        {
            //dbManager = null;
            generation = new Generation();
            ErrorCollection = new List<Error>();

            List<string> LangIdList = new List<string>();
            //ErrorCollection = new ErrorCollection();

            //to prevent error while generating from xml
            this.Model = string.Empty;
            this.User = string.Empty;
            this.DB = string.Empty;
        }
        public codegeneration OptionXML { get; set; }
        public Generation generation { get; set; }
        public List<Error> ErrorCollection { get; set; }
        public string Model { get; set; }
        public string User { get; set; }
        public string DB { get; set; }
        public string Customer { get; set; }
        public string Project { get; set; }
        public string Ecrno { get; set; }
        public string Process { get; set; }
        public string Component { get; set; }
        public string ComponentDesc { get; set; }
        public string Appliation_rm_type { get; set; }
        public string PreviousGenerationPath { get; set; }
        public string GenerationPath { get; set; }
        public string Platform { get; set; }
        public string RequestId { get; set; }
        public string CodegenClient { get; set; }
        public string RequestStart_Datetime { get; set; }
        public string EncodedConnectionstring { get; set; }
        public Boolean Webxml { get; set; }
        public Boolean Extjs { get; set; }
        public Boolean ErrorLookup { get; set; }
        public string AssemblyDescription { get; set; }

        public Boolean GenerateService { get; set; }
        public Boolean GenerateActivity { get; set; }

        public Boolean SeperateErrorDll { get; set; }
        public Boolean InTD { get; set; }
        public List<string> LangIdList { get; set; }
        //public Boolean ChartXml { get; set; }
        public String ReleaseVersion { get; set; }
        public string CurrentIlbo { get; set; }
        public bool IsReportAspxGenerated { get; set; }
        public bool TreatDefaultAsNull { get; set; }

        private string _thirdPartyGeneratorSourcePath = System.Configuration.ConfigurationManager.AppSettings["3rdPartyGenerators"];
        private string _referenceDllSourcePath = System.Configuration.ConfigurationManager.AppSettings["CompilationAssemblies"];

        public string ThirdPartyGeneratorSourcePath
        {
            get
            {
                return _thirdPartyGeneratorSourcePath;
            }
        }
        public string ReferenceDllSourcePath
        {
            get
            {
                return _referenceDllSourcePath;
            }
        }
    }

    public class Error
    {
        public ObjectType Type { get; set; }
        public String Name { get; set; }
        public String Description { get; set; }

        public Error()
        {

        }

        public Error(ObjectType type, String name, String desc)
        {
            this.Type = type;
            this.Name = name;
            this.Description = desc;
        }
    }
}
