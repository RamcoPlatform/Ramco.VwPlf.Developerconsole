/***************************************************************************************************
 * Case Id          :   TECH-9466,TECH-9438
 * Modified Date    :   10 May 2017
 * Modified By      :   Madhan Sekar M
 * Case Description :   Namespace format changed, Directory structure changed.
 ***************************************************************************************************
  * Case Id          :   TECH-XXXX
 * Modified Date    :   15 May 2017
 * Modified By      :   Madhan Sekar M
 * Case Description :   initialization expression in builoutsegment's looping statement has been modified
 ***************************************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.CodeDom;
using System.Reflection;

namespace Ramco.VwPlf.CodeGenerator.Callout
{
    internal class GenerateCallout : CodeDomHelper
    {
        DataStoreCallout dataStoreCallout;
        CodeUnit Calloutcls;
        Logger logger;

        public GenerateCallout(DataStoreCallout _dataStoreCallout)
        {
            logger = new Logger();
            Calloutcls = new CodeUnit();
            dataStoreCallout = _dataStoreCallout;
            Calloutcls.CompileUnit = new System.CodeDom.CodeCompileUnit();
            Calloutcls.NameSpace = new System.CodeDom.CodeNamespace();
            Calloutcls.UserDefinedTypes = new List<System.CodeDom.CodeTypeDeclaration>();
        }


        public void Generate()
        {
            CreateNamespace(String.Format("Ramco.VW.TaskCallout.{0}", dataStoreCallout.ComponentName));

            ImportNamespace();

            CreateClass(dataStoreCallout);

            Calloutcls.StitchCSFile();

            Calloutcls.WriteCSFile(dataStoreCallout.CalloutName, System.IO.Path.Combine(GlobalVar.SourcePath, dataStoreCallout.CalloutName));
        }

        private void CreateNamespace(string sDataStoreNamespace)
        {
            Calloutcls.NameSpace.Name = sDataStoreNamespace;
        }

        private void ImportNamespace()
        {
            Calloutcls.ReferencedNamespace.Add("System");
            Calloutcls.ReferencedNamespace.Add("System.Collections.Generic");
            //Calloutcls.ReferencedNamespace.Add("System.Linq");
            Calloutcls.ReferencedNamespace.Add("System.Text");
            Calloutcls.ReferencedNamespace.Add("Ramco.VW.RT.Web.TaskCallout");
            Calloutcls.ReferencedNamespace.Add(String.Format("Ramco.VW.TaskCallout.{0}.{1}datastore", dataStoreCallout.ComponentName, dataStoreCallout.CalloutName));
        }

        private void CreateClass(DataStoreCallout dataStoreCallout)
        {
            CodeTypeDeclaration newUserDefinedType = new CodeTypeDeclaration
            {
                Name = dataStoreCallout.CalloutName,
                IsClass = true,
                Attributes = MemberAttributes.Public | MemberAttributes.Final,
                TypeAttributes = TypeAttributes.Sealed
            };
            newUserDefinedType.BaseTypes.Add(new CodeTypeReference { BaseType = "CVWTaskCallout" });
            Calloutcls.UserDefinedTypes.Add(newUserDefinedType);

            CreateMemberFields(newUserDefinedType, dataStoreCallout);
            CreateMemberFunctions(newUserDefinedType, dataStoreCallout);
        }

        private void CreateMemberFields(CodeTypeDeclaration userDefinedType, DataStoreCallout dataStoreCallout)
        {

            foreach (string segmentname in dataStoreCallout.CalloutSegments.Select(s => s.Name).Distinct())
            {

                string segmentInst = dataStoreCallout.CalloutSegments.Where(s => s.Name == segmentname).Select(s => s.Inst).First();

                if (segmentInst == "1")
                    DeclareMemberField(MemberAttributes.Private, userDefinedType, segmentname, "List<c" + segmentname + ">", true, null, ObjectCreateExpression("List<c" + segmentname + ">"));
                else
                    DeclareMemberField(MemberAttributes.Private, userDefinedType, segmentname, "c_" + segmentname, true, null, ObjectCreateExpression("c_" + segmentname));
            }
        }

        private void CreateMemberFunctions(CodeTypeDeclaration UserDefinedType, DataStoreCallout dataStoreCallout)
        {
            UserDefinedType.Members.Add(ExecuteMethod());
            UserDefinedType.Members.Add(GetSegment());
            UserDefinedType.Members.Add(GetSegmentWithInstanceParam(dataStoreCallout));
            UserDefinedType.Members.Add(BuildOutSegments(dataStoreCallout));
        }

        private CodeMemberMethod ExecuteMethod()
        {
            CodeMemberMethod ExecuteMethod = new CodeMemberMethod
            {
                Name = "ExecuteMethod",
                Attributes = MemberAttributes.Public | MemberAttributes.Override,
            };
            CodeParameterDeclarationExpression parameter1 = new CodeParameterDeclarationExpression(typeof(string), "inTD");
            CodeParameterDeclarationExpression parameter2 = new CodeParameterDeclarationExpression(typeof(string), "outTD");
            parameter2.Direction = FieldDirection.Out;
            ExecuteMethod.Parameters.Add(parameter1);
            ExecuteMethod.Parameters.Add(parameter2);

            ExecuteMethod.Statements.Add(MethodInvocationExp(ThisReferenceExp(), "Callout_Pre_Process").AddParameters(new CodeExpression[] { VariableReferenceExp(parameter1.Name) }));
            ExecuteMethod.Statements.Add(AssignVariable(VariableReferenceExp(parameter2.Name), MethodInvocationExp(ThisReferenceExp(), "Callout_Post_process")));

            return ExecuteMethod;
        }

        private CodeMemberMethod GetSegment()
        {
            CodeMemberMethod GetSegment = new CodeMemberMethod
            {
                Name = "GetSegment",
                Attributes = MemberAttributes.Public | MemberAttributes.Override,
                ReturnType = new CodeTypeReference("IVWTaskCalloutData")
            };
            CodeParameterDeclarationExpression parameter1 = new CodeParameterDeclarationExpression(typeof(string), "segmentName");
            GetSegment.Parameters.Add(parameter1);

            GetSegment.Statements.Add(ReturnExpression(SnippetExpression("null")));

            return GetSegment;
        }

        private CodeMemberMethod GetSegmentWithInstanceParam(DataStoreCallout dataStoreCallout)
        {
            CodeMemberMethod GetSegmentWithInstanceParam = new CodeMemberMethod
            {
                Name = "GetSegment",
                Attributes = MemberAttributes.Public | MemberAttributes.Override,
                ReturnType = new CodeTypeReference("IVWTaskCalloutData")
            };
            CodeParameterDeclarationExpression parameter1 = new CodeParameterDeclarationExpression(typeof(string), "segmentName");
            GetSegmentWithInstanceParam.Parameters.Add(parameter1);

            CodeParameterDeclarationExpression parameter2 = new CodeParameterDeclarationExpression(typeof(Int64), "instance");
            GetSegmentWithInstanceParam.Parameters.Add(parameter2);

            GetSegmentWithInstanceParam.Statements.Add(SnippetStatement("switch (" + parameter1.Name + ")"));
            GetSegmentWithInstanceParam.Statements.Add(SnippetStatement("{"));

            foreach (string segmentname in dataStoreCallout.CalloutSegments.Select(s => s.Name).Distinct())
            {
                CalloutSegment segment = dataStoreCallout.CalloutSegments.Where(s => s.Name == segmentname).First();

                GetSegmentWithInstanceParam.Statements.Add(SnippetStatement(string.Format("case \"" + segment.Name + "\":")));
                if (segment.Inst == "0")
                {
                    GetSegmentWithInstanceParam.Statements.Add(ReturnExpression(VariableReferenceExp(segment.Name)));
                }
                else
                {
                    CodeConditionStatement IfCountIsEqualToInstance = IfCondition();
                    IfCountIsEqualToInstance.Condition = BinaryOpertorExpression(GetProperty(segment.Name, "Count"), CodeBinaryOperatorType.IdentityEquality, VariableReferenceExp(parameter2.Name));
                    IfCountIsEqualToInstance.TrueStatements.Add(MethodInvocationExp(FieldReferenceExp(ThisReferenceExp(), "Add"), "Add").AddParameters(new CodeExpression[] { ObjectCreateExpression("c" + segment.Name) }));


                    CodeConditionStatement IfCountIsLessThanInstance = IfCondition();
                    IfCountIsEqualToInstance.Condition = BinaryOpertorExpression(GetProperty(segment.Name, "Count"), CodeBinaryOperatorType.LessThan, VariableReferenceExp(parameter2.Name));
                    IfCountIsLessThanInstance.TrueStatements.Add(ThrowNewException("Mismatch Instance"));

                    GetSegmentWithInstanceParam.Statements.Add(ReturnExpression(ArrayIndexerExpression(segment.Name, MethodInvocationExp(VariableReferenceExp("Int32"), "Parse").AddParameters(new CodeExpression[] { MethodInvocationExp(VariableReferenceExp(parameter2.Name), "ToString") }))));
                }
            }

            GetSegmentWithInstanceParam.Statements.Add(SnippetStatement("}//ENDSWITCH"));
            GetSegmentWithInstanceParam.Statements.Add(ReturnExpression(SnippetExpression("null")));

            return GetSegmentWithInstanceParam;
        }


        private CodeMemberMethod BuildOutSegments(DataStoreCallout dataStoreCallout)
        {
            CodeMemberMethod BuildOutSegments = new CodeMemberMethod
            {
                Name = "BuildOutSegments",
                Attributes = MemberAttributes.Public | MemberAttributes.Override
            };

            CodeTryCatchFinallyStatement tryBlock = new CodeTryCatchFinallyStatement();
            BuildOutSegments.Statements.Add(tryBlock);

            CodeCatchClause catchBlock = new CodeCatchClause("e");
            catchBlock.ThrowException("e");
            tryBlock.CatchClauses.Add(catchBlock);

            foreach (CalloutSegment segment in dataStoreCallout.CalloutSegments.Where(s => s.FlowAttribute == "1" || s.FlowAttribute == "2"))
            {
                CodeTryCatchFinallyStatement innerTry = new CodeTryCatchFinallyStatement();
                if (segment.Inst == "1")
                {
                    CodeConditionStatement IfSegCountGreatherThanZero = IfCondition();
                    IfSegCountGreatherThanZero.Condition = BinaryOpertorExpression(GetProperty(segment.Name, "Count"), CodeBinaryOperatorType.GreaterThan, PrimitiveExpression(0));
                    innerTry.TryStatements.Add(IfSegCountGreatherThanZero);

                    IfSegCountGreatherThanZero.TrueStatements.Add(MethodInvocationExp(FieldReferenceExp(BaseReferenceExp(), "writer"), "WriteStartElement").AddParameters(new CodeExpression[] { PrimitiveExpression(segment.Name) }));
                    IfSegCountGreatherThanZero.TrueStatements.Add(MethodInvocationExp(FieldReferenceExp(BaseReferenceExp(), "writer"), "WriteAttributeString").AddParameters(new CodeExpression[] { PrimitiveExpression("RecordCount"), MethodInvocationExp(GetProperty(segment.Name, "Count"), "ToString") }));
                    IfSegCountGreatherThanZero.TrueStatements.Add(MethodInvocationExp(FieldReferenceExp(BaseReferenceExp(), "writer"), "WriteAttributeString").AddParameters(new CodeExpression[] { PrimitiveExpression("seq"), PrimitiveExpression(segment.Sequence) }));

                    CodeIterationStatement forEachRow = ForLoopExpression(DeclareVariableAndAssign(typeof(int), "i", true, PrimitiveExpression(0)), //TECH-XXXX
                                                                          BinaryOpertorExpression(VariableReferenceExp("i"), CodeBinaryOperatorType.LessThan, GetProperty(segment.Name, "Count")),
                                                                          AssignVariable(VariableReferenceExp("i"), BinaryOpertorExpression(VariableReferenceExp("i"), CodeBinaryOperatorType.Add, PrimitiveExpression(1))));
                    IfSegCountGreatherThanZero.TrueStatements.Add(forEachRow);

                    forEachRow.Statements.Add(MethodInvocationExp(FieldReferenceExp(BaseReferenceExp(), "writer"), "WriteStartElement").AddParameters(new CodeExpression[] { PrimitiveExpression("I" + segment.Sequence) }));
                    foreach (CalloutDataItem dataItem in segment.CalloutDataitems)
                    {
                        forEachRow.Statements.Add(MethodInvocationExp(FieldReferenceExp(BaseReferenceExp(), "writer"), "WriteAttributeString").AddParameters(new CodeExpression[] { PrimitiveExpression(dataItem.Name), MethodInvocationExp(TypeReferenceExp("Convert"),"ToString").AddParameter(GetProperty(ArrayIndexerExpression(segment.Name,VariableReferenceExp("i")),dataItem.Name)) }));
                    }
                    forEachRow.Statements.Add(MethodInvocationExp(FieldReferenceExp(BaseReferenceExp(), "writer"), "WriteEndElement"));
                    IfSegCountGreatherThanZero.TrueStatements.Add(MethodInvocationExp(FieldReferenceExp(BaseReferenceExp(), "writer"), "WriteEndElement"));
                }
                else
                {
                    CodeConditionStatement IfSegCountGreatherThanZero = IfCondition();
                    IfSegCountGreatherThanZero.Condition = BinaryOpertorExpression(VariableReferenceExp(segment.Name), CodeBinaryOperatorType.IdentityInequality, SnippetExpression("null"));
                    innerTry.TryStatements.Add(IfSegCountGreatherThanZero);

                    IfSegCountGreatherThanZero.TrueStatements.Add(MethodInvocationExp(FieldReferenceExp(BaseReferenceExp(), "writer"), "WriteStartElement").AddParameters(new CodeExpression[] { PrimitiveExpression(segment.Name) }));
                    IfSegCountGreatherThanZero.TrueStatements.Add(MethodInvocationExp(FieldReferenceExp(BaseReferenceExp(), "writer"), "WriteAttributeString").AddParameters(new CodeExpression[] { PrimitiveExpression("RecordCount"), PrimitiveExpression("1") }));
                    IfSegCountGreatherThanZero.TrueStatements.Add(MethodInvocationExp(FieldReferenceExp(BaseReferenceExp(), "writer"), "WriteAttributeString").AddParameters(new CodeExpression[] { PrimitiveExpression("seq"), PrimitiveExpression(segment.Sequence) }));

                    IfSegCountGreatherThanZero.TrueStatements.Add(MethodInvocationExp(FieldReferenceExp(BaseReferenceExp(), "writer"), "WriteStartElement").AddParameters(new CodeExpression[] { PrimitiveExpression("I" + segment.Sequence) }));
                    foreach (CalloutDataItem dataItem in segment.CalloutDataitems)
                    {
                        IfSegCountGreatherThanZero.TrueStatements.Add(MethodInvocationExp(FieldReferenceExp(BaseReferenceExp(), "writer"), "WriteAttributeString").AddParameters(new CodeExpression[] { PrimitiveExpression(dataItem.Name),
                                                                                                                                                                                                        MethodInvocationExp(TypeReferenceExp("Convert"),"ToString").AddParameter(GetProperty(VariableReferenceExp(segment.Name),dataItem.Name)) }));
                    }
                    IfSegCountGreatherThanZero.TrueStatements.Add(MethodInvocationExp(FieldReferenceExp(BaseReferenceExp(), "writer"), "WriteEndElement"));
                    IfSegCountGreatherThanZero.TrueStatements.Add(MethodInvocationExp(FieldReferenceExp(BaseReferenceExp(), "writer"), "WriteEndElement"));

                }
                CodeCatchClause innerCatch = AddCatchBlock(innerTry, "E1");
                innerCatch.AddStatement(ThrowNewException(String.Format("Error Occured while Building {0} Segment", segment.Name)));

                tryBlock.AddStatement(innerTry);
            }

            return BuildOutSegments;
        }

    }
}
