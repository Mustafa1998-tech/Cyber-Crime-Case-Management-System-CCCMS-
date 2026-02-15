# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/Nciems.Api/Nciems.Api.csproj src/Nciems.Api/
COPY src/Nciems.Application/Nciems.Application.csproj src/Nciems.Application/
COPY src/Nciems.Infrastructure/Nciems.Infrastructure.csproj src/Nciems.Infrastructure/
COPY src/Nciems.Domain/Nciems.Domain.csproj src/Nciems.Domain/
RUN dotnet restore src/Nciems.Api/Nciems.Api.csproj

COPY src/ src/
RUN dotnet publish src/Nciems.Api/Nciems.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_ENVIRONMENT=Development
ENV ASPNETCORE_URLS=http://+:5289;https://+:7261

EXPOSE 5289
EXPOSE 7261

COPY --from=build /app/publish .
COPY src/Nciems.Api/certs ./certs
RUN mkdir -p logs EvidenceStore

ENTRYPOINT ["dotnet", "Nciems.Api.dll"]
