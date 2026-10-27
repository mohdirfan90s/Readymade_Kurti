# Multi-stage Docker build for Garment Lot Manager (.NET 10)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["GarmentLotManager.csproj", "./"]
RUN dotnet restore "GarmentLotManager.csproj"
COPY . .
RUN dotnet publish "GarmentLotManager.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
ENV PORT=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "GarmentLotManager.dll"]
