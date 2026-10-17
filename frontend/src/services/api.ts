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
  const { data } = await api.post<LoginResponse>('/api/auth/login', payload, {
    timeout: 15000, // sem isso o axios espera para sempre
  })
  return data
}

// Erro típico de "API dormindo": sem resposta (rede/timeout) ou gateway 502/503/504.
// 401 (senha errada) NÃO entra aqui: a API respondeu, então está acordada.
export function isColdStartError(err: unknown): boolean {
  if (!axios.isAxiosError(err)) return false
  if (!err.response) return true
  return [502, 503, 504].includes(err.response.status)
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
  productDeleted: boolean
  storageDeleted: boolean
  productActive: boolean
  storageActive: boolean
}

export async function getStock() {
  const { data } = await api.get<StockBalance[]>('/api/stock')
  return data
}

export type MovementType =
  | 'Entry'
  | 'Exit'
  | 'TransferOut'
  | 'TransferIn'
  | 'Adjustment'
  | 'OpeningBalance'

export interface Movement {
  id: string
  type: MovementType
  productId: string
  productName: string
  storageId: string
  storageName: string
  batch: string
  quantity: number
  reason: string | null
  transferId: string | null
  createdBy: string
  createdAt: string
}

export interface MovementResult {
  mensagem: string
  saldoAtual: number
}

export interface StockMovementPayload {
  productId: string
  storageId: string
  batch: string
  quantity: number
}

export interface TransferPayload {
  productId: string
  fromStorageId: string
  toStorageId: string
  batch: string
  quantity: number
}

export interface AdjustmentPayload {
  productId: string
  storageId: string
  batch: string
  countedQuantity: number
  reason: string
}

export async function registerEntry(payload: StockMovementPayload) {
  const { data } = await api.post<MovementResult>('/api/movements/entry', payload)
  return data
}

export async function registerExit(payload: StockMovementPayload) {
  const { data } = await api.post<MovementResult>('/api/movements/exit', payload)
  return data
}

export async function registerTransfer(payload: TransferPayload) {
  const { data } = await api.post<MovementResult>('/api/movements/transfer', payload)
  return data
}

export async function registerAdjustment(payload: AdjustmentPayload) {
  const { data } = await api.post<MovementResult>('/api/movements/adjustment', payload)
  return data
}

export async function getMovements(filter: { productId?: string; storageId?: string; batch?: string }) {
  const { data } = await api.get<Movement[]>('/api/movements', { params: filter })
  return data
}

// Acorda a API em segundo plano (cold start). Não espera resposta e ignora erro.
export function wakeUpApi(): void {
  fetch(`${API_BASE_URL}/health`).catch(() => {})
}