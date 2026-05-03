FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Sadece ana proje dosyasını kopyalayalım (Wildcard yerine tam yol)
COPY AsistanApp/AsistanApp.csproj ./AsistanApp/
RUN dotnet restore "AsistanApp/AsistanApp.csproj"

# Tüm dosyaları kopyala ve yayınla
COPY . .
WORKDIR "/src/AsistanApp"
RUN dotnet publish "AsistanApp.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "AsistanApp.dll"]
