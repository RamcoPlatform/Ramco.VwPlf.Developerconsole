using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.CodeDom;
using System.CodeDom.Compiler;
using Microsoft.CSharp;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;

//for formatter.format
using Microsoft.CodeAnalysis.Formatting;

//for csharpsyntaxtree
using Microsoft.CodeAnalysis.CSharp;
//using Microsoft.CodeAnalysis.CSharp.Syntax;


namespace Ramco.VwPlf.CodeGenerator
{
    internal class CodeUnit
    {
        #region member variable
        private CSharpCodeProvider _codeProvider;
        private CodeGeneratorOptions _generatorOptions;
        private CodeCompileUnit _compileUnit;
        private CodeNamespace _namespace;
        private List<CodeTypeDeclaration> _userDefinedTypes;
        private CodeAttributeDeclarationCollection _assemblyAttributeCollection;
        private List<string> _referencedNamespace;
        private List<CodeMemberMethod> _methodCollection;
        private ECRLevelOptions _ecrOptions;
        #endregion

        #region properties
        /// <summary>
        /// Its a container for CodeDom graph.(Entire code will be available here).
        /// </summary>
        public CodeCompileUnit CompileUnit
        {
            get
            {
                return _compileUnit;
            }
            set
            {
                _compileUnit = value;
            }
        }

        /// <summary>
        /// Represent a namespace declaration.
        /// </summary>
        public CodeNamespace NameSpace
        {
            get
            {
                return _namespace;
            }
            set
            {
                _namespace = value;
            }
        }

        /// <summary>
        /// Referenced Namespaces should be included here.
        /// </summary>
        //public CodeNamespaceImport ReferencedNamespace
        //{
        //    get
        //    {
        //        return _ReferencedNamespace;
        //    }
        //    set
        //    {
        //        _ReferencedNamespace = value;
        //    }
        //}

        /// <summary>
        /// Represent a class declaration. Holds information about the class u r declaring.
        /// </summary>
        public List<CodeTypeDeclaration> UserDefinedTypes
        {
            get
            {
                return _userDefinedTypes;
            }
            set
            {
                _userDefinedTypes = value;
            }
        }

        /// <summary>
        /// Holds collection of attributes that u r representing for a class. eg.[Serializable]
        /// </summary>
        public CodeAttributeDeclarationCollection AssemblyAttributeCollection
        {
            get
            {
                return _assemblyAttributeCollection;
            }
            set
            {
                _assemblyAttributeCollection = value;
            }
        }

        /// <summary>
        /// Referenced Namespaces
        /// </summary>
        public List<string> ReferencedNamespace
        {
            get
            {
                return _referencedNamespace;
            }
            set
            {
                _referencedNamespace = value;
            }
        }

        /// <summary>
        /// Holds Methods
        /// </summary>
        public List<CodeMemberMethod> MethodCollection
        {
            get
            {
                return _methodCollection;
            }
            set
            {
                _methodCollection = value;
            }
        }
        #endregion

        #region constructor
        /// <summary>
        /// Constructor to initialize member fields.
        /// </summary>
        public CodeUnit(ECRLevelOptions ecrOptions)
        {
            _ecrOptions = ecrOptions;
            _compileUnit = new CodeCompileUnit();
            _namespace = new CodeNamespace();
            _userDefinedTypes = new List<CodeTypeDeclaration>();
            _assemblyAttributeCollection = new CodeAttributeDeclarationCollection();
            _referencedNamespace = new List<string>();
            _methodCollection = new List<CodeMemberMethod>();
        }
        #endregion

        /// <summary>
        /// Stitches objects to the codedom syntax tree
        /// </summary>
        public void StitchCsFile()
        {
            ///set custom attributes to assembly
            foreach (CodeAttributeDeclaration assemblyAttribute in AssemblyAttributeCollection)
            {
                CompileUnit.AssemblyCustomAttributes.Add(assemblyAttribute);
            }

            //add namespace to the assembly
            CompileUnit.Namespaces.Add(NameSpace);

            ///Import namespaces
            foreach (string namespaceToImport in ReferencedNamespace)
            {
                NameSpace.Imports.Add(new CodeNamespaceImport(namespaceToImport));
            }

            foreach (CodeTypeDeclaration UserDefinedType in UserDefinedTypes)
            {
                NameSpace.Types.Add(UserDefinedType);
            }
            //add methods to classes
        }

        /// <summary>
        /// Generates cs files from the codedom.
        /// </summary>
        public void WriteCsFile(string sClassName, string sTargetDir, ObjectType objectType)
        {
            string sCode = string.Empty;
            string sFilePath = string.Empty;

            try
            {
                _codeProvider = new CSharpCodeProvider();
                _generatorOptions = new CodeGeneratorOptions { BracingStyle = "C", BlankLinesBetweenMembers = false };

                //Writes code from dom to string
                Logger.WriteLogToTraceListener("GenerateCSFile", string.Format("Generate C# code for - {0}.cs", sClassName));

                using (StringWriter stringWriter = new StringWriter())
                {
                    _codeProvider.GenerateCodeFromCompileUnit(_compileUnit, stringWriter, _generatorOptions);
                    sCode = stringWriter.ToString();
                }


                //Replaces doc comment
                sCode = Regex.Replace(sCode, "a tool", "DeveloperConsole - Ramco.VwPlf.CodeGenerator");
                sCode = Regex.Replace(sCode, "Runtime Version:[^\r\n]*", "Runtime Version omitted for demo");


                //Writes code from string to physical file
                sFilePath = Path.Combine(sTargetDir, string.Format("{0}.cs", sClassName));
                Common.CreateDirectory(sTargetDir);
                using (StreamWriter csFileWriter = new StreamWriter(sFilePath, false))
                {
                    csFileWriter.Write(sCode);
                }

                //FormatClassFile(sFilePath);
                EnhanceAlignment(sFilePath, objectType);

            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("GenerateCSFile->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }

        /// <summary>
        /// Aligns the given cs file
        /// </summary>
        /// <param name="filePath">fullfilepath of the cs file that needs to be aligned.</param>
        private void EnhanceAlignment(string filePath, ObjectType objectType)
        {

            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(File.ReadAllText(filePath));
            SyntaxNode rootNode = syntaxTree.GetRoot();
            var formattedResult = Formatter.Format(rootNode, Microsoft.CodeAnalysis.MSBuild.MSBuildWorkspace.Create());
            using (StreamWriter sw = new StreamWriter(filePath, false))
            {
                string sPurpose = string.Empty;
                switch (objectType)
                {
                    case ObjectType.Service:
                        sPurpose = "Service Implementation";
                        break;
                    case ObjectType.Activity:
                        sPurpose = "Activity Implementation";
                        break;
                }

                sw.WriteLine("/***********************************************************************************************************");
                sw.WriteLine("Copyright @2004, RAMCO SYSTEMS,  All rights reserved");
                sw.WriteLine(string.Format("Author                   :   {0}", "Ramco.VwPlf.DeveloperConsole"));
                sw.WriteLine(string.Format("Application/Module Name  :   {0}", Path.GetFileName(filePath)));
                sw.WriteLine(string.Format("Code Generated on        :   {0}", DateTime.Now));                
                sw.WriteLine(string.Format("Code Generated From      :   {0}", Path.Combine(_ecrOptions.Customer, _ecrOptions.Project, _ecrOptions.Ecrno, !string.IsNullOrEmpty(_ecrOptions.Model)? _ecrOptions.Model: "MODEL_UNKNOWN", !string.IsNullOrEmpty(_ecrOptions.User)? _ecrOptions.User:"USER_UNKNOWN", !string.IsNullOrEmpty(_ecrOptions.DB)?_ecrOptions.DB:"DB_UNKNOWN", _ecrOptions.CodegenClient)));
                sw.WriteLine(string.Format("Revision/Version #       :   {0}", ""));
                sw.WriteLine(string.Format("Purpose                  :   {0}", sPurpose));
                sw.WriteLine(string.Format("Modifications            :   {0}", ""));
                sw.WriteLine(string.Format("Modifier Name & Date     :   {0}", ""));
                sw.WriteLine("***********************************************************************************************************/");

                sw.WriteLine(formattedResult.ToString());

                sw.Close();
                sw.Dispose();
            }
        }
    }
}
