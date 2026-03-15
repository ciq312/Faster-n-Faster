#build
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src
COPY FasterNFaster.Api.csproj .
RUN dotnet restore FasterNFaster.Api.csproj
COPY . .
RUN dotnet publish -c Release -o /app/publish --no-restore

# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
CMD ["dotnet", "FasterNFaster.Api.dll"]
