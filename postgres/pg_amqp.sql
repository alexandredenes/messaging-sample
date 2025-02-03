-- Habilita o PL/Python no banco de dados
CREATE EXTENSION IF NOT EXISTS plpython3u;

-- Função para publicar mensagens no RabbitMQ
CREATE OR REPLACE FUNCTION public.notify_rabbitmq(message TEXT)
RETURNS VOID AS $$
import pika

try:
    plpy.notice(f"INICIANDO ENVIO")
    connection = pika.BlockingConnection(pika.ConnectionParameters('rabbitmq'))
    channel = connection.channel()

    channel.queue_declare(queue='nota_queue', durable=False)

    channel.basic_publish(
        exchange='',
        routing_key='nota_queue',
        body=message
    )

    connection.close()
except Exception as e:
    plpy.notice(f"Erro ao publicar mensagem no RabbitMQ: {e}")
$$ LANGUAGE plpython3u;

-- Trigger para chamar a função ao inserir na tabela Nota
CREATE OR REPLACE FUNCTION public.nota_trigger_function()
RETURNS TRIGGER AS $$
BEGIN
    -- Constrói a mensagem com os dados da nova linha
    PERFORM public.notify_rabbitmq(
        NEW.ID::text || ',' || NEW.descricao || ',' || NEW.dataPagamento::text || ',' || NEW.valor::text || ',' || NEW.solicitante || ',' || NEW.aprovado
    );
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Cria o trigger na tabela Nota
CREATE TRIGGER nota_trigger
AFTER INSERT ON Nota
FOR EACH ROW EXECUTE FUNCTION public.nota_trigger_function();