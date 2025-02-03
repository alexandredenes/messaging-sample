FROM dpage/pgadmin4:latest

# Configurações de ambiente para o pgAdmin
ENV PGADMIN_DEFAULT_EMAIL=admin@example.com
ENV PGADMIN_DEFAULT_PASSWORD=admin

# Copia o arquivo de configuração do servidor
COPY servers.json /pgadmin4/servers.json

# Expõe a porta padrão do pgAdmin
EXPOSE 80