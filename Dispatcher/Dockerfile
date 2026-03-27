# .NET 10 Runtime imajını kullan
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 5000

# .NET 10 SDK imajını derleme için kullan
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Dispatcher.csproj", "./"]
RUN dotnet restore "Dispatcher.csproj"
COPY . .
RUN dotnet build "Dispatcher.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Dispatcher.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Dispatcher.dll"]