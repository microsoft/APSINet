using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using Microsoft.Research.APSI.Client;
using Microsoft.Research.APSI.Server;

namespace ConsoleTester
{
    public class Assert
    {
        internal static void True(bool v)
        {
            if (!v)
                throw new InvalidOperationException("Should be false, it's not!");
        }

        internal static void Equal(byte expected, byte v, int iteration)
        {
            if (expected != v)
                throw new InvalidOperationException($"Iteration: {iteration}. Should be equal to {expected}, it's {v}!");
        }
    }

    public class Tester
    {
        public static void Test(string jsonParams, int dbSize, ulong itemCount, int matchItems, int numIterations = 100)
        {
            int iterations;
            int failedCount = 0;
            int failed2Count = 0;
            DateTime start = DateTime.Now;
            uint threadCount = 8;

            long receiverPreQueryAccumulated = 0;
            long receiverPreQuery2Accumulated = 0;
            long receiverQueryAccumulated = 0;
            long receiverQuery2Accumulated = 0;
            long senderPreQueryAccumulated = 0;
            long senderPreQuery2Accumulated = 0;
            long senderQueryAccumulated = 0;
            long senderQuery2Accumulated = 0;
            long senderSetDataAccumulated = 0;
            long senderSaveDBAccumulated = 0;
            long senderLoadDBAccumulated = 0;
            long senderSaveDBToFileAccumulated = 0;
            long senderLoadDBFromFileAccumulated = 0;
            long queryBufferAccumulated = 0;
            long queryResultBufferAccumulated = 0;

            APSIServer.SetThreads(threadCount); // Max threads
            APSIParams parameters = new(jsonParams);

            for (iterations = 0; iterations < numIterations; iterations++)
            {
                Console.WriteLine($"Iteration: {iterations}");
                Random rand = new();
                ulong[,] data = new ulong[dbSize, 2];
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

                    // Randomly determine if at most 70 values are going to be included
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
                Console.WriteLine($"Matching idx: {matchingIdx}");

                // Fill rest of items with random values, which should not match
                for (; itemsIdx < (items.Length / 2); itemsIdx++)
                {
                    rand.NextBytes(ulongBuffer);
                    items[itemsIdx, 0] = BitConverter.ToUInt64(ulongBuffer, startIndex: 0);
                    rand.NextBytes(ulongBuffer);
                    items[itemsIdx, 1] = BitConverter.ToUInt64(ulongBuffer, startIndex: 0);
                }

                // Copy original items
                ulong[,] originalItems = new ulong[items.GetLength(dimension: 0), items.GetLength(dimension: 1)];
                Array.Copy(items, originalItems, items.Length);

                OPRFKey oprfKey = new("2000000097F4C67A657B3463DF3B008AC71CBC206F256A412A781766928C3D9593E50700");

                APSIServer server = new(parameters, oprfKey);

                Stopwatch senderSetDataElapsed = Stopwatch.StartNew();
                server.SetData(data);

                senderSetDataElapsed.Stop();
                senderSetDataAccumulated += senderSetDataElapsed.ElapsedMilliseconds;

                APSIClient client = new();

                byte[] paramsArr = server.GetParameters();
                client.SetParameters(paramsArr);

                Stopwatch receiverPreQueryElapsed = Stopwatch.StartNew();
                byte[] oprfRequest = client.CreateOPRFRequest(items);
                receiverPreQueryElapsed.Stop();
                receiverPreQueryAccumulated += receiverPreQueryElapsed.ElapsedMilliseconds;

                Stopwatch senderPreQueryElapsed = Stopwatch.StartNew();
                byte[] oprfResponse = OPRFSender.RunOPRF(oprfRequest, oprfKey);
                senderPreQueryElapsed.Stop();
                senderPreQueryAccumulated += senderPreQueryElapsed.ElapsedMilliseconds;

                ulong[,] hashedItems = client.ExtractHashes(oprfResponse);

                APSIServer.SetThreads(threadCount); // Max threads

                Stopwatch receiverQueryElapsed = Stopwatch.StartNew();
                byte[] encryptedQuery = client.CreateQuery(hashedItems);
                receiverQueryElapsed.Stop();
                receiverQueryAccumulated += receiverQueryElapsed.ElapsedMilliseconds;
                queryBufferAccumulated += encryptedQuery.LongLength;

                Stopwatch senderQueryElapsed = Stopwatch.StartNew();
                byte[] queryResult = server.Query(encryptedQuery);
                senderQueryElapsed.Stop();
                senderQueryAccumulated += senderQueryElapsed.ElapsedMilliseconds;
                queryResultBufferAccumulated += queryResult.LongLength;

                bool[] intersection = client.ProcessResult(queryResult);

                for (int i = 0; i < intersection.Length; i++)
                {
                    bool expected = (i < matchingIdx);
                    if (expected != intersection[i])
                    {
                        Console.WriteLine($"Iteration {iterations} failed!");
                        failedCount++;
                        break;
                    }
                }

                MemoryStream ms = new();
                Stopwatch senderSaveDBElapsed = Stopwatch.StartNew();
                server.SaveDB(ms);
                senderSaveDBElapsed.Stop();
                senderSaveDBAccumulated += senderSaveDBElapsed.ElapsedMilliseconds;

                senderSaveDBElapsed = Stopwatch.StartNew();
                server.SaveDB(@"d:\progs\temp\db.db");
                senderSaveDBElapsed.Stop();
                senderSaveDBToFileAccumulated += senderSaveDBElapsed.ElapsedMilliseconds;

                byte[] serverDB = ms.ToArray();

                ms.Dispose();
                server.Dispose();
                client.Dispose();

                // Copy back items
                Array.Copy(originalItems, items, originalItems.GetLength(dimension: 0) * 2);

                Stopwatch serverLoadElapsed = Stopwatch.StartNew();
                APSIServer server2 = APSIServer.LoadDB(serverDB);
                serverLoadElapsed.Stop();
                senderLoadDBAccumulated += serverLoadElapsed.ElapsedMilliseconds;

                APSIClient client2 = new APSIClient();

                paramsArr = server2.GetParameters();
                client2.SetParameters(paramsArr);

                receiverPreQueryElapsed = Stopwatch.StartNew();
                oprfRequest = client2.CreateOPRFRequest(items);
                receiverPreQueryElapsed.Stop();
                receiverPreQuery2Accumulated += receiverPreQueryElapsed.ElapsedMilliseconds;

                senderPreQueryElapsed = Stopwatch.StartNew();
                oprfResponse = OPRFSender.RunOPRF(oprfRequest, oprfKey);
                senderPreQueryElapsed.Stop();
                senderPreQuery2Accumulated += senderPreQueryElapsed.ElapsedMilliseconds;

                hashedItems = client2.ExtractHashes(oprfResponse);

                APSIServer.SetThreads(threadCount); // Max threads

                encryptedQuery = null;
                receiverQueryElapsed = Stopwatch.StartNew();
                encryptedQuery = client2.CreateQuery(hashedItems);
                receiverQueryElapsed.Stop();
                receiverQuery2Accumulated += receiverQueryElapsed.ElapsedMilliseconds;
                Console.WriteLine($"Resulting query buffer: {encryptedQuery.Length}, {receiverQueryElapsed.ElapsedMilliseconds}ms");

                senderQueryElapsed = Stopwatch.StartNew();
                queryResult = server2.Query(encryptedQuery);
                senderQueryElapsed.Stop();
                senderQuery2Accumulated += senderQueryElapsed.ElapsedMilliseconds;
                Console.WriteLine($"Query result size is: {queryResult.Length}, {senderQueryElapsed.ElapsedMilliseconds}ms");

                intersection = client2.ProcessResult(queryResult);

                for (int i = 0; i < intersection.Length; i++)
                {
                    bool expected = (i < matchingIdx);
                    if (expected != intersection[i])
                    {
                        Console.WriteLine($"Iteration {iterations} failed!");
                        failed2Count++;
                        break;
                    }
                }

                server2.Dispose();
                client2.Dispose();

                serverLoadElapsed = Stopwatch.StartNew();
                APSIServer server3 = APSIServer.LoadDB(@"d:\progs\temp\db.db");
                serverLoadElapsed.Stop();
                senderLoadDBFromFileAccumulated += serverLoadElapsed.ElapsedMilliseconds;

                server3.Dispose();
                File.Delete(@"d:\progs\temp\db.db");
            }
            DateTime end = DateTime.Now;

            Console.WriteLine($"Ran for {iterations} iterations, {dbSize} DB, {itemCount} items");
            Console.WriteLine($"Failed {failedCount} times, first query");
            Console.WriteLine($"Failed {failed2Count} times, second query");
            double iterationsD = iterations;
            Console.WriteLine($"Receiver prequery avg: {receiverPreQueryAccumulated / iterationsD}ms");
            Console.WriteLine($"Receiver query avg: {receiverQueryAccumulated / iterationsD}ms");
            Console.WriteLine($"Sender prequery avg: {senderPreQueryAccumulated / iterationsD}ms");
            Console.WriteLine($"Sender setdata avg: {senderSetDataAccumulated / iterationsD}ms");
            Console.WriteLine($"Sender query avg: {senderQueryAccumulated / iterationsD}ms");
            Console.WriteLine($"Receiver prequery2 avg: {receiverPreQuery2Accumulated / iterationsD}ms");
            Console.WriteLine($"Receiver query2 avg: {receiverQuery2Accumulated / iterationsD}ms");
            Console.WriteLine($"Sender prequery2 avg: {senderPreQuery2Accumulated / iterationsD}ms");
            Console.WriteLine($"Sender query2 avg: {senderQuery2Accumulated / iterationsD}ms");
            Console.WriteLine($"Sender save to stream avg: {senderSaveDBAccumulated / iterationsD}ms");
            Console.WriteLine($"Sender load from byte array avg: {senderLoadDBAccumulated / iterationsD}ms");
            Console.WriteLine($"Sender save to file avg: {senderSaveDBToFileAccumulated / iterationsD}ms");
            Console.WriteLine($"Sender load from file avg: {senderLoadDBFromFileAccumulated / iterationsD}ms");
            Console.WriteLine($"Query buffer size avg: {queryBufferAccumulated / iterationsD}");
            Console.WriteLine($"Query result buffer size avg: {queryResultBufferAccumulated / iterationsD}");

            TimeSpan timeSpan = end - start;
            Console.WriteLine($"Elapsed: {timeSpan}");
        }

        public static void MultiThreadingTest()
        {
            //ulong itemCount = 200;
            //int matchItems = 50;
            //int failedCount = 0;

            //Random rand = new Random();
            //ulong[,] data = new ulong[65536, 2];
            //int dataCount = data.GetLength(dimension: 0);
            //byte[] ulongBuffer = new byte[8];

            //for (int idx = 0; idx < dataCount; idx++)
            //{
            //    rand.NextBytes(ulongBuffer);
            //    data[idx, 0] = BitConverter.ToUInt64(ulongBuffer, startIndex: 0);
            //    rand.NextBytes(ulongBuffer);
            //    data[idx, 1] = BitConverter.ToUInt64(ulongBuffer, startIndex: 0);
            //}

            //OPRFKey oprfKey = new OPRFKey();
            //APSIServer server = new APSIServer(QueryType.UpTo300, oprfKey);

            ////OPRFSender.ComputeHashes(data, oprfKey);
            //server.SetData(data);
            //Thread[] threads = new Thread[5];

            //for (int threadIdx = 0; threadIdx < threads.Length; threadIdx++)
            //{
            //    threads[threadIdx] = new Thread(() =>
            //    {
            //        int startMatch = rand.Next(minValue: 0, maxValue: (int)itemCount - matchItems - 1);
            //        int dataMatchStart = rand.Next(minValue: 0, maxValue: data.Length - matchItems - 1);
            //        ulong[] items = new ulong[itemCount * 2];

            //        for (int idx = 0; idx < (int)itemCount; idx++)
            //        {
            //            if (idx < startMatch || idx >= (startMatch + matchItems))
            //            {
            //                // Should not match
            //                rand.NextBytes(ulongBuffer);
            //                items[idx * 2] = BitConverter.ToUInt64(ulongBuffer, startIndex: 0);
            //                rand.NextBytes(ulongBuffer);
            //                items[idx * 2 + 1] = BitConverter.ToUInt64(ulongBuffer, startIndex: 0);
            //            }
            //            else
            //            {
            //                // Match
            //                items[idx * 2] = data[idx, 0];
            //                items[idx * 2 + 1] = data[idx, 1];
            //            }
            //        }

            //        byte[] prequeryItems = new byte[APSIClient.GetPreQuerySize(itemCount)];
            //        APSIClient client = new APSIClient();
            //        client.ReceiverPreQuery(itemCount, items, (ulong)prequeryItems.Length, prequeryItems);

            //        byte[] prequeryResult = OPRFSender.PreQuery(prequeryItems, oprfKey);

            //        client.DecodePreQuery((ulong)prequeryResult.Length, prequeryResult, itemCount, items);

            //        ulong querySize = 0;
            //        byte[] encryptedQuery = null;
            //        client.ReceiverQuery(itemCount, items, ref querySize, ref encryptedQuery);

            //        byte[] queryResult = server.Query(encryptedQuery);
            //        byte[] intersection = new byte[itemCount];

            //        client.DecryptResult((ulong)queryResult.Length, queryResult, itemCount, items, (ulong)intersection.Length, intersection);
            //        bool failed = false;

            //        for (int i = 0; i < intersection.Length; i++)
            //        {
            //            byte expected = i < startMatch || i >= startMatch ? (byte)0 : (byte)1;
            //            if (expected != intersection[i])
            //            {
            //                failedCount++;
            //                failed = true;
            //                break;
            //            }
            //        }

            //        if (failed)
            //        {
            //            Console.WriteLine($"Thread {Thread.CurrentThread.ManagedThreadId} failed!");
            //        }
            //        else
            //        {
            //            Console.WriteLine($"Thread {Thread.CurrentThread.ManagedThreadId} succeeded!");
            //        }

            //        client.Dispose();
            //    });

            //    threads[threadIdx].Start();
            //}

            //for (int threadIdx = 0; threadIdx < threads.Length; threadIdx++)
            //{
            //    threads[threadIdx].Join();
            //}

            //server.Dispose();
        }

        public static void MultipleQuerySingleServerTest()
        {
            //int iterations;
            //int failedCount = 0;
            //DateTime start = DateTime.Now;
            //ulong itemCount = 9; // Number of items in query
            //int matchItems = 4;  // Number of items that should match

            //long receiverPreQueryAccumulated = 0;
            //long receiverQueryAccumulated = 0;
            //long senderPreQueryAccumulated = 0;
            //long senderQueryAccumulated = 0;
            //long senderSetDataAccumulated = 0;

            //Random rand = new Random();
            //ulong[,] data = new ulong[65536, 2];
            //int dataCount = data.GetLength(dimension: 0);
            //byte[] ulongBuffer = new byte[8];

            //for (int idx = 0; idx < dataCount; idx++)
            //{
            //    rand.NextBytes(ulongBuffer);
            //    data[idx, 0] = BitConverter.ToUInt64(ulongBuffer, startIndex: 0);
            //    rand.NextBytes(ulongBuffer);
            //    data[idx, 1] = BitConverter.ToUInt64(ulongBuffer, startIndex: 0);
            //}

            //OPRFKey oprfKey = new OPRFKey();
            //APSIServer server = new APSIServer(QueryType.UpTo300, oprfKey);

            //Stopwatch senderSetDataElapsed = Stopwatch.StartNew();
            ////OPRFSender.ComputeHashes(data, oprfKey);
            //server.SetData(data);
            //senderSetDataElapsed.Stop();
            //senderSetDataAccumulated += senderSetDataElapsed.ElapsedMilliseconds;

            //for (iterations = 0; iterations < 50; iterations++)
            //{
            //    Console.WriteLine($"Iteration: {iterations}");

            //    ulong[] items = new ulong[itemCount * 2];
            //    int itemsIdx = 0;

            //    for (int idx = 0; idx < dataCount; idx++)
            //    {
            //        if (itemsIdx >= matchItems)
            //            break;

            //        if (rand.Next() > (int.MaxValue * 0.8))
            //        {
            //            items[itemsIdx * 2] = data[idx, 0];
            //            items[itemsIdx * 2 + 1] = data[idx, 1];
            //            itemsIdx++;
            //        }
            //    }

            //    // Max index of matching items
            //    int matchingIdx = itemsIdx;
            //    Console.WriteLine($"Matching idx: {matchingIdx}");

            //    // Fill rest of items with random values, which should not match
            //    for (; itemsIdx < (items.Length / 2); itemsIdx++)
            //    {
            //        rand.NextBytes(ulongBuffer);
            //        items[itemsIdx * 2] = BitConverter.ToUInt64(ulongBuffer, startIndex: 0);
            //        rand.NextBytes(ulongBuffer);
            //        items[itemsIdx * 2 + 1] = BitConverter.ToUInt64(ulongBuffer, startIndex: 0);
            //    }

            //    Stopwatch receiverPreQueryElapsed = Stopwatch.StartNew();
            //    byte[] prequeryItems = new byte[APSIClient.GetPreQuerySize(itemCount)];
            //    APSIClient client = new APSIClient();
            //    client.ReceiverPreQuery(itemCount, items, (ulong)prequeryItems.Length, prequeryItems);
            //    receiverPreQueryElapsed.Stop();
            //    receiverPreQueryAccumulated += receiverPreQueryElapsed.ElapsedMilliseconds;

            //    Stopwatch senderPreQueryElapsed = Stopwatch.StartNew();
            //    byte[] prequeryResult = OPRFSender.PreQuery(prequeryItems, oprfKey);
            //    senderPreQueryElapsed.Stop();
            //    senderPreQueryAccumulated += senderPreQueryElapsed.ElapsedMilliseconds;

            //    client.DecodePreQuery((ulong)prequeryResult.Length, prequeryResult, itemCount, items);

            //    ulong querySize = 0;
            //    byte[] encryptedQuery = null;
            //    Stopwatch receiverQueryElapsed = Stopwatch.StartNew();
            //    client.ReceiverQuery(itemCount, items, ref querySize, ref encryptedQuery);
            //    receiverQueryElapsed.Stop();
            //    receiverQueryAccumulated += receiverQueryElapsed.ElapsedMilliseconds;
            //    Console.WriteLine($"Resulting query buffer: {querySize}, {receiverQueryElapsed.ElapsedMilliseconds}ms");

            //    Stopwatch senderQueryElapsed = Stopwatch.StartNew();
            //    byte[] queryResult = server.Query(encryptedQuery);
            //    senderQueryElapsed.Stop();
            //    senderQueryAccumulated += senderQueryElapsed.ElapsedMilliseconds;
            //    Console.WriteLine($"Query result size is: {queryResult.Length}, {senderQueryElapsed.ElapsedMilliseconds}ms");
            //    byte[] intersection = new byte[itemCount];

            //    client.DecryptResult((ulong)queryResult.Length, queryResult, itemCount, items, (ulong)intersection.Length, intersection);

            //    for (int i = 0; i < intersection.Length; i++)
            //    {
            //        byte expected = i < matchingIdx ? (byte)1 : (byte)0;
            //        if (expected != intersection[i])
            //        {
            //            Console.WriteLine($"Iteration {iterations} failed!");
            //            failedCount++;
            //            break;
            //        }
            //        //Assert.Equal(expected, intersection[i], iterations);
            //    }

            //    client.Dispose();
            //}

            //server.Dispose();

            //DateTime end = DateTime.Now;

            //Console.WriteLine($"Ran for {iterations} iterations");
            //Console.WriteLine($"Failed {failedCount} times");
            //double iterationsD = iterations;
            //Console.WriteLine($"Receiver prequery avg: {receiverPreQueryAccumulated / iterationsD}ms");
            //Console.WriteLine($"Receiver query avg: {receiverQueryAccumulated / iterationsD}ms");
            //Console.WriteLine($"Sender prequery avg: {senderPreQueryAccumulated / iterationsD}ms");
            //Console.WriteLine($"Sender setdata: {senderSetDataAccumulated}ms");
            //Console.WriteLine($"Sender query avg: {senderQueryAccumulated / iterationsD}ms");

            //TimeSpan timeSpan = end - start;
            //Console.WriteLine($"Elapsed: {timeSpan}");
        }

        public static void SaveLoadTest()
        {
        //    ulong itemCount = 300;
        //    int matchItems = 150;
        //    Random rand = new Random();
        //    ulong[,] data = new ulong[300000, 2];
        //    int dataCount = data.GetLength(dimension: 0);
        //    byte[] ulongBuffer = new byte[8];

        //    ulong[] items = new ulong[itemCount * 2];
        //    int itemsIdx = 0;

        //    for (int idx = 0; idx < dataCount; idx++)
        //    {
        //        rand.NextBytes(ulongBuffer);
        //        data[idx, 0] = BitConverter.ToUInt64(ulongBuffer, startIndex: 0);
        //        rand.NextBytes(ulongBuffer);
        //        data[idx, 1] = BitConverter.ToUInt64(ulongBuffer, startIndex: 0);

        //        // Randomly determine if at most 70 values are going to be included
        //        if (itemsIdx < matchItems)
        //        {
        //            if (rand.Next() > (int.MaxValue * 0.8))
        //            {
        //                items[itemsIdx * 2] = data[idx, 0];
        //                items[itemsIdx * 2 + 1] = data[idx, 1];
        //                itemsIdx++;
        //            }
        //        }
        //    }

        //    // Max index of matching items
        //    int matchingIdx = itemsIdx;
        //    Console.WriteLine($"Matching idx: {matchingIdx}");

        //    // Fill rest of items with random values, which should not match
        //    for (; itemsIdx < (items.Length / 2); itemsIdx++)
        //    {
        //        rand.NextBytes(ulongBuffer);
        //        items[itemsIdx * 2] = BitConverter.ToUInt64(ulongBuffer, startIndex: 0);
        //        rand.NextBytes(ulongBuffer);
        //        items[itemsIdx * 2 + 1] = BitConverter.ToUInt64(ulongBuffer, startIndex: 0);
        //    }

        //    // Copy items for posterity
        //    ulong[] originalItems = new ulong[items.Length];
        //    Array.Copy(items, originalItems, items.Length);

        //    OPRFKey oprfKey = new OPRFKey();
        //    using APSIServer server = new APSIServer(QueryType.UpTo300, oprfKey);

        //    server.SetData(data);

        //    using MemoryStream ms = new MemoryStream();
        //    server.SaveDB(ms);

        //    byte[] prequeryItems = new byte[APSIClient.GetPreQuerySize(itemCount)];
        //    using APSIClient client = new APSIClient();
        //    client.ReceiverPreQuery(itemCount, items, (ulong)prequeryItems.Length, prequeryItems);

        //    byte[] prequeryResult = OPRFSender.PreQuery(prequeryItems, oprfKey);

        //    client.DecodePreQuery((ulong)prequeryResult.Length, prequeryResult, itemCount, items);

        //    ulong querySize = 0;
        //    byte[] encryptedQuery = null;
        //    client.ReceiverQuery(itemCount, items, ref querySize, ref encryptedQuery);

        //    byte[] queryResult = server.Query(encryptedQuery);
        //    byte[] intersection = new byte[itemCount];

        //    client.DecryptResult((ulong)queryResult.Length, queryResult, itemCount, items, (ulong)intersection.Length, intersection);

        //    bool failed = false;
        //    for (int i = 0; i < intersection.Length; i++)
        //    {
        //        byte expected = i < matchingIdx ? (byte)1 : (byte)0;
        //        if (expected != intersection[i])
        //        {
        //            failed = true;
        //            break;
        //        }
        //    }

        //    if (failed)
        //        Console.WriteLine("Failed!");
        //    else
        //        Console.WriteLine("Succeeded!");


        //    ms.Seek(offset: 0, SeekOrigin.Begin);
        //    byte[] bytes = ms.ToArray();
        //    APSIServer server2 = APSIServer.LoadDB(bytes);

        //    server.Dispose();
        //    ms.Dispose();
        //    bytes = null;

        //    // Copy items back
        //    Array.Copy(originalItems, items, originalItems.Length);

        //    prequeryItems = new byte[APSIClient.GetPreQuerySize(itemCount)];
        //    using APSIClient client2 = new APSIClient();
        //    client2.ReceiverPreQuery(itemCount, items, (ulong)prequeryItems.Length, prequeryItems);

        //    prequeryResult = OPRFSender.PreQuery(prequeryItems, oprfKey);

        //    client2.DecodePreQuery((ulong)prequeryResult.Length, prequeryResult, itemCount, items);

        //    querySize = 0;
        //    encryptedQuery = null;
        //    client2.ReceiverQuery(itemCount, items, ref querySize, ref encryptedQuery);

        //    queryResult = server2.Query(encryptedQuery);
        //    intersection = new byte[itemCount];

        //    client2.DecryptResult((ulong)queryResult.Length, queryResult, itemCount, items, (ulong)intersection.Length, intersection);

        //    failed = false;
        //    for (int i = 0; i < intersection.Length; i++)
        //    {
        //        byte expected = i < matchingIdx ? (byte)1 : (byte)0;
        //        if (expected != intersection[i])
        //        {
        //            failed = true;
        //            break;
        //        }
        //    }

        //    if (failed)
        //        Console.WriteLine("Failed!");
        //    else
        //        Console.WriteLine("Succeeded!");

        //    Console.WriteLine("Did this work?");
        //}

        //public static (ulong[], string, int) SaveToFile(string fileToSave)
        //{
        //    ulong itemCount = 300;
        //    int matchItems = 150;
        //    Random rand = new Random();
        //    ulong[,] data = new ulong[300000, 2];
        //    int dataCount = data.GetLength(dimension: 0);
        //    byte[] ulongBuffer = new byte[8];

        //    ulong[] items = new ulong[itemCount * 2];
        //    int itemsIdx = 0;

        //    for (int idx = 0; idx < dataCount; idx++)
        //    {
        //        rand.NextBytes(ulongBuffer);
        //        data[idx, 0] = BitConverter.ToUInt64(ulongBuffer, startIndex: 0);
        //        rand.NextBytes(ulongBuffer);
        //        data[idx, 1] = BitConverter.ToUInt64(ulongBuffer, startIndex: 0);

        //        // Randomly determine if at most 70 values are going to be included
        //        if (itemsIdx < matchItems)
        //        {
        //            if (rand.Next() > (int.MaxValue * 0.8))
        //            {
        //                items[itemsIdx * 2] = data[idx, 0];
        //                items[itemsIdx * 2 + 1] = data[idx, 1];
        //                itemsIdx++;
        //            }
        //        }
        //    }

        //    // Max index of matching items
        //    int matchingIdx = itemsIdx;
        //    Console.WriteLine($"Matching idx: {matchingIdx}");

        //    // Fill rest of items with random values, which should not match
        //    for (; itemsIdx < (items.Length / 2); itemsIdx++)
        //    {
        //        rand.NextBytes(ulongBuffer);
        //        items[itemsIdx * 2] = BitConverter.ToUInt64(ulongBuffer, startIndex: 0);
        //        rand.NextBytes(ulongBuffer);
        //        items[itemsIdx * 2 + 1] = BitConverter.ToUInt64(ulongBuffer, startIndex: 0);
        //    }

        //    OPRFKey oprfKey = new OPRFKey();
        //    using APSIServer server = new APSIServer(QueryType.UpTo300, oprfKey);

        //    server.SetData(data);
        //    server.SaveDB(fileToSave);

        //    return (items, oprfKey.ToString(), matchingIdx);
        //}

        //public static void QueryFromFile(string fileToLoad, string oprfKeyStr, int matchingIdx, ulong[] items)
        //{
        //    ulong itemCount = (ulong)items.Length / 2;
        //    OPRFKey oprfKey = new OPRFKey(oprfKeyStr);

        //    using APSIServer server = APSIServer.LoadDB(fileToLoad);

        //    byte[] prequeryItems = new byte[APSIClient.GetPreQuerySize(itemCount)];
        //    using APSIClient client = new APSIClient();
        //    client.ReceiverPreQuery(itemCount, items, (ulong)prequeryItems.Length, prequeryItems);

        //    byte[] prequeryResult = OPRFSender.PreQuery(prequeryItems, oprfKey);

        //    client.DecodePreQuery((ulong)prequeryResult.Length, prequeryResult, itemCount, items);

        //    ulong querySize = 0;
        //    byte[] encryptedQuery = null;
        //    client.ReceiverQuery(itemCount, items, ref querySize, ref encryptedQuery);

        //    byte[] queryResult = server.Query(encryptedQuery);
        //    byte[] intersection = new byte[itemCount];

        //    client.DecryptResult((ulong)queryResult.Length, queryResult, itemCount, items, (ulong)intersection.Length, intersection);

        //    bool failed = false;
        //    for (int i = 0; i < intersection.Length; i++)
        //    {
        //        byte expected = i < matchingIdx ? (byte)1 : (byte)0;
        //        if (expected != intersection[i])
        //        {
        //            failed = true;
        //            break;
        //        }
        //    }

        //    if (failed)
        //        Console.WriteLine("Failed!");
        //    else
        //        Console.WriteLine("Succeeded!");
        }
    }
}
