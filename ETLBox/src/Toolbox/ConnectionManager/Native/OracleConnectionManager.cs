using CsvHelper;
using MySql.Data.MySqlClient;
using Oracle.ManagedDataAccess.Client;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;

namespace ALE.ETLBox.ConnectionManager
{
    /// <summary>
    /// Connection manager of a classic ADO.NET connection to a (Microsoft) Sql Server.
    /// </summary>
    /// <example>
    /// <code>
    /// ControlFlow.CurrentDbConnection = new SqlConnectionManager(new ConnectionString("Data Source=.;"));
    /// </code>
    /// </example>
    public class OracleConnectionManager : DbConnectionManager<OracleConnection>
    {
        public OracleConnectionManager() : base() { }

        public OracleConnectionManager(OracleConnectionString connectionString) : base(connectionString) { }

        public OracleConnectionManager(string connectionString) : base(new OracleConnectionString(connectionString)) { }

        string PageVerify { get; set; }
        string RecoveryModel { get; set; }
        public override void BulkInsert(ITableData data, string tableName)
        {
            BulkInsertSql<MySqlParameter> bulkInsert = new BulkInsertSql<MySqlParameter>()
            {
                UseParameterQuery = true,
                ConnectionType = ConnectionManagerType.MySql
            };
            string sql = bulkInsert.CreateBulkInsertStatement(data, tableName);
            var cmd = DbConnection.CreateCommand();
            cmd.Parameters.AddRange(bulkInsert.Parameters.ToArray());
            cmd.CommandText = sql;
            cmd.Prepare();
            cmd.ExecuteNonQuery();
        }

        public override void BeforeBulkInsert(string tableName) { }

        public override void AfterBulkInsert(string tableName) { }

        public override IConnectionManager Clone()
        {
            OracleConnectionManager clone = new OracleConnectionManager((OracleConnectionString)ConnectionString)
            {
                MaxLoginAttempts = this.MaxLoginAttempts
            };
            return clone;
        }
    }
}
