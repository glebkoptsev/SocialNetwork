namespace UserService.Database.Entities
{
    public class FeedOutboxEntity
    {
        public long Id { get; set; }
        public string Kafka_key { get; set; } = null!;
        public string Kafka_value { get; set; } = null!;
        public DateTime Created_at { get; set; }
        public DateTime? Processed_at { get; set; }
    }
}
