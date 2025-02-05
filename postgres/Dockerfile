FROM postgres:17

# Instala dependências para PL/Python e criação de ambiente virtual
RUN apt-get update && apt-get install -y \
    postgresql-plpython3-17 \
    python3-venv \
    && rm -rf /var/lib/apt/lists/*

# Cria um ambiente virtual Python
RUN python3 -m venv /opt/pyenv

# Instala a biblioteca pika no ambiente virtual
RUN /opt/pyenv/bin/pip install pika

# Copia o script de inicialização do banco de dados
COPY init.sql /docker-entrypoint-initdb.d/

# Define o caminho do ambiente virtual para o PL/Python
ENV PYTHONPATH /opt/pyenv/lib/python3.11/site-packages

EXPOSE 5432