import { Routes, Route, Navigate } from 'react-router-dom'
import { useAuth } from './context/AuthContext'
import Navbar from './components/Navbar'
import ProtectedRoute from './components/ProtectedRoute'
import LoginPage from './pages/LoginPage'
import DashboardPage from './pages/DashboardPage'
import ArticleDetailPage from './pages/ArticleDetailPage'
import ArticleFormPage from './pages/ArticleFormPage'
import AdminPanelPage from './pages/AdminPanelPage'
import BookmarksPage from './pages/BookmarksPage'

export default function App() {
  const { user, loading } = useAuth()

  if (loading) return <div className="loading-spinner" style={{ marginTop: 100 }} />

  return (
    <div className="app-layout">
      {user && <Navbar />}
      <Routes>
        <Route path="/login" element={user ? <Navigate to="/" replace /> : <LoginPage />} />

        <Route path="/" element={
          <ProtectedRoute><DashboardPage /></ProtectedRoute>
        } />
        <Route path="/articles/new" element={
          <ProtectedRoute><ArticleFormPage /></ProtectedRoute>
        } />
        <Route path="/articles/:id" element={
          <ProtectedRoute><ArticleDetailPage /></ProtectedRoute>
        } />
        <Route path="/articles/:id/edit" element={
          <ProtectedRoute><ArticleFormPage /></ProtectedRoute>
        } />
        <Route path="/bookmarks" element={
          <ProtectedRoute><BookmarksPage /></ProtectedRoute>
        } />
        <Route path="/admin" element={
          <ProtectedRoute adminOnly><AdminPanelPage /></ProtectedRoute>
        } />

        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </div>
  )
}
