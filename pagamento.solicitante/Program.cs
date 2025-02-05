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
        Console.WriteLine("Iniciando serviço de validação de solicitante");
        var factory = new ConnectionFactory() { HostName = "rabbitmq" };
        var validaAlcadaQueue = "pagamento.passo3.valida-alcada";
        using (var connection = factory.CreateConnection())
        using (var channel = connection.CreateModel())
        {
            channel.QueueDeclare(queue: validaAlcadaQueue,
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

            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (model, ea) =>
            {
                try {
                    Console.WriteLine("Recebendo mensagem");
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    Console.WriteLine($"Recebido: {message}" );

                    var messageDict = JsonSerializer.Deserialize<Dictionary<string, object>>(message);
                    decimal valor = ((JsonElement)messageDict["valor"]).GetDecimal();
                    string solicitante = messageDict["solicitante"].ToString();

                    switch(solicitante) {
                        case "Fulano":
                            if(valor <= 1500) {
                                channel.BasicPublish(exchange: "",
                                        routingKey: pagamentoFinalAprovadoQueue,
                                        basicProperties: null,
                                        body: body);
                            }
                            else {
                                messageDict["motivoRejeicao"] = "Valor acima da alçada";
                                body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(messageDict));
                                channel.BasicPublish(exchange: "",
                                        routingKey: pagamentoFinalRejeitadoQueue,
                                        basicProperties: null,
                                        body: body);
                            }
                            break;
                        case "Beltrano":
                            if(valor <= 5000) {
                                channel.BasicPublish(exchange: "",
                                        routingKey: pagamentoFinalAprovadoQueue,
                                        basicProperties: null,
                                        body: body);
                            }
                            else {
                                messageDict["motivoRejeicao"] = "Valor acima da alçada";
                                body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(messageDict));
                                channel.BasicPublish(exchange: "",
                                        routingKey: pagamentoFinalRejeitadoQueue,
                                        basicProperties: null,
                                        body: body);
                            }
                            break;
                        default:
                            messageDict["motivoRejeicao"] = "Solicitante não autorizado";
                            body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(messageDict));
                            channel.BasicPublish(exchange: "",
                                        routingKey: pagamentoFinalRejeitadoQueue,
                                        basicProperties: null,
                                        body: body);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erro: {ex.Message}");
                }
            };
            channel.BasicConsume(queue: validaAlcadaQueue,
                                 autoAck: true,
                                 consumer: consumer);

            Console.WriteLine(" Press. [enter] para sair");
            Console.ReadLine();
            Console.WriteLine("Finalizado");
        }
    }
}

