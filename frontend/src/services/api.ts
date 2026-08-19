import axios from 'axios'
import { getToken } from './token'

export const API_BASE_URL = import.meta.env.VITE_API_URL ?? 'http://localhost:5054'

export const api = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
})

api.interceptors.request.use((config) => {
  const token = getToken()
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }
  return config
})

export interface LoginPayload {
  email: string
  password: string
}

export interface LoginResponse {
  token: string
}

export async function login(payload: LoginPayload) {
  const { data } = await api.post<LoginResponse>('/api/auth/login', payload)
  return data
}

export interface Product {
  id: string
  name: string
  sku: string
  desc: string
  price: number
}

export interface CreateProductPayload {
  name: string
  sku: string
  desc: string
  price: number
}

export async function getProducts() {
  const { data } = await api.get<Product[]>('/api/products')
  return data
}

export async function createProduct(payload: CreateProductPayload) {
  const { data } = await api.post<Product>('/api/products', payload)
  return data
}

export async function deleteProduct(id: string) {
  await api.delete(`/api/products/${id}`)
}
