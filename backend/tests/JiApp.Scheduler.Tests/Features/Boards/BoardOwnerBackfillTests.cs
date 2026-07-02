using FluentAssertions;
using Microsoft.Data.Sqlite;

namespace JiApp.Scheduler.Tests.Features.Boards;

public sealed class BoardOwnerBackfillTests
{
	[Fact]
	public void Backfill_SetsOwnerUserIdToFirstMember()
	{
		using var connection = new SqliteConnection("Data Source=:memory:");
		connection.Open();

		using var cmd = connection.CreateCommand();
		cmd.CommandText = """
			CREATE TABLE Boards (
				Id INTEGER PRIMARY KEY,
				MemberUserIds TEXT,
				OwnerUserId INTEGER NOT NULL DEFAULT 0
			);
			INSERT INTO Boards (Id, MemberUserIds, OwnerUserId) VALUES (1, '[5,7]', 0);
			INSERT INTO Boards (Id, MemberUserIds, OwnerUserId) VALUES (2, '[42]', 0);
			INSERT INTO Boards (Id, MemberUserIds, OwnerUserId) VALUES (3, '[]', 0);
			""";
		cmd.ExecuteNonQuery();

		cmd.CommandText =
			"UPDATE Boards SET OwnerUserId = json_extract(MemberUserIds, '$[0]') " +
			"WHERE MemberUserIds IS NOT NULL AND json_array_length(MemberUserIds) > 0;";
		cmd.ExecuteNonQuery();

		cmd.CommandText = "SELECT Id, OwnerUserId FROM Boards ORDER BY Id;";
		using var reader = cmd.ExecuteReader();

		reader.Read();
		reader.GetInt64(0).Should().Be(1);
		reader.GetInt64(1).Should().Be(5);

		reader.Read();
		reader.GetInt64(0).Should().Be(2);
		reader.GetInt64(1).Should().Be(42);

		reader.Read();
		reader.GetInt64(0).Should().Be(3);
		reader.GetInt64(1).Should().Be(0);
	}
}
