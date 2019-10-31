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
using Xunit;

namespace ALE.ETLBoxTests.DataFlowTests
{
    [Collection("DataFlow")]
    public class MulticastSplitDataTests : IDisposable
    {
        public SqlConnectionManager Connection => Config.SqlConnectionManager("DataFlow");
        public MulticastSplitDataTests(DataFlowDatabaseFixture dbFixture)
        {
        }

        public void Dispose()
        {
        }

        internal class CSVPoco
        {
            public int CSVCol1 { get; set; }
            public string CSVCol2 { get; set; }
            public long? CSVCol3 { get; set; }
            public decimal CSVCol4 { get; set; }
        }

        internal class Entity1
        {
            public int Col1 { get; set; }
            public string Col2 { get; set; }
        }

        internal class Entity2
        {
            public string Col2 { get; set; }
            public long? Col3 { get; set; }
            public decimal Col4 { get; set; }
        }

        [Fact]
        public void SplitCSVSourceIn2Tables()
        {
            //Arrange
            TwoColumnsTableFixture dest1Table = new TwoColumnsTableFixture("SplitDataDestination1");
            FourColumnsTableFixture dest2Table = new FourColumnsTableFixture("SplitDataDestination2");

            var source = new CSVSource<CSVPoco>("res/Multicast/CSVSourceToSplit.csv")
            {
                Configuration = new CsvHelper.Configuration.Configuration() { Delimiter = ";" }
            };

            var multicast = new Multicast<CSVPoco>();

            var row1 = new RowTransformation<CSVPoco, Entity1>(input => {
                return new Entity1
                {
                    Col1 = input.CSVCol1,
                    Col2 = input.CSVCol2
                };
            });
            var row2 = new RowTransformation<CSVPoco, Entity2>(input => {
                return new Entity2
                {
                    Col2 = input.CSVCol2,
                    Col3 = input.CSVCol3,
                    Col4 = input.CSVCol4
                };
            });

            var destination1 = new DBDestination<Entity1>(Connection, "SplitDataDestination1");
            var destination2 = new DBDestination<Entity2>(Connection, "SplitDataDestination2");

            //Act
            source.LinkTo(multicast);
            multicast.LinkTo<Entity1>(row1);
            multicast.LinkTo<Entity2>(row2);

            row1.LinkTo(destination1);
            row2.LinkTo(destination2);

            source.Execute();
            destination1.Wait();
            destination2.Wait();

            //Assert
            dest1Table.AssertTestData();
            dest2Table.AssertTestData();
        }
    }
}
