import styles from './GameOver.module.css';
import { useLang } from '../context/LangContext';

export function GameOver({ isWon, answer, guessCount, mode, onNewRandom, onNewDaily, onDismiss }) {
  const { t } = useLang();

  const headline = isWon
    ? t.winMessages[Math.min(guessCount - 1, t.winMessages.length - 1)]
    : t.lossMessage;

  return (
    <div className={styles.backdrop}>
      <div className={styles.card}>
        <button className={styles.close} onClick={onDismiss} aria-label="Lukk">✕</button>
        <p className={styles.headline}>{headline}</p>
        {isWon && <p className={styles.sub}>{t.solvedIn(guessCount)}</p>}
        {!isWon && answer && <p className={styles.answer}>{answer}</p>}

        <div className={styles.actions}>
          {mode === 'random' ? (
            <>
              <button className={styles.primary} onClick={onNewRandom}>
                {t.newRandom}
              </button>
              <button className={styles.secondary} onClick={onNewDaily}>
                {t.backToDaily}
              </button>
            </>
          ) : (
            <>
              <button className={styles.primary} onClick={onNewRandom}>
                {t.newRandom}
              </button>
            </>
          )}
        </div>
      </div>
    </div>
  );
}
