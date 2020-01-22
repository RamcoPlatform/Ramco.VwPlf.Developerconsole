using System;
using System.Text;
using System.CodeDom;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Dynamic;
using System.Linq;
using System.Xml.Serialization;
using System.Text.RegularExpressions;
using System.Linq.Expressions;
using System.Data;
using System.Collections;
using System.Xml;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace Ramco.VwPlf.CodeGenerator
{
    /// <summary>
    /// Class to hold functions for writing log.
    /// </summary>
    public class Logger
    {
        private static object _thisLock = new object();
        private string _logFilePath;
        private string _compilationLogFile;
        //private ErrorCollection errorCollection = new ErrorCollection();

        public Logger(string sLogFilePath)
        {
            if (string.IsNullOrEmpty(sLogFilePath))
            {
                throw new Exception("Generation Path Unavailable");
            }
            this._logFilePath = sLogFilePath;
            this._compilationLogFile = Path.Combine(Path.GetDirectoryName(sLogFilePath), "Build.txt");
        }

        public Logger(string sLogFilePath, ObjectType type)
        {
            if (string.IsNullOrEmpty(sLogFilePath))
                throw new Exception("Generation Path Unavailable");

            _logFilePath = sLogFilePath;
        }

        /// <summary>
        /// Writes to the log file
        /// </summary>
        /// <param name="sFunction"></param>
        /// <param name="sMessage"></param>
        /// <param name="bFirstLog"></param>
        /// <param name="bLogFunctionName"></param>
        /// <param name="bLogTiming"></param>
        public void WriteLogToFile(string sFunction, string sMessage, bool bFirstLog = false, bool bLogFunctionName = true, bool bLogTiming = false, bool bCommandLine = false, bool bError = false)
        {
            lock (_thisLock)
            {

                WriteLogToTraceListener(sFunction, sMessage);

                Console.WriteLine(string.Format("{0}    :   {1}", sFunction, sMessage));

                //create directory
                Common.CreateDirectory(Path.GetDirectoryName(_logFilePath), false);

                //create log file
                Common.CreateFile(_logFilePath);

                try
                {
                    //writes trace to log file
                    using (FileStream fs = new FileStream(_logFilePath, (bFirstLog ? FileMode.Create : FileMode.Append)))
                    {
                        using (StreamWriter writer = new StreamWriter(fs))
                        {
                            if (!bError)
                            {
                                //T-Time, F-FunctionName, C-Message Context
                                StringBuilder tmp = new StringBuilder();

                                //if (bLogTiming)
                                tmp.Append(string.Format(" T:{0}", Convert.ToString(DateTime.Now.ToString())));

                                //if (bLogFunctionName)
                                //    tmp.Append(string.Format(" F:{0}", sFunction));

                                tmp.Append(string.Format(" C:{0}", sMessage));

                                writer.WriteLine(tmp.ToString());
                            }
                            else
                            {
                                //errorCollection.AddDescription(string.Format("{0}:{1}", sFunction, sMessage));
                                writer.WriteLine(string.Format("ERROR : {0}:{1}", sFunction, sMessage));
                            }

                            writer.Flush();
                            writer.Close();
                            writer.Dispose();
                        }
                        fs.Close();
                        fs.Dispose();
                    }
                }

                catch
                {
                    //throw ex;
                }
            }
        }

        public void WriteCompilationString(string sCompilationText)
        {
            try
            {
                if (!File.Exists(this._compilationLogFile))
                    File.Create(this._compilationLogFile).Close();

                using (FileStream fs = new FileStream(_compilationLogFile, FileMode.Append))
                {
                    using (StreamWriter writer = new StreamWriter(fs))
                    {
                        writer.WriteLine(sCompilationText);
                        writer.WriteLine();
                        writer.Flush();
                    }
                }
            }
            catch
            {

            }
        }

        /// <summary>
        /// Writes trace
        /// </summary>
        /// <param name="sContext"></param>
        /// <param name="sMessage"></param>
        public static void WriteLogToTraceListener(string sContext, string sMessage)
        {
            try
            {
                TraceListener traceWriter = new DefaultTraceListener();

                //for dbgview
                traceWriter.WriteLine(string.Format("{0} : {1}", sContext, sMessage));
            }
            catch
            {
                Console.WriteLine("Exception in Writing trace function.");
            }
        }

    }

    /// <summary>
    /// Class to hold reusable functions that can be exported for other projects too.
    /// </summary>
    internal class Common
    {
        //Logger logger;

        public Common()
        {
            //logger = new Logger(Path.Combine(ECRLevelOptions.GenerationPath, string.Format("{0}.txt", ECRLevelOptions.Ecrno)));
        }

        public static void SaveXmlDocument(string strFilePath, XDocument xmlDoc)
        {
            string strDirectory = Path.GetDirectoryName(@strFilePath);
            if (!Directory.Exists(@strDirectory))
            {
                Directory.CreateDirectory(@strDirectory);
            }
            XmlWriterSettings xws = new XmlWriterSettings { OmitXmlDeclaration = true, Indent = true, IndentChars = " ", NewLineChars = "\r\n", NewLineHandling = NewLineHandling.Replace };
            using (XmlWriter xw = XmlWriter.Create(@strFilePath, xws))
            {
                xmlDoc.Save(xw);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ditype"></param>
        /// <returns></returns>
        public static string CategorizeDIType(string ditype)
        {
            switch (ditype)
            {
                //ADO_INT
                case DataType.LONG:
                case DataType.INT:
                case DataType.INTEGER:
                    return DataType.INT;

                //ADO_STR
                case DataType.STRING:
                case DataType.DATE_TIME:
                case DataType.DATETIME:
                case DataType.CHAR:
                case DataType.NVARCHAR:
                case DataType.DATE:
                case DataType.TIME:
                case DataType.ENUMERATED:
                    return DataType.STRING;

                //ADO_DOUBLE
                case DataType.DOUBLE:
                case DataType.NUMERIC:
                    return DataType.DOUBLE;

                //varbinary
                case DataType.VARBINARY:
                    return DataType.VARBINARY;

                //timestamp
                case DataType.TIMESTAMP:
                    return DataType.TIMESTAMP;

                default:
                    return DataType.STRING;

            }
        }

        /// <summary>
        /// Creates directory if not exists.
        /// </summary>
        /// <param name="sTargetDirectory">directory as string</param>
        public static void CreateDirectory(string sTargetDirectory, bool bWriteLog = true)
        {
            try
            {

                if (!Directory.Exists(sTargetDirectory))
                {
                    if (bWriteLog)
                        Logger.WriteLogToTraceListener("CreateDirectory", string.Format("Creating Directory :{0}", sTargetDirectory));
                    Directory.CreateDirectory(sTargetDirectory);
                }
            }
            catch (System.Security.SecurityException)
            {
                Logger.WriteLogToTraceListener("CreateDirectory", "Change the Security permission for the generation folder prior to ur codegeneration Folder && C:/temp...");
            }

            catch (Exception ex)
            {
                if (Common.IsDiskFull(ex))
                {
                    throw (new Exception("Disk Full"));
                }
                else
                {
                    if (!object.Equals(ex.InnerException, null))
                        Logger.WriteLogToTraceListener("CreateDirectory", ex.InnerException.Message);
                    else
                        Logger.WriteLogToTraceListener("CreateDirectory", ex.Message);
                }

            }
        }

        public static void CopyDirectory(string source, string target, bool copySubDirectory)
        {
            DirectoryInfo dir = new DirectoryInfo(source);

            //get subdirectories from source directory
            DirectoryInfo[] subdirs = dir.GetDirectories();

            //create target directory if not available
            if (!Directory.Exists(target))
                Directory.CreateDirectory(target);

            //copy files 
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string sTempPath = Path.Combine(target, file.Name);
                file.CopyTo(sTempPath, true);
            }

            if (copySubDirectory)
            {
                //create subdirectories
                foreach (DirectoryInfo subdir in subdirs)
                {
                    string sTempPath = Path.Combine(target, subdir.Name);
                    CopyDirectory(subdir.FullName, sTempPath, copySubDirectory);
                }
            }
        }

        /// <summary>
        /// Creates file if not available.
        /// </summary>
        /// <param name="sFullFilePath">fullfilepath as string</param>
        public static void CreateFile(string sFullFilePath)
        {
            try
            {
                if (!File.Exists(sFullFilePath))
                {
                    Logger.WriteLogToTraceListener("CreateFile", string.Format("Creating File :{0}", sFullFilePath));
                    File.Create(sFullFilePath).Close();
                }
            }
            catch (System.Security.SecurityException)
            {
                Logger.WriteLogToTraceListener("CreateFile", "Change the Security permission for the generation folder prior to ur codegeneration Folder && C:/temp...");
            }

            catch (Exception ex)
            {
                if (Common.IsDiskFull(ex))
                {
                    throw (new Exception("Disk Full"));
                }
                else
                {
                    if (!object.Equals(ex.InnerException, null))
                        Logger.WriteLogToTraceListener("CreateFile", ex.InnerException.Message);
                    else
                        Logger.WriteLogToTraceListener("CreateFile", ex.Message);
                }

            }
        }

        /// <summary>
        /// Converts a string with its first letter to uppercase and others to lowercase eg:Mother
        /// </summary>
        /// <param name="inputstring">input string</param>
        /// <returns>output as string</returns>
        public static string InitCaps(string inputstring)
        {
            if (string.IsNullOrEmpty(inputstring))
            {
                return string.Empty;
            }
            return string.Concat(Char.ToUpper(inputstring[0]), inputstring.Substring(1).ToLower());
        }

        /// <summary>
        /// checks whether the given exception is occured because of 'no space in disk'
        /// </summary>
        /// <param name="ex">exception object</param>
        /// <returns></returns>
        public static bool IsDiskFull(Exception ex)
        {
            const int ERROR_HANDLE_DISK_FULL = 0x27;
            const int ERROR_DISK_FULL = 0x70;

            int win32ErrorCode = System.Runtime.InteropServices.Marshal.GetHRForException(ex) & 0xFFFF;
            return win32ErrorCode == ERROR_HANDLE_DISK_FULL || win32ErrorCode == ERROR_DISK_FULL;
        }

        /// <summary>
        /// Converts given entitiy object and writes to a xml file.
        /// </summary>
        /// <param name="t">entity object</param>
        /// <param name="outputFileName">fullfilepath of the xml file.</param>
        public static void SerializeToXML(Object t, out StringBuilder sb)
        {
            sb = new StringBuilder();

            System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(t.GetType());
            TextWriter textWriter = new StringWriter(sb);
            serializer.Serialize(textWriter, t);
            textWriter.Close();
            textWriter.Dispose();
        }

        //public static string TargetNamespace
        //{
        //    get
        //    {
        //        return "http://www.w3.org/2001/XMLSchema";
        //    }
        //}

        /// <summary>
        /// Deserializes an xml to object
        /// </summary>
        /// <param name="inFilename">fullfilepath of the xml file</param>
        /// <returns></returns>
        public static Object DeserializeFromXML(Type classType, string sXmlFilePath)
        {
            try
            {
                Object obj = null;
                XmlSerializer serializer = new XmlSerializer(classType);
                using (FileStream sr = new FileStream(sXmlFilePath, FileMode.Open))
                {
                    obj = serializer.Deserialize(sr);
                    sr.Close();
                    sr.Dispose();
                }
                return obj;
            }
            catch (Exception ex)
            {
                throw new Exception("Exception : Problem while deserializing Option xml. Kindly check the option xml for schema change " + ex.Message.ToString());
            }
        }

        /// <summary>
        /// Replaces special characters 
        /// </summary>
        /// <param name="inpStr"></param>
        /// <returns></returns>
        private static string RemoveSpecialCharacters(string inpStr)
        {
            Regex.Replace(inpStr, @"\t", " ");
            Regex.Replace(inpStr, @"\n", "");
            Regex.Replace(inpStr, @"\r", "");
            Regex.Replace(inpStr, @"\`", "\\");
            Regex.Replace(inpStr, @"\\", "\\\\");

            return inpStr;
        }

        /// <summary>
        /// Writes an sp result to the given file.
        /// </summary>
        /// <param name="connectionString">connectionstring as string</param>
        /// <param name="sQuery">sp/query as string</param>
        /// <param name="commandType">CommandType</param>
        /// <param name="paramAndValue">parameter and value as HashTable</param>
        /// <param name="fullFilePath">full filename where the has to be written</param>
        public static void WriteResultSetToFile(string connectionString, string sQuery, CommandType commandType, IDictionary paramAndValue, string fullFilePath, bool isMultiLangFile)
        {
            Ramco.VwPlf.DataAccess.DBManager dbManager = new Ramco.VwPlf.DataAccess.DBManager(connectionString);
            DataSet dsData = null;
            var parameters = new IDbDataParameter[0];
            if (paramAndValue.Count > 0)
            {
                int i = 0;
                parameters = dbManager.CreateParameters(paramAndValue.Count);
                IDictionaryEnumerator enumerator = paramAndValue.GetEnumerator();

                while (enumerator.MoveNext())
                {
                    dbManager.AddParamters(parameters, i, enumerator.Key.ToString(), enumerator.Value);
                    i++;
                }
            }
            dbManager.Open();
            dsData = dbManager.ExecuteDataSet(commandType, sQuery, parameters);
            dbManager.Close();
            if (dsData.Tables.Count > 0)
            {
                Common.CreateDirectory(Path.GetDirectoryName(fullFilePath));
                using (FileStream fs = new FileStream(fullFilePath, FileMode.Create))
                {
                    using (StreamWriter sw = new StreamWriter(fs, isMultiLangFile ? Encoding.UTF8 : Encoding.ASCII))
                    {
                        foreach (DataTable dtData in dsData.Tables)
                        {
                            if (dtData.Rows.Count > 0)
                            {
                                Common.CreateDirectory(Path.GetDirectoryName(fullFilePath));
                                foreach (DataRow drData in dtData.Rows)
                                {
                                    if (drData[0] != DBNull.Value)
                                        sw.WriteLine(drData[0].ToString());
                                    else
                                        Logger.WriteLogToTraceListener("WriteResultSetToFile", "null value were encountered while writing");
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                Logger.WriteLogToTraceListener("WriteResultSetToFile", "data not available");
            }
        }

        public static void WriteResultSetToFile(Ramco.VwPlf.DataAccess.DBManager _dbManager, string sQuery, CommandType commandType, IDictionary paramAndValue, string fullFilePath,bool isMultiLangFile)
        {
            Ramco.VwPlf.DataAccess.DBManager dbManager = _dbManager;
            DataSet dsData = null;
            var parameters = new IDbDataParameter[0];
            if (paramAndValue.Count > 0)
            {
                int i = 0;
                parameters = dbManager.CreateParameters(paramAndValue.Count);
                IDictionaryEnumerator enumerator = paramAndValue.GetEnumerator();

                while (enumerator.MoveNext())
                {
                    dbManager.AddParamters(parameters, i, enumerator.Key.ToString(), enumerator.Value);
                    i++;
                }
            }
            //dbManager.Open();
            dsData = dbManager.ExecuteDataSet(commandType, sQuery, parameters);
            //dbManager.Close();
            if (dsData.Tables.Count > 0)
            {
                Common.CreateDirectory(Path.GetDirectoryName(fullFilePath));
                using (FileStream fs = new FileStream(fullFilePath, FileMode.Create))
                {
                    using (StreamWriter sw = new StreamWriter(fs, isMultiLangFile ? Encoding.UTF8 : Encoding.ASCII))
                    {
                        foreach (DataTable dtData in dsData.Tables)
                        {
                            if (dtData.Rows.Count > 0)
                            {
                                Common.CreateDirectory(Path.GetDirectoryName(fullFilePath));
                                foreach (DataRow drData in dtData.Rows)
                                {
                                    if (drData[0] != DBNull.Value)
                                        sw.WriteLine(drData[0].ToString());
                                    else
                                        Logger.WriteLogToTraceListener("WriteResultSetToFile", "null value were encountered while writing");
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                Logger.WriteLogToTraceListener("WriteResultSetToFile", "data not available");
            }
        }

        /// <summary>
        /// Add a New xml node and an attribute to it
        /// </summary>
        /// <param name="xElmParent">container node</param>
        /// <param name="strElmName">name of the xml node to add</param>
        /// <param name="strAttributeName">name of the attribute to add</param>
        /// <param name="strAttributeValue">value for the attribute</param>
        /// <returns></returns>
        public static XElement AddElementAndAttribute(XContainer xElmParent, string strElmName, string strAttributeName, string strAttributeValue)
        {
            XElement xElm;
            xElm = new XElement(strElmName);
            xElmParent.Add(xElm);
            XAttribute xAtt = new XAttribute(strAttributeName, strAttributeValue);
            xElm.Add(xAtt);
            return xElm;
        }

        /// <summary>
        /// Finds an XElement with the given name.
        /// </summary>
        /// <param name="xCont">Parent XElement, Can be XDocument.</param>
        /// <param name="strElmName">XElement Name to find. Here XElement represents a tag.</param>
        /// <returns>XElement</returns>
        public static XElement GetSingleElembyName(XContainer xCont, string strElmName)
        {
            XElement xElm;

            xElm = (from xml in xCont.Descendants(strElmName)
                    select xml).FirstOrDefault();

            return xElm;
        }

    }

    /// <summary>
    /// Class to hold common functions specifically reusable for codegeneration
    /// </summary>
    internal class CodeDomHelper
    {

        /// <summary>
        /// Adds class summary
        /// </summary>
        /// <param name="_Class"></param>
        /// <param name="_sSummary"></param>
        public static void CreateClassSummary(CodeTypeDeclaration _Class, string _sSummary)
        {
            _Class.Comments.Add(new CodeCommentStatement("<summary>"));
            _Class.Comments.Add(new CodeCommentStatement(string.Format("This class {0}", _sSummary)));
            _Class.Comments.Add(new CodeCommentStatement("</summary>"));
        }

        /// <summary>
        /// Adds Method summary
        /// </summary>
        /// <param name="_Method"></param>
        /// <param name="_sSummary"></param>
        public static void AddMethodSummary(CodeMemberMethod _Method, string _sSummary)
        {
            _Method.Comments.Add(new CodeCommentStatement("<summary>"));
            _Method.Comments.Add(new CodeCommentStatement(string.Format("This method {0}", _sSummary)));
            _Method.Comments.Add(new CodeCommentStatement("</summary>"));
            _Method.Comments.Add(new CodeCommentStatement("**************************************************************************"));
            _Method.Comments.Add(new CodeCommentStatement(string.Format("Function Name		:	{0}", _Method.Name)));
            _Method.Comments.Add(new CodeCommentStatement(string.Format("Author				:	{0}", "ILDotNetCodeGenerator")));
            _Method.Comments.Add(new CodeCommentStatement(string.Format("Date					:	{0}", DateTime.Now.ToShortDateString())));
            _Method.Comments.Add(new CodeCommentStatement(string.Format("Description			:	{0}", _sSummary)));
            _Method.Comments.Add(new CodeCommentStatement("***************************************************************************"));

        }

        /// <summary>
        /// forms and returns declaration statement
        /// </summary>
        /// <param name="variableType">may be of Type 'Type/string'</param>
        /// <param name="variableName">variable name as string</param>
        /// <returns>CodeVariableDeclarationStatement</returns>
        public static CodeVariableDeclarationStatement DeclareVariable(Object variableType, string variableName)
        {
            CodeVariableDeclarationStatement variableDeclaration = new CodeVariableDeclarationStatement();
            variableDeclaration.Name = variableName;
            if (variableType is Type)
                variableDeclaration.Type = new CodeTypeReference((Type)variableType);
            else
                variableDeclaration.Type = new CodeTypeReference((string)variableType);
            return variableDeclaration;
        }

        /// <summary>
        /// Add a Member Varibale to a class.
        /// </summary>
        /// <param name="refClass"></param>
        /// <param name="sName"></param>
        /// <param name="sType"></param>
        /// <param name="NeedInitialization"></param>
        /// <param name="comments"></param>
        /// <returns></returns>
        public static CodeMemberField DeclareMemberField(MemberAttributes accessSpecifier, CodeTypeDeclaration refClass, string sName, Object type, bool NeedInitialization, CodeCommentStatementCollection comments = null, CodeExpression InitExpression = null)
        {
            //create membervariable
            CodeMemberField memberVariable = new CodeMemberField
            {
                Name = sName,
                Type = type is Type ? new CodeTypeReference((Type)type) : new CodeTypeReference((string)type),
                Attributes = accessSpecifier
            };

            //add intialization
            if (NeedInitialization)
            {
                if (InitExpression != null)
                    memberVariable.InitExpression = InitExpression;
                else
                    memberVariable.InitExpression = new CodeObjectCreateExpression(type is Type ? new CodeTypeReference((Type)type) : new CodeTypeReference((string)type));
            }

            //add comments if available
            if (!object.Equals(comments, null))
                foreach (CodeCommentStatement comment in comments)
                {
                    memberVariable.Comments.Add(comment);
                }

            refClass.Members.Add(memberVariable);

            //return member variable
            return memberVariable;
        }



        /// <summary>
        /// Assign a value to a variable.
        /// </summary>
        /// <param name="variable">varaible that should be previously declared</param>
        /// <param name="value">may be
        /// codesnippetExpression/codeMethodInvokeExpression
        /// CodeArgumentReferenceExpression/CodeObjectCreateExpression
        /// CodeArrayCreateExpression/CodePrimitiveExpression
        /// </param>
        /// <returns></returns>
        public static CodeAssignStatement AssignVariable(string variable, object value, int index = -1, bool bIsArrayOrHashTable = false, string key = null)
        {
            CodeAssignStatement assignmentStatement = new CodeAssignStatement();

            if (index > -1 && bIsArrayOrHashTable.Equals(true))
                assignmentStatement.Left = new CodeArrayIndexerExpression(new CodeVariableReferenceExpression(variable), new CodePrimitiveExpression(index));
            else if (index <= -1 && bIsArrayOrHashTable.Equals(true) && key != null)
                assignmentStatement.Left = new CodeArrayIndexerExpression(new CodeVariableReferenceExpression(variable), new CodeSnippetExpression(key));
            else
                assignmentStatement.Left = new CodeVariableReferenceExpression(variable);

            if (value is CodeExpression)
                assignmentStatement.Right = (CodeExpression)value;
            else
                assignmentStatement.Right = new CodePrimitiveExpression(value);

            return assignmentStatement;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static CodeArrayIndexerExpression ArrayIndexerExpression(string variableName, CodeExpression indexExpression)
        {
            return new CodeArrayIndexerExpression(new CodeVariableReferenceExpression(variableName), indexExpression);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="targetType"></param>
        /// <param name="expression"></param>
        /// <returns></returns>
        public static CodeCastExpression CastExpression(string targetType, CodeExpression expression)
        {
            return new CodeCastExpression(targetType, expression);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="left"></param>
        /// <param name="Right"></param>
        /// <returns></returns>
        public static CodeAssignStatement AssignVariable(CodeExpression left, CodeExpression Right)
        {
            CodeAssignStatement assignmentStatement = new CodeAssignStatement(left, Right);
            return assignmentStatement;
        }

        public static CodeAssignStatement AssignField(CodeExpression left, CodeExpression Right)
        {
            CodeAssignStatement assignmentStatement = new CodeAssignStatement(left, Right);
            return assignmentStatement;
        }

        /// <summary>
        /// declare variable and assign value.
        /// </summary>
        /// <param name="VariableType">type of the variable... May be of type 'Type/string'</param>
        /// <param name="sVariableName">name of the variable</param>
        /// <param name="bNeedInitialization">need initialization or not...'True/False'</param>
        /// <param name="assignment">may be 
        /// CodeMethodInvokeExpression/CodeSnippetExpression
        /// CodeObjectCreateExpression/CodePropertyReferenceExpression
        /// CodePrimitiveExpression
        /// </param>
        /// <returns></returns>
        public static CodeVariableDeclarationStatement DeclareVariableAndAssign(Object VariableType, string sVariableName, bool bNeedInitialization = false, Object assignment = null)
        {
            CodeVariableDeclarationStatement variableDeclaration = new CodeVariableDeclarationStatement();

            variableDeclaration.Name = sVariableName;

            if (VariableType is Type)
                variableDeclaration.Type = new CodeTypeReference((Type)VariableType);
            else
                variableDeclaration.Type = new CodeTypeReference((string)VariableType);

            if (bNeedInitialization)
            {
                //if (assignment is CodeMethodInvokeExpression)
                //    variableDeclaration.InitExpression = (CodeMethodInvokeExpression)assignment;
                //else if (assignment is CodeObjectCreateExpression)
                //    variableDeclaration.InitExpression = (CodeObjectCreateExpression)assignment;
                //else if (assignment is CodeSnippetExpression)
                //    variableDeclaration.InitExpression = (CodeSnippetExpression)assignment;
                //else if (assignment is CodePropertyReferenceExpression)
                //    variableDeclaration.InitExpression = (CodePropertyReferenceExpression)assignment;
                //else if (assignment is CodePrimitiveExpression)
                //    variableDeclaration.InitExpression = (CodePrimitiveExpression)assignment;
                if (assignment is CodeExpression)
                    variableDeclaration.InitExpression = (CodeExpression)assignment;
                else
                    variableDeclaration.InitExpression = new CodePrimitiveExpression(assignment);

            }
            return variableDeclaration;
        }


        /// <summary>
        /// Add a Try Block to a method
        /// </summary>
        /// <returns></returns>
        public static CodeTryCatchFinallyStatement AddTryBlock(CodeMemberMethod method)
        {
            CodeTryCatchFinallyStatement tryCatchBlock = new CodeTryCatchFinallyStatement();
            method.Statements.Add(tryCatchBlock);
            return tryCatchBlock;
        }



        /// <summary>
        /// 
        /// </summary>
        /// <param name="tryBlock"></param>
        /// <param name="sExceptionObjectName"></param>
        /// <returns></returns>
        public static CodeCatchClause AddCatchBlock(CodeTryCatchFinallyStatement tryBlock, string sExceptionObjectName = "e")
        {
            Exception ex = new Exception();
            CodeCatchClause catchBlock = new CodeCatchClause(sExceptionObjectName, new CodeTypeReference(ex.GetType()));
            tryBlock.CatchClauses.Add(catchBlock);
            return catchBlock;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="tryBlock">tryblock</param>
        /// <param name="Objecttype">Type of Exception</param>
        /// <param name="ObjectName">Name of the Exception object</param>
        /// <returns></returns>
        public static CodeCatchClause AddCatchBlock(CodeTryCatchFinallyStatement tryBlock, Type Objecttype, string ObjectName)
        {
            CodeCatchClause catchBlock = new CodeCatchClause(ObjectName, new CodeTypeReference(Objecttype));
            tryBlock.CatchClauses.Add(catchBlock);
            return catchBlock;
        }

        /// <summary>
        /// Throws exception object
        /// </summary>
        /// <param name="catchBlock"></param>
        /// <param name="exceptionObject"></param>
        /// <returns></returns>
        public static CodeThrowExceptionStatement ThrowException(CodeCatchClause catchBlock, string exceptionObject)
        {
            CodeThrowExceptionStatement throwStatement = new CodeThrowExceptionStatement(SnippetExpression(exceptionObject));
            catchBlock.Statements.Add(throwStatement);
            return throwStatement;
        }

        public static CodeThrowExceptionStatement ThrowNewException(CodeExpression expressionToThrow)
        {
            return new CodeThrowExceptionStatement(expressionToThrow);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="catchBlock"></param>
        /// <returns></returns>
        public static CodeThrowExceptionStatement ThrowException(CodeCatchClause catchBlock)
        {
            // This CodeThrowExceptionStatement throws a new System.Exception.
            CodeThrowExceptionStatement throwException = new CodeThrowExceptionStatement(

                // codeExpression parameter indicates the exception to throw.
                // You must use an object create expression to new an exception here.

                new CodeObjectCreateExpression(

                // createType parameter inidicates the type of object to create.
                new CodeTypeReference("Exception"),

                // parameters parameter indicates the constructor parameters.
                new CodeExpression[] {
                    new CodeFieldReferenceExpression( new CodeTypeReferenceExpression("e"), "Message") ,
                    new CodeTypeReferenceExpression("e")
                }));
            catchBlock.Statements.Add(throwException);

            return throwException;
        }

        /// <summary>
        /// create new exception with the given message
        /// </summary>
        /// <param name="Message">can be a CodeExpression/string</param>
        /// <returns></returns>
        public static CodeThrowExceptionStatement ThrowNewException(object Message)
        {
            CodeThrowExceptionStatement throwException = new CodeThrowExceptionStatement(
                                                                                            new CodeObjectCreateExpression
                                                                                                (
                                                                                                    new CodeTypeReference("Exception"),
                                                                                                    Message is CodeExpression ? (CodeExpression)Message : new CodePrimitiveExpression(Message)
                                                                                                )
                                                                                         );
            return throwException;
        }

        /// <summary>
        /// Adds a statement/expression to try block
        /// </summary>
        /// <param name="tryBlock"></param>
        /// <param name="statement"></param>
        public static void AddExpressionToTryBlock(CodeTryCatchFinallyStatement tryBlock, Object statement)
        {
            if (statement is CodeStatement)
                tryBlock.TryStatements.Add((CodeStatement)statement);
            else if (statement is CodeExpression)
                tryBlock.TryStatements.Add((CodeExpression)statement);
        }

        /// <summary>
        /// Add a statement to catch block
        /// </summary>
        /// <param name="catchBlock"></param>
        /// <param name="statement"></param>
        public static void AddExpressionToCatchBlock(CodeCatchClause catchBlock, CodeExpression statement)
        {
            catchBlock.Statements.Add(statement);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="objectName"></param>
        /// <param name="sMethodName"></param>
        /// <returns></returns>
        public static CodeMethodInvokeExpression MethodInvocationExp(CodeExpression objectName, string sMethodName)
        {
            CodeMethodReferenceExpression methodInfo = new CodeMethodReferenceExpression
            {
                MethodName = sMethodName,
                TargetObject = objectName
            };

            return (new CodeMethodInvokeExpression(methodInfo));
        }

        /// <summary>
        /// returns new CodeCommentStatement
        /// </summary>
        /// <param name="sCommentMessage"></param>
        /// <returns></returns>
        public static CodeCommentStatement CommentStatement(string sCommentMessage)
        {
            return new CodeCommentStatement(sCommentMessage);
        }

        /// <summary>
        /// returns new CodeTypeReferenceExpression
        /// </summary>
        /// <param name="Type">type as 'CodeTypeReference'</param>
        /// <returns>CodeTypeReferenceExpression</returns>
        public static CodeTypeReferenceExpression TypeReferenceExp(CodeTypeReference Type)
        {
            return new CodeTypeReferenceExpression(Type);
        }

        /// <summary>
        /// returns new CodeTypeReferenceExpression
        /// </summary>
        /// <param name="sType">type as 'string'</param>
        /// <returns>CodeTypeReferenceExpression</returns>
        public static CodeTypeReferenceExpression TypeReferenceExp(string sType)
        {
            return new CodeTypeReferenceExpression(sType);
        }

        /// <summary>
        /// returns new CodeTypeReferenceExpression
        /// </summary>
        /// <param name="type">type as 'Type'</param>
        /// <returns>CodeTypeReferenceExpression</returns>
        public static CodeTypeReferenceExpression TypeReferenceExp(Type type)
        {
            return new CodeTypeReferenceExpression(type);
        }

        /// <summary>
        /// Returns new CodeThisReferenceExpression expression
        /// </summary>
        /// <returns>CodeThisReferenceExpression</returns>
        public static CodeThisReferenceExpression ThisReferenceExp()
        {
            return new CodeThisReferenceExpression();
        }

        /// <summary>
        /// returns new CodeBaseReferenceExpression
        /// </summary>
        /// <returns>CodeBaseReferenceExpression</returns>
        public static CodeBaseReferenceExpression BaseReferenceExp()
        {
            return new CodeBaseReferenceExpression();
        }


        /// <summary>
        /// Returns new CodeSnippetStatement with the given Statement
        /// </summary>
        /// <param name="sStatement">statement as string</param>
        /// <returns>CodeSnippetStatement</returns>
        public static CodeSnippetStatement SnippetStatement(string sStatement)
        {
            return new CodeSnippetStatement(sStatement);
        }

        /// <summary>
        /// Returns new CodeSnippetExpression with the given Expression
        /// </summary>
        /// <param name="sExpression">Expression As string</param>
        /// <returns>CodeSnippetExpression</returns>
        public static CodeSnippetExpression SnippetExpression(string sExpression)
        {
            return new CodeSnippetExpression(sExpression);
        }

        /// <summary>
        /// Add indent to the snippetstatement
        /// </summary>
        /// <param name="snippetStatement"></param>
        /// <param name="tabCount"></param>
        /// <returns>SnippetStatement</returns>
        public static CodeSnippetStatement SnippetStatement(string statement, int tabcount)
        {
            return new CodeSnippetStatement(string.Format("{0}{1}", new string('\t', tabcount), statement));
        }

        /// <summary>
        /// Returns new CodeVariableReferenceExpression
        /// </summary>
        /// <param name="sVariableName">Name of the variable as string</param>
        /// <returns>CodeVariableReferenceExpression</returns>
        public static CodeVariableReferenceExpression VariableReferenceExp(string sVariableName)
        {
            return new CodeVariableReferenceExpression(sVariableName);
        }

        /// <summary>
        /// Returns new CodeArgumentReferenceExpression
        /// </summary>
        /// <param name="sParamName">Name of the Parameter as string</param>
        /// <returns>CodeArgumentReferenceExpression</returns>
        public static CodeArgumentReferenceExpression ArgumentReferenceExp(string sParamName)
        {
            return new CodeArgumentReferenceExpression(sParamName);
        }

        /// <summary>
        /// returns new CodePropertyReferenceExpression
        /// </summary>
        /// <param name="targetObject">Expression</param>
        /// <param name="sPropertyName">name of the property</param>
        /// <returns>CodePropertyReferenceExpression</returns>
        public static CodePropertyReferenceExpression PropertyReferenceExp(CodeExpression targetObject, string sPropertyName)
        {
            return new CodePropertyReferenceExpression(targetObject, sPropertyName);
        }

        /// <summary>
        /// returns an expression to set value for a property
        /// </summary>
        /// <param name="sType"></param>
        /// <param name="sPropertyName"></param>
        /// <param name="sPropertyValue"></param>
        /// <returns></returns>
        public static CodeAssignStatement SetProperty(string sType, string sPropertyName, CodeExpression PropertyValue)
        {
            return new CodeAssignStatement(FieldReferenceExp(TypeReferenceExp(sType), sPropertyName), PropertyValue);
        }

        /// <summary>
        /// returns new CodeParameterDeclarationExpression
        /// </summary>
        /// <param name="ParamType">parameter type as 'Type'</param>
        /// <param name="sParamName">name of the parameter as string</param>
        /// <returns>CodeParameterDeclarationExpression</returns>
        public static CodeParameterDeclarationExpression ParameterDeclarationExp(Type ParamType, string sParamName)
        {
            return new CodeParameterDeclarationExpression(ParamType, sParamName);
        }

        /// <summary>
        /// returns new CodeParameterDeclarationExpression
        /// </summary>
        /// <param name="sParamType">parameter type as string</param>
        /// <param name="sParamName">name of the parameter as string</param>
        /// <returns>CodeParameterDeclarationExpression</returns>
        public static CodeParameterDeclarationExpression ParameterDeclarationExp(string sParamType, string sParamName)
        {
            return new CodeParameterDeclarationExpression(sParamType, sParamName);
        }

        /// <summary>
        /// returns new CodeParameterDeclarationExpression
        /// </summary>
        /// <param name="ParamType">parameter type as CodeTypeReference</param>
        /// <param name="sParamName">name of the parameter as string</param>
        /// <returns>CodeParameterDeclarationExpression</returns>
        public static CodeParameterDeclarationExpression ParameterDeclarationExp(CodeTypeReference ParamType, string sParamName)
        {
            return new CodeParameterDeclarationExpression(ParamType, sParamName);
        }


        /// <summary>
        /// Returns new CodeFieldReferenceExpression
        /// </summary>
        /// <param name="targetObject">TargetObject as codeExpression</param>
        /// <param name="sFieldName">Name of the field as string</param>
        /// <returns>CodeFieldReferenceExpression</returns>
        public static CodeFieldReferenceExpression FieldReferenceExp(CodeExpression targetObject, string sFieldName)
        {
            return new CodeFieldReferenceExpression(targetObject, sFieldName);
        }


        /// <summary>
        /// Returns new CodePrimitiveExpression
        /// </summary>
        /// <param name="expression">value of type Object</param>
        /// <returns>CodePrimitiveExpression</returns>
        public static CodePrimitiveExpression PrimitiveExpression(Object value)
        {
            return new CodePrimitiveExpression(value);
        }

        /// <summary>
        /// return
        /// </summary>
        /// <returns></returns>
        public static CodeMethodReturnStatement ReturnExpression()
        {
            return new CodeMethodReturnStatement();
        }

        /// <summary>
        /// return some expression
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        public static CodeMethodReturnStatement ReturnExpression(CodeExpression expression)
        {
            return new CodeMethodReturnStatement(expression);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public static void AddParameters(CodeMethodInvokeExpression methodInvocation, Object[] parameters)
        {
            foreach (Object param in parameters)
            {
                if (param is CodeExpression)
                    methodInvocation.Parameters.Add((CodeExpression)param);
                else
                    methodInvocation.Parameters.Add(new CodePrimitiveExpression(param));

            }
        }

        /// <summary>
        /// returns a fieldreference parameters to be added in a method call
        /// </summary>
        /// <param name="fieldtype"></param>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        public static void AddFieldRefParameter(CodeMethodInvokeExpression methodInvocation, string fieldtype, string fieldName)
        {
            CodeTypeReferenceExpression userType = new CodeTypeReferenceExpression(fieldtype);
            methodInvocation.Parameters.Add(new CodeFieldReferenceExpression(userType, fieldName));
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="methodInvocation"></param>
        /// <param name="paramName"></param>
        public static void AddParameter(CodeMethodInvokeExpression methodInvocation, string parameterName)
        {
            methodInvocation.Parameters.Add(new CodeArgumentReferenceExpression(parameterName));
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <param name="property"></param>
        /// <returns></returns>
        public static CodePropertyReferenceExpression GetProperty(string type, string property)
        {
            return new CodePropertyReferenceExpression(new CodeTypeReferenceExpression(type), property);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <param name="property"></param>
        /// <returns></returns>
        public static CodePropertyReferenceExpression GetProperty(Type type, string property)
        {
            return new CodePropertyReferenceExpression(new CodeTypeReferenceExpression(type), property);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <param name="property"></param>
        /// <returns></returns>
        public static CodePropertyReferenceExpression GetProperty(CodeExpression type, string property)
        {
            return new CodePropertyReferenceExpression(type, property);
        }


        public static CodeObjectCreateExpression ObjectCreateExpression(Type ObjectType, CodeExpression[] parameters = null)
        {
            if (parameters != null)
                return new CodeObjectCreateExpression(ObjectType, parameters);
            else
                return new CodeObjectCreateExpression(ObjectType);
        }

        public static CodeObjectCreateExpression ObjectCreateExpression(string ObjectType, CodeExpression[] parameters = null)
        {
            if (parameters != null)
                return new CodeObjectCreateExpression(ObjectType, parameters);
            else
                return new CodeObjectCreateExpression(ObjectType);
        }

        /// <summary>
        /// Initializes member field 
        /// </summary>
        /// <param name="sMemberType"></param>
        /// <param name="sMemberName"></param>
        public static CodeStatement InitializeMemberField(string sMemberType, string sMemberName)
        {
            CodeExpression thisExpr = new CodeThisReferenceExpression();
            CodeStatement initialization = new CodeAssignStatement
            {
                Left = new CodeFieldReferenceExpression(thisExpr, sMemberName),
                Right = new CodeObjectCreateExpression(sMemberType)
            };
            return initialization;
        }

        /// <summary>
        /// Initializes member field 
        /// </summary>
        /// <param name="sMemberType"></param>
        /// <param name="sMemberName"></param>
        public static CodeStatement InitializeMemberField(CodeMemberMethod method, string sMemberType, string sMemberName)
        {
            CodeExpression thisExpr = new CodeThisReferenceExpression();
            CodeStatement initialization = new CodeAssignStatement
            {
                Left = new CodePropertyReferenceExpression(thisExpr, sMemberName),
                Right = new CodeObjectCreateExpression(sMemberType)
            };
            return initialization;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static CodeConditionStatement IfCondition()
        {
            return new CodeConditionStatement();
        }

        public static CodePrimitiveExpression CreatePrimitiveExpression(Object expression)
        {
            return new CodePrimitiveExpression(expression);
        }

        public static CodeSnippetExpression CreateSnippetExpression(string expression)
        {
            return new CodeSnippetExpression(expression);
        }

        public static CodeIterationStatement ForLoopExpression(CodeStatement initStatement, CodeExpression testExpression, CodeStatement incrementStatement)
        {
            return new CodeIterationStatement(initStatement, testExpression, incrementStatement);
        }

        public static CodeBinaryOperatorExpression BinaryOpertorExpression(CodeExpression left, CodeBinaryOperatorType Operator, CodeExpression right)
        {
            return new CodeBinaryOperatorExpression(left, Operator, right);
        }

        public static CodeNamespaceImport NewNamespace(string sName)
        {
            return new CodeNamespaceImport(sName);
        }

        public static CodeSnippetStatement SwitchStatement(string label)
        {
            return new CodeSnippetStatement(string.Format("switch({0})", label));
        }

        public static CodeSnippetStatement CaseStatement(string labelValue)
        {
            return new CodeSnippetStatement(string.Format("case \"{0}\":", labelValue));
        }

        public static CodeSnippetStatement StartSwitch()
        {
            return new CodeSnippetStatement("{");
        }

        public static CodeSnippetStatement EndSwitch()
        {
            return new CodeSnippetStatement("}");
        }

        //public static void UpdateStatusXML()
        //{
        //    Common.SerializeToXML(ECRLevelOptions.generation, Path.Combine(ECRLevelOptions.GenerationPath, string.Format("{0}_Status.xml", ECRLevelOptions.Ecrno)));
        //}
    }

    /// <summary>
    /// Class to hold function to execute a command script in shellprompt
    /// </summary>
    internal class ShellPrompt
    {
        private Process _program;
        private string _workingDirectory;
        private int _timeout = 20000;
        private StringBuilder _output = new StringBuilder();
        private StringBuilder _error = new StringBuilder();

        private Logger _logger = null;

        public string WorkingDirectory
        {
            get
            {
                return this._workingDirectory;
            }
            set
            {
                this._workingDirectory = value;
            }
        }

        /// <summary>
        /// Name of the exe. If it is al.exe, specify it as 'al'
        /// </summary>
        public string ExeName { get; set; }

        /// <summary>
        /// Argument list for the exe
        /// </summary>
        public List<string> Arguments { get; set; }

        /// <summary>
        /// Output from the current command line script
        /// </summary>
        public string Output
        {
            get
            {
                return Convert.ToString(_output);
            }
        }

        /// <summary>
        /// Error occured from the current command line script
        /// </summary>
        public string Error
        {
            get
            {
                return Convert.ToString(_error);
            }
        }

        public ShellPrompt(Logger logger)
        {
            this.Arguments = new List<string>();
            this._output = new StringBuilder();
            this._error = new StringBuilder();
            this._logger = logger;
        }

        /// <summary>
        /// 
        /// </summary>
        public void OpenInstance()
        {
            _program = new System.Diagnostics.Process();
            _output = new StringBuilder();
            _error = new StringBuilder();
        }

        ///// <summary>
        ///// Delegate Method that will be called when the process of the program is getting exit.
        ///// </summary>
        ///// <param name="sender"></param>
        ///// <param name="e"></param>
        //private void program_Exited(Object sender, System.EventArgs e)
        //{
        //    try
        //    {
        //        string sOutArgument = Arguments.Find(x => x.Contains("/out:"));
        //        if (!string.IsNullOrEmpty(sOutArgument))
        //        {
        //            string[] arrSeperator = new string[] { "out:" };
        //            string[] arrSeperatedStr = sOutArgument.Split(arrSeperator, StringSplitOptions.RemoveEmptyEntries);
        //            string sFilePath = arrSeperatedStr[1];
        //            System.TimeSpan tFileGeneratedTime = DateTime.Now.Subtract(File.GetCreationTime(sFilePath));
        //            if (!File.Exists(sFilePath) || tFileGeneratedTime.Minutes > 5)
        //            {
        //                ErrorCollection newError = new ErrorCollection();
        //                newError.ObjectName = Path.GetFileName(sFilePath);
        //                newError.ObjectType = ObjectType.Service;
        //                newError.Compilationstring = string.Join("", Arguments);
        //                if (tFileGeneratedTime.Minutes > 5)
        //                {
        //                    newError.AddDescription("Output file is previously generated one. Kindly use different path for generation");
        //                    _error.AppendLine("Output file is previously generated one. Kindly use different path for generation");
        //                }
        //                else
        //                {
        //                    newError.AddDescription(Output);
        //                    newError.AddDescription(Error);
        //                    _error.AppendLine(Output);
        //                    _error.AppendLine(Error);
        //                }

        //                GlobalVar.ecrLevelErrCollection.Add(newError);
        //            }
        //        }
        //    }
        //    catch
        //    {

        //    }
        //}

        //private void program_OutputDataReceived(Object sender, DataReceivedEventArgs e)
        //{
        //    string sOutputData = e.Data;
        //    if (!object.Equals(sOutputData, null))
        //    {
        //        _output.AppendLine(sOutputData);
        //    }
        //}

        //private void program_ErrorDataReceived(Object sender, DataReceivedEventArgs e)
        //{
        //    string sErrorData = e.Data;
        //    if (!object.Equals(sErrorData, null))
        //    {
        //        _error.AppendLine(sErrorData);
        //    }
        //}

        /// <summary>
        /// 
        /// </summary>
        public bool Run()
        {
            try
            {
                _program.StartInfo = new ProcessStartInfo();

                if (!string.IsNullOrEmpty(this._workingDirectory))
                    _program.StartInfo.WorkingDirectory = this._workingDirectory;

                _program.StartInfo.FileName = ExeName;
                foreach (string argument in Arguments.Distinct())
                {
                    _program.StartInfo.Arguments = string.Format("{0} {1}", _program.StartInfo.Arguments, argument);
                }

                _logger.WriteLogToFile("ShellPrompt.Run", string.Format("{0} {1}", _program.StartInfo.FileName, _program.StartInfo.Arguments));

                _program.StartInfo.CreateNoWindow = true;
                _program.StartInfo.ErrorDialog = false;
                _program.StartInfo.UseShellExecute = false;
                _program.StartInfo.RedirectStandardInput = true;
                _program.StartInfo.RedirectStandardOutput = true;
                _program.StartInfo.RedirectStandardError = true;
                //_program.EnableRaisingEvents = true;
                //_program.OutputDataReceived += program_OutputDataReceived;
                //_program.ErrorDataReceived += program_ErrorDataReceived;
                //_program.Exited += new EventHandler(program_Exited);

                //_program.Start();
                //_program.WaitForExit();
                //_program.BeginOutputReadLine();
                //_program.BeginErrorReadLine();
                using (System.Threading.AutoResetEvent outputWaitHandle = new System.Threading.AutoResetEvent(false))
                using (System.Threading.AutoResetEvent errorWaitHandle = new System.Threading.AutoResetEvent(false))
                {
                    _program.OutputDataReceived += (sender, e) =>
                    {
                        if (e.Data == null)
                        {
                            try
                            {
                                outputWaitHandle.Set();
                            }
                            catch
                            {
                                //_logger.WriteLogToFile("ShellPrompt->Run", e1.InnerException != null ? e1.InnerException.Message : e1.Message, bError: true);
                            }
                        }
                        else
                        {
                            try
                            {
                                _output.AppendLine(e.Data);
                            }
                            catch
                            {
                                //_logger.WriteLogToFile("ShellPrompt->Run", e2.InnerException!=null?e2.InnerException.Message:e2.Message, bError: true);
                            }
                        }
                    };
                    _program.ErrorDataReceived += (sender, e) =>
                    {
                        if (e.Data == null)
                        {
                            //errorWaitHandle.Set();
                        }
                        else
                        {
                            _error.AppendLine(e.Data);
                        }
                    };

                    _program.Start();
                    _program.BeginOutputReadLine();
                    _program.BeginErrorReadLine();

                    if (_program.WaitForExit(_timeout) && outputWaitHandle.WaitOne(_timeout) && errorWaitHandle.WaitOne(_timeout))
                    {
                        //process completed
                        int exitcode = _program.ExitCode;

                        _logger.WriteLogToFile("ShellPrompt->Run", "Output:" + _output.ToString());

                        if (_error.Length > 0)
                            _logger.WriteLogToFile("ShellPrompt->Run", _error.ToString(), bError: true);

                    }
                    else
                    {
                        //timed out
                        _logger.WriteLogToFile("ShellPrompt->Run", "shell command got timed out.", bError: false);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.WriteLogToFile("ShellPrompt->Run", ex.Message, false, true, true, false);
                return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void CloseInstance()
        {
            if (_program != null)
                _program.Dispose();
        }
    }

    /// <summary>
    /// 
    /// </summary>
    internal static class ExtensionMethods
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public static CodeMethodInvokeExpression AddParameters(this CodeMethodInvokeExpression methodInvocation, Object[] parameters)
        {
            foreach (Object param in parameters)
            {
                if (param is CodeExpression)
                    methodInvocation.Parameters.Add((CodeExpression)param);
                else
                    methodInvocation.Parameters.Add(new CodePrimitiveExpression(param));

            }

            return methodInvocation;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="methodInvocation"></param>
        /// <param name="parameter"></param>
        /// <returns></returns>
        public static CodeMethodInvokeExpression AddParameter(this CodeMethodInvokeExpression methodInvocation, CodeExpression parameter)
        {
            if (parameter is CodeExpression)
                methodInvocation.Parameters.Add((CodeExpression)parameter);

            return methodInvocation;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tryBlock"></param>
        /// <param name="sExceptionObjectName"></param>
        /// <returns></returns>
        public static CodeCatchClause AddCatch(this CodeTryCatchFinallyStatement tryBlock, string sExceptionObjectName = "e")
        {
            Exception ex = new Exception();
            CodeCatchClause catchBlock = new CodeCatchClause(sExceptionObjectName, new CodeTypeReference(ex.GetType()));
            tryBlock.CatchClauses.Add(catchBlock);
            return catchBlock;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tryBlock">tryblock</param>
        /// <param name="Objecttype">Type of Exception</param>
        /// <param name="ObjectName">Name of the Exception object</param>
        /// <returns></returns>
        public static CodeCatchClause AddCatch(this CodeTryCatchFinallyStatement tryBlock, Type Objecttype, string ObjectName)
        {
            CodeCatchClause catchBlock = new CodeCatchClause(ObjectName, new CodeTypeReference(Objecttype));
            tryBlock.CatchClauses.Add(catchBlock);
            return catchBlock;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tryBlock">tryblock</param>
        /// <param name="Objecttype">Type of Exception as string</param>
        /// <param name="ObjectName">Name of the Exception object</param>
        /// <returns></returns>
        public static CodeCatchClause AddCatch(this CodeTryCatchFinallyStatement tryBlock, string Objecttype, string ObjectName)
        {
            CodeCatchClause catchBlock = new CodeCatchClause(ObjectName, new CodeTypeReference(Objecttype));
            tryBlock.CatchClauses.Add(catchBlock);
            return catchBlock;
        }

        /// <summary>
        /// Throws exception object
        /// </summary>
        /// <param name="catchBlock"></param>
        /// <param name="exceptionObject"></param>
        /// <returns></returns>
        public static CodeThrowExceptionStatement ThrowException(this CodeCatchClause catchBlock, string exceptionObject)
        {
            CodeThrowExceptionStatement throwStatement = new CodeThrowExceptionStatement(CodeDomHelper.SnippetExpression(exceptionObject));
            catchBlock.Statements.Add(throwStatement);
            return throwStatement;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="catchBlock"></param>
        /// <returns></returns>
        public static CodeThrowExceptionStatement ThrowException(this CodeCatchClause catchBlock)
        {
            // This CodeThrowExceptionStatement throws a new System.Exception.
            CodeThrowExceptionStatement throwException = new CodeThrowExceptionStatement(

                // codeExpression parameter indicates the exception to throw.
                // You must use an object create expression to new an exception here.

                new CodeObjectCreateExpression(

                // createType parameter inidicates the type of object to create.
                new CodeTypeReference("Exception"),

                // parameters parameter indicates the constructor parameters.
                new CodeExpression[] {
                    new CodeFieldReferenceExpression( new CodeTypeReferenceExpression("e"), "Message") ,
                    new CodeTypeReferenceExpression("e")
                }));
            catchBlock.Statements.Add(throwException);

            return throwException;
        }

        public static void AddStatement(this CodeMemberMethod method, CodeExpression expression)
        {
            method.Statements.Add(expression);
        }

        public static void AddStatement(this CodeMemberMethod method, CodeStatement statement)
        {
            method.Statements.Add(statement);
        }

        public static void AddStatement(this CodeTryCatchFinallyStatement tryblock, CodeExpression expression)
        {
            tryblock.TryStatements.Add(expression);
        }

        public static void AddStatement(this CodeTryCatchFinallyStatement tryblock, CodeStatement statement)
        {
            tryblock.TryStatements.Add(statement);
        }

        public static void AddStatement(this CodeCatchClause catchblock, CodeExpression expression)
        {
            catchblock.Statements.Add(expression);
        }

        public static void AddStatement(this CodeCatchClause catchblock, CodeStatement statement)
        {
            catchblock.Statements.Add(statement);
        }

        public static void AddStatement(this CodeConditionStatement ifBlock, CodeExpression expression)
        {
            ifBlock.TrueStatements.Add(expression);
        }

        public static void AddStatement(this CodeConditionStatement ifBlock, CodeStatement statement )
        {
            ifBlock.TrueStatements.Add(statement);
        }

        /// <summary>
        /// Add a Try Block to a method
        /// </summary>
        /// <returns></returns>
        public static CodeTryCatchFinallyStatement AddTry(this CodeMemberMethod method)
        {
            CodeTryCatchFinallyStatement tryCatchBlock = new CodeTryCatchFinallyStatement();
            method.Statements.Add(tryCatchBlock);
            return tryCatchBlock;
        }
    }
}
