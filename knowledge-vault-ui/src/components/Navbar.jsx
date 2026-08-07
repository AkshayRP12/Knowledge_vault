import { Link, useNavigate, useLocation } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'
import { FiBookOpen, FiHome, FiPlusCircle, FiShield, FiLogOut, FiBookmark, FiUser } from 'react-icons/fi'
import './Navbar.css'

export default function Navbar() {
  const { user, logout, isAdmin } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()

  const handleLogout = () => {
    logout()
    navigate('/login')
  }

  const isActive = (path) => location.pathname === path

  return (
    <nav className="navbar">
      <div className="container navbar-inner">
        <Link to="/" className="navbar-brand">
          <FiBookOpen size={22} />
          <span>Knowledge<strong>Vault</strong></span>
        </Link>

        <div className="navbar-links">
          <Link to="/" className={`nav-link ${isActive('/') ? 'active' : ''}`}>
            <FiHome size={16} />
            <span>Home</span>
          </Link>
          <Link to="/articles/new" className={`nav-link ${isActive('/articles/new') ? 'active' : ''}`}>
            <FiPlusCircle size={16} />
            <span>Write</span>
          </Link>
          <Link to="/bookmarks" className={`nav-link ${isActive('/bookmarks') ? 'active' : ''}`}>
            <FiBookmark size={16} />
            <span>Saved</span>
          </Link>
          {isAdmin && (
            <Link to="/admin" className={`nav-link nav-link-admin ${isActive('/admin') ? 'active' : ''}`}>
              <FiShield size={16} />
              <span>Admin</span>
            </Link>
          )}
        </div>

        <div className="navbar-user">
          <div className="user-info">
            <div className="avatar">{user?.username?.[0]?.toUpperCase()}</div>
            <div className="user-details">
              <span className="user-name">{user?.username}</span>
              <span className={`user-role ${isAdmin ? 'admin' : ''}`}>{user?.role}</span>
            </div>
          </div>
          <button className="btn btn-ghost btn-icon" onClick={handleLogout} title="Logout">
            <FiLogOut size={18} />
          </button>
        </div>
      </div>
    </nav>
  )
}
