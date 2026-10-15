import { useEffect, useState, type FormEvent } from 'react'
import axios from 'axios'
import {
  getProducts,
  getStorages,
  registerAdjustment,
  registerEntry,
  registerExit,
  registerTransfer,
  type MovementResult,
  type Product,
  type StockBalance,
  type Storage,
} from '../services/api'

export type MovementKind = 'entry' | 'exit' | 'transfer' | 'adjustment'

interface MovementFormProps {
  kind: MovementKind
  item?: StockBalance // ausente só na entrada de um lote novo
  onDone: (result: MovementResult) => void
  onCancel: () => void
  onBusyChange?: (busy: boolean) => void
}

const SUBMIT_LABEL: Record<MovementKind, string> = {
  entry: 'Registrar entrada',
  exit: 'Registrar saída',
  transfer: 'Transferir',
  adjustment: 'Registrar ajuste',
}

// Campos que cada formulário mostra. Erros de campos que não aparecem na tela vão para o aviso geral.
const VISIBLE_FIELDS: Record<MovementKind, string[]> = {
  entry: ['ProductId', 'StorageId', 'Batch', 'Quantity'],
  exit: ['Quantity'],
  transfer: ['ToStorageId', 'Quantity'],
  adjustment: ['CountedQuantity', 'Reason'],
}

type Errors = Record<string, string[]>

function toErrors(err: unknown, kind: MovementKind, isNewLot: boolean): Errors {
  if (!axios.isAxiosError(err) || !err.response) {
    return { geral: ['Não foi possível falar com o servidor. Tente novamente.'] }
  }

  const { status, data } = err.response

  if (status === 400 && data?.errors) {
    const visible = isNewLot ? VISIBLE_FIELDS.entry : VISIBLE_FIELDS[kind]
    const result: Errors = {}
    for (const [field, messages] of Object.entries(data.errors as Errors)) {
      const key = visible.includes(field) ? field : 'geral'
      result[key] = [...(result[key] ?? []), ...messages]
    }
    return result
  }

  if (status === 422) {
    return { geral: [data?.detail ?? 'O movimento foi recusado.'] }
  }

  if (status === 409) {
    return {
      geral: [data?.detail ?? 'O saldo foi alterado por outra operação. Feche e confira o saldo atualizado.'],
    }
  }

  if (status === 403) {
    return { geral: ['Seu perfil não tem permissão para este movimento.'] }
  }

  return { geral: ['Não foi possível registrar o movimento.'] }
}

const inputClass =
  'mt-1 w-full rounded-lg border border-line bg-cream px-3 py-2.5 text-base text-ink outline-none transition focus:border-honey-dark sm:text-sm'

export function MovementForm({ kind, item, onDone, onCancel, onBusyChange }: MovementFormProps) {
  const isNewLot = kind === 'entry' && !item
  const needsStorages = isNewLot || kind === 'transfer'

  const [products, setProducts] = useState<Product[]>([])
  const [storages, setStorages] = useState<Storage[]>([])
  const [loadingOptions, setLoadingOptions] = useState(needsStorages)

  const [productId, setProductId] = useState('')
  const [storageId, setStorageId] = useState('')
  const [batch, setBatch] = useState('')
  const [toStorageId, setToStorageId] = useState('')
  const [quantity, setQuantity] = useState('')
  const [countedQuantity, setCountedQuantity] = useState(item ? String(item.balance) : '')
  const [reason, setReason] = useState('')

  const [errors, setErrors] = useState<Errors>({})
  const [submitting, setSubmitting] = useState(false)

  useEffect(() => {
    if (!needsStorages) return

    Promise.all([isNewLot ? getProducts() : Promise.resolve([]), getStorages()])
      .then(([loadedProducts, loadedStorages]) => {
        setProducts(loadedProducts.filter((p) => p.isActive))
        setStorages(
          loadedStorages.filter((s) => s.isActive && s.id !== item?.storageId),
        )
      })
      .catch(() => setErrors({ geral: ['Não foi possível carregar produtos e armazéns.'] }))
      .finally(() => setLoadingOptions(false))
  }, [needsStorages, isNewLot, item?.storageId])

  function setBusy(busy: boolean) {
    setSubmitting(busy)
    onBusyChange?.(busy)
  }

  async function handleSubmit(event: FormEvent) {
    event.preventDefault()
    setErrors({})
    setBusy(true)

    try {
      let result: MovementResult
      if (isNewLot) {
        result = await registerEntry({ productId, storageId, batch, quantity: Number(quantity) })
      } else if (!item) {
        setBusy(false)
        return
      } else if (kind === 'entry') {
        result = await registerEntry({ ...base(item), quantity: Number(quantity) })
      } else if (kind === 'exit') {
        result = await registerExit({ ...base(item), quantity: Number(quantity) })
      } else if (kind === 'transfer') {
        result = await registerTransfer({
          productId: item.productId,
          fromStorageId: item.storageId,
          toStorageId,
          batch: item.batch,
          quantity: Number(quantity),
        })
      } else {
        result = await registerAdjustment({
          ...base(item),
          countedQuantity: Number(countedQuantity),
          reason: reason.trim(),
        })
      }
      setBusy(false)
      onDone(result)
    } catch (err) {
      setErrors(toErrors(err, kind, isNewLot))
      setBusy(false)
    }
  }

  function fieldErrors(field: string) {
    const messages = errors[field]
    if (!messages?.length) return null
    return (
      <p className="mt-1 text-xs text-danger" role="alert">
        {messages.join(' ')}
      </p>
    )
  }

  if (loadingOptions) {
    return <p className="text-sm text-ink-muted">Carregando opções...</p>
  }

  const counted = countedQuantity === '' ? null : Number(countedQuantity)
  const difference = item && counted !== null && !Number.isNaN(counted) ? counted - item.balance : null

  return (
    <form onSubmit={handleSubmit} className="grid gap-4">
      {isNewLot && (
        <>
          <div>
            <label htmlFor="mv-product" className="text-sm font-medium text-ink-muted">
              Produto
            </label>
            <select
              id="mv-product"
              required
              value={productId}
              onChange={(event) => setProductId(event.target.value)}
              className={inputClass}
            >
              <option value="">Selecione...</option>
              {products.map((product) => (
                <option key={product.id} value={product.id}>
                  {product.name} ({product.sku})
                </option>
              ))}
            </select>
            {fieldErrors('ProductId')}
          </div>

          <div className="grid gap-4 sm:grid-cols-2">
            <div>
              <label htmlFor="mv-storage" className="text-sm font-medium text-ink-muted">
                Armazém
              </label>
              <select
                id="mv-storage"
                required
                value={storageId}
                onChange={(event) => setStorageId(event.target.value)}
                className={inputClass}
              >
                <option value="">Selecione...</option>
                {storages.map((storage) => (
                  <option key={storage.id} value={storage.id}>
                    {storage.name} ({storage.idNumber})
                  </option>
                ))}
              </select>
              {fieldErrors('StorageId')}
            </div>

            <div>
              <label htmlFor="mv-batch" className="text-sm font-medium text-ink-muted">
                Lote
              </label>
              <input
                id="mv-batch"
                required
                maxLength={30}
                autoCapitalize="characters"
                value={batch}
                onChange={(event) => setBatch(event.target.value.toUpperCase())}
                className={`${inputClass} font-mono`}
              />
              <p className="mt-1 text-xs text-ink-muted">Letras maiúsculas, números e hífen.</p>
              {fieldErrors('Batch')}
            </div>
          </div>
        </>
      )}

      {kind === 'transfer' && (
        <div>
          <label htmlFor="mv-to" className="text-sm font-medium text-ink-muted">
            Armazém de destino
          </label>
          <select
            id="mv-to"
            required
            value={toStorageId}
            onChange={(event) => setToStorageId(event.target.value)}
            className={inputClass}
          >
            <option value="">Selecione...</option>
            {storages.map((storage) => (
              <option key={storage.id} value={storage.id}>
                {storage.name} ({storage.idNumber})
              </option>
            ))}
          </select>
          {storages.length === 0 && (
            <p className="mt-1 text-xs text-ink-muted">Não há outro armazém ativo para receber.</p>
          )}
          {fieldErrors('ToStorageId')}
        </div>
      )}

      {kind !== 'adjustment' && (
        <div>
          <label htmlFor="mv-quantity" className="text-sm font-medium text-ink-muted">
            Quantidade
          </label>
          <input
            id="mv-quantity"
            type="number"
            inputMode="numeric"
            min="1"
            max={kind === 'exit' || kind === 'transfer' ? item?.balance : undefined}
            step="1"
            required
            value={quantity}
            onChange={(event) => setQuantity(event.target.value)}
            className={`${inputClass} tabular-nums`}
          />
          {item && (kind === 'exit' || kind === 'transfer') && (
            <p className="mt-1 text-xs text-ink-muted">
              Disponível: {item.balance.toLocaleString('pt-BR')}
            </p>
          )}
          {fieldErrors('Quantity')}
        </div>
      )}

      {kind === 'adjustment' && item && (
        <>
          <div>
            <label htmlFor="mv-counted" className="text-sm font-medium text-ink-muted">
              Quantidade contada
            </label>
            <input
              id="mv-counted"
              type="number"
              inputMode="numeric"
              min="0"
              step="1"
              required
              aria-describedby="mv-counted-hint"
              value={countedQuantity}
              onChange={(event) => setCountedQuantity(event.target.value)}
              className={`${inputClass} tabular-nums`}
            />
            <p id="mv-counted-hint" className="mt-1 text-xs text-ink-muted">
              Informe o total que você contou fisicamente, não a diferença. O sistema calcula o
              ajuste.
            </p>
            {fieldErrors('CountedQuantity')}
          </div>

          <dl className="grid grid-cols-3 gap-2 rounded-lg border border-line bg-cream px-3 py-3 text-sm">
            <div>
              <dt className="text-xs text-ink-muted">Saldo atual</dt>
              <dd className="tabular-nums text-ink">{item.balance.toLocaleString('pt-BR')}</dd>
            </div>
            <div>
              <dt className="text-xs text-ink-muted">Contado</dt>
              <dd className="tabular-nums text-ink">
                {counted === null || Number.isNaN(counted) ? '—' : counted.toLocaleString('pt-BR')}
              </dd>
            </div>
            <div>
              <dt className="text-xs text-ink-muted">Ajuste</dt>
              <dd
                className={`tabular-nums font-semibold ${
                  difference === null || difference === 0
                    ? 'text-ink-muted'
                    : difference > 0
                      ? 'text-honey-text'
                      : 'text-danger'
                }`}
              >
                {difference === null
                  ? '—'
                  : `${difference > 0 ? '+' : ''}${difference.toLocaleString('pt-BR')}`}
              </dd>
            </div>
          </dl>

          <div>
            <label htmlFor="mv-reason" className="text-sm font-medium text-ink-muted">
              Motivo
            </label>
            <textarea
              id="mv-reason"
              required
              minLength={5}
              maxLength={250}
              rows={3}
              value={reason}
              onChange={(event) => setReason(event.target.value)}
              placeholder="Ex.: inventário mensal, avaria encontrada na contagem"
              className={`${inputClass} resize-none`}
            />
            <p className="mt-1 text-right text-xs tabular-nums text-ink-muted">{reason.length}/250</p>
            {fieldErrors('Reason')}
          </div>
        </>
      )}

      {errors.geral?.length ? (
        <p className="rounded-lg bg-danger/10 px-3 py-2 text-sm text-danger" role="alert">
          {errors.geral.join(' ')}
        </p>
      ) : null}

      <div className="flex flex-col-reverse gap-2 pt-1 sm:flex-row sm:items-center sm:justify-end sm:gap-3">
        <button
          type="button"
          onClick={onCancel}
          disabled={submitting}
          className="h-11 rounded-full px-5 text-sm font-medium text-ink-muted transition hover:text-ink disabled:opacity-60 sm:h-auto sm:py-2"
        >
          Cancelar
        </button>
        <button
          type="submit"
          disabled={submitting}
          className="h-11 rounded-full bg-honey px-6 text-sm font-semibold text-hive transition hover:bg-honey-light disabled:opacity-60 sm:h-auto sm:py-2"
        >
          {submitting ? 'Registrando...' : SUBMIT_LABEL[kind]}
        </button>
      </div>
    </form>
  )
}

function base(item: StockBalance) {
  return { productId: item.productId, storageId: item.storageId, batch: item.batch }
}
