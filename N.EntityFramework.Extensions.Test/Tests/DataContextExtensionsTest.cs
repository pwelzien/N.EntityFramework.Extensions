﻿using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using N.EntityFramework.Extensions;
using N.EntityFramework.Extensions.Test.Data;

namespace N.EntityFramework.Extensions.Test.Tests
{
    [TestClass]
    public class DataContextExtensionsTest
    {
        //[TestMethod]
        //public void TestBulkInsert_EF_CustomTable()
        //{
        //    TestDbContext dbContext = new TestDbContext();
        //    var orders = new List<Order>();
        //    for (int i = 0; i < 20000; i++)
        //    {
        //        orders.Add(new Order { Id=i, Price = 1.57M });
        //    }
        //    int oldTotal = dbContext.Orders.Where(o => o.Price <= 10).Count();
        //    int rowsInserted = dbContext.BulkInsert(orders, new BulkInsertOptions<Order> { 
        //        TableName = "[dbo].[Orders3]",
        //        KeepIdentity = true,
        //        InputColumns = (o) => new { o.Id }
        //    });
        //    int newTotal = dbContext.Orders.Where(o => o.Price <= 10).Count();

        //    Assert.IsTrue(rowsInserted == orders.Count, "The number of rows inserted must match the count of order list");
        //    Assert.IsTrue(newTotal - oldTotal == rowsInserted, "The new count minus the old count should match the number of rows inserted.");
        //}
        [TestMethod]
        public void BulkDelete()
        {
            TestDbContext dbContext = new TestDbContext();
            SetupData(dbContext, true);
            var orders = dbContext.Orders.Where(o => o.Price <= 2).ToList();
            int rowsDeleted = dbContext.BulkDelete(orders);
            int newTotal = dbContext.Orders.Where(o => o.Price <= 2).Count();

            Assert.IsTrue(orders.Count > 0, "There must be orders in database that match this condition (Price < $2)");
            Assert.IsTrue(rowsDeleted == orders.Count, "The number of rows deleted must match the count of existing rows in database");
            Assert.IsTrue(newTotal == 0, "Must be 0 to indicate all records were deleted");
        }
        [TestMethod]
        public void BulkInsert()
        {
            TestDbContext dbContext = new TestDbContext();
            var orders = new List<Order>();
            for (int i = 0; i < 20000; i++)
            {
                orders.Add(new Order { Id = i, Price = 1.57M });
            }
            int oldTotal = dbContext.Orders.Where(o => o.Price <= 10).Count();
            int rowsInserted = dbContext.BulkInsert(orders);
            int newTotal = dbContext.Orders.Where(o => o.Price <= 10).Count();

            Assert.IsTrue(rowsInserted == orders.Count, "The number of rows inserted must match the count of order list");
            Assert.IsTrue(newTotal - oldTotal == rowsInserted, "The new count minus the old count should match the number of rows inserted.");
        }
        [TestMethod]
        public void BulkInsert_Without_Identity_Column()
        {
            TestDbContext dbContext = new TestDbContext();
            SetupData(dbContext, true);
            var articles = new List<Article>();
            for (int i = 0; i < 20000; i++)
            {
                articles.Add(new Article { ArticleId = i.ToString(), Price = 1.57M });
            }
            int oldTotal = dbContext.Articles.Where(o => o.Price <= 10).Count();
            int rowsInserted = dbContext.BulkInsert(articles);
            int newTotal = dbContext.Articles.Where(o => o.Price <= 10).Count();

            Assert.IsTrue(rowsInserted == articles.Count, "The number of rows inserted must match the count of order list");
            Assert.IsTrue(newTotal - oldTotal == rowsInserted, "The new count minus the old count should match the number of rows inserted.");
        }
        [TestMethod]
        public void BulkInsert_Options_AutoMapIdentity()
        {
            TestDbContext dbContext = new TestDbContext();
            SetupData(dbContext, true);
            var orders = new List<Order>
            {
                new Order { ExternalId = "id-1", Price=7.10M },
                new Order { ExternalId = "id-2", Price=9.33M },
                new Order { ExternalId = "id-3", Price=3.25M },
                new Order { ExternalId = "id-1000001", Price=2.15M },
                new Order { ExternalId = "id-1000002", Price=5.75M },
            };
            int rowsAdded = dbContext.BulkInsert(orders, new BulkInsertOptions<Order>
            {
                UsePermanentTable = true
            });
            bool autoMapIdentityMatched = true;
            foreach (var order in orders)
            {
                if (!dbContext.Orders.Any(o => o.ExternalId == order.ExternalId && o.Id == order.Id && o.Price == order.Price))
                {
                    autoMapIdentityMatched = false;
                    break;
                }
            }

            Assert.IsTrue(rowsAdded == orders.Count, "The number of rows inserted must match the count of order list");
            Assert.IsTrue(autoMapIdentityMatched, "The auto mapping of ids of entities that were merged failed to match up");
        }
        [TestMethod]
        public void BulkMerge()
        {
            TestDbContext dbContext = new TestDbContext();
            SetupData(dbContext, true);
            var orders = dbContext.Orders.Where(o => o.Id <= 10000).OrderBy(o => o.Id).ToList();
            int ordersToAdd = 5000;
            int ordersToUpdate = orders.Count;
            foreach (var order in orders)
            {
                order.Price = Convert.ToDecimal(order.Id + .25);
            }
            for (int i = 0; i < ordersToAdd; i++)
            {
                orders.Add(new Order { Id = 100000 + i, Price = 3.55M });
            }
            var result = dbContext.BulkMerge(orders);
            var newOrders = dbContext.Orders.OrderBy(o => o.Id).ToList();
            bool areAddedOrdersMerged = true;
            bool areUpdatedOrdersMerged = true;
            foreach (var newOrder in newOrders.Where(o => o.Id <= 10000).OrderBy(o => o.Id))
            {
                if (newOrder.Price != Convert.ToDecimal(newOrder.Id + .25))
                {
                    areUpdatedOrdersMerged = false;
                    break;
                }
            }
            foreach (var newOrder in newOrders.Where(o => o.Id >= 500000).OrderBy(o => o.Id))
            {
                if (newOrder.Price != 3.55M)
                {
                    areAddedOrdersMerged = false;
                    break;
                }
            }

            Assert.IsTrue(result.RowsAffected == orders.Count(), "The number of rows inserted must match the count of order list");
            Assert.IsTrue(result.RowsUpdated == ordersToUpdate, "The number of rows updated must match");
            Assert.IsTrue(result.RowsInserted == ordersToAdd, "The number of rows added must match");
            Assert.IsTrue(areAddedOrdersMerged, "The orders that were added did not merge correctly");
            Assert.IsTrue(areUpdatedOrdersMerged, "The orders that were updated did not merge correctly");
        }
        [TestMethod]
        public void BulkInsert_Options_KeepIdentity()
        {
            TestDbContext dbContext = new TestDbContext();
            SetupData(dbContext, false);
            var orders = new List<Order>();
            for (int i = 0; i < 20000; i++)
            {
                orders.Add(new Order { Id = i, Price = 1.57M });
            }
            int oldTotal = dbContext.Orders.Count();
            int rowsInserted = dbContext.BulkInsert(orders, new BulkInsertOptions<Order>()
            {
                KeepIdentity = true,
                BatchSize = 1000,
            });
            var oldOrders = dbContext.Orders.OrderBy(o => o.Id).ToList();
            var newOrders = dbContext.Orders.OrderBy(o => o.Id).ToList();
            bool allIdentityFieldsMatch = true;
            for (int i = 0; i < 20000; i++)
            {
                if (newOrders[i].Id != oldOrders[i].Id)
                {
                    allIdentityFieldsMatch = false;
                    break;
                }
            }

            Assert.IsTrue(oldTotal == 0, "There should not be any records in the table");
            Assert.IsTrue(rowsInserted == orders.Count, "The number of rows inserted must match the count of order list");
            Assert.IsTrue(allIdentityFieldsMatch, "The identities between the source and the database should match.");
        }
        [TestMethod]
        public void BulkMerge_Options_MergeOnCondition()
        {
            TestDbContext dbContext = new TestDbContext();
            SetupData(dbContext, true);
            var orders = dbContext.Orders.Where(o => o.Id <= 10000).OrderBy(o => o.Id).ToList();
            int ordersToAdd = 5000;
            int ordersToUpdate = orders.Count;
            foreach (var order in orders)
            {
                order.Price = Convert.ToDecimal(order.Id + .25);
            }
            for (int i = 0; i < ordersToAdd; i++)
            {
                orders.Add(new Order { Id = 100000 + i, Price = 3.55M });
            }
            var result = dbContext.BulkMerge(orders, new BulkMergeOptions<Order>
            {
                MergeOnCondition = (s, t) => s.Id == t.Id,
                BatchSize = 1000
            });
            var newOrders = dbContext.Orders.OrderBy(o => o.Id).ToList();
            bool areAddedOrdersMerged = true;
            bool areUpdatedOrdersMerged = true;
            foreach (var newOrder in newOrders.Where(o => o.Id <= 10000).OrderBy(o => o.Id))
            {
                if (newOrder.Price != Convert.ToDecimal(newOrder.Id + .25))
                {
                    areUpdatedOrdersMerged = false;
                    break;
                }
            }
            foreach (var newOrder in newOrders.Where(o => o.Id >= 500000).OrderBy(o => o.Id))
            {
                if (newOrder.Price != 3.55M)
                {
                    areAddedOrdersMerged = false;
                    break;
                }
            }

            Assert.IsTrue(result.RowsAffected == orders.Count(), "The number of rows inserted must match the count of order list");
            Assert.IsTrue(result.RowsUpdated == ordersToUpdate, "The number of rows updated must match");
            Assert.IsTrue(result.RowsInserted == ordersToAdd, "The number of rows added must match");
            Assert.IsTrue(areAddedOrdersMerged, "The orders that were added did not merge correctly");
            Assert.IsTrue(areUpdatedOrdersMerged, "The orders that were updated did not merge correctly");
        }
        [TestMethod]
        public void BulkMerge_Options_AutoMapIdentity()
        {
            TestDbContext dbContext = new TestDbContext();
            SetupData(dbContext, true);
            int ordersToUpdate = 3;
            int ordersToAdd = 2;
            var orders = new List<Order>
            {
                new Order { ExternalId = "id-1", Price=7.10M },
                new Order { ExternalId = "id-2", Price=9.33M },
                new Order { ExternalId = "id-3", Price=3.25M },
                new Order { ExternalId = "id-1000001", Price=2.15M },
                new Order { ExternalId = "id-1000002", Price=5.75M },
            };
            var result = dbContext.BulkMerge(orders, new BulkMergeOptions<Order>
            {
                MergeOnCondition = (s, t) => s.ExternalId == t.ExternalId,
                UsePermanentTable = true
            }) ;
            bool autoMapIdentityMatched = true;
            foreach(var order in orders)
            {
                if (!dbContext.Orders.Any(o => o.ExternalId == order.ExternalId && o.Id == order.Id && o.Price == order.Price))
                {
                    autoMapIdentityMatched = false;
                    break;
                }
            }

            Assert.IsTrue(result.RowsAffected == ordersToAdd + ordersToUpdate, "The number of rows inserted must match the count of order list");
            Assert.IsTrue(result.RowsUpdated == ordersToUpdate, "The number of rows updated must match");
            Assert.IsTrue(result.RowsInserted == ordersToAdd, "The number of rows added must match");
            Assert.IsTrue(autoMapIdentityMatched, "The auto mapping of ids of entities that were merged failed to match up");
        }
        [TestMethod]
        public void BulkUpdate()
        {
            TestDbContext dbContext = new TestDbContext();
            SetupData(dbContext, true);
            var orders = dbContext.Orders.Where(o => o.Price == 1.25M).OrderBy(o => o.Id).ToList();
            long maxId = 0;
            foreach (var order in orders)
            {
                order.Price = 2.35M;
                maxId = order.Id;
            }
            int rowsUpdated = dbContext.BulkUpdate(orders);
            var newOrders = dbContext.Orders.Where(o => o.Price == 2.35M).OrderBy(o => o.Id).Count();
            int entitiesWithChanges = dbContext.ChangeTracker.Entries().Where(t => t.State == EntityState.Modified).Count();

            Assert.IsTrue(orders.Count > 0, "There must be orders in database that match this condition (Price = $1.25)");
            Assert.IsTrue(rowsUpdated == orders.Count, "The number of rows updated must match the count of entities that were retrieved");
            Assert.IsTrue(newOrders == rowsUpdated, "The count of new orders must be equal the number of rows updated in the database.");
            //Assert.IsTrue(entitiesWithChanges == 0, "There should be no pending Order entities with changes after BulkInsert completes");
        }
        [TestMethod]
        public void DeleteFromQuery_IQuerable()
        {
            TestDbContext dbContext = new TestDbContext();
            SetupData(dbContext, true);
            int oldTotal = dbContext.Orders.Where(o => o.Price <= 10).Count();
            int rowsDeleted = dbContext.Orders.Where(o => o.Price <= 10).DeleteFromQuery();
            int newTotal = dbContext.Orders.Where(o => o.Price <= 10).Count();

            Assert.IsTrue(oldTotal > 0, "There must be orders in database that match this condition");
            Assert.IsTrue(rowsDeleted == oldTotal, "The number of rows deleted must match the count of existing rows in database");
            Assert.IsTrue(newTotal == 0, "Delete() Failed: must be 0 to indicate all records were delted");
        }
        [TestMethod]
        public void DeleteFromQuery_IEnumerable()
        {
            TestDbContext dbContext = new TestDbContext();
            SetupData(dbContext, true);
            int oldTotal = dbContext.Orders.Count();
            int rowsDeleted = dbContext.Orders.DeleteFromQuery();
            int newTotal = dbContext.Orders.Count();

            Assert.IsTrue(oldTotal > 0, "There must be orders in database that match this condition");
            Assert.IsTrue(rowsDeleted == oldTotal, "The number of rows deleted must match the count of existing rows in database");
            Assert.IsTrue(newTotal == 0, "The new count must be 0 to indicate all records were deleted");
        }
        [TestMethod]
        public void Fetch()
        {
            TestDbContext dbContext = new TestDbContext();
            SetupData(dbContext, true);
            int batchSize = 1000;
            int batchCount = 0;
            int totalCount = 0;
            int expectedTotalCount = dbContext.Orders.Where(o => o.Price < 10M).Count();
            int expectedBatchCount = (int)Math.Ceiling(expectedTotalCount / (decimal)batchSize);

            dbContext.Orders.Where(o => o.Price < 10M).Fetch(result =>
            {
                batchCount++;
                totalCount += result.Results.Count();
            }, new FetchOptions { BatchSize = 1000 });

            Assert.IsTrue(expectedTotalCount > 0, "There must be orders in database that match this condition");
            Assert.IsTrue(expectedTotalCount == totalCount, "The total number of rows fetched must match the count of existing rows in database");
            Assert.IsTrue(expectedBatchCount == batchCount, "The total number of batches fetched must match what is expected");
        }
        [TestMethod]
        public void InsertFromQuery()
        {
            TestDbContext dbContext = new TestDbContext();
            SetupData(dbContext, true);
            string tableName = "OrdersUnderTen";
            int oldSourceTotal = dbContext.Orders.Where(o => o.Price < 10M).Count();
            //int oldTargetTotal = dbContext.Orders.Where(o => o.Price < 10M).UsingTable(tableName).Count();
            int rowsInserted = dbContext.Orders.Where(o => o.Price < 10M).InsertFromQuery(tableName, o => new { o.Price, o.Id  });
            int newSourceTotal = dbContext.Orders.Where(o => o.Price < 10M).Count();
            int newTargetTotal = dbContext.Orders.Where(o => o.Price < 10M).UsingTable(tableName).Count();

            Assert.IsTrue(oldSourceTotal > 0, "There should be existing data in the source table");
            Assert.IsTrue(oldSourceTotal == newSourceTotal, "There should not be any change in the count of rows in the source table");
            Assert.IsTrue(rowsInserted == oldSourceTotal, "The number of records inserted  must match the count of the source table");
            //Assert.IsTrue(rowsInserted == newTargetTotal, "The different in count in the target table before and after the insert must match the total row inserted");
        }
        [TestMethod]
        public void UpdateFromQuery()
        {
            TestDbContext dbContext = new TestDbContext();
            SetupData(dbContext, true);
            int oldTotal = dbContext.Orders.Where(o => o.Price < 10M).Count();
            int rowUpdated = dbContext.Orders.Where(o => o.Price < 10M).UpdateFromQuery(o => new Order { Price = 25.30M });
            int newTotal = dbContext.Orders.Where(o => o.Price < 10M).Count();
            int matchCount = dbContext.Orders.Where(o => o.Price == 25.30M).Count();

            Assert.IsTrue(oldTotal > 0, "There must be orders in database that match this condition (Price < $10)");
            Assert.IsTrue(rowUpdated == oldTotal, "The number of rows update must match the count of rows that match the condtion (Price < $10)");
            Assert.IsTrue(newTotal == 0, "The new count must be 0 to indicate all records were updated");
            Assert.IsTrue(matchCount == rowUpdated, "The match count must be equal the number of rows updated in the database.");
        }
        [TestMethod]
        public void UpdateFromQuery_With_Different_Culture()
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("sv-SE");
            TestDbContext dbContext = new TestDbContext();
            SetupData(dbContext, true);
            int oldTotal = dbContext.Orders.Where(o => o.Price < 10M).Count();
            int rowUpdated = dbContext.Orders.Where(o => o.Price < 10M).UpdateFromQuery(o => new Order { Price = 25.30M });
            int newTotal = dbContext.Orders.Where(o => o.Price < 10M).Count();
            int matchCount = dbContext.Orders.Where(o => o.Price == 25.30M).Count();

            Assert.AreEqual("25,30", Convert.ToString(25.30M));
            Assert.IsTrue(oldTotal > 0, "There must be orders in database that match this condition (Price < $10)");
            Assert.IsTrue(rowUpdated == oldTotal, "The number of rows update must match the count of rows that match the condtion (Price < $10)");
            Assert.IsTrue(newTotal == 0, "The new count must be 0 to indicate all records were updated");
            Assert.IsTrue(matchCount == rowUpdated, "The match count must be equal the number of rows updated in the database.");
        }
        [TestMethod]
        public void UpdateFromQuery_With_Null_Value()
        {
            TestDbContext dbContext = new TestDbContext();
            SetupData(dbContext, true);
            int oldTotal = dbContext.Orders.Where(o => o.ExternalId != null).Count();
            int rowUpdated = dbContext.Orders.Where(o => o.ExternalId != null).UpdateFromQuery(o => new Order { ExternalId = null });
            int newTotal = dbContext.Orders.Where(o => o.ExternalId != null).Count();

            Assert.IsTrue(oldTotal > 0, "There must be orders in database that match this condition (ExternalId != null)");
            Assert.IsTrue(rowUpdated == oldTotal, "The number of rows update must match the count of rows that match the condition (ExternalId != null)");
            Assert.IsTrue(newTotal == 0, "The new count must be 0 to indicate all records were updated");
        }
        [TestMethod]
        public void UpdateFromQuery_With_Boolean_Value()
        {
            TestDbContext dbContext = new TestDbContext();
            SetupData(dbContext, true);
            int oldTotal = dbContext.Articles.Count(a => a.OutOfStock);
            int rowUpdated = dbContext.Articles.Where(a => a.OutOfStock).UpdateFromQuery(a => new Article { OutOfStock = false });
            int newTotal = dbContext.Articles.Count(o => o.OutOfStock);

            Assert.IsTrue(oldTotal > 0, "There must be articles in database that match this condition (OutOfStock == true)");
            Assert.IsTrue(rowUpdated == oldTotal, "The number of rows update must match the count of rows that match the condition (OutOfStock == false)");
            Assert.IsTrue(newTotal == 0, "The new count must be 0 to indicate all records were updated");
        }
        [TestMethod]
        public void UpdateFromQuery_With_Expression()
        {
            TestDbContext dbContext = new TestDbContext();
            SetupData(dbContext, true);
            decimal priceStart = 10M;
            decimal priceUpdate = 0.34M;

            int oldTotal = dbContext.Orders.Count(a => a.Price < 10);
            int rowUpdated = dbContext.Orders.Where(a => a.Price < 10).UpdateFromQuery(a => new Order { Price = priceStart + priceUpdate });
            int newTotal = dbContext.Orders.Count(o => o.Price < 10);

            Assert.IsTrue(oldTotal > 0, "There must be articles in database that match this condition (Price < 10)");
            Assert.IsTrue(rowUpdated == oldTotal, "The number of rows update must match the count of rows that match the condition (Price < 10)");
            Assert.IsTrue(newTotal == 0, "The new count must be 0 to indicate all records were updated");
        }

        [TestMethod]
        public void UpdateFromQuery_With_Expression_Accessing_Previous_Value_Fails()
        {
            TestDbContext dbContext = new TestDbContext();
            SetupData(dbContext, true);
            decimal priceUpdate = 0.34M;

            // Access to previous value creates an exception as the value must be present when we create the SQL code.
            Assert.ThrowsException<InvalidOperationException>(() => dbContext.Orders.Where(a => a.Price < 10).UpdateFromQuery(a => new Order { Price = a.Price + priceUpdate }));
        }
        
        [TestMethod]
        public void UpdateFromQuery_With_String_Containing_Apostrophe()
        {
            TestDbContext dbContext = new TestDbContext();
            SetupData(dbContext, true);
            int oldTotal = dbContext.Orders.Where(o => o.ExternalId == null).Count();
            int rowUpdated = dbContext.Orders.Where(o => o.ExternalId == null).UpdateFromQuery(o => new Order { ExternalId = "inv'alid" });
            int newTotal = dbContext.Orders.Where(o => o.ExternalId == null).Count();

            Assert.IsTrue(oldTotal > 0, "There must be orders in database that match this condition (ExternalId == null)");
            Assert.IsTrue(rowUpdated == oldTotal, "The number of rows update must match the count of rows that match the condition (ExternalId == null)");
            Assert.IsTrue(newTotal == 0, "The new count must be 0 to indicate all records were updated");
        }
        [TestMethod]
        public void UpdateFromQuery_With_DateTime()
        {
            TestDbContext dbContext = new TestDbContext();
            SetupData(dbContext, true);

            var now = DateTime.Now;

            int oldTotal = dbContext.Articles.Where(a => a.Updated == null).Count();
            int rowUpdated = dbContext.Articles.Where(a => a.Updated == null).UpdateFromQuery(o => new Article { Updated = now });
            int newTotal = dbContext.Articles.Where(a => a.Updated == null).Count();

            Assert.IsTrue(oldTotal > 0, "There must be orders in database that match this condition (Updated == null)");
            Assert.IsTrue(rowUpdated == oldTotal, "The number of rows update must match the count of rows that match the condition (Updated == null)");
            Assert.IsTrue(newTotal == 0, "The new count must be 0 to indicate all records were updated");
        }
        private void SetupData(TestDbContext dbcontext, bool populateData)
        {
            dbcontext.Orders.DeleteFromQuery();
            dbcontext.Articles.DeleteFromQuery();
            if (populateData)
            {
                var orders = new List<Order>();
                int id = 1;
                for (int i = 0; i < 2050; i++)
                {
                    orders.Add(new Order { Id = id, ExternalId=string.Format("id-{0}",i), Price = 1.25M });
                    id++;
                }
                for (int i = 0; i < 1050; i++)
                {
                    orders.Add(new Order { Id = id, Price = 5.35M });
                    id++;
                }
                for (int i = 0; i < 2050; i++)
                {
                    orders.Add(new Order { Id = id, Price = 1.25M });
                    id++;
                }
                for (int i = 0; i < 6000; i++)
                {
                    orders.Add(new Order { Id = id, Price = 15.35M });
                    id++;
                }
                for (int i = 0; i < 6000; i++)
                {
                    orders.Add(new Order { Id = id, Price = 15.35M });
                    id++;
                }

                Debug.WriteLine("Last Id for Order is {0}", id);
                dbcontext.BulkInsert(orders, new BulkInsertOptions<Order>() { KeepIdentity = true });
                var articles = new List<Article>();
                id = 1;
                for (int i = 0; i < 2050; i++)
                {
                    articles.Add(new Article { ArticleId = string.Format("id-{0}", i), Price = 1.25M, OutOfStock = false });
                    id++;
                }
                for (int i = 0; i < 2050; i++)
                {
                    articles.Add(new Article { ArticleId = string.Format("id-{0}", id), Price = 1.25M, OutOfStock = true });
                    id++;
                }

                Debug.WriteLine("Last Id for Article is {0}", id);
                dbcontext.BulkInsert(articles, new BulkInsertOptions<Article>() { KeepIdentity = false, AutoMapOutputIdentity = false });
            }
        }
    }
}
