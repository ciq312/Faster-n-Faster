FROM mcr.microsoft.com/dotnet/sdk:10.0 AS tools
RUN dotnet tool install dotnet-counters --tool-path /tools --version 10.0.750501

FROM mcr.microsoft.com/dotnet/runtime:10.0
COPY --from=tools /tools /tools
ENTRYPOINT ["/bin/sh", "-c", "set -eu; socket=/diag/dotnet-diagnostic.sock; rm -f \"$socket\"; umask 000; /tools/dotnet-counters collect --diagnostic-port \"$socket\" --refresh-interval 1 --counters System.Runtime,Microsoft.AspNetCore.Hosting --format csv --output /out/counters.csv & collector=$!; until [ -S \"$socket\" ]; do kill -0 \"$collector\" 2>/dev/null || { wait \"$collector\"; exit 1; }; sleep 0.1; done; chmod 777 \"$socket\"; wait \"$collector\""]
