CREATE TABLE Nota (
    ID SERIAL PRIMARY KEY,
    descricao VARCHAR(255),
    dataPagamento DATE,
    valor DECIMAL(10, 2),
    solicitante VARCHAR(255),
    situacao CHAR(1)
);

CREATE TABLE Reprovadas (
    ID SERIAL PRIMARY KEY,
    FK_Nota INT REFERENCES Nota(ID),
    descricao VARCHAR(255)
);


CREATE EXTENSION IF NOT EXISTS plpython3u;

-- Função para publicar mensagens no RabbitMQ
CREATE OR REPLACE FUNCTION enviaRabbitMQ(message TEXT)
RETURNS VOID AS $$
import pika

try:
    plpy.notice(f"INICIANDO ENVIO")
    connection = pika.BlockingConnection(pika.ConnectionParameters('rabbitmq'))
    channel = connection.channel()

    channel.queue_declare(queue='pagamento.passo1.recebe-requisicao', durable=False)

    channel.basic_publish(
        exchange='',
        routing_key='pagamento.passo1.recebe-requisicao',
        body=message
    )

    connection.close()
except Exception as e:
    plpy.notice(f"Erro ao publicar mensagem no RabbitMQ: {e}")
$$ LANGUAGE plpython3u;

-- Trigger para chamar a função ao inserir na tabela Nota
CREATE OR REPLACE FUNCTION nota_trigger_function()
RETURNS TRIGGER AS $$
BEGIN
    -- Constrói a mensagem com os dados da nova linha
    PERFORM enviaRabbitMQ(
        json_build_object(
            'ID', NEW.ID,
            'descricao', NEW.descricao,
            'dataPagamento', NEW.dataPagamento,
            'valor', NEW.valor,
            'solicitante', NEW.solicitante
        )::text
    );   RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Cria o trigger na tabela Nota
CREATE TRIGGER nota_trigger
AFTER INSERT ON Nota
FOR EACH ROW EXECUTE FUNCTION nota_trigger_function();