FROM mcr.microsoft.com/dotnet/sdk:10.0 AS tools
RUN dotnet tool install dotnet-counters --tool-path /tools --version 10.0.750501
RUN dotnet tool install dotnet-trace --tool-path /tools --version 10.0.750501

FROM mcr.microsoft.com/dotnet/runtime:10.0
COPY --from=tools /tools /tools
COPY entrypoint.sh /entrypoint.sh
RUN chmod +x /entrypoint.sh
ENTRYPOINT ["/entrypoint.sh"]
