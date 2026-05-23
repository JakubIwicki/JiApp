namespace JiApp.Api.Configuration;

public static class SwaggerConstants
{
    public static class Tags
    {
        public const string Auth = "Auth";
        public const string Search = "Search";
        public const string Downloads = "Downloads";
        public const string History = "History";
        public const string System = "System";
    }

    public static class TagDescriptions
    {
        public const string Auth = "User authentication, registration, and profile management";
        public const string Search = "YouTube video search and search history retrieval";
        public const string Downloads = "MP3 download operations — requesting links and retrieving files";
        public const string History = "Aggregated user activity history across search and downloads";
        public const string System = "Health checks and operational endpoints";
    }

    public static class Document
    {
        public const string Title = "JiApp API v1";
        public const string Version = "v1";
    }
}