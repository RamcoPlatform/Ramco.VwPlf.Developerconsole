using System;
using System.CodeDom;

namespace Ramco.VwPlf.CodeGenerator
{
    abstract class AbstractCSFileGenerator : CodeDomHelper
    {
        public CodeUnit _csFile = null;
        public Logger _logger = null;
        public ObjectType _objectType;
        public string _targetDir = string.Empty;//c:\test\
        public string _targetFileName = string.Empty;//sample.cs
        public AbstractCSFileGenerator(ECRLevelOptions ecrOptions)
        {
            _csFile = new CodeUnit(ecrOptions);
            _logger = new Logger(System.IO.Path.Combine(ecrOptions.GenerationPath, ecrOptions.Platform, ecrOptions.Customer, ecrOptions.Project, ecrOptions.Ecrno, string.Format("{0}.txt", ecrOptions.Ecrno)));
        }                 

        public abstract void CreateNamespace();
        public abstract void AddCustomAttributes();
        public abstract void ImportNamespace();
        public abstract void CreateClasses();
        public abstract void AddMemberFields(ref CodeTypeDeclaration classObj);
        public abstract void AddMemberFunctions(ref CodeTypeDeclaration classObj);
        public bool Generate()
        {
            try
            {
                CreateNamespace();
                AddCustomAttributes();
                ImportNamespace();
                CreateClasses();
                _csFile.StitchCsFile();
                _csFile.WriteCsFile(_targetFileName, _targetDir, _objectType);
                return true;
            }
            catch (Exception ex)
            {
                _logger.WriteLogToFile("Generate", ex.InnerException != null ? ex.InnerException.Message : ex.Message);
                return false;
            }
        }
    }
}
