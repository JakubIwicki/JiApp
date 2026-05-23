using System;
using FluentAssertions;
using JiApp.Common.Models;
using Xunit;

namespace JiApp.Tests.Common;

public class EntityTests
{
    [Fact]
    public void EventLog_Create_SetsTimestampTypeUserIdAndMessage()
    {
        var log = EventLog.Create(EventLogType.Exception, 42, "something went wrong");

        log.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        log.Type.Should().Be(EventLogType.Exception);
        log.UserId.Should().Be(42);
        log.Message.Should().Be("something went wrong");
        log.Exception.Should().BeNull();
    }

    [Fact]
    public void EventLog_Create_WithNullUserId_StoresNull()
    {
        var log = EventLog.Create(EventLogType.Insider, null, "info message");

        log.UserId.Should().BeNull();
        log.Type.Should().Be(EventLogType.Insider);
    }

    [Fact]
    public void EventLog_Create_DefaultTypeIsException()
    {
        var log = EventLog.Create(EventLogType.ThirdPartyService, 1, "third party call");

        log.Type.Should().Be(EventLogType.ThirdPartyService);
    }
}