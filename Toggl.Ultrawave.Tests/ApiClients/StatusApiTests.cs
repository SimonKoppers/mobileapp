using System;
using System.Net;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Toggl.Ultrawave.ApiClients;
using Toggl.Ultrawave.Helpers;
using Toggl.Ultrawave.Network;
using Xunit;
using static System.Net.HttpStatusCode;

namespace Toggl.Ultrawave.Tests.Clients
{
    public sealed class StatusClientTests
    {
        public sealed class TheGetMethod
        {
            private readonly StatusApi statusApi;
            private readonly IApiClient apiClient = Substitute.For<IApiClient>();

            public TheGetMethod()
            {
                var endpoints = new StatusEndpoints(ApiUrls.ForEnvironment(ApiEnvironment.Staging));
                statusApi = new StatusApi(endpoints, apiClient);
            }

            [Fact]
            public async Task DoesNotHideThrownExceptions()
            {
                bool caughtException = false;
                #pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
                apiClient
                    .Send(Arg.Any<IRequest>())
                    .Returns(async x => throw new WebException());
                #pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously

                statusApi.Get()
                    .Catch((Exception exception) =>
                    {
                        caughtException = exception is WebException;
                        return Observable.Return(false);
                    }).Wait();

                caughtException.Should().BeTrue();
            }

            [Fact]
            public async Task ReturnsASingleValueForWorkingApiCalls()
            {
                apiClient
                    .Send(Arg.Any<IRequest>())
                    .Returns(x => new Response("OK", true, "text/plain", OK));

                var status = await statusApi.Get().SingleAsync();
                status.Should().BeTrue();
            }

            [Fact]
            public async Task ReturnsASingleValueForFailingApiCalls()
            {
                apiClient
                    .Send(Arg.Any<IRequest>())
                    .Returns(x => new Response("PANIC", false, "text/plain", InternalServerError));

                var status = await statusApi.Get().SingleAsync();
                status.Should().BeFalse();
            }
        }
    }
}
