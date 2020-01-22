namespace Ramco.VwPlf.CodeGenerator.NTService
{
    partial class ProjectInstaller
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.DotnetCodegenServiceProcessInstaller1 = new System.ServiceProcess.ServiceProcessInstaller();
            this.DotnetCodeGenServiceInstaller = new System.ServiceProcess.ServiceInstaller();
            // 
            // DotnetCodegenServiceProcessInstaller1
            // 
            this.DotnetCodegenServiceProcessInstaller1.Account = System.ServiceProcess.ServiceAccount.LocalSystem;
            this.DotnetCodegenServiceProcessInstaller1.Password = null;
            this.DotnetCodegenServiceProcessInstaller1.Username = null;
            // 
            // DotnetCodeGenServiceInstaller
            // 
            this.DotnetCodeGenServiceInstaller.Description = "Platform dotnet codegeneration service";
            this.DotnetCodeGenServiceInstaller.ServiceName = "Platform Dotnet CodeGen Service";
            this.DotnetCodeGenServiceInstaller.StartType = System.ServiceProcess.ServiceStartMode.Automatic;
            // 
            // ProjectInstaller
            // 
            this.Installers.AddRange(new System.Configuration.Install.Installer[] {
            this.DotnetCodegenServiceProcessInstaller1,
            this.DotnetCodeGenServiceInstaller});

        }

        #endregion

        private System.ServiceProcess.ServiceProcessInstaller DotnetCodegenServiceProcessInstaller1;
        private System.ServiceProcess.ServiceInstaller DotnetCodeGenServiceInstaller;
    }
}