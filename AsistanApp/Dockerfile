# .NET SDK imajını kullan
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Sadece proje dosyasını kopyala ve paketleri geri yükle (Restore)
# Dosyan en üstte olduğu için doğrudan kopyalıyoruz
COPY AsistanApp.csproj ./
RUN dotnet restore "./AsistanApp.csproj"

# Geri kalan tüm dosyaları kopyala
COPY . .

# Uygulamayı yayınla (Publish)
RUN dotnet publish "AsistanApp.csproj" -c Release -o /app/publish

# Çalışma zamanı (Runtime) imajına geç
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "AsistanApp.dll"]
