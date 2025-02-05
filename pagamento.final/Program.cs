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
        Console.WriteLine("Iniciando serviço de finalizacao de pagamento"); 
        var factory = new ConnectionFactory() { HostName = "rabbitmq" };
        using (var connection = factory.CreateConnection())
        using (var channel = connection.CreateModel())
        {
            var pagamentoFinalAprovadoQueue = "pagamento.final.aprovado";
            channel.QueueDeclare(queue: pagamentoFinalAprovadoQueue,
                                 durable: false,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: null);

            var pagamentoFinalRejeitadoQueue = "pagamento.final.rejeitado";
            channel.QueueDeclare(queue: pagamentoFinalRejeitadoQueue,
                                 durable: false,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: null);

            var consumerAprovado = new EventingBasicConsumer(channel);
            consumerAprovado.Received += (model, ea) =>
            {
                try {
                    Console.WriteLine($"Mensagem recebida para aprovação");
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);

                    var messageDict = JsonSerializer.Deserialize<Dictionary<string, object>>(message);
                    int id = ((JsonElement)messageDict["ID"]).GetInt32();
                    AprovaPagamento(id);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erro: {ex.Message}");
                }
            };
            var consumerRejeitado = new EventingBasicConsumer(channel);
            consumerRejeitado.Received += (model, ea) =>
            {
                try {
                    Console.WriteLine($"Mensagem recebida para rejeição");
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);

                    var messageDict = JsonSerializer.Deserialize<Dictionary<string, object>>(message);
                    int id = ((JsonElement)messageDict["ID"]).GetInt32();
                    RejeitaPagamento(id, messageDict["motivoRejeicao"].ToString());
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erro: {ex.Message}");
                }
            };
            channel.BasicConsume(queue: pagamentoFinalAprovadoQueue,
                                 autoAck: true,
                                 consumer: consumerAprovado);
            channel.BasicConsume(queue: pagamentoFinalRejeitadoQueue,
                                 autoAck: true,
                                 consumer: consumerRejeitado);

            Console.WriteLine(" Press. [enter] para sair");
            Console.ReadLine();
            Console.WriteLine("Finalizado");
        }
    }

    static void AprovaPagamento(int id)
    {
        Console.WriteLine($"Marcando registro {id} como aprovado");
        string connectionString = "Host=postgres;Username=postgres;Password=postgres;Database=postgres";

        using (var conn = new NpgsqlConnection(connectionString))
        {
            conn.Open();
            using (var cmd = new NpgsqlCommand())
            {
                cmd.Connection = conn;
                cmd.CommandText = "UPDATE Nota SET situacao = 'A' WHERE ID = @id";
                cmd.Parameters.AddWithValue("id", id);
                cmd.ExecuteNonQuery();
            }
        }
    }

    static void RejeitaPagamento(int id, string motivo)
    {
        Console.WriteLine($"Marcando registro {id} como rejeitado");
        string connectionString = "Host=postgres;Username=postgres;Password=postgres;Database=postgres";

        using (var conn = new NpgsqlConnection(connectionString))
        {
            conn.Open();
            using (var cmd = new NpgsqlCommand())
            {
                cmd.Connection = conn;
                cmd.CommandText = "UPDATE Nota SET situacao = 'R' WHERE ID = @id";
                cmd.Parameters.AddWithValue("id", id);
                cmd.ExecuteNonQuery();
                cmd.CommandText = "INSERT INTO reprovadas (fk_nota, descricao) VALUES (@id, @motivo)";
                cmd.Parameters.AddWithValue("motivo", motivo);
                cmd.ExecuteNonQuery();
            }
        }
    }
}

