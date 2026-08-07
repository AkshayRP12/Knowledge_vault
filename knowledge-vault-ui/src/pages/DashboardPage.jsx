import { useState, useEffect } from 'react'
import { Link } from 'react-router-dom'
import { articlesApi, categoriesApi, bookmarksApi } from '../api'
import ArticleCard from '../components/ArticleCard'
import { FiSearch, FiFilter, FiX, FiPlusCircle } from 'react-icons/fi'
import toast from 'react-hot-toast'
import './DashboardPage.css'

export default function DashboardPage() {
  const [articles, setArticles] = useState([])
  const [categories, setCategories] = useState([])
  const [bookmarks, setBookmarks] = useState([])
  const [loading, setLoading] = useState(true)
  const [search, setSearch] = useState('')
  const [selectedCategory, setSelectedCategory] = useState('')
  const [selectedTag, setSelectedTag] = useState('')

  const POPULAR_TAGS = ['Azure', 'React', 'SQL', 'HR', '.NET', 'Docker', 'API', 'Security']

  useEffect(() => {
    Promise.all([
      articlesApi.getAll(),
      categoriesApi.getAll(),
      bookmarksApi.getAll(),
    ]).then(([artRes, catRes, bkRes]) => {
      setArticles(artRes.data)
      setCategories(catRes.data)
      setBookmarks(bkRes.data.map(b => b.articleId))
    }).catch(() => toast.error('Failed to load articles')).finally(() => setLoading(false))
  }, [])

  const handleBookmarkToggle = async (articleId) => {
    try {
      await bookmarksApi.toggle(articleId)
      setBookmarks(prev =>
        prev.includes(articleId) ? prev.filter(id => id !== articleId) : [...prev, articleId]
      )
    } catch { toast.error('Failed to update bookmark') }
  }

  const filtered = articles.filter(a => {
    const matchSearch = !search || a.title.toLowerCase().includes(search.toLowerCase()) || a.content?.toLowerCase().includes(search.toLowerCase())
    const matchCat = !selectedCategory || a.categoryId === Number(selectedCategory)
    const matchTag = !selectedTag || a.tags?.includes(selectedTag)
    return matchSearch && matchCat && matchTag
  })

  const clearFilters = () => { setSearch(''); setSelectedCategory(''); setSelectedTag('') }
  const hasFilters = search || selectedCategory || selectedTag

  return (
    <div className="main-content">
      <div className="container">
        {/* Hero */}
        <div className="dashboard-hero">
          <div>
            <h1>Knowledge Hub</h1>
            <p>Discover, share, and collaborate on internal knowledge</p>
          </div>
          <Link to="/articles/new" className="btn btn-primary">
            <FiPlusCircle size={18} />
            Write Article
          </Link>
        </div>

        {/* Search + Filters */}
        <div className="dashboard-filters card">
          <div className="search-bar">
            <FiSearch size={18} className="search-icon" />
            <input
              id="search-articles"
              type="text"
              className="search-input"
              placeholder="Search articles by title or content..."
              value={search}
              onChange={(e) => setSearch(e.target.value)}
            />
            {search && (
              <button className="search-clear" onClick={() => setSearch('')}><FiX size={16} /></button>
            )}
          </div>

          <div className="filter-row">
            <div className="flex items-center gap-2">
              <FiFilter size={14} style={{ color: 'var(--text-muted)' }} />
              <select
                id="category-filter"
                className="select filter-select"
                value={selectedCategory}
                onChange={(e) => setSelectedCategory(e.target.value)}
              >
                <option value="">All Categories</option>
                {categories.map(c => <option key={c.id} value={c.id}>{c.name}</option>)}
              </select>
            </div>

            <div className="tag-filters">
              {POPULAR_TAGS.map(tag => (
                <button
                  key={tag}
                  className={`tag-filter-btn ${selectedTag === tag ? 'active' : ''}`}
                  onClick={() => setSelectedTag(t => t === tag ? '' : tag)}
                >
                  {tag}
                </button>
              ))}
            </div>

            {hasFilters && (
              <button className="btn btn-ghost btn-sm" onClick={clearFilters}>
                <FiX size={14} /> Clear
              </button>
            )}
          </div>
        </div>

        {/* Article grid */}
        <div className="dashboard-content">
          <div className="results-info">
            {hasFilters ? (
              <span>{filtered.length} result{filtered.length !== 1 ? 's' : ''} found</span>
            ) : (
              <span>{articles.length} article{articles.length !== 1 ? 's' : ''} available</span>
            )}
          </div>

          {loading ? (
            <div className="loading-spinner" />
          ) : filtered.length === 0 ? (
            <div className="empty-state">
              <span className="empty-state-icon">📄</span>
              <h3>No articles found</h3>
              <p>{hasFilters ? 'Try adjusting your filters' : 'Be the first to write an article!'}</p>
              <Link to="/articles/new" className="btn btn-primary mt-4">Write Article</Link>
            </div>
          ) : (
            <div className="articles-grid">
              {filtered.map(article => (
                <ArticleCard
                  key={article.id}
                  article={article}
                  onBookmarkToggle={handleBookmarkToggle}
                  isBookmarked={bookmarks.includes(article.id)}
                />
              ))}
            </div>
          )}
        </div>
      </div>
    </div>
  )
}
