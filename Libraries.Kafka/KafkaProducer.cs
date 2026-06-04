using Confluent.Kafka;

namespace Libraries.Kafka
{
    public class KafkaProducer<K, V>(KafkaClientHandle handle) : IKafkaProducer where K : class where V : class
    {
        private readonly IProducer<K, V> kafkaHandle = new DependentProducerBuilder<K, V>(handle.Handle).Build();

        public Task ProduceAsync(string topic, Message<K, V> message)
        {
            return kafkaHandle.ProduceAsync(topic, message);
        }

        Task IKafkaProducer.ProduceAsync(string topic, Message<string, string> message)
        {
            return ProduceAsync(topic, message as Message<K, V> ?? throw new InvalidOperationException("Type mismatch"));
        }

        public void Produce(string topic, Message<K, V> message, Action<DeliveryReport<K, V>>? deliveryHandler = null)
        {
            kafkaHandle.Produce(topic, message, deliveryHandler);
        }

        public void Flush(TimeSpan timeout)
        {
            kafkaHandle.Flush(timeout);
        }
    }
}
