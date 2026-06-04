'use client'

import { useAuth } from '@/lib/auth'
import { useEffect } from 'react'
import { useRouter } from 'next/navigation'

export default function Home() {
  const { token, loading } = useAuth()
  const router = useRouter()

  useEffect(() => {
    if (!loading) {
      router.replace(token ? '/feed' : '/login')
    }
  }, [token, loading, router])

  return null
}
