# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY TaskFlow.slnx ./
COPY TaskFlow.API/TaskFlow.API.csproj TaskFlow.API/
COPY TaskFlow.Application/TaskFlow.Application.csproj TaskFlow.Application/
COPY TaskFlow.Domain/TaskFlow.Domain.csproj TaskFlow.Domain/
COPY TaskFlow.Infrastructure/TaskFlow.Infrastructure.csproj TaskFlow.Infrastructure/
RUN dotnet restore TaskFlow.API/TaskFlow.API.csproj

COPY . .
RUN dotnet publish TaskFlow.API/TaskFlow.API.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

COPY --from=build /app/publish .

EXPOSE 8080
ENTRYPOINT ["dotnet", "TaskFlow.API.dll"]

