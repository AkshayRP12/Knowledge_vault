import { useState, useEffect } from 'react'
import { Link } from 'react-router-dom'
import { articlesApi, categoriesApi, usersApi } from '../api'
import { FiShield, FiCheck, FiX, FiTrash2, FiTag, FiUsers, FiFileText, FiPlus } from 'react-icons/fi'
import toast from 'react-hot-toast'
import './AdminPanelPage.css'

export default function AdminPanelPage() {
  const [tab, setTab] = useState('pending')
  const [pending, setPending] = useState([])
  const [categories, setCategories] = useState([])
  const [users, setUsers] = useState([])
  const [loading, setLoading] = useState(true)
  const [newCategory, setNewCategory] = useState({ name: '', description: '' })
  const [showCatForm, setShowCatForm] = useState(false)

  useEffect(() => { loadData() }, [])

  const loadData = async () => {
    setLoading(true)
    try {
      const [pendRes, catRes, userRes] = await Promise.all([
        articlesApi.getPending(),
        categoriesApi.getAll(),
        usersApi.getAll(),
      ])
      setPending(pendRes.data)
      setCategories(catRes.data)
      setUsers(userRes.data)
    } catch { toast.error('Failed to load admin data') }
    finally { setLoading(false) }
  }

  const handleApprove = async (articleId, status) => {
    try {
      await articlesApi.approve(articleId, status)
      setPending(prev => prev.filter(a => a.id !== articleId))
      toast.success(`Article ${status.toLowerCase()}!`)
    } catch { toast.error('Action failed') }
  }

  const handleDeleteUser = async (userId) => {
    if (!confirm('Delete this user?')) return
    try {
      await usersApi.delete(userId)
      setUsers(prev => prev.filter(u => u.id !== userId))
      toast.success('User deleted')
    } catch { toast.error('Failed to delete user') }
  }

  const handleCreateCategory = async (e) => {
    e.preventDefault()
    if (!newCategory.name.trim()) return
    try {
      const res = await categoriesApi.create(newCategory)
      setCategories(prev => [...prev, res.data])
      setNewCategory({ name: '', description: '' })
      setShowCatForm(false)
      toast.success('Category created!')
    } catch { toast.error('Failed to create category') }
  }

  const handleDeleteCategory = async (catId) => {
    if (!confirm('Delete this category?')) return
    try {
      await categoriesApi.delete(catId)
      setCategories(prev => prev.filter(c => c.id !== catId))
      toast.success('Category deleted')
    } catch { toast.error('Failed to delete category') }
  }

  const formatDate = (d) => new Date(d).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' })

  return (
    <div className="main-content">
      <div className="container">
        <div className="page-header">
          <div className="flex items-center gap-3">
            <div className="admin-icon-wrap"><FiShield size={22} /></div>
            <div>
              <h1>Admin Panel</h1>
              <p>Manage articles, categories, and users</p>
            </div>
          </div>
        </div>

        {/* Stats */}
        <div className="admin-stats">
          <div className="stat-card">
            <FiFileText size={24} />
            <div>
              <div className="stat-value">{pending.length}</div>
              <div className="stat-label">Pending Review</div>
            </div>
          </div>
          <div className="stat-card">
            <FiTag size={24} />
            <div>
              <div className="stat-value">{categories.length}</div>
              <div className="stat-label">Categories</div>
            </div>
          </div>
          <div className="stat-card">
            <FiUsers size={24} />
            <div>
              <div className="stat-value">{users.length}</div>
              <div className="stat-label">Users</div>
            </div>
          </div>
        </div>

        {/* Tabs */}
        <div className="tabs">
          <button className={`tab-btn ${tab === 'pending' ? 'active' : ''}`} onClick={() => setTab('pending')}>
            Pending Articles {pending.length > 0 && <span className="tab-badge">{pending.length}</span>}
          </button>
          <button className={`tab-btn ${tab === 'categories' ? 'active' : ''}`} onClick={() => setTab('categories')}>Categories</button>
          <button className={`tab-btn ${tab === 'users' ? 'active' : ''}`} onClick={() => setTab('users')}>Users</button>
        </div>

        {loading ? <div className="loading-spinner" /> : (
          <>
            {/* Pending Articles */}
            {tab === 'pending' && (
              <div className="admin-section">
                {pending.length === 0 ? (
                  <div className="empty-state">
                    <span className="empty-state-icon">✅</span>
                    <h3>All caught up!</h3>
                    <p>No articles pending review</p>
                  </div>
                ) : (
                  <div className="pending-list">
                    {pending.map(article => (
                      <div key={article.id} className="pending-card card">
                        <div className="pending-card-body">
                          <div>
                            <div className="flex items-center gap-2 mb-1">
                              <h3 className="pending-title">
                                <Link to={`/articles/${article.id}`} style={{ color: 'inherit', textDecoration: 'none' }}>
                                  {article.title}
                                </Link>
                              </h3>
                            </div>
                            <div className="pending-meta">
                              <span>by <strong>{article.authorName}</strong></span>
                              <span>·</span>
                              <span>{article.categoryName || 'Uncategorized'}</span>
                              <span>·</span>
                              <span>{formatDate(article.createdAt)}</span>
                            </div>

                            {/* Render HTML Rich Text Content directly */}
                            <div
                              className="pending-excerpt prose"
                              dangerouslySetInnerHTML={{ __html: article.content || article.excerpt }}
                              style={{ marginTop: 8, color: 'var(--text-secondary)' }}
                            />
                          </div>
                          <div className="pending-actions">
                            <Link to={`/articles/${article.id}`} className="btn btn-secondary btn-sm" style={{ display: 'inline-flex', alignItems: 'center', gap: '4px' }}>
                              <FiFileText size={14} /> Read Full
                            </Link>
                            <button className="btn btn-success btn-sm" onClick={() => handleApprove(article.id, 'Approved')}>
                              <FiCheck size={14} /> Approve
                            </button>
                            <button className="btn btn-danger btn-sm" onClick={() => handleApprove(article.id, 'Rejected')}>
                              <FiX size={14} /> Reject
                            </button>
                          </div>
                        </div>
                      </div>
                    ))}
                  </div>
                )}
              </div>
            )}

            {/* Categories */}
            {tab === 'categories' && (
              <div className="admin-section">
                <div className="section-actions">
                  <button className="btn btn-primary btn-sm" onClick={() => setShowCatForm(v => !v)}>
                    <FiPlus size={15} /> Add Category
                  </button>
                </div>

                {showCatForm && (
                  <form className="inline-form card" onSubmit={handleCreateCategory}>
                    <div className="grid-2">
                      <div className="form-group">
                        <label className="form-label">Name *</label>
                        <input className="input" placeholder="e.g. DevOps" value={newCategory.name}
                          onChange={e => setNewCategory(f => ({ ...f, name: e.target.value }))} required />
                      </div>
                      <div className="form-group">
                        <label className="form-label">Description</label>
                        <input className="input" placeholder="Brief description..." value={newCategory.description}
                          onChange={e => setNewCategory(f => ({ ...f, description: e.target.value }))} />
                      </div>
                    </div>
                    <div className="flex gap-2">
                      <button type="submit" className="btn btn-primary btn-sm">Create</button>
                      <button type="button" className="btn btn-secondary btn-sm" onClick={() => setShowCatForm(false)}>Cancel</button>
                    </div>
                  </form>
                )}

                <div className="table-wrapper">
                  <table>
                    <thead>
                      <tr>
                        <th>#</th>
                        <th>Name</th>
                        <th>Description</th>
                        <th>Actions</th>
                      </tr>
                    </thead>
                    <tbody>
                      {categories.map(cat => (
                        <tr key={cat.id}>
                          <td style={{ color: 'var(--text-muted)' }}>{cat.id}</td>
                          <td><span className="badge badge-accent">{cat.name}</span></td>
                          <td style={{ color: 'var(--text-secondary)' }}>{cat.description || '—'}</td>
                          <td>
                            <button className="btn btn-danger btn-sm" onClick={() => handleDeleteCategory(cat.id)}>
                              <FiTrash2 size={13} /> Delete
                            </button>
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              </div>
            )}

            {/* Users */}
            {tab === 'users' && (
              <div className="admin-section">
                <div className="table-wrapper">
                  <table>
                    <thead>
                      <tr>
                        <th>User</th>
                        <th>Email</th>
                        <th>Role</th>
                        <th>Joined</th>
                        <th>Actions</th>
                      </tr>
                    </thead>
                    <tbody>
                      {users.map(u => (
                        <tr key={u.id}>
                          <td>
                            <div className="flex items-center gap-2">
                              <div className="avatar">{u.username?.[0]?.toUpperCase()}</div>
                              <strong>{u.username}</strong>
                            </div>
                          </td>
                          <td style={{ color: 'var(--text-secondary)' }}>{u.email}</td>
                          <td>
                            <span className={`badge ${u.role === 'Admin' ? 'badge-accent' : 'badge-gray'}`}>{u.role}</span>
                          </td>
                          <td style={{ color: 'var(--text-muted)' }}>{formatDate(u.createdAt)}</td>
                          <td>
                            {u.role !== 'Admin' && (
                              <button className="btn btn-danger btn-sm" onClick={() => handleDeleteUser(u.id)}>
                                <FiTrash2 size={13} /> Remove
                              </button>
                            )}
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              </div>
            )}
          </>
        )}
      </div>
    </div>
  )
}
