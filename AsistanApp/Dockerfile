FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
# Docker içinde çalışma alanını /src yapıyoruz
WORKDIR /src

# Docker'ın dosyaları bulabilmesi için kopyalama komutu
# Nokta işareti (.) Dockerfile'ın bulunduğu klasörü (yani AsistanApp klasörünü) temsil eder
COPY ["AsistanApp.csproj", "./"]
RUN dotnet restore "AsistanApp.csproj"

# Klasördeki her şeyi kopyala
COPY . .

# Yayınla
RUN dotnet publish "AsistanApp.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .

# Veritabanı izni (SQLite hatasını çözen kısım)
USER root
RUN mkdir -p /var/lib/vitaguard && chmod -R 777 /var/lib/vitaguard

ENV PORT=8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "AsistanApp.dll"]
