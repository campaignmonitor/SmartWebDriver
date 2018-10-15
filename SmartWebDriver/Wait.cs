using System;
using System.Diagnostics;
using System.Threading;
using SmartWebDriver.Extensions;

namespace SmartWebDriver
{
    /// <summary>
    /// This class is designed to wait for custom functions to be executed
    /// and have return successfully.
    /// Example usage:
    /// Wait.UpTo(10.Seconds()).For(() => VerifyReportStats());
    /// </summary>
    public class Wait
    {
        private readonly TimeSpan _timeout;
        private TimeSpan _interval;

        public Wait()
        {
            _interval = 1.Seconds();
        }

        public Wait(TimeSpan timeout)
        {
            _timeout = timeout;
            _interval = 1.Seconds();
        }

        public static Wait UpTo(TimeSpan timeout)
        {
            return new Wait(timeout);
        }

        /// <summary>
        /// Extend or shorten the interval between executing the Condition being checked
        /// </summary>
        /// <param name="interval"></param>
        /// <returns></returns>
        public Wait CheckingEvery(TimeSpan interval)
        {
            _interval = interval;
            return this;
        }

        public void For(Func<bool> boolFunction)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            // until the timeout expires, execute the boolFunction until it returns true
            while (stopwatch.Elapsed < _timeout)
            {
                if (boolFunction())
                {
                    stopwatch.Stop();
                    return;
                }
                Thread.Sleep(_interval);
            }
            stopwatch.Stop();
            throw new Exception("Waited " + _timeout.TotalSeconds + " seconds for the boolean function to be true, but it was still false");
        }

        /// <summary>
        /// Repeatedly execute the provided function until it is successful or
        /// we reach the timeout
        /// </summary>
        /// <param name="testResponseFunction"></param>
        public void For(Func<TestResponse> testResponseFunction)
        {
            var stopWatch = new Stopwatch();
            var response = new TestResponse(false, "");
            try
            {
                stopWatch.Start();

                while (stopWatch.Elapsed < _timeout)
                {
                    response = testResponseFunction.Invoke();
                    if (response.PassFailResult)
                    {
                        break;
                    }
                    Thread.Sleep(_interval);
                }
            }
            finally
            {
                stopWatch.Stop();
            }

            if (!response.PassFailResult)
            {
                var timeElapsed = $"{ Math.Round(stopWatch.Elapsed.TotalMinutes, 0)}m, {stopWatch.Elapsed.Seconds}s";
                throw new Exception(
                    $"Waited {timeElapsed} for the condition to be met, but it wasn't.\nError thrown: {response.GetMessages}");
            }
        }

        /// <summary>
        /// Repeatedly confirm the provided function is successful until we reach
        /// the timeout
        /// </summary>
        /// <param name="testResponseFunction"></param>
        public void WhileEnsuring(Func<TestResponse> testResponseFunction)
        {
            var stopWatch = new Stopwatch();
            TestResponse response = new TestResponse(false, "");
            try
            {
                stopWatch.Start();

                while (stopWatch.Elapsed < _timeout)
                {
                    response = testResponseFunction.Invoke();
                    if (!response.PassFailResult)
                    {
                        break;
                    }
                    Thread.Sleep(_interval);
                }
            }
            finally
            {
                stopWatch.Stop();
            }

            if (!response.PassFailResult)
            {
                var timeElapsed = $"{ Math.Round(stopWatch.Elapsed.TotalMinutes, 0)}m, {stopWatch.Elapsed.Seconds}s";
                throw new Exception(
                    $"I expected the condition to be met, and hold true, for {_timeout.TotalSeconds} seconds, but it failed after: {timeElapsed}.\nI got this error {response.GetMessages}");
            }
        }
    }
}
