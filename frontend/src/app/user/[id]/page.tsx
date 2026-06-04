'use client'

import { useQuery } from '@tanstack/react-query'
import { useParams } from 'next/navigation'
import { api } from '@/lib/api'
import { Post as PostType, User } from '@/types'
import PostCard from '@/components/PostCard'
import FriendButton from '@/components/FriendButton'
import { useAuth } from '@/lib/auth'

export default function UserPage() {
  const { id } = useParams()
  const userId = useAuth((s) => s.userId)

  const { data: user } = useQuery({
    queryKey: ['user', id],
    queryFn: () => api.get<User>(`/api/user/get/${id}`).then((r) => r.data),
  })

  const { data: posts } = useQuery({
    queryKey: ['user-posts', id],
    queryFn: () =>
      api.get<PostType[]>('/api/post/feed', {
        params: { offset: 0, limit: 100 },
      }).then((r) => r.data),
  })

  if (!user) return <p className="text-gray-400 text-center">Загрузка...</p>

  const ownPosts = posts?.filter((p) => p.user_id === id) ?? []

  return (
    <div className="space-y-4">
      <div className="bg-white rounded-lg border p-6 space-y-2">
        <h1 className="text-xl font-bold">
          {user.first_name} {user.second_name}
        </h1>
        <p className="text-sm text-gray-500">{user.city}</p>
        {user.biography && <p className="text-sm">{user.biography}</p>}
        <FriendButton userId={userId!} friendId={id as string} />
      </div>
      {ownPosts.map((post) => (
        <PostCard key={post.post_id} post={post} />
      ))}
    </div>
  )
}
