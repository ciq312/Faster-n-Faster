
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src


COPY loadtest.csproj ./
RUN dotnet restore loadtest.csproj


COPY . ./
RUN dotnet publish loadtest.csproj -c Release -o /app --no-restore


FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime
WORKDIR /app
COPY --from=build /app ./

# Args are passed at `docker run` time, e.g.:
#   docker run --rm loadtest --users 100 --server http://host:8080 --wpm 120
ENTRYPOINT ["dotnet", "loadtest.dll"]
CMD ["--help"]
