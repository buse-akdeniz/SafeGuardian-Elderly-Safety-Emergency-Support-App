FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Dosyayı bulması için klasör yapısını Docker içinde oluşturuyoruz
COPY ["AsistanApp/AsistanApp.csproj", "AsistanApp/"]
RUN dotnet restore "AsistanApp/AsistanApp.csproj"

# Geri kalan her şeyi kopyala
COPY . .

# Çalışma dizinini projenin içine çek ve yayınla
WORKDIR "/src/AsistanApp"
RUN dotnet publish "AsistanApp.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .

# Senin eklediğin kritik veritabanı izinleri
USER root
RUN mkdir -p /var/lib/vitaguard && chmod -R 777 /var/lib/vitaguard

ENV PORT=8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "AsistanApp.dll"]
