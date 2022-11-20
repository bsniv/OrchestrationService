#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["OrchestrationService/OrchestrationService.csproj", "OrchestrationService/"]
RUN dotnet restore "OrchestrationService/OrchestrationService.csproj"
COPY . .
WORKDIR "/src/OrchestrationService"
RUN dotnet build "OrchestrationService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "OrchestrationService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
RUN mkdir WireguardDb
RUN mkdir WireguardDb/x
RUN chmod -R 0777 /app/WireguardDb
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "OrchestrationService.dll"]