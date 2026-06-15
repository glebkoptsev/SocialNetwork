namespace Libraries.RabbitMQ;

public class RabbitMQSettings
{
    public string Host { get; set; } = null!;
    public string Host_debug { get; set; } = null!;
    public int Port { get; set; } = 5672;
    public string Username { get; set; } = "socialnetwork";
    public string Password { get; set; } = null!;
}
