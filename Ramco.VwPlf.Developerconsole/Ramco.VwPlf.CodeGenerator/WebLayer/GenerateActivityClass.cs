using System;
//using System.Collections.Generic;
//using System.Linq;
using System.IO;
using System.CodeDom;
//using System.Threading.Tasks;
using Ramco.VwPlf.CodeGenerator.Callout;

namespace Ramco.VwPlf.CodeGenerator.WebLayer
{
    internal class GenerateActivityClass : AbstractCSFileGenerator
    {
        private Activity _activity;
        private ECRLevelOptions _ecrOptions;

        public GenerateActivityClass(Activity activity, ref ECRLevelOptions ecrOptions) : base(ecrOptions)
        {
            this._ecrOptions = ecrOptions;
            this._activity = activity;
            base._objectType = ObjectType.Activity;
            base._targetFileName = "activity";
            base._targetDir = Path.Combine(_ecrOptions.GenerationPath, _ecrOptions.Platform, _ecrOptions.Customer, _ecrOptions.Project, _ecrOptions.Ecrno, "Updated", _ecrOptions.Component, "Source", "ILBO", activity.Name);
        }

        public override void CreateNamespace()
        {
            try
            {
                base._csFile.NameSpace.Name = _activity.Name;
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("CreateNamespace->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }

        public override void ImportNamespace()
        {
            try
            {
                base._csFile.ReferencedNamespace.Add("System");
                base._csFile.ReferencedNamespace.Add("System.Web");
                base._csFile.ReferencedNamespace.Add("System.Collections");
                base._csFile.ReferencedNamespace.Add("System.Collections.Generic");
                base._csFile.ReferencedNamespace.Add("System.Diagnostics");
                base._csFile.ReferencedNamespace.Add("Ramco.VW.RT.Web.Core");
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("ImportNamespace->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }

        public override void AddCustomAttributes()
        {
            try
            {
                base._csFile.AssemblyAttributeCollection.Add(new CodeAttributeDeclaration(new CodeTypeReference(typeof(System.Reflection.AssemblyDescriptionAttribute)),
                                                                new CodeAttributeArgument(PrimitiveExpression(_ecrOptions.AssemblyDescription))));
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("AddCustomAttributes->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }

        public override void CreateClasses()
        {
            try
            {
                CodeAttributeDeclarationCollection AttributeCollection = new CodeAttributeDeclarationCollection();
                AttributeCollection.Add(new CodeAttributeDeclaration("Serializable"));

                CodeTypeDeclaration activityCls = new CodeTypeDeclaration
                {
                    Name = "activity",
                    IsClass = true,
                    CustomAttributes = AttributeCollection
                };
                CreateClassSummary(activityCls, string.Format("defines all the methods of {0} class", "Activity"));


                activityCls.Attributes = MemberAttributes.Assembly;
                activityCls.BaseTypes.Add(new CodeTypeReference { BaseType = "CActivity" });

                AddMemberFields(ref activityCls);
                AddMemberFunctions(ref activityCls);

                base._csFile.UserDefinedTypes.Add(activityCls);
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("CreateClasses->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }

        public override void AddMemberFields(ref CodeTypeDeclaration classObj)
        {
            try
            {
                DeclareMemberField(MemberAttributes.Private, classObj, "htContextItems", "Dictionary<string, Object>", true, null);

                foreach (ILBO ilbo in _activity.ILBOs)
                {
                    DeclareMemberField(MemberAttributes.Private, classObj, string.Format("m_o{0}", ilbo.Code), ilbo.Code, true, null, SnippetExpression("null"));
                    DeclareMemberField(MemberAttributes.Private, classObj, string.Format("m_o{0}_Cyclic", ilbo.Code), string.Format("Dictionary<long,{0}>", ilbo.Code), true, null);
                }
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("AddMemberFields->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
        }

        public override void AddMemberFunctions(ref CodeTypeDeclaration classObj)
        {
            classObj.Members.Add(CreateConstructor());
            classObj.Members.Add(GetILBO());
            classObj.Members.Add(DisposeILBO());
            classObj.Members.Add(GetILBOEx());
            classObj.Members.Add(DisposeILBOEx());
            classObj.Members.Add(GetContextValue());
            classObj.Members.Add(SetContextValue());
            classObj.Members.Add(FillMessageObject());
        }

        /// <summary>
        /// creates constructor method.
        /// </summary>
        /// <returns>constructor method as 'CodeMemberMethod'</returns>
        private CodeConstructor CreateConstructor()
        {
            CodeConstructor _CodeConstructor = null;
            try
            {
                _CodeConstructor = new CodeConstructor
                {
                    Name = "activity",
                    Attributes = MemberAttributes.Public
                };
                _CodeConstructor.Statements.Add(AssignVariable("ComponentName", _ecrOptions.Component.ToLowerInvariant()));
                _CodeConstructor.Statements.Add(AssignVariable("ActivityName", _activity.Name));
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("CreateConstructor->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
            return _CodeConstructor;
        }


        /// <summary>
        /// constructs getilbo method
        /// </summary>
        /// <returns>CodeMemberMethod</returns>
        private CodeMemberMethod GetILBO()
        {

            CodeMemberMethod GetILBO = null;
            CodeMethodInvokeExpression MethodInvokation = null;
            try
            {
                GetILBO = new CodeMemberMethod
                {
                    Name = "GetILBO",
                    Attributes = MemberAttributes.Public | MemberAttributes.Override,
                    ReturnType = new CodeTypeReference("IILBO")
                };

                //method parameters
                GetILBO.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference(typeof(System.String)), "sILBOCode"));

                //method summary
                AddMethodSummary(GetILBO, "Creates/Gets the Object Handle of the ILBO");

                CodeTryCatchFinallyStatement TryBlock = AddTryBlock(GetILBO);
                TryBlock.TryStatements.Add(AddTrace(GetILBO, TraceSeverity.Info));

                TryBlock.TryStatements.Add(SnippetStatement("switch(sILBOCode)"));

                TryBlock.TryStatements.Add(SnippetStatement("{"));

                //for each ilbo
                foreach (ILBO ilbo in this._activity.ILBOs)
                {
                    string mILBOName = string.Format("m_o{0}", ilbo.Code.ToLower());

                    TryBlock.TryStatements.Add(SnippetStatement(string.Format("case \"{0}\":", ilbo.Code)));

                    CodeConditionStatement ILBONullCheck = new CodeConditionStatement(new CodeBinaryOperatorExpression(TypeReferenceExp(string.Format("{0}", mILBOName)), CodeBinaryOperatorType.IdentityEquality, SnippetExpression("null")));
                    ILBONullCheck.TrueStatements.Add(AssignVariable(string.Format("{0}", mILBOName), new CodeObjectCreateExpression(string.Format("{0}", ilbo.Code))));

                    TryBlock.TryStatements.Add(ILBONullCheck);
                    TryBlock.TryStatements.Add(ReturnExpression(VariableReferenceExp(string.Format("{0}", mILBOName))));
                }

                TryBlock.TryStatements.Add(SnippetStatement(string.Format("default:")));

                MethodInvokation = MethodInvocationExp(BaseReferenceExp(), "GetILBO");
                AddParameters(MethodInvokation, new Object[] { VariableReferenceExp("sILBOCode") });
                TryBlock.TryStatements.Add(ReturnExpression(MethodInvokation));

                TryBlock.TryStatements.Add(SnippetStatement("}//ENDSWITCH")); //close braces for switch case                
                CodeCatchClause catchBlock = AddCatchBlock(TryBlock);
                catchBlock.Statements.Add(FillMessageObject(GetILBO, SnippetExpression("string.Format(\"activity : GetILBO(sILBOCode=\\\"{0}\\\")\",sILBOCode)")));
                ThrowException(catchBlock);
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("GetILBO->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
            return GetILBO;
        }


        /// <summary>
        /// Adds invocation of FillMessageObject 
        /// </summary>
        /// <param name="catchBlock"></param>
        /// <param name="function"></param>
        /// <returns></returns>
        private CodeMethodInvokeExpression FillMessageObject(CodeMemberMethod function, Object sMessage)
        {
            CodeMethodInvokeExpression newMethodInvokation;
            newMethodInvokation = MethodInvocationExp(ThisReferenceExp(), "FillMessageObject");

            if (sMessage is CodeSnippetExpression)
                AddParameters(newMethodInvokation, new Object[] { (CodeSnippetExpression)sMessage });
            else
                AddParameters(newMethodInvokation, new Object[] { (string)sMessage });

            switch (function.Name.ToLower())
            {
                case "getilbo":
                    AddParameters(newMethodInvokation, new Object[] { "Act0001" });
                    break;
                case "disposeilbo":
                    AddParameters(newMethodInvokation, new Object[] { "Act0002" });
                    break;
                case "getilboex":
                    AddParameters(newMethodInvokation, new Object[] { "Act0001" });
                    break;
                case "disposeilboex":
                    AddParameters(newMethodInvokation, new Object[] { "Act0002" });
                    break;
                case "getcontextvalue":
                    AddParameters(newMethodInvokation, new Object[] { "Act0003" });
                    break;
                case "setcontextvalue":
                    AddParameters(newMethodInvokation, new Object[] { "Act0004" });
                    break;
                default:
                    break;
            }
            AddFieldRefParameter(newMethodInvokation, "e", "Message");
            return newMethodInvokation;
        }

        /// <summary>
        /// Constructs DisposeILBO method
        /// </summary>
        /// <returns>CodeMemberMethod</returns>
        private CodeMemberMethod DisposeILBO()
        {

            CodeMemberMethod DisposeILBO = null;
            CodeMethodInvokeExpression MethodInvokation = null;
            try
            {
                DisposeILBO = new CodeMemberMethod
                {
                    Name = "DisposeILBO",
                    Attributes = MemberAttributes.Public | MemberAttributes.Override
                };

                //method parameters
                DisposeILBO.Parameters.Add(new CodeParameterDeclarationExpression(typeof(string), "sILBOCode"));

                //method summary
                AddMethodSummary(DisposeILBO, "Disposes the Object Handle of the ILBO");

                //try block
                CodeTryCatchFinallyStatement TryBlock = CodeDomHelper.AddTryBlock(DisposeILBO);
                TryBlock.TryStatements.Add(AddTrace(DisposeILBO, TraceSeverity.Info));

                //switch case
                TryBlock.TryStatements.Add(SnippetStatement("switch(sILBOCode)"));
                TryBlock.TryStatements.Add(SnippetStatement("{"));


                //case for each ilbo
                foreach (ILBO ilbo in this._activity.ILBOs)
                {
                    string mILBOName = string.Format("m_o{0}", ilbo.Code);

                    TryBlock.TryStatements.Add(SnippetStatement(string.Format("case \"{0}\":", ilbo.Code)));

                    CodeConditionStatement ILBONullCheck = new CodeConditionStatement(new CodeBinaryOperatorExpression(TypeReferenceExp(string.Format("{0}", mILBOName)), CodeBinaryOperatorType.IdentityInequality, SnippetExpression("null")));

                    //ILBONullCheck.TrueStatements.Add(MethodInvocationExp(TypeReferenceExp(mILBOName), "Clear"));
                    ILBONullCheck.TrueStatements.Add(AssignVariable(mILBOName, SnippetExpression("null")));

                    AddExpressionToTryBlock(TryBlock, ILBONullCheck);

                    TryBlock.TryStatements.Add(SnippetExpression("break"));
                }

                //default case
                TryBlock.TryStatements.Add(SnippetStatement(string.Format("default:")));
                MethodInvokation = MethodInvocationExp(BaseReferenceExp(), "DisposeILBO");
                AddParameters(MethodInvokation, new Object[] { VariableReferenceExp("sILBOCode") });
                TryBlock.TryStatements.Add(MethodInvokation);
                TryBlock.TryStatements.Add(SnippetExpression("break"));


                TryBlock.TryStatements.Add(SnippetStatement("}//ENDSWITCH"));//close braces for switch case

                CodeCatchClause catchBlock = CodeDomHelper.AddCatchBlock(TryBlock);
                catchBlock.Statements.Add(FillMessageObject(DisposeILBO, SnippetExpression("string.Format(\"activity : DisposeILBO(sILBOCode=\\\"{0}\\\")\",sILBOCode)")));
                CodeDomHelper.ThrowException(catchBlock);
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("DisposeILBO->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
            return DisposeILBO;
        }


        /// <summary>
        /// Construct GetILBOEx method
        /// </summary>
        /// <returns>CodeMemberMethod</returns>
        private CodeMemberMethod GetILBOEx()
        {

            CodeMemberMethod GetILBOEx = null;
            CodeMethodInvokeExpression MethodInvokation = null;
            try
            {
                GetILBOEx = new CodeMemberMethod
                {
                    Name = "GetILBOEx",
                    Attributes = MemberAttributes.Public | MemberAttributes.Override,
                    ReturnType = new CodeTypeReference("IILBO")
                };

                //method Parameters
                GetILBOEx.Parameters.Add(new CodeParameterDeclarationExpression(typeof(string), "sILBOCode"));
                GetILBOEx.Parameters.Add(new CodeParameterDeclarationExpression(typeof(long), "lILBOIndex"));

                //method summary
                AddMethodSummary(GetILBOEx, "Creates/Gets the Object Handle of the ILBO");

                CodeTryCatchFinallyStatement TryBlock = AddTryBlock(GetILBOEx);
                TryBlock.TryStatements.Add(AddTrace(GetILBOEx, TraceSeverity.Info));

                TryBlock.TryStatements.Add(SnippetStatement("switch(sILBOCode)"));
                TryBlock.TryStatements.Add(SnippetStatement("{"));

                //GlobalVar.codeGeneration.activities[sName].ILBOList;
                foreach (ILBO ilbo in this._activity.ILBOs)
                {
                    string mILBOName = string.Format("m_o{0}", ilbo.Code);

                    TryBlock.TryStatements.Add(SnippetStatement(string.Format("case \"{0}\":", ilbo.Code)));

                    CodeConditionStatement ifCondition = new CodeConditionStatement();

                    //if condition
                    MethodInvokation = MethodInvocationExp(TypeReferenceExp(string.Format("{0}_Cyclic", mILBOName)), "ContainsKey");
                    AddParameters(MethodInvokation, new Object[] { new CodeBinaryOperatorExpression(VariableReferenceExp("lILBOIndex"), CodeBinaryOperatorType.Subtract, PrimitiveExpression(1)) });
                    ifCondition.Condition = new CodeBinaryOperatorExpression(MethodInvokation, CodeBinaryOperatorType.IdentityInequality, PrimitiveExpression(true));

                    //content for the if condition
                    MethodInvokation = MethodInvocationExp(TypeReferenceExp(string.Format("{0}_Cyclic", mILBOName)), "Add");
                    AddParameters(MethodInvokation, new Object[] { new CodeBinaryOperatorExpression(VariableReferenceExp("lILBOIndex"), CodeBinaryOperatorType.Subtract, PrimitiveExpression(1)), new CodeObjectCreateExpression(string.Format("{0}", ilbo.Code)) });
                    ifCondition.TrueStatements.Add(MethodInvokation);

                    //adding if condition to tryblock
                    TryBlock.TryStatements.Add(ifCondition);

                    //return statement for this case
                    TryBlock.TryStatements.Add(ReturnExpression(new CodeArrayIndexerExpression(VariableReferenceExp(string.Format("{0}_Cyclic", mILBOName)), new CodeBinaryOperatorExpression(VariableReferenceExp("lILBOIndex"), CodeBinaryOperatorType.Subtract, PrimitiveExpression(1)))));

                }

                //default case
                TryBlock.TryStatements.Add(SnippetStatement(string.Format("default:")));
                MethodInvokation = MethodInvocationExp(BaseReferenceExp(), GetILBOEx.Name);
                AddParameters(MethodInvokation, new Object[] { ArgumentReferenceExp(GetILBOEx.Parameters[0].Name), ArgumentReferenceExp(GetILBOEx.Parameters[1].Name) });
                TryBlock.TryStatements.Add(ReturnExpression(MethodInvokation));

                TryBlock.TryStatements.Add(new CodeSnippetStatement("}//ENDSWITCH")); // close braces for switch case

                CodeCatchClause catchBlock = CodeDomHelper.AddCatchBlock(TryBlock);
                catchBlock.Statements.Add(AddTrace(GetILBOEx, TraceSeverity.Error));
                CodeDomHelper.ThrowException(catchBlock);
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("GetILBOEx->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
            return GetILBOEx;
        }


        /// <summary>
        /// Constructs DisposeILBOEx
        /// </summary>
        /// <returns>CodeMemberMethod</returns>
        private CodeMemberMethod DisposeILBOEx()
        {

            CodeMemberMethod DisposeILBOEx = null;
            CodeMethodInvokeExpression MethodInvokation = null;
            try
            {
                DisposeILBOEx = new CodeMemberMethod
                {
                    Name = "DisposeILBOEx",
                    Attributes = MemberAttributes.Public | MemberAttributes.Override
                };

                //method parameters
                DisposeILBOEx.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference(typeof(System.String)), "sILBOCode"));
                DisposeILBOEx.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference(typeof(long)), "lILBOIndex"));

                //method summary
                AddMethodSummary(DisposeILBOEx, "Disposes the Object Handle of the ILBO");

                CodeTryCatchFinallyStatement TryBlock = CodeDomHelper.AddTryBlock(DisposeILBOEx);
                TryBlock.TryStatements.Add(AddTrace(DisposeILBOEx, TraceSeverity.Info));

                //switch case
                TryBlock.TryStatements.Add(SnippetStatement("switch(sILBOCode)"));
                TryBlock.TryStatements.Add(SnippetStatement("{"));

                //for each ilbo
                foreach (ILBO ilbo in this._activity.ILBOs)
                {
                    string mILBOName = string.Format("m_o{0}", ilbo.Code);

                    TryBlock.TryStatements.Add(SnippetStatement(string.Format("case \"{0}\":", ilbo.Code)));

                    CodeConditionStatement ILBOIndexCheck = new CodeConditionStatement(new CodeBinaryOperatorExpression(ArgumentReferenceExp(DisposeILBOEx.Parameters[1].Name), CodeBinaryOperatorType.GreaterThan, PrimitiveExpression(0)));

                    //ILBOIndexCheck.TrueStatements.Add( MethodInvocationExp( CastExpression(ilbo.Name.ToLowerInvariant(), ArrayIndexerExpression(string.Format("{0}_Cyclic", mILBOName), BinaryOpertorExpression(ArgumentReferenceExp("lILBOIndex"), CodeBinaryOperatorType.Subtract, PrimitiveExpression(1)))),"Clear"));
                    MethodInvokation = MethodInvocationExp(TypeReferenceExp(string.Format("{0}_Cyclic", mILBOName)), "Remove");
                    AddParameters(MethodInvokation, new Object[] { new CodeBinaryOperatorExpression(ArgumentReferenceExp(DisposeILBOEx.Parameters[1].Name), CodeBinaryOperatorType.Subtract, PrimitiveExpression(1)) });
                    ILBOIndexCheck.TrueStatements.Add(MethodInvokation);

                    MethodInvokation = MethodInvocationExp(TypeReferenceExp(string.Format("{0}_Cyclic", mILBOName)), "Clear");
                    ILBOIndexCheck.FalseStatements.Add(MethodInvokation);

                    TryBlock.TryStatements.Add(ILBOIndexCheck);
                    TryBlock.TryStatements.Add(SnippetExpression("break"));

                }

                //default case
                TryBlock.TryStatements.Add(SnippetStatement(string.Format("default:")));

                MethodInvokation = MethodInvocationExp(BaseReferenceExp(), DisposeILBOEx.Name);
                AddParameters(MethodInvokation, new Object[] { ArgumentReferenceExp(DisposeILBOEx.Parameters[0].Name), ArgumentReferenceExp(DisposeILBOEx.Parameters[1].Name) });
                TryBlock.TryStatements.Add(MethodInvokation);

                TryBlock.TryStatements.Add(SnippetExpression("break"));

                TryBlock.TryStatements.Add(SnippetStatement("}//ENDSWITCH")); //close braces for switch case

                CodeCatchClause catchBlock = CodeDomHelper.AddCatchBlock(TryBlock);
                catchBlock.Statements.Add(FillMessageObject(DisposeILBOEx, SnippetExpression("string.Format(\"activity : DisposeILBOEx(sILBOCode=\\\"{0}\\\")\",sILBOCode)")));
                ThrowException(catchBlock);
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("DiposeILBOEx->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
            return DisposeILBOEx;
        }


        /// <summary>
        /// Constructs GetContextValue method
        /// </summary>
        /// <returns>CodeMemberMethod</returns>
        private CodeMemberMethod GetContextValue()
        {

            CodeMemberMethod GetContextValue = null;

            try
            {
                GetContextValue = new CodeMemberMethod
                {
                    Name = "GetContextValue",
                    Attributes = MemberAttributes.Public | MemberAttributes.Override,
                    ReturnType = new CodeTypeReference(typeof(object))
                };

                //method parameters
                GetContextValue.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference(typeof(string)), "sContextName"));

                //method summary
                AddMethodSummary(GetContextValue, "Gets the Context Value Information");

                CodeTryCatchFinallyStatement TryBlock = AddTryBlock(GetContextValue);
                TryBlock.TryStatements.Add(AddTrace(GetContextValue, TraceSeverity.Info));
                TryBlock.TryStatements.Add(ReturnExpression(new CodeIndexerExpression(new CodeFieldReferenceExpression(ThisReferenceExp(), "htContextItems"), ArgumentReferenceExp("sContextName"))));

                CodeCatchClause catchBlock = AddCatchBlock(TryBlock);
                catchBlock.Statements.Add(FillMessageObject(GetContextValue, SnippetExpression("string.Format(\"activity : GetContextValue(sContextName=\\\"{0}\\\")\",sContextName)")));
                ThrowException(catchBlock);
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("GetContextValue->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
            return GetContextValue;
        }


        /// <summary>
        /// Constructs SetContextValue method
        /// </summary>
        /// <returns>CodeMemberMethod</returns>
        private CodeMemberMethod SetContextValue()
        {

            CodeMemberMethod SetContextValue = null;
            try
            {
                SetContextValue = new CodeMemberMethod
                {
                    Name = "SetContextValue",
                    Attributes = MemberAttributes.Public | MemberAttributes.Override
                };

                //method parameters
                SetContextValue.Parameters.Add(new CodeParameterDeclarationExpression(typeof(string), "sContextName"));
                SetContextValue.Parameters.Add(new CodeParameterDeclarationExpression(typeof(object), "sContextValue"));

                //method summary
                AddMethodSummary(SetContextValue, "Sets the Context Value Information");

                CodeTryCatchFinallyStatement TryBlock = AddTryBlock(SetContextValue);
                TryBlock.TryStatements.Add(AddTrace(SetContextValue, TraceSeverity.Info));
                TryBlock.TryStatements.Add(AssignVariable("htContextItems", new CodeArgumentReferenceExpression("sContextValue"), -1, true, "sContextName"));

                CodeCatchClause catchBlock = AddCatchBlock(TryBlock);
                catchBlock.Statements.Add(FillMessageObject(SetContextValue, SnippetExpression("string.Format(\"activity : SetContextValue(sContextName=\\\"{0}\\\",sContextValue=\\\"{1}\\\")\",sContextName,sContextValue)")));
                ThrowException(catchBlock);
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("SetContextValue->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
            return SetContextValue;
        }


        /// <summary>
        /// Constructs FillMessageObject
        /// </summary>
        /// <returns>CodeMemberMethod</returns>
        private CodeMemberMethod FillMessageObject()
        {

            CodeMemberMethod FillMessageObject = null;
            try
            {
                FillMessageObject = new CodeMemberMethod
                {
                    Name = "FillMessageObject",
                    Attributes = MemberAttributes.Private
                };

                //method parameters
                FillMessageObject.Parameters.Add(new CodeParameterDeclarationExpression(typeof(string), "sMethod"));
                FillMessageObject.Parameters.Add(new CodeParameterDeclarationExpression(typeof(string), "sErrNumber"));
                FillMessageObject.Parameters.Add(new CodeParameterDeclarationExpression(typeof(string), "sErrMessage"));

                //method summary
                AddMethodSummary(FillMessageObject, "Fills the Message object when an error occurs");

                CodeTryCatchFinallyStatement TryBlock = AddTryBlock(FillMessageObject);

                TryBlock.TryStatements.Add(AddTrace(FillMessageObject, TraceSeverity.Info));
                TryBlock.TryStatements.Add(DeclareVariableAndAssign("ISessionManager", "ISManager", true, SnippetExpression("(ISessionManager)System.Web.HttpContext.Current.Session[\"SessionManager\"]")));
                TryBlock.TryStatements.Add(DeclareVariableAndAssign("IMessage", "Imsg", true, SnippetExpression("ISManager.GetMessageObject()")));

                CodeMethodInvokeExpression AddMessage = MethodInvocationExp(TypeReferenceExp("Imsg"), "AddMessage");
                AddParameters(AddMessage, new object[] { ArgumentReferenceExp("sErrNumber"), ArgumentReferenceExp("sErrMessage"), ArgumentReferenceExp("sMethod"), GetProperty(TypeReferenceExp(typeof(string)), "Empty"), PrimitiveExpression("5") });
                TryBlock.TryStatements.Add(AddMessage);

                CodeCatchClause catchBlock = CodeDomHelper.AddCatchBlock(TryBlock);
                catchBlock.Statements.Add(AddTrace(FillMessageObject, TraceSeverity.Error));
                CodeDomHelper.ThrowException(catchBlock);
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("FillMessageObject->{0}", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message));
            }
            return FillMessageObject;
        }


        /// <summary>
        /// 
        /// </summary>
        private enum TraceSeverity
        {
            Info,
            Error
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sFunctionName"></param>
        /// <returns></returns>
        private string GetErrorCode(string sFunctionName)
        {
            string sErrorCode = string.Empty;
            switch (sFunctionName.ToLower())
            {
                case "getilbo":
                    sErrorCode = "ACT0001";
                    break;
                case "disposeilbo":
                    sErrorCode = "ACT0002";
                    break;
                case "getilboex":
                    sErrorCode = "ACT0003";
                    break;
                case "disposeilboex":
                    sErrorCode = "ACT0004";
                    break;
                case "getcontextvalue":
                    sErrorCode = "ACT0005";
                    break;
                case "setcontextvalue":
                    sErrorCode = "ACT0006";
                    break;
                case "FillMessageObject":
                    sErrorCode = "ACT0007";
                    break;
                default:
                    break;
            }
            return sErrorCode;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="codeMethod"></param>
        /// <param name="traceSevertiy"></param>
        /// <returns></returns>
        private CodeMethodInvokeExpression AddTrace(CodeMemberMethod codeMethod, TraceSeverity traceSevertiy)
        {
            CodeMethodInvokeExpression newMethodInvocation;
            newMethodInvocation = MethodInvocationExp(TypeReferenceExp("Trace"), "WriteLineIf");

            string sTraceSeverity = "TraceInfo";

            if (traceSevertiy == TraceSeverity.Error)
                sTraceSeverity = "TraceError";

            AddFieldRefParameter(newMethodInvocation, "SessionManager.m_ILActTraceSwitch", sTraceSeverity);


            string sMethodName = codeMethod.Name;
            string sMessage = string.Format("\"{0}(", sMethodName);

            foreach (CodeParameterDeclarationExpression Parameter in codeMethod.Parameters)
            {
                sMessage = sMessage + string.Format("{0} = \\\"\" + {0} + \"\\\", ", Parameter.Name.ToString());
            }

            sMessage = sMessage.Substring(0, sMessage.Length - 2);
            string sErrorCode = GetErrorCode(sMethodName);

            if (traceSevertiy == TraceSeverity.Error)
                sMessage = sMessage + string.Format(")\", \"" + sErrorCode + " - Exception - e.Message\"");
            else
                sMessage = sMessage + string.Format(")\", \"{0}\"", sErrorCode);

            AddParameters(newMethodInvocation, new Object[] { new CodeSnippetExpression(sMessage) });
            return newMethodInvocation;
        }


        /// <summary>
        /// Call this function to generate taskcallout
        /// </summary>
        /// <returns></returns>
        private bool GenerateTaskCallout(string sConnectionString)
        {
            string sLogFile = System.IO.Path.Combine(_ecrOptions.GenerationPath, _ecrOptions.Platform, _ecrOptions.Customer, _ecrOptions.Project, _ecrOptions.Ecrno, _ecrOptions.Ecrno + ".txt");
            return new CalloutWrapper(_ecrOptions.Customer, _ecrOptions.Project, _ecrOptions.Process, _ecrOptions.Component, _ecrOptions.Ecrno, _ecrOptions.GenerationPath, sLogFile, sConnectionString).Generate();
        }

        public bool Generate(string sConnectionString)
        {
            bool bStatus = false;

            try
            {
                base._logger.WriteLogToFile(string.Empty, string.Format("generating activity - {0}", this._activity.Name));

                #region generate activity.cs                
                try
                {
                    base.Generate();
                }
                catch (Exception ex)
                {
                    ///Write Log if any error occured in activity cs generation and proceed
                    _ecrOptions.ErrorCollection.Add(new Error(ObjectType.Activity, "activity cs for the activity : " + this._activity.Name, ex.InnerException != null ? ex.InnerException.Message : ex.Message));

                    base._logger.WriteLogToFile("GenerateActivityClass->Generate", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message, bError: true);
                }
                #endregion

                //Parallel.ForEach(_Activity.ilbos, (ilbo) =>
                //    {
                foreach (ILBO ilbo in _activity.ILBOs)
                {
                    #region generate [ilbo].cs
                    try
                    {
                        GenerateILBOClass objILBOGenerator = new GenerateILBOClass(this._activity, ilbo, ref this._ecrOptions);
                        objILBOGenerator.Generate();
                    }
                    catch (Exception ex)
                    {
                        _ecrOptions.ErrorCollection.Add(new Error(ObjectType.Activity, "ilbo : " + ilbo.Code, ex.InnerException != null ? ex.InnerException.Message : ex.Message));

                        ///Write Log if any error occured in ilbo cs generation and proceed
                        _logger.WriteLogToFile("GenerateActivityClass->Generate", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message, bError: true);
                    }
                    #endregion

                    #region generate [ilbo]_tr.cs
                    if (ilbo.HasTree)
                    {
                        _logger.WriteLogToFile(string.Empty, "creating tree class");
                        TreeClassGenerator objTreeClassGenerator = new TreeClassGenerator(_activity, ilbo, ref this._ecrOptions);
                        try
                        {
                            objTreeClassGenerator.Generate();
                        }
                        catch (Exception ex)
                        {
                            _ecrOptions.ErrorCollection.Add(new Error(ObjectType.Activity, "Tree cs for the ilbo : " + ilbo.Code, ex.InnerException != null ? ex.InnerException.Message : ex.Message));

                            ///Write Log if any error occured in tree class generation and proceed
                            _logger.WriteLogToFile("GenerateActivityClass->Generate", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message, bError: true);
                        }
                    }
                    #endregion

                    #region generate [ilbo]_ch.cs
                    if (ilbo.HasChart)
                    {
                        _logger.WriteLogToFile(string.Empty, "creating chart class");
                        ChartClassGenerator objChartClassGenerator = new ChartClassGenerator(_activity, ilbo, ref this._ecrOptions);
                        try
                        {
                            objChartClassGenerator.Generate();
                        }
                        catch (Exception ex)
                        {
                            _ecrOptions.ErrorCollection.Add(new Error(ObjectType.Activity, "chart cs for the ilbo : " + ilbo.Code, ex.InnerException != null ? ex.InnerException.Message : ex.Message));

                            ///Write Log if any error occured in chart cs generation and proceed
                            _logger.WriteLogToFile("GenerateActivityClass->Generate", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message, bError: true);
                        }
                    }
                    #endregion

                    #region taskcallout
                    if (ilbo.HasPreTaskCallout || ilbo.HasPostTaskCallout)
                    {
                        _logger.WriteLogToFile(string.Empty, "generating Task callout..");

                        try
                        {
                            GenerateTaskCallout(sConnectionString);
                        }
                        catch (Exception ex)
                        {
                            _ecrOptions.ErrorCollection.Add(new Error(ObjectType.TaskCallout, "TaskCallout for the ilbo : " + ilbo.Code, ex.InnerException != null ? ex.InnerException.Message : ex.Message));

                            ///Write Log if any error occured in chart cs generation and proceed
                            _logger.WriteLogToFile("GenerateActivityClass->Generate", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message, bError: true);
                        }
                    }
                    #endregion  
                }
                //});

                bStatus = true;
            }
            catch (Exception ex)
            {
                base._logger.WriteLogToFile("GenerateActivityClass->Generate", !object.Equals(ex.InnerException, null) ? ex.InnerException.Message : ex.Message, bError: true);
            }
            return bStatus;
        }
    }
}
