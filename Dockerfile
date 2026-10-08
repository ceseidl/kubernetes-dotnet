# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
# Camada de restore: so muda quando o .csproj muda.
COPY src/Pedidos/Pedidos.csproj src/Pedidos/
RUN dotnet restore src/Pedidos/Pedidos.csproj
COPY src/Pedidos/ src/Pedidos/
RUN dotnet publish src/Pedidos/Pedidos.csproj \
    -c Release --no-restore -o /app /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
COPY --from=build /app .
USER $APP_UID
ENTRYPOINT ["dotnet", "Pedidos.dll"]
