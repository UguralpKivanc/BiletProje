# 1. Aşama: Çalışma ortamı (Runtime)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 5001

# 2. Aşama: Derleme (Build)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["EventService.csproj", "./"]
RUN dotnet restore "EventService.csproj"
COPY . .
RUN dotnet build "EventService.csproj" -c Release -o /app/build

# 3. Aşama: Yayınlama (Publish)
FROM build AS publish
RUN dotnet publish "EventService.csproj" -c Release -o /app/publish /p:UseAppHost=false

# 4. Aşama: Final (Sadece çalışan dosyalar)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "EventService.dll"]