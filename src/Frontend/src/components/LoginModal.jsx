import { useState } from 'react';
import styles from './LoginModal.module.css';
import { useLang } from '../context/LangContext';
import { useUser } from '../context/UserContext';
import apiClient from '../api/client';

export function LoginModal({ onClose }) {
  const { t } = useLang();
  const { setUser } = useUser();
  const [tab, setTab] = useState('login');
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [error, setError] = useState(null);
  const [loading, setLoading] = useState(false);

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError(null);

    if (tab === 'register' && password !== confirmPassword) {
      setError(t.passwordMismatch);
      return;
    }

    setLoading(true);
    try {
      const user = tab === 'login'
        ? await apiClient.login(username, password)
        : await apiClient.register(username, password);
      setUser(user);
      onClose();
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  const switchTab = (next) => {
    setTab(next);
    setError(null);
  };

  return (
    <div className={styles.backdrop} onClick={onClose}>
      <div className={styles.card} onClick={e => e.stopPropagation()}>
        <button className={styles.close} onClick={onClose} aria-label="Lukk">✕</button>

        <div className={styles.tabs}>
          <button
            className={[styles.tab, tab === 'login' ? styles.activeTab : ''].join(' ')}
            onClick={() => switchTab('login')}
          >
            {t.login}
          </button>
          <button
            className={[styles.tab, tab === 'register' ? styles.activeTab : ''].join(' ')}
            onClick={() => switchTab('register')}
          >
            {t.register}
          </button>
        </div>

        <form className={styles.form} onSubmit={handleSubmit}>
          <div className={styles.field}>
            <label className={styles.label}>{t.username}</label>
            <input
              className={styles.input}
              type="text"
              autoComplete="username"
              value={username}
              onChange={e => setUsername(e.target.value)}
              required
            />
          </div>
          <div className={styles.field}>
            <label className={styles.label}>{t.password}</label>
            <input
              className={styles.input}
              type="password"
              autoComplete={tab === 'login' ? 'current-password' : 'new-password'}
              value={password}
              onChange={e => setPassword(e.target.value)}
              required
            />
          </div>
          {tab === 'register' && (
            <div className={styles.field}>
              <label className={styles.label}>{t.confirmPassword}</label>
              <input
                className={styles.input}
                type="password"
                autoComplete="new-password"
                value={confirmPassword}
                onChange={e => setConfirmPassword(e.target.value)}
                required
              />
            </div>
          )}
          {tab === 'register' && (
            <p className={styles.warning}>{t.passwordWarning}</p>
          )}
          {error && <p className={styles.error}>{error}</p>}
          <button type="submit" className={styles.submit} disabled={loading}>
            {loading ? '…' : (tab === 'login' ? t.login : t.register)}
          </button>
        </form>
      </div>
    </div>
  );
}
