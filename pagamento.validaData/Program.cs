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
        Console.WriteLine("Iniciando serviço de validação de data");
        var factory = new ConnectionFactory() { HostName = "rabbitmq" };
        var validaDataQueue = "pagamento.passo2.valida-data";
        using (var connection = factory.CreateConnection())
        using (var channel = connection.CreateModel())
        {
            channel.QueueDeclare(queue: validaDataQueue,
                                 durable: false,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: null);

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

            var pagamentoValidaAlcada = "pagamento.passo3.valida-alcada";
            channel.QueueDeclare(queue: pagamentoValidaAlcada,
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
                    var dataPagamento = DateTime.ParseExact(messageDict["dataPagamento"].ToString(), "yyyy-MM-dd", CultureInfo.InvariantCulture);
                    if (dataPagamento.Day >= 28 && dataPagamento.Day <= 30)
                    {
                        channel.BasicPublish(exchange: "",
                                        routingKey: pagamentoFinalAprovadoQueue,
                                        basicProperties: null,
                                        body: body);
                    }
                    else if (dataPagamento.Day >= 20 && dataPagamento.Day <= 27)
                    {
                        channel.BasicPublish(exchange: "",
                                        routingKey: pagamentoValidaAlcada,
                                        basicProperties: null,
                                        body: body);
                    }
                    else
                    {
                        messageDict["motivoRejeicao"] = "Data de pagamento fora do prazo";
                        var updatedMessage = JsonSerializer.Serialize(messageDict);
                        body = Encoding.UTF8.GetBytes(updatedMessage);
                        channel.BasicPublish(exchange: "",
                                        routingKey: pagamentoFinalRejeitadoQueue,
                                        basicProperties: null,
                                        body: body);
                    }
                    int id = ((JsonElement)messageDict["ID"]).GetInt32();

                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erro: {ex.Message}");
                }
            };
            channel.BasicConsume(queue: validaDataQueue,
                                 autoAck: true,
                                 consumer: consumer);

            Console.WriteLine(" Press. [enter] para sair");
            Console.ReadLine();
            Console.WriteLine("Finalizado");
        }
    }
}

