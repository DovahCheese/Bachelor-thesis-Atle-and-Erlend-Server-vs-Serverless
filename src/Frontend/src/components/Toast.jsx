import { useEffect, useRef, useState } from 'react';
import styles from './Toast.module.css';

export function Toast({ message }) {
  const [visible, setVisible]   = useState(false);
  const [text, setText]         = useState('');
  const timerRef = useRef(null);

  useEffect(() => {
    if (!message) return;
    clearTimeout(timerRef.current);
    setText(message);
    setVisible(true);
    timerRef.current = setTimeout(() => setVisible(false), 1800);
    return () => clearTimeout(timerRef.current);
  }, [message]);

  if (!text) return null;

  return (
    <div className={[styles.toast, visible ? styles.in : styles.out].join(' ')}>
      {text}
    </div>
  );
}
