﻿using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Handlers;
using Rebus.Transport;

namespace Rebus.Tests.Activation
{
    [TestFixture]
    public class TestBuiltinHandlerActivator : FixtureBase
    {
        BuiltinHandlerActivator _activator;

        protected override void SetUp()
        {
            _activator = new BuiltinHandlerActivator();
        }

        protected override void TearDown()
        {
            AmbientTransactionContext.Current = null;
        }

        [Test]
        public void CanGetHandlerWithoutArguments()
        {
            _activator.Register(() => new SomeHandler());

            var handlers = _activator.GetHandlers("hej med dig", new DefaultTransactionContext()).Result;

            Assert.That(handlers.Single(), Is.TypeOf<SomeHandler>());
        }

        [Test]
        public void CanGetHandlerWithMessageContextArgument()
        {
            _activator.Register(context => new SomeHandler());

            using (var transactionContext = new DefaultTransactionContext())
            {
                AmbientTransactionContext.Current = transactionContext;

                var handlers = _activator.GetHandlers("hej med dig", transactionContext).Result;

                Assert.That(handlers.Single(), Is.TypeOf<SomeHandler>());
            }
        }

        [Test]
        public void CanGetHandlerWithBusAndMessageContextArgument()
        {
            _activator.Register((bus, context) => new SomeHandler());

            using (var transactionContext = new DefaultTransactionContext())
            {
                AmbientTransactionContext.Current = transactionContext;

                var handlers = _activator.GetHandlers("hej med dig", transactionContext).Result;

                Assert.That(handlers.Single(), Is.TypeOf<SomeHandler>());
            }
        }

        class SomeHandler : IHandleMessages<string>
        {
            public Task Handle(string message)
            {
                throw new System.NotImplementedException();
            }
        }
    }
}