import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'
import { authApi } from '../api'
import { FiBookOpen, FiMail, FiLock, FiUser, FiEye, FiEyeOff } from 'react-icons/fi'
import toast from 'react-hot-toast'
import './LoginPage.css'

export default function LoginPage() {
  const [mode, setMode] = useState('login')
  const [form, setForm] = useState({ username: '', email: '', password: '', role: 'Employee' })
  const [loading, setLoading] = useState(false)
  const [showPass, setShowPass] = useState(false)
  const { login } = useAuth()
  const navigate = useNavigate()

  const handleChange = (e) => setForm(f => ({ ...f, [e.target.name]: e.target.value }))

  const handleSubmit = async (e) => {
    e.preventDefault()
    setLoading(true)
    try {
      if (mode === 'login') {
        const res = await authApi.login({ email: form.email, password: form.password })
        login(res.data.token, res.data.user)
        toast.success(`Welcome back, ${res.data.user.username}!`)
        navigate('/')
      } else {
        await authApi.register(form)
        toast.success('Account created! Please sign in.')
        setMode('login')
      }
    } catch (err) {
      toast.error(err.response?.data?.message || 'Invalid email or password')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="login-page">
      <div className="login-card">
        <div className="login-header">
          <div className="login-logo">
            <FiBookOpen size={24} />
          </div>
          <h1>Knowledge<strong>Vault</strong></h1>
          <p>Workplace Knowledge Sharing Platform</p>
        </div>

        <div className="tabs">
          <button className={`tab-btn ${mode === 'login' ? 'active' : ''}`} onClick={() => setMode('login')}>Sign In</button>
          <button className={`tab-btn ${mode === 'register' ? 'active' : ''}`} onClick={() => setMode('register')}>Register</button>
        </div>

        <form onSubmit={handleSubmit} className="login-form">
          {mode === 'register' && (
            <div className="form-group">
              <label className="form-label">Username</label>
              <div className="input-icon-wrap">
                <FiUser className="input-icon" size={15} />
                <input
                  id="username"
                  name="username"
                  type="text"
                  className="input input-with-icon"
                  placeholder="Enter username"
                  value={form.username}
                  onChange={handleChange}
                  required
                />
              </div>
            </div>
          )}

          <div className="form-group">
            <label className="form-label">Email</label>
            <div className="input-icon-wrap">
              <FiMail className="input-icon" size={15} />
              <input
                id="email"
                name="email"
                type="email"
                className="input input-with-icon"
                placeholder="email@company.com"
                value={form.email}
                onChange={handleChange}
                required
              />
            </div>
          </div>

          <div className="form-group">
            <label className="form-label">Password</label>
            <div className="input-icon-wrap">
              <FiLock className="input-icon" size={15} />
              <input
                id="password"
                name="password"
                type={showPass ? 'text' : 'password'}
                className="input input-with-icon input-with-icon-right"
                placeholder="••••••••"
                value={form.password}
                onChange={handleChange}
                required
              />
              <button type="button" className="input-icon-right" onClick={() => setShowPass(v => !v)}>
                {showPass ? <FiEyeOff size={15} /> : <FiEye size={15} />}
              </button>
            </div>
          </div>

          {mode === 'register' && (
            <div className="form-group">
              <label className="form-label">Role</label>
              <select name="role" className="select" value={form.role} onChange={handleChange}>
                <option value="Employee">Employee</option>
                <option value="Admin">Admin</option>
              </select>
            </div>
          )}

          <button type="submit" className="btn btn-primary btn-lg" style={{ width: '100%' }} disabled={loading}>
            {loading ? 'Please wait...' : mode === 'login' ? 'Sign In' : 'Create Account'}
          </button>
        </form>

        <div className="login-demo">
          <p>Demo Credentials</p>
          <div className="demo-creds">
            <div className="demo-cred">
              <span className="badge badge-accent">Admin</span>
              <code>admin@vault.com / Admin@123</code>
            </div>
            <div className="demo-cred">
              <span className="badge badge-gray">Employee</span>
              <code>employee@vault.com / Employee@123</code>
            </div>
          </div>
        </div>
      </div>
    </div>
  )
}
