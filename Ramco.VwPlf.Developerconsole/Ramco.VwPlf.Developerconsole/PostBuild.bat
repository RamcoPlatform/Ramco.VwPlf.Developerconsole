REM create necessary folders %CD% - current directory
IF NOT EXIST %1\Assemblies mkdir Assemblies
cd Assemblies
rmdir %1\Assemblies /s /q
IF NOT EXIST %CD%\3rdPartyGenerators mkdir 3rdPartyGenerators
IF NOT EXIST %CD%\CompilationAssemblies mkdir CompilationAssemblies
IF NOT EXIST %CD%\CompilationAssemblies\snk mkdir CompilationAssemblies\snk
IF NOT EXIST %CD%\FrameworkAssemblies mkdir FrameworkAssemblies
REM delete junk files before moving files
del %1\*.pdb /Q
del %1\Assemblies\*.* /Q
del %1\Assemblies\3rdPartyGenerators\*.* /Q
del %1\Assemblies\CompilationAssemblies\*.* /Q
del %1\Assemblies\FrameworkAssemblies\*.* /Q

REM return to previous directory
REM cd ..\
REM del %1\* /Q
cd %1\
robocopy ..\..\Reference\ %1\Assemblies\CompilationAssemblies Ramco.VW.RT.Web.TaskCallout.dll
robocopy ..\..\Reference\ %1\Assemblies\CompilationAssemblies Ramco.VW.RT.Web.Core.dll
robocopy ..\..\Reference\ %1\Assemblies\CompilationAssemblies Ramco.VW.RT.State.dll
robocopy ..\..\Reference\ %1\Assemblies\CompilationAssemblies Ramco.VW.RT.AsyncResult.dll
robocopy ..\..\Reference\ %1\Assemblies\CompilationAssemblies Plf.Itk.Ramco.Logicalextension.dll
robocopy ..\..\Reference\ %1\Assemblies\CompilationAssemblies Plf.Itk.Ramco.ComponentLevel.netmodule
robocopy ..\..\Reference\ %1\Assemblies\CompilationAssemblies Plf.Itk.Ramco.Callout.dll
robocopy ..\..\Reference\ %1\Assemblies\CompilationAssemblies Ramco.Plf.Global.Interfaces.dll
robocopy ..\..\Reference\ %1\Assemblies\CompilationAssemblies Plf.Ui.Ramco.Utility.dll
robocopy ..\..\Reference\ %1\Assemblies\CompilationAssemblies Plf.Ramco.WebCallout.Interface.dll
robocopy ..\..\Reference\ %1\Assemblies\CompilationAssemblies Plf.Ramco.Instrumentation.dll
robocopy ..\..\Reference\ %1\Assemblies\CompilationAssemblies Plf.Ramco.AppCallout.Interface.dll
robocopy ..\..\Reference\ %1\Assemblies\CompilationAssemblies CUtil.dll
robocopy ..\..\Reference\ %1\Assemblies\CompilationAssemblies VirtualWorksRT.dll
robocopy ..\..\Reference\ %1\Assemblies\CompilationAssemblies VWHelper.dll
robocopy ..\..\Reference\ %1\Assemblies\CompilationAssemblies csc.*
robocopy ..\..\Reference\snk %1\Assemblies\CompilationAssemblies\snk *.*
robocopy %1\ %1\Assemblies\CompilationAssemblies\ Ramco.VwPlf.DataAccess.dll

robocopy %1\ %1\Assemblies\FrameworkAssemblies Microsoft.* System.*
robocopy %1\ %1\Assemblies\FrameworkAssemblies Telerik*.*
robocopy ..\..\Reference\ %1\Assemblies\FrameworkAssemblies ADODB.dll

robocopy %1\ %1\Assemblies\3rdPartyGenerators *generator*.*
robocopy %1\ %1\Assemblies\3rdPartyGenerators Newtonsoft.Json.dll
robocopy ..\..\Reference\ %1\Assemblies\3rdPartyGenerators Microsoft.* 
robocopy ..\..\Reference\ %1\Assemblies\3rdPartyGenerators al*.* 
robocopy ..\..\Reference\ %1\Assemblies\3rdPartyGenerators *.exe 
robocopy ..\..\Reference\ %1\Assemblies\3rdPartyGenerators *.config 
robocopy ..\..\Reference\ %1\Assemblies\3rdPartyGenerators *.js 
robocopy ..\..\Reference\ %1\Assemblies\3rdPartyGenerators *.json
robocopy ..\..\Reference\ %1\Assemblies\3rdPartyGenerators Interop.* 
robocopy ..\..\Reference\ %1\Assemblies\3rdPartyGenerators Preview20.dll 
robocopy ..\..\Reference\ %1\Assemblies\3rdPartyGenerators RVWRepAspxGen.dll 
robocopy ..\..\Reference\ %1\Assemblies\3rdPartyGenerators AjaxMin.dll 
robocopy ..\..\Reference\ %1\Assemblies\3rdPartyGenerators npm-debug.log
robocopy ..\..\Reference\ %1\Assemblies\3rdPartyGenerators Ramco.Plf.Global.Interfaces.dll
robocopy ..\..\Reference\ %1\Assemblies\3rdPartyGenerators Ramco.Plf.Global.Interfaces.XmlSerializers.dll
robocopy ..\..\Reference\ %1\Assemblies\3rdPartyGenerators Ramco.VW.RT.Web.Core.dll
robocopy ..\..\Reference\ %1\Assemblies\3rdPartyGenerators Ramco.VWPlatform.Api.dll
robocopy ..\..\Reference\ %1\Assemblies\3rdPartyGenerators Ramco.VWPlatform.Api.Schema.dll
robocopy ..\..\Reference\ %1\Assemblies\3rdPartyGenerators Ramco.VWPlatform.Api.xml
robocopy ..\..\Reference\ %1\Assemblies\3rdPartyGenerators Ramco.VW.PLF.MDCFGenerator.dll
robocopy ..\..\Reference\ %1\Assemblies\3rdPartyGenerators DocumentFormat.OpenXml.dll
robocopy ..\..\Reference\ %1\Assemblies\3rdPartyGenerators InsertMasterData*

robocopy ..\..\Reference\node_modules %1\Assemblies\3rdPartyGenerators\node_modules /E



REM deleting copied files from source directory
del %1\Microsoft.* /Q
del %1\System.* /Q
del %1\ADODB.dll /Q
REM del %1\*generator*.* /Q
del %1\Newtonsoft.Json.dll /Q
del %1\Ramco.Plf.Global.Interfaces.dll /Q
del %1\Ramco.VW.RT.*.* /Q
del %1\Plf.Itk.*.* /Q
del %1\Plf.Ramco.*.* /Q
del %1\Plf.Ui.Ramco.*.* /Q
del %1\CUtil.dll /Q
del %1\VirtualWorksRT.dll /Q
del %1\VWHelper.dll /Q
del %1\al.* /Q
del %1\resgen.exe /Q
del %1\Telerik*.* /Q
del %1\Ramco.VwPlf.DataAccess.dll /Q
del %1\Ramco.Plf.Global.Interfaces.XmlSerializers.dll \Q
del %1\Ramco.VwPlatform.Api.Schema.dll \Q
del %1\Ramco.Glance.UiGenerator.* \Q
del %1\Ramco.mHub.MobileLayoutGenerator.* \Q
del %1\Ramco.Plf.Global.Interfaces.XmlSerializers.* \Q
del %1\Ramco.Plf.Layout.Generator.* \Q
del %1\Ramco.Plf.MobileLayoutGenerator20.* \Q
del %1\Ramco.Plf.UiGenerator.* \Q
del %1\Ramco.Plf.XamlGenerator.* \Q
del %1\Ramco.VwPlatform.Api.Schema.* \Q
del %1\Ramco.VwPlf.CodeGenerator.Callout.* \Q
del %1\Ramco.VwPlf.Generator.* \Q
del %1\Ramco.VwPlf.VwState.Generator.* \Q
del %1\Ramco.Xam.MobileLayoutGenerator.* \Q
del %1\AjaxMin.dll \Q
del %1\DeviceGenerator20.* \Q
del %1\Ramco.VW.PLF.MDCFGenerator.dll \Q
del %1\DocumentFormat.OpenXml.dll \Q
:END




