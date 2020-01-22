using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlClient;

namespace Ramco.VwPlf.DataAccess
{
    public interface IDBManager
    {
        DataProvider ProviderType { get; set; }
        String ConnectionString { get; set; }
        IDbConnection Connection { get; }
        IDbTransaction Transaction { get; }
        //IDataReader DataReader { get; }
        //IDbCommand Command { get; }

        bool Open();
        bool BeginTransaction();
        bool CommitTransaction();
        bool RollbackTransaction();
        IDbDataParameter[] CreateParameters(int paramCount);
        void AddParamters(IDbDataParameter[] parameters,int index,string paramName, Object paramValue);
        IDataReader ExecuteDataReader(CommandType commandType, String commandText, IDbDataParameter[] parameters);
        Object ExecuteScalar(CommandType commandType, String commandText, IDbDataParameter[] parameters);
        DataSet ExecuteDataSet(CommandType commandType, String commandText, IDbDataParameter[] parameters);
        DataTable ExecuteDataTable(CommandType commandType, String commandText, IDbDataParameter[] parameters);
        void CloseReader();
        void Close();
        void Dispose();

    }   
}
