using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Toggl.Ultrawave.Network;
using Xunit;
using Toggl.Ultrawave.Tests.Integration.BaseTests;

namespace Toggl.Ultrawave.Tests.Integration
{
    public sealed class StatusApiTests
    {
        public sealed class TheGetMethod : EndpointTestBase
        {
            [Fact, LogTestInfo]
            public async Task ShouldSucceedWithoutCredentials()
            {
                var togglClient = TogglApiWith(Credentials.None);
                Exception caughtException = null;

                await togglClient.Status.IsAvailable().Catch((Exception e) =>
                {
                    caughtException = e;
                    return Observable.Return(Unit.Default);
                });

                caughtException.Should().BeNull();
            }
        }
    }
}
