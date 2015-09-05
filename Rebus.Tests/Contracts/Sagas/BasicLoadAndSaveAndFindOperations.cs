using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Sagas;
using Rebus.Tests.Extensions;

namespace Rebus.Tests.Contracts.Sagas
{
    public abstract class BasicLoadAndSaveAndFindOperations<TFactory> : FixtureBase where TFactory : ISagaStorageFactory, new()
    {
        readonly IEnumerable<ISagaCorrelationProperty> _noCorrelationProperties = Enumerable.Empty<ISagaCorrelationProperty>();
        
        ISagaStorage _sagaStorage;
        TFactory _factory;

        protected override void SetUp()
        {
            _factory = new TFactory();
            _sagaStorage = _factory.GetSagaStorage();
        }

        protected override void TearDown()
        {
            CleanUpDisposables();

            _factory.CleanUp();
        }

        [Test]
        public async Task ThrowsIfIdHasNotBeenSet()
        {
            var sagaDataWithDefaultId = new AnotherSagaData { Id = Guid.Empty };

            Assert.Throws<InvalidOperationException>(async () => await _sagaStorage.Insert(sagaDataWithDefaultId, _noCorrelationProperties));
        }

        [Test]
        public async Task GetsNullWhenNoInstanceMatches()
        {
            var data = await _sagaStorage.Find(typeof(TestSagaData), "CorrelationId", "whatever");

            Assert.That(data, Is.Null);
        }

        [Test]
        public async Task GetsNullWhenPropertyDoesNotExist()
        {
            var data = await _sagaStorage.Find(typeof(TestSagaData), "NonExistingCorrelationId", "whatever");

            Assert.That(data, Is.Null);
        }

        [Test]
        public async Task GetsNullWhenValueDoesNotExist()
        {
            await _sagaStorage.Insert(new TestSagaData { Id = Guid.NewGuid(), CorrelationId = "existing" }, _noCorrelationProperties);

            var data = await _sagaStorage.Find(typeof(TestSagaData), "CorrelationId", "non-existing");

            Assert.That(data, Is.Null);
        }

        [Test]
        public async Task GetsTheInstanceWhenCorrelationPropertyMatches()
        {
            var sagaId = Guid.NewGuid();

            await _sagaStorage.Insert(new TestSagaData { Id = sagaId, CorrelationId = "existing" }, 
                CorrelationPropertiesFor<TestSagaData>(d => d.CorrelationId));

            var result = await _sagaStorage.Find(typeof(TestSagaData), "CorrelationId", "existing");

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Data, Is.Not.Null);
            Assert.That(result.Data.Id, Is.EqualTo(sagaId));
        }

        [Test]
        public async Task GetsNullWhenTheTypeDoesNotMatch()
        {
            var sagaId = Guid.NewGuid();

            await _sagaStorage.Insert(new TestSagaData { Id = sagaId, CorrelationId = "existing" },
                CorrelationPropertiesFor<TestSagaData>(d => d.CorrelationId));

            var result = await _sagaStorage.Find(typeof(AnotherSagaData), "CorrelationId", "existing");

            Assert.That(result, Is.Null);
        }

        [Test]
        public async Task GetsTheInstanceWhenIdPropertyMatches()
        {
            var sagaId = Guid.NewGuid();

            await _sagaStorage.Insert(new TestSagaData { Id = sagaId, CorrelationId = "existing" },
                CorrelationPropertiesFor<TestSagaData>(d => d.CorrelationId));

            var result = await _sagaStorage.Find(typeof(TestSagaData), "Id", sagaId);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Data, Is.Not.Null);
            Assert.That(result.Data.Id, Is.EqualTo(sagaId));
        }

        [Test]
        public async Task NewlyInsertedSagaDataIsRevisionZero()
        {
            var sagaId = Guid.NewGuid();

            await _sagaStorage.Insert(new TestSagaData
            {
                Id = sagaId,
                Data = "yes, den kender jeg"
            }, _noCorrelationProperties);

            var result = await _sagaStorage.Find(typeof(TestSagaData), "Id", sagaId);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Data, Is.Not.Null);
            Assert.That(result.Data.Revision, Is.EqualTo(0));
        }

        [Test]
        public async Task RevisionIsIncrementedOnEachUpdate()
        {
            var sagaId = Guid.NewGuid();

            var initialTransientInstance = new TestSagaData { Id = sagaId, Data = "yes, den kender jeg" };

            Assert.That(initialTransientInstance.Revision, Is.EqualTo(0));

            await _sagaStorage.Insert(initialTransientInstance, _noCorrelationProperties);
            var loadedSagaData0 = await _sagaStorage.Find(typeof(TestSagaData), "Id", sagaId);
            
            Assert.That(loadedSagaData0.Data.Revision, Is.EqualTo(0));
            Assert.That(initialTransientInstance.Revision, Is.EqualTo(0));

            await _sagaStorage.Update(loadedSagaData0.Data, _noCorrelationProperties);
            var loadedSagaData1 = await _sagaStorage.Find(typeof(TestSagaData), "Id", sagaId);
            
            Assert.That(loadedSagaData0.Data.Revision, Is.EqualTo(1));
            Assert.That(loadedSagaData1.Data.Revision, Is.EqualTo(1));

            await _sagaStorage.Update(loadedSagaData1.Data, _noCorrelationProperties);
            var loadedSagaData2 = await _sagaStorage.Find(typeof(TestSagaData), "Id", sagaId);
            
            Assert.That(loadedSagaData1.Data.Revision, Is.EqualTo(2));
            Assert.That(loadedSagaData2.Data.Revision, Is.EqualTo(2));
        }

        [Test]
        public async Task CanDeleteSagaData()
        {
            var sagaId = Guid.NewGuid();

            await _sagaStorage.Insert(new TestSagaData
            {
                Id = sagaId,
                Data = "yes, den kender jeg"
            }, _noCorrelationProperties);

            var findResult = await _sagaStorage.Find(typeof(TestSagaData), "Id", sagaId);

            Assert.That(findResult, Is.Not.Null);

            await _sagaStorage.Delete(findResult.Data);

            var loadedSagaDataAfterDelete = await _sagaStorage.Find(typeof(TestSagaData), "Id", sagaId);

            Assert.That(loadedSagaDataAfterDelete, Is.Null);
        }

        [Test]
        public async Task NizzleName()
        {
            var id = Guid.NewGuid();
            
            var guidCorrelationValue = Guid.NewGuid();
            var stringCorrelationValue = "hej";
            var dateTimeCorrelationValue = new DateTime(1979, 3, 19);
            var dateTimeOffsetCorrelationValue = new DateTimeOffset(1979, 3, 19, 20, 0, 0, TimeSpan.FromHours(2));
            var decimalCorrelationValue = 23M;
            var intCorrelationValue = 8;

            var data = new SagaDataWithVariousCorrelationProperties
            {
                Id = id,
                CorrelateByString = stringCorrelationValue,
                CorrelateByDateTime = dateTimeCorrelationValue,
                CorrelateByDateTimeOffset = dateTimeOffsetCorrelationValue,
                CorrelateByDecimal = decimalCorrelationValue,
                CorrelateByGuid = guidCorrelationValue,
                CorrelateByInt = intCorrelationValue
            };

            var correlationProperties = new[]
            {
                GetCorrelationProperty(d => d.CorrelateByString), 
                GetCorrelationProperty(d => d.CorrelateByInt), 
                //GetCorrelationProperty(d => d.CorrelateByDecimal), 
                //GetCorrelationProperty(d => d.CorrelateByDateTime), 
                //GetCorrelationProperty(d => d.CorrelateByDateTimeOffset), 
                GetCorrelationProperty(d => d.CorrelateByGuid), 
            };

            await _sagaStorage.Insert(data,correlationProperties);

            var dataByString = await Find(stringCorrelationValue, d => d.CorrelateByString);
            var dataByInt = await Find(intCorrelationValue, d => d.CorrelateByInt);
            var dataByGuid = await Find(guidCorrelationValue, d => d.CorrelateByGuid);

            Assert.That(dataByString.Data.Id, Is.EqualTo(id));
            Assert.That(dataByInt.Data.Id, Is.EqualTo(id));
            Assert.That(dataByGuid.Data.Id, Is.EqualTo(id));
        }

        async Task<SagaStorageFindResult> Find(object value, Expression<Func<SagaDataWithVariousCorrelationProperties, object>> expression)
        {
            return await _sagaStorage.Find(typeof (SagaDataWithVariousCorrelationProperties), Reflect.Path(expression), value);
        }

        CorrelationProperty GetCorrelationProperty(Expression<Func<SagaDataWithVariousCorrelationProperties, object>> expression)
        {
            return new CorrelationProperty(Reflect.Path(expression), typeof (SagaDataWithVariousCorrelationProperties));
        }

        class SagaDataWithVariousCorrelationProperties : ISagaData
        {
            public Guid Id { get; set; }
            public int Revision { get; set; }

            public string CorrelateByString { get; set; }
            public Guid CorrelateByGuid { get; set; }
            public int CorrelateByInt { get; set; }
            public decimal CorrelateByDecimal { get; set; }
            public DateTime CorrelateByDateTime { get; set; }
            public DateTimeOffset CorrelateByDateTimeOffset { get; set; }
        }

        class TestSagaData : ISagaData
        {
            public Guid Id { get; set; }
            public int Revision { get; set; }
            public string CorrelationId { get; set; }
            public string Data { get; set; }
        }

        class AnotherSagaData : ISagaData
        {
            public Guid Id { get; set; }
            public int Revision { get; set; }
        }

        IEnumerable<ISagaCorrelationProperty> CorrelationPropertiesFor<TSagaData>(params Expression<Func<TSagaData, object>>[] properties)
        {
            return properties
                .Select(Reflect.Path)
                .Select(propertyName => new CorrelationProperty(propertyName, typeof (TSagaData)));
        }

        class CorrelationProperty : ISagaCorrelationProperty
        {
            static readonly Type[] AllowedCorrelationPropertyTypes = {
                typeof (string),
                typeof (int),
                typeof (Guid)
            };

            public CorrelationProperty(string propertyName, Type sagaDataType)
            {
                PropertyName = propertyName;
                SagaDataType = sagaDataType;

                Validate();
            }

            void Validate()
            {
                var propertyType = SagaDataType.GetProperty(PropertyName).PropertyType;

                if (AllowedCorrelationPropertyTypes.Contains(propertyType)) return;

                throw new ArgumentException(string.Format("Cannot correlate with the '{0}' property on the '{1}' saga data type - only allowed types are: {2}",
                    PropertyName, SagaDataType.Name, string.Join(", ", AllowedCorrelationPropertyTypes.Select(t => t.Name))));
            }

            public string PropertyName { get; private set; }
            public Type SagaDataType { get; private set; }
        }
    }
}