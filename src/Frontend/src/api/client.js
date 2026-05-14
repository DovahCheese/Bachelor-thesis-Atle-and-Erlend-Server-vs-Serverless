// Base URL can be overridden via VITE_API_URL env var.
// This makes it easy to point the same frontend at either
// the App Service/VM backend or Azure Functions.
const BASE_URL = import.meta.env.VITE_API_URL ?? '/api'

const backendType = BASE_URL.includes('functions') ? 'Serverless (Azure Functions)'
  : BASE_URL.includes('webapi') ? 'Traditional (App Service)'
  : 'Local dev'
console.log(`[Ordle] Backend: ${backendType}`)

// Optional function key for Azure Functions (AuthorizationLevel.Function).
// Set VITE_FUNCTIONS_KEY in .env.local when pointing at a Functions backend.
// Leave unset when pointing at WebApi (App Service) — it has no key auth.
const FUNCTIONS_KEY = import.meta.env.VITE_FUNCTIONS_KEY ?? null
const authHeader = FUNCTIONS_KEY ? { 'x-functions-key': FUNCTIONS_KEY } : {}

const apiClient = {
  async get(path) {
    const response = await fetch(`${BASE_URL}${path}`, { headers: authHeader })
    if (!response.ok) {
      const text = await response.text()
      throw new Error(text || `HTTP ${response.status}`)
    }
    return response.json()
  },

  async put(path, body) {
    const response = await fetch(`${BASE_URL}${path}`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json', ...authHeader },
      body: JSON.stringify(body),
    })
    if (!response.ok) {
      const text = await response.text()
      throw new Error(text || `HTTP ${response.status}`)
    }
    return response.json()
  },

  async post(path, body) {
    const response = await fetch(`${BASE_URL}${path}`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json', ...authHeader },
      body: JSON.stringify(body),
    })
    if (!response.ok) {
      const text = await response.text()
      throw new Error(text || `HTTP ${response.status}`)
    }
    return response.json()
  },

  // ── Game endpoints ────────────────────────────────────────────────────────

  /** Fetch or resume today's daily game. Pass userId for a per-user session. */
  getDaily(userId = null) {
    const query = userId ? `?userId=${userId}` : '';
    return this.get(`/game${query}`)
  },

  /** Start or resume a random game. Pass userId to link it to a user account. */
  getRandomGame(gameId = null, userId = null) {
    const params = new URLSearchParams();
    if (gameId)  params.set('gameId', gameId);
    if (userId)  params.set('userId', userId);
    const query = params.size ? `?${params}` : '';
    return this.get(`/game/random${query}`)
  },

  /**
   * Submit a guess for an active game.
   * @param {string} gameId
   * @param {string} guess
   * @returns {Promise<GuessResult>}
   */
  submitGuess(gameId, guess) {
    return this.post('/game/guess', { gameId, guess })
  },

  // ── User endpoints ────────────────────────────────────────────────────────

  /** Register a new user. Returns UserResponse { id, username, profilePictureUrl }. */
  register(username, password) {
    return this.post('/users/register', { username, password })
  },

  /** Login. Returns UserResponse { id, username, profilePictureUrl }. */
  login(username, password) {
    return this.post('/users/login', { username, password })
  },

  // ── Leaderboard endpoints ─────────────────────────────────────────────────

  getLeaderboardDaily()         { return this.get('/leaderboard/daily') },
  getLeaderboardRandomSolved()  { return this.get('/leaderboard/random/solved') },
  getLeaderboardRandomTime()    { return this.get('/leaderboard/random/time') },

  // ── Stats endpoints ───────────────────────────────────────────────────────

  /** Record a completed game stat for a user. */
  recordStat(id, { gameType, won, guessesUsed, timeSeconds, gameDate = null }) {
    return this.post(`/users/${id}/stats`, { gameType, won, guessesUsed, timeSeconds, gameDate })
  },

  /** Get aggregated stats for a user. */
  getStats(id) {
    return this.get(`/users/${id}/stats`)
  },

  /** Upload a profile picture. Returns updated UserResponse. */
  async uploadPicture(id, file) {
    const form = new FormData();
    form.append('file', file);
    const response = await fetch(`${BASE_URL}/users/${id}/picture`, {
      method: 'POST',
      headers: authHeader,
      body: form,
      // Do NOT set Content-Type — the browser sets it with the correct multipart boundary
    });
    if (!response.ok) {
      const text = await response.text();
      throw new Error(text || `HTTP ${response.status}`);
    }
    return response.json();
  },

  /** Change username. Returns updated UserResponse. */
  changeUsername(id, currentPassword, newUsername) {
    return this.put(`/users/${id}/username`, { currentPassword, newUsername })
  },

  /** Change password. Returns updated UserResponse. */
  changePassword(id, currentPassword, newPassword) {
    return this.put(`/users/${id}/password`, { currentPassword, newPassword })
  },

  /** Delete user account. */
  async deleteUser(id, currentPassword) {
    const response = await fetch(`${BASE_URL}/users/${id}?password=${encodeURIComponent(currentPassword)}`, {
      method: 'DELETE',
      headers: authHeader,
    })
    if (!response.ok) {
      const text = await response.text()
      throw new Error(text || `HTTP ${response.status}`)
    }
  },

  // ── Game endpoints (continued) ────────────────────────────────────────────

  async deleteGame(gameId) {
    const response = await fetch(`${BASE_URL}/game/${gameId}`, { method: 'DELETE', headers: authHeader })
    if (!response.ok) {
      const text = await response.text()
      throw new Error(text || `HTTP ${response.status}`)
    }
  },
}

export default apiClient