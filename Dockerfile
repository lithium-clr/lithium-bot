FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/Lithium.Bot/Lithium.Bot.csproj", "src/Lithium.Bot/"]
RUN dotnet restore "src/Lithium.Bot/Lithium.Bot.csproj"
COPY . .
WORKDIR "/src/src/Lithium.Bot"
RUN dotnet build "./Lithium.Bot.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Lithium.Bot.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Lithium.Bot.dll"]
