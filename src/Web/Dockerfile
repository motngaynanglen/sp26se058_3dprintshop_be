# Giai đoạn 1: Base Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

# Giai đoạn 2: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy các file cấu hình solution 
COPY ["Directory.Packages.props", "."]
COPY ["Directory.Build.props", "."]
COPY ["sp26se058_3dprintshop_be.sln", "./"]

# Copy các file dự án theo cấu trúc thư mục src/
COPY ["src/Web/Web.csproj", "src/Web/"]
COPY ["src/Application/Application.csproj", "src/Application/"]
COPY ["src/Domain/Domain.csproj", "src/Domain/"]
COPY ["src/Infrastructure/Infrastructure.csproj", "src/Infrastructure/"]

# Restore các thư viện
RUN dotnet restore "./src/Web/Web.csproj"

# Copy toàn bộ mã nguồn còn lại
COPY . .

# Build dự án Web
WORKDIR "/src/src/Web"
RUN dotnet build "./Web.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Giai đoạn 3: Publish
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Web.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Giai đoạn cuối: Chạy ứng dụng
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
# Đảm bảo tên DLL này trùng khớp với file thực tế trong project Web
ENTRYPOINT ["dotnet", "sp26se058_3dprintshop_be.Web.dll"]