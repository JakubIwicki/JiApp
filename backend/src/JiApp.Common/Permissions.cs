namespace JiApp.Common;

public static class Permissions
{
	public const string SchedulerAccess = "scheduler.access";
	public const string YtDownloaderAccess = "ytdownloader.access";
	public const string LovingBoardsAccess = "lovingboards.access";
	public const string UsersManage = "users.manage";
	public const string RolesManage = "roles.manage";

	public static readonly string[] ModuleAccess = [SchedulerAccess, YtDownloaderAccess, LovingBoardsAccess];
	public static readonly string[] All = [SchedulerAccess, YtDownloaderAccess, LovingBoardsAccess, UsersManage, RolesManage];
}
