'use client'

import { useState, FormEvent } from 'react'
import { useAuth } from '@/lib/auth'
import { useRouter } from 'next/navigation'
import Link from 'next/link'

export default function LoginPage() {
  const [loginValue, setLoginValue] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)
  const login = useAuth((s) => s.login)
  const router = useRouter()

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault()
    setError('')
    setLoading(true)
    try {
      await login(loginValue, password)
      router.push('/feed')
    } catch {
      setError('Неверные данные')
    }
    setLoading(false)
  }

  return (
    <div className="flex items-center justify-center min-h-[60vh]">
      <form onSubmit={handleSubmit} className="w-full max-w-sm space-y-4">
        <h1 className="text-2xl font-bold text-center">Вход</h1>
        {error && <p className="text-red-500 text-sm text-center">{error}</p>}
        <label className="block">
          <span className="text-xs text-gray-500">Логин</span>
          <input
            className="w-full border rounded px-3 py-2 mt-1"
            value={loginValue}
            onChange={(e) => setLoginValue(e.target.value)}
            required
          />
        </label>
        <label className="block">
          <span className="text-xs text-gray-500">Пароль</span>
          <input
            className="w-full border rounded px-3 py-2 mt-1"
            type="password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            required
          />
        </label>
        <button className="w-full bg-blue-600 text-white rounded py-2 hover:bg-blue-700 disabled:opacity-50" disabled={loading}>
          {loading ? 'Вход...' : 'Войти'}
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
