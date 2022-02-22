using System.Threading.Tasks;
using FluentAssertions;
using heitech.zer0mqXt.core.Main;
using Xunit;

namespace heitech.zer0mqXt.core.tests
{
    public class InfrastructureTests
    {
        [Fact]
        public async Task NewtonSoft_Serializer_works_with_ReqRep()
        {
            // Arrange
            var patterns = Zer0Mq.Go()
                                 .SilenceLogger()
                                 .UseNewtonsoftJson()
                                 .BuildWithInProc("newtonsoft-pipe");

            using var responder = patterns.CreateResponder();
            responder.Respond<Req, Rep>(x => new Rep());
            using var client = patterns.CreateClient();
            
            // Act
            var result = await client.RequestAsync<Req,Rep>(new Req());

            // Assert
            result.IsSuccess.Should().BeTrue();
        }

        private class Req { }
        private class Rep { }
    }
}