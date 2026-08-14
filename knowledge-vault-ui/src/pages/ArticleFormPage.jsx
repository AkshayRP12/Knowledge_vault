import { useState, useEffect, useRef } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { articlesApi, categoriesApi } from '../api'
import { FiSave, FiArrowLeft, FiTag, FiAlertCircle, FiBold, FiItalic } from 'react-icons/fi'
import toast from 'react-hot-toast'
import './ArticleFormPage.css'

export default function ArticleFormPage() {
  const { id } = useParams()
  const isEdit = Boolean(id)
  const navigate = useNavigate()
  const textareaRef = useRef(null)

  const [categories, setCategories] = useState([])
  const [form, setForm] = useState({ title: '', content: '', categoryId: '', tags: [] })
  const [tagInput, setTagInput] = useState('')
  const [loading, setLoading] = useState(false)
  const [fetching, setFetching] = useState(isEdit)

  useEffect(() => {
    categoriesApi.getAll().then(res => setCategories(res.data)).catch(() => {})

    if (isEdit) {
      articlesApi.getById(id).then(res => {
        const a = res.data
        setForm({
          title: a.title,
          content: a.content,
          categoryId: a.categoryId || '',
          tags: a.tags || []
        })
      }).catch(() => toast.error('Failed to load article'))
      .finally(() => setFetching(false))
    }
  }, [id, isEdit])

  const handleChange = (e) => setForm(f => ({ ...f, [e.target.name]: e.target.value }))

  const handleAddTag = (tag) => {
    const trimmed = tag.trim()
    if (trimmed && !form.tags.includes(trimmed)) {
      setForm(f => ({ ...f, tags: [...f.tags, trimmed] }))
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
    setForm(f => ({ ...f, tags: f.tags.filter(t => t !== tagToRemove) }))
  }

  // Format Helper for Bold & Italic
  const applyFormat = (prefix, suffix) => {
    const textarea = textareaRef.current
    if (!textarea) return

    const start = textarea.selectionStart
    const end = textarea.selectionEnd
    const text = textarea.value

    const selectedText = text.substring(start, end) || 'text'
    const formatted = `${prefix}${selectedText}${suffix}`

    const newContent = text.substring(0, start) + formatted + text.substring(end)
    setForm(f => ({ ...f, content: newContent }))

    setTimeout(() => {
      textarea.focus()
      textarea.setSelectionRange(start + prefix.length, start + prefix.length + selectedText.length)
    }, 0)
  }

  const handleSubmit = async (e) => {
    e.preventDefault()
    if (!form.title.trim() || !form.content.trim()) {
      return toast.error('Title and Content are required')
    }

    setLoading(true)
    try {
      const payload = {
        title: form.title,
        content: form.content,
        categoryId: form.categoryId ? Number(form.categoryId) : null,
        tags: form.tags
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
              name="title"
              className="input"
              placeholder="e.g. Getting Started with Azure App Service"
              value={form.title}
              onChange={handleChange}
              required
            />
          </div>

          <div className="form-group">
            <label className="form-label">Category</label>
            <select name="categoryId" className="select" value={form.categoryId} onChange={handleChange}>
              <option value="">Select Category...</option>
              {categories.map(c => (
                <option key={c.id} value={c.id}>{c.name}</option>
              ))}
            </select>
          </div>

          <div className="form-group">
            <div className="flex justify-between items-center mb-1">
              <label className="form-label">Content *</label>
              {/* Formatting Toolbar */}
              <div className="format-toolbar flex gap-1">
                <button
                  type="button"
                  className="btn btn-ghost btn-sm"
                  title="Format Bold"
                  onClick={() => applyFormat('**', '**')}
                >
                  <FiBold size={15} /> <strong>Bold</strong>
                </button>
                <button
                  type="button"
                  className="btn btn-ghost btn-sm"
                  title="Format Italic"
                  onClick={() => applyFormat('*', '*')}
                >
                  <FiItalic size={15} /> <em>Italic</em>
                </button>
              </div>
            </div>
            <textarea
              ref={textareaRef}
              name="content"
              className="textarea article-textarea"
              placeholder="Write your article content here... Select text and click Bold or Italic to format!"
              value={form.content}
              onChange={handleChange}
              required
            />
          </div>

          <div className="form-group">
            <label className="form-label">Tags</label>
            <div className="tag-input-container">
              {form.tags.map(t => (
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
                !form.tags.includes(t) && (
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
