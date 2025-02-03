using System;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using Npgsql; // Usando Npgsql para PostgreSQL

class Program
{
    static void Main(string[] args)
    {
        var factory = new ConnectionFactory() { HostName = "rabbitmq" };
        using (var connection = factory.CreateConnection())
        using (var channel = connection.CreateModel())
        {
            channel.QueueDeclare(queue: "nota_queue",
                                 durable: false,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: null);

            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                Console.WriteLine(" [x] Received {0}", message);

                // Processar a mensagem e atualizar o banco de dados PostgreSQL
                UpdatePostgresDatabase(message);
            };
            channel.BasicConsume(queue: "nota_queue",
                                 autoAck: true,
                                 consumer: consumer);

            Console.WriteLine(" Press [enter] to exit.");
            Console.ReadLine();
        }
    }

    static void UpdatePostgresDatabase(string message)
    {
        var parts = message.Split(',');
        var id = parts[0];

        string connectionString = "Host=postgres;Username=postgres;Password=postgres;Database=SampleMessage";

        using (var conn = new NpgsqlConnection(connectionString))
        {
            conn.Open();
            using (var cmd = new NpgsqlCommand())
            {
                cmd.Connection = conn;
                cmd.CommandText = "UPDATE Nota SET aprovado = 'S' WHERE ID = @id";
                cmd.Parameters.AddWithValue("id", int.Parse(id));
                cmd.ExecuteNonQuery();
            }
        }
    }
}