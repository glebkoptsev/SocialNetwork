'use client'

import { useState, useEffect, useRef, useCallback } from 'react'
import { useQuery } from '@tanstack/react-query'
import { useParams, useRouter } from 'next/navigation'
import Link from 'next/link'
import { api, dialogApi } from '@/lib/api'
import { Post as PostType, User } from '@/types'
import PostCard from '@/components/PostCard'
import FriendButton from '@/components/FriendButton'
import { useAuth } from '@/lib/auth'

const PAGE_SIZE = 20

export default function UserPage() {
  const { id } = useParams<{ id: string }>()
  const currentUserId = useAuth((s) => s.userId)
  const router = useRouter()

  const { data: user, isLoading: userLoading, isError: userError } = useQuery({
    queryKey: ['user', id],
    queryFn: () => api.get<User>(`/api/user/get/${id}`).then((r) => r.data),
    retry: false,
  })

  const isOwnProfile = !!user && user.user_id === currentUserId

  const [showSubscriptions, setShowSubscriptions] = useState(false)
  const [showFollowers, setShowFollowers] = useState(false)

  const { data: subscriptions } = useQuery({
    queryKey: ['subscriptions', id],
    queryFn: () =>
      api.get<User[]>('/api/friend/subscriptions', { params: { user_id: id, limit: 20 } }).then((r) => r.data),
  })

  const { data: followers } = useQuery({
    queryKey: ['followers', id],
    queryFn: () =>
      api.get<User[]>('/api/friend/followers', { params: { user_id: id, limit: 20 } }).then((r) => r.data),
  })

  const [posts, setPosts] = useState<PostType[]>([])
  const [offset, setOffset] = useState(0)
  const [hasMore, setHasMore] = useState(true)
  const [loading, setLoading] = useState(false)
  const sentinelRef = useRef<HTMLDivElement>(null)
  const loadingRef = useRef(false)

  const loadPosts = useCallback(async (off: number, append: boolean) => {
    loadingRef.current = true
    setLoading(true)
    try {
      const { data } = await api.get<PostType[]>('/api/post/feed', {
        params: { offset: off, limit: PAGE_SIZE, user_id: id },
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
  }, [id])

  useEffect(() => {
    if (!user) return
    loadPosts(0, false)
  }, [user, loadPosts])

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

  if (userLoading) return <p className="text-gray-400 text-center">Загрузка...</p>
  if (userError || !user) return <p className="text-gray-400 text-center">Пользователь не найден.</p>

  return (
    <div className="space-y-4">
      {/* Profile card */}
      <div className="bg-white rounded-lg border">
        <div className="h-24 bg-gradient-to-r from-blue-400 to-blue-600 rounded-t-lg" />
        <div className="px-6 pb-5 -mt-1">
          <div className="flex items-end justify-between mb-3">
            <div className="rounded-full border-4 border-white bg-gray-200 w-20 h-20 flex items-center justify-center text-2xl font-bold text-gray-600">
              {user.first_name[0]}{user.second_name[0]}
            </div>
            <div className="flex gap-2">
              {!isOwnProfile && <FriendButton userId={currentUserId!} friendId={id} />}
              {!isOwnProfile && (
                <button
                  onClick={async () => {
                    try {
                      const { data: chatId } = await dialogApi.post(`/api/dialog/personal/${user.user_id}`)
                      router.push(`/dialog/${chatId}`)
                    } catch (err: any) {
                      alert(err?.response?.data?.error || 'Не удалось создать чат')
                    }
                  }}
                  className="bg-green-600 text-white rounded px-3 py-1 text-sm hover:bg-green-700 flex items-center gap-1"
                >
                  <svg className="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M8 12h.01M12 12h.01M16 12h.01M21 12c0 4.418-4.03 8-9 8a9.863 9.863 0 01-4.255-.949L3 20l1.395-3.72C3.512 15.042 3 13.574 3 12c0-4.418 4.03-8 9-8s9 3.582 9 8z" /></svg>
                  Написать
                </button>
              )}
              {isOwnProfile && (
                <Link href="/settings" className="bg-gray-100 text-gray-700 rounded px-3 py-1 text-sm hover:bg-gray-200">
                  Редактировать
                </Link>
              )}
            </div>
          </div>

          <h1 className="text-xl font-bold leading-tight">
            {user.first_name} {user.second_name}
          </h1>
          {user.city && <p className="text-sm text-gray-500">{user.city}</p>}
          {user.biography && <p className="text-sm mt-1">{user.biography}</p>}

          <div className="flex gap-4 mt-3 text-sm">
            <span>
              <span className="font-semibold">{posts.length}</span>
              <span className="text-gray-500 ml-1">постов</span>
            </span>
            {followers && (
              <button onClick={() => setShowFollowers(v => !v)} className="hover:text-blue-600">
                <span className="font-semibold">{followers.length}</span>
                <span className="text-gray-500 ml-1">подписчиков</span>
              </button>
            )}
            {subscriptions && (
              <button onClick={() => setShowSubscriptions(v => !v)} className="hover:text-blue-600">
                <span className="font-semibold">{subscriptions.length}</span>
                <span className="text-gray-500 ml-1">подписок</span>
              </button>
            )}
          </div>
        </div>
      </div>

      {/* Followers list (expandable) */}
      {showFollowers && followers && followers.length > 0 && (
        <div className="bg-white rounded-lg border p-4 space-y-2">
          <h2 className="text-sm font-semibold text-gray-500 uppercase tracking-wide">Подписчики</h2>
          <div className="flex flex-wrap gap-2">
            {followers.map((f) => (
              <Link
                key={f.user_id}
                href={`/user/${f.user_id}`}
                className="bg-gray-100 rounded px-3 py-1 text-sm hover:bg-gray-200"
              >
                {f.first_name} {f.second_name}
              </Link>
            ))}
          </div>
        </div>
      )}

      {/* Subscriptions list (expandable) */}
      {showSubscriptions && subscriptions && subscriptions.length > 0 && (
        <div className="bg-white rounded-lg border p-4 space-y-2">
          <h2 className="text-sm font-semibold text-gray-500 uppercase tracking-wide">Подписки</h2>
          <div className="flex flex-wrap gap-2">
            {subscriptions.map((f) => (
              <Link
                key={f.user_id}
                href={`/user/${f.user_id}`}
                className="bg-gray-100 rounded px-3 py-1 text-sm hover:bg-gray-200"
              >
                {f.first_name} {f.second_name}
              </Link>
            ))}
          </div>
        </div>
      )}

      {/* Posts */}
      <div className="space-y-3">
        <h2 className="text-sm font-semibold text-gray-500 uppercase tracking-wide px-1">Посты</h2>
        {posts.map((post) => (
          <PostCard key={post.post_id} post={post} />
        ))}
        {loading && <p className="text-gray-400 text-center">Загрузка...</p>}
        {!loading && hasMore && <div ref={sentinelRef} className="h-4" />}
        {!loading && posts.length === 0 && (
          <p className="text-gray-400 text-center py-8">У пользователя пока нет постов.</p>
        )}
      </div>
    </div>
  )
}
