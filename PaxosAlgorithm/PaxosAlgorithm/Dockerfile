FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["PaxosAlgorithm/PaxosAlgorithm.csproj", "PaxosAlgorithm/"]
RUN dotnet restore "PaxosAlgorithm/PaxosAlgorithm.csproj"
COPY . .
WORKDIR "/src/PaxosAlgorithm"
RUN dotnet build "./PaxosAlgorithm.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./PaxosAlgorithm.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PaxosAlgorithm.dll"]
