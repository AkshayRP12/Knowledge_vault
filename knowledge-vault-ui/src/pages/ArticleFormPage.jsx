import { useState, useEffect } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { articlesApi, categoriesApi } from '../api'
import { FiSave, FiArrowLeft, FiX } from 'react-icons/fi'
import toast from 'react-hot-toast'
import './ArticleFormPage.css'

const COMMON_TAGS = ['Azure', 'React', 'SQL', 'HR', '.NET', 'Docker', 'API', 'Security', 'DevOps', 'Python']

export default function ArticleFormPage() {
  const { id } = useParams()
  const isEdit = Boolean(id)
  const navigate = useNavigate()

  const [form, setForm] = useState({ title: '', content: '', categoryId: '', tags: [] })
  const [categories, setCategories] = useState([])
  const [tagInput, setTagInput] = useState('')
  const [loading, setLoading] = useState(false)

  useEffect(() => {
    categoriesApi.getAll().then(res => setCategories(res.data))
    if (isEdit) {
      articlesApi.getById(id).then(res => {
        const a = res.data
        setForm({ title: a.title, content: a.content, categoryId: a.categoryId || '', tags: a.tags || [] })
      })
    }
  }, [id])

  const handleChange = (e) => setForm(f => ({ ...f, [e.target.name]: e.target.value }))

  const addTag = (tag) => {
    const t = tag.trim()
    if (t && !form.tags.includes(t)) setForm(f => ({ ...f, tags: [...f.tags, t] }))
    setTagInput('')
  }

  const removeTag = (tag) => setForm(f => ({ ...f, tags: f.tags.filter(t => t !== tag) }))

  const handleTagKeyDown = (e) => {
    if (e.key === 'Enter' || e.key === ',') { e.preventDefault(); addTag(tagInput) }
  }

  const handleSubmit = async (e) => {
    e.preventDefault()
    if (!form.title.trim() || !form.content.trim()) return toast.error('Title and content are required')
    setLoading(true)
    try {
      const payload = { ...form, categoryId: form.categoryId ? Number(form.categoryId) : null }
      if (isEdit) {
        await articlesApi.update(id, payload)
        toast.success('Article updated!')
        navigate(`/articles/${id}`)
      } else {
        const res = await articlesApi.create(payload)
        toast.success('Article submitted for review!')
        navigate(`/articles/${res.data.id}`)
      }
    } catch (err) {
      toast.error(err.response?.data?.message || 'Failed to save article')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="main-content">
      <div className="container-sm">
        <div className="page-header flex items-center gap-3">
          <button className="btn btn-ghost btn-icon" onClick={() => navigate(-1)}>
            <FiArrowLeft size={20} />
          </button>
          <div>
            <h1>{isEdit ? 'Edit Article' : 'Write New Article'}</h1>
            <p>{isEdit ? 'Update your article' : 'Share your knowledge with the team'}</p>
          </div>
        </div>

        {!isEdit && (
          <div className="form-notice">
            <span>📋</span>
            Articles require Admin approval before being published to the feed.
          </div>
        )}

        <form onSubmit={handleSubmit} className="article-form card">
          <div className="form-group">
            <label className="form-label" htmlFor="title">Article Title *</label>
            <input
              id="title"
              name="title"
              type="text"
              className="input"
              placeholder="Enter a clear, descriptive title..."
              value={form.title}
              onChange={handleChange}
              required
            />
          </div>

          <div className="form-group">
            <label className="form-label" htmlFor="categoryId">Category</label>
            <select id="categoryId" name="categoryId" className="select" value={form.categoryId} onChange={handleChange}>
              <option value="">Select a category</option>
              {categories.map(c => <option key={c.id} value={c.id}>{c.name}</option>)}
            </select>
          </div>

          <div className="form-group">
            <label className="form-label">Tags</label>
            <div className="tag-input-container">
              {form.tags.map(tag => (
                <span key={tag} className="tag-chip">
                  {tag}
                  <button type="button" onClick={() => removeTag(tag)}><FiX size={12} /></button>
                </span>
              ))}
              <input
                type="text"
                className="tag-chip-input"
                placeholder="Add tag, press Enter..."
                value={tagInput}
                onChange={e => setTagInput(e.target.value)}
                onKeyDown={handleTagKeyDown}
              />
            </div>
            <div className="tag-suggestions">
              {COMMON_TAGS.filter(t => !form.tags.includes(t)).map(t => (
                <button key={t} type="button" className="tag-suggestion-btn" onClick={() => addTag(t)}>{t}</button>
              ))}
            </div>
          </div>

          <div className="form-group">
            <label className="form-label" htmlFor="content">Content *</label>
            <textarea
              id="content"
              name="content"
              className="input textarea article-textarea"
              placeholder="Write your article content here... Use blank lines to separate paragraphs."
              value={form.content}
              onChange={handleChange}
              required
            />
            <span className="char-count">{form.content.length} characters · ~{Math.max(1, Math.ceil(form.content.split(' ').length / 200))} min read</span>
          </div>

          <div className="form-actions">
            <button type="button" className="btn btn-secondary" onClick={() => navigate(-1)}>Cancel</button>
            <button type="submit" className="btn btn-primary" disabled={loading}>
              <FiSave size={16} />
              {loading ? 'Saving...' : isEdit ? 'Save Changes' : 'Submit for Review'}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}
