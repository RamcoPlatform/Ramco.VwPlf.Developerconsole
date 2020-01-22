using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.CodeDom;
using System.Reflection;
using System.Data;
using System.Data.SqlClient;

namespace Ramco.VwPlf.CodeGenerator.AppLayer
{
    class BROTypesGenerator : AbstractCSFileGenerator
    {
        List<Method> _brMethods = null;
        ECRLevelOptions _ecrOptions = null;

        public BROTypesGenerator(List<Method> brMethods, ECRLevelOptions ecrOptions) : base(ecrOptions)
        {
            base._objectType = ObjectType.BR;
            this._ecrOptions = ecrOptions;
            this._brMethods = brMethods;
            base._targetDir = System.IO.Path.Combine(_ecrOptions.GenerationPath, _ecrOptions.Platform, _ecrOptions.Customer, _ecrOptions.Project, _ecrOptions.Ecrno, "Updated", _ecrOptions.Component, "Source", "BRO");
            base._targetFileName = Common.InitCaps(_ecrOptions.Component) + "BRTypes";
        }

        public override void CreateNamespace()
        {
            _csFile.NameSpace.Name = string.Format("com.ramco.vw.{0}.br", _ecrOptions.Component.ToLower());
        }

        public override void AddCustomAttributes()
        {
            //throw new NotImplementedException();
        }

        public override void ImportNamespace()
        {
            _csFile.ReferencedNamespace.Add("System");
            _csFile.ReferencedNamespace.Add("System.Collections.Generic");
            _csFile.ReferencedNamespace.Add("System.Text");
        }

        public override void CreateClasses()
        {
            #region creating brtypes class
            CodeTypeDeclaration brTypeClass = new CodeTypeDeclaration
            {
                Name = Common.InitCaps(_ecrOptions.Component) + "BRTypes",
                TypeAttributes = TypeAttributes.Sealed | TypeAttributes.Public
            };
            AddMemberFields(ref brTypeClass);
            AddMemberFunctions(ref brTypeClass);
            #endregion

            #region creating result class
            CodeTypeDeclaration resultClass = new CodeTypeDeclaration
            {
                Name = "Result",
                TypeAttributes = TypeAttributes.Public
            };
            AddMemberFields(ref resultClass);
            AddMemberFunctions(ref resultClass);

            brTypeClass.Members.Add(resultClass);
            #endregion

            #region creating getresultconnection class
            CodeTypeDeclaration resetConnectionClass = new CodeTypeDeclaration
            {
                Name = "GetResetConnection",
                TypeAttributes = TypeAttributes.Sealed | TypeAttributes.Public
            };
            resetConnectionClass.BaseTypes.Add(new CodeTypeReference(resultClass.Name));
            AddMemberFields(ref resetConnectionClass);
            AddMemberFunctions(ref resetConnectionClass);

            brTypeClass.Members.Add(resetConnectionClass);
            #endregion                        

            #region creating class for each method
            foreach (Method method in _brMethods)
            {
                IEnumerable<Parameter> singleSegOutParams = method.Parameters.Where(p => p.Seg != null && p.Seg.Inst == "0" && p.DI != null && string.Compare(p.Seg.Name, "fw_context", true) != 0 && (p.FlowDirection == FlowAttribute.OUT || p.FlowDirection == FlowAttribute.INOUT)).OrderBy(p => p.Name);
                IEnumerable<Parameter> multiSegOutParams = method.Parameters.Where(p => p.Seg != null && p.Seg.Inst == "1" && p.DI != null && string.Compare(p.Seg.Name, "fw_context", true) != 0 && (p.FlowDirection == FlowAttribute.OUT || p.FlowDirection == FlowAttribute.INOUT)).OrderBy(p => p.Name);
                if (singleSegOutParams.Any() || multiSegOutParams.Any())
                {
                    if (method.Parameters.Where(p => p.Seg != null && p.DI != null && p.Seg.Inst == "1" && (p.FlowDirection == FlowAttribute.OUT || p.FlowDirection == FlowAttribute.INOUT)).Any())
                    {
                        CodeTypeDeclaration methodClass = new CodeTypeDeclaration
                        {
                            Name = Common.InitCaps(method.Name),
                            TypeAttributes = TypeAttributes.Sealed | TypeAttributes.Public
                        };
                        AddMemberFields(ref methodClass);
                        AddMemberFunctions(ref methodClass);
                        methodClass.Name = methodClass.Name + "_RSet";
                        brTypeClass.Members.Add(methodClass);

                        CodeTypeDeclaration methodClass1 = new CodeTypeDeclaration
                        {
                            Name = Common.InitCaps(method.Name),
                            TypeAttributes = TypeAttributes.Sealed | TypeAttributes.Public
                        };
                        methodClass1.BaseTypes.Add(new CodeTypeReference(resultClass.Name));
                        DeclareMemberField(MemberAttributes.Public, methodClass1, "resultSet", string.Format("IList<{0}_RSet>", Common.InitCaps(methodClass1.Name)), false);
                        brTypeClass.Members.Add(methodClass1);
                    }
                    else
                    {
                        CodeTypeDeclaration methodClass = new CodeTypeDeclaration
                        {
                            Name = Common.InitCaps(method.Name),
                            TypeAttributes = TypeAttributes.Sealed | TypeAttributes.Public
                        };
                        methodClass.BaseTypes.Add(new CodeTypeReference(resultClass.Name));

                        AddMemberFields(ref methodClass);
                        AddMemberFunctions(ref methodClass);

                        brTypeClass.Members.Add(methodClass);
                    }
                }
            }
            #endregion

            _csFile.UserDefinedTypes.Add(brTypeClass);
        }

        public override void AddMemberFields(ref CodeTypeDeclaration classObj)
        {
            string className = classObj.Name.ToLower();

            //for result class
            if (className == "result")
                DeclareMemberField(MemberAttributes.Public, classObj, "ErrorId", typeof(Int32), false);

            //for getresetconnection class
            else if (className == "getresetconnection")
                DeclareMemberField(MemberAttributes.Public, classObj, "connStr", typeof(string), false);

            //for method class
            else
            {
                var methods = _brMethods.Where(m => m.Name.ToLower() == className);
                if (methods.Any())
                {
                    foreach (Parameter param in methods.First().Parameters.Where(p => p.Seg != null
                                                        && p.DI != null
                                                        && string.Compare(p.Seg.Name, "fw_context", true) != 0
                                                        && (p.FlowDirection == FlowAttribute.OUT || p.FlowDirection == FlowAttribute.INOUT)
                                                        ).OrderBy(p => int.Parse(p.SequenceNo)))
                    {
                        string sParamType = param.CategorizedDataType;
                        Object oParamtype = null;
                        switch (sParamType)
                        {
                            case DataType.INT:
                                oParamtype = typeof(long);
                                break;
                            case DataType.STRING:
                            case DataType.TIMESTAMP:
                            case DataType.VARBINARY:
                                oParamtype = typeof(string);
                                break;
                            case DataType.DOUBLE:
                                oParamtype = typeof(double);
                                break;
                            default:
                                oParamtype = typeof(string);
                                break;
                        }
                        DeclareMemberField(MemberAttributes.Public, classObj, param.Name, oParamtype, false);
                    }
                }
            }
        }

        public override void AddMemberFunctions(ref CodeTypeDeclaration classObj)
        {
            //throw new NotImplementedException();
        }

    }
    class BROInterfaceGenerator : AbstractCSFileGenerator
    {
        List<Method> _brMethods = null;
        ECRLevelOptions _ecrOptions = null;

        public BROInterfaceGenerator(List<Method> brMethods, ECRLevelOptions ecrOptions) : base(ecrOptions)
        {
            base._objectType = ObjectType.BR;
            this._ecrOptions = ecrOptions;
            this._brMethods = brMethods;
            base._targetDir = System.IO.Path.Combine(_ecrOptions.GenerationPath, _ecrOptions.Platform, _ecrOptions.Customer, _ecrOptions.Project, _ecrOptions.Ecrno, "Updated", _ecrOptions.Component, "Source", "BRO");
            base._targetFileName = string.Format("I{0}BR", Common.InitCaps(_ecrOptions.Component));
        }

        public override void CreateNamespace()
        {
            _csFile.NameSpace.Name = string.Format("com.ramco.vw.{0}.br", _ecrOptions.Component.ToLower());
        }

        public override void ImportNamespace()
        {
            _csFile.ReferencedNamespace.Add("System");
            _csFile.ReferencedNamespace.Add("System.Collections.Generic");
            _csFile.ReferencedNamespace.Add("System.Text");
            _csFile.ReferencedNamespace.Add("System.Transactions");
        }

        public override void CreateClasses()
        {
            CodeTypeDeclaration interfaceClass = new CodeTypeDeclaration
            {
                Name = string.Format("I{0}BR", Common.InitCaps(_ecrOptions.Component)),
                TypeAttributes = TypeAttributes.Interface | TypeAttributes.Public
            };
            AddMemberFunctions(ref interfaceClass);

            _csFile.UserDefinedTypes.Add(interfaceClass);
        }

        public override void AddCustomAttributes()
        {
            //throw new NotImplementedException();
        }

        public override void AddMemberFields(ref CodeTypeDeclaration classObj)
        {
            //throw new NotImplementedException();
        }

        public override void AddMemberFunctions(ref CodeTypeDeclaration classObj)
        {
            foreach (Method method in this._brMethods)
            {
                IEnumerable<Parameter> singleSegOutParams = method.Parameters.Where(p => p.Seg != null && p.Seg.Inst == "0" && p.DI != null && string.Compare(p.Seg.Name, "fw_context", true) != 0 && (p.FlowDirection == FlowAttribute.OUT || p.FlowDirection == FlowAttribute.INOUT)).OrderBy(p => p.Name);
                IEnumerable<Parameter> multiSegOutParams = method.Parameters.Where(p => p.Seg != null && p.Seg.Inst == "1" && p.DI != null && string.Compare(p.Seg.Name, "fw_context", true) != 0 && (p.FlowDirection == FlowAttribute.OUT || p.FlowDirection == FlowAttribute.INOUT)).OrderBy(p => p.Name);
                string sReturnType = (singleSegOutParams.Any() == false && multiSegOutParams.Any() == false) ? "Result" : Common.InitCaps(method.Name);

                CodeMemberMethod codeMemberMethod = new CodeMemberMethod
                {
                    ReturnType = new CodeTypeReference(string.Format("{0}.{1}", Common.InitCaps(_ecrOptions.Component) + "BRTypes", sReturnType)),
                    Name = method.Name.ToLower()
                };

                #region adding parameters
                if (method.AccessDatabase)
                {
                    codeMemberMethod.Parameters.Add(ParameterDeclarationExp(typeof(string), "szConnectionString"));
                    if (_ecrOptions.InTD)
                        codeMemberMethod.Parameters.Add(ParameterDeclarationExp(typeof(string), "szInTD"));
                }
                //codeMemberMethod.Parameters.Add(ParameterDeclarationExp(typeof(string), "szConnectionString"));
                //if (_ecrOptions.InTD)
                //    codeMemberMethod.Parameters.Add(ParameterDeclarationExp(typeof(string), "szInTD"));
                //codeMemberMethod.Parameters.Add(ParameterDeclarationExp(typeof(long), "ctxt_language"));
                //codeMemberMethod.Parameters.Add(ParameterDeclarationExp(typeof(long), "ctxt_ouinstance"));
                //codeMemberMethod.Parameters.Add(ParameterDeclarationExp(typeof(string), "ctxt_role"));
                //codeMemberMethod.Parameters.Add(ParameterDeclarationExp(typeof(string), "ctxt_service"));
                //codeMemberMethod.Parameters.Add(ParameterDeclarationExp(typeof(string), "ctxt_user"));
                foreach (Parameter param in method.Parameters.Where(p => p.Seg != null
                                                        && p.DI != null
                                                        //&& string.Compare(p.Seg.Name, "fw_context", true) != 0
                                                        && (p.FlowDirection == FlowAttribute.IN || p.FlowDirection == FlowAttribute.INOUT)
                                                        ).OrderBy(p => int.Parse(p.SequenceNo)))
                {
                    string sParamType = param.CategorizedDataType;
                    Object oParamtype = null;
                    switch (sParamType)
                    {
                        case DataType.INT:
                            oParamtype = typeof(long);
                            break;
                        case DataType.STRING:
                        case DataType.TIMESTAMP:
                        case DataType.VARBINARY:
                            oParamtype = typeof(string);
                            break;
                        case DataType.DOUBLE:
                            oParamtype = typeof(double);
                            break;
                        default:
                            oParamtype = typeof(string);
                            break;
                    }
                    codeMemberMethod.Parameters.Add(ParameterDeclarationExp((Type)oParamtype, param.Name));
                }
                #endregion

                classObj.Members.Add(codeMemberMethod);
            }
        }

    }
    class BROFactoryGenerator : AbstractCSFileGenerator
    {
        ECRLevelOptions _ecrOptions = null;
        public BROFactoryGenerator(ECRLevelOptions ecrOptions) : base(ecrOptions)
        {
            this._ecrOptions = ecrOptions;
            base._objectType = ObjectType.BR;
            base._targetDir = System.IO.Path.Combine(_ecrOptions.GenerationPath, _ecrOptions.Platform, _ecrOptions.Customer, _ecrOptions.Project, _ecrOptions.Ecrno, "Updated", _ecrOptions.Component, "Source", "BRO");
            base._targetFileName = string.Format("{0}BRFactory", Common.InitCaps(_ecrOptions.Component));
        }

        public override void CreateNamespace()
        {
            base._csFile.NameSpace.Name = string.Format("com.ramco.vw.{0}.br", _ecrOptions.Component.ToLower());
        }

        public override void ImportNamespace()
        {
            base._csFile.ReferencedNamespace.Add("System");
            base._csFile.ReferencedNamespace.Add("System.Collections.Generic");
            base._csFile.ReferencedNamespace.Add("System.Text");
            base._csFile.ReferencedNamespace.Add(string.Format("com.ramco.vw.{0}.br.sql", _ecrOptions.Component.ToLower()));
            base._csFile.ReferencedNamespace.Add(string.Format("com.ramco.vw.{0}.br.orac", _ecrOptions.Component.ToLower()));
        }

        public override void AddCustomAttributes()
        {
            //throw new NotImplementedException();
        }

        public override void CreateClasses()
        {
            CodeTypeDeclaration factoryclass = new CodeTypeDeclaration
            {
                Name = string.Format("{0}BRFactory", Common.InitCaps(_ecrOptions.Component)),
                TypeAttributes = TypeAttributes.Public | TypeAttributes.Sealed
            };
            AddMemberFunctions(ref factoryclass);


            base._csFile.UserDefinedTypes.Add(factoryclass);
            //throw new NotImplementedException();
        }

        public override void AddMemberFields(ref CodeTypeDeclaration classObj)
        {
            //throw new NotImplementedException();
        }

        public override void AddMemberFunctions(ref CodeTypeDeclaration classObj)
        {
            //throw new NotImplementedException();
            classObj.Members.Add(GetBR());
        }

        public CodeMemberMethod GetBR()
        {
            CodeMemberMethod codeMemberMethod = null;
            try
            {
                codeMemberMethod = new CodeMemberMethod
                {
                    Name = string.Format("Get{0}BR", Common.InitCaps(_ecrOptions.Component)),
                    Attributes = MemberAttributes.Public | MemberAttributes.Static,
                    ReturnType = new CodeTypeReference(string.Format("I{0}BR", Common.InitCaps(_ecrOptions.Component)))
                };
                codeMemberMethod.Parameters.Add(ParameterDeclarationExp(typeof(string), "provider"));

                codeMemberMethod.AddStatement(SnippetStatement("switch (provider)"));
                codeMemberMethod.AddStatement(SnippetStatement("{"));
                codeMemberMethod.AddStatement(SnippetStatement("case \"ORACLE\":"));
                codeMemberMethod.AddStatement(ReturnExpression(ObjectCreateExpression(string.Format("{0}BR_Orac", Common.InitCaps(_ecrOptions.Component)))));
                codeMemberMethod.AddStatement(SnippetStatement("default:"));
                codeMemberMethod.AddStatement(ReturnExpression(ObjectCreateExpression(string.Format("{0}BR_Sql", Common.InitCaps(_ecrOptions.Component)))));
                codeMemberMethod.AddStatement(SnippetStatement("}"));
            }
            catch (Exception ex)
            {
                base._logger.WriteLogToFile("GetBR", ex.InnerException != null ? ex.InnerException.Message : ex.Message);
            }
            return codeMemberMethod;
        }
    }
    class BROBackendStubGenerator : AbstractCSFileGenerator
    {
        string _backendType = string.Empty;
        List<Method> _brMethods = null;
        ECRLevelOptions _ecrOptions = null;
        public BROBackendStubGenerator(string backendType, List<Method> brMethods, ECRLevelOptions ecrOptions) : base(ecrOptions)
        {
            this._ecrOptions = ecrOptions;
            base._objectType = ObjectType.BR;
            this._backendType = backendType;
            this._brMethods = brMethods;
            base._targetDir = System.IO.Path.Combine(_ecrOptions.GenerationPath, _ecrOptions.Platform, _ecrOptions.Customer, _ecrOptions.Project, _ecrOptions.Ecrno, "Updated", _ecrOptions.Component, "Source", "BRO");
            base._targetFileName = string.Format("{0}BR_{1}", Common.InitCaps(_ecrOptions.Component), Common.InitCaps(backendType));
        }

        public override void CreateNamespace()
        {
            base._csFile.NameSpace.Name = string.Format("com.ramco.vw.{0}.br.{1}", _ecrOptions.Component.ToLower(), this._backendType.ToLower());
        }

        public override void ImportNamespace()
        {
            if (string.Compare(this._backendType, "sql", true) == 0)
            {
                base._csFile.ReferencedNamespace.Add("System");
                base._csFile.ReferencedNamespace.Add("System.Data");
                base._csFile.ReferencedNamespace.Add("System.Data.SqlClient");
                base._csFile.ReferencedNamespace.Add("System.Transactions");
                base._csFile.ReferencedNamespace.Add("System.Text");
                base._csFile.ReferencedNamespace.Add("System.Collections");
                base._csFile.ReferencedNamespace.Add("System.Collections.Generic");
                base._csFile.ReferencedNamespace.Add("System.Collections.Specialized");
            }
            else
            {
                base._csFile.ReferencedNamespace.Add("System");
                base._csFile.ReferencedNamespace.Add("System.Collections.Generic");
                base._csFile.ReferencedNamespace.Add("System.Text");
            }
        }

        public override void AddCustomAttributes()
        {
            //throw new NotImplementedException();
        }

        public override void CreateClasses()
        {
            CodeTypeDeclaration backendStubClass = new CodeTypeDeclaration
            {
                Name = string.Format("{0}BR_{1}", Common.InitCaps(_ecrOptions.Component), Common.InitCaps(this._backendType)),
                TypeAttributes = TypeAttributes.Public
            };
            backendStubClass.BaseTypes.Add(string.Format("I{0}BR", Common.InitCaps(_ecrOptions.Component)));

            AddMemberFunctions(ref backendStubClass);

            base._csFile.UserDefinedTypes.Add(backendStubClass);
        }

        public override void AddMemberFields(ref CodeTypeDeclaration classObj)
        {
            //throw new NotImplementedException();
        }

        public override void AddMemberFunctions(ref CodeTypeDeclaration classObj)
        {
            foreach (Method method in this._brMethods)
            {
                IEnumerable<Parameter> singleSegOutParams = method.Parameters.Where(p => p.Seg != null && p.Seg.Inst == "0" && p.DI != null && string.Compare(p.Seg.Name, "fw_context", true) != 0 && (p.FlowDirection == FlowAttribute.OUT || p.FlowDirection == FlowAttribute.INOUT)).OrderBy(p => p.Name);
                IEnumerable<Parameter> multiSegOutParams = method.Parameters.Where(p => p.Seg != null && p.Seg.Inst == "1" && p.DI != null && string.Compare(p.Seg.Name, "fw_context", true) != 0 && (p.FlowDirection == FlowAttribute.OUT || p.FlowDirection == FlowAttribute.INOUT)).OrderBy(p => p.Name);
                string sReturnType = (singleSegOutParams.Any() == false && multiSegOutParams.Any() == false) ? "Result" : Common.InitCaps(method.Name);
                string sBroObjName = (singleSegOutParams.Any() == false && multiSegOutParams.Any() == false) ? "res" : "spxs";

                CodeMemberMethod codeMemberMethod = new CodeMemberMethod
                {
                    Name = method.Name.ToLower(),
                    ReturnType = new CodeTypeReference(string.Format("{0}BRTypes.{1}", Common.InitCaps(_ecrOptions.Component), sReturnType)),
                    Attributes = MemberAttributes.Public | MemberAttributes.Final
                };
                if (method.AccessDatabase)
                {
                    codeMemberMethod.Parameters.Add(ParameterDeclarationExp(typeof(string), "szConnectionString"));
                    if (_ecrOptions.InTD)
                        codeMemberMethod.Parameters.Add(ParameterDeclarationExp(typeof(string), "szInTD"));
                }
                //codeMemberMethod.Parameters.Add(ParameterDeclarationExp(typeof(long), "ctxt_language"));
                //codeMemberMethod.Parameters.Add(ParameterDeclarationExp(typeof(long), "ctxt_ouinstance"));
                //codeMemberMethod.Parameters.Add(ParameterDeclarationExp(typeof(string), "ctxt_role"));
                //codeMemberMethod.Parameters.Add(ParameterDeclarationExp(typeof(string), "ctxt_service"));
                //codeMemberMethod.Parameters.Add(ParameterDeclarationExp(typeof(string), "ctxt_user"));
                foreach (Parameter param in method.Parameters.Where(p => p.Seg != null
                                                                        && p.DI != null
                                                                        //&& string.Compare(p.Seg.Name, "fw_context", true) != 0
                                                                        && (p.FlowDirection == FlowAttribute.IN || p.FlowDirection == FlowAttribute.INOUT)
                                                                        ).OrderBy(p => int.Parse(p.SequenceNo)))
                {
                    string sParamType = param.CategorizedDataType;
                    Object oParamtype = null;
                    switch (sParamType)
                    {
                        case DataType.INT:
                            oParamtype = typeof(long);
                            break;
                        case DataType.STRING:
                        case DataType.TIMESTAMP:
                        case DataType.VARBINARY:
                            oParamtype = typeof(string);
                            break;
                        case DataType.DOUBLE:
                            oParamtype = typeof(double);
                            break;
                        default:
                            oParamtype = typeof(string);
                            break;
                    }
                    codeMemberMethod.Parameters.Add(ParameterDeclarationExp((Type)oParamtype, param.Name));
                }
                codeMemberMethod.AddStatement(DeclareVariableAndAssign(string.Format("{0}BRTypes.{1}", Common.InitCaps(_ecrOptions.Component), sReturnType), sBroObjName, true, ObjectCreateExpression(string.Format("{0}BRTypes.{1}", Common.InitCaps(_ecrOptions.Component), sReturnType))));
                codeMemberMethod.AddStatement(ReturnExpression(VariableReferenceExp(sBroObjName)));
                classObj.Members.Add(codeMemberMethod);
            }
        }
    }
    class BROGenerator
    {
        ECRLevelOptions _ecrOptions = null;
        List<Method> _brMethods = null;
        public BROGenerator(List<Method> brMethods, ECRLevelOptions ecrOptions)
        {
            this._brMethods = brMethods;
            this._ecrOptions = ecrOptions;
        }

        public bool Generate()
        {
            try
            {
                BROTypesGenerator brTypesGenerator = new BROTypesGenerator(_brMethods, this._ecrOptions);
                brTypesGenerator.Generate();

                BROInterfaceGenerator brInterfaceGenerator = new BROInterfaceGenerator(_brMethods, this._ecrOptions);
                brInterfaceGenerator.Generate();

                BROFactoryGenerator brFactoryGenerator = new BROFactoryGenerator(this._ecrOptions);
                brFactoryGenerator.Generate();

                BROBackendStubGenerator brSqlStubGenerator = new BROBackendStubGenerator("Sql", _brMethods, this._ecrOptions);
                brSqlStubGenerator.Generate();

                BROBackendStubGenerator brOracleStubGenerator = new BROBackendStubGenerator("Orac", _brMethods, this._ecrOptions);
                brOracleStubGenerator.Generate();

                return true;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
    class BulkBRGenerator : AbstractCSFileGenerator
    {
        List<Method> _brMethods = null;
        ECRLevelOptions _ecrOptions = null;

        public BulkBRGenerator(List<Method> brMethods, ECRLevelOptions ecrOptions) : base(ecrOptions)
        {
            this._brMethods = brMethods;
            this._ecrOptions = ecrOptions;
            base._objectType = ObjectType.Bulk;
            base._targetFileName = string.Format("{0}_bulk", _ecrOptions.Component.ToLower());
            base._targetDir = System.IO.Path.Combine(_ecrOptions.GenerationPath, _ecrOptions.Platform, _ecrOptions.Customer, _ecrOptions.Project, _ecrOptions.Ecrno, "Updated", _ecrOptions.Component, "Source", "Bulk");
        }

        public override void CreateNamespace()
        {
            base._csFile.NameSpace.Name = string.Format("com.ramco.vw.{0}.bulk", _ecrOptions.Component.ToLower());
        }
        public override void ImportNamespace()
        {
            base._csFile.ReferencedNamespace.Add("System");
            base._csFile.ReferencedNamespace.Add("System.Data");
            base._csFile.ReferencedNamespace.Add("System.Data.SqlClient");
        }
        public override void AddCustomAttributes()
        {
            //throw new NotImplementedException();
        }
        public override void CreateClasses()
        {
            CodeTypeDeclaration bulkClass = new CodeTypeDeclaration
            {
                Name = string.Format("C{0}_bulk", _ecrOptions.Component.ToLower()),
                TypeAttributes = TypeAttributes.Public
            };
            this.AddMemberFields(ref bulkClass);
            this.AddMemberFunctions(ref bulkClass);
            base._csFile.UserDefinedTypes.Add(bulkClass);
        }
        public override void AddMemberFields(ref CodeTypeDeclaration classObj)
        {
            //throw new NotImplementedException();
        }
        public override void AddMemberFunctions(ref CodeTypeDeclaration classObj)
        {
            foreach (Method method in this._brMethods)
            {
                CodeMemberMethod codeMemberMethod = new CodeMemberMethod
                {
                    Name = string.Format("{0}Ex", method.Name.ToLower()),
                    Attributes = MemberAttributes.Public | MemberAttributes.Final,
                    ReturnType = new CodeTypeReference(typeof(long))
                };

                #region parameter addition
                if (method.AccessDatabase)
                {
                    codeMemberMethod.Parameters.Add(ParameterDeclarationExp(typeof(string), "szConnectionString"));
                    //if (_ecrOptions.InTD)
                    codeMemberMethod.Parameters.Add(ParameterDeclarationExp(typeof(string), "szInTD"));
                }
                foreach (Parameter param in method.Parameters.Where(p => p.Seg != null
                                                        && p.DI != null
                                                        ).OrderBy(p => int.Parse(p.SequenceNo)))
                {
                    string sParamType = param.CategorizedDataType;
                    Object oParamtype = null;
                    switch (sParamType)
                    {
                        case DataType.INT:
                            oParamtype = typeof(long);
                            break;
                        case DataType.STRING:
                        case DataType.TIMESTAMP:
                        case DataType.VARBINARY:
                            oParamtype = typeof(string);
                            break;
                        case DataType.DOUBLE:
                            oParamtype = typeof(double);
                            break;
                        default:
                            oParamtype = typeof(string);
                            break;
                    }

                    CodeParameterDeclarationExpression parameter = new CodeParameterDeclarationExpression();
                    parameter.Name = param.Name;
                    parameter.Type = new CodeTypeReference((Type)oParamtype);
                    if (param.FlowDirection == FlowAttribute.OUT || param.FlowDirection == FlowAttribute.INOUT)
                        parameter.Direction = FieldDirection.Ref;
                    codeMemberMethod.Parameters.Add(parameter);
                }
                #endregion

                #region varibale declaration
                codeMemberMethod.AddStatement(DeclareVariable(typeof(SqlConnection), "con"));
                codeMemberMethod.AddStatement(DeclareVariable(typeof(SqlCommand), "command"));

                CodeTryCatchFinallyStatement tryBlock = AddTryBlock(codeMemberMethod);
                tryBlock.AddStatement(SnippetStatement("using(con = new SqlConnection())"));
                tryBlock.AddStatement(SnippetStatement("{"));
                tryBlock.AddStatement(SnippetStatement("using(command = new SqlCommand())"));
                tryBlock.AddStatement(SnippetStatement("{"));
                tryBlock.AddStatement(AssignVariable(GetProperty(VariableReferenceExp("con"), "ConnectionString"), VariableReferenceExp("szConnectionString")));
                tryBlock.AddStatement(MethodInvocationExp(VariableReferenceExp("con"), "Open"));
                tryBlock.AddStatement(AssignVariable(GetProperty(VariableReferenceExp("command"), "Connection"), VariableReferenceExp("con")));
                tryBlock.AddStatement(AssignVariable(GetProperty(VariableReferenceExp("command"), "CommandType"), GetProperty(VariableReferenceExp("CommandType"), "StoredProcedure")));
                tryBlock.AddStatement(AssignVariable(GetProperty(VariableReferenceExp("command"), "CommandText"), PrimitiveExpression(method.Name.ToLower() + "_sp")));


                foreach (Parameter param in method.Parameters.Where(p => p.Seg != null
                                                        && p.DI != null
                                                        ).OrderBy(p => int.Parse(p.SequenceNo)))
                {
                    string sParamType = param.CategorizedDataType;
                    Object oParamtype = null;

                    CodeMethodInvokeExpression methodInvokation = MethodInvocationExp(GetProperty("command", "Parameters"), "Add");
                    methodInvokation.AddParameter(PrimitiveExpression(param.Name.ToLower()));

                    switch (sParamType)
                    {
                        case DataType.INT:
                            oParamtype = typeof(long);
                            methodInvokation.AddParameter(GetProperty(TypeReferenceExp(typeof(SqlDbType)), "Int"));
                            break;
                        case DataType.STRING:
                        case DataType.TIMESTAMP:
                        case DataType.VARBINARY:
                            oParamtype = typeof(string);
                            methodInvokation.AddParameter(GetProperty(TypeReferenceExp(typeof(SqlDbType)), "VarChar"));
                            break;
                        case DataType.DOUBLE:
                            oParamtype = typeof(double);
                            methodInvokation.AddParameter(GetProperty(TypeReferenceExp(typeof(SqlDbType)), "BigInt"));
                            break;
                        default:
                            oParamtype = typeof(string);
                            methodInvokation.AddParameter(GetProperty(TypeReferenceExp(typeof(SqlDbType)), "VarChar"));
                            break;
                    }
                    //tryBlock.AddStatement(AssignVariable(GetProperty(MethodInvocationExp(GetProperty(VariableReferenceExp("command"), "Parameters"), "Add").
                    //                                        AddParameters(new CodeExpression[] {    PrimitiveExpression(param.Name.ToLower()),
                    //                                                                                GetProperty(TypeReferenceExp(typeof(SqlDbType)),(oParamtype == typeof(long)) ?"Int":"VarChar")
                    //                                                                            }),
                    //                                        "Value"),
                    //                                     VariableReferenceExp(param.Name)));

                    tryBlock.AddStatement(AssignVariable(GetProperty(methodInvokation, "Value"), VariableReferenceExp(param.Name)));
                }
                if (_ecrOptions.InTD)
                    tryBlock.AddStatement(AssignVariable(GetProperty(MethodInvocationExp(GetProperty("command", "Parameters"), "Add").AddParameters(new CodeExpression[] { PrimitiveExpression("intd"), GetProperty("SqlDbType", "Text") }), "Value"), VariableReferenceExp("szInTD")));
                tryBlock.AddStatement(MethodInvocationExp(VariableReferenceExp("command"), "ExecuteNonQuery"));
                tryBlock.AddStatement(SnippetStatement("}"));
                tryBlock.AddStatement(SnippetStatement("}"));
                tryBlock.AddStatement(ReturnExpression(PrimitiveExpression(0)));

                #region catch and finally block
                tryBlock.AddCatch();
                tryBlock.FinallyStatements.Add(AssignVariable(VariableReferenceExp("con"), SnippetExpression("null")));
                tryBlock.FinallyStatements.Add(AssignVariable(VariableReferenceExp("command"), SnippetExpression("null")));
                #endregion

                codeMemberMethod.AddStatement(ReturnExpression(PrimitiveExpression(0)));
                #endregion

                classObj.Members.Add(codeMemberMethod);
            }
            //throw new NotImplementedException();
        }
    }
}
