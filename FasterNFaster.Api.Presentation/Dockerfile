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

WORKDIR /src
RUN dotnet tool restore
RUN dotnet tool run dotnet-ef migrations bundle --project FasterNFaster.Api.Infrastructure \
    --startup-project FasterNFaster.Api.Presentation --configuration Release -o /app/publish/efbundle

# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

USER $APP_UID
EXPOSE 8080
HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
    CMD ["/bin/bash", "-ec", "exec 3<>/dev/tcp/127.0.0.1/8080; printf 'GET /api/health HTTP/1.1\\r\\nHost: localhost\\r\\nConnection: close\\r\\n\\r\\n' >&3; read -r version status reason <&3; [[ $version == HTTP/* && $status == 200 ]]"]
ENTRYPOINT ["dotnet", "FasterNFaster.Api.Presentation.dll"]
