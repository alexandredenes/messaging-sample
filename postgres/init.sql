CREATE TABLE Nota (
    ID SERIAL PRIMARY KEY,
    descricao VARCHAR(255),
    dataPagamento DATE,
    valor DECIMAL(10, 2),
    solicitante VARCHAR(255),
    aprovado CHAR(1)
);

CREATE TABLE Reprovadas (
    ID SERIAL PRIMARY KEY,
    FK_Nota INT REFERENCES Nota(ID),
    descricao VARCHAR(255)
);