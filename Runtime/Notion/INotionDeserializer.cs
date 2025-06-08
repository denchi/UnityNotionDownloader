namespace Game.Serialization.Notion
{
    public interface INotionDeserializer
    {
        public NotionDownloader notionDownloader { get; set; }
        public IJObjectValueReader valueReader { get; set; }
        public DatabaseCache databaseCache { get; set; }
    }
}