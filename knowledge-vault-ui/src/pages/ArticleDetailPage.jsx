import { useState, useEffect } from 'react'
import { useParams, Link, useNavigate } from 'react-router-dom'
import { articlesApi, likesApi, bookmarksApi } from '../api'
import { useAuth } from '../context/AuthContext'
import CommentSection from '../components/CommentSection'
import { FiThumbsUp, FiBookmark, FiEdit, FiTrash2, FiClock, FiCalendar, FiUser, FiArrowLeft } from 'react-icons/fi'
import toast from 'react-hot-toast'
import './ArticleDetailPage.css'

export default function ArticleDetailPage() {
  const { id } = useParams()
  const navigate = useNavigate()
  const { user, isAdmin } = useAuth()

  const [article, setArticle] = useState(null)
  const [loading, setLoading] = useState(true)
  const [liked, setLiked] = useState(false)
  const [likeCount, setLikeCount] = useState(0)
  const [bookmarked, setBookmarked] = useState(false)

  useEffect(() => {
    loadArticle()
  }, [id])

  const loadArticle = async () => {
    setLoading(true)
    try {
      const res = await articlesApi.getById(id)
      setArticle(res.data)
      setLiked(res.data.isLikedByUser)
      setLikeCount(res.data.likeCount)
      setBookmarked(res.data.isBookmarkedByUser)
    } catch {
      toast.error('Article not found')
      navigate('/')
    } finally {
      setLoading(false)
    }
  }

  const handleBack = () => {
    if (window.history.length > 1 && window.history.state && window.history.state.idx > 0) {
      navigate(-1)
    } else {
      navigate(isAdmin ? '/admin' : '/')
    }
  }

  const handleLike = async () => {
    try {
      const res = await likesApi.toggle(id)
      setLiked(res.data.liked)
      setLikeCount(c => res.data.liked ? c + 1 : c - 1)
    } catch {
      toast.error('Failed to update like')
    }
  }

  const handleBookmark = async () => {
    try {
      const res = await bookmarksApi.toggle(id)
      setBookmarked(res.data.bookmarked)
      toast.success(res.data.bookmarked ? 'Saved to bookmarks!' : 'Removed from bookmarks')
    } catch {
      toast.error('Failed to update bookmark')
    }
  }

  const handleDelete = async () => {
    if (!confirm('Are you sure you want to delete this article?')) return
    try {
      await articlesApi.delete(id)
      toast.success('Article deleted')
      navigate('/')
    } catch {
      toast.error('Failed to delete article')
    }
  }

  const formatDate = (dateStr) => {
    if (!dateStr) return ''
    return new Date(dateStr).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' })
  }

  const calculateReadTime = (text = '') => {
    const plainText = text.replace(/<[^>]*>?/gm, '')
    const words = plainText.trim().split(/\s+/).length
    return Math.max(1, Math.ceil(words / 200))
  }

  if (loading) return <div className="loading-spinner" />
  if (!article) return null

  const isAuthor = user?.id === article.authorId

  return (
    <div className="main-content">
      <div className="container-sm">
        <button className="btn btn-ghost mb-4" onClick={handleBack}>
          <FiArrowLeft size={16} /> Back
        </button>

        <div className="article-detail card">
          <div className="article-detail-header">
            <div className="flex items-center gap-2 mb-3">
              {article.categoryName && <span className="badge badge-accent">{article.categoryName}</span>}
              <span className={`badge ${article.status === 'Approved' ? 'badge-success' : 'badge-warning'}`}>
                {article.status}
              </span>
            </div>

            <h1 className="article-detail-title">{article.title}</h1>

            <div className="article-detail-meta">
              <div className="meta-item">
                <FiUser size={14} /> <span>{article.authorName}</span>
              </div>
              <div className="meta-item">
                <FiCalendar size={14} /> <span>{formatDate(article.createdAt)}</span>
              </div>
              <div className="meta-item">
                <FiClock size={14} /> <span>{calculateReadTime(article.content)} min read</span>
              </div>
            </div>

            {(isAuthor || isAdmin) && (
              <div className="article-detail-actions flex gap-2 mt-4">
                <Link to={`/articles/${id}/edit`} className="btn btn-secondary btn-sm">
                  <FiEdit size={14} /> Edit
                </Link>
                <button className="btn btn-danger btn-sm" onClick={handleDelete}>
                  <FiTrash2 size={14} /> Delete
                </button>
              </div>
            )}
          </div>

          <div className="divider" />

          {/* HTML Rich Text Body Rendering */}
          <div
            className="prose article-body"
            dangerouslySetInnerHTML={{ __html: article.content }}
          />

          {article.tags && article.tags.length > 0 && (
            <div className="article-detail-tags">
              <span className="tags-label">Tags:</span>
              <div className="flex flex-wrap gap-1">
                {article.tags.map(t => (
                  <span key={t} className="badge badge-gray">#{t}</span>
                ))}
              </div>
            </div>
          )}

          <div className="divider" />

          <div className="article-detail-footer">
            <div className="flex items-center gap-3">
              <button className={`btn ${liked ? 'btn-primary' : 'btn-secondary'}`} onClick={handleLike}>
                <FiThumbsUp size={16} /> {liked ? 'Liked' : 'Like'} ({likeCount})
              </button>
              <button className={`btn ${bookmarked ? 'btn-primary' : 'btn-secondary'}`} onClick={handleBookmark}>
                <FiBookmark size={16} /> {bookmarked ? 'Saved' : 'Save Bookmark'}
              </button>
            </div>
          </div>
        </div>

        <div className="mt-6">
          <CommentSection articleId={id} comments={article.comments || []} onCommentAdded={loadArticle} />
        </div>
      </div>
    </div>
  )
}
