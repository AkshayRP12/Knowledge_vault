import { createContext, useContext, useState, useEffect } from 'react'

const AuthContext = createContext(null)

export function AuthProvider({ children }) {
  const [user, setUser] = useState(null)
  const [token, setToken] = useState(null)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    const storedToken = localStorage.getItem('kv_token')
    const storedUser = localStorage.getItem('kv_user')
    if (storedToken && storedUser) {
      setToken(storedToken)
      setUser(JSON.parse(storedUser))
    }
    setLoading(false)
  }, [])

  const login = (tokenValue, userData) => {
    localStorage.setItem('kv_token', tokenValue)
    localStorage.setItem('kv_user', JSON.stringify(userData))
    setToken(tokenValue)
    setUser(userData)
  }

  const logout = () => {
    localStorage.removeItem('kv_token')
    localStorage.removeItem('kv_user')
    setToken(null)
    setUser(null)
  }

  return (
    <AuthContext.Provider value={{ user, token, login, logout, loading, isAdmin: user?.role === 'Admin' }}>
      {children}
    </AuthContext.Provider>
  )
}

export const useAuth = () => useContext(AuthContext)
