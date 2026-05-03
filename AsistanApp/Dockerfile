FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Railway bu Dockerfile'ı `AsistanApp` klasörünü build context olarak kullanarak çalıştırıyor.
# Bu yüzden dosya yolları bu klasöre göre verilmelidir.
COPY AsistanApp.csproj ./
RUN dotnet restore "AsistanApp.csproj"

# Tüm dosyaları kopyala ve yayınla
COPY . .
RUN dotnet publish "AsistanApp.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
ENV PORT=8080
USER root
RUN mkdir -p /var/lib/vitaguard && chmod -R 777 /var/lib/vitaguard
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "AsistanApp.dll"]
