import { useState, useEffect, useRef } from 'react';
import styles from './ProfileModal.module.css';
import { useLang } from '../context/LangContext';
import { useUser } from '../context/UserContext';
import apiClient from '../api/client';

function Section({ title, children }) {
  return (
    <div className={styles.section}>
      <h3 className={styles.sectionTitle}>{title}</h3>
      {children}
    </div>
  );
}

export function ProfileModal({ onClose }) {
  const { t } = useLang();
  const { user, setUser } = useUser();
  const [tab, setTab] = useState('stats');

  // ── Stats ──────────────────────────────────────────────────────────────────
  const [stats, setStats] = useState(null);
  useEffect(() => {
    apiClient.getStats(user.id).then(setStats).catch(() => {});
  }, [user.id]);

  const formatTime = (secs) => {
    if (secs < 60) return `${secs}s`;
    return `${Math.floor(secs / 60)}m ${Math.round(secs % 60)}s`;
  };

  // ── Profile picture ────────────────────────────────────────────────────────
  const fileInputRef = useRef(null);
  const [pictureError, setPictureError] = useState(null);
  const [pictureSuccess, setPictureSuccess] = useState(false);
  const [pictureLoading, setPictureLoading] = useState(false);

  const MAX_PICTURE_BYTES = 5 * 1024 * 1024; // 5 MB — large enough for thesis upload benchmarks

  const handlePictureUpload = async (e) => {
    const file = e.target.files?.[0];
    if (!file) return;
    setPictureError(null);
    setPictureSuccess(false);
    if (file.size > MAX_PICTURE_BYTES) {
      setPictureError(t.pictureTooLarge);
      e.target.value = '';
      return;
    }
    setPictureLoading(true);
    try {
      const updated = await apiClient.uploadPicture(user.id, file);
      setUser(updated);
      setPictureSuccess(true);
    } catch (err) {
      setPictureError(err.message);
    } finally {
      setPictureLoading(false);
      e.target.value = '';
    }
  };

  // ── Change username ────────────────────────────────────────────────────────
  const [usernamePassword, setUsernamePassword] = useState('');
  const [newUsername, setNewUsername] = useState('');
  const [usernameError, setUsernameError] = useState(null);
  const [usernameSuccess, setUsernameSuccess] = useState(false);
  const [usernameLoading, setUsernameLoading] = useState(false);

  const handleUsernameChange = async (e) => {
    e.preventDefault();
    setUsernameError(null);
    setUsernameSuccess(false);
    setUsernameLoading(true);
    try {
      const updated = await apiClient.changeUsername(user.id, usernamePassword, newUsername);
      setUser(updated);
      setNewUsername('');
      setUsernamePassword('');
      setUsernameSuccess(true);
    } catch (err) {
      setUsernameError(err.message);
    } finally {
      setUsernameLoading(false);
    }
  };

  // ── Change password ────────────────────────────────────────────────────────
  const [currentPassword, setCurrentPassword] = useState('');
  const [newPassword, setNewPassword] = useState('');
  const [confirmNewPassword, setConfirmNewPassword] = useState('');
  const [passwordError, setPasswordError] = useState(null);
  const [passwordSuccess, setPasswordSuccess] = useState(false);
  const [passwordLoading, setPasswordLoading] = useState(false);

  const handlePasswordChange = async (e) => {
    e.preventDefault();
    setPasswordError(null);
    setPasswordSuccess(false);
    if (newPassword !== confirmNewPassword) {
      setPasswordError(t.passwordMismatch);
      return;
    }
    setPasswordLoading(true);
    try {
      const updated = await apiClient.changePassword(user.id, currentPassword, newPassword);
      setUser(updated);
      setCurrentPassword('');
      setNewPassword('');
      setConfirmNewPassword('');
      setPasswordSuccess(true);
    } catch (err) {
      setPasswordError(err.message);
    } finally {
      setPasswordLoading(false);
    }
  };

  // ── Delete account ─────────────────────────────────────────────────────────
  const [deletePassword, setDeletePassword] = useState('');
  const [deleteConfirming, setDeleteConfirming] = useState(false);
  const [deleteError, setDeleteError] = useState(null);
  const [deleteLoading, setDeleteLoading] = useState(false);

  const handleDelete = async (e) => {
    e.preventDefault();
    setDeleteError(null);
    setDeleteLoading(true);
    try {
      await apiClient.deleteUser(user.id, deletePassword);
      setUser(null);
      onClose();
    } catch (err) {
      setDeleteError(err.message);
    } finally {
      setDeleteLoading(false);
    }
  };

  return (
    <div className={styles.backdrop} onClick={onClose}>
      <div className={styles.card} onClick={e => e.stopPropagation()}>
        <button className={styles.close} onClick={onClose} aria-label="Lukk">✕</button>
        <h2 className={styles.heading}>{user.username}</h2>

        <div className={styles.tabs}>
          <button
            className={[styles.tab, tab === 'stats' ? styles.activeTab : ''].join(' ')}
            onClick={() => setTab('stats')}
          >
            {t.stats}
          </button>
          <button
            className={[styles.tab, tab === 'settings' ? styles.activeTab : ''].join(' ')}
            onClick={() => setTab('settings')}
          >
            {t.profileSettings}
          </button>
        </div>

        {tab === 'stats' && (
          <div className={styles.tabContent}>
            {!stats ? (
              <p className={styles.muted}>…</p>
            ) : (
              <div className={styles.statsGrid}>
                <p className={styles.statsSub}>{t.statsDaily}</p>
                {stats.daily.gamesPlayed === 0 ? (
                  <p className={styles.muted}>{t.statsNoData}</p>
                ) : (
                  <>
                    <div className={styles.statRow}>
                      <span className={styles.statLabel}>{t.statsGamesPlayed}</span>
                      <span className={styles.statValue}>{stats.daily.gamesPlayed}</span>
                    </div>
                    <div className={styles.statRow}>
                      <span className={styles.statLabel}>{t.statsGamesWon}</span>
                      <span className={styles.statValue}>{stats.daily.gamesWon}</span>
                    </div>
                    <div className={styles.statRow}>
                      <span className={styles.statLabel}>{t.statsAvgGuesses}</span>
                      <span className={styles.statValue}>{stats.daily.averageGuesses}</span>
                    </div>
                    <div className={styles.statRow}>
                      <span className={styles.statLabel}>{t.statsAvgTime}</span>
                      <span className={styles.statValue}>{formatTime(stats.daily.averageTimeSeconds)}</span>
                    </div>
                    <p className={styles.statsSub} style={{ marginTop: 8 }}>{t.statsGuessDistribution}</p>
                    <div className={styles.distGrid}>
                      {[1,2,3,4,5,6].map(n => (
                        <div key={n} className={styles.distCell}>
                          <span className={styles.distNum}>{n}</span>
                          <span className={styles.distCount}>{stats.daily.guessDistribution[n] ?? 0}</span>
                        </div>
                      ))}
                    </div>
                  </>
                )}

                <div className={styles.divider} style={{ margin: '14px 0' }} />

                <p className={styles.statsSub}>{t.statsRandom}</p>
                {stats.random.gamesPlayed === 0 ? (
                  <p className={styles.muted}>{t.statsNoData}</p>
                ) : (
                  <>
                    <div className={styles.statRow}>
                      <span className={styles.statLabel}>{t.statsGamesPlayed}</span>
                      <span className={styles.statValue}>{stats.random.gamesPlayed}</span>
                    </div>
                    <div className={styles.statRow}>
                      <span className={styles.statLabel}>{t.statsWordsSolved}</span>
                      <span className={styles.statValue}>{stats.random.wordsSolved}</span>
                    </div>
                    <div className={styles.statRow}>
                      <span className={styles.statLabel}>{t.statsAvgTime}</span>
                      <span className={styles.statValue}>{formatTime(stats.random.averageTimeSeconds)}</span>
                    </div>
                  </>
                )}
              </div>
            )}
          </div>
        )}

        {tab === 'settings' && (
          <div className={styles.tabContent}>
            <button className={styles.logoutBtn} onClick={() => { setUser(null); onClose(); }}>
              {t.logout}
            </button>

            <div className={styles.divider} />

            <Section title={t.profilePicture}>
              <div className={styles.avatarSection}>
                <button
                  className={styles.avatarUpload}
                  onClick={() => fileInputRef.current.click()}
                  disabled={pictureLoading}
                  aria-label={t.changePicture}
                >
                  {user.profilePictureUrl
                    ? <img src={user.profilePictureUrl} className={styles.avatarImg} alt="" />
                    : <span className={styles.avatarInitial}>{user.username[0].toUpperCase()}</span>
                  }
                  <span className={styles.avatarOverlay}>{pictureLoading ? '…' : '✎'}</span>
                </button>
                <p className={styles.muted}>{t.changePicture}</p>
                <input
                  ref={fileInputRef}
                  type="file"
                  accept="image/*"
                  className={styles.fileInput}
                  onChange={handlePictureUpload}
                />
                {pictureError && <p className={styles.error}>{pictureError}</p>}
                {pictureSuccess && <p className={styles.success}>{t.pictureUploaded}</p>}
              </div>
            </Section>

            <div className={styles.divider} />

            <Section title={t.changeUsername}>
              <form className={styles.form} onSubmit={handleUsernameChange}>
                <div className={styles.field}>
                  <label className={styles.label}>{t.newUsername}</label>
                  <input className={styles.input} type="text" value={newUsername}
                    onChange={e => setNewUsername(e.target.value)} required />
                </div>
                <div className={styles.field}>
                  <label className={styles.label}>{t.currentPasswordConfirm}</label>
                  <input className={styles.input} type="password" value={usernamePassword}
                    onChange={e => setUsernamePassword(e.target.value)} required />
                </div>
                {usernameError && <p className={styles.error}>{usernameError}</p>}
                {usernameSuccess && <p className={styles.success}>{t.changesSaved}</p>}
                <button type="submit" className={styles.submit} disabled={usernameLoading}>
                  {usernameLoading ? '…' : t.save}
                </button>
              </form>
            </Section>

            <div className={styles.divider} />

            <Section title={t.changePassword}>
              <form className={styles.form} onSubmit={handlePasswordChange}>
                <div className={styles.field}>
                  <label className={styles.label}>{t.currentPassword}</label>
                  <input className={styles.input} type="password" value={currentPassword}
                    onChange={e => setCurrentPassword(e.target.value)} required />
                </div>
                <div className={styles.field}>
                  <label className={styles.label}>{t.newPassword}</label>
                  <input className={styles.input} type="password" value={newPassword}
                    onChange={e => setNewPassword(e.target.value)} required />
                </div>
                <div className={styles.field}>
                  <label className={styles.label}>{t.confirmPassword}</label>
                  <input className={styles.input} type="password" value={confirmNewPassword}
                    onChange={e => setConfirmNewPassword(e.target.value)} required />
                </div>
                {passwordError && <p className={styles.error}>{passwordError}</p>}
                {passwordSuccess && <p className={styles.success}>{t.changesSaved}</p>}
                <button type="submit" className={styles.submit} disabled={passwordLoading}>
                  {passwordLoading ? '…' : t.save}
                </button>
              </form>
            </Section>

            <div className={styles.divider} />

            <Section title={t.deleteAccount}>
              {!deleteConfirming ? (
                <button className={styles.deleteBtn} onClick={() => setDeleteConfirming(true)}>
                  {t.deleteAccount}
                </button>
              ) : (
                <form className={styles.form} onSubmit={handleDelete}>
                  <p className={styles.deleteWarning}>{t.deleteWarning}</p>
                  <div className={styles.field}>
                    <label className={styles.label}>{t.currentPasswordConfirm}</label>
                    <input className={styles.input} type="password" value={deletePassword}
                      onChange={e => setDeletePassword(e.target.value)} required autoFocus />
                  </div>
                  {deleteError && <p className={styles.error}>{deleteError}</p>}
                  <div className={styles.deleteActions}>
                    <button type="submit" className={styles.deleteConfirmBtn} disabled={deleteLoading}>
                      {deleteLoading ? '…' : t.confirmDelete}
                    </button>
                    <button type="button" className={styles.cancelBtn}
                      onClick={() => { setDeleteConfirming(false); setDeleteError(null); setDeletePassword(''); }}>
                      {t.cancel}
                    </button>
                  </div>
                </form>
              )}
            </Section>
          </div>
        )}
      </div>
    </div>
  );
}
