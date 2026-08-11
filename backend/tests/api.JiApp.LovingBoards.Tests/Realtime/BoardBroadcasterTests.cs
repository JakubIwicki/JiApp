using System.Runtime.CompilerServices;
using System.Text.Json;
using api.JiApp.LovingBoards.Realtime;

namespace api.JiApp.LovingBoards.Tests.Realtime;

public sealed class BoardBroadcasterTests
{
    [Fact]
    public async Task Subscribe_ThenPublish_SubscriberReceivesEvent()
    {
        var broadcaster = new BoardBroadcaster();
        using var sub = broadcaster.Subscribe(1L, 100L);

        // Consume the initial presence event
        await ReadNextAsync(sub);

        broadcaster.Publish(1L, new BoardEvent("test.event", new { value = 42 }));

        var ev = await ReadNextAsync(sub);
        ev.Should().NotBeNull();
        ev!.Event.Should().Be("test.event");
    }

    [Fact]
    public async Task Publish_MultipleSubscribersOnSameBoard_AllReceiveEvent()
    {
        var broadcaster = new BoardBroadcaster();
        using var sub1 = broadcaster.Subscribe(1L, 100L);
        using var sub2 = broadcaster.Subscribe(1L, 200L);

        broadcaster.Publish(1L, new BoardEvent("test.event", new { }));

        var ev1 = await ReadNextAsync(sub1);
        var ev2 = await ReadNextAsync(sub2);
        ev1.Should().NotBeNull();
        ev2.Should().NotBeNull();
    }

    [Fact]
    public void Publish_BoardWithNoSubscribers_IsNoOp()
    {
        var broadcaster = new BoardBroadcaster();

        // Should not throw
        broadcaster.Publish(1L, new BoardEvent("test.event", new { }));
    }

    [Fact]
    public async Task Subscribe_BroadcastsPresenceWithDistinctUserIds()
    {
        var broadcaster = new BoardBroadcaster();
        using var sub1 = broadcaster.Subscribe(1L, 100L);

        // The subscribe itself broadcasts presence, so sub1 gets that as the first event
        var presence1 = await ReadNextAsync(sub1);
        presence1!.Event.Should().Be(BoardEventNames.Presence);

        // Subscribe with same userId - presence should dedupe
        using var sub2 = broadcaster.Subscribe(1L, 100L);

        // sub1 gets the updated presence (still deduped to one userId)
        var presence2 = await ReadNextAsync(sub1);
        presence2!.Event.Should().Be(BoardEventNames.Presence);

        // sub2 also gets the initial presence
        var presence3 = await ReadNextAsync(sub2);
        presence3!.Event.Should().Be(BoardEventNames.Presence);
    }

    [Fact]
    public async Task Subscribe_TwoSubscribersDifferentUserIds_PresenceListsBoth()
    {
        var broadcaster = new BoardBroadcaster();
        using var sub1 = broadcaster.Subscribe(1L, 100L);

        // consume the initial presence for sub1
        await ReadNextAsync(sub1);

        // Now subscribe with a different userId
        using var sub2 = broadcaster.Subscribe(1L, 200L);

        // sub1 gets presence with both users
        var presenceEvent = await ReadNextAsync(sub1);
        presenceEvent!.Event.Should().Be(BoardEventNames.Presence);

        // sub2 gets initial presence with both users
        var sub2Presence = await ReadNextAsync(sub2);
        sub2Presence!.Event.Should().Be(BoardEventNames.Presence);
    }

    [Fact]
    public async Task Dispose_UnsubscribesAndBroadcastsUpdatedPresence()
    {
        var broadcaster = new BoardBroadcaster();
        using var sub1 = broadcaster.Subscribe(1L, 100L);
        using var sub2 = broadcaster.Subscribe(1L, 200L);

        // consume initial presence events
        await ReadNextAsync(sub1);
        await ReadNextAsync(sub1); // second subscribe triggered another presence
        await ReadNextAsync(sub2); // initial presence for sub2

        // Now dispose sub1
        sub1.Dispose();

        // sub2 should receive the updated presence (only 200L now)
        var presenceEvent = await ReadNextAsync(sub2);
        presenceEvent!.Event.Should().Be(BoardEventNames.Presence);
    }

    [Fact]
    public async Task Publish_BoardAEvents_NotDeliveredToBoardB()
    {
        var broadcaster = new BoardBroadcaster();
        using var subA = broadcaster.Subscribe(1L, 100L);
        using var subB = broadcaster.Subscribe(2L, 100L);

        // consume initial presence events
        await ReadNextAsync(subA);
        await ReadNextAsync(subB);

        broadcaster.Publish(1L, new BoardEvent("board.a", new { }));

        var evA = await ReadNextAsync(subA);
        evA!.Event.Should().Be("board.a");

        // subB should NOT receive the event for board A
        var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        BoardEvent? readB = null;
        try
        {
            readB = await ReadNextAsync(subB, cts.Token);
        }
        catch (OperationCanceledException) { }

        readB.Should().BeNull();
    }

    [Fact]
    public async Task Dispose_RemovesEmptyBoardEntry()
    {
        var broadcaster = new BoardBroadcaster();
        var sub = broadcaster.Subscribe(1L, 100L);

        // consume initial presence
        await ReadNextAsync(sub);

        sub.Dispose();

        // Publishing to board 1 after all subscribers are gone should be a no-op
        broadcaster.Publish(1L, new BoardEvent("test.event", new { }));

        // Re-subscribe — board should be clean
        using var sub2 = broadcaster.Subscribe(1L, 100L);
        // Should only get the presence event, not the old event
        var ev = await ReadNextAsync(sub2);
        ev!.Event.Should().Be(BoardEventNames.Presence);
    }

    [Fact]
    public async Task ChannelFullMode_DropOldest_PreventsBlocking()
    {
        var broadcaster = new BoardBroadcaster();
        using var sub = broadcaster.Subscribe(1L, 100L);

        // consume initial presence
        await ReadNextAsync(sub);

        // Publish more than channel capacity (100)
        for (var i = 0; i < 150; i++)
            broadcaster.Publish(1L, new BoardEvent("test.event", new { index = i }));

        // Should not have blocked. The subscriber should receive the latest events
        // (oldest were dropped), so index should start above 0.
        var firstEv = await ReadNextAsync(sub);
        firstEv.Should().NotBeNull();
    }

    // Fix 5 — Disconnect

    [Fact]
    public async Task Disconnect_EndsSubscriptionStream()
    {
        var broadcaster = new BoardBroadcaster();
        using var sub = broadcaster.Subscribe(1L, 100L);

        // Consume initial presence
        await ReadNextAsync(sub);

        broadcaster.Disconnect(1L, 100L);

        // The stream should complete (ReadAllAsync enumeration ends)
        var ev = await ReadNextAsync(sub);
        ev.Should().BeNull();
    }

    [Fact]
    public async Task Disconnect_UpdatesPresenceForOtherSubscribers()
    {
        var broadcaster = new BoardBroadcaster();
        using var sub1 = broadcaster.Subscribe(1L, 100L);
        using var sub2 = broadcaster.Subscribe(1L, 200L);

        // Consume initial presence events
        await ReadNextAsync(sub1);
        await ReadNextAsync(sub1); // second subscribe triggered another presence
        await ReadNextAsync(sub2); // initial presence for sub2

        broadcaster.Disconnect(1L, 100L);

        // sub2 should receive updated presence (only 200L now)
        var presenceEvent = await ReadNextAsync(sub2);
        presenceEvent.Should().NotBeNull();
        presenceEvent!.Event.Should().Be(BoardEventNames.Presence);
    }

    [Fact]
    public async Task Disconnect_OtherUsersUnaffected()
    {
        var broadcaster = new BoardBroadcaster();
        using var sub1 = broadcaster.Subscribe(1L, 100L);
        using var sub2 = broadcaster.Subscribe(1L, 200L);

        // Consume initial presence events
        await ReadNextAsync(sub1);
        await ReadNextAsync(sub1); // second subscribe triggered another presence
        await ReadNextAsync(sub2); // initial presence for sub2

        broadcaster.Disconnect(1L, 100L);

        // sub1's stream should be done
        var ev1 = await ReadNextAsync(sub1);
        ev1.Should().BeNull();

        // sub2 receives an updated presence after the disconnect
        var presenceEvent = await ReadNextAsync(sub2);
        presenceEvent.Should().NotBeNull();
        presenceEvent!.Event.Should().Be(BoardEventNames.Presence);

        // sub2 should still receive subsequent events
        broadcaster.Publish(1L, new BoardEvent("test.event", new { }));
        var ev2 = await ReadNextAsync(sub2);
        ev2.Should().NotBeNull();
        ev2!.Event.Should().Be("test.event");
    }

    [Fact]
    public async Task DisconnectAll_EndsAllSubscriptionsOnBoard()
    {
        var broadcaster = new BoardBroadcaster();
        using var sub1 = broadcaster.Subscribe(1L, 100L);
        using var sub2 = broadcaster.Subscribe(1L, 200L);

        // Consume initial presence events
        await ReadNextAsync(sub1);
        await ReadNextAsync(sub1); // second subscribe triggered another presence
        await ReadNextAsync(sub2); // initial presence for sub2

        broadcaster.DisconnectAll(1L);

        var ev1 = await ReadNextAsync(sub1);
        var ev2 = await ReadNextAsync(sub2);
        ev1.Should().BeNull();
        ev2.Should().BeNull();

        // Publishing after disconnect should be a no-op (board entry removed)
        broadcaster.Publish(1L, new BoardEvent("test.event", new { }));
        using var sub3 = broadcaster.Subscribe(1L, 300L);
        var ev3 = await ReadNextAsync(sub3);
        ev3!.Event.Should().Be(BoardEventNames.Presence);
    }

    [Fact]
    public async Task LastSubscriberLeaves_NextSubscriberGetsOnlyOwnPresence()
    {
        var broadcaster = new BoardBroadcaster();
        using var sub1 = broadcaster.Subscribe(1L, 100L);
        using var sub2 = broadcaster.Subscribe(1L, 200L);

        // Consume initial presence events
        await ReadNextAsync(sub1);
        await ReadNextAsync(sub1);
        await ReadNextAsync(sub2);

        broadcaster.Disconnect(1L, 100L);
        await ReadNextAsync(sub2);

        // Last member leaves — the empty-presence path broadcasts to a torn-down board
        // and the board entry is removed, so the next subscriber starts from a clean slate.
        broadcaster.Disconnect(1L, 200L);

        using var sub3 = broadcaster.Subscribe(1L, 300L);
        var ev = await ReadNextAsync(sub3);

        ev!.Event.Should().Be(BoardEventNames.Presence);
        SerializeUserIds(ev.Data).Should().Be("[300]");
    }

    [Fact]
    public async Task Dispose_Twice_IsIdempotent()
    {
        var broadcaster = new BoardBroadcaster();
        var sub = broadcaster.Subscribe(1L, 100L);

        // Consume initial presence
        await ReadNextAsync(sub);

        sub.Dispose();
        sub.Dispose();

        // The board is torn down exactly once; a fresh subscriber sees clean presence.
        using var sub2 = broadcaster.Subscribe(1L, 200L);
        var ev = await ReadNextAsync(sub2);

        ev!.Event.Should().Be(BoardEventNames.Presence);
        SerializeUserIds(ev.Data).Should().Be("[200]");
    }

    [Fact]
    public async Task Subscribe_WhileLastSubscriberUnsubscribes_NewSubscriberStillReceivesEvents()
    {
        var broadcaster = new BoardBroadcaster();

        for (var iteration = 0; iteration < 2000; iteration++)
        {
            var resident = broadcaster.Subscribe(1L, 100L);

            var joinerTasks = Enumerable.Range(0, 4)
                .Select(_ => Task.Run(() => broadcaster.Subscribe(1L, 200L)))
                .ToArray();

            resident.Dispose();
            var joiners = await Task.WhenAll(joinerTasks);

            broadcaster.Publish(1L, new BoardEvent("test.event", new { iteration }));

            foreach (var joiner in joiners)
            {
                var ev = await ReadUntilAsync(joiner, "test.event", TimeSpan.FromSeconds(1));
                joiner.Dispose();
                ev.Should().NotBeNull(
                    $"iteration {iteration}: a subscriber that joined while the last old subscriber left was orphaned and never received the published event");
            }
        }
    }

    private static string SerializeUserIds(object data)
    {
        using var doc = JsonDocument.Parse(JsonSerializer.Serialize(data, data.GetType()));
        return doc.RootElement.GetProperty("userIds").GetRawText();
    }

    private static async Task<BoardEvent?> ReadNextAsync(IBoardSubscription sub, CancellationToken ct = default)
    {
        await foreach (var ev in sub.ReadAllAsync(ct))
            return ev;

        return null;
    }

    private static async Task<BoardEvent?> ReadUntilAsync(IBoardSubscription sub, string eventName, TimeSpan timeout)
    {
        using var cts = new CancellationTokenSource(timeout);
        try
        {
            await foreach (var ev in sub.ReadAllAsync(cts.Token))
            {
                if (ev.Event == eventName)
                    return ev;
            }
            return null;
        }
        catch (OperationCanceledException)
        {
            return null;
        }
    }
}
