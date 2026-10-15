import { useEffect } from 'react'

interface ToastProps {
  message: string | null
  onDismiss: () => void
}

export function Toast({ message, onDismiss }: ToastProps) {
  useEffect(() => {
    if (!message) return
    const timer = setTimeout(onDismiss, 3000)
    return () => clearTimeout(timer)
  }, [message, onDismiss])

  if (!message) return null

  return (
    <div
      role="status"
      aria-live="polite"
      className="fixed bottom-4 left-4 right-4 z-50 flex items-center gap-2 sm:bottom-6 sm:left-auto sm:right-6 sm:max-w-sm rounded-xl bg-hive px-4 py-3 text-sm font-medium text-cream shadow-lg"
    >
      <span className="text-honey" aria-hidden="true">
        ✓
      </span>
      {message}
    </div>
  )
}
