using System;

using System.Linq;
using System.Text;

using System.CodeDom;
using System.IO;
using System.Diagnostics;

namespace Ramco.VwPlf.CodeGenerator.Callout
{
    internal class Common
    {
        private static Logger logger = new Logger();
        public Common()
        {

        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="ditype"></param>
        /// <returns></returns>
        public static Type CategorizeDIType(string ditype)
        {
            switch (ditype.ToUpper())
            {
                //ADO_INT
                case DataType.LONG:
                case DataType.INT:
                case DataType.INTEGER:
                    return typeof(Int32);

                //ADO_STR
                case DataType.STRING:
                case DataType.DATE_TIME:
                case DataType.DATETIME:
                case DataType.CHAR:
                case DataType.NVARCHAR:
                case DataType.DATE:
                case DataType.TIME:
                case DataType.ENUMERATED:
                    return typeof(string);

                //ADO_DOUBLE
                case DataType.DOUBLE:
                case DataType.NUMERIC:
                    return typeof(double);

                ////varbinary
                //case DataType.VARBINARY:
                //    return DataType.VARBINARY;

                ////timestamp
                //case DataType.TIMESTAMP:
                //    return DataType.TIMESTAMP;

                default:
                    return typeof(string);

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
                    Directory.CreateDirectory(sTargetDirectory);
                }
            }
            catch (System.Security.SecurityException)
            {
                logger.WriteLogToTraceListener("CreateDirectory", "Change the Security permission for the generation folder prior to ur codegeneration Folder && C:/temp...");
            }

            catch (Exception ex)
            {
                if (Common.IsDiskFull(ex))
                {
                    throw (new Exception("Disk Full"));
                }
                else
                {
                    if (!Object.Equals(ex.InnerException, null))
                        logger.WriteLogToTraceListener("CreateDirectory", ex.InnerException.Message);
                    else
                        logger.WriteLogToTraceListener("CreateDirectory", ex.Message);
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
                logger.WriteLogToTraceListener("CreateFile", String.Format("Creating File :{0}", sFullFilePath));

                if (!File.Exists(sFullFilePath))
                    File.Create(sFullFilePath).Close();
            }
            catch (System.Security.SecurityException)
            {
                logger.WriteLogToTraceListener("CreateFile", "Change the Security permission for the generation folder prior to ur codegeneration Folder && C:/temp...");
            }

            catch (Exception ex)
            {
                if (Common.IsDiskFull(ex))
                {
                    throw (new Exception("Disk Full"));
                }
                else
                {
                    if (!Object.Equals(ex.InnerException, null))
                        logger.WriteLogToTraceListener("CreateFile", ex.InnerException.Message);
                    else
                        logger.WriteLogToTraceListener("CreateFile", ex.Message);
                }

            }
        }

        /// <summary>
        /// Converts a string with its first letter to uppercase and others to lowercase eg:Mother
        /// </summary>
        /// <param name="inputString">input string</param>
        /// <returns>output as string</returns>
        public static String InitCaps(String inputString)
        {
            if (String.IsNullOrEmpty(inputString))
            {
                return String.Empty;
            }
            return String.Concat(Char.ToUpper(inputString[0]), inputString.Substring(1).ToLower());
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

    }

    internal class CodeDomHelper
    {

        /// <summary>
        /// Adds class summary
        /// </summary>
        /// <param name="_Class"></param>
        /// <param name="_sSummary"></param>
        public static void CreateClassSummary(CodeTypeDeclaration _Class, String _sSummary)
        {
            _Class.Comments.Add(new CodeCommentStatement("<summary>"));
            _Class.Comments.Add(new CodeCommentStatement(String.Format("This class {0}", _sSummary)));
            _Class.Comments.Add(new CodeCommentStatement("</summary>"));
        }

        /// <summary>
        /// Adds Method summary
        /// </summary>
        /// <param name="_Method"></param>
        /// <param name="_sSummary"></param>
        public static void AddMethodSummary(CodeMemberMethod _Method, String _sSummary)
        {
            _Method.Comments.Add(new CodeCommentStatement("<summary>"));
            _Method.Comments.Add(new CodeCommentStatement(String.Format("This method {0}", _sSummary)));
            _Method.Comments.Add(new CodeCommentStatement("</summary>"));
            _Method.Comments.Add(new CodeCommentStatement("**************************************************************************"));
            _Method.Comments.Add(new CodeCommentStatement(String.Format("Function Name		:	{0}", _Method.Name)));
            _Method.Comments.Add(new CodeCommentStatement(String.Format("Author				:	{0}", "ILDotNetCodeGenerator")));
            _Method.Comments.Add(new CodeCommentStatement(String.Format("Date					:	{0}", DateTime.Now.ToShortDateString())));
            _Method.Comments.Add(new CodeCommentStatement(String.Format("Description			:	{0}", _sSummary)));
            _Method.Comments.Add(new CodeCommentStatement("***************************************************************************"));

        }

        /// <summary>
        /// forms and returns declaration statement
        /// </summary>
        /// <param name="variableType">may be of Type 'Type/String'</param>
        /// <param name="variableName">variable name as String</param>
        /// <returns>CodeVariableDeclarationStatement</returns>
        public static CodeVariableDeclarationStatement DeclareVariable(Object variableType, string variableName)
        {
            CodeVariableDeclarationStatement variableDeclaration = new CodeVariableDeclarationStatement();
            variableDeclaration.Name = variableName;
            if (variableType is Type)
                variableDeclaration.Type = new CodeTypeReference((Type)variableType);
            else
                variableDeclaration.Type = new CodeTypeReference((String)variableType);
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
        public static CodeMemberField DeclareMemberField(MemberAttributes accessSpecifier, CodeTypeDeclaration refClass, String sName, Object type, bool NeedInitialization, CodeCommentStatementCollection comments = null, CodeExpression InitExpression = null)
        {
            //create membervariable
            CodeMemberField memberVariable = new CodeMemberField
            {
                Name = sName,
                Type = type is Type ? new CodeTypeReference((Type)type) : new CodeTypeReference((String)type),
                Attributes = accessSpecifier
            };

            //add intialization
            if (NeedInitialization)
            {
                if (InitExpression != null)
                    memberVariable.InitExpression = InitExpression;
                else
                    memberVariable.InitExpression = new CodeObjectCreateExpression(type is Type ? new CodeTypeReference((Type)type) : new CodeTypeReference((String)type));
            }

            //add comments if available
            if (!Object.Equals(comments, null))
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
        /// <param name="VariableType">type of the variable... May be of type 'Type/String'</param>
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
                variableDeclaration.Type = new CodeTypeReference((String)VariableType);

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
        public static CodeCatchClause AddCatchBlock(CodeTryCatchFinallyStatement tryBlock, String sExceptionObjectName = "e")
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
        public static CodeCatchClause AddCatchBlock(CodeTryCatchFinallyStatement tryBlock, Type Objecttype, String ObjectName)
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
        public static CodeThrowExceptionStatement ThrowException(CodeCatchClause catchBlock, String exceptionObject)
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
        /// <param name="Message">can be a CodeExpression/String</param>
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
        /// <param name="sType">type as 'String'</param>
        /// <returns>CodeTypeReferenceExpression</returns>
        public static CodeTypeReferenceExpression TypeReferenceExp(String sType)
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
        /// <param name="sStatement">statement as String</param>
        /// <returns>CodeSnippetStatement</returns>
        public static CodeSnippetStatement SnippetStatement(String sStatement)
        {
            return new CodeSnippetStatement(sStatement);
        }

        /// <summary>
        /// Returns new CodeSnippetExpression with the given Expression
        /// </summary>
        /// <param name="sExpression">Expression As String</param>
        /// <returns>CodeSnippetExpression</returns>
        public static CodeSnippetExpression SnippetExpression(String sExpression)
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
            return new CodeSnippetStatement(String.Format("{0}{1}", new String('\t', tabcount), statement));
        }

        /// <summary>
        /// Returns new CodeVariableReferenceExpression
        /// </summary>
        /// <param name="sVariableName">Name of the variable as String</param>
        /// <returns>CodeVariableReferenceExpression</returns>
        public static CodeVariableReferenceExpression VariableReferenceExp(String sVariableName)
        {
            return new CodeVariableReferenceExpression(sVariableName);
        }

        /// <summary>
        /// Returns new CodeArgumentReferenceExpression
        /// </summary>
        /// <param name="sParamName">Name of the Parameter as string</param>
        /// <returns>CodeArgumentReferenceExpression</returns>
        public static CodeArgumentReferenceExpression ArgumentReferenceExp(String sParamName)
        {
            return new CodeArgumentReferenceExpression(sParamName);
        }

        /// <summary>
        /// returns new CodePropertyReferenceExpression
        /// </summary>
        /// <param name="targetObject">Expression</param>
        /// <param name="sPropertyName">name of the property</param>
        /// <returns>CodePropertyReferenceExpression</returns>
        public static CodePropertyReferenceExpression PropertyReferenceExp(CodeExpression targetObject, String sPropertyName)
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
        public static CodeAssignStatement SetProperty(String sType, String sPropertyName, CodeExpression PropertyValue)
        {
            return new CodeAssignStatement(FieldReferenceExp(TypeReferenceExp(sType), sPropertyName), PropertyValue);
        }

        /// <summary>
        /// returns new CodeParameterDeclarationExpression
        /// </summary>
        /// <param name="ParamType">parameter type as 'Type'</param>
        /// <param name="sParamName">name of the parameter as string</param>
        /// <returns>CodeParameterDeclarationExpression</returns>
        public static CodeParameterDeclarationExpression ParameterDeclarationExp(Type ParamType, String sParamName)
        {
            return new CodeParameterDeclarationExpression(ParamType, sParamName);
        }

        /// <summary>
        /// returns new CodeParameterDeclarationExpression
        /// </summary>
        /// <param name="sParamType">parameter type as string</param>
        /// <param name="sParamName">name of the parameter as string</param>
        /// <returns>CodeParameterDeclarationExpression</returns>
        public static CodeParameterDeclarationExpression ParameterDeclarationExp(String sParamType, String sParamName)
        {
            return new CodeParameterDeclarationExpression(sParamType, sParamName);
        }

        /// <summary>
        /// returns new CodeParameterDeclarationExpression
        /// </summary>
        /// <param name="ParamType">parameter type as CodeTypeReference</param>
        /// <param name="sParamName">name of the parameter as string</param>
        /// <returns>CodeParameterDeclarationExpression</returns>
        public static CodeParameterDeclarationExpression ParameterDeclarationExp(CodeTypeReference ParamType, String sParamName)
        {
            return new CodeParameterDeclarationExpression(ParamType, sParamName);
        }


        /// <summary>
        /// Returns new CodeFieldReferenceExpression
        /// </summary>
        /// <param name="targetObject">TargetObject as codeExpression</param>
        /// <param name="sFieldName">Name of the field as string</param>
        /// <returns>CodeFieldReferenceExpression</returns>
        public static CodeFieldReferenceExpression FieldReferenceExp(CodeExpression targetObject, String sFieldName)
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
        public static CodePropertyReferenceExpression GetProperty(CodeExpression type, String property)
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

        public static CodeObjectCreateExpression ObjectCreateExpression(String ObjectType, CodeExpression[] parameters = null)
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

        public static CodeSnippetExpression CreateSnippetExpression(String expression)
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
            return new CodeSnippetStatement(String.Format("switch({0})", label));
        }

        public static CodeSnippetStatement CaseStatement(string labelValue)
        {
            return new CodeSnippetStatement(String.Format("case \"{0}\":", labelValue));
        }

        public static CodeSnippetStatement StartSwitch()
        {
            return new CodeSnippetStatement("{");
        }

        public static CodeSnippetStatement EndSwitch()
        {
            return new CodeSnippetStatement("}//ENDSWITCH");
        }
    }

    internal class Logger
    {
        private static object _thisLock = new object();

        public Logger()
        {

        }

        /// <summary>
        /// Writes trace
        /// </summary>
        /// <param name="sContext"></param>
        /// <param name="sMessage"></param>
        public void WriteLogToTraceListener(string sContext, string sMessage)
        {
            try
            {
                TraceListener traceWriter = new DefaultTraceListener();

                //for dbgview
                traceWriter.WriteLine(String.Format("{0} : {1}", sContext, sMessage));
            }
            catch
            {
                Console.WriteLine("Exception in Writing trace function.");
            }
        }

        public void WriteLogToFile(string sLogPath, string sMessage)
        {
            try
            {
                if (!File.Exists(sLogPath))
                    Common.CreateFile(sLogPath);
                using (StreamWriter sw = new StreamWriter(sLogPath, true))
                {
                    sw.WriteLine(sMessage);
                    sw.Close();
                    sw.Dispose();
                }

            }
            catch
            {
                Console.WriteLine("Exception in Writing trace to log file.");
            }
        }

    }

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
        public static CodeCatchClause AddCatch(this CodeTryCatchFinallyStatement tryBlock, String sExceptionObjectName = "e")
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
        public static CodeCatchClause AddCatch(this CodeTryCatchFinallyStatement tryBlock, Type Objecttype, String ObjectName)
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
        public static CodeCatchClause AddCatch(this CodeTryCatchFinallyStatement tryBlock, String Objecttype, String ObjectName)
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
        public static CodeThrowExceptionStatement ThrowException(this CodeCatchClause catchBlock, String exceptionObject)
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
