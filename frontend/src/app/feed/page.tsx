'use client'

import { useState, useEffect, useRef, useCallback } from 'react'
import { api } from '@/lib/api'
import { Post as PostType } from '@/types'
import PostCard from '@/components/PostCard'
import PostComposer from '@/components/PostComposer'
import { useAuth } from '@/lib/auth'
import { getFeedConnection } from '@/lib/signalr'

const PAGE_SIZE = 20

export default function FeedPage() {
  const token = useAuth((s) => s.token)
  const [posts, setPosts] = useState<PostType[]>([])
  const [offset, setOffset] = useState(0)
  const [hasMore, setHasMore] = useState(true)
  const [loading, setLoading] = useState(false)
  const [initialLoading, setInitialLoading] = useState(true)
  const sentinelRef = useRef<HTMLDivElement>(null)
  const loadingRef = useRef(false)

  const loadPosts = useCallback(async (off: number, append: boolean) => {
    loadingRef.current = true
    setLoading(true)
    try {
      const { data } = await api.get<PostType[]>('/api/post/feed', {
        params: { offset: off, limit: PAGE_SIZE },
      })
      if (append) {
        setPosts(prev => [...prev, ...data])
      } else {
        setPosts(data)
      }
      setHasMore(data.length === PAGE_SIZE)
      setOffset(off + data.length)
    } catch {
      if (!append) setPosts([])
      setHasMore(false)
    }
    loadingRef.current = false
    setLoading(false)
    setInitialLoading(false)
  }, [])

  // Initial load
  useEffect(() => {
    loadPosts(0, false)
  }, [loadPosts])

  // SignalR live updates
  useEffect(() => {
    if (!token) return
    const conn = getFeedConnection(token)
    conn.on('Receive', () => {
      loadPosts(0, false)
    })
    conn.start().catch(() => {})
    return () => { conn.stop() }
  }, [token, loadPosts])

  // PostComposer event fallback
  useEffect(() => {
    const handler = () => loadPosts(0, false)
    window.addEventListener('post-created', handler)
    return () => window.removeEventListener('post-created', handler)
  }, [loadPosts])

  // Infinite scroll
  useEffect(() => {
    const el = sentinelRef.current
    if (!el) return

    const observer = new IntersectionObserver(
      (entries) => {
        if (entries[0].isIntersecting && hasMore && !loadingRef.current) {
          loadPosts(offset, true)
        }
      },
      { rootMargin: '200px' }
    )
    observer.observe(el)
    return () => observer.disconnect()
  }, [hasMore, offset, loadPosts])

  return (
    <div className="space-y-4">
      <PostComposer />
      {initialLoading && <p className="text-gray-400 text-center">Загрузка...</p>}
      {posts.map((post) => (
        <PostCard key={post.post_id} post={post} showAuthor />
      ))}
      {loading && !initialLoading && <p className="text-gray-400 text-center">Загрузка...</p>}
      {!loading && hasMore && <div ref={sentinelRef} className="h-4" />}
      {!initialLoading && posts.length === 0 && (
        <p className="text-gray-400 text-center">В ленте пока нет постов.</p>
      )}
    </div>
  )
}
