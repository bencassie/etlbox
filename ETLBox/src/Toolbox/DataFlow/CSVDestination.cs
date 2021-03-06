﻿using CsvHelper;
using CsvHelper.Configuration;
using System;
using System.Globalization;
using System.IO;

namespace ALE.ETLBox.DataFlow
{
    /// <summary>
    /// A Csv destination defines a csv file where data from the flow is inserted. Inserts are done in batches (using Bulk insert).
    /// </summary>
    /// <see cref="DBDestination"/>
    /// <typeparam name="TInput">Type of data input.</typeparam>
    /// <example>
    /// <code>
    /// CSVDestination&lt;MyRow&gt; dest = new CSVDestination&lt;MyRow&gt;("/path/to/file.csv");
    /// dest.Wait(); //Wait for all data to arrive
    /// </code>
    /// </example>
    public class CSVDestination<TInput> : DataFlowBatchDestination<TInput>, ITask, IDataFlowDestination<TInput>
    {
        /* ITask Interface */
        public override string TaskName => $"Write CSV data into file {FileName ?? ""}";

        public string FileName { get; set; }
        public bool HasFileName => !String.IsNullOrWhiteSpace(FileName);
        public Configuration Configuration { get; set; }

        internal const int DEFAULT_BATCH_SIZE = 1000;
        StreamWriter StreamWriter { get; set; }
        CsvWriter CsvWriter { get; set; }

        public CSVDestination()
        {
            BatchSize = DEFAULT_BATCH_SIZE;
        }

        public CSVDestination(int batchSize)
        {
            BatchSize = batchSize;
        }

        public CSVDestination(string fileName) : this()
        {
            FileName = fileName;
        }

        public CSVDestination(string fileName, int batchSize) : this(batchSize)
        {
            FileName = fileName;
        }

        internal override void InitObjects(int batchSize)
        {
            base.InitObjects(batchSize);
            Configuration = new Configuration(CultureInfo.InvariantCulture);
            Configuration.TypeConverterOptionsCache.GetOptions<DateTime>().Formats = new[] { "yyyy-MM-dd HH:mm:ss.fff" };

        }

        internal void InitCsvWriter()
        {
            StreamWriter = new StreamWriter(FileName);
            CsvWriter = new CsvWriter(StreamWriter, Configuration, leaveOpen: true);
            this.CloseStreamsAction = CloseStreams;
        }

        internal override void WriteBatch(ref TInput[] data)
        {
            if (CsvWriter == null) InitCsvWriter();
            base.WriteBatch(ref data);
            if (TypeInfo.IsArray)
                WriteArray(ref data);
            else if (TypeInfo.IsDynamic)
                WriteDynamicObject(ref data);
            else
                CsvWriter.WriteRecords(data);

            LogProgress(data.Length);
        }

        private void WriteArray(ref TInput[] data)
        {
            foreach (var record in data)
            {
                var recordAsArray = record as object[];
                foreach (var field in recordAsArray)
                {
                    CsvWriter.WriteField(field);
                }

                CsvWriter.NextRecord();
            }
        }

        private void WriteDynamicObject(ref TInput[] data)
        {
            foreach (var record in data)
            {
                CsvWriter.WriteRecord(record);
                CsvWriter.NextRecord();
            }
        }

        public void CloseStreams()
        {
            CsvWriter?.Flush();
            StreamWriter?.Flush();
            CsvWriter?.Dispose();
            StreamWriter?.Close();
        }
    }

    /// <summary>
    /// A Csv destination defines a csv file where data from the flow is inserted. Inserts are done in batches (using Bulk insert).
    /// The CSVDestination access a string array as input type. If you need other data types, use the generic CSVDestination instead.
    /// </summary>
    /// <see cref="CSVDestination{TInput}"/>
    /// <example>
    /// <code>
    /// //Non generic CSVDestination works with string[] as input
    /// //use CSVDestination&lt;TInput&gt; for generic usage!
    /// CSVDestination dest = new CSVDestination("/path/to/file.csv");
    /// dest.Wait(); //Wait for all data to arrive
    /// </code>
    /// </example>
    public class CSVDestination : CSVDestination<string[]>
    {
        public CSVDestination() : base() { }

        public CSVDestination(int batchSize) : base(batchSize) { }

        public CSVDestination(string fileName) : base(fileName) { }

        public CSVDestination(string fileName, int batchSize) : base(fileName, batchSize) { }

    }

}
