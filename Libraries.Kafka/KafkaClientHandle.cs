using Confluent.Kafka;
using Microsoft.Extensions.Options;

namespace Libraries.Kafka
{
    public class KafkaClientHandle
    {
        private readonly Lazy<IProducer<byte[], byte[]>> kafkaProducer;

        public KafkaClientHandle(IOptions<KafkaSettings> config)
        {
            kafkaProducer = new Lazy<IProducer<byte[], byte[]>>(() =>
            {
                var conf = new ProducerConfig
                {
                    ClientId = "dotnet producer",
                    SocketConnectionSetupTimeoutMs = 10000,
                    ReconnectBackoffMaxMs = 5000,
#if DEBUG
                    BootstrapServers = config.Value.Host_debug,
#else
                    BootstrapServers = config.Value.Host,
#endif
                };
                return new ProducerBuilder<byte[], byte[]>(conf).Build();
            });
        }

        public Handle Handle { get => kafkaProducer.Value.Handle; }

        public void Dispose()
        {
            if (kafkaProducer.IsValueCreated)
            {
                kafkaProducer.Value.Flush();
                kafkaProducer.Value.Dispose();
            }
        }
    }
}
