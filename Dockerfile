# Imagen de producción de HobbitCoder.TMTBucaramanga.Sheepfold.
# Escucha HTTP en el puerto 8080: el HTTPS lo termina el proxy (Traefik de Dokploy) delante del contenedor.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Primero solo los archivos de proyecto: la restauración queda en caché mientras no cambien las dependencias.
COPY Directory.Build.props Directory.Packages.props ./
COPY HobbitCoder.TMTBucaramanga.Sheepfold.WebAPI/HobbitCoder.TMTBucaramanga.Sheepfold.WebAPI.csproj HobbitCoder.TMTBucaramanga.Sheepfold.WebAPI/
COPY HobbitCoder.TMTBucaramanga.Sheepfold.Application/HobbitCoder.TMTBucaramanga.Sheepfold.Application.csproj HobbitCoder.TMTBucaramanga.Sheepfold.Application/
COPY HobbitCoder.TMTBucaramanga.Sheepfold.Domain/HobbitCoder.TMTBucaramanga.Sheepfold.Domain.csproj HobbitCoder.TMTBucaramanga.Sheepfold.Domain/
COPY HobbitCoder.TMTBucaramanga.Sheepfold.Mediator/HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.csproj HobbitCoder.TMTBucaramanga.Sheepfold.Mediator/
COPY HobbitCoder.TMTBucaramanga.Sheepfold.Infrastructure/HobbitCoder.TMTBucaramanga.Sheepfold.Infrastructure.csproj HobbitCoder.TMTBucaramanga.Sheepfold.Infrastructure/
COPY HobbitCoder.TMTBucaramanga.Sheepfold.Persistence.MSSQL/HobbitCoder.TMTBucaramanga.Sheepfold.Persistence.MSSQL.csproj HobbitCoder.TMTBucaramanga.Sheepfold.Persistence.MSSQL/
COPY HobbitCoder.TMTBucaramanga.Sheepfold.Persistence.PostgreSQL/HobbitCoder.TMTBucaramanga.Sheepfold.Persistence.PostgreSQL.csproj HobbitCoder.TMTBucaramanga.Sheepfold.Persistence.PostgreSQL/
RUN dotnet restore HobbitCoder.TMTBucaramanga.Sheepfold.WebAPI/HobbitCoder.TMTBucaramanga.Sheepfold.WebAPI.csproj

COPY . .
RUN dotnet publish HobbitCoder.TMTBucaramanga.Sheepfold.WebAPI/HobbitCoder.TMTBucaramanga.Sheepfold.WebAPI.csproj \
    --configuration Release --no-restore --output /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# ASPNETCORE_FORWARDEDHEADERS_ENABLED: respeta X-Forwarded-Proto/For del proxy, así la app sabe que la
# petición original fue HTTPS.
ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_HTTP_PORTS=8080 \
    ASPNETCORE_FORWARDEDHEADERS_ENABLED=true

COPY --from=build /app/publish .

USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "HobbitCoder.TMTBucaramanga.Sheepfold.WebAPI.dll"]
