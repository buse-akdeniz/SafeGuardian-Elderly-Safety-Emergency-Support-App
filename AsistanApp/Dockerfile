FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Docker'a diyoruz ki: "Dosya hemen yanındaysa oradan al, 
# yoksa AsistanApp klasörünün içine bak."
COPY ["AsistanApp/AsistanApp.csproj", "AsistanApp/"]
COPY ["AsistanApp.csproj", "./"] || true

# Hangisini bulduysa onunla devam et
RUN dotnet restore "AsistanApp.csproj" || dotnet restore "AsistanApp/AsistanApp.csproj"

COPY . .
WORKDIR "/src/AsistanApp"
RUN dotnet publish "AsistanApp.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .

# Veritabanı klasörü ve izinleri
USER root
RUN mkdir -p /var/lib/vitaguard && chmod -R 777 /var/lib/vitaguard

ENV PORT=8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "AsistanApp.dll"]
