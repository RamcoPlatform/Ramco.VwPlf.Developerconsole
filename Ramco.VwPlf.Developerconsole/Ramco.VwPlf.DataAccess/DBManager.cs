using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlClient;

namespace Ramco.VwPlf.DataAccess
{
    public enum DataProvider
    {
        sqlserver
    }


    public sealed class DBManager : IDBManager, IDisposable
    {
        private DataProvider _providerType;
        private String _connectionString;
        private IDbConnection _connection;
        private IDbTransaction _transaction = null;
        //private IDataReader _dataReader;
        private IDbCommand _command;


        public DBManager(String sConnectionString)
        {
            this._connectionString = sConnectionString;
        }

        public DBManager(DataProvider providertype)
        {
            this._providerType = providertype;
        }

        public DataProvider ProviderType
        {
            get
            {
                return this._providerType;
            }
            set
            {
                this._providerType = value;
            }
        }

        public String ConnectionString
        {
            get
            {
                return this._connectionString;
            }
            set
            {
                this._connectionString = value;
            }
        }

        public IDbConnection Connection
        {
            get
            {
                return this._connection;
            }
        }

        public IDbTransaction Transaction
        {
            get
            {
                return this._transaction;
            }
        }

        //public IDataReader DataReader
        //{
        //    get
        //    {
        //        return this._dataReader;
        //    }
        //}

        public IDbCommand Command
        {
            get
            {
                return this._command;
            }
        }

        /// <summary>
        /// Opens the Current Connection
        /// </summary>
        public bool Open()
        {
            try
            {
                this._connection = DBManagerFactory.GetDbConnection(this.ProviderType);
                this._connection.ConnectionString = this._connectionString;

                if (this.Connection.State != ConnectionState.Open)
                    this.Connection.Open();

                return true;
            }
            catch (Exception ex)
            {
                throw (new Exception(ex.Message));
            }
        }

        /// <summary>
        /// Begins Transaction
        /// </summary>
        public bool BeginTransaction()
        {
            try
            {
                if (Object.Equals(this.Transaction, null))
                {
                    this._transaction = DBManagerFactory.GetTransaction(this.Connection, this.ProviderType);
                }
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("BeginTransaction : {0}", (Object.Equals(ex.InnerException, null) ? ex.Message : ex.InnerException.Message)));
            }

        }

        /// <summary>
        /// Commits current transaction.
        /// </summary>
        public bool CommitTransaction()
        {
            try
            {
                if (!Object.Equals(this.Transaction, null))
                    this.Transaction.Commit();
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("CommitTransaction : {0}", (Object.Equals(ex.InnerException, null) ? ex.Message : ex.InnerException.Message)));
            }
        }

        /// <summary>
        /// Rollbacks Current Transaction
        /// </summary>
        /// <returns></returns>
        public bool RollbackTransaction()
        {
            try
            {
                if (!Object.Equals(this.Transaction, null))
                    this.Transaction.Rollback();
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("RollbackTransaction : {0}", (Object.Equals(ex.InnerException, null) ? ex.Message : ex.InnerException.Message)));
            }
        }

        /// <summary>
        /// Creates parameter collection based on Connection provider
        /// </summary>
        public IDbDataParameter[] CreateParameters(int parmscount)
        {
            try
            {
                return (new IDbDataParameter[parmscount]);
                //this._parameters = DBManagerFactory.GetParameters(this.ProviderType, parmscount);
                //return true;
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("CreateParameters : {0}", (Object.Equals(ex.InnerException, null) ? ex.Message : ex.InnerException.Message)));
            }
        }


        ///// <summary>
        ///// Add parameter to parameter collection
        ///// </summary>
        ///// <param name="paramName"></param>
        ///// <param name="paramValue"></param>
        //public void AddParamters(int index, string paramName, Object paramValue)
        //{
        //    try
        //    {
        //        if (index < this._parameters.Length)
        //        {
        //            this._parameters[index].ParameterName = paramName;
        //            this._parameters[index].Value = paramValue;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        throw new Exception(String.Format("AddParameters : {0}", (Object.Equals(ex.InnerException, null) ? ex.Message : ex.InnerException.Message)));
        //    }
        //}

        public void AddParamters(IDbDataParameter[] parameters, int index, string paramName, Object paramValue)
        {
            try
            {
                if (index < parameters.Length)
                {
                    IDbDataParameter parameter = DBManagerFactory.GetParameter(this._providerType);
                    parameter.ParameterName = paramName;
                    parameter.Value = paramValue;
                    parameters[index] = parameter;
                }
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("AddParameters : {0}", (Object.Equals(ex.InnerException, null) ? ex.Message : ex.InnerException.Message)));
            }
        }

        /// <summary>
        /// Executes the query and returns datareader object to read the resultset row one by one.
        /// </summary>
        /// <param name="commandType"></param>
        /// <param name="commandText"></param>
        /// <returns></returns>
        public IDataReader ExecuteDataReader(CommandType commandType, String commandText, IDbDataParameter[] parameters)
        {
            this._command = DBManagerFactory.GetDbCommand(this.ProviderType);
            PrepareCommand(this._connection, this._command, this._transaction, this._command.CommandType, this._command.CommandText, parameters);
            return this.Command.ExecuteReader();
        }

        /// <summary>
        /// Build command object by assigning its necessary properties
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="command"></param>
        /// <param name="transaction"></param>
        /// <param name="commandtype"></param>
        /// <param name="commandtext"></param>
        /// <param name="commandParameters"></param>
        /// <param name="dataAdapter">Optional parameter</param>
        private void PrepareCommand(IDbConnection connection, IDbCommand command, IDbTransaction transaction, CommandType commandtype, String commandtext, IDbDataParameter[] commandParameters, IDbDataAdapter dataAdapter = null)
        {
            command.Connection = connection;
            command.CommandType = commandtype;
            command.CommandText = commandtext;
            command.CommandTimeout = 0;

            if (!Object.Equals(dataAdapter, null))
                dataAdapter.SelectCommand = command;

            if (!Object.Equals(transaction, null))
                command.Transaction = transaction;

            if (!Object.Equals(commandParameters, null))
                AttachParameters(command, commandParameters);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="command"></param>
        /// <param name="commandParameters"></param>
        private void AttachParameters(IDbCommand command, IDbDataParameter[] commandParameters)
        {
            foreach (IDbDataParameter Parameter in commandParameters)
            {
                if (Object.Equals(Parameter.Direction, ParameterDirection.InputOutput) && Object.Equals(Parameter.Value, null))
                    Parameter.Value = DBNull.Value;
                command.Parameters.Add(Parameter);
            }
        }

        /// <summary>
        /// Executes the query, and returns the first column of the first row in the result set returned by the query. Additional columns or rows are ignored
        /// </summary>
        /// <param name="commandType"></param>
        /// <param name="commandText"></param>
        /// <returns></returns>
        public Object ExecuteScalar(CommandType commandType, String commandText, IDbDataParameter[] parameters)
        {
            this._command = DBManagerFactory.GetDbCommand(this._providerType);
            PrepareCommand(this._connection, this._command, this._transaction, commandType, commandText, parameters);
            Object returnValue = this._command.ExecuteScalar();
            //this._parameters = null;
            return returnValue;
        }

        /// <summary>
        /// Use this function to execute statements other than select statement
        /// </summary>
        /// <param name="commandType"></param>
        /// <param name="commandText"></param>
        /// <returns>returns number of rows affected</returns>
        public Object ExecuteNonQuery(CommandType commandType, String commandText, IDbDataParameter[] parameters)
        {
            this._command = DBManagerFactory.GetDbCommand(this._providerType);
            PrepareCommand(this._connection, this._command, this._transaction, commandType, commandText, parameters);
            Object returnValue = this._command.ExecuteNonQuery();
            //this._parameters = null;
            return returnValue;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="commandType"></param>
        /// <param name="commandText"></param>
        /// <returns></returns>
        public DataSet ExecuteDataSet(CommandType commandType, String commandText, IDbDataParameter[] parameters)
        {
            DataSet dsResultSet = new DataSet();

            try
            {
                using (IDbCommand command = DBManagerFactory.GetDbCommand(this.ProviderType))
                {
                    IDbDataAdapter DbdataAdapter = DBManagerFactory.GetDataAdapter(this.ProviderType,command);
                    PrepareCommand(this._connection, command, this._transaction, commandType, commandText, parameters, DbdataAdapter);
                    DbdataAdapter.Fill(dsResultSet);
                }
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("DBManager->ExecuteDataSet->{0}", ex.InnerException != null ? ex.InnerException.Message : ex.Message));
            }

            //this._parameters = null;

            return dsResultSet;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="commandType"></param>
        /// <param name="commandText"></param>
        /// <returns></returns>
        public DataTable ExecuteDataTable(CommandType commandType, String commandText, IDbDataParameter[] parameters)
        {
            DataSet dsResultSet = new DataSet();
            DataTable dt;

            using (IDbCommand command = DBManagerFactory.GetDbCommand(this.ProviderType))
            {
              
                IDbDataAdapter DbdataAdapter = DBManagerFactory.GetDataAdapter(this.ProviderType,command);
                DbdataAdapter.SelectCommand = command;
                PrepareCommand(this._connection, command, this._transaction, commandType, commandText, parameters, DbdataAdapter);
                DbdataAdapter.Fill(dsResultSet);
                dt = dsResultSet.Tables[0];
                //this._parameters = null;

                dsResultSet.Tables.Remove(dt);
            }

            return dt;
        }


        /// <summary>
        /// Closes the Data Reader
        /// </summary>
        public void CloseReader()
        {
            //if (!Object.Equals(this.DataReader, null))
            //    this.DataReader.Close();
        }

        /// <summary>
        /// Closes the current connection
        /// </summary>
        public void Close()
        {
            if (!Object.Equals(this.Connection.State, ConnectionState.Closed))
                this.Connection.Close();
        }

        /// <summary>
        /// Dispose Object
        /// SuppressFinalize will make finally method to dispose object prior to Garbage Collected
        /// This SuppressFinalize will take no effect , if u dont use finally method.
        /// </summary>
        public void Dispose()
        {
            GC.SuppressFinalize(this);
            this.Close();
            this._transaction = null;
            this._command = null;
            this._connection = null;
        }
    }
}
