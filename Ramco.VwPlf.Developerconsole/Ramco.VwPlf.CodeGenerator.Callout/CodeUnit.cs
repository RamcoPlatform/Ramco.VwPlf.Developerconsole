using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.CodeDom;
using System.CodeDom.Compiler;
using Microsoft.CSharp;
using System.Linq;


namespace Ramco.VwPlf.CodeGenerator.Callout
{
    internal class CodeUnit
    {
        #region member variable
        private CSharpCodeProvider _CodeProvider;
        private ICodeGenerator _CodeGenerator;
        private CodeGeneratorOptions _GeneratorOptions;

        private CodeCompileUnit _CompileUnit;
        private CodeNamespace _Namespace;
        private List<CodeTypeDeclaration> _UserDefinedTypes;
        private CodeAttributeDeclarationCollection _AssemblyAttributeCollection;
        private List<String> _ReferencedNamespace;
        #endregion

        #region properties
        /// <summary>
        /// Its a container for CodeDom graph.(Entire code will be available here).
        /// </summary>
        public CodeCompileUnit CompileUnit
        {
            get
            {
                return _CompileUnit;
            }
            set
            {
                _CompileUnit = value;
            }
        }

        /// <summary>
        /// Represent a namespace declaration.
        /// </summary>
        public CodeNamespace NameSpace
        {
            get
            {
                return _Namespace;
            }
            set
            {
                _Namespace = value;
            }
        }

        /// <summary>
        /// Represent a class declaration. Holds information about the class u r declaring.
        /// </summary>
        public List<CodeTypeDeclaration> UserDefinedTypes
        {
            get
            {
                return _UserDefinedTypes;
            }
            set
            {
                _UserDefinedTypes = value;
            }
        }

        /// <summary>
        /// Holds collection of attributes that u r representing for a class. eg.[Serializable]
        /// </summary>
        public CodeAttributeDeclarationCollection AssemblyAttributeCollection
        {
            get
            {
                return this._AssemblyAttributeCollection;
            }
            set
            {
                this._AssemblyAttributeCollection = value;
            }
        }

        /// <summary>
        /// Referenced Namespaces
        /// </summary>
        public List<String> ReferencedNamespace
        {
            get
            {
                return this._ReferencedNamespace;
            }
            set
            {
                this._ReferencedNamespace = value;
            }
        }
        #endregion

        #region constructor
        /// <summary>
        /// Constructor to initialize member fields.
        /// </summary>
        public CodeUnit()
        {
            this._CompileUnit = new CodeCompileUnit();
            this._Namespace = new CodeNamespace();
            this._UserDefinedTypes = new List<CodeTypeDeclaration>();
            this._AssemblyAttributeCollection = new CodeAttributeDeclarationCollection();
            this._ReferencedNamespace = new List<String>();
        }
        #endregion

        /// <summary>
        /// Stitches objects to the codedom syntax tree
        /// </summary>
        public void StitchCSFile()
        {
            ///set custom attributes to assembly
            foreach (CodeAttributeDeclaration assemblyAttribute in this.AssemblyAttributeCollection)
            {
                this.CompileUnit.AssemblyCustomAttributes.Add(assemblyAttribute);
            }

            //add namespace to the assembly
            this.CompileUnit.Namespaces.Add(this.NameSpace);

            ///Import namespaces
            foreach (string namespaceToImport in this.ReferencedNamespace)
            {
                this.NameSpace.Imports.Add(new CodeNamespaceImport(namespaceToImport));
            }

            foreach (CodeTypeDeclaration userDefinedType in this.UserDefinedTypes)
            {
                this.NameSpace.Types.Add(userDefinedType);
            }

            //add methods to classes
        }

        /// <summary>
        /// Generates cs files from the codedom.
        /// </summary>
        public void WriteCSFile(string sClassName, string sTargetDir)
        {
            String sCode = String.Empty;
            String sFilePath = String.Empty;

            try
            {
                _CodeProvider = new CSharpCodeProvider();
                _CodeGenerator = _CodeProvider.CreateGenerator();
                _GeneratorOptions = new CodeGeneratorOptions { BracingStyle = "C", BlankLinesBetweenMembers = false };

                //Writes code from dom to string
//                Logger.WriteLogToTraceListener("GenerateCSFile", String.Format("Generate C# code for - {0}.cs", sClassName));

                using (StringWriter _stringwriter = new StringWriter())
                {
                    _CodeProvider.GenerateCodeFromCompileUnit(_CompileUnit, _stringwriter, _GeneratorOptions);
                    sCode = _stringwriter.ToString();
                }


                //Replaces doc comment
                sCode = Regex.Replace(sCode, "a tool", "DeveloperConsole - Ramco.VwPlf.CodeGenerator");
                sCode = Regex.Replace(sCode, "Runtime Version:[^\r\n]*", "Runtime Version omitted for demo");


                //Writes code from string to physical file
                sFilePath = Path.Combine(sTargetDir, String.Format("{0}.cs", sClassName));
                Common.CreateDirectory(sTargetDir);
                using (StreamWriter _CsFileWriter = new StreamWriter(sFilePath, false))
                {
                    _CsFileWriter.Write(sCode);
                    _CsFileWriter.Close();
                    _CsFileWriter.Dispose();
                }

                //FormatClassFile(sFilePath);
                FormatClassFile(sFilePath);

            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("GenerateCSFile->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sFilePath"></param>
        private void FormatClassFile(string sFilePath)
        {
            //1 indent that is tab sequence is equal to four white spaces
            const int cINDENT = 4;
            int IndentCountBeforeSwitch, SwitchIndentCount;
            int iLengthAfterChange;
            int iCounter = 0;

            //read the entire document
            String[] sFileContent = File.ReadAllLines(sFilePath);

            //getting line indexes of switch statement in the document
            IEnumerable<int> SwitchOccurences = Enumerable.Range(0, sFileContent.Length).Where(i => sFileContent[i].Contains("switch"));
            IEnumerable<int> EndSwitchOccurence = Enumerable.Range(0, sFileContent.Length).Where(i => sFileContent[i].Contains("//ENDSWITCH"));
            if (!SwitchOccurences.Count().Equals(EndSwitchOccurence.Count()))
                return;

            foreach (int index in SwitchOccurences)
            {
                //calculating line index of previous line to switch statement
                int previousLineIndex = index - 1;

                //get indent count of the previous line to switch statement
                int previousLineSpaceCount = sFileContent[previousLineIndex].TakeWhile(Char.IsWhiteSpace).Count();

                //calculating actual no of tab space
                IndentCountBeforeSwitch = previousLineSpaceCount / cINDENT;

                //calculating indent needed for switch line
                SwitchIndentCount = sFileContent[previousLineIndex].Contains("{") ? (IndentCountBeforeSwitch + 1) : IndentCountBeforeSwitch;
                iLengthAfterChange = sFileContent[index].Length + (SwitchIndentCount * 4);

                //re-arranging switch statement
                sFileContent[index] = String.Format("{0}{1}", String.Concat(Enumerable.Repeat("\t", SwitchIndentCount)), sFileContent[index].TrimStart());

                //re-arranging other statements in switch block
                int blockStart = index + 1;
                int blockEnd = EndSwitchOccurence.ElementAt(iCounter);
                sFileContent[blockStart] = String.Format("{0}{1}", String.Concat(Enumerable.Repeat("\t", SwitchIndentCount)), sFileContent[blockStart].TrimStart());

                int blockbody = blockStart + 1;
                while (blockbody < blockEnd)
                {
                    int BodyIndentCount;
                    if (sFileContent[blockbody].Contains("case") || sFileContent[blockbody].Contains("default"))
                    {
                        BodyIndentCount = SwitchIndentCount + 1;
                        sFileContent[blockbody] = String.Format("{0}{1}", String.Concat(Enumerable.Repeat("\t", BodyIndentCount)), sFileContent[blockbody].TrimStart());

                    }
                    else
                    {
                        //append the indent at the start to the existing string, so note here we are not using trim
                        BodyIndentCount = 2;
                        sFileContent[blockbody] = String.Format("{0}{1}", String.Concat(Enumerable.Repeat("\t", BodyIndentCount)), sFileContent[blockbody]);
                    }
                    blockbody++;
                }
                sFileContent[blockEnd] = String.Format("{0}{1}", String.Concat(Enumerable.Repeat("\t", SwitchIndentCount)), sFileContent[blockEnd].TrimStart());
                iCounter++;
            }
            File.WriteAllLines(sFilePath, sFileContent);
        }

    }
}
