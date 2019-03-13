using System;
using System.Collections.Generic;

namespace SmartWebDriver
{
    public class TestResponse
    {
        public bool PassFailResult { get; set; }
        public IList<string> Messages;

        public TestResponse() : this(true, null) { }
        /// <summary>
        /// For testing a single boolean result
        /// </summary>
        /// <param name="result"></param>
        /// <param name="message"></param>
        public TestResponse(bool result, string message)
        {
            PassFailResult = result;
            Messages = PassFailResult
                           ? new List<string>()
                           : new List<string> { message };
        }
        /// <summary>
        /// For testing two strings are equal
        /// </summary>
        /// <param name="expected"></param>
        /// <param name="actual"></param>
        /// <param name="message"></param>
        public TestResponse(string expected, string actual, string message)
        {
            // make sure neither input is null before comparing
            expected = expected ?? "";
            actual = actual ?? "";
            PassFailResult = expected.Trim() == actual.Trim();
            Messages = PassFailResult
                           ? new List<string>()
                           : new List<string> { "The two string arguments didn't match, expected: " + expected + ", got: " + actual, message };
        }
        /// <summary>
        /// For testing two ints are equal
        /// </summary>
        /// <param name="expected"></param>
        /// <param name="actual"></param>
        /// <param name="message"></param>
        public TestResponse(int expected, int actual, string message)
        {
            PassFailResult = expected == actual;
            Messages = PassFailResult
                           ? new List<string>()
                           : new List<string> { "The two int arguments didn't match, expected: " + expected + ", got: " + actual, message };
        }
        /// <summary>
        /// For testing two longs are equal for the result condition
        /// </summary>
        /// <param name="expected"></param>
        /// <param name="actual"></param>
        /// <param name="message"></param>
        public TestResponse(long expected, long actual, string message)
        {
            PassFailResult = expected == actual;
            Messages = PassFailResult
                           ? new List<string>()
                           : new List<string> { "The two long arguments didn't match, expected: " + expected + ", got: " + actual, message };
        }
        /// <summary>
        /// For testing two doubles are equal for the result condition
        /// </summary>
        /// <param name="expected"></param>
        /// <param name="actual"></param>
        /// <param name="message"></param>
        public TestResponse(double expected, double actual, string message)
        {
            PassFailResult = Math.Abs(expected - actual) < 0.001;
            Messages = PassFailResult
                           ? new List<string>()
                           : new List<string> { "The two double arguments didn't match, expected: " + expected + ", got: " + actual, message };
        }
        /// <summary>
        /// For testing two bools are equal for the result condition
        /// </summary>
        /// <param name="expected"></param>
        /// <param name="actual"></param>
        /// <param name="message"></param>
        public TestResponse(bool expected, bool actual, string message)
        {
            PassFailResult = expected == actual;
            Messages = PassFailResult
                           ? new List<string>()
                           : new List<string> { "The two bool arguments didn't match, expected: " + expected + ", got: " + actual, message };
        }

        public bool Add(TestResponse newResponse)
        {
            // compare new result with what we already have and update accordingly
            PassFailResult = PassFailResult && newResponse.PassFailResult;
            
            if (PassFailResult)
                return true;

            // if this is a failure, we need to add the corresponding failure message to the list
            if (!newResponse.PassFailResult)
            {
                foreach (var message in newResponse.Messages)
                {
                    Messages.Add(message);
                }
            }
            return false; // return the fail status, so we can use it to see if we should stop testing now
        }
        public bool Add(bool result, string message)
        {
            return Add(new TestResponse(result, message));
        }
        public bool Add(string expected, string actual, string message)
        {
            return Add(new TestResponse(expected, actual, message));
        }
        public bool Add(int expected, int actual, string message)
        {
            return Add(new TestResponse(expected, actual, message));
        }
        public bool Add(long expected, long actual, string message)
        {
            return Add(new TestResponse(expected, actual, message));
        }
        public bool Add(double expected, double actual, string message)
        {
            return Add(new TestResponse(expected, actual, message));
        }
        public bool Add(bool expected, bool actual, string message)
        {
            return Add(new TestResponse(expected, actual, message));
        }

        /// <summary>
        /// return the list of messages as a comma-separated string
        /// </summary>
        public string GetMessages => Messages == null ? "" : string.Join("\n", Messages);

        public void AssertIsTrue(string failureMessage)
        {
            if (!PassFailResult)
            {
                throw new Exception(failureMessage + ".\nExpected true, but got false.\nThe error messages returned are:\n" + GetMessages);
            }
        }

        public void AssertIsFalse(string failureMessage)
        {
            if (PassFailResult)
            {
                throw new Exception(failureMessage + ".\nExpected false, but got true.\nThe error messages returned are:\n" + GetMessages);
            }
        }
    }
}
