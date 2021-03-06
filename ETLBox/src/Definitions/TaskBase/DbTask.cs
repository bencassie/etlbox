﻿using ALE.ETLBox.ConnectionManager;
using ALE.ETLBox.DataFlow;
using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.Linq;

namespace ALE.ETLBox.ControlFlow
{
    public abstract class DbTask : GenericTask
    {

        /* Public Properties */
        public string Sql { get; set; }
        public FileConnectionManager FileConnection { get; set; }
        public List<Action<object>> Actions { get; set; }
        public Action BeforeRowReadAction { get; set; }
        public Action AfterRowReadAction { get; set; }
        Action InternalBeforeRowReadAction { get; set; }
        Action InternalAfterRowReadAction { get; set; }
        public long ReadTopX { get; set; } = long.MaxValue;
        public int? RowsAffected { get; private set; }
        public bool IsOdbcConnection => DbConnectionManager.GetType().IsSubclassOf(typeof(OdbcConnectionManager));
        internal virtual string NameAsComment => CommentStart + TaskName + CommentEnd + Environment.NewLine;
        private string CommentStart => DoXMLCommentStyle ? @"<!--" : "/*";
        private string CommentEnd => DoXMLCommentStyle ? @"-->" : "*/";
        public virtual bool DoXMLCommentStyle { get; set; }
        public bool DisableExtension { get; set; }
        public string Command
        {
            get
            {
                if (HasSql)
                    return HasName && !IsOdbcConnection ? NameAsComment + Sql : Sql;
                else if (HasFileConnection)
                {
                    if (FileConnection.FileExists)
                        return HasName ? NameAsComment + FileConnection.ReadContent() : FileConnection.ReadContent();
                    else
                    {
                        NLogger.Warn($"Sql file was not found: {FileConnection.FileName}", TaskType, "RUN", TaskHash, ControlFlow.STAGE);
                        return $"SELECT 'File {FileConnection.FileName} not found'";
                    }
                }
                else
                    throw new Exception("Empty command");
            }
        }
        public IEnumerable<QueryParameter> Parameter { get; set; }

        /* Internal/Private properties */
        internal bool DoSkipSql { get; private set; }
        bool HasSql => !(String.IsNullOrWhiteSpace(Sql));
        bool HasFileConnection => FileConnection != null;


        /* Some constructors */
        public DbTask()
        {

        }

        public DbTask(string name) : this()
        {
            this.TaskName = name;
        }

        public DbTask(string name, string sql) : this(name)
        {
            this.Sql = sql;
        }

        public DbTask(ITask callingTask, string sql)
        {
            this.Sql = sql;
            CopyTaskProperties(callingTask);
        }

        public DbTask(string name, string sql, params Action<object>[] actions) : this(name, sql)
        {
            Actions = actions.ToList();
        }


        public DbTask(string name, string sql, Action beforeRowReadAction, Action afterRowReadAction, params Action<object>[] actions) : this(name, sql)
        {
            BeforeRowReadAction = beforeRowReadAction;
            AfterRowReadAction = afterRowReadAction;
            Actions = actions.ToList();
        }

        public DbTask(string name, FileConnectionManager fileConnection) : this(name)
        {
            this.FileConnection = fileConnection;
        }

        /* Public methods */
        public int ExecuteNonQuery()
        {
            var conn = DbConnectionManager.Clone();
            try
            {
                conn.Open();
                QueryStart();
                RowsAffected = DoSkipSql ? 0 : conn.ExecuteNonQuery(Command, Parameter);
                QueryFinish(LogType.Rows);
            }
            finally
            {
                if (!conn.LeaveOpen)
                    conn.Close();
            }
            return RowsAffected ?? 0;
        }

        public object ExecuteScalar()
        {
            object result = null;
            var conn = DbConnectionManager.Clone();
            try
            {
                conn.Open();
                QueryStart();
                result = conn.ExecuteScalar(Command, Parameter);
                QueryFinish();
            }
            finally
            {
                if (!conn.LeaveOpen)
                    conn.Close();
            }
            return result;
        }

        public Nullable<T> ExecuteScalar<T>() where T : struct
        {
            object result = ExecuteScalar();
            if (result == null || result == DBNull.Value)
                return null;
            else
                return (T)(Convert.ChangeType(result, typeof(T)));
        }


        public bool ExecuteScalarAsBool()
        {
            object result = ExecuteScalar();
            return ObjectToBool(result);
        }

        static bool ObjectToBool(object result)
        {
            if (result == null) return false;
            int number = 0;
            int.TryParse(result.ToString(), out number);
            if (number > 0)
                return true;
            else if (result.ToString().Trim().ToLower() == "true")
                return true;
            else
                return false;
        }

        public void ExecuteReader()
        {
            var conn = DbConnectionManager.Clone();
            try
            {
                conn.Open();
                QueryStart();
                IDataReader reader = conn.ExecuteReader(Command, Parameter) as IDataReader;
                for (int rowNr = 0; rowNr < ReadTopX; rowNr++)
                {
                    if (reader.Read())
                    {
                        InternalBeforeRowReadAction?.Invoke();
                        BeforeRowReadAction?.Invoke();
                        for (int i = 0; i < Actions?.Count; i++)
                        {
                            if (!reader.IsDBNull(i))
                            {
                                Actions?[i]?.Invoke(reader.GetValue(i));
                            }
                            else
                            {
                                Actions?[i]?.Invoke(null);
                            }
                        }
                        AfterRowReadAction?.Invoke();
                        InternalAfterRowReadAction?.Invoke();
                    }
                    else
                    {
                        break;
                    }
                }
                reader.Close();
                QueryFinish();
            }
            finally
            {
                if (!conn.LeaveOpen)
                    conn.Close();
            }
        }

        internal List<T> Query<T>(Action<T> doWithRowAction, List<string> columnNames)
        {
            List<T> result = null;
            PrepareQuery();
            T row = default(T);
            TypeInfo typeInfo = new TypeInfo(typeof(T));

            if (typeInfo.IsArray)
            {
                InternalBeforeRowReadAction = () =>
                {
                    row = (T)Activator.CreateInstance(typeof(T), new object[] { columnNames.Count });
                };
                int index = 0;
                foreach (var colName in columnNames)
                {
                    int currentIndexAvoidingClosure = index;
                    Actions.Add(col =>
                    {
                        var ar = row as System.Array;
                        var x = Convert.ChangeType(col, typeof(T).GetElementType());
                        ar.SetValue(x, currentIndexAvoidingClosure);
                    });
                    index++;
                }
            }
            else
            {
                if (columnNames?.Count == 0) columnNames = typeInfo.PropertyNames;
                foreach (var colName in columnNames)
                {
                    if (typeInfo.HasPropertyOrColumnMapping(colName))
                        Actions.Add(colValue => typeInfo.GetInfoByPropertyNameOrColumnMapping(colName).SetValue(row, colValue));
                    else if (typeInfo.IsDynamic)
                        Actions.Add(colValue =>
                        {
                            dynamic r = row as ExpandoObject;
                            ((IDictionary<String, Object>)r).Add(colName, colValue);
                        });
                    else
                        Actions.Add(col => { });
                }
                InternalBeforeRowReadAction = () => row = (T)Activator.CreateInstance(typeof(T));
            }
            InternalAfterRowReadAction = () => doWithRowAction(row);
            ExecuteReader();
            CleanupQuery();
            return result;
        }

        private void PrepareQuery()
        {
            Actions = new List<Action<object>>();
        }

        private void CleanupQuery()
        {
            Actions = null;
        }

        public void BulkInsert(ITableData data, string tableName)
        {
            var conn = DbConnectionManager.Clone();
            try
            {
                conn.Open();
                QueryStart(LogType.Bulk);
                conn.BeforeBulkInsert(tableName);
                conn.BulkInsert(data, tableName);
                conn.AfterBulkInsert(tableName);
                RowsAffected = data.RecordsAffected;
                QueryFinish(LogType.Bulk);
            }
            finally
            {
                if (!conn.LeaveOpen)
                    conn.Close();
            }
        }


        /* Private implementation & stuff */
        enum LogType
        {
            None,
            Rows,
            Bulk
        }



        void QueryStart(LogType logType = LogType.None)
        {
            if (!DisableLogging)
                LoggingStart(logType);

            if (!DisableExtension)
                ExecuteExtension();
        }

        void QueryFinish(LogType logType = LogType.None)
        {
            if (!DisableLogging)
                LoggingEnd(logType);
        }

        void LoggingStart(LogType logType)
        {
            NLogger.Info(TaskName, TaskType, "START", TaskHash, ControlFlow.STAGE, ControlFlow.CurrentLoadProcess?.Id);
            if (logType == LogType.Bulk)
                NLogger.Debug($"SQL Bulk Insert", TaskType, "RUN", TaskHash, ControlFlow.STAGE, ControlFlow.CurrentLoadProcess?.Id);
            else
                NLogger.Debug($"{Command}", TaskType, "RUN", TaskHash, ControlFlow.STAGE, ControlFlow.CurrentLoadProcess?.Id);
        }

        void LoggingEnd(LogType logType)
        {
            NLogger.Info(TaskName, TaskType, "END", TaskHash, ControlFlow.STAGE, ControlFlow.CurrentLoadProcess?.Id);
            if (logType == LogType.Rows)
                NLogger.Debug($"Rows affected: {RowsAffected ?? 0}", TaskType, "RUN", TaskHash, ControlFlow.STAGE, ControlFlow.CurrentLoadProcess?.Id);
        }

        void ExecuteExtension()
        {
            if (ExtensionFileLoader.ExistsFolder && HasName)
            {
                List<ExtensionFile> extFiles = ExtensionFileLoader.GetExtensionFiles(TaskHash);

                if (extFiles.Count > 0)
                {
                    foreach (var extFile in extFiles)
                    {
                        new SqlTask($"Extensions: {extFile.Name}", new FileConnectionManager(extFile.FileName))
                        {
                            ConnectionManager = this.ConnectionManager,
                            DisableExtension = true
                        }.ExecuteNonQuery();
                    }
                    DoSkipSql = extFiles.Any(ef => ef.HasSkipNextStatement);
                }
            }
        }


    }


}
