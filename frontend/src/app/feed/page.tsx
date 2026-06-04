'use client'

import { useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '@/lib/api'
import { Post as PostType } from '@/types'
import PostCard from '@/components/PostCard'
import PostComposer from '@/components/PostComposer'
import { useEffect } from 'react'
import { useAuth } from '@/lib/auth'
import { getFeedConnection } from '@/lib/signalr'

export default function FeedPage() {
  const token = useAuth((s) => s.token)
  const queryClient = useQueryClient()

  const { data, isLoading } = useQuery({
    queryKey: ['feed'],
    queryFn: () => api.get<PostType[]>('/api/post/feed').then((r) => r.data),
  })

  useEffect(() => {
    if (!token) return
    const conn = getFeedConnection(token)
    conn.on('Receive', () => {
      queryClient.invalidateQueries({ queryKey: ['feed'] })
    })
    conn.start()
    return () => { conn.stop() }
  }, [token, queryClient])

  return (
    <div className="space-y-4">
      <PostComposer />
      {isLoading && <p className="text-gray-400 text-center">Загрузка...</p>}
      {data?.map((post) => (
        <PostCard key={post.post_id} post={post} showAuthor />
      ))}
      {data?.length === 0 && (
        <p className="text-gray-400 text-center">В ленте пока нет постов.</p>
      )}
    </div>
  )
}
