import json, os, csv
from datetime import datetime
import matplotlib.pyplot as plt
import numpy as np

RESULTS = os.path.dirname(__file__)

# ── Helpers ───────────────────────────────────────────────────────────────────

def make_fig():
    fig, ax = plt.subplots(figsize=(9, 5))
    fig.patch.set_facecolor('white')
    ax.set_facecolor('white')
    ax.tick_params(colors='#222222')
    ax.xaxis.label.set_color('#222222')
    ax.yaxis.label.set_color('#222222')
    ax.title.set_color('#222222')
    for spine in ax.spines.values():
        spine.set_edgecolor('#cccccc')
    return fig, ax

def save(fig, name):
    out = os.path.join(RESULTS, name)
    fig.savefig(out, dpi=150, facecolor=fig.get_facecolor(), bbox_inches='tight')
    plt.close(fig)
    print(f'Saved → {out}')

def load_req_times_from_json(path):
    times = []
    with open(path) as f:
        for line in f:
            obj = json.loads(line)
            if obj['type'] == 'Point' and obj['metric'] == 'http_reqs':
                t = datetime.fromisoformat(obj['data']['time'][:19])
                times.append(t.timestamp())
    return sorted(times)

def bucket_rps(times, bucket_s=10):
    if not times:
        return [], []
    t0 = times[0]
    end = times[-1]
    edges = np.arange(t0, end + bucket_s, bucket_s)
    counts, _ = np.histogram(times, bins=edges)
    rps = counts / bucket_s
    centres = (edges[:-1] - t0) / 60
    return centres, rps

def load_from_csv():
    summary_path = os.path.join(RESULTS, 'rampup-summary.csv')
    ts_path      = os.path.join(RESULTS, 'rampup-timeseries.csv')
    summary, timeseries = {}, {}
    with open(summary_path) as f:
        for row in csv.DictReader(f):
            summary[row['backend']] = {k: float(v) if k != 'backend' else v for k, v in row.items()}
    with open(ts_path) as f:
        for row in csv.DictReader(f):
            b = row['backend']
            timeseries.setdefault(b, ([], []))
            timeseries[b][0].append(float(row['minute']))
            timeseries[b][1].append(float(row['rps']))
    return summary, timeseries

# ── Load data — prefer raw JSON, fall back to CSV ────────────────────────────
C_FUNC = '#2e7d4f'
C_APP  = '#1a5fa0'

app_json  = os.path.join(RESULTS, 'rampup-appservice.json')
func_json = os.path.join(RESULTS, 'rampup-functions.json')

if os.path.exists(app_json) and os.path.exists(func_json):
    app_times  = load_req_times_from_json(app_json)
    func_times = load_req_times_from_json(func_json)
    app_x,  app_rps  = bucket_rps(app_times)
    func_x, func_rps = bucket_rps(func_times)
    app_avg  = len(app_times)  / (app_times[-1]  - app_times[0])
    func_avg = len(func_times) / (func_times[-1] - func_times[0])
else:
    summary, timeseries = load_from_csv()
    app_x,  app_rps  = timeseries['App Service']
    func_x, func_rps = timeseries['Functions']
    app_avg  = summary['App Service']['avg_rps']
    func_avg = summary['Functions']['avg_rps']

# ── Plot 1: req/s over time ───────────────────────────────────────────────────
fig, ax = make_fig()

ax.plot(func_x, func_rps, color=C_FUNC, linewidth=2, label=f'Azure Functions (avg {func_avg:.0f} req/s)')
ax.plot(app_x,  app_rps,  color=C_APP,  linewidth=2, label=f'App Service     (avg {app_avg:.0f} req/s)')

ax.axvline(2, color='#aaaaaa', linestyle='--', linewidth=1)
ax.axvline(5, color='#aaaaaa', linestyle='--', linewidth=1)

ax.set_title('Throughput over Time — Ramp-up Scenario (0 → 50 VUs)', fontweight='bold')
ax.set_xlabel('Time (minutes)')
ax.set_ylabel('Requests / second')
ax.legend(facecolor='white', edgecolor='#cccccc', labelcolor='#222222')
ax.grid(axis='y', color='#eeeeee', zorder=0)
ax.set_xlim(left=0)
ax.set_ylim(bottom=0)

# Phase labels placed after limits are finalised, anchored to axes-fraction y
# so they sit just inside the top of the plot and never overlap the title.
from matplotlib.transforms import blended_transform_factory
trans = blended_transform_factory(ax.transData, ax.transAxes)
ax.text(1,   0.95, 'ramp↑', transform=trans, color='#888888', fontsize=8, ha='center', va='top')
ax.text(3.5, 0.95, 'hold',  transform=trans, color='#888888', fontsize=8, ha='center', va='top')
ax.text(5.5, 0.95, 'ramp↓', transform=trans, color='#888888', fontsize=8, ha='center', va='top')

save(fig, 'plot-throughput-timeseries.png')

# ── Plot 2: avg req/s bar chart ───────────────────────────────────────────────
fig, ax = make_fig()

bars = ax.bar(['Azure Functions', 'App Service'], [func_avg, app_avg],
              color=[C_FUNC, C_APP], zorder=3, width=0.5)

for bar in bars:
    ax.text(bar.get_x() + bar.get_width() / 2, bar.get_height() + 0.5,
            f'{bar.get_height():.1f} req/s', ha='center', va='bottom',
            fontsize=11, color='#222222', fontweight='bold')

ax.set_title('Average Throughput — Ramp-up Scenario (0 → 50 VUs)', fontweight='bold')
ax.set_ylabel('Requests / second')
ax.grid(axis='y', color='#eeeeee', zorder=0)
ax.set_ylim(0, max(func_avg, app_avg) * 1.3)
ax.tick_params(axis='x', colors='#222222')

save(fig, 'plot-throughput-avg.png')
