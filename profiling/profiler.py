import json
import pandas as pd
import matplotlib.pyplot as plt
import sys
from pathlib import Path

# =========================================================
# CONFIG
# =========================================================


if len(sys.argv) < 2:
    print("Usage:")
    print("  Single run:   python profiler_graphs.py <csv_file> [phases.json]")
    print("  Overlay mode: python profiler_graphs.py <label1>=<csv1>,<phases1> <label2>=<csv2>,<phases2> ...")
    sys.exit(1)

METRICS = {
    "cpu_user":         "dotnet.process.cpu.time (s / 1 sec)[cpu.mode=user]",
    "cpu_system":       "dotnet.process.cpu.time (s / 1 sec)[cpu.mode=system]",
    "memory":           "dotnet.process.memory.working_set (By)",
    "threadpool_work":  "dotnet.thread_pool.work_item.count ({work_item} / 1 sec)",
    "threadpool_queue": "dotnet.thread_pool.queue.length ({work_item} / 1 sec)",
    "gc_alloc":         "dotnet.gc.heap.total_allocated (By / 1 sec)",
    "jit_methods":      "dotnet.jit.compiled_methods ({method} / 1 sec)",
    "jit_time":         "dotnet.jit.compilation.time (s / 1 sec)",
}

# =========================================================
# OVERLAY MODE — any arg with `=` triggers it
# =========================================================
combined_mode = any("=" in arg for arg in sys.argv[1:])

if combined_mode:
    runs = []
    for arg in sys.argv[1:]:
        if "=" not in arg:
            continue
        label, paths = arg.split("=", 1)
        if "," in paths:
            csv_p, phases_p = paths.split(",", 1)
        else:
            csv_p, phases_p = paths, None
        runs.append((label, csv_p, phases_p))

    out_dir = "graphs_combined"
    Path(out_dir).mkdir(exist_ok=True)

    loaded = []
    for label, csv_p, phases_p in runs:
        rdf = pd.read_csv(csv_p)
        rdf["Timestamp"] = pd.to_datetime(
            rdf["Timestamp"], format="%m/%d/%Y %H:%M:%S"
        ).dt.tz_localize("UTC")
        rdf["Mean/Increment"] = pd.to_numeric(rdf["Mean/Increment"], errors="coerce")
        rdf = rdf.dropna(subset=["Mean/Increment"])

        rphases, rrtts = {}, None
        if phases_p and Path(phases_p).exists():
            with open(phases_p, "r") as f:
                payload = json.load(f)
            rphases = {k: pd.to_datetime(v, utc=True) for k, v in payload.get("phases", {}).items()}
            if payload.get("rtts"):
                rrtts = pd.DataFrame(payload["rtts"])
                rrtts["t"] = pd.to_datetime(rrtts["t"], utc=True)
                rrtts = rrtts.sort_values("t")

        t0 = rphases.get("auth_start", rdf["Timestamp"].min())
        rdf["RelSec"] = (rdf["Timestamp"] - t0).dt.total_seconds()
        rphases_rel = {k: (v - t0).total_seconds() for k, v in rphases.items()}
        if rrtts is not None:
            rrtts["rel_sec"] = (rrtts["t"] - t0).dt.total_seconds()

        rpivot = rdf.pivot_table(
            index="RelSec", columns="Counter Name",
            values="Mean/Increment", aggfunc="mean"
        )
        loaded.append({"label": label, "pivot": rpivot, "phases": rphases_rel, "rtts": rrtts})

    # Average phase markers across runs (auth/race phases line up closely)
    phase_keys = ["auth_start", "auth_end", "join_start", "join_end",
                  "passage_ready", "race_started", "race_ended"]
    avg_phases = {}
    for k in phase_keys:
        vals = [r["phases"][k] for r in loaded if k in r["phases"]]
        if vals:
            avg_phases[k] = sum(vals) / len(vals)

    def draw_phase_markers_rel(ax):
        ymax = ax.get_ylim()[1]
        for name, sec in avg_phases.items():
            ax.axvline(sec, color="red", linestyle="--", alpha=0.4, linewidth=1)
            ax.text(sec, ymax, name, rotation=90,
                    verticalalignment="top", fontsize=8, color="red")

    for graph_name, counter_name in METRICS.items():
        plt.figure(figsize=(12, 6))
        plotted = False
        for r in loaded:
            if counter_name in r["pivot"].columns:
                series = r["pivot"][counter_name]
                if graph_name == "memory":
                    series = series / (1024 * 1024)
                plt.plot(series.index, series, label=r["label"], linewidth=1.5)
                plotted = True
        if not plotted:
            plt.close()
            continue
        plt.title(f"{counter_name} — overlay")
        plt.xlabel("Seconds since auth_start")
        plt.ylabel("MB" if graph_name == "memory" else "Value")
        plt.legend()
        draw_phase_markers_rel(plt.gca())
        plt.tight_layout()
        out = f"{out_dir}/{graph_name}.png"
        plt.savefig(out)
        plt.close()
        print(f"Saved: {out}")

    # RTT p95 overlay
    plt.figure(figsize=(12, 6))
    plotted = False
    for r in loaded:
        if r["rtts"] is None or len(r["rtts"]) == 0:
            continue
        tmp = r["rtts"].copy()
        tmp = tmp.set_index(pd.to_timedelta(tmp["rel_sec"], unit="s"))
        buckets = tmp["ms"].resample("1s").quantile(0.95)
        plt.plot(buckets.index.total_seconds(), buckets.values,
                 label=f"{r['label']} p95", linewidth=1.5)
        plotted = True
    if plotted:
        plt.title("UpdateRaceState RTT p95 (1s buckets) — overlay")
        plt.xlabel("Seconds since auth_start")
        plt.ylabel("RTT (ms)")
        plt.legend()
        draw_phase_markers_rel(plt.gca())
        plt.tight_layout()
        out = f"{out_dir}/rtt_p95.png"
        plt.savefig(out)
        plt.close()
        print(f"Saved: {out}")
    else:
        plt.close()

    print("Done (overlay mode).")
    sys.exit(0)

CSV_FILE = sys.argv[1]
PHASES_FILE = sys.argv[2] if len(sys.argv) >= 3 else None
OUTPUT_DIR = "graphs" + CSV_FILE

Path(OUTPUT_DIR).mkdir(exist_ok=True)

# =========================================================
# LOAD PHASES + RTT (optional)
# =========================================================

phases = {}
rtt_df = None
if PHASES_FILE and Path(PHASES_FILE).exists():
    with open(PHASES_FILE, "r") as f:
        payload = json.load(f)
    phases = {k: pd.to_datetime(v, utc=True) for k, v in payload.get("phases", {}).items()}
    rtts = payload.get("rtts", [])
    if rtts:
        rtt_df = pd.DataFrame(rtts)
        rtt_df["t"] = pd.to_datetime(rtt_df["t"], utc=True)
        rtt_df = rtt_df.set_index("t").sort_index()


def draw_phase_markers(ax):
    for name, ts in phases.items():
        ax.axvline(ts, color="red", linestyle="--", alpha=0.5, linewidth=1)
        ax.text(ts, ax.get_ylim()[1], name, rotation=90,
                verticalalignment="top", fontsize=8, color="red")

# =========================================================
# LOAD DATA
# =========================================================

df = pd.read_csv(CSV_FILE)

# Parse timestamps
df["Timestamp"] = pd.to_datetime(
    df["Timestamp"],
    format="%m/%d/%Y %H:%M:%S"
).dt.tz_localize("UTC")

# Convert metric values to numeric
df["Mean/Increment"] = pd.to_numeric(df["Mean/Increment"], errors="coerce")

# Remove bad rows
df = df.dropna(subset=["Mean/Increment"])

# =========================================================
# CLEAN COUNTER NAMES
# =========================================================

# Optional:
# Remove noisy unit suffixes like:
# "(By)", "({cpu})", "(s / 1 sec)"
#
# This makes graph titles cleaner.

df["CleanCounter"] = (
    df["Counter Name"]
    .str.replace(r"\s*\([^)]*\)", "", regex=True)
)

# =========================================================
# PIVOT DATA
# =========================================================

pivot = df.pivot_table(
    index="Timestamp",
    columns="Counter Name",
    values="Mean/Increment",
    aggfunc="mean"
)

# =========================================================
# IMPORTANT METRICS TO PLOT
# =========================================================

metrics = {
    "cpu_user":
        "dotnet.process.cpu.time (s / 1 sec)[cpu.mode=user]",

    "cpu_system":
        "dotnet.process.cpu.time (s / 1 sec)[cpu.mode=system]",

    "memory":
        "dotnet.process.memory.working_set (By)",

    "threadpool_work":
        "dotnet.thread_pool.work_item.count ({work_item} / 1 sec)",

    "threadpool_queue":
        "dotnet.thread_pool.queue.length ({work_item} / 1 sec)",

    "gc_alloc":
        "dotnet.gc.heap.total_allocated (By / 1 sec)",

    "jit_methods":
        "dotnet.jit.compiled_methods ({method} / 1 sec)",

    "jit_time":
        "dotnet.jit.compilation.time (s / 1 sec)"
}

# =========================================================
# GENERATE INDIVIDUAL GRAPHS
# =========================================================

for graph_name, counter_name in metrics.items():

    if counter_name not in pivot.columns:
        print(f"Skipping missing metric: {counter_name}")
        continue

    plt.figure(figsize=(12, 6))

    plt.plot(
        pivot.index,
        pivot[counter_name],
        linewidth=2
    )

    plt.title(counter_name)
    plt.xlabel("Time")
    plt.ylabel("Value")

    draw_phase_markers(plt.gca())

    plt.xticks(rotation=45)

    plt.tight_layout()

    output_path = f"{OUTPUT_DIR}/{graph_name}.png"

    plt.savefig(output_path)
    plt.close()

    print(f"Saved: {output_path}")

# =========================================================
# COMBINED SERVER LOAD GRAPH
# =========================================================

fig, ax = plt.subplots(figsize=(14, 7))

# CPU USER
if metrics["cpu_user"] in pivot.columns:
    ax.plot(
        pivot.index,
        pivot[metrics["cpu_user"]],
        label="CPU User"
    )

# CPU SYSTEM
if metrics["cpu_system"] in pivot.columns:
    ax.plot(
        pivot.index,
        pivot[metrics["cpu_system"]],
        label="CPU System"
    )

# Thread pool workload
if metrics["threadpool_work"] in pivot.columns:
    ax.plot(
        pivot.index,
        pivot[metrics["threadpool_work"]],
        label="ThreadPool WorkItems"
    )

ax.set_title("Server Load Overview")
ax.set_xlabel("Time")
ax.set_ylabel("Load")

ax.legend()
draw_phase_markers(ax)

plt.xticks(rotation=45)
plt.tight_layout()

combined_path = f"{OUTPUT_DIR}/server_load_overview.png"

plt.savefig(combined_path)
plt.close()

print(f"Saved: {combined_path}")

# =========================================================
# MEMORY GRAPH IN MB
# =========================================================

memory_counter = metrics["memory"]

if memory_counter in pivot.columns:

    memory_mb = pivot[memory_counter] / (1024 * 1024)

    plt.figure(figsize=(12, 6))

    plt.plot(
        pivot.index,
        memory_mb,
        linewidth=2
    )

    plt.title("Working Set Memory (MB)")
    plt.xlabel("Time")
    plt.ylabel("MB")

    draw_phase_markers(plt.gca())

    plt.xticks(rotation=45)

    plt.tight_layout()

    mem_path = f"{OUTPUT_DIR}/memory_mb.png"

    plt.savefig(mem_path)
    plt.close()

    print(f"Saved: {mem_path}")

# =========================================================
# RTT GRAPH (from phases.json)
# =========================================================

if rtt_df is not None and len(rtt_df) > 0:
    # Bucket per second for p50/p95/p99
    buckets = rtt_df["ms"].resample("1s").quantile([0.5, 0.95, 0.99]).unstack()

    plt.figure(figsize=(12, 6))
    plt.plot(buckets.index, buckets[0.5], label="p50", linewidth=1.5)
    plt.plot(buckets.index, buckets[0.95], label="p95", linewidth=1.5)
    plt.plot(buckets.index, buckets[0.99], label="p99", linewidth=1.5)

    plt.title(f"UpdateRaceState RTT (ms, {len(rtt_df)} samples, 1s buckets)")
    plt.xlabel("Time")
    plt.ylabel("RTT (ms)")
    plt.legend()

    draw_phase_markers(plt.gca())

    plt.xticks(rotation=45)
    plt.tight_layout()

    rtt_path = f"{OUTPUT_DIR}/rtt.png"
    plt.savefig(rtt_path)
    plt.close()

    print(f"Saved: {rtt_path}")

print("Done.")