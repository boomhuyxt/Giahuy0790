# ==============================================================
# Dockerfile cho ứng dụng ASP.NET Core 10.0 chạy trên cổng 8888
# ==============================================================

# Giai đoạn 1: Base Runtime Image (.NET 10)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8888
ENV ASPNETCORE_HTTP_PORTS=8888

# Giai đoạn 2: Build source code (.NET 10 SDK)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy project file và restore dependencies trước để tận dụng cache
COPY ["Giahuy0790.csproj", "."]
RUN dotnet restore "./Giahuy0790.csproj"

# Copy toàn bộ mã nguồn và biên dịch
COPY . .
WORKDIR "/src/."
RUN dotnet build "./Giahuy0790.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Giai đoạn 3: Publish ứng dụng
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Giahuy0790.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Giai đoạn 4: Final Image (Production/Container)
FROM base AS final
WORKDIR /app

# Copy các file đã build với quyền sở hữu của người dùng app
COPY --chown=$APP_UID:$APP_UID --from=publish /app/publish .

# Tạo thư mục lưu trữ uploads và phân quyền ghi cho ứng dụng
USER root
RUN mkdir -p /app/wwwroot/uploads/products \
             /app/wwwroot/uploads/posts \
             /app/wwwroot/uploads/voice \
             /app/wwwroot/uploads/kids \
             /app/wwwroot/uploads/avatars \
    && chown -R $APP_UID:$APP_UID /app/wwwroot/uploads \
    && chmod -R 775 /app/wwwroot/uploads
USER $APP_UID

# Cấu hình biến môi trường chạy trên cổng 8888
ENV ASPNETCORE_HTTP_PORTS=8888
ENV ASPNETCORE_ENVIRONMENT=Development

ENTRYPOINT ["dotnet", "Giahuy0790.dll"]
