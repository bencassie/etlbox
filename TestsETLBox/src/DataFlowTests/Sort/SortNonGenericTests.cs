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
    public class SortNonGenericTests
    {
        public SqlConnectionManager Connection => Config.SqlConnection.ConnectionManager("DataFlow");
        public SortNonGenericTests(DataFlowDatabaseFixture dbFixture)
        {
        }

        [Fact]
        public void SortSimpleDataDescending()
        {
            //Arrange
            TwoColumnsTableFixture source2Columns = new TwoColumnsTableFixture("SortSourceNonGeneric");
            source2Columns.InsertTestData();
            DBSource source = new DBSource(Connection, "SortSourceNonGeneric");

            //Act
            List<string[]> actual = new List<string[]>();
            CustomDestination dest = new CustomDestination(
                row => actual.Add(row)
            );
            Comparison<string[]> comp = new Comparison<string[]>(
                   (x, y) => int.Parse(y[0]) - int.Parse(x[0])
                );
            Sort block = new Sort(comp);
            source.LinkTo(block);
            block.LinkTo(dest);
            source.Execute();
            dest.Wait();

            //Assert
            List<int> expected = new List<int>() { 3, 2, 1 };
            Assert.Equal(expected, actual.Select(row => int.Parse(row[0])).ToList()) ;
        }
    }
}
