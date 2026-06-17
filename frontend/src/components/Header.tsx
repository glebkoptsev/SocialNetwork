'use client'

import { useAuth } from '@/lib/auth'
import Link from 'next/link'
import { usePathname, useRouter } from 'next/navigation'

const NAV_LINKS = [
  { href: '/feed', label: 'Лента' },
  { href: '/search', label: 'Поиск' },
  { href: '/dialog', label: 'Сообщения' },
  { href: '/settings', label: 'Настройки' },
]

export default function Header() {
  const { token, userId, logout } = useAuth()
  const router = useRouter()
  const pathname = usePathname()

  if (!token) return null

  return (
    <header className="bg-white border-b sticky top-0 z-10">
      <div className="max-w-3xl mx-auto flex items-center justify-between px-4 h-14">
        <div className="flex items-center gap-4 flex-wrap">
          <Link href="/feed" className="font-bold text-lg">SN</Link>
          {NAV_LINKS.map((link) => (
            <Link
              key={link.href}
              href={link.href}
              className={`text-sm hover:text-blue-600 ${pathname === link.href ? 'text-blue-600 font-medium' : ''}`}
            >
              {link.label}
            </Link>
          ))}
        </div>
        <div className="flex items-center gap-3">
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
