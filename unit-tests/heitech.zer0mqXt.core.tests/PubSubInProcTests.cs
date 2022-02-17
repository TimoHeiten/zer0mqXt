using System;
using System.Threading;
using System.Threading.Tasks;
using heitech.zer0mqXt.core.infrastructure;
using heitech.zer0mqXt.core.Main;
using heitech.zer0mqXt.core.PubSub;
using heitech.zer0mqXt.core.transport;
using Xunit;

namespace heitech.zer0mqXt.core.tests
{
    public class PubSubInProcTests : IDisposable
    {
        private readonly IPatternFactory _patterns;
        private readonly IPublisher _publisher;
        private readonly ISubscriber _subscriber;

        private const int SIG_WAIT = 1500;

        public PubSubInProcTests()
        {
            _patterns = Zer0Mq.Go()
                              .SilenceLogger()
                              .BuildWithInProc($"{Guid.NewGuid()}");

            _publisher = _patterns.CreatePublisher();
            _subscriber = _patterns.CreateSubscriber();
        }

        [Fact]
        public async Task SimplePubSub_InProc()
        {
            // Arrange
            Message incoming = null;
            var message = new Message { ThisIsAPublishedMessageText = "published a message", Array = new[] { 1, 2, 3, 4 } };
            var waitHandle = new ManualResetEvent(false);
            var xtResult = _subscriber.RegisterSubscriber<Message>(
                callback: m =>
                {
                    incoming = m;
                    waitHandle.Set();
                }, onError: null, topic: null, CancellationToken.None
            );
            Assert.True(xtResult.IsSuccess);

            // Act
            await _publisher.SendAsync<Message>(message);

            // Assert
            bool wasSignaled = waitHandle.WaitOne(SIG_WAIT);
            Assert.True(wasSignaled);
            Assert.NotNull(incoming);
            Assert.True(xtResult.IsSuccess);
            Assert.Equal(message.Array, incoming.Array);
            Assert.Equal(message.ThisIsAPublishedMessageText, incoming.ThisIsAPublishedMessageText);
        }

        // !! todo
        // [Theory] 
        // // [InlineData("topic", true)]
        // [InlineData("topic/subtopic", true)] does not work
        // // [InlineData("topical", true)] does not work
        // // [InlineData(null, true)] does not work

        // // [InlineData("TOPIC", false)] works
        // // [InlineData("top", false)] does not work
        // // [InlineData("Other", false)] works

        // // see here for how topics work:
        // // https://netmq.readthedocs.io/en/latest/pub-sub/
        // public async Task Publish_by_topic_alone_works(string msgTopic, bool wasCaptured)
        // {
        //     var pattern = Zer0Mq.Go().BuildWithInProc("publish-on-topic");
        //     if (msgTopic == null)
        //         msgTopic = typeof(Message).FullName;

        //     const string SENDER_TOPIC = "topic";
        //     // Arrange
        //     var waitHandle = new ManualResetEvent(false);
        //     using var sut = SUT<IPublisher, ISubscriber>.PubSub(pattern);
        //     sut.Client.RegisterSubscriber<Message>(
        //         m => waitHandle.Set(),
        //         onError: () => { /* intentionally left blank  */},
        //         topic: msgTopic
        //     );

        //     // Act
        //     await sut.Server.SendAsync(new Message(), SENDER_TOPIC);

        //     // Assert
        //     bool wassignaled = waitHandle.WaitOne(1500);
        //     Assert.Equal(wasCaptured, wassignaled);
        // }

        [Fact]
        public void Subscriber_can_still_be_bound_without_pairing_up_first()
        {
            var config = new ConfigurationTestData().GetSocketConfigInProc;
            using var subscriber =_patterns.CreateSubscriber();

            // Act
            XtResult result = subscriber.RegisterSubscriber<Message>(async m => await Task.CompletedTask, onError: null, topic: null, CancellationToken.None);

            // Assert
            // no assert as no exception is expected otherwiese the test would fail at the call to the function pub()
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task Subscriber_Cancellation_works()
        {
            // Arrange
            bool wasReceived = false;
            var tokenSource = new CancellationTokenSource();
            var token = tokenSource.Token;
            // cancel before it is registered
            tokenSource.Cancel();
            _subscriber.RegisterSubscriber<Message>(msg => wasReceived = true, cancellationToken: token);

            // Act
            await _publisher.SendAsync<Message>(new Message());

            // Assert
            Assert.False(wasReceived);
        }

        // #region TCP Tests do not work in automated testing for some reason or another...

        [Fact]
        public async Task PubSub_with_two_Subscribers_on_different_types_works_with_correct_topic_typeframe()
        {
            // Arrange
            Message incoming = null;
            OtherMessage otherIncoming = null;
            var waitHandle1 = new ManualResetEvent(false);
            var waitHandle2 = new ManualResetEvent(false);

            using var sndSub = _patterns.CreateSubscriber();
            Action a = () => _subscriber.RegisterSubscriber<Message>(callback: m => { incoming = m; waitHandle1.Set(); });
            // sanityCheck
            var ex = Record.Exception(a);
            Assert.Null(ex);
            Action a2 = () => sndSub.RegisterSubscriber<OtherMessage>(callback: m => { otherIncoming = m; waitHandle2.Set(); });
            // sanityCheck
            var ex2 = Record.Exception(a2);
            Assert.Null(ex2);

            // Act
            await _publisher.SendAsync<Message>(new Message());
            await _publisher.SendAsync<OtherMessage>(new OtherMessage { Content = "abcaffeschnee" });

            // Assert
            bool wasSignaled1 = waitHandle1.WaitOne(SIG_WAIT);
            bool wasSignaled2 = waitHandle2.WaitOne(SIG_WAIT);
            Assert.True(wasSignaled1);
            Assert.True(wasSignaled2);

            Assert.NotNull(incoming);
            Assert.NotNull(otherIncoming);
            Assert.Equal("abcaffeschnee", otherIncoming.Content);
        }

        [Fact()]
        public async Task TwoSubscribersWithOneSocket_Both_Get_A_Message_On_Same_Topic()
        {
            // Arrange
            Message incoming = null;
            Message otherIncoming = null;
            var cntdwn = new CountdownEvent(2);
            using var sndSub = _patterns.CreateSubscriber();

            Action a = () => _subscriber.RegisterSubscriber<Message>(
                callback: m =>
                {
                    incoming = m;
                    cntdwn.Signal();
                }
            );
            // sanityCheck
            var ex = Record.Exception(a);
            Assert.Null(ex);
            Action a2 = () => sndSub.RegisterSubscriber<Message>(callback: m =>
            {
                otherIncoming = m;
                cntdwn.Signal();
            });
            // sanityCheck
            var ex2 = Record.Exception(a2);
            Assert.Null(ex2);

            // Act
            await _publisher.SendAsync<Message>(new ()
                { 
                    ThisIsAPublishedMessageText = "abcaffeschnee" 
                }
            );

            // Assert
            bool wasSignaled = cntdwn.Wait(TimeSpan.FromMilliseconds(1500));
            Assert.True(wasSignaled);
            Assert.NotNull(incoming);
            Assert.NotNull(otherIncoming);
            Assert.Equal("abcaffeschnee", incoming.ThisIsAPublishedMessageText);
            Assert.Equal("abcaffeschnee", otherIncoming.ThisIsAPublishedMessageText);
        }

        [Fact]
        public async Task Multiple_Subscriber_on_Single_Publisher()
        {
            // Arrange
            // 3 subs
            using var sub1 = _patterns.CreateSubscriber();
            using var sub2 = _patterns.CreateSubscriber();
            using var sub3 = _patterns.CreateSubscriber();
            void subAction(ManualResetEvent handle, Message m) { handle.Set(); }
            var waitHandle1 = new ManualResetEvent(false);
            var waitHandle2 = new ManualResetEvent(false);
            var waitHandle3 = new ManualResetEvent(false);
            sub1.RegisterSubscriber<Message>(m => subAction(waitHandle1, m));
            sub2.RegisterSubscriber<Message>(m => subAction(waitHandle2, m));
            sub3.RegisterSubscriber<Message>(m => subAction(waitHandle3, m));

            // Act
            await _publisher.SendAsync(new Message());

            // Assert
            bool signaled1 = waitHandle1.WaitOne(SIG_WAIT);
            bool signaled2 = waitHandle2.WaitOne(SIG_WAIT);
            bool signaled3 = waitHandle3.WaitOne(SIG_WAIT);

            Assert.All(new[] { signaled1, signaled2, signaled3 }, x => Assert.True(x));
        }

        [Fact]
        public void Registering_Publisher_on_same_socket_twice_throws()
        {
            // Arrange
            // Act
            Action a = () => _patterns.CreatePublisher();

            // Assert
            Assert.ThrowsAny<ZeroMqXtSocketException>(a);
        }

        [Fact(Skip = "flaky test on github actions")]
        public async Task Subscriber_with_topic_but_wrong_type_calls_OnError_Callback()
        {
            // Arrange
            var handle = new ManualResetEvent(false);
            bool wasError = false;
            _subscriber.RegisterSubscriber<SndMessage>(
                m => { }, () => { handle.Set(); wasError = true; },
                "this-topic"
            );

            // Act
            await _publisher.SendAsync(new Message(), "this-topic");

            // Assert
            bool wasSignaled = handle.WaitOne(SIG_WAIT);
            Assert.True(wasSignaled);
            Assert.True(wasError); // todo not working on github actions for whatever reason
        }

        public void Dispose()
        {
            _publisher.Dispose();
            _subscriber.Dispose();
        }

        public class Message
        {
            public string ThisIsAPublishedMessageText { get; set; }
            public int[] Array { get; set; } = System.Array.Empty<int>();
        }

        public class OtherMessage
        {
            public string Content { get; set; }
        }

        public class SndMessage
        {
            public int Counter { get; set; }
        }
    }
}