'use client'

import { User } from '@/types'
import Link from 'next/link'

export default function UserCard({ user }: { user: User }) {
  return (
    <Link href={`/user/${user.user_id}`} className="block bg-white rounded-lg border p-4 hover:shadow-sm">
      <p className="font-medium">
        {user.first_name} {user.second_name}
      </p>
      <p className="text-sm text-gray-500">{user.city}</p>
    </Link>
  )
}
