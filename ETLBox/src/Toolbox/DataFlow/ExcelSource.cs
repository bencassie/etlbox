﻿using ExcelDataReader;
using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace ALE.ETLBox.DataFlow
{
    /// <summary>
    /// Reads data from a excel source. While reading the data from the file, data is also asnychronously posted into the targets.
    /// You can define a sheet name and a range - only the data in the specified sheet and range is read. Otherwise, all data
    /// in all sheets will be processed.
    /// </summary>
    /// <example>
    /// <code>
    /// ExcelSource&lt;ExcelData&gt; source = new ExcelSource&lt;ExcelData&gt;("src/DataFlow/ExcelDataFile.xlsx") {
    ///         Range = new ExcelRange(2, 4, 5, 9),
    ///         SheetName = "Sheet2"
    ///  };
    /// </code>
    /// </example>
    public class ExcelSource<TOutput> : DataFlowSource<TOutput>, ITask where TOutput : new()
    {
        /* ITask Interface */
        public override string TaskName => $"Dataflow: Read Excel source data from file {FileName}";

        /* Public properties */
        public string FileName { get; set; }
        public string ExcelFilePassword { get; set; }
        public ExcelRange Range { get; set; }
        public bool HasRange => Range != null;
        public string SheetName { get; set; }
        public bool HasSheetName => !String.IsNullOrWhiteSpace(SheetName);
        /* Private stuff */
        FileStream FileStream { get; set; }
        IExcelDataReader ExcelDataReader { get; set; }

        public ExcelSource()
        {
        }

        public ExcelSource(string fileName) : this()
        {
            FileName = fileName;
        }

        public void Execute() => PostAll();
        public override void PostAll()
        {
            NLogStart();
            Open();
            try
            {
                ReadAll();
                Buffer.Complete();
            }
            catch (Exception e)
            {
                throw new ETLBoxException("Error during reading data from excel file - see inner exception for details.", e);
            }
            finally
            {
                Close();
            }
            NLogFinish();
        }

        private void ReadAll()
        {
            do
            {
                int rowNr = 0;
                TypeInfo typeInfo = new TypeInfo(typeof(TOutput));
                while (ExcelDataReader.Read())
                {
                    if (ExcelDataReader.VisibleState != "visible") continue;
                    if (HasSheetName && ExcelDataReader.Name != SheetName) continue;
                    rowNr++;
                    if (HasRange && rowNr > Range.EndRowIfSet) break;
                    if (HasRange && rowNr < Range.StartRow) continue;
                    TOutput row = ParseDataRow(typeInfo);
                    Buffer.Post(row);
                    LogProgress(1);
                }
            } while (ExcelDataReader.NextResult());
        }

        private TOutput ParseDataRow(TypeInfo typeInfo)
        {
            TOutput row = new TOutput();
            int colInRange = 0;
            for (int col = 0; col < ExcelDataReader.FieldCount; col++)
            {
                if (HasRange && col > Range.EndColumnIfSet) break;
                if (HasRange && (col + 1) < Range.StartColumn) continue;
                if (colInRange > typeInfo.PropertyLength) break;
                if (!typeInfo.ExcelIndex2PropertyIndex.ContainsKey(colInRange)) { colInRange++; continue; }
                PropertyInfo propInfo = typeInfo.Properties[typeInfo.ExcelIndex2PropertyIndex[colInRange]];
                object value = ExcelDataReader.GetValue(col);
                propInfo.SetValue(row, TypeInfo.CastPropertyValue(propInfo, value?.ToString()));
                colInRange++;
            }
            return row;
        }

        private void Open()
        {
            FileStream = File.Open(FileName, FileMode.Open, FileAccess.Read);
            ExcelDataReader = ExcelReaderFactory.CreateReader(FileStream, new ExcelReaderConfiguration() { Password = ExcelFilePassword });
        }

        private void Close()
        {
            ExcelDataReader.Close();
        }
    }
}
