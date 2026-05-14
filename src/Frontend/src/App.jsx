import { useState, useEffect, useRef } from 'react'
import apiClient from './api/client'
import { LangProvider, useLang } from './context/LangContext'
import { UserProvider, useUser } from './context/UserContext'
import { useOrdle, getAnonymousId } from './hooks/useOrdle'
import { Header }       from './components/Header'
import { Board }        from './components/Board'
import { Keyboard }     from './components/Keyboard'
import { Toast }        from './components/Toast'
import { GameOver }     from './components/GameOver'
import { LoginModal }    from './components/LoginModal'
import { ProfileModal }      from './components/ProfileModal'
import { LeaderboardModal }  from './components/LeaderboardModal'
import './App.css'

const RANDOM_GAME_ID_KEY = 'ordle_random_gameId'

function Game() {
  const { t } = useLang()
  const { user } = useUser()
  const [mode, setMode] = useState('daily')
  const [gameKey, setGameKey] = useState(0)
  const [showGameOver, setShowGameOver] = useState(true)
  const [showLogin, setShowLogin] = useState(false)
  const [showProfile, setShowProfile] = useState(false)
  const [showLeaderboard, setShowLeaderboard] = useState(false)

  const {
    gameId, guesses, input, isOver, isWon, answer,
    error, loading, shake, revealRow, startTime, justFinished,
    letterStates, wordLength, maxGuesses,
    addLetter, removeLetter, submitCurrentGuess,
  } = useOrdle(mode.startsWith('random') ? 'random' : 'daily', gameKey, !user)

  const statRecorded = useRef(false)

  // Pull-to-refresh
  const pullStartY = useRef(0)
  const [pullY, setPullY] = useState(0)
  const [refreshing, setRefreshing] = useState(false)
  const THRESHOLD = 72

  const onTouchStart = (e) => { pullStartY.current = e.touches[0].clientY }
  const onTouchMove  = (e) => {
    const dy = e.touches[0].clientY - pullStartY.current
    if (dy > 0) setPullY(Math.min(dy * 0.5, THRESHOLD))
  }
  const onTouchEnd   = () => {
    if (pullY >= THRESHOLD) {
      setRefreshing(true)
      setTimeout(() => window.location.reload(), 300)
    } else {
      setPullY(0)
    }
  }
  useEffect(() => {
    if (!justFinished || !user || !startTime || statRecorded.current) return
    statRecorded.current = true
    const isRandom = mode.startsWith('random')
    const elapsed = Math.round((Date.now() - startTime) / 1000)
    const timeSeconds = isRandom && !isWon ? 180 : elapsed
    const gameDate = isRandom ? null : new Date().toISOString().slice(0, 10).replace(/-/g, '')
    apiClient.recordStat(user.id, {
      gameType: isRandom ? 'random' : 'daily',
      won: isWon,
      guessesUsed: guesses.length,
      timeSeconds,
      gameDate,
    }).catch(() => {}) // silent — don't interrupt the game
  }, [justFinished])

  const handleModeChange = (m) => {
    document.activeElement?.blur()
    statRecorded.current = false
    setShowGameOver(true)
    setMode(m)
    setGameKey(k => k + 1)
  }

  const clearRandomGameId = () => {
    const sessionId = user?.id ?? getAnonymousId();
    localStorage.removeItem(`${RANDOM_GAME_ID_KEY}_${sessionId}`);
  }

  // Clear the stored random gameId so useOrdle fetches a fresh word
  const handleNewRandom = () => {
    document.activeElement?.blur()
    statRecorded.current = false
    clearRandomGameId()
    setShowGameOver(true)
    setMode('random')
    setGameKey(k => k + 1)
  }

  const handleReset = async () => {
    document.activeElement?.blur()
    statRecorded.current = false
    if (gameId) await apiClient.deleteGame(gameId)
    if (mode.startsWith('random')) clearRandomGameId()
    setShowGameOver(true)
    setGameKey(k => k + 1)
  }

  return (
    <div className="shell" onTouchStart={onTouchStart} onTouchMove={onTouchMove} onTouchEnd={onTouchEnd}>
      <div
        className="pullIndicator"
        style={{ height: pullY, opacity: Math.min(pullY / THRESHOLD, 1) }}
      >
        <div className={`pullSpinner${refreshing || pullY >= THRESHOLD ? ' pullSpinnerActive' : ''}`} />
      </div>
      <Header
        mode={mode.startsWith('random') ? 'random' : 'daily'}
        onModeChange={handleModeChange}
        onLoginClick={() => setShowLogin(true)}
        onProfileClick={() => setShowProfile(true)}
        onLeaderboardClick={() => setShowLeaderboard(true)}
      />

      <main className="main">
        {!user ? (
          <div className="guestWall">
            <p className="guestTitle">{t.guestTitle}</p>
            <p className="guestSub">{t.guestSub}</p>
            <button className="guestLoginBtn" onClick={() => setShowLogin(true)}>
              {t.loginBtn}
            </button>
          </div>
        ) : loading ? (
          <div className="loader">
            <div className="spinner" />
            <span className="loaderText">{t.loading}</span>
          </div>
        ) : (
          <>
            <Board
              guesses={guesses}
              input={input}
              shake={shake}
              revealRow={revealRow}
              wordLength={wordLength}
              maxGuesses={maxGuesses}
            />
            {mode.startsWith('random') && (
              <button
                className="newGameBtn"
                onClick={handleNewRandom}
                style={{ visibility: isOver ? 'visible' : 'hidden', pointerEvents: isOver ? 'auto' : 'none' }}
              >
                {t.newRandom}
              </button>
            )}
            <div className="keyboardWrap">
              <Keyboard
                letterStates={letterStates}
                enterLabel={t.enter}
                deleteLabel={t.delete}
                onLetter={addLetter}
                onDelete={removeLetter}
                onEnter={submitCurrentGuess}
              />
            </div>
          </>
        )}
      </main>

      <Toast message={error} />

      {showLogin && <LoginModal onClose={() => setShowLogin(false)} />}
      {showProfile && <ProfileModal onClose={() => setShowProfile(false)} />}
      {showLeaderboard && <LeaderboardModal onClose={() => setShowLeaderboard(false)} />}

      {justFinished && showGameOver && (
        <GameOver
          isWon={isWon}
          answer={answer}
          guessCount={guesses.length}
          mode={mode.startsWith('random') ? 'random' : 'daily'}
          onNewRandom={handleNewRandom}
          onNewDaily={() => handleModeChange('daily')}
          onReset={handleReset}
          onDismiss={() => setShowGameOver(false)}
        />
      )}
    </div>
  )
}

export default function App() {
  return (
    <LangProvider>
      <UserProvider>
        <Game />
      </UserProvider>
    </LangProvider>
  )
}
