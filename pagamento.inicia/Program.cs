using System;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using Npgsql; // Usando Npgsql para PostgreSQL
using System.Text.Json; // Usando System.Text.Json para JsonSerializer
using System.Globalization; // Usando System.Globalization para CultureInfo
using System.Collections.Generic; // Usando System.Collections.Generic para Dictionary

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Iniciando serviço de pagamento");
        var factory = new ConnectionFactory() { HostName = "rabbitmq" };
        var pagamentoPasso1Queue = "pagamento.passo1.recebe-requisicao";
        using (var connection = factory.CreateConnection())
        using (var channel = connection.CreateModel())
        {
            channel.QueueDeclare(queue: pagamentoPasso1Queue,
                                 durable: false,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: null);

            var pagamentoPasso2Queue = "pagamento.passo2.valida-data";
            channel.QueueDeclare(queue: pagamentoPasso2Queue,
                                 durable: false,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: null);


            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (model, ea) =>
            {
                try {
                    Console.WriteLine("Recebendo mensagem");
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    Console.WriteLine($"Recebido: {message}" );

                    var messageDict = JsonSerializer.Deserialize<Dictionary<string, object>>(message);
                    if (messageDict.ContainsKey("dataPagamento"))
                    {
                        messageDict["dataPagamento"] = DateTime.ParseExact(messageDict["dataPagamento"].ToString(), "yyyy-MM-dd", CultureInfo.InvariantCulture);
                    }
                    int id = ((JsonElement)messageDict["ID"]).GetInt32();
                    
                    MarcaRegistroComoEmProcessamento(id);
                    Console.WriteLine($"Marcado como em processamento: {messageDict["ID"]}");

                    channel.BasicPublish(exchange: "",
                                        routingKey: pagamentoPasso2Queue,
                                        basicProperties: null,
                                        body: body);
                    Console.WriteLine($"Mensagem {id} publicada para {pagamentoPasso2Queue}");

                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erro: {ex.Message}");
                }
            };
            channel.BasicConsume(queue: pagamentoPasso1Queue,
                                 autoAck: true,
                                 consumer: consumer);

            Console.WriteLine(" Press. [enter] para sair");
            Console.ReadLine();
            Console.WriteLine("Finalizado");
        }
    }

    static void MarcaRegistroComoEmProcessamento(int id)
    {
        Console.WriteLine($"Marcando registro {id} como em processamento");
        string connectionString = "Host=postgres;Username=postgres;Password=postgres;Database=postgres";

        using (var conn = new NpgsqlConnection(connectionString))
        {
            conn.Open();
            using (var cmd = new NpgsqlCommand())
            {
                cmd.Connection = conn;
                cmd.CommandText = "UPDATE Nota SET situacao = 'P' WHERE ID = @id";
                cmd.Parameters.AddWithValue("id", id);
                cmd.ExecuteNonQuery();
            }
        }
    }
}