'use client'

import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { useEffect, useState } from 'react'
import { useAuth } from '@/lib/auth'

const queryClient = new QueryClient()

export function Providers({ children }: { children: React.ReactNode }) {
  const [mounted, setMounted] = useState(false)
  const hydrate = useAuth((s) => s.hydrate)

  useEffect(() => {
    hydrate()
    setMounted(true)
  }, [hydrate])

  if (!mounted) return null

  return (
    <QueryClientProvider client={queryClient}>
      {children}
    </QueryClientProvider>
  )
}
