FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# BU SATIRI DEĞİŞTİRİYORUZ:
# Alt klasörlerdeki tüm .csproj dosyalarını bul ve kopyala
COPY **/*.csproj ./
RUN dotnet restore

COPY . .
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "AsistanApp.dll"]
