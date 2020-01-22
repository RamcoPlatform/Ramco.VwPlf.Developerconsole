/***************************************************************************************************
 * Bug Id           :   PLF2.0_11668          
 * Fixed By         :   Madhan Sekar M
 * Modified On      :   12-Feb-2015
 * Fix Description  :   code fix for Trace and error log
 ***************************************************************************************************/

using Ramco.VwPlf.DataAccess;

namespace Ramco.VwPlf.VwState.Generator
{
    #region Class to Get & Set necessary options

    public class SetOptions
    {
        #region Properties

        /// <summary>
        /// Input Type from which ur xml will be getting generated.
        /// DataType - Integer.
        /// 0 specifies DB, 1 specifies InputPath
        /// </summary>
        public string Type
        {
            get
            {
                return this._type;
            }
            set
            {
                this._type = value;
            }
        }

        /// <summary>
        /// Customer.
        /// DataType - String.
        /// </summary>
        public string Customer
        {
            get
            {
                return this._customer;
            }
            set
            {
                this._customer = value;
            }
        }

        /// <summary>
        /// Project.
        /// DataType - String.
        /// </summary>
        public string Project
        {
            get
            {
                return this._project;
            }
            set
            {
                this._project = value;
            }
        }

        /// <summary>
        /// Process.
        /// DataType - String.
        /// </summary>
        public string Process
        {
            get
            {
                return this._process;
            }
            set
            {
                this._process = value;
            }
        }

        /// <summary>
        /// Component.
        /// DataType - String.
        /// </summary>
        public string Component
        {
            get
            {
                return this._component;
            }
            set
            {
                this._component = value;
            }
        }

        /// <summary>
        /// ECRNo.
        /// DataType - String.
        /// </summary>
        public string ECRNo
        {
            get
            {
                return this._ecrNo;
            }
            set
            {
                this._ecrNo = value;
            }
        }

        /// <summary>
        /// activity name
        /// </summary>
        public string Activity
        {
            get
            {
                return this._activity;
            }
            set
            {
                this._activity = value;
            }
        }

        /// <summary>
        /// ui name
        /// </summary>
        public string Ui
        {
            get
            {
                return this._ui;
            }
            set
            {
                this._ui = value;
            }
        }

        /// <summary>
        /// Output directory where u want generated files to be saved.
        /// DataType - String.
        /// </summary>
        public string OutputPath
        {
            get
            {
                return this._outputPath;
            }
            set
            {
                this._outputPath = value;
            }
        }

        /// <summary>
        /// Server.
        /// DataType - String.
        /// </summary>
        public string Server
        {
            get
            {
                return this._server;
            }
            set
            {
                this._server = value;
            }
        }

        /// <summary>
        /// Username.
        /// DataType - String.
        /// </summary>
        public string Username
        {
            get
            {
                return this._username;
            }
            set
            {
                this._username = value;
            }
        }

        /// <summary>
        /// Password.
        /// DataType - String.
        /// </summary>
        public string Password
        {
            get
            {
                return this._password;
            }
            set
            {
                this._password = value;
            }
        }

        /// <summary>
        /// DatabaseName.
        /// DataType - String.
        /// </summary>
        public string Database
        {
            get
            {
                return this._database;
            }
            set
            {
                this._database = value;
            }
        }

        /// <summary>
        /// InputPath - directory from which you have to load files.
        /// DataType - String.
        /// </summary>
        public string InputPath
        {
            get
            {
                return this._inputPath;
            }
            set
            {
                this._inputPath = value;
            }
        }

        /// <summary>
        /// Generation status
        /// </summary>
        public bool IsGenSucceed
        {
            get
            {
                return _isGenSucceed;
            }
            set
            {
                this._isGenSucceed = value;
            }
        }

        /// <summary>
        /// Segmentation
        /// </summary>
        public string IsSegmentation
        {
            get
            {
                return _isSegmentation;
            }
            set
            {
                this._isSegmentation = value;
            }
        }

        public string LogError
        {
            get
            {
                return _logError;
            }
            set
            {
                this._logError = value;
            }
        }

        public string LogSteps
        {
            get
            {
                return _logSteps;
            }
            set
            {
                this._logSteps = value;
            }
        }

        //PLF2.0_11668
        public string LogPath
        {
            get
            {
                return _logPath;
            }
            set
            {
                this._logPath = value;
                Logger.LogPath = value;
            }
        }

        public DBManager dbManager
        {
            get
            {
                return _dbManager;
            }
            set
            {
                this._dbManager = value;
            }
        }

        #endregion

        #region MemberVariables

        //Default variables

        //type 0-Database 1-InputPath
        //private int _type=0;
        private string _type = string.Empty;
        private string _customer = string.Empty;
        private string _project = string.Empty;
        private string _process = string.Empty;
        private string _component = string.Empty;
        private string _ecrNo = string.Empty;
        private string _activity = string.Empty;
        private string _ui = string.Empty;
        private string _outputPath = string.Empty;

        //variables needed when input type is DB
        private string _server = string.Empty;
        private string _database = string.Empty;
        private string _username = string.Empty;
        private string _password = string.Empty;

        //variables needed when input type is FilePath
        private string _inputPath = string.Empty;
        private string _isSegmentation = string.Empty;

        //Generation Status
        private bool _isGenSucceed = false;

        //Logger Settings
        private string _logError;
        private string _logSteps;
        private string _logPath; //PLF2.0_11668

        private DBManager _dbManager = null;

        #endregion
    }

    #endregion
}
