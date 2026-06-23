FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/RentalAPI.Web/RentalAPI.Web.csproj", "src/RentalAPI.Web/"]

RUN dotnet restore "src/RentalAPI.Web/RentalAPI.Web.csproj"

COPY . .

RUN dotnet publish "src/RentalAPI.Web/RentalAPI.Web.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8081
ENTRYPOINT ["dotnet", "RentalAPI.Web.dll"]