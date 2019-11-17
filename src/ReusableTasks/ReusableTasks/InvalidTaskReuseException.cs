using System;
using System.Collections.Generic;
using System.Text;

namespace ReusableTasks
{
    /// <summary>
    /// This exception is thrown whenever a <see cref="ReusableTask"/> has been mis-used. This can happen
    /// if the same instance is awaited twice.
    /// </summary>
    public class InvalidTaskReuseException : Exception
    {
        /// <summary>
        /// Creates a new instance of <see cref="InvalidTaskReuseException"/> with the given message
        /// </summary>
        public InvalidTaskReuseException ()
        {
        }

        /// <summary>
        /// Creates a new instance of <see cref="InvalidTaskReuseException"/> with the given message
        /// </summary>
        /// <param name="message">The message describing the failure</param>
        public InvalidTaskReuseException (string message)
            : base (message)
        {

        }
    }
}
