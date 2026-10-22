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
  rowVersion: string // base64; volta no PUT para a API detectar edição concorrente
}

export interface CreateProductPayload {
  name: string
  sku: string
  desc: string
  price: number
}

export interface UpdateProductPayload {
  rowVersion: string
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
  const { data } = await api.put<Product>(`/api/products/${id}`, payload)
  return data
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
  rowVersion: string // base64; volta no PUT para a API detectar edição concorrente
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
  rowVersion: string
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
  const { data } = await api.put<Storage>(`/api/storages/${id}`, payload)
  return data
}

export async function deleteStorage(id: string) {
  await api.delete(`/api/storages/${id}`)
}

export interface Supplier {
  id: string
  name: string
  document: string // CPF ou CNPJ, só dígitos
  email: string
  phone: string
  isActive: boolean
  rowVersion: string // base64; volta no PUT para a API detectar edição concorrente
}

interface SupplierFields {
  name: string
  email: string
  phone: string
}

export interface CreateSupplierPayload extends SupplierFields {
  document: string
}

export interface UpdateSupplierPayload extends SupplierFields {
  rowVersion: string
  isActive: boolean
}

// 11 dígitos = CPF (000.000.000-00), 14 = CNPJ (00.000.000/0000-00). Outro tamanho volta como está.
export function formatDocument(document: string) {
  if (document.length === 11) return document.replace(/(\d{3})(\d{3})(\d{3})(\d{2})/, '$1.$2.$3-$4')
  if (document.length === 14)
    return document.replace(/(\d{2})(\d{3})(\d{3})(\d{4})(\d{2})/, '$1.$2.$3/$4-$5')
  return document
}

export async function getSuppliers() {
  const { data } = await api.get<Supplier[]>('/api/suppliers')
  return data
}

export async function createSupplier(payload: CreateSupplierPayload) {
  const { data } = await api.post<Supplier>('/api/suppliers', payload)
  return data
}

export async function updateSupplier(id: string, payload: UpdateSupplierPayload) {
  const { data } = await api.put<Supplier>(`/api/suppliers/${id}`, payload)
  return data
}

export async function deleteSupplier(id: string) {
  await api.delete(`/api/suppliers/${id}`)
}

export interface Customer {
  id: string
  name: string
  document: string // CPF ou CNPJ, só dígitos
  email: string
  phone: string
  isActive: boolean
  rowVersion: string // base64; volta no PUT para a API detectar edição concorrente
}

interface CustomerFields {
  name: string
  email: string
  phone: string
}

export interface CreateCustomerPayload extends CustomerFields {
  document: string
}

export interface UpdateCustomerPayload extends CustomerFields {
  rowVersion: string
  isActive: boolean
}

export async function getCustomers() {
  const { data } = await api.get<Customer[]>('/api/customers')
  return data
}

export async function createCustomer(payload: CreateCustomerPayload) {
  const { data } = await api.post<Customer>('/api/customers', payload)
  return data
}

export async function updateCustomer(id: string, payload: UpdateCustomerPayload) {
  const { data } = await api.put<Customer>(`/api/customers/${id}`, payload)
  return data
}

export async function deleteCustomer(id: string) {
  await api.delete(`/api/customers/${id}`)
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

// Um saldo é identificado pela chave composta da API: produto + armazém + lote.
export function stockKey(item: StockBalance) {
  return `${item.productId}-${item.storageId}-${item.batch}`
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
  | 'PurchaseReceipt'

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

export type PurchaseOrderStatus = 'Open' | 'Partial' | 'Received' | 'Cancelled'

export const PURCHASE_STATUS_LABEL: Record<PurchaseOrderStatus, string> = {
  Open: 'Aberto',
  Partial: 'Parcial',
  Received: 'Recebido',
  Cancelled: 'Cancelado',
}

export interface PurchaseOrderSummary {
  id: string
  number: number
  status: PurchaseOrderStatus
  supplierId: string
  supplierName: string
  storageId: string
  storageName: string
  total: number
  orderedUnits: number
  receivedUnits: number
  createdAt: string
}

export interface PurchaseOrderItem {
  id: string
  productId: string
  productName: string
  productSku: string
  quantity: number
  receivedQuantity: number
  unitPrice: number
}

export interface PurchaseReceiptItem {
  purchaseOrderItemId: string
  productName: string
  batch: string
  quantity: number
  unitPrice: number // preço da nota
  orderedUnitPrice: number
}

export interface PurchaseReceipt {
  id: string
  invoiceNumber: string
  priceDivergenceReason: string | null
  createdBy: string
  createdAt: string
  items: PurchaseReceiptItem[]
}

export interface PurchaseOrderDetail {
  id: string
  number: number
  status: PurchaseOrderStatus
  supplierId: string
  supplierName: string
  supplierDocument: string
  storageId: string
  storageName: string
  notes: string | null
  createdBy: string
  createdAt: string
  cancelledBy: string | null
  cancelledAt: string | null
  cancelReason: string | null
  rowVersion: string // base64; volta no recebimento e no cancelamento
  items: PurchaseOrderItem[]
  receipts: PurchaseReceipt[]
}

export interface CreatePurchaseOrderPayload {
  supplierId: string
  storageId: string
  notes: string
  items: { productId: string; quantity: number; unitPrice: number }[]
}

export interface ReceivePurchaseOrderPayload {
  invoiceNumber: string
  priceDivergenceReason: string | null
  rowVersion: string
  items: { purchaseOrderItemId: string; batch: string; quantity: number; unitPrice: number }[]
}

export interface CancelPurchaseOrderPayload {
  reason: string
  rowVersion: string
}

export function purchaseOrderCode(number: number) {
  return `PC-${String(number).padStart(6, '0')}`
}

// Aberto ou parcial: ainda aceita recebimento e cancelamento.
export function isPending(status: PurchaseOrderStatus) {
  return status === 'Open' || status === 'Partial'
}

export async function getPurchaseOrders() {
  const { data } = await api.get<PurchaseOrderSummary[]>('/api/purchase-orders')
  return data
}

export async function getPurchaseOrder(id: string) {
  const { data } = await api.get<PurchaseOrderDetail>(`/api/purchase-orders/${id}`)
  return data
}

export async function createPurchaseOrder(payload: CreatePurchaseOrderPayload) {
  const { data } = await api.post<PurchaseOrderDetail>('/api/purchase-orders', payload)
  return data
}

export async function receivePurchaseOrder(id: string, payload: ReceivePurchaseOrderPayload) {
  const { data } = await api.post<PurchaseOrderDetail>(`/api/purchase-orders/${id}/receipts`, payload)
  return data
}

export async function cancelPurchaseOrder(id: string, payload: CancelPurchaseOrderPayload) {
  const { data } = await api.post<PurchaseOrderDetail>(`/api/purchase-orders/${id}/cancel`, payload)
  return data
}

// Acorda a API em segundo plano (cold start). Não espera resposta e ignora erro.
export function wakeUpApi(): void {
  fetch(`${API_BASE_URL}/health`).catch(() => {})
}

// Erros de formulário a partir da resposta da API: os de campo (400 de validação) quando existem,
// senão o "detail" do ProblemDetails (400 de corpo inválido, 409 de edição concorrente, 422 de regra),
// e por último a mensagem genérica. Assim nenhum erro termina com a tela calada.
export function formErrorsFrom(err: unknown, fallback: string): Record<string, string[]> {
  if (axios.isAxiosError(err) && err.response) {
    const { status, data } = err.response
    if (status === 400 && data?.errors && Object.keys(data.errors).length > 0) {
      return data.errors
    }
    if ([400, 409, 422].includes(status) && typeof data?.detail === 'string') {
      return { geral: [data.detail] }
    }
  }
  return { geral: [fallback] }
}
