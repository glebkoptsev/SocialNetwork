namespace UserService.Database
{
    public record OutboxEntry(string KafkaKey, string KafkaValue);
}
