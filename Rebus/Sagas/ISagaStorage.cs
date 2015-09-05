using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Rebus.Exceptions;

namespace Rebus.Sagas
{
    /// <summary>
    /// Details of the find operation
    /// </summary>
    public class SagaStorageFindResult
    {
        /// <summary>
        /// Indicates the saga data exists
        /// </summary>
        public bool Exists { get; set;}

        /// <summary>
        /// Indicates that not only does the saga exist but we were also able to lock it
        /// </summary>
        public bool Locked { get;set;}

        /// <summary>
        /// The actual data from the saga
        /// </summary>
        public ISagaData Data { get; set; }
    }

    /// <summary>
    /// Abstraction for a mechanism that is capable of storing saga state, retrieving it again by querying for value on the state
    /// </summary>
    public interface ISagaStorage
    {
        /// <summary>
        /// Indicates if this storage mechanism supports locking. If it does then <see cref="Find"/> must indicate that saga was successfully locked via <seealso cref="SagaStorageFindResult.Locked"/>. If it does not the then the message will be deferred
        /// </summary>
        bool SupportsLocking { get; }

        /// <summary>
        /// Finds an already-existing instance of the given saga data type that has a property with the given <paramref name="propertyName"/>
        /// whose value matches <paramref name="propertyValue"/>. Returns null if no such instance could be found
        /// </summary>
        Task<SagaStorageFindResult> Find(Type sagaDataType, string propertyName, object propertyValue);
        
        /// <summary>
        /// Inserts the given saga data as a new instance. Throws a <see cref="ConcurrencyException"/> if another saga data instance
        /// already exists with a correlation property that shares a value with this saga data.
        /// </summary>
        Task Insert(ISagaData sagaData, IEnumerable<ISagaCorrelationProperty> correlationProperties);
        
        /// <summary>
        /// Updates the already-existing instance of the given saga data, throwing a <see cref="ConcurrencyException"/> if another
        /// saga data instance exists with a correlation property that shares a value with this saga data, or if the saga data
        /// instance no longer exists.
        /// </summary>
        Task Update(ISagaData sagaData, IEnumerable<ISagaCorrelationProperty> correlationProperties);

        /// <summary>
        /// Deletes the saga data instance, throwing a <see cref="ConcurrencyException"/> if the instance no longer exists
        /// </summary>
        Task Delete(ISagaData sagaData);
    }
}