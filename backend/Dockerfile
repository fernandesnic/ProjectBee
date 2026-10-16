# Etapa 1: compilar (imagem grande, com o SDK)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ProjectBee.csproj .
RUN dotnet restore

COPY . .
RUN dotnet publish ProjectBee.csproj -c Release -o /app/publish --no-restore
# Etapa 2: rodar (imagem pequena, só com o runtime)
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .

USER $APP_UID
ENTRYPOINT ["dotnet", "ProjectBee.dll"]