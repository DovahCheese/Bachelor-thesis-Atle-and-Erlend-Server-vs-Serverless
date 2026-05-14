import styles from './Keyboard.module.css';

// Norwegian keyboard — ÆØÅ get their own dedicated row
const ROWS = [
  ['Q','W','E','R','T','Y','U','I','O','P'],
  ['A','S','D','F','G','H','J','K','L'],
  ['ENTER','Z','X','C','V','B','N','M','DEL'],
  ['Æ','Ø','Å'],
];

const STATE_CLS = {
  Correct: styles.correct,
  Present: styles.present,
  Absent:  styles.absent,
};

function Key({ label, state, wide, special, onPress }) {
  return (
    <button
      className={[
        styles.key,
        wide    ? styles.wide    : '',
        special ? styles.special : '',
        state   ? STATE_CLS[state] : '',
      ].filter(Boolean).join(' ')}
      onClick={() => onPress(label)}
      aria-label={label}
    >
      {label}
    </button>
  );
}

export function Keyboard({ letterStates, enterLabel = 'ENTER', deleteLabel = 'DEL', onLetter, onDelete, onEnter }) {
  const handle = (label) => {
    if (label === 'DEL' || label === 'SLETT') onDelete();
    else if (label === 'ENTER')               onEnter();
    else                                       onLetter(label);
  };

  return (
    <div className={styles.keyboard}>
      {ROWS.map((row, ri) => (
        <div key={ri} className={[styles.row, ri === 3 ? styles.accents : ''].join(' ')}>
          {row.map((key) => {
            const isEnter = key === 'ENTER';
            const isDel   = key === 'DEL';
            const display = isEnter ? enterLabel : isDel ? deleteLabel : key;
            return (
              <Key
                key={key}
                label={display}
                state={letterStates[key]}
                wide={isEnter || isDel}
                special={isEnter || isDel}
                onPress={() => handle(key)}
              />
            );
          })}
        </div>
      ))}
    </div>
  );
}
