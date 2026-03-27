# 1. Aşama: Çalışma ortamı (Runtime)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 5168

# 2. Aşama: Derleme (Build)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["TicketService.csproj", "./"]
RUN dotnet restore "TicketService.csproj"
COPY . .
RUN dotnet build "TicketService.csproj" -c Release -o /app/build

# 3. Aşama: Yayınlama (Publish)
FROM build AS publish
RUN dotnet publish "TicketService.csproj" -c Release -o /app/publish /p:UseAppHost=false

# 4. Aşama: Final
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TicketService.dll"]