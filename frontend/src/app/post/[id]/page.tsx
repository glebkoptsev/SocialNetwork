'use client'

import { useQuery } from '@tanstack/react-query'
import { useParams } from 'next/navigation'
import { api } from '@/lib/api'
import { Post } from '@/types'
import PostCard from '@/components/PostCard'

export default function PostPage() {
  const { id } = useParams<{ id: string }>()

  const { data, isLoading, isError } = useQuery({
    queryKey: ['post', id],
    queryFn: () => api.get<Post>(`/api/post/get/${id}`).then((r) => r.data),
    retry: false,
  })

  if (isLoading) return <p className="text-gray-400 text-center">Загрузка...</p>
  if (isError || !data) return <p className="text-gray-400 text-center">Пост не найден.</p>

  return (
    <div>
      <PostCard post={data} showAuthor />
    </div>
  )
}
