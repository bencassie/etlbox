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
    public class BlockTransformationNonGenericTests
    {
        public SqlConnectionManager SqlConnection => Config.SqlConnection.ConnectionManager("DataFlow");
        public BlockTransformationNonGenericTests(DataFlowDatabaseFixture dbFixture)
        {
        }

        [Fact]
        public void ModifyInputDataList()
        {
            //Arrange
            TwoColumnsTableFixture source2Columns = new TwoColumnsTableFixture("BlockTransSourceNonGeneric");
            source2Columns.InsertTestData();
            TwoColumnsTableFixture dest2Columns = new TwoColumnsTableFixture("BlockTransDestNonGeneric");

            DBSource source = new DBSource(SqlConnection, "BlockTransSourceNonGeneric");
            DBDestination dest = new DBDestination(SqlConnection, "BlockTransDestNonGeneric");

            //Act
            BlockTransformation block = new BlockTransformation(
                inputData => {
                    inputData.RemoveRange(1, 2);
                    inputData.Add(new string[] { "4", "Test4" });
                    return inputData;
                });
            source.LinkTo(block);
            block.LinkTo(dest);
            source.Execute();
            dest.Wait();

            //Assert
            Assert.Equal(2, RowCountTask.Count(SqlConnection, "BlockTransDestNonGeneric"));
            Assert.Equal(1, RowCountTask.Count(SqlConnection, "BlockTransDestNonGeneric", "Col1 = 1 AND Col2='Test1'"));
            Assert.Equal(1, RowCountTask.Count(SqlConnection, "BlockTransDestNonGeneric", "Col1 = 4 AND Col2='Test4'"));
        }
    }
}
