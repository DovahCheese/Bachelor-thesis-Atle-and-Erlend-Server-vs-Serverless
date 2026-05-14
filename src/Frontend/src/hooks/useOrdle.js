import { useState, useEffect, useCallback } from 'react';
import apiClient from '../api/client';
import { useUser } from '../context/UserContext';

const WORD_LENGTH = 5;
const MAX_GUESSES = 6;
const RANDOM_GAME_ID_KEY = 'ordle_random_gameId';

export function getAnonymousId() {
    let id = localStorage.getItem('ordle_anonymous_id');
    if (!id) { id = crypto.randomUUID(); localStorage.setItem('ordle_anonymous_id', id); }
    return id;
}

export function useOrdle(mode, gameKey = 0, skip = false) {
    const { user } = useUser();
    const [gameId, setGameId]        = useState(null);
    const [guesses, setGuesses]      = useState([]);
    const [input, setInput]          = useState('');
    const [isOver, setIsOver]        = useState(false);
    const [isWon, setIsWon]          = useState(false);
    const [answer, setAnswer]        = useState(null);
    const [error, setError]          = useState(null);
    const [loading, setLoading]      = useState(true);
    const [shake, setShake]          = useState(false);
    const [revealRow, setRevealRow]  = useState(-1);
    const [startTime, setStartTime]  = useState(null);
    const [justFinished, setJustFinished] = useState(false);

    const reset = useCallback(() => {
        setGuesses([]);
        setInput('');
        setIsOver(false);
        setIsWon(false);
        setAnswer(null);
        setError(null);
        setRevealRow(-1);
        setStartTime(null);
        setJustFinished(false);
    }, []);

    useEffect(() => {
        if (skip) { setLoading(false); return; }
        let cancelled = false;
        setLoading(true);
        reset();

        const loadGame = async () => {
            try {
                let state;

                if (mode.startsWith('random')) {
                    const sessionId = user?.id ?? getAnonymousId();
                    const storageKey = `${RANDOM_GAME_ID_KEY}_${sessionId}`;
                    const savedId = localStorage.getItem(storageKey);
                    state = await apiClient.getRandomGame(savedId, sessionId);

                    if (savedId !== state.gameId) {
                        localStorage.setItem(storageKey, state.gameId);
                    }
                } else {
                    state = await apiClient.getDaily(user?.id ?? getAnonymousId());
                }

                console.log('Game state from server:', state); // Debug log

                if (cancelled) return;

                setStartTime(Date.now());
                setGameId(state.gameId);
                setIsOver(state.isOver);
                setIsWon(state.isWon);
                if (state.answer) setAnswer(state.answer);
                if (state.previousGuesses?.length) {
                    setGuesses(state.previousGuesses);
                    setRevealRow(state.previousGuesses.length);
                }
            } catch (err) {
                if (!cancelled) setError(err.message);
            } finally {
                if (!cancelled) setLoading(false);
            }
        };

        loadGame();
        return () => { cancelled = true; };
    }, [mode, gameKey, user?.id, reset, skip]);

    const triggerShake = useCallback(() => {
        setShake(true);
        setTimeout(() => setShake(false), 600);
    }, []);

    const addLetter = useCallback((letter) => {
        if (isOver) return;
        setInput((p) => p.length < WORD_LENGTH ? p + letter.toUpperCase() : p);
    }, [isOver]);

    const removeLetter = useCallback(() => {
        setInput((p) => p.slice(0, -1));
    }, []);

    const submitCurrentGuess = useCallback(async () => {
        if (isOver || !gameId) return;
        if (input.length !== WORD_LENGTH) {
            triggerShake();
            return;
        }
        setError(null);
        try {
            const result = await apiClient.submitGuess(gameId, input);
            const newRow = guesses.length;
            setGuesses((prev) => [...prev, result]);
            setInput('');
            setRevealRow(newRow);

            const flipDuration = WORD_LENGTH * 80 + 500;
            setTimeout(() => {
                const over = result.isCorrect || result.guessesUsed >= MAX_GUESSES;
                setIsOver(over);
                setIsWon(result.isCorrect);
                if (over) setJustFinished(true);
                if (over && result.answer) setAnswer(result.answer);
                setRevealRow(newRow + 1);
            }, flipDuration);
        } catch (err) {
            setError(err.message);
            triggerShake();
        }
    }, [isOver, gameId, input, guesses.length, triggerShake]);

    // Physical keyboard
    useEffect(() => {
        const handler = (e) => {
            if (e.metaKey || e.ctrlKey || e.altKey) return;
            const tag = document.activeElement?.tagName;
            if (tag === 'INPUT' || tag === 'TEXTAREA') return;
            if (e.key === 'Enter' && tag === 'BUTTON') return;
            if (e.key === 'Enter')     { submitCurrentGuess(); return; }
            if (e.key === 'Backspace') { removeLetter();        return; }
            if (/^[a-zA-ZæøåÆØÅ]$/.test(e.key)) addLetter(e.key);
        };
        window.addEventListener('keydown', handler);
        return () => window.removeEventListener('keydown', handler);
    }, [addLetter, removeLetter, submitCurrentGuess]);

    const RANK = { Correct: 3, Present: 2, Absent: 1 };
    const letterStates = guesses.reduce((acc, guess) => {
        guess.letters.forEach(({ letter, result }) => {
            if ((RANK[result] ?? 0) > (RANK[acc[letter]] ?? 0)) acc[letter] = result;
        });
        return acc;
    }, {});

    return {
        gameId, guesses, input, isOver, isWon, answer,
        error, loading, shake, revealRow, startTime, justFinished,
        letterStates,
        wordLength: WORD_LENGTH,
        maxGuesses: MAX_GUESSES,
        addLetter, removeLetter, submitCurrentGuess,
    };
}