import json, os
import matplotlib.pyplot as plt
import numpy as np

RESULTS = os.path.dirname(__file__)

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

C_FUNC = '#2e7d4f'
C_APP  = '#1a5fa0'

func_dur = load_metric(os.path.join(RESULTS, 'baseline-functions.json'),  'http_req_duration')
app_dur  = load_metric(os.path.join(RESULTS, 'baseline-appservice.json'), 'http_req_duration')

labels    = ['p50', 'p95', 'p99']
percs     = [50, 95, 99]
func_vals = [percentile(func_dur, p) for p in percs]
app_vals  = [percentile(app_dur,  p) for p in percs]
x, width  = np.arange(len(labels)), 0.35

fig, ax = make_fig()

bars1 = ax.bar(x - width/2, func_vals, width, label='Azure Functions', color=C_FUNC, zorder=3)
bars2 = ax.bar(x + width/2, app_vals,  width, label='App Service',     color=C_APP,  zorder=3)

for bar in list(bars1) + list(bars2):
    ax.text(bar.get_x() + bar.get_width()/2, bar.get_height() + 1,
            f'{bar.get_height():.0f}ms', ha='center', va='bottom',
            fontsize=9, color='#222222')

ax.set_title('Baseline — Latency Comparison', fontweight='bold')
ax.set_ylabel('Duration (ms)')
ax.set_xticks(x)
ax.set_xticklabels(labels)
ax.legend(facecolor='white', edgecolor='#cccccc', labelcolor='#222222')
ax.grid(axis='y', color='#eeeeee', zorder=0)
ax.set_ylim(0, max(max(func_vals), max(app_vals)) * 1.3)

save(fig, 'plot-latency-comparison.png')
