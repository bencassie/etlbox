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
    public class MulticastPredicateTests : IDisposable
    {
        public SqlConnectionManager Connection => Config.SqlConnectionManager("DataFlow");
        public MulticastPredicateTests(DataFlowDatabaseFixture dbFixture)
        {
        }

        public void Dispose()
        {
        }

        public class MySimpleRow
        {
            public int Col1 { get; set; }
            public string Col2 { get; set; }
        }

        [Fact]
        public void PredicateFilteringWithInteger()
        {
            //Arrange
            TwoColumnsTableFixture sourceTable = new TwoColumnsTableFixture("Source");
            sourceTable.InsertTestData();
            TwoColumnsTableFixture dest1Table = new TwoColumnsTableFixture("Destination1");
            TwoColumnsTableFixture dest2Table = new TwoColumnsTableFixture("Destination2");

            DBSource<MySimpleRow> source = new DBSource<MySimpleRow>(Connection, "Source");
            DBDestination<MySimpleRow> dest1 = new DBDestination<MySimpleRow>(Connection, "Destination1");
            DBDestination<MySimpleRow> dest2 = new DBDestination<MySimpleRow>(Connection, "Destination2");

            //Act
            Multicast<MySimpleRow> multicast = new Multicast<MySimpleRow>();
            source.LinkTo(multicast);
            multicast.LinkTo<MySimpleRow>(dest1, row => row.Col1 <= 2);
            multicast.LinkTo<MySimpleRow>(dest2, row => row.Col1 > 2);
            source.Execute();
            dest1.Wait();
            dest2.Wait();

            //Assert
            Assert.Equal(1, RowCountTask.Count(Connection, "Destination1", "Col1 = 1 AND Col2='Test1'"));
            Assert.Equal(1, RowCountTask.Count(Connection, "Destination1", "Col1 = 2 AND Col2='Test2'"));
            Assert.Equal(1, RowCountTask.Count(Connection, "Destination2", "Col1 = 3 AND Col2='Test3'"));

        }
    }
}
