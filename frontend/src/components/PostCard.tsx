'use client'

import { Post } from '@/types'
import { api } from '@/lib/api'
import Link from 'next/link'
import { useState } from 'react'

export default function PostCard({ post, showAuthor }: { post: Post; showAuthor?: boolean }) {
  const [likeCount, setLikeCount] = useState(post.like_count)
  const [liked, setLiked] = useState(post.liked_by_me)

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
          <svg viewBox="0 0 24 24" className={`w-4 h-4 ${liked ? 'fill-red-500' : 'fill-none'} stroke-current ${liked ? 'stroke-red-500' : 'stroke-gray-400'}`}>
            <path d="M12 21.35l-1.45-1.32C5.4 15.36 2 12.28 2 8.5 2 5.42 4.42 3 7.5 3c1.74 0 3.41.81 4.5 2.09C13.09 3.81 14.76 3 16.5 3 19.58 3 22 5.42 22 8.5c0 3.78-3.4 6.86-8.55 11.54L12 21.35z" />
          </svg>
          <span className="text-gray-500">{likeCount}</span>
        </button>
      </div>
    </div>
  )
}
