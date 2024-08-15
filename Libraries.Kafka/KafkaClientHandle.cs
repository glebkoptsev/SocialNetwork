using Confluent.Kafka;
using Microsoft.Extensions.Options;

namespace Libraries.Kafka
{
    public class KafkaClientHandle
    {
        private readonly IProducer<byte[], byte[]> kafkaProducer;

        public KafkaClientHandle(IOptions<KafkaSettings> config)
        {
            var conf = new ProducerConfig
            {
                ClientId = "dotnet producer",
#if DEBUG
                BootstrapServers = config.Value.Host_debug,
#else
                BootstrapServers = config.Value.Host,
#endif
            };
            kafkaProducer = new ProducerBuilder<byte[], byte[]>(conf).Build();
        }

        public Handle Handle { get => kafkaProducer.Handle; }

        public void Dispose()
        {
            kafkaProducer.Flush();
            kafkaProducer.Dispose();
        }
    }
}
