import { Link } from 'react-router-dom'
import { FiHeart, FiBookmark, FiCalendar, FiTag } from 'react-icons/fi'

export default function ArticleCard({ article, onBookmarkToggle, isBookmarked }) {
  const statusColors = {
    Approved: 'badge-success',
    Pending: 'badge-warning',
    Rejected: 'badge-danger',
  }

  const formatDate = (dateStr) =>
    new Date(dateStr).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' })

  const getCleanExcerpt = (html = '') => {
    if (!html) return ''
    const tmp = document.createElement('div')
    tmp.innerHTML = html
    const plain = tmp.textContent || tmp.innerText || ''
    return plain.length > 130 ? plain.substring(0, 130) + '...' : plain
  }

  return (
    <div className="article-card card">
      <div className="article-card-header">
        <div className="flex items-center gap-2 flex-wrap">
          {article.categoryName && (
            <span className="badge badge-accent">
              <FiTag size={10} />
              {article.categoryName}
            </span>
          )}
          {article.status && (
            <span className={`badge ${statusColors[article.status] || 'badge-gray'}`}>
              {article.status}
            </span>
          )}
        </div>
      </div>

      <Link to={`/articles/${article.id}`} className="article-card-title">
        {article.title}
      </Link>

      {article.tags && article.tags.length > 0 && (
        <div className="article-tags flex flex-wrap gap-1 mt-2">
          {article.tags.slice(0, 4).map(tag => (
            <span key={tag} className="badge badge-gray">{tag}</span>
          ))}
        </div>
      )}

      <p className="article-excerpt">{getCleanExcerpt(article.content || article.excerpt)}</p>

      <div className="article-card-footer">
        <div className="article-meta">
          <div className="flex items-center gap-1" title={article.authorName}>
            <div className="avatar" style={{ width: 24, height: 24, fontSize: '0.625rem' }}>
              {article.authorName?.[0]?.toUpperCase()}
            </div>
            <span>{article.authorName}</span>
          </div>
          <div className="flex items-center gap-1">
            <FiCalendar size={12} />
            <span>{formatDate(article.createdAt)}</span>
          </div>
        </div>

        <div className="article-actions">
          <span className="action-stat">
            <FiHeart size={14} />
            {article.likeCount || 0}
          </span>
          {onBookmarkToggle && (
            <button
              className={`btn btn-icon btn-ghost btn-sm bookmark-btn ${isBookmarked ? 'bookmarked' : ''}`}
              onClick={(e) => { e.preventDefault(); onBookmarkToggle(article.id) }}
              title={isBookmarked ? 'Remove bookmark' : 'Bookmark'}
            >
              <FiBookmark size={15} fill={isBookmarked ? 'currentColor' : 'none'} />
            </button>
          )}
        </div>
      </div>
    </div>
  )
}
