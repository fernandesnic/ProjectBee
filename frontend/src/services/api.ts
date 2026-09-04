import axios from 'axios'
import { clearToken, getToken } from './token'
import { navigateTo } from './navigation'

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

api.interceptors.response.use(
  (response) => response,
  (error) => {
    if (axios.isAxiosError(error) && error.response?.status === 401) {
      const veioDoLogin = error.config?.url?.includes('/api/auth/login')

      if (!veioDoLogin) {
        clearToken()
        navigateTo('/login')   
      }
    }
    return Promise.reject(error)
  },
)

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
  isActive: boolean
}

export interface CreateProductPayload {
  name: string
  sku: string
  desc: string
  price: number
}

export interface UpdateProductPayload {
  name: string
  desc: string
  price: number
  isActive: boolean
}

export async function getProducts() {
  const { data } = await api.get<Product[]>('/api/products')
  return data
}

export async function createProduct(payload: CreateProductPayload) {
  const { data } = await api.post<Product>('/api/products', payload)
  return data
}

export async function updateProduct(id: string, payload: UpdateProductPayload) {
  await api.put(`/api/products/${id}`, payload)
}

export async function deleteProduct(id: string) {
  await api.delete(`/api/products/${id}`)
}

export interface Storage {
  id: string
  name: string
  idNumber: string
  addressNumber: string
  addressStreet: string
  addressCity: string
  isActive: boolean
}

interface StorageFields {
  name: string
  addressNumber: string
  addressStreet: string
  addressCity: string
}

export interface CreateStoragePayload extends StorageFields {
  idNumber: string
}

export interface UpdateStoragePayload extends StorageFields {
  isActive: boolean
}

export async function getStorages() {
  const { data } = await api.get<Storage[]>('/api/storages')
  return data
}

export async function createStorage(payload: CreateStoragePayload) {
  const { data } = await api.post<Storage>('/api/storages', payload)
  return data
}

export async function updateStorage(id: string, payload: UpdateStoragePayload) {
  await api.put(`/api/storages/${id}`, payload)
}

export async function deleteStorage(id: string) {
  await api.delete(`/api/storages/${id}`)
}

export interface StockBalance {
  productId: string
  productName: string
  productPrice: number
  storageId: string
  storageName: string
  storageAddress: string
  balance: number
  batch: string
}

export interface CreateStockPayload {
  productId: string
  storageId: string
  balance: number
  batch: string
}

export async function getStock() {
  const { data } = await api.get<StockBalance[]>('/api/stock')
  return data
}

export async function createStock(payload: CreateStockPayload) {
  await api.post('/api/stock', payload)
}

export async function updateStock(
  productId: string,
  storageId: string,
  batch: string,
  balance: number,
) {
  await api.put(`/api/stock/${productId}/${storageId}/${batch}`, { balance })
}

export async function deleteStock(productId: string, storageId: string, batch: string) {
  await api.delete(`/api/stock/${productId}/${storageId}/${batch}`)
}