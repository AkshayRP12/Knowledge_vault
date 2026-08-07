import { useState, useEffect } from 'react'
import { Link } from 'react-router-dom'
import { bookmarksApi, articlesApi } from '../api'
import ArticleCard from '../components/ArticleCard'
import { FiBookmark } from 'react-icons/fi'
import toast from 'react-hot-toast'

export default function BookmarksPage() {
  const [bookmarks, setBookmarks] = useState([])
  const [articles, setArticles] = useState([])
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    bookmarksApi.getAll().then(res => {
      setBookmarks(res.data.map(b => b.articleId))
      const articleDetails = res.data.map(b => b.article).filter(Boolean)
      setArticles(articleDetails)
    }).catch(() => toast.error('Failed to load bookmarks'))
    .finally(() => setLoading(false))
  }, [])

  const handleBookmarkToggle = async (articleId) => {
    try {
      await bookmarksApi.toggle(articleId)
      setBookmarks(prev => prev.filter(id => id !== articleId))
      setArticles(prev => prev.filter(a => a.id !== articleId))
      toast.success('Bookmark removed')
    } catch { toast.error('Failed to remove bookmark') }
  }

  return (
    <div className="main-content">
      <div className="container">
        <div className="page-header">
          <h1><FiBookmark style={{ display: 'inline', marginRight: 10, color: 'var(--accent)' }} />Saved Articles</h1>
          <p>Articles you've bookmarked for later reading</p>
        </div>

        {loading ? <div className="loading-spinner" /> : articles.length === 0 ? (
          <div className="empty-state">
            <span className="empty-state-icon">🔖</span>
            <h3>No bookmarks yet</h3>
            <p>Bookmark articles to save them for later</p>
            <Link to="/" className="btn btn-primary mt-4">Browse Articles</Link>
          </div>
        ) : (
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(320px, 1fr))', gap: 20 }}>
            {articles.map(article => (
              <ArticleCard
                key={article.id}
                article={article}
                onBookmarkToggle={handleBookmarkToggle}
                isBookmarked={true}
              />
            ))}
          </div>
        )}
      </div>
    </div>
  )
}
