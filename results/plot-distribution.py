import json, os
import matplotlib.pyplot as plt

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

fig, ax = make_fig()

bp = ax.boxplot(
    [func_dur, app_dur],
    tick_labels=['Functions', 'App Service'],
    patch_artist=True,
    medianprops=dict(color='#222222', linewidth=2),
    whiskerprops=dict(color='#888888'),
    capprops=dict(color='#888888'),
    flierprops=dict(marker='o', color='#aaaaaa', markersize=3, alpha=0.5),
)
bp['boxes'][0].set_facecolor(C_FUNC + '55')
bp['boxes'][1].set_facecolor(C_APP  + '55')
bp['boxes'][0].set_edgecolor(C_FUNC)
bp['boxes'][1].set_edgecolor(C_APP)

ax.set_title('Baseline — Duration Distribution', fontweight='bold')
ax.set_ylabel('Duration (ms)')
ax.grid(axis='y', color='#eeeeee', zorder=0)

save(fig, 'plot-distribution.png')
