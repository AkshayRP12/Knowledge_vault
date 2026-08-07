import { useState, useEffect } from 'react'
import { useParams, useNavigate, Link } from 'react-router-dom'
import { articlesApi, likesApi, bookmarksApi } from '../api'
import { useAuth } from '../context/AuthContext'
import CommentSection from '../components/CommentSection'
import { FiHeart, FiBookmark, FiEdit, FiTrash2, FiArrowLeft, FiTag, FiUser, FiCalendar, FiClock } from 'react-icons/fi'
import toast from 'react-hot-toast'
import './ArticleDetailPage.css'
import '../components/CommentSection.css'

export default function ArticleDetailPage() {
  const { id } = useParams()
  const { user, isAdmin } = useAuth()
  const navigate = useNavigate()
  const [article, setArticle] = useState(null)
  const [loading, setLoading] = useState(true)
  const [liked, setLiked] = useState(false)
  const [likeCount, setLikeCount] = useState(0)
  const [bookmarked, setBookmarked] = useState(false)

  useEffect(() => {
    articlesApi.getById(id).then(res => {
      setArticle(res.data)
      setLiked(res.data.isLikedByUser)
      setLikeCount(res.data.likeCount)
      setBookmarked(res.data.isBookmarkedByUser)
    }).catch(() => {
      toast.error('Article not found')
      navigate('/')
    }).finally(() => setLoading(false))
  }, [id])

  const handleLike = async () => {
    try {
      await likesApi.toggle(id)
      setLiked(v => !v)
      setLikeCount(c => liked ? c - 1 : c + 1)
    } catch { toast.error('Failed to toggle like') }
  }

  const handleBookmark = async () => {
    try {
      await bookmarksApi.toggle(id)
      setBookmarked(v => !v)
      toast.success(bookmarked ? 'Removed from bookmarks' : 'Bookmarked!')
    } catch { toast.error('Failed to update bookmark') }
  }

  const handleDelete = async () => {
    if (!confirm('Delete this article?')) return
    try {
      await articlesApi.delete(id)
      toast.success('Article deleted')
      navigate('/')
    } catch { toast.error('Failed to delete') }
  }

  const formatDate = (d) => new Date(d).toLocaleDateString('en-US', { year: 'numeric', month: 'long', day: 'numeric' })
  const readTime = (text) => Math.max(1, Math.ceil((text || '').split(' ').length / 200))

  if (loading) return <div className="loading-spinner" style={{ marginTop: 80 }} />

  if (!article) return null

  const canEdit = user?.id === article.authorId || isAdmin
  const statusColors = { Approved: 'badge-success', Pending: 'badge-warning', Rejected: 'badge-danger' }

  return (
    <div className="main-content">
      <div className="container-sm">
        {/* Back */}
        <Link to="/" className="back-link">
          <FiArrowLeft size={16} /> Back to articles
        </Link>

        {/* Article Header */}
        <article className="article-detail">
          <header className="article-detail-header">
            <div className="flex items-center gap-2 flex-wrap">
              {article.categoryName && (
                <span className="badge badge-accent"><FiTag size={10} />{article.categoryName}</span>
              )}
              <span className={`badge ${statusColors[article.status] || 'badge-gray'}`}>{article.status}</span>
            </div>

            <h1 className="article-detail-title">{article.title}</h1>

            {article.tags?.length > 0 && (
              <div className="flex flex-wrap gap-2 mt-3">
                {article.tags.map(t => <span key={t} className="badge badge-gray">{t}</span>)}
              </div>
            )}

            <div className="article-detail-meta">
              <div className="flex items-center gap-2">
                <div className="avatar avatar-lg">{article.authorName?.[0]?.toUpperCase()}</div>
                <div>
                  <div className="meta-author">{article.authorName}</div>
                  <div className="meta-date">
                    <FiCalendar size={12} /> {formatDate(article.createdAt)}
                    <span className="meta-dot">·</span>
                    <FiClock size={12} /> {readTime(article.content)} min read
                  </div>
                </div>
              </div>

              <div className="article-detail-actions">
                <button className={`action-btn ${liked ? 'action-btn-liked' : ''}`} onClick={handleLike}>
                  <FiHeart size={18} fill={liked ? 'currentColor' : 'none'} />
                  <span>{likeCount}</span>
                </button>
                <button className={`action-btn ${bookmarked ? 'action-btn-bookmarked' : ''}`} onClick={handleBookmark}>
                  <FiBookmark size={18} fill={bookmarked ? 'currentColor' : 'none'} />
                </button>
                {canEdit && (
                  <>
                    <Link to={`/articles/${id}/edit`} className="btn btn-secondary btn-sm">
                      <FiEdit size={14} /> Edit
                    </Link>
                    <button className="btn btn-danger btn-sm" onClick={handleDelete}>
                      <FiTrash2 size={14} /> Delete
                    </button>
                  </>
                )}
              </div>
            </div>
          </header>

          <hr className="divider" />

          {/* Content */}
          <div className="article-content prose">
            {article.content?.split('\n').map((para, i) =>
              para.trim() ? <p key={i}>{para}</p> : <br key={i} />
            )}
          </div>

          <hr className="divider" />

          {/* Comments */}
          <CommentSection articleId={id} comments={article.comments || []} />
        </article>
      </div>
    </div>
  )
}
