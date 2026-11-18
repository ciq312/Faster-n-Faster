#!/bin/sh
set -eu

socket=/diag/dotnet-diagnostic.sock
rm -f "$socket"
umask 000

case "${PROFILE_COLLECTOR:-counters}" in
  counters)
    /tools/dotnet-counters collect \
      --diagnostic-port "$socket" \
      --refresh-interval 1 \
      --maxHistograms 32 \
      --counters System.Runtime,Microsoft.AspNetCore.Hosting,FasterNFaster.Races \
      --format csv \
      --output /out/counters.csv &
    ;;
  trace)
    /tools/dotnet-trace collect \
      --diagnostic-port "$socket" \
      --profile dotnet-common,dotnet-sampled-thread-time \
      --duration "${PROFILE_TRACE_DURATION:-00:00:00:45}" \
      --output "/out/${PROFILE_TRACE_FILE:-profile.nettrace}" &
    ;;
  *)
    echo "Unknown PROFILE_COLLECTOR: ${PROFILE_COLLECTOR}" >&2
    exit 2
    ;;
esac

collector=$!
until [ -S "$socket" ]; do
  kill -0 "$collector" 2>/dev/null || { wait "$collector"; exit 1; }
  sleep 0.1
done
chmod 777 "$socket"
wait "$collector"
