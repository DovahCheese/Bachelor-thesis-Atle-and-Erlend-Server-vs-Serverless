import { useState, useEffect } from 'react';
import styles from './LeaderboardModal.module.css';
import { useLang } from '../context/LangContext';
import apiClient from '../api/client';

const TABS = ['daily', 'randomSolved'];

function formatTime(secs) {
  if (!secs && secs !== 0) return '—';
  if (secs < 60) return `${secs}s`;
  return `${Math.floor(secs / 60)}m ${Math.round(secs % 60)}s`;
}

function Table({ entries, columns }) {
  if (!entries) return <p className={styles.loading}>…</p>;
  if (entries.length === 0) return <p className={styles.empty}>—</p>;
  return (
    <table className={styles.table}>
      <thead>
        <tr>
          <th className={styles.th}>#</th>
          <th className={styles.th}>{columns.username}</th>
          <th className={styles.th}>{columns.primary}</th>
          <th className={styles.th}>{columns.secondary}</th>
          <th className={styles.th}>{columns.games}</th>
        </tr>
      </thead>
      <tbody>
        {entries.map(e => (
          <tr key={e.rank} className={styles.tr}>
            <td className={styles.tdRank}>{e.rank}</td>
            <td className={styles.td}>{e.username}</td>
            <td className={styles.tdNum}>{e.primaryValue != null ? columns.primaryFmt(e.primaryValue) : '—'}</td>
            <td className={styles.tdNum}>{e.secondaryValue != null ? columns.secondaryFmt(e.secondaryValue) : '—'}</td>
            <td className={styles.tdNum}>{e.gamesCount}</td>
          </tr>
        ))}
      </tbody>
    </table>
  );
}

export function LeaderboardModal({ onClose }) {
  const { t } = useLang();
  const [tab, setTab] = useState('daily');
  const [data, setData] = useState({ daily: null, randomSolved: null });

  useEffect(() => {
    if (data[tab] !== null) return;
    const fetchers = {
      daily:        apiClient.getLeaderboardDaily.bind(apiClient),
      randomSolved: apiClient.getLeaderboardRandomSolved.bind(apiClient),
    };
    fetchers[tab]()
      .then(rows => setData(prev => ({ ...prev, [tab]: rows })))
      .catch(() => setData(prev => ({ ...prev, [tab]: [] })));
  }, [tab]);

  const columnDefs = {
    daily: {
      username:     t.lbUsername,
      primary:      t.lbAvgGuesses,
      secondary:    t.lbAvgTime,
      games:        t.lbGamesWon,
      primaryFmt:   v => v,
      secondaryFmt: v => formatTime(v),
    },
    randomSolved: {
      username:     t.lbUsername,
      primary:      t.lbSolveRate,
      secondary:    t.lbWordsSolved,
      games:        t.lbGamesPlayed,
      primaryFmt:   v => `${v}%`,
      secondaryFmt: v => v,
    },
  };

  return (
    <div className={styles.backdrop} onClick={onClose}>
      <div className={styles.card} onClick={e => e.stopPropagation()}>
        <button className={styles.close} onClick={onClose} aria-label="Lukk">✕</button>
        <h2 className={styles.heading}>{t.leaderboard}</h2>

        <div className={styles.tabs}>
          {TABS.map(key => (
            <button
              key={key}
              className={[styles.tab, tab === key ? styles.activeTab : ''].join(' ')}
              onClick={() => setTab(key)}
            >
              {t[`lb_${key}`]}
            </button>
          ))}
        </div>

        <Table entries={data[tab]} columns={columnDefs[tab]} />
      </div>
    </div>
  );
}
