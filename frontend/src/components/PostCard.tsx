'use client'

import { Post } from '@/types'
import Link from 'next/link'

export default function PostCard({ post, showAuthor }: { post: Post; showAuthor?: boolean }) {
  const authorName = post.authorFirstName && post.authorSecondName
    ? `${post.authorFirstName} ${post.authorSecondName}`
    : null
  return (
    <div className="bg-white rounded-lg border p-4 space-y-2">
      {showAuthor && (
        <Link href={`/user/${post.user_id}`} className="text-sm text-blue-600 hover:underline">
          {authorName ?? `${post.user_id.slice(0, 8)}...`}
        </Link>
      )}
      <p className="whitespace-pre-wrap">{post.text}</p>
      <p className="text-xs text-gray-400">
        {new Date(post.creation_datetime).toLocaleString()}
      </p>
    </div>
  )
}
