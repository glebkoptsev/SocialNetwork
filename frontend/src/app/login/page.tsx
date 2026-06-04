'use client'

import { useState, FormEvent } from 'react'
import { useAuth } from '@/lib/auth'
import { useRouter } from 'next/navigation'
import Link from 'next/link'

export default function LoginPage() {
  const [id, setId] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState('')
  const login = useAuth((s) => s.login)
  const router = useRouter()

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault()
    setError('')
    try {
      await login(id, password)
      router.push('/feed')
    } catch {
      setError('Неверные данные')
    }
  }

  return (
    <div className="flex items-center justify-center min-h-[60vh]">
      <form onSubmit={handleSubmit} className="w-full max-w-sm space-y-4">
        <h1 className="text-2xl font-bold text-center">Вход</h1>
        {error && <p className="text-red-500 text-sm text-center">{error}</p>}
        <input
          className="w-full border rounded px-3 py-2"
          placeholder="ID пользователя"
          value={id}
          onChange={(e) => setId(e.target.value)}
          required
        />
        <input
          className="w-full border rounded px-3 py-2"
          type="password"
          placeholder="Пароль"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          required
        />
        <button className="w-full bg-blue-600 text-white rounded py-2 hover:bg-blue-700">
          Войти
        </button>
        <p className="text-sm text-center text-gray-500">
          Нет аккаунта?{' '}
          <Link href="/register" className="text-blue-600 underline">
            Регистрация
          </Link>
        </p>
      </form>
    </div>
  )
}
