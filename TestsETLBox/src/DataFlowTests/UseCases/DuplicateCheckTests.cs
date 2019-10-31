using ALE.ETLBox;
using ALE.ETLBox.ConnectionManager;
using ALE.ETLBox.ControlFlow;
using ALE.ETLBox.DataFlow;
using ALE.ETLBox.Helper;
using ALE.ETLBox.Logging;
using ALE.ETLBoxTests.Fixtures;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace ALE.ETLBoxTests.DataFlowTests
{
    [Collection("DataFlow")]
    public class DuplicateCheckTests : IDisposable
    {
        public SqlConnectionManager Connection => Config.SqlConnectionManager("DataFlow");
        public DuplicateCheckTests(DataFlowDatabaseFixture dbFixture)
        {
        }

        public void Dispose()
        {
        }

        public class Poco
        {
            public int ID { get; set; }
            public string Name { get; set; }
            [CsvHelper.Configuration.Attributes.Name("Text")]
            public string Value { get; set; }
            public bool IsDuplicate { get; set; }
        }

        private CSVSource<Poco> CreateDuplicateCSVSource(string fileName)
        {
            CSVSource<Poco> source = new CSVSource<Poco>(fileName);
            source.Configuration.Delimiter = ";";
            source.Configuration.TrimOptions = CsvHelper.Configuration.TrimOptions.Trim;
            source.Configuration.MissingFieldFound = null;
            return source;
        }

        private DBDestination<Poco> CreateDestinationTable(string tableName)
        {
            DropTableTask.DropIfExists(Connection, tableName);
            var dest = new DBDestination<Poco>(Connection, tableName);
            TableDefinition stagingTable = new TableDefinition(tableName, new List<TableColumn>() {
                new TableColumn("PKey", "INT", allowNulls: false, isPrimaryKey:true, isIdentity:true),
                new TableColumn("ID", "INT", allowNulls: false),
                new TableColumn("Value", "NVARCHAR(100)", allowNulls: false),
                new TableColumn("Name", "NVARCHAR(100)", allowNulls: false)
            });
            stagingTable.CreateTable(Connection);
            return dest;
        }

        private void AssertDataWithoutDuplicates()
        {
            Assert.Equal(3, RowCountTask.Count(Connection, "dbo.DuplicateCheck"));
            Assert.Equal(1, RowCountTask.Count(Connection, "dbo.DuplicateCheck", "ID = 1 AND Name='ROOT' AND Value = 'Lorem ipsum'"));
            Assert.Equal(1, RowCountTask.Count(Connection, "dbo.DuplicateCheck", "ID = 2 AND Name='TEST 2' AND Value = 'Lalandia'"));
            Assert.Equal(1, RowCountTask.Count(Connection, "dbo.DuplicateCheck", "ID = 3 AND Name='TEST 3' AND Value = 'XX'"));
        }

        [Fact]
        public void DuplicateCheckInRowTrans()
        {
            //Arrange
            CSVSource<Poco> source = CreateDuplicateCSVSource("res/UseCases/DuplicateCheck.csv");
            List<int> IDs = new List<int>(); //at the end of the flow, this list will contain all IDs of your source

            //Act
            RowTransformation<Poco, Poco> rowTrans = new RowTransformation<Poco, Poco>(input =>
            {
                if (IDs.Contains(input.ID))
                    input.IsDuplicate = true;
                else
                    IDs.Add(input.ID);
                return input;
            });

            Multicast<Poco> multicast = new Multicast<Poco>();
            DBDestination<Poco> dest = CreateDestinationTable("dbo.DuplicateCheck");
            VoidDestination<Poco> trash = new VoidDestination<Poco>();

            source.LinkTo(rowTrans);
            rowTrans.LinkTo(multicast);
            multicast.LinkTo<Poco>(dest, input => input.IsDuplicate == false);
            multicast.LinkTo<Poco>(trash, input => input.IsDuplicate == true);

            source.Execute();
            dest.Wait();
            trash.Wait();

            //Assert
            AssertDataWithoutDuplicates();
        }




        [Fact]
        public void DuplicateCheckWithBlockTrans()
        {
            //Arrange
            CSVSource<Poco> source = CreateDuplicateCSVSource("res/UseCases/DuplicateCheck.csv");
            List<int> IDs = new List<int>(); //at the end of the flow, this list will contain all IDs of your source

            //Act
            BlockTransformation<Poco> blockTrans = new BlockTransformation<Poco>(inputList =>
            {
                return inputList.GroupBy(item => item.ID).Select(y => y.First()).ToList();
            });
            DBDestination<Poco> dest = CreateDestinationTable("dbo.DuplicateCheck");

            source.LinkTo(blockTrans);
            blockTrans.LinkTo(dest);
            source.Execute();
            dest.Wait();

            //Assert
            AssertDataWithoutDuplicates();
        }
    }
}
