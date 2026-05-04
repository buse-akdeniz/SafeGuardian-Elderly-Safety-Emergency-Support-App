FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["AsistanApp/AsistanApp.csproj", "AsistanApp/"]
RUN dotnet restore "AsistanApp/AsistanApp.csproj"
COPY . .
WORKDIR "/src/AsistanApp"
RUN dotnet build "AsistanApp.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AsistanApp.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AsistanApp.dll"]
