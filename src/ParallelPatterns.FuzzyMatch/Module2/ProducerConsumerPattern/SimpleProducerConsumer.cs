using System;
using System.Collections.Concurrent;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ParallelPatterns.Fsharp;

namespace ParallelPatterns
{
    public class SimpleProducerConsumer
    {
        public static void Start()
        {
            var bufferA = new BlockingCollection<int>(20);
            var bufferB = new BlockingCollection<int>(20);

            var createStage = Task.Factory.StartNew(() =>
            {
                CreateRange(bufferA);
            }, TaskCreationOptions.LongRunning);

            var squareStage = Task.Factory.StartNew(() =>
            {
                SquareTheRange(bufferA, bufferB);
            }, TaskCreationOptions.LongRunning);

            var displayStage = Task.Factory.StartNew(() =>
            {
                DisplayResults(bufferB);
            }, TaskCreationOptions.LongRunning);

            Task.WaitAll(createStage, squareStage, displayStage);

            Console.ReadLine();
        }

        static void CreateRange(BlockingCollection<int> result)
        {
            try
            {
                for (int i = 1; i < 20; i++)
                {
                    result.Add(i);
                    Console.WriteLine("Create Range {0}", i);
                }
            }
            finally
            {
                result.CompleteAdding();
            }
        }

        static void SquareTheRange(BlockingCollection<int> source, BlockingCollection<int> result)
        {
            try
            {
                foreach (var value in source.GetConsumingEnumerable())
                {
                    result.Add((int)(value * value));
                }
            }
            finally
            {
                result.CompleteAdding();
            }
        }

        static void DisplayResults(BlockingCollection<int> input)
        {
            foreach (var value in input.GetConsumingEnumerable())
            {
                Console.WriteLine("The result is {0}", value);
            }
        }
    }
}