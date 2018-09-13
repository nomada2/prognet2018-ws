using System;
using System.Collections.Concurrent;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ParallelPatterns.Fsharp;

namespace ParallelPatterns
{
    public class PipelineFilter<TInput, TOutput>
    {
        Func<TInput, TOutput> m_function = null;
        public BlockingCollection<TInput> m_inputData = null;
        public BlockingCollection<TOutput> m_outputData = null;
        Action<TInput> m_outputAction = null;
        public string Name { get; private set; }

        public PipelineFilter(BlockingCollection<TInput> input, Func<TInput, TOutput> processor, string name)
        {
            m_inputData = input;
            // no buffer
            m_outputData = new BlockingCollection<TOutput>();

            m_function = processor;
            Name = name;
        }

        //used for final endpoint 
        public PipelineFilter(BlockingCollection<TInput> input, Action<TInput> renderer, string name)
        {
            m_inputData = input;
            m_outputAction = renderer;
            Name = name;
        }

        public void Run()
        {
            Console.WriteLine("filter {0} is running", this.Name);
            while (!m_inputData.IsCompleted)
            {
                TInput receivedItem;
                if (m_inputData.TryTake(out receivedItem, 50))
                {
                    if (m_outputData != null)
                    {
                        TOutput outputItem = m_function(receivedItem);
                        m_outputData.TryAdd(outputItem);
                        Console.WriteLine("{0} sent {1} to next filter", this.Name, outputItem);
                    }
                    else
                    {
                        m_outputAction(receivedItem);
                    }
                }
                else
                    Console.WriteLine("Could not get data from previous filter");
            }
            if (m_outputData != null)
            {
                m_outputData.CompleteAdding();
            }
        }

        public static void TestFilteringPipeline()
        {
            //Generate the source data.
            var source = new BlockingCollection<int>();

            Parallel.For(0, 100, (data) =>
            {
                if(source.TryAdd(data))
                    Console.WriteLine("added {0} to source data", data);
            });

            source.CompleteAdding();

            // calculate the square 
            var calculateFilter = new PipelineFilter<int, int>
            (
                source,
                (n) => n * n,
                "calculateFilter"
             );

            //Convert ints to strings
            var convertFilter = new PipelineFilter<int, string>
            (
                calculateFilter.m_outputData,
                (s) => String.Format("{0}", s),
                "convertFilter"
             );

            // Displays the results
            var displayFilter = new PipelineFilter<string, string>
            (
                convertFilter.m_outputData,
                (s) => Console.WriteLine("The final result is {0}", s),
                "displayFilter");

            // Start the pipeline
            try
            {
                Parallel.Invoke(
                             () => calculateFilter.Run(),
                             () => convertFilter.Run(),
                             () => displayFilter.Run()
                         );
            }
            catch (AggregateException aggregate)
            {
                foreach (var exception in aggregate.InnerExceptions)
                    Console.WriteLine(exception.Message + exception.StackTrace);
            }

            Console.ReadLine();
        }
    }
}