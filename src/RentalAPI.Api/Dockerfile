FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/RentalAPI.Api/RentalAPI.Api.csproj",            "src/RentalAPI.Api/"]
COPY ["src/RentalAPI.Infrastructure/RentalAPI.Infrastructure.csproj", "src/RentalAPI.Infrastructure/"]
COPY ["src/RentalAPI.Domain/RentalAPI.Domain.csproj",      "src/RentalAPI.Domain/"]
RUN dotnet restore "src/RentalAPI.Api/RentalAPI.Api.csproj"
COPY . .
RUN dotnet publish "src/RentalAPI.Api/RentalAPI.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "RentalAPI.Api.dll"]
