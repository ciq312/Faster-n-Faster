# Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src


COPY ["FasterNFaster.Api.sln", "./"]


COPY ["FasterNFaster.Api.Presentation/FasterNFaster.Api.Presentation.csproj", "FasterNFaster.Api.Presentation/"]
COPY ["FasterNFaster.Api.UseCases/FasterNFaster.Api.UseCases.csproj", "FasterNFaster.Api.UseCases/"]
COPY ["FasterNFaster.Api.Domain/FasterNFaster.Api.Domain.csproj", "FasterNFaster.Api.Domain/"]
COPY ["FasterNFaster.Api.Infrastructure/FasterNFaster.Api.Infrastructure.csproj", "FasterNFaster.Api.Infrastructure/"]

RUN dotnet restore "FasterNFaster.Api.Presentation/FasterNFaster.Api.Presentation.csproj"
COPY . .

WORKDIR "/src/FasterNFaster.Api.Presentation"
RUN dotnet publish "FasterNFaster.Api.Presentation.csproj" -c Release -o /app/publish --no-restore

# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "FasterNFaster.Api.Presentation.dll"]