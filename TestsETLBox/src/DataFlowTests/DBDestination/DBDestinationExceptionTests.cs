using ALE.ETLBox;
using ALE.ETLBox.ConnectionManager;
using ALE.ETLBox.ControlFlow;
using ALE.ETLBox.DataFlow;
using ALE.ETLBox.Helper;
using ALE.ETLBox.Logging;
using ALE.ETLBoxTests.Fixtures;
using System;
using System.Collections.Generic;
using Xunit;

namespace ALE.ETLBoxTests.DataFlowTests
{
    [Collection("DataFlow")]
    public class DBDestinationExceptionTests
    {
        public static SqlConnectionManager SqlConnection => Config.SqlConnection.ConnectionManager("DataFlow");

        public DBDestinationExceptionTests(DataFlowDatabaseFixture dbFixture)
        {
        }

        [Fact]
        public void UnknownTable()
        {
            //Arrange
            string[] data = { "1", "2" };
            MemorySource source = new MemorySource();
            source.Data.Add(data);
            DBDestination dest = new DBDestination(SqlConnection, "UnknownTable");
            source.LinkTo(dest);

            //Act & Assert
            Assert.Throws<ETLBoxException>(() =>
            {
                try
                {
                    source.Execute();
                    dest.Wait();
                }
                catch (AggregateException e)
                {
                    throw e.InnerException;
                }
            });
        }

        [Fact]
        public void UnknownTableViaTableDefinition()
        {
            //Arrange
            TableDefinition def = new TableDefinition("UnknownTable",
                new List<TableColumn>()
                {
                    new TableColumn("id", "INT")
                });

            //Arrange
            string[] data = { "1", "2" };
            MemorySource source = new MemorySource();
            source.Data.Add(data);
            DBDestination dest = new DBDestination()
            {
                ConnectionManager = SqlConnection,
                DestinationTableDefinition = def
            };
            source.LinkTo(dest);

            //Act & Assert
            Assert.Throws<System.InvalidOperationException>(() =>
            {
                try
                {
                    source.Execute();
                    dest.Wait();
                }
                catch (AggregateException e)
                {
                    throw e.InnerException;
                }
            });
        }
    }
}
