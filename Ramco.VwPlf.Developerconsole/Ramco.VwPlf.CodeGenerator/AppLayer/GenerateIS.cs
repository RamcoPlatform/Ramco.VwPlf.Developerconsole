using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Data;

namespace Ramco.VwPlf.CodeGenerator.AppLayer
{
    class GenerateIS
    {
        //Constants
        private const string CONTEXT_SEGMENT = "fw_context";
        private const string ERR_SEGMENT_NAME = "errordetails";
        private const string SYSFPROWNO = "sysfprowno";
        Logger _logger;
        ECRLevelOptions _ecrOptions;

        public GenerateIS(ECRLevelOptions ecrOptions)
        {
            _ecrOptions = ecrOptions;
            _logger = new Logger(Path.Combine(_ecrOptions.GenerationPath, _ecrOptions.Platform, _ecrOptions.Customer, _ecrOptions.Project, _ecrOptions.Ecrno, string.Format("{0}.txt", _ecrOptions.Ecrno)));
        }

        /// <summary>
        /// Generates dummy cs file for IS service
        /// </summary>
        /// <param name="IS"></param>
        private ISDumpInfo Generate_ISDumpClass(Service service,ref ISDumpInfo isDumpInfo)
        {
            try
            {
                string sAssemblyFile = Path.Combine(Path.GetDirectoryName(isDumpInfo.SourcePath), "ABY.cs");

                #region create IS service assembly description
                StringBuilder sbAssemblyDesc = new StringBuilder();
                sbAssemblyDesc.AppendLine("using System.Reflection;");
                sbAssemblyDesc.AppendLine(String.Format("[assembly:AssemblyKeyFile(\"{0}\")]", @"c:\\temp\\bin\\snk\\RVWSnk.snk"));
                sbAssemblyDesc.AppendLine(String.Format("[assembly:AssemblyVersion(\"{0}\")]", "1.4.3.12"));
                if (service.HasISinSameComponent == false)
                {
                    File.WriteAllText(sAssemblyFile, sbAssemblyDesc.ToString());
                }
                else
                {
                    isDumpInfo.AssemblyDescr = sbAssemblyDesc.ToString();
                }
                #endregion

                #region create IS service dump class
                ISDumpTemplate _ISDumpTemplate = new ISDumpTemplate(isDumpInfo);
                _ISDumpTemplate.ecrOptions = this._ecrOptions;
                String sISDumpClass = _ISDumpTemplate.TransformText();
                File.WriteAllText(isDumpInfo.SourcePath, sISDumpClass);
                #endregion

                return isDumpInfo;
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("Problem While generating ISDump for the Service {0} ---> {1}", service.Name, !Object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }

        public bool Generate(ref Service service)
        {
            bool bSuccessFlg = false;
            try
            {
                foreach (Method mt in (from ps in service.ProcessSections
                                       from mt in ps.Methods
                                       where mt.IsIntegService == true
                                       select mt))
                {
                    try
                    {
                        //IS
                        IntegrationService IS = mt.IS;
                        _logger.WriteLogToFile(string.Empty, string.Format("Generating IS Dump - {0}", IS.ISServiceName));

                        //Validating for migration problem
                        if (string.IsNullOrEmpty(IS.ISComponent))
                            throw new Exception(string.Format("{0} service is unavailable under same customer-project.Migrate it!", IS.ISServiceName));

                        //Form IS source and release path
                        string sISDumpSrcPath = Path.Combine(Path.GetDirectoryName(service.SourcePath), "IS", IS.ISComponent.ToLowerInvariant(), "S", String.Format("C{0}.cs", IS.ISServiceName.ToLower()));
                        string sISDumpRelPath = Path.Combine(Path.GetDirectoryName(service.SourcePath), "IS", IS.ISComponent.ToLower() == _ecrOptions.Component.ToLower() ? ("C" + IS.ISServiceName.ToLower() + ".netmodule") : (IS.ISComponent.ToLower() + ".dll"));

                        //create directory
                        Common.CreateDirectory(Path.GetDirectoryName(sISDumpSrcPath));

                        //getting is mapping details
                        ISDumpInfo _ISDumpInfo = new ISDumpInfo
                        {
                            ComponentName = IS.ISComponent.ToLowerInvariant(),
                            ServiceName = IS.ISServiceName.ToLowerInvariant(),
                            Segments = (from drISMapping in _ecrOptions.generation.ServiceInfo.Tables["ISMappingDetails"].AsEnumerable()
                                        where string.Compare(drISMapping.Field<string>("integservicename"), IS.ISServiceName, true) == 0
                                        && string.Compare(drISMapping.Field<string>("issegname"), CONTEXT_SEGMENT, true) != 0
                                        && string.Compare(drISMapping.Field<string>("issegname"), ERR_SEGMENT_NAME, true) != 0
                                        select drISMapping.Field<string>("issegname").ToUpper()).Distinct(),
                            SourcePath = sISDumpSrcPath,
                            ReleasePath = sISDumpRelPath,
                            AssemblyDescr = string.Empty
                        };
                        service.HasISinSameComponent = string.Compare(_ecrOptions.Component, IS.ISComponent, true) == 0 ? true : false;

                        //generation is necessary for user selected service alone.
                        if (service.IsSelected == true)
                        {
                            //generating is dump class - actual generation
                            Generate_ISDumpClass(service, ref _ISDumpInfo);
                        }

                        //map is dump info to the method
                        mt.ISDump = _ISDumpInfo;
                    }
                    catch (Exception inex)
                    {
                        _logger.WriteLogToFile("GenerateIS->Generate", String.Format("Problem While generating ISDump for the Service {0} ---> {1}", service.Name, !Object.Equals(inex.InnerException, null) ? inex.InnerException.Message : inex.Message),bError:true);                        
                    }
                }
                bSuccessFlg = true;
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("GenerateIS->Generate->{0}", ex.InnerException != null ? ex.InnerException.Message : ex.Message));
            }
            return bSuccessFlg;
        }
    }
}
