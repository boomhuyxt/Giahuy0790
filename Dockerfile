# Dockerfile cho ứng dụng ASP.NET Core 10.0 chạy trên cổng 8888
# ==============================================================

# Giai đoạn 1: Runtime Base
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8888
ENV ASPNETCORE_HTTP_PORTS=8888

# Giai đoạn 2: Build source code
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Giahuy0790.csproj", "."]
RUN dotnet restore "./Giahuy0790.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "./Giahuy0790.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Giai đoạn 3: Publish ứng dụng
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Giahuy0790.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Giai đoạn 4: Image cuối cùng (Production)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_HTTP_PORTS=8888
ENTRYPOINT ["dotnet", "Giahuy0790.dll"]
