import axios from 'axios'

function getBaseUrl() {
  if (typeof window !== 'undefined') {
    return (window as any).__NEXT_DATA__?.runtimeConfig?.apiUrl
  }
  return process.env.NEXT_PUBLIC_API_URL
}

function authInterceptor(config: any) {
  if (typeof window !== 'undefined') {
    const token = document.cookie
      .split('; ')
      .find((row) => row.startsWith('token='))
      ?.split('=')[1]
    if (token) {
      config.headers.Authorization = `Bearer ${token}`
    }
  }
  return config
}

export const api = axios.create({
  baseURL: process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000',
})

api.interceptors.request.use(authInterceptor)

export const dialogApi = axios.create({
  baseURL: process.env.NEXT_PUBLIC_DIALOG_API_URL || 'http://localhost:5002',
})

dialogApi.interceptors.request.use(authInterceptor)
