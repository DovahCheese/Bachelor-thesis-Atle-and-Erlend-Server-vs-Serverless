import { createContext, useContext, useState } from 'react';

const UserContext = createContext(null);

const STORAGE_KEY = 'ordle_user';

function loadUser() {
  try { return JSON.parse(localStorage.getItem(STORAGE_KEY)) ?? null; }
  catch { return null; }
}

export function UserProvider({ children }) {
  const [user, setUserState] = useState(loadUser);

  const setUser = (u) => {
    setUserState(u);
    if (u) localStorage.setItem(STORAGE_KEY, JSON.stringify(u));
    else localStorage.removeItem(STORAGE_KEY);
  };

  return (
    <UserContext.Provider value={{ user, setUser }}>
      {children}
    </UserContext.Provider>
  );
}

export function useUser() {
  return useContext(UserContext);
}
