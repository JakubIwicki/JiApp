using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JiApp.Identity.Persistence.Migrations
{
	/// <inheritdoc />
	public partial class AddRbacRemoveModuleGrants : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			// 1. Insert "User" role into AspNetRoles (idempotent — INSERT OR IGNORE on unique NormalizedName index)
			migrationBuilder.Sql(
				"INSERT OR IGNORE INTO AspNetRoles (Name, NormalizedName, ConcurrencyStamp) " +
				"VALUES ('User', 'USER', 'a1b2c3d4-e5f6-7890-abcd-ef1234567890');");

			// 2. Assign every distinct UserId from UserModuleGrants to the "User" role
			migrationBuilder.Sql(
				"INSERT OR IGNORE INTO AspNetUserRoles (UserId, RoleId) " +
				"SELECT DISTINCT g.UserId, r.Id " +
				"FROM UserModuleGrants g, AspNetRoles r " +
				"WHERE r.NormalizedName = 'USER';");

			// 3. Drop the now-redundant UserModuleGrants table
			migrationBuilder.Sql("DROP TABLE IF EXISTS UserModuleGrants;");
		}

		/// <inheritdoc />
		/// <remarks>
		/// Down() recreates the UserModuleGrants table but does NOT restore data or
		/// remove the seeded role/assignments — those were migrated forward and are
		/// treated as part of the RBAC upgrade.
		/// </remarks>
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.CreateTable(
				name: "UserModuleGrants",
				columns: table => new
				{
					Id = table.Column<long>(type: "INTEGER", nullable: false)
						.Annotation("Sqlite:Autoincrement", true),
					UserId = table.Column<long>(type: "INTEGER", nullable: false),
					ModuleName = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
					GrantedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_UserModuleGrants", x => x.Id);
					table.ForeignKey(
						name: "FK_UserModuleGrants_AspNetUsers_UserId",
						column: x => x.UserId,
						principalTable: "AspNetUsers",
						principalColumn: "Id",
						onDelete: ReferentialAction.Cascade);
				});

			migrationBuilder.CreateIndex(
				name: "IX_UserModuleGrants_UserId_ModuleName",
				table: "UserModuleGrants",
				columns: ["UserId", "ModuleName"],
				unique: true);
		}
	}
}
