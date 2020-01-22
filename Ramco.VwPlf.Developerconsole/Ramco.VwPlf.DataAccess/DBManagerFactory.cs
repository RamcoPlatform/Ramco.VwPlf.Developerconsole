using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlClient;

namespace Ramco.VwPlf.DataAccess
{
    public sealed class DBManagerFactory
    {
        private DBManagerFactory() { }
        public static IDbConnection GetDbConnection(DataProvider ProviderType)
        {
            IDbConnection DbConnection = null;

            switch (ProviderType)
            {
                case DataProvider.sqlserver:
                    DbConnection = new SqlConnection();
                    break;
                default:
                    DbConnection = new SqlConnection();
                    break;
            }

            return DbConnection;
        }
        public static IDbCommand GetDbCommand(DataProvider ProviderType)
        {
            IDbCommand DbCommand = null;

            switch (ProviderType)
            {
                case DataProvider.sqlserver:
                    DbCommand = new SqlCommand();
                    break;
                default:
                    DbCommand = new SqlCommand();
                    break;
            }

            return DbCommand;
        }
        public static IDbDataAdapter GetDataAdapter(DataProvider ProviderType,IDbCommand command)
        {
            IDbDataAdapter DataAdapter = null;

            switch (ProviderType)
            {
                case DataProvider.sqlserver:
                    DataAdapter = new SqlDataAdapter(command as SqlCommand);
                    break;
                default:
                    //DataAdapter = new SqlDataAdapter();
                    break;
            }

            return DataAdapter;
        }
        public static IDbTransaction GetTransaction(IDbConnection conObj, DataProvider ProviderType)
        {
            //IDbConnection DbConnection = null;
            IDbTransaction DbTransaction = null;

            //DbConnection = GetDbConnection(ProviderType);
            if (!Object.Equals(conObj, null) && Object.Equals(conObj.State, ConnectionState.Open))
                DbTransaction = conObj.BeginTransaction();

            return DbTransaction;
        }
        public static IDbDataParameter GetParameter(DataProvider ProviderType)
        {
            IDbDataParameter Parameter = null;

            switch (ProviderType)
            {
                case DataProvider.sqlserver:
                        Parameter = new SqlParameter();
                    break;
                default:
                        Parameter = new SqlParameter();
                    break;
            }

            return Parameter;
        }
    }
}
