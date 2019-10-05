using System.Data.SqlClient;

// for string extensions
using ALE.ETLBox.Helper;
using MySql.Data.MySqlClient;
using Oracle.ManagedDataAccess.Client;

namespace ALE.ETLBox {
    /// <summary>
    /// A helper class for encapsulating a conection string to a Oracle server in an object.
    /// Internally the OracleConnectionStringBuilder is used to access the values of the given connection string.
    /// </summary>
    public class OracleConnectionString : IDbConnectionString{

        OracleConnectionStringBuilder _builder;

        public string Value {
            get {
                return _builder?.ConnectionString;
            }
            set {
                _builder = new OracleConnectionStringBuilder(value);
            }
        }

        public string DBName => _builder?.DataSource;

        public OracleConnectionStringBuilder OracleConnectionStringBuilder => _builder;

        public OracleConnectionString() {
            _builder = new OracleConnectionStringBuilder();
        }

        public OracleConnectionString(string connectionString) {
            this.Value = connectionString;
        }

        public OracleConnectionString GetMasterConnection() {
            OracleConnectionStringBuilder con = new OracleConnectionStringBuilder(Value);
            con.DataSource = "";
            return new OracleConnectionString(con.ConnectionString);
        }

        public OracleConnectionString GetConnectionWithoutCatalog() {
            OracleConnectionStringBuilder con = new OracleConnectionStringBuilder(Value);
            con.DataSource = "";
            return new OracleConnectionString(con.ConnectionString);
        }

        public static implicit operator OracleConnectionString(string v) {
            return new OracleConnectionString(v);
        }

        public override string ToString() {
            return Value;
        }
    }
}
