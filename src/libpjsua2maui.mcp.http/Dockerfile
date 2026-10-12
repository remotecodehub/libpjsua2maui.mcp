FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/libpjsua2maui.mcp.http/libpjsua2maui.mcp.http.csproj", "src/libpjsua2maui.mcp.http/"]
COPY ["src/libpjsua2maui.mcp/libpjsua2maui.mcp.csproj", "src/libpjsua2maui.mcp/"]
RUN dotnet restore "./src/libpjsua2maui.mcp.http/libpjsua2maui.mcp.http.csproj"
COPY . .
WORKDIR "/src/src/libpjsua2maui.mcp.http"
RUN dotnet build "./libpjsua2maui.mcp.http.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./libpjsua2maui.mcp.http.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "libpjsua2maui.mcp.http.dll"]