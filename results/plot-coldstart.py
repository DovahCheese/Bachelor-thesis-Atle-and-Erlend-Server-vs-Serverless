import json, os, re
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

C_COLD = '#c0392b'
C_WARM = '#2e7d4f'
C_APP  = '#1a5fa0'

# Functions cold-start files: cold-start1.json, cold-start2.json, etc.
func_cold_files = sorted([
    f for f in os.listdir(RESULTS)
    if re.fullmatch(r'cold-start\d+\.json', f)
])

app_cold_path  = os.path.join(RESULTS, 'cold-start-appservice.json')
app_cold_iters = load_metric(app_cold_path, 'http_req_duration') if os.path.exists(app_cold_path) else []

fig, ax = make_fig()

all_deltas   = []
x_offset     = 0
tick_pos     = []
tick_labels  = []

for i, fname in enumerate(func_cold_files):
    iters = load_metric(os.path.join(RESULTS, fname), 'http_req_duration')
    if not iters:
        continue
    cold     = iters[0]
    warm_avg = sum(iters[1:]) / len(iters[1:]) if len(iters) > 1 else 0
    all_deltas.append(cold - warm_avg)

    x_pos = np.array([0, 1]) + i * 2.6
    ax.bar(x_pos[0], cold,     color=C_COLD, label='Functions — Cold' if i == 0 else '', zorder=3)
    ax.bar(x_pos[1], warm_avg, color=C_WARM, label='Functions — Warm' if i == 0 else '', zorder=3)
    ax.text(x_pos[0], cold     + 20, f'{cold:.0f}ms',     ha='center', fontsize=8, color='#222222')
    ax.text(x_pos[1], warm_avg + 20, f'{warm_avg:.0f}ms', ha='center', fontsize=8, color='#222222')
    tick_pos.append((x_pos[0] + x_pos[1]) / 2)
    tick_labels.append(f'Run {i+1}')
    x_offset = i

if app_cold_iters:
    app_avg = sum(app_cold_iters) / len(app_cold_iters)
    gap_x = x_offset * 2.6 + 3.5
    ax.bar(gap_x, app_avg, color=C_APP, label='App Service (no cold start)', zorder=3)
    ax.text(gap_x, app_avg + 20, f'{app_avg:.0f}ms', ha='center', fontsize=8, color='#222222')
    tick_pos.append(gap_x)
    tick_labels.append('App Service')

ax.set_title('Cold-Start: Functions vs App Service', fontweight='bold')
ax.set_ylabel('Duration (ms)')
ax.set_xticks(tick_pos)
ax.set_xticklabels(tick_labels, fontsize=9, color='#888888')
ax.tick_params(axis='x', length=0)
ax.legend(facecolor='white', edgecolor='#cccccc', labelcolor='#222222')
ax.grid(axis='y', color='#eeeeee', zorder=0)
if all_deltas:
    ax.set_xlabel(f'Functions avg cold-start penalty: {sum(all_deltas)/len(all_deltas):.0f} ms',
                  color='#888888', labelpad=10)

save(fig, 'plot-cold-start.png')
