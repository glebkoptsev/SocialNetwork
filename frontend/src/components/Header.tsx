'use client'

import { useAuth } from '@/lib/auth'
import Link from 'next/link'
import { useRouter } from 'next/navigation'

export default function Header() {
  const { token, userId, logout } = useAuth()
  const router = useRouter()

  if (!token) return null

  return (
    <header className="bg-white border-b sticky top-0 z-10">
      <div className="max-w-3xl mx-auto flex items-center justify-between px-4 h-14">
        <div className="flex items-center gap-6">
          <Link href="/feed" className="font-bold text-lg">SN</Link>
          <Link href="/feed" className="text-sm hover:text-blue-600">Лента</Link>
          <Link href="/search" className="text-sm hover:text-blue-600">Поиск</Link>
          <Link href="/dialog" className="text-sm hover:text-blue-600">Сообщения</Link>
          <Link href="/settings" className="text-sm hover:text-blue-600">Настройки</Link>
        </div>
        <div className="flex items-center gap-4">
          <Link href={`/user/${userId}`} className="text-sm hover:text-blue-600">Профиль</Link>
          <button
            onClick={() => { logout(); router.push('/login') }}
            className="text-sm text-red-500 hover:text-red-700"
          >
            Выйти
          </button>
        </div>
      </div>
    </header>
  )
}
