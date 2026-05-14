import json, os
import matplotlib.pyplot as plt
import numpy as np

RESULTS = os.path.dirname(__file__)

# ── Helpers ───────────────────────────────────────────────────────────────────

def load_metric(path, metric):
    values = []
    with open(path) as f:
        for line in f:
            obj = json.loads(line)
            if obj['type'] == 'Point' and obj['metric'] == metric:
                values.append(obj['data']['value'])
    return values

def percentile(values, p):
    s = sorted(values)
    i = int(len(s) * p / 100)
    return s[min(i, len(s) - 1)]

def make_fig():
    fig, ax = plt.subplots(figsize=(7, 5))
    fig.patch.set_facecolor('#0f1012')
    ax.set_facecolor('#1a1c1f')
    ax.tick_params(colors='#e8e9eb')
    ax.xaxis.label.set_color('#e8e9eb')
    ax.yaxis.label.set_color('#e8e9eb')
    ax.title.set_color('#e8e9eb')
    for spine in ax.spines.values():
        spine.set_edgecolor('#2e3036')
    return fig, ax

def save(fig, name):
    out = os.path.join(RESULTS, name)
    fig.savefig(out, dpi=150, facecolor=fig.get_facecolor(), bbox_inches='tight')
    plt.close(fig)
    print(f'Saved → {out}')

# ── Colours ───────────────────────────────────────────────────────────────────
C_FUNC = '#4a7c59'
C_APP  = '#3a6fa0'
C_COLD = '#c0392b'
C_WARM = '#4a7c59'

func_dur = load_metric(os.path.join(RESULTS, 'baseline-functions.json'),  'http_req_duration')
app_dur  = load_metric(os.path.join(RESULTS, 'baseline-appservice.json'), 'http_req_duration')

# ── Plot 1: p50 / p95 / p99 bar chart ────────────────────────────────────────
fig, ax = make_fig()

labels    = ['p50', 'p95', 'p99']
percs     = [50, 95, 99]
func_vals = [percentile(func_dur, p) for p in percs]
app_vals  = [percentile(app_dur,  p) for p in percs]
x, width  = np.arange(len(labels)), 0.35

bars1 = ax.bar(x - width/2, func_vals, width, label='Azure Functions', color=C_FUNC, zorder=3)
bars2 = ax.bar(x + width/2, app_vals,  width, label='App Service',     color=C_APP,  zorder=3)

for bar in list(bars1) + list(bars2):
    ax.text(bar.get_x() + bar.get_width()/2, bar.get_height() + 1,
            f'{bar.get_height():.0f}ms', ha='center', va='bottom',
            fontsize=9, color='#e8e9eb')

ax.set_title('Baseline — Latency Comparison', fontweight='bold')
ax.set_ylabel('Duration (ms)')
ax.set_xticks(x)
ax.set_xticklabels(labels)
ax.legend(facecolor='#2e3036', edgecolor='#52555f', labelcolor='#e8e9eb')
ax.grid(axis='y', color='#2e3036', zorder=0)
ax.set_ylim(0, max(max(func_vals), max(app_vals)) * 1.3)

save(fig, 'plot-latency-comparison.png')

# ── Plot 2: box plot distribution ────────────────────────────────────────────
fig, ax = make_fig()

bp = ax.boxplot(
    [func_dur, app_dur],
    tick_labels=['Functions', 'App Service'],
    patch_artist=True,
    medianprops=dict(color='#e8e9eb', linewidth=2),
    whiskerprops=dict(color='#6b6f7a'),
    capprops=dict(color='#6b6f7a'),
    flierprops=dict(marker='o', color='#6b6f7a', markersize=3, alpha=0.5),
)
bp['boxes'][0].set_facecolor(C_FUNC + '99')
bp['boxes'][1].set_facecolor(C_APP  + '99')
bp['boxes'][0].set_edgecolor(C_FUNC)
bp['boxes'][1].set_edgecolor(C_APP)

ax.set_title('Baseline — Duration Distribution', fontweight='bold')
ax.set_ylabel('Duration (ms)')
ax.grid(axis='y', color='#2e3036', zorder=0)

save(fig, 'plot-distribution.png')

# ── Plot 3: cold starts ───────────────────────────────────────────────────────
fig, ax = make_fig()

cold_files = sorted([
    f for f in os.listdir(RESULTS) if f.startswith('cold-start') and f.endswith('.json')
])

all_deltas = []

for i, fname in enumerate(cold_files):
    iters = load_metric(os.path.join(RESULTS, fname), 'http_req_duration')
    if not iters:
        continue
    cold     = iters[0]
    warm_avg = sum(iters[1:]) / len(iters[1:]) if len(iters) > 1 else 0
    all_deltas.append(cold - warm_avg)

    x_pos = np.array([0, 1]) + i * 2.6
    ax.bar(x_pos[0], cold,     color=C_COLD, label='Cold'     if i == 0 else '', zorder=3)
    ax.bar(x_pos[1], warm_avg, color=C_WARM, label='Warm avg' if i == 0 else '', zorder=3)
    ax.text(x_pos[0], cold     + 20, f'{cold:.0f}ms',     ha='center', fontsize=8, color='#e8e9eb')
    ax.text(x_pos[1], warm_avg + 20, f'{warm_avg:.0f}ms', ha='center', fontsize=8, color='#e8e9eb')
    ax.text((x_pos[0] + x_pos[1]) / 2, -100, f'Run {i+1}', ha='center', fontsize=8, color='#6b6f7a')

ax.set_title('Functions — Cold vs Warm', fontweight='bold')
ax.set_ylabel('Duration (ms)')
ax.set_xticks([])
ax.legend(facecolor='#2e3036', edgecolor='#52555f', labelcolor='#e8e9eb')
ax.grid(axis='y', color='#2e3036', zorder=0)
if all_deltas:
    ax.set_xlabel(f'Avg cold-start penalty: {sum(all_deltas)/len(all_deltas):.0f} ms', color='#6b6f7a')

save(fig, 'plot-cold-start.png')
