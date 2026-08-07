import '../components/ArticleCard.css'

import { useState } from 'react'
import { useAuth } from '../context/AuthContext'
import { commentsApi } from '../api'
import { FiSend, FiTrash2, FiMessageSquare } from 'react-icons/fi'
import toast from 'react-hot-toast'

export default function CommentSection({ articleId, comments: initialComments }) {
  const { user } = useAuth()
  const [comments, setComments] = useState(initialComments || [])
  const [content, setContent] = useState('')
  const [submitting, setSubmitting] = useState(false)

  const handleSubmit = async (e) => {
    e.preventDefault()
    if (!content.trim()) return
    setSubmitting(true)
    try {
      const res = await commentsApi.create(articleId, { content })
      setComments(prev => [...prev, res.data])
      setContent('')
      toast.success('Comment added!')
    } catch {
      toast.error('Failed to add comment')
    } finally {
      setSubmitting(false)
    }
  }

  const handleDelete = async (commentId) => {
    try {
      await commentsApi.delete(articleId, commentId)
      setComments(prev => prev.filter(c => c.id !== commentId))
      toast.success('Comment deleted')
    } catch {
      toast.error('Failed to delete comment')
    }
  }

  const formatDate = (d) => new Date(d).toLocaleDateString('en-US', { month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit' })

  return (
    <div className="comment-section">
      <h3 className="comment-section-title">
        <FiMessageSquare size={18} />
        Comments <span className="comment-count">{comments.length}</span>
      </h3>

      <form className="comment-form" onSubmit={handleSubmit}>
        <div className="comment-input-row">
          <div className="avatar">{user?.username?.[0]?.toUpperCase()}</div>
          <textarea
            className="input textarea comment-textarea"
            placeholder="Share your thoughts..."
            value={content}
            onChange={(e) => setContent(e.target.value)}
            rows={2}
          />
        </div>
        <div style={{ display: 'flex', justifyContent: 'flex-end' }}>
          <button type="submit" className="btn btn-primary btn-sm" disabled={submitting || !content.trim()}>
            <FiSend size={14} />
            {submitting ? 'Posting...' : 'Post Comment'}
          </button>
        </div>
      </form>

      <div className="comments-list">
        {comments.length === 0 ? (
          <div className="empty-state" style={{ padding: '40px' }}>
            <span className="empty-state-icon">💬</span>
            <h3>No comments yet</h3>
            <p>Be the first to comment on this article</p>
          </div>
        ) : (
          comments.map(comment => (
            <div key={comment.id} className="comment-item">
              <div className="avatar">{comment.authorName?.[0]?.toUpperCase()}</div>
              <div className="comment-content">
                <div className="comment-header">
                  <span className="comment-author">{comment.authorName}</span>
                  <span className="comment-date">{formatDate(comment.createdAt)}</span>
                </div>
                <p className="comment-text">{comment.content}</p>
              </div>
              {(user?.id === comment.userId || user?.role === 'Admin') && (
                <button
                  className="btn btn-icon btn-ghost btn-sm comment-delete"
                  onClick={() => handleDelete(comment.id)}
                  title="Delete"
                >
                  <FiTrash2 size={14} />
                </button>
              )}
            </div>
          ))
        )}
      </div>
    </div>
  )
}
