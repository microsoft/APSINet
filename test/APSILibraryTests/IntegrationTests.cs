using Microsoft.Research.APSI.Client;
using Microsoft.Research.APSI.Server;
using System;
using System.Diagnostics;
using System.IO;
using Xunit;
using Xunit.Abstractions;

namespace APSILibraryTests
{
    public class IntegrationTests
    {
        // The client is only accessed through static methods so we need a mechanism
        // to prevent multiple tests running at the same time.
        private static readonly object _lockObj = new();

        private readonly ITestOutputHelper _output;

        public IntegrationTests(ITestOutputHelper output)
        {
            _output = output;
        }

        private void OutputStr(string msg)
        {
            if (null != _output)
            {
                _output.WriteLine(msg);
            }
        }

        [Fact]
        public void MatchOneTest()
        {
            string paramsString = @"{
                ""table_params"": {
                    ""hash_func_count"": 3,
                    ""table_size"": 512,
                    ""max_items_per_bin"": 92
                },
                ""item_params"": {
                    ""felts_per_item"": 8
                },
                ""query_params"": {
                    ""ps_low_degree"": 0,
                    ""query_powers"": [ 1, 3, 4, 5, 8, 14, 20, 26, 32, 38, 41, 42, 43, 45, 46 ]
                },
                ""seal_params"": {
                    ""plain_modulus"": 40961,
                    ""poly_modulus_degree"": 4096,
                    ""coeff_modulus_bits"": [ 40, 32, 32 ]
                }
            }";

            lock (_lockObj)
            {
                // First test a single match
                ulong[,] data = {
                { 10, 0 },
                { 20, 0 },
                { 30, 0 },
                { 40, 0 },
                { 50, 0 },
                { 60, 0 },
                { 70, 0 },
                { 80, 0 },
                { 90, 0 },
                { 100, 0 } };

                OPRFKey oprfKey = new();
                APSIParams parameters = new(paramsString);
                APSIServer server = new(parameters, oprfKey);
                server.SetData(data);

                ulong[,] items = {
                    { 1, 0 },
                    { 2, 0 },
                    { 40, 0 },  // match
                    { 5, 0 } };

                APSIClient client = new();

                byte[] paramsArr = server.GetParameters();
                client.SetParameters(paramsArr);

                byte[] oprfRequest = client.CreateOPRFRequest(items);

                byte[] oprfResponse = OPRFSender.RunOPRF(oprfRequest, oprfKey);

                ulong[,] hashedItems = client.ExtractHashes(oprfResponse);

                byte[] encryptedQuery = client.CreateQuery(hashedItems);
                Assert.NotNull(encryptedQuery);

                byte[] queryResult = server.Query(encryptedQuery);

                bool[] intersection = client.ProcessResult(queryResult);

                Assert.Equal(items.GetLength(dimension: 0), intersection.Length);
                Assert.False(intersection[0]);
                Assert.False(intersection[1]);
                Assert.True(intersection[2]);
                Assert.False(intersection[3]);

                server.Dispose();
                client.Dispose();
            }
        }

        [Fact]
        public void MatchTwoTest()
        {
            string paramsString = @"{
                ""table_params"": {
                    ""hash_func_count"": 3,
                    ""table_size"": 512,
                    ""max_items_per_bin"": 92
                },
                ""item_params"": {
                    ""felts_per_item"": 8
                },
                ""query_params"": {
                    ""ps_low_degree"": 0,
                    ""query_powers"": [ 1, 3, 4, 5, 8, 14, 20, 26, 32, 38, 41, 42, 43, 45, 46 ]
                },
                ""seal_params"": {
                    ""plain_modulus"": 40961,
                    ""poly_modulus_degree"": 4096,
                    ""coeff_modulus_bits"": [ 40, 32, 32 ]
                }
            }";

            lock (_lockObj)
            {
                // Now test a couple of matches
                ulong[,] data = new ulong[,] {
                { 100, 0 },
                { 200, 0 },
                { 300, 0 },
                { 400, 0 },
                { 500, 0x12345678123456 }, // 120 bits
                { 600, 0 },
                { 700, 0 },
                { 800, 0 },
                { 900, 0 },
                { 1000, 0 },
                { 1100, 0 },
                { 1200, 0 },
                { 1300, 0 },
                { 1400, 0 },
                { 1500, 0 },
                { 1600, 0 },
                { 1700, 0 },
                { 1800, 0 },
                { 1900, 0 },
                { 2000, 0 },
                { 500, 0x12345678123455 } };

                OPRFKey oprfKey1 = new();
                OPRFKey oprfKey2 = new();
                APSIParams parameters = new(paramsString);
                APSIServer server = new(parameters, oprfKey1);
                server.SetData(data);

                using (MemoryStream stream = new())
                {
                    oprfKey1.Save(stream);

                    stream.Seek(offset: 0, loc: SeekOrigin.Begin);

                    oprfKey2.Load(stream);
                }

                ulong[,] items = new ulong[,] {
                    { 1200, 0 }, // match
                    { 45, 0 },
                    { 70, 0 },
                    { 90, 0 },
                    { 110, 0 },
                    { 120, 0 },
                    { 500, 0x12345678123456 } }; // match

                APSIClient client = new();

                byte[] paramsArr = server.GetParameters();
                client.SetParameters(paramsArr);

                byte[] oprfRequest = client.CreateOPRFRequest(items);

                byte[] oprfResponse = OPRFSender.RunOPRF(oprfRequest, oprfKey2);

                ulong[,] hashedItems = client.ExtractHashes(oprfResponse);

                byte[] encryptedQuery = client.CreateQuery(hashedItems);
                Assert.NotNull(encryptedQuery);

                byte[] queryResult = server.Query(encryptedQuery);

                bool[] intersection = client.ProcessResult(queryResult);

                Assert.Equal(items.GetLength(dimension: 0), intersection.Length);
                Assert.True(intersection[0]);
                Assert.False(intersection[1]);
                Assert.False(intersection[2]);
                Assert.False(intersection[3]);
                Assert.False(intersection[4]);
                Assert.False(intersection[5]);
                Assert.True(intersection[6]);

                client.Dispose();
                server.Dispose();
            }
        }

        [Fact]
        public void Match120bitTest()
        {
            string paramsString = @"{
                ""table_params"": {
                    ""hash_func_count"": 3,
                    ""table_size"": 512,
                    ""max_items_per_bin"": 92
                },
                ""item_params"": {
                    ""felts_per_item"": 8
                },
                ""query_params"": {
                    ""ps_low_degree"": 0,
                    ""query_powers"": [ 1, 3, 4, 5, 8, 14, 20, 26, 32, 38, 41, 42, 43, 45, 46 ]
                },
                ""seal_params"": {
                    ""plain_modulus"": 40961,
                    ""poly_modulus_degree"": 4096,
                    ""coeff_modulus_bits"": [ 40, 32, 32 ]
                }
            }";

            lock (_lockObj)
            {
                // Now test matches at 120 bits
                ulong[,] data = new ulong[,] {
                { 100, 0 },
                { 200, 0 },
                { 300, 0 },
                { 400, 0 },
                { 500, 0xC2345678123456 }, // 120 bits
                { 500, 0xD2345678123456 }, // 120 bits
                { 700, 0 },
                { 800, 0 },
                { 900, 0 },
                { 1000, 0 },
                { 1100, 0 },
                { 1200, 0 },
                { 1300, 0 },
                { 1400, 0 },
                { 1500, 0 },
                { 1600, 0 },
                { 1700, 0 },
                { 1800, 0 },
                { 1900, 0 },
                { 2000, 0 },
                { 500, 0x32345678123456 } };

                OPRFKey oprfKey = new();
                APSIParams parameters = new(paramsString);
                APSIServer server = new(parameters, oprfKey);

                //OPRFSender.ComputeHashes(data, oprfKey);
                server.SetData(data);

                ulong[,] items = new ulong[,] {
                    { 45, 0 },
                    { 70, 0 },
                    { 1200, 0 }, // match
                    { 90, 0 },
                    { 500, 0x42345678123456 }, // should not match
                    { 500, 0xC2345678123456 }, // match
                    { 120, 0 } };

                APSIClient client = new();

                byte[] paramsArr = server.GetParameters();
                client.SetParameters(paramsArr);

                byte[] oprfRequest = client.CreateOPRFRequest(items);

                byte[] oprfResponse = OPRFSender.RunOPRF(oprfRequest, oprfKey);

                ulong[,] hashedItems = client.ExtractHashes(oprfResponse);

                byte[] encryptedQuery = client.CreateQuery(hashedItems);
                Assert.NotNull(encryptedQuery);

                byte[] queryResult = server.Query(encryptedQuery);

                bool[] intersection = client.ProcessResult(queryResult);

                Assert.Equal(items.GetLength(dimension: 0), intersection.Length);
                Assert.False(intersection[0]);
                Assert.False(intersection[1]);
                Assert.True(intersection[2]);
                Assert.False(intersection[3]);
                Assert.False(intersection[4]);
                Assert.True(intersection[5]);
                Assert.False(intersection[6]);

                client.Dispose();
                server.Dispose();
            }
        }

        private static void DeleteIfExists(string file)
        {
            if (File.Exists(file))
                File.Delete(file);
        }

        private static ulong[,] CopyItems(ulong[,] items)
        {
            int length0 = items.GetLength(dimension: 0);
            int length1 = items.GetLength(dimension: 1);

            ulong[,] result = new ulong[length0, length1];

            for (int i = 0; i < length0; i++)
            {
                for (int j = 0; j < length1; j++)
                {
                    result[i, j] = items[i, j];
                }
            }

            return result;
        }

        private void DBRandomTest(string paramsString, ulong db_size, ulong itemCount, int matchItems, bool saveToFile = false)
        {
            ulong queryTotal = 0;
            ulong resultTotal = 0;

            lock (_lockObj)
            {
                string dbFile = null;
                if (saveToFile)
                {
                    dbFile = Path.Combine(Path.GetTempPath(), "saveloadtest.db");
                    DeleteIfExists(dbFile);
                }

                Random rand = new();
                ulong[,] data = new ulong[db_size, 2];
                int dataCount = data.GetLength(dimension: 0);
                byte[] ulongBuffer = new byte[8];

                ulong[,] items = new ulong[itemCount, 2];
                int itemsIdx = 0;

                for (int idx = 0; idx < dataCount; idx++)
                {
                    rand.NextBytes(ulongBuffer);
                    data[idx, 0] = BitConverter.ToUInt64(ulongBuffer, startIndex: 0);
                    rand.NextBytes(ulongBuffer);
                    data[idx, 1] = BitConverter.ToUInt64(ulongBuffer, startIndex: 0);

                    // Randomly determine if at most 80% values are going to be included
                    if (itemsIdx < matchItems)
                    {
                        if (rand.Next() > (int.MaxValue * 0.8))
                        {
                            items[itemsIdx, 0] = data[idx, 0];
                            items[itemsIdx, 1] = data[idx, 1];
                            itemsIdx++;
                        }
                    }
                }

                // Max index of matching items
                int matchingIdx = itemsIdx;

                // Fill rest of items with random values, which should not match
                for (; itemsIdx < (items.Length / 2); itemsIdx++)
                {
                    rand.NextBytes(ulongBuffer);
                    items[itemsIdx, 0] = BitConverter.ToUInt64(ulongBuffer, startIndex: 0);
                    rand.NextBytes(ulongBuffer);
                    items[itemsIdx, 1] = BitConverter.ToUInt64(ulongBuffer, startIndex: 0);
                }

                // Make a copy of the items to perform a second query
                ulong[,] originalItems = CopyItems(items);

                APSIParams parameters = new(paramsString);
                OPRFKey oprfKey = new();

                using APSIServer server = new(parameters, oprfKey);

                Stopwatch setDataElapsed = Stopwatch.StartNew();
                server.SetData(data);
                setDataElapsed.Stop();

                using APSIClient client = new();

                byte[] paramsArr = server.GetParameters();
                client.SetParameters(paramsArr);

                byte[] oprfRequest = client.CreateOPRFRequest(items);

                byte[] oprfResponse = OPRFSender.RunOPRF(oprfRequest, oprfKey);

                ulong[,] hashedItems = client.ExtractHashes(oprfResponse);

                byte[] encryptedQuery = client.CreateQuery(hashedItems);
                queryTotal += (ulong)encryptedQuery.Length;
                OutputStr($"Resulting query buffer: {encryptedQuery.Length} for {itemCount} items.");
                Assert.NotNull(encryptedQuery);

                Stopwatch serverQueryElapsed = Stopwatch.StartNew();
                byte[] queryResult = server.Query(encryptedQuery);
                serverQueryElapsed.Stop();

                resultTotal += (ulong)queryResult.Length;
                OutputStr($"Query result size is: {queryResult.Length}.");

                bool[] intersection = client.ProcessResult(queryResult);

                //OutputStr($"Compute hashes: {computeHashesElapsed.ElapsedMilliseconds}ms");
                OutputStr($"SetData: {setDataElapsed.ElapsedMilliseconds}ms");
                OutputStr($"Server Query: {serverQueryElapsed.ElapsedMilliseconds}ms");

                for (int i = 0; i < intersection.Length; i++)
                {
                    bool expected = (i < matchingIdx);
                    Assert.Equal(expected, intersection[i]);
                }

                // Save DB
                using MemoryStream ms = new();
                Stopwatch saveServerElapsed = Stopwatch.StartNew();
                if (saveToFile)
                {
                    server.SaveDB(dbFile);
                }
                else
                {
                    server.SaveDB(ms);
                }
                saveServerElapsed.Stop();
                server.Dispose();

                APSIServer server2 = null;
                Stopwatch loadServerElapsed = Stopwatch.StartNew();

                if (saveToFile)
                {
                    server2 = APSIServer.LoadDB(dbFile);
                }
                else
                {
                    ms.Seek(offset: 0, loc: SeekOrigin.Begin);
                    byte[] bytes = ms.ToArray();
                    server2 = APSIServer.LoadDB(bytes);
                    bytes = null;
                }
                loadServerElapsed.Stop();
                ms.Dispose();

                OutputStr($"Save server: {saveServerElapsed.ElapsedMilliseconds}ms");
                OutputStr($"Load server: {loadServerElapsed.ElapsedMilliseconds}ms");

                // Restore items
                items = CopyItems(originalItems);

                using APSIClient client2 = new();

                paramsArr = server2.GetParameters();
                client2.SetParameters(paramsArr);

                oprfRequest = client2.CreateOPRFRequest(items);

                oprfResponse = OPRFSender.RunOPRF(oprfRequest, oprfKey);

                hashedItems = client2.ExtractHashes(oprfResponse);

                encryptedQuery = client2.CreateQuery(hashedItems);
                queryTotal += (ulong)encryptedQuery.Length ;
                OutputStr($"Resulting query buffer: {encryptedQuery.Length} for {itemCount} items.");
                Assert.NotNull(encryptedQuery);

                serverQueryElapsed = Stopwatch.StartNew();
                queryResult = server2.Query(encryptedQuery);
                serverQueryElapsed.Stop();

                resultTotal += (ulong)queryResult.Length;
                OutputStr($"Query result size is: {queryResult.Length}.");

                intersection = client2.ProcessResult(queryResult);

                OutputStr($"SetData: {setDataElapsed.ElapsedMilliseconds}ms");
                OutputStr($"Server Query: {serverQueryElapsed.ElapsedMilliseconds}ms");

                for (int i = 0; i < intersection.Length; i++)
                {
                    bool expected = (i < matchingIdx);
                    Assert.Equal(expected, intersection[i]);
                }

                if (saveToFile)
                {
                    DeleteIfExists(dbFile);
                }
            }
        }

        [Fact]
        public void DB64KRandom1Test()
        {
            ulong db_size = 65536;
            ulong itemCount = 1;
            int matchItems = 1;
            string paramsString = @"{
                ""table_params"": {
                    ""hash_func_count"": 3,
                    ""table_size"": 512,
                    ""max_items_per_bin"": 92
                },
                ""item_params"": {
                    ""felts_per_item"": 8
                },
                ""query_params"": {
                    ""ps_low_degree"": 0,
                    ""query_powers"": [ 1, 3, 4, 5, 8, 14, 20, 26, 32, 38, 41, 42, 43, 45, 46 ]
                },
                ""seal_params"": {
                    ""plain_modulus"": 40961,
                    ""poly_modulus_degree"": 4096,
                    ""coeff_modulus_bits"": [ 40, 32, 32 ]
                }
            }";

            DBRandomTest(paramsString, db_size, itemCount, matchItems);
        }

        [Fact]
        public void DB64KRandom9Test()
        {
            ulong db_size = 65536;
            ulong itemCount = 9;  // Number of items to include in query
            int matchItems = 4;   // Number of items that will match
            string paramsString = @"{
                ""table_params"": {
                    ""hash_func_count"": 3,
                    ""table_size"": 512,
                    ""max_items_per_bin"": 92
                },
                ""item_params"": {
                    ""felts_per_item"": 8
                },
                ""query_params"": {
                    ""ps_low_degree"": 0,
                    ""query_powers"": [ 1, 3, 4, 5, 8, 14, 20, 26, 32, 38, 41, 42, 43, 45, 46 ]
                },
                ""seal_params"": {
                    ""plain_modulus"": 40961,
                    ""poly_modulus_degree"": 4096,
                    ""coeff_modulus_bits"": [ 40, 32, 32 ]
                }
            }";

            DBRandomTest(paramsString, db_size, itemCount, matchItems);
        }

        [Fact]
        public void DB64KRandom10Test()
        {
            ulong db_size = 65536;
            ulong itemCount = 10;  // Number of items to include in query
            int matchItems = 4;    // Number of items that will match
            string paramsString = @"{
                ""table_params"": {
                    ""hash_func_count"": 3,
                    ""table_size"": 512,
                    ""max_items_per_bin"": 92
                },
                ""item_params"": {
                    ""felts_per_item"": 8
                },
                ""query_params"": {
                    ""ps_low_degree"": 0,
                    ""query_powers"": [ 1, 3, 4, 5, 8, 14, 20, 26, 32, 38, 41, 42, 43, 45, 46 ]
                },
                ""seal_params"": {
                    ""plain_modulus"": 40961,
                    ""poly_modulus_degree"": 4096,
                    ""coeff_modulus_bits"": [ 40, 32, 32 ]
                }
            }";

            DBRandomTest(paramsString, db_size, itemCount, matchItems);
        }

        [Fact]
        public void DB64KRandom11Test()
        {
            ulong db_size = 65536;
            ulong itemCount = 11;  // Number of items to include in query
            int matchItems = 4;    // Number of items that will match
            string paramsString = @"{
                ""table_params"": {
                    ""hash_func_count"": 3,
                    ""table_size"": 512,
                    ""max_items_per_bin"": 92
                },
                ""item_params"": {
                    ""felts_per_item"": 8
                },
                ""query_params"": {
                    ""ps_low_degree"": 0,
                    ""query_powers"": [ 1, 3, 4, 5, 8, 14, 20, 26, 32, 38, 41, 42, 43, 45, 46 ]
                },
                ""seal_params"": {
                    ""plain_modulus"": 40961,
                    ""poly_modulus_degree"": 4096,
                    ""coeff_modulus_bits"": [ 40, 32, 32 ]
                }
            }";

            DBRandomTest(paramsString, db_size, itemCount, matchItems);
        }

        [Fact]
        public void DB64KRandom250Test()
        {
            ulong db_size = 65536;
            ulong itemCount = 250;  // Number of items to include in query
            int matchItems = 110;   // Number of items that will match
            string paramsString = @"{
                ""table_params"": {
                    ""hash_func_count"": 3,
                    ""table_size"": 512,
                    ""max_items_per_bin"": 92
                },
                ""item_params"": {
                    ""felts_per_item"": 8
                },
                ""query_params"": {
                    ""ps_low_degree"": 0,
                    ""query_powers"": [ 1, 3, 4, 5, 8, 14, 20, 26, 32, 38, 41, 42, 43, 45, 46 ]
                },
                ""seal_params"": {
                    ""plain_modulus"": 40961,
                    ""poly_modulus_degree"": 4096,
                    ""coeff_modulus_bits"": [ 40, 32, 32 ]
                }
            }";

            DBRandomTest(paramsString, db_size, itemCount, matchItems);
        }

        [Fact]
        public void DB64KRandom300Test()
        {
            ulong db_size = 65536;
            ulong itemCount = 300;  // Number of items to include in query
            int matchItems = 200;   // Number of items that will match
            string paramsString = @"{
                ""table_params"": {
                    ""hash_func_count"": 3,
                    ""table_size"": 512,
                    ""max_items_per_bin"": 92
                },
                ""item_params"": {
                    ""felts_per_item"": 8
                },
                ""query_params"": {
                    ""ps_low_degree"": 0,
                    ""query_powers"": [ 1, 3, 4, 5, 8, 14, 20, 26, 32, 38, 41, 42, 43, 45, 46 ]
                },
                ""seal_params"": {
                    ""plain_modulus"": 40961,
                    ""poly_modulus_degree"": 4096,
                    ""coeff_modulus_bits"": [ 40, 32, 32 ]
                }
            }";

            DBRandomTest(paramsString, db_size, itemCount, matchItems);
        }

        [Fact]
        public void DB64KRandom300SaveToFileTest()
        {
            ulong db_size = 65536;
            ulong itemCount = 300;
            int matchItems = 200;
            string paramsString = @"{
                ""table_params"": {
                    ""hash_func_count"": 3,
                    ""table_size"": 512,
                    ""max_items_per_bin"": 92
                },
                ""item_params"": {
                    ""felts_per_item"": 8
                },
                ""query_params"": {
                    ""ps_low_degree"": 0,
                    ""query_powers"": [ 1, 3, 4, 5, 8, 14, 20, 26, 32, 38, 41, 42, 43, 45, 46 ]
                },
                ""seal_params"": {
                    ""plain_modulus"": 40961,
                    ""poly_modulus_degree"": 4096,
                    ""coeff_modulus_bits"": [ 40, 32, 32 ]
                }
            }";

            DBRandomTest(paramsString, db_size, itemCount, matchItems, saveToFile: true);
        }

        [Fact]
        public void DB300KRandom1Test()
        {
            ulong db_size = 300000;
            ulong itemCount = 1;
            int matchItems = 1;
            string paramsString = @"{
                ""table_params"": {
                    ""hash_func_count"": 3,
                    ""table_size"": 512,
                    ""max_items_per_bin"": 92
                },
                ""item_params"": {
                    ""felts_per_item"": 8
                },
                ""query_params"": {
                    ""ps_low_degree"": 0,
                    ""query_powers"": [ 1, 3, 4, 5, 8, 14, 20, 26, 32, 38, 41, 42, 43, 45, 46 ]
                },
                ""seal_params"": {
                    ""plain_modulus"": 40961,
                    ""poly_modulus_degree"": 4096,
                    ""coeff_modulus_bits"": [ 40, 32, 32 ]
                }
            }";

            DBRandomTest(paramsString, db_size, itemCount, matchItems);
        }

        [Fact]
        public void DB300KRandom10Test()
        {
            ulong db_size = 300000;
            ulong itemCount = 10;
            int matchItems = 5;
            string paramsString = @"{
                ""table_params"": {
                    ""hash_func_count"": 3,
                    ""table_size"": 512,
                    ""max_items_per_bin"": 92
                },
                ""item_params"": {
                    ""felts_per_item"": 8
                },
                ""query_params"": {
                    ""ps_low_degree"": 0,
                    ""query_powers"": [ 1, 3, 4, 5, 8, 14, 20, 26, 32, 38, 41, 42, 43, 45, 46 ]
                },
                ""seal_params"": {
                    ""plain_modulus"": 40961,
                    ""poly_modulus_degree"": 4096,
                    ""coeff_modulus_bits"": [ 40, 32, 32 ]
                }
            }";

            DBRandomTest(paramsString, db_size, itemCount, matchItems);
        }

        [Fact]
        public void DB300KRandom50Test()
        {
            ulong db_size = 300000;
            ulong itemCount = 50;
            int matchItems = 20;
            string paramsString = @"{
                ""table_params"": {
                    ""hash_func_count"": 3,
                    ""table_size"": 512,
                    ""max_items_per_bin"": 92
                },
                ""item_params"": {
                    ""felts_per_item"": 8
                },
                ""query_params"": {
                    ""ps_low_degree"": 0,
                    ""query_powers"": [ 1, 3, 4, 5, 8, 14, 20, 26, 32, 38, 41, 42, 43, 45, 46 ]
                },
                ""seal_params"": {
                    ""plain_modulus"": 40961,
                    ""poly_modulus_degree"": 4096,
                    ""coeff_modulus_bits"": [ 40, 32, 32 ]
                }
            }";

            DBRandomTest(paramsString, db_size, itemCount, matchItems);
        }

        [Fact]
        public void DB300KRandom300Test()
        {
            ulong db_size = 300000;
            ulong itemCount = 300;
            int matchItems = 200;
            string paramsString = @"{
                ""table_params"": {
                    ""hash_func_count"": 3,
                    ""table_size"": 512,
                    ""max_items_per_bin"": 92
                },
                ""item_params"": {
                    ""felts_per_item"": 8
                },
                ""query_params"": {
                    ""ps_low_degree"": 0,
                    ""query_powers"": [ 1, 3, 4, 5, 8, 14, 20, 26, 32, 38, 41, 42, 43, 45, 46 ]
                },
                ""seal_params"": {
                    ""plain_modulus"": 40961,
                    ""poly_modulus_degree"": 4096,
                    ""coeff_modulus_bits"": [ 40, 32, 32 ]
                }
            }";

            DBRandomTest(paramsString, db_size, itemCount, matchItems);
        }

        [Fact]
        public void DB300KRandom300SaveToFileTest()
        {
            ulong db_size = 300000;
            ulong itemCount = 300;
            int matchItems = 150;
            string paramsString = @"{
                ""table_params"": {
                    ""hash_func_count"": 3,
                    ""table_size"": 512,
                    ""max_items_per_bin"": 92
                },
                ""item_params"": {
                    ""felts_per_item"": 8
                },
                ""query_params"": {
                    ""ps_low_degree"": 0,
                    ""query_powers"": [ 1, 3, 4, 5, 8, 14, 20, 26, 32, 38, 41, 42, 43, 45, 46 ]
                },
                ""seal_params"": {
                    ""plain_modulus"": 40961,
                    ""poly_modulus_degree"": 4096,
                    ""coeff_modulus_bits"": [ 40, 32, 32 ]
                }
            }";

            DBRandomTest(paramsString, db_size, itemCount, matchItems, saveToFile: true);
        }

        [Fact]
        public void DB64KTest()
        {
            string paramsString = @"{
                ""table_params"": {
                    ""hash_func_count"": 3,
                    ""table_size"": 512,
                    ""max_items_per_bin"": 92
                },
                ""item_params"": {
                    ""felts_per_item"": 8
                },
                ""query_params"": {
                    ""ps_low_degree"": 0,
                    ""query_powers"": [ 1, 3, 4, 5, 8, 14, 20, 26, 32, 38, 41, 42, 43, 45, 46 ]
                },
                ""seal_params"": {
                    ""plain_modulus"": 40961,
                    ""poly_modulus_degree"": 4096,
                    ""coeff_modulus_bits"": [ 40, 32, 32 ]
                }
            }";

            lock (_lockObj)
            {
                ulong[,] data = new ulong[65536, 2];
                int dataCount = data.GetLength(dimension: 0);
                for (int idx = 0; idx < dataCount; idx++)
                {
                    data[idx, 0] = 0;
                    data[idx, 1] = (ulong)(idx + 1);
                }

                OPRFKey oprfKey = new();
                APSIParams parameters = new(paramsString);
                APSIServer server = new(parameters, oprfKey);

                server.SetData(data);

                ulong[,] items = new ulong[,]
                {
                   { 1, 0 },
                   { 0, 5 }, // match
                   { 2, 0 },
                   { 0, 10000 }, // match
                   { 3, 0 }
                };

                APSIClient client = new();

                byte[] paramsArr = server.GetParameters();
                client.SetParameters(paramsArr);

                byte[] oprfRequest = client.CreateOPRFRequest(items);

                byte[] oprfResponse = OPRFSender.RunOPRF(oprfRequest, oprfKey);

                ulong[,] hashedItems = client.ExtractHashes(oprfResponse);

                byte[] encryptedQuery = client.CreateQuery(hashedItems);
                Assert.NotNull(encryptedQuery);

                byte[] queryResult = server.Query(encryptedQuery);

                bool[] intersection = client.ProcessResult(queryResult);

                Assert.Equal(items.GetLength(dimension: 0), intersection.Length);
                Assert.False(intersection[0]);
                Assert.True(intersection[1]);
                Assert.False(intersection[2]);
                Assert.True(intersection[3]);
                Assert.False(intersection[4]);

                client.Dispose();
                server.Dispose();
            }
        }

        [Fact]
        public void SingleQueryTest()
        {
            string paramsStr = @"
            {
                ""table_params"": {
                    ""hash_func_count"": 1,
                    ""table_size"": 409,
                    ""max_items_per_bin"": 35
                },
                ""item_params"": {
                    ""felts_per_item"": 5
                },
                ""query_params"": {
                    ""ps_low_degree"": 0,
                    ""query_powers"": [ 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35 ]
                },
                ""seal_params"": {
                    ""plain_modulus"": 65537,
                    ""poly_modulus_degree"": 2048,
                    ""coeff_modulus_bits"": [ 48 ]
                }
            }";

            lock (_lockObj)
            {
                ulong[,] data = {
                        { 10, 0 },
                        { 20, 0 },
                        { 30, 0 },
                        { 40, 0 },
                        { 50, 0 },
                        { 60, 0 },
                        { 70, 0 },
                        { 80, 0 },
                        { 90, 0 },
                        { 100, 0 } };

                OPRFKey oprfKey = new();
                APSIParams parameters = new(paramsStr);
                APSIServer server = new(parameters, oprfKey);

                server.SetData(data);

                ulong[,] items = new ulong[,]
                {
                    { 70, 0 }  // match
                };

                APSIClient client = new();

                byte[] paramsArr = server.GetParameters();
                client.SetParameters(paramsArr);

                byte[] oprfRequest = client.CreateOPRFRequest(items);

                byte[] oprfResponse = OPRFSender.RunOPRF(oprfRequest, oprfKey);

                ulong[,] hashedItems = client.ExtractHashes(oprfResponse);

                byte[] encryptedQuery = client.CreateQuery(hashedItems);
                Assert.NotNull(encryptedQuery);

                byte[] queryResult = server.Query(encryptedQuery);

                bool[] intersection = client.ProcessResult(queryResult);

                Assert.Single(intersection);
                Assert.True(intersection[0]);

                items = new ulong[,]
                {
                    { 200, 0 } // no match
                };

                oprfRequest = client.CreateOPRFRequest(items);

                oprfResponse = OPRFSender.RunOPRF(oprfRequest, oprfKey);

                hashedItems = client.ExtractHashes(oprfResponse);

                encryptedQuery = client.CreateQuery(hashedItems);
                Assert.NotNull(encryptedQuery);

                queryResult = server.Query(encryptedQuery);

                intersection = client.ProcessResult(queryResult);

                Assert.Single(intersection);
                Assert.False(intersection[0]);

                client.Dispose();
                server.Dispose();
            }
        }

        [Fact]
        public void SingleQuery64KDBTest()
        {
            string paramsStr = @"
            {
                ""table_params"": {
                    ""hash_func_count"": 1,
                    ""table_size"": 409,
                    ""max_items_per_bin"": 35
                },
                ""item_params"": {
                    ""felts_per_item"": 5
                },
                ""query_params"": {
                    ""ps_low_degree"": 0,
                    ""query_powers"": [ 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35 ]
                },
                ""seal_params"": {
                    ""plain_modulus"": 65537,
                    ""poly_modulus_degree"": 2048,
                    ""coeff_modulus_bits"": [ 48 ]
                }
            }";

            lock (_lockObj)
            {
                ulong[,] data = new ulong[65536, 2];
                int dataCount = data.GetLength(dimension: 0);
                for (int idx = 0; idx < dataCount; idx++)
                {
                    data[idx, 0] = 0;
                    data[idx, 1] = (ulong)(idx + 1);
                }

                OPRFKey oprfKey = new();
                APSIParams parameters = new(paramsStr);
                APSIServer server = new(parameters, oprfKey);

                server.SetData(data);

                ulong[,] items = new ulong[,]
                {
                    { 0, 10000 } // match
                };

                APSIClient client = new();

                byte[] paramBytes = server.GetParameters();
                client.SetParameters(paramBytes);

                byte[] oprfRequest = client.CreateOPRFRequest(items);

                byte[] oprfResponse = OPRFSender.RunOPRF(oprfRequest, oprfKey);

                ulong[,] hashedItems = client.ExtractHashes(oprfResponse);

                byte[] encryptedQuery = client.CreateQuery(hashedItems);
                Assert.NotNull(encryptedQuery);

                byte[] queryResult = server.Query(encryptedQuery);

                bool[] intersection = client.ProcessResult(queryResult);

                Assert.Single(intersection);
                Assert.True(intersection[0]);

                items = new ulong[,]
                {
                    { 10000, 0 } // no match
                };

                oprfRequest = client.CreateOPRFRequest(items);

                oprfResponse = OPRFSender.RunOPRF(oprfRequest, oprfKey);

                hashedItems = client.ExtractHashes(oprfResponse);

                encryptedQuery = client.CreateQuery(hashedItems);
                Assert.NotNull(encryptedQuery);

                queryResult = server.Query(encryptedQuery);

                intersection = client.ProcessResult(queryResult);

                Assert.Single(intersection);
                Assert.False(intersection[0]);


                client.Dispose();
                server.Dispose();
            }
        }
    }
}
