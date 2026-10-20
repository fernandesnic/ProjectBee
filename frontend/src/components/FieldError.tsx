// Mensagens de erro de um campo, como vêm da API (400 de validação) ou da checagem local.
export function FieldError({ messages }: { messages?: string[] }) {
  if (!messages?.length) return null
  return (
    <p className="mt-1 text-xs text-danger" role="alert">
      {messages.join(' ')}
    </p>
  )
}
