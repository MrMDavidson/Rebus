﻿using System;
#if NET45
using System.Runtime.Serialization;
#elif NETSTANDARD2_0
using System.Runtime.Serialization;
#endif

namespace Rebus.Exceptions
{
    /// <summary>
    /// Fail-fast exception bypasses the retry logic and goes to the error queue directly
    /// </summary>
#if NET45
    [Serializable]
#elif NETSTANDARD2_0
    [Serializable]
#endif
    public class FailFastException : Exception, IFailFastException
    {
        /// <summary>
        /// Constructs the exception with the given message
        /// </summary>
        public FailFastException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Constructs the exception with the given message and inner exception
        /// </summary>
        public FailFastException(Exception innerException, string message)
            : base(message, innerException)
        {
        }

#if NET45
        /// <summary>
        /// Happy cross-domain serialization!
        /// </summary>
        public FailFastException(SerializationInfo info, StreamingContext context)
            :base(info, context)
        {
        }
#elif NETSTANDARD2_0
        /// <summary>
        /// Happy cross-domain serialization!
        /// </summary>
        public FailFastException(SerializationInfo info, StreamingContext context)
            :base(info, context)
        {
        }
#endif
    }
}