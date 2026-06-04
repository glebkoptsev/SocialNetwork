'use client'

import { useQuery } from '@tanstack/react-query'
import { dialogApi } from '@/lib/api'
import { Chat } from '@/types'
import Link from 'next/link'

export default function DialogListPage() {
  const { data, isLoading } = useQuery({
    queryKey: ['chats'],
    queryFn: () => dialogApi.get<Chat[]>('/api/dialog/list').then((r) => r.data),
  })

  return (
    <div className="space-y-2">
      <h1 className="text-xl font-bold">Сообщения</h1>
      {isLoading && <p className="text-gray-400">Загрузка...</p>}
      {data?.map((chat) => (
        <Link
          key={chat.chat_id}
          href={`/dialog/${chat.chat_id}`}
          className="block bg-white rounded-lg border p-4 hover:shadow-sm"
        >
          <p className="font-medium">{chat.chat_name}</p>
        </Link>
      ))}
      {data?.length === 0 && (
        <p className="text-gray-400">Чатов пока нет.</p>
      )}
    </div>
  )
}
