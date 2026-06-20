'use client'

import { Post } from '@/types'
import { api } from '@/lib/api'
import Link from 'next/link'
import { useState } from 'react'

export default function PostCard({ post, showAuthor }: { post: Post; showAuthor?: boolean }) {
  const [likeCount, setLikeCount] = useState(post.like_count)
  const [liked, setLiked] = useState(false)

  const authorName = post.authorFirstName && post.authorSecondName
    ? `${post.authorFirstName} ${post.authorSecondName}`
    : null

  const handleLike = async () => {
    try {
      const { data } = await api.post<{ liked: boolean; like_count: number }>(`/api/post/${post.post_id}/like`)
      setLiked(data.liked)
      setLikeCount(data.like_count)
    } catch { }
  }

  return (
    <div className="bg-white rounded-lg border p-4 space-y-2">
      {showAuthor && (
        <Link href={`/user/${post.user_id}`} className="text-sm text-blue-600 hover:underline">
          {authorName ?? 'Неизвестный автор'}
        </Link>
      )}
      <p className="whitespace-pre-wrap">{post.text}</p>
      <div className="flex items-center gap-4">
        <p className="text-xs text-gray-400">
          {new Date(post.creation_datetime).toLocaleString()}
        </p>
        <button onClick={handleLike} className="flex items-center gap-1 text-sm hover:opacity-70">
          <span className={liked ? 'text-red-500' : 'text-gray-400'}>&#x2764;</span>
          <span className="text-gray-500">{likeCount}</span>
        </button>
      </div>
    </div>
  )
}
