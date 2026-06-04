using Confluent.Kafka;

namespace Libraries.Kafka
{
    public interface IKafkaProducer
    {
        Task ProduceAsync(string topic, Message<string, string> message);
    }
}
