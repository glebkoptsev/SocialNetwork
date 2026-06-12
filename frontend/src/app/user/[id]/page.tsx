'use client'

import { useQuery } from '@tanstack/react-query'
import { useParams } from 'next/navigation'
import Link from 'next/link'
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

  const { data: subscriptions } = useQuery({
    queryKey: ['subscriptions', id],
    queryFn: () =>
      api.get<User[]>('/api/friend/subscriptions', { params: { user_id: id } }).then((r) => r.data),
  })

  const { data: followers } = useQuery({
    queryKey: ['followers', id],
    queryFn: () =>
      api.get<User[]>('/api/friend/followers', { params: { user_id: id } }).then((r) => r.data),
  })

  const { data: posts } = useQuery({
    queryKey: ['user-posts', id],
    queryFn: () =>
      api.get<PostType[]>('/api/post/feed', {
        params: { offset: 0, limit: 100, user_id: id },
      }).then((r) => r.data),
    enabled: !!user,
  })

  if (!user) return <p className="text-gray-400 text-center">Загрузка...</p>

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

      {subscriptions && subscriptions.length > 0 && (
        <div className="bg-white rounded-lg border p-4 space-y-2">
          <h2 className="text-lg font-semibold">Подписки ({subscriptions.length})</h2>
          <div className="flex flex-wrap gap-2">
            {subscriptions.map((f) => (
              <Link
                key={f.user_id}
                href={`/user/${f.login}`}
                className="bg-gray-100 rounded px-3 py-1 text-sm hover:bg-gray-200"
              >
                {f.first_name} {f.second_name}
              </Link>
            ))}
          </div>
        </div>
      )}

      {followers && followers.length > 0 && (
        <div className="bg-white rounded-lg border p-4 space-y-2">
          <h2 className="text-lg font-semibold">Подписчики ({followers.length})</h2>
          <div className="flex flex-wrap gap-2">
            {followers.map((f) => (
              <Link
                key={f.user_id}
                href={`/user/${f.login}`}
                className="bg-gray-100 rounded px-3 py-1 text-sm hover:bg-gray-200"
              >
                {f.first_name} {f.second_name}
              </Link>
            ))}
          </div>
        </div>
      )}

      {posts?.map((post) => (
        <PostCard key={post.post_id} post={post} />
      ))}
    </div>
  )
}