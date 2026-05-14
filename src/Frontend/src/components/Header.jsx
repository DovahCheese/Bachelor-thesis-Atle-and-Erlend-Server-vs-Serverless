import styles from './Header.module.css';
import { useLang } from '../context/LangContext';
import { useUser } from '../context/UserContext';

export function Header({ mode, onModeChange, onLoginClick, onProfileClick, onLeaderboardClick }) {
  const { lang, t, toggle } = useLang();
  const { user, setUser } = useUser();

  return (
    <header className={styles.header}>
      {/* Mode toggle — left side */}
      <div className={styles.modeToggle}>
        <button
          className={[styles.modeBtn, mode === 'daily' ? styles.active : ''].join(' ')}
          onClick={() => onModeChange('daily')}
        >
          {t.daily}
        </button>
        <button
          className={[styles.modeBtn, mode === 'random' ? styles.active : ''].join(' ')}
          onClick={() => onModeChange('random')}
        >
          {t.random}
        </button>
      </div>

      {/* Logo — centre */}
      <h1 className={styles.logo}>{t.title}</h1>

      {/* Right side — leaderboard + language toggle + login/user */}
      <div className={styles.right}>
        <button className={styles.lbBtn} onClick={onLeaderboardClick} aria-label="Leaderboard">
          {t.lbBtn}
        </button>
        <button className={styles.langBtn} onClick={toggle} aria-label="Toggle language">
          <span className={lang === 'no' ? styles.langActive : styles.langInactive}>NO</span>
          <span className={styles.langSep}>/</span>
          <span className={lang === 'en' ? styles.langActive : styles.langInactive}>EN</span>
        </button>
        {user ? (
          <div className={styles.userArea}>
            <button className={styles.avatarBtn} onClick={onProfileClick} aria-label="Profil">
              {user.profilePictureUrl
                ? <img src={user.profilePictureUrl} className={styles.avatarImg} alt="" />
                : <span className={styles.avatarInitial}>{user.username[0].toUpperCase()}</span>
              }
            </button>
            <button className={styles.username} onClick={onProfileClick}>{user.username}</button>
            <button className={styles.logoutBtn} onClick={() => setUser(null)}>
              {t.logout}
            </button>
          </div>
        ) : (
          <button className={styles.loginBtn} onClick={onLoginClick}>
            {t.loginBtn}
          </button>
        )}
      </div>
    </header>
  );
}
