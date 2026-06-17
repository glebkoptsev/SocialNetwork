'use client'

import { useState, FormEvent } from 'react'
import { useAuth } from '@/lib/auth'
import { useRouter } from 'next/navigation'
import Link from 'next/link'

export default function RegisterPage() {
  const [form, setForm] = useState({
    login: '',
    first_name: '',
    second_name: '',
    birthdate: '',
    biography: '',
    city: '',
    password: '',
  })
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)
  const register = useAuth((s) => s.register)
  const login = useAuth((s) => s.login)
  const router = useRouter()

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault()
    setError('')
    setLoading(true)
    try {
      await register(form)
    } catch {
      setError('Ошибка регистрации. Возможно, логин уже занят.')
      setLoading(false)
      return
    }
    try {
      await login(form.login, form.password)
      router.push('/feed')
    } catch {
      setError('Аккаунт создан, но не удалось войти. Войдите вручную.')
      setLoading(false)
    }
  }

  const set = (key: string) => (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) =>
    setForm((prev) => ({ ...prev, [key]: e.target.value }))

  return (
    <div className="flex items-center justify-center min-h-[60vh]">
      <form onSubmit={handleSubmit} className="w-full max-w-sm space-y-3">
        <h1 className="text-2xl font-bold text-center">Регистрация</h1>
        {error && <p className="text-red-500 text-sm text-center">{error}</p>}
        <label className="block">
          <span className="text-xs text-gray-500">Имя</span>
          <input className="w-full border rounded px-3 py-2 mt-1" value={form.first_name} onChange={set('first_name')} required />
        </label>
        <label className="block">
          <span className="text-xs text-gray-500">Фамилия</span>
          <input className="w-full border rounded px-3 py-2 mt-1" value={form.second_name} onChange={set('second_name')} required />
        </label>
        <label className="block">
          <span className="text-xs text-gray-500">Логин</span>
          <input className="w-full border rounded px-3 py-2 mt-1" value={form.login} onChange={set('login')} required />
        </label>
        <label className="block">
          <span className="text-xs text-gray-500">Дата рождения</span>
          <input className="w-full border rounded px-3 py-2 mt-1" type="date" max={new Date().toISOString().split('T')[0]} value={form.birthdate} onChange={set('birthdate')} required />
        </label>
        <label className="block">
          <span className="text-xs text-gray-500">О себе</span>
          <textarea className="w-full border rounded px-3 py-2 mt-1" value={form.biography} onChange={set('biography')} />
        </label>
        <label className="block">
          <span className="text-xs text-gray-500">Город</span>
          <input className="w-full border rounded px-3 py-2 mt-1" value={form.city} onChange={set('city')} />
        </label>
        <label className="block">
          <span className="text-xs text-gray-500">Пароль</span>
          <input className="w-full border rounded px-3 py-2 mt-1" type="password" minLength={6} value={form.password} onChange={set('password')} required />
        </label>
        <button className="w-full bg-blue-600 text-white rounded py-2 hover:bg-blue-700 disabled:opacity-50" disabled={loading}>
          {loading ? 'Регистрация...' : 'Зарегистрироваться'}
        </button>
        <p className="text-sm text-center text-gray-500">
          Уже есть аккаунт?{' '}
          <Link href="/login" className="text-blue-600 underline">Войти</Link>
        </p>
      </form>
    </div>
  )
}
