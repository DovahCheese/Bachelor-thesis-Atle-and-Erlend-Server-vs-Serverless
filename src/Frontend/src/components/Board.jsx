import { memo } from 'react';
import styles from './Board.module.css';

const STATE = { Correct: styles.correct, Present: styles.present, Absent: styles.absent };

function Tile({ letter = '', state = null, isRevealing = false, isRevealed = false }) {
  const colourClass = (isRevealing || isRevealed) && state ? STATE[state] : '';
  return (
    <div
      className={[
        styles.tile,
        letter       ? styles.filled    : '',
        isRevealing  ? styles.flipping  : '',
        isRevealed   ? styles.revealed  : '',
        colourClass,
      ].filter(Boolean).join(' ')}
    >
      <span className={styles.front}>{letter}</span>
      <span className={styles.back}>{letter}</span>
    </div>
  );
}

const Row = memo(function Row({ letters, input, isActive, isRevealing, isRevealed, shake, wordLength }) {
  const cls = [styles.row, shake ? styles.shake : ''].filter(Boolean).join(' ');

  if (isActive) {
    return (
      <div className={cls}>
        {Array.from({ length: wordLength }, (_, i) => (
          <Tile key={i} letter={input?.[i] ?? ''} />
        ))}
      </div>
    );
  }

  if (letters) {
    return (
      <div className={cls}>
        {letters.map((lg, i) => (
          <Tile
            key={i}
            letter={lg.letter}
            state={lg.result}
            isRevealing={isRevealing}
            isRevealed={isRevealed}
          />
        ))}
      </div>
    );
  }

  return (
    <div className={cls}>
      {Array.from({ length: wordLength }, (_, i) => <Tile key={i} />)}
    </div>
  );
});

export function Board({ guesses, input, shake, revealRow, wordLength, maxGuesses }) {
  const emptyCount = Math.max(0, maxGuesses - guesses.length - 1);

  return (
    <div className={styles.board} style={{ '--cols': wordLength }}>
      {guesses.map((g, i) => (
        <Row
          key={i}
          letters={g.letters}
          isRevealing={i === revealRow}
          isRevealed={i < revealRow}
          wordLength={wordLength}
        />
      ))}
      {guesses.length < maxGuesses && (
        <Row isActive input={input} shake={shake} wordLength={wordLength} />
      )}
      {Array.from({ length: emptyCount }, (_, i) => (
        <Row key={`e${i}`} wordLength={wordLength} />
      ))}
    </div>
  );
}
