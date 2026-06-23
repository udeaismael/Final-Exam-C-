FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
# Install Tesseract + Spanish language pack
RUN apt-get update && apt-get install -y \
    tesseract-ocr \
    tesseract-ocr-spa \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/RentalAPI.Worker/RentalAPI.Worker.csproj",      "src/RentalAPI.Worker/"]
COPY ["src/RentalAPI.Infrastructure/RentalAPI.Infrastructure.csproj", "src/RentalAPI.Infrastructure/"]
COPY ["src/RentalAPI.Domain/RentalAPI.Domain.csproj",      "src/RentalAPI.Domain/"]
RUN dotnet restore "src/RentalAPI.Worker/RentalAPI.Worker.csproj"
COPY . .
RUN dotnet publish "src/RentalAPI.Worker/RentalAPI.Worker.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "RentalAPI.Worker.dll"]
