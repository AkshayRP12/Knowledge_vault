import { useState, useEffect, useRef } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { articlesApi, categoriesApi } from '../api'
import { FiSave, FiArrowLeft, FiAlertCircle, FiBold, FiItalic } from 'react-icons/fi'
import toast from 'react-hot-toast'
import './ArticleFormPage.css'

export default function ArticleFormPage() {
  const { id } = useParams()
  const isEdit = Boolean(id)
  const navigate = useNavigate()
  const editorRef = useRef(null)

  const [categories, setCategories] = useState([])
  const [title, setTitle] = useState('')
  const [categoryId, setCategoryId] = useState('')
  const [tags, setTags] = useState([])
  const [tagInput, setTagInput] = useState('')
  const [loading, setLoading] = useState(false)
  const [fetching, setFetching] = useState(isEdit)

  useEffect(() => {
    categoriesApi.getAll().then(res => setCategories(res.data)).catch(() => {})

    if (isEdit) {
      articlesApi.getById(id).then(res => {
        const a = res.data
        setTitle(a.title)
        setCategoryId(a.categoryId || '')
        setTags(a.tags || [])
        if (editorRef.current) {
          editorRef.current.innerHTML = a.content || ''
        }
      }).catch(() => toast.error('Failed to load article'))
      .finally(() => setFetching(false))
    }
  }, [id, isEdit])

  const handleAddTag = (tag) => {
    const trimmed = tag.trim()
    if (trimmed && !tags.includes(trimmed)) {
      setTags(t => [...t, trimmed])
    }
    setTagInput('')
  }

  const handleTagKeyDown = (e) => {
    if (e.key === 'Enter' || e.key === ',') {
      e.preventDefault()
      handleAddTag(tagInput)
    }
  }

  const handleRemoveTag = (tagToRemove) => {
    setTags(t => t.filter(x => x !== tagToRemove))
  }

  // Real WYSIWYG ExecCommand Formatting
  const handleFormat = (command) => {
    document.execCommand(command, false, null)
    if (editorRef.current) editorRef.current.focus()
  }

  const handleSubmit = async (e) => {
    e.preventDefault()
    const contentHtml = editorRef.current ? editorRef.current.innerHTML : ''

    if (!title.trim() || !contentHtml.trim() || contentHtml === '<br>') {
      return toast.error('Title and Content are required')
    }

    setLoading(true)
    try {
      const payload = {
        title,
        content: contentHtml,
        categoryId: categoryId ? Number(categoryId) : null,
        tags
      }

      if (isEdit) {
        await articlesApi.update(id, payload)
        toast.success('Article updated!')
        navigate(`/articles/${id}`)
      } else {
        const res = await articlesApi.create(payload)
        toast.success(res.data.status === 'Approved' ? 'Article published!' : 'Article submitted for Admin approval!')
        navigate('/')
      }
    } catch (err) {
      toast.error(err.response?.data?.message || 'Failed to save article')
    } finally {
      setLoading(false)
    }
  }

  if (fetching) return <div className="loading-spinner" />

  return (
    <div className="main-content">
      <div className="container-sm">
        <button className="btn btn-ghost mb-4" onClick={() => navigate(-1)}>
          <FiArrowLeft size={16} /> Back
        </button>

        <div className="page-header">
          <h1>{isEdit ? 'Edit Article' : 'Write New Article'}</h1>
          <p>{isEdit ? 'Update your knowledge post' : 'Share internal technical documentation with your team'}</p>
        </div>

        <form onSubmit={handleSubmit} className="card article-form">
          <div className="form-notice">
            <FiAlertCircle size={18} />
            <span>Articles written by employees require <strong>Admin approval</strong> before appearing publicly on the team feed.</span>
          </div>

          <div className="form-group">
            <label className="form-label">Article Title *</label>
            <input
              className="input"
              placeholder="e.g. Getting Started with Azure App Service"
              value={title}
              onChange={e => setTitle(e.target.value)}
              required
            />
          </div>

          <div className="form-group">
            <label className="form-label">Category</label>
            <select className="select" value={categoryId} onChange={e => setCategoryId(e.target.value)}>
              <option value="">Select Category...</option>
              {categories.map(c => (
                <option key={c.id} value={c.id}>{c.name}</option>
              ))}
            </select>
          </div>

          <div className="form-group">
            <div className="flex justify-between items-center mb-1">
              <label className="form-label">Content *</label>
              {/* WYSIWYG Formatting Toolbar */}
              <div className="format-toolbar flex gap-1">
                <button
                  type="button"
                  className="btn btn-secondary btn-sm"
                  title="Make Selected Text Bold"
                  onMouseDown={(e) => { e.preventDefault(); handleFormat('bold'); }}
                >
                  <FiBold size={15} /> <strong>Bold</strong>
                </button>
                <button
                  type="button"
                  className="btn btn-secondary btn-sm"
                  title="Make Selected Text Italic"
                  onMouseDown={(e) => { e.preventDefault(); handleFormat('italic'); }}
                >
                  <FiItalic size={15} /> <em>Italic</em>
                </button>
              </div>
            </div>
            
            {/* Visual ContentEditable Rich Text Editor */}
            <div
              ref={editorRef}
              contentEditable
              className="textarea article-wysiwyg-editor"
              placeholder="Write your article content here... Select any text and click Bold or Italic!"
              style={{
                minHeight: '220px',
                padding: '14px',
                outline: 'none',
                overflowY: 'auto',
                backgroundColor: 'var(--color-bg-card)',
                color: 'var(--text-primary)',
                lineHeight: '1.6'
              }}
            />
          </div>

          <div className="form-group">
            <label className="form-label">Tags</label>
            <div className="tag-input-container">
              {tags.map(t => (
                <span key={t} className="tag-chip">
                  {t}
                  <button type="button" onClick={() => handleRemoveTag(t)}>×</button>
                </span>
              ))}
              <input
                className="tag-chip-input"
                placeholder="Add tag and press Enter..."
                value={tagInput}
                onChange={e => setTagInput(e.target.value)}
                onKeyDown={handleTagKeyDown}
              />
            </div>
            <div className="tag-suggestions">
              {['Azure', 'React', 'SQL', 'HR', '.NET', 'Docker', 'API'].map(t => (
                !tags.includes(t) && (
                  <button key={t} type="button" className="tag-suggestion-btn" onClick={() => handleAddTag(t)}>
                    + {t}
                  </button>
                )
              ))}
            </div>
          </div>

          <div className="form-actions">
            <button type="button" className="btn btn-secondary" onClick={() => navigate(-1)}>Cancel</button>
            <button type="submit" className="btn btn-primary" disabled={loading}>
              <FiSave size={16} /> {loading ? 'Saving...' : isEdit ? 'Update Article' : 'Submit Article'}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}
