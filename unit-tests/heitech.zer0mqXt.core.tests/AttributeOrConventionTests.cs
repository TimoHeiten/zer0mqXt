using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using heitech.zer0mqXt.core.Main;
using heitech.zer0mqXt.core.Transport;
using Xunit;

namespace heitech.zer0mqXt.core.tests
{
    public class AttributeOrConventionTests
    {
        private readonly IPatternFactory _patterns;
        public AttributeOrConventionTests()
            => _patterns = Zer0Mq.Go().SilenceLogger().BuildWithInProc($"{Guid.NewGuid()}");

        [Fact]
        public async Task RqRep_With_MessageId_Works()
        {
            // Arrange
            using var responder = _patterns.CreateResponder();
            var waitHandle = new ManualResetEvent(false);
            responder.Respond<Request, Response>(x =>
            {
                waitHandle.Set();
                return new Response();
            }, onError: null);
            using var client = _patterns.CreateClient();

            // Act
            var result = await client.RequestAsync<RequestV2, Response>(new RequestV2());

            // Assert
            bool wasSignaled = waitHandle.WaitOne(1000);
            wasSignaled.Should().BeTrue();
            result.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public async Task RqRep_With_SasmeTypeName_Only_Works_when_both_have_the_Attribute()
        {
            // Arrange
            using var responder = _patterns.CreateResponder();
            // never be reached since parsing should fail
            var waitHandle = new ManualResetEvent(false);
            responder.Respond<Request, Response>(x => new Response(), onError: () => { waitHandle.Set(); return new Response(); });
            using var client = _patterns.CreateClient();

            // Act
            // does have the correct name, but not the correct attribute which has precedence 
            var result = await client.RequestAsync<ClientServerInProcTests.Request, Response>(new ClientServerInProcTests.Request());

            // Assert
            bool wasSignaled = waitHandle.WaitOne(1000);
            wasSignaled.Should().BeTrue();
            result.IsSuccess.Should().BeFalse();
        }

        [Fact]
        public async Task RqRep_Message_with_different_ids_Returns_Failure()
        {
            // Arrange
            using var responder = _patterns.CreateResponder();
            bool calledOnError = false;
            var waitHandle = new ManualResetEvent(false);
            // never be reached since parsing should fail
            responder.Respond<Request, Response>(x => new Response(), onError: () => { calledOnError = true; waitHandle.Set(); return new Response(); });
            using var client = _patterns.CreateClient();

            // Act
            var result = await client.RequestAsync<RequestV3, Response>(new RequestV3());

            // Assert
            bool wasSignaled = waitHandle.WaitOne(1000);
            wasSignaled.Should().BeTrue();
            calledOnError.Should().BeTrue();
            result.IsSuccess.Should().BeFalse();
        }

        [Fact]
        public async Task PubSub_With_MessageId_Works()
        {
            // Arrange
            using var subscriber = _patterns.CreateSubscriber();
            using var publisher = _patterns.CreatePublisher();
            var waitHandle = new ManualResetEvent(false);
            subscriber.RegisterSubscriber<Request>((r) => waitHandle.Set());

            // Act
            var result = await publisher.SendAsync<RequestV2>(new RequestV2());

            // Assert
            bool wasSignaled = waitHandle.WaitOne(1000);
            result.IsSuccess.Should().BeTrue();
            wasSignaled.Should().BeTrue();
        }

        [Fact]
        public async Task PubSub_With_Incorrect_MessageId_Fails()
        {
            // Arrange
            using var subscriber = _patterns.CreateSubscriber();
            using var publisher = _patterns.CreatePublisher();
            var waitHandle = new ManualResetEvent(false);
            subscriber.RegisterSubscriber<Request>((r) => { }, onError: () => waitHandle.Set());

            // Act
            var result = await publisher.SendAsync<RequestV3>(new RequestV3());

            // Assert
            bool wasSignaled = waitHandle.WaitOne(1000);
            result.IsSuccess.Should().BeTrue();
            wasSignaled.Should().BeTrue();
        }

        [Zer0mqMessage("Request-Id")]
        private class Request { public int RequestNumber { get; set; } }

        private class Response { public string ResponseText { get; set; } }

        [Zer0mqMessage("Request-Id")]
        private class RequestV2 { public int RequestNumber { get; set; } }

        [Zer0mqMessage("Request-Id-V3")]
        private class RequestV3 { public int RequestNumber { get; set; } }
    }
}
