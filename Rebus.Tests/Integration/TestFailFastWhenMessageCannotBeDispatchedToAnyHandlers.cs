﻿using System;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Logging;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Utilities;
using Rebus.Transport.InMem;
// ReSharper disable ArgumentsStyleLiteral

namespace Rebus.Tests.Integration
{
    [TestFixture]
    public class TestFailFastWhenMessageCannotBeDispatchedToAnyHandlers : FixtureBase
    {
        BuiltinHandlerActivator _activator;
        ListLoggerFactory _loggerFactory;

        protected override void SetUp()
        {
            _activator = new BuiltinHandlerActivator();

            Using(_activator);

            _loggerFactory = new ListLoggerFactory(outputToConsole: true);

            Configure.With(_activator)
                .Logging(l => l.Use(_loggerFactory))
                .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "only-try-once"))
                .Start();
        }

        [Test]
        public async Task OnlyDeliversMessageOnceWhenThereIsNoHandlerForIt()
        {
            _activator.Bus.Advanced.SyncBus.SendLocal("hej med dig din gamle hængerøv");

            await Task.Delay(TimeSpan.FromSeconds(2));

            var numberOfWarnings = _loggerFactory.Count(l => l.Level == LogLevel.Warn);
            var numberOfErrors = _loggerFactory.Count(l => l.Level == LogLevel.Error);

            Assert.That(numberOfWarnings, Is.EqualTo(1), "Expected onle one single WARNING, because the delivery should not be retried");
            Assert.That(numberOfErrors, Is.EqualTo(1), "Expected an error message saying that the message is moved to the error queue");
        }
    }
}